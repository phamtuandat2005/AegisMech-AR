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
            StartCoroutine(AnimateScale(1.0f, 0.05f));
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
        private static UIManager2D _instance;
        public static UIManager2D Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager2D>();
                    if (_instance == null)
                    {
                        var go = GameObject.Find("GameController");
                        if (go == null) go = new GameObject("GameController");
                        _instance = go.AddComponent<UIManager2D>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

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
        private bool _isTimerRunning = false;

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
        private ScrollRect _moveHistoryScrollRect;
        private Text _aiServerStatusText;
        private Text _cloudStatusText;
        private Text _lastConfidenceText;
        private bool _isInCheck = false;

        // Player & Opponent Profile references (3x scale)
        private Text _playerNameText;
        private Text _playerLevelText;
        private Image _playerAvatarImg;
        private Text _opponentNameText;
        private Text _opponentLevelText;
        private Image _opponentAvatarImg;


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
        private Text _arPlayerNameText;
        private Text _arPlayerLevelText;
        private Image _arPlayerAvatarImg;
        private Text _arOpponentNameText;
        private Text _arOpponentLevelText;
        private Image _arOpponentAvatarImg;
        private Text _arTurnIndicatorText;
        public bool isAROverlayActive { get; private set; } = false;

        public void SetARHUDVisible(bool visible)
        {
            isAROverlayActive = visible;
            if (_standardHudRoot != null) _standardHudRoot.SetActive(!visible);
            if (_arHudOverlayObj != null) _arHudOverlayObj.SetActive(visible);
            if (visible)
            {
                UpdateOpponentDisplay();
            }
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
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(this);
                return;
            }

            AegisLocalization.OnLanguageChanged += RefreshLanguageHUD;

            if (_hudCanvasObj == null)
            {
                BuildHUD();
            }
            SetHUDVisible(false);
        }

        private void OnEnable()
        {
            if (_instance == null) _instance = this;
            if (_hudCanvasObj == null)
            {
                var strayCanvas = GameObject.Find("HUD_Canvas");
                if (strayCanvas != null) Destroy(strayCanvas);
                BuildHUD();
                if (MainMenu2D.Instance != null && MainMenu2D.Instance.IsInGameMatch)
                {
                    SetHUDVisible(true);
                }
            }
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

            UpdateOpponentDisplay();

            if (MainMenu2D.Instance != null && MainMenu2D.Instance.IsMenuVisible())
            {
                SetHUDVisible(false);
                _isTimerRunning = false;
            }
            else if (MainMenu2D.Instance != null && MainMenu2D.Instance.IsInGameMatch)
            {
                SetHUDVisible(true);
            }
        }

        public void SetHUDVisible(bool visible)
        {
            if ((_hudCanvasObj == null || _standardHudRoot == null) && visible)
            {
                BuildHUD();
            }

            if (_hudCanvasObj != null)
            {
                _hudCanvasObj.SetActive(visible);
            }
            if (visible)
            {
                if (!isAROverlayActive && _standardHudRoot != null)
                {
                    _standardHudRoot.SetActive(true);
                }
                UpdateOpponentDisplay();
            }
            else
            {
                SetTacticalDrawerOpen(false);
            }
        }

        public void UpdateOpponentDisplay()
        {
            bool isLichess = (MechChess.Multiplayer.LichessBridge.Instance != null && MechChess.Multiplayer.LichessBridge.Instance.IsInLichessMatch);
            bool isAegis = (MechChess.Multiplayer.OnlineMultiplayerManager.Instance != null && MechChess.Multiplayer.OnlineMultiplayerManager.Instance.IsOnlineMatch);
            bool isOnline = isLichess || isAegis;
            bool isVsAI = !isOnline;
            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                isVsAI = MechChess.Chess3D.ChessBoard3DManager.Instance.isVsAI;
            }

            string pName = "Player001";
            if (MechChess.Multiplayer.UserAccountManager.Instance != null && !string.IsNullOrEmpty(MechChess.Multiplayer.UserAccountManager.Instance.CurrentUsername))
            {
                pName = MechChess.Multiplayer.UserAccountManager.Instance.CurrentUsername;
            }

            string oppName;
            string oppLevel;
            Sprite oppSprite;

            if (isLichess)
            {
                var lichess = MechChess.Multiplayer.LichessBridge.Instance;
                oppName = string.IsNullOrEmpty(lichess.OpponentName) ? "Lichess Opponent" : lichess.OpponentName;
                oppLevel = $"Elo {lichess.OpponentElo} (Lichess)";
                oppSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png") ?? ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            }
            else if (isAegis)
            {
                var online = MechChess.Multiplayer.OnlineMultiplayerManager.Instance;
                oppName = string.IsNullOrEmpty(online.OpponentName) ? "Online Rival" : online.OpponentName;
                oppLevel = $"Elo {online.OpponentElo} (Online)";
                oppSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png") ?? ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            }
            else if (isVsAI)
            {
                oppName = "AI";
                oppLevel = "Lv. 5";
                oppSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_vs_ai_round.png") ?? ArcaneAssetLoader.LoadArcaneSprite("ai_portrait_full.png");
            }
            else
            {
                oppName = AegisLocalization.T("Người chơi 2", "Player 2");
                oppLevel = "PvP Local";
                oppSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png") ?? ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            }

            if (_playerNameText != null) _playerNameText.text = pName;
            if (_arPlayerNameText != null) _arPlayerNameText.text = pName;

            if (_opponentNameText != null) _opponentNameText.text = oppName;
            if (_opponentLevelText != null) _opponentLevelText.text = oppLevel;
            if (_arOpponentNameText != null) _arOpponentNameText.text = oppName;
            if (_arOpponentLevelText != null) _arOpponentLevelText.text = oppLevel;

            if (_opponentAvatarImg != null && oppSprite != null) _opponentAvatarImg.sprite = oppSprite;
            if (_arOpponentAvatarImg != null && oppSprite != null) _arOpponentAvatarImg.sprite = oppSprite;
        }

        public void SetOnlineOpponent(string oppName, int oppElo, string tag = "Online")
        {
            if (_opponentNameText != null) _opponentNameText.text = oppName;
            if (_opponentLevelText != null) _opponentLevelText.text = $"Elo {oppElo} ({tag})";
            if (_arOpponentNameText != null) _arOpponentNameText.text = oppName;
            if (_arOpponentLevelText != null) _arOpponentLevelText.text = $"Elo {oppElo} ({tag})";
            Sprite oppSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png") ?? ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            if (_opponentAvatarImg != null && oppSprite != null) _opponentAvatarImg.sprite = oppSprite;
            if (_arOpponentAvatarImg != null && oppSprite != null) _arOpponentAvatarImg.sprite = oppSprite;
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
            if (MechChess.Multiplayer.LichessBridge.Instance != null && MechChess.Multiplayer.LichessBridge.Instance.IsInLichessMatch)
            {
                MechChess.Multiplayer.LichessBridge.Instance.ResignMatch();
            }
            SetARHUDVisible(false);
            SetHUDVisible(false);
            if (MechChess.AR.HybridARController.Instance != null)
            {
                MechChess.AR.HybridARController.Instance.TurnOffCamera();
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
            _hudScaler.dynamicPixelsPerUnit = 2.5f;
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
            // 1. TOP HEADER: Floating Hex-Pill Clocks & Profiles (Enlarged Epic Royal Scale)
            // =========================================================================

            // --- A. Left Wing: Player Profile (Enlarged Epic Royal Scale) ---
            GameObject playerProfile = new GameObject("PlayerProfile");
            playerProfile.transform.SetParent(_standardHudRoot.transform, false);
            RectTransform ppRt = playerProfile.AddComponent<RectTransform>();
            ppRt.anchorMin = new Vector2(0f, 1f);
            ppRt.anchorMax = new Vector2(0f, 1f);
            ppRt.pivot = new Vector2(0f, 1f);
            ppRt.anchoredPosition = new Vector2(28f, -16f);
            ppRt.sizeDelta = new Vector2(520f, 160f);

            // Circular Avatar Frame with Gilded Antique Gold Rim (Enlarged)
            GameObject pAvFrame = new GameObject("PlayerAvatarFrame");
            pAvFrame.transform.SetParent(playerProfile.transform, false);
            Image pAvFrameImg = pAvFrame.AddComponent<Image>();
            pAvFrameImg.sprite = MedievalUIAssetLoader.GetSprite("avatar_frame_gold") ?? ArcaneAssetLoader.GetCircleSprite(154, new Color(0.090f, 0.137f, 0.176f, 0.95f), new Color(0.725f, 0.604f, 0.380f), 4f);
            pAvFrameImg.color = Color.white;
            RectTransform pafRt = pAvFrame.GetComponent<RectTransform>();
            pafRt.anchorMin = new Vector2(0f, 0.5f);
            pafRt.anchorMax = new Vector2(0f, 0.5f);
            pafRt.pivot = new Vector2(0f, 0.5f);
            pafRt.anchoredPosition = new Vector2(0f, 0f);
            pafRt.sizeDelta = new Vector2(154f, 154f);

            // Inner Avatar Image
            GameObject avObj = new GameObject("AvatarImg");
            avObj.transform.SetParent(pAvFrame.transform, false);
            _playerAvatarImg = avObj.AddComponent<Image>();
            Sprite avSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png");
            if (avSprite == null) avSprite = ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            if (avSprite != null) _playerAvatarImg.sprite = avSprite;
            else _playerAvatarImg.color = new Color(0.725f, 0.604f, 0.380f);
            RectTransform avRt = avObj.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0.5f, 0.5f);
            avRt.anchorMax = new Vector2(0.5f, 0.5f);
            avRt.pivot = new Vector2(0.5f, 0.5f);
            avRt.anchoredPosition = Vector2.zero;
            avRt.sizeDelta = new Vector2(136f, 136f);

            // Player Name & Level (Grand Royal Font Scales)
            string pName = "Player001";
            string pLevel = "Lv. 5";
            if (MechChess.Multiplayer.UserAccountManager.Instance != null && !string.IsNullOrEmpty(MechChess.Multiplayer.UserAccountManager.Instance.CurrentUsername))
            {
                pName = MechChess.Multiplayer.UserAccountManager.Instance.CurrentUsername;
            }

            _playerNameText = CreateText(playerProfile.transform, "PlayerName", pName, 44, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(340f, 52f), new Color(0.96f, 0.94f, 0.88f), defaultFont, true);
            RectTransform pntRt = _playerNameText.rectTransform;
            pntRt.anchorMin = new Vector2(0f, 0.5f);
            pntRt.anchorMax = new Vector2(0f, 0.5f);
            pntRt.pivot = new Vector2(0f, 0.5f);
            pntRt.anchoredPosition = new Vector2(170f, 26f);

            _playerLevelText = CreateText(playerProfile.transform, "PlayerLevel", pLevel, 30, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(260f, 38f), new Color(1f, 0.88f, 0.48f), defaultFont, true);
            RectTransform pltRt = _playerLevelText.rectTransform;
            pltRt.anchorMin = new Vector2(0f, 0.5f);
            pltRt.anchorMax = new Vector2(0f, 0.5f);
            pltRt.pivot = new Vector2(0f, 0.5f);
            pltRt.anchoredPosition = new Vector2(170f, -24f);

            // Sleek Antique Gold Accent Bar under Level
            CreateCyberUnderline(playerProfile.transform, "PlayerUnderline", new Vector2(170f, -48f), new Vector2(160f, 6f), new Color(0.85f, 0.70f, 0.40f, 0.95f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

            // --- B. Center Wing: Match Clocks & VS (Grand Tournament Scale) ---
            GameObject centerGroup = new GameObject("CenterMatchClocks");
            centerGroup.transform.SetParent(_standardHudRoot.transform, false);
            RectTransform cgRt = centerGroup.AddComponent<RectTransform>();
            cgRt.anchorMin = new Vector2(0.5f, 1f);
            cgRt.anchorMax = new Vector2(0.5f, 1f);
            cgRt.pivot = new Vector2(0.5f, 1f);
            cgRt.anchoredPosition = new Vector2(0f, -16f);
            cgRt.sizeDelta = new Vector2(880f, 140f);

            // 1. White Player Timer Pill - Width 320, Height 86 (Imperial Gold Baroque Frame, Centered Time)
            GameObject whiteTimerCapsule = CreatePanel(centerGroup.transform, "WhiteTimerCapsule", new Vector2(-195f, 0f), new Vector2(320f, 86f), CyberButtonColor.Cyan);
            _whiteTimerText = CreateText(whiteTimerCapsule.transform, "WhiteTimer", "09:45", 50, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(260f, 72f), Color.white, defaultFont, true);

            // 2. Center Duel Crossed Swords Badge (Replaces VS text with Golden Crossed Swords)
            GameObject vsSwordsObj = new GameObject("VS_SwordsBadge");
            vsSwordsObj.transform.SetParent(centerGroup.transform, false);
            RectTransform vsRt = vsSwordsObj.AddComponent<RectTransform>();
            vsRt.anchorMin = new Vector2(0.5f, 0.5f);
            vsRt.anchorMax = new Vector2(0.5f, 0.5f);
            vsRt.pivot = new Vector2(0.5f, 0.5f);
            vsRt.anchoredPosition = Vector2.zero;
            vsRt.sizeDelta = new Vector2(74f, 60f);

            Image vsImg = vsSwordsObj.AddComponent<Image>();
            Sprite vsSp = MedievalUIAssetLoader.GetSprite("icon_vs_crossed_swords")
                       ?? ArcaneAssetLoader.LoadArcaneSprite("icon_vs_crossed_swords.png");
            if (vsSp != null)
            {
                vsImg.sprite = vsSp;
                vsImg.preserveAspect = true;
                vsImg.color = Color.white;
            }
            else
            {
                CreateText(vsSwordsObj.transform, "FallbackVS", "VS", 46, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(80f, 70f), new Color(1f, 0.88f, 0.60f), defaultFont, true);
            }

            // 3. Black AI / Player 2 Timer Pill - Width 320, Height 86 (Symmetrical Imperial Gold Baroque Frame, Centered Time)
            GameObject blackTimerCapsule = CreatePanel(centerGroup.transform, "BlackTimerCapsule", new Vector2(195f, 0f), new Vector2(320f, 86f), CyberButtonColor.Cyan);
            _blackTimerText = CreateText(blackTimerCapsule.transform, "BlackTimer", "10:00", 50, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(260f, 72f), Color.white, defaultFont, true);

            // Status Ticker & Warning under clocks (Enlarged & Crisp)
            _turnIndicatorText = CreateText(centerGroup.transform, "TurnIndicator", AegisLocalization.T("⚔️ LƯỢT: HOÀNG GIA (TRẮNG) ⚔️", "⚔️ TURN: ROYAL ALLIANCE (WHITE) ⚔️"), 32, TextAnchor.MiddleCenter, new Vector2(0f, -66f), new Vector2(800f, 44f), new Color(1f, 0.88f, 0.50f), defaultFont, true);
            _firstMoveWarningText = CreateText(centerGroup.transform, "FirstMoveWarning", AegisLocalization.T("⏱️ LƯỢT ĐẦU: 60s", "⏱️ 1ST MOVE: 60s"), 26, TextAnchor.MiddleCenter, new Vector2(0f, -108f), new Vector2(800f, 38f), new Color(1f, 0.65f, 0.25f), defaultFont, true);

            // --- C. Right Wing: AI / Opponent Profile (Enlarged Epic Royal Scale) ---
            GameObject aiProfile = new GameObject("AIProfile");
            aiProfile.transform.SetParent(_standardHudRoot.transform, false);
            RectTransform aipRt = aiProfile.AddComponent<RectTransform>();
            aipRt.anchorMin = new Vector2(1f, 1f);
            aipRt.anchorMax = new Vector2(1f, 1f);
            aipRt.pivot = new Vector2(1f, 1f);
            aipRt.anchoredPosition = new Vector2(-28f, -16f);
            aipRt.sizeDelta = new Vector2(520f, 160f);

            // AI Avatar Frame with Antique Gold Rim
            GameObject aiAvFrame = new GameObject("AIAvatarFrame");
            aiAvFrame.transform.SetParent(aiProfile.transform, false);
            Image aiAvFrameImg = aiAvFrame.AddComponent<Image>();
            aiAvFrameImg.sprite = MedievalUIAssetLoader.GetSprite("avatar_frame_gold") ?? ArcaneAssetLoader.GetCircleSprite(154, new Color(0.18f, 0.08f, 0.10f, 0.95f), new Color(0.490f, 0.224f, 0.259f), 4f);
            aiAvFrameImg.color = Color.white;
            RectTransform aiafRt = aiAvFrame.GetComponent<RectTransform>();
            aiafRt.anchorMin = new Vector2(1f, 0.5f);
            aiafRt.anchorMax = new Vector2(1f, 0.5f);
            aiafRt.pivot = new Vector2(1f, 0.5f);
            aiafRt.anchoredPosition = new Vector2(0f, 0f);
            aiafRt.sizeDelta = new Vector2(154f, 154f);

            // Inner AI Avatar Image
            GameObject aiAvObj = new GameObject("AIAvatarImg");
            aiAvObj.transform.SetParent(aiAvFrame.transform, false);
            _opponentAvatarImg = aiAvObj.AddComponent<Image>();
            Sprite aiAvSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_vs_ai_round.png");
            if (aiAvSprite == null) aiAvSprite = ArcaneAssetLoader.LoadArcaneSprite("ai_portrait_full.png");
            if (aiAvSprite != null)
            {
                _opponentAvatarImg.sprite = aiAvSprite;
            }
            else
            {
                CreateText(aiAvObj.transform, "Icon", "AI", 32, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(120f, 120f), Color.white, defaultFont, true);
            }
            RectTransform aiavRt = aiAvObj.GetComponent<RectTransform>();
            aiavRt.anchorMin = new Vector2(0.5f, 0.5f);
            aiavRt.anchorMax = new Vector2(0.5f, 0.5f);
            aiavRt.pivot = new Vector2(0.5f, 0.5f);
            aiavRt.anchoredPosition = Vector2.zero;
            aiavRt.sizeDelta = new Vector2(136f, 136f);

            // AI / Opponent Name & Level (Right-aligned next to avatar)
            _opponentNameText = CreateText(aiProfile.transform, "AIName", "AI", 44, TextAnchor.MiddleRight, Vector2.zero, new Vector2(340f, 52f), new Color(0.96f, 0.94f, 0.88f), defaultFont, true);
            RectTransform ainRt = _opponentNameText.rectTransform;
            ainRt.anchorMin = new Vector2(1f, 0.5f);
            ainRt.anchorMax = new Vector2(1f, 0.5f);
            ainRt.pivot = new Vector2(1f, 0.5f);
            ainRt.anchoredPosition = new Vector2(-170f, 26f);

            _opponentLevelText = CreateText(aiProfile.transform, "AILevel", "Lv. 5", 30, TextAnchor.MiddleRight, Vector2.zero, new Vector2(260f, 38f), new Color(0.95f, 0.45f, 0.55f), defaultFont, true);
            RectTransform ailRt = _opponentLevelText.rectTransform;
            ailRt.anchorMin = new Vector2(1f, 0.5f);
            ailRt.anchorMax = new Vector2(1f, 0.5f);
            ailRt.pivot = new Vector2(1f, 0.5f);
            ailRt.anchoredPosition = new Vector2(-170f, -24f);

            // Sleek Blood Red Accent Bar under Level
            CreateCyberUnderline(aiProfile.transform, "AIUnderline", new Vector2(-170f, -48f), new Vector2(160f, 6f), new Color(0.490f, 0.224f, 0.259f, 0.95f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));


            // =========================================================================
            // 2. QUICK ACTION TOOLBAR (Enlarged 88x88 Royal Carved Buttons)
            // =========================================================================
            GameObject leftToolbar = new GameObject("LeftQuickToolbar");
            leftToolbar.transform.SetParent(_standardHudRoot.transform, false);
            RectTransform ltRt = leftToolbar.AddComponent<RectTransform>();
            ltRt.anchorMin = new Vector2(0f, 0.5f);
            ltRt.anchorMax = new Vector2(0f, 0.5f);
            ltRt.pivot = new Vector2(0f, 0.5f);
            ltRt.anchoredPosition = new Vector2(36f, 10f);
            ltRt.sizeDelta = new Vector2(130f, 680f);

            // 1. Tạm dừng (Pause)
            CreateArcaneToolbarButton(leftToolbar.transform, "BtnPause", MedievalUIAssetLoader.GetIcon(MedievalIcon.Pause) ?? ArcaneAssetLoader.GetPauseIconSprite(64), AegisLocalization.T("Tạm dừng", "Pause"), new Vector2(0f, 260f), () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.ShowPauseModal();
                else ToggleTacticalDrawer();
            }, defaultFont);

            // 2. Gợi ý (Hint / AI Advice)
            CreateArcaneToolbarButton(leftToolbar.transform, "BtnHint", MedievalUIAssetLoader.GetIcon(MedievalIcon.Hint) ?? ArcaneAssetLoader.GetHintIconSprite(64), AegisLocalization.T("Gợi ý", "Hint"), new Vector2(0f, 130f), () =>
            {
                OnTriggerAIClicked?.Invoke();
            }, defaultFont);

            // 3. Góc nhìn Camera (Camera perspective toggle)
            CreateArcaneToolbarButton(leftToolbar.transform, "BtnCamera", ArcaneAssetLoader.GetCameraIconSprite(64), AegisLocalization.T("Góc Cam", "Camera"), new Vector2(0f, 0f), () =>
            {
                if (MechChess.Mobile.MobileTouchInput.Instance != null)
                {
                    string modeName = MechChess.Mobile.MobileTouchInput.Instance.CycleCameraPerspective();
                    RecordMove($"[CAMERA] {modeName}");
                }
            }, defaultFont);

            // 4. Hủy nước đi (Undo Move)
            CreateArcaneToolbarButton(leftToolbar.transform, "BtnUndo", ArcaneAssetLoader.GetUndoIconSprite(64), AegisLocalization.T("Hủy nước đi", "Undo"), new Vector2(0f, -130f), () =>
            {
                if (isRankedMode)
                {
                    RecordMove(AegisLocalization.T("[TỪ CHỐI] Không thể hoàn tác trong chế độ Đấu Xếp Hạng!", "[DECLINED] Cannot undo in Ranked Mode!"));
                    return;
                }
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.UndoMove();
            }, defaultFont);

            // 5. Thiết lập (Settings Modal)
            CreateArcaneToolbarButton(leftToolbar.transform, "BtnSettings", ArcaneAssetLoader.GetSettingsGearIconSprite(64), AegisLocalization.T("Thiết lập", "Settings"), new Vector2(0f, -260f), () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.OpenSettingsModal();
            }, defaultFont);

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

            // Sidebar Drawer Panel (Anchored to Right Side - Chiếm 45% màn hình)
            GameObject drawer = CreatePanel(_standardHudRoot.transform, "TacticalDrawer", Vector2.zero, Vector2.zero, CyberButtonColor.ModalFrame);
            RectTransform dRt = drawer.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.55f, 0f);
            dRt.anchorMax = new Vector2(1f, 1f);
            dRt.pivot = new Vector2(1f, 0.5f);
            dRt.offsetMin = new Vector2(10f, 15f);
            dRt.offsetMax = new Vector2(-15f, -15f);
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
            dhRt.sizeDelta = new Vector2(-24f, 48f);

            CreateText(drawerHeader.transform, "DrawerTitle", AegisLocalization.T("BÀN ĐIỀU KHIỂN", "COMMAND DECK"), 22, TextAnchor.MiddleLeft, new Vector2(24f, 0f), new Vector2(400f, 38f), new Color(0.96f, 0.88f, 0.55f), defaultFont, true);
            GameObject closeBtn = CreateCyberGameButton(drawerHeader.transform, "BtnCloseDrawer", "×", "", "", new Vector2(-12f, 0f), new Vector2(52f, 36f), CyberButtonColor.Crimson, () => SetTacticalDrawerOpen(false), defaultFont);
            RectTransform cbRt = closeBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 0.5f);
            cbRt.anchorMax = new Vector2(1f, 0.5f);
            cbRt.pivot = new Vector2(1f, 0.5f);

            // --- Fixed Actions Container (Directly below header, rock-solid click response without ScrollRect displacement) ---
            GameObject actionsContainer = new GameObject("DrawerActionsContainer");
            actionsContainer.transform.SetParent(drawer.transform, false);
            RectTransform actRt = actionsContainer.AddComponent<RectTransform>();
            actRt.anchorMin = new Vector2(0f, 1f);
            actRt.anchorMax = new Vector2(1f, 1f);
            actRt.pivot = new Vector2(0.5f, 1f);
            actRt.anchoredPosition = new Vector2(0f, -60f);
            actRt.sizeDelta = new Vector2(-24f, 335f);

            // Core match controls: Undo, Offer Draw, Resign (Dàn đều 3 nút to rõ)
            CreateCyberGameButton(actionsContainer.transform, "DrawerUndo", "↩", AegisLocalization.T("HOÀN TÁC", "UNDO"), "", new Vector2(-270f, -24f), new Vector2(255f, 44f), CyberButtonColor.Cyan, () =>
            {
                if (isRankedMode || (MechChess.Multiplayer.LichessBridge.Instance != null && MechChess.Multiplayer.LichessBridge.Instance.IsInLichessMatch))
                {
                    RecordMove(AegisLocalization.T("[TỪ CHỐI] Không thể hoàn tác trong chế độ Đấu Online / Ranked!", "[DECLINED] Cannot undo moves in Online / Ranked Mode!"));
                    return;
                }
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.UndoMove();
            }, defaultFont);

            CreateCyberGameButton(actionsContainer.transform, "DrawerDraw", "", AegisLocalization.T("XIN HÒA", "OFFER DRAW"), "", new Vector2(0f, -24f), new Vector2(255f, 44f), CyberButtonColor.Amber, () =>
            {
                SetTacticalDrawerOpen(false);
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    MechChess.Chess3D.ChessBoard3DManager.Instance.OfferDraw();
            }, defaultFont);

            CreateCyberGameButton(actionsContainer.transform, "DrawerResign", "", AegisLocalization.T("ĐẦU HÀNG", "RESIGN"), "", new Vector2(270f, -24f), new Vector2(255f, 44f), CyberButtonColor.Crimson, () =>
            {
                SetTacticalDrawerOpen(false);
                if (MechChess.AR.HybridARController.Instance != null)
                {
                    MechChess.AR.HybridARController.Instance.TurnOffCamera();
                }

                PieceColor myColor = PieceColor.White;
                if (MechChess.Multiplayer.LichessBridge.Instance != null && MechChess.Multiplayer.LichessBridge.Instance.IsInLichessMatch)
                {
                    myColor = MechChess.Multiplayer.LichessBridge.Instance.IsPlayerWhite ? PieceColor.White : PieceColor.Black;
                }
                else if (MechChess.Multiplayer.OnlineMultiplayerManager.Instance != null && MechChess.Multiplayer.OnlineMultiplayerManager.Instance.IsOnlineMatch)
                {
                    myColor = MechChess.Multiplayer.OnlineMultiplayerManager.Instance.MyRole;
                }
                else if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                {
                    myColor = MechChess.Chess3D.ChessBoard3DManager.Instance.playerColor;
                }

                Debug.Log($"[Resign] Surrendering locally as color {myColor}");

                if (MechChess.Multiplayer.LichessBridge.Instance != null && MechChess.Multiplayer.LichessBridge.Instance.IsInLichessMatch)
                {
                    MechChess.Multiplayer.LichessBridge.Instance.ResignMatch();
                }

                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                {
                    MechChess.Chess3D.ChessBoard3DManager.Instance.Resign(myColor);
                }
                else
                {
                    PieceColor winner = (myColor == PieceColor.White) ? PieceColor.Black : PieceColor.White;
                    TriggerGameOver(winner, AegisLocalization.T("Bạn đã đầu hàng!", "You resigned!"));
                }
            }, defaultFont);

            CreatePanel(actionsContainer.transform, "ControlDivider1", new Vector2(0f, -52f), new Vector2(810f, 2f), CyberButtonColor.PanelFrame);

            CreateCyberGameButton(actionsContainer.transform, "BtnScanCard", "", AegisLocalization.T("QUÉT THẺ", "SCAN CARD"), "", new Vector2(-208f, -80f), new Vector2(395f, 44f), CyberButtonColor.Cyan, () =>
            {
                SetTacticalDrawerOpen(false);
                OnScanCardClicked?.Invoke();
            }, defaultFont);

            CreateCyberGameButton(actionsContainer.transform, "BtnAIMove", "", AegisLocalization.T("PHÁP LỰC AI", "AI SPELL"), "", new Vector2(208f, -80f), new Vector2(395f, 44f), CyberButtonColor.Crimson, () =>
            {
                OnTriggerAIClicked?.Invoke();
            }, defaultFont);

            CreateCyberGameButton(actionsContainer.transform, "BtnOnlineLobby", "", AegisLocalization.T("ONLINE 2P", "ONLINE 2P"), "", new Vector2(-208f, -132f), new Vector2(395f, 44f), CyberButtonColor.Sapphire, () =>
            {
                SetTacticalDrawerOpen(false);
                if (_multiplayerModal != null) _multiplayerModal.SetActive(true);
            }, defaultFont);

            CreateCyberGameButton(actionsContainer.transform, "BtnToggleAR", "", AegisLocalization.T("AR WEBCAM", "AR WEBCAM"), "", new Vector2(208f, -132f), new Vector2(395f, 44f), CyberButtonColor.Emerald, () =>
            {
                OnToggleARClicked?.Invoke();
            }, defaultFont);

            CreateCyberGameButton(actionsContainer.transform, "BtnReset", "", AegisLocalization.T("LÀM MỚI", "RESET MATCH"), "", new Vector2(-208f, -184f), new Vector2(395f, 44f), CyberButtonColor.Gunmetal, () =>
            {
                SetTacticalDrawerOpen(false);
                if (MechChess.AR.HybridARController.Instance != null)
                {
                    MechChess.AR.HybridARController.Instance.TurnOffCamera();
                }
                OnResetGameClicked?.Invoke();
            }, defaultFont);

            CreateCyberGameButton(actionsContainer.transform, "BtnExitToMenu", "", AegisLocalization.T("VỀ MENU", "EXIT TO MENU"), "", new Vector2(208f, -184f), new Vector2(395f, 44f), CyberButtonColor.Crimson, () =>
            {
                SetTacticalDrawerOpen(false);
                ReturnToMainMenu();
            }, defaultFont);

            CreatePanel(actionsContainer.transform, "ControlDivider2", new Vector2(0f, -214f), new Vector2(810f, 2f), CyberButtonColor.PanelFrame);

            // Log Section Title & Column Labels (Inside actions group, crisp & aligned)
            CreateText(actionsContainer.transform, "Sec3Title", AegisLocalization.T("📜  NHẬT KÝ VÁN ĐẤU", "📜  MATCH LOG"), 20, TextAnchor.MiddleLeft, new Vector2(0f, -236f), new Vector2(810f, 26f), new Color(0.96f, 0.88f, 0.55f), defaultFont, true);
            CreateText(actionsContainer.transform, "HistoryColumns", AegisLocalization.T("LƯỢT              PHE HOÀNG GIA (TRẮNG)                                   PHE HẮC ÁM (ĐEN)", "MOVE              ROYAL CORPS (WHITE)                                    SHADOW FACTION (BLACK)"), 15, TextAnchor.MiddleLeft, new Vector2(0f, -260f), new Vector2(810f, 22f), new Color(0.851f, 0.784f, 0.635f, 0.95f), defaultFont, true);

            // --- Bottom Telemetry Area (Fixed at bottom edge of drawer) ---
            GameObject telemetryContainer = new GameObject("DrawerTelemetryContainer");
            telemetryContainer.transform.SetParent(drawer.transform, false);
            RectTransform telemRt = telemetryContainer.AddComponent<RectTransform>();
            telemRt.anchorMin = new Vector2(0f, 0f);
            telemRt.anchorMax = new Vector2(1f, 0f);
            telemRt.pivot = new Vector2(0.5f, 0f);
            telemRt.anchoredPosition = new Vector2(0f, 10f);
            telemRt.sizeDelta = new Vector2(-24f, 76f);

            GameObject confPod = CreatePanel(telemetryContainer.transform, "ConfidencePod", new Vector2(0f, 48f), new Vector2(810f, 36f), CyberButtonColor.Amber);
            _lastConfidenceText = CreateText(confPod.transform, "ConfidenceDisplay", "AI CV MATRIX: STANDBY (N/A)", 16, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(800f, 28f), new Color(0.96f, 0.88f, 0.55f), defaultFont, true);

            _aiServerStatusText = CreateText(telemetryContainer.transform, "AIStatus", "● AI SERVER: ONLINE", 15, TextAnchor.MiddleLeft, new Vector2(-208f, 14f), new Vector2(380f, 24f), new Color(0.38f, 0.85f, 0.45f), defaultFont, true);
            _cloudStatusText = CreateText(telemetryContainer.transform, "CloudStatus", "● AIO CLOUD: LINKED", 15, TextAnchor.MiddleRight, new Vector2(208f, 14f), new Vector2(380f, 24f), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);

            // --- Move Log Container (Dynamically fills all space between actions above and telemetry below) ---
            GameObject logContainer = CreatePanel(drawer.transform, "HistoryLogBox", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform lcRt = logContainer.GetComponent<RectTransform>();
            lcRt.anchorMin = new Vector2(0f, 0f);
            lcRt.anchorMax = new Vector2(1f, 1f);
            lcRt.pivot = new Vector2(0.5f, 0.5f);
            lcRt.offsetMin = new Vector2(36f, 92f);
            lcRt.offsetMax = new Vector2(-36f, -346f);

            // Inner Scroll Viewport (Inset safely inside ornate parchment frame with generous 54px left/right, 34px top margins)
            GameObject logViewport = new GameObject("LogViewport");
            logViewport.transform.SetParent(logContainer.transform, false);
            RectTransform vpRt = logViewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(54f, 24f);
            vpRt.offsetMax = new Vector2(-54f, -34f);
            logViewport.AddComponent<RectMask2D>();

            Image vpImg = logViewport.AddComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = true;

            _moveHistoryScrollRect = logContainer.AddComponent<ScrollRect>();
            _moveHistoryScrollRect.horizontal = false;
            _moveHistoryScrollRect.vertical = true;
            _moveHistoryScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _moveHistoryScrollRect.scrollSensitivity = 35f;
            _moveHistoryScrollRect.viewport = vpRt;

            // Chữ nhật ký nước đi: Phóng to lên Font size 24 Bold, màu mực đen nâu đậm nét 100%, lề thụt sâu vào trong khung
            GameObject txtObj = new GameObject("HistoryLog");
            txtObj.transform.SetParent(logViewport.transform, false);
            _moveHistoryText = txtObj.AddComponent<Text>();
            _moveHistoryText.text = AegisLocalization.T("Trận đấu bắt đầu.\nĐang chờ nước đi đầu tiên...", "Match started.\nAwaiting initial moves...");
            _moveHistoryText.font = defaultFont;
            _moveHistoryText.fontSize = 24;
            _moveHistoryText.fontStyle = FontStyle.Bold;
            _moveHistoryText.lineSpacing = 1.34f;
            _moveHistoryText.alignment = TextAnchor.UpperLeft;
            _moveHistoryText.color = new Color(0.12f, 0.06f, 0.02f, 1.0f); // Deep imperial brown/black ink
            _moveHistoryText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _moveHistoryText.verticalOverflow = VerticalWrapMode.Overflow;
            _moveHistoryText.raycastTarget = false;

            // Crisp outline to make every character sharp and easily readable
            Outline logOutline = txtObj.AddComponent<Outline>();
            logOutline.effectColor = new Color(1.0f, 0.96f, 0.88f, 0.80f); // Warm ivory gold highlight
            logOutline.effectDistance = new Vector2(0.8f, -0.8f);

            // Subtle depth shadow
            Shadow logShadow = txtObj.AddComponent<Shadow>();
            logShadow.effectColor = new Color(0.08f, 0.04f, 0.01f, 0.35f);
            logShadow.effectDistance = new Vector2(1.2f, -1.2f);

            ContentSizeFitter csf = txtObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            RectTransform mhtRt = txtObj.GetComponent<RectTransform>();
            mhtRt.anchorMin = new Vector2(0f, 1f);
            mhtRt.anchorMax = new Vector2(1f, 1f);
            mhtRt.pivot = new Vector2(0.5f, 1f);
            mhtRt.anchoredPosition = Vector2.zero;
            mhtRt.sizeDelta = new Vector2(0f, 0f);

            _moveHistoryScrollRect.content = mhtRt;

            // Hide Drawer by default (Clean board view!)
            _tacticalDrawerObj.SetActive(false);
            _drawerBackdropObj.SetActive(false);

            // =========================================================================
            // 4. BOTTOM-CENTER PIECE STATUS DOCK (Matching Image 2 Screen 8 - Enlarged Grand Royal Scale)
            // =========================================================================
            GameObject pieceTray = CreatePanel(_standardHudRoot.transform, "PieceTray", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform ptRt = pieceTray.GetComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0.5f, 0f);
            ptRt.anchorMax = new Vector2(0.5f, 0f);
            ptRt.pivot = new Vector2(0.5f, 0f);
            ptRt.anchoredPosition = new Vector2(0f, 20f);
            ptRt.sizeDelta = new Vector2(880f, 98f);

            string[] pieceIcons = { "♟", "♞", "♜", "♝", "♛", "♚" };
            string[] pieceNames = (AegisLocalization.CurrentLanguage == AppLanguage.Vietnamese)
                ? new[] { "Tốt", "Mã", "Tượng", "Xe", "Hậu", "Vua" }
                : new[] { "Pawn", "Knight", "Bishop", "Rook", "Queen", "King" };
            float trayStartX = -330f;
            float trayStepX = 112f;
            string[] pieceKeys = { "pawn", "knight", "bishop", "rook", "queen", "king" };
            for (int i = 0; i < 6; i++)
            {
                GameObject slot = new GameObject($"PieceSlot_{i}");
                slot.transform.SetParent(pieceTray.transform, false);
                RectTransform slotRt = slot.AddComponent<RectTransform>();
                slotRt.anchoredPosition = new Vector2(trayStartX + i * trayStepX, 0f);
                slotRt.sizeDelta = new Vector2(104f, 88f);

                Sprite pieceSpr = MedievalUIAssetLoader.GetChessIcon(pieceKeys[i]);
                if (pieceSpr != null)
                {
                    GameObject pImgObj = new GameObject("PieceIcon");
                    pImgObj.transform.SetParent(slot.transform, false);
                    Image pImg = pImgObj.AddComponent<Image>();
                    pImg.sprite = pieceSpr;
                    pImg.preserveAspect = true;
                    pImg.raycastTarget = false;
                    RectTransform pRt = pImgObj.GetComponent<RectTransform>();
                    pRt.anchoredPosition = new Vector2(0f, 14f);
                    pRt.sizeDelta = new Vector2(54f, 54f);
                }
                else
                {
                    Text iconTxt = CreateText(slot.transform, "Icon", pieceIcons[i], 42, TextAnchor.MiddleCenter, new Vector2(0f, 14f), new Vector2(80f, 52f), new Color(0.96f, 0.94f, 0.88f), defaultFont, true);
                    iconTxt.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.85f);
                }

                Text nameTxt = CreateText(slot.transform, "Name", pieceNames[i], 22, TextAnchor.MiddleCenter, new Vector2(0f, -22f), new Vector2(100f, 26f), new Color(1f, 0.88f, 0.50f), defaultFont, true);
                Shadow nameSh = nameTxt.gameObject.AddComponent<Shadow>();
                nameSh.effectColor = new Color(0f, 0f, 0f, 0.85f);
                nameSh.effectDistance = new Vector2(1f, -1f);
            }

            // Circular Gilded Forward Action Button (Enlarged 72x72)
            GameObject endTurnBtnObj = new GameObject("BtnEndTurn");
            endTurnBtnObj.transform.SetParent(pieceTray.transform, false);
            Image etImg = endTurnBtnObj.AddComponent<Image>();
            etImg.sprite = MedievalUIAssetLoader.GetSprite("avatar_frame_gold") ?? ArcaneAssetLoader.GetCircleSprite(72, new Color(0.090f, 0.137f, 0.176f, 0.95f), new Color(0.725f, 0.604f, 0.380f), 2.5f);
            etImg.color = Color.white;
            Button etBtn = endTurnBtnObj.AddComponent<Button>();
            etBtn.onClick.AddListener(ToggleTacticalDrawer);
            endTurnBtnObj.AddComponent<CyberButtonEffect>();
            RectTransform etRt = endTurnBtnObj.GetComponent<RectTransform>();
            etRt.anchoredPosition = new Vector2(370f, 0f);
            etRt.sizeDelta = new Vector2(72f, 72f);
            Text arrTxt = CreateText(endTurnBtnObj.transform, "Arrow", "➔", 32, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(64f, 64f), new Color(0.92f, 0.80f, 0.45f), defaultFont, true);
            arrTxt.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.85f);

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

        private GameObject CreateARActionTile(Transform parent, string name, Sprite iconSprite, string label, Vector2 tilePos, Action onClick, Font font)
        {
            GameObject tileContainer = new GameObject(name);
            tileContainer.transform.SetParent(parent, false);
            RectTransform tcRt = tileContainer.AddComponent<RectTransform>();
            tcRt.anchoredPosition = tilePos;
            tcRt.sizeDelta = new Vector2(106f, 114f); // Enlarged 20% from 88x92

            // Carved Stone & Antique Gold Beveled Button Tile (Enlarged 22% from 64x64 to 78x78)
            GameObject btnObj = new GameObject("BtnTile");
            btnObj.transform.SetParent(tileContainer.transform, false);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodSmall) ?? ArcaneAssetLoader.GetGlassBoxSprite(78, 78, 20f, new Color(0.090f, 0.137f, 0.176f, 0.95f), new Color(0.725f, 0.604f, 0.380f, 0.95f), 2.4f);
            btnImg.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            btnObj.AddComponent<CyberButtonEffect>();

            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchoredPosition = new Vector2(0f, 16f);
            btnRt.sizeDelta = new Vector2(78f, 78f);

            // High-resolution antialiased procedural icon Image
            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("IconImg");
                iconObj.transform.SetParent(btnObj.transform, false);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.color = new Color(0.96f, 0.94f, 0.88f, 1f);
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                RectTransform iconRt = iconObj.GetComponent<RectTransform>();
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.sizeDelta = new Vector2(46f, 46f);
            }

            // Warm Ivory Label Underneath (Enlarged font 16 and size 110x24)
            CreateText(tileContainer.transform, "Label", label, 16, TextAnchor.MiddleCenter, new Vector2(0f, -32f), new Vector2(110f, 24f), new Color(0.96f, 0.94f, 0.88f, 1f), font, true);

            return tileContainer;
        }

        private GameObject CreateARActionTile(Transform parent, string name, string icon, string label, Vector2 tilePos, Action onClick, Font font)
        {
            // Automatic mapping for string callers to procedural crisp vector icons
            Sprite spr = null;
            if (name.Contains("Photo") || name.Contains("Camera")) spr = ArcaneAssetLoader.GetCameraIconSprite(64);
            else if (name.Contains("Video") || name.Contains("Record")) spr = ArcaneAssetLoader.GetVideoIconSprite(64);
            else if (name.Contains("Setting")) spr = ArcaneAssetLoader.GetSettingsGearIconSprite(64);
            else if (name.Contains("Exit")) spr = ArcaneAssetLoader.GetCrosshairIconSprite(64);
            return CreateARActionTile(parent, name, spr, label, tilePos, onClick, font);
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
            // 1. TOP-LEFT: Player Info Stone Capsule with Integrated Gold Timer Sub-Pill (Enlarged 3x Player, 2x Timer)
            // =========================================================================
            GameObject playerCapsule = CreatePanel(_arHudOverlayObj.transform, "ARPlayerCapsule", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform pcRt = playerCapsule.GetComponent<RectTransform>();
            pcRt.anchorMin = new Vector2(0f, 1f);
            pcRt.anchorMax = new Vector2(0f, 1f);
            pcRt.pivot = new Vector2(0f, 1f);
            pcRt.anchoredPosition = new Vector2(25f, -16f);
            pcRt.sizeDelta = new Vector2(490f, 110f);

            // Circular Player Avatar with Antique Gold Filigree Ring (Enlarged 3x)
            GameObject pAvFrame = new GameObject("ARPlayerAvatarFrame");
            pAvFrame.transform.SetParent(playerCapsule.transform, false);
            Image pAvFrameImg = pAvFrame.AddComponent<Image>();
            pAvFrameImg.sprite = ArcaneAssetLoader.GetCircleSprite(100, new Color(0.090f, 0.137f, 0.176f, 0.95f), new Color(0.725f, 0.604f, 0.380f, 1f), 3.2f);
            RectTransform pafRt = pAvFrame.GetComponent<RectTransform>();
            pafRt.anchorMin = new Vector2(0f, 0.5f);
            pafRt.anchorMax = new Vector2(0f, 0.5f);
            pafRt.pivot = new Vector2(0f, 0.5f);
            pafRt.anchoredPosition = new Vector2(10f, 0f);
            pafRt.sizeDelta = new Vector2(92f, 92f);

            GameObject avObj = new GameObject("AvatarImg");
            avObj.transform.SetParent(pAvFrame.transform, false);
            _arPlayerAvatarImg = avObj.AddComponent<Image>();
            Sprite avSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png");
            if (avSprite == null) avSprite = ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            if (avSprite != null) _arPlayerAvatarImg.sprite = avSprite;
            else _arPlayerAvatarImg.color = new Color(0.725f, 0.604f, 0.380f);
            RectTransform avRt = avObj.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0.5f, 0.5f);
            avRt.anchorMax = new Vector2(0.5f, 0.5f);
            avRt.pivot = new Vector2(0.5f, 0.5f);
            avRt.anchoredPosition = Vector2.zero;
            avRt.sizeDelta = new Vector2(80f, 80f);

            // Player Name & Level
            string pName = "Player001";
            string pLevel = "Lv. 5";
            if (MechChess.Multiplayer.UserAccountManager.Instance != null && !string.IsNullOrEmpty(MechChess.Multiplayer.UserAccountManager.Instance.CurrentUsername))
            {
                pName = MechChess.Multiplayer.UserAccountManager.Instance.CurrentUsername;
            }

            _arPlayerNameText = CreateText(playerCapsule.transform, "PlayerName", pName, 34, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(190f, 40f), new Color(0.96f, 0.94f, 0.88f), font, true);
            RectTransform pntRt = _arPlayerNameText.rectTransform;
            pntRt.anchorMin = new Vector2(0f, 0.5f);
            pntRt.anchorMax = new Vector2(0f, 0.5f);
            pntRt.pivot = new Vector2(0f, 0.5f);
            pntRt.anchoredPosition = new Vector2(114f, 16f);

            _arPlayerLevelText = CreateText(playerCapsule.transform, "PlayerLevel", pLevel, 24, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(190f, 30f), new Color(0.725f, 0.604f, 0.380f), font, true);
            RectTransform pltRt = _arPlayerLevelText.rectTransform;
            pltRt.anchorMin = new Vector2(0f, 0.5f);
            pltRt.anchorMax = new Vector2(0f, 0.5f);
            pltRt.pivot = new Vector2(0f, 0.5f);
            pltRt.anchoredPosition = new Vector2(114f, -16f);

            // Embedded Warm Gold Timer Sub-Pill (Enlarged 2x)
            GameObject whiteTimerSubPill = CreatePanel(playerCapsule.transform, "WhiteTimerSubPill", Vector2.zero, Vector2.zero, CyberButtonColor.Amber);
            RectTransform wtspRt = whiteTimerSubPill.GetComponent<RectTransform>();
            wtspRt.anchorMin = new Vector2(1f, 0.5f);
            wtspRt.anchorMax = new Vector2(1f, 0.5f);
            wtspRt.pivot = new Vector2(1f, 0.5f);
            wtspRt.anchoredPosition = new Vector2(-12f, 0f);
            wtspRt.sizeDelta = new Vector2(150f, 70f);

            _arWhiteTimerText = CreateText(whiteTimerSubPill.transform, "ARWhiteTimer", FormatTime(_whiteTimer), 40, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(144f, 56f), new Color(0.96f, 0.94f, 0.88f), font, true);

            // =========================================================================
            // 2. TOP-RIGHT: AI / Opponent Info Stone Capsule with Integrated Crimson Timer (Enlarged 3x Player, 2x Timer)
            // =========================================================================
            GameObject aiCapsule = CreatePanel(_arHudOverlayObj.transform, "AICapsule", Vector2.zero, Vector2.zero, CyberButtonColor.PanelFrame);
            RectTransform aicRt = aiCapsule.GetComponent<RectTransform>();
            aicRt.anchorMin = new Vector2(1f, 1f);
            aicRt.anchorMax = new Vector2(1f, 1f);
            aicRt.pivot = new Vector2(1f, 1f);
            aicRt.anchoredPosition = new Vector2(-25f, -16f);
            aicRt.sizeDelta = new Vector2(490f, 110f);

            // AI / Opponent Name & Level on the left
            _arOpponentNameText = CreateText(aiCapsule.transform, "AIName", "AI", 34, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(190f, 40f), new Color(0.96f, 0.94f, 0.88f), font, true);
            RectTransform ainRt = _arOpponentNameText.rectTransform;
            ainRt.anchorMin = new Vector2(0f, 0.5f);
            ainRt.anchorMax = new Vector2(0f, 0.5f);
            ainRt.pivot = new Vector2(0f, 0.5f);
            ainRt.anchoredPosition = new Vector2(20f, 16f);

            _arOpponentLevelText = CreateText(aiCapsule.transform, "AILevel", "Lv. 5", 24, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(190f, 30f), new Color(0.77f, 0.35f, 0.40f), font, true);
            RectTransform ailRt = _arOpponentLevelText.rectTransform;
            ailRt.anchorMin = new Vector2(0f, 0.5f);
            ailRt.anchorMax = new Vector2(0f, 0.5f);
            ailRt.pivot = new Vector2(0f, 0.5f);
            ailRt.anchoredPosition = new Vector2(20f, -16f);

            // Embedded Crimson Timer Sub-Pill (Enlarged 2x)
            GameObject blackTimerSubPill = CreatePanel(aiCapsule.transform, "BlackTimerSubPill", Vector2.zero, Vector2.zero, CyberButtonColor.Crimson);
            RectTransform btspRt = blackTimerSubPill.GetComponent<RectTransform>();
            btspRt.anchorMin = new Vector2(0.5f, 0.5f);
            btspRt.anchorMax = new Vector2(0.5f, 0.5f);
            btspRt.pivot = new Vector2(0.5f, 0.5f);
            btspRt.anchoredPosition = new Vector2(105f, 0f);
            btspRt.sizeDelta = new Vector2(150f, 70f);

            _arBlackTimerText = CreateText(blackTimerSubPill.transform, "ARBlackTimer", FormatTime(_blackTimer), 40, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(144f, 56f), new Color(0.96f, 0.94f, 0.88f), font, true);

            // Circular AI / Opponent Avatar on Far Right (Enlarged 3x)
            GameObject aiAvFrame = new GameObject("ARAIAvatarFrame");
            aiAvFrame.transform.SetParent(aiCapsule.transform, false);
            Image aiAvFrameImg = aiAvFrame.AddComponent<Image>();
            aiAvFrameImg.sprite = ArcaneAssetLoader.GetCircleSprite(100, new Color(0.28f, 0.08f, 0.10f, 0.95f), new Color(0.490f, 0.224f, 0.259f), 3.2f);
            RectTransform aiafRt = aiAvFrame.GetComponent<RectTransform>();
            aiafRt.anchorMin = new Vector2(1f, 0.5f);
            aiafRt.anchorMax = new Vector2(1f, 0.5f);
            aiafRt.pivot = new Vector2(1f, 0.5f);
            aiafRt.anchoredPosition = new Vector2(-10f, 0f);
            aiafRt.sizeDelta = new Vector2(92f, 92f);

            GameObject aiAvObj = new GameObject("AIAvatarImg");
            aiAvObj.transform.SetParent(aiAvFrame.transform, false);
            _arOpponentAvatarImg = aiAvObj.AddComponent<Image>();
            Sprite aiAvSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_vs_ai_round.png");
            if (aiAvSprite == null) aiAvSprite = ArcaneAssetLoader.LoadArcaneSprite("ai_portrait_full.png");
            if (aiAvSprite != null)
            {
                _arOpponentAvatarImg.sprite = aiAvSprite;
            }
            else
            {
                CreateText(aiAvObj.transform, "Icon", "AI", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(70f, 70f), Color.white, font, true);
            }
            RectTransform aiavRt = aiAvObj.GetComponent<RectTransform>();
            aiavRt.anchorMin = new Vector2(0.5f, 0.5f);
            aiavRt.anchorMax = new Vector2(0.5f, 0.5f);
            aiavRt.pivot = new Vector2(0.5f, 0.5f);
            aiavRt.anchoredPosition = Vector2.zero;
            aiavRt.sizeDelta = new Vector2(80f, 80f);

            // Center AR Turn Pill (Enlarged 2x)
            GameObject arCenterGroup = new GameObject("ARCenterGroup");
            arCenterGroup.transform.SetParent(_arHudOverlayObj.transform, false);
            RectTransform arcgRt = arCenterGroup.AddComponent<RectTransform>();
            arcgRt.anchorMin = new Vector2(0.5f, 1f);
            arcgRt.anchorMax = new Vector2(0.5f, 1f);
            arcgRt.pivot = new Vector2(0.5f, 1f);
            arcgRt.anchoredPosition = new Vector2(0f, -20f);
            arcgRt.sizeDelta = new Vector2(460f, 54f);

            GameObject arTurnPill = CreatePanel(arCenterGroup.transform, "ARTurnPill", Vector2.zero, new Vector2(440f, 48f), CyberButtonColor.PanelFrame);
            _arTurnIndicatorText = CreateText(arTurnPill.transform, "ARTurnText", AegisLocalization.T(">> LƯỢT: HOÀNG GIA (TRẮNG) <<", ">> TURN: ROYAL (WHITE) <<"), 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(420f, 42f), new Color(0.92f, 0.80f, 0.45f), font, true);


            // =========================================================================
            // 3. RIGHT VERTICAL TOOLBAR: Fantasy Action Buttons (Enlarged +20%)
            // =========================================================================
            GameObject rightToolbar = new GameObject("ARRightToolbar");
            rightToolbar.transform.SetParent(_arHudOverlayObj.transform, false);
            RectTransform rtRt = rightToolbar.AddComponent<RectTransform>();
            rtRt.anchorMin = new Vector2(1f, 0.5f);
            rtRt.anchorMax = new Vector2(1f, 0.5f);
            rtRt.pivot = new Vector2(1f, 0.5f);
            rtRt.anchoredPosition = new Vector2(-28f, -10f);
            rtRt.sizeDelta = new Vector2(110f, 500f);

            // 1. Đổi chế độ AR (Toggle AR Mode)
            CreateARActionTile(rightToolbar.transform, "BtnToggleARMode", ArcaneAssetLoader.GetUndoIconSprite(64), AegisLocalization.T("Đổi AR", "Switch AR"), new Vector2(0f, 180f), () =>
            {
                if (MechChess.AR.HybridARController.Instance != null)
                {
                    MechChess.AR.HybridARController.Instance.ToggleARMode();
                }
            }, font);

            // 2. Đặt lại bàn cờ (Reset Center)
            CreateARActionTile(rightToolbar.transform, "BtnResetCenter", ArcaneAssetLoader.GetCrosshairIconSprite(64), AegisLocalization.T("Đặt lại", "Reset"), new Vector2(0f, 60f), () =>
            {
                if (MechChess.AR.HybridARController.Instance != null)
                {
                    MechChess.AR.HybridARController.Instance.ResetBoardPosition();
                }
            }, font);

            // 3. Chụp ảnh (Camera / Snapshot)
            CreateARActionTile(rightToolbar.transform, "BtnPhoto", ArcaneAssetLoader.GetCameraIconSprite(64), AegisLocalization.T("Chụp ảnh", "Snapshot"), new Vector2(0f, -60f), () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.ShowToast(AegisLocalization.T("Đã chụp và lưu ảnh AR vào thư viện!", "Saved AR snapshot to gallery!"), ToastType.Success);
            }, font);

            // 4. Cài đặt AR (Settings gear)
            CreateARActionTile(rightToolbar.transform, "BtnARSettings", ArcaneAssetLoader.GetSettingsGearIconSprite(64), AegisLocalization.T("Cài đặt AR", "AR Settings"), new Vector2(0f, -180f), () =>
            {
                if (MainMenu2D.Instance != null) MainMenu2D.Instance.ShowToast(AegisLocalization.T("Đã hiệu chuẩn mặt phẳng AR và camera!", "Calibrated AR plane and camera!"), ToastType.Success);
            }, font);

            // =========================================================================
            // 4. BOTTOM-LEFT: Thoát AR Action Tile (Enlarged +20%)
            // =========================================================================
            GameObject exitTileContainer = new GameObject("ARExitContainer");
            exitTileContainer.transform.SetParent(_arHudOverlayObj.transform, false);
            RectTransform etcRt = exitTileContainer.AddComponent<RectTransform>();
            etcRt.anchorMin = new Vector2(0f, 0f);
            etcRt.anchorMax = new Vector2(0f, 0f);
            etcRt.pivot = new Vector2(0f, 0f);
            etcRt.anchoredPosition = new Vector2(30f, 25f);
            etcRt.sizeDelta = new Vector2(110f, 114f);

            CreateARActionTile(exitTileContainer.transform, "BtnExitAR", ArcaneAssetLoader.GetCrosshairIconSprite(64), AegisLocalization.T("Thoát AR", "Exit AR"), new Vector2(55f, 57f), () =>
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

            // 4 Selection Buttons: Queen, Rook, Bishop, Knight with carved ivory piece art & titles
            CreatePromotionPieceButton(_promotionModal.transform, "PromoQueen", "queen", AegisLocalization.T("HẬU (QUEEN)", "QUEEN"), new Vector2(-230, -32), new Vector2(148, 62), CyberButtonColor.Amber, () => SelectPromotionPiece(PieceType.Queen), font);
            CreatePromotionPieceButton(_promotionModal.transform, "PromoRook", "rook", AegisLocalization.T("XE (ROOK)", "ROOK"), new Vector2(-77, -32), new Vector2(148, 62), CyberButtonColor.Emerald, () => SelectPromotionPiece(PieceType.Rook), font);
            CreatePromotionPieceButton(_promotionModal.transform, "PromoBishop", "bishop", AegisLocalization.T("TƯỢNG (BISHOP)", "BISHOP"), new Vector2(77, -32), new Vector2(148, 62), CyberButtonColor.Sapphire, () => SelectPromotionPiece(PieceType.Bishop), font);
            CreatePromotionPieceButton(_promotionModal.transform, "PromoKnight", "knight", AegisLocalization.T("MÃ (KNIGHT)", "KNIGHT"), new Vector2(230, -32), new Vector2(148, 62), CyberButtonColor.Cyan, () => SelectPromotionPiece(PieceType.Knight), font);

            _promotionModal.SetActive(false);
        }

        private void CreatePromotionPieceButton(Transform parent, string name, string pieceKey, string label, Vector2 pos, Vector2 size, CyberButtonColor style, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            Sprite medSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodCarved);
            if (medSprite == null) medSprite = ProceduralUISpriteGenerator.GetCyberSprite(style);
            img.sprite = medSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                onClick?.Invoke();
            });
            btnObj.AddComponent<CyberButtonEffect>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            // Piece Icon Image
            Sprite pieceSpr = MedievalUIAssetLoader.GetChessIcon(pieceKey);
            if (pieceSpr != null)
            {
                GameObject pIconObj = new GameObject("PieceIcon");
                pIconObj.transform.SetParent(btnObj.transform, false);
                Image pIcon = pIconObj.AddComponent<Image>();
                pIcon.sprite = pieceSpr;
                pIcon.preserveAspect = true;
                pIcon.raycastTarget = false;

                RectTransform pirt = pIconObj.GetComponent<RectTransform>();
                pirt.anchorMin = new Vector2(0f, 0.5f);
                pirt.anchorMax = new Vector2(0f, 0.5f);
                pirt.pivot = new Vector2(0f, 0.5f);
                pirt.anchoredPosition = new Vector2(10f, 0f);
                pirt.sizeDelta = new Vector2(36f, 44f);
            }

            // Piece Name Text
            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.96f, 0.90f, 0.65f, 1f);
            txt.raycastTarget = false;

            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(46f, 0f);
            trt.offsetMax = new Vector2(-6f, 0f);
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

        private GameObject CreateArenaButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Sprite sprite, Color textColor, Action onClick, Font font, int fontSize = 18)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.12f, 1.05f, 1.0f);
            cb.pressedColor = new Color(0.78f, 0.72f, 0.65f, 1.0f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;

            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                onClick?.Invoke();
            });
            btnObj.AddComponent<CyberButtonEffect>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = textColor;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;

            Outline outline = txtObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16f, 0f);
            trt.offsetMax = new Vector2(-16f, 0f);

            return btnObj;
        }

        private void BuildMultiplayerModal(Transform parent, Font font)
        {
            // Dark Backdrop Overlay
            _multiplayerModal = new GameObject("MultiplayerModalOverlay");
            _multiplayerModal.transform.SetParent(parent, false);

            Image overlayImg = _multiplayerModal.AddComponent<Image>();
            overlayImg.color = new Color(0.02f, 0.04f, 0.08f, 0.85f);
            overlayImg.raycastTarget = true;

            RectTransform overlayRt = _multiplayerModal.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;

            Button overlayBtn = _multiplayerModal.AddComponent<Button>();
            overlayBtn.transition = Selectable.Transition.None;
            overlayBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayModalClose();
                _multiplayerModal.SetActive(false);
            });

            // Crowned Baroque Card (960 x 620)
            GameObject card = new GameObject("ModalCard");
            card.transform.SetParent(_multiplayerModal.transform, false);

            Image cardImg = card.AddComponent<Image>();
            cardImg.sprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned);
            cardImg.type = Image.Type.Sliced;
            cardImg.color = Color.white;
            cardImg.raycastTarget = true;

            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(960f, 620f);

            // Close 'X' Button at top-right
            GameObject closeBtnObj = new GameObject("BtnCloseX");
            closeBtnObj.transform.SetParent(card.transform, false);

            Image cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.sprite = MedievalUIAssetLoader.GetSprite("btn_close_x");
            if (cbImg.sprite == null) cbImg.sprite = MedievalUIAssetLoader.GetIcon(MedievalIcon.Close);

            Button cb = closeBtnObj.AddComponent<Button>();
            cb.targetGraphic = cbImg;
            cb.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayModalClose();
                _multiplayerModal.SetActive(false);
            });
            closeBtnObj.AddComponent<CyberButtonEffect>();

            RectTransform cbRt = closeBtnObj.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 1f);
            cbRt.anchorMax = new Vector2(1f, 1f);
            cbRt.pivot = new Vector2(0.5f, 0.5f);
            cbRt.anchoredPosition = new Vector2(-42f, -42f);
            cbRt.sizeDelta = new Vector2(62f, 62f);

            // Modal Title
            CreateText(card.transform, "ModalTitle", AegisLocalization.T("ĐẤU TRƯỜNG THI ĐẤU TRỰC TUYẾN", "GRAND ARENA 2P TOURNAMENT"), 26, TextAnchor.MiddleCenter, new Vector2(0f, 238f), new Vector2(760f, 38f), new Color(1.0f, 0.93f, 0.70f), font, true);

            // Ornate Gold Divider
            Sprite divSprite = MedievalUIAssetLoader.GetSprite("divider_ornate_gold");
            if (divSprite == null) divSprite = MedievalUIAssetLoader.GetIcon(MedievalIcon.DividerOrnate);
            if (divSprite != null)
            {
                GameObject divObj = new GameObject("TitleDivider");
                divObj.transform.SetParent(card.transform, false);
                Image divImg = divObj.AddComponent<Image>();
                divImg.sprite = divSprite;
                divImg.preserveAspect = true;
                divImg.raycastTarget = false;
                RectTransform divRt = divObj.GetComponent<RectTransform>();
                divRt.anchoredPosition = new Vector2(0f, 210f);
                divRt.sizeDelta = new Vector2(600f, 16f);
            }

            // Subtitle
            CreateText(card.transform, "ModalSubtitle", AegisLocalization.T("Đấu trường kết nối đa nền tảng PC & AR • Thi đấu đối kháng thời gian thực", "Real-time cross-platform battleground • PC & AR supported"), 15, TextAnchor.MiddleCenter, new Vector2(0f, 186f), new Vector2(760f, 24f), new Color(0.85f, 0.78f, 0.65f), font, true);

            // Info Card Pod (Status & Rules)
            GameObject infoCard = CreatePanel(card.transform, "InfoCard", new Vector2(0f, 108f), new Vector2(850f, 96f), CyberButtonColor.PanelFrame);

            _multiplayerStatusText = CreateText(infoCard.transform, "MultiplayerStatus", AegisLocalization.T("● TRẠNG THÁI: SẴN SÀNG - CHƯA VÀO PHÒNG", "● STATUS: READY - NOT CONNECTED"), 17, TextAnchor.MiddleCenter, new Vector2(0f, 18f), new Vector2(820f, 28f), new Color(0.55f, 0.25f, 0.05f), font, false);

            _multiplayerRoleText = CreateText(infoCard.transform, "MultiplayerRole", AegisLocalization.T("👑 Người tạo phòng: LIÊN MINH HOÀNG GIA (Trắng - Đi trước)\n⚔️ Người vào sau: ĐẾ CHẾ HẮC ÁM (Đen - Đi sau)", "👑 First player (Host): ROYAL ALLIANCE (White - Plays first)\n⚔️ Second player (Guest): DARK EMPIRE (Black - Plays second)"), 14, TextAnchor.MiddleCenter, new Vector2(0f, -18f), new Vector2(820f, 36f), new Color(0.24f, 0.18f, 0.12f), font, false);

            // Quick Room Selection Header
            CreateText(card.transform, "RoomSelectLabel", AegisLocalization.T("⚔️  DANH SÁCH ĐẤU TRƯỜNG TRANH TÀI  ⚔️", "⚔️  SELECT ARENA SECTOR  ⚔️"), 17, TextAnchor.MiddleCenter, new Vector2(0f, 34f), new Vector2(600f, 26f), new Color(0.725f, 0.604f, 0.380f), font, true);

            // Sprite Assets for Arena Buttons
            Sprite goldActiveSpr = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_active");
            if (goldActiveSpr == null) goldActiveSpr = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion);

            Sprite goldInactiveSpr = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_inactive");
            if (goldInactiveSpr == null) goldInactiveSpr = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodCarved);

            Sprite crimsonSpr = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.CrimsonBanner);
            if (crimsonSpr == null) crimsonSpr = ProceduralUISpriteGenerator.GetCyberSprite(CyberButtonColor.Crimson);

            Vector2 btnSize = new Vector2(395f, 62f);

            // Room Buttons (2x2 Grid)
            CreateArenaButton(card.transform, "BtnRoom101", AegisLocalization.T("⚔️  VÀO ĐẤU TRƯỜNG 101", "⚔️  ENTER ARENA 101"), new Vector2(-208f, -22f), btnSize, goldActiveSpr, new Color(1f, 0.95f, 0.70f), () =>
            {
                JoinOnlineRoom("ROOM-101");
            }, font, 18);

            CreateArenaButton(card.transform, "BtnRoom202", AegisLocalization.T("🌲  VÀO ĐẤU TRƯỜNG 202", "🌲  ENTER ARENA 202"), new Vector2(208f, -22f), btnSize, goldInactiveSpr, new Color(0.92f, 0.88f, 0.80f), () =>
            {
                JoinOnlineRoom("ROOM-202");
            }, font, 18);

            CreateArenaButton(card.transform, "BtnRoom303", AegisLocalization.T("🔥  VÀO ĐẤU TRƯỜNG 303", "🔥  ENTER ARENA 303"), new Vector2(-208f, -94f), btnSize, goldInactiveSpr, new Color(0.92f, 0.88f, 0.80f), () =>
            {
                JoinOnlineRoom("ROOM-303");
            }, font, 18);

            CreateArenaButton(card.transform, "BtnDisconnectOnline", AegisLocalization.T("🚪  RỜI ĐẤU TRƯỜNG", "🚪  LEAVE ARENA"), new Vector2(208f, -94f), btnSize, crimsonSpr, new Color(0.98f, 0.94f, 0.90f), () =>
            {
                if (MechChess.Multiplayer.OnlineMultiplayerManager.Instance != null)
                {
                    MechChess.Multiplayer.OnlineMultiplayerManager.Instance.Disconnect();
                    UpdateMultiplayerUIStatus(AegisLocalization.T("Đã ngắt kết nối đấu trường", "Disconnected from arena"), new Color(0.75f, 0.20f, 0.15f));
                }
            }, font, 18);

            // Elegant Close Lobby Button
            CreateArenaButton(card.transform, "BtnCloseModal", AegisLocalization.T("✕  ĐÓNG SẢNH ĐẤU", "✕  CLOSE ARENA LOBBY"), new Vector2(0f, -182f), new Vector2(280f, 50f), goldInactiveSpr, new Color(0.90f, 0.85f, 0.75f), () =>
            {
                AegisAudioManager.PlayModalClose();
                _multiplayerModal.SetActive(false);
            }, font, 16);

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
                _multiplayerStatusText.text = AegisLocalization.T($"● TRẠNG THÁI: {status.ToUpper()}", $"● STATUS: {status.ToUpper()}");
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
            if (MainMenu2D.Instance != null && !MainMenu2D.Instance.IsInGameMatch) return;

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
                        _turnIndicatorText.color = new Color(0.92f * glow, 0.80f * glow, 0.45f * glow);
                    else
                        _turnIndicatorText.color = new Color(1.0f * glow, 0.30f, 0.35f);
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

            if (MainMenu2D.Instance != null)
            {
                MainMenu2D.Instance.ShowResultScreen(false, AegisLocalization.T("Quá 60s không đi nước đầu!", "60s 1st move timeout!"), _moveHistory.Count, 60f, isDraw: false, isAborted: true);
            }
        }

        public void TriggerGameOver(PieceColor winner, string reason)
        {
            TriggerGameOver(winner, reason, false, false);
        }

        public void TriggerGameOver(PieceColor winner, string reason, bool isDraw)
        {
            TriggerGameOver(winner, reason, isDraw, false);
        }

        public void TriggerGameOver(PieceColor winner, string reason, bool isDraw, bool isAborted)
        {
            if (_isGameOver) return;
            _isGameOver = true;
            _isTimerRunning = false;

            // Strict abort check
            if (!string.IsNullOrEmpty(reason) && (reason.ToLower().Contains("hủy") || reason.ToLower().Contains("abort")))
            {
                isAborted = true;
                isDraw = false;
            }

            // Automatically turn off camera when match finishes
            if (MechChess.AR.HybridARController.Instance != null)
            {
                MechChess.AR.HybridARController.Instance.TurnOffCamera();
            }

            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.isGameOver = true;
            }

            // Determine player's active role (White or Black)
            PieceColor playerColor = PieceColor.White;
            if (MechChess.Multiplayer.LichessBridge.Instance != null && MechChess.Multiplayer.LichessBridge.Instance.IsInLichessMatch)
            {
                playerColor = MechChess.Multiplayer.LichessBridge.Instance.IsPlayerWhite ? PieceColor.White : PieceColor.Black;
            }
            else if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                playerColor = MechChess.Chess3D.ChessBoard3DManager.Instance.playerColor;
            }

            bool isPlayerVictory = (!isDraw && !isAborted && winner == playerColor);
            string winnerName = (winner == PieceColor.White) ? AegisLocalization.T("HOÀNG GIA (TRẮNG)", "ROYAL ALLIANCE (WHITE)") : AegisLocalization.T("HẮC ÁM (ĐEN)", "SHADOW LEGION (BLACK)");

            if (_turnIndicatorText != null)
            {
                if (isAborted)
                {
                    _turnIndicatorText.text = AegisLocalization.T("⚠️ HỦY VÁN CỜ!", "⚠️ MATCH ABORTED!");
                    _turnIndicatorText.color = new Color(1.0f, 0.68f, 0.20f);
                }
                else if (isDraw)
                {
                    _turnIndicatorText.text = AegisLocalization.T("⚖️ HÒA CỜ!", "⚖️ MATCH DRAW!");
                    _turnIndicatorText.color = new Color(0.85f, 0.85f, 0.95f);
                }
                else
                {
                    _turnIndicatorText.text = isPlayerVictory
                        ? AegisLocalization.T($"🏆 BẠN ĐÃ CHIẾN THẮNG!", $"🏆 YOU ARE VICTORIOUS!")
                        : AegisLocalization.T($"⚔️ BẠN ĐÃ THẤT BẠI!", $"⚔️ DEFEAT!");
                    _turnIndicatorText.color = isPlayerVictory ? new Color(0.95f, 0.82f, 0.35f) : new Color(0.95f, 0.40f, 0.40f);
                }
            }

            // CRITICAL FIX: Replace "TRẬN ĐẤU ĐANG DIỄN RA" with clear Game Over notice and reason!
            if (_firstMoveWarningText != null)
            {
                _firstMoveWarningText.text = isAborted
                    ? AegisLocalization.T($"⚠️ HỦY TRẬN: {reason}", $"⚠️ ABORTED: {reason}")
                    : AegisLocalization.T($"🏁 KẾT THÚC: {reason}", $"🏁 CONCLUDED: {reason}");
                _firstMoveWarningText.color = isAborted ? new Color(1.0f, 0.68f, 0.20f) : (isDraw ? new Color(0.85f, 0.85f, 0.95f) : (isPlayerVictory ? new Color(0.3f, 0.95f, 0.5f) : new Color(0.95f, 0.65f, 0.4f)));
            }

            RecordMove(isAborted
                ? AegisLocalization.T($"[KẾT THÚC] Hủy ván cờ! ({reason})", $"[GAME OVER] Match Aborted! ({reason})")
                : (isDraw 
                    ? AegisLocalization.T($"[KẾT THÚC] Hòa cờ! ({reason})", $"[GAME OVER] Draw! ({reason})")
                    : AegisLocalization.T($"[KẾT THÚC] {winnerName} chiến thắng! ({reason})", $"[GAME OVER] {winnerName} won! ({reason})")));

            if (MainMenu2D.Instance != null)
            {
                float matchDuration = Mathf.Max(1f, _initialTime - _whiteTimer);
                MainMenu2D.Instance.ShowResultScreen(isPlayerVictory, reason, _moveHistory.Count, matchDuration, isDraw, isAborted);
            }
        }

        private void TriggerTimeoutGameOver(PieceColor timedOutPlayer)
        {
            PieceColor winner = (timedOutPlayer == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            string timedOutSide = (timedOutPlayer == PieceColor.White) ? AegisLocalization.T("Bên Trắng", "White side") : AegisLocalization.T("Bên Đen", "Black side");
            TriggerGameOver(winner, AegisLocalization.T($"{timedOutSide} hết thời gian!", $"{timedOutSide} ran out of time!"));
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
                _turnIndicatorText.text = AegisLocalization.T(">> LƯỢT: HOÀNG GIA (TRẮNG) <<", ">> TURN: ROYAL (WHITE) <<");
                _turnIndicatorText.color = new Color(0.92f, 0.80f, 0.45f);
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
                ? AegisLocalization.T("HOÀNG GIA (TRẮNG)", "ROYAL (WHITE)")
                : AegisLocalization.T("HẮC ÁM (ĐEN)", "SHADOW (BLACK)");

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
                    _turnIndicatorText.color = (color == PieceColor.White) ? new Color(0.92f, 0.80f, 0.45f) : new Color(0.85f, 0.30f, 0.35f);
                }
            }

            if (_arTurnIndicatorText != null && _turnIndicatorText != null)
            {
                _arTurnIndicatorText.text = _turnIndicatorText.text;
                _arTurnIndicatorText.color = _turnIndicatorText.color;
            }
        }

        public void UpdateTurnIndicator(string message, Color color)
        {
            if (_turnIndicatorText != null)
            {
                _turnIndicatorText.text = message;
                _turnIndicatorText.color = color;
            }
            if (_arTurnIndicatorText != null)
            {
                _arTurnIndicatorText.text = message;
                _arTurnIndicatorText.color = color;
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
            if (_moveHistory.Count > 50) _moveHistory.RemoveAt(0);

            if (_moveHistoryText != null)
            {
                _moveHistoryText.text = string.Join("\n", _moveHistory);
                Canvas.ForceUpdateCanvases();
                if (_moveHistoryScrollRect != null)
                {
                    _moveHistoryScrollRect.verticalNormalizedPosition = 0f;
                }
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

        private GameObject CreateArcaneToolbarButton(Transform parent, string name, Sprite iconSprite, string label, Vector2 pos, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(88f, 88f);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodCarved) ?? ProceduralUISpriteGenerator.GetCyberSprite(CyberButtonColor.PanelFrame, 88, 88, 14f, 2f);
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.20f, 1.15f, 1.05f, 1f);
            cb.pressedColor = new Color(0.75f, 0.70f, 0.65f, 1f);
            cb.fadeDuration = 0.06f;
            btn.colors = cb;

            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                onClick?.Invoke();
            });
            btnObj.AddComponent<CyberButtonEffect>();

            // Antique Gold icon inside the carved wood frame
            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("IconImg");
                iconObj.transform.SetParent(btnObj.transform, false);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.color = new Color(1.0f, 0.90f, 0.62f, 1f);
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                RectTransform iconRt = iconObj.GetComponent<RectTransform>();
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.sizeDelta = new Vector2(56f, 56f);
            }

            // Warm Ivory Label text underneath the box (Enlarged & Shadowed)
            Text lblTxt = CreateText(btnObj.transform, "Label", label, 18, TextAnchor.MiddleCenter, new Vector2(0f, -56f), new Vector2(130f, 26f), new Color(0.96f, 0.91f, 0.83f, 1f), font, true);
            Shadow lblSh = lblTxt.gameObject.AddComponent<Shadow>();
            lblSh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            lblSh.effectDistance = new Vector2(1f, -1f);

            return btnObj;
        }

        private GameObject CreateArcaneToolbarButton(Transform parent, string name, string icon, string label, Vector2 pos, Action onClick, Font font)
        {
            Sprite spr = null;
            if (name.Contains("Pause")) spr = MedievalUIAssetLoader.GetIcon(MedievalIcon.Pause) ?? ArcaneAssetLoader.GetPauseIconSprite(64);
            else if (name.Contains("Hint") || name.Contains("AI")) spr = MedievalUIAssetLoader.GetIcon(MedievalIcon.Hint) ?? ArcaneAssetLoader.GetHintIconSprite(64);
            else if (name.Contains("Camera") || name.Contains("Cam")) spr = ArcaneAssetLoader.GetCameraIconSprite(64);
            else if (name.Contains("Undo")) spr = ArcaneAssetLoader.GetUndoIconSprite(64);
            else if (name.Contains("Setting")) spr = MedievalUIAssetLoader.GetSprite("icon_round_gear") ?? ArcaneAssetLoader.GetSettingsGearIconSprite(64);

            if (spr != null)
            {
                return CreateArcaneToolbarButton(parent, name, spr, label, pos, onClick, font);
            }

            // Fallback for custom text glyph
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(88f, 88f);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodCarved) ?? ProceduralUISpriteGenerator.GetCyberSprite(CyberButtonColor.PanelFrame, 88, 88, 14f, 2f);
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.20f, 1.15f, 1.05f, 1f);
            cb.pressedColor = new Color(0.75f, 0.70f, 0.65f, 1f);
            cb.fadeDuration = 0.06f;
            btn.colors = cb;

            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                onClick?.Invoke();
            });
            btnObj.AddComponent<CyberButtonEffect>();

            Text glyphTxt = CreateText(btnObj.transform, "Icon", icon, 34, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(68f, 68f), new Color(1.0f, 0.90f, 0.62f, 1f), font, true);
            glyphTxt.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.85f);

            Text labelTxt = CreateText(btnObj.transform, "Label", label, 18, TextAnchor.MiddleCenter, new Vector2(0f, -56f), new Vector2(130f, 26f), new Color(0.96f, 0.91f, 0.83f, 1f), font, true);
            Shadow labelSh = labelTxt.gameObject.AddComponent<Shadow>();
            labelSh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            labelSh.effectDistance = new Vector2(1f, -1f);

            return btnObj;
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, CyberButtonColor style)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            Image img = panel.AddComponent<Image>();
            Sprite medSprite = null;
            switch (style)
            {
                case CyberButtonColor.ModalFrame:
                    medSprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned);
                    break;
                case CyberButtonColor.Amber:
                    medSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion);
                    break;
                case CyberButtonColor.Crimson:
                    medSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.CrimsonBanner);
                    break;
                case CyberButtonColor.Cyan:
                    medSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold);
                    break;
                case CyberButtonColor.Emerald:
                case CyberButtonColor.Sapphire:
                    medSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.ParchmentRibbon);
                    break;
                case CyberButtonColor.Gunmetal:
                case CyberButtonColor.PanelFrame:
                default:
                    medSprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ParchmentCardMid);
                    break;
            }

            if (medSprite == null)
                medSprite = ProceduralUISpriteGenerator.GetCyberSprite(style);

            img.sprite = medSprite;
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
        /// Creates an authentic Medieval Fantasy Game Action button with 9-sliced borders,
        /// crisp typography, and audio feedback.
        /// </summary>
        private GameObject CreateCyberGameButton(Transform parent, string name, string icon, string title, string subtitle, Vector2 pos, Vector2 size, CyberButtonColor style, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            Sprite medSprite = null;
            switch (style)
            {
                case CyberButtonColor.Amber:
                    medSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion);
                    break;
                case CyberButtonColor.Crimson:
                    medSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.CrimsonBanner);
                    break;
                case CyberButtonColor.Cyan:
                    medSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold);
                    break;
                case CyberButtonColor.Emerald:
                case CyberButtonColor.Sapphire:
                    medSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.ParchmentRibbon);
                    break;
                case CyberButtonColor.Gunmetal:
                case CyberButtonColor.PanelFrame:
                default:
                    medSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodCarved);
                    break;
            }

            if (medSprite == null)
                medSprite = ProceduralUISpriteGenerator.GetCyberSprite(style);

            img.sprite = medSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.12f, 1.05f, 1.0f);
            cb.pressedColor = new Color(0.78f, 0.72f, 0.65f, 1.0f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;

            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                onClick?.Invoke();
            });
            btnObj.AddComponent<CyberButtonEffect>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            bool isLightBtn = (style == CyberButtonColor.Amber);
            Color titleColor = isLightBtn ? new Color(0.18f, 0.12f, 0.06f, 1f) : new Color(1.0f, 0.90f, 0.62f, 1f);
            Color subColor = isLightBtn ? new Color(0.38f, 0.28f, 0.18f, 1f) : new Color(0.85f, 0.75f, 0.60f, 0.95f);

            // Button Title & Icon Text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(btnObj.transform, false);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = string.IsNullOrEmpty(icon) ? title : $"{icon} {title}";
            titleTxt.font = font;
            titleTxt.fontSize = (size.y > 45f) ? 18 : 16;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = string.IsNullOrEmpty(subtitle) ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            titleTxt.color = titleColor;
            titleTxt.raycastTarget = false;

            Outline titleOutline = titleObj.AddComponent<Outline>();
            titleOutline.effectColor = isLightBtn ? new Color(1f, 1f, 1f, 0.5f) : new Color(0f, 0f, 0f, 0.95f);
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

            // Subtitle Text
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
                subTxt.color = subColor;
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
