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
        public string gmail;
        public string username;
        public int elo = 1200;
        public float created_at;
    }

    [Serializable]
    public class LoginApiResponse
    {
        public bool success;
        public string message;
        public UserAccountData user;
    }

    /// <summary>
    /// User Account Manager:
    /// Manages player Gmail authentication, unique alphanumeric username enforcement,
    /// Elo rating persistence, and session state.
    /// </summary>
    public class UserAccountManager : MonoBehaviour
    {
        public static UserAccountManager Instance { get; private set; }

        private const string PrefsKeyGmail = "MECH_USER_GMAIL";
        private const string PrefsKeyUsername = "MECH_USER_NAME";
        private const string PrefsKeyElo = "MECH_USER_ELO";

        public string CurrentGmail { get; private set; } = "";
        public string CurrentUsername { get; private set; } = "";
        public int CurrentElo { get; private set; } = 1200;

        public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentGmail) && !string.IsNullOrEmpty(CurrentUsername);

        public event Action<string, string, int> OnAccountUpdated; // username, gmail, elo
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
                Destroy(gameObject);
            }
        }

        public void LoadSavedAccount()
        {
            CurrentGmail = PlayerPrefs.GetString(PrefsKeyGmail, "");
            CurrentUsername = PlayerPrefs.GetString(PrefsKeyUsername, "");
            CurrentElo = PlayerPrefs.GetInt(PrefsKeyElo, 1200);

            if (IsLoggedIn)
            {
                Debug.Log($"[UserAccount] Loaded saved user: {CurrentUsername} ({CurrentGmail}) | Elo: {CurrentElo}");
                OnAccountUpdated?.Invoke(CurrentUsername, CurrentGmail, CurrentElo);
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

        private void ApplyAccount(string gmail, string username, int elo)
        {
            CurrentGmail = gmail;
            CurrentUsername = username;
            CurrentElo = elo;

            PlayerPrefs.SetString(PrefsKeyGmail, CurrentGmail);
            PlayerPrefs.SetString(PrefsKeyUsername, CurrentUsername);
            PlayerPrefs.SetInt(PrefsKeyElo, CurrentElo);
            PlayerPrefs.Save();

            // Sync with OnlineMultiplayerManager
            if (OnlineMultiplayerManager.Instance != null)
            {
                OnlineMultiplayerManager.Instance.playerName = CurrentUsername;
                OnlineMultiplayerManager.Instance.ConnectToLobby();
            }

            OnAccountUpdated?.Invoke(CurrentUsername, CurrentGmail, CurrentElo);
        }

        public void Logout()
        {
            CurrentGmail = "";
            CurrentUsername = "";
            CurrentElo = 1200;

            PlayerPrefs.DeleteKey(PrefsKeyGmail);
            PlayerPrefs.DeleteKey(PrefsKeyUsername);
            PlayerPrefs.DeleteKey(PrefsKeyElo);
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
