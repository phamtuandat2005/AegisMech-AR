using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using MechChess.Chess3D;
using MechChess.UI;

namespace MechChess.Multiplayer
{
    [Serializable]
    public class LichessGameState
    {
        public string type;
        public string id;
        public string speed;
        public string moves;
        public string status; // "created", "started", "mate", "resign", "outoftime", "draw"
        public string winner; // "white", "black"
        public LichessPlayer white;
        public LichessPlayer black;
    }

    [Serializable]
    public class LichessPlayer
    {
        public string id;
        public string name;
        public int rating = 400;
    }

    /// <summary>
    /// Lichess Online Bridge for AegisMech AR:
    /// Seamlessly connects the 3D Mech AR board to Lichess.org via the official Lichess Board API.
    /// Opponents on web browsers/phones see a standard 2D chess game; 
    /// the local player experiences full 3D sci-fi mechs and AR holograms!
    /// Upon conclusion, results trigger Playchess dynamic ELO (+8/-8 or +7/-9) and Gold rewards.
    /// </summary>
    public class LichessBridge : MonoBehaviour
    {
        public static LichessBridge Instance { get; private set; }

        private const string PrefsKeyLichessToken = "MECH_LICHESS_TOKEN_SEC";
        private const string PrefsKeyLichessTokenLegacy = "MECH_LICHESS_TOKEN";
        private const string LichessApiBase = "https://lichess.org/api";
        private static readonly byte[] ObfKey = Encoding.UTF8.GetBytes("AegisMechAR_SecKey_2026");

        [Header("State")]
        public bool IsInLichessMatch { get; private set; } = false;
        public string ActiveGameId { get; private set; } = "";
        public bool IsPlayerWhite { get; private set; } = true;
        public string OpponentName { get; private set; } = "Lichess Player";
        public int OpponentElo { get; private set; } = 400;
        public bool IsMyTurn { get; private set; } = true;

        public event Action<string, int, bool> OnGameStarted; // oppName, oppElo, isWhite
        public event Action<string, string> OnOpponentMoved;   // uciMove, san
        public event Action<string, int, int> OnGameEnded;     // result ("win"/"loss"/"draw"), eloChange, goldGained

        private Coroutine _streamCoroutine;
        private int _lastMoveCount = 0;
        private float _matchStartTime = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                if (!HasToken)
                {
                    try
                    {
                        string localPath = Path.Combine(Application.dataPath, "..", ".env.local");
                        if (File.Exists(localPath))
                        {
                            string t = File.ReadAllText(localPath).Trim();
                            if (!string.IsNullOrEmpty(t) && t.StartsWith("lip_"))
                            {
                                SaveToken(t);
                                Debug.Log("[Lichess] Local token successfully initialized.");
                            }
                        }
                    }
                    catch { }
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public string GetSavedToken()
        {
            string enc = PlayerPrefs.GetString(PrefsKeyLichessToken, "");
            if (!string.IsNullOrEmpty(enc))
            {
                try
                {
                    byte[] cipherBytes = Convert.FromBase64String(enc);
                    byte[] plainBytes = new byte[cipherBytes.Length];
                    for (int i = 0; i < cipherBytes.Length; i++)
                    {
                        plainBytes[i] = (byte)(cipherBytes[i] ^ ObfKey[i % ObfKey.Length]);
                    }
                    return Encoding.UTF8.GetString(plainBytes);
                }
                catch
                {
                    // Fallback if decode failed
                }
            }

            // Fallback legacy plaintext
            string legacy = PlayerPrefs.GetString(PrefsKeyLichessTokenLegacy, "");
            if (!string.IsNullOrEmpty(legacy))
            {
                SaveToken(legacy); // Auto-migrate to encrypted storage
                PlayerPrefs.DeleteKey(PrefsKeyLichessTokenLegacy);
                PlayerPrefs.Save();
                return legacy;
            }

            return "";
        }

        public void SaveToken(string token)
        {
            string clean = (token ?? "").Trim();
            if (string.IsNullOrEmpty(clean))
            {
                PlayerPrefs.DeleteKey(PrefsKeyLichessToken);
                PlayerPrefs.DeleteKey(PrefsKeyLichessTokenLegacy);
            }
            else
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(clean);
                byte[] cipherBytes = new byte[plainBytes.Length];
                for (int i = 0; i < plainBytes.Length; i++)
                {
                    cipherBytes[i] = (byte)(plainBytes[i] ^ ObfKey[i % ObfKey.Length]);
                }
                PlayerPrefs.SetString(PrefsKeyLichessToken, Convert.ToBase64String(cipherBytes));
                PlayerPrefs.DeleteKey(PrefsKeyLichessTokenLegacy);
            }
            PlayerPrefs.Save();
        }

        public bool HasToken => !string.IsNullOrEmpty(GetSavedToken());

