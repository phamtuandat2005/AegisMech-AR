using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MechChess.ProceduralMech;

namespace MechChess.UI
{
    public enum CyberButtonColor
    {
        Cyan,       // Alliance / Primary action
        Crimson,    // Syndicate / Attack / AI
        Amber,      // Overdrive / 2D Hack / Warning
        Emerald,    // AR / Matrix Tracking
        Violet,     // Perspective / View 2D-3D
        Sapphire,   // Online PVP / Netlink
        Gunmetal,   // System Purge / Reset
        PanelFrame, // Semi-transparent glass frame
        ModalFrame  // Heavy command modal frame
    }

    /// <summary>
    /// Procedural AAA Cyber HUD & Button Sprite Generator.
    /// Creates ultra-crisp chamfered sci-fi 9-sliced sprites at runtime with neon glow borders.
    /// </summary>
    public static class ProceduralUISpriteGenerator
    {
        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        public static Sprite GetCyberSprite(CyberButtonColor style, int width = 128, int height = 64, float cut = 14f, float border = 2.5f)
        {
            Color borderColor;
            Color fillColor;
            Color glowColor;

            switch (style)
            {
                case CyberButtonColor.Cyan:
                    borderColor = new Color(0.0f, 0.95f, 1.0f, 1.0f);
                    fillColor = new Color(0.03f, 0.09f, 0.16f, 0.88f);
                    glowColor = new Color(0.0f, 0.95f, 1.0f, 0.35f);
                    break;
                case CyberButtonColor.Crimson:
                    borderColor = new Color(1.0f, 0.22f, 0.35f, 1.0f);
                    fillColor = new Color(0.16f, 0.04f, 0.07f, 0.88f);
                    glowColor = new Color(1.0f, 0.22f, 0.35f, 0.35f);
                    break;
                case CyberButtonColor.Amber:
                    borderColor = new Color(1.0f, 0.72f, 0.10f, 1.0f);
                    fillColor = new Color(0.18f, 0.11f, 0.02f, 0.88f);
                    glowColor = new Color(1.0f, 0.72f, 0.10f, 0.35f);
                    break;
                case CyberButtonColor.Emerald:
                    borderColor = new Color(0.0f, 1.0f, 0.55f, 1.0f);
                    fillColor = new Color(0.02f, 0.14f, 0.07f, 0.88f);
                    glowColor = new Color(0.0f, 1.0f, 0.55f, 0.35f);
                    break;
                case CyberButtonColor.Violet:
                    borderColor = new Color(0.70f, 0.35f, 1.0f, 1.0f);
                    fillColor = new Color(0.11f, 0.03f, 0.18f, 0.88f);
                    glowColor = new Color(0.70f, 0.35f, 1.0f, 0.35f);
                    break;
                case CyberButtonColor.Sapphire:
                    borderColor = new Color(0.15f, 0.65f, 1.0f, 1.0f);
                    fillColor = new Color(0.03f, 0.08f, 0.18f, 0.88f);
                    glowColor = new Color(0.15f, 0.65f, 1.0f, 0.35f);
                    break;
                case CyberButtonColor.Gunmetal:
                    borderColor = new Color(0.50f, 0.58f, 0.68f, 1.0f);
                    fillColor = new Color(0.07f, 0.09f, 0.13f, 0.90f);
                    glowColor = new Color(0.60f, 0.70f, 0.80f, 0.20f);
                    break;
                case CyberButtonColor.PanelFrame:
                    borderColor = new Color(0.0f, 0.85f, 1.0f, 0.60f);
                    fillColor = new Color(0.03f, 0.05f, 0.09f, 0.86f);
                    glowColor = new Color(0.0f, 0.85f, 1.0f, 0.15f);
                    break;
                case CyberButtonColor.ModalFrame:
                    borderColor = new Color(0.0f, 0.95f, 1.0f, 0.88f);
                    fillColor = new Color(0.02f, 0.03f, 0.06f, 0.97f);
                    glowColor = new Color(0.0f, 0.95f, 1.0f, 0.25f);
                    break;
                default:
                    borderColor = Color.cyan;
                    fillColor = new Color(0.05f, 0.08f, 0.12f, 0.85f);
                    glowColor = Color.clear;
                    break;
            }

            string key = $"{style}_{width}_{height}_{cut}_{border}";
            if (_spriteCache.TryGetValue(key, out Sprite cached)) return cached;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[width * height];
            float sqrt2 = Mathf.Sqrt(2.0f);

            for (int y = 0; y < height; y++)
            {
                float py = y + 0.5f;
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f;

                    float d_left = px;
                    float d_right = width - px;
                    float d_bottom = py;
                    float d_top = height - py;

                    float d_bl = (px + py - cut) / sqrt2;
                    float d_br = ((width - px) + py - cut) / sqrt2;
                    float d_tl = (px + (height - py) - cut) / sqrt2;
                    float d_tr = ((width - px) + (height - py) - cut) / sqrt2;

                    float dist = Mathf.Min(d_left, Mathf.Min(d_right, Mathf.Min(d_bottom, Mathf.Min(d_top, Mathf.Min(d_tl, Mathf.Min(d_tr, Mathf.Min(d_bl, d_br)))))));

                    if (dist < -0.5f)
                    {
                        colors[y * width + x] = Color.clear;
                        continue;
                    }

                    float outerAlpha = Mathf.Clamp01(dist + 0.5f);

                    if (dist < border)
                    {
                        Color bCol = borderColor;
                        bCol.a *= outerAlpha;
                        colors[y * width + x] = bCol;
                    }
                    else
                    {
                        float t_vert = (float)y / height;
                        Color fCol = fillColor;
                        fCol.r += 0.06f * t_vert;
                        fCol.g += 0.09f * t_vert;
                        fCol.b += 0.14f * t_vert;

                        float innerDist = dist - border;
                        if (innerDist < 4.0f)
                        {
                            float glowFactor = (1.0f - innerDist / 4.0f) * glowColor.a;
                            fCol = Color.Lerp(fCol, glowColor, glowFactor);
                        }

                        if ((height - py) < (border + 4.0f))
                        {
                            float hlFactor = (1.0f - ((height - py) - border) / 4.0f) * 0.35f;
                            fCol = Color.Lerp(fCol, Color.white, hlFactor);
                        }

                        fCol.a *= outerAlpha;
                        colors[y * width + x] = fCol;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            int sliceBorder = (int)(cut + border + 2);
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(sliceBorder, sliceBorder, sliceBorder, sliceBorder)
            );

            _spriteCache[key] = sprite;
            return sprite;
        }

        public static Color GetBorderColor(CyberButtonColor style)
        {
            switch (style)
            {
                case CyberButtonColor.Cyan: return new Color(0.0f, 0.95f, 1.0f, 1.0f);
                case CyberButtonColor.Crimson: return new Color(1.0f, 0.22f, 0.35f, 1.0f);
                case CyberButtonColor.Amber: return new Color(1.0f, 0.72f, 0.10f, 1.0f);
                case CyberButtonColor.Emerald: return new Color(0.0f, 1.0f, 0.55f, 1.0f);
                case CyberButtonColor.Violet: return new Color(0.70f, 0.35f, 1.0f, 1.0f);
                case CyberButtonColor.Sapphire: return new Color(0.15f, 0.65f, 1.0f, 1.0f);
                case CyberButtonColor.Gunmetal: return new Color(0.50f, 0.58f, 0.68f, 1.0f);
                default: return Color.white;
            }
        }
    }

    /// <summary>
    /// Interactive button hover & press feedback with smooth punch scale and procedural sci-fi audio chirp.
    /// </summary>
    public class CyberButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform _rect;
        private Vector3 _originalScale = Vector3.one;

        private static AudioSource _audioSource;
        private static AudioClip _clickClip;
        private static AudioClip _hoverClip;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (_rect != null) _originalScale = _rect.localScale;

            InitAudio();
        }

