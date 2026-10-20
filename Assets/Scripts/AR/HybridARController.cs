using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
#if UNITY_XR_ARFOUNDATION
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#endif
using MechChess.Chess3D;
using MechChess.UI;
using MechChess.Cloud;

namespace MechChess.AR
{
    public enum ARMode
    {
        WebcamFallback,
        ARFoundationSimulation
    }

    [System.Serializable]
    public class CardRecognitionResult
    {
        public bool success;
        public string command;
        public string from_square;
        public string to_square;
        public string piece_type;
        public float confidence_score;
        public string description;
        public float inference_time_ms;
    }

    [System.Serializable]
    public class CardRecognitionPayload
    {
        public string image_base64;
        public string card_mode;
    }

    /// <summary>
    /// HybridARController supports:
    /// 1. True Fullscreen AR Reality (Real-world webcam background Quad behind 3D board).
    /// 2. Picture-in-Picture Tactical Card Scanner.
    /// 3. AR Foundation plane tracking (in XR simulation or mobile AR hardware).
    /// 4. Live camera frame capture to Deep Learning PyTorch Backend.
    /// </summary>
    public class HybridARController : MonoBehaviour
    {
        public static HybridARController Instance { get; private set; }

        [Header("Mode Configuration")]
        public ARMode activeMode = ARMode.WebcamFallback;
        public string backendApiUrl = "http://localhost:8000/api/recognize-card";

        [Header("References")]
        public Transform chessboardRoot;

        // Webcam Video Feed
        private WebCamTexture _webcamTexture;
        private RawImage _backgroundRawImage;
        private Canvas _arCanvas;

        // True AR Background Quad
        private GameObject _backgroundQuad;
        private Material _webcamQuadMat;
        private int _arDisplayMode = 0; // 0 = Fullscreen AR Reality, 1 = PIP Scanner, 2 = Cyber Studio

#if UNITY_XR_ARFOUNDATION
        private ARRaycastManager _raycastManager;
        private ARPlaneManager _planeManager;
        private static List<ARRaycastHit> _hits = new List<ARRaycastHit>();
#endif

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            SetupWebcamBackgroundUI();

#if UNITY_XR_ARFOUNDATION
            _raycastManager = GetComponent<ARRaycastManager>();
            _planeManager = GetComponent<ARPlaneManager>();
#endif
        }

        private void Start()
        {
            if (chessboardRoot == null && ChessBoard3DManager.Instance != null)
            {
                chessboardRoot = ChessBoard3DManager.Instance.transform;
            }

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.OnScanCardClicked += CaptureAndRecognizeCard;
                UIManager2D.Instance.OnToggleARClicked += ToggleARMode;
            }

            StartWebcam();
            SetupARBackgroundQuad();