        public void CancelMatch()
        {
            IsInLichessMatch = false;
            ActiveGameId = "";
            StopAllCoroutines();
        }

        /// <summary>
        /// Seeks a live match against a real human player online on Lichess.org.
        /// </summary>
        public void SeekHumanMatch(int timeMinutes, int incrementSec, Action<bool, string> onComplete)
        {
            SeekHumanMatch(timeMinutes, incrementSec, 400, onComplete);
        }

        public void SeekHumanMatch(int timeMinutes = 10, int incrementSec = 0, int targetElo = 400, Action<bool, string> onComplete = null)
        {
            StartCoroutine(SeekMatchRoutine(timeMinutes, incrementSec, targetElo, onComplete));
        }

        /// <summary>
        /// Quick challenge against Lichess Stockfish AI engine (Level 1-8).
        /// Useful when testing or playing immediately with or without a full account.
        /// </summary>
        public void ChallengeLichessAI(int aiLevel = 1, Action<bool, string> onComplete = null)
        {
            StartCoroutine(ChallengeAiRoutine(aiLevel, onComplete));
        }

        private IEnumerator SeekMatchRoutine(int timeMinutes, int incrementSec, int targetElo, Action<bool, string> onComplete)
        {
            string token = GetSavedToken();
            if (string.IsNullOrEmpty(token))
            {
                onComplete?.Invoke(false, "Vui lòng nhập Lichess Personal Token (với quyền 'board:play') để tìm người đấu online!");
                yield break;
            }

            string url = $"{LichessApiBase}/board/seek";
            WWWForm form = new WWWForm();
            form.AddField("time", timeMinutes);
            form.AddField("increment", incrementSec);
            form.AddField("rated", "false"); // Unrated for third-party client play

            if (targetElo >= 600)
            {
                int minRating = Mathf.Max(600, targetElo - 300);
                int maxRating = targetElo + 300;
                form.AddField("ratingRange", $"{minRating}-{maxRating}");
            }

            using (UnityWebRequest req = UnityWebRequest.Post(url, form))
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
                req.timeout = 30;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    // Lichess will stream incoming events or assign a game ID
                    Debug.Log("[Lichess] Seek request sent successfully. Waiting for match pairing...");
                    onComplete?.Invoke(true, "Đang tìm đối thủ trên Lichess.org...");
                    StartListeningToEvents();
                }
                else
                {
                    string err = req.downloadHandler != null ? req.downloadHandler.text : req.error;
                    Debug.LogWarning($"[Lichess] Seek failed: {err}");
                    onComplete?.Invoke(false, $"Không thể kết nối Lichess: {err}");
                }
            }
        }

        private IEnumerator ChallengeAiRoutine(int aiLevel, Action<bool, string> onComplete)
        {
            string token = GetSavedToken();
            string url = $"{LichessApiBase}/challenge/ai";
            WWWForm form = new WWWForm();
            form.AddField("level", Mathf.Clamp(aiLevel, 1, 8));
            form.AddField("clock.limit", 600); // 10 minutes
            form.AddField("clock.increment", 0);

            using (UnityWebRequest req = UnityWebRequest.Post(url, form))
            {
                if (!string.IsNullOrEmpty(token))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + token);
                }
                req.timeout = 15;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string json = req.downloadHandler.text;
                    Debug.Log($"[Lichess] AI Game created: {json}");
                    try
                    {
                        var state = JsonUtility.FromJson<LichessGameState>(json);
                        if (state != null && !string.IsNullOrEmpty(state.id))
                        {
                            StartGameSession(state.id, true, $"Lichess Stockfish (Lv {aiLevel})", 400 + (aiLevel - 1) * 200);
                            onComplete?.Invoke(true, $"Đã ghép trận với Lichess Stockfish Level {aiLevel}!");
                        }
                        else
                        {
                            onComplete?.Invoke(true, "Đã tạo trận đấu với Lichess AI!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Lichess] Parse error: {ex.Message}");
                        onComplete?.Invoke(true, "Trận đấu đã sẵn sàng!");
                    }
                }
                else
                {
                    string err = req.downloadHandler != null ? req.downloadHandler.text : req.error;
                    onComplete?.Invoke(false, $"Lỗi tạo trận: {err}");
                }
            }
        }

        public void StartGameSession(string gameId, bool playerIsWhite, string oppName, int oppElo)
        {
            ActiveGameId = gameId;
            IsPlayerWhite = playerIsWhite;
            OpponentName = string.IsNullOrEmpty(oppName) ? "Lichess Opponent" : oppName;
            OpponentElo = oppElo > 0 ? oppElo : 400;
            IsInLichessMatch = true;
            IsMyTurn = playerIsWhite;
            _lastMoveCount = 0;
            _matchStartTime = Time.time;

            Debug.Log($"[Lichess] Game started! ID: {gameId} | White: {playerIsWhite} | Opponent: {OpponentName} (Elo: {OpponentElo})");
            OnGameStarted?.Invoke(OpponentName, OpponentElo, IsPlayerWhite);

            if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);
            _streamCoroutine = StartCoroutine(StreamGameRoutine(gameId));
        }

        private void StartListeningToEvents()
        {
            // Event listener loop for Lichess board events
            if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);
            _streamCoroutine = StartCoroutine(StreamEventRoutine());
        }

        private IEnumerator StreamEventRoutine()
        {
            string token = GetSavedToken();
            if (string.IsNullOrEmpty(token)) yield break;

            string url = $"{LichessApiBase}/stream/event";
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
                req.downloadHandler = new DownloadHandlerBuffer();
                var op = req.SendWebRequest();

                while (!op.isDone)
                {
                    string text = req.downloadHandler.text;
                    if (!string.IsNullOrEmpty(text) && text.Contains("\"gameStart\""))
                    {
                        string gid = "";
                        string oppName = "Lichess Opponent";
                        int oppElo = 400;
                        bool playerIsWhite = true;

                        // Parse game ID
                        int idx = text.IndexOf("\"id\":\"", StringComparison.OrdinalIgnoreCase);
                        if (idx > 0)
                        {
                            string sub = text.Substring(idx + 6);
                            int end = sub.IndexOf("\"");
                            if (end > 0) gid = sub.Substring(0, end);
                        }

                        // Parse assigned color
                        int idxCol = text.IndexOf("\"color\":\"", StringComparison.OrdinalIgnoreCase);
                        if (idxCol > 0)
                        {
                            string sub = text.Substring(idxCol + 9);
                            int end = sub.IndexOf("\"");
                            if (end > 0)
                            {
                                string col = sub.Substring(0, end).ToLower();
                                playerIsWhite = (col == "white");
                            }
                        }

                        // Parse opponent name & rating
                        int idxOpp = text.IndexOf("\"opponent\":", StringComparison.OrdinalIgnoreCase);
                        if (idxOpp > 0)
                        {
                            string subOpp = text.Substring(idxOpp);
                            int endOpp = subOpp.IndexOf("}");
                            if (endOpp > 0)
                            {
                                string oppBlock = subOpp.Substring(0, endOpp);
                                int idxUname = oppBlock.IndexOf("\"username\":\"", StringComparison.OrdinalIgnoreCase);
                                if (idxUname < 0) idxUname = oppBlock.IndexOf("\"name\":\"", StringComparison.OrdinalIgnoreCase);
                                if (idxUname > 0)
                                {
                                    int uStart = oppBlock.IndexOf("\"", idxUname + 6) + 1;
                                    int uEnd = oppBlock.IndexOf("\"", uStart);
                                    if (uEnd > uStart) oppName = oppBlock.Substring(uStart, uEnd - uStart);
                                }

                                int idxRating = oppBlock.IndexOf("\"rating\":", StringComparison.OrdinalIgnoreCase);
                                if (idxRating > 0)
                                {
                                    string rSub = oppBlock.Substring(idxRating + 9).Trim();
                                    string digits = "";
                                    foreach (char c in rSub)
                                    {
                                        if (char.IsDigit(c)) digits += c;
                                        else break;
                                    }
                                    if (int.TryParse(digits, out int r)) oppElo = r;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(gid))
                        {
                            StartGameSession(gid, playerIsWhite, oppName, oppElo);
                            yield break;
                        }
                    }
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        private IEnumerator StreamGameRoutine(string gameId)
        {
            string token = GetSavedToken();
            string url = $"{LichessApiBase}/board/game/stream/{gameId}";

            while (IsInLichessMatch && !string.IsNullOrEmpty(ActiveGameId))
            {
                using (UnityWebRequest req = UnityWebRequest.Get(url))
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        req.SetRequestHeader("Authorization", "Bearer " + token);
                    }
                    req.downloadHandler = new DownloadHandlerBuffer();
                    var op = req.SendWebRequest();

                    int processedBytes = 0;
                    while (!op.isDone && IsInLichessMatch)
                    {
                        if (req.downloadHandler != null && req.downloadHandler.data != null)
                        {
                            int currentLen = req.downloadHandler.data.Length;
                            if (currentLen > processedBytes)
                            {
                                string chunk = Encoding.UTF8.GetString(req.downloadHandler.data, processedBytes, currentLen - processedBytes);
                                processedBytes = currentLen;
                                ProcessStreamChunk(chunk);
                            }
                        }
                        yield return new WaitForSeconds(0.2f);
                    }

                    if (req.downloadHandler != null && req.downloadHandler.data != null)
                    {
                        int currentLen = req.downloadHandler.data.Length;
                        if (currentLen > processedBytes)
                        {
                            string chunk = Encoding.UTF8.GetString(req.downloadHandler.data, processedBytes, currentLen - processedBytes);
                            ProcessStreamChunk(chunk);
                        }
                    }
                }
                yield return new WaitForSeconds(1.0f);
            }
        }

        private void ProcessStreamChunk(string chunk)
        {
            if (string.IsNullOrEmpty(chunk)) return;

            string[] lines = chunk.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith("{")) continue;

                try
                {
                    if (trimmed.Contains("\"moves\":"))
                    {
                        int mIdx = trimmed.IndexOf("\"moves\":\"", StringComparison.OrdinalIgnoreCase);
                        if (mIdx > 0)
                        {
                            string sub = trimmed.Substring(mIdx + 9);
                            int end = sub.IndexOf("\"");
                            if (end > 0)
                            {
                                string movesStr = sub.Substring(0, end).Trim();
                                string[] moveList = movesStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (moveList.Length > _lastMoveCount)
                                {
                                    string latestMove = moveList[moveList.Length - 1];
                                    bool isWhiteMove = (moveList.Length % 2 == 1);
                                    bool wasOpponentMove = (isWhiteMove != IsPlayerWhite);

                                    _lastMoveCount = moveList.Length;
                                    IsMyTurn = !wasOpponentMove;

                                    if (wasOpponentMove)
                                    {
                                        Debug.Log($"[Lichess] Opponent move received: {latestMove}");
                                        OnOpponentMoved?.Invoke(latestMove, latestMove);
                                    }
                                }
                            }
                        }
                    }

                    // Check game conclusion status
                    if (trimmed.Contains("\"winner\":") || trimmed.Contains("\"status\":\"mate\"") || trimmed.Contains("\"status\":\"resign\""))
                    {
                        string result = "draw";
                        if (trimmed.Contains("\"winner\":\"white\""))
                        {
                            result = IsPlayerWhite ? "win" : "loss";
                        }
                        else if (trimmed.Contains("\"winner\":\"black\""))
                        {
                            result = !IsPlayerWhite ? "win" : "loss";
                        }

                        EndGameSession(result);
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Lichess] Error processing stream chunk: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Sends local player's move to Lichess server (e.g. "e2e4", "g1f3", "e7e8q").
        /// </summary>
        public void SendMove(string uciMove, Action<bool> onComplete = null)
        {
            if (!IsInLichessMatch || string.IsNullOrEmpty(ActiveGameId))
            {
                onComplete?.Invoke(false);
                return;
            }

            StartCoroutine(SendMoveRoutine(ActiveGameId, uciMove, onComplete));
        }

        private IEnumerator SendMoveRoutine(string gameId, string uciMove, Action<bool> onComplete)
        {
            string token = GetSavedToken();
            string url = $"{LichessApiBase}/board/game/{gameId}/move/{uciMove.ToLower()}";

            using (UnityWebRequest req = UnityWebRequest.PostWwwForm(url, ""))
            {
                if (!string.IsNullOrEmpty(token))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + token);
                }
                req.timeout = 6;
                yield return req.SendWebRequest();

                bool ok = (req.result == UnityWebRequest.Result.Success);
                if (ok)
                {
                    Debug.Log($"[Lichess] Move '{uciMove}' sent successfully!");
                    IsMyTurn = false;
                }
                else
                {
                    Debug.LogWarning($"[Lichess] Send move '{uciMove}' failed: {req.error}");
                }
                onComplete?.Invoke(ok);
            }
        }

        private void EndGameSession(string result)
        {
            if (!IsInLichessMatch) return;
            IsInLichessMatch = false;

            if (_streamCoroutine != null)
            {
                StopCoroutine(_streamCoroutine);
                _streamCoroutine = null;
            }

            float duration = Mathf.Max(30f, Time.time - _matchStartTime);

            // Record match result using Playchess-style ELO and Gold rewards
            if (UserAccountManager.Instance != null)
            {
                UserAccountManager.Instance.RecordMatchResult(
                    result: result,
                    opponentName: OpponentName,
                    isAi: false,
                    role: IsPlayerWhite ? "white" : "black",
                    moves: _lastMoveCount,
                    durationSec: duration,
                    opponentElo: OpponentElo,
                    onComplete: (ok, res) =>
                    {
                        int eloDelta = (res != null) ? res.elo_change : (result == "win" ? 8 : (result == "loss" ? -8 : 0));
                        int goldGain = (res != null) ? res.gold_gained : (result == "win" ? 100 : 20);
                        OnGameEnded?.Invoke(result, eloDelta, goldGain);
                    }
                );
            }
            else
            {
                int eloDelta = (result == "win") ? 8 : (result == "loss" ? -8 : 0);
                int goldGain = (result == "win") ? 100 : 20;
                OnGameEnded?.Invoke(result, eloDelta, goldGain);
            }
        }
    }
}
