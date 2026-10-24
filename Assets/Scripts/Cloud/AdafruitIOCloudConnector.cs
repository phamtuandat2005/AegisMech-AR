using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using MechChess.Chess3D;
using MechChess.UI;

namespace MechChess.Cloud
{
    [System.Serializable]
    public class AIODataPoint
    {
        public string value;
    }

    [System.Serializable]
    public class AIOResponsePoint
    {
        public string id;
        public string value;
        public string created_at;
    }

    /// <summary>
    /// Bidirectional Cloud Connector for Adafruit IO.
    /// Upstream: Publishes turn-counter, ai-confidence, board-status, and last-move.
    /// Downstream: Listens for remote-command feed (RESET, EMP_STRIKE).
    /// </summary>
    public class AdafruitIOCloudConnector : MonoBehaviour
    {
        public static AdafruitIOCloudConnector Instance { get; private set; }

        [Header("Adafruit IO Credentials")]
        [Tooltip("Enter your Adafruit IO Username")]
        public string aioUsername = "phamtuandat";

        [Tooltip("Enter your Adafruit IO Active Key")]
        public string aioKey = "YOUR_AIO_KEY";

        [Header("Feed Keys")]
        public string feedTurnCounter = "turn-counter";
        public string feedAIConfidence = "ai-confidence";
        public string feedBoardStatus = "board-status";
        public string feedLastMove = "last-move";
        public string feedRemoteCommand = "remote-command";

        [Header("Polling Configuration")]
        public float downstreamPollInterval = 2.5f;

        private string _lastProcessedCommandId = "";
        private bool _isCloudConfigured = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // Load key locally from untracked adafruit_key.txt or PlayerPrefs
            if (string.IsNullOrEmpty(aioKey) || aioKey == "YOUR_AIO_KEY")
            {
                try
                {
                    string localKeyPath = System.IO.Path.Combine(Application.dataPath, "..", "adafruit_key.txt");
                    if (System.IO.File.Exists(localKeyPath))
                    {
                        aioKey = System.IO.File.ReadAllText(localKeyPath).Trim();
                    }
                    else
                    {
                        string savedKey = PlayerPrefs.GetString("AIO_KEY", "");
                        if (!string.IsNullOrEmpty(savedKey)) aioKey = savedKey;
                    }
                }
                catch { }
            }
        }

        private void Start()
        {
            _isCloudConfigured = !string.IsNullOrEmpty(aioUsername) &&
                                 !string.IsNullOrEmpty(aioKey) &&
                                 aioUsername != "YOUR_AIO_USERNAME";

            if (_isCloudConfigured)
            {
                Debug.Log($"[AdafruitIO] Initialized cloud telemetry for user: {aioUsername}");
                if (UIManager2D.Instance != null) UIManager2D.Instance.UpdateCloudStatus(true);
                StartCoroutine(DownstreamListenerRoutine());
            }
            else
            {
                Debug.LogWarning("[AdafruitIO] AIO credentials not yet populated. Running in local simulation mode.");
                if (UIManager2D.Instance != null) UIManager2D.Instance.UpdateCloudStatus(false, "Local Dev Mode");
            }
        }

        // =========================================================================
        // Upstream Telemetry
        // =========================================================================
        public void PublishTelemetry(int turn, float confidence, string boardStatus, string lastMove)
        {
            SendFeedData(feedTurnCounter, turn.ToString());
            SendFeedData(feedAIConfidence, $"{confidence * 100f:0.1f}%");
            SendFeedData(feedBoardStatus, boardStatus);
            SendFeedData(feedLastMove, lastMove);
        }

        public void PublishConfidence(float confidence)
        {
            SendFeedData(feedAIConfidence, $"{confidence * 100f:0.1f}%");
        }

        public void SendFeedData(string feedKey, string value)
        {
            if (!_isCloudConfigured)
            {
                Debug.Log($"[AdafruitIO Simulation] Uplink -> [{feedKey}]: {value}");
                return;
            }

            StartCoroutine(PostFeedRoutine(feedKey, value));
        }

        private IEnumerator PostFeedRoutine(string feedKey, string value)
        {
            string url = $"https://io.adafruit.com/api/v2/{aioUsername}/feeds/{feedKey}/data";
            AIODataPoint payload = new AIODataPoint { value = value };
            string jsonBody = JsonUtility.ToJson(payload);

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("X-AIO-Key", aioKey);

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[AdafruitIO] Feed [{feedKey}] updated: {value}");
                }
                else
                {
                    Debug.LogWarning($"[AdafruitIO] Failed to update feed [{feedKey}]: {req.error}");
                }
            }
        }

        // =========================================================================
        // Downstream Remote Control Listener
        // =========================================================================
        private IEnumerator DownstreamListenerRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(downstreamPollInterval);
                yield return StartCoroutine(CheckRemoteCommandRoutine());
            }
        }

        private IEnumerator CheckRemoteCommandRoutine()
        {
            string url = $"https://io.adafruit.com/api/v2/{aioUsername}/feeds/{feedRemoteCommand}/data/last";

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("X-AIO-Key", aioKey);
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string json = req.downloadHandler.text;
                    AIOResponsePoint pt = JsonUtility.FromJson<AIOResponsePoint>(json);

                    if (pt != null && pt.id != _lastProcessedCommandId && !string.IsNullOrEmpty(pt.value))
                    {
                        _lastProcessedCommandId = pt.id;
                        HandleRemoteCommand(pt.value.Trim().ToUpper());
                    }
                }
            }
        }

        private void HandleRemoteCommand(string command)
        {
            Debug.Log($"[AdafruitIO] Incoming Remote Command: {command}");

            if (command == "RESET")
            {
                if (UIManager2D.Instance != null)
                {
                    UIManager2D.Instance.RecordMove("[CLOUD REMOTE COMMAND] Game Reset Triggered!");
                }
                if (ChessBoard3DManager.Instance != null)
                {
                    ChessBoard3DManager.Instance.ResetBoard();
                }
            }
            else if (command == "EMP_STRIKE")
            {
                if (UIManager2D.Instance != null)
                {
                    UIManager2D.Instance.RecordMove("[CLOUD REMOTE COMMAND] Global EMP Strike Activated!");
                }
                if (ChessBoard3DManager.Instance != null)
                {
                    ChessBoard3DManager.Instance.TriggerEMPShockwave();
                }
            }
        }
    }
}
