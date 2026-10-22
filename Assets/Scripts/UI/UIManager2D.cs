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
                    // Royal Antique Gold
                    borderColor = new Color(0.725f, 0.604f, 0.380f, 1.0f); // #B99A61 Antique Gold
                    fillColor = new Color(0.090f, 0.137f, 0.176f, 0.95f);   // #17232D Midnight Slate
                    glowColor = new Color(0.92f, 0.80f, 0.45f, 0.40f);      // Radiant Gold glow
                    break;
                case CyberButtonColor.Crimson:
                    // Blood Red / Syndicate / Combat Alert
                    borderColor = new Color(0.490f, 0.224f, 0.259f, 1.0f); // #7D3942 Blood Red
                    fillColor = new Color(0.18f, 0.08f, 0.10f, 0.95f);      // Deep dark burgundy
                    glowColor = new Color(0.85f, 0.25f, 0.32f, 0.40f);      // Blood Red glow
                    break;
                case CyberButtonColor.Amber:
                    // Burnished Amber / Warning
                    borderColor = new Color(0.78f, 0.51f, 0.16f, 1.0f);    // Amber Bronze
                    fillColor = new Color(0.20f, 0.13f, 0.04f, 0.95f);      // Dark bronze fill
                    glowColor = new Color(0.95f, 0.68f, 0.20f, 0.40f);
                    break;
                case CyberButtonColor.Emerald:
                    // Magic Emerald Green / AR
                    borderColor = new Color(0.322f, 0.427f, 0.341f, 1.0f); // #526D57 Magic Green
                    fillColor = new Color(0.06f, 0.12f, 0.08f, 0.95f);      // Deep forest slate fill
                    glowColor = new Color(0.45f, 0.85f, 0.55f, 0.40f);
                    break;
                case CyberButtonColor.Violet:
                    // Arcane Amethyst
                    borderColor = new Color(0.55f, 0.35f, 0.70f, 1.0f);
                    fillColor = new Color(0.14f, 0.08f, 0.18f, 0.95f);
                    glowColor = new Color(0.70f, 0.45f, 0.88f, 0.35f);
                    break;
                case CyberButtonColor.Sapphire:
                    // Knight Sapphire / PVP
                    borderColor = new Color(0.25f, 0.45f, 0.75f, 1.0f);
                    fillColor = new Color(0.07f, 0.12f, 0.22f, 0.95f);
                    glowColor = new Color(0.35f, 0.65f, 0.95f, 0.35f);
                    break;
                case CyberButtonColor.Gunmetal:
                    // Carved Granite Slate
                    borderColor = new Color(0.35f, 0.40f, 0.46f, 1.0f);
                    fillColor = new Color(0.10f, 0.13f, 0.16f, 0.95f);
                    glowColor = new Color(0.55f, 0.65f, 0.75f, 0.25f);
                    break;
                case CyberButtonColor.PanelFrame:
                    // Carved Stone Slate & Antique Bronze
                    borderColor = new Color(0.47f, 0.35f, 0.27f, 0.85f);   // #47352B Antique Brown
                    fillColor = new Color(0.090f, 0.137f, 0.176f, 0.92f);  // #17232D Midnight Slate
                    glowColor = new Color(0.725f, 0.604f, 0.380f, 0.25f);  // Antique Gold hint
                    break;
                case CyberButtonColor.ModalFrame:
                    // Heavy Castle Stone & Gilded Antique Gold
                    borderColor = new Color(0.725f, 0.604f, 0.380f, 0.95f); // #B99A61 Antique Gold
                    fillColor = new Color(0.065f, 0.095f, 0.125f, 0.98f);  // Midnight Slate
                    glowColor = new Color(0.92f, 0.80f, 0.45f, 0.30f);      // Gold highlight
                    break;
                default:
                    borderColor = new Color(0.725f, 0.604f, 0.380f, 1.0f);
                    fillColor = new Color(0.090f, 0.137f, 0.176f, 0.90f);
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
                case CyberButtonColor.Cyan: return new Color(0.725f, 0.604f, 0.380f, 1.0f); // Antique Gold
                case CyberButtonColor.Crimson: return new Color(0.490f, 0.224f, 0.259f, 1.0f); // Blood Red
                case CyberButtonColor.Amber: return new Color(0.78f, 0.51f, 0.16f, 1.0f);   // Amber Bronze
                case CyberButtonColor.Emerald: return new Color(0.322f, 0.427f, 0.341f, 1.0f); // Magic Green
                case CyberButtonColor.Violet: return new Color(0.55f, 0.35f, 0.70f, 1.0f);
                case CyberButtonColor.Sapphire: return new Color(0.25f, 0.45f, 0.75f, 1.0f);
                case CyberButtonColor.Gunmetal: return new Color(0.35f, 0.40f, 0.46f, 1.0f);
                case CyberButtonColor.PanelFrame: return new Color(0.47f, 0.35f, 0.27f, 0.85f);
                case CyberButtonColor.ModalFrame: return new Color(0.725f, 0.604f, 0.380f, 0.95f);
                default: return new Color(0.725f, 0.604f, 0.380f, 1.0f);
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

        public static void PlayCustomTone(float startFreq, float endFreq, float duration, float volume)
        {
            InitAudio();
            if (_audioSource != null)
            {
                try
                {
                    AudioClip clip = CreateTone(startFreq, endFreq, duration, volume);
                    _audioSource.PlayOneShot(clip);
                }
                catch { }
            }
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
            UpdateTimers(_whiteTimer, _blackTimer);
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

            UpdateTimers(_whiteTimer, _blackTimer);
            RecordMove(AegisLocalization.T(
                $"[ĐỒNG HỒ] Thể thức: {timeControlStr} ({FormatTime(totalSec)} + {incSec}s/nước)",
                $"[CLOCK] Format: {timeControlStr} ({FormatTime(totalSec)} + {incSec}s/move)"
            ));
        }

        private void UpdateTimers(float whiteSec, float blackSec)
        {
            string wStr = FormatTime(whiteSec);
            string bStr = FormatTime(blackSec);
            if (_whiteTimerText != null) _whiteTimerText.text = wStr;
            if (_blackTimerText != null) _blackTimerText.text = bStr;
            if (_arWhiteTimerText != null) _arWhiteTimerText.text = wStr;
            if (_arBlackTimerText != null) _arBlackTimerText.text = bStr;
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
        private bool _isInCheck = false;

        public static void PlayCheckAlertTone()
        {
            CyberButtonEffect.PlayCustomTone(880f, 540f, 0.20f, 0.40f);
        }

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
        private GameObject _standardHudRoot;
        private Text _arWhiteTimerText;
        private Text _arBlackTimerText;
        public bool isAROverlayActive { get; private set; } = false;

        public void SetARHUDVisible(bool visible)
        {
            isAROverlayActive = visible;
            if (_standardHudRoot != null) _standardHudRoot.SetActive(!visible);
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
            else { Destroy(this); return; }

            AegisLocalization.OnLanguageChanged += RefreshLanguageHUD;

            BuildHUD();
            SetHUDVisible(false);
        }

        private void OnDestroy()
        {
            AegisLocalization.OnLanguageChanged -= RefreshLanguageHUD;
        }

        private void RefreshLanguageHUD()
        {
            if (_hudCanvasObj == null) return;
            bool wasDrawerOpen = _isTacticalDrawerOpen;
            bool wasARActive = isAROverlayActive;
            bool wasHudActive = _hudCanvasObj.activeSelf;
            bool wasMultiplayerModalActive = _multiplayerModal != null && _multiplayerModal.activeSelf;
            bool wasPromoModalActive = _promotionModal != null && _promotionModal.activeSelf;

            Destroy(_hudCanvasObj);
            BuildHUD();

            SetHUDVisible(wasHudActive);
            SetARHUDVisible(wasARActive);
            SetTacticalDrawerOpen(wasDrawerOpen);
            if (_multiplayerModal != null) _multiplayerModal.SetActive(wasMultiplayerModalActive);
            if (_promotionModal != null) _promotionModal.SetActive(wasPromoModalActive);

            UpdateTimers(_whiteTimer, _blackTimer);
            SetActiveTurn(_activeColor);
            if (_moveHistoryText != null && _moveHistory != null && _moveHistory.Count > 0)
            {
                _moveHistoryText.text = string.Join("\n", _moveHistory);
            }
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
            // Container for Screen 8 Standard 3D HUD Elements (Hidden in AR Screen 9)
            // =========================================================================
            _standardHudRoot = new GameObject("StandardHUD_Root");
            _standardHudRoot.transform.SetParent(canvasObj.transform, false);
            RectTransform shrRt = _standardHudRoot.AddComponent<RectTransform>();
            shrRt.anchorMin = Vector2.zero;
            shrRt.anchorMax = Vector2.one;
            shrRt.offsetMin = Vector2.zero;
            shrRt.offsetMax = Vector2.zero;

            // =========================================================================
            // 1. TOP HEADER: Floating Hex-Pill Clocks & Profiles (Matching Image 2 Screen 8)
            // =========================================================================

            // --- A. Left Wing: Player Profile ---
            GameObject playerProfile = new GameObject("PlayerProfile");
            playerProfile.transform.SetParent(_standardHudRoot.transform, false);
            RectTransform ppRt = playerProfile.AddComponent<RectTransform>();
            ppRt.anchorMin = new Vector2(0f, 1f);
            ppRt.anchorMax = new Vector2(0f, 1f);
            ppRt.pivot = new Vector2(0f, 1f);
            ppRt.anchoredPosition = new Vector2(30f, -22f);
            ppRt.sizeDelta = new Vector2(240f, 64f);

            // Circular Avatar Frame with Gilded Antique Gold Rim
            GameObject pAvFrame = new GameObject("PlayerAvatarFrame");
            pAvFrame.transform.SetParent(playerProfile.transform, false);
            Image pAvFrameImg = pAvFrame.AddComponent<Image>();
            pAvFrameImg.sprite = ArcaneAssetLoader.GetCircleSprite(60, new Color(0.090f, 0.137f, 0.176f, 0.95f), new Color(0.725f, 0.604f, 0.380f), 2.5f);
            RectTransform pafRt = pAvFrame.GetComponent<RectTransform>();
            pafRt.anchorMin = new Vector2(0f, 0.5f);
            pafRt.anchorMax = new Vector2(0f, 0.5f);
            pafRt.pivot = new Vector2(0f, 0.5f);
            pafRt.anchoredPosition = new Vector2(0f, 0f);
            pafRt.sizeDelta = new Vector2(58f, 58f);

            // Inner Avatar Image
            GameObject avObj = new GameObject("AvatarImg");
            avObj.transform.SetParent(pAvFrame.transform, false);
            Image avImg = avObj.AddComponent<Image>();
            Sprite avSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png");
            if (avSprite == null) avSprite = ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            if (avSprite != null) avImg.sprite = avSprite;
            else avImg.color = new Color(0.725f, 0.604f, 0.380f);
            RectTransform avRt = avObj.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0.5f, 0.5f);
            avRt.anchorMax = new Vector2(0.5f, 0.5f);
            avRt.pivot = new Vector2(0.5f, 0.5f);
            avRt.anchoredPosition = Vector2.zero;
            avRt.sizeDelta = new Vector2(50f, 50f);

            // Player Name & Level
            string pName = "Player001";
            string pLevel = "Lv. 5";
            if (MechChess.Multiplayer.UserAccountManager.Instance != null && !string.IsNullOrEmpty(MechChess.Multiplayer.UserAccountManager.Instance.CurrentUsername))
            {
                pName = MechChess.Multiplayer.UserAccountManager.Instance.CurrentUsername;
            }

            Text pnText = CreateText(playerProfile.transform, "PlayerName", pName, 24, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(200f, 30f), new Color(0.96f, 0.94f, 0.88f), defaultFont, true);
            RectTransform pntRt = pnText.rectTransform;
            pntRt.anchorMin = new Vector2(0f, 0.5f);
            pntRt.anchorMax = new Vector2(0f, 0.5f);
            pntRt.pivot = new Vector2(0f, 0.5f);
            pntRt.anchoredPosition = new Vector2(68f, 13f);

            Text plText = CreateText(playerProfile.transform, "PlayerLevel", pLevel, 17, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(200f, 22f), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);
            RectTransform pltRt = plText.rectTransform;
            pltRt.anchorMin = new Vector2(0f, 0.5f);
            pltRt.anchorMax = new Vector2(0f, 0.5f);
            pltRt.pivot = new Vector2(0f, 0.5f);
            pltRt.anchoredPosition = new Vector2(68f, -11f);

            // Sleek Antique Gold Accent Bar under Level
            CreateCyberUnderline(playerProfile.transform, "PlayerUnderline", new Vector2(68f, -25f), new Vector2(65f, 3.5f), new Color(0.725f, 0.604f, 0.380f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

            // --- B. Center Wing: Match Clocks & VS ---
            GameObject centerGroup = new GameObject("CenterMatchClocks");
            centerGroup.transform.SetParent(_standardHudRoot.transform, false);
            RectTransform cgRt = centerGroup.AddComponent<RectTransform>();
            cgRt.anchorMin = new Vector2(0.5f, 1f);
            cgRt.anchorMax = new Vector2(0.5f, 1f);
            cgRt.pivot = new Vector2(0.5f, 1f);
            cgRt.anchoredPosition = new Vector2(0f, -22f);
            cgRt.sizeDelta = new Vector2(380f, 54f);

            // 1. White Player Timer Pill (Pointing Left `〔`)
            GameObject whiteTimerCapsule = CreatePanel(centerGroup.transform, "WhiteTimerCapsule", new Vector2(-96f, 0f), new Vector2(144f, 46f), CyberButtonColor.Cyan);
            CreateText(whiteTimerCapsule.transform, "ChevronLeft", "〔", 22, TextAnchor.MiddleCenter, new Vector2(-52f, 0f), new Vector2(30f, 36f), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);
            _whiteTimerText = CreateText(whiteTimerCapsule.transform, "WhiteTimer", "09:45", 28, TextAnchor.MiddleCenter, new Vector2(10f, 0f), new Vector2(120f, 40f), Color.white, defaultFont, true);

            // 2. Center VS Badge
            CreateText(centerGroup.transform, "VS_Badge", "VS", 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(48f, 36f), new Color(0.85f, 0.75f, 0.55f), defaultFont, true);

            // 3. Black AI Timer Pill (Pointing Right `〕`)
            GameObject blackTimerCapsule = CreatePanel(centerGroup.transform, "BlackTimerCapsule", new Vector2(96f, 0f), new Vector2(144f, 46f), CyberButtonColor.Crimson);
            CreateText(blackTimerCapsule.transform, "RedIcon", "⚔️", 20, TextAnchor.MiddleCenter, new Vector2(-50f, 0f), new Vector2(30f, 36f), new Color(0.95f, 0.45f, 0.45f), defaultFont, false);
            _blackTimerText = CreateText(blackTimerCapsule.transform, "BlackTimer", "10:00", 28, TextAnchor.MiddleCenter, new Vector2(4f, 0f), new Vector2(120f, 40f), Color.white, defaultFont, true);
            CreateText(blackTimerCapsule.transform, "ChevronRight", "〕", 22, TextAnchor.MiddleCenter, new Vector2(52f, 0f), new Vector2(30f, 36f), new Color(0.95f, 0.45f, 0.45f), defaultFont, true);

            // Subtle Status Ticker under clocks
            _turnIndicatorText = CreateText(centerGroup.transform, "TurnIndicator", AegisLocalization.T("⚔️ LƯỢT: HOÀNG GIA (TRẮNG) ⚔️", "⚔️ TURN: ROYAL ALLIANCE (WHITE) ⚔️"), 18, TextAnchor.MiddleCenter, new Vector2(0f, -34f), new Vector2(420f, 26f), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);
            _firstMoveWarningText = CreateText(centerGroup.transform, "FirstMoveWarning", AegisLocalization.T("⏱️ LƯỢT ĐẦU: 60s", "⏱️ 1ST MOVE: 60s"), 15, TextAnchor.MiddleCenter, new Vector2(0f, -56f), new Vector2(420f, 22f), new Color(0.85f, 0.70f, 0.35f), defaultFont, true);

            // --- C. Right Wing: AI Profile ---
            GameObject aiProfile = new GameObject("AIProfile");
            aiProfile.transform.SetParent(_standardHudRoot.transform, false);
            RectTransform aipRt = aiProfile.AddComponent<RectTransform>();
            aipRt.anchorMin = new Vector2(1f, 1f);
            aipRt.anchorMax = new Vector2(1f, 1f);
            aipRt.pivot = new Vector2(1f, 1f);
            aipRt.anchoredPosition = new Vector2(-30f, -22f);
            aipRt.sizeDelta = new Vector2(240f, 64f);

            // AI Avatar Frame with Blood Red Glow
            GameObject aiAvFrame = new GameObject("AIAvatarFrame");
            aiAvFrame.transform.SetParent(aiProfile.transform, false);
            Image aiAvFrameImg = aiAvFrame.AddComponent<Image>();
            aiAvFrameImg.sprite = ArcaneAssetLoader.GetCircleSprite(60, new Color(0.18f, 0.08f, 0.10f, 0.95f), new Color(0.490f, 0.224f, 0.259f), 2.5f);
            RectTransform aiafRt = aiAvFrame.GetComponent<RectTransform>();
            aiafRt.anchorMin = new Vector2(1f, 0.5f);
            aiafRt.anchorMax = new Vector2(1f, 0.5f);
            aiafRt.pivot = new Vector2(1f, 0.5f);
            aiafRt.anchoredPosition = new Vector2(0f, 0f);
            aiafRt.sizeDelta = new Vector2(58f, 58f);

            // Inner AI Avatar Image
            GameObject aiAvObj = new GameObject("AIAvatarImg");
            aiAvObj.transform.SetParent(aiAvFrame.transform, false);
            Image aiAvImg = aiAvObj.AddComponent<Image>();
            Sprite aiAvSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_vs_ai_round.png");
            if (aiAvSprite == null) aiAvSprite = ArcaneAssetLoader.LoadArcaneSprite("ai_portrait_full.png");
            if (aiAvSprite != null)
            {
                aiAvImg.sprite = aiAvSprite;
            }
            else
            {
                CreateText(aiAvObj.transform, "Icon", "AI", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(44f, 44f), Color.white, defaultFont, true);
            }
            RectTransform aiavRt = aiAvObj.GetComponent<RectTransform>();
            aiavRt.anchorMin = new Vector2(0.5f, 0.5f);
            aiavRt.anchorMax = new Vector2(0.5f, 0.5f);
            aiavRt.pivot = new Vector2(0.5f, 0.5f);
            aiavRt.anchoredPosition = Vector2.zero;
            aiavRt.sizeDelta = new Vector2(50f, 50f);

            // AI Name & Level (Right-aligned next to avatar)
            Text aiNameText = CreateText(aiProfile.transform, "AIName", "AI", 24, TextAnchor.MiddleRight, Vector2.zero, new Vector2(200f, 30f), new Color(0.96f, 0.94f, 0.88f), defaultFont, true);
            RectTransform ainRt = aiNameText.rectTransform;
            ainRt.anchorMin = new Vector2(1f, 0.5f);
            ainRt.anchorMax = new Vector2(1f, 0.5f);
            ainRt.pivot = new Vector2(1f, 0.5f);
            ainRt.anchoredPosition = new Vector2(-68f, 13f);

            Text aiLvlText = CreateText(aiProfile.transform, "AILevel", "Lv. 5", 17, TextAnchor.MiddleRight, Vector2.zero, new Vector2(200f, 22f), new Color(0.95f, 0.45f, 0.55f), defaultFont, true);
            RectTransform ailRt = aiLvlText.rectTransform;
            ailRt.anchorMin = new Vector2(1f, 0.5f);
            ailRt.anchorMax = new Vector2(1f, 0.5f);
            ailRt.pivot = new Vector2(1f, 0.5f);
            ailRt.anchoredPosition = new Vector2(-68f, -11f);

            // Sleek Blood Red Accent Bar under Level
            CreateCyberUnderline(aiProfile.transform, "AIUnderline", new Vector2(-68f, -25f), new Vector2(65f, 3.5f), new Color(0.490f, 0.224f, 0.259f, 0.95f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));

            // =========================================================================
            // 2. LEFT QUICK ACTION TOOLBAR (Matching Image 2 Screen 8)
            // =========================================================================
            GameObject leftToolbar = new GameObject("LeftQuickToolbar");
            leftToolbar.transform.SetParent(_standardHudRoot.transform, false);
            RectTransform ltRt = leftToolbar.AddComponent<RectTransform>();
            ltRt.anchorMin = new Vector2(0f, 0.5f);
            ltRt.anchorMax = new Vector2(0f, 0.5f);
            ltRt.pivot = new Vector2(0f, 0.5f);
            ltRt.anchoredPosition = new Vector2(25f, 30f);
            ltRt.sizeDelta = new Vector2(76f, 390f);

            // 1. Tạm dừng
            CreateArcaneToolbarButton(leftToolbar.transform, "BtnPause", "❚❚", AegisLocalization.T("Tạm dừng", "Pause"), new Vector2(0f, 150f), () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.ShowPauseModal();
                else ToggleTacticalDrawer();
            }, defaultFont);

            // 2. Gợi ý
            CreateArcaneToolbarButton(leftToolbar.transform, "BtnHint", "🛡️", AegisLocalization.T("Gợi ý", "Hint"), new Vector2(0f, 75f), () =>
            {
                OnTriggerAIClicked?.Invoke();
            }, defaultFont);

            // 3. Góc nhìn Camera (White 3D -> Black 3D -> 2D Top-Down -> Isometric Hero)
            CreateArcaneToolbarButton(leftToolbar.transform, "BtnCamera", "🎥", AegisLocalization.T("Góc Cam", "Camera"), new Vector2(0f, 0f), () =>
            {
                if (MechChess.Mobile.MobileTouchInput.Instance != null)
                {
                    string modeName = MechChess.Mobile.MobileTouchInput.Instance.CycleCameraPerspective();
                    RecordMove($"[CAMERA] {modeName}");
                }
            }, defaultFont);

            // 4. Hủy nước đi
            CreateArcaneToolbarButton(leftToolbar.transform, "BtnUndo", "↺", AegisLocalization.T("Hủy nước đi", "Undo"), new Vector2(0f, -75f), () =>
            {
                if (isRankedMode)
                {
                    RecordMove(AegisLocalization.T("[TỪ CHỐI] Không thể hoàn tác trong chế độ Đấu Xếp Hạng!", "[DECLINED] Cannot undo in Ranked Mode!"));
                    return;
                }
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.UndoMove();
            }, defaultFont);

            // 5. Thiết lập
            CreateArcaneToolbarButton(leftToolbar.transform, "BtnSettings", "⚙", AegisLocalization.T("Thiết lập", "Settings"), new Vector2(0f, -150f), () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.OpenSettingsModal();
            }, defaultFont);

            // Subtle chevron (>>) next to toolbar
            GameObject chevObj = new GameObject("ToolbarChevron");
            chevObj.transform.SetParent(leftToolbar.transform, false);
            RectTransform chevRt = chevObj.AddComponent<RectTransform>();
            chevRt.anchoredPosition = new Vector2(50f, -150f);
            chevRt.sizeDelta = new Vector2(26f, 36f);
            Button chevBtn = chevObj.AddComponent<Button>();
            chevBtn.onClick.AddListener(ToggleTacticalDrawer);
            CreateText(chevObj.transform, "Icon", ">>", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(26f, 36f), new Color(0.0f, 0.85f, 1.0f, 0.80f), defaultFont, true);

            // =========================================================================
            // 2. IN-GAME COLLAPSIBLE SIDEBAR DRAWER (Tucked away, slides out on demand)
            // =========================================================================
            // Dark Backdrop (tapping outside closes drawer)
            GameObject backdrop = new GameObject("TacticalDrawerBackdrop");
            backdrop.transform.SetParent(_standardHudRoot.transform, false);
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
            GameObject drawer = CreatePanel(_standardHudRoot.transform, "TacticalDrawer", Vector2.zero, Vector2.zero, CyberButtonColor.ModalFrame);
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

            CreateText(drawerHeader.transform, "DrawerTitle", AegisLocalization.T("⚔️ SỔ TAY CHIẾN THUẬT & NHẬT KÝ ⚔️", "⚔️ TACTICAL SCROLL & CHRONICLES ⚔️"), 18, TextAnchor.MiddleLeft, new Vector2(-35f, 0f), new Vector2(320f, 36f), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);
            CreateCyberGameButton(drawerHeader.transform, "BtnCloseDrawer", "✕", AegisLocalization.T("ĐÓNG", "CLOSE"), "", new Vector2(170f, 0f), new Vector2(74f, 32f), CyberButtonColor.Crimson, () => SetTacticalDrawerOpen(false), defaultFont);

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
            CreateText(contentObj.transform, "Sec1Title", AegisLocalization.T("── LỆNH CHIẾN TRƯỜNG ──", "── BATTLEFIELD COMMANDS ──"), 16, TextAnchor.MiddleCenter, new Vector2(0, -15), new Vector2(420, 22), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);

            CreateCyberGameButton(contentObj.transform, "DrawerUndo", "↩️", AegisLocalization.T("HOÀN TÁC", "UNDO"), "", new Vector2(-142, -54), new Vector2(136, 44), CyberButtonColor.Cyan, () =>
            {
                if (isRankedMode)
                {
                    RecordMove(AegisLocalization.T("[TỪ CHỐI] Không thể hoàn tác trong chế độ Đấu Xếp Hạng (Ranked)!", "[DECLINED] Cannot undo moves in Ranked Mode!"));
                    return;
                }
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.UndoMove();
            }, defaultFont);

            CreateCyberGameButton(contentObj.transform, "DrawerDraw", "🤝", AegisLocalization.T("XIN HÒA", "OFFER DRAW"), "", new Vector2(0, -54), new Vector2(136, 44), CyberButtonColor.Amber, () =>
            {
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.OfferDraw();
            }, defaultFont);

            CreateCyberGameButton(contentObj.transform, "DrawerResign", "🏳️", AegisLocalization.T("ĐẦU HÀNG", "RESIGN"), "", new Vector2(142, -54), new Vector2(136, 44), CyberButtonColor.Crimson, () =>
            {
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.Resign(_activeColor);
            }, defaultFont);

            // --- Section 2: Tactical Commands & Modes (Công cụ tác chiến) ---
            CreatePanel(contentObj.transform, "Sec2Header", new Vector2(0, -96), new Vector2(426, 24), CyberButtonColor.PanelFrame);
            CreateText(contentObj.transform, "Sec2Title", AegisLocalization.T("── PHÉP THUẬT & PHƯƠNG THỨC ──", "── ARCANUM & MODES ──"), 16, TextAnchor.MiddleCenter, new Vector2(0, -96), new Vector2(420, 22), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);

            // Row 1: AI CV Card Scan & AI Auto Strike
            CreateCyberGameButton(contentObj.transform, "BtnScanCard", "📷", AegisLocalization.T("QUÉT THẺ", "SCAN CARD"), AegisLocalization.T("NHẬN DIỆN THẺ AI", "AI CARD VISION"), new Vector2(-108, -138), new Vector2(210, 46), CyberButtonColor.Cyan, () =>
            {
                SetTacticalDrawerOpen(false);
                OnScanCardClicked?.Invoke();
            }, defaultFont);

            CreateCyberGameButton(contentObj.transform, "BtnAIMove", "🤖", AegisLocalization.T("ĐÒN AI", "TRIGGER AI"), AegisLocalization.T("ĐÒN ĐÁNH STOCKFISH", "STOCKFISH STRIKE"), new Vector2(108, -138), new Vector2(210, 46), CyberButtonColor.Crimson, () =>
            {
                OnTriggerAIClicked?.Invoke();
            }, defaultFont);

            // Row 2: Online 2P Lobby & AR Matrix View
            CreateCyberGameButton(contentObj.transform, "BtnOnlineLobby", "🌐", AegisLocalization.T("ONLINE 2P", "ONLINE 2P"), AegisLocalization.T("SẢNH ĐẤU WEBSOCKET", "WEBSOCKET ROOM HUB"), new Vector2(-108, -190), new Vector2(210, 46), CyberButtonColor.Sapphire, () =>
            {
                SetTacticalDrawerOpen(false);
                if (_multiplayerModal != null) _multiplayerModal.SetActive(true);
            }, defaultFont);

            CreateCyberGameButton(contentObj.transform, "BtnToggleAR", "📹", AegisLocalization.T("AR WEBCAM", "AR WEBCAM"), AegisLocalization.T("CHIẾN TRƯỜNG AR", "AR BATTLEFIELD"), new Vector2(108, -190), new Vector2(210, 46), CyberButtonColor.Emerald, () =>
            {
                OnToggleARClicked?.Invoke();
            }, defaultFont);

            // Row 3: 2D/3D Perspective Toggle & 2D Combat Minigame Hack
            CreateCyberGameButton(contentObj.transform, "BtnToggle2DView", "📐", AegisLocalization.T("GÓC NHÌN", "VIEW MODE"), AegisLocalization.T("ĐỔI GÓC CAM", "CYCLE CAM VIEW"), new Vector2(-108, -242), new Vector2(210, 46), CyberButtonColor.Violet, () =>
            {
                if (MechChess.Mobile.MobileTouchInput.Instance != null)
                {
                    string modeName = MechChess.Mobile.MobileTouchInput.Instance.CycleCameraPerspective();
                    RecordMove($"[GÓC NHÌN] {modeName}");
                }
                else
                {
                    var touch = Camera.main != null ? Camera.main.GetComponent<MechChess.Mobile.MobileTouchInput>() : null;
                    if (touch != null)
                    {
                        string modeName = touch.CycleCameraPerspective();
                        RecordMove($"[GÓC NHÌN] {modeName}");
                    }
                }
            }, defaultFont);

            CreateCyberGameButton(contentObj.transform, "BtnTest2DCombat", "⚡", AegisLocalization.T("MINIGAME 2D", "2D COMBAT"), AegisLocalization.T("CHIẾN ĐẤU CỔ ĐIỂN", "TACTICAL OVERRIDE"), new Vector2(108, -242), new Vector2(210, 46), CyberButtonColor.Amber, () =>
            {
                SetTacticalDrawerOpen(false);
                if (MechChess.Combat2D.CombatMinigame2D.Instance != null)
                {
                    RecordMove(AegisLocalization.T("[CHIẾN ĐẤU 2D] Bắt đầu minigame đấu trí!", "[2D COMBAT] Manually Initiated Tactical Minigame!"));
                    MechChess.Combat2D.CombatMinigame2D.Instance.StartMinigame((win) =>
                    {
                        RecordMove(win ? AegisLocalization.T("[CHIẾN ĐẤU 2D] Đột kích thành công: Sát thương 100%!", "[2D COMBAT] Strike Succeeded: 100% Lethal Damage!") : AegisLocalization.T("[CHIẾN ĐẤU 2D] Hết thời gian vi mạch: Đòn đánh cơ bản.", "[2D COMBAT] Circuit Timeout: Standard Strike."));
                    });
                }
            }, defaultFont);

            // Row 4: Reset Match & Return to Menu Buttons
            CreateCyberGameButton(contentObj.transform, "BtnReset", "🔄", AegisLocalization.T("LÀM MỚI", "RESET MATCH"), AegisLocalization.T("BẮT ĐẦU LẠI", "PURGE & RESTART"), new Vector2(-108, -292), new Vector2(210, 42), CyberButtonColor.Gunmetal, () =>
            {
                SetTacticalDrawerOpen(false);
                OnResetGameClicked?.Invoke();
            }, defaultFont);

            CreateCyberGameButton(contentObj.transform, "BtnExitToMenu", "🚪", AegisLocalization.T("VỀ MENU", "EXIT TO MENU"), AegisLocalization.T("VỀ MÀN HÌNH CHÍNH", "LEAVE TO MAIN MENU"), new Vector2(108, -292), new Vector2(210, 42), CyberButtonColor.Crimson, () =>
            {
                SetTacticalDrawerOpen(false);
                ReturnToMainMenu();
            }, defaultFont);

            // --- Section 3: Battle Telemetry Log (Nhật ký nước đi PGN) ---
            CreatePanel(contentObj.transform, "Sec3Header", new Vector2(0, -332), new Vector2(426, 24), CyberButtonColor.PanelFrame);
            CreateText(contentObj.transform, "Sec3Title", AegisLocalization.T("── BIÊN NIÊN SỬ NƯỚC ĐI (PGN) ──", "── MOVE CHRONICLES (PGN) ──"), 16, TextAnchor.MiddleCenter, new Vector2(0, -332), new Vector2(420, 22), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);

            CreateText(contentObj.transform, "HistoryColumns", AegisLocalization.T("LƯỢT     HOÀNG GIA (T)       HẮC ÁM (Đ)", "MOVE     ROYAL (W)          SHADOW (B)"), 15, TextAnchor.MiddleLeft, new Vector2(0, -354), new Vector2(415, 20), new Color(0.851f, 0.784f, 0.635f, 0.90f), defaultFont, true);

            // Log Content Box
            GameObject logContainer = CreatePanel(contentObj.transform, "HistoryLogBox", new Vector2(0, -500), new Vector2(426, 260), CyberButtonColor.PanelFrame);
            _moveHistoryText = CreateText(logContainer.transform, "HistoryLog", AegisLocalization.T("Trận đấu bắt đầu.\nĐang chờ nước đi đầu tiên...", "Match started.\nAwaiting initial moves..."), 16, TextAnchor.UpperLeft, new Vector2(0, 0), new Vector2(406, 240), new Color(0.96f, 0.94f, 0.88f), defaultFont, true);

            // Confidence display pod
            GameObject confPod = CreatePanel(contentObj.transform, "ConfidencePod", new Vector2(0, -655), new Vector2(426, 36), CyberButtonColor.Amber);
            _lastConfidenceText = CreateText(confPod.transform, "ConfidenceDisplay", "AI CV MATRIX: STANDBY (N/A)", 15, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(415, 30), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);

            // System Telemetry indicators
            _aiServerStatusText = CreateText(contentObj.transform, "AIStatus", "● AI SERVER: ONLINE", 15, TextAnchor.MiddleLeft, new Vector2(-108, -694), new Vector2(200, 24), new Color(0.38f, 0.85f, 0.45f), defaultFont, true);
            _cloudStatusText = CreateText(contentObj.transform, "CloudStatus", "● AIO CLOUD: LINKED", 15, TextAnchor.MiddleRight, new Vector2(108, -694), new Vector2(200, 24), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);

            // Hide Drawer by default (Clean board view!)
            _tacticalDrawerObj.SetActive(false);
            _drawerBackdropObj.SetActive(false);

            // =========================================================================
            // 4. BOTTOM-CENTER PIECE STATUS DOCK (Matching Image 2 Screen 8)
            // =========================================================================
            GameObject pieceTray = CreatePanel(_standardHudRoot.transform, "PieceTray", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform ptRt = pieceTray.GetComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0.5f, 0f);
            ptRt.anchorMax = new Vector2(0.5f, 0f);
            ptRt.pivot = new Vector2(0.5f, 0f);
            ptRt.anchoredPosition = new Vector2(0f, 16f);
            ptRt.sizeDelta = new Vector2(620f, 68f);

            string[] pieceIcons = { "♟", "♞", "♜", "♝", "♛", "♚" };
            string[] pieceNames = (AegisLocalization.CurrentLanguage == AppLanguage.Vietnamese)
                ? new[] { "Tốt", "Mã", "Tượng", "Xe", "Hậu", "Vua" }
                : new[] { "Pawn", "Knight", "Bishop", "Rook", "Queen", "King" };
            float trayStartX = -225f;
            float trayStepX = 78f;
            for (int i = 0; i < 6; i++)
            {
                GameObject slot = new GameObject($"PieceSlot_{i}");
                slot.transform.SetParent(pieceTray.transform, false);
                RectTransform slotRt = slot.AddComponent<RectTransform>();
                slotRt.anchoredPosition = new Vector2(trayStartX + i * trayStepX, 0f);
                slotRt.sizeDelta = new Vector2(72f, 60f);

                CreateText(slot.transform, "Icon", pieceIcons[i], 30, TextAnchor.MiddleCenter, new Vector2(0f, 10f), new Vector2(68f, 32f), new Color(0.96f, 0.94f, 0.88f), defaultFont, true);
                CreateText(slot.transform, "Name", pieceNames[i], 16, TextAnchor.MiddleCenter, new Vector2(0f, -14f), new Vector2(68f, 20f), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);
            }

            // Circular Gilded Forward Action Button (Matching Image 2 Screen 8)
            GameObject endTurnBtnObj = new GameObject("BtnEndTurn");
            endTurnBtnObj.transform.SetParent(pieceTray.transform, false);
            Image etImg = endTurnBtnObj.AddComponent<Image>();
            etImg.sprite = ArcaneAssetLoader.GetCircleSprite(52, new Color(0.090f, 0.137f, 0.176f, 0.95f), new Color(0.725f, 0.604f, 0.380f), 2.5f);
            Button etBtn = endTurnBtnObj.AddComponent<Button>();
            etBtn.onClick.AddListener(ToggleTacticalDrawer);
            endTurnBtnObj.AddComponent<CyberButtonEffect>();
            RectTransform etRt = endTurnBtnObj.GetComponent<RectTransform>();
            etRt.anchoredPosition = new Vector2(258f, 0f);
            etRt.sizeDelta = new Vector2(52f, 52f);
            CreateText(endTurnBtnObj.transform, "Arrow", "➔", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(50f, 50f), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);

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

        private GameObject CreateARActionTile(Transform parent, string name, string icon, string label, Vector2 tilePos, Action onClick, Font font)
        {
            GameObject tileContainer = new GameObject(name);
            tileContainer.transform.SetParent(parent, false);
            RectTransform tcRt = tileContainer.AddComponent<RectTransform>();
            tcRt.anchoredPosition = tilePos;
            tcRt.sizeDelta = new Vector2(88f, 92f);

            // Carved Stone & Antique Gold Beveled Button Tile
            GameObject btnObj = new GameObject("BtnTile");
            btnObj.transform.SetParent(tileContainer.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(64, 64, 18f, new Color(0.090f, 0.137f, 0.176f, 0.95f), new Color(0.725f, 0.604f, 0.380f, 0.95f), 2.2f);
            btnImg.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            btnObj.AddComponent<CyberButtonEffect>();

            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchoredPosition = new Vector2(0f, 14f);
            btnRt.sizeDelta = new Vector2(64f, 64f);

            CreateText(btnObj.transform, "Icon", icon, 30, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(56f, 56f), new Color(0.96f, 0.94f, 0.88f, 1f), font, false);

            // Warm Ivory Label Underneath
            CreateText(tileContainer.transform, "Label", label, 15, TextAnchor.MiddleCenter, new Vector2(0f, -28f), new Vector2(100f, 22f), new Color(0.96f, 0.94f, 0.88f, 1f), font, true);

            return tileContainer;
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

            // =========================================================================
            // 1. TOP-LEFT: Player Info Stone Capsule with Integrated Gold Timer Sub-Pill
            // =========================================================================
            GameObject playerCapsule = CreatePanel(_arHudOverlayObj.transform, "ARPlayerCapsule", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform pcRt = playerCapsule.GetComponent<RectTransform>();
            pcRt.anchorMin = new Vector2(0f, 1f);
            pcRt.anchorMax = new Vector2(0f, 1f);
            pcRt.pivot = new Vector2(0f, 1f);
            pcRt.anchoredPosition = new Vector2(30f, -22f);
            pcRt.sizeDelta = new Vector2(305f, 62f);

            // Circular Player Avatar with Antique Gold Filigree Ring
            GameObject pAvFrame = new GameObject("ARPlayerAvatarFrame");
            pAvFrame.transform.SetParent(playerCapsule.transform, false);
            Image pAvFrameImg = pAvFrame.AddComponent<Image>();
            pAvFrameImg.sprite = ArcaneAssetLoader.GetCircleSprite(54, new Color(0.090f, 0.137f, 0.176f, 0.95f), new Color(0.725f, 0.604f, 0.380f, 1f), 2.5f);
            RectTransform pafRt = pAvFrame.GetComponent<RectTransform>();
            pafRt.anchorMin = new Vector2(0f, 0.5f);
            pafRt.anchorMax = new Vector2(0f, 0.5f);
            pafRt.pivot = new Vector2(0f, 0.5f);
            pafRt.anchoredPosition = new Vector2(6f, 0f);
            pafRt.sizeDelta = new Vector2(52f, 52f);

            GameObject avObj = new GameObject("AvatarImg");
            avObj.transform.SetParent(pAvFrame.transform, false);
            Image avImg = avObj.AddComponent<Image>();
            Sprite avSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png");
            if (avSprite == null) avSprite = ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            if (avSprite != null) avImg.sprite = avSprite;
            else avImg.color = new Color(0.725f, 0.604f, 0.380f);
            RectTransform avRt = avObj.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0.5f, 0.5f);
            avRt.anchorMax = new Vector2(0.5f, 0.5f);
            avRt.pivot = new Vector2(0.5f, 0.5f);
            avRt.anchoredPosition = Vector2.zero;
            avRt.sizeDelta = new Vector2(46f, 46f);

            // Player Name & Level
            string pName = "Player001";
            string pLevel = "Lv. 5";
            if (MechChess.Multiplayer.UserAccountManager.Instance != null && !string.IsNullOrEmpty(MechChess.Multiplayer.UserAccountManager.Instance.CurrentUsername))
            {
                pName = MechChess.Multiplayer.UserAccountManager.Instance.CurrentUsername;
            }

            Text pnText = CreateText(playerCapsule.transform, "PlayerName", pName, 24, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(140f, 26f), new Color(0.96f, 0.94f, 0.88f), font, true);
            RectTransform pntRt = pnText.rectTransform;
            pntRt.anchorMin = new Vector2(0f, 0.5f);
            pntRt.anchorMax = new Vector2(0f, 0.5f);
            pntRt.pivot = new Vector2(0f, 0.5f);
            pntRt.anchoredPosition = new Vector2(66f, 11f);

            Text plText = CreateText(playerCapsule.transform, "PlayerLevel", pLevel, 17, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(140f, 22f), new Color(0.725f, 0.604f, 0.380f), font, true);
            RectTransform pltRt = plText.rectTransform;
            pltRt.anchorMin = new Vector2(0f, 0.5f);
            pltRt.anchorMax = new Vector2(0f, 0.5f);
            pltRt.pivot = new Vector2(0f, 0.5f);
            pltRt.anchoredPosition = new Vector2(66f, -11f);

            // Embedded Warm Gold Timer Sub-Pill
            GameObject whiteTimerSubPill = CreatePanel(playerCapsule.transform, "WhiteTimerSubPill", Vector2.zero, Vector2.zero, CyberButtonColor.Amber);
            RectTransform wtspRt = whiteTimerSubPill.GetComponent<RectTransform>();
            wtspRt.anchorMin = new Vector2(1f, 0.5f);
            wtspRt.anchorMax = new Vector2(1f, 0.5f);
            wtspRt.pivot = new Vector2(1f, 0.5f);
            wtspRt.anchoredPosition = new Vector2(-8f, 0f);
            wtspRt.sizeDelta = new Vector2(94f, 44f);

            _arWhiteTimerText = CreateText(whiteTimerSubPill.transform, "ARWhiteTimer", FormatTime(_whiteTimer), 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(90f, 36f), new Color(0.96f, 0.94f, 0.88f), font, true);

            // =========================================================================
            // 2. TOP-RIGHT: AI Info Stone Capsule with Integrated Crimson Timer
            // =========================================================================
            GameObject aiCapsule = CreatePanel(_arHudOverlayObj.transform, "AICapsule", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform aicRt = aiCapsule.GetComponent<RectTransform>();
            aicRt.anchorMin = new Vector2(1f, 1f);
            aicRt.anchorMax = new Vector2(1f, 1f);
            aicRt.pivot = new Vector2(1f, 1f);
            aicRt.anchoredPosition = new Vector2(-30f, -22f);
            aicRt.sizeDelta = new Vector2(305f, 62f);

            // AI Name & Level on the left
            Text aiNameText = CreateText(aiCapsule.transform, "AIName", "AI", 24, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(80f, 26f), new Color(0.96f, 0.94f, 0.88f), font, true);
            RectTransform ainRt = aiNameText.rectTransform;
            ainRt.anchorMin = new Vector2(0f, 0.5f);
            ainRt.anchorMax = new Vector2(0f, 0.5f);
            ainRt.pivot = new Vector2(0f, 0.5f);
            ainRt.anchoredPosition = new Vector2(16f, 11f);

            Text aiLvlText = CreateText(aiCapsule.transform, "AILevel", "Lv. 5", 17, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(80f, 22f), new Color(0.77f, 0.35f, 0.40f), font, true);
            RectTransform ailRt = aiLvlText.rectTransform;
            ailRt.anchorMin = new Vector2(0f, 0.5f);
            ailRt.anchorMax = new Vector2(0f, 0.5f);
            ailRt.pivot = new Vector2(0f, 0.5f);
            ailRt.anchoredPosition = new Vector2(16f, -11f);

            // Embedded Crimson Timer Sub-Pill
            GameObject blackTimerSubPill = CreatePanel(aiCapsule.transform, "BlackTimerSubPill", Vector2.zero, Vector2.zero, CyberButtonColor.Crimson);
            RectTransform btspRt = blackTimerSubPill.GetComponent<RectTransform>();
            btspRt.anchorMin = new Vector2(0.5f, 0.5f);
            btspRt.anchorMax = new Vector2(0.5f, 0.5f);
            btspRt.pivot = new Vector2(0.5f, 0.5f);
            btspRt.anchoredPosition = new Vector2(18f, 0f);
            btspRt.sizeDelta = new Vector2(94f, 44f);

            _arBlackTimerText = CreateText(blackTimerSubPill.transform, "ARBlackTimer", FormatTime(_blackTimer), 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(90f, 36f), new Color(0.96f, 0.94f, 0.88f), font, true);

            // Circular AI Avatar with Crimson Beveled Ring on Far Right
            GameObject aiAvFrame = new GameObject("ARAIAvatarFrame");
            aiAvFrame.transform.SetParent(aiCapsule.transform, false);
            Image aiAvFrameImg = aiAvFrame.AddComponent<Image>();
            aiAvFrameImg.sprite = ArcaneAssetLoader.GetCircleSprite(54, new Color(0.28f, 0.08f, 0.10f, 0.95f), new Color(0.490f, 0.224f, 0.259f), 2.5f);
            RectTransform aiafRt = aiAvFrame.GetComponent<RectTransform>();
            aiafRt.anchorMin = new Vector2(1f, 0.5f);
            aiafRt.anchorMax = new Vector2(1f, 0.5f);
            aiafRt.pivot = new Vector2(1f, 0.5f);
            aiafRt.anchoredPosition = new Vector2(-6f, 0f);
            aiafRt.sizeDelta = new Vector2(52f, 52f);

            GameObject aiAvObj = new GameObject("AIAvatarImg");
            aiAvObj.transform.SetParent(aiAvFrame.transform, false);
            Image aiAvImg = aiAvObj.AddComponent<Image>();
            Sprite aiAvSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_vs_ai_round.png");
            if (aiAvSprite == null) aiAvSprite = ArcaneAssetLoader.LoadArcaneSprite("ai_portrait_full.png");
            if (aiAvSprite != null)
            {
                aiAvImg.sprite = aiAvSprite;
            }
            else
            {
                CreateText(aiAvObj.transform, "Icon", "AI", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(44f, 44f), Color.white, font, true);
            }
            RectTransform aiavRt = aiAvObj.GetComponent<RectTransform>();
            aiavRt.anchorMin = new Vector2(0.5f, 0.5f);
            aiavRt.anchorMax = new Vector2(0.5f, 0.5f);
            aiavRt.pivot = new Vector2(0.5f, 0.5f);
            aiavRt.anchoredPosition = Vector2.zero;
            aiavRt.sizeDelta = new Vector2(46f, 46f);

            // =========================================================================
            // 3. RIGHT VERTICAL TOOLBAR: Fantasy Action Buttons
            // =========================================================================
            GameObject rightToolbar = new GameObject("ARRightToolbar");
            rightToolbar.transform.SetParent(_arHudOverlayObj.transform, false);
            RectTransform rtRt = rightToolbar.AddComponent<RectTransform>();
            rtRt.anchorMin = new Vector2(1f, 0.5f);
            rtRt.anchorMax = new Vector2(1f, 0.5f);
            rtRt.pivot = new Vector2(1f, 0.5f);
            rtRt.anchoredPosition = new Vector2(-30f, -10f);
            rtRt.sizeDelta = new Vector2(90f, 320f);

            // 1. Chụp ảnh (Camera)
            CreateARActionTile(rightToolbar.transform, "BtnPhoto", "📷", AegisLocalization.T("Chụp ảnh", "Snapshot"), new Vector2(0f, 105f), () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.ShowToast(AegisLocalization.T("Đã chụp và lưu ảnh AR vào thư viện!", "Saved AR snapshot to gallery!"), ToastType.Success);
            }, font);

            // 2. Quay video (Video camera)
            CreateARActionTile(rightToolbar.transform, "BtnVideo", "🎥", AegisLocalization.T("Quay video", "Record"), new Vector2(0f, 0f), () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.ShowToast(AegisLocalization.T("Đang ghi hình trận đấu AR...", "Recording AR match..."), ToastType.Info);
            }, font);

            // 3. Cài đặt AR (Settings gear)
            CreateARActionTile(rightToolbar.transform, "BtnARSettings", "⚙", AegisLocalization.T("Cài đặt AR", "AR Settings"), new Vector2(0f, -105f), () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.ShowToast(AegisLocalization.T("Đã hiệu chuẩn mặt phẳng AR và camera!", "Calibrated AR plane and camera!"), ToastType.Success);
            }, font);

            // =========================================================================
            // 4. BOTTOM-LEFT: Thoát AR Action Tile
            // =========================================================================
            GameObject exitTileContainer = new GameObject("ARExitContainer");
            exitTileContainer.transform.SetParent(_arHudOverlayObj.transform, false);
            RectTransform etcRt = exitTileContainer.AddComponent<RectTransform>();
            etcRt.anchorMin = new Vector2(0f, 0f);
            etcRt.anchorMax = new Vector2(0f, 0f);
            etcRt.pivot = new Vector2(0f, 0f);
            etcRt.anchoredPosition = new Vector2(30f, 25f);
            etcRt.sizeDelta = new Vector2(90f, 96f);

            CreateARActionTile(exitTileContainer.transform, "BtnExitAR", "⌖", AegisLocalization.T("Thoát AR", "Exit AR"), new Vector2(45f, 48f), () =>
            {
                SetARHUDVisible(false);
                if (MechChess.AR.HybridARController.Instance != null)
                {
                    MechChess.AR.HybridARController.Instance.SetARActive(false);
                }
                if (MainMenu2D.Instance != null)
                {
                    MainMenu2D.Instance.ShowMenu();
                }
            }, font);

            // =========================================================================
            // 5. BOTTOM-CENTER: Floating Stone Guidance Banner
            // =========================================================================
            GameObject bannerObj = CreatePanel(_arHudOverlayObj.transform, "ARGuideBanner", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform bRt = bannerObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 32f);
            bRt.sizeDelta = new Vector2(480f, 50f);

            CreateText(bannerObj.transform, "GuideTxt", AegisLocalization.T("Chạm vào quân cờ để di chuyển", "Tap on a piece to move"), 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(460f, 38f), new Color(0.96f, 0.94f, 0.88f), font, true);

            _arHudOverlayObj.SetActive(false);
        }

        private void BuildPromotionModal(Transform parent, Font font)
        {
            _promotionModal = CreatePanel(parent, "PromotionModal", Vector2.zero, new Vector2(660, 230), CyberButtonColor.ModalFrame);

            CreatePanel(_promotionModal.transform, "PromoHeader", new Vector2(0, 75), new Vector2(620, 36), CyberButtonColor.Amber);
            CreateText(_promotionModal.transform, "PromoTitle", AegisLocalization.T("👑 PHONG CẤP CHIẾN BINH (HEROIC PROMOTION) 👑", "👑 HEROIC PAWN PROMOTION 👑"), 22, TextAnchor.MiddleCenter, new Vector2(0, 75), new Vector2(600, 30), new Color(0.96f, 0.94f, 0.88f), font, true);

            CreateText(_promotionModal.transform, "PromoSub", AegisLocalization.T("Chiến binh đã tiến tới phòng tuyến cuối! Hãy chọn tước vị phong cấp:", "Pawn reached the final rank! Choose a royal reinforcement:"), 17, TextAnchor.MiddleCenter, new Vector2(0, 35), new Vector2(600, 26), new Color(0.851f, 0.784f, 0.635f), font, true);

            // 4 Selection Buttons: Queen, Rook, Bishop, Knight with fantasy titles
            CreateCyberGameButton(_promotionModal.transform, "PromoQueen", "👑", AegisLocalization.T("NỮ HOÀNG (QUEEN)", "QUEEN"), AegisLocalization.T("QUYỀN NĂNG TỐI THƯỢNG", "SUPREME SORCERY"), new Vector2(-230, -32), new Vector2(148, 60), CyberButtonColor.Amber, () => SelectPromotionPiece(PieceType.Queen), font);
            CreateCyberGameButton(_promotionModal.transform, "PromoRook", "🏰", AegisLocalization.T("CHIẾN XE (ROOK)", "ROOK"), AegisLocalization.T("PHÁO ĐÀI CỔ THÀNH", "STONE FORTRESS"), new Vector2(-77, -32), new Vector2(148, 60), CyberButtonColor.Emerald, () => SelectPromotionPiece(PieceType.Rook), font);
            CreateCyberGameButton(_promotionModal.transform, "PromoBishop", "🧙", AegisLocalization.T("GIÁM MỤC (BISHOP)", "BISHOP"), AegisLocalization.T("PHÉP THUẬT HUYỀN BÍ", "ARCANE SORCERY"), new Vector2(77, -32), new Vector2(148, 60), CyberButtonColor.Sapphire, () => SelectPromotionPiece(PieceType.Bishop), font);
            CreateCyberGameButton(_promotionModal.transform, "PromoKnight", "🐴", AegisLocalization.T("KỴ SĨ (KNIGHT)", "KNIGHT"), AegisLocalization.T("KỴ BINH HOÀNG THÁNH", "ROYAL CAVALRY"), new Vector2(230, -32), new Vector2(148, 60), CyberButtonColor.Cyan, () => SelectPromotionPiece(PieceType.Knight), font);

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
            CreatePanel(_multiplayerModal.transform, "ModalHeader", new Vector2(0, 235), new Vector2(760, 46), CyberButtonColor.Amber);
            CreateText(_multiplayerModal.transform, "ModalTitle", AegisLocalization.T("🌐 ĐẤU TRƯỜNG THI ĐẤU TRỰC TUYẾN (GRAND ARENA 2P)", "🌐 GRAND ARENA: 2P ONLINE TOURNAMENT"), 24, TextAnchor.MiddleCenter, new Vector2(0, 235), new Vector2(740, 40), new Color(0.96f, 0.94f, 0.88f), font, true);

            // Subtitle
            CreateText(_multiplayerModal.transform, "ModalSubtitle", AegisLocalization.T("Kết nối đấu trường đa nền tảng PC & AR qua máy chủ thi đấu Docker :8000", "Real-time arena link via Docker backend :8000 | Cross-platform PC & AR"), 16, TextAnchor.MiddleCenter, new Vector2(0, 192), new Vector2(740, 26), new Color(0.851f, 0.784f, 0.635f), font, true);

            // Server Address Box
            GameObject serverBox = CreatePanel(_multiplayerModal.transform, "ServerBox", new Vector2(0, 150), new Vector2(740, 36), CyberButtonColor.PanelFrame);
            CreateText(serverBox.transform, "ServerAddressInfo", AegisLocalization.T("MÁY CHỦ: ws://localhost:8000 (Chơi LAN: cấu hình IP máy chủ trong OnlineMultiplayerManager)", "SERVER: ws://localhost:8000 (LAN: configure host IP in OnlineMultiplayerManager)"), 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(720, 30), new Color(0.725f, 0.604f, 0.380f), font, true);

            // Status Badge
            _multiplayerStatusText = CreateText(_multiplayerModal.transform, "MultiplayerStatus", AegisLocalization.T("Trạng thái: Chưa vào phòng nào", "Status: Not in any room"), 20, TextAnchor.MiddleCenter, new Vector2(0, 105), new Vector2(720, 35), new Color(0.725f, 0.604f, 0.380f), font, true);

            // Role Explanation Pod
            GameObject roleBox = CreatePanel(_multiplayerModal.transform, "RoleBox", new Vector2(0, 48), new Vector2(740, 54), CyberButtonColor.PanelFrame);
            _multiplayerRoleText = CreateText(roleBox.transform, "MultiplayerRole", AegisLocalization.T("👉 Người vào phòng ĐẦU TIÊN: Cầm LIÊN MINH HOÀNG GIA - TRẮNG (Host - Đi trước)\n👉 Người vào phòng THỨ HAI: Cầm ĐẾ CHẾ HẮC ÁM - ĐEN (Guest - Chờ đối thủ)", "👉 First player to join: ROYAL ALLIANCE - WHITE (Host - Plays first)\n👉 Second player to join: DARK SYNDICATE - BLACK (Guest - Awaits opponent)"), 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(720, 48), new Color(0.96f, 0.94f, 0.88f), font, true);

            // Quick Room Selection Header
            CreateText(_multiplayerModal.transform, "RoomSelectLabel", AegisLocalization.T("⚔️ CHỌN ĐẤU TRƯỜNG TRANH TÀI ⚔️", "⚔️ SELECT ARENA COMBAT SECTOR ⚔️"), 18, TextAnchor.MiddleCenter, new Vector2(0, -5), new Vector2(600, 28), new Color(0.725f, 0.604f, 0.380f), font, true);

            // Room Buttons
            CreateCyberGameButton(_multiplayerModal.transform, "BtnRoom101", "⚔️", AegisLocalization.T("VÀO ĐẤU TRƯỜNG 101", "JOIN ARENA 101"), AegisLocalization.T("SẢNH ĐÁ HOÀNG GIA", "ROYAL STONE CITADEL"), new Vector2(-190, -60), new Vector2(360, 54), CyberButtonColor.Cyan, () =>
            {
                JoinOnlineRoom("ROOM-101");
            }, font);

            CreateCyberGameButton(_multiplayerModal.transform, "BtnRoom202", "⚔️", AegisLocalization.T("VÀO ĐẤU TRƯỜNG 202", "JOIN ARENA 202"), AegisLocalization.T("RỪNG THẦN BÍ BẢO", "ELVEN SACRED FOREST"), new Vector2(190, -60), new Vector2(360, 54), CyberButtonColor.Emerald, () =>
            {
                JoinOnlineRoom("ROOM-202");
            }, font);

            CreateCyberGameButton(_multiplayerModal.transform, "BtnRoom303", "⚔️", AegisLocalization.T("VÀO ĐẤU TRƯỜNG 303", "JOIN ARENA 303"), AegisLocalization.T("HẦM MỘ CỔ TÍCH", "ANCIENT CATACOMBS"), new Vector2(-190, -125), new Vector2(360, 54), CyberButtonColor.Sapphire, () =>
            {
                JoinOnlineRoom("ROOM-303");
            }, font);

            CreateCyberGameButton(_multiplayerModal.transform, "BtnDisconnectOnline", "🚪", AegisLocalization.T("RỜI ĐẤU TRƯỜNG", "LEAVE ARENA"), AegisLocalization.T("NGẮT KẾT NỐI SẢNH", "DISCONNECT ARENA"), new Vector2(190, -125), new Vector2(360, 54), CyberButtonColor.Crimson, () =>
            {
                if (MechChess.Multiplayer.OnlineMultiplayerManager.Instance != null)
                {
                    MechChess.Multiplayer.OnlineMultiplayerManager.Instance.Disconnect();
                    UpdateMultiplayerUIStatus(AegisLocalization.T("Đã ngắt kết nối đấu trường", "Disconnected from arena"), Color.white);
                }
            }, font);

            // Close button
            CreateCyberGameButton(_multiplayerModal.transform, "BtnCloseModal", "✖", AegisLocalization.T("ĐÓNG SẢNH ĐẤU", "CLOSE ARENA LOBBY"), AegisLocalization.T("TRỞ VỀ BÀN CỜ", "RETURN TO COMBAT"), new Vector2(0, -200), new Vector2(300, 48), CyberButtonColor.Gunmetal, () =>
            {
                _multiplayerModal.SetActive(false);
            }, font);

            _multiplayerModal.SetActive(false);
        }

        private void JoinOnlineRoom(string roomId)
        {
            if (MechChess.Multiplayer.OnlineMultiplayerManager.Instance != null)
            {
                UpdateMultiplayerUIStatus(AegisLocalization.T($"Đang kết nối vào {roomId}...", $"Connecting to {roomId}..."), Color.yellow);
                MechChess.Multiplayer.OnlineMultiplayerManager.Instance.ConnectAndJoinRoom(roomId);
            }
        }

        public void UpdateMultiplayerUIStatus(string status, Color color)
        {
            if (_multiplayerStatusText != null)
            {
                _multiplayerStatusText.text = AegisLocalization.T($"Trạng thái: {status}", $"Status: {status}");
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
                    _firstMoveWarningText.text = AegisLocalization.T($"⏱️ NƯỚC ĐẦU: {secLeft}s", $"⏱️ 1ST MOVE: {secLeft}s");
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

            UpdateTimers(_whiteTimer, _blackTimer);

            // Live glow pulse on turn indicator (Rapid warning gold flash when in check)
            if (_turnIndicatorText != null && !_isGameOver && !_isMatchAborted)
            {
                if (_isInCheck)
                {
                    float checkGlow = 0.70f + 0.30f * Mathf.Sin(Time.time * 8f);
                    _turnIndicatorText.color = new Color(1.0f * checkGlow, 0.85f * checkGlow, 0.15f * checkGlow);
                }
                else
                {
                    float glow = 0.82f + 0.18f * Mathf.Sin(Time.time * 3f);
                    if (_activeColor == PieceColor.White)
                        _turnIndicatorText.color = new Color(0.0f, 0.95f * glow, 1.0f * glow);
                    else
                        _turnIndicatorText.color = new Color(1.0f * glow, 0.25f, 0.35f);
                }
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
                _turnIndicatorText.text = AegisLocalization.T("⚠️ TRẬN ĐẤU ĐÃ HỦY", "⚠️ MATCH ABORTED");
                _turnIndicatorText.color = Color.red;
            }

            if (_firstMoveWarningText != null)
            {
                _firstMoveWarningText.text = AegisLocalization.T("Quá 60s không đi nước đầu!", "60s 1st move timeout!");
                _firstMoveWarningText.color = Color.red;
            }

            RecordMove("===============================");
            RecordMove(AegisLocalization.T("[HỦY TRẬN] Hết 1 phút nước đi đầu tiên!", "[ABORT] 1-minute 1st move timeout!"));
            RecordMove(AegisLocalization.T("Trận đấu đã bị hủy tự động.", "Match automatically cancelled."));
            RecordMove(AegisLocalization.T("Nhấn [RESET TRẬN ĐẤU] để chơi lại.", "Click [RESET MATCH] to play again."));
            RecordMove("===============================");

            if (MechChess.Cloud.AdafruitIOCloudConnector.Instance != null)
            {
                MechChess.Cloud.AdafruitIOCloudConnector.Instance.PublishTelemetry(1, 0f, "Match_Aborted_Timeout", "ABORTED_1MIN_TIMEOUT");
            }
        }

        public void TriggerGameOver(PieceColor winner, string reason)
        {
            TriggerGameOver(winner, reason, false);
        }

        public void TriggerGameOver(PieceColor winner, string reason, bool isDraw)
        {
            _isGameOver = true;
            _isTimerRunning = false;

            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.isGameOver = true;
            }

            string winnerName = (winner == PieceColor.White) ? AegisLocalization.T("LIÊN MINH", "ALLIANCE") : AegisLocalization.T("HẮC ÁM", "SYNDICATE");
            if (_turnIndicatorText != null)
            {
                if (isDraw)
                {
                    _turnIndicatorText.text = AegisLocalization.T("⚖️ HÒA CỜ!", "⚖️ MATCH DRAW!");
                    _turnIndicatorText.color = new Color(0.85f, 0.85f, 0.95f);
                }
                else
                {
                    _turnIndicatorText.text = AegisLocalization.T($"🏆 {winnerName} CHIẾN THẮNG!", $"🏆 {winnerName} VICTORIOUS!");
                    _turnIndicatorText.color = Color.yellow;
                }
            }

            RecordMove(isDraw 
                ? AegisLocalization.T($"[KẾT THÚC] Hòa cờ! ({reason})", $"[GAME OVER] Draw! ({reason})")
                : AegisLocalization.T($"[KẾT THÚC] {winnerName} chiến thắng! ({reason})", $"[GAME OVER] {winnerName} won! ({reason})"));

            if (MainMenu2D.Instance != null)
            {
                bool isPlayerVictory = (!isDraw && winner == PieceColor.White);
                float matchDuration = _initialTime - _whiteTimer;
                MainMenu2D.Instance.ShowResultScreen(isPlayerVictory, reason, _moveHistory.Count, matchDuration, isDraw);
            }
        }

        private void TriggerTimeoutGameOver(PieceColor timedOutPlayer)
        {
            PieceColor winner = (timedOutPlayer == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            TriggerGameOver(winner, AegisLocalization.T("Hết thời gian trận đấu!", "Out of match time!"));
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
            _isInCheck = false;

            UpdateTimers(_whiteTimer, _blackTimer);

            if (_firstMoveWarningText != null)
            {
                _firstMoveWarningText.text = AegisLocalization.T("⏱️ NƯỚC ĐẦU: 60s", "⏱️ 1ST MOVE: 60s");
                _firstMoveWarningText.color = new Color(1.0f, 0.75f, 0.1f);
            }

            if (_turnIndicatorText != null)
            {
                _turnIndicatorText.text = AegisLocalization.T(">> LƯỢT: LIÊN MINH (TRẮNG) <<", ">> TURN: ALLIANCE (WHITE) <<");
                _turnIndicatorText.color = new Color(0.0f, 0.95f, 1.0f);
            }
        }

        private string FormatTime(float seconds)
        {
            int min = (int)(seconds / 60);
            int sec = (int)(seconds % 60);
            return $"{min:00}:{sec:00}";
        }

        public void SetActiveTurn(PieceColor color, bool isCheck = false)
        {
            _isInCheck = isCheck;

            // F02: Add increment time to the player who just completed their turn
            if (!_isFirstMoveWaiting && !_isGameOver && !_isMatchAborted && _timeIncrement > 0f)
            {
                if (color == PieceColor.Black && _activeColor == PieceColor.White)
                {
                    _whiteTimer += _timeIncrement;
                    UpdateTimers(_whiteTimer, _blackTimer);
                }
                else if (color == PieceColor.White && _activeColor == PieceColor.Black)
                {
                    _blackTimer += _timeIncrement;
                    UpdateTimers(_whiteTimer, _blackTimer);
                }
            }

            _activeColor = color;
            if (_isMatchAborted || _isGameOver) return;

            string teamName = (color == PieceColor.White)
                ? AegisLocalization.T("LIÊN MINH (TRẮNG)", "ALLIANCE (WHITE)")
                : AegisLocalization.T("HẮC ÁM (ĐEN)", "SYNDICATE (BLACK)");

            if (_turnIndicatorText != null)
            {
                if (isCheck)
                {
                    _turnIndicatorText.text = AegisLocalization.T($">> ⚠️ CHIẾU TƯỚNG! LƯỢT: {teamName} <<", $">> ⚠️ CHECK! TURN: {teamName} <<");
                    _turnIndicatorText.color = new Color(1.0f, 0.85f, 0.15f);

                    if (MainMenu2D.Instance != null)
                    {
                        MainMenu2D.Instance.ShowToast(AegisLocalization.T($"⚠️ CHIẾU TƯỚNG! Vua phe {teamName} đang bị đe dọa!", $"⚠️ CHECK! King {teamName} is under attack!"), ToastType.Warning);
                    }

                    PlayCheckAlertTone();
                }
                else
                {
                    _turnIndicatorText.text = AegisLocalization.T($">> LƯỢT: {teamName} <<", $">> TURN: {teamName} <<");
                    _turnIndicatorText.color = (color == PieceColor.White) ? new Color(0.0f, 0.95f, 1.0f) : new Color(1.0f, 0.25f, 0.35f);
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
                    _firstMoveWarningText.text = AegisLocalization.T("✅ TRẬN ĐẤU ĐANG DIỄN RA", "✅ MATCH IN PROGRESS");
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
        private GameObject CreateCyberUnderline(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Vector2 anchor, Vector2 pivot)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.SetParent(parent, false);
            Image img = lineObj.AddComponent<Image>();
            img.sprite = ArcaneAssetLoader.GetSolidSprite(4, 4, color);
            img.color = Color.white;
            img.raycastTarget = false;

            RectTransform rt = lineObj.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return lineObj;
        }

        private GameObject CreateArcaneToolbarButton(Transform parent, string name, string icon, string label, Vector2 pos, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(56f, 56f);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = ProceduralUISpriteGenerator.GetCyberSprite(CyberButtonColor.PanelFrame, 64, 64, 10f, 2f);
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            cb.pressedColor = new Color(0.7f, 1.1f, 1.4f, 1f);
            cb.fadeDuration = 0.06f;
            btn.colors = cb;

            btn.onClick.AddListener(() => onClick?.Invoke());
            btnObj.AddComponent<CyberButtonEffect>();

            // Icon inside the box
            CreateText(btnObj.transform, "Icon", icon, 24, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(50f, 50f), Color.white, font, true);

            // Label text underneath the box
            CreateText(btnObj.transform, "Label", label, 14, TextAnchor.MiddleCenter, new Vector2(0f, -36f), new Vector2(86f, 20f), new Color(0.78f, 0.92f, 1.0f), font, true);

            return btnObj;
        }

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
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
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
            titleTxt.fontSize = (size.y > 45f) ? 16 : 14;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleTxt.verticalOverflow = VerticalWrapMode.Overflow;
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
                subTxt.fontSize = 11;
                subTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                subTxt.verticalOverflow = VerticalWrapMode.Overflow;
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
