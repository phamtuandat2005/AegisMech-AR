using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace MechChess.Multiplayer
{
    [Serializable]
    public class UserAccountData
    {
        public string id;
        public string gmail;
        public string username;
        public string avatar_url = "profile_avatar_round.png";
        public int level = 1;
        public int exp = 0;
        public int max_exp = 100;
        public int gold = 500;
        public int gems = 50;
        public int energy = 100;
        public int elo = 1200;
        public int matches_played = 0;
        public int wins = 0;
        public int losses = 0;
        public int draws = 0;
        public float win_rate = 0f;
        public string created_at;
    }

    [Serializable]
    public class LoginApiResponse
    {
        public bool success;
        public string message;
        public UserAccountData user;
        public string access_token;
        public string token_type;
    }

    [Serializable]
    public class MatchStartResponse
    {
        public bool success;
        public string match_id;
        public string match_ticket;
        public float issued_at;
    }

    [Serializable]
    public class MatchHistoryItem
    {
        public string id;
        public string match_code;
        public string opponent_name;
        public bool is_ai;
        public string player_role;
        public string result;
        public string score;
        public int elo_change;
        public int moves_count;
        public float duration_sec;
        public string ended_at;
    }

    [Serializable]
    public class MatchHistoryResponse
    {
        public int total;
        public MatchHistoryItem[] matches;
    }

    [Serializable]
    public class MatchRecordResponse
    {
        public bool success;
        public string message;
        public int elo_change;
        public int new_elo;
        public int exp_gained;
        public int gold_gained;
        public bool leveled_up;
        public int new_level;
        public int new_exp;
        public int new_gold;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string username;
        public int elo;
        public int level;
        public int wins;
        public int matches_played;
        public float win_rate;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public int total;
        public LeaderboardEntry[] leaderboard;
    }

    /// <summary>
    /// User Account Manager:
    /// Manages player Gmail authentication, unique alphanumeric username enforcement,
    /// dynamic Supabase PostgreSQL cloud sync, and session state.
    /// </summary>
    public class UserAccountManager : MonoBehaviour
    {
        public static UserAccountManager Instance { get; private set; }

        private const string PrefsKeyGmail = "MECH_USER_GMAIL";
        private const string PrefsKeyUsername = "MECH_USER_NAME";
        private const string PrefsKeyElo = "MECH_USER_ELO";
        private const string PrefsKeyLevel = "MECH_USER_LEVEL";
        private const string PrefsKeyGold = "MECH_USER_GOLD";
        private const string PrefsKeyGems = "MECH_USER_GEMS";
        private const string PrefsKeyAuthToken = "MECH_AUTH_TOKEN";

        public string CurrentGmail { get; private set; } = "";
        public string CurrentUsername { get; private set; } = "";
        public int CurrentElo { get; private set; } = 1200;
        public string AuthToken { get; private set; } = "";
        public string CurrentMatchTicket { get; private set; } = "";
        public string CurrentMatchId { get; private set; } = "";

        public UserAccountData CurrentProfile { get; private set; }

        public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentGmail) && !string.IsNullOrEmpty(CurrentUsername);

        public event Action<string, string, int> OnAccountUpdated; // username, gmail, elo
        public event Action<UserAccountData> OnProfileUpdated;
        public event Action OnLoggedOut;

        public string backendApiBaseUrl = "http://localhost:8000";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSavedAccount();
            }
            else
            {
                Destroy(this);
            }
        }

        public void LoadSavedAccount()
        {
            CurrentGmail = PlayerPrefs.GetString(PrefsKeyGmail, "");
            CurrentUsername = PlayerPrefs.GetString(PrefsKeyUsername, "");
            CurrentElo = PlayerPrefs.GetInt(PrefsKeyElo, 1200);
            AuthToken = PlayerPrefs.GetString(PrefsKeyAuthToken, "");

            if (IsLoggedIn)
            {
                CurrentProfile = new UserAccountData
                {
                    gmail = CurrentGmail,
                    username = CurrentUsername,
                    elo = CurrentElo,
                    level = PlayerPrefs.GetInt(PrefsKeyLevel, 1),
                    gold = PlayerPrefs.GetInt(PrefsKeyGold, 500),
                    gems = PlayerPrefs.GetInt(PrefsKeyGems, 50),
                    energy = 100
                };

                Debug.Log($"[UserAccount] Loaded saved user: {CurrentUsername} ({CurrentGmail}) | Elo: {CurrentElo}");
                OnAccountUpdated?.Invoke(CurrentUsername, CurrentGmail, CurrentElo);
                OnProfileUpdated?.Invoke(CurrentProfile);

                // Try fetching latest cloud data from Supabase backend in background
                FetchProfile();
            }
        }

        /// <summary>
        /// Validates username according to user specifications:
        /// Only alphanumeric characters (a-z, A-Z, 0-9), no spaces, no special characters, no accents/diacritics, continuous text.
        /// Valid examples: PhamVaA, DatIT, SiuVipPro, nhavua01, nhavua
        /// </summary>
        public static bool ValidateUsername(string username, out string error)
        {
            error = "";
            if (string.IsNullOrEmpty(username))
            {
                error = "Tên người chơi không được để trống!";
                return false;
            }

            string trimmed = username.Trim();
            if (trimmed.Length < 3 || trimmed.Length > 20)
            {
                error = "Độ dài tên người chơi phải từ 3 đến 20 ký tự!";
                return false;
            }

            if (trimmed.Contains(" "))
            {
                error = "Tên người chơi phải viết liền, không được chứa khoảng cách (space)!";
                return false;
            }

            // Regex: Letters and numbers only, continuous
            if (!Regex.IsMatch(trimmed, "^[a-zA-Z0-9]+$"))
            {
                error = "Tên chỉ được gồm chữ cái (A-Z, a-z) và chữ số (0-9), không có dấu tiếng Việt hay ký tự đặc biệt (Ví dụ: PhamVaA, DatIT, SiuVipPro, nhavua01).";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates Gmail format: must end with @gmail.com
        /// </summary>
        public static bool ValidateGmail(string gmail, out string error)
        {
            error = "";
            if (string.IsNullOrEmpty(gmail))
            {
                error = "Địa chỉ Gmail không được để trống!";
                return false;
            }

            string clean = gmail.Trim().ToLowerInvariant();
            if (!clean.EndsWith("@gmail.com") || clean.Length < 11)
            {
                error = "Vui lòng nhập địa chỉ Gmail hợp lệ (kết thúc bằng @gmail.com, ví dụ: nguyenvana@gmail.com)!";
                return false;
            }

            string prefix = clean.Substring(0, clean.IndexOf("@gmail.com"));
            if (string.IsNullOrEmpty(prefix))
            {
                error = "Địa chỉ Gmail không hợp lệ!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Derives a clean default username from Gmail prefix if the player does not specify one.
        /// Example: nguyenvana@gmail.com -> nguyenvana
        /// </summary>
        public static string DeriveDefaultUsername(string gmail)
        {
            if (string.IsNullOrEmpty(gmail)) return "Player01";

            string prefix = gmail.Split('@')[0];
            string clean = Regex.Replace(prefix, "[^a-zA-Z0-9]", "");
            if (clean.Length < 3) clean += "01";
            if (clean.Length > 20) clean = clean.Substring(0, 20);
            return clean;
        }

        /// <summary>
        /// Authenticates or registers player with Gmail and optional custom username.
        /// </summary>
        public void Login(string gmail, string customUsername, Action<bool, string> onComplete)
        {
            if (!ValidateGmail(gmail, out string gmailErr))
            {
                onComplete?.Invoke(false, gmailErr);
                return;
            }

            string targetName = string.IsNullOrEmpty(customUsername) ? DeriveDefaultUsername(gmail) : customUsername.Trim();
            if (!ValidateUsername(targetName, out string nameErr))
            {
                onComplete?.Invoke(false, nameErr);
                return;
            }

            StartCoroutine(LoginRoutine(gmail.Trim().ToLowerInvariant(), targetName, onComplete));
        }

        private IEnumerator LoginRoutine(string cleanGmail, string cleanUsername, Action<bool, string> onComplete)
        {
            string url = $"{backendApiBaseUrl.TrimEnd('/')}/api/auth/login";
            string jsonBody = $"{{\"gmail\":\"{cleanGmail}\",\"username\":\"{cleanUsername}\"}}";

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        LoginApiResponse res = JsonUtility.FromJson<LoginApiResponse>(req.downloadHandler.text);
                        if (res != null && res.user != null)
                        {
                            if (!string.IsNullOrEmpty(res.access_token))
                            {
                                AuthToken = res.access_token;
                                PlayerPrefs.SetString(PrefsKeyAuthToken, AuthToken);
                                PlayerPrefs.Save();
                            }
                            ApplyAccount(cleanGmail, res.user.username, res.user.elo);
                            onComplete?.Invoke(true, "Đăng nhập thành công!");
                            yield break;
                        }
                    }
                    catch { }
                }

                // If backend returned error (e.g. username taken by someone else)
                if (req.responseCode == 400 && !string.IsNullOrEmpty(req.downloadHandler.text))
                {
                    string errText = req.downloadHandler.text;
                    try
                    {
                        // Parse FastAPI detail message
                        int detailIdx = errText.IndexOf("\"detail\":");
                        if (detailIdx >= 0)
                        {
                            string rawDetail = errText.Substring(detailIdx + 9).Trim().Trim('}', '"');
                            errText = rawDetail;
                        }
                    }
                    catch { }
                    onComplete?.Invoke(false, errText);
                    yield break;
                }

                // Fallback offline registration if local Docker server is not yet running
                Debug.LogWarning($"[UserAccount] Backend unavailable ({req.error}), falling back to local session validation.");
                ApplyAccount(cleanGmail, cleanUsername, 1200);
                onComplete?.Invoke(true, "Đăng nhập thành công (Chế độ Cục Bộ)!");
            }
        }

        private void ApplyAccount(string gmail, string username, int elo, UserAccountData fullProfile = null)
        {
            CurrentGmail = gmail;
            CurrentUsername = username;
            CurrentElo = elo;

            if (fullProfile != null)
            {
                CurrentProfile = fullProfile;
            }
            else
            {
                CurrentProfile = new UserAccountData
                {
                    gmail = CurrentGmail,
                    username = CurrentUsername,
                    elo = CurrentElo,
                    level = PlayerPrefs.GetInt(PrefsKeyLevel, 1),
                    gold = PlayerPrefs.GetInt(PrefsKeyGold, 500),
                    gems = PlayerPrefs.GetInt(PrefsKeyGems, 50),
                    energy = 100
                };
            }

            PlayerPrefs.SetString(PrefsKeyGmail, CurrentGmail);
            PlayerPrefs.SetString(PrefsKeyUsername, CurrentUsername);
            PlayerPrefs.SetInt(PrefsKeyElo, CurrentElo);
            PlayerPrefs.SetInt(PrefsKeyLevel, CurrentProfile.level);
            PlayerPrefs.SetInt(PrefsKeyGold, CurrentProfile.gold);
            PlayerPrefs.SetInt(PrefsKeyGems, CurrentProfile.gems);
            PlayerPrefs.Save();

            // Sync with OnlineMultiplayerManager
            if (OnlineMultiplayerManager.Instance != null)
            {
                OnlineMultiplayerManager.Instance.playerName = CurrentUsername;
                OnlineMultiplayerManager.Instance.ConnectToLobby();
            }

            OnAccountUpdated?.Invoke(CurrentUsername, CurrentGmail, CurrentElo);
            OnProfileUpdated?.Invoke(CurrentProfile);
        }

        /// <summary>
        /// Fetches full player profile (Level, Exp, Gold, Gems, Elo, Win/Loss/Draw) from Supabase FastAPI.
        /// Safe fallback if offline or backend is unreachable.
        /// </summary>
        public void FetchProfile(Action<bool, UserAccountData> onComplete = null)
        {
            if (string.IsNullOrEmpty(CurrentUsername))
            {
                onComplete?.Invoke(false, null);
                return;
            }
            StartCoroutine(FetchProfileRoutine(CurrentUsername, onComplete));
        }

        private IEnumerator FetchProfileRoutine(string username, Action<bool, UserAccountData> onComplete)
        {
            string url = $"{backendApiBaseUrl.TrimEnd('/')}/api/player/profile?username={UnityWebRequest.EscapeURL(username)}";
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(AuthToken))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + AuthToken);
                }
                req.timeout = 5;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        UserAccountData profile = JsonUtility.FromJson<UserAccountData>(req.downloadHandler.text);
                        if (profile != null)
                        {
                            ApplyAccount(profile.gmail, profile.username, profile.elo, profile);
                            onComplete?.Invoke(true, CurrentProfile);
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UserAccount] Failed parsing profile: {ex.Message}");
                    }
                }
                onComplete?.Invoke(false, CurrentProfile);
            }
        }

        /// <summary>
        /// Fetches match history list from Supabase backend.
        /// </summary>
        public void FetchMatchHistory(int limit, Action<bool, MatchHistoryItem[]> onComplete)
        {
            if (string.IsNullOrEmpty(CurrentUsername))
            {
                onComplete?.Invoke(false, null);
                return;
            }
            StartCoroutine(FetchMatchHistoryRoutine(CurrentUsername, limit, onComplete));
        }

        private IEnumerator FetchMatchHistoryRoutine(string username, int limit, Action<bool, MatchHistoryItem[]> onComplete)
        {
            string url = $"{backendApiBaseUrl.TrimEnd('/')}/api/player/history?username={UnityWebRequest.EscapeURL(username)}&limit={limit}";
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(AuthToken))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + AuthToken);
                }
                req.timeout = 5;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        MatchHistoryResponse res = JsonUtility.FromJson<MatchHistoryResponse>(req.downloadHandler.text);
                        if (res != null && res.matches != null)
                        {
                            onComplete?.Invoke(true, res.matches);
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UserAccount] Failed parsing match history: {ex.Message}");
                    }
                }
                onComplete?.Invoke(false, null);
            }
        }

        /// <summary>
        /// Requests a signed cryptographic match ticket from the backend at match start.
        /// Prevents match result spoofing and replay attacks.
        /// </summary>
        public void StartMatchSession(string opponentName, bool isAi, string role, Action<bool, string> onComplete = null)
        {
            StartCoroutine(StartMatchSessionRoutine(opponentName, isAi, role, onComplete));
        }

        private IEnumerator StartMatchSessionRoutine(string opponentName, bool isAi, string role, Action<bool, string> onComplete)
        {
            string url = $"{backendApiBaseUrl.TrimEnd('/')}/api/match/start";
            string jsonBody = $"{{\"opponent_name\":\"{opponentName}\",\"is_ai\":{(isAi ? "true" : "false")},\"player_role\":\"{role}\"}}";

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(AuthToken))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + AuthToken);
                }
                req.timeout = 5;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        MatchStartResponse res = JsonUtility.FromJson<MatchStartResponse>(req.downloadHandler.text);
                        if (res != null && res.success)
                        {
                            CurrentMatchId = res.match_id;
                            CurrentMatchTicket = res.match_ticket;
                            onComplete?.Invoke(true, res.match_ticket);
                            yield break;
                        }
                    }
                    catch { }
                }
                onComplete?.Invoke(false, "");
            }
        }

        /// <summary>
        /// Securely records match result to Supabase backend in a database transaction.
        /// Backend updates Elo, calculates Exp and Gold rewards, and logs history.
        /// </summary>
        public void RecordMatchResult(string result, string opponentName, bool isAi, string role, int moves, float durationSec, Action<bool, MatchRecordResponse> onComplete = null)
        {
            if (string.IsNullOrEmpty(CurrentUsername))
            {
                onComplete?.Invoke(false, null);
                return;
            }
            StartCoroutine(RecordMatchRoutine(result, opponentName, isAi, role, moves, durationSec, onComplete));
        }

        private IEnumerator RecordMatchRoutine(string result, string opponentName, bool isAi, string role, int moves, float durationSec, Action<bool, MatchRecordResponse> onComplete)
        {
            string url = $"{backendApiBaseUrl.TrimEnd('/')}/api/match/record";
            string jsonBody;
            if (!string.IsNullOrEmpty(CurrentMatchTicket))
            {
                jsonBody = $"{{\"username\":\"{CurrentUsername}\",\"opponent_name\":\"{opponentName}\",\"is_ai\":{(isAi ? "true" : "false")},\"player_role\":\"{role}\",\"result\":\"{result}\",\"moves_count\":{moves},\"duration_sec\":{durationSec:F1},\"match_ticket\":\"{CurrentMatchTicket}\",\"match_id\":\"{CurrentMatchId}\"}}";
            }
            else
            {
                jsonBody = $"{{\"username\":\"{CurrentUsername}\",\"opponent_name\":\"{opponentName}\",\"is_ai\":{(isAi ? "true" : "false")},\"player_role\":\"{role}\",\"result\":\"{result}\",\"moves_count\":{moves},\"duration_sec\":{durationSec:F1}}}";
            }

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(AuthToken))
                {
                    req.SetRequestHeader("Authorization", "Bearer " + AuthToken);
                }
                req.timeout = 6;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        MatchRecordResponse res = JsonUtility.FromJson<MatchRecordResponse>(req.downloadHandler.text);
                        if (res != null && res.success)
                        {
                            CurrentMatchTicket = "";
                            CurrentMatchId = "";
                            CurrentElo = res.new_elo;
                            if (CurrentProfile != null)
                            {
                                CurrentProfile.elo = res.new_elo;
                                CurrentProfile.level = res.new_level;
                                CurrentProfile.exp = res.new_exp;
                                CurrentProfile.gold = res.new_gold;
                                CurrentProfile.matches_played += 1;
                                if (result == "win") CurrentProfile.wins += 1;
                                else if (result == "loss") CurrentProfile.losses += 1;
                                else CurrentProfile.draws += 1;

                                ApplyAccount(CurrentGmail, CurrentUsername, CurrentElo, CurrentProfile);
                            }
                            onComplete?.Invoke(true, res);
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UserAccount] Failed parsing match record response: {ex.Message}");
                    }
                }

                // Fallback offline progression if backend unreachable
                Debug.LogWarning($"[UserAccount] Match recording offline fallback ({req.error}).");
                int eloDelta = (result == "win") ? 12 : ((result == "loss") ? -8 : 0);
                CurrentElo = Mathf.Max(100, CurrentElo + eloDelta);
                if (CurrentProfile != null)
                {
                    CurrentProfile.elo = CurrentElo;
                    CurrentProfile.matches_played += 1;
                    if (result == "win") { CurrentProfile.wins += 1; CurrentProfile.gold += 50; }
                    else if (result == "loss") CurrentProfile.losses += 1;
                    else CurrentProfile.draws += 1;
                    ApplyAccount(CurrentGmail, CurrentUsername, CurrentElo, CurrentProfile);
                }
                MatchRecordResponse fallbackRes = new MatchRecordResponse
                {
                    success = true,
                    message = "Lưu kết quả cục bộ",
                    elo_change = eloDelta,
                    new_elo = CurrentElo,
                    gold_gained = (result == "win" ? 50 : 15),
                    exp_gained = (result == "win" ? 60 : 20),
                    new_level = (CurrentProfile != null ? CurrentProfile.level : 1),
                    new_gold = (CurrentProfile != null ? CurrentProfile.gold : 500)
                };
                onComplete?.Invoke(false, fallbackRes);
            }
        }

        /// <summary>
        /// Fetches global leaderboard list from Supabase backend.
        /// </summary>
        public void FetchLeaderboard(int limit, Action<bool, LeaderboardEntry[]> onComplete)
        {
            StartCoroutine(FetchLeaderboardRoutine(limit, onComplete));
        }

        private IEnumerator FetchLeaderboardRoutine(int limit, Action<bool, LeaderboardEntry[]> onComplete)
        {
            string url = $"{backendApiBaseUrl.TrimEnd('/')}/api/leaderboard?limit={limit}";
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        LeaderboardResponse res = JsonUtility.FromJson<LeaderboardResponse>(req.downloadHandler.text);
                        if (res != null && res.leaderboard != null)
                        {
                            onComplete?.Invoke(true, res.leaderboard);
                            yield break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UserAccount] Failed parsing leaderboard: {ex.Message}");
                    }
                }
                onComplete?.Invoke(false, null);
            }
        }

        public void Logout()
        {
            CurrentGmail = "";
            CurrentUsername = "";
            CurrentElo = 1200;
            CurrentProfile = null;
            AuthToken = "";
            CurrentMatchTicket = "";
            CurrentMatchId = "";

            PlayerPrefs.DeleteKey(PrefsKeyGmail);
            PlayerPrefs.DeleteKey(PrefsKeyUsername);
            PlayerPrefs.DeleteKey(PrefsKeyElo);
            PlayerPrefs.DeleteKey(PrefsKeyLevel);
            PlayerPrefs.DeleteKey(PrefsKeyGold);
            PlayerPrefs.DeleteKey(PrefsKeyGems);
            PlayerPrefs.DeleteKey(PrefsKeyAuthToken);
            PlayerPrefs.Save();

            if (OnlineMultiplayerManager.Instance != null)
            {
                OnlineMultiplayerManager.Instance.DisconnectLobby();
                OnlineMultiplayerManager.Instance.Disconnect();
            }

            OnLoggedOut?.Invoke();
        }
    }
}
