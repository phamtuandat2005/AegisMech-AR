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
        public string backendApiUrl = "https://aegismech-ar.onrender.com/api/recognize-card";

        [Header("References")]
        public Transform chessboardRoot;

        [Header("Board Scale Configuration (F10)")]
        public float currentScaleMultiplier = 1.0f; // 1.0 = 100% standard full-size board (0.6x to 2.0x range)
        public float currentScaleMeters => currentScaleMultiplier * 0.5f;

        // Webcam Video Feed
        private WebCamTexture _webcamTexture;
        private RawImage _backgroundRawImage;
        private Canvas _arCanvas;

        // True AR Background Quad
        private GameObject _backgroundQuad;
        private Material _webcamQuadMat;
        private int _arDisplayMode = 0; // 0 = Fullscreen AR Reality, 1 = PIP Scanner, 2 = Arcane Realm Studio

        // F10 & F12 UI references
        private GameObject _pipContainer;
        private GameObject _guidanceOverlay;
        private Text _scaleIndicatorText;
        private bool _isGuidanceMinimized = true;

#if UNITY_XR_ARFOUNDATION
        private ARRaycastManager _raycastManager;
        private ARPlaneManager _planeManager;
        private static List<ARRaycastHit> _hits = new List<ARRaycastHit>();