        private static void InitAudio()
        {
            if (_audioSource != null) return;
            try
            {
                GameObject sfxObj = new GameObject("UI_SFX_Manager");
                GameObject.DontDestroyOnLoad(sfxObj);
                _audioSource = sfxObj.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 0f; // Pure 2D UI sound
                _audioSource.volume = 0.35f;

                _clickClip = CreateTone(1200f, 1650f, 0.035f, 0.35f);
                _hoverClip = CreateTone(720f, 960f, 0.02f, 0.10f);
            }
            catch { }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_rect == null) return;
            StopAllCoroutines();
            StartCoroutine(AnimateScale(1.035f, 0.07f));
            if (_audioSource != null && _hoverClip != null) _audioSource.PlayOneShot(_hoverClip);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_rect == null) return;
            StopAllCoroutines();
            StartCoroutine(AnimateScale(1.0f, 0.09f));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_rect == null) return;
            StopAllCoroutines();
            StartCoroutine(AnimateScale(0.96f, 0.04f));
            if (_audioSource != null && _clickClip != null) _audioSource.PlayOneShot(_clickClip);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_rect == null) return;
            StopAllCoroutines();
            StartCoroutine(AnimateScale(1.035f, 0.06f));
        }

        private IEnumerator AnimateScale(float targetScale, float duration)
        {
            Vector3 start = _rect.localScale;
            Vector3 target = _originalScale * targetScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _rect.localScale = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            _rect.localScale = target;
        }

        private static AudioClip CreateTone(float startFreq, float endFreq, float duration, float volume)
        {
            int samples = (int)(44100 * duration);
            AudioClip clip = AudioClip.Create("SfxTone", samples, 1, 44100, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float env = Mathf.Sin(t * Mathf.PI); // Attack-Release envelope
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * ((float)i / 44100f)) * env * volume;
            }
            clip.SetData(data, 0);
            return clip;
        }
    }

    /// <summary>
    /// Complete AAA Cyberpunk & Mech Tactical HUD Manager:
    /// Chamfered neon buttons, digital chronometers, algebraic move history,
    /// promotion modal, resign/draw/undo actions, and real-time status indicators.
    /// </summary>
    public class UIManager2D : MonoBehaviour
    {
        public static UIManager2D Instance { get; private set; }

        [Header("Match State")]
        private float _whiteTimer = 600f; // 10 minutes
        private float _blackTimer = 600f;
        private float _initialTime = 600f;
        private float _timeIncrement = 0f;
        private float _firstMoveTimer = 60f; // 1 minute limit for the first move
        private bool _isFirstMoveWaiting = true;
        private bool _isMatchAborted = false;
        private bool _isGameOver = false;
        private PieceColor _activeColor = PieceColor.White;
        private bool _isTimerRunning = true;

        [Header("F02 & F03 Settings")]
        public string ActiveTimeControl { get; private set; } = "10 + 0";
        public bool isRankedMode = false;

        public float WhiteTime => _whiteTimer;
        public float BlackTime => _blackTimer;

        public void SetTimers(float whiteSec, float blackSec)
        {
            _whiteTimer = Mathf.Max(0f, whiteSec);
            _blackTimer = Mathf.Max(0f, blackSec);
            if (_whiteTimerText != null) _whiteTimerText.text = FormatTime(_whiteTimer);
            if (_blackTimerText != null) _blackTimerText.text = FormatTime(_blackTimer);
        }

        public void ConfigureTimeControl(string timeControlStr)
        {
            ActiveTimeControl = timeControlStr;
            float totalSec = 600f;
            float incSec = 0f;

            if (timeControlStr.Contains("3 + 2") || timeControlStr.Contains("3+2"))
            {
                totalSec = 180f;
                incSec = 2f;
            }
            else if (timeControlStr.Contains("5 + 0") || timeControlStr.Contains("5+0"))
            {
                totalSec = 300f;
                incSec = 0f;
            }
            else if (timeControlStr.Contains("10 + 0") || timeControlStr.Contains("10+0"))
            {
                totalSec = 600f;
                incSec = 0f;
            }
            else if (timeControlStr.Contains("15 + 10") || timeControlStr.Contains("15+10"))
            {
                totalSec = 900f;
                incSec = 10f;
            }

            _initialTime = totalSec;
            _timeIncrement = incSec;
            _whiteTimer = totalSec;
            _blackTimer = totalSec;
            _firstMoveTimer = 60f;
            _isFirstMoveWaiting = true;

            if (_whiteTimerText != null) _whiteTimerText.text = FormatTime(_whiteTimer);
            if (_blackTimerText != null) _blackTimerText.text = FormatTime(_blackTimer);
            RecordMove($"[ĐỒNG HỒ] Thể thức: {timeControlStr} ({FormatTime(totalSec)} + {incSec}s/nước)");
        }

        private List<string> _moveHistory = new List<string>();

        // UI Element References
        private Text _whiteTimerText;
        private Text _blackTimerText;
        private Text _firstMoveWarningText;
        private Text _turnIndicatorText;
        private Text _moveHistoryText;
        private Text _aiServerStatusText;
        private Text _cloudStatusText;
        private Text _lastConfidenceText;

        // Multiplayer UI References
        private GameObject _multiplayerModal;
        private Text _multiplayerStatusText;
        private Text _multiplayerRoleText;

        // In-Game Tactical Sidebar Drawer References
        private GameObject _tacticalDrawerObj;
        private GameObject _drawerBackdropObj;
        private bool _isTacticalDrawerOpen = false;

        public void ToggleTacticalDrawer()
        {
            SetTacticalDrawerOpen(!_isTacticalDrawerOpen);
        }

        public void SetTacticalDrawerOpen(bool open)
        {
            _isTacticalDrawerOpen = open;
            if (_tacticalDrawerObj != null) _tacticalDrawerObj.SetActive(open);
            if (_drawerBackdropObj != null) _drawerBackdropObj.SetActive(open);
        }

        // Promotion UI References
        private GameObject _promotionModal;
        private Action<PieceType> _onPromotionSelected;

        // Screen 9: AR Overlay References
        private GameObject _arHudOverlayObj;
        public bool isAROverlayActive { get; private set; } = false;

        public void SetARHUDVisible(bool visible)
        {
            isAROverlayActive = visible;
            if (_arHudOverlayObj != null) _arHudOverlayObj.SetActive(visible);
        }

        public Action OnScanCardClicked;
        public Action OnToggleARClicked;
        public Action OnResetGameClicked;
        public Action OnTriggerAIClicked;

        private GameObject _hudCanvasObj;
        private CanvasScaler _hudScaler;
        private float _lastHudAspect = -1f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            BuildHUD();
            SetHUDVisible(false);
        }

        private void Start()
        {
            // Connect Pawn Promotion callback to 3D Chessboard Manager
            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.OnPawnPromotionNeeded = ShowPromotionModal;
            }

            if (MainMenu2D.Instance != null && MainMenu2D.Instance.IsMenuVisible())
            {
                SetHUDVisible(false);
            }
        }

        public void SetHUDVisible(bool visible)
        {
            if (_hudCanvasObj != null)
            {
                _hudCanvasObj.SetActive(visible);
            }
            if (!visible)
            {
                SetTacticalDrawerOpen(false);
            }
        }

        public void OpenOnlineModal()
        {
            if (_multiplayerModal != null)
            {
                _multiplayerModal.SetActive(true);
            }
        }

        public void ReturnToMainMenu()
        {
            SetHUDVisible(false);
            if (MechChess.AR.HybridARController.Instance != null)
            {
                MechChess.AR.HybridARController.Instance.SetARActive(false);
            }
            if (MainMenu2D.Instance != null)
            {
                MainMenu2D.Instance.ShowMenu();
            }
        }

        private void BuildHUD()
        {
            // Ensure EventSystem exists for UI button pointer interactions
            if (EventSystem.current == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }

            // Canvas
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvasObj.transform.SetParent(transform, false);
            _hudCanvasObj = canvasObj;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            _hudScaler = canvasObj.AddComponent<CanvasScaler>();
            _hudScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _hudScaler.referenceResolution = new Vector2(1920, 1080);
            float initialHudAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            _lastHudAspect = initialHudAspect;
            _hudScaler.matchWidthOrHeight = (initialHudAspect < 1.7777f) ? 0f : 1f;
            canvasObj.AddComponent<GraphicRaycaster>();

            Font defaultFont = GetDefaultFont();

            // =========================================================================
            // 1. TOP HEADER: Suspended Arcane Command Capsule (Screen 8 & 9)
            // =========================================================================
            GameObject topBar = CreatePanel(canvasObj.transform, "TopBar", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform tbRt = topBar.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.5f, 1f);
            tbRt.anchorMax = new Vector2(0.5f, 1f);
            tbRt.pivot = new Vector2(0.5f, 1f);
            tbRt.anchoredPosition = new Vector2(0f, -14f);
            tbRt.sizeDelta = new Vector2(980f, 68f);

            // Left Wing: Player Avatar, Name, Level & White Chronometer (09:45)
            GameObject playerCapsule = new GameObject("PlayerCapsule");
            playerCapsule.transform.SetParent(topBar.transform, false);
            RectTransform pcRt = playerCapsule.AddComponent<RectTransform>();
            pcRt.anchorMin = new Vector2(0f, 0.5f);
            pcRt.anchorMax = new Vector2(0f, 0.5f);
            pcRt.pivot = new Vector2(0f, 0.5f);
            pcRt.anchoredPosition = new Vector2(16f, 0f);
            pcRt.sizeDelta = new Vector2(360f, 54f);

            GameObject avObj = new GameObject("Avatar");
            avObj.transform.SetParent(playerCapsule.transform, false);
            Image avImg = avObj.AddComponent<Image>();
            Sprite avSprite = ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            if (avSprite != null) avImg.sprite = avSprite;
            else avImg.color = Color.cyan;
            RectTransform avRt = avObj.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0f, 0.5f);
            avRt.anchorMax = new Vector2(0f, 0.5f);
            avRt.pivot = new Vector2(0f, 0.5f);
            avRt.anchoredPosition = new Vector2(0f, 0f);
            avRt.sizeDelta = new Vector2(44f, 44f);

            CreateText(playerCapsule.transform, "PlayerName", "Player001", 15, TextAnchor.MiddleLeft, new Vector2(52f, 8f), new Vector2(140f, 22f), Color.white, defaultFont, true);
            CreateText(playerCapsule.transform, "PlayerLevel", "Lv. 5", 11, TextAnchor.MiddleLeft, new Vector2(52f, -10f), new Vector2(140f, 18f), new Color(0.0f, 0.85f, 1.0f), defaultFont, true);

            // White Timer Box
            GameObject whiteTimerBox = CreatePanel(playerCapsule.transform, "WhiteTimerBox", new Vector2(275f, 0f), new Vector2(130f, 44f), CyberButtonColor.Cyan);
            _whiteTimerText = CreateText(whiteTimerBox.transform, "WhiteTimer", "10:00", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(120f, 36f), Color.white, defaultFont, true);

            // Center Node: VS Badge + Turn Indicator
            GameObject centerNode = CreatePanel(topBar.transform, "CenterNode", Vector2.zero, new Vector2(180f, 54f), CyberButtonColor.Gunmetal);
            RectTransform cnRt = centerNode.GetComponent<RectTransform>();
            cnRt.anchorMin = new Vector2(0.5f, 0.5f);
            cnRt.anchorMax = new Vector2(0.5f, 0.5f);
            cnRt.pivot = new Vector2(0.5f, 0.5f);
            cnRt.anchoredPosition = Vector2.zero;

            _turnIndicatorText = CreateText(centerNode.transform, "TurnIndicator", "VS", 18, TextAnchor.MiddleCenter, new Vector2(0f, 8f), new Vector2(160f, 26f), new Color(0.0f, 0.95f, 1.0f), defaultFont, true);
            _firstMoveWarningText = CreateText(centerNode.transform, "FirstMoveWarning", "⏱️ 60s", 11, TextAnchor.MiddleCenter, new Vector2(0f, -12f), new Vector2(160f, 20f), new Color(1.0f, 0.75f, 0.1f), defaultFont, true);

            // Right Wing: Black Chronometer + AI / Opponent Info (10:00)
            GameObject aiCapsule = new GameObject("AICapsule");
            aiCapsule.transform.SetParent(topBar.transform, false);
            RectTransform acRt = aiCapsule.AddComponent<RectTransform>();
            acRt.anchorMin = new Vector2(1f, 0.5f);
            acRt.anchorMax = new Vector2(1f, 0.5f);
            acRt.pivot = new Vector2(1f, 0.5f);
            acRt.anchoredPosition = new Vector2(-16f, 0f);
            acRt.sizeDelta = new Vector2(360f, 54f);

            // Black Timer Box
            GameObject blackTimerBox = CreatePanel(aiCapsule.transform, "BlackTimerBox", new Vector2(-275f, 0f), new Vector2(130f, 44f), CyberButtonColor.Crimson);
            _blackTimerText = CreateText(blackTimerBox.transform, "BlackTimer", "10:00", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(120f, 36f), Color.white, defaultFont, true);

            CreateText(aiCapsule.transform, "AIName", "AI", 15, TextAnchor.MiddleRight, new Vector2(-52f, 8f), new Vector2(140f, 22f), Color.white, defaultFont, true);
            CreateText(aiCapsule.transform, "AILevel", "Lv. 5", 11, TextAnchor.MiddleRight, new Vector2(-52f, -10f), new Vector2(140f, 18f), new Color(1.0f, 0.35f, 0.45f), defaultFont, true);

            GameObject aiAvObj = new GameObject("AIAvatar");
            aiAvObj.transform.SetParent(aiCapsule.transform, false);
            Image aiAvImg = aiAvObj.AddComponent<Image>();
            aiAvImg.sprite = ArcaneAssetLoader.GetCircleSprite(44, new Color(0.35f, 0.08f, 0.12f), new Color(1.0f, 0.35f, 0.45f), 1.5f);
            RectTransform aiavRt = aiAvObj.GetComponent<RectTransform>();
            aiavRt.anchorMin = new Vector2(1f, 0.5f);
            aiavRt.anchorMax = new Vector2(1f, 0.5f);
            aiavRt.pivot = new Vector2(1f, 0.5f);
            aiavRt.anchoredPosition = Vector2.zero;
            aiavRt.sizeDelta = new Vector2(44f, 44f);
            CreateText(aiAvObj.transform, "Icon", "🤖", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(44f, 44f), Color.white, defaultFont, false);

            // =========================================================================
            // 2. LEFT QUICK ACTION TOOLBAR (Vertical Stack matching Screen 8)
            // =========================================================================
            GameObject leftToolbar = new GameObject("LeftQuickToolbar");
            leftToolbar.transform.SetParent(canvasObj.transform, false);
            RectTransform ltRt = leftToolbar.AddComponent<RectTransform>();
            ltRt.anchorMin = new Vector2(0f, 0.5f);
            ltRt.anchorMax = new Vector2(0f, 0.5f);
            ltRt.pivot = new Vector2(0f, 0.5f);
            ltRt.anchoredPosition = new Vector2(25f, 0f);
            ltRt.sizeDelta = new Vector2(60f, 320f);

            CreateCyberGameButton(leftToolbar.transform, "BtnHome", "🏠", "", "", new Vector2(0f, 120f), new Vector2(54f, 54f), CyberButtonColor.Gunmetal, ReturnToMainMenu, defaultFont);
            CreateCyberGameButton(leftToolbar.transform, "BtnUndo", "↩️", "", "", new Vector2(0f, 60f), new Vector2(54f, 54f), CyberButtonColor.Cyan, () =>
            {
                if (isRankedMode)
                {
                    RecordMove("[TỪ CHỐI] Không thể hoàn tác trong chế độ Đấu Xếp Hạng!");
                    return;
                }
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.UndoMove();
            }, defaultFont);
            CreateCyberGameButton(leftToolbar.transform, "BtnHint", "💡", "", "", new Vector2(0f, 0f), new Vector2(54f, 54f), CyberButtonColor.Amber, () =>
            {
                OnTriggerAIClicked?.Invoke();
            }, defaultFont);
            CreateCyberGameButton(leftToolbar.transform, "BtnResign", "✖️", "", "", new Vector2(0f, -60f), new Vector2(54f, 54f), CyberButtonColor.Crimson, () =>
            {
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.Resign(_activeColor);
            }, defaultFont);
            CreateCyberGameButton(leftToolbar.transform, "BtnSettings", "⚙️", "", "", new Vector2(0f, -120f), new Vector2(54f, 54f), CyberButtonColor.Gunmetal, () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.OpenSettingsModal();
            }, defaultFont);

            // =========================================================================
            // 2. IN-GAME COLLAPSIBLE SIDEBAR DRAWER (Tucked away, slides out on demand)
            // =========================================================================
            // Dark Backdrop (tapping outside closes drawer)
            GameObject backdrop = new GameObject("TacticalDrawerBackdrop");
            backdrop.transform.SetParent(canvasObj.transform, false);
            RectTransform brt = backdrop.AddComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            Image bImg = backdrop.AddComponent<Image>();
            bImg.color = new Color(0f, 0f, 0f, 0.45f);
            bImg.raycastTarget = true;
            Button bBtn = backdrop.AddComponent<Button>();
            bBtn.onClick.AddListener(() => SetTacticalDrawerOpen(false));
            _drawerBackdropObj = backdrop;

            // Sidebar Drawer Panel (Anchored to Right Side)
            GameObject drawer = CreatePanel(canvasObj.transform, "TacticalDrawer", Vector2.zero, Vector2.zero, CyberButtonColor.ModalFrame);
            RectTransform dRt = drawer.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(1f, 0f);
            dRt.anchorMax = new Vector2(1f, 1f);
            dRt.pivot = new Vector2(1f, 0.5f);
            dRt.anchoredPosition = new Vector2(-15f, 0f);
            dRt.sizeDelta = new Vector2(460f, -30f);
            Image dImg = drawer.GetComponent<Image>();
            if (dImg != null) dImg.raycastTarget = true;
            _tacticalDrawerObj = drawer;

            // --- Drawer Header (Fixed at top of drawer) ---
            GameObject drawerHeader = CreatePanel(drawer.transform, "DrawerHeader", Vector2.zero, Vector2.zero, CyberButtonColor.Gunmetal);
            RectTransform dhRt = drawerHeader.GetComponent<RectTransform>();
            dhRt.anchorMin = new Vector2(0f, 1f);
            dhRt.anchorMax = new Vector2(1f, 1f);
            dhRt.pivot = new Vector2(0.5f, 1f);
            dhRt.anchoredPosition = new Vector2(0f, -8f);
            dhRt.sizeDelta = new Vector2(-16f, 44f);

            CreateText(drawerHeader.transform, "DrawerTitle", "/// TACTICAL COMMAND & LOGS ///", 13, TextAnchor.MiddleLeft, new Vector2(-35f, 0f), new Vector2(280f, 36f), new Color(0.0f, 0.95f, 1.0f), defaultFont, true);
            CreateCyberGameButton(drawerHeader.transform, "BtnCloseDrawer", "✕", "ĐÓNG", "", new Vector2(170f, 0f), new Vector2(74f, 32f), CyberButtonColor.Crimson, () => SetTacticalDrawerOpen(false), defaultFont);

            // --- Scrollable View Area (Fills space from below header to bottom) ---
            GameObject scrollAreaObj = new GameObject("DrawerScrollArea");
            scrollAreaObj.transform.SetParent(drawer.transform, false);
            RectTransform saRt = scrollAreaObj.AddComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero;
            saRt.anchorMax = Vector2.one;
            saRt.offsetMin = new Vector2(10f, 10f);
            saRt.offsetMax = new Vector2(-10f, -56f);

            ScrollRect scrollRect = scrollAreaObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollAreaObj.transform, false);
            RectTransform vpRt = viewportObj.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewportObj.AddComponent<RectMask2D>();

            // Content Container
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 790f);

            scrollRect.viewport = vpRt;
            scrollRect.content = contentRt;

            // --- Section 1: Match Actions (Thao tác trận đấu) ---
            CreatePanel(contentObj.transform, "Sec1Header", new Vector2(0, -15), new Vector2(426, 24), CyberButtonColor.PanelFrame);
            CreateText(contentObj.transform, "Sec1Title", "── THAO TÁC TRẬN ĐẤU ──", 12, TextAnchor.MiddleCenter, new Vector2(0, -15), new Vector2(420, 22), new Color(1.0f, 0.8f, 0.2f), defaultFont, true);

            CreateCyberGameButton(contentObj.transform, "DrawerUndo", "↩️", "HOÀN TÁC", "", new Vector2(-142, -54), new Vector2(136, 44), CyberButtonColor.Cyan, () =>
            {
                if (isRankedMode)
                {
                    RecordMove("[TỪ CHỐI] Không thể hoàn tác trong chế độ Đấu Xếp Hạng (Ranked)!");
                    return;
                }
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.UndoMove();
            }, defaultFont);

            CreateCyberGameButton(contentObj.transform, "DrawerDraw", "🤝", "XIN HÒA", "", new Vector2(0, -54), new Vector2(136, 44), CyberButtonColor.Amber, () =>
            {
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.OfferDraw();
            }, defaultFont);

            CreateCyberGameButton(contentObj.transform, "DrawerResign", "🏳️", "ĐẦU HÀNG", "", new Vector2(142, -54), new Vector2(136, 44), CyberButtonColor.Crimson, () =>
            {
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.Resign(_activeColor);
            }, defaultFont);

            // --- Section 2: Tactical Commands & Modes (Công cụ tác chiến) ---
            CreatePanel(contentObj.transform, "Sec2Header", new Vector2(0, -96), new Vector2(426, 24), CyberButtonColor.PanelFrame);
            CreateText(contentObj.transform, "Sec2Title", "── HỆ THỐNG TÁC CHIẾN & CHẾ ĐỘ ──", 12, TextAnchor.MiddleCenter, new Vector2(0, -96), new Vector2(420, 22), new Color(0.0f, 0.95f, 1.0f), defaultFont, true);

            // Row 1: AI CV Card Scan & AI Auto Strike
            CreateCyberGameButton(contentObj.transform, "BtnScanCard", "📷", "SCAN CARD", "AI CV RECOGNITION", new Vector2(-108, -138), new Vector2(210, 46), CyberButtonColor.Cyan, () =>
            {
                SetTacticalDrawerOpen(false);
                OnScanCardClicked?.Invoke();
            }, defaultFont);

            CreateCyberGameButton(contentObj.transform, "BtnAIMove", "🤖", "TRIGGER AI", "STOCKFISH STRIKE", new Vector2(108, -138), new Vector2(210, 46), CyberButtonColor.Crimson, () =>
            {
                OnTriggerAIClicked?.Invoke();
            }, defaultFont);

            // Row 2: Online 2P Lobby & AR Matrix View
            CreateCyberGameButton(contentObj.transform, "BtnOnlineLobby", "🌐", "ONLINE 2P", "WEBSOCKET ROOM HUB", new Vector2(-108, -190), new Vector2(210, 46), CyberButtonColor.Sapphire, () =>
            {
                SetTacticalDrawerOpen(false);
                if (_multiplayerModal != null) _multiplayerModal.SetActive(true);
            }, defaultFont);

            CreateCyberGameButton(contentObj.transform, "BtnToggleAR", "📹", "AR WEBCAM", "CHẾ ĐỘ AR CAMERA", new Vector2(108, -190), new Vector2(210, 46), CyberButtonColor.Emerald, () =>
            {
                OnToggleARClicked?.Invoke();
            }, defaultFont);

            // Row 3: 2D/3D Perspective Toggle & 2D Combat Minigame Hack
            CreateCyberGameButton(contentObj.transform, "BtnToggle2DView", "📐", "GÓC NHÌN", "VIEW 2D/3D TOGGLE", new Vector2(-108, -242), new Vector2(210, 46), CyberButtonColor.Violet, () =>
            {
                var touch = Camera.main != null ? Camera.main.GetComponent<MechChess.Mobile.MobileTouchInput>() : null;
                if (touch != null)
                {
                    bool is2D = touch.Toggle2DView();
                    RecordMove(is2D ? "[VIEW] Chuyển sang góc nhìn 2D (Top-down)" : "[VIEW] Chuyển sang góc nhìn 3D (Isometric)");
                }
            }, defaultFont);

            CreateCyberGameButton(contentObj.transform, "BtnTest2DCombat", "⚡", "2D COMBAT", "FIREWALL OVERRIDE", new Vector2(108, -242), new Vector2(210, 46), CyberButtonColor.Amber, () =>
            {
                SetTacticalDrawerOpen(false);
                if (MechChess.Combat2D.CombatMinigame2D.Instance != null)
                {
                    RecordMove("[2D COMBAT] Manually Initiated Override Minigame!");
                    MechChess.Combat2D.CombatMinigame2D.Instance.StartMinigame((win) =>
                    {
                        RecordMove(win ? "[2D COMBAT] Firewall Overridden: 100% Lethal Strike!" : "[2D COMBAT] Circuit Timeout: Standard Strike.");
                    });
                }
            }, defaultFont);

            // Row 4: Full-Width Emergency Reset Match Button
            CreateCyberGameButton(contentObj.transform, "BtnReset", "🔄", "RESET TRẬN ĐẤU", "PURGE FIELD & RESTART", new Vector2(0, -292), new Vector2(426, 42), CyberButtonColor.Gunmetal, () =>
            {
                SetTacticalDrawerOpen(false);
                OnResetGameClicked?.Invoke();
            }, defaultFont);

            // --- Section 3: Battle Telemetry Log (Nhật ký nước đi PGN) ---
            CreatePanel(contentObj.transform, "Sec3Header", new Vector2(0, -332), new Vector2(426, 24), CyberButtonColor.PanelFrame);
            CreateText(contentObj.transform, "Sec3Title", "── NHẬT KÝ NƯỚC ĐI (PGN) ──", 12, TextAnchor.MiddleCenter, new Vector2(0, -332), new Vector2(420, 22), new Color(0.0f, 0.95f, 1.0f), defaultFont, true);

            CreateText(contentObj.transform, "HistoryColumns", "LƯỢT     ALLIANCE (W)       SYNDICATE (B)", 11, TextAnchor.MiddleLeft, new Vector2(0, -354), new Vector2(415, 18), new Color(0.0f, 0.85f, 1.0f, 0.75f), defaultFont, true);

            // Log Content Box
            GameObject logContainer = CreatePanel(contentObj.transform, "HistoryLogBox", new Vector2(0, -500), new Vector2(426, 260), CyberButtonColor.PanelFrame);
            _moveHistoryText = CreateText(logContainer.transform, "HistoryLog", "Match started.\nAwaiting initial moves...", 13, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(406, 240), new Color(0.9f, 0.95f, 1.0f), defaultFont, true);

            // Confidence display pod
            GameObject confPod = CreatePanel(contentObj.transform, "ConfidencePod", new Vector2(0, -655), new Vector2(426, 36), CyberButtonColor.Amber);
            _lastConfidenceText = CreateText(confPod.transform, "ConfidenceDisplay", "AI CV MATRIX: STANDBY (N/A)", 12, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(415, 30), new Color(1.0f, 0.85f, 0.2f), defaultFont, true);

            // System Telemetry indicators
            _aiServerStatusText = CreateText(contentObj.transform, "AIStatus", "● AI SERVER: ONLINE", 12, TextAnchor.MiddleLeft, new Vector2(-108, -694), new Vector2(200, 24), Color.green, defaultFont, true);
            _cloudStatusText = CreateText(contentObj.transform, "CloudStatus", "● AIO CLOUD: LINKED", 12, TextAnchor.MiddleRight, new Vector2(108, -694), new Vector2(200, 24), new Color(0.2f, 0.85f, 1.0f), defaultFont, true);

            // Hide Drawer by default (Clean board view!)
            _tacticalDrawerObj.SetActive(false);
            _drawerBackdropObj.SetActive(false);

            // =========================================================================
            // 3. BOTTOM-LEFT COMMANDER CARD (Screen 8)
            // =========================================================================
            GameObject cmdCard = CreatePanel(canvasObj.transform, "CommanderCard", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform cmdRt = cmdCard.GetComponent<RectTransform>();
            cmdRt.anchorMin = new Vector2(0f, 0f);
            cmdRt.anchorMax = new Vector2(0f, 0f);
            cmdRt.pivot = new Vector2(0f, 0f);
            cmdRt.anchoredPosition = new Vector2(25f, 20f);
            cmdRt.sizeDelta = new Vector2(220f, 80f);

            GameObject cmdPortObj = new GameObject("CommanderPortrait");
            cmdPortObj.transform.SetParent(cmdCard.transform, false);
            Image cmdImg = cmdPortObj.AddComponent<Image>();
            Sprite cmdSprite = ArcaneAssetLoader.LoadArcaneSprite("commander_portrait.png");
            if (cmdSprite != null) cmdImg.sprite = cmdSprite;
            else cmdImg.color = Color.cyan;
            cmdImg.preserveAspect = true;
            RectTransform cmdPortRt = cmdPortObj.GetComponent<RectTransform>();
            cmdPortRt.anchorMin = new Vector2(0f, 0.5f);
            cmdPortRt.anchorMax = new Vector2(0f, 0.5f);
            cmdPortRt.pivot = new Vector2(0f, 0.5f);
            cmdPortRt.anchoredPosition = new Vector2(10f, 0f);
            cmdPortRt.sizeDelta = new Vector2(60f, 60f);

            CreateText(cmdCard.transform, "CmdTitle", "COMMANDER", 13, TextAnchor.MiddleLeft, new Vector2(80f, 14f), new Vector2(120f, 22f), Color.white, defaultFont, true);
            CreateText(cmdCard.transform, "CmdStatus", "● SẴN SÀNG", 11, TextAnchor.MiddleLeft, new Vector2(80f, -12f), new Vector2(120f, 18f), new Color(0.2f, 0.95f, 0.5f), defaultFont, true);

            // =========================================================================
            // 4. BOTTOM-CENTER PIECE CEMETERY / TRAY (Screen 8)
            // =========================================================================
            GameObject pieceTray = CreatePanel(canvasObj.transform, "PieceTray", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform ptRt = pieceTray.GetComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0.5f, 0f);
            ptRt.anchorMax = new Vector2(0.5f, 0f);
            ptRt.pivot = new Vector2(0.5f, 0f);
            ptRt.anchoredPosition = new Vector2(0f, 20f);
            ptRt.sizeDelta = new Vector2(560f, 52f);

            string[] pieceIcons = { "♟", "♞", "♝", "♜", "♛", "♚" };
            string[] pieceNames = { "Tốt", "Mã", "Tượng", "Xe", "Hậu", "Vua" };
            float trayStartX = -225f;
            float trayStepX = 90f;
            for (int i = 0; i < 6; i++)
            {
                CreateText(pieceTray.transform, $"Piece_{i}", $"{pieceIcons[i]} {pieceNames[i]}", 13, TextAnchor.MiddleCenter, new Vector2(trayStartX + i * trayStepX, 0f), new Vector2(85f, 40f), new Color(0.85f, 0.92f, 1.0f), defaultFont, true);
            }

            // =========================================================================
            // 5. BOTTOM-RIGHT ACTION BUTTON: GOLDEN DIAMOND [TÁC CHIẾN] (Screen 8)
            // =========================================================================
            GameObject actionBtnObj = CreateCyberGameButton(canvasObj.transform, "BtnTacticalCommand", "⚡", "TÁC CHIẾN", "MENU & LOGS", Vector2.zero, new Vector2(170f, 60f), CyberButtonColor.Amber, ToggleTacticalDrawer, defaultFont);
            RectTransform actRt = actionBtnObj.GetComponent<RectTransform>();
            actRt.anchorMin = new Vector2(1f, 0f);
            actRt.anchorMax = new Vector2(1f, 0f);
            actRt.pivot = new Vector2(1f, 0f);
            actRt.anchoredPosition = new Vector2(-25f, 20f);

            // =========================================================================
            // 6. SCREEN 9: AR IN-GAME HUD OVERLAY
            // =========================================================================
            BuildScreen9AROverlay(canvasObj.transform, defaultFont);

            // =========================================================================
            // 7. ONLINE MULTIPLAYER MODAL (Tactical Netlink Dialog)
            // =========================================================================
            BuildMultiplayerModal(canvasObj.transform, defaultFont);

            // =========================================================================
            // 8. PAWN PROMOTION SELECTION MODAL (F01 Full Rule Implementation)
            // =========================================================================
            BuildPromotionModal(canvasObj.transform, defaultFont);
        }

        private void BuildScreen9AROverlay(Transform parent, Font font)
        {
            _arHudOverlayObj = new GameObject("Screen9_AROverlay");
            _arHudOverlayObj.transform.SetParent(parent, false);
            RectTransform arRt = _arHudOverlayObj.AddComponent<RectTransform>();
            arRt.anchorMin = Vector2.zero;
            arRt.anchorMax = Vector2.one;
            arRt.offsetMin = Vector2.zero;
            arRt.offsetMax = Vector2.zero;

            // Center-Bottom Floating Guidance Banner
            GameObject bannerObj = CreatePanel(_arHudOverlayObj.transform, "ARGuideBanner", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform bRt = bannerObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 85f);
            bRt.sizeDelta = new Vector2(480f, 44f);

            CreateText(bannerObj.transform, "GuideTxt", "Chạm vào quân cờ để di chuyển", 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(460f, 36f), new Color(0.0f, 0.95f, 1.0f), font, true);

            // Right Vertical AR Controls
            GameObject arControls = new GameObject("ARRightControls");
            arControls.transform.SetParent(_arHudOverlayObj.transform, false);
            RectTransform acRt = arControls.AddComponent<RectTransform>();
            acRt.anchorMin = new Vector2(1f, 0.5f);
            acRt.anchorMax = new Vector2(1f, 0.5f);
            acRt.pivot = new Vector2(1f, 0.5f);
            acRt.anchoredPosition = new Vector2(-25f, 0f);
            acRt.sizeDelta = new Vector2(80f, 260f);

            CreateCyberGameButton(arControls.transform, "BtnPhoto", "📷", "CHỤP", "", new Vector2(0f, 80f), new Vector2(74f, 60f), CyberButtonColor.Cyan, () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.ShowToast("Đã chụp và lưu ảnh AR!", ToastType.Success);
            }, font);

            CreateCyberGameButton(arControls.transform, "BtnVideo", "🎥", "QUAY", "", new Vector2(0f, 0f), new Vector2(74f, 60f), CyberButtonColor.Crimson, () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.ShowToast("Đang ghi hình trận đấu AR...", ToastType.Info);
            }, font);

            CreateCyberGameButton(arControls.transform, "BtnARSettings", "⚙️", "CÀI ĐẶT", "AR", new Vector2(0f, -80f), new Vector2(74f, 60f), CyberButtonColor.Gunmetal, () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.ShowToast("Đã hiệu chuẩn mặt phẳng AR!", ToastType.Success);
            }, font);

            // Bottom-Left Exit AR Button
            GameObject exitARBtn = CreateCyberGameButton(_arHudOverlayObj.transform, "BtnExitAR", "🚪", "THOÁT AR", "", Vector2.zero, new Vector2(140f, 48f), CyberButtonColor.Crimson, () =>
            {
                SetARHUDVisible(false);
                OnToggleARClicked?.Invoke();
            }, font);
            RectTransform ebRt = exitARBtn.GetComponent<RectTransform>();
            ebRt.anchorMin = new Vector2(0f, 0f);
            ebRt.anchorMax = new Vector2(0f, 0f);
            ebRt.pivot = new Vector2(0f, 0f);
            ebRt.anchoredPosition = new Vector2(25f, 115f);

            _arHudOverlayObj.SetActive(false);
        }

        private void BuildPromotionModal(Transform parent, Font font)
        {
            _promotionModal = CreatePanel(parent, "PromotionModal", Vector2.zero, new Vector2(660, 230), CyberButtonColor.ModalFrame);

            CreatePanel(_promotionModal.transform, "PromoHeader", new Vector2(0, 75), new Vector2(620, 36), CyberButtonColor.Cyan);
            CreateText(_promotionModal.transform, "PromoTitle", "/// PHONG CẤP TỐT (PAWN PROMOTION) ///", 15, TextAnchor.MiddleCenter, new Vector2(0, 75), new Vector2(600, 30), Color.white, font, true);

            CreateText(_promotionModal.transform, "PromoSub", "Tốt đã tiến tới hàng cuối! Hãy chọn đơn vị Mech chi viện:", 13, TextAnchor.MiddleCenter, new Vector2(0, 35), new Vector2(600, 26), new Color(0.8f, 0.95f, 1.0f), font, true);

            // 4 Selection Buttons: Queen, Rook, Bishop, Knight
            CreateCyberGameButton(_promotionModal.transform, "PromoQueen", "👑", "QUEEN (HẬU)", "HỎA LỰC TỐI THƯỢNG", new Vector2(-230, -32), new Vector2(148, 60), CyberButtonColor.Violet, () => SelectPromotionPiece(PieceType.Queen), font);
            CreateCyberGameButton(_promotionModal.transform, "PromoRook", "🏰", "ROOK (XE)", "PHÁO ĐÀI CƠ ĐỘNG", new Vector2(-77, -32), new Vector2(148, 60), CyberButtonColor.Emerald, () => SelectPromotionPiece(PieceType.Rook), font);
            CreateCyberGameButton(_promotionModal.transform, "PromoBishop", "⚔️", "BISHOP (TƯỢNG)", "TIA LASER TẦM XA", new Vector2(77, -32), new Vector2(148, 60), CyberButtonColor.Amber, () => SelectPromotionPiece(PieceType.Bishop), font);
            CreateCyberGameButton(_promotionModal.transform, "PromoKnight", "🐴", "KNIGHT (MÃ)", "ĐỘT KÍCH XUYÊN GIÁP", new Vector2(230, -32), new Vector2(148, 60), CyberButtonColor.Cyan, () => SelectPromotionPiece(PieceType.Knight), font);

            _promotionModal.SetActive(false);
        }

        public void ShowPromotionModal(MechChess.Chess3D.ChessPiece3D pawn, Action<PieceType> callback)
        {
            _onPromotionSelected = callback;
            if (_promotionModal != null)
            {
                _promotionModal.SetActive(true);
            }
            else
            {
                callback?.Invoke(PieceType.Queen);
            }
        }

        private void SelectPromotionPiece(PieceType selectedType)
        {
            if (_promotionModal != null) _promotionModal.SetActive(false);
            _onPromotionSelected?.Invoke(selectedType);
            _onPromotionSelected = null;
        }

        private void BuildMultiplayerModal(Transform parent, Font font)
        {
            _multiplayerModal = CreatePanel(parent, "MultiplayerModal", Vector2.zero, new Vector2(800, 540), CyberButtonColor.ModalFrame);

            // Header Banner
            CreatePanel(_multiplayerModal.transform, "ModalHeader", new Vector2(0, 235), new Vector2(760, 46), CyberButtonColor.Sapphire);
            CreateText(_multiplayerModal.transform, "ModalTitle", "🌐 AEGIS-MECH SECURE NETLINK: PHÒNG THI ĐẤU ONLINE 2P", 19, TextAnchor.MiddleCenter, new Vector2(0, 235), new Vector2(740, 40), Color.white, font, true);

            // Subtitle
            CreateText(_multiplayerModal.transform, "ModalSubtitle", "Kết nối thời gian thực qua Backend AI Docker :8000 | Chơi đối kháng PC vs PC hoặc PC vs Android", 13, TextAnchor.MiddleCenter, new Vector2(0, 192), new Vector2(740, 26), new Color(0.7f, 0.88f, 1.0f), font, true);

            // Server Address Box
            GameObject serverBox = CreatePanel(_multiplayerModal.transform, "ServerBox", new Vector2(0, 150), new Vector2(740, 36), CyberButtonColor.Gunmetal);
            CreateText(serverBox.transform, "ServerAddressInfo", "MÁY CHỦ: ws://localhost:8000 (Chơi LAN: chỉnh IP máy chủ trong OnlineMultiplayerManager)", 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(720, 30), new Color(1.0f, 0.85f, 0.2f), font, true);

            // Status Badge
            _multiplayerStatusText = CreateText(_multiplayerModal.transform, "MultiplayerStatus", "Trạng thái: Chưa vào phòng nào", 16, TextAnchor.MiddleCenter, new Vector2(0, 105), new Vector2(720, 35), new Color(1.0f, 0.75f, 0.1f), font, true);

            // Role Explanation Pod
            GameObject roleBox = CreatePanel(_multiplayerModal.transform, "RoleBox", new Vector2(0, 48), new Vector2(740, 54), CyberButtonColor.PanelFrame);
            _multiplayerRoleText = CreateText(roleBox.transform, "MultiplayerRole", "👉 Người vào phòng ĐẦU TIÊN: Cầm LIÊN MINH - QUÂN TRẮNG (Host - Đi trước)\n👉 Người vào phòng THỨ HAI: Cầm HẮC ÁM - QUÂN ĐEN (Guest - Chờ đối thủ)", 13, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(720, 48), Color.white, font, true);

            // Quick Room Selection Header
            CreateText(_multiplayerModal.transform, "RoomSelectLabel", "-- BẤM NÚT ĐỂ KẾT NỐI VÀO PHÒNG CHIẾN --", 14, TextAnchor.MiddleCenter, new Vector2(0, -5), new Vector2(600, 28), new Color(0.0f, 0.95f, 1.0f), font, true);

            // Room Buttons
            CreateCyberGameButton(_multiplayerModal.transform, "BtnRoom101", "⚔️", "VÀO PHÒNG: ROOM-101", "SECTOR CYBER MATRIX", new Vector2(-190, -60), new Vector2(360, 54), CyberButtonColor.Sapphire, () =>
            {
                JoinOnlineRoom("ROOM-101");
            }, font);

            CreateCyberGameButton(_multiplayerModal.transform, "BtnRoom202", "⚔️", "VÀO PHÒNG: ROOM-202", "SECTOR NEO TOKYO", new Vector2(190, -60), new Vector2(360, 54), CyberButtonColor.Violet, () =>
            {
                JoinOnlineRoom("ROOM-202");
            }, font);

            CreateCyberGameButton(_multiplayerModal.transform, "BtnRoom303", "⚔️", "VÀO PHÒNG: ROOM-303", "SECTOR ORBITAL RUINS", new Vector2(-190, -125), new Vector2(360, 54), CyberButtonColor.Emerald, () =>
            {
                JoinOnlineRoom("ROOM-303");
            }, font);

            CreateCyberGameButton(_multiplayerModal.transform, "BtnDisconnectOnline", "🚪", "RỜI PHÒNG ONLINE", "DISCONNECT NETLINK", new Vector2(190, -125), new Vector2(360, 54), CyberButtonColor.Crimson, () =>
            {
                if (MechChess.Multiplayer.OnlineMultiplayerManager.Instance != null)
                {
                    MechChess.Multiplayer.OnlineMultiplayerManager.Instance.Disconnect();
                    UpdateMultiplayerUIStatus("Đã ngắt kết nối phòng online", Color.white);
                }
            }, font);

            // Close button
            CreateCyberGameButton(_multiplayerModal.transform, "BtnCloseModal", "✖", "ĐÓNG BẢNG LOBBY", "RETURN TO COMBAT", new Vector2(0, -200), new Vector2(300, 48), CyberButtonColor.Gunmetal, () =>
            {
                _multiplayerModal.SetActive(false);
            }, font);

            _multiplayerModal.SetActive(false);
        }

        private void JoinOnlineRoom(string roomId)
        {
            if (MechChess.Multiplayer.OnlineMultiplayerManager.Instance != null)
            {
                UpdateMultiplayerUIStatus($"Đang kết nối vào {roomId}...", Color.yellow);
                MechChess.Multiplayer.OnlineMultiplayerManager.Instance.ConnectAndJoinRoom(roomId);
            }
        }

        public void UpdateMultiplayerUIStatus(string status, Color color)
        {
            if (_multiplayerStatusText != null)
            {
                _multiplayerStatusText.text = $"Trạng thái: {status}";
                _multiplayerStatusText.color = color;
            }
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Tahoma" }, 16);
            return font;
        }

        private void Update()
        {
            // Dynamic adaptive CanvasScaler for multi-device responsive HUD
            if (_hudScaler != null)
            {
                float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                if (Mathf.Abs(aspect - _lastHudAspect) > 0.01f)
                {
                    _lastHudAspect = aspect;
                    _hudScaler.matchWidthOrHeight = (aspect < 1.7777f) ? 0f : 1f;
                }
            }

            if (!_isTimerRunning || _isMatchAborted || _isGameOver) return;

            // 1. First Move Rule: If 1st move has not been made, count down the 60s abort timer
            if (_isFirstMoveWaiting)
            {
                _firstMoveTimer = Mathf.Max(0f, _firstMoveTimer - Time.deltaTime);

                if (_firstMoveTimer <= 0f)
                {
                    AbortMatchFirstMoveTimeout();
                    return;
                }

                if (_firstMoveWarningText != null)
                {
                    int secLeft = Mathf.CeilToInt(_firstMoveTimer);
                    _firstMoveWarningText.text = $"⏱️ NƯỚC ĐẦU: {secLeft}s (HỦY NẾU HẾT)";
                    if (secLeft <= 15)
                    {
                        float pulse = Mathf.PingPong(Time.time * 4f, 1f);
                        _firstMoveWarningText.color = Color.Lerp(new Color(1f, 0.45f, 0f), Color.red, pulse);
                    }
                    else
                    {
                        _firstMoveWarningText.color = new Color(1.0f, 0.75f, 0.1f);
                    }
                }
            }

            // 2. 10-Minute Main Clock for Active Player
            if (_activeColor == PieceColor.White)
            {
                _whiteTimer = Mathf.Max(0f, _whiteTimer - Time.deltaTime);
                if (_whiteTimer <= 0f)
                {
                    TriggerTimeoutGameOver(PieceColor.White);
                    return;
                }
            }
            else
            {
                _blackTimer = Mathf.Max(0f, _blackTimer - Time.deltaTime);
                if (_blackTimer <= 0f)
                {
                    TriggerTimeoutGameOver(PieceColor.Black);
                    return;
                }
            }

            if (_whiteTimerText != null) _whiteTimerText.text = FormatTime(_whiteTimer);
            if (_blackTimerText != null) _blackTimerText.text = FormatTime(_blackTimer);

            // Subtle live glow pulse on turn indicator
            if (_turnIndicatorText != null)
            {
                float glow = 0.82f + 0.18f * Mathf.Sin(Time.time * 3f);
                if (_activeColor == PieceColor.White)
                    _turnIndicatorText.color = new Color(0.0f, 0.95f * glow, 1.0f * glow);
                else
                    _turnIndicatorText.color = new Color(1.0f * glow, 0.25f, 0.35f);
            }
        }

        private void AbortMatchFirstMoveTimeout()
        {
            _isMatchAborted = true;
            _isTimerRunning = false;

            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.isGameOver = true;
                MechChess.Chess3D.ChessBoard3DManager.Instance.TriggerEMPShockwave();
            }

            if (_turnIndicatorText != null)
            {
                _turnIndicatorText.text = "⚠️ TRẬN ĐẤU ĐÃ HỦY: Quá 1 phút không có nước đi đầu!";
                _turnIndicatorText.color = Color.red;
            }

            if (_firstMoveWarningText != null)
            {
                _firstMoveWarningText.text = "⛔ TRẬN ĐẤU ĐÃ BỊ HỦY TỰ ĐỘNG";
                _firstMoveWarningText.color = Color.red;
            }

            RecordMove("===============================");
            RecordMove("[HỦY TRẬN] Hết 1 phút nước đi đầu tiên!");
            RecordMove("Trận đấu đã bị hủy tự động.");
            RecordMove("Nhấn [RESET MATCH] để chơi lại.");
            RecordMove("===============================");

            if (MechChess.Cloud.AdafruitIOCloudConnector.Instance != null)
            {
                MechChess.Cloud.AdafruitIOCloudConnector.Instance.PublishTelemetry(1, 0f, "Match_Aborted_Timeout", "ABORTED_1MIN_TIMEOUT");
            }
        }

        public void TriggerGameOver(PieceColor winner, string reason)
        {
            _isGameOver = true;
            _isTimerRunning = false;

            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.isGameOver = true;
            }

            string winnerName = (winner == PieceColor.White) ? "LIÊN MINH (WHITE)" : "HẮC ÁM (BLACK)";
            if (_turnIndicatorText != null)
            {
                _turnIndicatorText.text = $"🏆 {winnerName} CHIẾN THẮNG!";
                _turnIndicatorText.color = Color.yellow;
            }

            RecordMove($"[KẾT THÚC] {winnerName} chiến thắng! ({reason})");

            if (MainMenu2D.Instance != null)
            {
                bool isPlayerVictory = (winner == PieceColor.White);
                float matchDuration = _initialTime - _whiteTimer;
                MainMenu2D.Instance.ShowResultScreen(isPlayerVictory, reason, _moveHistory.Count, matchDuration);
            }
        }

        private void TriggerTimeoutGameOver(PieceColor timedOutPlayer)
        {
            PieceColor winner = (timedOutPlayer == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            TriggerGameOver(winner, "Hết thời gian trận đấu!");
        }

        public void ResetGameTimers()
        {
            _whiteTimer = _initialTime;
            _blackTimer = _initialTime;
            _firstMoveTimer = 60f;
            _isFirstMoveWaiting = true;
            _isMatchAborted = false;
            _isGameOver = false;
            _isTimerRunning = true;
            _activeColor = PieceColor.White;

            if (_whiteTimerText != null) _whiteTimerText.text = FormatTime(_whiteTimer);
            if (_blackTimerText != null) _blackTimerText.text = FormatTime(_blackTimer);

            if (_firstMoveWarningText != null)
            {
                _firstMoveWarningText.text = "⏱️ NƯỚC ĐẦU: 60s (HỦY NẾU HẾT)";
                _firstMoveWarningText.color = new Color(1.0f, 0.75f, 0.1f);
            }

            if (_turnIndicatorText != null)
            {
                _turnIndicatorText.text = ">>> CURRENT TURN: ALLIANCE (WHITE) <<<";
                _turnIndicatorText.color = new Color(0.0f, 0.95f, 1.0f);
            }
        }

        private string FormatTime(float seconds)
        {
            int min = (int)(seconds / 60);
            int sec = (int)(seconds % 60);
            return $"{min:00}:{sec:00}";
        }

        public void SetActiveTurn(PieceColor color)
        {
            // F02: Add increment time to the player who just completed their turn
            if (!_isFirstMoveWaiting && !_isGameOver && !_isMatchAborted && _timeIncrement > 0f)
            {
                if (color == PieceColor.Black && _activeColor == PieceColor.White)
                {
                    _whiteTimer += _timeIncrement;
                    if (_whiteTimerText != null) _whiteTimerText.text = FormatTime(_whiteTimer);
                }
                else if (color == PieceColor.White && _activeColor == PieceColor.Black)
                {
                    _blackTimer += _timeIncrement;
                    if (_blackTimerText != null) _blackTimerText.text = FormatTime(_blackTimer);
                }
            }

            _activeColor = color;
            if (_isMatchAborted || _isGameOver) return;

            if (color == PieceColor.White)
            {
                if (_turnIndicatorText != null)
                {
                    _turnIndicatorText.text = ">>> CURRENT TURN: ALLIANCE (WHITE) <<<";
                    _turnIndicatorText.color = new Color(0.0f, 0.95f, 1.0f);
                }
            }
            else
            {
                if (_turnIndicatorText != null)
                {
                    _turnIndicatorText.text = ">>> CURRENT TURN: SYNDICATE (BLACK) <<<";
                    _turnIndicatorText.color = new Color(1.0f, 0.25f, 0.35f);
                }
            }
        }

        public void UpdateTurnIndicator(string message, Color color)
        {
            if (_turnIndicatorText != null)
            {
                _turnIndicatorText.text = message;
                _turnIndicatorText.color = color;
            }
        }

        public void RecordMove(string moveNotation)
        {
            // If this is an actual game move and first move was waiting -> mark first move completed
            if (_isFirstMoveWaiting && !moveNotation.StartsWith("[") && !moveNotation.StartsWith(">") && !moveNotation.StartsWith("="))
            {
                _isFirstMoveWaiting = false;
                if (_firstMoveWarningText != null)
                {
                    _firstMoveWarningText.text = "✅ TRẬN ĐẤU ĐANG DIỄN RA";
                    _firstMoveWarningText.color = new Color(0.2f, 0.9f, 0.4f);
                }
            }

            _moveHistory.Add(moveNotation);
            if (_moveHistory.Count > 22) _moveHistory.RemoveAt(0);

            if (_moveHistoryText != null)
            {
                _moveHistoryText.text = string.Join("\n", _moveHistory);
            }
        }

        public void UpdateAIServerStatus(bool online, string detail = "")
        {
            if (_aiServerStatusText == null) return;
            if (online)
            {
                _aiServerStatusText.text = "● AI SERVER: ONLINE";
                _aiServerStatusText.color = Color.green;
            }
            else
            {
                _aiServerStatusText.text = "● AI SERVER: OFFLINE";
                _aiServerStatusText.color = Color.red;
            }
        }

        public void UpdateCloudStatus(bool connected, string detail = "")
        {
            if (_cloudStatusText == null) return;
            if (connected)
            {
                _cloudStatusText.text = "● AIO CLOUD: LINKED";
                _cloudStatusText.color = new Color(0.2f, 0.85f, 1.0f);
            }
            else
            {
                _cloudStatusText.text = "● AIO CLOUD: CONNECTING";
                _cloudStatusText.color = Color.yellow;
            }
        }

        public void UpdateConfidence(float confidence, string cardName)
        {
            if (_lastConfidenceText != null)
            {
                _lastConfidenceText.text = $"AI DETECT: {cardName} [{confidence * 100f:0.1f}%]";
            }
        }

        // =========================================================================
        // UI Construction Helpers
        // =========================================================================
        private GameObject CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, CyberButtonColor style)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            Image img = panel.AddComponent<Image>();
            img.sprite = ProceduralUISpriteGenerator.GetCyberSprite(style);
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = false;

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return panel;
        }

        private Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Vector2 pos, Vector2 size, Color color, Font font, bool addOutline = false)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = content;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = color;
            txt.raycastTarget = false;

            if (addOutline)
            {
                Outline outline = txtObj.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(1.2f, -1.2f);

                Shadow shadow = txtObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
                shadow.effectDistance = new Vector2(2f, -2f);
            }

            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return txt;
        }

        /// <summary>
        /// Creates an authentic sci-fi game action button with chamfered neon outline,
        /// glowing accent pip, uppercase title, tech subtitle, and interactive punch/sound effects.
        /// </summary>
        private GameObject CreateCyberGameButton(Transform parent, string name, string icon, string title, string subtitle, Vector2 pos, Vector2 size, CyberButtonColor style, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = ProceduralUISpriteGenerator.GetCyberSprite(style);
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.30f, 1.30f, 1.30f, 1.0f);
            cb.pressedColor = new Color(0.80f, 1.15f, 1.40f, 1.0f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;

            btn.onClick.AddListener(() => onClick?.Invoke());
            btnObj.AddComponent<CyberButtonEffect>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            // Left Edge Tactical Neon Accent Pip
            GameObject pipObj = new GameObject("AccentPip");
            pipObj.transform.SetParent(btnObj.transform, false);
            Image pipImg = pipObj.AddComponent<Image>();
            pipImg.color = ProceduralUISpriteGenerator.GetBorderColor(style);
            pipImg.raycastTarget = false;
            RectTransform pipRt = pipObj.GetComponent<RectTransform>();
            pipRt.anchorMin = new Vector2(0f, 0.5f);
            pipRt.anchorMax = new Vector2(0f, 0.5f);
            pipRt.anchoredPosition = new Vector2(9f, 0f);
            pipRt.sizeDelta = new Vector2(3.5f, size.y - 18f);

            // Button Title & Icon Text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(btnObj.transform, false);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = string.IsNullOrEmpty(icon) ? title : $"{icon} {title}";
            titleTxt.font = font;
            titleTxt.fontSize = (size.y > 45f) ? 13 : 11;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = string.IsNullOrEmpty(subtitle) ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            titleTxt.color = Color.white;
            titleTxt.raycastTarget = false;

            Outline titleOutline = titleObj.AddComponent<Outline>();
            titleOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            titleOutline.effectDistance = new Vector2(1.2f, -1.2f);

            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            if (string.IsNullOrEmpty(subtitle))
            {
                titleRt.anchorMin = Vector2.zero;
                titleRt.anchorMax = Vector2.one;
                titleRt.offsetMin = new Vector2(16f, 0f);
                titleRt.offsetMax = new Vector2(-16f, 0f);
            }
            else
            {
                titleRt.anchorMin = new Vector2(0f, 0.5f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.offsetMin = new Vector2(20f, 0f);
                titleRt.offsetMax = new Vector2(-12f, -4f);
            }

            // Subtitle Text (High-tech military avionics styling)
            if (!string.IsNullOrEmpty(subtitle))
            {
                GameObject subObj = new GameObject("SubtitleText");
                subObj.transform.SetParent(btnObj.transform, false);
                Text subTxt = subObj.AddComponent<Text>();
                subTxt.text = subtitle;
                subTxt.font = font;
                subTxt.fontSize = 9;
                subTxt.alignment = TextAnchor.MiddleLeft;
                subTxt.color = new Color(0.70f, 0.88f, 1.0f, 0.80f);
                subTxt.raycastTarget = false;

                Outline subOutline = subObj.AddComponent<Outline>();
                subOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                subOutline.effectDistance = new Vector2(1f, -1f);

                RectTransform subRt = subObj.GetComponent<RectTransform>();
                subRt.anchorMin = new Vector2(0f, 0f);
                subRt.anchorMax = new Vector2(1f, 0.5f);
                subRt.offsetMin = new Vector2(20f, 4f);
                subRt.offsetMax = new Vector2(-12f, 0f);
            }

            return btnObj;
        }

        // Backward compatibility overload
        private GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color btnColor, Action onClick, Font font)
        {
            CyberButtonColor style = CyberButtonColor.Cyan;
            if (btnColor.r > 0.6f && btnColor.g < 0.3f) style = CyberButtonColor.Crimson;
            else if (btnColor.r > 0.6f && btnColor.g > 0.4f) style = CyberButtonColor.Amber;
            else if (btnColor.g > 0.5f && btnColor.r < 0.3f) style = CyberButtonColor.Emerald;
            else if (btnColor.b > 0.6f && btnColor.r > 0.3f) style = CyberButtonColor.Violet;
            else if (btnColor.r < 0.4f && btnColor.g < 0.4f && btnColor.b < 0.4f) style = CyberButtonColor.Gunmetal;

            return CreateCyberGameButton(parent, name, "", label, "", pos, size, style, onClick, font);
        }
    }
}