            // Default: Start in Fullscreen AR Reality mode
            ApplyDisplayMode(0);
        }

        private void SetupWebcamBackgroundUI()
        {
            GameObject canvasObj = new GameObject("AR_WebcamBackground_Canvas");
            canvasObj.transform.SetParent(transform, false);

            _arCanvas = canvasObj.AddComponent<Canvas>();
            _arCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _arCanvas.sortingOrder = 95;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Picture-in-Picture Frame in Top-Right Corner
            GameObject pipContainer = new GameObject("AR_PIP_Frame");
            pipContainer.transform.SetParent(canvasObj.transform, false);
            Image pipBg = pipContainer.AddComponent<Image>();
            pipBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
            pipBg.raycastTarget = false;

            RectTransform pipRt = pipContainer.GetComponent<RectTransform>();
            pipRt.anchorMin = new Vector2(1, 1);
            pipRt.anchorMax = new Vector2(1, 1);
            pipRt.pivot = new Vector2(1, 1);
            pipRt.sizeDelta = new Vector2(360, 230);
            pipRt.anchoredPosition = new Vector2(-40, -100);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(pipContainer.transform, false);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 14;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.0f, 0.95f, 1.0f);
            titleTxt.text = "[AR SCANNER: LIVE WEBCAM FEED]";
            titleTxt.raycastTarget = false;
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 98);
            titleRt.sizeDelta = new Vector2(340, 24);

            // Live Webcam Feed RawImage
            GameObject rawImgObj = new GameObject("WebcamVideoFeed");
            rawImgObj.transform.SetParent(pipContainer.transform, false);
            _backgroundRawImage = rawImgObj.AddComponent<RawImage>();
            _backgroundRawImage.raycastTarget = false;

            RectTransform rt = rawImgObj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -12);
            rt.sizeDelta = new Vector2(340, 185);
        }

        private void SetupARBackgroundQuad()
        {
            if (_backgroundQuad != null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            _backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _backgroundQuad.name = "AR_WebcamBackgroundQuad";
            _backgroundQuad.transform.SetParent(cam.transform, false);

            Collider c = _backgroundQuad.GetComponent<Collider>();
            if (c != null) Destroy(c);

            float dist = 18f;
            _backgroundQuad.transform.localPosition = new Vector3(0, 0, dist);
            _backgroundQuad.transform.localRotation = Quaternion.identity;

            float frustumHeight = 2.0f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * cam.aspect;
            _backgroundQuad.transform.localScale = new Vector3(frustumWidth, frustumHeight, 1f);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Diffuse");
            _webcamQuadMat = new Material(unlitShader) { name = "AR_WebcamQuadMat" };
            if (_webcamTexture != null)
            {
                if (_webcamQuadMat.HasProperty("_BaseMap")) _webcamQuadMat.SetTexture("_BaseMap", _webcamTexture);
                if (_webcamQuadMat.HasProperty("_MainTex")) _webcamQuadMat.SetTexture("_MainTex", _webcamTexture);
            }

            _backgroundQuad.GetComponent<Renderer>().sharedMaterial = _webcamQuadMat;
        }

        public void StartWebcam()
        {
            if (WebCamTexture.devices.Length > 0)
            {
                string cameraName = WebCamTexture.devices[0].name;
                // Prefer back camera on mobile
                foreach (var dev in WebCamTexture.devices)
                {
                    if (!dev.isFrontFacing)
                    {
                        cameraName = dev.name;
                        break;
                    }
                }

                _webcamTexture = new WebCamTexture(cameraName, 1280, 720, 30);
                _backgroundRawImage.texture = _webcamTexture;
                _webcamTexture.Play();

                if (_webcamQuadMat != null)
                {
                    if (_webcamQuadMat.HasProperty("_BaseMap")) _webcamQuadMat.SetTexture("_BaseMap", _webcamTexture);
                    if (_webcamQuadMat.HasProperty("_MainTex")) _webcamQuadMat.SetTexture("_MainTex", _webcamTexture);
                }

                Debug.Log($"[HybridAR] Webcam stream initiated: {cameraName}");
            }
            else
            {
                Debug.LogWarning("[HybridAR] No physical webcam detected. Generating virtual AR grid backdrop.");
                Texture2D virtualBackdrop = new Texture2D(256, 256);
                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        bool isLine = (x % 32 == 0) || (y % 32 == 0);
                        virtualBackdrop.SetPixel(x, y, isLine ? new Color(0, 0.4f, 0.6f, 0.5f) : new Color(0.02f, 0.04f, 0.06f));
                    }
                }
                virtualBackdrop.Apply();
                _backgroundRawImage.texture = virtualBackdrop;
            }
        }

        public void ToggleARMode()
        {
            _arDisplayMode = (_arDisplayMode + 1) % 3;
            ApplyDisplayMode(_arDisplayMode);
        }

        private void ApplyDisplayMode(int mode)
        {
            _arDisplayMode = mode;
            if (_arDisplayMode == 0) // Fullscreen AR Reality (Real room background behind board)
            {
                if (_backgroundQuad != null) _backgroundQuad.SetActive(true);
                if (_arCanvas != null) _arCanvas.gameObject.SetActive(false);
                if (UIManager2D.Instance != null) UIManager2D.Instance.RecordMove("[AR MODE] Fullscreen Reality (Room Background) ON");
            }
            else if (_arDisplayMode == 1) // Corner PIP Tactical Scanner
            {
                if (_backgroundQuad != null) _backgroundQuad.SetActive(false);
                if (_arCanvas != null) _arCanvas.gameObject.SetActive(true);
                if (UIManager2D.Instance != null) UIManager2D.Instance.RecordMove("[AR MODE] Corner PIP Scanner Viewport ON");
            }
            else // Pure Cyber Studio Arena
            {
                if (_backgroundQuad != null) _backgroundQuad.SetActive(false);
                if (_arCanvas != null) _arCanvas.gameObject.SetActive(false);
                if (UIManager2D.Instance != null) UIManager2D.Instance.RecordMove("[AR MODE] Pure Cyber Studio Arena ON");
            }
        }

        private void LateUpdate()
        {
            // Dynamically scale AR Background Quad to match camera aspect ratio
            if (_backgroundQuad != null && _backgroundQuad.activeSelf)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    float dist = 18f;
                    float frustumHeight = 2.0f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                    float frustumWidth = frustumHeight * cam.aspect;
                    _backgroundQuad.transform.localScale = new Vector3(frustumWidth, frustumHeight, 1f);
                }
            }
        }

        private void Update()
        {
            // AR Foundation Raycasting in Mobile/Simulation mode
#if UNITY_XR_ARFOUNDATION
            if (activeMode == ARMode.ARFoundationSimulation && _raycastManager != null)
            {
                Touchscreen ts = Touchscreen.current;
                Mouse mouse = Mouse.current;
                Vector2 pointerPos = Vector2.zero;
                bool pressed = false;

                if (ts != null && ts.primaryTouch.press.wasPressedThisFrame)
                {
                    pointerPos = ts.primaryTouch.position.ReadValue();
                    pressed = true;
                }
                else if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    pointerPos = mouse.position.ReadValue();
                    pressed = true;
                }

                if (pressed)
                {
                    if (_raycastManager.Raycast(pointerPos, _hits, TrackableType.PlaneWithinPolygon))
                    {
                        Pose hitPose = _hits[0].pose;
                        if (chessboardRoot != null)
                        {
                            chessboardRoot.position = hitPose.position;
                            chessboardRoot.rotation = hitPose.rotation;
                        }
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Captures the current webcam frame, encodes to Base64, and sends to Deep Learning FastAPI backend.
        /// </summary>
        public void CaptureAndRecognizeCard()
        {
            StartCoroutine(CaptureAndPostFrameRoutine());
        }

        private IEnumerator CaptureAndPostFrameRoutine()
        {
            byte[] jpgBytes = null;

            if (_webcamTexture != null && _webcamTexture.isPlaying)
            {
                Texture2D snap = new Texture2D(_webcamTexture.width, _webcamTexture.height, TextureFormat.RGB24, false);
                snap.SetPixels(_webcamTexture.GetPixels());
                snap.Apply();
                jpgBytes = snap.EncodeToJPG(85);
                Destroy(snap);
            }
            else
            {
                // Standalone fallback image for testing if no webcam attached
                Texture2D fallbackSnap = new Texture2D(224, 224, TextureFormat.RGB24, false);
                for (int y = 0; y < 224; y++)
                {
                    for (int x = 0; x < 224; x++)
                    {
                        fallbackSnap.SetPixel(x, y, new Color(0.1f, 0.5f, 0.8f));
                    }
                }
                fallbackSnap.Apply();
                jpgBytes = fallbackSnap.EncodeToJPG(85);
                Destroy(fallbackSnap);
            }

            string base64String = Convert.ToBase64String(jpgBytes);
            CardRecognitionPayload payload = new CardRecognitionPayload
            {
                image_base64 = base64String,
                card_mode = "tactical_card"
            };

            string jsonBody = JsonUtility.ToJson(payload);

            using (UnityWebRequest req = new UnityWebRequest(backendApiUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = req.downloadHandler.text;
                    Debug.Log($"[HybridAR] AI Server Response: {jsonResponse}");

                    CardRecognitionResult result = JsonUtility.FromJson<CardRecognitionResult>(jsonResponse);
                    if (result != null && result.success)
                    {
                        if (UIManager2D.Instance != null)
                        {
                            UIManager2D.Instance.UpdateAIServerStatus(true);
                            UIManager2D.Instance.UpdateConfidence(result.confidence_score, result.description);
                            UIManager2D.Instance.RecordMove($"[AI CARD SCAN] {result.description} (Conf: {result.confidence_score * 100:0.0}%)");
                        }

                        // Execute recognized move if valid squares provided
                        if (!string.IsNullOrEmpty(result.from_square) && !string.IsNullOrEmpty(result.to_square))
                        {
                            if (result.command == "EMP_STRIKE")
                            {
                                ChessBoard3DManager.Instance.TriggerEMPShockwave();
                            }
                            else
                            {
                                ChessBoard3DManager.Instance.ExecuteAlgebraicMove(result.from_square, result.to_square);
                            }
                        }

                        // Stream to Adafruit IO
                        if (AdafruitIOCloudConnector.Instance != null)
                        {
                            AdafruitIOCloudConnector.Instance.PublishConfidence(result.confidence_score);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[HybridAR] AI Recognition Request failed: {req.error}");
                    if (UIManager2D.Instance != null)
                    {
                        UIManager2D.Instance.UpdateAIServerStatus(false);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_webcamTexture != null && _webcamTexture.isPlaying)
            {
                _webcamTexture.Stop();
            }
        }
    }
}
