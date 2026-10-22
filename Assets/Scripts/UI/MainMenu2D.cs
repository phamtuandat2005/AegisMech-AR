using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MechChess.ProceduralMech;
using MechChess.Multiplayer;

namespace MechChess.UI
{
    public enum ToastType
    {
        Success, // Emerald Green checkmark
        Error,   // Crimson Red cross
        Info,    // Cyan Info icon
        Warning  // Fiery Amber Warning icon
    }

    /// <summary>
    /// Interactive button effect for Arcane Sci-Fi UI elements with smooth scale, hover highlight, and click punch.
    /// </summary>
    public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private RectTransform _rect;
        private Vector3 _originalScale = Vector3.one;
        public float hoverScale = 1.035f;
        public float clickScale = 0.96f;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (_rect != null) _originalScale = _rect.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_rect == null) return;
            StopAllCoroutines();
            StartCoroutine(AnimateScale(hoverScale, 0.08f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_rect == null) return;
            StopAllCoroutines();
            StartCoroutine(AnimateScale(1.0f, 0.1f));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_rect == null) return;
            StopAllCoroutines();
            StartCoroutine(AnimateScale(clickScale, 0.04f));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_rect == null) return;
            StopAllCoroutines();
            StartCoroutine(AnimateScale(hoverScale, 0.06f));
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
    }

    /// <summary>
    /// MainMenu2D: Complete redesign adhering to the 15-screen UI design board:
    /// Screen 1: Màn hình khởi động (Splash Screen with animated progress bar)
    /// Screen 2: Màn hình chính (Main Menu with Citadel background, Champion dialogue, Left glassy nav)
    /// Screen 3: Chọn chế độ chơi (Mode Select with 4 feature cards + Online Ranked)
    /// Screen 4: Chọn độ khó AI (AI Select with Paladin portrait, 4 Elo levels, time & side selector, golden CTA)
    /// Screen 5: Cài đặt (Settings modal with 5 category tabs and toggles)
    /// Screen 6: Hướng dẫn (Tutorial modal with 2D board diagram & numbered steps)
    /// Screen 7: Cửa hàng (Shop modal with 4 chess piece skins and currency)
    /// Screen 10: Tạm dừng (Pause modal)
    /// Screen 11: Kết quả trận đấu (Result screen with Victory Knight, player stats, action CTAs)
    /// Screen 12: Hồ sơ người chơi (Profile modal with rank stats, match history, account settings)
    /// Screen 13: Màn hình Học cờ (Learn Chess with Sicilian defense opening diagram & lessons)
    /// Screen 14: Thông báo / Pop-up (Confirmation dialog)
    /// Screen 15: UI Phụ (Toast notification system & piece tooltip)
    /// + Complete Online Multiplayer Matchmaking & Gmail authentication integration.
    /// </summary>
    public class MainMenu2D : MonoBehaviour
    {
        public static MainMenu2D Instance { get; private set; }

        // Root Canvas & Scaler
        private GameObject _menuCanvasObj;
        private CanvasScaler _menuScaler;
        private float _lastMenuAspect = -1f;

        // Visual Theme Palette (Dark Fantasy Medieval Chess)
        private readonly Color ColBg = new Color(0.090f, 0.137f, 0.176f, 1f);        // #17232D Midnight
        private readonly Color ColGlassFill = new Color(0.12f, 0.16f, 0.20f, 0.94f);  // Carved stone slate fill
        private readonly Color ColGlassActive = new Color(0.278f, 0.208f, 0.169f, 0.95f); // #47352B Antique Brown / Bronze
        private readonly Color ColCyanBorder = new Color(0.725f, 0.604f, 0.380f, 0.95f);  // #B99A61 Antique Gold Border
        private readonly Color ColCyanGlow = new Color(0.92f, 0.80f, 0.45f, 1f);       // Radiant Antique Gold
        private readonly Color ColMutedBorder = new Color(0.35f, 0.28f, 0.22f, 0.85f); // Weathered Bronze Border
        private readonly Color ColGoldCta = new Color(0.725f, 0.604f, 0.380f, 1f);      // Imperial Antique Gold CTA
        private readonly Color ColGoldBorder = new Color(0.92f, 0.80f, 0.45f, 1f);    // Radiant Gold Rim
        private readonly Color ColTextWhite = new Color(0.96f, 0.94f, 0.88f, 1f);     // #F5EEDB Warm Ivory Text
        private readonly Color ColTextMuted = new Color(0.851f, 0.784f, 0.635f, 1f);    // #D9C8A2 Aged Parchment Text
        private readonly Color ColDarkBtnText = new Color(0.12f, 0.08f, 0.05f, 1f);  // Dark Antique Ebony Text

        // Distinct Fantasy Accents
        private readonly Color ColMidnight = new Color(0.090f, 0.137f, 0.176f, 1f);   // #17232D
        private readonly Color ColAntiqueBrown = new Color(0.278f, 0.208f, 0.169f, 1f); // #47352B
        private readonly Color ColAntiqueGold = new Color(0.725f, 0.604f, 0.380f, 1f);  // #B99A61
        private readonly Color ColParchment = new Color(0.851f, 0.784f, 0.635f, 1f);    // #D9C8A2
        private readonly Color ColMagicGreen = new Color(0.322f, 0.427f, 0.341f, 1f);   // #526D57
        private readonly Color ColBloodRed = new Color(0.490f, 0.224f, 0.259f, 1f);     // #7D3942

        // Screens
        private GameObject _screenSplash;
        private GameObject _screenMainMenu;
        private GameObject _screenModeSelect;

        // Modals
        private GameObject _aiDifficultyModal;
        private GameObject _settingsModal;
        private GameObject _tutorialModal;
        private GameObject _shopModal;
        private GameObject _pauseModal;
        private GameObject _resultScreenModal;
        private GameObject _profileModal;
        private GameObject _learnScreenModal;
        private GameObject _confirmDialogModal;

        // Toast & Tooltip Container
        private GameObject _toastContainer;
        private GameObject _tooltipCard;

        // Splash elements
        private RectTransform _splashBarFillRt;
        private Text _splashPercentTxt;

        // Main Menu Header References
        private Text _profileNameTxt;
        private Text _profileLevelTxt;

        // Dynamic Main Menu Navigation Selection (0: Chơi ngay, 1: Hướng dẫn, 2: Cài đặt, 3: Cửa hàng, 4: Thoát)
        private int _selectedMenuNavIndex = 0;
        private GameObject _navContainer;
        private readonly GameObject[] _navItemObjs = new GameObject[5];
        private readonly Image[] _navItemBgs = new Image[5];
        private readonly RectTransform[] _navItemRts = new RectTransform[5];
        private readonly RectTransform[] _navItemIcons = new RectTransform[5];
        private readonly Image[] _navItemIconImgs = new Image[5];
        private readonly Text[] _navItemTexts = new Text[5];
        private readonly GameObject[] _navItemChevrons = new GameObject[5];
        private readonly LayoutElement[] _navItemLayoutElements = new LayoutElement[5];

        // Settings Modal State (5 tabs, user reference design)
        private int _selectedSettingsTab = 0;
        private readonly Image[] _settingsTabBgs = new Image[5];
        private readonly Text[] _settingsTabTexts = new Text[5];
        private readonly Image[] _settingsTabIcons = new Image[5];
        private readonly GameObject[] _settingsPanels = new GameObject[5];

        // Mode Select State (4 feature cards, user reference design)
        private int _selectedModeCardIndex = 0; // 0: Đấu với máy, 1: Hai người, 2: AR, 3: Học cờ & Puzzle
        private readonly Image[] _modeCardFrameBgs = new Image[4];

        // Shop State (3 category tabs, 4 feature items, user reference design)
        private int _selectedShopTab = 0; // 0: Skin quân cờ, 1: Bàn cờ, 2: Hiệu ứng
        private readonly Image[] _shopTabBgs = new Image[3];
        private readonly Text[] _shopTabTexts = new Text[3];
        private readonly GameObject[] _shopCategoryContainers = new GameObject[3];
        private Text _shopGoldTxt;
        private int _currentGold = 1250;

        // Profile Modal State (3 tabs: Lịch sử đấu, Thành tích, Skin, user reference design)
        private int _selectedProfileTab = 0;
        private readonly Image[] _profileTabBgs = new Image[3];
        private readonly Text[] _profileTabTexts = new Text[3];
        private readonly GameObject[] _profileTabContainers = new GameObject[3];

        // Tutorial Modal State (4 tabs: Cơ bản, Luật chơi, Các quân cờ, AR; 20 pages total)
        private TutorialTabType _selectedTutorialTab = TutorialTabType.Basics;
        private int _currentTutorialPage = 0;
        private readonly Image[] _tutorialTabBgs = new Image[4];
        private readonly Text[] _tutorialTabTexts = new Text[4];
        private Image _tutorialDiagramImg;
        private Transform _tutorialPieceOverlay;
        private Text _tutorialCaptionTxt;
        private Text _tutorialBadgeTxt;
        private Text _tutorialTitleTxt;
        private Text _tutorialOverviewTxt;
        private readonly GameObject[] _tutorialStepRows = new GameObject[3];
        private readonly Text[] _tutorialStepBadges = new Text[3];
        private readonly Text[] _tutorialStepTitles = new Text[3];
        private readonly Text[] _tutorialStepDescs = new Text[3];
        private GameObject _tutorialTipContainer;
        private Text _tutorialTipTxt;
        private Text _tutorialPageIndicatorTxt;
        private Button _tutorialBtnPrev;
        private Image _tutorialBtnPrevBg;
        private Text _tutorialBtnPrevTxt;
        private Button _tutorialBtnNext;
        private Image _tutorialBtnNextBg;
        private Text _tutorialBtnNextTxt;

        // AI Selector elements
        private int _selectedAIDifficulty = 2; // 1: Dễ, 2: Trung bình, 3: Khó, 4: Rất khó
        private int _selectedAITimeIndex = 2; // 0: 3m, 1: 5m, 2: 10m, 3: 15m, 4: unlimited
        private string GetAITimeDisplay(int idx)
        {
            switch (idx)
            {
                case 0: return AegisLocalization.T("3 phút", "3 mins");
                case 1: return AegisLocalization.T("5 phút", "5 mins");
                case 2: return AegisLocalization.T("10 phút", "10 mins");
                case 3: return AegisLocalization.T("15 phút", "15 mins");
                default: return AegisLocalization.T("Không giới hạn", "Unlimited");
            }
        }
        private PieceColor _selectedAIPieceColor = PieceColor.White;
        private Image[] _aiDiffCardBgs;
        private Text[] _aiDiffCardTitles;
        private Image _radioWhiteImg;
        private Image _radioBlackImg;

        // Result Screen elements
        private Text _resultTitleTxt;
        private Text _resultMovesTxt;
        private Text _resultDurationTxt;
        private Text _resultScoreTxt;
        private Text _resultEloTxt;

        // Online Matchmaking & Auth references
        private GameObject _loginModal;
        private InputField _loginGmailInput;
        private InputField _loginUsernameInput;
        private Text _loginErrorTxt;
        private Action _postLoginAction;

        private GameObject _matchmakingModal;
        private Text _matchmakingTimerTxt;
        private Text _matchmakingStatusTxt;
        private RectTransform _radarIconRt;
        private Coroutine _matchmakingTimerCoroutine;

        private GameObject _friendModal;
        private Text _friendMyNameTxt;
        private InputField _friendTargetInput;
        private Text _friendStatusTxt;
        private Transform _onlinePlayersContainer;
        private readonly List<GameObject> _onlinePlayerRowObjs = new List<GameObject>();

        private GameObject _challengeModal;
        private Text _challengeBodyTxt;
        private string _incomingInviteId;
        private string _incomingFromUser;
        private string _incomingRoomId;

        // Confirm Dialog references
        private Text _confirmTitleTxt;
        private Text _confirmBodyTxt;
        private Action _onConfirmAction;

        public enum GameLaunchMode
        {
            VersusAI,
            LocalPassAndPlay,
            OnlineMultiplayer,
            AugmentedReality
        }
        private GameLaunchMode _lastLaunchMode = GameLaunchMode.VersusAI;
        private bool _isInGameMatch = false;
        public bool IsInGameMatch => _isInGameMatch;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }

            _isInGameMatch = false;
            AegisLocalization.LoadSavedLanguage();
            AegisLocalization.OnLanguageChanged += OnLanguageChanged;

            BuildAllScreens(false);
        }

        private void OnDestroy()
        {
            AegisLocalization.OnLanguageChanged -= OnLanguageChanged;
        }

        private Coroutine _refreshLanguageCoroutine;

        private void OnLanguageChanged()
        {
            RefreshLanguageUI();
        }

        public void RefreshLanguageUI()
        {
            if (_refreshLanguageCoroutine != null)
            {
                StopCoroutine(_refreshLanguageCoroutine);
                _refreshLanguageCoroutine = null;
            }

            if (gameObject.activeInHierarchy)
            {
                _refreshLanguageCoroutine = StartCoroutine(DoRefreshLanguageUI());
            }
            else
            {
                ExecuteRefreshLanguageUI();
            }
        }

        private IEnumerator DoRefreshLanguageUI()
        {
            // Wait 1 frame so the current UI click event (dropdown selection) finishes processing
            // cleanly in Unity's EventSystem without destroying components mid-invocation.
            yield return null;
            ExecuteRefreshLanguageUI();
            _refreshLanguageCoroutine = null;
        }

        private void ExecuteRefreshLanguageUI()
        {
            bool wasMenuVisible = _menuCanvasObj != null && _menuCanvasObj.activeSelf;
            bool isModeSelectOpen = _screenModeSelect != null && _screenModeSelect.activeSelf;
            bool isSettingsOpen = _settingsModal != null && _settingsModal.activeSelf;
            int savedSettingsTab = _selectedSettingsTab;
            bool isTutorialOpen = _tutorialModal != null && _tutorialModal.activeSelf;
            TutorialTabType savedTutorialTab = _selectedTutorialTab;
            int savedTutorialPage = _currentTutorialPage;
            bool isShopOpen = _shopModal != null && _shopModal.activeSelf;
            int savedShopTab = _selectedShopTab;
            bool isProfileOpen = _profileModal != null && _profileModal.activeSelf;
            int savedProfileTab = _selectedProfileTab;
            bool isAIDiffOpen = _aiDifficultyModal != null && _aiDifficultyModal.activeSelf;
            bool isPauseOpen = _pauseModal != null && _pauseModal.activeSelf;
            bool isResultOpen = _resultScreenModal != null && _resultScreenModal.activeSelf;
            bool isLearnOpen = _learnScreenModal != null && _learnScreenModal.activeSelf;
            int savedNavIndex = _selectedMenuNavIndex;

            if (_menuCanvasObj != null)
            {
                Destroy(_menuCanvasObj);
                _menuCanvasObj = null;
            }

            BuildAllScreens(true);

            if (_isInGameMatch)
            {
                // In game: Base menu screens and background must NEVER be shown!
                if (_screenMainMenu != null) _screenMainMenu.SetActive(false);
                if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
                if (_screenSplash != null) _screenSplash.SetActive(false);

                if (isSettingsOpen)
                {
                    if (_menuCanvasObj != null) _menuCanvasObj.SetActive(true);
                    if (_settingsModal != null) _settingsModal.SetActive(true);
                    SwitchSettingsTab(savedSettingsTab);
                }
                else if (isPauseOpen)
                {
                    if (_menuCanvasObj != null) _menuCanvasObj.SetActive(true);
                    if (_pauseModal != null) _pauseModal.SetActive(true);
                }
                else if (isResultOpen)
                {
                    if (_menuCanvasObj != null) _menuCanvasObj.SetActive(true);
                    if (_resultScreenModal != null) _resultScreenModal.SetActive(true);
                }
                else
                {
                    if (_menuCanvasObj != null) _menuCanvasObj.SetActive(false);
                }

                if (UIManager2D.Instance != null) UIManager2D.Instance.SetHUDVisible(true);
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null && MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer != null)
                {
                    MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer.gameObject.SetActive(true);
                }
            }
            else
            {
                // 1. In Main Menu: ALWAYS activate the proper base screen so there is NEVER a black void behind modals
                if (isModeSelectOpen)
                {
                    if (_screenMainMenu != null) _screenMainMenu.SetActive(false);
                    if (_screenModeSelect != null) _screenModeSelect.SetActive(true);
                }
                else
                {
                    if (_screenMainMenu != null) _screenMainMenu.SetActive(true);
                    if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
                    UpdateNavSelection(savedNavIndex);
                }

                // 2. Re-open any modal overlay that was open prior to language switch
                if (isSettingsOpen)
                {
                    if (_settingsModal != null) _settingsModal.SetActive(true);
                    SwitchSettingsTab(savedSettingsTab);
                }
                else if (isTutorialOpen)
                {
                    if (_tutorialModal != null)
                    {
                        _tutorialModal.SetActive(true);
                        _selectedTutorialTab = savedTutorialTab;
                        _currentTutorialPage = savedTutorialPage;
                        UpdateTutorialUI();
                    }
                }
                else if (isShopOpen)
                {
                    if (_shopModal != null) _shopModal.SetActive(true);
                    SwitchShopTab(savedShopTab);
                }
                else if (isProfileOpen)
                {
                    if (_profileModal != null) _profileModal.SetActive(true);
                    SwitchProfileTab(savedProfileTab);
                }
                else if (isAIDiffOpen)
                {
                    if (_aiDifficultyModal != null) _aiDifficultyModal.SetActive(true);
                }
                else if (isPauseOpen)
                {
                    if (_pauseModal != null) _pauseModal.SetActive(true);
                }
                else if (isResultOpen)
                {
                    if (_resultScreenModal != null) _resultScreenModal.SetActive(true);
                }
                else if (isLearnOpen)
                {
                    if (_learnScreenModal != null) _learnScreenModal.SetActive(true);
                }

                if (!wasMenuVisible && _menuCanvasObj != null)
                {
                    _menuCanvasObj.SetActive(false);
                }
            }

            UpdateUserProfileUI();

            ShowToast(AegisLocalization.T("Đã chuyển sang Tiếng Việt", "Switched to English"), ToastType.Success);
        }

        private void Start()
        {
            if (UserAccountManager.Instance != null)
            {
                UserAccountManager.Instance.OnAccountUpdated += (u, g, e) => UpdateUserProfileUI();
                UserAccountManager.Instance.OnLoggedOut += UpdateUserProfileUI;
                UpdateUserProfileUI();
            }

            if (OnlineMultiplayerManager.Instance != null)
            {
                OnlineMultiplayerManager.Instance.OnMatchFoundEvent += HandleMatchFound;
                OnlineMultiplayerManager.Instance.OnFriendInviteReceivedEvent += HandleIncomingChallenge;
                OnlineMultiplayerManager.Instance.OnFriendInviteDeclinedEvent += HandleInviteDeclined;
                OnlineMultiplayerManager.Instance.OnInviteErrorEvent += HandleInviteError;
                OnlineMultiplayerManager.Instance.OnOnlinePlayersUpdatedEvent += UpdateOnlinePlayersUI;
            }

            // Start with Splash Screen Loading Animation
            StartCoroutine(RunSplashSequence());
        }

        private void Update()
        {
            // Responsive canvas aspect ratio updates
            if (_menuScaler != null)
            {
                float currentAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                if (Mathf.Abs(currentAspect - _lastMenuAspect) > 0.02f)
                {
                    _lastMenuAspect = currentAspect;
                    // Better responsive breakpoints:
                    // Ultra-wide (>2.0): match height fully
                    // Landscape (>1.3): match height mostly
                    // Portrait (<1.3): balance width/height
                    // Maintain balanced 0.5 match width/height as per SKILL.md guidelines
                    _menuScaler.matchWidthOrHeight = 0.5f;
                }
            }

            // Radar pulse in matchmaking modal
            if (_radarIconRt != null && _matchmakingModal != null && _matchmakingModal.activeSelf)
            {
                _radarIconRt.Rotate(0f, 0f, -120f * Time.unscaledDeltaTime);
            }
        }

        public bool IsMenuVisible() => _menuCanvasObj != null && _menuCanvasObj.activeSelf;

        public void ShowMenu()
        {
            _isInGameMatch = false;
            CloseAllModals();
            if (_menuCanvasObj != null) _menuCanvasObj.SetActive(true);
            if (UIManager2D.Instance != null) UIManager2D.Instance.SetHUDVisible(false);
            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null && MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer.gameObject.SetActive(false);
            }
            NavigateToHome();
        }

        public void HideMenu()
        {
            _isInGameMatch = true;
            CloseAllModals();
            if (_screenMainMenu != null) _screenMainMenu.SetActive(false);
            if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
            if (_screenSplash != null) _screenSplash.SetActive(false);
            if (_menuCanvasObj != null) _menuCanvasObj.SetActive(false);

            if (UIManager2D.Instance != null) UIManager2D.Instance.SetHUDVisible(true);
            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null && MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer.gameObject.SetActive(true);
            }
        }

        // =========================================================================
        // NAVIGATION CONTROLLERS
        // =========================================================================
        public void NavigateToHome()
        {
            _isInGameMatch = false;
            CloseAllModals();
            if (_screenSplash != null) _screenSplash.SetActive(false);
            if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
            if (_screenMainMenu != null) _screenMainMenu.SetActive(true);
            if (_menuCanvasObj != null) _menuCanvasObj.SetActive(true);
            _selectedMenuNavIndex = 0;
            UpdateNavSelection(0);

            // Double safeguard: keep in-game HUD and 3D board hidden while on main menu
            if (UIManager2D.Instance != null) UIManager2D.Instance.SetHUDVisible(false);
            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null && MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer.gameObject.SetActive(false);
            }
        }

        public void NavigateToChooseMode()
        {
            _isInGameMatch = false;
            CloseAllModals();
            if (_screenMainMenu != null) _screenMainMenu.SetActive(false);
            if (_screenSplash != null) _screenSplash.SetActive(false);
            if (_screenModeSelect != null) _screenModeSelect.SetActive(true);
            if (_menuCanvasObj != null) _menuCanvasObj.SetActive(true);

            if (UIManager2D.Instance != null) UIManager2D.Instance.SetHUDVisible(false);
            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null && MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer.gameObject.SetActive(false);
            }
        }

        public void CloseAllModals()
        {
            if (_aiDifficultyModal != null) _aiDifficultyModal.SetActive(false);
            if (_settingsModal != null) _settingsModal.SetActive(false);
            if (_tutorialModal != null) _tutorialModal.SetActive(false);
            if (_shopModal != null) _shopModal.SetActive(false);
            if (_pauseModal != null) _pauseModal.SetActive(false);
            if (_resultScreenModal != null) _resultScreenModal.SetActive(false);
            if (_profileModal != null) _profileModal.SetActive(false);
            if (_learnScreenModal != null) _learnScreenModal.SetActive(false);
            if (_confirmDialogModal != null) _confirmDialogModal.SetActive(false);
            if (_loginModal != null) _loginModal.SetActive(false);
            if (_matchmakingModal != null) _matchmakingModal.SetActive(false);
            if (_friendModal != null) _friendModal.SetActive(false);
            if (_challengeModal != null) _challengeModal.SetActive(false);
        }

        // =========================================================================
        // MASTER UI BUILDER
        // =========================================================================
        private void BuildAllScreens(bool isRefresh = false)
        {
            if (EventSystem.current == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<StandaloneInputModule>();
#endif
            }

            _menuCanvasObj = new GameObject("ArcaneMenuCanvas");
            _menuCanvasObj.transform.SetParent(transform, false);

            Canvas canvas = _menuCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;

            _menuScaler = _menuCanvasObj.AddComponent<CanvasScaler>();
            _menuScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _menuScaler.referenceResolution = new Vector2(1920, 1080);
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            _menuScaler.matchWidthOrHeight = 0.5f;

            _menuCanvasObj.AddComponent<GraphicRaycaster>();
            Font font = GetDefaultFont();

            // Build Screens
            BuildScreen1Splash(_menuCanvasObj.transform, font);
            BuildScreen2MainMenu(_menuCanvasObj.transform, font);
            BuildScreen3ModeSelect(_menuCanvasObj.transform, font);
            BuildScreen4AIDifficulty(_menuCanvasObj.transform, font);
            BuildScreen5Settings(_menuCanvasObj.transform, font);
            BuildScreen6Tutorial(_menuCanvasObj.transform, font);
            BuildScreen7Shop(_menuCanvasObj.transform, font);
            BuildScreen10Pause(_menuCanvasObj.transform, font);
            BuildScreen11Result(_menuCanvasObj.transform, font);
            BuildScreen12Profile(_menuCanvasObj.transform, font);
            BuildScreen13Learn(_menuCanvasObj.transform, font);
            BuildScreen14Confirm(_menuCanvasObj.transform, font);
            BuildScreen15ToastContainer(_menuCanvasObj.transform, font);

            // Build Multiplayer & Auth Dialogs
            BuildLoginModal(_menuCanvasObj.transform, font);
            BuildMatchmakingModal(_menuCanvasObj.transform, font);
            BuildFriendModal(_menuCanvasObj.transform, font);
            BuildIncomingChallengeModal(_menuCanvasObj.transform, font);

            // Initially show Splash only if not refresh; on refresh ensure Main Menu base is active
            CloseAllModals();
            if (!isRefresh)
            {
                if (_screenMainMenu != null) _screenMainMenu.SetActive(false);
                if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
                if (_screenSplash != null) _screenSplash.SetActive(true);
            }
            else
            {
                if (_screenSplash != null) _screenSplash.SetActive(false);
                if (_screenMainMenu != null) _screenMainMenu.SetActive(true);
                if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
            }
        }

        // =========================================================================
        // SCREEN 1: SPLASH SCREEN (Animated loading bar 0-100%)
        // =========================================================================
        private void BuildScreen1Splash(Transform parent, Font font)
        {
            _screenSplash = CreateFullscreenPanel(parent, "Screen1_Splash");

            // Background
            Image bgImg = _screenSplash.AddComponent<Image>();
            Sprite splashBg = ArcaneAssetLoader.LoadArcaneSprite("splash_bg.png");
            if (splashBg != null)
            {
                bgImg.sprite = splashBg;
                bgImg.type = Image.Type.Simple;
                bgImg.preserveAspect = false;
            }
            else
            {
                bgImg.color = ColBg;
            }

            // Note: Logo "CHESS 3D AR" and "STRATEGY BEYOND REALITY" are already in the high-res artwork,
            // so we don't render duplicate title text on top.

            // Loading bar container - centered horizontally, below logo (matching Box 1 design)
            float barWidth = 700f;
            float barHeight = 12f;

            GameObject barContainer = new GameObject("LoadingBarContainer");
            barContainer.transform.SetParent(_screenSplash.transform, false);
            Image barBg = barContainer.AddComponent<Image>();
            barBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)barWidth, (int)barHeight, 6f, ColMidnight, ColAntiqueGold, 1f);
            barBg.type = Image.Type.Sliced;
            RectTransform bcRt = barContainer.GetComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0.135f);
            bcRt.anchorMax = new Vector2(0.5f, 0.135f);
            bcRt.pivot = new Vector2(0.5f, 0.5f);
            bcRt.anchoredPosition = new Vector2(-25f, 0f);
            bcRt.sizeDelta = new Vector2(barWidth, barHeight);

            // Loading bar fill
            GameObject barFill = new GameObject("LoadingBarFill");
            barFill.transform.SetParent(barContainer.transform, false);
            Image fillImg = barFill.AddComponent<Image>();
            fillImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)barWidth, (int)barHeight, 5f, ColAntiqueGold, Color.white, 0f);
            fillImg.type = Image.Type.Sliced;
            _splashBarFillRt = barFill.GetComponent<RectTransform>();
            _splashBarFillRt.anchorMin = new Vector2(0f, 0f);
            _splashBarFillRt.anchorMax = new Vector2(0f, 1f);
            _splashBarFillRt.pivot = new Vector2(0f, 0.5f);
            _splashBarFillRt.anchoredPosition = new Vector2(1f, 0f);
            _splashBarFillRt.sizeDelta = new Vector2(0f, -2f);

            // Percentage Text - right beside the loading bar (e.g. 88%)
            GameObject pctObj = new GameObject("PercentText");
            pctObj.transform.SetParent(_screenSplash.transform, false);
            _splashPercentTxt = pctObj.AddComponent<Text>();
            _splashPercentTxt.text = "0%";
            _splashPercentTxt.font = font;
            _splashPercentTxt.fontSize = 20;
            _splashPercentTxt.fontStyle = FontStyle.Bold;
            _splashPercentTxt.alignment = TextAnchor.MiddleLeft;
            _splashPercentTxt.color = ColCyanGlow;
            RectTransform pctRt = pctObj.GetComponent<RectTransform>();
            pctRt.anchorMin = new Vector2(0.5f, 0.135f);
            pctRt.anchorMax = new Vector2(0.5f, 0.135f);
            pctRt.pivot = new Vector2(0f, 0.5f);
            pctRt.anchoredPosition = new Vector2(-25f + barWidth * 0.5f + 14f, 0f);
            pctRt.sizeDelta = new Vector2(90f, 28f);

            // "Đang tải..." Text - centered directly below the loading bar
            GameObject txtObj = new GameObject("LoadingText");
            txtObj.transform.SetParent(_screenSplash.transform, false);
            Text loadingTxt = txtObj.AddComponent<Text>();
            loadingTxt.text = AegisLocalization.T("Đang tải...", "Loading...");
            loadingTxt.font = font;
            loadingTxt.fontSize = 18;
            loadingTxt.fontStyle = FontStyle.Normal;
            loadingTxt.alignment = TextAnchor.MiddleCenter;
            loadingTxt.color = ColParchment;
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0.5f, 0.135f);
            txtRt.anchorMax = new Vector2(0.5f, 0.135f);
            txtRt.pivot = new Vector2(0.5f, 0.5f);
            txtRt.anchoredPosition = new Vector2(-25f, -24f);
            txtRt.sizeDelta = new Vector2(250f, 24f);
        }

        private IEnumerator RunSplashSequence()
        {
            float duration = 1.8f;
            float elapsed = 0f;
            float maxFillWidth = 698f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                // Ease-out curve for smooth loading feel
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                if (_splashBarFillRt != null)
                {
                    _splashBarFillRt.sizeDelta = new Vector2(easedProgress * maxFillWidth, -2f);
                }
                if (_splashPercentTxt != null)
                {
                    _splashPercentTxt.text = $"{Mathf.RoundToInt(easedProgress * 100f)}%";
                }
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.3f);
            NavigateToHome();
        }

        // =========================================================================
        // SCREEN 2: MAIN MENU (Floating Citadel + Champion Dialogue + Glassy Left Nav)
        // =========================================================================
        private void BuildScreen2MainMenu(Transform parent, Font font)
        {
            _screenMainMenu = CreateFullscreenPanel(parent, "Screen2_MainMenu");

            // Citadel Background
            Image bgImg = _screenMainMenu.AddComponent<Image>();
            Sprite mainBg = ArcaneAssetLoader.LoadArcaneSprite("main_menu_bg.png");
            if (mainBg != null)
            {
                bgImg.sprite = mainBg;
                bgImg.type = Image.Type.Simple;
                bgImg.preserveAspect = false;
            }
            else
            {
                bgImg.color = ColBg;
            }


            // Top-Left Logo Badge: CHESS 3D AR (Exact User Design 100%)
            GameObject logoContainer = new GameObject("TopLeftLogo");
            logoContainer.transform.SetParent(_screenMainMenu.transform, false);
            Image logoImg = logoContainer.AddComponent<Image>();
            Sprite logoSprite = ArcaneAssetLoader.LoadArcaneSprite("hud_logo_chess_3dar.png");
            if (logoSprite != null)
            {
                logoImg.sprite = logoSprite;
                logoImg.preserveAspect = true;
            }
            else
            {
                // Fallback to procedural plate and text if sprite is not yet imported
                Sprite logoPlate = ArcaneAssetLoader.LoadArcaneSprite("hud_logo_plate.png");
                if (logoPlate != null) logoImg.sprite = logoPlate;
                else logoImg.color = new Color(0.02f, 0.08f, 0.18f, 0.85f);
            }

            RectTransform lcRt = logoContainer.GetComponent<RectTransform>();
            lcRt.anchorMin = new Vector2(0f, 1f);
            lcRt.anchorMax = new Vector2(0f, 1f);
            lcRt.pivot = new Vector2(0f, 1f);
            lcRt.anchoredPosition = new Vector2(60f, -36f);
            lcRt.sizeDelta = new Vector2(400f, 157f);

            // Add smooth hover/click interaction to the logo
            Button logoBtn = logoContainer.AddComponent<Button>();
            logoBtn.onClick.AddListener(() =>
            {
                ShowToast("AegisMech AR — Phiên bản Chess 3D AR", ToastType.Info);
            });
            logoContainer.AddComponent<MenuButtonEffect>();

            // Top-Right User Profile Status Pill
            BuildTopRightProfileHeader(_screenMainMenu.transform, font);

            // Left Navigation: Chevron Play Now + Clean Transparent Rows
            BuildLeftMainMenuNav(_screenMainMenu.transform, font);

            // Bottom-Right Champion Speech Dialogue Bubble
            BuildChampionDialogueBubble(_screenMainMenu.transform, font);
        }

        private void BuildTopRightProfileHeader(Transform parent, Font font)
        {
            GameObject headerObj = new GameObject("TopRightProfilePill");
            headerObj.transform.SetParent(parent, false);

            Image pillBg = headerObj.AddComponent<Image>();
            Sprite pillSprite = ArcaneAssetLoader.LoadArcaneSprite("hud_profile_pill.png");
            if (pillSprite != null) pillBg.sprite = pillSprite;
            else pillBg.color = new Color(0.03f, 0.08f, 0.18f, 0.85f);

            Button pillBtn = headerObj.AddComponent<Button>();
            pillBtn.onClick.AddListener(ShowProfileDetailsModal);
            headerObj.AddComponent<MenuButtonEffect>();

            RectTransform hRt = headerObj.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(1f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(1f, 1f);
            hRt.anchoredPosition = new Vector2(-60f, -36f);
            hRt.sizeDelta = new Vector2(540f, 86f);
            headerObj.transform.localScale = new Vector3(1.1f, 1.1f, 1.0f); // Phóng to 10% theo yêu cầu người dùng

            // Circular Avatar with Cyan Ring
            GameObject avObj = new GameObject("Avatar");
            avObj.transform.SetParent(headerObj.transform, false);
            Image avImg = avObj.AddComponent<Image>();
            Sprite avSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png");
            if (avSprite == null) avSprite = ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            if (avSprite != null) avImg.sprite = avSprite;
            avImg.preserveAspect = true;
            RectTransform avRt = avObj.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0f, 0.5f);
            avRt.anchorMax = new Vector2(0f, 0.5f);
            avRt.pivot = new Vector2(0f, 0.5f);
            avRt.anchoredPosition = new Vector2(12f, 0f);
            avRt.sizeDelta = new Vector2(64f, 64f);

            // Name & Level
            GameObject infoObj = new GameObject("UserInfo");
            infoObj.transform.SetParent(headerObj.transform, false);
            _profileNameTxt = infoObj.AddComponent<Text>();
            _profileNameTxt.text = "Player001";
            _profileNameTxt.font = font;
            _profileNameTxt.fontSize = 25;
            _profileNameTxt.fontStyle = FontStyle.Bold;
            _profileNameTxt.alignment = TextAnchor.MiddleLeft;
            _profileNameTxt.color = ColTextWhite;
            RectTransform infoRt = infoObj.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0f, 0.5f);
            infoRt.anchorMax = new Vector2(0f, 0.5f);
            infoRt.pivot = new Vector2(0f, 0.5f);
            infoRt.anchoredPosition = new Vector2(88f, 14f);
            infoRt.sizeDelta = new Vector2(180f, 30f);

            GameObject lvlObj = new GameObject("UserLevel");
            lvlObj.transform.SetParent(headerObj.transform, false);
            _profileLevelTxt = lvlObj.AddComponent<Text>();
            _profileLevelTxt.text = "Lv. 5";
            _profileLevelTxt.font = font;
            _profileLevelTxt.fontSize = 19;
            _profileLevelTxt.fontStyle = FontStyle.Bold;
            _profileLevelTxt.alignment = TextAnchor.MiddleLeft;
            _profileLevelTxt.color = ColAntiqueGold;
            RectTransform lvlRt = lvlObj.GetComponent<RectTransform>();
            lvlRt.anchorMin = new Vector2(0f, 0.5f);
            lvlRt.anchorMax = new Vector2(0f, 0.5f);
            lvlRt.pivot = new Vector2(0f, 0.5f);
            lvlRt.anchoredPosition = new Vector2(88f, -14f);
            lvlRt.sizeDelta = new Vector2(75f, 24f);

            // Small diamond dropdown indicator next to Lv. 5
            GameObject diamondObj = new GameObject("DiamondIndicator");
            diamondObj.transform.SetParent(headerObj.transform, false);
            Text dTxt = diamondObj.AddComponent<Text>();
            dTxt.text = "◊";
            dTxt.font = font;
            dTxt.fontSize = 20;
            dTxt.alignment = TextAnchor.MiddleCenter;
            dTxt.color = ColAntiqueGold;
            RectTransform dRt = diamondObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0.5f);
            dRt.anchorMax = new Vector2(0f, 0.5f);
            dRt.pivot = new Vector2(0f, 0.5f);
            dRt.anchoredPosition = new Vector2(165f, -14f);
            dRt.sizeDelta = new Vector2(22f, 22f);

            // 3 Quick Action Icons (Mail, Reward with red dot, Settings) inside recessed right sub-panel
            CreateHeaderIconButton(headerObj.transform, "BtnMail", "icon_nav_mail.png", "✉", false, new Vector2(355f, 0f), () => ShowToast(AegisLocalization.T("Không có thư mới.", "No new mail."), ToastType.Info), font);
            CreateHeaderIconButton(headerObj.transform, "BtnInbox", "icon_nav_reward.png", "★", true, new Vector2(415f, 0f), () => ShowToast(AegisLocalization.T("1 phần thưởng chưa nhận!", "1 unclaimed reward!"), ToastType.Success), font);
            CreateHeaderIconButton(headerObj.transform, "BtnQuickSettings", "icon_nav_settings.png", "⚙", false, new Vector2(475f, 0f), OpenSettingsModal, font);
        }

        private void CreateHeaderIconButton(Transform parent, string name, string spriteName, string fallbackGlyph, bool showRedBadge, Vector2 pos, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(46, 46, 10f, new Color(0.06f, 0.14f, 0.26f, 0.85f), ColCyanBorder, 1.5f);
            bg.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            btnObj.AddComponent<MenuButtonEffect>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(46f, 46f);

            Sprite iconSpr = null;
            if (!string.IsNullOrEmpty(spriteName))
            {
                iconSpr = ArcaneAssetLoader.LoadArcaneSprite(spriteName);
            }

            if (iconSpr != null)
            {
                GameObject imgObj = new GameObject("Icon");
                imgObj.transform.SetParent(btnObj.transform, false);
                Image icImg = imgObj.AddComponent<Image>();
                icImg.sprite = iconSpr;
                icImg.color = ColTextWhite;
                icImg.preserveAspect = true;
                icImg.raycastTarget = false;
                RectTransform irt = imgObj.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0.5f, 0.5f);
                irt.anchorMax = new Vector2(0.5f, 0.5f);
                irt.pivot = new Vector2(0.5f, 0.5f);
                irt.anchoredPosition = Vector2.zero;
                irt.sizeDelta = new Vector2(26f, 26f);
            }
            else
            {
                GameObject txtObj = new GameObject("Icon");
                txtObj.transform.SetParent(btnObj.transform, false);
                Text t = txtObj.AddComponent<Text>();
                t.text = fallbackGlyph;
                t.font = font;
                t.fontSize = 22;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = ColTextWhite;
                RectTransform trt = txtObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
            }

            if (showRedBadge)
            {
                GameObject badge = new GameObject("RedBadge");
                badge.transform.SetParent(btnObj.transform, false);
                Image bImg = badge.AddComponent<Image>();
                bImg.sprite = ArcaneAssetLoader.GetCircleSprite(14, new Color(1f, 0.25f, 0.25f, 1f), Color.white, 1f);
                RectTransform brt = badge.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1f, 1f);
                brt.anchorMax = new Vector2(1f, 1f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2(-3f, -3f);
                brt.sizeDelta = new Vector2(14f, 14f);
            }
        }

        private void BuildLeftMainMenuNav(Transform parent, Font font)
        {
            // Dark Frosted Glass Dock Backing for Left Navigation (ensures high contrast over bright cloud backgrounds)
            GameObject dockBacking = new GameObject("NavDockBacking");
            dockBacking.transform.SetParent(parent, false);
            RectTransform dbRt = dockBacking.AddComponent<RectTransform>();
            dbRt.anchorMin = new Vector2(0f, 1f);
            dbRt.anchorMax = new Vector2(0f, 1f);
            dbRt.pivot = new Vector2(0f, 1f);
            dbRt.anchoredPosition = new Vector2(40f, -205f);
            dbRt.sizeDelta = new Vector2(420f, 495f);

            Image dbImg = dockBacking.AddComponent<Image>();
            dbImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(420, 495, 24f, ColMidnight, new Color(0.725f, 0.604f, 0.380f, 0.35f), 1.5f);
            dbImg.type = Image.Type.Sliced;
            dbImg.raycastTarget = false;

            _navContainer = new GameObject("LeftNavigationStack");
            _navContainer.transform.SetParent(parent, false);
            RectTransform ncRt = _navContainer.AddComponent<RectTransform>();
            ncRt.anchorMin = new Vector2(0f, 1f);
            ncRt.anchorMax = new Vector2(0f, 1f);
            ncRt.pivot = new Vector2(0f, 1f);
            ncRt.anchoredPosition = new Vector2(60f, -220f);
            ncRt.sizeDelta = new Vector2(480f, 480f);

            VerticalLayoutGroup vlg = _navContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            string[] navLabels = {
                AegisLocalization.T("Chơi ngay", "Play"),
                AegisLocalization.T("Hướng dẫn", "Tutorial"),
                AegisLocalization.T("Cài đặt", "Settings"),
                AegisLocalization.T("Cửa hàng", "Shop"),
                AegisLocalization.T("Thoát", "Exit")
            };
            string[] navIcons = { "icon_nav_home.png", "icon_nav_guide.png", "icon_nav_settings.png", "icon_nav_shop.png", "icon_nav_exit.png" };

            for (int i = 0; i < 5; i++)
            {
                int index = i;
                GameObject itemObj = new GameObject("NavItem_" + i);
                itemObj.transform.SetParent(_navContainer.transform, false);

                RectTransform rt = itemObj.AddComponent<RectTransform>();
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(380f, 68f);

                LayoutElement le = itemObj.AddComponent<LayoutElement>();
                le.preferredWidth = 380f;
                le.preferredHeight = 68f;

                Image bg = itemObj.AddComponent<Image>();
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;

                Button btn = itemObj.AddComponent<Button>();
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => OnNavMenuItemClicked(index));
                itemObj.AddComponent<MenuButtonEffect>();

                // Left Icon (Crisp white asset tinted dynamically to white if active, or warm ivory / gold if inactive)
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(itemObj.transform, false);
                Image icImg = iconObj.AddComponent<Image>();
                Sprite s = ArcaneAssetLoader.LoadArcaneSprite(navIcons[i]);
                if (s != null) icImg.sprite = s;
                icImg.preserveAspect = true;
                icImg.raycastTarget = false;
                icImg.color = (i == 0) ? Color.white : ColParchment;
                RectTransform icRt = iconObj.GetComponent<RectTransform>();
                icRt.anchorMin = new Vector2(0f, 0.5f);
                icRt.anchorMax = new Vector2(0f, 0.5f);
                icRt.pivot = new Vector2(0f, 0.5f);
                icRt.anchoredPosition = new Vector2(20f, 0f);
                icRt.sizeDelta = new Vector2(50f, 50f);

                // Label Text
                GameObject txtObj = new GameObject("Label");
                txtObj.transform.SetParent(itemObj.transform, false);
                Text t = txtObj.AddComponent<Text>();
                t.text = navLabels[i];
                t.font = font;
                t.fontSize = 28;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleLeft;
                t.color = (i == 0) ? Color.white : ColTextWhite;
                t.raycastTarget = false;

                Outline outline = txtObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.01f, 0.03f, 0.08f, 0.95f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);

                Shadow shadow = txtObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
                shadow.effectDistance = new Vector2(2f, -2f);

                RectTransform trt = txtObj.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0f, 0.5f);
                trt.anchorMax = new Vector2(0f, 0.5f);
                trt.pivot = new Vector2(0f, 0.5f);
                trt.anchoredPosition = new Vector2(86f, 0f);
                trt.sizeDelta = new Vector2(280f, 50f);

                // Right Chevron Arrow Tip
                GameObject chevObj = new GameObject("RightChevron");
                chevObj.transform.SetParent(itemObj.transform, false);
                Text cText = chevObj.AddComponent<Text>();
                cText.text = ">";
                cText.font = font;
                cText.fontSize = 32;
                cText.fontStyle = FontStyle.Bold;
                cText.alignment = TextAnchor.MiddleCenter;
                cText.color = ColCyanGlow;
                cText.raycastTarget = false;
                RectTransform crt = chevObj.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(1f, 0.5f);
                crt.anchorMax = new Vector2(1f, 0.5f);
                crt.pivot = new Vector2(1f, 0.5f);
                crt.anchoredPosition = new Vector2(-28f, 0f);
                crt.sizeDelta = new Vector2(30f, 30f);
                chevObj.SetActive(false);

                _navItemObjs[i] = itemObj;
                _navItemBgs[i] = bg;
                _navItemRts[i] = rt;
                _navItemIcons[i] = icRt;
                _navItemIconImgs[i] = icImg;
                _navItemTexts[i] = t;
                _navItemChevrons[i] = chevObj;
                _navItemLayoutElements[i] = le;
            }

            // Initially highlight item 0 ("Chơi ngay")
            UpdateNavSelection(0);
        }

        public void UpdateNavSelection(int selectedIndex)
        {
            _selectedMenuNavIndex = selectedIndex;
            Sprite chevronSprite = ArcaneAssetLoader.LoadArcaneSprite("btn_play_chevron.png");

            for (int i = 0; i < 5; i++)
            {
                bool isActive = (i == selectedIndex);
                if (_navItemBgs[i] != null)
                {
                    if (isActive)
                    {
                        if (chevronSprite != null)
                        {
                            _navItemBgs[i].sprite = chevronSprite;
                            _navItemBgs[i].type = Image.Type.Simple;
                        }
                        else
                        {
                            _navItemBgs[i].sprite = ArcaneAssetLoader.GetGlassBoxSprite(440, 105, 18f, ColGlassActive, ColCyanBorder, 2f);
                            _navItemBgs[i].type = Image.Type.Sliced;
                        }
                        _navItemBgs[i].color = Color.white;
                    }
                    else
                    {
                        _navItemBgs[i].sprite = ArcaneAssetLoader.GetGlassBoxSprite(380, 68, 14f, ColMidnight, ColMutedBorder, 1.2f);
                        _navItemBgs[i].type = Image.Type.Sliced;
                        _navItemBgs[i].color = Color.white;
                    }
                }

                if (_navItemIconImgs[i] != null)
                {
                    _navItemIconImgs[i].color = isActive ? Color.white : ColParchment;
                }

                if (_navItemRts[i] != null)
                {
                    _navItemRts[i].sizeDelta = isActive ? new Vector2(440f, 105f) : new Vector2(380f, 68f);
                }

                if (_navItemLayoutElements[i] != null)
                {
                    _navItemLayoutElements[i].preferredWidth = isActive ? 440f : 380f;
                    _navItemLayoutElements[i].preferredHeight = isActive ? 105f : 68f;
                }

                if (_navItemIcons[i] != null)
                {
                    _navItemIcons[i].sizeDelta = isActive ? new Vector2(58f, 58f) : new Vector2(48f, 48f);
                    _navItemIcons[i].anchoredPosition = isActive ? new Vector2(22f, 0f) : new Vector2(20f, 0f);
                }

                if (_navItemTexts[i] != null)
                {
                    _navItemTexts[i].fontSize = isActive ? 32 : 28;
                    _navItemTexts[i].color = isActive ? Color.white : new Color(0.95f, 0.98f, 1.0f, 0.95f);
                    _navItemTexts[i].rectTransform.anchoredPosition = isActive ? new Vector2(98f, 0f) : new Vector2(86f, 0f);
                }

                if (_navItemChevrons[i] != null)
                {
                    _navItemChevrons[i].SetActive(isActive);
                }
            }

            if (_navContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_navContainer.GetComponent<RectTransform>());
            }
        }

        private void OnNavMenuItemClicked(int index)
        {
            UpdateNavSelection(index);

            switch (index)
            {
                case 0:
                    NavigateToChooseMode();
                    break;
                case 1:
                    OpenTutorialModal();
                    break;
                case 2:
                    OpenSettingsModal();
                    break;
                case 3:
                    OpenShopModal();
                    break;
                case 4:
                    ShowConfirmDialog(
                        AegisLocalization.T("Thoát trò chơi", "Exit Game"),
                        AegisLocalization.T("Bạn có muốn thoát khỏi trò chơi?", "Are you sure you want to exit the game?"),
                        () =>
                    {
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                    });
                    break;
            }
        }

        private void BuildChampionDialogueBubble(Transform parent, Font font)
        {
            GameObject bubbleObj = new GameObject("ChampionDialogueBubble");
            bubbleObj.transform.SetParent(parent, false);

            Image bg = bubbleObj.AddComponent<Image>();
            Sprite bubbleSprite = ArcaneAssetLoader.LoadArcaneSprite("hud_dialogue_bubble.png");
            if (bubbleSprite != null)
            {
                bg.sprite = bubbleSprite;
                bg.preserveAspect = false;
            }
            else
            {
                bg.color = new Color(0.02f, 0.08f, 0.18f, 0.85f);
            }

            RectTransform bRt = bubbleObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(1f, 0f);
            bRt.anchorMax = new Vector2(1f, 0f);
            bRt.pivot = new Vector2(1f, 0f);
            bRt.anchoredPosition = new Vector2(-60f, 32f);
            bRt.sizeDelta = new Vector2(490f, 108f);

            GameObject txtObj = new GameObject("DialogueText");
            txtObj.transform.SetParent(bubbleObj.transform, false);
            Text t = txtObj.AddComponent<Text>();
            t.text = AegisLocalization.T("Khám phá thế giới cờ vua\ntrong không gian 3D và AR", "Explore the world of chess\nin 3D and AR space");
            t.font = font;
            t.fontSize = 21;
            t.lineSpacing = 1.15f;
            t.fontStyle = FontStyle.Normal;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = ColTextWhite;
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(44f, 10f); // offset to account for left pointer notch
            trt.offsetMax = new Vector2(-20f, -10f);
        }

        // =========================================================================
        // SCREEN 3: CHỌN CHẾ ĐỘ CHƠI (Mode Select with 4 Feature Cards, User Ref Design)
        // =========================================================================
        private void BuildScreen3ModeSelect(Transform parent, Font font)
        {
            _screenModeSelect = CreateFullscreenPanel(parent, "Screen3_ModeSelect");

            // Dark Obsidian Backing
            Image bgImg = _screenModeSelect.AddComponent<Image>();
            bgImg.color = new Color(0.02f, 0.05f, 0.10f, 0.94f);

            // Centered Main Window Card (1520 x 820)
            GameObject windowObj = new GameObject("ModeSelectWindow");
            windowObj.transform.SetParent(_screenModeSelect.transform, false);
            RectTransform winRt = windowObj.AddComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.anchoredPosition = Vector2.zero;
            winRt.sizeDelta = new Vector2(1520f, 820f);

            Image winBg = windowObj.AddComponent<Image>();
            Sprite winBgSprite = ArcaneAssetLoader.LoadArcaneSprite("mode_select_window_bg.png");
            if (winBgSprite != null)
            {
                winBg.sprite = winBgSprite;
                winBg.type = Image.Type.Simple;
            }
            else
            {
                winBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1520, 820, 24f, new Color(0.03f, 0.08f, 0.16f, 0.94f), ColCyanBorder, 2f);
                winBg.type = Image.Type.Sliced;
            }

            // Window Header Bar (Height 70px, anchored top)
            GameObject headerObj = new GameObject("ModeHeader");
            headerObj.transform.SetParent(windowObj.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 1f);
            hRt.anchorMax = new Vector2(0.5f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -16f);
            hRt.sizeDelta = new Vector2(1440f, 64f);

            // Back Button [ ❮ ] (Left)
            GameObject backBtnObj = new GameObject("BtnBack");
            backBtnObj.transform.SetParent(headerObj.transform, false);
            Image bbImg = backBtnObj.AddComponent<Image>();
            bbImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(48, 48, 10f, new Color(0.04f, 0.12f, 0.22f, 0.90f), ColCyanBorder, 1.5f);
            Button bbBtn = backBtnObj.AddComponent<Button>();
            bbBtn.onClick.AddListener(NavigateToHome);
            backBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform bbRt = backBtnObj.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0f, 0.5f);
            bbRt.anchorMax = new Vector2(0f, 0.5f);
            bbRt.pivot = new Vector2(0f, 0.5f);
            bbRt.anchoredPosition = new Vector2(10f, 0f);
            bbRt.sizeDelta = new Vector2(48f, 48f);

            GameObject backIconObj = new GameObject("BackIcon");
            backIconObj.transform.SetParent(backBtnObj.transform, false);
            Image backIconImg = backIconObj.AddComponent<Image>();
            Sprite backSprite = ArcaneAssetLoader.LoadArcaneSprite("icon_nav_back.png");
            if (backSprite != null)
            {
                backIconImg.sprite = backSprite;
                backIconImg.color = ColCyanGlow;
            }
            else
            {
                Text bkt = backIconObj.AddComponent<Text>();
                bkt.text = "<";
                bkt.font = font;
                bkt.fontSize = 24;
                bkt.fontStyle = FontStyle.Bold;
                bkt.alignment = TextAnchor.MiddleCenter;
                bkt.color = ColCyanGlow;
            }
            RectTransform backIconRt = backIconObj.GetComponent<RectTransform>();
            backIconRt.anchorMin = Vector2.zero;
            backIconRt.anchorMax = Vector2.one;
            backIconRt.offsetMin = new Vector2(10f, 10f);
            backIconRt.offsetMax = new Vector2(-10f, -10f);

            // Settings Cog Button [ ⚙ ] (Right)
            GameObject cogBtnObj = new GameObject("BtnSettings");
            cogBtnObj.transform.SetParent(headerObj.transform, false);
            Image cogImg = cogBtnObj.AddComponent<Image>();
            cogImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(48, 48, 10f, new Color(0.04f, 0.12f, 0.22f, 0.90f), ColCyanBorder, 1.5f);
            Button cogBtn = cogBtnObj.AddComponent<Button>();
            cogBtn.onClick.AddListener(OpenSettingsModal);
            cogBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform cogRt = cogBtnObj.GetComponent<RectTransform>();
            cogRt.anchorMin = new Vector2(1f, 0.5f);
            cogRt.anchorMax = new Vector2(1f, 0.5f);
            cogRt.pivot = new Vector2(1f, 0.5f);
            cogRt.anchoredPosition = new Vector2(-10f, 0f);
            cogRt.sizeDelta = new Vector2(48f, 48f);

            GameObject cogIconObj = new GameObject("CogIcon");
            cogIconObj.transform.SetParent(cogBtnObj.transform, false);
            Image cogIconImg = cogIconObj.AddComponent<Image>();
            Sprite setSprite = ArcaneAssetLoader.LoadArcaneSprite("icon_nav_settings.png");
            if (setSprite != null)
            {
                cogIconImg.sprite = setSprite;
                cogIconImg.color = ColCyanGlow;
            }
            else
            {
                Text cogTxt = cogIconObj.AddComponent<Text>();
                cogTxt.text = "⚙";
                cogTxt.font = font;
                cogTxt.fontSize = 22;
                cogTxt.alignment = TextAnchor.MiddleCenter;
                cogTxt.color = ColCyanGlow;
            }
            RectTransform cogIconRt = cogIconObj.GetComponent<RectTransform>();
            cogIconRt.anchorMin = Vector2.zero;
            cogIconRt.anchorMax = Vector2.one;
            cogIconRt.offsetMin = new Vector2(8f, 8f);
            cogIconRt.offsetMax = new Vector2(-8f, -8f);

            // Title: "CHỌN CHẾ ĐỘ CHƠI" with left and right horizontal accent lines
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = AegisLocalization.T("CHỌN CHẾ ĐỘ CHƠI", "CHOOSE GAME MODE");
            titleTxt.font = font;
            titleTxt.fontSize = 32;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = ColTextWhite;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0.5f);
            tRt.anchorMax = new Vector2(0.5f, 0.5f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.anchoredPosition = Vector2.zero;
            tRt.sizeDelta = new Vector2(460f, 44f);

            // Decorative Accent Lines beside Title
            GameObject leftLineObj = new GameObject("AccentLineLeft");
            leftLineObj.transform.SetParent(headerObj.transform, false);
            Image llImg = leftLineObj.AddComponent<Image>();
            llImg.color = ColAntiqueGold;
            RectTransform llRt = leftLineObj.GetComponent<RectTransform>();
            llRt.anchorMin = new Vector2(0.5f, 0.5f);
            llRt.anchorMax = new Vector2(0.5f, 0.5f);
            llRt.pivot = new Vector2(1f, 0.5f);
            llRt.anchoredPosition = new Vector2(-190f, 0f);
            llRt.sizeDelta = new Vector2(110f, 2f);

            GameObject rightLineObj = new GameObject("AccentLineRight");
            rightLineObj.transform.SetParent(headerObj.transform, false);
            Image rlImg = rightLineObj.AddComponent<Image>();
            rlImg.color = ColAntiqueGold;
            RectTransform rlRt = rightLineObj.GetComponent<RectTransform>();
            rlRt.anchorMin = new Vector2(0.5f, 0.5f);
            rlRt.anchorMax = new Vector2(0.5f, 0.5f);
            rlRt.pivot = new Vector2(0f, 0.5f);
            rlRt.anchoredPosition = new Vector2(190f, 0f);
            rlRt.sizeDelta = new Vector2(110f, 2f);

            // Horizontal Separator Line below Header
            GameObject sepObj = new GameObject("HeaderSeparator");
            sepObj.transform.SetParent(windowObj.transform, false);
            Image sepImg = sepObj.AddComponent<Image>();
            sepImg.color = ColMutedBorder;
            RectTransform sepRt = sepObj.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0.5f, 1f);
            sepRt.anchorMax = new Vector2(0.5f, 1f);
            sepRt.pivot = new Vector2(0.5f, 1f);
            sepRt.anchoredPosition = new Vector2(0f, -80f);
            sepRt.sizeDelta = new Vector2(1440f, 1.5f);

            // 4 Vertical Featured Cards Container (Full height inside window, no squishing bottom banner)
            GameObject cardsContainer = new GameObject("CardsContainer");
            cardsContainer.transform.SetParent(windowObj.transform, false);
            RectTransform ccRt = cardsContainer.AddComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0.5f, 0.5f);
            ccRt.anchorMax = new Vector2(0.5f, 0.5f);
            ccRt.pivot = new Vector2(0.5f, 0.5f);
            ccRt.anchoredPosition = new Vector2(0f, -38f);
            ccRt.sizeDelta = new Vector2(1440f, 660f);

            float cardWidth = 330f;
            float cardGap = 35f;
            float totalSpan = 4 * cardWidth + 3 * cardGap;
            float startX = -totalSpan / 2f + cardWidth / 2f;

            // Card 1: Đấu với máy (Index 0)
            CreateModeFeatureCard(0, cardsContainer.transform, "CardAI",
                AegisLocalization.T("Đấu với máy", "Vs Computer"),
                AegisLocalization.T("Chọn độ khó và bắt đầu\nthử thách AI", "Choose difficulty & challenge\nintelligent AI"),
                "mode_card_ai.png", new Vector2(startX + 0 * (cardWidth + cardGap), 0f), OpenAIDifficultyModal, font);

            // Card 2: Hai người (Index 1)
            CreateModeFeatureCard(1, cardsContainer.transform, "Card2P",
                AegisLocalization.T("Hai người", "Pass & Play"),
                AegisLocalization.T("Chơi cùng bạn bè\ntrên cùng thiết bị", "Play with friends\non the same device"),
                "mode_card_2p.png", new Vector2(startX + 1 * (cardWidth + cardGap), 0f), () => LaunchGame(GameLaunchMode.LocalPassAndPlay), font);

            // Card 3: AR (Index 2)
            CreateModeFeatureCard(2, cardsContainer.transform, "CardAR",
                AegisLocalization.T("AR", "AR Mode"),
                AegisLocalization.T("Đặt bàn cờ lên mặt bàn thật\nqua camera", "Place board on tabletop\nvia camera tracking"),
                "mode_card_ar.png", new Vector2(startX + 2 * (cardWidth + cardGap), 0f), () => LaunchGame(GameLaunchMode.AugmentedReality), font);

            // Card 4: Học cờ & Puzzle (Index 3)
            CreateModeFeatureCard(3, cardsContainer.transform, "CardLearn",
                AegisLocalization.T("Học cờ & Puzzle", "Learn & Puzzles"),
                AegisLocalization.T("Nâng cao kỹ năng\nvà tư duy chiến thuật", "Enhance chess tactics\nand strategic thinking"),
                "mode_card_learn.png", new Vector2(startX + 3 * (cardWidth + cardGap), 0f), OpenLearnScreen, font);
        }

        private void CreateModeFeatureCard(int index, Transform parent, string name, string title, string desc, string textureName, Vector2 pos, Action onClick, Font font)
        {
            GameObject cardObj = new GameObject(name);
            cardObj.transform.SetParent(parent, false);

            Image cardBg = cardObj.AddComponent<Image>();
            bool isSelected = (index == _selectedModeCardIndex);
            Sprite frameSprite = ArcaneAssetLoader.LoadArcaneSprite(isSelected ? "mode_card_frame_active.png" : "mode_card_frame_inactive.png");
            if (frameSprite != null)
            {
                cardBg.sprite = frameSprite;
                cardBg.type = Image.Type.Simple;
            }
            else
            {
                cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(330, 570, 20f, ColGlassFill, isSelected ? ColCyanGlow : ColCyanBorder, isSelected ? 2.5f : 1.5f);
                cardBg.type = Image.Type.Sliced;
            }
            if (index >= 0 && index < _modeCardFrameBgs.Length)
            {
                _modeCardFrameBgs[index] = cardBg;
            }

            Button btn = cardObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectModeCard(index, onClick));
            cardObj.AddComponent<MenuButtonEffect>();

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(330f, 570f);

            // Card Artwork Image - fills upper 67% of card
            GameObject artObj = new GameObject("Artwork");
            artObj.transform.SetParent(cardObj.transform, false);
            Image artImg = artObj.AddComponent<Image>();
            Sprite artSprite = ArcaneAssetLoader.LoadArcaneSprite(textureName);
            if (artSprite != null)
            {
                artImg.sprite = artSprite;
                artImg.preserveAspect = false;
            }
            else
            {
                artImg.color = new Color(0.1f, 0.2f, 0.35f, 0.8f);
            }
            RectTransform artRt = artObj.GetComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0.5f, 1f);
            artRt.anchorMax = new Vector2(0.5f, 1f);
            artRt.pivot = new Vector2(0.5f, 1f);
            artRt.anchoredPosition = new Vector2(0f, -14f);
            artRt.sizeDelta = new Vector2(302f, 385f);

            // Title
            GameObject titleObj = new GameObject("CardTitle");
            titleObj.transform.SetParent(cardObj.transform, false);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = title;
            titleTxt.font = font;
            titleTxt.fontSize = 26;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = ColTextWhite;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0f);
            tRt.anchorMax = new Vector2(0.5f, 0f);
            tRt.pivot = new Vector2(0.5f, 0f);
            tRt.anchoredPosition = new Vector2(0f, 96f);
            tRt.sizeDelta = new Vector2(310f, 36f);

            // Description (centered 2 lines)
            GameObject descObj = new GameObject("CardDesc");
            descObj.transform.SetParent(cardObj.transform, false);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.text = desc;
            descTxt.font = font;
            descTxt.fontSize = 17;
            descTxt.lineSpacing = 1.25f;
            descTxt.alignment = TextAnchor.MiddleCenter;
            descTxt.color = new Color(0.75f, 0.85f, 0.96f, 0.95f);
            descTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            descTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0f);
            dRt.anchorMax = new Vector2(0.5f, 0f);
            dRt.pivot = new Vector2(0.5f, 0f);
            dRt.anchoredPosition = new Vector2(0f, 26f);
            dRt.sizeDelta = new Vector2(310f, 60f);
        }

        private void SelectModeCard(int index, Action onAction)
        {
            _selectedModeCardIndex = index;
            Sprite activeSprite = ArcaneAssetLoader.LoadArcaneSprite("mode_card_frame_active.png");
            Sprite inactiveSprite = ArcaneAssetLoader.LoadArcaneSprite("mode_card_frame_inactive.png");

            for (int i = 0; i < _modeCardFrameBgs.Length; i++)
            {
                if (_modeCardFrameBgs[i] != null)
                {
                    if (activeSprite != null && inactiveSprite != null)
                    {
                        _modeCardFrameBgs[i].sprite = (i == index) ? activeSprite : inactiveSprite;
                    }
                    else
                    {
                        _modeCardFrameBgs[i].sprite = ArcaneAssetLoader.GetGlassBoxSprite(330, 570, 20f, ColGlassFill, (i == index) ? ColCyanGlow : ColCyanBorder, (i == index) ? 2.5f : 1.5f);
                    }
                }
            }

            onAction?.Invoke();
        }

        private void BuildOnlineRankedBanner(Transform parent, Font font)
        {
            GameObject bannerObj = new GameObject("OnlineRankedBanner");
            bannerObj.transform.SetParent(parent, false);

            Image bg = bannerObj.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1200, 60, 18f, new Color(0.04f, 0.10f, 0.22f, 0.92f), ColCyanBorder, 1.2f);
            bg.type = Image.Type.Sliced;

            Button btn = bannerObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                RequireLogin(() =>
                {
                    // Show options: Random Match or Friend Duel
                    OpenOnlineMatchChoiceModal();
                });
            });
            bannerObj.AddComponent<MenuButtonEffect>();

            RectTransform bRt = bannerObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 25f);
            bRt.sizeDelta = new Vector2(1200f, 58f);

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(bannerObj.transform, false);
            Text t = txtObj.AddComponent<Text>();
            t.text = AegisLocalization.T(
                "🌐  <color=#B99A61><b>ĐẤU TRƯỜNG ONLINE:</b></color> Ghép Đấu Trực Tuyến & Thách Đấu Bạn Bè  ➜  <color=#B99A61><b>THAM GIA NGAY</b></color>",
                "🌐  <color=#B99A61><b>ONLINE MULTIPLAYER:</b></color> Auto Matchmaking & Friend Duel  ➜  <color=#B99A61><b>JOIN NOW</b></color>");
            t.font = font;
            t.fontSize = 19;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = ColTextWhite;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10f, 0f);
            trt.offsetMax = new Vector2(-10f, 0f);
        }

        // =========================================================================
        // SCREEN 4: CHỌN ĐỘ KHÓ AI (AI Select Modal with Paladin portrait & golden CTA)
        // =========================================================================
        private void BuildScreen4AIDifficulty(Transform parent, Font font)
        {
            _aiDifficultyModal = CreateModalOverlay(parent, "Screen4_AIDifficultyModal");

            GameObject cardObj = new GameObject("AIDifficultyCard");
            cardObj.transform.SetParent(_aiDifficultyModal.transform, false);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1280, 720, 28f, ColGlassFill, ColCyanBorder, 2f);
            cardBg.type = Image.Type.Sliced;

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1280f, 720f);

            // Modal Header
            CreateModalHeader(cardObj.transform, AegisLocalization.T("<  ĐẤU VỚI MÁY", "<  VS COMPUTER"), () => _aiDifficultyModal.SetActive(false), font);

            // Left Column: Paladin Knight Portrait
            GameObject portraitObj = new GameObject("PaladinPortrait");
            portraitObj.transform.SetParent(cardObj.transform, false);
            Image portImg = portraitObj.AddComponent<Image>();
            Sprite portSprite = ArcaneAssetLoader.LoadArcaneSprite("ai_portrait_full.png");
            if (portSprite != null) portImg.sprite = portSprite;
            else portImg.color = new Color(0.1f, 0.2f, 0.35f);
            portImg.preserveAspect = true;
            RectTransform pRt = portraitObj.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 0.5f);
            pRt.anchorMax = new Vector2(0f, 0.5f);
            pRt.pivot = new Vector2(0f, 0.5f);
            pRt.anchoredPosition = new Vector2(60f, -20f);
            pRt.sizeDelta = new Vector2(280f, 480f);

            GameObject tipObj = new GameObject("PortTip");
            tipObj.transform.SetParent(portraitObj.transform, false);
            Text tipTxt = tipObj.AddComponent<Text>();
            tipTxt.text = AegisLocalization.T("Chọn cấp độ của AI\nvà thiết lập thời gian.", "Select AI difficulty\nand match timer.");
            tipTxt.font = font;
            tipTxt.fontSize = 17;
            tipTxt.fontStyle = FontStyle.Italic;
            tipTxt.alignment = TextAnchor.MiddleCenter;
            tipTxt.color = ColTextMuted;
            tipTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            tipTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform tpRt = tipObj.GetComponent<RectTransform>();
            tpRt.anchorMin = new Vector2(0.5f, 0f);
            tpRt.anchorMax = new Vector2(0.5f, 0f);
            tpRt.pivot = new Vector2(0.5f, 0f);
            tpRt.anchoredPosition = new Vector2(0f, -40f);
            tpRt.sizeDelta = new Vector2(260f, 40f);

            // Center Column: 4 Difficulty Selection Cards
            GameObject diffCol = new GameObject("DifficultyColumn");
            diffCol.transform.SetParent(cardObj.transform, false);
            RectTransform dcRt = diffCol.AddComponent<RectTransform>();
            dcRt.anchorMin = new Vector2(0.5f, 0.5f);
            dcRt.anchorMax = new Vector2(0.5f, 0.5f);
            dcRt.pivot = new Vector2(0.5f, 0.5f);
            dcRt.anchoredPosition = new Vector2(-40f, -20f);
            dcRt.sizeDelta = new Vector2(380f, 480f);

            _aiDiffCardBgs = new Image[4];
            _aiDiffCardTitles = new Text[4];

            string[] diffTitles = {
                AegisLocalization.T("DỄ", "EASY"),
                AegisLocalization.T("TRUNG BÌNH", "MEDIUM"),
                AegisLocalization.T("KHÓ", "HARD"),
                AegisLocalization.T("RẤT KHÓ", "EXPERT")
            };
            string[] diffElos = { "Elo ~ 900", "Elo ~ 1200", "Elo ~ 1600", "Elo ~ 2000" };
            float diffStartY = 160f;
            float diffStepY = 105f;

            for (int i = 0; i < 4; i++)
            {
                int levelIndex = i + 1;
                int idx = i;
                GameObject btnObj = new GameObject($"BtnDiff_{levelIndex}");
                btnObj.transform.SetParent(diffCol.transform, false);

                Image bg = btnObj.AddComponent<Image>();
                _aiDiffCardBgs[idx] = bg;
                bg.type = Image.Type.Sliced;

                Button btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectAIDifficultyLevel(levelIndex));
                btnObj.AddComponent<MenuButtonEffect>();

                RectTransform bRt = btnObj.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 0.5f);
                bRt.anchorMax = new Vector2(0.5f, 0.5f);
                bRt.pivot = new Vector2(0.5f, 0.5f);
                bRt.anchoredPosition = new Vector2(0f, diffStartY - i * diffStepY);
                bRt.sizeDelta = new Vector2(360f, 85f);

                GameObject tObj = new GameObject("Title");
                tObj.transform.SetParent(btnObj.transform, false);
                Text t = tObj.AddComponent<Text>();
                t.text = diffTitles[i];
                t.font = font;
                t.fontSize = 22;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleLeft;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                _aiDiffCardTitles[idx] = t;
                RectTransform trt = tObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(30f, 15f);
                trt.offsetMax = new Vector2(-20f, 0f);

                GameObject eloObj = new GameObject("Elo");
                eloObj.transform.SetParent(btnObj.transform, false);
                Text eloTxt = eloObj.AddComponent<Text>();
                eloTxt.text = diffElos[i];
                eloTxt.font = font;
                eloTxt.fontSize = 17;
                eloTxt.alignment = TextAnchor.MiddleLeft;
                eloTxt.color = ColTextMuted;
                eloTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                eloTxt.verticalOverflow = VerticalWrapMode.Overflow;
                RectTransform elort = eloObj.GetComponent<RectTransform>();
                elort.anchorMin = Vector2.zero;
                elort.anchorMax = Vector2.one;
                elort.offsetMin = new Vector2(30f, 0f);
                elort.offsetMax = new Vector2(-20f, -25f);
            }
            RefreshAIDifficultyVisuals();

            // Right Column: Time Dropdown + Side Selector + Golden CTA Button [BẮT ĐẦU]
            GameObject rightCol = new GameObject("RightConfigColumn");
            rightCol.transform.SetParent(cardObj.transform, false);
            RectTransform rcRt = rightCol.AddComponent<RectTransform>();
            rcRt.anchorMin = new Vector2(1f, 0.5f);
            rcRt.anchorMax = new Vector2(1f, 0.5f);
            rcRt.pivot = new Vector2(1f, 0.5f);
            rcRt.anchoredPosition = new Vector2(-70f, -20f);
            rcRt.sizeDelta = new Vector2(380f, 480f);

            // Time Section
            CreateConfigLabel(rightCol.transform, AegisLocalization.T("Thời gian", "Time Control"), new Vector2(0f, 180f), font);
            GameObject timeDropdownObj = null;
            timeDropdownObj = CreateGlassPill(rightCol.transform, "TimePill", $"{GetAITimeDisplay(_selectedAITimeIndex)}  ▼", new Vector2(0f, 135f), new Vector2(360f, 50f), font, () =>
            {
                _selectedAITimeIndex = (_selectedAITimeIndex + 1) % 5;
                if (timeDropdownObj != null)
                {
                    Text tt = timeDropdownObj.GetComponentInChildren<Text>();
                    if (tt != null) tt.text = $"{GetAITimeDisplay(_selectedAITimeIndex)}  ▼";
                }
            });

            // Side Color Section
            CreateConfigLabel(rightCol.transform, AegisLocalization.T("Màu quân", "Your Side"), new Vector2(0f, 65f), font);

            // Radio White
            GameObject radioW = new GameObject("RadioWhite");
            radioW.transform.SetParent(rightCol.transform, false);
            Button rwb = radioW.AddComponent<Button>();
            rwb.onClick.AddListener(() => SelectAIPieceColor(PieceColor.White));
            radioW.AddComponent<MenuButtonEffect>();
            RectTransform rwRt = radioW.AddComponent<RectTransform>();
            rwRt.anchoredPosition = new Vector2(-80f, 15f);
            rwRt.sizeDelta = new Vector2(140f, 40f);

            _radioWhiteImg = CreateRadioIndicator(radioW.transform, new Vector2(-45f, 0f));
            CreateTextComp(radioW.transform, AegisLocalization.T("Trắng", "White"), 18, FontStyle.Bold, ColTextWhite, font, new Vector2(15f, 0f), new Vector2(90f, 30f));

            // Radio Black
            GameObject radioB = new GameObject("RadioBlack");
            radioB.transform.SetParent(rightCol.transform, false);
            Button rbb = radioB.AddComponent<Button>();
            rbb.onClick.AddListener(() => SelectAIPieceColor(PieceColor.Black));
            radioB.AddComponent<MenuButtonEffect>();
            RectTransform rbRt = radioB.AddComponent<RectTransform>();
            rbRt.anchoredPosition = new Vector2(80f, 15f);
            rbRt.sizeDelta = new Vector2(140f, 40f);

            _radioBlackImg = CreateRadioIndicator(radioB.transform, new Vector2(-45f, 0f));
            CreateTextComp(radioB.transform, AegisLocalization.T("Đen", "Black"), 18, FontStyle.Bold, ColTextWhite, font, new Vector2(15f, 0f), new Vector2(90f, 30f));

            RefreshRadioVisuals();

            // Golden CTA [BẮT ĐẦU]
            GameObject ctaObj = new GameObject("BtnStartAIMatch");
            ctaObj.transform.SetParent(rightCol.transform, false);
            Image ctaBg = ctaObj.AddComponent<Image>();
            ctaBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(360, 64, 32f, ColGoldCta, ColGoldBorder, 2f);
            ctaBg.type = Image.Type.Sliced;
            Button ctaBtn = ctaObj.AddComponent<Button>();
            ctaBtn.onClick.AddListener(ExecuteStartAIMatch);
            ctaObj.AddComponent<MenuButtonEffect>();
            RectTransform ctaRt = ctaObj.GetComponent<RectTransform>();
            ctaRt.anchoredPosition = new Vector2(0f, -120f);
            ctaRt.sizeDelta = new Vector2(360f, 64f);

            GameObject ctaTxtObj = new GameObject("Txt");
            ctaTxtObj.transform.SetParent(ctaObj.transform, false);
            Text ctaTxt = ctaTxtObj.AddComponent<Text>();
            ctaTxt.text = AegisLocalization.T("BẮT ĐẦU", "START MATCH");
            ctaTxt.font = font;
            ctaTxt.fontSize = 26;
            ctaTxt.fontStyle = FontStyle.Bold;
            ctaTxt.alignment = TextAnchor.MiddleCenter;
            ctaTxt.color = ColDarkBtnText;
            ctaTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            ctaTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform ctrt = ctaTxtObj.GetComponent<RectTransform>();
            ctrt.anchorMin = Vector2.zero;
            ctrt.anchorMax = Vector2.one;
            ctrt.offsetMin = Vector2.zero;
            ctrt.offsetMax = Vector2.zero;

            _aiDifficultyModal.SetActive(false);
        }

        private void SelectAIDifficultyLevel(int level)
        {
            _selectedAIDifficulty = level;
            RefreshAIDifficultyVisuals();
        }

        private void RefreshAIDifficultyVisuals()
        {
            if (_aiDiffCardBgs == null) return;
            for (int i = 0; i < 4; i++)
            {
                bool active = (i + 1 == _selectedAIDifficulty);
                if (_aiDiffCardBgs[i] != null)
                {
                    _aiDiffCardBgs[i].sprite = ArcaneAssetLoader.GetGlassBoxSprite(
                        360, 85, 18f,
                        active ? ColGlassActive : ColGlassFill,
                        active ? ColCyanBorder : ColMutedBorder,
                        active ? 2.2f : 1.2f
                    );
                }
                if (_aiDiffCardTitles[i] != null)
                {
                    _aiDiffCardTitles[i].color = active ? new Color(0.0f, 0.95f, 1.0f) : ColTextWhite;
                }
            }
        }

        private void SelectAIPieceColor(PieceColor color)
        {
            _selectedAIPieceColor = color;
            RefreshRadioVisuals();
        }

        private void RefreshRadioVisuals()
        {
            if (_radioWhiteImg != null)
            {
                bool isWhite = (_selectedAIPieceColor == PieceColor.White);
                _radioWhiteImg.sprite = ArcaneAssetLoader.GetCircleSprite(20, isWhite ? ColCyanBorder : Color.clear, ColCyanBorder, 2f);
            }
            if (_radioBlackImg != null)
            {
                bool isBlack = (_selectedAIPieceColor == PieceColor.Black);
                _radioBlackImg.sprite = ArcaneAssetLoader.GetCircleSprite(20, isBlack ? ColCyanBorder : Color.clear, ColCyanBorder, 2f);
            }
        }

        private void ExecuteStartAIMatch()
        {
            _aiDifficultyModal.SetActive(false);
            StartGameVsAI(_selectedAIDifficulty);
        }

        public void StartGameVsAI(int level)
        {
            _lastLaunchMode = GameLaunchMode.VersusAI;
            HideMenu();

            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                var board = MechChess.Chess3D.ChessBoard3DManager.Instance;
                board.ResetBoard();
                board.isVsAI = true;
                board.aiLevel = Mathf.Clamp(level, 1, 4);
            }

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.isRankedMode = false;
                string[] standardTimeControlCodes = { "3 + 2", "5 + 0", "10 + 0", "15 + 10", "10 + 0" };
                UIManager2D.Instance.ConfigureTimeControl(standardTimeControlCodes[_selectedAITimeIndex]);
                string levelStr = (level == 1) ? AegisLocalization.T("Dễ (800)", "Easy (800)") :
                                  (level == 2) ? AegisLocalization.T("Trung bình (1200)", "Medium (1200)") :
                                  (level == 3) ? AegisLocalization.T("Khó (1600)", "Hard (1600)") :
                                                 AegisLocalization.T("Rất khó (2000)", "Expert (2000)");
                UIManager2D.Instance.RecordMove(AegisLocalization.T(
                    $"[BẮT ĐẦU] Đấu với Máy: Cấp độ {levelStr} | Bên: {_selectedAIPieceColor} | Thời gian: {GetAITimeDisplay(_selectedAITimeIndex)}",
                    $"[START] Vs Computer: Level {levelStr} | Side: {_selectedAIPieceColor} | Time: {GetAITimeDisplay(_selectedAITimeIndex)}"));
            }

            ShowToast(AegisLocalization.T($"Bắt đầu ván đấu với AI ({level})!", $"Started match vs AI ({level})!"), ToastType.Success);
        }

        // =========================================================================
        // SCREEN 5: CÀI ĐẶT (Settings Modal matching User Reference Design)
        // =========================================================================
        private void BuildScreen5Settings(Transform parent, Font font)
        {
            _settingsModal = CreateModalOverlay(parent, "Screen5_SettingsModal");

            GameObject cardObj = new GameObject("SettingsCard");
            cardObj.transform.SetParent(_settingsModal.transform, false);

            Image cardBg = cardObj.AddComponent<Image>();
            Sprite cardSprite = ArcaneAssetLoader.LoadArcaneSprite("settings_card_bg.png");
            if (cardSprite != null)
            {
                cardBg.sprite = cardSprite;
                cardBg.type = Image.Type.Sliced;
            }
            else
            {
                cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(980, 620, 24f, new Color(0.03f, 0.08f, 0.16f, 0.95f), ColCyanBorder, 2f);
                cardBg.type = Image.Type.Sliced;
            }

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(980f, 620f);

            // 1. Header Bar
            // Left Title: "<  CÀI ĐẶT"
            GameObject headerLeftObj = new GameObject("HeaderLeftTitle");
            headerLeftObj.transform.SetParent(cardObj.transform, false);
            RectTransform hlRt = headerLeftObj.AddComponent<RectTransform>();
            hlRt.anchorMin = new Vector2(0f, 1f);
            hlRt.anchorMax = new Vector2(0f, 1f);
            hlRt.pivot = new Vector2(0f, 1f);
            hlRt.anchoredPosition = new Vector2(28f, -16f);
            hlRt.sizeDelta = new Vector2(300f, 44f);
            Text hlText = headerLeftObj.AddComponent<Text>();
            hlText.text = AegisLocalization.T("<b><color=#B99A61>‹</color>  CÀI ĐẶT</b>", "<b><color=#B99A61>‹</color>  SETTINGS</b>");
            hlText.font = font;
            hlText.fontSize = 24;
            hlText.alignment = TextAnchor.MiddleLeft;
            hlText.color = Color.white;

            // Right Close: "✕"
            GameObject closeBtnObj = new GameObject("HeaderCloseBtn");
            closeBtnObj.transform.SetParent(cardObj.transform, false);
            RectTransform cbRt = closeBtnObj.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 1f);
            cbRt.anchorMax = new Vector2(1f, 1f);
            cbRt.pivot = new Vector2(1f, 1f);
            cbRt.anchoredPosition = new Vector2(-22f, -16f);
            cbRt.sizeDelta = new Vector2(44f, 44f);

            Button closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(() =>
            {
                CloseModal(_settingsModal);
            });
            closeBtnObj.AddComponent<MenuButtonEffect>();

            Text cbText = closeBtnObj.AddComponent<Text>();
            cbText.text = "✕";
            cbText.font = font;
            cbText.fontSize = 22;
            cbText.fontStyle = FontStyle.Bold;
            cbText.alignment = TextAnchor.MiddleCenter;
            cbText.color = new Color(0.75f, 0.85f, 0.95f, 0.85f);

            // Header horizontal separator line
            GameObject sepLine = new GameObject("HeaderSeparatorLine");
            sepLine.transform.SetParent(cardObj.transform, false);
            Image sepImg = sepLine.AddComponent<Image>();
            sepImg.color = new Color(0.08f, 0.22f, 0.38f, 0.6f);
            RectTransform slRt = sepLine.GetComponent<RectTransform>();
            slRt.anchorMin = new Vector2(0f, 1f);
            slRt.anchorMax = new Vector2(1f, 1f);
            slRt.pivot = new Vector2(0.5f, 1f);
            slRt.anchoredPosition = new Vector2(0f, -66f);
            slRt.sizeDelta = new Vector2(-40f, 1.5f);

            // 2. Left Tabs Column
            GameObject tabCol = new GameObject("TabsColumn");
            tabCol.transform.SetParent(cardObj.transform, false);
            RectTransform tcRt = tabCol.AddComponent<RectTransform>();
            tcRt.anchorMin = new Vector2(0f, 0f);
            tcRt.anchorMax = new Vector2(0f, 1f);
            tcRt.pivot = new Vector2(0f, 0.5f);
            tcRt.anchoredPosition = new Vector2(24f, -40f);
            tcRt.sizeDelta = new Vector2(236f, -110f);

            VerticalLayoutGroup tvlg = tabCol.AddComponent<VerticalLayoutGroup>();
            tvlg.spacing = 10f;
            tvlg.childAlignment = TextAnchor.UpperLeft;
            tvlg.childControlWidth = true;
            tvlg.childControlHeight = false;
            tvlg.childForceExpandWidth = true;
            tvlg.childForceExpandHeight = false;

            string[] tabLabels = {
                AegisLocalization.T("Game", "Gameplay"),
                AegisLocalization.T("Đồ họa", "Graphics"),
                AegisLocalization.T("Âm thanh", "Audio"),
                AegisLocalization.T("Điều khiển", "Controls"),
                AegisLocalization.T("Ngôn ngữ", "Language")
            };
            string[] tabIconNames = { "icon_set_game.png", "icon_set_graphics.png", "icon_set_audio.png", "icon_set_controls.png", "icon_set_language.png" };

            Sprite activeTabSprite = ArcaneAssetLoader.LoadArcaneSprite("settings_tab_active.png");

            for (int i = 0; i < 5; i++)
            {
                int tabIdx = i;
                GameObject tObj = new GameObject("Tab_" + tabLabels[i]);
                tObj.transform.SetParent(tabCol.transform, false);

                RectTransform trt = tObj.AddComponent<RectTransform>();
                trt.sizeDelta = new Vector2(236f, 54f);

                LayoutElement le = tObj.AddComponent<LayoutElement>();
                le.preferredWidth = 236f;
                le.preferredHeight = 54f;

                Image bg = tObj.AddComponent<Image>();
                bg.type = Image.Type.Sliced;
                bg.color = (i == 0) ? Color.white : Color.clear;
                if (i == 0 && activeTabSprite != null) bg.sprite = activeTabSprite;

                Button btn = tObj.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => SwitchSettingsTab(tabIdx));
                tObj.AddComponent<MenuButtonEffect>();

                // Icon
                GameObject icObj = new GameObject("Icon");
                icObj.transform.SetParent(tObj.transform, false);
                Image ic = icObj.AddComponent<Image>();
                Sprite icS = ArcaneAssetLoader.LoadArcaneSprite(tabIconNames[i]);
                if (icS != null) ic.sprite = icS;
                ic.preserveAspect = true;
                ic.raycastTarget = false;
                ic.color = (i == 0) ? Color.white : new Color(0.72f, 0.82f, 0.95f, 0.9f);
                RectTransform icRt = icObj.GetComponent<RectTransform>();
                icRt.anchorMin = new Vector2(0f, 0.5f);
                icRt.anchorMax = new Vector2(0f, 0.5f);
                icRt.pivot = new Vector2(0f, 0.5f);
                icRt.anchoredPosition = new Vector2(18f, 0f);
                icRt.sizeDelta = new Vector2(36f, 36f);

                // Label
                GameObject lbObj = new GameObject("Label");
                lbObj.transform.SetParent(tObj.transform, false);
                Text lb = lbObj.AddComponent<Text>();
                lb.text = tabLabels[i];
                lb.font = font;
                lb.fontSize = 19;
                lb.fontStyle = (i == 0) ? FontStyle.Bold : FontStyle.Normal;
                lb.alignment = TextAnchor.MiddleLeft;
                lb.color = (i == 0) ? Color.white : new Color(0.78f, 0.88f, 0.98f, 0.9f);
                lb.raycastTarget = false;
                RectTransform lbRt = lbObj.GetComponent<RectTransform>();
                lbRt.anchorMin = new Vector2(0f, 0.5f);
                lbRt.anchorMax = new Vector2(0f, 0.5f);
                lbRt.pivot = new Vector2(0f, 0.5f);
                lbRt.anchoredPosition = new Vector2(68f, 0f);
                lbRt.sizeDelta = new Vector2(160f, 40f);

                _settingsTabBgs[i] = bg;
                _settingsTabTexts[i] = lb;
                _settingsTabIcons[i] = ic;
            }

            // 3. Vertical Divider Line
            GameObject vLine = new GameObject("VerticalDivider");
            vLine.transform.SetParent(cardObj.transform, false);
            Image vImg = vLine.AddComponent<Image>();
            vImg.color = new Color(0.08f, 0.22f, 0.38f, 0.55f);
            RectTransform vlRt = vLine.GetComponent<RectTransform>();
            vlRt.anchorMin = new Vector2(0f, 0f);
            vlRt.anchorMax = new Vector2(0f, 1f);
            vlRt.pivot = new Vector2(0.5f, 0.5f);
            vlRt.anchoredPosition = new Vector2(280f, -40f);
            vlRt.sizeDelta = new Vector2(1.5f, -110f);

            // 4. Right Content Container
            GameObject contentContainer = new GameObject("SettingsContentContainer");
            contentContainer.transform.SetParent(cardObj.transform, false);
            RectTransform ccRt = contentContainer.AddComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0f, 0f);
            ccRt.anchorMax = new Vector2(1f, 1f);
            ccRt.pivot = new Vector2(0f, 0.5f);
            ccRt.offsetMin = new Vector2(305f, 20f);
            ccRt.offsetMax = new Vector2(-25f, -80f);

            // Build all 5 content panels
            BuildSettingsPanelGame(contentContainer.transform, font);
            BuildSettingsPanelGraphics(contentContainer.transform, font);
            BuildSettingsPanelAudio(contentContainer.transform, font);
            BuildSettingsPanelControls(contentContainer.transform, font);
            BuildSettingsPanelLanguage(contentContainer.transform, font);

            // Activate initial tab 0
            SwitchSettingsTab(0);

            _settingsModal.SetActive(false);
        }

        private void SwitchSettingsTab(int tabIndex)
        {
            _selectedSettingsTab = tabIndex;
            Sprite activeSprite = ArcaneAssetLoader.LoadArcaneSprite("settings_tab_active.png");

            for (int i = 0; i < 5; i++)
            {
                bool active = (i == tabIndex);
                if (_settingsTabBgs[i] != null)
                {
                    _settingsTabBgs[i].sprite = active ? activeSprite : null;
                    _settingsTabBgs[i].color = active ? Color.white : Color.clear;
                }
                if (_settingsTabTexts[i] != null)
                {
                    _settingsTabTexts[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                    _settingsTabTexts[i].color = active ? Color.white : new Color(0.78f, 0.88f, 0.98f, 0.9f);
                }
                if (_settingsTabIcons[i] != null)
                {
                    _settingsTabIcons[i].color = active ? Color.white : new Color(0.72f, 0.82f, 0.95f, 0.9f);
                }
                if (_settingsPanels[i] != null)
                {
                    _settingsPanels[i].SetActive(active);
                }
            }
        }

        private void BuildSettingsPanelGame(Transform parent, Font font)
        {
            GameObject panel = new GameObject("Panel_Game");
            panel.transform.SetParent(parent, false);
            RectTransform prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            float yStart = -15f;
            float step = 68f;

            string[] diffOpts = {
                AegisLocalization.T("Dễ", "Easy"),
                AegisLocalization.T("Trung bình", "Medium"),
                AegisLocalization.T("Khó", "Hard"),
                AegisLocalization.T("Rất khó", "Expert")
            };
            string[] fxOpts = {
                AegisLocalization.T("Thấp", "Low"),
                AegisLocalization.T("Trung bình", "Medium"),
                AegisLocalization.T("Cao", "High"),
                AegisLocalization.T("Siêu thực", "Ultra")
            };
            string[] langOpts = { "Tiếng Việt", "English" };

            CreateModernDropdownRow(panel.transform, AegisLocalization.T("Độ khó AI", "AI Difficulty"), diffOpts, 1, yStart - 0 * step, font, val =>
            {
                if (val == "Dễ" || val == "Easy") _selectedAIDifficulty = 1;
                else if (val == "Trung bình" || val == "Medium") _selectedAIDifficulty = 2;
                else if (val == "Khó" || val == "Hard") _selectedAIDifficulty = 3;
                else if (val == "Rất khó" || val == "Expert") _selectedAIDifficulty = 4;
            });

            CreateModernDropdownRow(panel.transform, AegisLocalization.T("Hiệu ứng hình ảnh*", "Visual Effects*"), fxOpts, 2, yStart - 1 * step, font, val =>
            {
                PlayerPrefs.SetString("aegis.fx", val);
            });

            CreateModernToggleRow(panel.transform, AegisLocalization.T("Nhạc nền", "Music"), true, yStart - 2 * step, font, on =>
            {
                PlayerPrefs.SetInt("aegis.music", on ? 1 : 0);
            });

            CreateModernToggleRow(panel.transform, AegisLocalization.T("Âm thanh", "SFX"), true, yStart - 3 * step, font, on =>
            {
                PlayerPrefs.SetInt("aegis.sfx", on ? 1 : 0);
            });

            CreateModernToggleRow(panel.transform, AegisLocalization.T("Rung", "Vibration"), true, yStart - 4 * step, font, on =>
            {
                PlayerPrefs.SetInt("aegis.vibrate", on ? 1 : 0);
            });

            CreateModernDropdownRow(panel.transform, AegisLocalization.T("Ngôn ngữ", "Language"), langOpts, AegisLocalization.GetCurrentLanguageIndex(), yStart - 5 * step, font, val =>
            {
                AppLanguage target = (val == "English") ? AppLanguage.English : AppLanguage.Vietnamese;
                AegisLocalization.SetLanguage(target);
            });

            _settingsPanels[0] = panel;
        }

        private void BuildSettingsPanelGraphics(Transform parent, Font font)
        {
            GameObject panel = new GameObject("Panel_Graphics");
            panel.transform.SetParent(parent, false);
            RectTransform prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            float yStart = -15f;
            float step = 68f;

            string[] fxOpts = {
                AegisLocalization.T("Thấp", "Low"),
                AegisLocalization.T("Trung bình", "Medium"),
                AegisLocalization.T("Cao", "High"),
                AegisLocalization.T("Siêu thực", "Ultra")
            };
            string[] shadowOpts = {
                AegisLocalization.T("Tắt", "Off"),
                AegisLocalization.T("Thấp", "Low"),
                AegisLocalization.T("Cao", "High")
            };
            string[] fpsOpts = { "30 FPS", "60 FPS", "120 FPS" };

            CreateModernDropdownRow(panel.transform, AegisLocalization.T("Hiệu ứng hình ảnh*", "Visual Effects*"), fxOpts, 2, yStart - 0 * step, font, null);
            CreateModernDropdownRow(panel.transform, AegisLocalization.T("Chất lượng bóng", "Shadow Quality"), shadowOpts, 2, yStart - 1 * step, font, null);
            CreateModernDropdownRow(panel.transform, AegisLocalization.T("Tốc độ khung hình", "Framerate"), fpsOpts, 1, yStart - 2 * step, font, null);
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Khử răng cưa (Anti-Aliasing)", "Anti-Aliasing (FXAA)"), true, yStart - 3 * step, font, null);
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Hậu kỳ phát sáng (Bloom)", "Post-processing (Bloom)"), true, yStart - 4 * step, font, null);

            _settingsPanels[1] = panel;
        }

        private void BuildSettingsPanelAudio(Transform parent, Font font)
        {
            GameObject panel = new GameObject("Panel_Audio");
            panel.transform.SetParent(parent, false);
            RectTransform prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            float yStart = -15f;
            float step = 68f;

            CreateModernToggleRow(panel.transform, AegisLocalization.T("Nhạc nền (BGM)", "Music (BGM)"), true, yStart - 0 * step, font, null);
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Âm thanh hiệu ứng (SFX)", "Sound Effects (SFX)"), true, yStart - 1 * step, font, null);
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Âm thanh nước cờ", "Move Sound Cues"), true, yStart - 2 * step, font, null);
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Âm thanh thông báo", "Notification Sounds"), true, yStart - 3 * step, font, null);

            _settingsPanels[2] = panel;
        }

        private void BuildSettingsPanelControls(Transform parent, Font font)
        {
            GameObject panel = new GameObject("Panel_Controls");
            panel.transform.SetParent(parent, false);
            RectTransform prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            float yStart = -15f;
            float step = 68f;

            string[] sensOpts = {
                AegisLocalization.T("Chậm", "Slow"),
                AegisLocalization.T("Bình thường", "Normal"),
                AegisLocalization.T("Nhanh", "Fast")
            };

            CreateModernToggleRow(panel.transform, AegisLocalization.T("Rung phản hồi (Haptic)", "Haptic Feedback"), true, yStart - 0 * step, font, null);
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Gợi ý nước đi hợp lệ", "Highlight Valid Moves"), true, yStart - 1 * step, font, null);
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Tự động xoay bàn cờ", "Auto-rotate Board"), false, yStart - 2 * step, font, null);
            CreateModernDropdownRow(panel.transform, AegisLocalization.T("Độ nhạy xoay camera 3D", "Camera Orbit Sensitivity"), sensOpts, 1, yStart - 3 * step, font, null);

            _settingsPanels[3] = panel;
        }

        private void BuildSettingsPanelLanguage(Transform parent, Font font)
        {
            GameObject panel = new GameObject("Panel_Language");
            panel.transform.SetParent(parent, false);
            RectTransform prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            float yStart = -15f;
            float step = 68f;

            // Chế độ 2 ngôn ngữ duy nhất: Tiếng Việt hoặc English theo yêu cầu
            string[] langOpts = { "Tiếng Việt", "English" };
            int currentIdx = AegisLocalization.GetCurrentLanguageIndex();

            CreateModernDropdownRow(panel.transform, AegisLocalization.T("Ngôn ngữ hiển thị", "Display Language"), langOpts, currentIdx, yStart - 0 * step, font, val =>
            {
                AppLanguage target = (val == "English") ? AppLanguage.English : AppLanguage.Vietnamese;
                AegisLocalization.SetLanguage(target);
            });

            CreateModernToggleRow(panel.transform, AegisLocalization.T("Phụ đề hướng dẫn", "Tutorial Subtitles"), true, yStart - 1 * step, font, null);

            _settingsPanels[4] = panel;
        }

        private void CreateModernDropdownRow(Transform parent, string label, string[] options, int defaultIdx, float y, Font font, Action<string> onSelected)
        {
            GameObject row = new GameObject("Row_" + label);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, 48f);

            // Left Label
            GameObject lbObj = new GameObject("Label");
            lbObj.transform.SetParent(row.transform, false);
            Text lb = lbObj.AddComponent<Text>();
            lb.text = label;
            lb.font = font;
            lb.fontSize = 20;
            lb.alignment = TextAnchor.MiddleLeft;
            lb.color = new Color(0.9f, 0.95f, 1.0f, 0.95f);
            RectTransform lrt = lbObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(0.6f, 0.5f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.anchoredPosition = new Vector2(10f, 0f);
            lrt.sizeDelta = new Vector2(0f, 40f);

            // Right Dropdown Box
            GameObject ddObj = new GameObject("Dropdown");
            ddObj.transform.SetParent(row.transform, false);
            Image bg = ddObj.AddComponent<Image>();
            Sprite ddSprite = ArcaneAssetLoader.LoadArcaneSprite("settings_dropdown_box.png");
            if (ddSprite != null)
            {
                bg.sprite = ddSprite;
                bg.type = Image.Type.Sliced;
            }
            else
            {
                bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(190, 44, 10f, new Color(0.04f, 0.10f, 0.18f, 0.9f), ColCyanBorder, 1.2f);
                bg.type = Image.Type.Sliced;
            }

            Button btn = ddObj.AddComponent<Button>();
            ddObj.AddComponent<MenuButtonEffect>();

            RectTransform drt = ddObj.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(1f, 0.5f);
            drt.anchorMax = new Vector2(1f, 0.5f);
            drt.pivot = new Vector2(1f, 0.5f);
            drt.anchoredPosition = new Vector2(-15f, 0f);
            drt.sizeDelta = new Vector2(190f, 44f);

            // Text value
            int currentIdx = Mathf.Clamp(defaultIdx, 0, options.Length - 1);
            GameObject valObj = new GameObject("ValueText");
            valObj.transform.SetParent(ddObj.transform, false);
            Text valTxt = valObj.AddComponent<Text>();
            valTxt.text = options[currentIdx];
            valTxt.font = font;
            valTxt.fontSize = 17;
            valTxt.alignment = TextAnchor.MiddleLeft;
            valTxt.color = Color.white;
            valTxt.raycastTarget = false;
            RectTransform vrt = valObj.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0f, 0f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.offsetMin = new Vector2(18f, 0f);
            vrt.offsetMax = new Vector2(-36f, 0f);

            // Right Chevron Arrow "∨"
            GameObject arrObj = new GameObject("Arrow");
            arrObj.transform.SetParent(ddObj.transform, false);
            Text arrTxt = arrObj.AddComponent<Text>();
            arrTxt.text = "∨";
            arrTxt.font = font;
            arrTxt.fontSize = 16;
            arrTxt.alignment = TextAnchor.MiddleCenter;
            arrTxt.color = new Color(0.0f, 0.85f, 1.0f, 0.9f);
            arrTxt.raycastTarget = false;
            RectTransform art = arrObj.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(1f, 0.5f);
            art.anchorMax = new Vector2(1f, 0.5f);
            art.pivot = new Vector2(1f, 0.5f);
            art.anchoredPosition = new Vector2(-14f, 0f);
            art.sizeDelta = new Vector2(24f, 24f);

            btn.onClick.AddListener(() =>
            {
                currentIdx = (currentIdx + 1) % options.Length;
                string chosen = options[currentIdx];
                if (valTxt != null) valTxt.text = chosen;
                onSelected?.Invoke(chosen);
                ShowToast($"{label}: {chosen}", ToastType.Info);
            });
        }

        private void CreateModernToggleRow(Transform parent, string label, bool initialOn, float y, Font font, Action<bool> onToggled)
        {
            GameObject row = new GameObject("Row_" + label);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(0f, 48f);

            // Left Label
            GameObject lbObj = new GameObject("Label");
            lbObj.transform.SetParent(row.transform, false);
            Text lb = lbObj.AddComponent<Text>();
            lb.text = label;
            lb.font = font;
            lb.fontSize = 20;
            lb.alignment = TextAnchor.MiddleLeft;
            lb.color = new Color(0.9f, 0.95f, 1.0f, 0.95f);
            RectTransform lrt = lbObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(0.6f, 0.5f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.anchoredPosition = new Vector2(10f, 0f);
            lrt.sizeDelta = new Vector2(0f, 40f);

            // Right Toggle Capsule
            bool state = initialOn;
            GameObject toggleObj = new GameObject("ToggleCapsule");
            toggleObj.transform.SetParent(row.transform, false);

            Image toggleImg = toggleObj.AddComponent<Image>();
            Sprite onSprite = ArcaneAssetLoader.LoadArcaneSprite("settings_toggle_on.png");
            Sprite offSprite = ArcaneAssetLoader.LoadArcaneSprite("settings_toggle_off.png");
            toggleImg.sprite = state ? onSprite : offSprite;
            toggleImg.preserveAspect = true;

            Button btn = toggleObj.AddComponent<Button>();
            toggleObj.AddComponent<MenuButtonEffect>();

            RectTransform trt = toggleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(1f, 0.5f);
            trt.anchorMax = new Vector2(1f, 0.5f);
            trt.pivot = new Vector2(1f, 0.5f);
            trt.anchoredPosition = new Vector2(-22f, 0f);
            trt.sizeDelta = new Vector2(68f, 36f);

            btn.onClick.AddListener(() =>
            {
                state = !state;
                toggleImg.sprite = state ? onSprite : offSprite;
                onToggled?.Invoke(state);
                ShowToast($"{label}: {(state ? "BẬT" : "TẮT")}", ToastType.Info);
            });
        }

        // =========================================================================
        // SCREEN 6: HƯỚNG DẪN (Interactive Tutorial Modal with Dynamic 2D Board & AR Visuals)
        // =========================================================================
        private void BuildScreen6Tutorial(Transform parent, Font font)
        {
            _tutorialModal = CreateModalOverlay(parent, "Screen6_TutorialModal");

            GameObject cardObj = new GameObject("TutorialCard");
            cardObj.transform.SetParent(_tutorialModal.transform, false);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1200, 720, 24f, ColGlassFill, ColCyanBorder, 2f);
            cardBg.type = Image.Type.Sliced;

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1200f, 720f);

            CreateModalHeader(cardObj.transform, AegisLocalization.T("<  HƯỚNG DẪN", "<  TUTORIAL"), () => _tutorialModal.SetActive(false), font);

            // Sub-Tabs Header (4 tabs: Cơ bản, Luật chơi, Các quân cờ, AR)
            GameObject tabsHeader = new GameObject("TutorialTabs");
            tabsHeader.transform.SetParent(cardObj.transform, false);
            RectTransform thRt = tabsHeader.AddComponent<RectTransform>();
            thRt.anchoredPosition = new Vector2(0f, 252f);
            thRt.sizeDelta = new Vector2(900f, 44f);

            string[] tabLabels = {
                AegisLocalization.T("Cơ bản", "Basics"),
                AegisLocalization.T("Luật chơi", "Rules"),
                AegisLocalization.T("Các quân cờ", "Pieces"),
                "AR"
            };

            float tabWidth = 210f;
            for (int i = 0; i < 4; i++)
            {
                int tabIdx = i;
                float x = (i - 1.5f) * tabWidth;
                GameObject tabObj = new GameObject("Tab_" + i);
                tabObj.transform.SetParent(tabsHeader.transform, false);

                Image bg = tabObj.AddComponent<Image>();
                bg.type = Image.Type.Sliced;
                _tutorialTabBgs[i] = bg;

                Button btn = tabObj.AddComponent<Button>();
                btn.onClick.AddListener(() => SwitchTutorialTab((TutorialTabType)tabIdx));
                tabObj.AddComponent<MenuButtonEffect>();

                RectTransform rt = tabObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x, 0f);
                rt.sizeDelta = new Vector2(195f, 44f);

                Text tabTxt = CreateTextComp(tabObj.transform, tabLabels[i], 19, FontStyle.Bold, ColTextWhite, font, Vector2.zero, new Vector2(190f, 34f));
                _tutorialTabTexts[i] = tabTxt;
            }

            // Left: 2D Tactical Diagram Container
            GameObject leftContainer = new GameObject("Left_DiagramContainer");
            leftContainer.transform.SetParent(cardObj.transform, false);
            Image lcBg = leftContainer.AddComponent<Image>();
            lcBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(410, 440, 16f, new Color(0.04f, 0.08f, 0.14f, 0.85f), ColMutedBorder, 1.2f);
            lcBg.type = Image.Type.Sliced;
            RectTransform lcRt = leftContainer.GetComponent<RectTransform>();
            lcRt.anchoredPosition = new Vector2(-285f, -10f);
            lcRt.sizeDelta = new Vector2(410f, 440f);

            // Diagram Image (360x360)
            GameObject boardObj = new GameObject("BoardDiagram");
            boardObj.transform.SetParent(leftContainer.transform, false);
            _tutorialDiagramImg = boardObj.AddComponent<Image>();
            _tutorialDiagramImg.preserveAspect = true;
            RectTransform bRt = boardObj.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(0f, 22f);
            bRt.sizeDelta = new Vector2(360f, 360f);

            // Piece / Overlay Container
            GameObject overlayObj = new GameObject("PieceOverlay");
            overlayObj.transform.SetParent(boardObj.transform, false);
            _tutorialPieceOverlay = overlayObj.transform;
            RectTransform oRt = overlayObj.AddComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero;
            oRt.anchorMax = Vector2.one;
            oRt.offsetMin = Vector2.zero;
            oRt.offsetMax = Vector2.zero;

            // Caption Text
            GameObject capObj = new GameObject("DiagramCaption");
            capObj.transform.SetParent(leftContainer.transform, false);
            _tutorialCaptionTxt = capObj.AddComponent<Text>();
            _tutorialCaptionTxt.font = font;
            _tutorialCaptionTxt.fontSize = 14;
            _tutorialCaptionTxt.fontStyle = FontStyle.Italic;
            _tutorialCaptionTxt.alignment = TextAnchor.MiddleCenter;
            _tutorialCaptionTxt.color = ColCyanGlow;
            _tutorialCaptionTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tutorialCaptionTxt.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform capRt = capObj.GetComponent<RectTransform>();
            capRt.anchoredPosition = new Vector2(0f, -185f);
            capRt.sizeDelta = new Vector2(390f, 40f);

            // Right: Guide Information & Steps
            GameObject rightContainer = new GameObject("Right_GuideContainer");
            rightContainer.transform.SetParent(cardObj.transform, false);
            RectTransform rcRt = rightContainer.AddComponent<RectTransform>();
            rcRt.anchoredPosition = new Vector2(275f, -10f);
            rcRt.sizeDelta = new Vector2(590f, 440f);

            // Badge / Category Indicator
            GameObject badgeObj = new GameObject("CategoryBadge");
            badgeObj.transform.SetParent(rightContainer.transform, false);
            _tutorialBadgeTxt = badgeObj.AddComponent<Text>();
            _tutorialBadgeTxt.font = font;
            _tutorialBadgeTxt.fontSize = 15;
            _tutorialBadgeTxt.fontStyle = FontStyle.Bold;
            _tutorialBadgeTxt.alignment = TextAnchor.MiddleLeft;
            _tutorialBadgeTxt.color = ColGoldBorder;
            RectTransform bdRt = badgeObj.GetComponent<RectTransform>();
            bdRt.anchoredPosition = new Vector2(0f, 195f);
            bdRt.sizeDelta = new Vector2(580f, 24f);

            // Main Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(rightContainer.transform, false);
            _tutorialTitleTxt = titleObj.AddComponent<Text>();
            _tutorialTitleTxt.font = font;
            _tutorialTitleTxt.fontSize = 25;
            _tutorialTitleTxt.fontStyle = FontStyle.Bold;
            _tutorialTitleTxt.alignment = TextAnchor.MiddleLeft;
            _tutorialTitleTxt.color = ColTextWhite;
            RectTransform ttRt = titleObj.GetComponent<RectTransform>();
            ttRt.anchoredPosition = new Vector2(0f, 165f);
            ttRt.sizeDelta = new Vector2(580f, 32f);

            // Overview Paragraph
            GameObject overviewObj = new GameObject("Overview");
            overviewObj.transform.SetParent(rightContainer.transform, false);
            _tutorialOverviewTxt = overviewObj.AddComponent<Text>();
            _tutorialOverviewTxt.font = font;
            _tutorialOverviewTxt.fontSize = 15;
            _tutorialOverviewTxt.fontStyle = FontStyle.Normal;
            _tutorialOverviewTxt.alignment = TextAnchor.UpperLeft;
            _tutorialOverviewTxt.color = new Color(0.85f, 0.92f, 1.0f);
            _tutorialOverviewTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tutorialOverviewTxt.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform ovRt = overviewObj.GetComponent<RectTransform>();
            ovRt.anchoredPosition = new Vector2(0f, 120f);
            ovRt.sizeDelta = new Vector2(580f, 48f);

            // Steps Guide Container
            GameObject stepsGroup = new GameObject("StepsGroup");
            stepsGroup.transform.SetParent(rightContainer.transform, false);
            RectTransform sgRt = stepsGroup.AddComponent<RectTransform>();
            sgRt.anchoredPosition = new Vector2(0f, 5f);
            sgRt.sizeDelta = new Vector2(580f, 180f);

            for (int i = 0; i < 3; i++)
            {
                float y = 55f - i * 58f;
                GameObject row = new GameObject($"Step_{i + 1}");
                row.transform.SetParent(stepsGroup.transform, false);
                _tutorialStepRows[i] = row;
                RectTransform rRt = row.AddComponent<RectTransform>();
                rRt.anchoredPosition = new Vector2(0f, y);
                rRt.sizeDelta = new Vector2(580f, 54f);

                // Number badge
                GameObject numObj = new GameObject("Badge");
                numObj.transform.SetParent(row.transform, false);
                Image nbImg = numObj.AddComponent<Image>();
                nbImg.sprite = ArcaneAssetLoader.GetCircleSprite(32, ColCyanBorder, Color.white, 1.5f);
                RectTransform nbRt = numObj.GetComponent<RectTransform>();
                nbRt.anchoredPosition = new Vector2(-265f, 8f);
                nbRt.sizeDelta = new Vector2(32f, 32f);

                _tutorialStepBadges[i] = CreateTextComp(numObj.transform, (i + 1).ToString(), 18, FontStyle.Bold, ColDarkBtnText, font, Vector2.zero, new Vector2(32f, 32f));

                // Step Title & Description
                _tutorialStepTitles[i] = CreateTextComp(row.transform, "", 18, FontStyle.Bold, ColTextWhite, font, new Vector2(30f, 12f), new Vector2(520f, 26f), TextAnchor.MiddleLeft);
                _tutorialStepDescs[i] = CreateTextComp(row.transform, "", 14, FontStyle.Normal, new Color(0.82f, 0.90f, 0.98f), font, new Vector2(30f, -14f), new Vector2(520f, 28f), TextAnchor.MiddleLeft);
                _tutorialStepDescs[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            // Tactical Tip Box
            _tutorialTipContainer = new GameObject("TipContainer");
            _tutorialTipContainer.transform.SetParent(rightContainer.transform, false);
            Image tipBg = _tutorialTipContainer.AddComponent<Image>();
            tipBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(580, 52, 10f, new Color(0.06f, 0.12f, 0.20f, 0.9f), ColGoldBorder, 1.2f);
            tipBg.type = Image.Type.Sliced;
            RectTransform tipRt = _tutorialTipContainer.GetComponent<RectTransform>();
            tipRt.anchoredPosition = new Vector2(0f, -175f);
            tipRt.sizeDelta = new Vector2(580f, 52f);

            GameObject tipTxtObj = new GameObject("TipText");
            tipTxtObj.transform.SetParent(_tutorialTipContainer.transform, false);
            _tutorialTipTxt = tipTxtObj.AddComponent<Text>();
            _tutorialTipTxt.font = font;
            _tutorialTipTxt.fontSize = 14;
            _tutorialTipTxt.alignment = TextAnchor.MiddleLeft;
            _tutorialTipTxt.color = new Color(1.0f, 0.90f, 0.60f);
            _tutorialTipTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tutorialTipTxt.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform ttRt2 = tipTxtObj.GetComponent<RectTransform>();
            ttRt2.anchorMin = Vector2.zero;
            ttRt2.anchorMax = Vector2.one;
            ttRt2.offsetMin = new Vector2(16f, 4f);
            ttRt2.offsetMax = new Vector2(-16f, -4f);

            // Bottom Navigation Footer
            GameObject footer = new GameObject("TutorialFooter");
            footer.transform.SetParent(cardObj.transform, false);
            RectTransform fRt = footer.AddComponent<RectTransform>();
            fRt.anchoredPosition = new Vector2(0f, -295f);
            fRt.sizeDelta = new Vector2(1100f, 50f);

            // Button Prev
            GameObject prevBtnObj = new GameObject("BtnPrev");
            prevBtnObj.transform.SetParent(footer.transform, false);
            _tutorialBtnPrevBg = prevBtnObj.AddComponent<Image>();
            _tutorialBtnPrevBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(160, 46, 22f, ColGlassFill, ColCyanBorder, 1.5f);
            _tutorialBtnPrevBg.type = Image.Type.Sliced;
            _tutorialBtnPrev = prevBtnObj.AddComponent<Button>();
            _tutorialBtnPrev.onClick.AddListener(PreviousTutorialPage);
            prevBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform pRt = prevBtnObj.GetComponent<RectTransform>();
            pRt.anchoredPosition = new Vector2(-320f, 0f);
            pRt.sizeDelta = new Vector2(160f, 46f);
            _tutorialBtnPrevTxt = CreateTextComp(prevBtnObj.transform, AegisLocalization.T("❮ Trước", "❮ Prev"), 18, FontStyle.Bold, ColCyanGlow, font, Vector2.zero, new Vector2(150f, 36f));

            // Page Indicator
            GameObject indObj = new GameObject("PageIndicator");
            indObj.transform.SetParent(footer.transform, false);
            _tutorialPageIndicatorTxt = indObj.AddComponent<Text>();
            _tutorialPageIndicatorTxt.font = font;
            _tutorialPageIndicatorTxt.fontSize = 20;
            _tutorialPageIndicatorTxt.fontStyle = FontStyle.Bold;
            _tutorialPageIndicatorTxt.alignment = TextAnchor.MiddleCenter;
            _tutorialPageIndicatorTxt.color = ColTextWhite;
            RectTransform indRt = indObj.GetComponent<RectTransform>();
            indRt.anchoredPosition = new Vector2(0f, 0f);
            indRt.sizeDelta = new Vector2(280f, 40f);

            // Button Next
            GameObject nextBtnObj = new GameObject("BtnNext");
            nextBtnObj.transform.SetParent(footer.transform, false);
            _tutorialBtnNextBg = nextBtnObj.AddComponent<Image>();
            _tutorialBtnNextBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(160, 46, 22f, ColCyanBorder, Color.white, 1.5f);
            _tutorialBtnNextBg.type = Image.Type.Sliced;
            _tutorialBtnNext = nextBtnObj.AddComponent<Button>();
            _tutorialBtnNext.onClick.AddListener(NextTutorialPage);
            nextBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform nRt = nextBtnObj.GetComponent<RectTransform>();
            nRt.anchoredPosition = new Vector2(320f, 0f);
            nRt.sizeDelta = new Vector2(160f, 46f);
            _tutorialBtnNextTxt = CreateTextComp(nextBtnObj.transform, AegisLocalization.T("Tiếp theo ❯", "Next ❯"), 18, FontStyle.Bold, ColDarkBtnText, font, Vector2.zero, new Vector2(150f, 36f));

            // Initial view update
            UpdateTutorialUI(font);

            _tutorialModal.SetActive(false);
        }

        private void SwitchTutorialTab(TutorialTabType tab)
        {
            _selectedTutorialTab = tab;
            _currentTutorialPage = 0;
            UpdateTutorialUI();
        }

        private void PreviousTutorialPage()
        {
            if (_currentTutorialPage > 0)
            {
                _currentTutorialPage--;
                UpdateTutorialUI();
            }
        }

        private void NextTutorialPage()
        {
            int total = TutorialRepository.GetPageCount(_selectedTutorialTab);
            if (_currentTutorialPage < total - 1)
            {
                _currentTutorialPage++;
                UpdateTutorialUI();
            }
            else
            {
                // Last page reached in this tab
                int nextTab = (int)_selectedTutorialTab + 1;
                if (nextTab < 4)
                {
                    ShowToast(AegisLocalization.T("Chuyển sang phần hướng dẫn kế tiếp!", "Advancing to next tutorial section!"), ToastType.Info);
                    SwitchTutorialTab((TutorialTabType)nextTab);
                }
                else
                {
                    ShowToast(AegisLocalization.T("Bạn đã hoàn thành toàn bộ hệ thống Hướng dẫn!", "You completed the entire Tutorial!"), ToastType.Success);
                }
            }
        }

        private void UpdateTutorialUI(Font font = null)
        {
            if (font == null && _tutorialTitleTxt != null) font = _tutorialTitleTxt.font;

            // 1. Update Tabs Highlight
            for (int i = 0; i < 4; i++)
            {
                bool active = (i == (int)_selectedTutorialTab);
                if (_tutorialTabBgs[i] != null)
                {
                    _tutorialTabBgs[i].sprite = ArcaneAssetLoader.GetGlassBoxSprite(195, 44, 14f, 
                        active ? ColGlassActive : ColGlassFill, 
                        active ? ColCyanBorder : ColMutedBorder, 
                        active ? 1.8f : 1.0f);
                }
                if (_tutorialTabTexts[i] != null)
                {
                    _tutorialTabTexts[i].color = active ? ColCyanGlow : ColTextWhite;
                    _tutorialTabTexts[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                }
            }

            // 2. Fetch Active Page
            int total = TutorialRepository.GetPageCount(_selectedTutorialTab);
            _currentTutorialPage = Mathf.Clamp(_currentTutorialPage, 0, Mathf.Max(0, total - 1));
            TutorialPageData page = TutorialRepository.GetPage(_selectedTutorialTab, _currentTutorialPage);
            if (page == null) return;

            // 3. Update Text Content
            if (_tutorialBadgeTxt != null) _tutorialBadgeTxt.text = page.GetBadge();
            if (_tutorialTitleTxt != null) _tutorialTitleTxt.text = page.GetTitle();
            if (_tutorialOverviewTxt != null) _tutorialOverviewTxt.text = page.GetOverview();
            if (_tutorialCaptionTxt != null) _tutorialCaptionTxt.text = page.GetCaption();

            // 4. Update Steps
            for (int i = 0; i < 3; i++)
            {
                if (_tutorialStepRows[i] == null) continue;
                if (i < page.steps.Count)
                {
                    _tutorialStepRows[i].SetActive(true);
                    if (_tutorialStepBadges[i] != null) _tutorialStepBadges[i].text = page.steps[i].number;
                    if (_tutorialStepTitles[i] != null) _tutorialStepTitles[i].text = page.steps[i].GetTitle();
                    if (_tutorialStepDescs[i] != null) _tutorialStepDescs[i].text = page.steps[i].GetDesc();
                }
                else
                {
                    _tutorialStepRows[i].SetActive(false);
                }
            }

            // 5. Update Tip Box
            string tip = page.GetTip();
            if (_tutorialTipContainer != null)
            {
                bool hasTip = !string.IsNullOrEmpty(tip);
                _tutorialTipContainer.SetActive(hasTip);
                if (hasTip && _tutorialTipTxt != null) _tutorialTipTxt.text = tip;
            }

            // 6. Update Visual Diagram
            if (_tutorialDiagramImg != null)
            {
                if (_selectedTutorialTab == TutorialTabType.AR)
                {
                    _tutorialDiagramImg.sprite = ArcaneAssetLoader.GetARDiagramSprite(360, page.arGraphicType);
                }
                else
                {
                    _tutorialDiagramImg.sprite = ArcaneAssetLoader.GetCustomBoardDiagram(360, 
                        page.hasSelectedSquare, page.selectedSquare, 
                        page.moveSquares, page.captureSquares, page.specialSquares);
                }
            }

            // Clear old piece overlay markers
            if (_tutorialPieceOverlay != null)
            {
                for (int i = _tutorialPieceOverlay.childCount - 1; i >= 0; i--)
                {
                    Destroy(_tutorialPieceOverlay.GetChild(i).gameObject);
                }

                // If not AR, render piece symbols on the board
                if (_selectedTutorialTab != TutorialTabType.AR && page.pieces != null)
                {
                    float squarePx = 360f / 8f; // 45px per tile
                    Font pieceFont = font ?? _tutorialTitleTxt?.font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    foreach (var p in page.pieces)
                    {
                        GameObject pObj = new GameObject($"Piece_{p.file}_{p.rank}");
                        pObj.transform.SetParent(_tutorialPieceOverlay, false);
                        Text pt = pObj.AddComponent<Text>();
                        pt.text = p.symbol;
                        pt.font = pieceFont;
                        pt.fontSize = 28;
                        pt.fontStyle = FontStyle.Bold;
                        pt.alignment = TextAnchor.MiddleCenter;
                        pt.color = p.isWhite ? Color.white : new Color(0.98f, 0.42f, 0.42f, 1f);

                        // Add outline for sharp visual definition against any square color
                        Outline outline = pObj.AddComponent<Outline>();
                        outline.effectColor = new Color(0.02f, 0.05f, 0.10f, 0.95f);
                        outline.effectDistance = new Vector2(1.5f, -1.5f);

                        RectTransform prt = pObj.GetComponent<RectTransform>();
                        float px = (p.file - 3.5f) * squarePx;
                        float py = (p.rank - 3.5f) * squarePx;
                        prt.anchoredPosition = new Vector2(px, py);
                        prt.sizeDelta = new Vector2(squarePx, squarePx);
                    }
                }
            }

            // 7. Update Footer Navigation
            if (_tutorialPageIndicatorTxt != null)
            {
                _tutorialPageIndicatorTxt.text = AegisLocalization.T(
                    $"Trang {_currentTutorialPage + 1} / {total}", 
                    $"Page {_currentTutorialPage + 1} / {total}");
            }

            if (_tutorialBtnPrev != null)
            {
                bool canPrev = (_currentTutorialPage > 0);
                _tutorialBtnPrev.interactable = canPrev;
                if (_tutorialBtnPrevBg != null)
                {
                    _tutorialBtnPrevBg.color = canPrev ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.5f);
                }
                if (_tutorialBtnPrevTxt != null)
                {
                    _tutorialBtnPrevTxt.color = canPrev ? ColCyanGlow : new Color(0.5f, 0.5f, 0.5f, 0.6f);
                }
            }

            if (_tutorialBtnNext != null && _tutorialBtnNextTxt != null)
            {
                bool isLastPage = (_currentTutorialPage == total - 1);
                bool hasNextTab = ((int)_selectedTutorialTab < 3);

                if (isLastPage && !hasNextTab)
                {
                    _tutorialBtnNextTxt.text = AegisLocalization.T("Hoàn thành ✓", "Complete ✓");
                    _tutorialBtnNextTxt.color = ColDarkBtnText;
                    if (_tutorialBtnNextBg != null) _tutorialBtnNextBg.color = ColGoldBorder;
                }
                else if (isLastPage && hasNextTab)
                {
                    _tutorialBtnNextTxt.text = AegisLocalization.T("Mục tiếp ❯❯", "Next Tab ❯❯");
                    _tutorialBtnNextTxt.color = ColDarkBtnText;
                    if (_tutorialBtnNextBg != null) _tutorialBtnNextBg.color = ColGoldBorder;
                }
                else
                {
                    _tutorialBtnNextTxt.text = AegisLocalization.T("Tiếp theo ❯", "Next ❯");
                    _tutorialBtnNextTxt.color = ColDarkBtnText;
                    if (_tutorialBtnNextBg != null) _tutorialBtnNextBg.color = Color.white;
                }
            }
        }

        // =========================================================================
        // SCREEN 7: CỬA HÀNG (Shop Modal with 4 skins & gold currency counter)
        // =========================================================================
        private void BuildScreen7Shop(Transform parent, Font font)
        {
            _shopModal = CreateModalOverlay(parent, "Screen7_ShopModal");

            // Centered Main Window Card (1520 x 820)
            GameObject cardObj = new GameObject("ShopCard");
            cardObj.transform.SetParent(_shopModal.transform, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1520f, 820f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1520, 820, 24f, new Color(0.03f, 0.08f, 0.16f, 0.94f), ColCyanBorder, 2f);
            cardBg.type = Image.Type.Sliced;

            // Window Header Bar (Height 64px, anchored top)
            GameObject headerObj = new GameObject("ShopHeader");
            headerObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 1f);
            hRt.anchorMax = new Vector2(0.5f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -16f);
            hRt.sizeDelta = new Vector2(1440f, 60f);

            // Back Button [ ❮ ] (Left)
            GameObject backBtnObj = new GameObject("BtnBack");
            backBtnObj.transform.SetParent(headerObj.transform, false);
            Image bbImg = backBtnObj.AddComponent<Image>();
            bbImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(48, 48, 10f, new Color(0.04f, 0.12f, 0.22f, 0.90f), ColCyanBorder, 1.5f);
            Button bbBtn = backBtnObj.AddComponent<Button>();
            bbBtn.onClick.AddListener(() =>
            {
                CloseModal(_shopModal);
            });
            backBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform bbRt = backBtnObj.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0f, 0.5f);
            bbRt.anchorMax = new Vector2(0f, 0.5f);
            bbRt.pivot = new Vector2(0f, 0.5f);
            bbRt.anchoredPosition = new Vector2(10f, 0f);
            bbRt.sizeDelta = new Vector2(48f, 48f);

            GameObject backIconObj = new GameObject("BackIcon");
            backIconObj.transform.SetParent(backBtnObj.transform, false);
            Image backIconImg = backIconObj.AddComponent<Image>();
            Sprite backSprite = ArcaneAssetLoader.LoadArcaneSprite("icon_nav_back.png");
            if (backSprite != null)
            {
                backIconImg.sprite = backSprite;
                backIconImg.color = ColCyanGlow;
            }
            else
            {
                Text bkt = backIconObj.AddComponent<Text>();
                bkt.text = "<";
                bkt.font = font;
                bkt.fontSize = 24;
                bkt.fontStyle = FontStyle.Bold;
                bkt.alignment = TextAnchor.MiddleCenter;
                bkt.color = ColCyanGlow;
            }
            RectTransform backIconRt = backIconObj.GetComponent<RectTransform>();
            backIconRt.anchorMin = Vector2.zero;
            backIconRt.anchorMax = Vector2.one;
            backIconRt.offsetMin = new Vector2(10f, 10f);
            backIconRt.offsetMax = new Vector2(-10f, -10f);

            // Title: "CỬA HÀNG" placed next to back button
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = AegisLocalization.T("CỬA HÀNG", "SHOP");
            titleTxt.font = font;
            titleTxt.fontSize = 30;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = ColCyanGlow;
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(0f, 0.5f);
            tRt.pivot = new Vector2(0f, 0.5f);
            tRt.anchoredPosition = new Vector2(70f, 0f);
            tRt.sizeDelta = new Vector2(250f, 40f);

            // Gold Currency Badge in Header (Right)
            GameObject coinObj = new GameObject("CoinBadge");
            coinObj.transform.SetParent(headerObj.transform, false);
            Image cbImg = coinObj.AddComponent<Image>();
            Sprite coinBadgeSprite = ArcaneAssetLoader.LoadArcaneSprite("shop_coin_badge.png");
            if (coinBadgeSprite != null)
            {
                cbImg.sprite = coinBadgeSprite;
                cbImg.type = Image.Type.Simple;
            }
            else
            {
                cbImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(140, 44, 14f, new Color(0.18f, 0.12f, 0.04f, 0.9f), ColGoldBorder, 1.5f);
                cbImg.type = Image.Type.Sliced;
            }
            RectTransform cbRt = coinObj.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 0.5f);
            cbRt.anchorMax = new Vector2(1f, 0.5f);
            cbRt.pivot = new Vector2(1f, 0.5f);
            cbRt.anchoredPosition = new Vector2(-10f, 0f);
            cbRt.sizeDelta = new Vector2(140f, 44f);

            // Gold Coin Icon inside Badge
            GameObject goldIconObj = new GameObject("CoinIcon");
            goldIconObj.transform.SetParent(coinObj.transform, false);
            Image goldIconImg = goldIconObj.AddComponent<Image>();
            Sprite coinSprite = ArcaneAssetLoader.LoadArcaneSprite("icon_gold_coin.png");
            if (coinSprite != null)
            {
                goldIconImg.sprite = coinSprite;
                goldIconImg.preserveAspect = true;
            }
            else
            {
                goldIconImg.color = ColGoldBorder;
            }
            RectTransform giRt = goldIconObj.GetComponent<RectTransform>();
            giRt.anchorMin = new Vector2(0f, 0.5f);
            giRt.anchorMax = new Vector2(0f, 0.5f);
            giRt.pivot = new Vector2(0f, 0.5f);
            giRt.anchoredPosition = new Vector2(12f, 0f);
            giRt.sizeDelta = new Vector2(24f, 24f);

            // Gold Text
            GameObject goldTxtObj = new GameObject("GoldTxt");
            goldTxtObj.transform.SetParent(coinObj.transform, false);
            _shopGoldTxt = goldTxtObj.AddComponent<Text>();
            _shopGoldTxt.text = _currentGold.ToString();
            _shopGoldTxt.font = font;
            _shopGoldTxt.fontSize = 22;
            _shopGoldTxt.fontStyle = FontStyle.Bold;
            _shopGoldTxt.alignment = TextAnchor.MiddleRight;
            _shopGoldTxt.color = ColGoldBorder;
            _shopGoldTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _shopGoldTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform gtRt = goldTxtObj.GetComponent<RectTransform>();
            gtRt.anchorMin = new Vector2(0f, 0f);
            gtRt.anchorMax = new Vector2(1f, 1f);
            gtRt.offsetMin = new Vector2(40f, 0f);
            gtRt.offsetMax = new Vector2(-16f, 0f);

            // Category Sub-Tabs: Skin quân cờ (active), Bàn cờ, Hiệu ứng (Left-aligned)
            GameObject shopTabs = new GameObject("ShopTabs");
            shopTabs.transform.SetParent(cardObj.transform, false);
            RectTransform stRt = shopTabs.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.5f, 1f);
            stRt.anchorMax = new Vector2(0.5f, 1f);
            stRt.pivot = new Vector2(0.5f, 1f);
            stRt.anchoredPosition = new Vector2(0f, -84f);
            stRt.sizeDelta = new Vector2(1440f, 48f);

            string[] sTabs = {
                AegisLocalization.T("Skin quân cờ", "Piece Skins"),
                AegisLocalization.T("Bàn cờ", "Chessboards"),
                AegisLocalization.T("Hiệu ứng", "Effects")
            };
            Sprite tabActSprite = ArcaneAssetLoader.LoadArcaneSprite("shop_tab_active.png");
            Sprite tabInactSprite = ArcaneAssetLoader.LoadArcaneSprite("shop_tab_inactive.png");

            for (int i = 0; i < 3; i++)
            {
                int tabIdx = i;
                bool active = (i == _selectedShopTab);
                GameObject tObj = new GameObject("Tab_" + sTabs[i]);
                tObj.transform.SetParent(shopTabs.transform, false);

                Image bg = tObj.AddComponent<Image>();
                bg.sprite = active ? tabActSprite : tabInactSprite;
                bg.type = Image.Type.Simple;
                _shopTabBgs[i] = bg;

                Button btn = tObj.AddComponent<Button>();
                btn.onClick.AddListener(() => SwitchShopTab(tabIdx));
                tObj.AddComponent<MenuButtonEffect>();

                RectTransform rt = tObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(10f + i * 236f, 0f);
                rt.sizeDelta = new Vector2(220f, 46f);

                GameObject txtObj = new GameObject("Txt");
                txtObj.transform.SetParent(tObj.transform, false);
                Text txtComp = txtObj.AddComponent<Text>();
                txtComp.text = sTabs[i];
                txtComp.font = font;
                txtComp.fontSize = 20;
                txtComp.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                txtComp.alignment = TextAnchor.MiddleCenter;
                txtComp.color = active ? ColCyanGlow : ColTextWhite;
                txtComp.horizontalOverflow = HorizontalWrapMode.Overflow;
                txtComp.verticalOverflow = VerticalWrapMode.Overflow;
                _shopTabTexts[i] = txtComp;

                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
            }

            // Container 0: 4 Skin Cards Grid
            GameObject skinsContainer = new GameObject("Category_Skins");
            skinsContainer.transform.SetParent(cardObj.transform, false);
            _shopCategoryContainers[0] = skinsContainer;
            RectTransform scRt = skinsContainer.AddComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.5f, 0.5f);
            scRt.anchorMax = new Vector2(0.5f, 0.5f);
            scRt.pivot = new Vector2(0.5f, 0.5f);
            scRt.anchoredPosition = new Vector2(0f, -14f);
            scRt.sizeDelta = new Vector2(1440f, 480f);

            string[] skinNames = { "Knight of Light", "Dark Legion", "Cyber Future", "Forest Spirit" };
            string[] skinTextures = { "skin_knight_light.png", "skin_dark_legion.png", "skin_cyber_future.png", "skin_forest_spirit.png" };
            string[] frameSprites = { "shop_card_frame_default.png", "shop_card_frame_crimson.png", "shop_card_frame_cyan.png", "shop_card_frame_default.png" };
            int[] skinPrices = { 1000, 1000, 1500, 1500 };

            float skinCardWidth = 330f;
            float skinCardGap = 35f;
            float skinStartX = -((4 * skinCardWidth + 3 * skinCardGap) / 2f) + skinCardWidth / 2f;

            for (int i = 0; i < 4; i++)
            {
                CreateShopSkinCard(skinsContainer.transform, skinNames[i], skinTextures[i], frameSprites[i], skinPrices[i], new Vector2(skinStartX + i * (skinCardWidth + skinCardGap), 0f), font);
            }

            // Container 1: Chessboards (Category 1)
            GameObject boardsContainer = new GameObject("Category_Boards");
            boardsContainer.transform.SetParent(cardObj.transform, false);
            _shopCategoryContainers[1] = boardsContainer;
            RectTransform bcRt = boardsContainer.AddComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0.5f);
            bcRt.anchorMax = new Vector2(0.5f, 0.5f);
            bcRt.pivot = new Vector2(0.5f, 0.5f);
            bcRt.anchoredPosition = new Vector2(0f, -14f);
            bcRt.sizeDelta = new Vector2(1440f, 480f);
            string[] boardNames = {
                AegisLocalization.T("Bàn Cờ Cyber Neon", "Cyber Neon Board"),
                AegisLocalization.T("Bàn Cờ Cổ Điển", "Classic Board"),
                AegisLocalization.T("Bàn Cờ Dung Nham", "Magma Board"),
                AegisLocalization.T("Bàn Cờ Pha Lê", "Crystal Board")
            };
            int[] boardPrices = { 800, 500, 1200, 1600 };
            for (int i = 0; i < 4; i++)
            {
                CreateShopSkinCard(boardsContainer.transform, boardNames[i], skinTextures[i], frameSprites[i], boardPrices[i], new Vector2(skinStartX + i * (skinCardWidth + skinCardGap), 0f), font);
            }
            boardsContainer.SetActive(false);

            // Container 2: Effects (Category 2)
            GameObject effectsContainer = new GameObject("Category_Effects");
            effectsContainer.transform.SetParent(cardObj.transform, false);
            _shopCategoryContainers[2] = effectsContainer;
            RectTransform ecRt = effectsContainer.AddComponent<RectTransform>();
            ecRt.anchorMin = new Vector2(0.5f, 0.5f);
            ecRt.anchorMax = new Vector2(0.5f, 0.5f);
            ecRt.pivot = new Vector2(0.5f, 0.5f);
            ecRt.anchoredPosition = new Vector2(0f, -14f);
            ecRt.sizeDelta = new Vector2(1440f, 480f);
            string[] effectNames = {
                AegisLocalization.T("Sấm Sét Arcane", "Arcane Lightning"),
                AegisLocalization.T("Hào Quang Thánh", "Divine Aura"),
                AegisLocalization.T("Lửa Hỏa Ngục", "Hellfire Blaze"),
                AegisLocalization.T("Bụi Sao Vũ Trụ", "Cosmic Stardust")
            };
            int[] effectPrices = { 600, 900, 1100, 1400 };
            for (int i = 0; i < 4; i++)
            {
                CreateShopSkinCard(effectsContainer.transform, effectNames[i], skinTextures[(i + 2) % 4], frameSprites[i], effectPrices[i], new Vector2(skinStartX + i * (skinCardWidth + skinCardGap), 0f), font);
            }
            effectsContainer.SetActive(false);

            // Bottom Navigation Dock Bar (Height 74px, anchored bottom)
            BuildShopBottomDock(cardObj.transform);

            _shopModal.SetActive(false);
        }

        private void BuildShopBottomDock(Transform parent)
        {
            GameObject dockObj = new GameObject("ShopBottomDock");
            dockObj.transform.SetParent(parent, false);
            RectTransform dRt = dockObj.AddComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0f);
            dRt.anchorMax = new Vector2(0.5f, 0f);
            dRt.pivot = new Vector2(0.5f, 0f);
            dRt.anchoredPosition = new Vector2(0f, 18f);
            dRt.sizeDelta = new Vector2(1440f, 74f);

            Image dockBg = dockObj.AddComponent<Image>();
            Sprite dockSprite = ArcaneAssetLoader.LoadArcaneSprite("shop_bottom_dock_bg.png");
            if (dockSprite != null)
            {
                dockBg.sprite = dockSprite;
                dockBg.type = Image.Type.Simple;
            }
            else
            {
                dockBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1440, 74, 20f, new Color(0.03f, 0.08f, 0.16f, 0.95f), ColCyanBorder, 1.5f);
                dockBg.type = Image.Type.Sliced;
            }

            // 4 Dock Icon Buttons: 0: Castle/Home, 1: Crossed Swords/Shop (Active), 2: Shield/Inventory, 3: Trophy/Rank
            string[] dockIcons = { "dock_icon_play.png", "dock_icon_shop.png", "dock_icon_inventory.png", "dock_icon_rank.png" };
            Action[] dockActions = {
                () => { _shopModal.SetActive(false); NavigateToChooseMode(); },
                () => { ShowToast(AegisLocalization.T("Bạn đang ở Cửa hàng!", "You are in the Shop!"), ToastType.Info); },
                () => { ShowToast(AegisLocalization.T("Bộ sưu tập quân cờ đã mở!", "Piece collection opened!"), ToastType.Info); },
                () => { ShowToast(AegisLocalization.T("Bảng xếp hạng mùa giải mới!", "New season leaderboard!"), ToastType.Info); }
            };

            float slotWidth = 360f;
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                GameObject btnObj = new GameObject("DockBtn_" + i, typeof(RectTransform));
                btnObj.transform.SetParent(dockObj.transform, false);

                Image btnHitImg = btnObj.AddComponent<Image>();
                btnHitImg.color = Color.clear;

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = btnHitImg;
                btn.onClick.AddListener(() => dockActions[idx]?.Invoke());
                btnObj.AddComponent<MenuButtonEffect>();

                RectTransform bRt = btnObj.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.5f, 0.5f);
                bRt.anchorMax = new Vector2(0.5f, 0.5f);
                bRt.pivot = new Vector2(0.5f, 0.5f);
                bRt.anchoredPosition = new Vector2(-540f + i * slotWidth, 0f);
                bRt.sizeDelta = new Vector2(slotWidth - 20f, 64f);

                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(btnObj.transform, false);
                Image img = iconObj.AddComponent<Image>();
                Sprite iconSp = ArcaneAssetLoader.LoadArcaneSprite(dockIcons[i]);
                if (iconSp != null)
                {
                    img.sprite = iconSp;
                    img.preserveAspect = true;
                }
                else
                {
                    img.color = (i == 1) ? ColCyanGlow : ColTextWhite;
                }
                RectTransform iRt = iconObj.GetComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0.5f, 0.5f);
                iRt.anchorMax = new Vector2(0.5f, 0.5f);
                iRt.pivot = new Vector2(0.5f, 0.5f);
                iRt.anchoredPosition = Vector2.zero;
                iRt.sizeDelta = new Vector2(36f, 36f);
            }
        }

        private void SwitchShopTab(int tabIndex)
        {
            _selectedShopTab = tabIndex;
            Sprite activeSprite = ArcaneAssetLoader.LoadArcaneSprite("shop_tab_active.png");
            Sprite inactiveSprite = ArcaneAssetLoader.LoadArcaneSprite("shop_tab_inactive.png");

            for (int i = 0; i < _shopTabBgs.Length; i++)
            {
                if (_shopTabBgs[i] != null)
                {
                    _shopTabBgs[i].sprite = (i == tabIndex) ? activeSprite : inactiveSprite;
                }
                if (_shopTabTexts[i] != null)
                {
                    _shopTabTexts[i].color = (i == tabIndex) ? ColCyanGlow : ColTextWhite;
                    _shopTabTexts[i].fontStyle = (i == tabIndex) ? FontStyle.Bold : FontStyle.Normal;
                }
                if (_shopCategoryContainers[i] != null)
                {
                    _shopCategoryContainers[i].SetActive(i == tabIndex);
                }
            }
        }

        private void CreateShopSkinCard(Transform parent, string name, string textureName, string frameSpriteName, int price, Vector2 pos, Font font)
        {
            GameObject card = new GameObject("Skin_" + name);
            card.transform.SetParent(parent, false);

            Image bg = card.AddComponent<Image>();
            Sprite frameSp = ArcaneAssetLoader.LoadArcaneSprite(frameSpriteName);
            if (frameSp != null)
            {
                bg.sprite = frameSp;
                bg.type = Image.Type.Simple;
            }
            else
            {
                bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(330, 480, 20f, ColGlassFill, ColCyanBorder, 1.5f);
                bg.type = Image.Type.Sliced;
            }

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(330f, 480f);

            // Skin Artwork Image - fills upper 68% of card
            GameObject imgObj = new GameObject("Artwork");
            imgObj.transform.SetParent(card.transform, false);
            Image img = imgObj.AddComponent<Image>();
            Sprite s = ArcaneAssetLoader.LoadArcaneSprite(textureName);
            if (s != null)
            {
                img.sprite = s;
                img.preserveAspect = false;
            }
            else
            {
                img.color = new Color(0.1f, 0.2f, 0.35f);
            }
            RectTransform irt = imgObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 1f);
            irt.anchorMax = new Vector2(0.5f, 1f);
            irt.pivot = new Vector2(0.5f, 1f);
            irt.anchoredPosition = new Vector2(0f, -14f);
            irt.sizeDelta = new Vector2(302f, 330f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(card.transform, false);
            Text titleComp = titleObj.AddComponent<Text>();
            titleComp.text = name;
            titleComp.font = font;
            titleComp.fontSize = 24;
            titleComp.fontStyle = FontStyle.Bold;
            titleComp.alignment = TextAnchor.MiddleCenter;
            titleComp.color = ColTextWhite;
            titleComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleComp.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0f);
            tRt.anchorMax = new Vector2(0.5f, 0f);
            tRt.pivot = new Vector2(0.5f, 0f);
            tRt.anchoredPosition = new Vector2(0f, 72f);
            tRt.sizeDelta = new Vector2(300f, 32f);

            // Buy / Equip Button (Pill with Coin icon + Price)
            GameObject buyBtnObj = new GameObject("BtnBuy");
            buyBtnObj.transform.SetParent(card.transform, false);
            Image bbImg = buyBtnObj.AddComponent<Image>();
            Sprite badgeSp = ArcaneAssetLoader.LoadArcaneSprite("shop_coin_badge.png");
            if (badgeSp != null)
            {
                bbImg.sprite = badgeSp;
                bbImg.type = Image.Type.Simple;
            }
            else
            {
                bbImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(180, 42, 14f, new Color(0.18f, 0.12f, 0.04f, 0.95f), ColGoldBorder, 1.5f);
                bbImg.type = Image.Type.Sliced;
            }
            Button bBtn = buyBtnObj.AddComponent<Button>();
            bBtn.onClick.AddListener(() =>
            {
                ShowToast(AegisLocalization.T($"Đã trang bị skin '{name}'!", $"Equipped skin '{name}'!"), ToastType.Success);
            });
            buyBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform bbrt = buyBtnObj.GetComponent<RectTransform>();
            bbrt.anchorMin = new Vector2(0.5f, 0f);
            bbrt.anchorMax = new Vector2(0.5f, 0f);
            bbrt.pivot = new Vector2(0.5f, 0f);
            bbrt.anchoredPosition = new Vector2(0f, 20f);
            bbrt.sizeDelta = new Vector2(180f, 42f);

            // Gold Coin Icon inside button
            GameObject btnCoinObj = new GameObject("CoinIcon");
            btnCoinObj.transform.SetParent(buyBtnObj.transform, false);
            Image btnCoinImg = btnCoinObj.AddComponent<Image>();
            Sprite coinSprite = ArcaneAssetLoader.LoadArcaneSprite("icon_gold_coin.png");
            if (coinSprite != null)
            {
                btnCoinImg.sprite = coinSprite;
                btnCoinImg.preserveAspect = true;
            }
            else
            {
                btnCoinImg.color = ColGoldBorder;
            }
            RectTransform bciRt = btnCoinObj.GetComponent<RectTransform>();
            bciRt.anchorMin = new Vector2(0f, 0.5f);
            bciRt.anchorMax = new Vector2(0f, 0.5f);
            bciRt.pivot = new Vector2(0f, 0.5f);
            bciRt.anchoredPosition = new Vector2(24f, 0f);
            bciRt.sizeDelta = new Vector2(22f, 22f);

            // Price Text
            GameObject priceTxtObj = new GameObject("PriceTxt");
            priceTxtObj.transform.SetParent(buyBtnObj.transform, false);
            Text priceComp = priceTxtObj.AddComponent<Text>();
            priceComp.text = price.ToString();
            priceComp.font = font;
            priceComp.fontSize = 20;
            priceComp.fontStyle = FontStyle.Bold;
            priceComp.alignment = TextAnchor.MiddleLeft;
            priceComp.color = ColGoldBorder;
            priceComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            priceComp.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform ptRt = priceTxtObj.GetComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0f, 0f);
            ptRt.anchorMax = new Vector2(1f, 1f);
            ptRt.offsetMin = new Vector2(56f, 0f);
            ptRt.offsetMax = new Vector2(-10f, 0f);
        }

        // =========================================================================
        // SCREEN 10: TẠM DỪNG (Pause Modal)
        // =========================================================================
        private void BuildScreen10Pause(Transform parent, Font font)
        {
            _pauseModal = CreateModalOverlay(parent, "Screen10_PauseModal");

            GameObject cardObj = new GameObject("PauseCard");
            cardObj.transform.SetParent(_pauseModal.transform, false);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(540, 440, 24f, ColGlassFill, ColCyanBorder, 2f);
            cardBg.type = Image.Type.Sliced;

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(540f, 440f);

            CreateModalHeader(cardObj.transform, AegisLocalization.T("ĐÃ TẠM DỪNG", "PAUSED"), () => CloseModal(_pauseModal), font);

            // 3 Buttons: Tiếp tục, Thiết lập, Thoát về menu
            CreateMenuActionButton(cardObj.transform, "BtnResume", AegisLocalization.T("▶  Tiếp tục", "▶  Resume"), new Vector2(0f, 50f), new Vector2(360f, 54f), ColCyanBorder, ColDarkBtnText, 20, () => CloseModal(_pauseModal), font);
            CreateMenuActionButton(cardObj.transform, "BtnPauseSettings", AegisLocalization.T("⚙  Thiết lập", "⚙  Settings"), new Vector2(0f, -20f), new Vector2(360f, 54f), ColGlassFill, ColTextWhite, 18, OpenSettingsModal, font, ColCyanBorder, 1.2f);
            CreateMenuActionButton(cardObj.transform, "BtnQuitToMenu", AegisLocalization.T("🚪  Thoát về menu", "🚪  Quit to Menu"), new Vector2(0f, -90f), new Vector2(360f, 54f), new Color(0.25f, 0.08f, 0.08f, 0.9f), new Color(1f, 0.45f, 0.45f), 18, ReturnToMainMenuFromPause, font, new Color(0.85f, 0.25f, 0.25f), 1.2f);

            _pauseModal.SetActive(false);
        }

        public void ShowPauseModal()
        {
            if (_menuCanvasObj != null)
            {
                _menuCanvasObj.SetActive(true);
                Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                if (c != null) c.sortingOrder = 200;
            }
            if (_screenMainMenu != null) _screenMainMenu.SetActive(false);
            if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
            if (_screenSplash != null) _screenSplash.SetActive(false);
            if (_pauseModal != null) _pauseModal.SetActive(true);
        }

        private void ReturnToMainMenuFromPause()
        {
            if (_menuCanvasObj != null)
            {
                Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                if (c != null) c.sortingOrder = 90;
            }
            _pauseModal.SetActive(false);
            ShowMenu();
        }

        // =========================================================================
        // SCREEN 11: KẾT QUẢ TRẬN ĐẤU (Result Screen Matching Image 2 Design 100%)
        // =========================================================================
        private void BuildScreen11Result(Transform parent, Font font)
        {
            _resultScreenModal = CreateModalOverlay(parent, "Screen11_ResultScreenModal");

            // Main Card (1020 x 610) with victory_screen_bg
            GameObject cardObj = new GameObject("ResultCard");
            cardObj.transform.SetParent(_resultScreenModal.transform, false);

            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1020f, 610f);

            Image cardBg = cardObj.AddComponent<Image>();
            Sprite bgSprite = ArcaneAssetLoader.LoadArcaneSprite("victory_screen_bg.png");
            if (bgSprite != null)
            {
                cardBg.sprite = bgSprite;
            }
            else
            {
                cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1020, 610, 24f, new Color(0.02f, 0.06f, 0.12f, 0.98f), ColCyanBorder, 2f);
            }
            cardBg.raycastTarget = true;

            // Frame Border Overlay with Glowing Cyan Edge
            GameObject frameBorder = new GameObject("FrameBorder");
            frameBorder.transform.SetParent(cardObj.transform, false);
            RectTransform fbRt = frameBorder.AddComponent<RectTransform>();
            fbRt.anchorMin = Vector2.zero;
            fbRt.anchorMax = Vector2.one;
            fbRt.offsetMin = Vector2.zero;
            fbRt.offsetMax = Vector2.zero;
            Image fbImg = frameBorder.AddComponent<Image>();
            fbImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1020, 610, 22f, Color.clear, ColCyanBorder, 2.5f);
            fbImg.type = Image.Type.Sliced;
            fbImg.raycastTarget = false;

            // Subtle Central Gradient Shadow under panels for contrast
            GameObject shadowObj = new GameObject("CentralDimmer");
            shadowObj.transform.SetParent(cardObj.transform, false);
            RectTransform sRt = shadowObj.AddComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, -10f);
            sRt.sizeDelta = new Vector2(560f, 540f);
            Image sImg = shadowObj.AddComponent<Image>();
            sImg.sprite = ArcaneAssetLoader.GetCircleSprite(256, new Color(0.01f, 0.03f, 0.08f, 0.65f), Color.clear, 0f);
            sImg.raycastTarget = false;

            // Heraldic Crest at Top-Right
            GameObject crestObj = new GameObject("TopRightCrest");
            crestObj.transform.SetParent(cardObj.transform, false);
            Image crImg = crestObj.AddComponent<Image>();
            Sprite crSprite = ArcaneAssetLoader.LoadArcaneSprite("arcane_crest.png");
            if (crSprite != null) crImg.sprite = crSprite;
            crImg.color = new Color(1f, 0.84f, 0.35f, 0.75f);
            crImg.preserveAspect = true;
            RectTransform crRt = crestObj.GetComponent<RectTransform>();
            crRt.anchoredPosition = new Vector2(455f, 250f);
            crRt.sizeDelta = new Vector2(36f, 36f);

            // Close [✕] Button at Top-Right
            GameObject closeBtn = new GameObject("BtnCloseResult");
            closeBtn.transform.SetParent(cardObj.transform, false);
            Image cbImg = closeBtn.AddComponent<Image>();
            cbImg.sprite = ArcaneAssetLoader.GetCircleSprite(34, new Color(0.04f, 0.10f, 0.20f, 0.85f), ColCyanBorder, 1.2f);
            Button cb = closeBtn.AddComponent<Button>();
            cb.onClick.AddListener(() =>
            {
                CloseModal(_resultScreenModal);
                if (_menuCanvasObj != null)
                {
                    Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                    if (c != null) c.sortingOrder = 90;
                }
                ShowMenu();
            });
            closeBtn.AddComponent<MenuButtonEffect>();
            RectTransform cbrt = closeBtn.GetComponent<RectTransform>();
            cbrt.anchoredPosition = new Vector2(475f, 270f);
            cbrt.sizeDelta = new Vector2(34f, 34f);
            CreateTextComp(closeBtn.transform, "✕", 20, FontStyle.Bold, ColTextWhite, font, Vector2.zero, new Vector2(34f, 34f));

            // Crown + Rays Graphic
            GameObject crownObj = new GameObject("CrownGraphic");
            crownObj.transform.SetParent(cardObj.transform, false);
            RectTransform crownRt = crownObj.AddComponent<RectTransform>();
            crownRt.anchoredPosition = new Vector2(0f, 238f);
            crownRt.sizeDelta = new Vector2(160f, 34f);
            CreateTextComp(crownObj.transform, "✦   👑   ✦", 28, FontStyle.Bold, new Color(1.0f, 0.88f, 0.40f, 0.95f), font, Vector2.zero, new Vector2(160f, 34f));

            // Title: CHIẾN THẮNG
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            _resultTitleTxt = titleObj.AddComponent<Text>();
            _resultTitleTxt.text = AegisLocalization.T("CHIẾN THẮNG", "VICTORY");
            _resultTitleTxt.font = font;
            _resultTitleTxt.fontSize = 46;
            _resultTitleTxt.fontStyle = FontStyle.Bold;
            _resultTitleTxt.alignment = TextAnchor.MiddleCenter;
            _resultTitleTxt.color = ColGoldBorder;
            _resultTitleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _resultTitleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 198f);
            tRt.sizeDelta = new Vector2(500f, 48f);

            // Player Profile Badge (Avatar + Player001 + Elo +12)
            GameObject profileBadge = new GameObject("PlayerBadge");
            profileBadge.transform.SetParent(cardObj.transform, false);
            Image pbImg = profileBadge.AddComponent<Image>();
            pbImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(320, 52, 26f, new Color(0.02f, 0.08f, 0.18f, 0.92f), ColCyanBorder, 1.4f);
            pbImg.type = Image.Type.Sliced;
            RectTransform pbRt = profileBadge.GetComponent<RectTransform>();
            pbRt.anchoredPosition = new Vector2(0f, 136f);
            pbRt.sizeDelta = new Vector2(320f, 52f);

            // Circular Avatar with Cyan Ring
            GameObject avObj = new GameObject("Avatar");
            avObj.transform.SetParent(profileBadge.transform, false);
            Image avImg = avObj.AddComponent<Image>();
            Sprite avSp = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png");
            if (avSp == null) avSp = ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            if (avSp != null) avImg.sprite = avSp;
            else avImg.color = new Color(0.1f, 0.3f, 0.6f);
            RectTransform avRt = avObj.GetComponent<RectTransform>();
            avRt.anchoredPosition = new Vector2(-118f, 0f);
            avRt.sizeDelta = new Vector2(40f, 40f);

            GameObject avRing = new GameObject("Ring");
            avRing.transform.SetParent(avObj.transform, false);
            Image ringImg = avRing.AddComponent<Image>();
            ringImg.sprite = ArcaneAssetLoader.GetCircleSprite(42, Color.clear, ColCyanBorder, 1.5f);
            RectTransform rrt = avRing.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;

            // Player Name & Elo
            string pName = "Player001";
            if (UserAccountManager.Instance != null && UserAccountManager.Instance.IsLoggedIn)
            {
                pName = UserAccountManager.Instance.CurrentUsername;
            }
            CreateTextComp(profileBadge.transform, pName, 20, FontStyle.Bold, ColTextWhite, font, new Vector2(10f, 9f), new Vector2(200f, 24f), TextAnchor.MiddleLeft);

            GameObject eloObj = new GameObject("EloTxt");
            eloObj.transform.SetParent(profileBadge.transform, false);
            _resultEloTxt = eloObj.AddComponent<Text>();
            _resultEloTxt.text = "<color=#00e676>Elo +12</color>";
            _resultEloTxt.font = font;
            _resultEloTxt.fontSize = 17;
            _resultEloTxt.fontStyle = FontStyle.Bold;
            _resultEloTxt.alignment = TextAnchor.MiddleLeft;
            _resultEloTxt.supportRichText = true;
            _resultEloTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _resultEloTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform eloRt = eloObj.GetComponent<RectTransform>();
            eloRt.anchoredPosition = new Vector2(10f, -11f);
            eloRt.sizeDelta = new Vector2(200f, 22f);

            // Match Stats Card ("Thống kê ván đấu")
            GameObject statsCard = new GameObject("StatsCard");
            statsCard.transform.SetParent(cardObj.transform, false);
            Image scImg = statsCard.AddComponent<Image>();
            scImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(460, 195, 16f, new Color(0.015f, 0.06f, 0.14f, 0.92f), new Color(0.0f, 0.65f, 0.95f, 0.85f), 1.5f);
            scImg.type = Image.Type.Sliced;
            RectTransform scRt = statsCard.GetComponent<RectTransform>();
            scRt.anchoredPosition = new Vector2(0f, -10f);
            scRt.sizeDelta = new Vector2(460f, 195f);

            CreateTextComp(statsCard.transform, AegisLocalization.T("Thống kê ván đấu", "Match Summary"), 20, FontStyle.Bold, new Color(0.80f, 0.95f, 1.0f), font, new Vector2(0f, 66f), new Vector2(420f, 28f));

            _resultMovesTxt = CreateStatRow(statsCard.transform, AegisLocalization.T("Nước đi", "Moves"), "42", 22f, font);
            _resultDurationTxt = CreateStatRow(statsCard.transform, AegisLocalization.T("Thời gian", "Duration"), "08:36", -22f, font);
            _resultScoreTxt = CreateStatRow(statsCard.transform, AegisLocalization.T("Mã trận", "Result"), "1 - 0", -66f, font);

            // 3 Bottom Action Buttons: [Phát lại] [Chia sẻ] [Ván mới]
            CreateMenuActionButton(cardObj.transform, "BtnReplay", AegisLocalization.T("↺  Phát lại", "↺  Replay"), new Vector2(-175f, -195f), new Vector2(145f, 50f), new Color(0.02f, 0.10f, 0.22f, 0.95f), ColTextWhite, 17, () => ShowToast(AegisLocalization.T("Đang chuẩn bị bản xem lại...", "Preparing match replay..."), ToastType.Info), font, ColCyanBorder, 1.4f);
            
            CreateMenuActionButton(cardObj.transform, "BtnShare", AegisLocalization.T("🔀  Chia sẻ", "🔀  Share"), new Vector2(0f, -195f), new Vector2(145f, 50f), new Color(0.02f, 0.10f, 0.22f, 0.95f), ColTextWhite, 17, () =>
            {
                GUIUtility.systemCopyBuffer = $"AegisMech Match: {_resultScoreTxt?.text} ({_resultMovesTxt?.text} moves, {_resultDurationTxt?.text})";
                ShowToast(AegisLocalization.T("Đã sao chép liên kết trận đấu!", "Match link copied!"), ToastType.Success);
            }, font, ColCyanBorder, 1.4f);

            CreateMenuActionButton(cardObj.transform, "BtnNewMatch", AegisLocalization.T("⚔  Ván mới", "⚔  New Match"), new Vector2(185f, -195f), new Vector2(165f, 50f), new Color(0.85f, 0.55f, 0.08f, 0.96f), ColDarkBtnText, 18, () =>
            {
                CloseModal(_resultScreenModal);
                if (_menuCanvasObj != null)
                {
                    Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                    if (c != null) c.sortingOrder = 90;
                    _menuCanvasObj.SetActive(false);
                }
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                {
                    MechChess.Chess3D.ChessBoard3DManager.Instance.ResetBoard();
                }
                if (UIManager2D.Instance != null)
                {
                    UIManager2D.Instance.ResetGameTimers();
                    UIManager2D.Instance.SetHUDVisible(true);
                }
            }, font, ColGoldBorder, 2.0f);

            _resultScreenModal.SetActive(false);
        }

        private Text CreateStatRow(Transform parent, string label, string val, float y, Font font)
        {
            GameObject row = new GameObject("Row_" + label);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(400f, 28f);

            CreateTextComp(row.transform, label, 18, FontStyle.Normal, ColTextMuted, font, new Vector2(-110f, 0f), new Vector2(160f, 26f), TextAnchor.MiddleLeft);

            GameObject valObj = new GameObject("Val");
            valObj.transform.SetParent(row.transform, false);
            Text t = valObj.AddComponent<Text>();
            t.text = val;
            t.font = font;
            t.fontSize = 20;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleRight;
            t.color = ColTextWhite;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform vrt = valObj.GetComponent<RectTransform>();
            vrt.anchoredPosition = new Vector2(110f, 0f);
            vrt.sizeDelta = new Vector2(160f, 26f);
            return t;
        }

        public void ShowResultScreen(bool victory, string reason, int moves, float durationSec)
        {
            ShowResultScreen(victory, reason, moves, durationSec, false);
        }

        public void ShowResultScreen(bool victory, string reason, int moves, float durationSec, bool isDraw)
        {
            if (_resultScreenModal == null) return;

            // 1. Activate Canvas and raise sortingOrder above in-game HUD (100)
            if (_menuCanvasObj != null)
            {
                _menuCanvasObj.SetActive(true);
                Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                if (c != null) c.sortingOrder = 200;
            }

            // 2. Hide base screens so only modal is visible on top of 3D game
            if (_screenMainMenu != null) _screenMainMenu.SetActive(false);
            if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
            if (_screenSplash != null) _screenSplash.SetActive(false);
            CloseAllModals();

            // 3. Populate dynamic text
            if (_resultTitleTxt != null)
            {
                if (isDraw)
                {
                    _resultTitleTxt.text = AegisLocalization.T("HÒA CỜ", "MATCH DRAW");
                    _resultTitleTxt.color = new Color(0.85f, 0.88f, 0.95f);
                }
                else
                {
                    _resultTitleTxt.text = victory ? AegisLocalization.T("CHIẾN THẮNG", "VICTORY") : AegisLocalization.T("THẤT BẠI", "DEFEAT");
                    _resultTitleTxt.color = victory ? ColGoldBorder : new Color(0.95f, 0.35f, 0.35f);
                }
            }
            if (_resultMovesTxt != null) _resultMovesTxt.text = moves.ToString();
            if (_resultDurationTxt != null)
            {
                int min = Mathf.FloorToInt(durationSec / 60f);
                int sec = Mathf.FloorToInt(durationSec % 60f);
                _resultDurationTxt.text = $"{min:00}:{sec:00}";
            }
            if (_resultScoreTxt != null)
            {
                _resultScoreTxt.text = isDraw ? "½ - ½" : (victory ? "1 - 0" : "0 - 1");
            }
            if (_resultEloTxt != null)
            {
                _resultEloTxt.text = isDraw ? "<color=#80d8ff>Elo +0</color>" : (victory ? "<color=#00e676>Elo +12</color>" : "<color=#ff5252>Elo -8</color>");
            }

            _resultScreenModal.SetActive(true);
        }


        // =========================================================================
        // SCREEN 12: HỒ SƠ NGƯỜI CHƠI (Profile Modal - User Reference Design 100%)
        // =========================================================================
        private void BuildScreen12Profile(Transform parent, Font font)
        {
            _profileModal = CreateModalOverlay(parent, "Screen12_ProfileModal");

            // Tapping backdrop closes modal
            Button overlayBtn = _profileModal.GetComponent<Button>();
            if (overlayBtn == null) overlayBtn = _profileModal.AddComponent<Button>();
            overlayBtn.onClick.AddListener(() =>
            {
                CloseModal(_profileModal);
            });

            // Centered Main Cyber Profile Card (1280 x 510) - Prominent, Big, Crisp
            GameObject cardObj = new GameObject("ProfileCard");
            cardObj.transform.SetParent(_profileModal.transform, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1280f, 510f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1280, 510, 22f, new Color(0.02f, 0.05f, 0.10f, 0.96f), ColCyanBorder, 2.5f);
            cardBg.type = Image.Type.Sliced;
            cardBg.raycastTarget = true; // Blocks click from closing modal when tapping inside

            // Close Button [✕] at Top-Right
            GameObject closeBtnObj = new GameObject("BtnClose");
            closeBtnObj.transform.SetParent(cardObj.transform, false);
            Image cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.sprite = ArcaneAssetLoader.GetCircleSprite(36, new Color(0.04f, 0.12f, 0.22f, 0.90f), ColCyanBorder, 1.5f);
            Button cbBtn = closeBtnObj.AddComponent<Button>();
            cbBtn.onClick.AddListener(() =>
            {
                CloseModal(_profileModal);
            });
            closeBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform cbRt = closeBtnObj.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 1f);
            cbRt.anchorMax = new Vector2(1f, 1f);
            cbRt.pivot = new Vector2(1f, 1f);
            cbRt.anchoredPosition = new Vector2(-16f, -16f);
            cbRt.sizeDelta = new Vector2(36f, 36f);

            GameObject cbTxtObj = new GameObject("Txt");
            cbTxtObj.transform.SetParent(closeBtnObj.transform, false);
            Text cbt = cbTxtObj.AddComponent<Text>();
            cbt.text = "✕";
            cbt.font = font;
            cbt.fontSize = 20;
            cbt.fontStyle = FontStyle.Bold;
            cbt.alignment = TextAnchor.MiddleCenter;
            cbt.color = ColCyanGlow;
            RectTransform cbtrt = cbTxtObj.GetComponent<RectTransform>();
            cbtrt.anchorMin = Vector2.zero;
            cbtrt.anchorMax = Vector2.one;
            cbtrt.offsetMin = Vector2.zero;
            cbtrt.offsetMax = Vector2.zero;

            // =====================================================================
            // LEFT PANEL: Avatar + Info (Top) & Stats Table Card (Bottom)
            // =====================================================================
            GameObject leftPanel = new GameObject("LeftPanel");
            leftPanel.transform.SetParent(cardObj.transform, false);
            RectTransform lpRt = leftPanel.AddComponent<RectTransform>();
            lpRt.anchorMin = new Vector2(0.5f, 0.5f);
            lpRt.anchorMax = new Vector2(0.5f, 0.5f);
            lpRt.pivot = new Vector2(0.5f, 0.5f);
            lpRt.anchoredPosition = new Vector2(-335f, 0f);
            lpRt.sizeDelta = new Vector2(550f, 460f);

            // Circular Avatar Frame with glowing cyan ring (96x96)
            GameObject avFrame = new GameObject("AvatarFrame");
            avFrame.transform.SetParent(leftPanel.transform, false);
            Image avFrameImg = avFrame.AddComponent<Image>();
            avFrameImg.sprite = ArcaneAssetLoader.GetCircleSprite(96, new Color(0.04f, 0.12f, 0.22f, 0.95f), ColCyanGlow, 2.5f);
            RectTransform afRt = avFrame.GetComponent<RectTransform>();
            afRt.anchoredPosition = new Vector2(-215f, 160f);
            afRt.sizeDelta = new Vector2(96f, 96f);

            // Inner Avatar Image
            GameObject avObj = new GameObject("AvatarImg");
            avObj.transform.SetParent(avFrame.transform, false);
            Image avImg = avObj.AddComponent<Image>();
            Sprite avSp = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png");
            if (avSp != null) avImg.sprite = avSp;
            else avImg.color = ColCyanBorder;
            RectTransform avRt = avObj.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0.5f, 0.5f);
            avRt.anchorMax = new Vector2(0.5f, 0.5f);
            avRt.pivot = new Vector2(0.5f, 0.5f);
            avRt.anchoredPosition = Vector2.zero;
            avRt.sizeDelta = new Vector2(84f, 84f);

            // Username: "Player001" (Bold 30 White)
            string currentName = "Player001";
            if (UserAccountManager.Instance != null && !string.IsNullOrEmpty(UserAccountManager.Instance.CurrentUsername))
                currentName = UserAccountManager.Instance.CurrentUsername;
            CreateTextComp(leftPanel.transform, currentName, 30, FontStyle.Bold, ColTextWhite, font, new Vector2(-25f, 188f), new Vector2(260f, 38f), TextAnchor.MiddleLeft);

            // Level: "Lv. 5" (Cyan 20 Bold)
            CreateTextComp(leftPanel.transform, "Lv. 5", 20, FontStyle.Bold, ColCyanGlow, font, new Vector2(-25f, 154f), new Vector2(140f, 26f), TextAnchor.MiddleLeft);

            // EXP Progress Bar (Width 180px, height 16px, fill ~64%) + "320 / 500"
            GameObject expBarObj = new GameObject("ExpBarBg");
            expBarObj.transform.SetParent(leftPanel.transform, false);
            Image expBg = expBarObj.AddComponent<Image>();
            expBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(180, 16, 8f, new Color(0.05f, 0.10f, 0.20f, 0.95f), new Color(0.10f, 0.35f, 0.55f, 0.8f), 1f);
            expBg.type = Image.Type.Sliced;
            RectTransform expRt = expBarObj.GetComponent<RectTransform>();
            expRt.anchoredPosition = new Vector2(-30f, 124f);
            expRt.sizeDelta = new Vector2(180f, 16f);

            GameObject expFillObj = new GameObject("ExpFill");
            expFillObj.transform.SetParent(expBarObj.transform, false);
            Image expFill = expFillObj.AddComponent<Image>();
            expFill.sprite = ArcaneAssetLoader.GetGlassBoxSprite(115, 16, 8f, ColCyanGlow, ColCyanGlow, 0f);
            expFill.type = Image.Type.Sliced;
            RectTransform fillRt = expFillObj.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0.5f);
            fillRt.anchorMax = new Vector2(0f, 0.5f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(115f, 16f); // 320 / 500 = 64%

            CreateTextComp(leftPanel.transform, "320 / 500", 18, FontStyle.Bold, new Color(0.88f, 0.94f, 1.0f), font, new Vector2(120f, 124f), new Vector2(110f, 26f), TextAnchor.MiddleLeft);

            // ---------------------------------------------------------------------
            // Stats Table Card (Lower Left - Matching Image 2)
            // ---------------------------------------------------------------------
            GameObject statsCard = new GameObject("StatsCard");
            statsCard.transform.SetParent(leftPanel.transform, false);
            Image scBg = statsCard.AddComponent<Image>();
            scBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(530, 260, 16f, new Color(0.02f, 0.06f, 0.12f, 0.94f), ColMutedBorder, 1.2f);
            scBg.type = Image.Type.Sliced;
            RectTransform scRt = statsCard.GetComponent<RectTransform>();
            scRt.anchoredPosition = new Vector2(0f, -50f);
            scRt.sizeDelta = new Vector2(530f, 260f);

            // 3 Column Dividers
            float[] divX = { -130f, 0f, 130f };
            for (int d = 0; d < 3; d++)
            {
                GameObject divObj = new GameObject("ColDiv_" + d);
                divObj.transform.SetParent(statsCard.transform, false);
                Image divImg = divObj.AddComponent<Image>();
                divImg.color = new Color(0.08f, 0.28f, 0.45f, 0.50f);
                RectTransform drt = divObj.GetComponent<RectTransform>();
                drt.anchoredPosition = new Vector2(divX[d], 0f);
                drt.sizeDelta = new Vector2(1.5f, 200f);
            }

            // 1 Horizontal Divider
            GameObject hDivObj = new GameObject("RowDiv");
            hDivObj.transform.SetParent(statsCard.transform, false);
            Image hDivImg = hDivObj.AddComponent<Image>();
            hDivImg.color = new Color(0.08f, 0.28f, 0.45f, 0.50f);
            RectTransform hdrt = hDivObj.GetComponent<RectTransform>();
            hdrt.anchoredPosition = new Vector2(0f, -10f);
            hdrt.sizeDelta = new Vector2(490f, 1.5f);

            // 4 Column Headers: Elo, Thắng, Thua, Hòa (Bold 21)
            float[] colX = { -195f, -65f, 65f, 195f };
            string[] statHeaders = {
                AegisLocalization.T("Elo", "Elo"),
                AegisLocalization.T("Thắng", "Wins"),
                AegisLocalization.T("Thua", "Losses"),
                AegisLocalization.T("Hòa", "Draws")
            };
            for (int i = 0; i < 4; i++)
            {
                CreateTextComp(statsCard.transform, statHeaders[i], 21, FontStyle.Bold, new Color(0.80f, 0.88f, 0.98f), font, new Vector2(colX[i], 78f), new Vector2(110f, 30f), TextAnchor.MiddleCenter);
            }

            // Row 1: 1250 (Cyan 32 Bold), 129 (White 28 Bold), 8 (White 28 Bold), Draw Icon
            int currentElo = 1250;
            if (UserAccountManager.Instance != null && UserAccountManager.Instance.CurrentElo > 0)
                currentElo = UserAccountManager.Instance.CurrentElo;
            CreateTextComp(statsCard.transform, currentElo.ToString(), 32, FontStyle.Bold, ColCyanGlow, font, new Vector2(colX[0], 28f), new Vector2(110f, 40f), TextAnchor.MiddleCenter);
            CreateTextComp(statsCard.transform, "129", 28, FontStyle.Bold, ColTextWhite, font, new Vector2(colX[1], 28f), new Vector2(110f, 38f), TextAnchor.MiddleCenter);
            CreateTextComp(statsCard.transform, "8", 28, FontStyle.Bold, ColTextWhite, font, new Vector2(colX[2], 28f), new Vector2(110f, 38f), TextAnchor.MiddleCenter);

            // Draw Icon (profile_draw_icon.png)
            GameObject drawIconObj = new GameObject("DrawIcon");
            drawIconObj.transform.SetParent(statsCard.transform, false);
            Image drawIconImg = drawIconObj.AddComponent<Image>();
            Sprite drawSp = ArcaneAssetLoader.LoadArcaneSprite("profile_draw_icon.png");
            if (drawSp != null) drawIconImg.sprite = drawSp;
            else drawIconImg.color = ColCyanGlow;
            drawIconImg.preserveAspect = true;
            RectTransform diRt = drawIconObj.GetComponent<RectTransform>();
            diRt.anchoredPosition = new Vector2(colX[3], 28f);
            diRt.sizeDelta = new Vector2(36f, 36f);

            // Row 2: Rank Crest, 12 (White 26 Bold), 8 (Cyan 26 Bold), 2 (White 26 Bold)
            GameObject rankCrestObj = new GameObject("RankCrest");
            rankCrestObj.transform.SetParent(statsCard.transform, false);
            Image rankCrestImg = rankCrestObj.AddComponent<Image>();
            Sprite crestSp = ArcaneAssetLoader.LoadArcaneSprite("profile_rank_crest.png");
            if (crestSp != null) rankCrestImg.sprite = crestSp;
            else rankCrestImg.color = ColCyanGlow;
            rankCrestImg.preserveAspect = true;
            RectTransform rcRt = rankCrestObj.GetComponent<RectTransform>();
            rcRt.anchoredPosition = new Vector2(colX[0], -62f);
            rcRt.sizeDelta = new Vector2(38f, 38f);

            CreateTextComp(statsCard.transform, "12", 26, FontStyle.Bold, ColTextWhite, font, new Vector2(colX[1], -62f), new Vector2(110f, 36f), TextAnchor.MiddleCenter);
            CreateTextComp(statsCard.transform, "8", 26, FontStyle.Bold, ColCyanGlow, font, new Vector2(colX[2], -62f), new Vector2(110f, 36f), TextAnchor.MiddleCenter);
            CreateTextComp(statsCard.transform, "2", 26, FontStyle.Bold, ColTextWhite, font, new Vector2(colX[3], -62f), new Vector2(110f, 36f), TextAnchor.MiddleCenter);

            // =====================================================================
            // RIGHT PANEL: Sub-Tabs & Match History Card (Matching Image 2)
            // =====================================================================
            GameObject rightPanel = new GameObject("RightPanel");
            rightPanel.transform.SetParent(cardObj.transform, false);
            RectTransform rpRt = rightPanel.AddComponent<RectTransform>();
            rpRt.anchorMin = new Vector2(0.5f, 0.5f);
            rpRt.anchorMax = new Vector2(0.5f, 0.5f);
            rpRt.pivot = new Vector2(0.5f, 0.5f);
            rpRt.anchoredPosition = new Vector2(305f, 0f);
            rpRt.sizeDelta = new Vector2(620f, 460f);

            // Category Sub-Tabs: Lịch sử đấu (Active), Thành tích, Skin
            GameObject profileTabs = new GameObject("ProfileTabs");
            profileTabs.transform.SetParent(rightPanel.transform, false);
            RectTransform ptRt = profileTabs.AddComponent<RectTransform>();
            ptRt.anchoredPosition = new Vector2(0f, 185f);
            ptRt.sizeDelta = new Vector2(610f, 46f);

            string[] pTabs = {
                AegisLocalization.T("Lịch sử đấu", "Match History"),
                AegisLocalization.T("Thành tích", "Achievements"),
                AegisLocalization.T("Skin", "Skins")
            };
            Sprite pTabActSp = ArcaneAssetLoader.LoadArcaneSprite("profile_tab_active.png");
            if (pTabActSp == null) pTabActSp = ArcaneAssetLoader.GetGlassBoxSprite(190, 44, 14f, new Color(0.04f, 0.16f, 0.32f, 0.95f), ColCyanGlow, 2f);
            Sprite pTabInactSp = ArcaneAssetLoader.LoadArcaneSprite("profile_tab_inactive.png");

            float[] tabX = { -195f, 15f, 210f };
            float[] tabW = { 190f, 170f, 150f };
            for (int i = 0; i < 3; i++)
            {
                int tabIdx = i;
                bool active = (i == _selectedProfileTab);
                GameObject tObj = new GameObject("Tab_" + pTabs[i]);
                tObj.transform.SetParent(profileTabs.transform, false);

                Image bg = tObj.AddComponent<Image>();
                bg.sprite = active ? pTabActSp : pTabInactSp;
                bg.type = Image.Type.Simple;
                _profileTabBgs[i] = bg;

                Button btn = tObj.AddComponent<Button>();
                btn.onClick.AddListener(() => SwitchProfileTab(tabIdx));
                tObj.AddComponent<MenuButtonEffect>();

                RectTransform rt = tObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(tabX[i], 0f);
                rt.sizeDelta = new Vector2(tabW[i], 44f);

                GameObject txtObj = new GameObject("Txt");
                txtObj.transform.SetParent(tObj.transform, false);
                Text txtComp = txtObj.AddComponent<Text>();
                txtComp.text = pTabs[i];
                txtComp.font = font;
                txtComp.fontSize = 21;
                txtComp.fontStyle = FontStyle.Bold;
                txtComp.alignment = TextAnchor.MiddleCenter;
                txtComp.color = active ? ColTextWhite : new Color(0.75f, 0.84f, 0.96f, 0.85f);
                _profileTabTexts[i] = txtComp;

                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
            }

            // Tab 0 Content: Match History Container (Recessed Dark Card)
            GameObject historyContainer = new GameObject("Tab0_MatchHistory");
            historyContainer.transform.SetParent(rightPanel.transform, false);
            _profileTabContainers[0] = historyContainer;
            Image hcBg = historyContainer.AddComponent<Image>();
            hcBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(610, 360, 18f, new Color(0.02f, 0.05f, 0.10f, 0.94f), ColMutedBorder, 1.2f);
            hcBg.type = Image.Type.Sliced;
            RectTransform hcRt = historyContainer.GetComponent<RectTransform>();
            hcRt.anchoredPosition = new Vector2(0f, -40f);
            hcRt.sizeDelta = new Vector2(610f, 360f);

            // Horizontal row dividers in Match History
            float[] rowDivY = { 35f, -65f };
            for (int rd = 0; rd < 2; rd++)
            {
                GameObject rdivObj = new GameObject("RowDiv_" + rd);
                rdivObj.transform.SetParent(historyContainer.transform, false);
                Image rdivImg = rdivObj.AddComponent<Image>();
                rdivImg.color = new Color(0.08f, 0.28f, 0.45f, 0.50f);
                RectTransform rdrt = rdivObj.GetComponent<RectTransform>();
                rdrt.anchoredPosition = new Vector2(0f, rowDivY[rd]);
                rdrt.sizeDelta = new Vector2(560f, 1.5f);
            }

            // 3 Match Rows matching Image 2 exactly:
            // Row 0: Silver mech crest | VS AI | 1 - 0 | 10/10/2026
            // Row 1: Red circle avatar | VS Player | 0 - 1 | 08/10/2026
            // Row 2: Blue circle avatar | VS AI | 1 - 0 | 05/10/2026
            string[] opponents = { "VS AI", "VS Player", "VS AI" };
            string[] rowIcons = { "profile_vs_ai_icon.png", "profile_vs_player_icon.png", "profile_vs_ai_round.png" };
            string[] dates = { "10/10/2026", "08/10/2026", "05/10/2026" };

            float[] rowY = { 90f, -15f, -115f };
            for (int i = 0; i < 3; i++)
            {
                GameObject rowObj = new GameObject("Row_" + i);
                rowObj.transform.SetParent(historyContainer.transform, false);
                RectTransform rRt = rowObj.AddComponent<RectTransform>();
                rRt.anchoredPosition = new Vector2(0f, rowY[i]);
                rRt.sizeDelta = new Vector2(570f, 80f);

                // Row Icon
                GameObject rIconObj = new GameObject("Icon");
                rIconObj.transform.SetParent(rowObj.transform, false);
                Image rIconImg = rIconObj.AddComponent<Image>();
                Sprite icSp = ArcaneAssetLoader.LoadArcaneSprite(rowIcons[i]);
                if (icSp != null) rIconImg.sprite = icSp;
                else rIconImg.color = ColCyanGlow;
                rIconImg.preserveAspect = true;
                RectTransform riRt = rIconObj.GetComponent<RectTransform>();
                riRt.anchoredPosition = new Vector2(-240f, 0f);
                riRt.sizeDelta = new Vector2(44f, 44f);

                // Opponent Name (Bold 24)
                CreateTextComp(rowObj.transform, opponents[i], 24, FontStyle.Bold, ColTextWhite, font, new Vector2(-125f, 0f), new Vector2(190f, 34f), TextAnchor.MiddleLeft);

                // Score (Bold 28)
                if (i == 1)
                {
                    // Loss score: 0 - 1 (1 in bright red)
                    GameObject scoreObj = new GameObject("Score");
                    scoreObj.transform.SetParent(rowObj.transform, false);
                    Text scoreTxt = scoreObj.AddComponent<Text>();
                    scoreTxt.text = "0 - <color=#FF4444>1</color>";
                    scoreTxt.font = font;
                    scoreTxt.fontSize = 28;
                    scoreTxt.fontStyle = FontStyle.Bold;
                    scoreTxt.alignment = TextAnchor.MiddleCenter;
                    scoreTxt.color = ColTextWhite;
                    scoreTxt.supportRichText = true;
                    RectTransform sRt = scoreObj.GetComponent<RectTransform>();
                    sRt.anchoredPosition = new Vector2(35f, 0f);
                    sRt.sizeDelta = new Vector2(120f, 38f);
                }
                else
                {
                    CreateTextComp(rowObj.transform, "1 - 0", 28, FontStyle.Bold, ColTextWhite, font, new Vector2(35f, 0f), new Vector2(120f, 38f), TextAnchor.MiddleCenter);
                }

                // Date (Normal 19)
                CreateTextComp(rowObj.transform, dates[i], 19, FontStyle.Normal, new Color(0.75f, 0.85f, 0.98f), font, new Vector2(205f, 0f), new Vector2(140f, 32f), TextAnchor.MiddleRight);
            }

            // Tab 1 Content: Achievements (Thành tích)
            GameObject achContainer = new GameObject("Tab1_Achievements");
            achContainer.transform.SetParent(rightPanel.transform, false);
            _profileTabContainers[1] = achContainer;
            Image achBg = achContainer.AddComponent<Image>();
            achBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(610, 360, 18f, new Color(0.02f, 0.05f, 0.10f, 0.94f), ColMutedBorder, 1.2f);
            achBg.type = Image.Type.Sliced;
            RectTransform achRt = achContainer.GetComponent<RectTransform>();
            achRt.anchoredPosition = new Vector2(0f, -40f);
            achRt.sizeDelta = new Vector2(610f, 360f);

            string[] achTitles = {
                AegisLocalization.T("Chiến Tướng Khởi Nguyên", "Genesis Knight"),
                AegisLocalization.T("Đại Sư Đấu Máy", "AI Grandmaster"),
                AegisLocalization.T("Bậc Thầy AR", "AR Pioneer")
            };
            string[] achProgress = {
                AegisLocalization.T("100 / 100 (Đạt)", "100 / 100 (Done)"),
                AegisLocalization.T("Hoàn thành", "Completed"),
                "38 / 50"
            };
            for (int i = 0; i < 3; i++)
            {
                CreateTextComp(achContainer.transform, "🏆  " + achTitles[i], 21, FontStyle.Bold, ColTextWhite, font, new Vector2(-110f, 80f - i * 80f), new Vector2(350f, 34f), TextAnchor.MiddleLeft);
                CreateTextComp(achContainer.transform, achProgress[i], 19, FontStyle.Bold, ColCyanGlow, font, new Vector2(190f, 80f - i * 80f), new Vector2(150f, 34f), TextAnchor.MiddleRight);
            }
            achContainer.SetActive(false);

            // Tab 2 Content: Skin Collection
            GameObject skinTabContainer = new GameObject("Tab2_Skins");
            skinTabContainer.transform.SetParent(rightPanel.transform, false);
            _profileTabContainers[2] = skinTabContainer;
            Image stkBg = skinTabContainer.AddComponent<Image>();
            stkBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(610, 360, 18f, new Color(0.02f, 0.05f, 0.10f, 0.94f), ColMutedBorder, 1.2f);
            stkBg.type = Image.Type.Sliced;
            RectTransform stkRt = skinTabContainer.GetComponent<RectTransform>();
            stkRt.anchoredPosition = new Vector2(0f, -40f);
            stkRt.sizeDelta = new Vector2(610f, 360f);

            string[] ownedSkins = {
                AegisLocalization.T("Knight of Light (Đang dùng)", "Knight of Light (Equipped)"),
                AegisLocalization.T("Dark Legion (Đã sở hữu)", "Dark Legion (Owned)"),
                AegisLocalization.T("Cyber Future (Đã sở hữu)", "Cyber Future (Owned)")
            };
            for (int i = 0; i < 3; i++)
            {
                CreateTextComp(skinTabContainer.transform, "🛡  " + ownedSkins[i], 21, FontStyle.Bold, ColTextWhite, font, new Vector2(-110f, 80f - i * 80f), new Vector2(370f, 34f), TextAnchor.MiddleLeft);
            }
            skinTabContainer.SetActive(false);

            _profileModal.SetActive(false);
        }

        private void SwitchProfileTab(int tabIndex)
        {
            _selectedProfileTab = tabIndex;
            Sprite activeSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_tab_active.png");
            Sprite inactiveSprite = ArcaneAssetLoader.LoadArcaneSprite("profile_tab_inactive.png");

            for (int i = 0; i < _profileTabBgs.Length; i++)
            {
                if (_profileTabBgs[i] != null)
                {
                    _profileTabBgs[i].sprite = (i == tabIndex) ? activeSprite : inactiveSprite;
                }
                if (_profileTabTexts[i] != null)
                {
                    _profileTabTexts[i].color = (i == tabIndex) ? ColTextWhite : new Color(0.75f, 0.84f, 0.96f, 0.85f);
                    _profileTabTexts[i].fontStyle = FontStyle.Bold;
                    _profileTabTexts[i].fontSize = 21;
                }
                if (_profileTabContainers[i] != null)
                {
                    _profileTabContainers[i].SetActive(i == tabIndex);
                }
            }
        }

        public void ShowProfileDetailsModal()
        {
            if (_profileModal != null) _profileModal.SetActive(true);
        }

        public void OpenProfileModal()
        {
            ShowProfileDetailsModal();
        }

        // =========================================================================
        // SCREEN 13: MÀN HÌNH HỌC CỜ (Learn Chess with Sicilian defense diagram)
        // =========================================================================
        private void BuildScreen13Learn(Transform parent, Font font)
        {
            _learnScreenModal = CreateModalOverlay(parent, "Screen13_LearnModal");

            GameObject cardObj = new GameObject("LearnCard");
            cardObj.transform.SetParent(_learnScreenModal.transform, false);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1280, 720, 28f, ColGlassFill, ColCyanBorder, 2f);
            cardBg.type = Image.Type.Sliced;

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1280f, 720f);

            CreateModalHeader(cardObj.transform, AegisLocalization.T("<  KHAI CUỘC - Phòng thủ Sicilian", "<  OPENING - Sicilian Defense"), () => _learnScreenModal.SetActive(false), font);

            // Left: Lessons List
            GameObject lessonsCol = new GameObject("LessonsColumn");
            lessonsCol.transform.SetParent(cardObj.transform, false);
            RectTransform lcRt = lessonsCol.AddComponent<RectTransform>();
            lcRt.anchorMin = new Vector2(0f, 0.5f);
            lcRt.anchorMax = new Vector2(0f, 0.5f);
            lcRt.pivot = new Vector2(0f, 0.5f);
            lcRt.anchoredPosition = new Vector2(50f, -20f);
            lcRt.sizeDelta = new Vector2(300f, 480f);

            string[] lessons = {
                AegisLocalization.T("📘  Bài 1: Giới thiệu", "📘  Lesson 1: Intro"),
                AegisLocalization.T("📘  Bài 2: Các biến thể chính", "📘  Lesson 2: Main Lines"),
                AegisLocalization.T("📘  Bài 3: Luyện tập", "📘  Lesson 3: Practice")
            };
            for (int i = 0; i < 3; i++)
            {
                bool active = (i == 1);
                GameObject lObj = new GameObject("Lesson_" + i);
                lObj.transform.SetParent(lessonsCol.transform, false);

                Image bg = lObj.AddComponent<Image>();
                bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(280, 56, 16f, active ? ColGlassActive : ColGlassFill, active ? ColCyanBorder : ColMutedBorder, active ? 2f : 1.2f);
                bg.type = Image.Type.Sliced;

                Button btn = lObj.AddComponent<Button>();
                string lesName = lessons[i];
                btn.onClick.AddListener(() => ShowToast(AegisLocalization.T($"Đang mở: {lesName}", $"Opening: {lesName}"), ToastType.Info));
                lObj.AddComponent<MenuButtonEffect>();

                RectTransform rt = lObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, 120f - i * 75f);
                rt.sizeDelta = new Vector2(280f, 56f);

                CreateTextComp(lObj.transform, lessons[i], 14, active ? FontStyle.Bold : FontStyle.Normal, active ? ColCyanGlow : ColTextWhite, font, Vector2.zero, new Vector2(260f, 30f));
            }

            // Center: 2D Sicilian Board Diagram
            GameObject boardObj = new GameObject("SicilianBoard");
            boardObj.transform.SetParent(cardObj.transform, false);
            Image bImg = boardObj.AddComponent<Image>();
            bImg.sprite = ArcaneAssetLoader.GetMiniChessboardDiagram(360);
            RectTransform bRt = boardObj.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(20f, -20f);
            bRt.sizeDelta = new Vector2(380f, 380f);

            // Right: Najdorf Explanation Card + [Xem tiếp]
            GameObject descCard = new GameObject("DescCard");
            descCard.transform.SetParent(cardObj.transform, false);
            Image dcBg = descCard.AddComponent<Image>();
            dcBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(420, 480, 20f, new Color(0.06f, 0.12f, 0.22f, 0.95f), ColCyanBorder, 1.5f);
            dcBg.type = Image.Type.Sliced;
            RectTransform dcRt = descCard.GetComponent<RectTransform>();
            dcRt.anchorMin = new Vector2(1f, 0.5f);
            dcRt.anchorMax = new Vector2(1f, 0.5f);
            dcRt.pivot = new Vector2(1f, 0.5f);
            dcRt.anchoredPosition = new Vector2(-50f, -20f);
            dcRt.sizeDelta = new Vector2(420f, 480f);

            CreateTextComp(descCard.transform, AegisLocalization.T("Biến thể Najdorf", "Najdorf Variation"), 20, FontStyle.Bold, ColTextWhite, font, new Vector2(0f, 180f), new Vector2(360f, 36f));

            GameObject bodyTxtObj = new GameObject("Body");
            bodyTxtObj.transform.SetParent(descCard.transform, false);
            Text bt = bodyTxtObj.AddComponent<Text>();
            bt.text = AegisLocalization.T(
                "Đây là một trong những biến thể phổ biến và mạnh mẽ nhất của Phòng thủ Sicilian.\n\nĐặc trưng bởi nước đi 5...a6, ngăn chặn ô b5 của Trắng và chuẩn bị triển khai phản công cánh Hậu đầy uy lực.",
                "One of the most popular and sharp variations of the Sicilian Defense.\n\nCharacterized by 5...a6, denying the b5 square to White and preparing powerful queenside counter-play.");
            bt.font = font;
            bt.fontSize = 18;
            bt.lineSpacing = 1.35f;
            bt.alignment = TextAnchor.UpperLeft;
            bt.color = ColTextMuted;
            bt.horizontalOverflow = HorizontalWrapMode.Overflow;
            bt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform btRt = bodyTxtObj.GetComponent<RectTransform>();
            btRt.anchoredPosition = new Vector2(0f, 50f);
            btRt.sizeDelta = new Vector2(360f, 200f);

            // [Xem tiếp] Button
            CreateMenuActionButton(descCard.transform, "BtnNextLesson", AegisLocalization.T("Xem tiếp  ❯", "Continue  ❯"), new Vector2(0f, -170f), new Vector2(360f, 52f), ColGoldCta, ColDarkBtnText, 16, () => ShowToast(AegisLocalization.T("Đã hoàn thành bài học 2!", "Lesson 2 completed!"), ToastType.Success), font, ColGoldBorder, 1.5f);

            _learnScreenModal.SetActive(false);
        }

        public void OpenLearnScreen()
        {
            if (_learnScreenModal != null) _learnScreenModal.SetActive(true);
        }

        // =========================================================================
        // SCREEN 14: THÔNG BÁO / POP-UP (Confirmation Dialog)
        // =========================================================================
        private void BuildScreen14Confirm(Transform parent, Font font)
        {
            _confirmDialogModal = CreateModalOverlay(parent, "Screen14_ConfirmDialogModal");

            GameObject cardObj = new GameObject("ConfirmCard");
            cardObj.transform.SetParent(_confirmDialogModal.transform, false);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(520, 280, 24f, ColGlassFill, ColCyanBorder, 2f);
            cardBg.type = Image.Type.Sliced;

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(520f, 280f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            _confirmTitleTxt = titleObj.AddComponent<Text>();
            _confirmTitleTxt.text = AegisLocalization.T("Thông báo", "Notice");
            _confirmTitleTxt.font = font;
            _confirmTitleTxt.fontSize = 28;
            _confirmTitleTxt.fontStyle = FontStyle.Bold;
            _confirmTitleTxt.alignment = TextAnchor.MiddleCenter;
            _confirmTitleTxt.color = ColTextWhite;
            _confirmTitleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _confirmTitleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 85f);
            tRt.sizeDelta = new Vector2(460f, 38f);

            // Body
            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(cardObj.transform, false);
            _confirmBodyTxt = bodyObj.AddComponent<Text>();
            _confirmBodyTxt.text = AegisLocalization.T("Bạn có muốn thoát trò chơi?", "Are you sure you want to exit the game?");
            _confirmBodyTxt.font = font;
            _confirmBodyTxt.fontSize = 22;
            _confirmBodyTxt.alignment = TextAnchor.MiddleCenter;
            _confirmBodyTxt.color = new Color(0.85f, 0.92f, 1.0f);
            _confirmBodyTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _confirmBodyTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform bRt = bodyObj.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(0f, 20f);
            bRt.sizeDelta = new Vector2(460f, 60f);

            // Cancel Button
            CreateMenuActionButton(cardObj.transform, "BtnCancel", AegisLocalization.T("Hủy", "Cancel"), new Vector2(-115f, -70f), new Vector2(180f, 52f), ColGlassFill, ColTextMuted, 20, () => _confirmDialogModal.SetActive(false), font, ColMutedBorder, 1.2f);

            // Confirm Button
            CreateMenuActionButton(cardObj.transform, "BtnConfirm", AegisLocalization.T("Xác nhận", "Confirm"), new Vector2(115f, -70f), new Vector2(180f, 52f), ColCyanBorder, ColDarkBtnText, 20, () =>
            {
                _confirmDialogModal.SetActive(false);
                _onConfirmAction?.Invoke();
            }, font, Color.white, 1.5f);

            _confirmDialogModal.SetActive(false);
        }

        public void ShowConfirmDialog(string title, string body, Action onConfirm)
        {
            if (_confirmTitleTxt != null) _confirmTitleTxt.text = title;
            if (_confirmBodyTxt != null) _confirmBodyTxt.text = body;
            _onConfirmAction = onConfirm;
            if (_confirmDialogModal != null) _confirmDialogModal.SetActive(true);
        }

        // =========================================================================
        // SCREEN 15: UI PHỤ (Toast Notifications & Tooltip)
        // =========================================================================
        private void BuildScreen15ToastContainer(Transform parent, Font font)
        {
            _toastContainer = new GameObject("Screen15_ToastContainer");
            _toastContainer.transform.SetParent(parent, false);

            RectTransform tcRt = _toastContainer.AddComponent<RectTransform>();
            tcRt.anchorMin = new Vector2(0.5f, 1f);
            tcRt.anchorMax = new Vector2(0.5f, 1f);
            tcRt.pivot = new Vector2(0.5f, 1f);
            tcRt.anchoredPosition = new Vector2(0f, -90f);
            tcRt.sizeDelta = new Vector2(460f, 60f);
            _toastContainer.SetActive(false);

            Image bg = _toastContainer.AddComponent<Image>();
            bg.type = Image.Type.Sliced;

            GameObject txtObj = new GameObject("ToastText");
            txtObj.transform.SetParent(_toastContainer.transform, false);
            Text t = txtObj.AddComponent<Text>();
            t.font = font;
            t.fontSize = 15;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = ColTextWhite;
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        public void ShowToast(string message, ToastType type = ToastType.Info)
        {
            if (_toastContainer == null) return;
            StopCoroutine(nameof(AnimateToastCoroutine));
            StartCoroutine(AnimateToastCoroutine(message, type));
        }

        private IEnumerator AnimateToastCoroutine(string message, ToastType type)
        {
            Image bg = _toastContainer.GetComponent<Image>();
            Text t = _toastContainer.GetComponentInChildren<Text>();

            Color fillColor = (type == ToastType.Success) ? new Color(0.06f, 0.28f, 0.16f, 0.95f) :
                              (type == ToastType.Error) ? new Color(0.35f, 0.08f, 0.12f, 0.95f) :
                              (type == ToastType.Warning) ? new Color(0.36f, 0.22f, 0.04f, 0.95f) :
                              new Color(0.04f, 0.14f, 0.28f, 0.95f);

            Color borderColor = (type == ToastType.Success) ? new Color(0.2f, 0.95f, 0.5f) :
                                (type == ToastType.Error) ? new Color(1.0f, 0.35f, 0.45f) :
                                (type == ToastType.Warning) ? new Color(1.0f, 0.85f, 0.2f) :
                                ColCyanBorder;

            string icon = (type == ToastType.Success) ? "✓" : (type == ToastType.Error) ? "✕" : (type == ToastType.Warning) ? "⚠" : "ℹ";

            if (bg != null)
            {
                bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(460, 50, 25f, fillColor, borderColor, 1.5f);
            }
            if (t != null)
            {
                t.text = $"{icon}   {message}";
            }

            _toastContainer.SetActive(true);
            yield return new WaitForSecondsRealtime(2.5f);
            _toastContainer.SetActive(false);
        }

        // =========================================================================
        // MULTIPLAYER & AUTHENTICATION INTEGRATION (Prompt #8)
        // =========================================================================
        public void RequireLogin(Action onAuthenticated)
        {
            if (UserAccountManager.Instance != null && UserAccountManager.Instance.IsLoggedIn)
            {
                onAuthenticated?.Invoke();
            }
            else
            {
                _postLoginAction = onAuthenticated;
                ShowLoginModal();
            }
        }

        public void ShowLoginModal()
        {
            if (_loginModal != null)
            {
                if (_loginGmailInput != null) _loginGmailInput.text = "";
                if (_loginUsernameInput != null) _loginUsernameInput.text = "";
                if (_loginErrorTxt != null) _loginErrorTxt.text = "";
                _loginModal.SetActive(true);
            }
        }

        private void BuildLoginModal(Transform parent, Font font)
        {
            _loginModal = CreateModalOverlay(parent, "LoginModalOverlay");

            GameObject cardObj = new GameObject("LoginCard");
            cardObj.transform.SetParent(_loginModal.transform, false);

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(640, 520, 24f, ColGlassFill, ColCyanBorder, 2f);
            cardImg.type = Image.Type.Sliced;

            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(640f, 520f);

            CreateModalHeader(cardObj.transform, AegisLocalization.T("ĐĂNG NHẬP / TẠO TÀI KHOẢN", "SIGN IN / CREATE ACCOUNT"), () => _loginModal.SetActive(false), font);

            CreateTextComp(cardObj.transform, AegisLocalization.T("Đăng nhập bằng Gmail để tham gia đấu ngẫu nhiên & mời bạn bè", "Sign in with Gmail for Ranked matchmaking & friend duels"), 17, FontStyle.Normal, ColTextMuted, font, new Vector2(0f, 160f), new Vector2(560f, 26f));

            // Gmail Input
            CreateTextComp(cardObj.transform, AegisLocalization.T("ĐỊA CHỈ GMAIL (BẮT BUỘC):", "GMAIL ADDRESS (REQUIRED):"), 16, FontStyle.Bold, ColCyanBorder, font, new Vector2(0f, 115f), new Vector2(540f, 22f));
            _loginGmailInput = CreateInputField(cardObj.transform, "GmailInput", AegisLocalization.T("Ví dụ: nguyenvana@gmail.com", "E.g.: player@gmail.com"), new Vector2(0f, 75f), new Vector2(540f, 48f), font);

            // Username Input
            CreateTextComp(cardObj.transform, AegisLocalization.T("TÊN NGƯỜI CHƠI (TÙY CHỌN - ĐỂ TRỐNG SẼ LẤY THEO GMAIL):", "PLAYER USERNAME (OPTIONAL):"), 16, FontStyle.Bold, ColCyanBorder, font, new Vector2(0f, 25f), new Vector2(540f, 22f));
            _loginUsernameInput = CreateInputField(cardObj.transform, "UsernameInput", AegisLocalization.T("Ví dụ: PhamVaA, DatIT, SiuVipPro...", "E.g.: Knight01, AegisPro..."), new Vector2(0f, -15f), new Vector2(540f, 48f), font);

            CreateTextComp(cardObj.transform, AegisLocalization.T("★ Quy tắc: Chữ cái, chữ số (0-9), không khoảng trắng, không ký tự đặc biệt, không dấu.", "★ Rules: Letters, numbers (0-9), no spaces, no special characters."), 15, FontStyle.Normal, ColTextMuted, font, new Vector2(0f, -55f), new Vector2(540f, 24f));

            // Error Text
            GameObject errObj = new GameObject("ErrorText");
            errObj.transform.SetParent(cardObj.transform, false);
            _loginErrorTxt = errObj.AddComponent<Text>();
            _loginErrorTxt.text = "";
            _loginErrorTxt.font = font;
            _loginErrorTxt.fontSize = 16;
            _loginErrorTxt.fontStyle = FontStyle.Bold;
            _loginErrorTxt.alignment = TextAnchor.MiddleCenter;
            _loginErrorTxt.color = new Color(1.0f, 0.4f, 0.4f);
            _loginErrorTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _loginErrorTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform errRt = errObj.GetComponent<RectTransform>();
            errRt.anchoredPosition = new Vector2(0f, -95f);
            errRt.sizeDelta = new Vector2(540f, 30f);

            // Submit Button
            CreateMenuActionButton(cardObj.transform, "BtnSubmitLogin", AegisLocalization.T("ĐĂNG NHẬP / XÁC NHẬN", "SIGN IN / CONFIRM"), new Vector2(0f, -150f), new Vector2(540f, 52f), ColGoldCta, ColDarkBtnText, 20, ExecuteLogin, font, ColGoldBorder, 1.5f);

            _loginModal.SetActive(false);
        }

        private void ExecuteLogin()
        {
            string gmail = _loginGmailInput != null ? _loginGmailInput.text.Trim() : "";
            string username = _loginUsernameInput != null ? _loginUsernameInput.text.Trim() : "";

            if (string.IsNullOrEmpty(gmail) || !gmail.Contains("@"))
            {
                if (_loginErrorTxt != null) _loginErrorTxt.text = AegisLocalization.T("Vui lòng nhập địa chỉ Gmail hợp lệ!", "Please enter a valid Gmail address!");
                return;
            }

            if (UserAccountManager.Instance != null)
            {
                if (_loginErrorTxt != null) _loginErrorTxt.text = AegisLocalization.T("Đang xác thực...", "Authenticating...");
                UserAccountManager.Instance.Login(gmail, username, (success, msg) =>
                {
                    if (success)
                    {
                        if (_loginModal != null) _loginModal.SetActive(false);
                        UpdateUserProfileUI();
                        ShowToast(AegisLocalization.T($"Xin chào {UserAccountManager.Instance.CurrentUsername}!", $"Welcome {UserAccountManager.Instance.CurrentUsername}!"), ToastType.Success);
                        _postLoginAction?.Invoke();
                        _postLoginAction = null;
                    }
                    else
                    {
                        if (_loginErrorTxt != null) _loginErrorTxt.text = msg;
                    }
                });
            }
        }

        private void OpenOnlineMatchChoiceModal()
        {
            ShowConfirmDialog(AegisLocalization.T("CHẾ ĐỘ ĐẤU ONLINE", "ONLINE MULTIPLAYER"), AegisLocalization.T("Bạn muốn ghép trận ngẫu nhiên (Ranked) hay Thách đấu bạn bè?", "Do you want Ranked Matchmaking or Friend Duel?"), () =>
            {
                ShowMatchmakingModal();
            });
        }

        private void ShowMatchmakingModal()
        {
            if (_matchmakingModal == null) return;
            if (OnlineMultiplayerManager.Instance != null)
            {
                OnlineMultiplayerManager.Instance.StartRandomMatchmaking("10 + 0");
            }
            if (_matchmakingTimerCoroutine != null) StopCoroutine(_matchmakingTimerCoroutine);
            _matchmakingTimerCoroutine = StartCoroutine(AnimateMatchmakingTimer());
            _matchmakingModal.SetActive(true);
        }

        private IEnumerator AnimateMatchmakingTimer()
        {
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                int min = Mathf.FloorToInt(elapsed / 60f);
                int sec = Mathf.FloorToInt(elapsed % 60f);
                if (_matchmakingTimerTxt != null)
                {
                    _matchmakingTimerTxt.text = AegisLocalization.T($"Thời gian tìm trận: {min:00}:{sec:00}", $"Queue time: {min:00}:{sec:00}");
                }
                yield return null;
            }
        }

        private void BuildMatchmakingModal(Transform parent, Font font)
        {
            _matchmakingModal = CreateModalOverlay(parent, "MatchmakingModalOverlay");

            GameObject cardObj = new GameObject("MatchmakingCard");
            cardObj.transform.SetParent(_matchmakingModal.transform, false);

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(600, 420, 24f, ColGlassFill, ColCyanBorder, 2f);
            cardImg.type = Image.Type.Sliced;

            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(600f, 420f);

            // Radar Icon
            GameObject radarObj = new GameObject("RadarIcon");
            radarObj.transform.SetParent(cardObj.transform, false);
            Text radarTxt = radarObj.AddComponent<Text>();
            radarTxt.text = "◎";
            radarTxt.font = font;
            radarTxt.fontSize = 54;
            radarTxt.fontStyle = FontStyle.Bold;
            radarTxt.alignment = TextAnchor.MiddleCenter;
            radarTxt.color = ColCyanBorder;
            _radarIconRt = radarObj.GetComponent<RectTransform>();
            _radarIconRt.anchoredPosition = new Vector2(0f, 110f);
            _radarIconRt.sizeDelta = new Vector2(80f, 80f);

            CreateTextComp(cardObj.transform, AegisLocalization.T("ĐANG TÌM ĐỐI THỦ ONLINE...", "SEARCHING FOR ONLINE OPPONENT..."), 22, FontStyle.Bold, ColTextWhite, font, new Vector2(0f, 45f), new Vector2(540f, 36f));

            GameObject sObj = new GameObject("Status");
            sObj.transform.SetParent(cardObj.transform, false);
            _matchmakingStatusTxt = sObj.AddComponent<Text>();
            _matchmakingStatusTxt.text = AegisLocalization.T("Ghép ngẫu nhiên người chơi trực tuyến có mức Rank tương đồng.", "Matching online players with similar Elo ratings.");
            _matchmakingStatusTxt.font = font;
            _matchmakingStatusTxt.fontSize = 18;
            _matchmakingStatusTxt.alignment = TextAnchor.MiddleCenter;
            _matchmakingStatusTxt.color = ColTextMuted;
            _matchmakingStatusTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _matchmakingStatusTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform sRt = sObj.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, 10f);
            sRt.sizeDelta = new Vector2(520f, 26f);

            GameObject timerObj = new GameObject("Timer");
            timerObj.transform.SetParent(cardObj.transform, false);
            _matchmakingTimerTxt = timerObj.AddComponent<Text>();
            _matchmakingTimerTxt.text = AegisLocalization.T("Thời gian tìm trận: 00:00", "Queue time: 00:00");
            _matchmakingTimerTxt.font = font;
            _matchmakingTimerTxt.fontSize = 24;
            _matchmakingTimerTxt.fontStyle = FontStyle.Bold;
            _matchmakingTimerTxt.alignment = TextAnchor.MiddleCenter;
            _matchmakingTimerTxt.color = ColGoldBorder;
            _matchmakingTimerTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _matchmakingTimerTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform tRt = timerObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, -35f);
            tRt.sizeDelta = new Vector2(520f, 32f);

            CreateMenuActionButton(cardObj.transform, "BtnCancelMatchmaking", AegisLocalization.T("HỦY TÌM TRẬN", "CANCEL QUEUE"), new Vector2(0f, -115f), new Vector2(260f, 52f), new Color(0.28f, 0.08f, 0.08f, 0.95f), new Color(1f, 0.45f, 0.45f), 18, () =>
            {
                if (OnlineMultiplayerManager.Instance != null) OnlineMultiplayerManager.Instance.CancelRandomMatchmaking();
                if (_matchmakingTimerCoroutine != null) StopCoroutine(_matchmakingTimerCoroutine);
                _matchmakingModal.SetActive(false);
            }, font, new Color(0.85f, 0.25f, 0.25f), 1.5f);

            _matchmakingModal.SetActive(false);
        }

        public void ShowFriendModal()
        {
            if (_friendModal == null) return;
            if (_friendMyNameTxt != null && UserAccountManager.Instance != null)
            {
                _friendMyNameTxt.text = AegisLocalization.T(
                    $"Tên của bạn: <color=#B99A61><b>{UserAccountManager.Instance.CurrentUsername}</b></color> (Elo {UserAccountManager.Instance.CurrentElo})\n<i>Hãy chia sẻ tên này để bạn bè tìm và mời bạn!</i>",
                    $"Your Name: <color=#B99A61><b>{UserAccountManager.Instance.CurrentUsername}</b></color> (Elo {UserAccountManager.Instance.CurrentElo})\n<i>Share this name so friends can invite you!</i>"
                );
            }
            if (_friendTargetInput != null) _friendTargetInput.text = "";
            if (_friendStatusTxt != null) _friendStatusTxt.text = "";

            if (OnlineMultiplayerManager.Instance != null)
            {
                OnlineMultiplayerManager.Instance.ConnectToLobby();
                UpdateOnlinePlayersUI(OnlineMultiplayerManager.Instance.CachedOnlinePlayers);
            }
            _friendModal.SetActive(true);
        }

        private void BuildFriendModal(Transform parent, Font font)
        {
            _friendModal = CreateModalOverlay(parent, "FriendModalOverlay");

            GameObject cardObj = new GameObject("FriendCard");
            cardObj.transform.SetParent(_friendModal.transform, false);

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(680, 580, 24f, ColGlassFill, ColCyanBorder, 2f);
            cardImg.type = Image.Type.Sliced;

            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(680f, 580f);

            CreateModalHeader(cardObj.transform, AegisLocalization.T("THÁCH ĐẤU BẠN BÈ", "CHALLENGE FRIEND"), () => _friendModal.SetActive(false), font);

            GameObject mnObj = new GameObject("MyNameBanner");
            mnObj.transform.SetParent(cardObj.transform, false);
            _friendMyNameTxt = mnObj.AddComponent<Text>();
            _friendMyNameTxt.text = AegisLocalization.T("Tên của bạn: ...", "Your name: ...");
            _friendMyNameTxt.font = font;
            _friendMyNameTxt.fontSize = 18;
            _friendMyNameTxt.alignment = TextAnchor.MiddleCenter;
            _friendMyNameTxt.color = ColTextWhite;
            _friendMyNameTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _friendMyNameTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform mnRt = mnObj.GetComponent<RectTransform>();
            mnRt.anchoredPosition = new Vector2(0f, 205f);
            mnRt.sizeDelta = new Vector2(600f, 36f);

            _friendTargetInput = CreateInputField(cardObj.transform, "TargetInput", AegisLocalization.T("Nhập tên bạn bè (vd: DatIT, PhamVaA)...", "Enter friend name (e.g. DatIT, PhamVaA)..."), new Vector2(-75f, 145f), new Vector2(390f, 48f), font);

            CreateMenuActionButton(cardObj.transform, "BtnSendInvite", AegisLocalization.T("MỜI ĐẤU", "INVITE"), new Vector2(215f, 145f), new Vector2(130f, 48f), ColGoldCta, ColDarkBtnText, 18, () =>
            {
                if (_friendTargetInput != null) SendFriendChallenge(_friendTargetInput.text);
            }, font, ColGoldBorder, 1.5f);

            GameObject fsObj = new GameObject("InviteStatus");
            fsObj.transform.SetParent(cardObj.transform, false);
            _friendStatusTxt = fsObj.AddComponent<Text>();
            _friendStatusTxt.text = "";
            _friendStatusTxt.font = font;
            _friendStatusTxt.fontSize = 17;
            _friendStatusTxt.alignment = TextAnchor.MiddleCenter;
            _friendStatusTxt.color = ColCyanBorder;
            _friendStatusTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _friendStatusTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform fsRt = fsObj.GetComponent<RectTransform>();
            fsRt.anchoredPosition = new Vector2(0f, 100f);
            fsRt.sizeDelta = new Vector2(580f, 26f);

            CreateTextComp(cardObj.transform, AegisLocalization.T("NGƯỜI CHƠI ĐANG TRỰC TUYẾN (ONLINE)", "ONLINE PLAYERS"), 16, FontStyle.Bold, ColTextMuted, font, new Vector2(0f, 65f), new Vector2(580f, 22f));

            GameObject containerObj = new GameObject("OnlinePlayersContainer");
            containerObj.transform.SetParent(cardObj.transform, false);
            _onlinePlayersContainer = containerObj.transform;
            RectTransform cRt = containerObj.AddComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(0f, 45f);
            cRt.sizeDelta = new Vector2(580f, 240f);

            CreateMenuActionButton(cardObj.transform, "BtnCloseFriendModal", AegisLocalization.T("ĐÓNG", "CLOSE"), new Vector2(0f, -240f), new Vector2(180f, 42f), ColGlassFill, ColTextMuted, 16, () => _friendModal.SetActive(false), font, ColMutedBorder, 1.2f);

            _friendModal.SetActive(false);
        }

        private void SendFriendChallenge(string targetName)
        {
            if (string.IsNullOrEmpty(targetName))
            {
                if (_friendStatusTxt != null) _friendStatusTxt.text = AegisLocalization.T("<color=#EF4444>Vui lòng nhập tên người chơi muốn thách đấu!</color>", "<color=#EF4444>Please enter the name of the player to challenge!</color>");
                return;
            }
            if (UserAccountManager.Instance != null && targetName.Trim().Equals(UserAccountManager.Instance.CurrentUsername, StringComparison.OrdinalIgnoreCase))
            {
                if (_friendStatusTxt != null) _friendStatusTxt.text = AegisLocalization.T("<color=#EF4444>Bạn không thể tự thách đấu chính mình!</color>", "<color=#EF4444>You cannot challenge yourself!</color>");
                return;
            }
            if (OnlineMultiplayerManager.Instance != null)
            {
                OnlineMultiplayerManager.Instance.SendFriendInvite(targetName.Trim());
                if (_friendStatusTxt != null) _friendStatusTxt.text = AegisLocalization.T($"<color=#B99A61>Đang gửi lời mời thách đấu tới '{targetName.Trim()}'...</color>", $"<color=#B99A61>Sending challenge invite to '{targetName.Trim()}'...</color>");
            }
        }

        private void UpdateOnlinePlayersUI(List<OnlinePlayerInfo> players)
        {
            if (_onlinePlayersContainer == null) return;
            foreach (var obj in _onlinePlayerRowObjs) if (obj != null) Destroy(obj);
            _onlinePlayerRowObjs.Clear();

            string myUsername = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentUsername : "";
            var otherPlayers = (players ?? new List<OnlinePlayerInfo>()).FindAll(p => p != null && p.username != myUsername);
            Font font = GetDefaultFont();

            if (otherPlayers.Count == 0)
            {
                GameObject emptyObj = new GameObject("EmptyNotice");
                emptyObj.transform.SetParent(_onlinePlayersContainer, false);
                Text emptyTxt = emptyObj.AddComponent<Text>();
                emptyTxt.text = AegisLocalization.T("Chưa có người chơi khác trực tuyến.\nBạn có thể mở thêm thiết bị với Gmail khác để thử thách đấu!", "No other players online.\nYou can open another device with a different Gmail to test friend matches!");
                emptyTxt.font = font;
                emptyTxt.fontSize = 17;
                emptyTxt.fontStyle = FontStyle.Italic;
                emptyTxt.alignment = TextAnchor.MiddleCenter;
                emptyTxt.color = ColTextMuted;
                emptyTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                emptyTxt.verticalOverflow = VerticalWrapMode.Overflow;
                RectTransform rt = emptyObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -60f);
                rt.sizeDelta = new Vector2(520f, 60f);
                _onlinePlayerRowObjs.Add(emptyObj);
                return;
            }

            for (int i = 0; i < Mathf.Min(otherPlayers.Count, 4); i++)
            {
                var p = otherPlayers[i];
                string targetName = p.username;
                int targetElo = p.elo;

                GameObject rowObj = new GameObject($"PlayerRow_{i}");
                rowObj.transform.SetParent(_onlinePlayersContainer, false);
                Image rowBg = rowObj.AddComponent<Image>();
                rowBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(580, 44, 12f, new Color(0.06f, 0.12f, 0.22f, 0.9f), ColMutedBorder, 1f);
                rowBg.type = Image.Type.Sliced;
                RectTransform rowRt = rowObj.GetComponent<RectTransform>();
                rowRt.anchoredPosition = new Vector2(0f, -30f - i * 55f);
                rowRt.sizeDelta = new Vector2(580f, 44f);

                CreateTextComp(rowObj.transform, $"●  {targetName} (Elo {targetElo})", 18, FontStyle.Bold, ColTextWhite, font, new Vector2(-100f, 0f), new Vector2(320f, 30f));

                CreateMenuActionButton(rowObj.transform, "BtnInvite", AegisLocalization.T("THÁCH ĐẤU", "CHALLENGE"), new Vector2(210f, 0f), new Vector2(120f, 34f), ColGoldCta, ColDarkBtnText, 16, () =>
                {
                    if (_friendTargetInput != null) _friendTargetInput.text = targetName;
                    SendFriendChallenge(targetName);
                }, font, ColGoldBorder, 1.2f);

                _onlinePlayerRowObjs.Add(rowObj);
            }
        }

        private void BuildIncomingChallengeModal(Transform parent, Font font)
        {
            _challengeModal = CreateModalOverlay(parent, "IncomingChallengeOverlay");

            GameObject cardObj = new GameObject("ChallengeCard");
            cardObj.transform.SetParent(_challengeModal.transform, false);

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(600, 350, 24f, ColGlassFill, ColCyanBorder, 2f);
            cardImg.type = Image.Type.Sliced;

            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(600f, 350f);

            CreateTextComp(cardObj.transform, AegisLocalization.T("⚔️  LỜI MỜI THÁCH ĐẤU  ⚔️", "⚔️  INCOMING CHALLENGE  ⚔️"), 20, FontStyle.Bold, ColGoldBorder, font, new Vector2(0f, 115f), new Vector2(540f, 36f));

            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(cardObj.transform, false);
            _challengeBodyTxt = bodyObj.AddComponent<Text>();
            _challengeBodyTxt.text = AegisLocalization.T("Người chơi muốn thách đấu với bạn!", "A player challenged you!");
            _challengeBodyTxt.font = font;
            _challengeBodyTxt.fontSize = 20;
            _challengeBodyTxt.alignment = TextAnchor.MiddleCenter;
            _challengeBodyTxt.color = ColTextWhite;
            _challengeBodyTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _challengeBodyTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
            bodyRt.anchoredPosition = new Vector2(0f, 25f);
            bodyRt.sizeDelta = new Vector2(520f, 90f);

            CreateMenuActionButton(cardObj.transform, "BtnAcceptChallenge", AegisLocalization.T("CHẤP NHẬN", "ACCEPT"), new Vector2(-110f, -95f), new Vector2(180f, 48f), ColGoldCta, ColDarkBtnText, 18, () =>
            {
                if (OnlineMultiplayerManager.Instance != null && !string.IsNullOrEmpty(_incomingInviteId))
                {
                    OnlineMultiplayerManager.Instance.RespondToFriendInvite(_incomingInviteId, _incomingFromUser, _incomingRoomId, true);
                }
                _challengeModal.SetActive(false);
            }, font, ColGoldBorder, 1.5f);

            CreateMenuActionButton(cardObj.transform, "BtnDeclineChallenge", AegisLocalization.T("TỪ CHỐI", "DECLINE"), new Vector2(110f, -95f), new Vector2(180f, 48f), ColGlassFill, ColTextMuted, 18, () =>
            {
                if (OnlineMultiplayerManager.Instance != null && !string.IsNullOrEmpty(_incomingInviteId))
                {
                    OnlineMultiplayerManager.Instance.RespondToFriendInvite(_incomingInviteId, _incomingFromUser, _incomingRoomId, false);
                }
                _challengeModal.SetActive(false);
            }, font, ColMutedBorder, 1.2f);

            _challengeModal.SetActive(false);
        }

        private void HandleIncomingChallenge(string fromUser, int fromElo, string inviteId, string roomId)
        {
            _incomingInviteId = inviteId;
            _incomingFromUser = fromUser;
            _incomingRoomId = roomId;

            if (_challengeBodyTxt != null)
            {
                _challengeBodyTxt.text = AegisLocalization.T(
                    $"Người chơi <b><color=#B99A61>{fromUser}</color></b> (Elo {fromElo})\ngửi lời mời thách đấu cờ vua với bạn!\n\nBạn có muốn chấp nhận lời mời thi đấu không?",
                    $"Player <b><color=#B99A61>{fromUser}</color></b> (Elo {fromElo})\nchallenged you to a chess match!\n\nDo you want to accept the challenge?"
                );
            }
            if (_challengeModal != null) _challengeModal.SetActive(true);
        }

        private void HandleInviteDeclined(string reason)
        {
            if (_friendStatusTxt != null) _friendStatusTxt.text = AegisLocalization.T($"<color=#EF4444>Đối phương đã từ chối: {reason}</color>", $"<color=#EF4444>Opponent declined: {reason}</color>");
        }

        private void HandleInviteError(string error)
        {
            if (_friendStatusTxt != null) _friendStatusTxt.text = AegisLocalization.T($"<color=#EF4444>Lỗi: {error}</color>", $"<color=#EF4444>Error: {error}</color>");
        }

        private void HandleMatchFound(string roomId, PieceColor role, string oppName, int oppElo)
        {
            if (_matchmakingModal != null) _matchmakingModal.SetActive(false);
            if (_friendModal != null) _friendModal.SetActive(false);
            if (_challengeModal != null) _challengeModal.SetActive(false);
            if (_matchmakingTimerCoroutine != null) StopCoroutine(_matchmakingTimerCoroutine);

            _lastLaunchMode = GameLaunchMode.OnlineMultiplayer;
            HideMenu();

            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                var board = MechChess.Chess3D.ChessBoard3DManager.Instance;
                board.ResetBoard();
                board.isVsAI = false;
            }

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.isRankedMode = true;
                UIManager2D.Instance.ConfigureTimeControl("10 + 0");
                string colorStr = (role == PieceColor.White) ? AegisLocalization.T("TRẮNG (đi trước)", "WHITE (plays first)") : AegisLocalization.T("ĐEN (đi sau)", "BLACK (plays second)");
                UIManager2D.Instance.RecordMove(AegisLocalization.T($"[TRẬN ĐẤU ONLINE] Bạn cầm quân {colorStr} đấu với {oppName} (Elo {oppElo}) | Phòng: {roomId}", $"[ONLINE MATCH] You play {colorStr} vs {oppName} (Elo {oppElo}) | Room: {roomId}"));
            }

            ShowToast(AegisLocalization.T($"Trận đấu bắt đầu! Đối thủ: {oppName}", $"Match started! Opponent: {oppName}"), ToastType.Success);
        }

        // =========================================================================
        // GAME LAUNCHER
        // =========================================================================
        public void LaunchGame(GameLaunchMode mode)
        {
            _lastLaunchMode = mode;
            HideMenu();

            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.ResetBoard();
            }

            switch (mode)
            {
                case GameLaunchMode.VersusAI:
                    StartGameVsAI(2);
                    break;

                case GameLaunchMode.LocalPassAndPlay:
                    if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                        MechChess.Chess3D.ChessBoard3DManager.Instance.isVsAI = false;
                    if (UIManager2D.Instance != null)
                    {
                        UIManager2D.Instance.isRankedMode = false;
                        UIManager2D.Instance.ConfigureTimeControl("10 + 0");
                        UIManager2D.Instance.RecordMove(AegisLocalization.T("[BẮT ĐẦU] Chế độ: Hai người chơi luân phiên (Pass & Play)", "[START] Mode: Local Pass & Play"));
                    }
                    ShowToast(AegisLocalization.T("Bắt đầu chế độ Hai người!", "Starting Pass & Play mode!"), ToastType.Success);
                    break;

                case GameLaunchMode.AugmentedReality:
                    if (UIManager2D.Instance != null)
                    {
                        UIManager2D.Instance.SetHUDVisible(true);
                        UIManager2D.Instance.SetARHUDVisible(true);
                        UIManager2D.Instance.RecordMove(AegisLocalization.T("[BẮT ĐẦU] Chế độ AR Thực Tế Tăng Cường kích hoạt.", "[START] AR Augmented Reality mode activated."));
                    }
                    if (MechChess.AR.HybridARController.Instance != null)
                    {
                        MechChess.AR.HybridARController.Instance.ApplyDisplayMode(0);
                        MechChess.AR.HybridARController.Instance.SetARActive(true);
                    }
                    ShowToast(AegisLocalization.T("Chế độ AR Camera sẵn sàng!", "AR Camera mode ready!"), ToastType.Success);
                    break;

                case GameLaunchMode.OnlineMultiplayer:
                    RequireLogin(OpenOnlineMatchChoiceModal);
                    break;
            }
        }

        public void ShowAILevelSelector()
        {
            OpenAIDifficultyModal();
        }

        public bool IsAnyModalOpen()
        {
            return (_aiDifficultyModal != null && _aiDifficultyModal.activeSelf) ||
                   (_settingsModal != null && _settingsModal.activeSelf) ||
                   (_tutorialModal != null && _tutorialModal.activeSelf) ||
                   (_shopModal != null && _shopModal.activeSelf) ||
                   (_pauseModal != null && _pauseModal.activeSelf) ||
                   (_resultScreenModal != null && _resultScreenModal.activeSelf) ||
                   (_profileModal != null && _profileModal.activeSelf) ||
                   (_learnScreenModal != null && _learnScreenModal.activeSelf) ||
                   (_confirmDialogModal != null && _confirmDialogModal.activeSelf) ||
                   (_loginModal != null && _loginModal.activeSelf) ||
                   (_matchmakingModal != null && _matchmakingModal.activeSelf) ||
                   (_friendModal != null && _friendModal.activeSelf) ||
                   (_challengeModal != null && _challengeModal.activeSelf);
        }

        public void EnsureBaseScreenActive()
        {
            if (_isInGameMatch)
            {
                if (_screenMainMenu != null) _screenMainMenu.SetActive(false);
                if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
                if (_screenSplash != null) _screenSplash.SetActive(false);
                if (!IsAnyModalOpen() && _menuCanvasObj != null)
                {
                    _menuCanvasObj.SetActive(false);
                }
                return;
            }

            if ((_screenMainMenu == null || !_screenMainMenu.activeSelf) &&
                (_screenModeSelect == null || !_screenModeSelect.activeSelf) &&
                (_screenSplash == null || !_screenSplash.activeSelf))
            {
                if (_screenMainMenu != null) _screenMainMenu.SetActive(true);
            }
        }

        public void CloseModal(GameObject modal)
        {
            if (modal != null) modal.SetActive(false);

            if (_isInGameMatch)
            {
                if (_screenMainMenu != null) _screenMainMenu.SetActive(false);
                if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
                if (_screenSplash != null) _screenSplash.SetActive(false);

                if (!IsAnyModalOpen() && _menuCanvasObj != null)
                {
                    Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                    if (c != null) c.sortingOrder = 90;
                    _menuCanvasObj.SetActive(false);
                }

                if (UIManager2D.Instance != null)
                {
                    UIManager2D.Instance.SetHUDVisible(true);
                }
                if (MechChess.Chess3D.ChessBoard3DManager.Instance != null && MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer != null)
                {
                    MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer.gameObject.SetActive(true);
                }
            }
            else
            {
                EnsureBaseScreenActive();
            }
        }

        public void OpenAIDifficultyModal()
        {
            CloseAllModals();
            if (!_isInGameMatch) EnsureBaseScreenActive();
            if (_aiDifficultyModal != null) _aiDifficultyModal.SetActive(true);
        }

        public void OpenSettingsModal()
        {
            if (_menuCanvasObj != null)
            {
                _menuCanvasObj.SetActive(true);
                if (_isInGameMatch)
                {
                    Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                    if (c != null) c.sortingOrder = 200;
                }
            }
            CloseAllModals();
            if (!_isInGameMatch)
            {
                EnsureBaseScreenActive();
            }
            else
            {
                if (_screenMainMenu != null) _screenMainMenu.SetActive(false);
                if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
                if (_screenSplash != null) _screenSplash.SetActive(false);
            }
            if (_settingsModal != null)
            {
                _settingsModal.SetActive(true);
                SwitchSettingsTab(0);
            }
        }

        public void OpenTutorialModal()
        {
            CloseAllModals();
            if (!_isInGameMatch) EnsureBaseScreenActive();
            if (_tutorialModal != null)
            {
                _tutorialModal.SetActive(true);
                UpdateTutorialUI();
            }
        }

        public void OpenShopModal()
        {
            CloseAllModals();
            if (!_isInGameMatch) EnsureBaseScreenActive();
            if (_shopModal != null) _shopModal.SetActive(true);
        }

        public void UpdateUserProfileUI()
        {
            string username = "Player001";
            int elo = 1200;

            if (UserAccountManager.Instance != null && UserAccountManager.Instance.IsLoggedIn)
            {
                username = UserAccountManager.Instance.CurrentUsername;
                elo = UserAccountManager.Instance.CurrentElo;
            }

            if (_profileNameTxt != null) _profileNameTxt.text = username;
            if (_profileLevelTxt != null) _profileLevelTxt.text = $"Lv. 5 • Elo {elo}";
        }

        // =========================================================================
        // PROCEDURAL UI HELPERS
        // =========================================================================
        private GameObject CreateFullscreenPanel(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            obj.SetActive(false);
            return obj;
        }

        private GameObject CreateModalOverlay(Transform parent, string name)
        {
            GameObject overlay = new GameObject(name);
            overlay.transform.SetParent(parent, false);

            Image bg = overlay.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.04f, 0.08f, 0.85f);
            bg.raycastTarget = true;

            RectTransform rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            overlay.SetActive(false);
            return overlay;
        }

        private void CreateModalHeader(Transform parent, string title, Action onClose, Font font)
        {
            GameObject header = new GameObject("ModalHeader");
            header.transform.SetParent(parent, false);
            RectTransform hRt = header.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -20f);
            hRt.sizeDelta = new Vector2(-60f, 50f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);
            Text t = titleObj.AddComponent<Text>();
            t.text = title;
            t.font = font;
            t.fontSize = 24;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = ColTextWhite;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.5f);
            trt.anchorMax = new Vector2(0f, 0.5f);
            trt.pivot = new Vector2(0f, 0.5f);
            trt.anchoredPosition = new Vector2(10f, 0f);
            trt.sizeDelta = new Vector2(480f, 36f);

            // Close ✕ button
            GameObject closeBtn = new GameObject("BtnClose");
            closeBtn.transform.SetParent(header.transform, false);
            Image cbImg = closeBtn.AddComponent<Image>();
            cbImg.sprite = ArcaneAssetLoader.GetCircleSprite(36, ColGlassFill, ColCyanBorder, 1.2f);
            Button cb = closeBtn.AddComponent<Button>();
            cb.onClick.AddListener(() =>
            {
                onClose?.Invoke();
                if (_isInGameMatch)
                {
                    if (!IsAnyModalOpen() && _menuCanvasObj != null)
                    {
                        _menuCanvasObj.SetActive(false);
                    }
                    if (UIManager2D.Instance != null)
                    {
                        UIManager2D.Instance.SetHUDVisible(true);
                    }
                    if (MechChess.Chess3D.ChessBoard3DManager.Instance != null && MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer != null)
                    {
                        MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer.gameObject.SetActive(true);
                    }
                }
                else
                {
                    EnsureBaseScreenActive();
                }
            });
            closeBtn.AddComponent<MenuButtonEffect>();
            RectTransform cbrt = closeBtn.GetComponent<RectTransform>();
            cbrt.anchorMin = new Vector2(1f, 0.5f);
            cbrt.anchorMax = new Vector2(1f, 0.5f);
            cbrt.pivot = new Vector2(1f, 0.5f);
            cbrt.anchoredPosition = new Vector2(-10f, 0f);
            cbrt.sizeDelta = new Vector2(36f, 36f);

            GameObject cTxtObj = new GameObject("✕");
            cTxtObj.transform.SetParent(closeBtn.transform, false);
            Text ct = cTxtObj.AddComponent<Text>();
            ct.text = "✕";
            ct.font = font;
            ct.fontSize = 20;
            ct.alignment = TextAnchor.MiddleCenter;
            ct.color = ColTextWhite;
            ct.horizontalOverflow = HorizontalWrapMode.Overflow;
            ct.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform ctr = cTxtObj.GetComponent<RectTransform>();
            ctr.anchorMin = Vector2.zero;
            ctr.anchorMax = Vector2.one;
            ctr.offsetMin = Vector2.zero;
            ctr.offsetMax = Vector2.zero;
        }

        private GameObject CreateGlassPill(Transform parent, string name, string label, Vector2 pos, Vector2 size, Font font, Action onClick)
        {
            GameObject pill = new GameObject(name);
            pill.transform.SetParent(parent, false);

            Image bg = pill.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)size.x, (int)size.y, size.y / 2f, ColGlassFill, ColCyanBorder, 1.2f);
            bg.type = Image.Type.Sliced;

            if (onClick != null)
            {
                Button btn = pill.AddComponent<Button>();
                btn.onClick.AddListener(() => onClick.Invoke());
                pill.AddComponent<MenuButtonEffect>();
            }

            RectTransform rt = pill.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            CreateTextComp(pill.transform, label, 18, FontStyle.Bold, ColTextWhite, font, Vector2.zero, size - new Vector2(20f, 0f));
            return pill;
        }

        private void CreateConfigLabel(Transform parent, string text, Vector2 pos, Font font)
        {
            CreateTextComp(parent, text, 17, FontStyle.Bold, ColCyanBorder, font, pos, new Vector2(360f, 24f));
        }

        private Image CreateRadioIndicator(Transform parent, Vector2 pos)
        {
            GameObject rObj = new GameObject("RadioCircle");
            rObj.transform.SetParent(parent, false);
            Image img = rObj.AddComponent<Image>();
            RectTransform rt = rObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(20f, 20f);
            return img;
        }

        private Text CreateTextComp(Transform parent, string content, int size, FontStyle style, Color color, Font font, Vector2 pos, Vector2 delta, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            GameObject txtObj = new GameObject("Txt_" + content.GetHashCode());
            txtObj.transform.SetParent(parent, false);
            Text t = txtObj.AddComponent<Text>();
            t.text = content;
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = delta;
            return t;
        }

        private void CreateMenuActionButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color fill, Color textCol, int fontSize, Action onClick, Font font, Color? border = null, float borderWidth = 1.2f)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image bg = btnObj.AddComponent<Image>();
            Color bCol = border.HasValue ? border.Value : Color.clear;
            bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)size.x, (int)size.y, size.y / 2f, fill, bCol, borderWidth);
            bg.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            btn.onClick.AddListener(() => onClick?.Invoke());
            btnObj.AddComponent<MenuButtonEffect>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            GameObject txtObj = new GameObject("Txt");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text t = txtObj.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = textCol;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        private InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 pos, Vector2 size, Font font)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent, false);

            Image bg = inputObj.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)size.x, (int)size.y, 14f, new Color(0.04f, 0.08f, 0.16f, 0.95f), ColCyanBorder, 1.2f);
            bg.type = Image.Type.Sliced;

            RectTransform rt = inputObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(inputObj.transform, false);
            Text phText = phObj.AddComponent<Text>();
            phText.text = placeholder;
            phText.font = font;
            phText.fontSize = 18;
            phText.fontStyle = FontStyle.Italic;
            phText.color = ColTextMuted;
            phText.alignment = TextAnchor.MiddleLeft;
            phText.horizontalOverflow = HorizontalWrapMode.Overflow;
            phText.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform phRt = phObj.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(16f, 0f);
            phRt.offsetMax = new Vector2(-16f, 0f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            Text textComp = textObj.AddComponent<Text>();
            textComp.font = font;
            textComp.fontSize = 18;
            textComp.color = ColTextWhite;
            textComp.alignment = TextAnchor.MiddleLeft;
            textComp.supportRichText = false;
            textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComp.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 0f);
            textRt.offsetMax = new Vector2(-16f, 0f);

            InputField inputField = inputObj.AddComponent<InputField>();
            inputField.textComponent = textComp;
            inputField.placeholder = phText;
            inputField.lineType = InputField.LineType.SingleLine;
            inputField.selectionColor = new Color(0.0f, 0.82f, 1.0f, 0.4f);

            return inputField;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Tahoma" }, 16);
            return font;
        }
    }
}