#endif

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);

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
                chessboardRoot = ChessBoard3DManager.Instance.BoardRootContainer != null 
                    ? ChessBoard3DManager.Instance.BoardRootContainer 
                    : ChessBoard3DManager.Instance.transform;
            }

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.OnScanCardClicked += CaptureAndRecognizeCard;
                UIManager2D.Instance.OnToggleARClicked += ToggleARMode;
            }

            // Do NOT start webcam on startup! Keep camera hardware completely off until a match/AR mode is started
            SetupARBackgroundQuad();

            // Default display mode: Arcane Realm Studio Arena (Mode 2)
            // Clean 3D studio chessboard at scale 1.0, with zero webcam background intrusion
            currentScaleMultiplier = 1.0f;
            ApplyDisplayMode(2);

            // Initially hide AR overlays and ensure camera is stopped if MainMenu is active
            SetARActive(false);
        }

        public void SetARActive(bool active)
        {
            if (!active)
            {
                if (_arCanvas != null) _arCanvas.gameObject.SetActive(false);
                if (_backgroundQuad != null) _backgroundQuad.SetActive(false);
                if (_pipContainer != null) _pipContainer.SetActive(false);
                if (_guidanceOverlay != null) _guidanceOverlay.SetActive(false);
                StopWebcam();
                _arDisplayMode = 2;
            }
            else
            {
                if (_arDisplayMode == 0 || _arDisplayMode == 1)
                {
                    StartWebcam();
                    if (_arCanvas != null) _arCanvas.gameObject.SetActive(true);
                    if (_backgroundQuad != null) _backgroundQuad.SetActive(_arDisplayMode == 0);
                }
                else
                {
                    // Pure Studio 3D Arena: No camera hardware needed!
                    StopWebcam();
                    if (_arCanvas != null) _arCanvas.gameObject.SetActive(false);
                    if (_backgroundQuad != null) _backgroundQuad.SetActive(false);
                }
            }
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
            scaler.matchWidthOrHeight = ((float)Screen.width / Mathf.Max(1, Screen.height) < 1.7777f) ? 0f : 1f;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                            ?? Resources.GetBuiltinResource<Font>("Arial.ttf")
                            ?? Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Tahoma" }, 14);

            // 1. Picture-in-Picture Frame in Top-Right Corner
            GameObject pipContainer = new GameObject("AR_PIP_Frame");
            pipContainer.transform.SetParent(canvasObj.transform, false);
            _pipContainer = pipContainer;
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
            titleTxt.font = defaultFont;
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

            // 2. F12 AR Onboarding & Guidance Banner with F10 Scale Controls
            _guidanceOverlay = new GameObject("AR_GuidanceOverlay");
            _guidanceOverlay.transform.SetParent(canvasObj.transform, false);
            Image guideBg = _guidanceOverlay.AddComponent<Image>();
            guideBg.color = new Color(0.04f, 0.08f, 0.12f, 0.94f);
            RectTransform guideRt = _guidanceOverlay.GetComponent<RectTransform>();
            guideRt.anchorMin = new Vector2(0.5f, 1f);
            guideRt.anchorMax = new Vector2(0.5f, 1f);
            guideRt.pivot = new Vector2(0.5f, 1f);
            guideRt.anchoredPosition = new Vector2(0, -90);
            guideRt.sizeDelta = new Vector2(1300, 58);

            // Guidance step instruction text
            GameObject guideTxtObj = new GameObject("InstructionText");
            guideTxtObj.transform.SetParent(_guidanceOverlay.transform, false);
            Text guideTxt = guideTxtObj.AddComponent<Text>();
            guideTxt.text = "💡 HƯỚNG DẪN AR: 1. Quét camera quanh mặt bàn | 2. Chạm màn hình để chỉnh vị trí | 3. Chạm quân cờ để di chuyển";
            guideTxt.font = defaultFont;
            guideTxt.fontSize = 15;
            guideTxt.fontStyle = FontStyle.Bold;
            guideTxt.alignment = TextAnchor.MiddleLeft;
            guideTxt.color = new Color(0.3f, 0.95f, 1.0f);
            RectTransform guideTxtRt = guideTxtObj.GetComponent<RectTransform>();
            guideTxtRt.anchorMin = new Vector2(0f, 0.5f);
            guideTxtRt.anchorMax = new Vector2(0f, 0.5f);
            guideTxtRt.pivot = new Vector2(0f, 0.5f);
            guideTxtRt.anchoredPosition = new Vector2(20, 0);
            guideTxtRt.sizeDelta = new Vector2(820, 44);

            // Scale Indicator Text
            GameObject scaleLblObj = new GameObject("ScaleIndicator");
            scaleLblObj.transform.SetParent(_guidanceOverlay.transform, false);
            _scaleIndicatorText = scaleLblObj.AddComponent<Text>();
            _scaleIndicatorText.text = "CỠ: 100%";
            _scaleIndicatorText.font = defaultFont;
            _scaleIndicatorText.fontSize = 15;
            _scaleIndicatorText.fontStyle = FontStyle.Bold;
            _scaleIndicatorText.alignment = TextAnchor.MiddleCenter;
            _scaleIndicatorText.color = new Color(1.0f, 0.75f, 0.1f);
            RectTransform scaleLblRt = scaleLblObj.GetComponent<RectTransform>();
            scaleLblRt.anchorMin = new Vector2(1f, 0.5f);
            scaleLblRt.anchorMax = new Vector2(1f, 0.5f);
            scaleLblRt.pivot = new Vector2(1f, 0.5f);
            scaleLblRt.anchoredPosition = new Vector2(-285, 0);
            scaleLblRt.sizeDelta = new Vector2(100, 40);

            // Scale Button: 80% (Nhỏ)
            CreateARQuickButton(_guidanceOverlay.transform, "BtnScale80", "80%", new Vector2(-225, 0), new Vector2(68, 38), () => SetBoardScale(0.80f), defaultFont);

            // Scale Button: 100% (Chuẩn)
            CreateARQuickButton(_guidanceOverlay.transform, "BtnScale100", "100%", new Vector2(-150, 0), new Vector2(72, 38), () => SetBoardScale(1.00f), defaultFont);

            // Scale Button: 130% (Lớn)
            CreateARQuickButton(_guidanceOverlay.transform, "BtnScale130", "130%", new Vector2(-72, 0), new Vector2(72, 38), () => SetBoardScale(1.30f), defaultFont);

            // Close / Minimize guidance button
            CreateARQuickButton(_guidanceOverlay.transform, "BtnCloseGuide", "✕", new Vector2(-15, 0), new Vector2(38, 38), () =>
            {
                _isGuidanceMinimized = true;
                _guidanceOverlay.SetActive(false);
            }, defaultFont);
        }

        private void CreateARQuickButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.22f, 0.32f, 0.95f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;

            RectTransform tRt = txtObj.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
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
            if (_webcamTexture != null && _webcamTexture.isPlaying) return;

            if (_webcamTexture != null)
            {
                _webcamTexture.Play();
                if (_backgroundRawImage != null) _backgroundRawImage.texture = _webcamTexture;
                if (_webcamQuadMat != null)
                {
                    if (_webcamQuadMat.HasProperty("_BaseMap")) _webcamQuadMat.SetTexture("_BaseMap", _webcamTexture);
                    if (_webcamQuadMat.HasProperty("_MainTex")) _webcamQuadMat.SetTexture("_MainTex", _webcamTexture);
                }
                return;
            }

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
                if (_backgroundRawImage != null) _backgroundRawImage.texture = _webcamTexture;
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
                Debug.LogWarning("[HybridAR] No physical webcam detected. Loading AR table backdrop.");
                Sprite tableSprite = MechChess.UI.ArcaneAssetLoader.LoadArcaneSprite("ingame_3d_table_bg.png");
                Texture2D virtualBackdrop = (tableSprite != null) ? tableSprite.texture : null;
                if (virtualBackdrop == null)
                {
                    virtualBackdrop = new Texture2D(256, 256);
                    for (int y = 0; y < 256; y++)
                    {
                        for (int x = 0; x < 256; x++)
                        {
                            bool isLine = (x % 32 == 0) || (y % 32 == 0);
                            virtualBackdrop.SetPixel(x, y, isLine ? new Color(0, 0.4f, 0.6f, 0.5f) : new Color(0.02f, 0.04f, 0.06f));
                        }
                    }
                    virtualBackdrop.Apply();
                }
                if (_backgroundRawImage != null) _backgroundRawImage.texture = virtualBackdrop;
                if (_webcamQuadMat != null)
                {
                    if (_webcamQuadMat.HasProperty("_BaseMap")) _webcamQuadMat.SetTexture("_BaseMap", virtualBackdrop);
                    if (_webcamQuadMat.HasProperty("_MainTex")) _webcamQuadMat.SetTexture("_MainTex", virtualBackdrop);
                }
            }
        }

        public void StopWebcam()
        {
            if (_webcamTexture != null)
            {
                if (_webcamTexture.isPlaying)
                {
                    _webcamTexture.Stop();
                }
                Debug.Log("[HybridAR] Webcam stream stopped.");
            }
            if (_backgroundRawImage != null) _backgroundRawImage.texture = null;
        }

        /// <summary>
        /// Automatically shuts down the webcam stream, hides AR background quad/pip overlay,
        /// and restores standard 3D studio arena. Called when a match ends or player returns to menu.
        /// </summary>
        public void TurnOffCamera()
        {
            SetARActive(false);
            ApplyDisplayMode(2);
            StopWebcam();
            Debug.Log("[HybridAR] Match ended: Webcam and AR overlays automatically turned OFF.");
        }

        public void ToggleARMode()
        {
            _arDisplayMode = (_arDisplayMode + 1) % 3;
            ApplyDisplayMode(_arDisplayMode);
        }

        public void ApplyDisplayMode(int mode)
        {
            _arDisplayMode = mode;
            if (_arDisplayMode == 0) // Fullscreen AR Reality (Real room background behind board)
            {
                StartWebcam();
                if (_backgroundQuad != null) _backgroundQuad.SetActive(true);
                if (_pipContainer != null) _pipContainer.SetActive(false);
                if (_guidanceOverlay != null) _guidanceOverlay.SetActive(!_isGuidanceMinimized);
                if (_arCanvas != null) _arCanvas.gameObject.SetActive(true);
                if (chessboardRoot != null)
                {
                    chessboardRoot.localScale = Vector3.one * currentScaleMultiplier;
                }
                if (UIManager2D.Instance != null) UIManager2D.Instance.RecordMove("[AR MODE] Fullscreen Reality (Room Background) ON");
            }
            else if (_arDisplayMode == 1) // Corner PIP Tactical Scanner
            {
                StartWebcam();
                if (_backgroundQuad != null) _backgroundQuad.SetActive(false);
                if (_pipContainer != null) _pipContainer.SetActive(true);
                if (_guidanceOverlay != null) _guidanceOverlay.SetActive(!_isGuidanceMinimized);
                if (_arCanvas != null) _arCanvas.gameObject.SetActive(true);
                if (chessboardRoot != null)
                {
                    chessboardRoot.localScale = Vector3.one;
                }
                if (UIManager2D.Instance != null) UIManager2D.Instance.RecordMove("[AR MODE] Corner PIP Scanner Viewport ON");
            }
            else // Arcane Realm Studio Arena (Mode 2) - Camera turned off
            {
                StopWebcam();
                if (_backgroundQuad != null) _backgroundQuad.SetActive(false);
                if (_pipContainer != null) _pipContainer.SetActive(false);
                if (_guidanceOverlay != null) _guidanceOverlay.SetActive(false);
                if (_arCanvas != null) _arCanvas.gameObject.SetActive(false);
                if (chessboardRoot != null)
                {
                    chessboardRoot.localScale = Vector3.one;
                    chessboardRoot.localPosition = Vector3.zero;
                    chessboardRoot.localRotation = Quaternion.identity;
                }
                if (UIManager2D.Instance != null) UIManager2D.Instance.RecordMove("[CHẾ ĐỘ AR] Cung Điện Huyền Thuật (Arcane Realm Studio) ON");
            }
        }

        public void SetBoardScale(float scaleMultiplier)
        {
            currentScaleMultiplier = Mathf.Clamp(scaleMultiplier, 0.50f, 2.0f);
            if (chessboardRoot != null)
            {
                chessboardRoot.localScale = Vector3.one * currentScaleMultiplier;
            }

            if (_scaleIndicatorText != null)
            {
                _scaleIndicatorText.text = $"SIZE: {Mathf.RoundToInt(currentScaleMultiplier * 100f)}%";
            }

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.RecordMove($"[AR SCALE] Kích thước bàn cờ: {Mathf.RoundToInt(currentScaleMultiplier * 100f)}% (Chuẩn)");
            }
        }

        public void ScaleBoardDelta(float deltaMultiplier)
        {
            SetBoardScale(currentScaleMultiplier + deltaMultiplier);
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
            // F10: 2-Finger Pinch Gesture on touchscreen to adjust board scale
            if (Input.touchCount == 2)
            {
                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                Vector2 prevPos0 = t0.position - t0.deltaPosition;
                Vector2 prevPos1 = t1.position - t1.deltaPosition;

                float prevDist = (prevPos0 - prevPos1).magnitude;
                float curDist = (t0.position - t1.position).magnitude;
                float diff = curDist - prevDist;

                if (Mathf.Abs(diff) > 0.5f)
                {
                    ScaleBoardDelta(diff * 0.0008f);
                }
            }

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
            bool temporaryWebcamStarted = false;

            if (_webcamTexture == null || !_webcamTexture.isPlaying)
            {
                StartWebcam();
                temporaryWebcamStarted = true;
                yield return new WaitForSeconds(0.25f);
            }

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

            if (temporaryWebcamStarted && _arDisplayMode == 2)
            {
                StopWebcam();
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
