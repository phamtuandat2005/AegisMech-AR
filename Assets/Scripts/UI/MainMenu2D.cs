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
    /// Distinguishes between dragging to scroll horizontally and clicking to select a game mode card.
    /// </summary>
    public class ModeCardClickFilter : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Vector2 _downPos;
        private float _downTime;
        public Action onClick;

        public void OnPointerDown(PointerEventData eventData)
        {
            _downPos = eventData.position;
            _downTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            float dist = Vector2.Distance(_downPos, eventData.position);
            float duration = Time.unscaledTime - _downTime;
            if (dist < 28f && duration < 1.2f)
            {
                onClick?.Invoke();
            }
        }
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
            AegisAudioManager.PlayButtonHover();
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
            AegisAudioManager.PlayButtonClick();
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
    /// Screen 8: Bộ Sưu Tập (Collection modal with 4 categories, 18 collectables, armory inspector & equip system)
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
        private static MainMenu2D _instance;
        public static MainMenu2D Instance
        {
            get
            {
                if (_instance == null) _instance = FindObjectOfType<MainMenu2D>();
                return _instance;
            }
            private set => _instance = value;
        }

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
        private GameObject _mailboxModal;
        private GameObject _mailBadgeObj;
        private GameObject _pauseModal;
        private GameObject _resultScreenModal;
        private GameObject _profileModal;
        private GameObject _learnScreenModal;
        private GameObject _confirmDialogModal;
        private GameObject _leaderboardModal;
        private int _selectedLeaderboardTab = 0;
        private readonly GameObject[] _leaderboardTabPanels = new GameObject[3];
        private readonly Image[] _leaderboardTabBgs = new Image[3];
        private readonly Text[] _leaderboardTabTexts = new Text[3];

        // Screen 16: Game Review & Analysis references
        private GameObject _gameReviewModal;
        private Image _reviewBoardImg;
        private Text _reviewTitleTxt;
        private Text _reviewAccuracyTxt;
        private Text _reviewPillsTxt;
        private Text _reviewMoveNavTxt;
        private Text _reviewEvalBadgeTxt;
        private Text _reviewEvalScoreTxt;
        private Text _reviewBestMoveTxt;
        private Text _reviewCommentTxt;
        private Transform _reviewMoveListParent;
        private RectTransform _reviewAccWhiteFill;
        private RectTransform _reviewAccBlackFill;
        private GameAnalysisData _activeAnalysis;
        private int _currentReviewPly = 0;
        private Coroutine _autoPlayCoroutine = null;
        private bool _isAutoPlaying = false;
        private Text _btnAutoPlayTxt = null;
        private MatchHistoryItem _lastFinishedMatch = null;

        // Screen 8: Collection State & References
        private GameObject _collectionModal;
        private int _selectedCollectionTab = 0; // 0: Quân Cờ, 1: Skin, 2: Bàn Cờ, 3: Hiệu Ứng
        private readonly Image[] _collectionTabBgs = new Image[4];
        private readonly Text[] _collectionTabTexts = new Text[4];
        private readonly GameObject[] _collectionCategoryContainers = new GameObject[4];
        private Text _collectionGoldTxt;
        private Text _collectionProgressTxt;
        private RectTransform _collectionProgressBarFill;
        private ScrollRect _collectionSkinsScrollRect;
        private ScrollRect _collectionBoardsScrollRect;
        private ScrollRect _collectionEffectsScrollRect;
        private GameObject _collectionChevronLeft;
        private GameObject _collectionChevronRight;
        private Coroutine _collectionScrollCoroutine;

        // Collection Inspector references
        private CollectionItemData _inspectedItem;
        private Image _inspectImg;
        private Text _inspectTitle;
        private Text _inspectRarity;
        private Text _inspectType;
        private Text _inspectDesc;
        private Text _inspectPerks;
        private GameObject _inspectEquipBtnObj;
        private Image _inspectEquipBtnBg;
        private Text _inspectEquipBtnTxt;
        private Button _inspectEquipBtn;

        // Player preference keys for equipped cosmetics
        public const string PrefsEquippedSkin = "AegisMech_Equipped_Skin";
        public const string PrefsEquippedBoard = "AegisMech_Equipped_Board";
        public const string PrefsEquippedEffect = "AegisMech_Equipped_Effect";

        public static string EquippedSkin => PlayerPrefs.GetString(PrefsEquippedSkin, "Quân Cờ Cổ Điển");
        public static string EquippedBoard => PlayerPrefs.GetString(PrefsEquippedBoard, "Spirit Blossom Arena");
        public static string EquippedEffect => PlayerPrefs.GetString(PrefsEquippedEffect, "Arcane Lightning");

        private class CollectionItemData
        {
            public string id;
            public string nameVi;
            public string nameEn;
            public int category; // 0: Piece, 1: Skin, 2: Board, 3: Effect
            public string rarity;
            public Color rarityColor;
            public string typeVi;
            public string typeEn;
            public string descVi;
            public string descEn;
            public string perksVi;
            public string perksEn;
            public string spriteName;
            public PieceType? pieceType;
            public bool isOwned;
            public string prefsKey;

            // Runtime UI references
            public GameObject cardObj;
            public Text statusText;
            public Image statusBg;
            public Image borderImg;
        }

        private readonly List<CollectionItemData> _allCollectionItems = new List<CollectionItemData>();

        // Toast & Tooltip Container
        private GameObject _toastContainer;
        private GameObject _tooltipCard;

        // Splash elements
        private RectTransform _splashBarFillRt;
        private RectTransform _splashFlareRt;
        private RectTransform _splashGearRt;
        private Text _splashPercentTxt;
        private Text _splashLoadingTxt;
        private Text _splashTipTxt;
        private bool _userTappedSplash = false;

        // Main Menu Header References
        private Text _profileNameTxt;
        private Text _profileLevelTxt;

        // Dynamic Profile Modal References
        private Text _profileModalNameComp;
        private Text _profileModalLevelComp;
        private Text _profileModalExpComp;
        private RectTransform _profileModalExpFillRt;
        private Text _profileModalEloComp;
        private Text _profileModalSubPvpLabel;
        private Text _profileModalPvpWinsComp;
        private Text _profileModalPvpLossesComp;
        private Text _profileModalPvpDrawsComp;
        private Text _profileModalRankTierComp;
        private Text _profileModalAiWinsComp;
        private Text _profileModalAiLossesComp;
        private Text _profileModalAiDrawsComp;
        private Text _profileModalWinsComp;
        private Text _profileModalLossesComp;
        private Text _profileModalDrawsComp;
        private Transform _matchHistoryRowsParent;
        private GameObject _profileSwitchAccountBtnObj;

        // Dynamic Main Menu Navigation Selection (0: Bắt đầu, 1: Chế độ chơi, 2: Cửa hàng, 3: Bộ sưu tập, 4: Cài đặt, 5: Thoát)
        private int _selectedMenuNavIndex = 0;
        private GameObject _navContainer;
        private readonly GameObject[] _navItemObjs = new GameObject[6];
        private readonly Image[] _navItemBgs = new Image[6];
        private readonly RectTransform[] _navItemRts = new RectTransform[6];
        private readonly RectTransform[] _navItemIcons = new RectTransform[6];
        private readonly Image[] _navItemIconImgs = new Image[6];
        private readonly Text[] _navItemTexts = new Text[6];
        private readonly GameObject[] _navItemChevrons = new GameObject[6];
        private readonly LayoutElement[] _navItemLayoutElements = new LayoutElement[6];

        // Settings Modal State (5 tabs, user reference design)
        private int _selectedSettingsTab = 0;
        private readonly Image[] _settingsTabBgs = new Image[5];
        private readonly Text[] _settingsTabTexts = new Text[5];
        private readonly Image[] _settingsTabIcons = new Image[5];
        private readonly GameObject[] _settingsPanels = new GameObject[5];

        // Mode Select State (5 feature cards: AI, Online PvP, 2P, AR, Learn)
        private int _selectedModeCardIndex = 0;
        private readonly Image[] _modeCardFrameBgs = new Image[5];
        private ScrollRect _modeScrollRect;
        private RectTransform _modeScrollContent;
        private Image _modePageDot0;
        private Image _modePageDot1;
        private GameObject _modeChevronLeft;
        private GameObject _modeChevronRight;
        private Coroutine _modeScrollCoroutine;

        // Shop State (3 category tabs, 4 feature items, user reference design)
        private int _selectedShopTab = 0; // 0: Skin quân cờ, 1: Bàn cờ, 2: Hiệu ứng
        private readonly Image[] _shopTabBgs = new Image[3];
        private readonly Text[] _shopTabTexts = new Text[3];
        private readonly GameObject[] _shopCategoryContainers = new GameObject[3];
        private readonly ScrollRect[] _shopCategoryScrollRects = new ScrollRect[3];
        private GameObject _shopChevronLeft;
        private GameObject _shopChevronRight;
        private Coroutine _shopScrollCoroutine;
        private Text _shopGoldTxt;
        private Text _shopGemTxt;
        private Text _headerGoldText;
        private Text _headerGemText;
        private GameObject _topRightToolbar;
        private int _currentGold = 1250;

        // Profile Modal State (3 tabs: Lịch sử đấu, Thành tích, Skin, user reference design)
        private int _selectedProfileTab = 0;
        private readonly Image[] _profileTabBgs = new Image[3];
        private readonly Text[] _profileTabTexts = new Text[3];
        private readonly GameObject[] _profileTabContainers = new GameObject[3];
        private Transform _profileSkinsScrollContent;

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
        private Text[] _aiDiffCardElos;
        private Image _sideWhiteCardBg;
        private Image _sideBlackCardBg;
        private Text _sideWhiteCardTxt;
        private Text _sideBlackCardTxt;
        private GameObject _aiTimeDropdownMenu;
        private GameObject _aiTimeDropdownBlocker;
        private Text _aiTimeDropdownBtnText;
        private Image[] _aiTimeOptionBgs;
        private Text[] _aiTimeOptionTexts;
        private Text[] _aiTimeOptionChecks;

        // Result Screen elements
        private Text _resultTitleTxt;
        private Text _resultReasonTxt;
        private Text _resultMovesTxt;
        private Text _resultDurationTxt;
        private Text _resultScoreTxt;
        private Text _resultEloTxt;
        private readonly Image[] _resultStarImgs = new Image[3];
        private Image _resultCrownImg;
        private Image _resultLaurelLeftImg;
        private Image _resultLaurelRightImg;
        private Image _resultDividerImg;

        // Online Matchmaking & Auth references (Liên Quân / Arena of Valor Style)
        private GameObject _loginModal;
        private GameObject _loginTabCredsPanel;
        private GameObject _loginTabEmailPanel;
        private Image _loginTabCredsBtnBg;
        private Image _loginTabEmailBtnBg;
        private Text _loginTabCredsBtnTxt;
        private Text _loginTabEmailBtnTxt;
        private InputField _loginAccountInput;
        private InputField _loginPasswordInput;
        private InputField _loginGmailInput;
        private InputField _loginUsernameInput;
        private Text _loginErrorTxt;
        private Action _postLoginAction;

        private GameObject _onlineChoiceModal;
        private GameObject _matchmakingModal;
        private Text _matchmakingTimerTxt;
        private Text _matchmakingStatusTxt;
        private RectTransform _radarIconRt;
        private Coroutine _matchmakingTimerCoroutine;

        private GameObject _lichessConnectModal;
        private InputField _lichessTokenInput;
        private Text _lichessStatusStripTxt;

        private GameObject _friendModal;
        private Text _friendMyNameTxt;
        private InputField _friendTargetInput;
        private Text _friendStatusTxt;
        private Transform _onlinePlayersContainer;
        private readonly List<GameObject> _onlinePlayerRowObjs = new List<GameObject>();

        // Mobile Legends Right Friends Drawer (Image 3)
        private GameObject _rightFriendsDrawer;
        private RectTransform _rightFriendsDrawerRt;
        private bool _friendsDrawerOpen = false;
        private RectTransform _friendsToggleArrowRt;
        private Text _friendsToggleArrowTxt;
        private Transform _rightFriendsListContainer;
        private Text _friendsDrawerHeaderTxt;
        private readonly List<GameObject> _rightFriendRowObjs = new List<GameObject>();

        // Mobile Legends Bottom Navigation Bar (Image 2)
        private GameObject _mlbbBottomBar;
        private Text _btnPlayNowText;

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
            if (_instance == null) _instance = this;
            else if (_instance != this) { Destroy(this); return; }

            _isInGameMatch = false;

            // Xóa sạch toàn bộ các Canvas cũ / tồn đọng từ Edit Mode trước khi dựng mới
            var existingCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in existingCanvases)
            {
                if (c != null && c.gameObject.name == "ArcaneMenuCanvas")
                {
                    DestroyImmediate(c.gameObject);
                }
            }
            _menuCanvasObj = null;

            MedievalUIAssetLoader.ClearCache();
            AegisAudioManager.EnsureInitialized();
            AegisAudioManager.Prewarm();
            AegisLocalization.LoadSavedLanguage();
            AegisLocalization.OnLanguageChanged += OnLanguageChanged;

            if (UserAccountManager.Instance != null)
            {
                UserAccountManager.Instance.OnProfileUpdated += HandleProfileUpdated;
                UserAccountManager.Instance.OnAccountUpdated += HandleAccountUpdated;
            }
            AegisMech.UI.AegisPrefs.CoinsChanged += UpdateUserProfileUI;
            AegisMech.UI.AegisPrefs.GemsChanged += UpdateUserProfileUI;

            BuildAllScreens(false);
        }

        private void OnEnable()
        {
            if (_instance == null) _instance = this;
            if (_menuCanvasObj == null)
            {
                var strayCanvas = GameObject.Find("ArcaneMenuCanvas");
                if (strayCanvas != null) Destroy(strayCanvas);
                BuildAllScreens(true);
                if (!_isInGameMatch)
                {
                    NavigateToHome();
                }
            }
            else if (!_isInGameMatch)
            {
                _menuCanvasObj.SetActive(true);
                if (_screenSplash == null || !_screenSplash.activeSelf)
                {
                    if (_screenMainMenu != null) _screenMainMenu.SetActive(true);
                }
            }
        }

        private void OnDestroy()
        {
            AegisLocalization.OnLanguageChanged -= OnLanguageChanged;
            if (UserAccountManager.Instance != null)
            {
                UserAccountManager.Instance.OnProfileUpdated -= HandleProfileUpdated;
                UserAccountManager.Instance.OnAccountUpdated -= HandleAccountUpdated;
            }
            AegisMech.UI.AegisPrefs.CoinsChanged -= UpdateUserProfileUI;
            AegisMech.UI.AegisPrefs.GemsChanged -= UpdateUserProfileUI;
        }

        private void HandleProfileUpdated(MechChess.Multiplayer.UserAccountData profile)
        {
            UpdateUserProfileUI();
        }

        private void HandleAccountUpdated(string username, string gmail, int elo)
        {
            UpdateUserProfileUI();
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
            bool isLeaderboardOpen = _leaderboardModal != null && _leaderboardModal.activeSelf;
            int savedLeaderboardTab = _selectedLeaderboardTab;
            bool isCollectionOpen = _collectionModal != null && _collectionModal.activeSelf;
            int savedCollectionTab = _selectedCollectionTab;
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
                else if (isLeaderboardOpen)
                {
                    if (_leaderboardModal != null)
                    {
                        _leaderboardModal.SetActive(true);
                        SwitchLeaderboardTab(savedLeaderboardTab);
                    }
                }
                else if (isCollectionOpen)
                {
                    if (_collectionModal != null)
                    {
                        _collectionModal.SetActive(true);
                        SwitchCollectionTab(savedCollectionTab);
                    }
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

            if (LichessBridge.Instance != null)
            {
                LichessBridge.Instance.OnGameStarted += (oppName, oppElo, isWhite) =>
                {
                    HandleLichessMatchFound(oppName, oppElo, isWhite);
                };
                LichessBridge.Instance.OnGameEnded += (result, eloDelta, goldGain, endReason) =>
                {
                    HandleLichessGameEnded(result, eloDelta, goldGain, endReason);
                };
                LichessBridge.Instance.OnAccountProfileUpdated += (ok, u, e) =>
                {
                    // UpdateLichessStatusStrip();
                };
            }

            // Ensure app keeps running in background for smooth multiplayer testing
            Application.runInBackground = true;

            // Start with Splash Screen Loading Animation
            StartCoroutine(RunSplashSequence());
        }

        private void Update()
        {
            // Toggle Fullscreen / Windowed with F11 or Alt+Enter on PC (New Input System safe)
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.f11Key.wasPressedThisFrame || ((kb.leftAltKey.isPressed || kb.rightAltKey.isPressed) && kb.enterKey.wasPressedThisFrame))
                {
                    if (Screen.fullScreenMode == FullScreenMode.Windowed)
                    {
                        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                    }
                    else
                    {
                        Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                    }
                }
            }

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

            // Hide main menu toolbar when any modal is open to prevent duplicate overlapping bars
            if (_topRightToolbar != null)
            {
                bool hideToolbar = IsAnyModalOpen() || _isInGameMatch || (_screenMainMenu != null && !_screenMainMenu.activeSelf);
                if (_topRightToolbar.activeSelf == hideToolbar)
                {
                    _topRightToolbar.SetActive(!hideToolbar);
                }
            }

            // Radar pulse in matchmaking modal
            if (_radarIconRt != null && _matchmakingModal != null && _matchmakingModal.activeSelf)
            {
                _radarIconRt.Rotate(0f, 0f, -120f * Time.unscaledDeltaTime);
            }

            // Update Mode Select Carousel Scroll Indicators (Dots & Side Chevrons)
            if (_screenModeSelect != null && _screenModeSelect.activeSelf && _modeScrollRect != null)
            {
                bool canScroll = _modeScrollContent != null && _modeScrollRect.viewport != null 
                    && (_modeScrollContent.rect.width > _modeScrollRect.viewport.rect.width + 10f);

                if (!canScroll)
                {
                    if (_modeChevronLeft != null) _modeChevronLeft.SetActive(false);
                    if (_modeChevronRight != null) _modeChevronRight.SetActive(false);
                    if (_modePageDot0 != null && _modePageDot0.transform.parent != null)
                        _modePageDot0.transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    float hPos = Mathf.Clamp01(_modeScrollRect.horizontalNormalizedPosition);
                    bool isPage0 = hPos < 0.5f;

                    if (_modePageDot0 != null)
                    {
                        if (_modePageDot0.transform.parent != null) _modePageDot0.transform.parent.gameObject.SetActive(true);
                        _modePageDot0.color = isPage0 ? new Color(0.95f, 0.82f, 0.42f, 1f) : new Color(0.35f, 0.28f, 0.20f, 0.65f);
                        _modePageDot0.rectTransform.sizeDelta = new Vector2(isPage0 ? 32f : 12f, 8f);
                    }
                    if (_modePageDot1 != null)
                    {
                        _modePageDot1.color = !isPage0 ? new Color(0.95f, 0.82f, 0.42f, 1f) : new Color(0.35f, 0.28f, 0.20f, 0.65f);
                        _modePageDot1.rectTransform.sizeDelta = new Vector2(!isPage0 ? 32f : 12f, 8f);
                    }

                    if (_modeChevronLeft != null)
                    {
                        _modeChevronLeft.SetActive(true);
                        var lBtn = _modeChevronLeft.GetComponent<Button>();
                        var lImg = _modeChevronLeft.GetComponent<Image>();
                        bool active = hPos > 0.05f;
                        if (lBtn != null) lBtn.interactable = active;
                        if (lImg != null) lImg.color = active ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                    }
                    if (_modeChevronRight != null)
                    {
                        _modeChevronRight.SetActive(true);
                        var rBtn = _modeChevronRight.GetComponent<Button>();
                        var rImg = _modeChevronRight.GetComponent<Image>();
                        bool active = hPos < 0.95f;
                        if (rBtn != null) rBtn.interactable = active;
                        if (rImg != null) rImg.color = active ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                    }
                }
            }

            // Update Shop Scroll Indicators (Side Chevrons)
            if (_shopModal != null && _shopModal.activeSelf)
            {
                int tab = _selectedShopTab;
                ScrollRect curSr = (tab >= 0 && tab < _shopCategoryScrollRects.Length) ? _shopCategoryScrollRects[tab] : null;
                if (curSr != null)
                {
                    float hPos = Mathf.Clamp01(curSr.horizontalNormalizedPosition);
                    if (_shopChevronLeft != null)
                    {
                        var lBtn = _shopChevronLeft.GetComponent<Button>();
                        var lImg = _shopChevronLeft.GetComponent<Image>();
                        bool active = hPos > 0.05f;
                        if (lBtn != null) lBtn.interactable = active;
                        if (lImg != null) lImg.color = active ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                    }
                    if (_shopChevronRight != null)
                    {
                        var rBtn = _shopChevronRight.GetComponent<Button>();
                        var rImg = _shopChevronRight.GetComponent<Image>();
                        bool active = hPos < 0.95f;
                        if (rBtn != null) rBtn.interactable = active;
                        if (rImg != null) rImg.color = active ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                    }
                }
            }
        }

        public bool IsMenuVisible() => _menuCanvasObj != null && _menuCanvasObj.activeSelf;

        public void ShowMenu()
        {
            _isInGameMatch = false;
            CloseAllModals();
            if (_menuCanvasObj != null) _menuCanvasObj.SetActive(true);
            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.SetARHUDVisible(false);
                UIManager2D.Instance.SetHUDVisible(false);
            }
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

            if (UIManager2D.Instance != null)
            {
                if (_lastLaunchMode != GameLaunchMode.AugmentedReality)
                {
                    UIManager2D.Instance.SetARHUDVisible(false);
                }
                UIManager2D.Instance.SetHUDVisible(true);
            }
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
            if (_screenMainMenu != null)
            {
                _screenMainMenu.SetActive(true);
                _screenMainMenu.transform.SetSiblingIndex(1);
            }
            if (_menuCanvasObj != null) _menuCanvasObj.SetActive(true);
            _selectedMenuNavIndex = 0;
            UpdateNavSelection(0);
            UpdateRightFriendsDrawerUI(OnlineMultiplayerManager.Instance?.CachedOnlinePlayers);

            // Double safeguard: keep in-game HUD and 3D board hidden while on main menu
            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.SetARHUDVisible(false);
                UIManager2D.Instance.SetHUDVisible(false);
            }
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

            if (_modeScrollRect != null)
            {
                _modeScrollRect.horizontalNormalizedPosition = 0f;
            }

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.SetARHUDVisible(false);
                UIManager2D.Instance.SetHUDVisible(false);
            }
            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null && MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.BoardRootContainer.gameObject.SetActive(false);
            }
        }

        public void CloseAllModals()
        {
            bool anyOpen = IsAnyModalOpen();
            if (_aiDifficultyModal != null) _aiDifficultyModal.SetActive(false);
            if (_settingsModal != null) _settingsModal.SetActive(false);
            if (_tutorialModal != null) _tutorialModal.SetActive(false);
            if (_shopModal != null) _shopModal.SetActive(false);
            if (_mailboxModal != null) _mailboxModal.SetActive(false);
            if (_collectionModal != null)
            {
                if (_collectionScrollCoroutine != null)
                {
                    StopCoroutine(_collectionScrollCoroutine);
                    _collectionScrollCoroutine = null;
                }
                _collectionModal.SetActive(false);
            }
            if (_pauseModal != null) _pauseModal.SetActive(false);
            if (_resultScreenModal != null) _resultScreenModal.SetActive(false);
            if (_profileModal != null) _profileModal.SetActive(false);
            if (_learnScreenModal != null) _learnScreenModal.SetActive(false);
            if (_confirmDialogModal != null) _confirmDialogModal.SetActive(false);
            if (_loginModal != null) _loginModal.SetActive(false);
            if (_onlineChoiceModal != null) _onlineChoiceModal.SetActive(false);
            if (_matchmakingModal != null) _matchmakingModal.SetActive(false);
            if (_friendModal != null) _friendModal.SetActive(false);
            if (_challengeModal != null) _challengeModal.SetActive(false);
            if (_leaderboardModal != null) _leaderboardModal.SetActive(false);
            if (_gameReviewModal != null) _gameReviewModal.SetActive(false);
            if (_lichessConnectModal != null) _lichessConnectModal.SetActive(false);
            if (anyOpen) AegisAudioManager.PlayModalClose();
        }

        // =========================================================================
        // MASTER UI BUILDER
        // =========================================================================
        private void BuildAllScreens(bool isRefresh = false)
        {
            EventSystem es = FindObjectOfType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            else
            {
                var legacyInput = es.GetComponent<StandaloneInputModule>();
                if (legacyInput != null)
                {
                    DestroyImmediate(legacyInput);
                }
                if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }

            _menuCanvasObj = new GameObject("ArcaneMenuCanvas");
            _menuCanvasObj.transform.SetParent(transform, false);

            Canvas canvas = _menuCanvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;

            _menuScaler = _menuCanvasObj.AddComponent<CanvasScaler>();
            _menuScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _menuScaler.referenceResolution = new Vector2(1920, 1080);
            _menuScaler.dynamicPixelsPerUnit = 2.5f;
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
            BuildMailboxModal(_menuCanvasObj.transform, font);
            BuildScreen7Shop(_menuCanvasObj.transform, font);
            BuildScreen8Collection(_menuCanvasObj.transform, font);
            BuildScreen10Pause(_menuCanvasObj.transform, font);
            BuildScreen11Result(_menuCanvasObj.transform, font);
            BuildScreen12Profile(_menuCanvasObj.transform, font);
            BuildScreen13Learn(_menuCanvasObj.transform, font);
            BuildScreen14Confirm(_menuCanvasObj.transform, font);
            BuildScreen15ToastContainer(_menuCanvasObj.transform, font);
            BuildScreen16GameReview(_menuCanvasObj.transform, font);

            // Build Multiplayer & Auth Dialogs
            BuildLoginModal(_menuCanvasObj.transform, font);
            BuildMatchmakingModal(_menuCanvasObj.transform, font);
            BuildFriendModal(_menuCanvasObj.transform, font);
            BuildOnlineChoiceModal(_menuCanvasObj.transform, font);
            BuildIncomingChallengeModal(_menuCanvasObj.transform, font);
            BuildSeasonLeaderboardModal(_menuCanvasObj.transform, font);
            // BuildLichessConnectModal(_menuCanvasObj.transform, font);

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
        // =========================================================================
        // SCREEN 1: SPLASH SCREEN (Animated loading bar 0-100% with Medieval Royal Design)
        // =========================================================================
        private void BuildScreen1Splash(Transform parent, Font font)
        {
            _screenSplash = CreateFullscreenPanel(parent, "Screen1_Splash");
            _screenSplash.AddComponent<CanvasGroup>();

            // Fullscreen click to enter after loading finishes
            Button splashBtn = _screenSplash.AddComponent<Button>();
            splashBtn.transition = Selectable.Transition.None;
            splashBtn.onClick.AddListener(() =>
            {
                _userTappedSplash = true;
            });

            // 1. Fullscreen Citadel Background
            Image bgImg = _screenSplash.AddComponent<Image>();
            bgImg.raycastTarget = true;
            splashBtn.targetGraphic = bgImg;
            Sprite splashBg = MedievalUIAssetLoader.GetSprite("medieval_main_menu_bg") ?? ArcaneAssetLoader.LoadArcaneSprite("main_menu_bg.png");
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

            // 2. Top-Left: Aegis Games Publisher Logo
            GameObject logoObj = new GameObject("AegisGamesLogo");
            logoObj.transform.SetParent(_screenSplash.transform, false);
            RectTransform logoRt = logoObj.AddComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0f, 1f);
            logoRt.anchorMax = new Vector2(0f, 1f);
            logoRt.pivot = new Vector2(0f, 1f);
            logoRt.anchoredPosition = new Vector2(25f, -18f);
            logoRt.sizeDelta = new Vector2(133f, 60f);
            Image logoImg = logoObj.AddComponent<Image>();
            Sprite logoSp = MedievalUIAssetLoader.GetSprite("aegis_games_logo");
            if (logoSp != null)
            {
                logoImg.sprite = logoSp;
                logoImg.preserveAspect = true;
            }
            else
            {
                logoImg.color = Color.clear;
            }
            logoImg.raycastTarget = false;

            // 3. Top-Right: Version & Server Info
            GameObject verObj = new GameObject("VersionServerText");
            verObj.transform.SetParent(_screenSplash.transform, false);
            RectTransform verRt = verObj.AddComponent<RectTransform>();
            verRt.anchorMin = new Vector2(1f, 1f);
            verRt.anchorMax = new Vector2(1f, 1f);
            verRt.pivot = new Vector2(1f, 1f);
            verRt.anchoredPosition = new Vector2(-25f, -20f);
            verRt.sizeDelta = new Vector2(260f, 24f);
            Text verTxt = verObj.AddComponent<Text>();
            verTxt.font = font;
            verTxt.fontSize = 12;
            verTxt.alignment = TextAnchor.MiddleRight;
            verTxt.color = new Color(0.88f, 0.82f, 0.72f, 0.80f);
            verTxt.text = $"Phiên bản: v{Application.version} | Server: Asia";
            verTxt.raycastTarget = false;

            // 4. Center-Top: Grand "AegisMech-AR" Title Logo with Crown and Ribbon
            GameObject titleObj = new GameObject("TitleLogoAegisMech");
            titleObj.transform.SetParent(_screenSplash.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.73f);
            titleRt.anchorMax = new Vector2(0.5f, 0.73f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(620f, 246f);
            Image titleImg = titleObj.AddComponent<Image>();
            titleImg.sprite = MedievalUIAssetLoader.GetSprite("logo_aegismech");
            titleImg.preserveAspect = true;
            titleImg.raycastTarget = false;

            // Grand Subtitle Ribbon: [ CHESS · STRATEGY · AR ]
            GameObject ribbonObj = new GameObject("TitleRibbon");
            ribbonObj.transform.SetParent(titleObj.transform, false);
            RectTransform rRt = ribbonObj.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.5f, 0f);
            rRt.anchorMax = new Vector2(0.5f, 0f);
            rRt.pivot = new Vector2(0.5f, 1f);
            rRt.anchoredPosition = new Vector2(0f, 10f);
            rRt.sizeDelta = new Vector2(340f, 46f);

            Image rImg = ribbonObj.AddComponent<Image>();
            rImg.sprite = MedievalUIAssetLoader.GetSprite("banner_gold_ribbon");
            rImg.type = Image.Type.Sliced;
            rImg.color = Color.white;
            rImg.raycastTarget = false;

            GameObject rTxtObj = new GameObject("Text");
            rTxtObj.transform.SetParent(ribbonObj.transform, false);
            Text rTxt = rTxtObj.AddComponent<Text>();
            rTxt.text = "CHESS  ·  STRATEGY  ·  AR";
            rTxt.font = font;
            rTxt.fontSize = 15;
            rTxt.fontStyle = FontStyle.Bold;
            rTxt.alignment = TextAnchor.MiddleCenter;
            rTxt.color = new Color(0.96f, 0.88f, 0.58f, 1f);
            rTxt.raycastTarget = false;
            MakeTextCrisp(rTxt);

            RectTransform rtrt = rTxtObj.GetComponent<RectTransform>();
            rtrt.anchorMin = Vector2.zero;
            rtrt.anchorMax = Vector2.one;
            rtrt.offsetMin = new Vector2(25f, 2f);
            rtrt.offsetMax = new Vector2(-25f, -2f);

            // 6. Loading Status: Gear Icon + "ĐANG TẢI... 74%" (To rõ, vàng rực rỡ)
            GameObject statusObj = new GameObject("LoadingStatusContainer");
            statusObj.transform.SetParent(_screenSplash.transform, false);
            RectTransform statusRt = statusObj.AddComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.5f, 0.25f);
            statusRt.anchorMax = new Vector2(0.5f, 0.25f);
            statusRt.pivot = new Vector2(0.5f, 0.5f);
            statusRt.anchoredPosition = Vector2.zero;
            statusRt.sizeDelta = new Vector2(460f, 44f);

            GameObject gearObj = new GameObject("LoadingGearIcon");
            gearObj.transform.SetParent(statusObj.transform, false);
            _splashGearRt = gearObj.AddComponent<RectTransform>();
            _splashGearRt.anchorMin = new Vector2(0.5f, 0.5f);
            _splashGearRt.anchorMax = new Vector2(0.5f, 0.5f);
            _splashGearRt.pivot = new Vector2(0.5f, 0.5f);
            _splashGearRt.anchoredPosition = new Vector2(-120f, 0f);
            _splashGearRt.sizeDelta = new Vector2(36f, 36f);
            Image gearImg = gearObj.AddComponent<Image>();
            gearImg.sprite = MedievalUIAssetLoader.GetSprite("icon_round_gear");
            gearImg.preserveAspect = true;
            gearImg.raycastTarget = false;

            GameObject pctObj = new GameObject("LoadingPercentText");
            pctObj.transform.SetParent(statusObj.transform, false);
            RectTransform pctRt = pctObj.AddComponent<RectTransform>();
            pctRt.anchorMin = new Vector2(0.5f, 0.5f);
            pctRt.anchorMax = new Vector2(0.5f, 0.5f);
            pctRt.pivot = new Vector2(0f, 0.5f);
            pctRt.anchoredPosition = new Vector2(-92f, 0f);
            pctRt.sizeDelta = new Vector2(300f, 38f);
            _splashPercentTxt = pctObj.AddComponent<Text>();
            _splashPercentTxt.font = font;
            _splashPercentTxt.fontSize = 25;
            _splashPercentTxt.fontStyle = FontStyle.Bold;
            _splashPercentTxt.alignment = TextAnchor.MiddleLeft;
            _splashPercentTxt.color = new Color(1.0f, 0.90f, 0.60f, 1.0f);
            _splashPercentTxt.text = AegisLocalization.T("ĐANG TẢI... 0%", "LOADING... 0%");
            MakeTextCrisp(_splashPercentTxt, new Color(0.12f, 0.06f, 0.02f, 0.95f));
            _splashPercentTxt.raycastTarget = false;

            // 7. Ornate Loading Progress Bar (Phóng to 1040 x 44, cân đối hoàn mỹ)
            float barWidth = 1040f;
            float barHeight = 44f;
            float trackInsetX = 56f;

            GameObject barContainer = new GameObject("LoadingBarContainer");
            barContainer.transform.SetParent(_screenSplash.transform, false);
            RectTransform bcRt = barContainer.AddComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0.18f);
            bcRt.anchorMax = new Vector2(0.5f, 0.18f);
            bcRt.pivot = new Vector2(0.5f, 0.5f);
            bcRt.anchoredPosition = Vector2.zero;
            bcRt.sizeDelta = new Vector2(barWidth, barHeight);

            Image barBg = barContainer.AddComponent<Image>();
            barBg.sprite = MedievalUIAssetLoader.GetSprite("loading_bar_frame");
            barBg.type = Image.Type.Sliced;
            barBg.raycastTarget = false;

            // Inner Fill Track Container with RectMask2D
            GameObject fillTrack = new GameObject("FillTrack");
            fillTrack.transform.SetParent(barContainer.transform, false);
            RectTransform ftRt = fillTrack.AddComponent<RectTransform>();
            ftRt.anchorMin = new Vector2(0f, 0f);
            ftRt.anchorMax = new Vector2(1f, 1f);
            ftRt.pivot = new Vector2(0.5f, 0.5f);
            ftRt.anchoredPosition = Vector2.zero;
            ftRt.sizeDelta = new Vector2(-trackInsetX * 2f, -12f);
            fillTrack.AddComponent<RectMask2D>();

            // Molten Gold Fill Beam
            GameObject barFill = new GameObject("LoadingBarFill");
            barFill.transform.SetParent(fillTrack.transform, false);
            Image fillImg = barFill.AddComponent<Image>();
            fillImg.sprite = MedievalUIAssetLoader.GetSprite("loading_bar_fill");
            fillImg.type = Image.Type.Sliced;
            fillImg.raycastTarget = false;
            _splashBarFillRt = barFill.GetComponent<RectTransform>();
            _splashBarFillRt.anchorMin = new Vector2(0f, 0f);
            _splashBarFillRt.anchorMax = new Vector2(0f, 1f);
            _splashBarFillRt.pivot = new Vector2(0f, 0.5f);
            _splashBarFillRt.anchoredPosition = Vector2.zero;
            _splashBarFillRt.sizeDelta = new Vector2(0f, 0f);
            _splashFlareRt = null;

            // 8. Subcaption underneath bar
            GameObject subObj = new GameObject("LoadingSubcaptionText");
            subObj.transform.SetParent(_screenSplash.transform, false);
            RectTransform subRt = subObj.AddComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 0.115f);
            subRt.anchorMax = new Vector2(0.5f, 0.115f);
            subRt.pivot = new Vector2(0.5f, 0.5f);
            subRt.anchoredPosition = Vector2.zero;
            subRt.sizeDelta = new Vector2(850f, 32f);
            _splashLoadingTxt = subObj.AddComponent<Text>();
            _splashLoadingTxt.font = font;
            _splashLoadingTxt.fontSize = 17;
            _splashLoadingTxt.fontStyle = FontStyle.Normal;
            _splashLoadingTxt.alignment = TextAnchor.MiddleCenter;
            _splashLoadingTxt.color = new Color(0.90f, 0.86f, 0.78f, 0.95f);
            _splashLoadingTxt.text = AegisLocalization.T(
                "Đang tải tài nguyên trò chơi, vui lòng không tắt ứng dụng...",
                "Loading game resources, please do not close the app..."
            );
            MakeTextCrisp(_splashLoadingTxt, new Color(0.10f, 0.05f, 0.02f, 0.90f));
            _splashLoadingTxt.raycastTarget = false;

            // 9. Copyright Footer
            GameObject copyObj = new GameObject("CopyrightFooter");
            copyObj.transform.SetParent(_screenSplash.transform, false);
            RectTransform copyRt = copyObj.AddComponent<RectTransform>();
            copyRt.anchorMin = new Vector2(0.5f, 0.045f);
            copyRt.anchorMax = new Vector2(0.5f, 0.045f);
            copyRt.pivot = new Vector2(0.5f, 0.5f);
            copyRt.anchoredPosition = Vector2.zero;
            copyRt.sizeDelta = new Vector2(500f, 24f);
            Text copyTxt = copyObj.AddComponent<Text>();
            copyTxt.font = font;
            copyTxt.fontSize = 14;
            copyTxt.alignment = TextAnchor.MiddleCenter;
            copyTxt.color = new Color(0.70f, 0.65f, 0.58f, 0.75f);
            copyTxt.text = "© 2026 Aegis Games. All Rights Reserved.";
            copyTxt.raycastTarget = false;
            copyTxt.fontSize = 11;
            copyTxt.alignment = TextAnchor.MiddleCenter;
            copyTxt.color = new Color(0.60f, 0.55f, 0.50f, 0.70f);
            copyTxt.text = "© 2026 AegisMech Studios. All rights reserved.";
            copyTxt.raycastTarget = false;
        }

        private IEnumerator RunSplashSequence()
        {
            _userTappedSplash = false;
            if (_screenSplash != null)
            {
                _screenSplash.SetActive(true);
                _screenSplash.transform.SetAsLastSibling();
            }

            CanvasGroup cg = _screenSplash != null ? _screenSplash.GetComponent<CanvasGroup>() : null;
            if (cg != null) cg.alpha = 1f;

            float barWidth = 1040f;
            float trackInsetX = 56f;
            float maxFillWidth = barWidth - (trackInsetX * 2f); // 928f

            // 1. Reset loading bar and text to clean initial 0% state
            if (_splashBarFillRt != null) _splashBarFillRt.sizeDelta = new Vector2(0f, 0f);
            if (_splashFlareRt != null) _splashFlareRt.anchoredPosition = new Vector2(trackInsetX, 0f);
            if (_splashPercentTxt != null) _splashPercentTxt.text = AegisLocalization.T("ĐANG TẢI... 0%", "LOADING... 0%");

            // 2. CRUCIAL FIX FOR PLAY MODE AUDIO DESYNC:
            // When entering Play mode in Unity Editor, the engine stalls for domain reload / shader warmup / GameView rendering.
            // If audio plays immediately on frame 0, the player hears the sound while the GameView is still blank or frozen!
            // We wait for initial frames so the splash screen is 100% visibly drawn on screen in the GameView:
            yield return new WaitForEndOfFrame();
            yield return null;
            yield return new WaitForEndOfFrame();

            // A tiny micro-pause (120ms) after GameView appears to allow player eye to register the splash screen
            yield return new WaitForSecondsRealtime(0.12f);

            // 3. NOW play the startup jingle at the EXACT moment the animation starts!
            AegisAudioManager.PlayStartupJingle();

            float duration = 2.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

                if (_splashBarFillRt != null)
                {
                    _splashBarFillRt.sizeDelta = new Vector2(easedProgress * maxFillWidth, 0f);
                }
                if (_splashFlareRt != null)
                {
                    _splashFlareRt.anchoredPosition = new Vector2(trackInsetX + (easedProgress * maxFillWidth), 0f);
                }
                if (_splashGearRt != null)
                {
                    _splashGearRt.Rotate(0f, 0f, -120f * Time.unscaledDeltaTime);
                }
                if (_splashPercentTxt != null)
                {
                    _splashPercentTxt.text = AegisLocalization.T(
                        $"ĐANG TẢI... {Mathf.RoundToInt(easedProgress * 100f)}%",
                        $"LOADING... {Mathf.RoundToInt(easedProgress * 100f)}%"
                    );
                }
                yield return null;
            }

            if (_splashBarFillRt != null) _splashBarFillRt.sizeDelta = new Vector2(maxFillWidth, 0f);
            if (_splashFlareRt != null) _splashFlareRt.anchoredPosition = new Vector2(trackInsetX + maxFillWidth, 0f);
            if (_splashPercentTxt != null) _splashPercentTxt.text = AegisLocalization.T("ĐANG TẢI... 100%", "LOADING... 100%");

            // Brief ready notification and auto-advance into Main Menu (no infinite stall waiting for click)
            if (_splashLoadingTxt != null)
            {
                _splashLoadingTxt.text = AegisLocalization.T("✦ SẴN SÀNG ✦", "✦ READY ✦");
                _splashLoadingTxt.fontSize = 16;
                _splashLoadingTxt.fontStyle = FontStyle.Bold;
                _splashLoadingTxt.color = ColCyanGlow;
            }

            _userTappedSplash = false;
            float autoAdvanceTimer = 0.35f;
            while (!_userTappedSplash && autoAdvanceTimer > 0f)
            {
                autoAdvanceTimer -= Time.unscaledDeltaTime;
                var mouse = UnityEngine.InputSystem.Mouse.current;
                var touch = UnityEngine.InputSystem.Touchscreen.current;
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if ((mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                    (touch != null && touch.primaryTouch.press.wasPressedThisFrame) ||
                    (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.anyKey.wasPressedThisFrame)))
                {
                    _userTappedSplash = true;
                    break;
                }
                yield return null;
            }

            AegisAudioManager.PlayButtonClick();

            if (_screenMainMenu != null)
            {
                _screenMainMenu.SetActive(true);
                _screenMainMenu.transform.SetSiblingIndex(1);
            }

            if (cg != null)
            {
                float fadeDuration = 0.35f;
                float fadeElapsed = 0f;
                while (fadeElapsed < fadeDuration)
                {
                    fadeElapsed += Time.unscaledDeltaTime;
                    cg.alpha = 1f - Mathf.Clamp01(fadeElapsed / fadeDuration);
                    yield return null;
                }
            }

            NavigateToHome();
        }

        // =========================================================================
        // SCREEN 2: MAIN MENU (Floating Citadel + Champion Dialogue + Glassy Left Nav)
        // =========================================================================
        // SCREEN 2: MAIN MENU (Medieval Fantasy Chess - Clean RPG Presentation)
        // =========================================================================
        private void BuildScreen2MainMenu(Transform parent, Font font)
        {
            _screenMainMenu = CreateFullscreenPanel(parent, "Screen2_MainMenu");

            // Citadel Background (High-res 1920x1080 Medieval Fantasy Castle + Mech scene)
            Image bgImg = _screenMainMenu.AddComponent<Image>();
            Sprite mainBg = MedievalUIAssetLoader.GetSprite("medieval_main_menu_bg") ?? ArcaneAssetLoader.LoadArcaneSprite("main_menu_bg.png");
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

            // Top-Left Logo
            BuildTopLeftLogo(_screenMainMenu.transform, font);

            // Left Navigation Stack (6 Medieval Buttons matching user mockup)
            BuildLeftMainMenuNav(_screenMainMenu.transform, font);

            // Top-Right Quick Header Toolbar (Gold 12,560 +, Gems 320 +, Mail, Inbox)
            BuildTopRightProfileHeader(_screenMainMenu.transform, font);

            // Right Friends Drawer (MLBB/WildRift style collapsible social drawer)
            BuildRightFriendsDrawer(_screenMainMenu.transform, font);
        }

        private void BuildTopLeftLogo(Transform parent, Font font)
        {
            GameObject logoContainer = new GameObject("TopLeftLogo");
            logoContainer.transform.SetParent(parent, false);

            RectTransform lcRt = logoContainer.AddComponent<RectTransform>();
            lcRt.anchorMin = new Vector2(0f, 1f);
            lcRt.anchorMax = new Vector2(0f, 1f);
            lcRt.pivot = new Vector2(0f, 1f);
            lcRt.anchoredPosition = new Vector2(50f, -20f);
            lcRt.sizeDelta = new Vector2(640f, 254f); // Enlarged to occupy 1/3 of the 1920 interface width

            Image logoImg = logoContainer.AddComponent<Image>();
            Sprite logoSprite = MedievalUIAssetLoader.GetSprite("logo_aegismech") ?? ArcaneAssetLoader.LoadArcaneSprite("hud_logo_chess_3dar.png");
            if (logoSprite != null)
            {
                logoImg.sprite = logoSprite;
                logoImg.preserveAspect = true;
            }
            else
            {
                logoImg.color = Color.clear;
            }

            Button logoBtn = logoContainer.AddComponent<Button>();
            logoBtn.onClick.AddListener(() =>
            {
                ShowToast("AegisMech AR — Phiên bản Chess 3D AR", ToastType.Info);
            });
            logoContainer.AddComponent<MenuButtonEffect>();
        }

        private void BuildBottomMottoRibbon(Transform parent, Font font)
        {
            GameObject ribbonObj = new GameObject("BottomMottoRibbon");
            ribbonObj.transform.SetParent(parent, false);

            RectTransform rRt = ribbonObj.AddComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0f, 0f);
            rRt.anchorMax = new Vector2(0f, 0f);
            rRt.pivot = new Vector2(0.5f, 0f);
            rRt.anchoredPosition = new Vector2(370f, 20f); // Centered with Logo & Navigation Stack at X=370
            rRt.sizeDelta = new Vector2(520f, 104f);

            Image rImg = ribbonObj.AddComponent<Image>();
            Sprite rSp = MedievalUIAssetLoader.GetSprite("ribbon_parchment") ?? MedievalUIAssetLoader.GetSprite("banner_tip_full") ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.ParchmentRibbon);
            if (rSp != null)
            {
                rImg.sprite = rSp;
                rImg.type = Image.Type.Simple;
                rImg.preserveAspect = true;
            }
            else
            {
                rImg.color = new Color(0.12f, 0.08f, 0.04f, 0.85f);
            }

            GameObject txtObj = new GameObject("MottoText");
            txtObj.transform.SetParent(ribbonObj.transform, false);
            Text t = txtObj.AddComponent<Text>();
            t.text = AegisLocalization.T("“Trí tuệ dẫn lối, Mecha chinh phục.”", "“Wisdom guides, Mecha conquers.”");
            t.font = font;
            t.fontSize = 18;
            t.fontStyle = FontStyle.BoldAndItalic;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.24f, 0.15f, 0.08f, 1f); // Deep antique scribe ink on aged parchment
            MakeTextCrisp(t);

            RectTransform trt = txtObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(68f, 18f);
            trt.offsetMax = new Vector2(-68f, -14f);
        }

        private GameObject CreateHeaderProfileButton(Transform parent, Font font)
        {
            float btnW = 92f;
            float btnH = 92f;

            GameObject btnObj = new GameObject("BtnProfile");
            btnObj.transform.SetParent(parent, false);

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = btnW;
            le.preferredHeight = btnH;

            // 1. Circular Avatar inside
            GameObject avObj = new GameObject("AvatarImg");
            avObj.transform.SetParent(btnObj.transform, false);
            Image avImg = avObj.AddComponent<Image>();
            Sprite avSp = MedievalUIAssetLoader.GetSprite("profile_avatar_round")
                         ?? ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png")
                         ?? ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            if (avSp != null) avImg.sprite = avSp;
            avImg.preserveAspect = true;
            avImg.raycastTarget = false;
            RectTransform avRt = avObj.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0.5f, 0.5f);
            avRt.anchorMax = new Vector2(0.5f, 0.5f);
            avRt.pivot = new Vector2(0.5f, 0.5f);
            avRt.anchoredPosition = Vector2.zero;
            avRt.sizeDelta = new Vector2(72f, 72f);

            // 2. Royal Golden Medallion Frame Overlay
            Image bg = btnObj.AddComponent<Image>();
            Sprite s = MedievalUIAssetLoader.GetSprite("avatar_frame_gold")
                      ?? MedievalUIAssetLoader.GetSprite("avatar_medallion_gold");
            if (s != null)
            {
                bg.sprite = s;
                bg.type = Image.Type.Simple;
                bg.preserveAspect = true;
            }
            else
            {
                bg.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodSmall);
                bg.type = Image.Type.Sliced;
            }
            bg.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                ShowProfileDetailsModal();
            });
            btnObj.AddComponent<MenuButtonEffect>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(btnW, btnH);

            // 3. Level Badge Tag (Bottom right corner)
            GameObject badgeObj = new GameObject("LevelBadge");
            badgeObj.transform.SetParent(btnObj.transform, false);
            Image bImg = badgeObj.AddComponent<Image>();
            bImg.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion)
                         ?? ArcaneAssetLoader.GetGlassBoxSprite(44, 20, 6f, ColGoldCta, ColGoldBorder, 1.2f);
            bImg.type = Image.Type.Sliced;
            bImg.raycastTarget = false;
            RectTransform brt = badgeObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.pivot = new Vector2(1f, 0f);
            brt.anchoredPosition = new Vector2(4f, -4f);
            brt.sizeDelta = new Vector2(48f, 22f);

            GameObject lvlTxtObj = new GameObject("LvlTxt");
            lvlTxtObj.transform.SetParent(badgeObj.transform, false);
            _profileLevelTxt = lvlTxtObj.AddComponent<Text>();
            int curLvl = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentLevel : 1;
            _profileLevelTxt.text = $"Lv.{curLvl}";
            _profileLevelTxt.font = font;
            _profileLevelTxt.fontSize = 12;
            _profileLevelTxt.fontStyle = FontStyle.Bold;
            _profileLevelTxt.alignment = TextAnchor.MiddleCenter;
            _profileLevelTxt.color = new Color(0.18f, 0.11f, 0.04f, 1f); // Crisp dark gold/carved text
            _profileLevelTxt.raycastTarget = false;
            MakeTextCrisp(_profileLevelTxt);
            RectTransform ltrt = lvlTxtObj.GetComponent<RectTransform>();
            ltrt.anchorMin = Vector2.zero;
            ltrt.anchorMax = Vector2.one;
            ltrt.offsetMin = Vector2.zero;
            ltrt.offsetMax = Vector2.zero;
            return btnObj;
        }

        private void BuildTopRightProfileHeader(Transform parent, Font font)
        {
            GameObject toolbarObj = new GameObject("TopRightToolbar");
            _topRightToolbar = toolbarObj;
            toolbarObj.transform.SetParent(parent, false);

            RectTransform tbRt = toolbarObj.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(1f, 1f);
            tbRt.anchorMax = new Vector2(1f, 1f);
            tbRt.pivot = new Vector2(1f, 1f);
            tbRt.anchoredPosition = new Vector2(-45f, -16f);
            tbRt.sizeDelta = new Vector2(1200f, 115f);

            HorizontalLayoutGroup hlg = toolbarObj.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 16f;

            // 0. Player Profile Button (Royal Golden Medallion Avatar with Lv badge) -> opens Profile Modal
            CreateHeaderProfileButton(toolbarObj.transform, font);

            // 1. Gold Currency Display: [🪙 12,560] (Enlarged 300x104 HUD display, not a button)
            CreateCurrencyCapsule(toolbarObj.transform, "GoldCapsule", "🪙", "12,560", MedievalUIAssetLoader.GetSprite("counter_gold_capsule"), font);

            // 2. Gem Currency Display: [💎 320] (Enlarged 300x104 HUD display, not a button)
            CreateCurrencyCapsule(toolbarObj.transform, "GemCapsule", "💎", "320", MedievalUIAssetLoader.GetSprite("counter_gem_capsule"), font);

            // 3. Help Button (?) -> opens Tutorial / How-to-play (Enlarged 90x90)
            CreateHeaderIconButton(toolbarObj.transform, "BtnHelp", "icon_round_help", "?", false, new Vector2(90f, 90f), () =>
            {
                AegisAudioManager.PlayButtonClick();
                OpenTutorialModal();
            }, font);

            // 4. Settings Button (⚙ Gear) -> opens Settings Modal (Enlarged 90x90)
            CreateHeaderIconButton(toolbarObj.transform, "BtnSettings", "icon_round_gear", "⚙", false, new Vector2(90f, 90f), () =>
            {
                AegisAudioManager.PlayButtonClick();
                OpenSettingsModal();
            }, font);


            // 6. Mail Button (✉) -> Royal Mailbox / Inbox Modal (Enlarged 112x90)
            CreateHeaderIconButton(toolbarObj.transform, "BtnMail", "icon_mail_envelope", "✉", true, new Vector2(112f, 90f), () =>
            {
                AegisAudioManager.PlayButtonClick();
                OpenMailboxModal();
            }, font);
        }

        private void CreateCurrencyCapsule(Transform parent, string name, string iconGlyph, string defaultAmount, Sprite capsuleSprite, Font font)
        {
            GameObject pillObj = new GameObject(name);
            pillObj.transform.SetParent(parent, false);

            bool isGem = name.Contains("Gem");
            bool isGold = name.Contains("Gold");

            // Substantially enlarged HUD counter dimensions: 300px width x 104px height (matches ~2.88:1 ratio of the HD art)
            float width = 300f;
            float height = 104f;

            LayoutElement le = pillObj.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            Image bg = pillObj.AddComponent<Image>();
            if (capsuleSprite != null)
            {
                bg.sprite = capsuleSprite;
                bg.type = (isGem || isGold) ? Image.Type.Simple : Image.Type.Sliced;
                bg.preserveAspect = true;
            }
            else
            {
                bg.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold);
                bg.type = Image.Type.Sliced;
            }
            bg.color = Color.white;
            bg.raycastTarget = false; // Pure display HUD, not clickable

            RectTransform rt = pillObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            // If neither Gem nor Gold bar sprite is used, render a fallback left icon
            if (!isGem && !isGold)
            {
                string iconName = "icon_gold_coins";
                Sprite iconSprite = MedievalUIAssetLoader.GetSprite(iconName);
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
                iconObj.transform.SetParent(pillObj.transform, false);
                RectTransform irt = iconObj.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0f, 0.5f);
                irt.anchorMax = new Vector2(0f, 0.5f);
                irt.pivot = new Vector2(0f, 0.5f);
                irt.anchoredPosition = new Vector2(10f, 0f);
                irt.sizeDelta = new Vector2(56f, 56f);

                if (iconSprite != null)
                {
                    Image iconImg = iconObj.AddComponent<Image>();
                    iconImg.sprite = iconSprite;
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;
                }
                else
                {
                    Text itxt = iconObj.AddComponent<Text>();
                    itxt.text = iconGlyph;
                    itxt.font = font;
                    itxt.fontSize = 32;
                    itxt.alignment = TextAnchor.MiddleCenter;
                    itxt.raycastTarget = false;
                }
            }

            // Amount Text (Centered in the carved plate, enlarged and bold)
            GameObject txtObj = new GameObject("Amount", typeof(RectTransform));
            txtObj.transform.SetParent(pillObj.transform, false);
            Text t = txtObj.AddComponent<Text>();
            int realAmount = isGem ? AegisMech.UI.AegisPrefs.Gems : (isGold ? AegisMech.UI.AegisPrefs.Coins : 0);
            t.text = realAmount > 0 ? realAmount.ToString("N0") : defaultAmount;
            t.font = font;
            t.fontSize = 34;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = isGem ? new Color(0.92f, 0.98f, 1.0f, 1f) : new Color(1.0f, 0.96f, 0.78f, 1f);
            t.raycastTarget = false;
            MakeTextCrisp(t);

            if (isGem) _headerGemText = t;
            else if (isGold) _headerGoldText = t;

            RectTransform trt = txtObj.GetComponent<RectTransform>();
            if (isGem || isGold)
            {
                trt.anchorMin = new Vector2(0.5f, 0.5f);
                trt.anchorMax = new Vector2(0.5f, 0.5f);
                trt.pivot = new Vector2(0.5f, 0.5f);
                trt.anchoredPosition = isGold ? new Vector2(19f, -1f) : new Vector2(2.5f, 0f);
                trt.sizeDelta = new Vector2(144f, 60f);
            }
            else
            {
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(65f, 0f);
                trt.offsetMax = new Vector2(-20f, 0f);
            }
            // Pure display HUD counter: No Button component, no clicks, no hover effects.
        }

        private void CreateHeaderIconButton(Transform parent, string name, string spriteName, string fallbackGlyph, bool showRedBadge, Vector2 customSize, Action onClick, Font font)
        {
            float btnW = customSize.x > 0 ? customSize.x : 90f;
            float btnH = customSize.y > 0 ? customSize.y : 90f;

            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = btnW;
            le.preferredHeight = btnH;

            Image bg = btnObj.AddComponent<Image>();
            Sprite s = MedievalUIAssetLoader.GetSprite(spriteName);
            if (s != null)
            {
                bg.sprite = s;
                bg.type = Image.Type.Simple;
                bg.preserveAspect = true;
            }
            else
            {
                bg.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodSmall);
                bg.type = Image.Type.Sliced;

                GameObject txtObj = new GameObject("Glyph", typeof(RectTransform));
                txtObj.transform.SetParent(btnObj.transform, false);
                Text gt = txtObj.AddComponent<Text>();
                gt.text = fallbackGlyph;
                gt.font = font;
                gt.fontSize = 32;
                gt.fontStyle = FontStyle.Bold;
                gt.alignment = TextAnchor.MiddleCenter;
                gt.color = new Color(0.95f, 0.85f, 0.45f);
                RectTransform grt = txtObj.GetComponent<RectTransform>();
                grt.anchorMin = Vector2.zero;
                grt.anchorMax = Vector2.one;
                grt.offsetMin = Vector2.zero;
                grt.offsetMax = Vector2.zero;
            }
            bg.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            btnObj.AddComponent<MenuButtonEffect>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(btnW, btnH);

            if (showRedBadge)
            {
                GameObject badge = new GameObject("RedBadge");
                badge.transform.SetParent(btnObj.transform, false);
                _mailBadgeObj = badge;
                Image bImg = badge.AddComponent<Image>();
                bImg.sprite = ArcaneAssetLoader.GetCircleSprite(32, new Color(0.88f, 0.12f, 0.12f, 1f), Color.white, 2f);
                bImg.raycastTarget = false;
                RectTransform brt = badge.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1f, 1f);
                brt.anchorMax = new Vector2(1f, 1f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2(-2f, -2f);
                brt.sizeDelta = new Vector2(32f, 32f);

                GameObject badgeTxt = new GameObject("1");
                badgeTxt.transform.SetParent(badge.transform, false);
                Text bt = badgeTxt.AddComponent<Text>();
                bt.text = "1";
                bt.font = font;
                bt.fontSize = 18;
                bt.fontStyle = FontStyle.Bold;
                bt.alignment = TextAnchor.MiddleCenter;
                bt.color = Color.white;
                bt.raycastTarget = false;
                MakeTextCrisp(bt);
                RectTransform btrt = badgeTxt.GetComponent<RectTransform>();
                btrt.anchorMin = Vector2.zero;
                btrt.anchorMax = Vector2.one;
                btrt.offsetMin = Vector2.zero;
                btrt.offsetMax = Vector2.zero;
            }
        }

        private void BuildLeftMainMenuNav(Transform parent, Font font)
        {
            _navContainer = new GameObject("LeftNavigationStack");
            _navContainer.transform.SetParent(parent, false);
            RectTransform ncRt = _navContainer.AddComponent<RectTransform>();
            ncRt.anchorMin = new Vector2(0f, 1f);
            ncRt.anchorMax = new Vector2(0f, 1f);
            ncRt.pivot = new Vector2(0f, 1f);
            ncRt.anchoredPosition = new Vector2(-26f, -315f);
            ncRt.sizeDelta = new Vector2(640f, 640f);
            ncRt.localScale = new Vector3(1.24f, 1f, 1f); // Exactly matches the 640px logo width for perfect center alignment

            VerticalLayoutGroup vlg = _navContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter; // Centered directly under the 640px logo axis (X = 370px)
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            string[] navLabels = {
                AegisLocalization.T("Bắt đầu", "Start Game"),
                AegisLocalization.T("Chế độ chơi", "Game Modes"),
                AegisLocalization.T("Cửa hàng", "Shop"),
                AegisLocalization.T("Bộ sưu tập", "Collection"),
                AegisLocalization.T("Thoát", "Exit")
            };
            string[] navSpriteNames = {
                "btn_gold_start",
                "btn_wood_mode",
                "btn_wood_shop",
                "btn_wood_collection",
                "btn_wood_quit"
            };

            for (int i = 0; i < navLabels.Length; i++)
            {
                int index = i;
                bool isPrimary = (i == 0);
                GameObject itemObj = new GameObject("NavItem_" + i);
                itemObj.transform.SetParent(_navContainer.transform, false);

                RectTransform rt = itemObj.AddComponent<RectTransform>();
                rt.pivot = new Vector2(0.5f, 0.5f);
                float itemW = 430f;
                float itemH = isPrimary ? 116f : 104f;
                rt.sizeDelta = new Vector2(itemW, itemH);

                LayoutElement le = itemObj.AddComponent<LayoutElement>();
                le.preferredWidth = itemW;
                le.preferredHeight = itemH;

                Image bg = itemObj.AddComponent<Image>();
                Sprite s = (AegisLocalization.CurrentLanguage == AppLanguage.Vietnamese)
                    ? MedievalUIAssetLoader.GetSprite(navSpriteNames[i])
                    : (isPrimary ? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion) : MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodCarved));

                if (s != null)
                {
                    bg.sprite = s;
                    bool isBaked = (AegisLocalization.CurrentLanguage == AppLanguage.Vietnamese);
                    bg.type = isBaked ? Image.Type.Simple : Image.Type.Sliced;
                    bg.preserveAspect = isBaked;
                }
                else
                {
                    bg.sprite = isPrimary ? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion) : MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodCarved);
                    bg.type = Image.Type.Sliced;
                }
                bg.color = Color.white;

                Button btn = itemObj.AddComponent<Button>();
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => OnNavMenuItemClicked(index));
                itemObj.AddComponent<MenuButtonEffect>();

                // Text overlay (only shown when localized text is different or fallback is needed)
                GameObject txtObj = new GameObject("Label");
                txtObj.transform.SetParent(itemObj.transform, false);
                Text t = txtObj.AddComponent<Text>();
                t.text = navLabels[i];
                t.font = font;
                t.fontSize = isPrimary ? 28 : 22;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleCenter;
                t.raycastTarget = false;
                MakeTextCrisp(t);

                // Hide text overlay if sprite already has the exact baked Vietnamese artwork (except blank plate for Hồ sơ)
                if (s != null && AegisLocalization.CurrentLanguage == AppLanguage.Vietnamese)
                {
                    t.color = new Color(0f, 0f, 0f, 0f);
                }
                else
                {
                    t.color = new Color(0.96f, 0.92f, 0.84f, 1f); // Harmonious ivory-gold on dark wood
                }

                RectTransform trt = txtObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(isPrimary ? 50f : 40f, 0f);
                trt.offsetMax = new Vector2(-20f, 0f);

                _navItemObjs[i] = itemObj;
                _navItemBgs[i] = bg;
                _navItemRts[i] = rt;
                _navItemTexts[i] = t;
                _navItemLayoutElements[i] = le;
            }

            for (int k = navLabels.Length; k < _navItemObjs.Length; k++)
            {
                _navItemObjs[k] = null;
                _navItemBgs[k] = null;
                _navItemRts[k] = null;
                _navItemTexts[k] = null;
                _navItemLayoutElements[k] = null;
            }
        }

        public void UpdateNavSelection(int selectedIndex)
        {
            _selectedMenuNavIndex = selectedIndex;

            for (int i = 0; i < _navItemBgs.Length; i++)
            {
                if (i >= _navItemBgs.Length || _navItemBgs[i] == null) continue;
                bool isActive = (i == selectedIndex);
                if (_navItemTexts[i] != null && _navItemTexts[i].color.a > 0.01f)
                {
                    Color normalColor = new Color(0.96f, 0.92f, 0.84f, 1f);
                    Color activeColor = Color.white;
                    _navItemTexts[i].color = (i == 0) ? new Color(0.18f, 0.11f, 0.04f, 1f) : (isActive ? activeColor : normalColor);
                }
            }
        }

        private void OnNavMenuItemClicked(int index)
        {
            UpdateNavSelection(index);

            switch (index)
            {
                case 0:
                    AegisAudioManager.PlayButtonClick();
                    OpenAIDifficultyModal();
                    break;
                case 1:
                    AegisAudioManager.PlayButtonClick();
                    NavigateToChooseMode();
                    break;
                case 2:
                    AegisAudioManager.PlayButtonClick();
                    OpenShopModal();
                    break;
                case 3:
                    AegisAudioManager.PlayButtonClick();
                    OpenCollectionModal();
                    break;
                case 4:
                    AegisAudioManager.PlayButtonClick();
                    ShowConfirmDialog(
                        AegisLocalization.T("THOÁT TRÒ CHƠI", "EXIT GAME"),
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
            // Floating AR Exploration Banner (Floats neatly above the right gold button)
            GameObject bannerObj = new GameObject("RoyalARFloatingBanner");
            bannerObj.transform.SetParent(parent, false);

            RectTransform bRt = bannerObj.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(1f, 0f);
            bRt.anchorMax = new Vector2(1f, 0f);
            bRt.pivot = new Vector2(1f, 0f);
            bRt.anchoredPosition = new Vector2(-15f, 102f);
            bRt.sizeDelta = new Vector2(306f, 60f);

            Image bg = bannerObj.AddComponent<Image>();
            Sprite bannerSp = ArcaneAssetLoader.LoadArcaneSprite("hud_royal_ar_banner.png");
            if (bannerSp != null)
            {
                bg.sprite = bannerSp;
                bg.preserveAspect = false;
            }
            else
            {
                bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(306, 60, 12f, new Color(0.02f, 0.12f, 0.32f, 0.95f), new Color(0.85f, 0.72f, 0.30f), 2f);
                bg.type = Image.Type.Sliced;
            }

            Button btn = bannerObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                OpenTutorialModal();
            });
            bannerObj.AddComponent<MenuButtonEffect>();
        }

        // =========================================================================
        // FULL-WIDTH MLBB BOTTOM NAVIGATION BAR (EXACT MATCH MEDIA_1792821344323.PNG)
        // [Avatar + Lv. 5 + Exp] | [Cửa Hàng] | [Bộ Sưu Tập] | [Thành Tích] | [Chế Độ] | [♞ CỔ ĐIỂN]
        // =========================================================================
        private void BuildMLBBBottomBar(Transform parent, Font font)
        {
            _mlbbBottomBar = new GameObject("BottomNavigationBar");
            _mlbbBottomBar.transform.SetParent(parent, false);

            RectTransform barRt = _mlbbBottomBar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(1f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(0f, 96f);

            // Full Continuous Dark Wood & Bronze Dock Background with Antique Gold Inlay
            Image barBg = _mlbbBottomBar.AddComponent<Image>();
            Sprite dockBgSp = MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold") ?? ArcaneAssetLoader.LoadArcaneSprite("hud_mlbb_dock_bg.png");
            if (dockBgSp != null)
            {
                barBg.sprite = dockBgSp;
                barBg.type = Image.Type.Sliced;
                barBg.color = Color.white;
            }
            else
            {
                barBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1920, 96, 0f, new Color(0.04f, 0.10f, 0.22f, 0.94f), new Color(0f, 0.85f, 1f, 0.8f), 2f);
                barBg.type = Image.Type.Sliced;
            }

            // ---------------------------------------------------------------------
            // 1. LEFT SECTION: [ Avatar + Lv. 5 + Exp Progress Bar ]
            // ---------------------------------------------------------------------
            GameObject profilePanel = new GameObject("PlayerProfilePanel");
            profilePanel.transform.SetParent(_mlbbBottomBar.transform, false);

            RectTransform profRt = profilePanel.AddComponent<RectTransform>();
            profRt.anchorMin = new Vector2(0f, 0f);
            profRt.anchorMax = new Vector2(0f, 1f);
            profRt.pivot = new Vector2(0f, 0.5f);
            profRt.anchoredPosition = new Vector2(16f, 0f);
            profRt.sizeDelta = new Vector2(185f, 0f);

            // Button collider for profile
            Button profBtn = profilePanel.AddComponent<Button>();
            Image profHit = profilePanel.AddComponent<Image>();
            profHit.color = Color.clear;
            profBtn.targetGraphic = profHit;
            profBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                ShowProfileDetailsModal();
            });
            profilePanel.AddComponent<MenuButtonEffect>();

            // Circular Avatar Ring with Antique Gold Medallion
            GameObject avObj = new GameObject("AvatarRing");
            avObj.transform.SetParent(profilePanel.transform, false);
            RectTransform avRt = avObj.AddComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0f, 0.5f);
            avRt.anchorMax = new Vector2(0f, 0.5f);
            avRt.pivot = new Vector2(0.5f, 0.5f);
            avRt.anchoredPosition = new Vector2(38f, 0f);
            avRt.sizeDelta = new Vector2(64f, 64f);

            Image avRing = avObj.AddComponent<Image>();
            avRing.sprite = MedievalUIAssetLoader.GetSprite("avatar_frame_gold") ?? MedievalUIAssetLoader.GetSprite("avatar_medallion_gold") ?? ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png");
            if (avRing.sprite == null) avRing.color = new Color(0.85f, 0.70f, 0.35f);
            avRing.preserveAspect = true;
            avRing.raycastTarget = false;

            // Level Text: "Lv. 5"
            GameObject lvlObj = new GameObject("LevelText");
            lvlObj.transform.SetParent(profilePanel.transform, false);
            _profileLevelTxt = lvlObj.AddComponent<Text>();
            _profileLevelTxt.text = "Lv. 5";
            _profileLevelTxt.font = font;
            _profileLevelTxt.fontSize = 17;
            _profileLevelTxt.fontStyle = FontStyle.Bold;
            _profileLevelTxt.alignment = TextAnchor.MiddleLeft;
            _profileLevelTxt.color = Color.white;
            _profileLevelTxt.raycastTarget = false;
            RectTransform lvlRt = lvlObj.GetComponent<RectTransform>();
            lvlRt.anchorMin = new Vector2(0f, 0.5f);
            lvlRt.anchorMax = new Vector2(0f, 0.5f);
            lvlRt.pivot = new Vector2(0f, 0.5f);
            lvlRt.anchoredPosition = new Vector2(80f, 13f);
            lvlRt.sizeDelta = new Vector2(90f, 24f);

            // Exp Progress Bar Background
            GameObject expBgObj = new GameObject("ExpBarBg");
            expBgObj.transform.SetParent(profilePanel.transform, false);
            RectTransform expBgRt = expBgObj.AddComponent<RectTransform>();
            expBgRt.anchorMin = new Vector2(0f, 0.5f);
            expBgRt.anchorMax = new Vector2(0f, 0.5f);
            expBgRt.pivot = new Vector2(0f, 0.5f);
            expBgRt.anchoredPosition = new Vector2(80f, -9f);
            expBgRt.sizeDelta = new Vector2(86f, 6f);

            Image expBgImg = expBgObj.AddComponent<Image>();
            expBgImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(86, 6, 3f, new Color(0.04f, 0.10f, 0.22f, 0.90f), new Color(0.15f, 0.35f, 0.60f, 0.5f), 1f);
            expBgImg.type = Image.Type.Sliced;
            expBgImg.raycastTarget = false;

            // Exp Progress Bar Fill
            GameObject expFillObj = new GameObject("ExpBarFill");
            expFillObj.transform.SetParent(expBgObj.transform, false);
            RectTransform expFillRt = expFillObj.AddComponent<RectTransform>();
            expFillRt.anchorMin = new Vector2(0f, 0f);
            expFillRt.anchorMax = new Vector2(0.68f, 1f); // 68% progress
            expFillRt.offsetMin = Vector2.zero;
            expFillRt.offsetMax = Vector2.zero;

            Image expFillImg = expFillObj.AddComponent<Image>();
            expFillImg.color = new Color(0f, 0.90f, 1f, 1f); // Glowing cyan fill
            expFillImg.raycastTarget = false;

            // ---------------------------------------------------------------------
            // 2. MIDDLE SECTION: 3 CORE NAVIGATION SLOTS
            // [ Cửa Hàng ] | [ Bộ Sưu Tập ] | [ Thành Tích ]
            // ---------------------------------------------------------------------
            GameObject middleNav = new GameObject("MiddleNavGroup");
            middleNav.transform.SetParent(_mlbbBottomBar.transform, false);

            RectTransform midRt = middleNav.AddComponent<RectTransform>();
            midRt.anchorMin = new Vector2(0f, 0f);
            midRt.anchorMax = new Vector2(1f, 1f);
            midRt.offsetMin = new Vector2(210f, 0f);
            midRt.offsetMax = new Vector2(-460f, 0f); // Space for HD Hero Gold Play Button

            HorizontalLayoutGroup hlg = middleNav.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 20f;

            // Slot 1: Cửa Hàng (Shop)
            CreateEvenNavSlot(middleNav.transform, "BtnNavShop", "dock_icon_shop.png", "🛒", AegisLocalization.T("Cửa Hàng", "Shop"), () =>
            {
                OpenShopModal();
            }, font);

            // Slot 2: Bộ Sưu Tập (Collection)
            CreateEvenNavSlot(middleNav.transform, "BtnNavCollection", "dock_icon_inventory.png", "🛡️", AegisLocalization.T("Bộ Sưu Tập", "Collection"), () =>
            {
                OpenCollectionModal();
            }, font);

            // Slot 3: Thành Tích (Achievements)
            CreateEvenNavSlot(middleNav.transform, "BtnNavAchievements", "dock_icon_rank.png", "🏆", AegisLocalization.T("Thành Tích", "Achievements"), () =>
            {
                OpenAchievementsModal();
            }, font);

            // ---------------------------------------------------------------------
            // 3. RIGHT SECTION: THE BIG HERO GOLD PLAY BUTTON [ CHƠI / PLAY ]
            // High-definition ornate gold banner with side sapphire crystals (1024x341 HD)
            // ---------------------------------------------------------------------
            GameObject playObj = new GameObject("BtnPlayNowGold");
            playObj.transform.SetParent(_mlbbBottomBar.transform, false);

            RectTransform pRt = playObj.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(1f, 0.5f);
            pRt.anchorMax = new Vector2(1f, 0.5f);
            pRt.pivot = new Vector2(1f, 0.5f);
            pRt.anchoredPosition = new Vector2(-16f, 12f);
            pRt.sizeDelta = new Vector2(430f, 142f);

            Image pBg = playObj.AddComponent<Image>();
            Sprite pSp = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion) ?? ArcaneAssetLoader.LoadArcaneSprite("hud_mlbb_gold_btn.png");
            if (pSp != null)
            {
                pBg.sprite = pSp;
                pBg.type = Image.Type.Sliced;
                pBg.color = Color.white;
            }
            else
            {
                pBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(430, 142, 16f, new Color(0.92f, 0.72f, 0.25f, 0.98f), new Color(1f, 0.9f, 0.5f), 2.5f);
                pBg.type = Image.Type.Sliced;
            }

            Button playBtn = playObj.AddComponent<Button>();
            playBtn.targetGraphic = pBg;
            ColorBlock pcb = playBtn.colors;
            pcb.normalColor = Color.white;
            pcb.highlightedColor = new Color(1.1f, 1.1f, 1.05f, 1f);
            pcb.pressedColor = new Color(0.88f, 0.82f, 0.72f, 1f);
            pcb.fadeDuration = 0.10f;
            playBtn.colors = pcb;

            playBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                NavigateToChooseMode();
            });
            playObj.AddComponent<MenuButtonEffect>();

            // Sharp, localized high-contrast text on gold plate ("Chơi" / "Play")
            GameObject playTxtObj = new GameObject("PlayLabel");
            playTxtObj.transform.SetParent(playObj.transform, false);

            RectTransform ptRt = playTxtObj.AddComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0.5f, 0.5f);
            ptRt.anchorMax = new Vector2(0.5f, 0.5f);
            ptRt.pivot = new Vector2(0.5f, 0.5f);
            ptRt.anchoredPosition = new Vector2(0f, 2f);
            ptRt.sizeDelta = new Vector2(280f, 60f);

            Font activeFont = font ?? GetDefaultFont();
            if (activeFont == null) activeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (activeFont == null) activeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            _btnPlayNowText = playTxtObj.AddComponent<Text>();
            _btnPlayNowText.text = AegisLocalization.T("Chơi", "Play");
            _btnPlayNowText.font = activeFont;
            _btnPlayNowText.fontSize = 38;
            _btnPlayNowText.fontStyle = FontStyle.Bold;
            _btnPlayNowText.alignment = TextAnchor.MiddleCenter;
            _btnPlayNowText.color = new Color(0.18f, 0.12f, 0.06f, 1f); // Dark Scribe Ink on Gold
            _btnPlayNowText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _btnPlayNowText.verticalOverflow = VerticalWrapMode.Overflow;
            _btnPlayNowText.raycastTarget = false;

            Outline ptOutline = playTxtObj.AddComponent<Outline>();
            ptOutline.effectColor = new Color(0.18f, 0.08f, 0.01f, 0.95f);
            ptOutline.effectDistance = new Vector2(2f, -2f);

            Shadow ptShadow = playTxtObj.AddComponent<Shadow>();
            ptShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            ptShadow.effectDistance = new Vector2(2f, -3f);
        }

        private GameObject CreateEvenNavSlot(Transform parent, string name, string spriteName, string fallbackGlyph, string labelText, Action onClick, Font font)
        {
            GameObject slotObj = new GameObject(name);
            slotObj.transform.SetParent(parent, false);

            LayoutElement le = slotObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = 90f;

            Image hitImg = slotObj.AddComponent<Image>();
            hitImg.color = Color.clear;

            Button btn = slotObj.AddComponent<Button>();
            btn.targetGraphic = hitImg;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.clear;
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
            cb.pressedColor = new Color(1f, 1f, 1f, 0.16f);
            cb.selectedColor = Color.clear;
            cb.fadeDuration = 0.12f;
            btn.colors = cb;

            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                onClick?.Invoke();
            });
            slotObj.AddComponent<MenuButtonEffect>();

            // Inner horizontal container (Icon on Left + Label on Right in one row)
            GameObject contentRow = new GameObject("ContentRow");
            contentRow.transform.SetParent(slotObj.transform, false);
            RectTransform crRt = contentRow.AddComponent<RectTransform>();
            crRt.anchorMin = Vector2.zero;
            crRt.anchorMax = Vector2.one;
            crRt.offsetMin = Vector2.zero;
            crRt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = contentRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.spacing = 14f;

            // Icon on Left (Enlarged to 38x38 for prominent clarity)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(contentRow.transform, false);
            RectTransform irt = iconObj.AddComponent<RectTransform>();
            irt.sizeDelta = new Vector2(38f, 38f);

            Sprite spr = !string.IsNullOrEmpty(spriteName) ? ArcaneAssetLoader.LoadArcaneSprite(spriteName) : null;
            if (spr == null)
            {
                if (name.Contains("Shop")) spr = ArcaneAssetLoader.LoadArcaneSprite("dock_icon_shop.png") ?? ArcaneAssetLoader.LoadArcaneSprite("nav_fantasy_shop.png");
                else if (name.Contains("Collection")) spr = ArcaneAssetLoader.LoadArcaneSprite("dock_icon_inventory.png") ?? ArcaneAssetLoader.LoadArcaneSprite("nav_fantasy_collection.png");
                else if (name.Contains("Achievements")) spr = ArcaneAssetLoader.LoadArcaneSprite("dock_icon_rank.png") ?? ArcaneAssetLoader.LoadArcaneSprite("nav_fantasy_achievements.png");
            }

            if (spr != null)
            {
                Image icImg = iconObj.AddComponent<Image>();
                icImg.sprite = spr;
                icImg.preserveAspect = true;
                icImg.raycastTarget = false;
            }
            else
            {
                Text gt = iconObj.AddComponent<Text>();
                gt.text = fallbackGlyph;
                gt.font = font;
                gt.fontSize = 28;
                gt.alignment = TextAnchor.MiddleCenter;
                gt.color = new Color(0.40f, 0.85f, 1f, 0.95f);
                gt.raycastTarget = false;
            }

            // Label Text on Right (Increased font size to 24px bold, with crisp drop shadow and outline)
            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(contentRow.transform, false);
            Text lt = lblObj.AddComponent<Text>();
            lt.text = labelText;
            lt.font = font;
            lt.fontSize = 24;
            lt.fontStyle = FontStyle.Bold;
            lt.alignment = TextAnchor.MiddleLeft;
            lt.color = new Color(0.96f, 0.98f, 1.0f, 1f);
            lt.raycastTarget = false;

            Outline outline = lblObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.01f, 0.05f, 0.14f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Shadow s = lblObj.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.90f);
            s.effectDistance = new Vector2(2f, -2f);

            return slotObj;
        }

        private void CreateSlantedDivider(Transform parent, float? posX = null)
        {
            // Removed to eliminate unwanted blue boxes at the bottom menu
        }

        public void OpenCollectionModal()
        {
            CloseAllModals();
            if (!_isInGameMatch) EnsureBaseScreenActive();
            if (_collectionModal != null)
            {
                _collectionModal.transform.SetAsLastSibling();
                _collectionModal.SetActive(true);
                AegisAudioManager.PlayModalOpen();
                RefreshCollectionUI();
                SwitchCollectionTab(_selectedCollectionTab);
            }
            else
            {
                ShowToast(AegisLocalization.T("Bộ sưu tập quân cờ & Skin huyền thoại", "Legendary Chess Pieces & Skin Collection"), ToastType.Info);
            }
        }

        public void OpenAchievementsModal()
        {
            if (_profileModal != null)
            {
                ShowProfileDetailsModal();
                SwitchProfileTab(1); // Tab 1: Achievements
            }
            else
            {
                OpenSeasonLeaderboardModal();
            }
        }

        // =========================================================================
        // MOBILE LEGENDS STYLE RIGHT FRIENDS DRAWER (IMAGE 3)
        // =========================================================================
        private void BuildRightFriendsDrawer(Transform parent, Font font)
        {
            _rightFriendsDrawer = new GameObject("RightFriendsDrawer");
            _rightFriendsDrawer.transform.SetParent(parent, false);

            _rightFriendsDrawerRt = _rightFriendsDrawer.AddComponent<RectTransform>();
            _rightFriendsDrawerRt.anchorMin = new Vector2(1f, 0.5f);
            _rightFriendsDrawerRt.anchorMax = new Vector2(1f, 0.5f);
            _rightFriendsDrawerRt.pivot = new Vector2(1f, 0.5f);
            _rightFriendsDrawerRt.anchoredPosition = _friendsDrawerOpen ? new Vector2(0f, -10f) : new Vector2(280f, -10f);
            _rightFriendsDrawerRt.sizeDelta = new Vector2(280f, 540f);

            // Translucent Charcoal Glass Background with sleek Gold / Cyan border (Image 3 Style)
            Image drawerBg = _rightFriendsDrawer.AddComponent<Image>();
            drawerBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(280, 540, 20f, new Color(0.04f, 0.08f, 0.16f, 0.94f), new Color(0.725f, 0.604f, 0.380f, 0.60f), 1.6f);
            drawerBg.type = Image.Type.Sliced;

            // Collapsible Toggle Tab on Left Edge of the drawer (with chevron, friends icon, and online status)
            GameObject tabObj = new GameObject("ToggleTab");
            tabObj.transform.SetParent(_rightFriendsDrawer.transform, false);

            RectTransform tabRt = tabObj.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0f, 0.5f);
            tabRt.anchorMax = new Vector2(0f, 0.5f);
            tabRt.pivot = new Vector2(1f, 0.5f);
            tabRt.anchoredPosition = new Vector2(1f, 0f);
            tabRt.sizeDelta = new Vector2(40f, 108f);

            Image tabBg = tabObj.AddComponent<Image>();
            tabBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(40, 108, 16f, new Color(0.06f, 0.12f, 0.22f, 0.96f), ColGoldBorder, 1.8f);
            tabBg.type = Image.Type.Sliced;

            Button tabBtn = tabObj.AddComponent<Button>();
            tabBtn.onClick.AddListener(ToggleFriendsDrawer);
            tabObj.AddComponent<MenuButtonEffect>();

            // 1. Tab Chevron Arrow (glowing cyan vector sprite, rotates on toggle)
            GameObject arrowObj = new GameObject("Chevron");
            arrowObj.transform.SetParent(tabObj.transform, false);
            _friendsToggleArrowRt = arrowObj.AddComponent<RectTransform>();
            _friendsToggleArrowRt.anchorMin = new Vector2(0.5f, 1f);
            _friendsToggleArrowRt.anchorMax = new Vector2(0.5f, 1f);
            _friendsToggleArrowRt.pivot = new Vector2(0.5f, 0.5f);
            _friendsToggleArrowRt.anchoredPosition = new Vector2(0f, -20f);
            _friendsToggleArrowRt.sizeDelta = new Vector2(18f, 18f);
            _friendsToggleArrowRt.localEulerAngles = new Vector3(0f, 0f, _friendsDrawerOpen ? 180f : 0f);

            Sprite chevSp = ArcaneAssetLoader.LoadArcaneSprite("icon_drawer_chevron.png");
            if (chevSp != null)
            {
                Image arrowImg = arrowObj.AddComponent<Image>();
                arrowImg.sprite = chevSp;
                arrowImg.preserveAspect = true;
                arrowImg.raycastTarget = false;
            }
            else
            {
                _friendsToggleArrowTxt = arrowObj.AddComponent<Text>();
                _friendsToggleArrowTxt.text = _friendsDrawerOpen ? ">" : "<";
                _friendsToggleArrowTxt.font = font;
                _friendsToggleArrowTxt.fontSize = 20;
                _friendsToggleArrowTxt.fontStyle = FontStyle.Bold;
                _friendsToggleArrowTxt.alignment = TextAnchor.MiddleCenter;
                _friendsToggleArrowTxt.color = ColCyanGlow;
                _friendsToggleArrowTxt.raycastTarget = false;
            }

            // 2. Tab Friends Icon (glowing cyan / gold 2-avatars silhouette)
            GameObject fIconObj = new GameObject("FriendsIcon");
            fIconObj.transform.SetParent(tabObj.transform, false);
            RectTransform fiRt = fIconObj.AddComponent<RectTransform>();
            fiRt.anchorMin = new Vector2(0.5f, 0.5f);
            fiRt.anchorMax = new Vector2(0.5f, 0.5f);
            fiRt.pivot = new Vector2(0.5f, 0.5f);
            fiRt.anchoredPosition = new Vector2(0f, 4f);
            fiRt.sizeDelta = new Vector2(26f, 26f);

            Sprite fIconSp = ArcaneAssetLoader.LoadArcaneSprite("icon_drawer_friends.png");
            if (fIconSp != null)
            {
                Image fImg = fIconObj.AddComponent<Image>();
                fImg.sprite = fIconSp;
                fImg.preserveAspect = true;
                fImg.raycastTarget = false;
            }
            else
            {
                Text fTxt = fIconObj.AddComponent<Text>();
                fTxt.text = "👥";
                fTxt.font = font;
                fTxt.fontSize = 18;
                fTxt.alignment = TextAnchor.MiddleCenter;
                fTxt.color = ColGoldBorder;
                fTxt.raycastTarget = false;
            }

            // 3. Tab Online Green Pulse Dot
            GameObject dotObj = new GameObject("OnlineDot");
            dotObj.transform.SetParent(tabObj.transform, false);
            RectTransform dotRt = dotObj.AddComponent<RectTransform>();
            dotRt.anchorMin = new Vector2(0.5f, 0f);
            dotRt.anchorMax = new Vector2(0.5f, 0f);
            dotRt.pivot = new Vector2(0.5f, 0.5f);
            dotRt.anchoredPosition = new Vector2(0f, 18f);
            dotRt.sizeDelta = new Vector2(10f, 10f);

            Image dotImg = dotObj.AddComponent<Image>();
            dotImg.sprite = ArcaneAssetLoader.GetCircleSprite(12, new Color(0.18f, 0.95f, 0.40f, 1f), Color.white, 1f);
            dotImg.raycastTarget = false;

            // Drawer Header (Title + Online Count + Add Friend button)
            GameObject headerObj = new GameObject("DrawerHeader");
            headerObj.transform.SetParent(_rightFriendsDrawer.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 1f);
            hRt.anchorMax = new Vector2(0.5f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -115f);
            hRt.sizeDelta = new Vector2(256f, 38f);

            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(headerObj.transform, false);
            _friendsDrawerHeaderTxt = titleObj.AddComponent<Text>();
            _friendsDrawerHeaderTxt.text = AegisLocalization.T("👥 BẠN BÈ (3)", "👥 FRIENDS (3)");
            _friendsDrawerHeaderTxt.font = font;
            _friendsDrawerHeaderTxt.fontSize = 16;
            _friendsDrawerHeaderTxt.fontStyle = FontStyle.Bold;
            _friendsDrawerHeaderTxt.alignment = TextAnchor.MiddleLeft;
            _friendsDrawerHeaderTxt.color = ColGoldBorder;
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(6f, 0f);
            trt.offsetMax = new Vector2(-42f, 0f);

            // Quick Add/Invite Friend button in header
            GameObject addBtnObj = new GameObject("BtnAddFriend");
            addBtnObj.transform.SetParent(headerObj.transform, false);
            Image addBg = addBtnObj.AddComponent<Image>();
            addBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(34, 34, 8f, new Color(0.08f, 0.16f, 0.28f, 0.95f), ColCyanBorder, 1.2f);
            addBg.type = Image.Type.Sliced;
            Button addBtn = addBtnObj.AddComponent<Button>();
            addBtn.onClick.AddListener(ShowFriendModal);
            addBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform addRt = addBtnObj.GetComponent<RectTransform>();
            addRt.anchorMin = new Vector2(1f, 0.5f);
            addRt.anchorMax = new Vector2(1f, 0.5f);
            addRt.pivot = new Vector2(1f, 0.5f);
            addRt.anchoredPosition = new Vector2(-2f, 0f);
            addRt.sizeDelta = new Vector2(34f, 34f);

            GameObject addTxtObj = new GameObject("Plus");
            addTxtObj.transform.SetParent(addBtnObj.transform, false);
            Text at = addTxtObj.AddComponent<Text>();
            at.text = "+";
            at.font = font;
            at.fontSize = 24;
            at.fontStyle = FontStyle.Bold;
            at.alignment = TextAnchor.MiddleCenter;
            at.color = ColCyanGlow;
            at.raycastTarget = false;
            RectTransform atRt = addTxtObj.GetComponent<RectTransform>();
            atRt.anchorMin = Vector2.zero;
            atRt.anchorMax = Vector2.one;
            atRt.offsetMin = Vector2.zero;
            atRt.offsetMax = Vector2.zero;

            // Friends List Scroll/Container
            GameObject containerObj = new GameObject("FriendsListContainer");
            containerObj.transform.SetParent(_rightFriendsDrawer.transform, false);
            _rightFriendsListContainer = containerObj.transform;

            RectTransform cRt = containerObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(0f, -22f);
            cRt.sizeDelta = new Vector2(256f, 450f);

            // Populate initially with online players or realistic companion friends
            UpdateRightFriendsDrawerUI(OnlineMultiplayerManager.Instance?.CachedOnlinePlayers);
        }

        private void ToggleFriendsDrawer()
        {
            _friendsDrawerOpen = !_friendsDrawerOpen;
            AegisAudioManager.PlayButtonClick();

            if (_rightFriendsDrawerRt != null)
            {
                // When open: at edge (0f). When collapsed: shifted right (+280f) leaving only the tab visible
                _rightFriendsDrawerRt.anchoredPosition = _friendsDrawerOpen ? new Vector2(0f, -10f) : new Vector2(280f, -10f);
            }
            if (_friendsToggleArrowRt != null)
            {
                _friendsToggleArrowRt.localEulerAngles = new Vector3(0f, 0f, _friendsDrawerOpen ? 180f : 0f);
            }
            if (_friendsToggleArrowTxt != null)
            {
                _friendsToggleArrowTxt.text = _friendsDrawerOpen ? ">" : "<";
            }
        }

        private void UpdateRightFriendsDrawerUI(List<OnlinePlayerInfo> players)
        {
            if (_rightFriendsListContainer == null) return;
            foreach (var obj in _rightFriendRowObjs) if (obj != null) Destroy(obj);
            _rightFriendRowObjs.Clear();

            string myUsername = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentUsername : "";
            var realPlayers = (players ?? new List<OnlinePlayerInfo>()).FindAll(p => p != null && p.username != myUsername);
            Font font = GetDefaultFont();

            // Structure to hold display friend info
            List<(string name, int elo, string status, string flag, Sprite avatar)> displayFriends = new List<(string, int, string, string, Sprite)>();

            // Add all real online players first
            Sprite roundAv = ArcaneAssetLoader.LoadArcaneSprite("profile_avatar_round.png") ?? ArcaneAssetLoader.LoadArcaneSprite("user_avatar.png");
            Sprite aiAv = ArcaneAssetLoader.LoadArcaneSprite("profile_vs_ai_round.png") ?? ArcaneAssetLoader.LoadArcaneSprite("ai_portrait_full.png");

            foreach (var rp in realPlayers)
            {
                displayFriends.Add((rp.username, rp.elo, AegisLocalization.T("● Trực tuyến", "● Online"), "🇻🇳", roundAv));
            }

            // If fewer than 4 friends online, supplement with realistic practice companions/friends matching Image 3
            if (displayFriends.Count < 4)
            {
                string[] sampleNames = { "DatIT", "Aegis_Vanguard", "Grandmaster_V", "Knight_Solar" };
                int[] sampleElos = { 1520, 1380, 1850, 1260 };
                string[] sampleFlags = { "🇻🇳", "🇰🇷", "🇵🇭", "🇯🇵" };
                string[] sampleStatuses = {
                    AegisLocalization.T("● Đang rảnh", "● Ready"),
                    AegisLocalization.T("⚔️ Đang đấu", "⚔️ In match"),
                    AegisLocalization.T("● Đang rảnh", "● Ready"),
                    AegisLocalization.T("● Đang rảnh", "● Ready")
                };

                for (int i = 0; i < sampleNames.Length && displayFriends.Count < 4; i++)
                {
                    if (!sampleNames[i].Equals(myUsername, StringComparison.OrdinalIgnoreCase))
                    {
                        displayFriends.Add((sampleNames[i], sampleElos[i], sampleStatuses[i], sampleFlags[i], (i % 2 == 0) ? roundAv : aiAv));
                    }
                }
            }

            if (_friendsDrawerHeaderTxt != null)
            {
                _friendsDrawerHeaderTxt.text = AegisLocalization.T($"👥 BẠN BÈ ({displayFriends.Count})", $"👥 FRIENDS ({displayFriends.Count})");
            }

            float cardHeight = 84f;
            float spacing = 8f;

            for (int i = 0; i < displayFriends.Count; i++)
            {
                var f = displayFriends[i];
                string friendName = f.name;
                int friendElo = f.elo;
                string statusText = f.status;
                string flagEmoji = f.flag;
                Sprite friendAvatar = f.avatar;

                GameObject cardObj = new GameObject($"FriendItem_{i}");
                cardObj.transform.SetParent(_rightFriendsListContainer, false);

                RectTransform crt = cardObj.AddComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 1f);
                crt.anchorMax = new Vector2(0.5f, 1f);
                crt.pivot = new Vector2(0.5f, 1f);
                crt.anchoredPosition = new Vector2(0f, -i * (cardHeight + spacing));
                crt.sizeDelta = new Vector2(256f, cardHeight);

                Image bg = cardObj.AddComponent<Image>();
                bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(256, (int)cardHeight, 14f, new Color(0.06f, 0.12f, 0.22f, 0.90f), ColCyanBorder, 1.2f);
                bg.type = Image.Type.Sliced;

                // Click friend card / avatar to view their player profile
                Button cardBtn = cardObj.AddComponent<Button>();
                string capFriendName = friendName;
                int capFriendElo = friendElo;
                Sprite capFriendAv = friendAvatar;
                cardBtn.onClick.AddListener(() =>
                {
                    AegisAudioManager.PlayButtonClick();
                    ShowOtherPlayerProfile(capFriendName, capFriendElo, capFriendAv);
                });
                cardObj.AddComponent<MenuButtonEffect>();

                // 1. Avatar with round frame + Corner Flag (Image 3 Style)
                GameObject avObj = new GameObject("Avatar");
                avObj.transform.SetParent(cardObj.transform, false);
                Image avImg = avObj.AddComponent<Image>();
                if (friendAvatar != null) avImg.sprite = friendAvatar;
                avImg.preserveAspect = true;
                RectTransform avRt = avObj.GetComponent<RectTransform>();
                avRt.anchorMin = new Vector2(0f, 0.5f);
                avRt.anchorMax = new Vector2(0f, 0.5f);
                avRt.pivot = new Vector2(0.5f, 0.5f);
                avRt.anchoredPosition = new Vector2(36f, 0f);
                avRt.sizeDelta = new Vector2(50f, 50f);

                // Golden rank border ring around avatar
                GameObject ringObj = new GameObject("Ring");
                ringObj.transform.SetParent(avObj.transform, false);
                Image ringImg = ringObj.AddComponent<Image>();
                ringImg.sprite = ArcaneAssetLoader.GetCircleSprite(52, Color.clear, ColGoldBorder, 2f);
                ringImg.raycastTarget = false;
                RectTransform ringRt = ringObj.GetComponent<RectTransform>();
                ringRt.anchorMin = Vector2.zero;
                ringRt.anchorMax = Vector2.one;
                ringRt.offsetMin = new Vector2(-2f, -2f);
                ringRt.offsetMax = new Vector2(2f, 2f);

                // Country flag or rank badge at corner
                GameObject flagObj = new GameObject("Flag");
                flagObj.transform.SetParent(cardObj.transform, false);
                Text flTxt = flagObj.AddComponent<Text>();
                flTxt.text = flagEmoji;
                flTxt.font = font;
                flTxt.fontSize = 14;
                flTxt.alignment = TextAnchor.MiddleCenter;
                RectTransform flRt = flagObj.GetComponent<RectTransform>();
                flRt.anchorMin = new Vector2(0f, 0.5f);
                flRt.anchorMax = new Vector2(0f, 0.5f);
                flRt.pivot = new Vector2(0.5f, 0.5f);
                flRt.anchoredPosition = new Vector2(16f, 20f);
                flRt.sizeDelta = new Vector2(22f, 22f);

                // 2. Friend Name & Info (Middle)
                GameObject nameObj = new GameObject("Name");
                nameObj.transform.SetParent(cardObj.transform, false);
                Text nt = nameObj.AddComponent<Text>();
                nt.text = friendName;
                nt.font = font;
                nt.fontSize = 15;
                nt.fontStyle = FontStyle.Bold;
                nt.alignment = TextAnchor.MiddleLeft;
                nt.color = ColTextWhite;
                RectTransform nRt = nameObj.GetComponent<RectTransform>();
                nRt.anchorMin = new Vector2(0f, 0.5f);
                nRt.anchorMax = new Vector2(0f, 0.5f);
                nRt.pivot = new Vector2(0f, 0.5f);
                nRt.anchoredPosition = new Vector2(68f, 16f);
                nRt.sizeDelta = new Vector2(125f, 22f);

                // Rank / Elo subtext
                GameObject eloObj = new GameObject("Elo");
                eloObj.transform.SetParent(cardObj.transform, false);
                Text et = eloObj.AddComponent<Text>();
                et.text = $"🏆 Elo {friendElo}";
                et.font = font;
                et.fontSize = 12;
                et.alignment = TextAnchor.MiddleLeft;
                et.color = ColAntiqueGold;
                RectTransform eRt = eloObj.GetComponent<RectTransform>();
                eRt.anchorMin = new Vector2(0f, 0.5f);
                eRt.anchorMax = new Vector2(0f, 0.5f);
                eRt.pivot = new Vector2(0f, 0.5f);
                eRt.anchoredPosition = new Vector2(68f, -3f);
                eRt.sizeDelta = new Vector2(125f, 18f);

                // Status text (Online green or in match yellow)
                GameObject stObj = new GameObject("Status");
                stObj.transform.SetParent(cardObj.transform, false);
                Text st = stObj.AddComponent<Text>();
                bool isInMatch = statusText.Contains("đấu") || statusText.Contains("match");
                st.text = isInMatch ? $"<color=#EAB308>{statusText}</color>" : $"<color=#22C55E>{statusText}</color>";
                st.font = font;
                st.fontSize = 11;
                st.alignment = TextAnchor.MiddleLeft;
                RectTransform sRt = stObj.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0f, 0.5f);
                sRt.anchorMax = new Vector2(0f, 0.5f);
                sRt.pivot = new Vector2(0f, 0.5f);
                sRt.anchoredPosition = new Vector2(68f, -22f);
                sRt.sizeDelta = new Vector2(125f, 18f);

                // 3. Quick Challenge Duel Button [ ⚔️ ] (Right side)
                GameObject duelBtnObj = new GameObject("BtnDuel");
                duelBtnObj.transform.SetParent(cardObj.transform, false);
                Image dImg = duelBtnObj.AddComponent<Image>();
                dImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(40, 40, 10f, ColGoldCta, ColGoldBorder, 1.2f);
                dImg.type = Image.Type.Sliced;

                Button dBtn = duelBtnObj.AddComponent<Button>();
                dBtn.onClick.AddListener(() =>
                {
                    AegisAudioManager.PlayButtonClick();
                    SendFriendChallenge(friendName);
                    ShowToast(AegisLocalization.T($"Đã gửi lời mời thách đấu tới {friendName}!", $"Sent challenge invite to {friendName}!"), ToastType.Success);
                });
                duelBtnObj.AddComponent<MenuButtonEffect>();

                RectTransform dRt = duelBtnObj.GetComponent<RectTransform>();
                dRt.anchorMin = new Vector2(1f, 0.5f);
                dRt.anchorMax = new Vector2(1f, 0.5f);
                dRt.pivot = new Vector2(0.5f, 0.5f);
                dRt.anchoredPosition = new Vector2(-28f, 0f);
                dRt.sizeDelta = new Vector2(40f, 40f);

                GameObject dtObj = new GameObject("DuelGlyph");
                dtObj.transform.SetParent(duelBtnObj.transform, false);
                Text dt = dtObj.AddComponent<Text>();
                dt.text = "⚔️";
                dt.font = font;
                dt.fontSize = 20;
                dt.alignment = TextAnchor.MiddleCenter;
                dt.color = ColDarkBtnText;
                dt.raycastTarget = false;
                RectTransform dtRt = dtObj.GetComponent<RectTransform>();
                dtRt.anchorMin = Vector2.zero;
                dtRt.anchorMax = Vector2.one;
                dtRt.offsetMin = Vector2.zero;
                dtRt.offsetMax = Vector2.zero;

                _rightFriendRowObjs.Add(cardObj);
            }
        }

        // =========================================================================
        // SCREEN 3: CHỌN CHẾ ĐỘ CHƠI (Mode Select with 4 Feature Cards, User Ref Design)
        // =========================================================================
        private void BuildScreen3ModeSelect(Transform parent, Font font)
        {
            _screenModeSelect = CreateFullscreenPanel(parent, "Screen3_ModeSelect");

            // Atmospheric Royal Citadel Backdrop (fullscreen, rich fantasy medieval ambience)
            Image bgImg = _screenModeSelect.AddComponent<Image>();
            Sprite mainBg = MedievalUIAssetLoader.GetSprite("medieval_main_menu_bg") ?? ArcaneAssetLoader.LoadArcaneSprite("main_menu_bg.png");
            if (mainBg != null)
            {
                bgImg.sprite = mainBg;
                bgImg.color = new Color(0.24f, 0.20f, 0.18f, 0.95f);
            }
            else
            {
                bgImg.color = new Color(0.08f, 0.05f, 0.03f, 1.0f);
            }

            // Screen Header Bar (Spanning width, prominent AAA royal proportions)
            GameObject headerObj = new GameObject("ModeHeader");
            headerObj.transform.SetParent(_screenModeSelect.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0f, 1f);
            hRt.anchorMax = new Vector2(1f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -115f);
            hRt.sizeDelta = new Vector2(-160f, 130f);

            // Back Button [ ‹ ] (Left, 72x72, Royal Round Chevron)
            GameObject backBtnObj = new GameObject("BtnBack");
            backBtnObj.transform.SetParent(headerObj.transform, false);
            Image bbImg = backBtnObj.AddComponent<Image>();
            Sprite backSp = MedievalUIAssetLoader.GetSprite("btn_round_chevron_left")
                          ?? MedievalUIAssetLoader.GetSprite("btn_wood_blank_baroque");
            if (backSp != null)
            {
                bbImg.sprite = backSp;
                bbImg.type = Image.Type.Simple;
                bbImg.preserveAspect = true;
                bbImg.color = Color.white;
            }
            else
            {
                bbImg.color = Color.clear;
            }
            Button bbBtn = backBtnObj.AddComponent<Button>();
            bbBtn.onClick.AddListener(NavigateToHome);
            backBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform bbRt = backBtnObj.GetComponent<RectTransform>();
            bbRt.anchorMin = new Vector2(0f, 0.5f);
            bbRt.anchorMax = new Vector2(0f, 0.5f);
            bbRt.pivot = new Vector2(0f, 0.5f);
            bbRt.anchoredPosition = new Vector2(20f, 0f);
            bbRt.sizeDelta = new Vector2(72f, 72f);

            // Settings Cog Button [ ⚙ ] (Right, 72x72, Royal Round Gear)
            GameObject cogBtnObj = new GameObject("BtnSettings");
            cogBtnObj.transform.SetParent(headerObj.transform, false);
            Image cogImg = cogBtnObj.AddComponent<Image>();
            Sprite setSprite = MedievalUIAssetLoader.GetSprite("icon_round_gear")
                            ?? MedievalUIAssetLoader.GetSprite("btn_wood_blank_baroque");
            if (setSprite != null)
            {
                cogImg.sprite = setSprite;
                cogImg.type = Image.Type.Simple;
                cogImg.preserveAspect = true;
                cogImg.color = Color.white;
            }
            else
            {
                cogImg.color = Color.clear;
            }
            Button cogBtn = cogBtnObj.AddComponent<Button>();
            cogBtn.onClick.AddListener(OpenSettingsModal);
            cogBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform cogRt = cogBtnObj.GetComponent<RectTransform>();
            cogRt.anchorMin = new Vector2(1f, 0.5f);
            cogRt.anchorMax = new Vector2(1f, 0.5f);
            cogRt.pivot = new Vector2(1f, 0.5f);
            cogRt.anchoredPosition = new Vector2(-20f, 0f);
            cogRt.sizeDelta = new Vector2(72f, 72f);

            // Crown Crest above Title (Enlarged to 130x56)
            GameObject crownObj = new GameObject("CrownCrest");
            crownObj.transform.SetParent(headerObj.transform, false);
            Image crownImg = crownObj.AddComponent<Image>();
            Sprite crownSp = MedievalUIAssetLoader.GetSprite("crown_victory_gold");
            if (crownSp != null)
            {
                crownImg.sprite = crownSp;
                crownImg.preserveAspect = true;
                crownImg.color = Color.white;
                crownImg.raycastTarget = false;
            }
            RectTransform crRt = crownObj.GetComponent<RectTransform>();
            crRt.anchorMin = new Vector2(0.5f, 0.5f);
            crRt.anchorMax = new Vector2(0.5f, 0.5f);
            crRt.pivot = new Vector2(0.5f, 0.5f);
            crRt.anchoredPosition = new Vector2(0f, 79f);
            crRt.sizeDelta = new Vector2(130f, 56f);
            crRt.localScale = new Vector3(1.98f, 2.8f, 1f);

            // Title: "CHỌN CHẾ ĐỘ CHƠI" (Enlarged to fontSize 42)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = AegisLocalization.T("CHỌN CHẾ ĐỘ CHƠI", "CHOOSE GAME MODE");
            titleTxt.font = font;
            titleTxt.fontSize = 42;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(1f, 0.90f, 0.62f, 1f); // Radiant Antique Gold
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            titleTxt.raycastTarget = false;
            MakeTextCrisp(titleTxt, new Color(0.18f, 0.10f, 0.04f, 0.95f));
            Shadow tShadow = titleObj.AddComponent<Shadow>();
            tShadow.effectColor = new Color(0.10f, 0.05f, 0.02f, 0.90f);
            tShadow.effectDistance = new Vector2(2f, -2f);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0.5f);
            tRt.anchorMax = new Vector2(0.5f, 0.5f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.anchoredPosition = new Vector2(0f, -6f);
            tRt.sizeDelta = new Vector2(650f, 52f);

            // Ornate Divider Wings under Title (Enlarged to 520x24)
            GameObject divObj = new GameObject("Divider");
            divObj.transform.SetParent(headerObj.transform, false);
            Image divImg = divObj.AddComponent<Image>();
            Sprite divSp = MedievalUIAssetLoader.GetSprite("divider_ornate_gold");
            if (divSp != null)
            {
                divImg.sprite = divSp;
                divImg.preserveAspect = true;
                divImg.color = Color.white;
                divImg.raycastTarget = false;
            }
            RectTransform divRt = divObj.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.5f, 0.5f);
            divRt.anchorMax = new Vector2(0.5f, 0.5f);
            divRt.pivot = new Vector2(0.5f, 0.5f);
            divRt.anchoredPosition = new Vector2(0f, -40f);
            divRt.sizeDelta = new Vector2(520f, 24f);

            // Horizontal Separator Line below Header
            GameObject sepObj = new GameObject("HeaderSeparator");
            sepObj.transform.SetParent(_screenModeSelect.transform, false);
            Image sepImg = sepObj.AddComponent<Image>();
            sepImg.color = new Color(0.72f, 0.55f, 0.28f, 0.35f);
            RectTransform sepRt = sepObj.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0f, 1f);
            sepRt.anchorMax = new Vector2(1f, 1f);
            sepRt.pivot = new Vector2(0.5f, 1f);
            sepRt.anchoredPosition = new Vector2(0f, -267f);
            sepRt.sizeDelta = new Vector2(-160f, 2f);

            // Mode Select Horizontal Scrollable Carousel Container (1700 x 720)
            GameObject scrollObj = new GameObject("ModeScrollView");
            scrollObj.transform.SetParent(_screenModeSelect.transform, false);
            RectTransform svRt = scrollObj.AddComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.5f, 0.5f);
            svRt.anchorMax = new Vector2(0.5f, 0.5f);
            svRt.pivot = new Vector2(0.5f, 0.5f);
            svRt.anchoredPosition = new Vector2(0f, -108f);
            svRt.sizeDelta = new Vector2(1540f, 720f);

            // Transparent RaycastTarget Image for dragging anywhere across the carousel
            Image svImg = scrollObj.AddComponent<Image>();
            svImg.color = Color.clear;
            svImg.raycastTarget = true;

            scrollObj.AddComponent<RectMask2D>();

            _modeScrollRect = scrollObj.AddComponent<ScrollRect>();
            _modeScrollRect.horizontal = true;
            _modeScrollRect.vertical = false;
            _modeScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _modeScrollRect.elasticity = 0.12f;
            _modeScrollRect.inertia = true;
            _modeScrollRect.decelerationRate = 0.135f;
            _modeScrollRect.scrollSensitivity = 35f;

            // Content Container (5 cards: 5 * 312 + 4 * 24 = 1656px, centered with 22px padding on each side)
            GameObject contentObj = new GameObject("ScrollContent");
            contentObj.transform.SetParent(scrollObj.transform, false);
            _modeScrollContent = contentObj.AddComponent<RectTransform>();
            _modeScrollContent.anchorMin = new Vector2(0f, 0.5f);
            _modeScrollContent.anchorMax = new Vector2(0f, 0.5f);
            _modeScrollContent.pivot = new Vector2(0f, 0.5f);
            _modeScrollContent.anchoredPosition = Vector2.zero;

            float cardW = 480f;
            float cardGap = 36f;
            float totalCardsW = 5 * cardW + 4 * cardGap; // 2544px (Exactly 3 cards fit in 1540px viewport)
            float padLeft = 14f;
            _modeScrollContent.sizeDelta = new Vector2(totalCardsW + padLeft * 2f, 720f);

            _modeScrollRect.content = _modeScrollContent;
            _modeScrollRect.viewport = svRt;

            // Card 1: Đấu với máy (Index 0)
            CreateModeFeatureCard(0, contentObj.transform, "CardAI",
                AegisLocalization.T("Đấu với máy", "Vs Computer"),
                AegisLocalization.T("Chọn độ khó và bắt đầu\nthử thách AI", "Choose difficulty & challenge\nintelligent AI"),
                "mode_card_ai.png", new Vector2(padLeft + 0 * (cardW + cardGap), 0f), OpenAIDifficultyModal, font);

            // Card 2: Đấu Online (Index 1) - Bấm là tìm trận đánh luôn!
            CreateModeFeatureCard(1, contentObj.transform, "CardOnline",
                AegisLocalization.T("Đấu Online", "Online Match"),
                AegisLocalization.T("Ghép đấu xếp hạng ELO\nvà tìm đối thủ trực tuyến", "Ranked matchmaking\nand online duels"),
                "mode_card_online.png", new Vector2(padLeft + 1 * (cardW + cardGap), 0f), () => RequireLogin(() => ShowMatchmakingModal()), font);

            // Card 3: Hai người (Index 2)
            CreateModeFeatureCard(2, contentObj.transform, "Card2P",
                AegisLocalization.T("Hai người", "Pass & Play"),
                AegisLocalization.T("Chơi cùng bạn bè\ntrên cùng thiết bị", "Play with friends\non the same device"),
                "mode_card_2p.png", new Vector2(padLeft + 2 * (cardW + cardGap), 0f), () => LaunchGame(GameLaunchMode.LocalPassAndPlay), font);

            // Card 4: AR (Index 3)
            CreateModeFeatureCard(3, contentObj.transform, "CardAR",
                AegisLocalization.T("Thực tế ảo AR", "AR Mode"),
                AegisLocalization.T("Đặt bàn cờ lên mặt bàn thật\nqua camera không gian", "Place board on tabletop\nvia spatial camera tracking"),
                "mode_card_ar.png", new Vector2(padLeft + 3 * (cardW + cardGap), 0f), () => LaunchGame(GameLaunchMode.AugmentedReality), font);

            // Card 5: Học cờ & Puzzle (Index 4)
            CreateModeFeatureCard(4, contentObj.transform, "CardLearn",
                AegisLocalization.T("Học cờ & Puzzle", "Learn & Puzzles"),
                AegisLocalization.T("Giải thế cờ kinh điển\nvà nâng cao chiến thuật", "Solve classic chess puzzles\nand master tactics"),
                "mode_card_learn.png", new Vector2(padLeft + 4 * (cardW + cardGap), 0f), OpenLearnScreen, font);

            // Navigation Left Chevron [ ◀ ] - Large Ornate Bronze/Gold Disc
            _modeChevronLeft = new GameObject("ModeChevronLeft");
            _modeChevronLeft.transform.SetParent(_screenModeSelect.transform, false);
            Image lChvImg = _modeChevronLeft.AddComponent<Image>();
            lChvImg.sprite = MedievalUIAssetLoader.GetSprite("btn_round_chevron_left");
            lChvImg.type = Image.Type.Simple;
            lChvImg.preserveAspect = true;
            Button lChvBtn = _modeChevronLeft.AddComponent<Button>();
            lChvBtn.onClick.AddListener(() => SmoothScrollModeSelectDelta(-0.50f));
            _modeChevronLeft.AddComponent<MenuButtonEffect>();
            RectTransform lChvRt = _modeChevronLeft.GetComponent<RectTransform>();
            lChvRt.anchorMin = new Vector2(0.5f, 0.5f);
            lChvRt.anchorMax = new Vector2(0.5f, 0.5f);
            lChvRt.pivot = new Vector2(0.5f, 0.5f);
            lChvRt.anchoredPosition = new Vector2(-825f, -22f);
            lChvRt.sizeDelta = new Vector2(76f, 76f);
            _modeChevronLeft.transform.SetAsLastSibling();

            // Navigation Right Chevron [ ▶ ] - Large Ornate Bronze/Gold Disc
            _modeChevronRight = new GameObject("ModeChevronRight");
            _modeChevronRight.transform.SetParent(_screenModeSelect.transform, false);
            Image rChvImg = _modeChevronRight.AddComponent<Image>();
            rChvImg.sprite = MedievalUIAssetLoader.GetSprite("btn_round_chevron_right");
            rChvImg.type = Image.Type.Simple;
            rChvImg.preserveAspect = true;
            Button rChvBtn = _modeChevronRight.AddComponent<Button>();
            rChvBtn.onClick.AddListener(() => SmoothScrollModeSelectDelta(0.50f));
            _modeChevronRight.AddComponent<MenuButtonEffect>();
            RectTransform rChvRt = _modeChevronRight.GetComponent<RectTransform>();
            rChvRt.anchorMin = new Vector2(0.5f, 0.5f);
            rChvRt.anchorMax = new Vector2(0.5f, 0.5f);
            rChvRt.pivot = new Vector2(0.5f, 0.5f);
            rChvRt.anchoredPosition = new Vector2(825f, -22f);
            rChvRt.sizeDelta = new Vector2(76f, 76f);
            _modeChevronRight.transform.SetAsLastSibling();

            // Bottom Pagination Indicator (Dots Container - hidden when 5 cards fit)
            GameObject dotsObj = new GameObject("PaginationDots");
            dotsObj.transform.SetParent(_screenModeSelect.transform, false);
            RectTransform dotsRt = dotsObj.AddComponent<RectTransform>();
            dotsRt.anchorMin = new Vector2(0.5f, 0f);
            dotsRt.anchorMax = new Vector2(0.5f, 0f);
            dotsRt.pivot = new Vector2(0.5f, 0f);
            dotsRt.anchoredPosition = new Vector2(0f, 62f);
            dotsRt.sizeDelta = new Vector2(100f, 20f);

            // Dot 0
            GameObject dot0Obj = new GameObject("Dot0");
            dot0Obj.transform.SetParent(dotsObj.transform, false);
            _modePageDot0 = dot0Obj.AddComponent<Image>();
            _modePageDot0.color = new Color(0.92f, 0.80f, 0.45f, 1f);
            Button d0Btn = dot0Obj.AddComponent<Button>();
            d0Btn.onClick.AddListener(() => SmoothScrollModeSelect(0f));
            RectTransform d0Rt = dot0Obj.GetComponent<RectTransform>();
            d0Rt.anchorMin = new Vector2(0.5f, 0.5f);
            d0Rt.anchorMax = new Vector2(0.5f, 0.5f);
            d0Rt.pivot = new Vector2(0.5f, 0.5f);
            d0Rt.anchoredPosition = new Vector2(-18f, 0f);
            d0Rt.sizeDelta = new Vector2(32f, 8f);

            // Dot 1
            GameObject dot1Obj = new GameObject("Dot1");
            dot1Obj.transform.SetParent(dotsObj.transform, false);
            _modePageDot1 = dot1Obj.AddComponent<Image>();
            _modePageDot1.color = new Color(0.35f, 0.28f, 0.20f, 0.65f);
            Button d1Btn = dot1Obj.AddComponent<Button>();
            d1Btn.onClick.AddListener(() => SmoothScrollModeSelect(1f));
            RectTransform d1Rt = dot1Obj.GetComponent<RectTransform>();
            d1Rt.anchorMin = new Vector2(0.5f, 0.5f);
            d1Rt.anchorMax = new Vector2(0.5f, 0.5f);
            d1Rt.pivot = new Vector2(0.5f, 0.5f);
            d1Rt.anchoredPosition = new Vector2(18f, 0f);
            d1Rt.sizeDelta = new Vector2(12f, 8f);

            dotsObj.SetActive(false);

            // Bottom Royal Quote Banner
            GameObject quoteObj = new GameObject("ModeQuoteBanner");
            quoteObj.transform.SetParent(_screenModeSelect.transform, false);
            Text qTxt = quoteObj.AddComponent<Text>();
            qTxt.text = AegisLocalization.T("“Trí tuệ dẫn lối, Mecha chinh phục. Hãy chọn đấu trường của bạn!”", "\"Wisdom guides, Mecha conquers. Choose your arena!\"");
            qTxt.font = font;
            qTxt.fontSize = 17;
            qTxt.fontStyle = FontStyle.Italic;
            qTxt.alignment = TextAnchor.MiddleCenter;
            qTxt.color = new Color(0.85f, 0.72f, 0.52f, 0.85f);
            MakeTextCrisp(qTxt);
            RectTransform qRt = quoteObj.GetComponent<RectTransform>();
            qRt.anchorMin = new Vector2(0.5f, 0f);
            qRt.anchorMax = new Vector2(0.5f, 0f);
            qRt.pivot = new Vector2(0.5f, 0f);
            qRt.anchoredPosition = new Vector2(0f, 24f);
            qRt.sizeDelta = new Vector2(800f, 32f);
        }

        public void SmoothScrollModeSelect(float targetPos)
        {
            if (_modeScrollCoroutine != null) StopCoroutine(_modeScrollCoroutine);
            _modeScrollCoroutine = StartCoroutine(DoSmoothScrollModeSelect(targetPos));
        }

        public void SmoothScrollModeSelectDelta(float delta)
        {
            if (_modeScrollRect == null) return;
            float current = _modeScrollRect.horizontalNormalizedPosition;
            SmoothScrollModeSelect(Mathf.Clamp01(current + delta));
        }

        private IEnumerator DoSmoothScrollModeSelect(float targetPos)
        {
            if (_modeScrollRect == null) yield break;
            AegisAudioManager.PlayTabSwitch();
            float startPos = _modeScrollRect.horizontalNormalizedPosition;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic
                _modeScrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPos, targetPos, ease);
                yield return null;
            }
            _modeScrollRect.horizontalNormalizedPosition = targetPos;
            _modeScrollCoroutine = null;
        }

        private void CreateModeFeatureCard(int index, Transform parent, string name, string title, string desc, string textureName, Vector2 pos, Action onClick, Font font)
        {
            GameObject cardObj = new GameObject(name);
            cardObj.transform.SetParent(parent, false);

            Image cardBg = cardObj.AddComponent<Image>();
            bool isSelected = (index == _selectedModeCardIndex);
            Sprite frameSprite = isSelected
                ? MedievalUIAssetLoader.GetSprite("frame_mode_card_royal_active")
                : MedievalUIAssetLoader.GetSprite("frame_mode_card_royal");
            if (frameSprite != null)
            {
                cardBg.sprite = frameSprite;
                cardBg.type = Image.Type.Simple;
                cardBg.color = Color.white;
            }
            else
            {
                cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(400, 660, 20f, new Color(0.12f, 0.08f, 0.05f, 0.95f), isSelected ? new Color(1f, 0.85f, 0.40f) : ColGoldBorder, isSelected ? 3f : 1.5f);
                cardBg.type = Image.Type.Sliced;
            }
            if (index >= 0 && index < _modeCardFrameBgs.Length)
            {
                _modeCardFrameBgs[index] = cardBg;
            }

            Button btn = cardObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = cardBg;
            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                SelectModeCard(index, onClick);
            });
            cardObj.AddComponent<MenuButtonEffect>();

            var clickFilter = cardObj.AddComponent<ModeCardClickFilter>();
            clickFilter.onClick = () =>
            {
                AegisAudioManager.PlayButtonClick();
                SelectModeCard(index, onClick);
            };

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(480f, 680f);

            // Card Artwork Image - fills upper portion of card (430 x 424)
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
                artImg.color = new Color(0.1f, 0.15f, 0.25f, 0.9f);
            }
            artImg.raycastTarget = false;
            RectTransform artRt = artObj.GetComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0.5f, 1f);
            artRt.anchorMax = new Vector2(0.5f, 1f);
            artRt.pivot = new Vector2(0.5f, 1f);
            artRt.anchoredPosition = new Vector2(0f, -24f);
            artRt.sizeDelta = new Vector2(430f, 424f);

            // Plaque Separator Line
            GameObject sepLine = new GameObject("PlaqueSep");
            sepLine.transform.SetParent(cardObj.transform, false);
            Image slImg = sepLine.AddComponent<Image>();
            slImg.color = new Color(0.78f, 0.62f, 0.32f, 0.65f);
            slImg.raycastTarget = false;
            RectTransform slRt = sepLine.GetComponent<RectTransform>();
            slRt.anchorMin = new Vector2(0.5f, 1f);
            slRt.anchorMax = new Vector2(0.5f, 1f);
            slRt.pivot = new Vector2(0.5f, 1f);
            slRt.anchoredPosition = new Vector2(0f, -458f);
            slRt.sizeDelta = new Vector2(420f, 2f);

            // Title (Large, Bold & Crisp, Royal Antique Dark Ink on Parchment)
            GameObject titleObj = new GameObject("CardTitle");
            titleObj.transform.SetParent(cardObj.transform, false);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = title;
            titleTxt.font = font;
            titleTxt.fontSize = 34; // Enlarged from 26 to 34 for prominent readability
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.20f, 0.10f, 0.04f, 1f); // Deep Royal Dark Walnut Ink
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            titleTxt.raycastTarget = false;
            MakeTextCrisp(titleTxt, new Color(1f, 0.88f, 0.55f, 0.70f));
            Shadow tShadow = titleObj.AddComponent<Shadow>();
            tShadow.effectColor = new Color(0.12f, 0.08f, 0.04f, 0.35f);
            tShadow.effectDistance = new Vector2(1f, -1f);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0f);
            tRt.anchorMax = new Vector2(0.5f, 0f);
            tRt.pivot = new Vector2(0.5f, 0f);
            tRt.anchoredPosition = new Vector2(0f, 150f);
            tRt.sizeDelta = new Vector2(440f, 48f);

            // Description (Balanced in center of parchment plaque, clear from bottom corner filigree)
            GameObject descObj = new GameObject("CardDesc");
            descObj.transform.SetParent(cardObj.transform, false);
            Text descTxt = descObj.AddComponent<Text>();
            descTxt.text = desc;
            descTxt.font = font;
            descTxt.fontSize = 21; // Enlarged from 17 to 21 for crystal clear reading
            descTxt.fontStyle = FontStyle.Bold;
            descTxt.lineSpacing = 1.20f;
            descTxt.alignment = TextAnchor.MiddleCenter;
            descTxt.color = new Color(0.35f, 0.22f, 0.10f, 1f); // Deep Medieval Walnut
            descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            descTxt.verticalOverflow = VerticalWrapMode.Overflow;
            descTxt.raycastTarget = false;
            MakeTextCrisp(descTxt, new Color(1f, 0.92f, 0.70f, 0.50f));
            Shadow dShadow = descObj.AddComponent<Shadow>();
            dShadow.effectColor = new Color(0.12f, 0.08f, 0.04f, 0.25f);
            dShadow.effectDistance = new Vector2(1f, -1f);
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0f);
            dRt.anchorMax = new Vector2(0.5f, 0f);
            dRt.pivot = new Vector2(0.5f, 0f);
            dRt.anchoredPosition = new Vector2(0f, 68f);
            dRt.sizeDelta = new Vector2(440f, 72f);
        }

        private void SelectModeCard(int index, Action onAction)
        {
            _selectedModeCardIndex = index;
            Sprite activeSprite = MedievalUIAssetLoader.GetSprite("frame_mode_card_royal_active");
            Sprite inactiveSprite = MedievalUIAssetLoader.GetSprite("frame_mode_card_royal");

            for (int i = 0; i < _modeCardFrameBgs.Length; i++)
            {
                if (_modeCardFrameBgs[i] != null)
                {
                    if (activeSprite != null && inactiveSprite != null)
                    {
                        _modeCardFrameBgs[i].sprite = (i == index) ? activeSprite : inactiveSprite;
                        _modeCardFrameBgs[i].type = Image.Type.Simple;
                    }
                }
            }

            onAction?.Invoke();
        }


        // =========================================================================
        // SCREEN 4: CHỌN ĐỘ KHÓ AI (Royal Medieval AI Difficulty & Config Modal)
        // =========================================================================
        private void BuildScreen4AIDifficulty(Transform parent, Font font)
        {
            _aiDifficultyModal = CreateModalOverlay(parent, "Screen4_AIDifficultyModal");

            GameObject cardObj = new GameObject("AIDifficultyCard");
            cardObj.transform.SetParent(_aiDifficultyModal.transform, false);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned)
                          ?? MedievalUIAssetLoader.GetSprite("frame_shop_collection_1520")
                          ?? ArcaneAssetLoader.GetGlassBoxSprite(1260, 700, 28f, ColGlassFill, ColGoldBorder, 2f);
            cardBg.type = Image.Type.Sliced;
            cardBg.color = Color.white;

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1260f, 700f);

            // ---------------------------------------------------------------------
            // 1. Centered Royal Title Banner & Prominent Close Button
            // (Safely centered below the Royal Crown arch, with large crisp [X] button)
            // ---------------------------------------------------------------------
            // Prominent Royal Close Button [ Crossed Swords Gold ] (Positioned as designed in Scene)
            CreateRoyalCloseButton(cardObj.transform, new Vector2(-41f, -115f), () =>
            {
                SetAITimeDropdownOpen(false);
                _aiDifficultyModal.SetActive(false);
            }, 74f, "BtnAIDiffClose");

            // ---------------------------------------------------------------------
            // 2. Left Column: Paladin Portrait in Golden Royal Frame
            // ---------------------------------------------------------------------
            GameObject portCard = new GameObject("PaladinPortraitCard");
            portCard.transform.SetParent(cardObj.transform, false);
            RectTransform pCardRt = portCard.AddComponent<RectTransform>();
            pCardRt.anchorMin = new Vector2(0f, 0.5f);
            pCardRt.anchorMax = new Vector2(0f, 0.5f);
            pCardRt.pivot = new Vector2(0f, 0.5f);
            pCardRt.anchoredPosition = new Vector2(65f, -38f);
            pCardRt.sizeDelta = new Vector2(280f, 480f);

            // Royal Frame around Portrait
            GameObject portFrameObj = new GameObject("PortraitFrame");
            portFrameObj.transform.SetParent(portCard.transform, false);
            Image pfImg = portFrameObj.AddComponent<Image>();
            pfImg.sprite = MedievalUIAssetLoader.GetSprite("frame_mode_card_royal");
            pfImg.type = Image.Type.Sliced;
            pfImg.color = Color.white;
            RectTransform pfRt = portFrameObj.GetComponent<RectTransform>();
            pfRt.anchorMin = new Vector2(0.5f, 1f);
            pfRt.anchorMax = new Vector2(0.5f, 1f);
            pfRt.pivot = new Vector2(0.5f, 1f);
            pfRt.anchoredPosition = Vector2.zero;
            pfRt.sizeDelta = new Vector2(276f, 396f);

            // Paladin Portrait Artwork
            GameObject artObj = new GameObject("Artwork");
            artObj.transform.SetParent(portFrameObj.transform, false);
            Image artImg = artObj.AddComponent<Image>();
            Sprite portSprite = ArcaneAssetLoader.LoadArcaneSprite("ai_portrait_full.png");
            if (portSprite != null) artImg.sprite = portSprite;
            else artImg.color = new Color(0.1f, 0.2f, 0.35f);
            artImg.preserveAspect = true;
            artImg.raycastTarget = false;
            RectTransform artRt = artObj.GetComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0.5f, 0.5f);
            artRt.anchorMax = new Vector2(0.5f, 0.5f);
            artRt.pivot = new Vector2(0.5f, 0.5f);
            artRt.anchoredPosition = Vector2.zero;
            artRt.sizeDelta = new Vector2(252f, 372f);

            // Paladin Name & Description below Frame
            GameObject pNameObj = new GameObject("PaladinTitle");
            pNameObj.transform.SetParent(portCard.transform, false);
            Text pNameTxt = pNameObj.AddComponent<Text>();
            pNameTxt.text = AegisLocalization.T("HIỆP SĨ PALADIN", "ROYAL PALADIN");
            pNameTxt.font = font;
            pNameTxt.fontSize = 20;
            pNameTxt.fontStyle = FontStyle.Bold;
            pNameTxt.alignment = TextAnchor.MiddleCenter;
            pNameTxt.color = new Color(0.22f, 0.12f, 0.04f, 1f);
            pNameTxt.raycastTarget = false;
            MakeTextCrisp(pNameTxt, new Color(1f, 0.88f, 0.55f, 0.6f));
            RectTransform pnRt = pNameObj.GetComponent<RectTransform>();
            pnRt.anchorMin = new Vector2(0.5f, 0f);
            pnRt.anchorMax = new Vector2(0.5f, 0f);
            pnRt.pivot = new Vector2(0.5f, 0f);
            pnRt.anchoredPosition = new Vector2(0f, 44f);
            pnRt.sizeDelta = new Vector2(280f, 28f);

            GameObject pSubObj = new GameObject("PaladinSub");
            pSubObj.transform.SetParent(portCard.transform, false);
            Text pSubTxt = pSubObj.AddComponent<Text>();
            pSubTxt.text = AegisLocalization.T("Trí tuệ nhân tạo Aegis", "Aegis Chess AI Engine");
            pSubTxt.font = font;
            pSubTxt.fontSize = 14;
            pSubTxt.fontStyle = FontStyle.Italic;
            pSubTxt.alignment = TextAnchor.MiddleCenter;
            pSubTxt.color = new Color(0.48f, 0.35f, 0.20f, 1f);
            pSubTxt.raycastTarget = false;
            MakeTextCrisp(pSubTxt);
            RectTransform psRt = pSubObj.GetComponent<RectTransform>();
            psRt.anchorMin = new Vector2(0.5f, 0f);
            psRt.anchorMax = new Vector2(0.5f, 0f);
            psRt.pivot = new Vector2(0.5f, 0f);
            psRt.anchoredPosition = new Vector2(0f, 18f);
            psRt.sizeDelta = new Vector2(280f, 22f);

            // ---------------------------------------------------------------------
            // 3. Center Column: 4 Centered Difficulty Buttons
            // ---------------------------------------------------------------------
            GameObject diffCol = new GameObject("DifficultyColumn");
            diffCol.transform.SetParent(cardObj.transform, false);
            RectTransform dcRt = diffCol.AddComponent<RectTransform>();
            dcRt.anchorMin = new Vector2(0.5f, 0.5f);
            dcRt.anchorMax = new Vector2(0.5f, 0.5f);
            dcRt.pivot = new Vector2(0.5f, 0.5f);
            dcRt.anchoredPosition = new Vector2(-15f, -38f);
            dcRt.sizeDelta = new Vector2(380f, 480f);

            _aiDiffCardBgs = new Image[4];
            _aiDiffCardTitles = new Text[4];
            _aiDiffCardElos = new Text[4];

            string[] diffTitles = {
                AegisLocalization.T("DỄ", "EASY"),
                AegisLocalization.T("TRUNG BÌNH", "MEDIUM"),
                AegisLocalization.T("KHÓ", "HARD"),
                AegisLocalization.T("RẤT KHÓ", "EXPERT")
            };
            string[] diffElos = { "Elo ~ 800", "Elo ~ 1200", "Elo ~ 1600", "Elo ~ 2000" };
            float diffStartY = 145f;
            float diffStepY = 100f;

            for (int i = 0; i < 4; i++)
            {
                int levelIndex = i + 1;
                int idx = i;
                GameObject btnObj = new GameObject($"BtnDiff_{levelIndex}");
                btnObj.transform.SetParent(diffCol.transform, false);

                Image bg = btnObj.AddComponent<Image>();
                _aiDiffCardBgs[idx] = bg;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;

                Button btn = btnObj.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    AegisAudioManager.PlayButtonClick();
                    SelectAIDifficultyLevel(levelIndex);
                });
                btnObj.AddComponent<MenuButtonEffect>();

                RectTransform bRtCard = btnObj.GetComponent<RectTransform>();
                bRtCard.anchorMin = new Vector2(0.5f, 0.5f);
                bRtCard.anchorMax = new Vector2(0.5f, 0.5f);
                bRtCard.pivot = new Vector2(0.5f, 0.5f);
                bRtCard.anchoredPosition = new Vector2(0f, diffStartY - i * diffStepY);
                bRtCard.sizeDelta = new Vector2(366f, 88f);

                // Title centered horizontally in upper half of button
                GameObject tObj = new GameObject("Title");
                tObj.transform.SetParent(btnObj.transform, false);
                Text t = tObj.AddComponent<Text>();
                t.text = diffTitles[i];
                t.font = font;
                t.fontSize = 21;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleCenter;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.raycastTarget = false;
                MakeTextCrisp(t);
                _aiDiffCardTitles[idx] = t;
                RectTransform trt = tObj.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.5f, 0.5f);
                trt.anchorMax = new Vector2(0.5f, 0.5f);
                trt.pivot = new Vector2(0.5f, 0.5f);
                trt.anchoredPosition = new Vector2(0f, 13f);
                trt.sizeDelta = new Vector2(340f, 28f);

                // Elo centered horizontally in lower half of button
                GameObject eloObj = new GameObject("Elo");
                eloObj.transform.SetParent(btnObj.transform, false);
                Text eloTxt = eloObj.AddComponent<Text>();
                eloTxt.text = diffElos[i];
                eloTxt.font = font;
                eloTxt.fontSize = 15;
                eloTxt.fontStyle = FontStyle.Bold;
                eloTxt.alignment = TextAnchor.MiddleCenter;
                eloTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                eloTxt.verticalOverflow = VerticalWrapMode.Overflow;
                eloTxt.raycastTarget = false;
                MakeTextCrisp(eloTxt);
                _aiDiffCardElos[idx] = eloTxt;
                RectTransform elort = eloObj.GetComponent<RectTransform>();
                elort.anchorMin = new Vector2(0.5f, 0.5f);
                elort.anchorMax = new Vector2(0.5f, 0.5f);
                elort.pivot = new Vector2(0.5f, 0.5f);
                elort.anchoredPosition = new Vector2(0f, -14f);
                elort.sizeDelta = new Vector2(340f, 22f);
            }
            RefreshAIDifficultyVisuals();

            // ---------------------------------------------------------------------
            // 4. Right Column: Time Dropdown + Side Selector Cards + Royal Start CTA
            // ---------------------------------------------------------------------
            GameObject rightCol = new GameObject("RightConfigColumn");
            rightCol.transform.SetParent(cardObj.transform, false);
            RectTransform rcRt = rightCol.AddComponent<RectTransform>();
            rcRt.anchorMin = new Vector2(1f, 0.5f);
            rcRt.anchorMax = new Vector2(1f, 0.5f);
            rcRt.pivot = new Vector2(1f, 0.5f);
            rcRt.anchoredPosition = new Vector2(-65f, -38f);
            rcRt.sizeDelta = new Vector2(350f, 480f);

            // --- Section 1: Time Control ---
            CreateSectionHeaderLabel(rightCol.transform, AegisLocalization.T("THỜI GIAN VÁN ĐẤU", "MATCH TIME CONTROL"), new Vector2(0f, 175f), font);

            GameObject timeBtnObj = new GameObject("TimeDropdownBtn");
            timeBtnObj.transform.SetParent(rightCol.transform, false);
            Image timeBtnBg = timeBtnObj.AddComponent<Image>();
            timeBtnBg.sprite = MedievalUIAssetLoader.GetSprite("dropdown_parchment_box")
                           ?? MedievalUIAssetLoader.GetSprite("parchment_box_clean");
            timeBtnBg.type = Image.Type.Sliced;
            timeBtnBg.color = Color.white;
            Button timeBtn = timeBtnObj.AddComponent<Button>();
            timeBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                ToggleAITimeDropdown();
            });
            timeBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform tbRt = timeBtnObj.GetComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.5f, 0.5f);
            tbRt.anchorMax = new Vector2(0.5f, 0.5f);
            tbRt.pivot = new Vector2(0.5f, 0.5f);
            tbRt.anchoredPosition = new Vector2(0f, 130f);
            tbRt.sizeDelta = new Vector2(340f, 50f);

            GameObject timeTxtObj = new GameObject("Label");
            timeTxtObj.transform.SetParent(timeBtnObj.transform, false);
            _aiTimeDropdownBtnText = timeTxtObj.AddComponent<Text>();
            _aiTimeDropdownBtnText.text = $"⏱  {GetAITimeDisplay(_selectedAITimeIndex)}";
            _aiTimeDropdownBtnText.font = font;
            _aiTimeDropdownBtnText.fontSize = 16;
            _aiTimeDropdownBtnText.fontStyle = FontStyle.Bold;
            _aiTimeDropdownBtnText.alignment = TextAnchor.MiddleLeft;
            _aiTimeDropdownBtnText.color = new Color(0.22f, 0.12f, 0.04f, 1f);
            _aiTimeDropdownBtnText.raycastTarget = false;
            MakeTextCrisp(_aiTimeDropdownBtnText, new Color(1.0f, 0.94f, 0.75f, 0.85f));
            RectTransform ttRt = timeTxtObj.GetComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.offsetMin = new Vector2(26f, 0f);
            ttRt.offsetMax = new Vector2(-48f, 0f);

            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(timeBtnObj.transform, false);
            Text arrowTxt = arrowObj.AddComponent<Text>();
            arrowTxt.text = "▼";
            arrowTxt.font = font;
            arrowTxt.fontSize = 13;
            arrowTxt.fontStyle = FontStyle.Bold;
            arrowTxt.alignment = TextAnchor.MiddleCenter;
            arrowTxt.color = new Color(0.35f, 0.20f, 0.08f);
            arrowTxt.raycastTarget = false;
            RectTransform arRt = arrowObj.GetComponent<RectTransform>();
            arRt.anchorMin = new Vector2(1f, 0.5f);
            arRt.anchorMax = new Vector2(1f, 0.5f);
            arRt.pivot = new Vector2(1f, 0.5f);
            arRt.anchoredPosition = new Vector2(-18f, 0f);
            arRt.sizeDelta = new Vector2(24f, 24f);

            // --- Section 2: Side Selection Cards ---
            CreateSectionHeaderLabel(rightCol.transform, AegisLocalization.T("CHỌN PHE CHIẾN ĐẤU", "CHOOSE YOUR SIDE"), new Vector2(0f, 65f), font);

            // Side White Card Button
            GameObject sideWhiteObj = new GameObject("BtnSideWhite");
            sideWhiteObj.transform.SetParent(rightCol.transform, false);
            _sideWhiteCardBg = sideWhiteObj.AddComponent<Image>();
            _sideWhiteCardBg.type = Image.Type.Sliced;
            _sideWhiteCardBg.color = Color.white;
            Button swBtn = sideWhiteObj.AddComponent<Button>();
            swBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                SelectAIPieceColor(PieceColor.White);
            });
            sideWhiteObj.AddComponent<MenuButtonEffect>();
            RectTransform swRt = sideWhiteObj.GetComponent<RectTransform>();
            swRt.anchorMin = new Vector2(0.5f, 0.5f);
            swRt.anchorMax = new Vector2(0.5f, 0.5f);
            swRt.pivot = new Vector2(0.5f, 0.5f);
            swRt.anchoredPosition = new Vector2(-88f, 15f);
            swRt.sizeDelta = new Vector2(160f, 50f);

            GameObject swTxtObj = new GameObject("Label");
            swTxtObj.transform.SetParent(sideWhiteObj.transform, false);
            _sideWhiteCardTxt = swTxtObj.AddComponent<Text>();
            _sideWhiteCardTxt.text = AegisLocalization.T("♔  Quân Trắng", "♔  White");
            _sideWhiteCardTxt.font = font;
            _sideWhiteCardTxt.fontSize = 16;
            _sideWhiteCardTxt.fontStyle = FontStyle.Bold;
            _sideWhiteCardTxt.alignment = TextAnchor.MiddleCenter;
            _sideWhiteCardTxt.raycastTarget = false;
            MakeTextCrisp(_sideWhiteCardTxt);
            RectTransform swtRt = swTxtObj.GetComponent<RectTransform>();
            swtRt.anchorMin = Vector2.zero;
            swtRt.anchorMax = Vector2.one;
            swtRt.offsetMin = Vector2.zero;
            swtRt.offsetMax = Vector2.zero;

            // Side Black Card Button
            GameObject sideBlackObj = new GameObject("BtnSideBlack");
            sideBlackObj.transform.SetParent(rightCol.transform, false);
            _sideBlackCardBg = sideBlackObj.AddComponent<Image>();
            _sideBlackCardBg.type = Image.Type.Sliced;
            _sideBlackCardBg.color = Color.white;
            Button sbBtn = sideBlackObj.AddComponent<Button>();
            sbBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                SelectAIPieceColor(PieceColor.Black);
            });
            sideBlackObj.AddComponent<MenuButtonEffect>();
            RectTransform sbRt = sideBlackObj.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.5f, 0.5f);
            sbRt.anchorMax = new Vector2(0.5f, 0.5f);
            sbRt.pivot = new Vector2(0.5f, 0.5f);
            sbRt.anchoredPosition = new Vector2(88f, 15f);
            sbRt.sizeDelta = new Vector2(160f, 50f);

            GameObject sbTxtObj = new GameObject("Label");
            sbTxtObj.transform.SetParent(sideBlackObj.transform, false);
            _sideBlackCardTxt = sbTxtObj.AddComponent<Text>();
            _sideBlackCardTxt.text = AegisLocalization.T("♚  Quân Đen", "♚  Black");
            _sideBlackCardTxt.font = font;
            _sideBlackCardTxt.fontSize = 16;
            _sideBlackCardTxt.fontStyle = FontStyle.Bold;
            _sideBlackCardTxt.alignment = TextAnchor.MiddleCenter;
            _sideBlackCardTxt.raycastTarget = false;
            MakeTextCrisp(_sideBlackCardTxt);
            RectTransform sbtRt = sbTxtObj.GetComponent<RectTransform>();
            sbtRt.anchorMin = Vector2.zero;
            sbtRt.anchorMax = Vector2.one;
            sbtRt.offsetMin = Vector2.zero;
            sbtRt.offsetMax = Vector2.zero;

            RefreshRadioVisuals();

            // --- Section 3: Golden Royal CTA Button [⚔ VÀO TRẬN] ---
            GameObject ctaObj = new GameObject("BtnStartAIMatch");
            ctaObj.transform.SetParent(rightCol.transform, false);
            Image ctaBg = ctaObj.AddComponent<Image>();
            ctaBg.sprite = MedievalUIAssetLoader.GetSprite("btn_hex_gold_radiant")
                         ?? MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold");
            ctaBg.type = Image.Type.Sliced;
            ctaBg.color = Color.white;
            Button ctaBtn = ctaObj.AddComponent<Button>();
            ctaBtn.targetGraphic = ctaBg;
            ctaBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                ExecuteStartAIMatch();
            });
            ctaObj.AddComponent<MenuButtonEffect>();
            RectTransform ctaRt = ctaObj.GetComponent<RectTransform>();
            ctaRt.anchorMin = new Vector2(0.5f, 0.5f);
            ctaRt.anchorMax = new Vector2(0.5f, 0.5f);
            ctaRt.pivot = new Vector2(0.5f, 0.5f);
            ctaRt.anchoredPosition = new Vector2(0f, -115f);
            ctaRt.sizeDelta = new Vector2(340f, 66f);

            GameObject ctaTxtObj = new GameObject("Txt");
            ctaTxtObj.transform.SetParent(ctaObj.transform, false);
            Text ctaTxt = ctaTxtObj.AddComponent<Text>();
            ctaTxt.text = AegisLocalization.T("⚔   VÀO TRẬN", "⚔   START MATCH");
            ctaTxt.font = font;
            ctaTxt.fontSize = 25;
            ctaTxt.fontStyle = FontStyle.Bold;
            ctaTxt.alignment = TextAnchor.MiddleCenter;
            ctaTxt.color = new Color(0.18f, 0.08f, 0.02f, 1f); // Deep Royal Dark Ink
            ctaTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            ctaTxt.verticalOverflow = VerticalWrapMode.Overflow;
            ctaTxt.raycastTarget = false;
            MakeTextCrisp(ctaTxt, new Color(1f, 0.92f, 0.60f, 0.8f));
            RectTransform ctrt = ctaTxtObj.GetComponent<RectTransform>();
            ctrt.anchorMin = Vector2.zero;
            ctrt.anchorMax = Vector2.one;
            ctrt.offsetMin = Vector2.zero;
            ctrt.offsetMax = Vector2.zero;

            // ---------------------------------------------------------------------
            // 5. Time Dropdown Menu (Overlaid above Right Column)
            // ---------------------------------------------------------------------
            _aiTimeDropdownBlocker = new GameObject("TimeDropdownBlocker");
            _aiTimeDropdownBlocker.transform.SetParent(cardObj.transform, false);
            RectTransform blkRt = _aiTimeDropdownBlocker.AddComponent<RectTransform>();
            blkRt.anchorMin = Vector2.zero;
            blkRt.anchorMax = Vector2.one;
            blkRt.offsetMin = new Vector2(-600f, -600f);
            blkRt.offsetMax = new Vector2(600f, 600f);
            Image blkImg = _aiTimeDropdownBlocker.AddComponent<Image>();
            blkImg.color = new Color(0f, 0f, 0f, 0.01f);
            Button blkBtn = _aiTimeDropdownBlocker.AddComponent<Button>();
            blkBtn.onClick.AddListener(() => SetAITimeDropdownOpen(false));
            _aiTimeDropdownBlocker.SetActive(false);

            _aiTimeDropdownMenu = new GameObject("TimeDropdownMenu");
            _aiTimeDropdownMenu.transform.SetParent(cardObj.transform, false);
            Image ddBg = _aiTimeDropdownMenu.AddComponent<Image>();
            // Rich Dark Mahogany Frame with Radiant Gold Border (High contrast, elegant popup)
            ddBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(356, 268, 14f, new Color(0.12f, 0.08f, 0.04f, 0.98f), new Color(0.85f, 0.70f, 0.35f, 1.0f), 2.5f);
            ddBg.type = Image.Type.Sliced;
            ddBg.color = Color.white;
            RectTransform ddRt = _aiTimeDropdownMenu.GetComponent<RectTransform>();
            ddRt.anchorMin = new Vector2(1f, 0.5f);
            ddRt.anchorMax = new Vector2(1f, 0.5f);
            ddRt.pivot = new Vector2(1f, 1f);
            // Position directly below TimeDropdownBtn (bottom is at +65px)
            ddRt.anchoredPosition = new Vector2(-62f, 62f);
            ddRt.sizeDelta = new Vector2(356f, 268f);

            _aiTimeOptionBgs = new Image[5];
            _aiTimeOptionTexts = new Text[5];
            _aiTimeOptionChecks = new Text[5];

            for (int i = 0; i < 5; i++)
            {
                int optIdx = i;
                GameObject itemObj = new GameObject($"TimeOption_{optIdx}");
                itemObj.transform.SetParent(_aiTimeDropdownMenu.transform, false);
                RectTransform iRt = itemObj.AddComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0.5f, 1f);
                iRt.anchorMax = new Vector2(0.5f, 1f);
                iRt.pivot = new Vector2(0.5f, 1f);
                iRt.anchoredPosition = new Vector2(0f, -12f - optIdx * 50f);
                iRt.sizeDelta = new Vector2(334f, 44f);

                Image itemBg = itemObj.AddComponent<Image>();
                itemBg.type = Image.Type.Sliced;
                itemBg.color = Color.white;
                _aiTimeOptionBgs[optIdx] = itemBg;

                Button itemBtn = itemObj.AddComponent<Button>();
                itemBtn.onClick.AddListener(() => SelectAITime(optIdx));
                itemObj.AddComponent<MenuButtonEffect>();

                GameObject txtObj = new GameObject("Label");
                txtObj.transform.SetParent(itemObj.transform, false);
                Text txt = txtObj.AddComponent<Text>();
                txt.font = font;
                txt.fontSize = 16;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleLeft;
                txt.raycastTarget = false;
                RectTransform itRt = txtObj.GetComponent<RectTransform>();
                itRt.anchorMin = Vector2.zero;
                itRt.anchorMax = Vector2.one;
                // Generous left padding (34px) ensures numbers "3 ", "5 ", "10 ", "15 " are NEVER cut off!
                itRt.offsetMin = new Vector2(34f, 0f);
                itRt.offsetMax = new Vector2(-46f, 0f);
                _aiTimeOptionTexts[optIdx] = txt;

                GameObject chkObj = new GameObject("Check");
                chkObj.transform.SetParent(itemObj.transform, false);
                Text chk = chkObj.AddComponent<Text>();
                chk.font = font;
                chk.fontSize = 19;
                chk.fontStyle = FontStyle.Bold;
                chk.alignment = TextAnchor.MiddleCenter;
                chk.color = new Color(1.0f, 0.88f, 0.35f, 1f);
                chk.raycastTarget = false;
                RectTransform chkRt = chkObj.GetComponent<RectTransform>();
                chkRt.anchorMin = new Vector2(1f, 0.5f);
                chkRt.anchorMax = new Vector2(1f, 0.5f);
                chkRt.pivot = new Vector2(1f, 0.5f);
                chkRt.anchoredPosition = new Vector2(-14f, 0f);
                chkRt.sizeDelta = new Vector2(28f, 28f);
                _aiTimeOptionChecks[optIdx] = chk;
            }

            _aiTimeDropdownMenu.SetActive(false);
            RefreshAITimeDropdownItems();

            _aiDifficultyModal.SetActive(false);
        }

        private void CreateSectionHeaderLabel(Transform parent, string title, Vector2 pos, Font font)
        {
            GameObject lblObj = new GameObject("SectionHeader");
            lblObj.transform.SetParent(parent, false);
            Text txt = lblObj.AddComponent<Text>();
            txt.text = title;
            txt.font = font;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.35f, 0.22f, 0.10f, 1f); // Deep Medieval Walnut Ink
            txt.raycastTarget = false;
            MakeTextCrisp(txt);
            RectTransform rt = lblObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(340f, 24f);
        }

        private void SelectAIDifficultyLevel(int level)
        {
            _selectedAIDifficulty = level;
            RefreshAIDifficultyVisuals();
        }

        private void RefreshAIDifficultyVisuals()
        {
            if (_aiDiffCardBgs == null) return;
            Sprite activeSprite = MedievalUIAssetLoader.GetSprite("btn_hex_gold_radiant")
                                ?? MedievalUIAssetLoader.GetSprite("btn_blank_parchment_gold");
            Sprite inactiveSprite = MedievalUIAssetLoader.GetSprite("parchment_box_clean")
                                  ?? MedievalUIAssetLoader.GetSprite("btn_wood_blank_baroque");

            for (int i = 0; i < 4; i++)
            {
                bool active = (i + 1 == _selectedAIDifficulty);
                if (_aiDiffCardBgs[i] != null)
                {
                    _aiDiffCardBgs[i].sprite = active ? activeSprite : inactiveSprite;
                }
                if (_aiDiffCardTitles != null && i < _aiDiffCardTitles.Length && _aiDiffCardTitles[i] != null)
                {
                    _aiDiffCardTitles[i].color = active ? new Color(0.18f, 0.08f, 0.02f, 1f) : new Color(0.32f, 0.20f, 0.10f, 1f);
                }
                if (_aiDiffCardElos != null && i < _aiDiffCardElos.Length && _aiDiffCardElos[i] != null)
                {
                    _aiDiffCardElos[i].color = active ? new Color(0.45f, 0.22f, 0.05f, 1f) : new Color(0.48f, 0.35f, 0.20f, 1f);
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
            Sprite activeSprite = MedievalUIAssetLoader.GetSprite("btn_hex_gold_radiant")
                                ?? MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold");
            Sprite inactiveSprite = MedievalUIAssetLoader.GetSprite("parchment_box_clean")
                                  ?? MedievalUIAssetLoader.GetSprite("btn_wood_blank_baroque");

            bool isWhite = (_selectedAIPieceColor == PieceColor.White);

            if (_sideWhiteCardBg != null)
            {
                _sideWhiteCardBg.sprite = isWhite ? activeSprite : inactiveSprite;
            }
            if (_sideWhiteCardTxt != null)
            {
                _sideWhiteCardTxt.color = isWhite ? new Color(0.18f, 0.10f, 0.02f, 1f) : new Color(0.42f, 0.30f, 0.18f, 1f);
            }

            if (_sideBlackCardBg != null)
            {
                _sideBlackCardBg.sprite = (!isWhite) ? activeSprite : inactiveSprite;
            }
            if (_sideBlackCardTxt != null)
            {
                _sideBlackCardTxt.color = (!isWhite) ? new Color(0.18f, 0.10f, 0.02f, 1f) : new Color(0.42f, 0.30f, 0.18f, 1f);
            }
        }

        private void ExecuteStartAIMatch()
        {
            SetAITimeDropdownOpen(false);
            _aiDifficultyModal.SetActive(false);
            StartGameVsAI(_selectedAIDifficulty);
        }

        private string GetAITimeOptionLabel(int idx)
        {
            switch (idx)
            {
                case 0: return AegisLocalization.T("3 phút  •  Blitz", "3 mins  •  Blitz");
                case 1: return AegisLocalization.T("5 phút  •  Blitz", "5 mins  •  Blitz");
                case 2: return AegisLocalization.T("10 phút  •  Rapid", "10 mins  •  Rapid");
                case 3: return AegisLocalization.T("15 phút  •  Rapid", "15 mins  •  Rapid");
                default: return AegisLocalization.T("Không giới hạn  •  Tự do", "Unlimited  •  Casual");
            }
        }

        private void ToggleAITimeDropdown()
        {
            if (_aiTimeDropdownMenu == null) return;
            SetAITimeDropdownOpen(!_aiTimeDropdownMenu.activeSelf);
        }

        private void SetAITimeDropdownOpen(bool isOpen)
        {
            if (_aiTimeDropdownMenu == null) return;
            _aiTimeDropdownMenu.SetActive(isOpen);
            if (_aiTimeDropdownBlocker != null) _aiTimeDropdownBlocker.SetActive(isOpen);
            if (_aiTimeDropdownBtnText != null)
            {
                _aiTimeDropdownBtnText.text = $"⏱   {GetAITimeDisplay(_selectedAITimeIndex)}";
            }
            if (isOpen)
            {
                if (_aiTimeDropdownBlocker != null) _aiTimeDropdownBlocker.transform.SetAsLastSibling();
                _aiTimeDropdownMenu.transform.SetAsLastSibling();
                RefreshAITimeDropdownItems();
            }
        }

        private void SelectAITime(int index)
        {
            _selectedAITimeIndex = index;
            if (_aiTimeDropdownBtnText != null)
            {
                _aiTimeDropdownBtnText.text = $"⏱   {GetAITimeDisplay(_selectedAITimeIndex)}";
            }
            SetAITimeDropdownOpen(false);
            AegisAudioManager.PlayButtonClick();
        }

        private void RefreshAITimeDropdownItems()
        {
            if (_aiTimeOptionBgs == null) return;

            // Selected: Radiant Gilded Plate with Vivid Gold Border (Rich warm gold)
            Sprite selSprite = ArcaneAssetLoader.GetGlassBoxSprite(334, 44, 8f, new Color(0.48f, 0.30f, 0.10f, 0.98f), new Color(1.0f, 0.88f, 0.45f, 1.0f), 2f);
            // Unselected: Polished Dark Walnut Plate with Subtle Bronze Border (Crystal clear contrast)
            Sprite unselSprite = ArcaneAssetLoader.GetGlassBoxSprite(334, 44, 8f, new Color(0.18f, 0.12f, 0.07f, 0.92f), new Color(0.45f, 0.32f, 0.18f, 0.75f), 1.2f);

            for (int i = 0; i < 5; i++)
            {
                bool isSelected = (i == _selectedAITimeIndex);
                if (_aiTimeOptionBgs[i] != null)
                {
                    _aiTimeOptionBgs[i].sprite = isSelected ? selSprite : unselSprite;
                }
                if (_aiTimeOptionTexts[i] != null)
                {
                    // 100% High-Contrast Colors:
                    // Selected: Sunlit Brilliant Ivory Gold on Gilded Plate
                    // Unselected: Crisp Radiant Antique Ivory on Dark Walnut
                    _aiTimeOptionTexts[i].color = isSelected ? new Color(1.0f, 0.96f, 0.82f, 1f) : new Color(0.92f, 0.88f, 0.80f, 1f);
                    _aiTimeOptionTexts[i].fontStyle = FontStyle.Bold;
                    _aiTimeOptionTexts[i].text = GetAITimeOptionLabel(i);
                    MakeTextCrisp(_aiTimeOptionTexts[i], new Color(0.08f, 0.03f, 0.01f, 0.95f));
                }
                if (_aiTimeOptionChecks[i] != null)
                {
                    _aiTimeOptionChecks[i].text = isSelected ? "✓" : "";
                    _aiTimeOptionChecks[i].color = isSelected ? new Color(1.0f, 0.88f, 0.35f, 1f) : Color.clear;
                }
            }
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
                UIManager2D.Instance.SetHUDVisible(true);
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
            Sprite cardSprite = MedievalUIAssetLoader.GetSprite("frame_shop_collection_1520")
                              ?? MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned)
                              ?? ArcaneAssetLoader.LoadArcaneSprite("settings_card_bg.png");
            if (cardSprite != null)
            {
                cardBg.sprite = cardSprite;
                cardBg.type = Image.Type.Simple;
                cardBg.preserveAspect = false;
                cardBg.color = Color.white;
            }
            else
            {
                cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1240, 740, 24f, new Color(0.03f, 0.08f, 0.16f, 0.95f), ColCyanBorder, 2f);
                cardBg.type = Image.Type.Sliced;
            }

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1260f, 750f);

            // Close Button [ Crossed Swords Gold ] (Positioned as designed in Scene)
            GameObject setCloseBtn = CreateRoyalCloseButton(cardObj.transform, new Vector2(-51f, -130f), () => CloseModal(_settingsModal), 66f, "HeaderCloseBtn");
            if (setCloseBtn != null) setCloseBtn.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            // 2. Left Tabs Column (Offset safely inside parchment, avoiding left filigree border & top-left corner ornament)
            GameObject tabCol = new GameObject("TabsColumn");
            tabCol.transform.SetParent(cardObj.transform, false);
            RectTransform tcRt = tabCol.AddComponent<RectTransform>();
            tcRt.anchorMin = new Vector2(0f, 1f);
            tcRt.anchorMax = new Vector2(0f, 1f);
            tcRt.pivot = new Vector2(0f, 1f);
            tcRt.anchoredPosition = new Vector2(52f, -172f);
            tcRt.sizeDelta = new Vector2(265f, 460f);

            VerticalLayoutGroup tvlg = tabCol.AddComponent<VerticalLayoutGroup>();
            tvlg.spacing = 16f;
            tvlg.childAlignment = TextAnchor.UpperLeft;
            tvlg.childControlWidth = true;
            tvlg.childControlHeight = false;
            tvlg.childForceExpandWidth = true;
            tvlg.childForceExpandHeight = false;

            string[] tabLabels = {
                AegisLocalization.T("Trò chơi", "Gameplay"),
                AegisLocalization.T("Đồ họa", "Graphics"),
                AegisLocalization.T("Âm thanh", "Audio"),
                AegisLocalization.T("Điều khiển", "Controls"),
                AegisLocalization.T("Ngôn ngữ", "Language")
            };

            Sprite activeTabSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold)
                                    ?? MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold")
                                    ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodCarved);
            Sprite inactiveTabSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodSmall)
                                      ?? MedievalUIAssetLoader.GetSprite("dropdown_parchment_box");

            for (int i = 0; i < 5; i++)
            {
                int tabIdx = i;
                GameObject tObj = new GameObject("Tab_" + i);
                tObj.transform.SetParent(tabCol.transform, false);

                RectTransform trt = tObj.AddComponent<RectTransform>();
                trt.sizeDelta = new Vector2(265f, 60f);

                LayoutElement le = tObj.AddComponent<LayoutElement>();
                le.preferredWidth = 265f;
                le.preferredHeight = 60f;

                Image bg = tObj.AddComponent<Image>();
                bg.type = Image.Type.Sliced;
                bg.sprite = (i == 0) ? activeTabSprite : inactiveTabSprite;
                bg.color = (i == 0) ? Color.white : new Color(0.85f, 0.78f, 0.68f, 0.55f);

                Button btn = tObj.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() => SwitchSettingsTab(tabIdx));
                tObj.AddComponent<MenuButtonEffect>();

                // Label (Warm Ivory-Gold when active, rich walnut ink when inactive)
                GameObject lbObj = new GameObject("Label");
                lbObj.transform.SetParent(tObj.transform, false);
                Text lb = lbObj.AddComponent<Text>();
                lb.text = tabLabels[i];
                lb.font = font;
                lb.fontSize = 22;
                lb.fontStyle = FontStyle.Bold;
                lb.alignment = TextAnchor.MiddleCenter;
                lb.color = (i == 0) ? new Color(1f, 0.95f, 0.82f, 1f) : new Color(0.32f, 0.20f, 0.12f, 0.92f);
                lb.raycastTarget = false;
                MakeTextCrisp(lb);
                RectTransform lbRt = lbObj.GetComponent<RectTransform>();
                lbRt.anchorMin = Vector2.zero;
                lbRt.anchorMax = Vector2.one;
                lbRt.offsetMin = Vector2.zero;
                lbRt.offsetMax = Vector2.zero;

                _settingsTabBgs[i] = bg;
                _settingsTabTexts[i] = lb;
                _settingsTabIcons[i] = null;
            }

            // 3. Vertical Divider Line (Warm Antique Gold)
            GameObject vLine = new GameObject("VerticalDivider");
            vLine.transform.SetParent(cardObj.transform, false);
            Image vImg = vLine.AddComponent<Image>();
            vImg.color = new Color(0.72f, 0.54f, 0.28f, 0.55f);
            RectTransform vlRt = vLine.GetComponent<RectTransform>();
            vlRt.anchorMin = new Vector2(0f, 1f);
            vlRt.anchorMax = new Vector2(0f, 1f);
            vlRt.pivot = new Vector2(0.5f, 1f);
            vlRt.anchoredPosition = new Vector2(335f, -165f);
            vlRt.sizeDelta = new Vector2(2f, 480f);

            // 4. Right Content Container (With ample padding avoiding golden corner carvings)
            GameObject contentContainer = new GameObject("SettingsContentContainer");
            contentContainer.transform.SetParent(cardObj.transform, false);
            RectTransform ccRt = contentContainer.AddComponent<RectTransform>();
            ccRt.anchorMin = new Vector2(0f, 0f);
            ccRt.anchorMax = new Vector2(1f, 1f);
            ccRt.pivot = new Vector2(0f, 0.5f);
            ccRt.offsetMin = new Vector2(360f, 40f);
            ccRt.offsetMax = new Vector2(-55f, -165f);

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
            if (_selectedSettingsTab != tabIndex) AegisAudioManager.PlayTabSwitch();
            _selectedSettingsTab = tabIndex;
            Sprite activeSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold)
                                ?? MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold")
                                ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodCarved);
            Sprite inactiveSprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodSmall)
                                  ?? MedievalUIAssetLoader.GetSprite("dropdown_parchment_box");

            for (int i = 0; i < 5; i++)
            {
                bool active = (i == tabIndex);
                if (_settingsTabBgs[i] != null)
                {
                    _settingsTabBgs[i].sprite = active ? activeSprite : inactiveSprite;
                    _settingsTabBgs[i].type = Image.Type.Sliced;
                    _settingsTabBgs[i].color = active ? Color.white : new Color(0.85f, 0.78f, 0.68f, 0.55f);
                }
                if (_settingsTabTexts[i] != null)
                {
                    _settingsTabTexts[i].fontStyle = FontStyle.Bold;
                    _settingsTabTexts[i].color = active ? new Color(1f, 0.95f, 0.82f, 1f) : new Color(0.32f, 0.20f, 0.12f, 0.92f);
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
            float step = 85f;

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

            CreateModernToggleRow(panel.transform, AegisLocalization.T("Nhạc nền", "Music"), AegisAudioManager.IsMusicEnabled(), yStart - 2 * step, font, on =>
            {
                AegisAudioManager.SetMusicEnabled(on);
            });

            CreateModernToggleRow(panel.transform, AegisLocalization.T("Âm thanh hiệu ứng", "Sound Effects"), AegisAudioManager.IsSFXEnabled(), yStart - 3 * step, font, on =>
            {
                AegisAudioManager.SetSFXEnabled(on);
            });

            CreateModernToggleRow(panel.transform, AegisLocalization.T("Rung phản hồi (Vibration)", "Haptic Vibration"), true, yStart - 4 * step, font, on =>
            {
                PlayerPrefs.SetInt("aegis.vibrate", on ? 1 : 0);
            });

            CreateModernDropdownRow(panel.transform, AegisLocalization.T("Ngôn ngữ giao diện", "Interface Language"), langOpts, AegisLocalization.GetCurrentLanguageIndex(), yStart - 5 * step, font, val =>
            {
                AppLanguage targetLang = (val == "Tiếng Việt") ? AppLanguage.Vietnamese : AppLanguage.English;
                AegisLocalization.SetLanguage(targetLang);
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

            float yStart = -25f;
            float step = 98f;

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

            float yStart = -35f;
            float step = 115f;

            CreateModernToggleRow(panel.transform, AegisLocalization.T("Nhạc nền (BGM)", "Background Music"), AegisAudioManager.IsMusicEnabled(), yStart - 0 * step, font, on =>
            {
                AegisAudioManager.SetMusicEnabled(on);
            });
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Âm thanh hiệu ứng (SFX)", "Sound Effects"), AegisAudioManager.IsSFXEnabled(), yStart - 1 * step, font, on =>
            {
                AegisAudioManager.SetSFXEnabled(on);
            });
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Giọng bình luận viên (Voice)", "Announcer Voice"), true, yStart - 2 * step, font, null);
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Rung phản hồi lực", "Haptic Feedback"), true, yStart - 3 * step, font, null);

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

            float yStart = -35f;
            float step = 115f;

            string[] sensOpts = {
                AegisLocalization.T("Chậm", "Slow"),
                AegisLocalization.T("Tiêu chuẩn", "Normal"),
                AegisLocalization.T("Nhanh", "Fast")
            };

            CreateModernToggleRow(panel.transform, AegisLocalization.T("Chạm để chọn & di chuyển", "Tap to Select & Move"), true, yStart - 0 * step, font, null);
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Kéo & thả quân cờ", "Drag & Drop Pieces"), true, yStart - 1 * step, font, null);
            CreateModernToggleRow(panel.transform, AegisLocalization.T("Đảo ngược trục xoay Camera", "Invert Camera Orbit"), false, yStart - 2 * step, font, null);
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

            float yStart = -50f;
            float step = 115f;

            string[] langOpts = { "Tiếng Việt", "English" };
            int currentIdx = AegisLocalization.GetCurrentLanguageIndex();

            CreateModernDropdownRow(panel.transform, AegisLocalization.T("Ngôn ngữ hiển thị", "Display Language"), langOpts, currentIdx, yStart - 0 * step, font, val =>
            {
                AppLanguage targetLang = (val == "Tiếng Việt") ? AppLanguage.Vietnamese : AppLanguage.English;
                AegisLocalization.SetLanguage(targetLang);
            });

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
            rt.sizeDelta = new Vector2(0f, 60f);

            // Left Label (Ample width 460px and bold 23px font for effortless reading)
            GameObject lbObj = new GameObject("Label");
            lbObj.transform.SetParent(row.transform, false);
            Text lb = lbObj.AddComponent<Text>();
            lb.text = label;
            lb.font = font;
            lb.fontSize = 23;
            lb.fontStyle = FontStyle.Bold;
            lb.alignment = TextAnchor.MiddleLeft;
            lb.horizontalOverflow = HorizontalWrapMode.Wrap;
            lb.verticalOverflow = VerticalWrapMode.Overflow;
            lb.color = new Color(0.18f, 0.10f, 0.04f, 1f); // Deep High-Contrast Antique Ink
            MakeTextCrisp(lb);
            RectTransform lrt = lbObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(0f, 0.5f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.anchoredPosition = new Vector2(10f, 0f);
            lrt.sizeDelta = new Vector2(460f, 52f);

            // Right Dropdown Box (Scaled up to 260x50 for comfortable touch and crystal readability)
            GameObject ddObj = new GameObject("Dropdown");
            ddObj.transform.SetParent(row.transform, false);
            Image bg = ddObj.AddComponent<Image>();
            Sprite ddSprite = MedievalUIAssetLoader.GetSprite("dropdown_parchment_box")
                              ?? ArcaneAssetLoader.LoadArcaneSprite("settings_dropdown_box.png");
            if (ddSprite != null)
            {
                bg.sprite = ddSprite;
                bg.type = Image.Type.Sliced;
            }
            else
            {
                bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(260, 50, 10f, new Color(0.04f, 0.10f, 0.18f, 0.9f), ColCyanBorder, 1.2f);
                bg.type = Image.Type.Sliced;
            }

            Button btn = ddObj.AddComponent<Button>();
            ddObj.AddComponent<MenuButtonEffect>();

            RectTransform drt = ddObj.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(1f, 0.5f);
            drt.anchorMax = new Vector2(1f, 0.5f);
            drt.pivot = new Vector2(1f, 0.5f);
            drt.anchoredPosition = new Vector2(-20f, 0f);
            drt.sizeDelta = new Vector2(260f, 50f);

            // Text value (Bold 20px)
            int currentIdx = Mathf.Clamp(defaultIdx, 0, options.Length - 1);
            GameObject valObj = new GameObject("ValueText");
            valObj.transform.SetParent(ddObj.transform, false);
            Text valTxt = valObj.AddComponent<Text>();
            valTxt.text = options[currentIdx];
            valTxt.font = font;
            valTxt.fontSize = 20;
            valTxt.fontStyle = FontStyle.Bold;
            valTxt.alignment = TextAnchor.MiddleLeft;
            valTxt.color = new Color(0.20f, 0.12f, 0.05f, 1f); // Dark Antique Ink on Parchment
            valTxt.raycastTarget = false;
            MakeTextCrisp(valTxt);
            RectTransform vrt = valObj.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0f, 0f);
            vrt.anchorMax = new Vector2(1f, 1f);
            vrt.offsetMin = new Vector2(20f, 0f);
            vrt.offsetMax = new Vector2(-44f, 0f);

            // Right Chevron Arrow
            GameObject arrObj = new GameObject("Arrow");
            arrObj.transform.SetParent(ddObj.transform, false);
            bool hasBakedArrow = (ddSprite != null && ddSprite.name == "dropdown_parchment_box");
            if (!hasBakedArrow)
            {
                Text arrTxt = arrObj.AddComponent<Text>();
                arrTxt.text = "▼";
                arrTxt.font = font;
                arrTxt.fontSize = 14;
                arrTxt.alignment = TextAnchor.MiddleCenter;
                arrTxt.color = new Color(0.48f, 0.32f, 0.18f, 1f);
                arrTxt.raycastTarget = false;
                MakeTextCrisp(arrTxt);
                RectTransform art = arrObj.GetComponent<RectTransform>();
                art.anchorMin = new Vector2(1f, 0.5f);
                art.anchorMax = new Vector2(1f, 0.5f);
                art.pivot = new Vector2(1f, 0.5f);
                art.anchoredPosition = new Vector2(-15f, 0f);
                art.sizeDelta = new Vector2(24f, 24f);
            }

            // Click listener: cycle options
            btn.onClick.AddListener(() =>
            {
                currentIdx = (currentIdx + 1) % options.Length;
                valTxt.text = options[currentIdx];
                onSelected?.Invoke(options[currentIdx]);
                AegisAudioManager.PlayButtonClick();
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
            rt.sizeDelta = new Vector2(0f, 60f);

            // Left Label (Ample width 460px and bold 23px font)
            GameObject lbObj = new GameObject("Label");
            lbObj.transform.SetParent(row.transform, false);
            Text lb = lbObj.AddComponent<Text>();
            lb.text = label;
            lb.font = font;
            lb.fontSize = 23;
            lb.fontStyle = FontStyle.Bold;
            lb.alignment = TextAnchor.MiddleLeft;
            lb.horizontalOverflow = HorizontalWrapMode.Wrap;
            lb.verticalOverflow = VerticalWrapMode.Overflow;
            lb.color = new Color(0.18f, 0.10f, 0.04f, 1f); // Deep High-Contrast Antique Ink 
            MakeTextCrisp(lb);
            RectTransform lrt = lbObj.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0.5f);
            lrt.anchorMax = new Vector2(0f, 0.5f);
            lrt.pivot = new Vector2(0f, 0.5f);
            lrt.anchoredPosition = new Vector2(10f, 0f);
            lrt.sizeDelta = new Vector2(460f, 52f);

            // Right Toggle Capsule (Enlarged to 88x40 for prominence)
            bool state = initialOn;
            GameObject toggleObj = new GameObject("ToggleCapsule");
            toggleObj.transform.SetParent(row.transform, false);

            Image toggleImg = toggleObj.AddComponent<Image>();
            Sprite onSprite = MedievalUIAssetLoader.GetSprite("toggle_switch_on") ?? MedievalUIAssetLoader.GetSprite("checkbox_checked") ?? ArcaneAssetLoader.LoadArcaneSprite("settings_toggle_on.png");
            Sprite offSprite = MedievalUIAssetLoader.GetSprite("toggle_switch_off") ?? MedievalUIAssetLoader.GetSprite("checkbox_unchecked") ?? ArcaneAssetLoader.LoadArcaneSprite("settings_toggle_off.png");
            toggleImg.sprite = state ? onSprite : offSprite;
            toggleImg.preserveAspect = true;

            Button btn = toggleObj.AddComponent<Button>();
            toggleObj.AddComponent<MenuButtonEffect>();

            RectTransform trt = toggleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(1f, 0.5f);
            trt.anchorMax = new Vector2(1f, 0.5f);
            trt.pivot = new Vector2(1f, 0.5f);
            trt.anchoredPosition = new Vector2(-25f, 0f);
            trt.sizeDelta = new Vector2(88f, 40f);

            btn.onClick.AddListener(() =>
            {
                state = !state;
                toggleImg.sprite = state ? onSprite : offSprite;
                onToggled?.Invoke(state);
                ShowToast($"{label}: {(state ? "BẬT" : "TẮT")}", ToastType.Info);
                AegisAudioManager.PlayButtonClick();
            });
        }

        private Text CreateGrimoireTextComp(Transform parent, string content, int size, FontStyle style, Color color, Font font, Vector2 pos, Vector2 delta, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            string safeContent = content ?? "";
            GameObject txtObj = new GameObject("Txt_" + safeContent.GetHashCode(), typeof(RectTransform));
            txtObj.transform.SetParent(parent, false);
            Text t = txtObj.AddComponent<Text>();
            t.text = safeContent;
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            MakeTextCrisp(t);
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = delta;
            return t;
        }
        private void BuildScreen6Tutorial(Transform parent, Font font)
        {
            _tutorialModal = CreateFullscreenPanel(parent, "Screen6_TutorialScreen");
            _tutorialModal.transform.SetAsLastSibling();

            // Fullscreen Background & Deep Dark Backdrop to completely cover previous screens
            GameObject bgImgObj = new GameObject("FullscreenBg");
            bgImgObj.transform.SetParent(_tutorialModal.transform, false);
            RectTransform bgRt = bgImgObj.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            Image bgImg = bgImgObj.AddComponent<Image>();
            bgImg.sprite = MedievalUIAssetLoader.GetSprite("medieval_main_menu_bg");
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
            bgImg.color = new Color(0.12f, 0.10f, 0.08f, 1f);

            GameObject darkBackdropObj = new GameObject("DarkBackdrop");
            darkBackdropObj.transform.SetParent(_tutorialModal.transform, false);
            RectTransform dbRt = darkBackdropObj.AddComponent<RectTransform>();
            dbRt.anchorMin = Vector2.zero;
            dbRt.anchorMax = Vector2.one;
            dbRt.offsetMin = Vector2.zero;
            dbRt.offsetMax = Vector2.zero;
            Image dbImg = darkBackdropObj.AddComponent<Image>();
            dbImg.color = new Color(0.02f, 0.02f, 0.03f, 0.88f);
            dbImg.raycastTarget = true;
            Button dbBtn = darkBackdropObj.AddComponent<Button>();
            dbBtn.onClick.AddListener(() => CloseModal(_tutorialModal));

            // 1. Fullscreen Epic Grimoire Card (1820x980 - Fits comfortably without clipping)
            GameObject cardObj = new GameObject("TutorialCard");
            cardObj.transform.SetParent(_tutorialModal.transform, false);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.GrimoireOpen) 
                          ?? ArcaneAssetLoader.GetGlassBoxSprite(1820, 980, 24f, ColGlassFill, ColCyanBorder, 2f);
            cardBg.type = Image.Type.Simple;
            cardBg.preserveAspect = false;
            cardBg.color = Color.white;
            cardBg.raycastTarget = true;

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1820f, 980f);

            // Clean Header & Royal Close Button [ Nút X Vàng Hoàng Gia ] (Top right of book, enlarged 72x72)
            GameObject tutCloseBtn = CreateRoyalCloseButton(cardObj.transform, new Vector2(-75f, -60f), () => CloseModal(_tutorialModal), 72f, "BtnCloseTutorial");

            // Sub-Tabs Header (4 tabs: Cơ bản, Luật chơi, Các quân cờ, AR - Enlarged Royal Gold Tabs)
            GameObject tabsHeader = new GameObject("TutorialTabs");
            tabsHeader.transform.SetParent(cardObj.transform, false);
            RectTransform thRt = tabsHeader.AddComponent<RectTransform>();
            thRt.anchorMin = new Vector2(0.5f, 0.5f);
            thRt.anchorMax = new Vector2(0.5f, 0.5f);
            thRt.pivot = new Vector2(0.5f, 0.5f);
            thRt.anchoredPosition = new Vector2(-101f, 353f);
            thRt.localScale = new Vector3(0.91f, 1f, 1f);
            thRt.sizeDelta = new Vector2(1520f, 76f);

            string[] tabLabels = {
                AegisLocalization.T("Cơ bản", "Basics"),
                AegisLocalization.T("Luật chơi", "Rules"),
                AegisLocalization.T("Các quân cờ", "Pieces"),
                "AR"
            };

            float tabSpacing = 370f;
            for (int i = 0; i < 4; i++)
            {
                int tabIdx = i;
                float x = (i - 1.5f) * tabSpacing;
                GameObject tabObj = new GameObject("Tab_" + i);
                tabObj.transform.SetParent(tabsHeader.transform, false);

                Image bg = tabObj.AddComponent<Image>();
                bg.sprite = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_inactive");
                bg.type = Image.Type.Sliced;
                _tutorialTabBgs[i] = bg;

                Button btn = tabObj.AddComponent<Button>();
                btn.onClick.AddListener(() => SwitchTutorialTab((TutorialTabType)tabIdx));
                tabObj.AddComponent<MenuButtonEffect>();

                RectTransform rt = tabObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(x, 0f);
                rt.sizeDelta = new Vector2(350f, 76f);

                Text tabTxt = CreateTextComp(tabObj.transform, tabLabels[i], 26, FontStyle.Bold, new Color(0.24f, 0.12f, 0.05f), font, new Vector2(0f, 4f), new Vector2(310f, 52f));
                tabTxt.resizeTextForBestFit = true;
                tabTxt.resizeTextMinSize = 22;
                tabTxt.resizeTextMaxSize = 28;
                MakeTextCrisp(tabTxt, new Color(1.0f, 0.95f, 0.82f, 0.7f));
                _tutorialTabTexts[i] = tabTxt;
            }

            // Left: 2D Tactical Diagram Container (Enlarged to 680x720)
            GameObject leftContainer = new GameObject("Left_DiagramContainer");
            leftContainer.transform.SetParent(cardObj.transform, false);
            RectTransform lcRt = leftContainer.AddComponent<RectTransform>();
            lcRt.anchorMin = new Vector2(0.5f, 0.5f);
            lcRt.anchorMax = new Vector2(0.5f, 0.5f);
            lcRt.pivot = new Vector2(0.5f, 0.5f);
            lcRt.anchoredPosition = new Vector2(-470f, -10f);
            lcRt.sizeDelta = new Vector2(680f, 720f);

            // Diagram Image (Enlarged to 590x590 for clear, magnificent visibility)
            GameObject boardObj = new GameObject("BoardDiagram");
            boardObj.transform.SetParent(leftContainer.transform, false);
            _tutorialDiagramImg = boardObj.AddComponent<Image>();
            _tutorialDiagramImg.preserveAspect = true;
            RectTransform bRt = boardObj.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(0f, 30f);
            bRt.sizeDelta = new Vector2(590f, 590f);

            // Piece / Overlay Container
            GameObject overlayObj = new GameObject("PieceOverlay");
            overlayObj.transform.SetParent(boardObj.transform, false);
            _tutorialPieceOverlay = overlayObj.transform;
            RectTransform oRt = overlayObj.AddComponent<RectTransform>();
            oRt.anchorMin = Vector2.zero;
            oRt.anchorMax = Vector2.one;
            oRt.offsetMin = Vector2.zero;
            oRt.offsetMax = Vector2.zero;

            // Caption Box & Text (Semi-transparent dark gold glass pill with crisp radiant gold text)
            GameObject capObj = new GameObject("DiagramCaption");
            capObj.transform.SetParent(leftContainer.transform, false);
            Image capBg = capObj.AddComponent<Image>();
            capBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(640, 56, 12f, new Color(0.12f, 0.08f, 0.04f, 0.90f), ColGoldBorder, 1.4f);
            capBg.type = Image.Type.Sliced;
            RectTransform capRt = capObj.GetComponent<RectTransform>();
            capRt.anchoredPosition = new Vector2(0f, -295f);
            capRt.sizeDelta = new Vector2(640f, 56f);

            GameObject capTxtObj = new GameObject("CaptionText");
            capTxtObj.transform.SetParent(capObj.transform, false);
            _tutorialCaptionTxt = capTxtObj.AddComponent<Text>();
            _tutorialCaptionTxt.font = font;
            _tutorialCaptionTxt.fontSize = 20;
            _tutorialCaptionTxt.fontStyle = FontStyle.Bold;
            _tutorialCaptionTxt.alignment = TextAnchor.MiddleCenter;
            _tutorialCaptionTxt.color = new Color(1.0f, 0.95f, 0.82f); // Radiant warm ivory gold
            _tutorialCaptionTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tutorialCaptionTxt.verticalOverflow = VerticalWrapMode.Truncate;
            _tutorialCaptionTxt.resizeTextForBestFit = true;
            _tutorialCaptionTxt.resizeTextMinSize = 14;
            _tutorialCaptionTxt.resizeTextMaxSize = 20;
            _tutorialCaptionTxt.lineSpacing = 1.15f;
            MakeTextCrisp(_tutorialCaptionTxt, new Color(0.10f, 0.05f, 0.02f, 0.95f));
            RectTransform ctxtRt = capTxtObj.GetComponent<RectTransform>();
            ctxtRt.anchorMin = Vector2.zero;
            ctxtRt.anchorMax = Vector2.one;
            ctxtRt.offsetMin = new Vector2(16f, 4f);
            ctxtRt.offsetMax = new Vector2(-16f, -4f);

            // Right: Guide Information & Steps (Shifted left to X=285 to balance margin and stay away from leather straps)
            GameObject rightContainer = new GameObject("Right_GuideContainer");
            rightContainer.transform.SetParent(cardObj.transform, false);
            RectTransform rcRt = rightContainer.AddComponent<RectTransform>();
            rcRt.anchorMin = new Vector2(0.5f, 0.5f);
            rcRt.anchorMax = new Vector2(0.5f, 0.5f);
            rcRt.pivot = new Vector2(0.5f, 0.5f);
            rcRt.anchoredPosition = new Vector2(243f, -24f);
            rcRt.sizeDelta = new Vector2(580f, 700f);

            // Badge / Category Indicator
            GameObject badgeObj = new GameObject("CategoryBadge");
            badgeObj.transform.SetParent(rightContainer.transform, false);
            _tutorialBadgeTxt = badgeObj.AddComponent<Text>();
            _tutorialBadgeTxt.font = font;
            _tutorialBadgeTxt.fontSize = 19;
            _tutorialBadgeTxt.fontStyle = FontStyle.Bold;
            _tutorialBadgeTxt.alignment = TextAnchor.MiddleLeft;
            _tutorialBadgeTxt.color = new Color(0.72f, 0.40f, 0.12f); // Royal bronze
            MakeTextCrisp(_tutorialBadgeTxt);
            RectTransform bdRt = badgeObj.GetComponent<RectTransform>();
            bdRt.anchoredPosition = new Vector2(0f, 310f);
            bdRt.sizeDelta = new Vector2(580f, 32f);

            // Main Title (Deep Imperial Brown, font 32 bold, readable, elegant)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(rightContainer.transform, false);
            _tutorialTitleTxt = titleObj.AddComponent<Text>();
            _tutorialTitleTxt.font = font;
            _tutorialTitleTxt.fontSize = 32;
            _tutorialTitleTxt.fontStyle = FontStyle.Bold;
            _tutorialTitleTxt.alignment = TextAnchor.MiddleLeft;
            _tutorialTitleTxt.color = new Color(0.20f, 0.10f, 0.04f); // Crisp Dark Chestnut
            MakeTextCrisp(_tutorialTitleTxt);
            RectTransform ttRt = titleObj.GetComponent<RectTransform>();
            ttRt.anchoredPosition = new Vector2(0f, 260f);
            ttRt.sizeDelta = new Vector2(580f, 52f);

            // Overview Paragraph (Font 19, clear scribe dark ink, cleanly wrapped on parchment)
            GameObject overviewObj = new GameObject("Overview");
            overviewObj.transform.SetParent(rightContainer.transform, false);
            _tutorialOverviewTxt = overviewObj.AddComponent<Text>();
            _tutorialOverviewTxt.font = font;
            _tutorialOverviewTxt.fontSize = 19;
            _tutorialOverviewTxt.fontStyle = FontStyle.Normal;
            _tutorialOverviewTxt.alignment = TextAnchor.UpperLeft;
            _tutorialOverviewTxt.color = new Color(0.20f, 0.12f, 0.06f);
            _tutorialOverviewTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tutorialOverviewTxt.verticalOverflow = VerticalWrapMode.Truncate;
            _tutorialOverviewTxt.resizeTextForBestFit = true;
            _tutorialOverviewTxt.resizeTextMinSize = 15;
            _tutorialOverviewTxt.resizeTextMaxSize = 19;
            _tutorialOverviewTxt.lineSpacing = 1.25f;
            MakeTextCrisp(_tutorialOverviewTxt);
            RectTransform ovRt = overviewObj.GetComponent<RectTransform>();
            ovRt.anchoredPosition = new Vector2(0f, 180f);
            ovRt.sizeDelta = new Vector2(580f, 95f);

            // Steps Guide Container (Non-overlapping layout: 105px step spacing)
            GameObject stepsGroup = new GameObject("StepsGroup");
            stepsGroup.transform.SetParent(rightContainer.transform, false);
            RectTransform sgRt = stepsGroup.AddComponent<RectTransform>();
            sgRt.anchoredPosition = new Vector2(0f, -40f);
            sgRt.sizeDelta = new Vector2(580f, 330f);

            for (int i = 0; i < 3; i++)
            {
                float y = 105f - i * 105f;
                GameObject row = new GameObject($"Step_{i + 1}");
                row.transform.SetParent(stepsGroup.transform, false);
                _tutorialStepRows[i] = row;
                RectTransform rRt = row.AddComponent<RectTransform>();
                rRt.anchoredPosition = new Vector2(0f, y);
                rRt.sizeDelta = new Vector2(580f, 90f);

                // Number badge (Antique gold seal, enlarged to 44x44)
                GameObject numObj = new GameObject("Badge");
                numObj.transform.SetParent(row.transform, false);
                Image nbImg = numObj.AddComponent<Image>();
                nbImg.sprite = ArcaneAssetLoader.GetCircleSprite(44, ColGoldBorder, new Color(0.35f, 0.22f, 0.10f), 1.5f);
                RectTransform nbRt = numObj.GetComponent<RectTransform>();
                nbRt.anchoredPosition = new Vector2(-260f, 0f);
                nbRt.sizeDelta = new Vector2(44f, 44f);

                _tutorialStepBadges[i] = CreateGrimoireTextComp(numObj.transform, (i + 1).ToString(), 20, FontStyle.Bold, new Color(1.0f, 0.95f, 0.80f), font, Vector2.zero, new Vector2(44f, 44f));

                // Step Title (Chestnut bold, font 21)
                _tutorialStepTitles[i] = CreateGrimoireTextComp(row.transform, "", 21, FontStyle.Bold, new Color(0.24f, 0.12f, 0.05f), font, new Vector2(28f, 18f), new Vector2(510f, 28f), TextAnchor.MiddleLeft);

                // Step Description (Scribe dark ink, wrapped cleanly, font 17)
                _tutorialStepDescs[i] = CreateGrimoireTextComp(row.transform, "", 17, FontStyle.Normal, new Color(0.22f, 0.14f, 0.08f), font, new Vector2(28f, -18f), new Vector2(510f, 48f), TextAnchor.MiddleLeft);
                _tutorialStepDescs[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                _tutorialStepDescs[i].verticalOverflow = VerticalWrapMode.Truncate;
                _tutorialStepDescs[i].resizeTextForBestFit = true;
                _tutorialStepDescs[i].resizeTextMinSize = 13;
                _tutorialStepDescs[i].resizeTextMaxSize = 17;
                _tutorialStepDescs[i].lineSpacing = 1.18f;
            }

            // Tactical Tip Box (Styled as royal parchment note with lightbulb banner, font 17)
            _tutorialTipContainer = new GameObject("TipContainer");
            _tutorialTipContainer.transform.SetParent(rightContainer.transform, false);
            Image tipBg = _tutorialTipContainer.AddComponent<Image>();
            tipBg.sprite = MedievalUIAssetLoader.GetSprite("banner_tip_full")
                          ?? ArcaneAssetLoader.GetGlassBoxSprite(620, 96, 8f, new Color(0.18f, 0.12f, 0.06f, 0.94f), ColGoldBorder, 1.4f);
            tipBg.type = Image.Type.Sliced;
            RectTransform tipRt = _tutorialTipContainer.GetComponent<RectTransform>();
            tipRt.anchoredPosition = new Vector2(-9f, -285f);
            tipRt.sizeDelta = new Vector2(620f, 96f);

            GameObject tipTxtObj = new GameObject("TipText");
            tipTxtObj.transform.SetParent(_tutorialTipContainer.transform, false);
            _tutorialTipTxt = tipTxtObj.AddComponent<Text>();
            _tutorialTipTxt.font = font;
            _tutorialTipTxt.fontSize = 17;
            _tutorialTipTxt.fontStyle = FontStyle.Bold;
            _tutorialTipTxt.alignment = TextAnchor.MiddleLeft;
            _tutorialTipTxt.color = new Color(0.24f, 0.12f, 0.05f); // Deep Antique Scribe Ink
            _tutorialTipTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _tutorialTipTxt.lineSpacing = 1.20f;
            MakeTextCrisp(_tutorialTipTxt, new Color(1.0f, 0.95f, 0.82f, 0.85f));
            RectTransform ttCompRt = tipTxtObj.GetComponent<RectTransform>();
            ttCompRt.anchorMin = Vector2.zero;
            ttCompRt.anchorMax = Vector2.one;
            ttCompRt.offsetMin = new Vector2(95f, 8f);
            ttCompRt.offsetMax = new Vector2(-25f, -8f);

            // Footer Navigation (Enlarged to 1200x60 at Y = -465)
            GameObject footer = new GameObject("FooterPagination");
            footer.transform.SetParent(cardObj.transform, false);
            RectTransform fRt = footer.AddComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0.5f, 0.5f);
            fRt.anchorMax = new Vector2(0.5f, 0.5f);
            fRt.pivot = new Vector2(0.5f, 0.5f);
            fRt.anchoredPosition = new Vector2(0f, -405f);
            fRt.sizeDelta = new Vector2(1200f, 60f);

            // Button Prev (Gothic Gold Nav Button as in User Ref 5: 230x60)
            GameObject prevBtnObj = new GameObject("BtnPrev");
            prevBtnObj.transform.SetParent(footer.transform, false);
            _tutorialBtnPrevBg = prevBtnObj.AddComponent<Image>();
            _tutorialBtnPrevBg.sprite = MedievalUIAssetLoader.GetSprite("btn_gothic_gold_blank") 
                                       ?? MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold");
            _tutorialBtnPrevBg.type = Image.Type.Simple;
            _tutorialBtnPrevBg.preserveAspect = true;
            _tutorialBtnPrev = prevBtnObj.AddComponent<Button>();
            _tutorialBtnPrev.onClick.AddListener(PreviousTutorialPage);
            prevBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform pRt = prevBtnObj.GetComponent<RectTransform>();
            pRt.anchoredPosition = new Vector2(-430f, 0f);
            pRt.sizeDelta = new Vector2(230f, 60f);
            _tutorialBtnPrevTxt = CreateTextComp(prevBtnObj.transform, AegisLocalization.T("⬅  Trở về", "⬅  Back"), 20, FontStyle.Bold, new Color(1.0f, 0.94f, 0.80f), font, Vector2.zero, new Vector2(210f, 48f));
            MakeTextCrisp(_tutorialBtnPrevTxt, new Color(0.10f, 0.05f, 0.02f, 0.95f));

            // Page Indicator (Golden parchment counter, font 24)
            GameObject indObj = new GameObject("PageIndicator");
            indObj.transform.SetParent(footer.transform, false);
            _tutorialPageIndicatorTxt = indObj.AddComponent<Text>();
            _tutorialPageIndicatorTxt.font = font;
            _tutorialPageIndicatorTxt.fontSize = 24;
            _tutorialPageIndicatorTxt.fontStyle = FontStyle.Bold;
            _tutorialPageIndicatorTxt.alignment = TextAnchor.MiddleCenter;
            _tutorialPageIndicatorTxt.color = new Color(0.96f, 0.90f, 0.75f);
            MakeTextCrisp(_tutorialPageIndicatorTxt);
            RectTransform indRt = indObj.GetComponent<RectTransform>();
            indRt.anchoredPosition = new Vector2(0f, 0f);
            indRt.sizeDelta = new Vector2(300f, 46f);

            // Button Next (Gothic Gold Nav Button as in User Ref 4: 230x60)
            GameObject nextBtnObj = new GameObject("BtnNext");
            nextBtnObj.transform.SetParent(footer.transform, false);
            _tutorialBtnNextBg = nextBtnObj.AddComponent<Image>();
            _tutorialBtnNextBg.sprite = MedievalUIAssetLoader.GetSprite("btn_gothic_gold_blank") 
                                       ?? MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold");
            _tutorialBtnNextBg.type = Image.Type.Simple;
            _tutorialBtnNextBg.preserveAspect = true;
            _tutorialBtnNext = nextBtnObj.AddComponent<Button>();
            _tutorialBtnNext.onClick.AddListener(NextTutorialPage);
            nextBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform nRt = nextBtnObj.GetComponent<RectTransform>();
            nRt.anchoredPosition = new Vector2(430f, 0f);
            nRt.sizeDelta = new Vector2(230f, 60f);
            _tutorialBtnNextTxt = CreateTextComp(nextBtnObj.transform, AegisLocalization.T("Tiếp tục  ➔", "Next  ➔"), 20, FontStyle.Bold, new Color(1.0f, 0.94f, 0.80f), font, Vector2.zero, new Vector2(210f, 48f));
            MakeTextCrisp(_tutorialBtnNextTxt, new Color(0.10f, 0.05f, 0.02f, 0.95f));

            // Ensure Royal Close Button is on top-most visual layer
            if (tutCloseBtn != null) tutCloseBtn.transform.SetAsLastSibling();

            // Initialize display
            SwitchTutorialTab(TutorialTabType.Basics);

            _tutorialModal.SetActive(false);
        }

        private void SwitchTutorialTab(TutorialTabType tab)
        {
            if (_selectedTutorialTab != tab) AegisAudioManager.PlayTabSwitch();
            _selectedTutorialTab = tab;
            _currentTutorialPage = 0;
            UpdateTutorialUI();
        }

        private void PreviousTutorialPage()
        {
            if (_currentTutorialPage > 0)
            {
                AegisAudioManager.PlayButtonClick();
                _currentTutorialPage--;
                UpdateTutorialUI();
            }
        }

        private void NextTutorialPage()
        {
            int total = TutorialRepository.GetPageCount(_selectedTutorialTab);
            if (_currentTutorialPage < total - 1)
            {
                AegisAudioManager.PlayButtonClick();
                _currentTutorialPage++;
                UpdateTutorialUI();
            }
            else
            {
                // Advance to next tab if available
                int nextTab = (int)_selectedTutorialTab + 1;
                if (nextTab < 4)
                {
                    AegisAudioManager.PlayTabSwitch();
                    ShowToast(AegisLocalization.T($"Chuyển sang mục: {((TutorialTabType)nextTab)}", $"Switched to: {((TutorialTabType)nextTab)}"), ToastType.Info);
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

            // 1. Update Tabs Highlight (Gold Ribbon style from User Reference 2)
            Sprite tabActiveSp = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_active");
            Sprite tabInactiveSp = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_inactive");

            for (int i = 0; i < 4; i++)
            {
                bool active = (i == (int)_selectedTutorialTab);
                if (_tutorialTabBgs[i] != null)
                {
                    _tutorialTabBgs[i].sprite = active ? tabActiveSp : tabInactiveSp;
                    _tutorialTabBgs[i].type = Image.Type.Sliced;
                }
                if (_tutorialTabTexts[i] != null)
                {
                    _tutorialTabTexts[i].color = active ? new Color(0.18f, 0.06f, 0.02f) : new Color(0.38f, 0.24f, 0.12f);
                    _tutorialTabTexts[i].fontStyle = FontStyle.Bold;
                    _tutorialTabTexts[i].fontSize = 26;
                    MakeTextCrisp(_tutorialTabTexts[i], active ? new Color(1.0f, 0.92f, 0.70f, 0.85f) : new Color(1.0f, 0.95f, 0.85f, 0.5f));
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
                bool hasStep = page.steps != null && i < page.steps.Count && !string.IsNullOrEmpty(page.steps[i].titleVi);
                _tutorialStepRows[i].SetActive(hasStep);
                if (hasStep)
                {
                    if (_tutorialStepTitles[i] != null) _tutorialStepTitles[i].text = page.steps[i].GetTitle();
                    if (_tutorialStepDescs[i] != null) _tutorialStepDescs[i].text = page.steps[i].GetDesc();
                }
            }

            // 5. Update Tactical Tip
            string tip = page.GetTip();
            bool hasTip = !string.IsNullOrEmpty(tip);
            if (_tutorialTipContainer != null)
            {
                _tutorialTipContainer.SetActive(hasTip);
                if (hasTip && _tutorialTipTxt != null) _tutorialTipTxt.text = tip;
            }

            // 6. Update Visual Diagram (Render at 590px high-resolution for clear tactical vision)
            if (_tutorialDiagramImg != null)
            {
                if (_selectedTutorialTab == TutorialTabType.AR)
                {
                    _tutorialDiagramImg.sprite = ArcaneAssetLoader.GetARDiagramSprite(590, page.arGraphicType);
                }
                else
                {
                    _tutorialDiagramImg.sprite = ArcaneAssetLoader.GetCustomBoardDiagram(590, 
                        page.hasSelectedSquare, page.selectedSquare, 
                        page.moveSquares, page.captureSquares, page.specialSquares,
                        page.pieces);
                }
            }

            // Clear old piece overlay markers (pieces are rendered with crisp anti-aliasing directly in the board diagram)
            if (_tutorialPieceOverlay != null)
            {
                for (int i = _tutorialPieceOverlay.childCount - 1; i >= 0; i--)
                {
                    Destroy(_tutorialPieceOverlay.GetChild(i).gameObject);
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
                    _tutorialBtnPrevBg.color = canPrev ? Color.white : new Color(0.55f, 0.50f, 0.45f, 0.4f);
                }
                if (_tutorialBtnPrevTxt != null)
                {
                    _tutorialBtnPrevTxt.color = canPrev ? new Color(1.0f, 0.94f, 0.80f) : new Color(0.60f, 0.55f, 0.45f, 0.5f);
                    _tutorialBtnPrevTxt.text = AegisLocalization.T("⬅  Trở về", "⬅  Back");
                }
            }

            if (_tutorialBtnNext != null && _tutorialBtnNextTxt != null)
            {
                bool isLastPage = (_currentTutorialPage == total - 1);
                bool hasNextTab = ((int)_selectedTutorialTab < 3);

                if (isLastPage && !hasNextTab)
                {
                    _tutorialBtnNextTxt.text = AegisLocalization.T("Hoàn thành  ✓", "Complete  ✓");
                }
                else if (isLastPage && hasNextTab)
                {
                    _tutorialBtnNextTxt.text = AegisLocalization.T("Mục tiếp  ➔➔", "Next Tab  ➔➔");
                }
                else
                {
                    _tutorialBtnNextTxt.text = AegisLocalization.T("Tiếp tục  ➔", "Next  ➔");
                }
                _tutorialBtnNextTxt.color = new Color(1.0f, 0.94f, 0.80f);
                if (_tutorialBtnNextBg != null) _tutorialBtnNextBg.color = Color.white;
            }
        }
        // =========================================================================
        // SCREEN 6.5: HÒM THƯ HOÀNG GIA (Royal Mailbox / Inbox Modal)
        // =========================================================================
        private void BuildMailboxModal(Transform parent, Font font)
        {
            _mailboxModal = CreateModalOverlay(parent, "Screen17_MailboxModal");

            GameObject cardObj = new GameObject("MailboxCard");
            cardObj.transform.SetParent(_mailboxModal.transform, false);

            Image cardBg = cardObj.AddComponent<Image>();
            Sprite cardSprite = MedievalUIAssetLoader.GetSprite("frame_shop_collection_1520")
                              ?? MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned);
            cardBg.sprite = cardSprite;
            cardBg.type = Image.Type.Simple;
            cardBg.preserveAspect = false;
            cardBg.color = Color.white;

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1260f, 780f);

            // Close Button [ Nút X Vàng Hoàng Gia ] (Top-Right)
            CreateRoyalCloseButton(cardObj.transform, new Vector2(-43f, -130f), () => CloseModal(_mailboxModal), 68f, "BtnClose");

            // Content Area for Mail Items (Scrollable Viewport & Content - Enlarged to embrace all mail items cleanly)
            GameObject mailScrollObj = new GameObject("MailScrollView");
            mailScrollObj.transform.SetParent(cardObj.transform, false);
            RectTransform msRt = mailScrollObj.AddComponent<RectTransform>();
            msRt.anchorMin = new Vector2(0.5f, 0.5f);
            msRt.anchorMax = new Vector2(0.5f, 0.5f);
            msRt.pivot = new Vector2(0.5f, 0.5f);
            msRt.anchoredPosition = new Vector2(-16f, -50f);
            msRt.sizeDelta = new Vector2(1030f, 540f);

            Image msImg = mailScrollObj.AddComponent<Image>();
            msImg.color = Color.clear;
            msImg.raycastTarget = true;
            mailScrollObj.AddComponent<RectMask2D>();

            ScrollRect sr = mailScrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 25f;

            GameObject mailContentObj = new GameObject("MailContent");
            mailContentObj.transform.SetParent(mailScrollObj.transform, false);
            RectTransform mcRt = mailContentObj.AddComponent<RectTransform>();
            mcRt.anchorMin = new Vector2(0f, 1f);
            mcRt.anchorMax = new Vector2(1f, 1f);
            mcRt.pivot = new Vector2(0.5f, 1f);
            mcRt.anchoredPosition = Vector2.zero;
            mcRt.sizeDelta = new Vector2(0f, 320f);

            VerticalLayoutGroup vlg = mailContentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(6, 6, 8, 8);

            ContentSizeFitter csf = mailContentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = mcRt;
            sr.viewport = msRt;

            // Royal Vertical Scrollbar (Carved Walnut Track & Embossed Gold Thumb)
            GameObject scrollbarObj = new GameObject("MailScrollbarVertical");
            scrollbarObj.transform.SetParent(cardObj.transform, false);
            RectTransform sbRt = scrollbarObj.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.5f, 0.5f);
            sbRt.anchorMax = new Vector2(0.5f, 0.5f);
            sbRt.pivot = new Vector2(0.5f, 0.5f);
            sbRt.anchoredPosition = new Vector2(485f, -60f);
            sbRt.sizeDelta = new Vector2(14f, 536f);

            Image trackImg = scrollbarObj.AddComponent<Image>();
            trackImg.sprite = MedievalUIAssetLoader.GetSprite("scrollbar_track_vertical");
            trackImg.type = Image.Type.Sliced;
            trackImg.color = Color.white;

            GameObject slidingArea = new GameObject("SlidingArea");
            slidingArea.transform.SetParent(scrollbarObj.transform, false);
            RectTransform saRt = slidingArea.AddComponent<RectTransform>();
            saRt.anchorMin = Vector2.zero;
            saRt.anchorMax = Vector2.one;
            saRt.offsetMin = new Vector2(1f, 4f);
            saRt.offsetMax = new Vector2(-1f, -4f);

            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(slidingArea.transform, false);
            RectTransform handleRt = handleObj.AddComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;

            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.sprite = MedievalUIAssetLoader.GetSprite("scrollbar_thumb_vertical");
            handleImg.type = Image.Type.Sliced;
            handleImg.color = Color.white;

            Scrollbar sb = scrollbarObj.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;
            sb.targetGraphic = handleImg;
            sb.handleRect = handleRt;

            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            // MAIL ITEM 1: Starter / Season Reward Mail
            CreateMailItem(mailContentObj.transform, font,
                AegisLocalization.T("PHẦN THƯỞNG KHỞI ĐẦU MÙA GIẢI", "SEASON STARTER REWARD"),
                AegisLocalization.T("Chào mừng chỉ huy gia nhập AegisMech AR! Nhận ngay 500 Vàng và 50 Kim Cương danh dự.", "Welcome Commander! Claim 500 Gold and 50 Gems to start your journey."),
                true,
                () =>
                {
                    AegisMech.UI.AegisPrefs.Coins += 500;
                    AegisMech.UI.AegisPrefs.Gems += 50;
                    UpdateUserProfileUI();
                    if (_mailBadgeObj != null) _mailBadgeObj.SetActive(false);
                    ShowToast(AegisLocalization.T("Đã nhận thành công 500 Vàng & 50 Kim Cương!", "Claimed 500 Gold & 50 Gems successfully!"), ToastType.Success);
                }, "icon_gold_coins");

            // MAIL ITEM 2: System Announcement Mail
            CreateMailItem(mailContentObj.transform, font,
                AegisLocalization.T("THÔNG BÁO HỆ THỐNG: MÙA GIẢI 1 ĐÃ MỞ", "SYSTEM NOTICE: SEASON 1 OPEN"),
                AegisLocalization.T("Đấu trường Online đa nền tảng và Đấu Máy AI đã hoàn tất chuẩn bị. Chúc bạn bách chiến bách thắng!", "Online Crossplay and AI Arenas are fully live. Glory awaits you on the 3D board!"),
                false,
                null, "icon_mail_envelope");

            // MAIL ITEM 3: Weekly Login Bonus
            CreateMailItem(mailContentObj.transform, font,
                AegisLocalization.T("QUÀ ĐĂNG NHẬP TUẦN ĐẦU TIÊN", "FIRST WEEK LOGIN BONUS"),
                AegisLocalization.T("Cảm ơn bạn đã đồng hành cùng AegisMech AR! Nhận ngay 300 Vàng để chuẩn bị cho trận chiến tiếp theo.", "Thank you for playing AegisMech AR! Claim 300 Gold to prepare for your next battle."),
                true,
                () =>
                {
                    AegisMech.UI.AegisPrefs.Coins += 300;
                    UpdateUserProfileUI();
                    ShowToast(AegisLocalization.T("Đã nhận thành công 300 Vàng!", "Claimed 300 Gold successfully!"), ToastType.Success);
                }, "icon_gold_coins");

            // MAIL ITEM 4: Tournament / Arena Announcement
            CreateMailItem(mailContentObj.transform, font,
                AegisLocalization.T("SỰ KIỆN: ĐẠI CHIẾN ĐẠI KIỆN TƯỚNG", "EVENT: GRANDMASTER SHOWDOWN"),
                AegisLocalization.T("Hoàn thành 5 trận thi đấu trực tuyến trong tuần để ghi danh vào bảng vàng và nhận danh hiệu Hiệp Sĩ Hoàng Gia.", "Complete 5 online matches this week to enter the leaderboard and earn the Royal Knight title."),
                false,
                null, "crest_royal_lion");

            // MAIL ITEM 5: Patch Notes / Visual Update
            CreateMailItem(mailContentObj.transform, font,
                AegisLocalization.T("BẢN CẬP NHẬT 2.0: GIAO DIỆN HOÀNG GIA", "VERSION 2.0: GOTHIC UI UPDATE"),
                AegisLocalization.T("Toàn bộ giao diện đã được nâng cấp sang phong cách Gothic Trung cổ cổ điển: Bàn cờ 3D, Hòm thư, Cửa hàng và Cài đặt.", "All interfaces upgraded to Medieval Gothic style: 3D Board, Mailbox, Shop, and Settings."),
                false,
                null, "icon_round_info");

            // Bottom Action Button: [ ⚔  ĐÓNG ] (Royal Wood Button)
            GameObject closeBtnBottom = new GameObject("BtnCloseBottom");
            closeBtnBottom.transform.SetParent(cardObj.transform, false);
            Image cbbImg = closeBtnBottom.AddComponent<Image>();
            cbbImg.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodSmall)
                          ?? MedievalUIAssetLoader.GetSprite("btn_wood_back");
            cbbImg.type = Image.Type.Sliced;
            Button cbbBtn = closeBtnBottom.AddComponent<Button>();
            cbbBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                CloseModal(_mailboxModal);
            });
            closeBtnBottom.AddComponent<MenuButtonEffect>();
            RectTransform cbbRt = closeBtnBottom.GetComponent<RectTransform>();
            cbbRt.anchorMin = new Vector2(0.5f, 0f);
            cbbRt.anchorMax = new Vector2(0.5f, 0f);
            cbbRt.pivot = new Vector2(0.5f, 0.5f);
            cbbRt.anchoredPosition = new Vector2(0f, 44f);
            cbbRt.sizeDelta = new Vector2(230f, 54f);

            GameObject cbbTxtObj = new GameObject("Txt");
            cbbTxtObj.transform.SetParent(closeBtnBottom.transform, false);
            Text cbbTxt = cbbTxtObj.AddComponent<Text>();
            cbbTxt.text = AegisLocalization.T("⚔ ĐÓNG", "⚔ CLOSE");
            cbbTxt.font = font;
            cbbTxt.fontSize = 22;
            cbbTxt.fontStyle = FontStyle.Bold;
            cbbTxt.alignment = TextAnchor.MiddleCenter;
            cbbTxt.color = new Color(0.96f, 0.90f, 0.78f, 1f);
            cbbTxt.raycastTarget = false;
            MakeTextCrisp(cbbTxt, new Color(0.10f, 0.05f, 0.02f, 0.95f));
            RectTransform cbbTxtRt = cbbTxtObj.GetComponent<RectTransform>();
            cbbTxtRt.anchorMin = Vector2.zero;
            cbbTxtRt.anchorMax = Vector2.one;
            cbbTxtRt.offsetMin = Vector2.zero;
            cbbTxtRt.offsetMax = Vector2.zero;

            _mailboxModal.SetActive(false);
        }

        private GameObject CreateMailItem(Transform parent, Font font, string title, string description, bool hasClaimableReward, Action onClaim, string customIcon = null)
        {
            GameObject itemObj = new GameObject("MailItem");
            itemObj.transform.SetParent(parent, false);

            LayoutElement le = itemObj.AddComponent<LayoutElement>();
            le.preferredHeight = 138f;
            le.minHeight = 138f;

            Image bg = itemObj.AddComponent<Image>();
            Sprite rowBg = MedievalUIAssetLoader.GetSprite("dropdown_parchment_box")
                        ?? MedievalUIAssetLoader.GetSprite("parchment_box_clean")
                        ?? ArcaneAssetLoader.GetGlassBoxSprite(1000, 138, 16f, new Color(0.96f, 0.92f, 0.84f, 0.95f), new Color(0.70f, 0.54f, 0.28f, 0.95f), 1.5f);
            bg.sprite = rowBg;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            RectTransform rt = itemObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1000f, 138f);

            // Icon on Left
            GameObject iconObj = new GameObject("MailIcon");
            iconObj.transform.SetParent(itemObj.transform, false);
            Image iconImg = iconObj.AddComponent<Image>();
            string iconName = customIcon ?? (hasClaimableReward ? "icon_gold_coins" : "icon_mail_envelope");
            iconImg.sprite = MedievalUIAssetLoader.GetSprite(iconName)
                          ?? MedievalUIAssetLoader.GetSprite("icon_mail_envelope")
                          ?? MedievalUIAssetLoader.GetSprite("icon_round_info");
            iconImg.preserveAspect = true;
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(22f, 0f);
            iconRt.sizeDelta = new Vector2(52f, 52f);

            // Title (Enlarged to 22pt, shifted down to -24f for vertical harmony)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(itemObj.transform, false);
            Text t = titleObj.AddComponent<Text>();
            t.text = title;
            t.font = font;
            t.fontSize = 22;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = new Color(0.20f, 0.10f, 0.04f, 1f); // Deep imperial brown ink
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(t, new Color(1f, 0.95f, 0.85f, 0.65f));
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.pivot = new Vector2(0f, 1f);
            trt.anchoredPosition = new Vector2(88f, -24f);
            trt.sizeDelta = new Vector2(-280f, 32f);

            // Description (Enlarged to 17pt, shifted down to -60f, high contrast clear text)
            GameObject descObj = new GameObject("Desc");
            descObj.transform.SetParent(itemObj.transform, false);
            Text dt = descObj.AddComponent<Text>();
            dt.text = description;
            dt.font = font;
            dt.fontSize = 17;
            dt.fontStyle = FontStyle.Normal;
            dt.alignment = TextAnchor.UpperLeft;
            dt.color = new Color(0.22f, 0.13f, 0.06f, 1f); // Classic walnut scribe text, clear and readable
            dt.lineSpacing = 1.22f;
            dt.horizontalOverflow = HorizontalWrapMode.Wrap;
            dt.verticalOverflow = VerticalWrapMode.Truncate;
            MakeTextCrisp(dt, new Color(1f, 0.96f, 0.88f, 0.50f));
            RectTransform drt = descObj.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(1f, 1f);
            drt.pivot = new Vector2(0f, 1f);
            drt.offsetMin = new Vector2(88f, 12f);
            drt.offsetMax = new Vector2(-195f, -60f);

            // Claim Reward Button or Status Tag
            if (hasClaimableReward)
            {
                GameObject claimBtnObj = new GameObject("BtnClaim");
                claimBtnObj.transform.SetParent(itemObj.transform, false);
                Image cImg = claimBtnObj.AddComponent<Image>();
                cImg.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion)
                           ?? MedievalUIAssetLoader.GetSprite("btn_hex_gold_radiant");
                cImg.type = Image.Type.Sliced;
                Button cBtn = claimBtnObj.AddComponent<Button>();
                claimBtnObj.AddComponent<MenuButtonEffect>();

                RectTransform crt = claimBtnObj.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(1f, 0.5f);
                crt.anchorMax = new Vector2(1f, 0.5f);
                crt.pivot = new Vector2(1f, 0.5f);
                crt.anchoredPosition = new Vector2(-18f, 0f);
                crt.sizeDelta = new Vector2(160f, 46f);

                GameObject cTxtObj = new GameObject("Text");
                cTxtObj.transform.SetParent(claimBtnObj.transform, false);
                Text ct = cTxtObj.AddComponent<Text>();
                ct.text = AegisLocalization.T("NHẬN NGAY", "CLAIM");
                ct.font = font;
                ct.fontSize = 15;
                ct.fontStyle = FontStyle.Bold;
                ct.alignment = TextAnchor.MiddleCenter;
                ct.color = new Color(0.22f, 0.10f, 0.04f, 1f);
                MakeTextCrisp(ct, new Color(1f, 0.92f, 0.65f, 0.85f));
                RectTransform ctrt = cTxtObj.GetComponent<RectTransform>();
                ctrt.anchorMin = Vector2.zero;
                ctrt.anchorMax = Vector2.one;
                ctrt.offsetMin = Vector2.zero;
                ctrt.offsetMax = Vector2.zero;

                cBtn.onClick.AddListener(() =>
                {
                    onClaim?.Invoke();
                    cBtn.interactable = false;
                    ct.text = AegisLocalization.T("✓ ĐÃ NHẬN", "✓ CLAIMED");
                    ct.color = new Color(0.35f, 0.35f, 0.35f);
                    cImg.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                });
            }
            else
            {
                GameObject statusObj = new GameObject("ReadStatus");
                statusObj.transform.SetParent(itemObj.transform, false);
                Image stBg = statusObj.AddComponent<Image>();
                stBg.sprite = MedievalUIAssetLoader.GetSprite("parchment_box_clean")
                           ?? ArcaneAssetLoader.GetGlassBoxSprite(120, 38, 10f, new Color(0.85f, 0.80f, 0.70f, 0.6f), new Color(0.60f, 0.50f, 0.38f, 0.6f), 1f);
                stBg.type = Image.Type.Sliced;
                RectTransform srt = statusObj.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(1f, 0.5f);
                srt.anchorMax = new Vector2(1f, 0.5f);
                srt.pivot = new Vector2(1f, 0.5f);
                srt.anchoredPosition = new Vector2(-18f, 0f);
                srt.sizeDelta = new Vector2(120f, 38f);

                GameObject stTxtObj = new GameObject("Txt");
                stTxtObj.transform.SetParent(statusObj.transform, false);
                Text st = stTxtObj.AddComponent<Text>();
                st.text = AegisLocalization.T("✓ ĐÃ ĐỌC", "✓ READ");
                st.font = font;
                st.fontSize = 14;
                st.fontStyle = FontStyle.Bold;
                st.alignment = TextAnchor.MiddleCenter;
                st.color = new Color(0.48f, 0.36f, 0.24f, 1f);
                MakeTextCrisp(st);
                RectTransform stTxtRt = stTxtObj.GetComponent<RectTransform>();
                stTxtRt.anchorMin = Vector2.zero;
                stTxtRt.anchorMax = Vector2.one;
                stTxtRt.offsetMin = Vector2.zero;
                stTxtRt.offsetMax = Vector2.zero;
            }

            return itemObj;
        }

        // =========================================================================
        // SCREEN 7: CỬA HÀNG (Shop Modal with 4 skins & gold currency counter)
        // =========================================================================
        private void BuildScreen7Shop(Transform parent, Font font)
        {
            _shopModal = CreateModalOverlay(parent, "Screen7_ShopModal");

            // Centered Main Window Card (Scaled up 10% to 1.1x -> 1672 x 902)
            GameObject cardObj = new GameObject("ShopCard");
            cardObj.transform.SetParent(_shopModal.transform, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1520f, 820f);
            cRt.localScale = new Vector3(1.10f, 1.10f, 1f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = MedievalUIAssetLoader.GetSprite("frame_shop_collection_1520")
                          ?? MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned);
            cardBg.type = Image.Type.Simple;
            cardBg.preserveAspect = false;
            cardBg.color = Color.white;

            // Window Header Bar (Height 60px, anchored safely below parchment crown arch)
            GameObject headerObj = new GameObject("ShopHeader");
            headerObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 1f);
            hRt.anchorMax = new Vector2(0.5f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(54f, -92f);
            hRt.sizeDelta = new Vector2(1420f, 60f);

            // Title: "CỬA HÀNG"
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.text = AegisLocalization.T("CỬA HÀNG", "SHOP");
            titleTxt.font = font;
            titleTxt.fontSize = 28;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = new Color(0.24f, 0.15f, 0.08f, 1f); // Deep Antique Walnut Scribe Ink
            titleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(titleTxt);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.5f);
            tRt.anchorMax = new Vector2(0f, 0.5f);
            tRt.pivot = new Vector2(0f, 0.5f);
            tRt.anchoredPosition = new Vector2(25f, -4f);
            tRt.sizeDelta = new Vector2(250f, 40f);

            // Gold Currency Badge in Header (Right, lowered safely into parchment)
            GameObject coinObj = new GameObject("CoinBadge");
            coinObj.transform.SetParent(headerObj.transform, false);
            Image cbImg = coinObj.AddComponent<Image>();
            Sprite goldBarSp = MedievalUIAssetLoader.GetSprite("counter_gold_capsule");
            if (goldBarSp != null)
            {
                cbImg.sprite = goldBarSp;
                cbImg.type = Image.Type.Simple;
                cbImg.preserveAspect = true;
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
            cbRt.anchoredPosition = new Vector2(-246f, -4f);
            cbRt.sizeDelta = new Vector2(152f, 52f);

            // Gold Text (Precisely centered on carved dark plate between coin icon and + button)
            GameObject goldTxtObj = new GameObject("GoldTxt", typeof(RectTransform));
            goldTxtObj.transform.SetParent(coinObj.transform, false);
            _shopGoldTxt = goldTxtObj.AddComponent<Text>();
            _shopGoldTxt.text = _currentGold.ToString("N0");
            _shopGoldTxt.font = font;
            _shopGoldTxt.fontSize = 18;
            _shopGoldTxt.fontStyle = FontStyle.Bold;
            _shopGoldTxt.alignment = TextAnchor.MiddleCenter;
            _shopGoldTxt.color = new Color(1.0f, 0.95f, 0.82f);
            _shopGoldTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _shopGoldTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_shopGoldTxt);
            RectTransform gtRt = goldTxtObj.GetComponent<RectTransform>();
            gtRt.anchorMin = new Vector2(0.5f, 0.5f);
            gtRt.anchorMax = new Vector2(0.5f, 0.5f);
            gtRt.pivot = new Vector2(0.5f, 0.5f);
            gtRt.anchoredPosition = new Vector2(9.5f, -0.5f);
            gtRt.sizeDelta = new Vector2(72f, 36f);

            // Gem / Diamond Currency Badge in Shop Header (Right, lowered safely into parchment)
            GameObject gemBadgeObj = new GameObject("GemBadge");
            gemBadgeObj.transform.SetParent(headerObj.transform, false);
            Image gbImg = gemBadgeObj.AddComponent<Image>();
            Sprite gemBarSp = MedievalUIAssetLoader.GetSprite("counter_gem_capsule");
            if (gemBarSp != null)
            {
                gbImg.sprite = gemBarSp;
                gbImg.type = Image.Type.Simple;
                gbImg.preserveAspect = true;
            }
            else
            {
                gbImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(140, 44, 14f, new Color(0.18f, 0.12f, 0.04f, 0.9f), ColGoldBorder, 1.5f);
                gbImg.type = Image.Type.Sliced;
            }
            RectTransform gbRt = gemBadgeObj.GetComponent<RectTransform>();
            gbRt.anchorMin = new Vector2(1f, 0.5f);
            gbRt.anchorMax = new Vector2(1f, 0.5f);
            gbRt.pivot = new Vector2(1f, 0.5f);
            gbRt.anchoredPosition = new Vector2(-82f, -4f);
            gbRt.sizeDelta = new Vector2(152f, 52f);

            GameObject gemTxtObj = new GameObject("GemTxt", typeof(RectTransform));
            gemTxtObj.transform.SetParent(gemBadgeObj.transform, false);
            _shopGemTxt = gemTxtObj.AddComponent<Text>();
            int curGems = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentGems : AegisMech.UI.AegisPrefs.Gems;
            _shopGemTxt.text = curGems.ToString("N0");
            _shopGemTxt.font = font;
            _shopGemTxt.fontSize = 18;
            _shopGemTxt.fontStyle = FontStyle.Bold;
            _shopGemTxt.alignment = TextAnchor.MiddleCenter;
            _shopGemTxt.color = new Color(1.0f, 0.96f, 0.88f);
            _shopGemTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _shopGemTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_shopGemTxt);
            RectTransform gemTxtRt = gemTxtObj.GetComponent<RectTransform>();
            gemTxtRt.anchorMin = new Vector2(0.5f, 0.5f);
            gemTxtRt.anchorMax = new Vector2(0.5f, 0.5f);
            gemTxtRt.pivot = new Vector2(0.5f, 0.5f);
            gemTxtRt.anchoredPosition = new Vector2(1.5f, 0f);
            gemTxtRt.sizeDelta = new Vector2(72f, 36f);

            // Close Button [ Crossed Swords Gold ] at Top-Right of ShopHeader (Enlarged 64x64)
            CreateRoyalCloseButton(headerObj.transform, new Vector2(-10f, 0f), () => CloseModal(_shopModal), 64f, "BtnCloseShop", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));

            // Category Sub-Tabs: Skin quân cờ (active), Bàn cờ, Hiệu ứng (Enlarged Royal Gold Tabs)
            GameObject shopTabs = new GameObject("ShopTabs");
            shopTabs.transform.SetParent(cardObj.transform, false);
            RectTransform stRt = shopTabs.AddComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.5f, 1f);
            stRt.anchorMax = new Vector2(0.5f, 1f);
            stRt.pivot = new Vector2(0.5f, 1f);
            stRt.anchoredPosition = new Vector2(0f, -160f);
            stRt.sizeDelta = new Vector2(1420f, 76f);

            string[] sTabs = {
                AegisLocalization.T("Skin quân cờ", "Piece Skins"),
                AegisLocalization.T("Bàn cờ", "Chessboards"),
                AegisLocalization.T("Hiệu ứng", "Effects")
            };
            Sprite tabActSprite = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_active")
                                ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_active")
                                ?? MedievalUIAssetLoader.GetSprite("btn_hex_gold_radiant")
                                ?? MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold");
            Sprite tabInactSprite = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_inactive")
                                  ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_inactive")
                                  ?? MedievalUIAssetLoader.GetSprite("btn_blank_black_banner");

            for (int i = 0; i < 3; i++)
            {
                int tabIdx = i;
                bool active = (i == _selectedShopTab);
                GameObject tObj = new GameObject("Tab_" + sTabs[i]);
                tObj.transform.SetParent(shopTabs.transform, false);

                Image bg = tObj.AddComponent<Image>();
                bg.sprite = active ? tabActSprite : tabInactSprite;
                bg.type = Image.Type.Sliced;
                _shopTabBgs[i] = bg;

                Button btn = tObj.AddComponent<Button>();
                btn.onClick.AddListener(() => SwitchShopTab(tabIdx));
                tObj.AddComponent<MenuButtonEffect>();

                RectTransform rt = tObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                float[] sTabX = { 84f, 430f, 744f };
                rt.anchoredPosition = new Vector2(sTabX[i], 0f);
                rt.sizeDelta = new Vector2(310f, 74f);

                GameObject txtObj = new GameObject("Txt");
                txtObj.transform.SetParent(tObj.transform, false);
                Text txtComp = txtObj.AddComponent<Text>();
                txtComp.text = sTabs[i];
                txtComp.font = font;
                txtComp.fontSize = 26;
                txtComp.fontStyle = FontStyle.Bold;
                txtComp.alignment = TextAnchor.MiddleCenter;
                txtComp.color = active ? new Color(0.18f, 0.08f, 0.02f, 1f) : new Color(0.38f, 0.24f, 0.12f, 1f);
                txtComp.horizontalOverflow = HorizontalWrapMode.Overflow;
                txtComp.verticalOverflow = VerticalWrapMode.Overflow;
                MakeTextCrisp(txtComp, active ? new Color(1f, 0.90f, 0.60f, 0.85f) : new Color(1f, 0.95f, 0.85f, 0.40f));
                _shopTabTexts[i] = txtComp;

                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = new Vector2(16f, 2f);
                txtRt.offsetMax = new Vector2(-16f, 0f);
            }

            // Category Horizontal ScrollViews (0: Skins, 1: Boards, 2: Effects)
            string[] skinNames = { "Knight of Light", "Dark Legion", "Arcane Paladin", "Forest Spirit" };
            string[] skinTextures = { "skin_knight_light.png", "skin_dark_legion.png", "skin_cyber_future.png", "skin_forest_spirit.png" };
            string[] frameSprites = { "shop_card_frame_default.png", "shop_card_frame_crimson.png", "shop_card_frame_cyan.png", "shop_card_frame_default.png" };
            int[] skinPricesGold = { 1000, 1200, 1500, 1500 };
            int[] skinPricesGems = { 100, 120, 150, 150 };

            _shopCategoryContainers[0] = CreateShopCategoryScrollView(cardObj.transform, "Category_Skins", 0, skinNames, skinTextures, frameSprites, skinPricesGold, skinPricesGems, font);

            string[] boardNames = {
                AegisLocalization.T("Bàn Cờ Hoàng Gia Cổ Điển", "Royal Antique Board"),
                AegisLocalization.T("Bàn Cờ Cổ Điển", "Classic Board"),
                AegisLocalization.T("Bàn Cờ Dung Nham", "Magma Board"),
                AegisLocalization.T("Bàn Cờ Pha Lê", "Crystal Board")
            };
            int[] boardPricesGold = { 800, 500, 1200, 1600 };
            int[] boardPricesGems = { 80, 50, 120, 160 };

            _shopCategoryContainers[1] = CreateShopCategoryScrollView(cardObj.transform, "Category_Boards", 1, boardNames, skinTextures, frameSprites, boardPricesGold, boardPricesGems, font);
            _shopCategoryContainers[1].SetActive(false);

            string[] effectNames = {
                AegisLocalization.T("Sấm Sét Arcane", "Arcane Lightning"),
                AegisLocalization.T("Hào Quang Thánh", "Divine Aura"),
                AegisLocalization.T("Lửa Hỏa Ngục", "Hellfire Blaze"),
                AegisLocalization.T("Bụi Sao Vũ Trụ", "Cosmic Stardust")
            };
            int[] effectPricesGold = { 600, 900, 1100, 1400 };
            int[] effectPricesGems = { 60, 90, 110, 140 };
            string[] effectTextures = {
                skinTextures[2],
                skinTextures[3],
                skinTextures[0],
                skinTextures[1]
            };

            _shopCategoryContainers[2] = CreateShopCategoryScrollView(cardObj.transform, "Category_Effects", 2, effectNames, effectTextures, frameSprites, effectPricesGold, effectPricesGems, font);
            _shopCategoryContainers[2].SetActive(false);

            // Navigation Left Chevron [ ◀ ] - Large Ornate Bronze/Gold Disc
            _shopChevronLeft = new GameObject("ShopChevronLeft");
            _shopChevronLeft.transform.SetParent(cardObj.transform, false);
            Image lChvImg = _shopChevronLeft.AddComponent<Image>();
            lChvImg.sprite = MedievalUIAssetLoader.GetSprite("btn_round_chevron_left");
            lChvImg.type = Image.Type.Simple;
            lChvImg.preserveAspect = true;
            Button lChvBtn = _shopChevronLeft.AddComponent<Button>();
            lChvBtn.onClick.AddListener(() => SmoothScrollShopDelta(-0.50f));
            _shopChevronLeft.AddComponent<MenuButtonEffect>();
            RectTransform lChvRt = _shopChevronLeft.GetComponent<RectTransform>();
            lChvRt.anchorMin = new Vector2(0.5f, 0.5f);
            lChvRt.anchorMax = new Vector2(0.5f, 0.5f);
            lChvRt.pivot = new Vector2(0.5f, 0.5f);
            lChvRt.anchoredPosition = new Vector2(-710f, -80f);
            lChvRt.sizeDelta = new Vector2(64f, 64f);
            _shopChevronLeft.transform.SetAsLastSibling();

            // Navigation Right Chevron [ ▶ ] - Large Ornate Bronze/Gold Disc
            _shopChevronRight = new GameObject("ShopChevronRight");
            _shopChevronRight.transform.SetParent(cardObj.transform, false);
            Image rChvImg = _shopChevronRight.AddComponent<Image>();
            rChvImg.sprite = MedievalUIAssetLoader.GetSprite("btn_round_chevron_right");
            rChvImg.type = Image.Type.Simple;
            rChvImg.preserveAspect = true;
            Button rChvBtn = _shopChevronRight.AddComponent<Button>();
            rChvBtn.onClick.AddListener(() => SmoothScrollShopDelta(0.50f));
            _shopChevronRight.AddComponent<MenuButtonEffect>();
            RectTransform rChvRt = _shopChevronRight.GetComponent<RectTransform>();
            rChvRt.anchorMin = new Vector2(0.5f, 0.5f);
            rChvRt.anchorMax = new Vector2(0.5f, 0.5f);
            rChvRt.pivot = new Vector2(0.5f, 0.5f);
            rChvRt.anchoredPosition = new Vector2(710f, -80f);
            rChvRt.sizeDelta = new Vector2(64f, 64f);
            _shopChevronRight.transform.SetAsLastSibling();

            _shopModal.SetActive(false);
        }

        private void SwitchShopTab(int tabIndex)
        {
            if (_selectedShopTab != tabIndex) AegisAudioManager.PlayTabSwitch();
            _selectedShopTab = tabIndex;
            if (_shopScrollCoroutine != null)
            {
                StopCoroutine(_shopScrollCoroutine);
                _shopScrollCoroutine = null;
            }
            if (tabIndex >= 0 && tabIndex < _shopCategoryScrollRects.Length && _shopCategoryScrollRects[tabIndex] != null)
            {
                _shopCategoryScrollRects[tabIndex].horizontalNormalizedPosition = 0f;
            }

            Sprite activeSprite = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_active")
                                ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_active")
                                ?? MedievalUIAssetLoader.GetSprite("btn_hex_gold_radiant")
                                ?? MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold");
            Sprite inactiveSprite = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_inactive")
                                  ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_inactive")
                                  ?? MedievalUIAssetLoader.GetSprite("btn_blank_black_banner");

            for (int i = 0; i < _shopTabBgs.Length; i++)
            {
                bool active = (i == tabIndex);
                if (_shopTabBgs[i] != null)
                {
                    _shopTabBgs[i].sprite = active ? activeSprite : inactiveSprite;
                    _shopTabBgs[i].type = Image.Type.Sliced;
                }
                if (_shopTabTexts[i] != null)
                {
                    _shopTabTexts[i].color = active ? new Color(0.18f, 0.08f, 0.02f, 1f) : new Color(0.38f, 0.24f, 0.12f, 1f);
                    _shopTabTexts[i].fontStyle = FontStyle.Bold;
                    _shopTabTexts[i].fontSize = 26;
                    MakeTextCrisp(_shopTabTexts[i], active ? new Color(1f, 0.90f, 0.60f, 0.85f) : new Color(1f, 0.95f, 0.85f, 0.40f));
                }
                if (_shopCategoryContainers[i] != null)
                {
                    _shopCategoryContainers[i].SetActive(active);
                }
            }
        }

        public void SmoothScrollShopDelta(float delta)
        {
            int tab = _selectedShopTab;
            if (tab < 0 || tab >= _shopCategoryScrollRects.Length) return;
            ScrollRect sr = _shopCategoryScrollRects[tab];
            if (sr == null) return;
            float current = sr.horizontalNormalizedPosition;
            SmoothScrollShop(Mathf.Clamp01(current + delta));
        }

        public void SmoothScrollShop(float targetPos)
        {
            if (_shopScrollCoroutine != null) StopCoroutine(_shopScrollCoroutine);
            _shopScrollCoroutine = StartCoroutine(DoSmoothScrollShop(targetPos));
        }

        private IEnumerator DoSmoothScrollShop(float targetPos)
        {
            int tab = _selectedShopTab;
            if (tab < 0 || tab >= _shopCategoryScrollRects.Length) yield break;
            ScrollRect sr = _shopCategoryScrollRects[tab];
            if (sr == null) yield break;
            AegisAudioManager.PlayTabSwitch();
            float startPos = sr.horizontalNormalizedPosition;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                sr.horizontalNormalizedPosition = Mathf.Lerp(startPos, targetPos, ease);
                yield return null;
            }
            sr.horizontalNormalizedPosition = targetPos;
            _shopScrollCoroutine = null;
        }

        private GameObject CreateShopCategoryScrollView(Transform parent, string name, int categoryIndex, string[] itemNames, string[] itemTextures, string[] frameSprites, int[] pricesGold, int[] pricesGems, Font font)
        {
            GameObject scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            RectTransform svRt = scrollObj.AddComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.5f, 0.5f);
            svRt.anchorMax = new Vector2(0.5f, 0.5f);
            svRt.pivot = new Vector2(0.5f, 0.5f);
            svRt.anchoredPosition = new Vector2(0f, -80f);
            svRt.sizeDelta = new Vector2(1380f, 510f);

            Image svImg = scrollObj.AddComponent<Image>();
            svImg.color = Color.clear;
            svImg.raycastTarget = true;

            scrollObj.AddComponent<RectMask2D>();

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = true;
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.elasticity = 0.12f;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;
            sr.scrollSensitivity = 35f;
            _shopCategoryScrollRects[categoryIndex] = sr;

            GameObject contentObj = new GameObject("ScrollContent");
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform ctRt = contentObj.AddComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 0.5f);
            ctRt.anchorMax = new Vector2(0f, 0.5f);
            ctRt.pivot = new Vector2(0f, 0.5f);
            ctRt.anchoredPosition = Vector2.zero;

            float cardW = 428f;
            float cardGap = 32f;
            float padLeft = 15f;
            int count = itemNames.Length;
            float totalW = count * cardW + (count - 1) * cardGap + padLeft * 2f;
            ctRt.sizeDelta = new Vector2(totalW, 540f);

            sr.content = ctRt;
            sr.viewport = svRt;

            for (int i = 0; i < count; i++)
            {
                Vector2 pos = new Vector2(padLeft + i * (cardW + cardGap), 0f);
                CreateShopSkinCard(contentObj.transform, itemNames[i], itemTextures[i], frameSprites[i], pricesGold[i], pricesGems[i], pos, font);
            }

            return scrollObj;
        }

        private void CreateShopSkinCard(Transform parent, string name, string textureName, string frameSpriteName, int priceGold, int priceGems, Vector2 pos, Font font)
        {
            string cleanId = "item_" + System.Text.RegularExpressions.Regex.Replace(name.ToLower(), @"[^a-z0-9]", "_");
            bool isOwned = AegisMech.UI.AegisPrefs.Owns(cleanId);

            GameObject card = new GameObject("Skin_" + name);
            card.transform.SetParent(parent, false);

            Image bg = card.AddComponent<Image>();
            // Use authentic royal parchment card frame (filigree gold corners & rim)
            Sprite frameSp = MedievalUIAssetLoader.GetSprite("frame_parchment_tall");
            if (frameSp != null)
            {
                bg.sprite = frameSp;
                bg.type = Image.Type.Sliced;
            }
            else
            {
                bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(330, 480, 20f, new Color(0.18f, 0.12f, 0.05f, 0.95f), ColGoldBorder, 1.5f);
                bg.type = Image.Type.Sliced;
            }

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(428f, 516f);

            // Skin Artwork Image - fills upper portion of card
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
            irt.sizeDelta = new Vector2(398f, 308f);

            // Title (Deep Walnut Scribe Ink, bold, crisp on royal parchment)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(card.transform, false);
            Text titleComp = titleObj.AddComponent<Text>();
            titleComp.text = name;
            titleComp.font = font;
            titleComp.fontSize = 25;
            titleComp.fontStyle = FontStyle.Bold;
            titleComp.alignment = TextAnchor.MiddleCenter;
            titleComp.color = new Color(0.24f, 0.15f, 0.08f, 1f);
            titleComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            titleComp.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(titleComp);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0f);
            tRt.anchorMax = new Vector2(0.5f, 0f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.anchoredPosition = new Vector2(0f, 134f);
            tRt.sizeDelta = new Vector2(390f, 36f);

            // Button containers
            GameObject ownedBtnObj = new GameObject("BtnOwned");
            ownedBtnObj.transform.SetParent(card.transform, false);
            Image ownedImg = ownedBtnObj.AddComponent<Image>();
            ownedImg.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion)
                            ?? MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold");
            ownedImg.type = Image.Type.Sliced;
            RectTransform ownedRt = ownedBtnObj.GetComponent<RectTransform>();
            ownedRt.anchorMin = new Vector2(0.5f, 0f);
            ownedRt.anchorMax = new Vector2(0.5f, 0f);
            ownedRt.pivot = new Vector2(0.5f, 0.5f);
            ownedRt.anchoredPosition = new Vector2(0f, 50f);
            ownedRt.sizeDelta = new Vector2(360f, 52f);

            GameObject ownedTxtObj = new GameObject("OwnedTxt");
            ownedTxtObj.transform.SetParent(ownedBtnObj.transform, false);
            Text ownedTxt = ownedTxtObj.AddComponent<Text>();
            ownedTxt.text = AegisLocalization.T("✓ ĐÃ SỞ HỮU", "✓ OWNED");
            ownedTxt.font = font;
            ownedTxt.fontSize = 22;
            ownedTxt.fontStyle = FontStyle.Bold;
            ownedTxt.alignment = TextAnchor.MiddleCenter;
            ownedTxt.color = new Color(0.24f, 0.12f, 0.05f); // Deep imperial brown on bright gold banner
            MakeTextCrisp(ownedTxt);
            RectTransform otRt = ownedTxtObj.GetComponent<RectTransform>();
            otRt.anchorMin = Vector2.zero;
            otRt.anchorMax = Vector2.one;
            otRt.offsetMin = Vector2.zero;
            otRt.offsetMax = Vector2.zero;

            Button ownedBtn = ownedBtnObj.AddComponent<Button>();
            ownedBtn.onClick.AddListener(() =>
            {
                PlayerPrefs.SetString(PrefsEquippedSkin, name);
                PlayerPrefs.Save();
                ShowToast(AegisLocalization.T($"Đã trang bị '{name}'!", $"Equipped '{name}'!"), ToastType.Success);
            });
            ownedBtnObj.AddComponent<MenuButtonEffect>();

            // Dual Buy Buttons: 1 for Gold, 1 for Gems (using authentic capsule art with baked-in icons)
            GameObject buyGoldObj = new GameObject("BtnBuyGold");
            buyGoldObj.transform.SetParent(card.transform, false);
            Image bgImg = buyGoldObj.AddComponent<Image>();
            Sprite goldCapSp = MedievalUIAssetLoader.GetSprite("counter_gold_capsule")
                            ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold);
            bgImg.sprite = goldCapSp;
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = true;
            RectTransform bgRt = buyGoldObj.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0f);
            bgRt.anchorMax = new Vector2(0.5f, 0f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = new Vector2(-96f, 50f);
            bgRt.sizeDelta = new Vector2(182f, 52f);

            // Note: counter_gold_capsule already features the embossed coin on the left and '+' on the right; no duplicate icon overlay
            GameObject goldTxtObj = new GameObject("GoldTxt");
            goldTxtObj.transform.SetParent(buyGoldObj.transform, false);
            Text goldTxt = goldTxtObj.AddComponent<Text>();
            goldTxt.text = priceGold.ToString("N0");
            goldTxt.font = font;
            goldTxt.fontSize = 20;
            goldTxt.fontStyle = FontStyle.Bold;
            goldTxt.alignment = TextAnchor.MiddleCenter;
            goldTxt.color = new Color(1f, 0.94f, 0.65f); // Warm gold on dark capsule
            MakeTextCrisp(goldTxt);
            RectTransform gtRt = goldTxtObj.GetComponent<RectTransform>();
            gtRt.anchorMin = new Vector2(0.5f, 0.5f);
            gtRt.anchorMax = new Vector2(0.5f, 0.5f);
            gtRt.pivot = new Vector2(0.5f, 0.5f);
            gtRt.anchoredPosition = new Vector2(9.5f, -0.5f);
            gtRt.sizeDelta = new Vector2(85f, 36f);

            Button buyGoldBtn = buyGoldObj.AddComponent<Button>();
            buyGoldObj.AddComponent<MenuButtonEffect>();

            // Gem Buy Button (Royal Sapphire Gem Capsule with baked-in gem icon)
            GameObject buyGemObj = new GameObject("BtnBuyGem");
            buyGemObj.transform.SetParent(card.transform, false);
            Image gemBgImg = buyGemObj.AddComponent<Image>();
            Sprite gemCapSp = MedievalUIAssetLoader.GetSprite("counter_gem_capsule")
                           ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold);
            gemBgImg.sprite = gemCapSp;
            gemBgImg.type = Image.Type.Simple;
            gemBgImg.preserveAspect = true;
            RectTransform gemBgRt = buyGemObj.GetComponent<RectTransform>();
            gemBgRt.anchorMin = new Vector2(0.5f, 0f);
            gemBgRt.anchorMax = new Vector2(0.5f, 0f);
            gemBgRt.pivot = new Vector2(0.5f, 0.5f);
            gemBgRt.anchoredPosition = new Vector2(96f, 50f);
            gemBgRt.sizeDelta = new Vector2(182f, 52f);

            // Note: counter_gem_capsule already features the sapphire gem on the left and '+' on the right; no duplicate icon overlay
            GameObject gemTxtObj = new GameObject("GemTxt");
            gemTxtObj.transform.SetParent(buyGemObj.transform, false);
            Text gemTxt = gemTxtObj.AddComponent<Text>();
            gemTxt.text = priceGems.ToString("N0");
            gemTxt.font = font;
            gemTxt.fontSize = 20;
            gemTxt.fontStyle = FontStyle.Bold;
            gemTxt.alignment = TextAnchor.MiddleCenter;
            gemTxt.color = new Color(1f, 0.96f, 0.88f);
            MakeTextCrisp(gemTxt);
            RectTransform gmtRt = gemTxtObj.GetComponent<RectTransform>();
            gmtRt.anchorMin = new Vector2(0.5f, 0.5f);
            gmtRt.anchorMax = new Vector2(0.5f, 0.5f);
            gmtRt.pivot = new Vector2(0.5f, 0.5f);
            gmtRt.anchoredPosition = new Vector2(1.5f, -0.5f);
            gmtRt.sizeDelta = new Vector2(85f, 36f);

            Button buyGemBtn = buyGemObj.AddComponent<Button>();
            buyGemObj.AddComponent<MenuButtonEffect>();

            // Setup state
            ownedBtnObj.SetActive(isOwned);
            buyGoldObj.SetActive(!isOwned);
            buyGemObj.SetActive(!isOwned);

            // Buy with Gold handler
            buyGoldBtn.onClick.AddListener(() =>
            {
                int currentGold = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentGold : AegisMech.UI.AegisPrefs.Coins;
                if (currentGold < priceGold)
                {
                    ShowToast(AegisLocalization.T($"Không đủ vàng! Cần {priceGold:N0} vàng (bạn có {currentGold:N0}).", $"Not enough gold! Need {priceGold:N0} gold (you have {currentGold:N0})."), ToastType.Error);
                    return;
                }

                bool spent = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.SpendGold(priceGold) : true;
                if (spent)
                {
                    AegisMech.UI.AegisPrefs.Coins = Mathf.Max(0, AegisMech.UI.AegisPrefs.Coins - priceGold);
                    AegisMech.UI.AegisPrefs.Own(cleanId);
                    ownedBtnObj.SetActive(true);
                    buyGoldObj.SetActive(false);
                    buyGemObj.SetActive(false);
                    UpdateUserProfileUI();
                    ShowToast(AegisLocalization.T($"Mua thành công: '{name}' bằng {priceGold:N0} Vàng!", $"Successfully purchased: '{name}' for {priceGold:N0} Gold!"), ToastType.Success);
                }
            });

            // Buy with Gems handler
            buyGemBtn.onClick.AddListener(() =>
            {
                int currentGems = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentGems : AegisMech.UI.AegisPrefs.Gems;
                if (currentGems < priceGems)
                {
                    ShowToast(AegisLocalization.T($"Không đủ kim cương! Cần {priceGems:N0} kim cương (bạn có {currentGems:N0}).", $"Not enough gems! Need {priceGems:N0} gems (you have {currentGems:N0})."), ToastType.Error);
                    return;
                }

                bool spent = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.SpendGems(priceGems) : true;
                if (spent)
                {
                    AegisMech.UI.AegisPrefs.Gems = Mathf.Max(0, AegisMech.UI.AegisPrefs.Gems - priceGems);
                    AegisMech.UI.AegisPrefs.Own(cleanId);
                    ownedBtnObj.SetActive(true);
                    buyGoldObj.SetActive(false);
                    buyGemObj.SetActive(false);
                    UpdateUserProfileUI();
                    ShowToast(AegisLocalization.T($"Mua thành công: '{name}' bằng {priceGems:N0} Kim Cương!", $"Successfully purchased: '{name}' for {priceGems:N0} Gems!"), ToastType.Success);
                }
            });
        }

        // =========================================================================
        // SCREEN 8: BỘ SƯU TẬP (Collection Modal with 4 categories & Armory Inspector)
        // =========================================================================
        private void InitializeCollectionData()
        {
            _allCollectionItems.Clear();

            Color colMythic = new Color(0.96f, 0.80f, 0.32f);    // Radiant Gold
            Color colLegendary = new Color(0.24f, 0.88f, 1.0f);   // Neon Cyan
            Color colEpic = new Color(0.82f, 0.40f, 0.98f);      // Hextech Violet
            Color colRare = new Color(0.38f, 0.70f, 1.0f);       // Celestial Blue

            // Category 0: Quân Cờ Mech (Pieces) - 6 items
            _allCollectionItems.Add(new CollectionItemData
            {
                id = "piece_king",
                nameVi = "Vua Sovereign",
                nameEn = "Sovereign King",
                category = 0,
                rarity = "THẦN THOẠI",
                rarityColor = colMythic,
                typeVi = "Quân Cờ Lãnh Đạo Tối Cao",
                typeEn = "Supreme Commander Mech",
                descVi = "Trái tim tối cao của vương triều Aegis, được bảo vệ bởi khiên từ trường Nano và lò phản ứng hạt nhân lượng tử.",
                descEn = "Supreme heart of the Aegis dynasty, shielded by Nano-magnetic barriers and a quantum core.",
                perksVi = "⚡ Thần uy tối thượng • Hộ vệ toàn quân • Di chuyển 1 ô mọi hướng",
                perksEn = "⚡ Supreme majesty • Guards all allies • Moves 1 square in all directions",
                pieceType = PieceType.King,
                isOwned = true,
                prefsKey = null
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "piece_queen",
                nameVi = "Hậu Empress",
                nameEn = "Empress Queen",
                category = 0,
                rarity = "HUYỀN THOẠI",
                rarityColor = colLegendary,
                typeVi = "Quân Cờ Đột Kích Tối Thượng",
                typeEn = "Supreme Assault Mech",
                descVi = "Cỗ máy chiến tranh uy lực nhất chiến trường, trang bị cánh phản lực siêu thanh và song kiếm plasma tích điện.",
                descEn = "Most lethal war machine on the board, equipped with hypersonic thrusters and dual charged plasma blades.",
                perksVi = "⚡ Cơ động vô song • Quét ngang dọc chéo không giới hạn",
                perksEn = "⚡ Unrivaled mobility • Controls ranks, files & diagonals without limit",
                pieceType = PieceType.Queen,
                isOwned = true,
                prefsKey = null
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "piece_rook",
                nameVi = "Xe Fortress",
                nameEn = "Siege Fortress",
                category = 0,
                rarity = "SỬ THI",
                rarityColor = colEpic,
                typeVi = "Quân Cờ Công Thành Hạng Nặng",
                typeEn = "Heavy Siege Bastion",
                descVi = "Pháo đài bọc thép hạng nặng mang giáp titan đa tầng, húc văng mọi phòng tuyến thẳng hàng của đối phương.",
                descEn = "Heavy armored bastion with multi-layered titanium armor, smashing through any straight enemy defense.",
                perksVi = "🛡️ Giáp phản ứng nổ • Quét đường thẳng cực hạn • Hỗ trợ nhập thành",
                perksEn = "🛡️ Reactive armor • Unlimited straight-line rush • Castling support",
                pieceType = PieceType.Rook,
                isOwned = true,
                prefsKey = null
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "piece_bishop",
                nameVi = "Tượng Sniper",
                nameEn = "Arcane Sniper",
                category = 0,
                rarity = "SỬ THI",
                rarityColor = colEpic,
                typeVi = "Quân Cờ Pháo Xạ Tầm Xa",
                typeEn = "Long-Range Railgun Mech",
                descVi = "Hệ thống pháo từ trường ngắm bắn tầm xa, kiểm soát các đường chéo chiến thuật và khống chế góc khuất.",
                descEn = "Long-range electromagnetic railgun, locking down tactical diagonals and suppressing blind spots.",
                perksVi = "🎯 Bắn tỉa đường chéo • Vận tốc đầu nòng Mach 7 • Xuyên thấu ma trận",
                perksEn = "🎯 Diagonal sniping • Mach 7 muzzle velocity • Matrix penetration",
                pieceType = PieceType.Bishop,
                isOwned = true,
                prefsKey = null
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "piece_knight",
                nameVi = "Mã Striker",
                nameEn = "Assault Striker",
                category = 0,
                rarity = "HUYỀN THOẠI",
                rarityColor = colLegendary,
                typeVi = "Quân Cờ Xung Kích Phản Trọng Lực",
                typeEn = "Anti-Gravity Striker Mech",
                descVi = "Kỵ binh xung kích trang bị động cơ phản trọng lực, có khả năng nhảy vượt qua đầu các cỗ máy khác để tập kích hiểm hóc.",
                descEn = "Shock cavalry equipped with anti-gravity thrusters, leaping over obstacles and striking behind enemy lines.",
                perksVi = "🚀 Nhảy vượt chướng ngại • Đòn đánh chữ L bất ngờ • Tập kích hiểm",
                perksEn = "🚀 Obstacle leaping • Unpredictable L-step assault • Ambush master",
                pieceType = PieceType.Knight,
                isOwned = true,
                prefsKey = null
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "piece_pawn",
                nameVi = "Tốt Vanguard",
                nameEn = "Aegis Vanguard",
                category = 0,
                rarity = "TINH ANH",
                rarityColor = colRare,
                typeVi = "Quân Cờ Tiên Phong Bọc Thép",
                typeEn = "Frontline Armored Vanguard",
                descVi = "Chiến binh tiên phong quả cảm giữ vững phòng tuyến. Khi tiến đến bờ cuối chiến trường sẽ thăng cấp Mecha tối thượng.",
                descEn = "Brave vanguard holding the frontline. Reaching the end rank ascends into supreme Mecha forms.",
                perksVi = "⚔️ Tiên phong mở đường • Bước tiến kép nước đầu • Thăng cấp tối thượng",
                perksEn = "⚔️ Trailblazer • Initial double step • Supreme promotion",
                pieceType = PieceType.Pawn,
                isOwned = true,
                prefsKey = null
            });

            // Category 1: Skin Quân Cờ (Skins) - 5 items (Default Classic + 4 Premium)
            _allCollectionItems.Add(new CollectionItemData
            {
                id = "skin_classic_chess",
                nameVi = "Quân Cờ Cổ Điển",
                nameEn = "Classic Wood & Marble",
                category = 1,
                rarity = "CƠ BẢN",
                rarityColor = new Color(0.85f, 0.85f, 0.85f),
                typeVi = "Bộ Quân Cờ Truyền Thống Mặc Định",
                typeEn = "Default Classic Chess Set",
                descVi = "Bộ quân cờ phong cách mộc cổ điển hoàng gia truyền thống, chạm khắc thủ công từ gỗ sồi và đá cẩm thạch trắng đen. Cân bằng hoàn mỹ cho mọi đại kiện tướng.",
                descEn = "Timeless hand-carved wooden and marble chess pieces. Elegant, traditional and balanced for true grandmasters.",
                perksVi = "🪵 Khắc gỗ thủ công • Điêu khắc cổ điển • Âm thanh gõ cờ mộc trầm ấm",
                perksEn = "🪵 Handcrafted wood • Timeless silhouette • Authentic tactile wooden clicks",
                spriteName = "skin_classic_chess.png",
                isOwned = true, // MẶC ĐỊNH SỞ HỮU MIỄN PHÍ
                prefsKey = PrefsEquippedSkin
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "skin_knight_light",
                nameVi = "Knight of Light",
                nameEn = "Knight of Light",
                category = 1,
                rarity = "THẦN THOẠI",
                rarityColor = colMythic,
                typeVi = "Bộ Skin Thần Thoại Toàn Quân",
                typeEn = "Mythic Army Skin Set",
                descVi = "Bộ giáp hoàng kim thánh khiết trui rèn từ mảnh vỡ mặt trời, phản chiếu ánh hào quang bất diệt trên toàn bộ đội quân.",
                descEn = "Holy golden armor forged from solar essence, radiating divine brilliance across the entire army.",
                perksVi = "✨ Hào quang hoàng kim • Viền giáp mạ vàng 24K • Hiệu ứng bước đi phát sáng",
                perksEn = "✨ Solar aura • 24K gold gilded plates • Glowing stride VFX",
                spriteName = "skin_knight_light.png",
                isOwned = AegisMech.UI.AegisPrefs.Owns("item_knight_of_light"),
                prefsKey = PrefsEquippedSkin
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "skin_dark_legion",
                nameVi = "Dark Legion",
                nameEn = "Dark Legion",
                category = 1,
                rarity = "SỬ THI",
                rarityColor = colEpic,
                typeVi = "Bộ Skin Sử Thi Hắc Ám",
                typeEn = "Epic Shadow Skin Set",
                descVi = "Đoàn quân bóng tối từ lòng vực sâu thẳm, rực cháy ngọn lửa hắc diện thạch và khát khao phục thù bóng đêm.",
                descEn = "Dark legions rising from the Abyss, shrouded in obsidian flames and shadow magic.",
                perksVi = "🔥 Lửa hỏa ngục • Giáp đen nhám phản quang • Âm thanh di chuyển uy lực",
                perksEn = "🔥 Hellfire flames • Matte obsidian armor • Thunderous march sound",
                spriteName = "skin_dark_legion.png",
                isOwned = AegisMech.UI.AegisPrefs.Owns("item_dark_legion"),
                prefsKey = PrefsEquippedSkin
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "skin_cyber_future",
                nameVi = "Arcane Paladin",
                nameEn = "Arcane Paladin",
                category = 1,
                rarity = "HUYỀN THOẠI",
                rarityColor = colLegendary,
                typeVi = "Bộ Skin Huyền Thoại Lượng Tử",
                typeEn = "Legendary Quantum Set",
                descVi = "Thiết kế cơ giới công nghệ cao tương lai, sử dụng lõi năng lượng Arcane Hextech và đường nét neon cyberpunk rực rỡ.",
                descEn = "Futuristic mech engineering from 2088, powered by Arcane Hextech cores and cyberpunk neon lines.",
                perksVi = "⚡ Tia laser lượng tử • Lõi năng lượng nhịp đập • Giao diện HUD tương lai",
                perksEn = "⚡ Quantum lasers • Pulsing power core • Futuristic HUD interface",
                spriteName = "skin_cyber_future.png",
                isOwned = AegisMech.UI.AegisPrefs.Owns("item_arcane_paladin"),
                prefsKey = PrefsEquippedSkin
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "skin_forest_spirit",
                nameVi = "Forest Spirit",
                nameEn = "Forest Spirit",
                category = 1,
                rarity = "SỬ THI",
                rarityColor = colEpic,
                typeVi = "Bộ Skin Tinh Hoa Tự Nhiên",
                typeEn = "Primal Nature Skin Set",
                descVi = "Khởi nguồn từ rừng nguyên sinh linh thiêng, mang linh hồn hoa cỏ và sức sống trường tồn của đất trời thiên nhiên.",
                descEn = "Millennia-old tree spirits crystallized with cherry blossoms and ancient moss.",
                perksVi = "🌿 Cánh hoa anh đào rơi • Gỗ thiêng cổ thụ • Âm thanh chim hót thanh khiết",
                perksEn = "🌿 Falling sakura petals • Sacred wood • Serene birdsong ambient",
                spriteName = "skin_forest_spirit.png",
                isOwned = AegisMech.UI.AegisPrefs.Owns("item_forest_spirit"),
                prefsKey = PrefsEquippedSkin
            });

            // Category 2: Đấu Trường & Bàn Cờ (Boards) - 6 items
            _allCollectionItems.Add(new CollectionItemData
            {
                id = "board_spirit_blossom",
                nameVi = "Vườn Hoa Linh Hồn",
                nameEn = "Spirit Blossom Arena",
                category = 2,
                rarity = "HUYỀN THOẠI",
                rarityColor = colLegendary,
                typeVi = "Đấu Trường 3D Độc Quyền",
                typeEn = "Exclusive 3D Arena",
                descVi = "Đấu trường thanh nhã với những cánh hoa anh đào rơi lãng đãng, cầu đá phong rêu và không gian thiền định bất tận.",
                descEn = "Ancient shrine arena with blooming sakura trees, mossy stone bridges, and tranquil streams.",
                perksVi = "🌸 Cây Sakura 3D • Cánh hoa rơi theo gió • Đá phong thủy cổ trấn",
                perksEn = "🌸 3D Sakura tree • Wind-blown petals • Sacred stone bridge",
                spriteName = "board_spirit_blossom.png",
                isOwned = true,
                prefsKey = PrefsEquippedBoard
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "board_royal_antique",
                nameVi = "Bàn Cờ Hoàng Gia Cổ Điển",
                nameEn = "Royal Antique Board",
                category = 2,
                rarity = "SỬ THI",
                rarityColor = colEpic,
                typeVi = "Bàn Cờ Gỗ Sồi Hoàng Gia",
                typeEn = "Royal Oak Chessboard",
                descVi = "Bàn cờ bằng gỗ sồi ngàn năm khảm viền vàng hoa văn hoàng gia châu Âu thế kỷ 18 sang trọng quý phái.",
                descEn = "Handcrafted from solid oak inlaid with precious metals, once reserved for Renaissance monarchs.",
                perksVi = "🪵 Gỗ sồi tự nhiên • Viền khắc hoa văn hoàng tộc • Âm thanh gõ gỗ thanh tao",
                perksEn = "🪵 Natural oak texture • Gilded imperial borders • Crisp wooden click SFX",
                spriteName = "board_royal_antique.png",
                isOwned = true,
                prefsKey = PrefsEquippedBoard
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "board_magma_core",
                nameVi = "Đấu Trường Dung Nham",
                nameEn = "Magma Core Arena",
                category = 2,
                rarity = "HUYỀN THOẠI",
                rarityColor = colLegendary,
                typeVi = "Đấu Trường Hỏa Ngục",
                typeEn = "Infernal Magma Arena",
                descVi = "Được xây dựng ngay trên miệng núi lửa đang phun trào, mỗi nước đi kích hoạt dung nham sủi bọt cuồn cuộn.",
                descEn = "Built right atop an active volcanic caldera, bubbling magma under every single move.",
                perksVi = "🌋 Vết nứt dung nham • Khói lửa địa ngục • Sức nóng thiêu đốt",
                perksEn = "🌋 Magma fissures • Volcanic smoke • Scorching heat glow",
                spriteName = "board_magma_core.png",
                isOwned = true,
                prefsKey = PrefsEquippedBoard
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "board_crystal_sanctuary",
                nameVi = "Điện Thờ Pha Lê",
                nameEn = "Crystal Sanctuary",
                category = 2,
                rarity = "THẦN THOẠI",
                rarityColor = colMythic,
                typeVi = "Đấu Trường Thần Thoại",
                typeEn = "Mythic Crystal Battlefield",
                descVi = "Đền thờ cổ ngập tràn tinh thể thạch anh tím tỏa ánh sáng thanh tẩy tâm trí người kỳ thủ.",
                descEn = "Sacred amethyst sanctuary where eternal starlight converges upon the board.",
                perksVi = "💎 Tinh thể phát quang • Phản chiếu đa chiều • Tiếng chuông pha lê ngân vang",
                perksEn = "💎 Luminescent crystals • Multi-dimensional prism • Crystal resonance SFX",
                spriteName = "board_crystal_sanctuary.png",
                isOwned = false,
                prefsKey = PrefsEquippedBoard
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "board_celestial_temple",
                nameVi = "Điện Thờ Thần Thánh",
                nameEn = "Celestial Sanctuary",
                category = 2,
                rarity = "THẦN THOẠI",
                rarityColor = colMythic,
                typeVi = "Đấu Trường Thượng Giới",
                typeEn = "Ascended Realm Arena",
                descVi = "Đấu trường La Mã cổ trên đỉnh tầng mây bồng bềnh, nơi các vị thần tối cao dõi theo những ván cờ định đoạt vận mệnh thế giới.",
                descEn = "Ancient pantheon amphitheater above the clouds where ancient deities watch over epoch-defining matches.",
                perksVi = "🏛️ Thức cột đá cẩm thạch • Biển mây bồng bềnh • Ánh dương quang vĩnh cửu",
                perksEn = "🏛️ Marble colonnades • Sea of clouds • Eternal daylight brilliance",
                spriteName = "board_celestial_temple.png",
                isOwned = false,
                prefsKey = PrefsEquippedBoard
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "board_cyber_neon",
                nameVi = "Đấu Trường Cyberpunk",
                nameEn = "Cyber Neon Grid",
                category = 2,
                rarity = "HUYỀN THOẠI",
                rarityColor = colLegendary,
                typeVi = "Đấu Trường Công Nghệ 2088",
                typeEn = "Futuristic Cyber Arena",
                descVi = "Sàn đấu vi mạch kỹ thuật số phản chiếu những tòa nhà chọc trời neon, mưa acid lất phất và âm nhạc synthwave sôi động.",
                descEn = "High-tech circuit arena reflecting neon skyscrapers under light acid drizzle with synthwave beats.",
                perksVi = "🏙️ Thành phố đêm Neon • Đường truyền laser xanh tím • Nhịp đập âm nhạc tương lai",
                perksEn = "🏙️ Neon cityscape • Laser datalines • Pumping synthwave pulses",
                spriteName = "board_cyber_neon.png",
                isOwned = false,
                prefsKey = PrefsEquippedBoard
            });

            // Category 3: Hiệu Ứng Bắt Quân (Effects) - 8 items
            _allCollectionItems.Add(new CollectionItemData
            {
                id = "fx_arcane_lightning",
                nameVi = "Sấm Sét Arcane",
                nameEn = "Arcane Lightning",
                category = 3,
                rarity = "HUYỀN THOẠI",
                rarityColor = colLegendary,
                typeVi = "Hiệu Ứng Bắt Quân (Finisher)",
                typeEn = "Capture Finisher FX",
                descVi = "Tia sét năng lượng cao giáng xuống khi quân cờ hạ gục đối thủ trong các pha bắt quân nảy lửa.",
                descEn = "High-voltage lightning strike from the stratosphere, disintegrating captured pieces.",
                perksVi = "⚡ Tia chớp xanh neon • Âm thanh sấm nổ vang rền • Chấn động màn hình",
                perksEn = "⚡ Cyan neon bolts • Thunder crack audio • Screen rumble",
                spriteName = "effect_arcane_lightning.png",
                isOwned = true,
                prefsKey = PrefsEquippedEffect
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "fx_divine_aura",
                nameVi = "Hào Quang Thánh",
                nameEn = "Divine Aura",
                category = 3,
                rarity = "SỬ THI",
                rarityColor = colEpic,
                typeVi = "Hiệu Ứng Hộ Thân",
                typeEn = "Guardian Aura FX",
                descVi = "Cột ánh sáng thiên giới chiếu rọi xuống người chiến thắng pha giao tranh, thanh lọc mọi hiểm nguy.",
                descEn = "Pillar of celestial light showering the victor of the engagement.",
                perksVi = "✨ Vòng hào quang thiên thần • Bụi sáng hoàng kim • Âm vang thánh ca",
                perksEn = "✨ Halo burst • Golden dust • Choral hymn resonance",
                spriteName = "effect_divine_aura.png",
                isOwned = true,
                prefsKey = PrefsEquippedEffect
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "fx_hellfire_blaze",
                nameVi = "Hỏa Ngục Bùng Nổ",
                nameEn = "Hellfire Blaze",
                category = 3,
                rarity = "SỬ THI",
                rarityColor = colEpic,
                typeVi = "Hiệu Ứng Thiêu Đốt",
                typeEn = "Inferno Burn FX",
                descVi = "Cột lửa ngùn ngụt bốc lên từ lòng đất thiêu đốt quân bị bắt, để lại tàn tích tro bụi rực cháy.",
                descEn = "Infernal flames erupt from below to consume the vanquished piece into embers.",
                perksVi = "🔥 Cột lửa hỏa ngục • Tàn tro bay theo gió • Âm thanh bùng cháy mãnh liệt",
                perksEn = "🔥 Fire geyser • Flying ash • Roaring blaze audio",
                spriteName = "effect_hellfire_blaze.png",
                isOwned = true,
                prefsKey = PrefsEquippedEffect
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "fx_cosmic_stardust",
                nameVi = "Bụi Sao Vũ Trụ",
                nameEn = "Cosmic Stardust",
                category = 3,
                rarity = "THẦN THOẠI",
                rarityColor = colMythic,
                typeVi = "Hiệu Ứng Thiên Hà",
                typeEn = "Galactic Stardust FX",
                descVi = "Mở ra một lỗ sâu vũ trụ thu nhỏ, hút quân cờ biến mất vào dòng chảy không thời gian huyền bí.",
                descEn = "Opens a miniature cosmic wormhole, drawing the piece into space-time currents.",
                perksVi = "🌌 Vòng xoáy lỗ sâu • Bụi tinh vân lấp lánh • Biến mất huyền bí",
                perksEn = "🌌 Wormhole vortex • Sparkling nebula dust • Mystic phase-out",
                spriteName = "effect_cosmic_stardust.png",
                isOwned = false,
                prefsKey = PrefsEquippedEffect
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "fx_void_singularity",
                nameVi = "Hố Đen Hư Không",
                nameEn = "Void Singularity",
                category = 3,
                rarity = "THẦN THOẠI",
                rarityColor = colMythic,
                typeVi = "Hiệu Ứng Lượng Tử Không Gian",
                typeEn = "Quantum Singularity FX",
                descVi = "Vòng xoáy hố đen lượng tử nuốt chửng quân cờ bị bắt vào chiều không gian thứ 4, tỏa ra sóng xung kích tím đậm.",
                descEn = "Quantum event horizon devouring captured pieces into hyper-dimensions with dark violet shockwaves.",
                perksVi = "🌀 Hố đen không gian • Sóng xung kích tím • Tiếng gầm lượng tử",
                perksEn = "🌀 Event horizon • Void shockwave • Quantum roar audio",
                spriteName = "effect_void_singularity.png",
                isOwned = false,
                prefsKey = PrefsEquippedEffect
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "fx_frost_shatter",
                nameVi = "Băng Phong Tuyệt Đối",
                nameEn = "Absolute Frostbite",
                category = 3,
                rarity = "SỬ THI",
                rarityColor = colEpic,
                typeVi = "Hiệu Ứng Băng Giá Cực Độ",
                typeEn = "Glacial Shatter FX",
                descVi = "Cơn lốc hàn băng âm 273 độ C lập tức đóng băng và đập vỡ vụn quân cờ đối phương thành hàng vạn mảnh băng tuyết lấp lánh.",
                descEn = "Sub-zero blizzard freezing and shattering defeated pieces into thousands of glinting ice crystals.",
                perksVi = "❄️ Đóng băng cực hạn • Vỡ vụn tinh thể tuyết • Âm thanh băng nứt giòn tan",
                perksEn = "❄️ Absolute zero freeze • Crystal shatter VFX • Crisp cracking ice SFX",
                spriteName = "effect_frost_shatter.png",
                isOwned = false,
                prefsKey = PrefsEquippedEffect
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "fx_cherry_sakura",
                nameVi = "Vũ Điệu Hoa Anh Đào",
                nameEn = "Sakura Petal Dance",
                category = 3,
                rarity = "SỬ THI",
                rarityColor = colEpic,
                typeVi = "Hiệu Ứng Phong Thủy Tinh Hoa",
                typeEn = "Zen Sakura Blossom FX",
                descVi = "Cơn lốc cánh hoa anh đào nở rộ xoay tròn thanh nhã, đưa linh hồn quân cờ tử trận về cõi thiên đường an lạc.",
                descEn = "Swirling vortex of blossoming cherry petals escorting fallen warriors into peaceful eternal slumber.",
                perksVi = "🌸 Cánh hoa anh đào rơi • Cơn gió thiền định • Âm vang sáo trúc thanh khiết",
                perksEn = "🌸 Sakura storm • Zen wind breeze • Gentle bamboo flute resonance",
                spriteName = "effect_cherry_sakura.png",
                isOwned = false,
                prefsKey = PrefsEquippedEffect
            });

            _allCollectionItems.Add(new CollectionItemData
            {
                id = "fx_golden_judgment",
                nameVi = "Phán Quyết Hoàng Kim",
                nameEn = "Divine Golden Judgment",
                category = 3,
                rarity = "HUYỀN THOẠI",
                rarityColor = colLegendary,
                typeVi = "Hiệu Ứng Thánh Kiếm Trảm",
                typeEn = "Holy Judgement Blade FX",
                descVi = "Thanh cự kiếm ánh sáng rực rỡ từ cổng trời giáng thẳng xuống đất, phân giải đối thủ trong hào quang chói lọi.",
                descEn = "Radiant colossus sword descending from celestial gates, vaporizing targets in pure holy brilliance.",
                perksVi = "⚔️ Cự kiếm ánh sáng • Cột sáng thiên đường • Tiếng chuông khải hoàn",
                perksEn = "⚔️ Holy colossus sword • Divine light column • Triumphant cathedral chime",
                spriteName = "effect_golden_judgment.png",
                isOwned = false,
                prefsKey = PrefsEquippedEffect
            });
        }

        private Sprite GetCollectionItemSprite(CollectionItemData item)
        {
            if (item == null) return null;
            if (item.pieceType.HasValue)
            {
                Sprite medPiece = MedievalUIAssetLoader.GetChessIcon(item.pieceType.Value.ToString());
                if (medPiece != null) return medPiece;
                return ArcaneAssetLoader.GetChessPiece2DSprite(item.pieceType.Value, true, 128);
            }
            if (!string.IsNullOrEmpty(item.spriteName))
            {
                Sprite sp = MedievalUIAssetLoader.GetSprite(item.spriteName) ?? ArcaneAssetLoader.LoadArcaneSprite(item.spriteName);
                if (sp != null) return sp;
            }
            return ArcaneAssetLoader.GetMiniChessboardDiagram(128);
        }

        private bool IsCollectionItemEquipped(CollectionItemData item)
        {
            if (item == null) return false;
            if (item.category == 0) return true; // Tất cả quân cờ mech đều tham chiến mặc định

            if (string.IsNullOrEmpty(item.prefsKey)) return false;

            string defaultVal = item.category == 1 ? "Quân Cờ Cổ Điển" :
                                item.category == 2 ? "Vườn Hoa Linh Hồn" : "Sấm Sét Arcane";
            string current = PlayerPrefs.GetString(item.prefsKey, defaultVal);

            if (item.id == "skin_classic_chess")
            {
                return string.IsNullOrEmpty(current) ||
                       string.Equals(current, item.nameVi, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(current, item.nameEn, StringComparison.OrdinalIgnoreCase) ||
                       current.Contains("Classic") || current.Contains("Cổ Điển");
            }

            return string.Equals(current, item.nameVi, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(current, item.nameEn, StringComparison.OrdinalIgnoreCase) ||
                   (item.category == 1 && current.Contains(item.nameEn)) ||
                   (item.category == 2 && current.Contains("Spirit") && item.id == "board_spirit_blossom");
        }

        private void BuildScreen8Collection(Transform parent, Font font)
        {
            InitializeCollectionData();

            _collectionModal = CreateModalOverlay(parent, "Screen8_CollectionModal");

            // Centered Royal Crowned Card Frame (Scaled up 10% to 1.1x -> 1672 x 902)
            GameObject cardObj = new GameObject("CollectionCard");
            cardObj.transform.SetParent(_collectionModal.transform, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1520f, 820f);
            cRt.localScale = new Vector3(1.10f, 1.10f, 1f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = MedievalUIAssetLoader.GetSprite("frame_shop_collection_1520")
                          ?? MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned);
            cardBg.type = Image.Type.Simple;
            cardBg.preserveAspect = false;
            cardBg.color = Color.white;

            // Window Header Bar (Height 54px, anchored safely below parchment crown arch)
            GameObject headerObj = new GameObject("CollectionHeader");
            headerObj.transform.SetParent(cardObj.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 1f);
            hRt.anchorMax = new Vector2(0.5f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(54f, -92f);
            hRt.sizeDelta = new Vector2(1420f, 58f);

            // Title: "BỘ SƯU TẬP"
            // Header Title removed as per Scene design to optimize header bar space

            // Category Navigation Tabs (Positioned in Center of Header Bar, styled identically to Shop tabs)
            GameObject tabsObj = new GameObject("CollectionTabs");
            tabsObj.transform.SetParent(headerObj.transform, false);
            RectTransform ctbRt = tabsObj.AddComponent<RectTransform>();
            ctbRt.anchorMin = new Vector2(0.5f, 0.5f);
            ctbRt.anchorMax = new Vector2(0.5f, 0.5f);
            ctbRt.pivot = new Vector2(0.5f, 0.5f);
            ctbRt.anchoredPosition = new Vector2(-234f, -13f);
            ctbRt.sizeDelta = new Vector2(940f, 54f);

            string[] tabNamesVi = { "Quân Cờ Mech", "Skin Huyền Thoại", "Đấu Trường & Bàn", "Hiệu Ứng Bắt Quân" };
            string[] tabNamesEn = { "♟️ Mech Pieces", "🛡️ Mythic Skins", "🏟️ Battlefields", "✨ Finisher FX" };
            Sprite tabActSprite = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_active")
                                ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_active")
                                ?? MedievalUIAssetLoader.GetSprite("btn_hex_gold_radiant");
            Sprite tabInactSprite = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_inactive")
                                  ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_inactive")
                                  ?? MedievalUIAssetLoader.GetSprite("btn_blank_black_banner");

            float[] tabPosXs = { -345f, -115f, 115f, 345f };
            for (int i = 0; i < 4; i++)
            {
                int tabIdx = i;
                bool active = (i == _selectedCollectionTab);
                GameObject tObj = new GameObject("Tab_" + i);
                tObj.transform.SetParent(tabsObj.transform, false);

                Image bg = tObj.AddComponent<Image>();
                bg.sprite = active ? tabActSprite : tabInactSprite;
                bg.type = Image.Type.Sliced;
                _collectionTabBgs[i] = bg;

                Button btn = tObj.AddComponent<Button>();
                btn.onClick.AddListener(() => SwitchCollectionTab(tabIdx));
                tObj.AddComponent<MenuButtonEffect>();

                RectTransform rt = tObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(tabPosXs[i], 0f);
                rt.sizeDelta = new Vector2(220f, 52f);

                GameObject txtObj = new GameObject("Txt");
                txtObj.transform.SetParent(tObj.transform, false);
                Text txtComp = txtObj.AddComponent<Text>();
                txtComp.text = AegisLocalization.T(tabNamesVi[i], tabNamesEn[i]);
                txtComp.font = font;
                txtComp.fontSize = active ? 19 : 18;
                txtComp.fontStyle = FontStyle.Bold;
                txtComp.alignment = TextAnchor.MiddleCenter;
                txtComp.color = active ? new Color(0.18f, 0.08f, 0.02f, 1f) : new Color(0.92f, 0.86f, 0.74f, 1f);
                txtComp.horizontalOverflow = HorizontalWrapMode.Overflow;
                txtComp.verticalOverflow = VerticalWrapMode.Overflow;
                MakeTextCrisp(txtComp, active ? new Color(1f, 0.90f, 0.60f, 0.85f) : new Color(0.10f, 0.05f, 0.02f, 0.95f));
                _collectionTabTexts[i] = txtComp;

                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = new Vector2(6f, 2f);
                txtRt.offsetMax = new Vector2(-6f, 0f);
            }

            // Gold Badge
            GameObject coinObj = new GameObject("CoinBadge");
            coinObj.transform.SetParent(headerObj.transform, false);
            Image cbImg = coinObj.AddComponent<Image>();
            Sprite goldBarSp = MedievalUIAssetLoader.GetSprite("counter_gold_capsule");
            if (goldBarSp != null)
            {
                cbImg.sprite = goldBarSp;
                cbImg.type = Image.Type.Simple;
                cbImg.preserveAspect = true;
            }
            else
            {
                cbImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(140, 42, 12f, new Color(0.18f, 0.12f, 0.04f, 0.9f), ColGoldBorder, 1.5f);
                cbImg.type = Image.Type.Sliced;
            }
            RectTransform cbRt = coinObj.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 0.5f);
            cbRt.anchorMax = new Vector2(1f, 0.5f);
            cbRt.pivot = new Vector2(1f, 0.5f);
            cbRt.anchoredPosition = new Vector2(-65f, 0f);
            cbRt.sizeDelta = new Vector2(140f, 42f);

            GameObject goldTxtObj = new GameObject("GoldTxt", typeof(RectTransform));
            goldTxtObj.transform.SetParent(coinObj.transform, false);
            _collectionGoldTxt = goldTxtObj.AddComponent<Text>();
            _collectionGoldTxt.text = _currentGold.ToString("N0");
            _collectionGoldTxt.font = font;
            _collectionGoldTxt.fontSize = 17;
            _collectionGoldTxt.fontStyle = FontStyle.Bold;
            _collectionGoldTxt.alignment = TextAnchor.MiddleCenter;
            _collectionGoldTxt.color = new Color(1.0f, 0.95f, 0.82f);
            _collectionGoldTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _collectionGoldTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_collectionGoldTxt);
            RectTransform gtRt = goldTxtObj.GetComponent<RectTransform>();
            gtRt.anchorMin = new Vector2(0.5f, 0.5f);
            gtRt.anchorMax = new Vector2(0.5f, 0.5f);
            gtRt.pivot = new Vector2(0.5f, 0.5f);
            gtRt.anchoredPosition = new Vector2(8f, -0.5f);
            gtRt.sizeDelta = new Vector2(60f, 32f);

            // Close Button [ Crossed Swords Gold ] (Top-Right Corner of Card Frame, Enlarged 66x66)
            CreateRoyalCloseButton(cardObj.transform, new Vector2(-44f, -130f), () => CloseModal(_collectionModal), 66f, "BtnClose");

            // Left Showcase Area (Width 930, Height 630 - perfectly filling left side of parchment)
            GameObject showcaseArea = new GameObject("ShowcaseArea");
            showcaseArea.transform.SetParent(cardObj.transform, false);
            RectTransform scRt = showcaseArea.AddComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0f, 0.5f);
            scRt.anchorMax = new Vector2(0f, 0.5f);
            scRt.pivot = new Vector2(0f, 0.5f);
            scRt.anchoredPosition = new Vector2(61f, -62f);
            scRt.sizeDelta = new Vector2(930f, 630f);

            // Container 0: 6 Quân Cờ Mech (2 rows x 3 columns)
            GameObject cont0 = new GameObject("Category_Pieces");
            cont0.transform.SetParent(showcaseArea.transform, false);
            _collectionCategoryContainers[0] = cont0;
            RectTransform c0Rt = cont0.AddComponent<RectTransform>();
            c0Rt.anchorMin = Vector2.zero;
            c0Rt.anchorMax = Vector2.one;
            c0Rt.offsetMin = Vector2.zero;
            c0Rt.offsetMax = Vector2.zero;

            List<CollectionItemData> pieces = _allCollectionItems.FindAll(x => x.category == 0);
            float[] pieceColX = { -305f, 0f, 305f };
            float[] pieceRowY = { 150f, -150f };
            for (int i = 0; i < pieces.Count; i++)
            {
                var pItem = pieces[i];
                int col = i % 3;
                int row = i / 3;
                CreatePieceCollectionCard(cont0.transform, pItem, new Vector2(pieceColX[col], pieceRowY[row]), font);
            }

            // Container 1: Skin Sets (Horizontal Scrollable Carousel, 3 visible at a time)
            GameObject cont1 = new GameObject("Category_Skins");
            cont1.transform.SetParent(showcaseArea.transform, false);
            _collectionCategoryContainers[1] = cont1;
            RectTransform c1Rt = cont1.AddComponent<RectTransform>();
            c1Rt.anchorMin = Vector2.zero;
            c1Rt.anchorMax = Vector2.one;
            c1Rt.offsetMin = Vector2.zero;
            c1Rt.offsetMax = Vector2.zero;

            Image c1MaskImg = cont1.AddComponent<Image>();
            c1MaskImg.color = Color.clear;
            c1MaskImg.raycastTarget = true;

            cont1.AddComponent<RectMask2D>();

            ScrollRect sr1 = cont1.AddComponent<ScrollRect>();
            sr1.horizontal = true;
            sr1.vertical = false;
            sr1.movementType = ScrollRect.MovementType.Clamped;
            sr1.elasticity = 0.12f;
            sr1.inertia = true;
            sr1.decelerationRate = 0.135f;
            sr1.scrollSensitivity = 40f;
            _collectionSkinsScrollRect = sr1;

            GameObject content1Obj = new GameObject("ScrollContent");
            content1Obj.transform.SetParent(cont1.transform, false);
            RectTransform ct1Rt = content1Obj.AddComponent<RectTransform>();
            ct1Rt.anchorMin = new Vector2(0f, 0.5f);
            ct1Rt.anchorMax = new Vector2(0f, 0.5f);
            ct1Rt.pivot = new Vector2(0f, 0.5f);
            ct1Rt.anchoredPosition = Vector2.zero;

            sr1.content = ct1Rt;
            sr1.viewport = c1Rt;

            List<CollectionItemData> skins = _allCollectionItems.FindAll(x => x.category == 1);
            float skinCardW = 285f;
            float skinGap = 20f;
            float padLeft = 15f;
            int skinCount = skins.Count;
            float totalSkinW = padLeft * 2f + skinCount * skinCardW + (skinCount - 1) * skinGap;
            ct1Rt.sizeDelta = new Vector2(totalSkinW, 610f);

            for (int i = 0; i < skinCount; i++)
            {
                var sItem = skins[i];
                Vector2 pos = new Vector2(padLeft + i * (skinCardW + skinGap), 0f);
                CreateSkinCollectionCard(content1Obj.transform, sItem, pos, skinCardW, font);
            }
            cont1.SetActive(false);

            // Container 2: Arenas / Boards (Vertical Scrollable, 4 visible at a time in 2x2 view)
            GameObject cont2 = CreateVerticalCollectionScrollView(showcaseArea.transform, "Category_Boards", 2, font);
            _collectionCategoryContainers[2] = cont2;
            cont2.SetActive(false);

            // Container 3: Finishers / Combat FX (Vertical Scrollable, 4 visible at a time in 2x2 view)
            GameObject cont3 = CreateVerticalCollectionScrollView(showcaseArea.transform, "Category_Effects", 3, font);
            _collectionCategoryContainers[3] = cont3;
            cont3.SetActive(false);

            // Right Inspector Panel (Width 460, Height 630 - perfectly filling right side of parchment)
            BuildCollectionInspectorPanel(cardObj.transform, font);

            // Navigation Left Chevron [ ◀ ] for Skin Carousel
            _collectionChevronLeft = new GameObject("CollectionChevronLeft");
            _collectionChevronLeft.transform.SetParent(cardObj.transform, false);
            Image lChvImg = _collectionChevronLeft.AddComponent<Image>();
            lChvImg.sprite = MedievalUIAssetLoader.GetSprite("btn_round_chevron_left");
            lChvImg.type = Image.Type.Simple;
            lChvImg.preserveAspect = true;
            Button lChvBtn = _collectionChevronLeft.AddComponent<Button>();
            lChvBtn.onClick.AddListener(() => SmoothScrollCollectionSkinsDelta(-0.50f));
            _collectionChevronLeft.AddComponent<MenuButtonEffect>();
            RectTransform lChvRt = _collectionChevronLeft.GetComponent<RectTransform>();
            lChvRt.anchorMin = new Vector2(0f, 0.5f);
            lChvRt.anchorMax = new Vector2(0f, 0.5f);
            lChvRt.pivot = new Vector2(0f, 0.5f);
            lChvRt.anchoredPosition = new Vector2(20f, -56f);
            lChvRt.sizeDelta = new Vector2(48f, 48f);
            _collectionChevronLeft.transform.SetAsLastSibling();

            // Navigation Right Chevron [ ▶ ] for Skin Carousel
            _collectionChevronRight = new GameObject("CollectionChevronRight");
            _collectionChevronRight.transform.SetParent(cardObj.transform, false);
            Image rChvImg = _collectionChevronRight.AddComponent<Image>();
            rChvImg.sprite = MedievalUIAssetLoader.GetSprite("btn_round_chevron_right");
            rChvImg.type = Image.Type.Simple;
            rChvImg.preserveAspect = true;
            Button rChvBtn = _collectionChevronRight.AddComponent<Button>();
            rChvBtn.onClick.AddListener(() => SmoothScrollCollectionSkinsDelta(0.50f));
            _collectionChevronRight.AddComponent<MenuButtonEffect>();
            RectTransform rChvRt = _collectionChevronRight.GetComponent<RectTransform>();
            rChvRt.anchorMin = new Vector2(0f, 0.5f);
            rChvRt.anchorMax = new Vector2(0f, 0.5f);
            rChvRt.pivot = new Vector2(1f, 0.5f);
            rChvRt.anchoredPosition = new Vector2(990f, -56f);
            rChvRt.sizeDelta = new Vector2(48f, 48f);
            _collectionChevronRight.transform.SetAsLastSibling();

            _collectionChevronLeft.SetActive(false);
            _collectionChevronRight.SetActive(false);

            _collectionModal.SetActive(false);
            if (_allCollectionItems.Count > 0)
            {
                InspectCollectionItem(_allCollectionItems[0]);
            }
        }

        private void BuildCollectionInspectorPanel(Transform parent, Font font)
        {
            GameObject inspectObj = new GameObject("ArmoryInspectorPanel");
            inspectObj.transform.SetParent(parent, false);

            RectTransform iRt = inspectObj.AddComponent<RectTransform>();
            iRt.anchorMin = new Vector2(1f, 0.5f);
            iRt.anchorMax = new Vector2(1f, 0.5f);
            iRt.pivot = new Vector2(1f, 0.5f);
            iRt.anchoredPosition = new Vector2(-82f, -56f);
            iRt.sizeDelta = new Vector2(460f, 630f);

            Image iBg = inspectObj.AddComponent<Image>();
            iBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(460, 630, 20f, new Color(0.02f, 0.05f, 0.10f, 0.96f), new Color(0.85f, 0.70f, 0.35f, 0.90f), 2f);
            iBg.type = Image.Type.Sliced;

            // Inspector Header
            CreateTextComp(inspectObj.transform, AegisLocalization.T("✦ CHI TIẾT BINH KHÍ ✦", "✦ ARMORY INSPECTOR ✦"), 20, FontStyle.Bold, ColCyanGlow, font, new Vector2(0f, 280f), new Vector2(420f, 28f));

            // Artwork Preview Frame (Width 420, Height 180)
            GameObject frameObj = new GameObject("PreviewFrame");
            frameObj.transform.SetParent(inspectObj.transform, false);
            Image frameBg = frameObj.AddComponent<Image>();
            frameBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(420, 180, 14f, new Color(0.01f, 0.03f, 0.07f, 0.95f), ColMutedBorder, 1.2f);
            frameBg.type = Image.Type.Sliced;
            RectTransform fRt = frameObj.GetComponent<RectTransform>();
            fRt.anchoredPosition = new Vector2(0f, 165f);
            fRt.sizeDelta = new Vector2(420f, 180f);

            GameObject imgObj = new GameObject("Artwork");
            imgObj.transform.SetParent(frameObj.transform, false);
            _inspectImg = imgObj.AddComponent<Image>();
            _inspectImg.preserveAspect = true;
            RectTransform imgRt = imgObj.GetComponent<RectTransform>();
            imgRt.anchorMin = Vector2.zero;
            imgRt.anchorMax = Vector2.one;
            imgRt.offsetMin = new Vector2(10f, 10f);
            imgRt.offsetMax = new Vector2(-10f, -10f);

            // Title
            _inspectTitle = CreateTextComp(inspectObj.transform, "Vật phẩm", 22, FontStyle.Bold, ColTextWhite, font, new Vector2(0f, 50f), new Vector2(420f, 30f));
            _inspectTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            _inspectTitle.resizeTextForBestFit = true;
            _inspectTitle.resizeTextMinSize = 15;
            _inspectTitle.resizeTextMaxSize = 22;

            // Rarity & Type Subtitle Row
            GameObject tagRow = new GameObject("TagRow");
            tagRow.transform.SetParent(inspectObj.transform, false);
            RectTransform trRt = tagRow.AddComponent<RectTransform>();
            trRt.anchoredPosition = new Vector2(0f, 20f);
            trRt.sizeDelta = new Vector2(420f, 24f);

            _inspectRarity = CreateTextComp(tagRow.transform, "THẦN THOẠI", 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(-105f, 0f), new Vector2(190f, 22f), TextAnchor.MiddleRight);
            CreateTextComp(tagRow.transform, "•", 14, FontStyle.Bold, ColMutedBorder, font, Vector2.zero, new Vector2(20f, 22f), TextAnchor.MiddleCenter);
            _inspectType = CreateTextComp(tagRow.transform, "Quân Cờ", 13, FontStyle.Normal, ColTextMuted, font, new Vector2(105f, 0f), new Vector2(190f, 22f), TextAnchor.MiddleLeft);

            // Lore / Description Box (Width 420, Height 105 - text never spills out)
            GameObject descObj = new GameObject("DescBox");
            descObj.transform.SetParent(inspectObj.transform, false);
            Image descBg = descObj.AddComponent<Image>();
            descBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(420, 105, 12f, new Color(0.04f, 0.08f, 0.15f, 0.90f), ColMutedBorder, 1f);
            descBg.type = Image.Type.Sliced;
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchoredPosition = new Vector2(0f, -50f);
            dRt.sizeDelta = new Vector2(420f, 105f);

            _inspectDesc = CreateTextComp(descObj.transform, "Mô tả chiến thuật...", 13, FontStyle.Normal, ColTextWhite, font, Vector2.zero, new Vector2(396f, 85f), TextAnchor.UpperLeft);
            _inspectDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
            _inspectDesc.verticalOverflow = VerticalWrapMode.Truncate;
            _inspectDesc.resizeTextForBestFit = true;
            _inspectDesc.resizeTextMinSize = 10;
            _inspectDesc.resizeTextMaxSize = 13;
            _inspectDesc.lineSpacing = 1.15f;

            // Perks Box (Width 420, Height 105 - text never spills out)
            GameObject perksObj = new GameObject("PerksBox");
            perksObj.transform.SetParent(inspectObj.transform, false);
            Image perksBg = perksObj.AddComponent<Image>();
            perksBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(420, 105, 12f, new Color(0.04f, 0.08f, 0.15f, 0.90f), ColMutedBorder, 1f);
            perksBg.type = Image.Type.Sliced;
            RectTransform pkRt = perksObj.GetComponent<RectTransform>();
            pkRt.anchoredPosition = new Vector2(0f, -165f);
            pkRt.sizeDelta = new Vector2(420f, 105f);

            CreateTextComp(perksObj.transform, AegisLocalization.T("⚡ ĐẶC TÍNH CHIẾN THUẬT", "⚡ TACTICAL PERKS"), 13, FontStyle.Bold, ColCyanGlow, font, new Vector2(0f, 34f), new Vector2(396f, 22f), TextAnchor.MiddleLeft);
            _inspectPerks = CreateTextComp(perksObj.transform, "Hiệu ứng...", 12, FontStyle.Normal, ColTextMuted, font, new Vector2(0f, -12f), new Vector2(396f, 56f), TextAnchor.UpperLeft);
            _inspectPerks.horizontalOverflow = HorizontalWrapMode.Wrap;
            _inspectPerks.verticalOverflow = VerticalWrapMode.Truncate;
            _inspectPerks.resizeTextForBestFit = true;
            _inspectPerks.resizeTextMinSize = 9;
            _inspectPerks.resizeTextMaxSize = 12;
            _inspectPerks.lineSpacing = 1.15f;

            // Action CTA Button (Width 420, Height 56)
            _inspectEquipBtnObj = new GameObject("BtnActionEquip");
            _inspectEquipBtnObj.transform.SetParent(inspectObj.transform, false);
            _inspectEquipBtnBg = _inspectEquipBtnObj.AddComponent<Image>();
            _inspectEquipBtnBg.sprite = MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold")
                                       ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold);
            _inspectEquipBtnBg.type = Image.Type.Sliced;
            _inspectEquipBtnBg.color = Color.white;

            _inspectEquipBtn = _inspectEquipBtnObj.AddComponent<Button>();
            _inspectEquipBtnObj.AddComponent<MenuButtonEffect>();

            RectTransform ebRt = _inspectEquipBtnObj.GetComponent<RectTransform>();
            ebRt.anchoredPosition = new Vector2(0f, -258f);
            ebRt.sizeDelta = new Vector2(420f, 56f);

            _inspectEquipBtnTxt = CreateTextComp(_inspectEquipBtnObj.transform, AegisLocalization.T("🛒  XEM TRONG CỬA HÀNG", "🛒  VIEW IN SHOP"), 18, FontStyle.Bold, new Color(1.0f, 0.94f, 0.82f), font, Vector2.zero, new Vector2(390f, 36f));
            MakeTextCrisp(_inspectEquipBtnTxt, new Color(0.12f, 0.06f, 0.02f, 0.95f));
        }

        private GameObject CreateVerticalCollectionScrollView(Transform parent, string name, int categoryIndex, Font font)
        {
            GameObject scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            RectTransform svRt = scrollObj.AddComponent<RectTransform>();
            svRt.anchorMin = Vector2.zero;
            svRt.anchorMax = Vector2.one;
            svRt.offsetMin = Vector2.zero;
            svRt.offsetMax = Vector2.zero;

            Image maskImg = scrollObj.AddComponent<Image>();
            maskImg.color = Color.clear;
            maskImg.raycastTarget = true;

            scrollObj.AddComponent<RectMask2D>();

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.elasticity = 0.12f;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;
            sr.scrollSensitivity = 40f;

            if (categoryIndex == 2) _collectionBoardsScrollRect = sr;
            else if (categoryIndex == 3) _collectionEffectsScrollRect = sr;

            GameObject contentObj = new GameObject("ScrollContent");
            contentObj.transform.SetParent(scrollObj.transform, false);
            RectTransform ctRt = contentObj.AddComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0.5f, 1f);
            ctRt.anchorMax = new Vector2(0.5f, 1f);
            ctRt.pivot = new Vector2(0.5f, 1f);
            ctRt.anchoredPosition = Vector2.zero;

            sr.content = ctRt;
            sr.viewport = svRt;

            List<CollectionItemData> items = _allCollectionItems.FindAll(x => x.category == categoryIndex);
            int count = items.Count;
            int numRows = (count + 1) / 2;

            float cardH = 295f;
            float gapY = 15f;
            float padTop = 10f;
            float padBottom = 15f;

            float totalH = padTop + numRows * cardH + (numRows > 1 ? (numRows - 1) * gapY : 0f) + padBottom;
            ctRt.sizeDelta = new Vector2(930f, Mathf.Max(totalH, 630f));

            float[] colX = { -228f, 228f };

            for (int i = 0; i < count; i++)
            {
                var item = items[i];
                int col = i % 2;
                int row = i / 2;
                float posY = -(padTop + row * (cardH + gapY) + cardH / 2f);
                CreateWideCollectionCard(contentObj.transform, item, new Vector2(colX[col], posY), font);
            }

            return scrollObj;
        }

        private void CreatePieceCollectionCard(Transform parent, CollectionItemData item, Vector2 pos, Font font)
        {
            GameObject card = new GameObject("Card_" + item.id);
            card.transform.SetParent(parent, false);
            item.cardObj = card;

            Image bg = card.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(285, 290, 16f, ColGlassFill, item.rarityColor, 1.5f);
            bg.type = Image.Type.Sliced;
            item.borderImg = bg;

            Button btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                InspectCollectionItem(item);
            });
            card.AddComponent<MenuButtonEffect>();

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(285f, 290f);

            // Piece Icon Token
            GameObject tokenObj = new GameObject("Token");
            tokenObj.transform.SetParent(card.transform, false);
            Image tokenImg = tokenObj.AddComponent<Image>();
            tokenImg.sprite = GetCollectionItemSprite(item);
            tokenImg.preserveAspect = true;
            RectTransform trt = tokenObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 50f);
            trt.sizeDelta = new Vector2(110f, 110f);

            // Title
            var nameTxt = CreateTextComp(card.transform, AegisLocalization.T(item.nameVi, item.nameEn), 20, FontStyle.Bold, ColTextWhite, font, new Vector2(0f, -24f), new Vector2(265f, 28f));
            nameTxt.resizeTextForBestFit = true;
            nameTxt.resizeTextMinSize = 14;
            nameTxt.resizeTextMaxSize = 20;

            // Class / Rarity
            CreateTextComp(card.transform, item.rarity, 13, FontStyle.Bold, item.rarityColor, font, new Vector2(0f, -50f), new Vector2(265f, 22f));

            // Status Pill
            GameObject pill = new GameObject("StatusPill");
            pill.transform.SetParent(card.transform, false);
            Image pImg = pill.AddComponent<Image>();
            pImg.sprite = MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold")
                       ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold);
            pImg.type = Image.Type.Sliced;
            pImg.color = Color.white;
            item.statusBg = pImg;
            RectTransform prt = pill.GetComponent<RectTransform>();
            prt.anchoredPosition = new Vector2(0f, -95f);
            prt.sizeDelta = new Vector2(170f, 36f);

            Text st = CreateTextComp(pill.transform, AegisLocalization.T("✓ ĐANG DÙNG", "✓ EQUIPPED"), 14, FontStyle.Bold, new Color(1.0f, 0.94f, 0.75f), font, Vector2.zero, new Vector2(160f, 26f));
            MakeTextCrisp(st, new Color(0.12f, 0.06f, 0.02f, 0.95f));
            item.statusText = st;
        }

        private void CreateSkinCollectionCard(Transform parent, CollectionItemData item, Vector2 pos, float cardW, Font font)
        {
            GameObject card = new GameObject("Card_" + item.id);
            card.transform.SetParent(parent, false);
            item.cardObj = card;

            Image bg = card.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)cardW, 590, 18f, ColGlassFill, item.rarityColor, 1.5f);
            bg.type = Image.Type.Sliced;
            item.borderImg = bg;

            Button btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                InspectCollectionItem(item);
            });
            card.AddComponent<MenuButtonEffect>();

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(cardW, 590f);

            // Artwork Mech
            GameObject artObj = new GameObject("Artwork");
            artObj.transform.SetParent(card.transform, false);
            Image artImg = artObj.AddComponent<Image>();
            artImg.sprite = GetCollectionItemSprite(item);
            artImg.preserveAspect = false;
            RectTransform artRt = artObj.GetComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0.5f, 0.5f);
            artRt.anchorMax = new Vector2(0.5f, 0.5f);
            artRt.pivot = new Vector2(0.5f, 0.5f);
            artRt.anchoredPosition = new Vector2(0f, 85f);
            artRt.sizeDelta = new Vector2(cardW - 20f, 370f);

            // Title
            var nameTxt = CreateTextComp(card.transform, AegisLocalization.T(item.nameVi, item.nameEn), 20, FontStyle.Bold, ColTextWhite, font, new Vector2(0f, -135f), new Vector2(cardW - 20f, 28f));
            nameTxt.resizeTextForBestFit = true;
            nameTxt.resizeTextMinSize = 14;
            nameTxt.resizeTextMaxSize = 20;

            // Rarity Tag
            CreateTextComp(card.transform, item.rarity, 13, FontStyle.Bold, item.rarityColor, font, new Vector2(0f, -165f), new Vector2(cardW - 20f, 22f));

            // Status Pill
            GameObject pill = new GameObject("StatusPill");
            pill.transform.SetParent(card.transform, false);
            Image pImg = pill.AddComponent<Image>();
            bool isEq = IsCollectionItemEquipped(item);
            Color fillCol = isEq ? new Color(0.05f, 0.20f, 0.25f, 0.95f) : (item.isOwned ? new Color(0.08f, 0.14f, 0.20f, 0.90f) : new Color(0.18f, 0.06f, 0.06f, 0.90f));
            Color borderCol = isEq ? ColCyanGlow : (item.isOwned ? ColMutedBorder : new Color(0.85f, 0.35f, 0.35f));
            pImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)(cardW - 40f), 42, 10f, fillCol, borderCol, 1.2f);
            pImg.type = Image.Type.Sliced;
            item.statusBg = pImg;
            RectTransform prt = pill.GetComponent<RectTransform>();
            prt.anchoredPosition = new Vector2(0f, -220f);
            prt.sizeDelta = new Vector2(cardW - 40f, 42f);

            string sTxt = isEq ? AegisLocalization.T("✓ ĐANG DÙNG", "✓ EQUIPPED") : (item.isOwned ? AegisLocalization.T("ĐÃ SỞ HỮU", "OWNED") : AegisLocalization.T("🔒 CHƯA MỞ", "🔒 LOCKED"));
            Color tCol = isEq ? ColCyanGlow : (item.isOwned ? ColTextWhite : new Color(0.95f, 0.5f, 0.5f));
            Text st = CreateTextComp(pill.transform, sTxt, 14, FontStyle.Bold, tCol, font, Vector2.zero, new Vector2(cardW - 44f, 28f));
            item.statusText = st;
        }

        private void CreateWideCollectionCard(Transform parent, CollectionItemData item, Vector2 pos, Font font)
        {
            GameObject card = new GameObject("Card_" + item.id);
            card.transform.SetParent(parent, false);
            item.cardObj = card;

            Image bg = card.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(445, 295, 16f, ColGlassFill, item.rarityColor, 1.5f);
            bg.type = Image.Type.Sliced;
            item.borderImg = bg;

            Button btn = card.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                InspectCollectionItem(item);
            });
            card.AddComponent<MenuButtonEffect>();

            RectTransform rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(445f, 295f);

            // Preview Image
            GameObject artObj = new GameObject("Artwork");
            artObj.transform.SetParent(card.transform, false);
            Image artImg = artObj.AddComponent<Image>();
            artImg.sprite = GetCollectionItemSprite(item);
            artImg.preserveAspect = false;
            RectTransform artRt = artObj.GetComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0.5f, 1f);
            artRt.anchorMax = new Vector2(0.5f, 1f);
            artRt.pivot = new Vector2(0.5f, 1f);
            artRt.anchoredPosition = new Vector2(0f, -10f);
            artRt.sizeDelta = new Vector2(425f, 165f);

            // Title
            var nameTxt = CreateTextComp(card.transform, AegisLocalization.T(item.nameVi, item.nameEn), 18, FontStyle.Bold, ColTextWhite, font, new Vector2(0f, -55f), new Vector2(420f, 26f));
            nameTxt.resizeTextForBestFit = true;
            nameTxt.resizeTextMinSize = 13;
            nameTxt.resizeTextMaxSize = 18;

            // Rarity Tag
            CreateTextComp(card.transform, item.rarity + " • " + AegisLocalization.T(item.typeVi, item.typeEn), 13, FontStyle.Bold, item.rarityColor, font, new Vector2(0f, -80f), new Vector2(420f, 20f));

            // Status Pill
            GameObject pill = new GameObject("StatusPill");
            pill.transform.SetParent(card.transform, false);
            Image pImg = pill.AddComponent<Image>();
            bool isEq = IsCollectionItemEquipped(item);
            Sprite pSprite = isEq
                ? (MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold") ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold))
                : (item.isOwned
                    ? (MedievalUIAssetLoader.GetSprite("btn_blank_parchment_gold") ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion))
                    : (MedievalUIAssetLoader.GetSprite("btn_blank_black_banner") ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.BlackBanner)));
            pImg.sprite = pSprite;
            pImg.type = Image.Type.Sliced;
            pImg.color = Color.white;
            item.statusBg = pImg;
            RectTransform prt = pill.GetComponent<RectTransform>();
            prt.anchoredPosition = new Vector2(0f, -114f);
            prt.sizeDelta = new Vector2(180f, 36f);

            string sTxt = isEq ? AegisLocalization.T("✓ ĐANG DÙNG", "✓ EQUIPPED") : (item.isOwned ? AegisLocalization.T("ĐÃ SỞ HỮU", "OWNED") : AegisLocalization.T("🔒 CHƯA MỞ", "🔒 LOCKED"));
            Color tCol = isEq ? new Color(1.0f, 0.94f, 0.75f) : (item.isOwned ? new Color(0.20f, 0.10f, 0.04f) : new Color(0.92f, 0.85f, 0.75f));
            Text st = CreateTextComp(pill.transform, sTxt, 13, FontStyle.Bold, tCol, font, Vector2.zero, new Vector2(170f, 26f));
            MakeTextCrisp(st, new Color(0.12f, 0.06f, 0.02f, 0.95f));
            item.statusText = st;
        }

        public void SmoothScrollCollectionSkinsDelta(float delta)
        {
            if (_collectionSkinsScrollRect == null) return;
            float current = _collectionSkinsScrollRect.horizontalNormalizedPosition;
            SmoothScrollCollectionSkins(Mathf.Clamp01(current + delta));
        }

        public void SmoothScrollCollectionSkins(float targetPos)
        {
            if (_collectionScrollCoroutine != null) StopCoroutine(_collectionScrollCoroutine);
            _collectionScrollCoroutine = StartCoroutine(DoSmoothScrollCollectionSkins(targetPos));
        }

        private IEnumerator DoSmoothScrollCollectionSkins(float targetPos)
        {
            if (_collectionSkinsScrollRect == null) yield break;
            AegisAudioManager.PlayTabSwitch();
            float startPos = _collectionSkinsScrollRect.horizontalNormalizedPosition;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                _collectionSkinsScrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPos, targetPos, ease);
                yield return null;
            }
            _collectionSkinsScrollRect.horizontalNormalizedPosition = targetPos;
            _collectionScrollCoroutine = null;
        }

        private void SwitchCollectionTab(int tabIndex)
        {
            if (_selectedCollectionTab != tabIndex) AegisAudioManager.PlayTabSwitch();
            _selectedCollectionTab = tabIndex;
            if (_collectionScrollCoroutine != null)
            {
                StopCoroutine(_collectionScrollCoroutine);
                _collectionScrollCoroutine = null;
            }

            bool isSkinsTab = (tabIndex == 1);
            if (_collectionChevronLeft != null) _collectionChevronLeft.SetActive(isSkinsTab);
            if (_collectionChevronRight != null) _collectionChevronRight.SetActive(isSkinsTab);

            if (isSkinsTab && _collectionSkinsScrollRect != null)
            {
                _collectionSkinsScrollRect.horizontalNormalizedPosition = 0f;
            }
            else if (tabIndex == 2 && _collectionBoardsScrollRect != null)
            {
                _collectionBoardsScrollRect.verticalNormalizedPosition = 1f;
            }
            else if (tabIndex == 3 && _collectionEffectsScrollRect != null)
            {
                _collectionEffectsScrollRect.verticalNormalizedPosition = 1f;
            }

            Sprite activeSprite = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_active")
                                ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_active");
            Sprite inactiveSprite = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_inactive")
                                  ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_inactive");

            for (int i = 0; i < _collectionTabBgs.Length; i++)
            {
                bool active = (i == tabIndex);
                if (_collectionTabBgs[i] != null)
                {
                    _collectionTabBgs[i].sprite = active ? activeSprite : inactiveSprite;
                    _collectionTabBgs[i].type = Image.Type.Sliced;
                }
                if (_collectionTabTexts[i] != null)
                {
                    _collectionTabTexts[i].color = active ? new Color(0.18f, 0.08f, 0.02f, 1f) : new Color(0.92f, 0.86f, 0.74f, 1f);
                    _collectionTabTexts[i].fontSize = active ? 19 : 18;
                    _collectionTabTexts[i].fontStyle = FontStyle.Bold;
                    MakeTextCrisp(_collectionTabTexts[i], active ? new Color(1f, 0.90f, 0.60f, 0.85f) : new Color(0.10f, 0.05f, 0.02f, 0.95f));
                }
                if (_collectionCategoryContainers[i] != null)
                {
                    _collectionCategoryContainers[i].SetActive(active);
                }
            }

            CollectionItemData firstInTab = _allCollectionItems.Find(x => x.category == tabIndex);
            if (firstInTab != null)
            {
                InspectCollectionItem(firstInTab);
            }
        }

        private void InspectCollectionItem(CollectionItemData item)
        {
            if (item == null) return;
            _inspectedItem = item;

            if (_inspectTitle != null) _inspectTitle.text = AegisLocalization.T(item.nameVi, item.nameEn);
            if (_inspectRarity != null)
            {
                _inspectRarity.text = item.rarity;
                _inspectRarity.color = item.rarityColor;
            }
            if (_inspectType != null) _inspectType.text = AegisLocalization.T(item.typeVi, item.typeEn);
            if (_inspectDesc != null) _inspectDesc.text = AegisLocalization.T(item.descVi, item.descEn);
            if (_inspectPerks != null) _inspectPerks.text = AegisLocalization.T(item.perksVi, item.perksEn);

            if (_inspectImg != null)
            {
                Sprite sp = GetCollectionItemSprite(item);
                if (sp != null)
                {
                    _inspectImg.sprite = sp;
                    _inspectImg.color = Color.white;
                    _inspectImg.preserveAspect = true;
                }
            }

            bool equipped = IsCollectionItemEquipped(item);
            if (_inspectEquipBtn != null && _inspectEquipBtnTxt != null && _inspectEquipBtnBg != null)
            {
                _inspectEquipBtn.onClick.RemoveAllListeners();

                if (equipped)
                {
                    _inspectEquipBtnTxt.text = AegisLocalization.T("✓ ĐANG TRANG BỊ", "✓ EQUIPPED");
                    _inspectEquipBtnTxt.color = new Color(0.96f, 0.90f, 0.74f);
                    _inspectEquipBtnBg.sprite = MedievalUIAssetLoader.GetSprite("btn_blank_black_banner")
                                               ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodSmall);
                    _inspectEquipBtnBg.type = Image.Type.Sliced;
                    _inspectEquipBtnBg.color = new Color(0.85f, 0.80f, 0.70f, 0.85f);
                    _inspectEquipBtn.interactable = false;
                }
                else if (item.isOwned)
                {
                    _inspectEquipBtnTxt.text = AegisLocalization.T("⚔️  TRANG BỊ NGAY", "⚔️  EQUIP NOW");
                    _inspectEquipBtnTxt.color = new Color(0.20f, 0.10f, 0.04f);
                    _inspectEquipBtnBg.sprite = MedievalUIAssetLoader.GetSprite("btn_blank_parchment_gold")
                                               ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion);
                    _inspectEquipBtnBg.type = Image.Type.Sliced;
                    _inspectEquipBtnBg.color = Color.white;
                    _inspectEquipBtn.interactable = true;
                    _inspectEquipBtn.onClick.AddListener(() => EquipCollectionItem(item));
                }
                else
                {
                    _inspectEquipBtnTxt.text = AegisLocalization.T("🛒  XEM TRONG CỬA HÀNG", "🛒  VIEW IN SHOP");
                    _inspectEquipBtnTxt.color = new Color(1.0f, 0.94f, 0.82f);
                    _inspectEquipBtnBg.sprite = MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold")
                                               ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold);
                    _inspectEquipBtnBg.type = Image.Type.Sliced;
                    _inspectEquipBtnBg.color = Color.white;
                    _inspectEquipBtn.interactable = true;
                    _inspectEquipBtn.onClick.AddListener(() =>
                    {
                        CloseModal(_collectionModal);
                        OpenShopModal();
                        SwitchShopTab(item.category == 1 ? 0 : (item.category == 2 ? 1 : 2));
                    });
                }
                MakeTextCrisp(_inspectEquipBtnTxt, new Color(0.12f, 0.06f, 0.02f, 0.95f));
            }
        }

        private void EquipCollectionItem(CollectionItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.prefsKey)) return;

            PlayerPrefs.SetString(item.prefsKey, item.nameVi);
            PlayerPrefs.Save();
            AegisAudioManager.PlayButtonClick();

            string toastMsg = AegisLocalization.T($"Đã trang bị: {item.nameVi}!", $"Equipped: {item.nameEn}!");
            ShowToast(toastMsg, ToastType.Success);

            RefreshCollectionUI();
            InspectCollectionItem(item);
        }

        public void RefreshCollectionUI()
        {
            if (_collectionGoldTxt != null) _collectionGoldTxt.text = _currentGold.ToString();

            // Refresh ownership status for all items
            for (int i = 0; i < _allCollectionItems.Count; i++)
            {
                var itm = _allCollectionItems[i];
                if (itm.id == "skin_classic_chess" || itm.category == 0)
                {
                    itm.isOwned = true;
                }
                else
                {
                    string cleanId = "item_" + System.Text.RegularExpressions.Regex.Replace((itm.nameVi ?? itm.nameEn).ToLower(), @"[^a-z0-9]", "_");
                    itm.isOwned = AegisMech.UI.AegisPrefs.Owns(itm.id) || AegisMech.UI.AegisPrefs.Owns(cleanId);
                }
            }

            int total = _allCollectionItems.Count;
            int owned = 0;
            for (int i = 0; i < _allCollectionItems.Count; i++)
            {
                if (_allCollectionItems[i].isOwned) owned++;
            }

            int percent = total > 0 ? (int)((float)owned / total * 100f) : 0;
            if (_collectionProgressTxt != null)
            {
                _collectionProgressTxt.text = AegisLocalization.T($"SỞ HỮU: {owned} / {total} ({percent}%)", $"OWNED: {owned} / {total} ({percent}%)");
            }

            if (_collectionProgressBarFill != null)
            {
                float ratio = total > 0 ? Mathf.Clamp01((float)owned / total) : 0f;
                _collectionProgressBarFill.sizeDelta = new Vector2(180f * ratio, 6f);
            }

            for (int i = 0; i < _allCollectionItems.Count; i++)
            {
                var itm = _allCollectionItems[i];
                if (itm.statusText == null) continue;

                bool isEq = IsCollectionItemEquipped(itm);
                if (isEq)
                {
                    itm.statusText.text = AegisLocalization.T("✓ ĐANG DÙNG", "✓ EQUIPPED");
                    itm.statusText.color = new Color(1.0f, 0.94f, 0.75f);
                    if (itm.statusBg != null)
                    {
                        itm.statusBg.sprite = MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold")
                                           ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.DarkGold);
                        itm.statusBg.type = Image.Type.Sliced;
                        itm.statusBg.color = Color.white;
                    }
                    if (itm.borderImg != null) itm.borderImg.color = new Color(1f, 0.85f, 0.40f);
                }
                else if (itm.isOwned)
                {
                    itm.statusText.text = AegisLocalization.T("ĐÃ SỞ HỮU", "OWNED");
                    itm.statusText.color = new Color(0.20f, 0.10f, 0.04f);
                    if (itm.statusBg != null)
                    {
                        itm.statusBg.sprite = MedievalUIAssetLoader.GetSprite("btn_blank_parchment_gold")
                                           ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion);
                        itm.statusBg.type = Image.Type.Sliced;
                        itm.statusBg.color = Color.white;
                    }
                    if (itm.borderImg != null) itm.borderImg.color = itm.rarityColor;
                }
                else
                {
                    itm.statusText.text = AegisLocalization.T("🔒 CHƯA MỞ", "🔒 LOCKED");
                    itm.statusText.color = new Color(0.92f, 0.85f, 0.75f);
                    if (itm.statusBg != null)
                    {
                        itm.statusBg.sprite = MedievalUIAssetLoader.GetSprite("btn_blank_black_banner")
                                           ?? MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.BlackBanner);
                        itm.statusBg.type = Image.Type.Sliced;
                        itm.statusBg.color = Color.white;
                    }
                    if (itm.borderImg != null) itm.borderImg.color = ColMutedBorder;
                }
                MakeTextCrisp(itm.statusText, new Color(0.12f, 0.06f, 0.02f, 0.95f));
            }

            if (_inspectedItem != null)
            {
                InspectCollectionItem(_inspectedItem);
            }
        }

        // =========================================================================
        // SCREEN 10: TẠM DỪNG (Pause Modal - Royal Medieval Baroque Redesign)
        // =========================================================================
        private void BuildScreen10Pause(Transform parent, Font font)
        {
            _pauseModal = CreateModalOverlay(parent, "Screen10_PauseModal");

            GameObject cardObj = new GameObject("PauseCard");
            cardObj.transform.SetParent(_pauseModal.transform, false);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned)
                          ?? MedievalUIAssetLoader.GetSprite("frame_modal_crowned")
                          ?? ArcaneAssetLoader.GetGlassBoxSprite(540, 460, 24f, ColGlassFill, ColGoldBorder, 2f);
            cardBg.type = Image.Type.Sliced;
            cardBg.color = Color.white;

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(540f, 500f);
            cardObj.transform.localScale = new Vector3(1.41f, 1.63f, 1f);

            // 1 & 2: PauseTitle & DividerWings removed as per Scene design

            // 3. Right Royal Close Button [ Crossed Swords Gold ] (Enlarged 66x66)
            CreateRoyalCloseButton(cardObj.transform, new Vector2(-38f, -89f), () => CloseModal(_pauseModal), 66f, "BtnPauseClose");

            // 4. Three Royal Action Buttons (Tiếp tục, Thiết lập, Thoát về menu)
            // Button 1: TIẾP TỤC (Radiant Gold / Dark Stone Plate)
            CreateRoyalModalActionButton(cardObj.transform, "BtnResume",
                "",
                new Vector2(0f, 70f), new Vector2(380f, 110f),
                "btn_pause_resume", Color.white, 19,
                () => CloseModal(_pauseModal), font);

            // Button 2: THIẾT LẬP (Royal Gear / Dark Stone Plate)
            CreateRoyalModalActionButton(cardObj.transform, "BtnPauseSettings",
                "",
                new Vector2(0f, -44f), new Vector2(380f, 110f),
                "btn_pause_settings", Color.white, 18,
                OpenSettingsModal, font);

            // Button 3: THOÁT VỀ MENU (Return Arrow / Dark Stone Plate)
            CreateRoyalModalActionButton(cardObj.transform, "BtnQuitToMenu",
                "",
                new Vector2(0f, -158f), new Vector2(380f, 110f),
                "btn_pause_menu", Color.white, 18,
                ReturnToMainMenuFromPause, font);

            _pauseModal.SetActive(false);
        }

        private GameObject CreateRoyalModalActionButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, string spriteName, Color textCol, int fontSize, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image img = btnObj.AddComponent<Image>();
            Sprite sp = MedievalUIAssetLoader.GetSprite(spriteName) ?? MedievalUIAssetLoader.GetSprite("btn_wood_back");
            if (sp != null)
            {
                img.sprite = sp;
                img.type = (spriteName.StartsWith("btn_pause_") && spriteName != "btn_pause_blank") ? Image.Type.Simple : Image.Type.Sliced;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)size.x, (int)size.y, 14f, new Color(0.24f, 0.16f, 0.10f, 0.95f), ColGoldBorder, 1.5f);
                img.type = Image.Type.Sliced;
            }

            Button btn = btnObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                onClick?.Invoke();
            });
            btnObj.AddComponent<MenuButtonEffect>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            if (!string.IsNullOrEmpty(text))
            {
                // Centered crisp text with royal shadow
                GameObject txtObj = new GameObject("Text");
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
                MakeTextCrisp(t);
                Shadow sh = txtObj.AddComponent<Shadow>();
                sh.effectColor = new Color(0.12f, 0.08f, 0.04f, 0.95f);
                sh.effectDistance = new Vector2(1.2f, -1.2f);
                RectTransform trt = txtObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = new Vector2(0f, 2f);
            }

            return btnObj;
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
            if (_pauseModal != null)
            {
                _pauseModal.transform.SetAsLastSibling();
                _pauseModal.SetActive(true);
                AegisAudioManager.PlayModalOpen();
            }
        }

        private void ReturnToMainMenuFromPause()
        {
            if (LichessBridge.Instance != null && LichessBridge.Instance.IsInLichessMatch)
            {
                LichessBridge.Instance.ResignMatch();
            }
            if (MechChess.AR.HybridARController.Instance != null)
            {
                MechChess.AR.HybridARController.Instance.TurnOffCamera();
            }
            if (_menuCanvasObj != null)
            {
                Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                if (c != null) c.sortingOrder = 90;
            }
            _pauseModal.SetActive(false);

            // Record match if game was in progress and moves were played
            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null && MechChess.Chess3D.ChessBoard3DManager.Instance.TotalMovesCount > 0)
            {
                int moves = MechChess.Chess3D.ChessBoard3DManager.Instance.TotalMovesCount;
                bool isAi = (_lastLaunchMode == GameLaunchMode.VersusAI);
                bool isOnline = (_lastLaunchMode == GameLaunchMode.OnlineMultiplayer);
                string oppName = isAi
                    ? (_selectedAIDifficulty == 1 ? "Cyber AI (Dễ)" :
                       _selectedAIDifficulty == 2 ? "Cyber AI (Trung bình)" :
                       _selectedAIDifficulty == 3 ? "Cyber AI (Khó)" : "Cyber AI (Rất khó)")
                    : "VS Player (Online)";

                if (UserAccountManager.Instance != null)
                {
                    UserAccountManager.Instance.RecordMatchResult("loss", oppName, isAi, "white", moves, 180f, -1, isOnline, (ok, res) =>
                    {
                        UpdateUserProfileUI();
                    });
                }
            }

            ShowMenu();
        }

        // =========================================================================
        // SCREEN 11: KẾT QUẢ TRẬN ĐẤU (Result Screen Matching Image 2 Design 100%)
        // =========================================================================
        private void BuildScreen11Result(Transform parent, Font font)
        {
            _resultScreenModal = CreateModalOverlay(parent, "Screen11_ResultScreenModal");

            // Main Card (1020 x 610) with epic chess cathedral background
            GameObject cardObj = new GameObject("ResultCard");
            cardObj.transform.SetParent(_resultScreenModal.transform, false);

            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1020f, 610f);

            Image cardBg = cardObj.AddComponent<Image>();
            Sprite bgSprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.VictoryCathedral) ?? MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned);
            if (bgSprite != null)
            {
                cardBg.sprite = bgSprite;
                cardBg.type = Image.Type.Simple;
                cardBg.color = Color.white;
            }
            else
            {
                cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1020, 610, 24f, new Color(0.02f, 0.06f, 0.12f, 0.98f), ColCyanBorder, 2f);
            }
            cardBg.raycastTarget = true;

            // Close Button [ Crossed Swords Gold ] at Top-Right
            GameObject resClose = CreateRoyalCloseButton(cardObj.transform, new Vector2(-39f, -42f), () =>
            {
                CloseModal(_resultScreenModal);
                if (_menuCanvasObj != null)
                {
                    Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                    if (c != null) c.sortingOrder = 90;
                }
                ShowMenu();
            }, 64f, "BtnCloseResult");
            if (resClose != null) resClose.transform.localScale = new Vector3(1.23f, 1.23f, 1f);

            // 3-Star Rating Podial Graphic
            GameObject starsObj = new GameObject("RatingStars");
            starsObj.transform.SetParent(cardObj.transform, false);
            RectTransform starsRt = starsObj.AddComponent<RectTransform>();
            starsRt.anchoredPosition = new Vector2(0f, 262f);
            starsRt.sizeDelta = new Vector2(160f, 48f);

            float[] starXs = { -46f, 0f, 46f };
            float[] starSizes = { 36f, 46f, 36f };
            float[] starYs = { 0f, 4f, 0f };

            for (int si = 0; si < 3; si++)
            {
                GameObject sObj = new GameObject("Star_" + si);
                sObj.transform.SetParent(starsObj.transform, false);
                Image starImg = sObj.AddComponent<Image>();
                starImg.sprite = MedievalUIAssetLoader.GetIcon(MedievalIcon.StarFilled);
                starImg.preserveAspect = true;
                starImg.raycastTarget = false;
                RectTransform srt = sObj.GetComponent<RectTransform>();
                srt.anchoredPosition = new Vector2(starXs[si], starYs[si]);
                srt.sizeDelta = new Vector2(starSizes[si], starSizes[si]);
                _resultStarImgs[si] = starImg;
            }

            // Royal Golden Crown (Emblem top center)
            GameObject crownObj = new GameObject("CrownVictory");
            crownObj.transform.SetParent(cardObj.transform, false);
            _resultCrownImg = crownObj.AddComponent<Image>();
            _resultCrownImg.sprite = MedievalUIAssetLoader.GetIcon(MedievalIcon.CrownVictory);
            _resultCrownImg.preserveAspect = true;
            _resultCrownImg.raycastTarget = false;
            RectTransform crRt = crownObj.GetComponent<RectTransform>();
            crRt.anchoredPosition = new Vector2(0f, 218f);
            crRt.sizeDelta = new Vector2(115f, 50f);

            // Left Laurel Branch (embracing Title)
            GameObject llObj = new GameObject("LaurelLeft");
            llObj.transform.SetParent(cardObj.transform, false);
            _resultLaurelLeftImg = llObj.AddComponent<Image>();
            _resultLaurelLeftImg.sprite = MedievalUIAssetLoader.GetIcon(MedievalIcon.LaurelLeft);
            _resultLaurelLeftImg.preserveAspect = true;
            _resultLaurelLeftImg.raycastTarget = false;
            RectTransform llRt = llObj.GetComponent<RectTransform>();
            llRt.anchoredPosition = new Vector2(-205f, 142f);
            llRt.sizeDelta = new Vector2(70f, 122f);

            // Right Laurel Branch (embracing Title)
            GameObject lrObj = new GameObject("LaurelRight");
            lrObj.transform.SetParent(cardObj.transform, false);
            _resultLaurelRightImg = lrObj.AddComponent<Image>();
            _resultLaurelRightImg.sprite = MedievalUIAssetLoader.GetIcon(MedievalIcon.LaurelRight);
            _resultLaurelRightImg.preserveAspect = true;
            _resultLaurelRightImg.raycastTarget = false;
            RectTransform lrRt = lrObj.GetComponent<RectTransform>();
            lrRt.anchoredPosition = new Vector2(205f, 142f);
            lrRt.sizeDelta = new Vector2(70f, 122f);

            // Title: "Chiến Thắng" / "Thất Bại" (Centered between Laurel Branches)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            _resultTitleTxt = titleObj.AddComponent<Text>();
            _resultTitleTxt.text = AegisLocalization.T("Chiến Thắng", "Victory");
            _resultTitleTxt.font = font;
            _resultTitleTxt.fontSize = 46;
            _resultTitleTxt.fontStyle = FontStyle.Bold;
            _resultTitleTxt.alignment = TextAnchor.MiddleCenter;
            _resultTitleTxt.color = new Color(0.98f, 0.90f, 0.74f, 1f); // Antique golden ivory
            _resultTitleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _resultTitleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 148f);
            tRt.sizeDelta = new Vector2(340f, 52f);

            // Subtitle Reason: "Bạn đã chiến thắng!"
            GameObject reasonObj = new GameObject("ReasonText");
            reasonObj.transform.SetParent(cardObj.transform, false);
            _resultReasonTxt = reasonObj.AddComponent<Text>();
            _resultReasonTxt.text = AegisLocalization.T("Bạn đã chiến thắng!", "Victory is yours!");
            _resultReasonTxt.font = font;
            _resultReasonTxt.fontSize = 20;
            _resultReasonTxt.fontStyle = FontStyle.Normal;
            _resultReasonTxt.alignment = TextAnchor.MiddleCenter;
            _resultReasonTxt.color = new Color(0.91f, 0.85f, 0.73f, 1f); // Warm ivory parchment
            _resultReasonTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _resultReasonTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform rRt = reasonObj.GetComponent<RectTransform>();
            rRt.anchoredPosition = new Vector2(0f, 100f);
            rRt.sizeDelta = new Vector2(520f, 28f);

            // Ornate Golden Divider Rod
            GameObject divObj = new GameObject("OrnateDivider");
            divObj.transform.SetParent(cardObj.transform, false);
            _resultDividerImg = divObj.AddComponent<Image>();
            _resultDividerImg.sprite = MedievalUIAssetLoader.GetIcon(MedievalIcon.DividerOrnate);
            _resultDividerImg.preserveAspect = true;
            _resultDividerImg.raycastTarget = false;
            RectTransform divRt = divObj.GetComponent<RectTransform>();
            divRt.anchoredPosition = new Vector2(0f, 68f);
            divRt.sizeDelta = new Vector2(500f, 22f);

            // Stats Container (3-Stripe Layout as shown in Reference Image)
            GameObject statsContainer = new GameObject("StatsContainer");
            statsContainer.transform.SetParent(cardObj.transform, false);
            RectTransform scRt = statsContainer.AddComponent<RectTransform>();
            scRt.anchoredPosition = new Vector2(0f, -30f);
            scRt.sizeDelta = new Vector2(520f, 150f);

            _resultDurationTxt = CreateStatRow(statsContainer.transform, AegisLocalization.T("Thời gian", "Duration"), "02:15", 48f, font);
            _resultMovesTxt = CreateStatRow(statsContainer.transform, AegisLocalization.T("Nước đi", "Moves"), "32", 0f, font);
            _resultScoreTxt = CreateStatRow(statsContainer.transform, AegisLocalization.T("Thưởng", "Reward"), "+1,000", -48f, font, true);

            // Left Button: "Quay lại" (Royal Shop-Tab Style: Dark Banner with Carved Gold Border)
            CreateMedievalHexButton(cardObj.transform, "BtnReturn", AegisLocalization.T("Quay lại", "Return"),
                MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_inactive") ?? MedievalUIAssetLoader.GetSprite("btn_blank_black_banner"),
                new Vector2(-150f, -165f), new Vector2(250f, 62f),
                new Color(0.92f, 0.86f, 0.74f, 1f), 20, () =>
            {
                CloseModal(_resultScreenModal);
                if (_menuCanvasObj != null)
                {
                    Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                    if (c != null) c.sortingOrder = 90;
                }
                ShowMenu();
            }, font);

            // Right Button: "Tiếp tục" (Royal Shop-Tab Style: Radiant Gold Banner)
            CreateMedievalHexButton(cardObj.transform, "BtnContinue", AegisLocalization.T("Tiếp tục", "Continue"),
                MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_active") ?? MedievalUIAssetLoader.GetSprite("btn_hex_gold_radiant"),
                new Vector2(150f, -165f), new Vector2(250f, 62f),
                new Color(0.18f, 0.08f, 0.02f, 1f), 20, () =>
            {
                CloseModal(_resultScreenModal);
                if (_menuCanvasObj != null)
                {
                    Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                    if (c != null) c.sortingOrder = 90;
                }
                if (_lastLaunchMode == GameLaunchMode.OnlineMultiplayer)
                {
                    ShowMenu();
                    NavigateToChooseMode();
                }
                else
                {
                    if (_menuCanvasObj != null) _menuCanvasObj.SetActive(false);
                    if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                    {
                        MechChess.Chess3D.ChessBoard3DManager.Instance.ResetBoard();
                    }
                    if (UIManager2D.Instance != null)
                    {
                        UIManager2D.Instance.ResetGameTimers();
                        UIManager2D.Instance.SetHUDVisible(true);
                    }
                }
            }, font);

            // Review Match Action: Elegant subtle link at bottom
            GameObject revObj = new GameObject("BtnReviewLink");
            revObj.transform.SetParent(cardObj.transform, false);
            RectTransform revRt = revObj.AddComponent<RectTransform>();
            revRt.anchoredPosition = new Vector2(0f, -225f);
            revRt.sizeDelta = new Vector2(240f, 28f);

            Button revBtn = revObj.AddComponent<Button>();
            Text revTxt = revObj.AddComponent<Text>();
            revTxt.text = AegisLocalization.T("🔍  Xem lại ván đấu", "🔍  Review Match");
            revTxt.font = font;
            revTxt.fontSize = 15;
            revTxt.fontStyle = FontStyle.Bold;
            revTxt.alignment = TextAnchor.MiddleCenter;
            revTxt.color = new Color(0.82f, 0.70f, 0.50f, 0.90f);
            revBtn.targetGraphic = revTxt;
            ColorBlock rcb = revBtn.colors;
            rcb.normalColor = Color.white;
            rcb.highlightedColor = new Color(1.15f, 1.1f, 0.9f, 1f);
            rcb.pressedColor = new Color(0.7f, 0.6f, 0.4f, 1f);
            revBtn.colors = rcb;
            revBtn.onClick.AddListener(() =>
            {
                if (_lastFinishedMatch != null)
                {
                    OpenGameReview(_lastFinishedMatch);
                }
                else
                {
                    ShowToast(AegisLocalization.T("Không có dữ liệu nước đi để xem lại!", "No move history available for review!"), ToastType.Warning);
                }
            });
            revObj.AddComponent<MenuButtonEffect>();

            _resultScreenModal.SetActive(false);
        }

        private GameObject CreateMedievalHexButton(Transform parent, string name, string text, Sprite sprite, Vector2 pos, Vector2 size, Color textCol, int fontSize, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = sprite;
            bool isBakedArt = sprite != null && (sprite.name.StartsWith("btn_pause_") || sprite.name.StartsWith("btn_wood_") || sprite.name == "btn_gold_start");
            bg.type = isBakedArt ? Image.Type.Simple : Image.Type.Sliced;
            bg.preserveAspect = isBakedArt;
            bg.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.08f, 1.05f, 0.95f, 1f);
            cb.pressedColor = new Color(0.85f, 0.82f, 0.78f, 1f);
            cb.selectedColor = Color.white;
            btn.colors = cb;
            btn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                onClick?.Invoke();
            });

            btnObj.AddComponent<MenuButtonEffect>();

            if (!isBakedArt && !string.IsNullOrEmpty(text))
            {
                GameObject txtObj = new GameObject("Text");
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
                MakeTextCrisp(t, (textCol.r < 0.5f) ? new Color(1f, 0.90f, 0.60f, 0.85f) : new Color(0.10f, 0.05f, 0.02f, 0.95f));

                RectTransform trt = txtObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
            }

            return btnObj;
        }

        private Text CreateStatRow(Transform parent, string label, string val, float y, Font font, bool isReward = false)
        {
            GameObject row = new GameObject("Row_" + label);
            row.transform.SetParent(parent, false);
            RectTransform rt = row.AddComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(500f, 44f);

            Image rowBg = row.AddComponent<Image>();
            Sprite stSprite = MedievalUIAssetLoader.GetSprite("row_stat_stripe");
            if (stSprite != null)
            {
                rowBg.sprite = stSprite;
                rowBg.type = Image.Type.Sliced;
                rowBg.color = Color.white;
            }
            else
            {
                rowBg.color = new Color(0.08f, 0.05f, 0.04f, 0.55f);
            }

            CreateTextComp(row.transform, label, 19, FontStyle.Normal, new Color(0.86f, 0.78f, 0.67f, 1f), font, new Vector2(-120f, 0f), new Vector2(220f, 32f), TextAnchor.MiddleLeft);

            GameObject valObj = new GameObject("Val");
            valObj.transform.SetParent(row.transform, false);
            Text t = valObj.AddComponent<Text>();
            t.text = val;
            t.font = font;
            t.fontSize = isReward ? 23 : 21;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleRight;
            t.color = isReward ? new Color(1.0f, 0.84f, 0.20f, 1f) : Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform vrt = valObj.GetComponent<RectTransform>();
            vrt.anchoredPosition = new Vector2(120f, 0f);
            vrt.sizeDelta = new Vector2(220f, 32f);
            return t;
        }

        public void ShowResultScreen(bool victory, string reason, int moves, float durationSec)
        {
            ShowResultScreen(victory, reason, moves, durationSec, false);
        }

        public void ShowResultScreen(bool victory, string reason, int moves, float durationSec, bool isDraw, bool isAborted = false)
        {
            if (_resultScreenModal == null || _menuCanvasObj == null)
            {
                BuildAllScreens(true);
            }
            if (_resultScreenModal == null) return;

            // Strict abort check: never treat an aborted game as a draw!
            if (!string.IsNullOrEmpty(reason) && (reason.ToLower().Contains("hủy") || reason.ToLower().Contains("abort")))
            {
                isAborted = true;
                isDraw = false;
                victory = false;
            }

            // Automatically turn off camera when match finishes
            if (MechChess.AR.HybridARController.Instance != null)
            {
                MechChess.AR.HybridARController.Instance.TurnOffCamera();
            }

            // 1. Activate Canvas and raise sortingOrder above in-game HUD (100)
            if (_menuCanvasObj != null)
            {
                _menuCanvasObj.SetActive(true);
                Canvas c = _menuCanvasObj.GetComponent<Canvas>();
                if (c != null) c.sortingOrder = 250;
            }

            // 2. Hide base screens so only modal is visible on top of 3D game
            if (_screenMainMenu != null) _screenMainMenu.SetActive(false);
            if (_screenModeSelect != null) _screenModeSelect.SetActive(false);
            if (_screenSplash != null) _screenSplash.SetActive(false);
            CloseAllModals();

            // 3. Populate dynamic text & medieval victory elements
            if (_resultTitleTxt != null)
            {
                if (isAborted)
                {
                    _resultTitleTxt.text = AegisLocalization.T("HỦY VÁN CỜ", "MATCH ABORTED");
                    _resultTitleTxt.color = new Color(1.0f, 0.68f, 0.20f);
                }
                else if (isDraw)
                {
                    _resultTitleTxt.text = AegisLocalization.T("HÒA CỜ", "MATCH DRAW");
                    _resultTitleTxt.color = new Color(0.88f, 0.90f, 0.95f);
                }
                else
                {
                    _resultTitleTxt.text = victory ? AegisLocalization.T("Chiến Thắng", "Victory") : AegisLocalization.T("Thất Bại", "Defeat");
                    _resultTitleTxt.color = victory ? new Color(0.98f, 0.90f, 0.74f, 1f) : new Color(0.95f, 0.35f, 0.35f);
                }
            }
            if (_resultReasonTxt != null)
            {
                if (isAborted)
                {
                    _resultReasonTxt.text = !string.IsNullOrEmpty(reason) ? reason : AegisLocalization.T("Trận đấu đã bị hủy!", "Match aborted!");
                    _resultReasonTxt.color = new Color(1.0f, 0.80f, 0.45f);
                }
                else
                {
                    _resultReasonTxt.text = !string.IsNullOrEmpty(reason) ? reason : (isDraw ? AegisLocalization.T("Hòa ván cờ!", "Match drawn!") : (victory ? AegisLocalization.T("Bạn đã chiến thắng!", "Victory is yours!") : AegisLocalization.T("Bạn đã thất bại!", "Defeat!")));
                    _resultReasonTxt.color = isDraw ? new Color(0.85f, 0.90f, 1.0f) : (victory ? new Color(0.91f, 0.85f, 0.73f) : new Color(1.0f, 0.65f, 0.65f));
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
                _resultScoreTxt.text = isAborted ? "+0" : (isDraw ? "+200" : (victory ? "+1,000" : "+50"));
            }
            if (_resultEloTxt != null)
            {
                if (isAborted)
                {
                    int currentElo = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentElo : 1200;
                    _resultEloTxt.text = $"<color=#80d8ff>Elo +0 ({AegisLocalization.T("Hủy ván", "Aborted")})</color> (Tổng: {currentElo})";
                }
                else
                {
                    _resultEloTxt.text = isDraw ? "<color=#80d8ff>Elo +0</color>" : (victory ? "<color=#00e676>Elo +8</color>" : "<color=#ff5252>Elo -8</color>");
                }
            }

            // Tint Crown and Laurel Branches to reflect victory vs defeat
            Color emblemTint = victory ? Color.white : (isDraw ? new Color(0.85f, 0.85f, 0.92f, 0.95f) : (isAborted ? new Color(0.75f, 0.60f, 0.45f, 0.9f) : new Color(0.65f, 0.50f, 0.45f, 0.9f)));
            if (_resultCrownImg != null) _resultCrownImg.color = emblemTint;
            if (_resultLaurelLeftImg != null) _resultLaurelLeftImg.color = emblemTint;
            if (_resultLaurelRightImg != null) _resultLaurelRightImg.color = emblemTint;
            if (_resultDividerImg != null) _resultDividerImg.color = Color.white;

            // Dynamic 3-Star Rating based on match outcome
            Sprite filledStar = MedievalUIAssetLoader.GetIcon(MedievalIcon.StarFilled);
            Sprite emptyStar = MedievalUIAssetLoader.GetIcon(MedievalIcon.StarEmpty);
            int starCount = isAborted ? 0 : (isDraw ? 1 : (victory ? 3 : 0));
            for (int si = 0; si < 3; si++)
            {
                if (_resultStarImgs[si] != null)
                {
                    bool active = (starCount == 3) || (starCount == 1 && si == 1);
                    _resultStarImgs[si].sprite = active ? filledStar : emptyStar;
                }
            }

            // Audio cues
            if (isAborted || isDraw) AegisAudioManager.PlayToast();
            else if (victory) AegisAudioManager.PlayVictorySound();
            else AegisAudioManager.PlayDefeatSound();

            // Securely record match result to Supabase database via FastAPI and local storage
            string matchRes = isAborted ? "aborted" : (isDraw ? "draw" : (victory ? "win" : "loss"));
            bool isAi = (_lastLaunchMode == GameLaunchMode.VersusAI);
            bool isOnlinePvP = (_lastLaunchMode == GameLaunchMode.OnlineMultiplayer) ||
                               (OnlineMultiplayerManager.Instance != null && OnlineMultiplayerManager.Instance.IsOnlineMatch) ||
                               (LichessBridge.Instance != null && LichessBridge.Instance.IsInLichessMatch);

            string oppName;
            int oppElo = 1200;
            if (isOnlinePvP)
            {
                if (OnlineMultiplayerManager.Instance != null && OnlineMultiplayerManager.Instance.IsOnlineMatch)
                {
                    oppName = !string.IsNullOrEmpty(OnlineMultiplayerManager.Instance.OpponentName) ? OnlineMultiplayerManager.Instance.OpponentName : "Online Rival";
                    oppElo = OnlineMultiplayerManager.Instance.OpponentElo > 0 ? OnlineMultiplayerManager.Instance.OpponentElo : 1200;
                }
                else if (LichessBridge.Instance != null && !string.IsNullOrEmpty(LichessBridge.Instance.OpponentName))
                {
                    oppName = LichessBridge.Instance.OpponentName;
                    oppElo = LichessBridge.Instance.OpponentElo > 0 ? LichessBridge.Instance.OpponentElo : 1500;
                }
                else
                {
                    oppName = "Online Rival";
                }
            }
            else
            {
                if (isAi)
                {
                    oppName = (_selectedAIDifficulty == 1 ? "Cyber AI (Dễ)" :
                               _selectedAIDifficulty == 2 ? "Cyber AI (Trung bình)" :
                               _selectedAIDifficulty == 3 ? "Cyber AI (Khó)" : "Cyber AI (Rất khó)");
                }
                else
                {
                    oppName = AegisLocalization.T("Người chơi 2 (Cục bộ)", "Player 2 (Local)");
                }
            }

            string myRole = "white";
            if (LichessBridge.Instance != null && LichessBridge.Instance.IsInLichessMatch && !LichessBridge.Instance.IsPlayerWhite)
            {
                myRole = "black";
            }
            else if (OnlineMultiplayerManager.Instance != null && OnlineMultiplayerManager.Instance.IsOnlineMatch && OnlineMultiplayerManager.Instance.MyRole == PieceColor.Black)
            {
                myRole = "black";
            }
            else if (MechChess.Chess3D.ChessBoard3DManager.Instance != null && MechChess.Chess3D.ChessBoard3DManager.Instance.playerColor == PieceColor.Black)
            {
                myRole = "black";
            }

            // Preview Elo for instant visual feedback: ONLY Online matches change Elo!
            if (isAborted)
            {
                int currentElo = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentElo : 1200;
                if (_resultEloTxt != null)
                {
                    _resultEloTxt.text = $"<color=#80d8ff>Elo +0 ({AegisLocalization.T("Hủy ván", "Aborted")})</color> (Tổng: {currentElo})";
                }
            }
            else if (isOnlinePvP)
            {
                int myElo = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentElo : 1200;
                int diff = myElo - oppElo;
                int previewDelta;
                if (Mathf.Abs(diff) <= 100) previewDelta = victory ? 8 : (isDraw ? 0 : -8);
                else if (diff > 100) previewDelta = victory ? 7 : (isDraw ? -1 : -9);
                else previewDelta = victory ? 9 : (isDraw ? 1 : -7);

                string sign = previewDelta >= 0 ? "+" : "";
                string colorHex = previewDelta > 0 ? "#00e676" : (previewDelta < 0 ? "#ff5252" : "#80d8ff");
                if (_resultEloTxt != null)
                {
                    _resultEloTxt.text = $"<color={colorHex}>Elo {sign}{previewDelta}</color> (Tổng: {myElo + previewDelta})";
                }
            }
            else
            {
                int currentElo = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentElo : 1200;
                if (_resultEloTxt != null)
                {
                    _resultEloTxt.text = $"<color=#80d8ff>Elo +0 ({AegisLocalization.T("Tập luyện", "Practice")})</color> (Tổng: {currentElo})";
                }
            }

            int safeMoves = Mathf.Max(1, moves);
            float safeDuration = Mathf.Max(5f, durationSec);

            string[] uciMoves = null;
            string[] sanMoves = null;
            string pgn = null;
            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                var uciList = MechChess.Chess3D.ChessBoard3DManager.Instance.CompletedUciMoves;
                var sanList = MechChess.Chess3D.ChessBoard3DManager.Instance.CompletedSanMoves;
                if (uciList != null && uciList.Count > 0) uciMoves = uciList.ToArray();
                if (sanList != null && sanList.Count > 0) sanMoves = sanList.ToArray();
                pgn = MechChess.Chess3D.ChessBoard3DManager.Instance.GetCurrentMatchPGN();
            }

            _lastFinishedMatch = new MatchHistoryItem
            {
                id = "last_" + System.DateTime.UtcNow.Ticks,
                opponent_name = oppName,
                is_ai = !isOnlinePvP,
                player_role = myRole,
                result = matchRes,
                moves_count = safeMoves,
                duration_sec = safeDuration,
                duration_seconds = safeDuration,
                ended_at = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                created_at = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                moves = uciMoves,
                san_moves = sanMoves,
                pgn = pgn
            };

            if (UserAccountManager.Instance != null)
            {
                UserAccountManager.Instance.RecordMatchResult(matchRes, oppName, isAi: !isOnlinePvP, myRole, safeMoves, safeDuration, oppElo, isOnlinePvP, uciMoves, sanMoves, pgn, (ok, res) =>
                {
                    if (res != null)
                    {
                        if (res.analysis != null && _lastFinishedMatch != null)
                        {
                            _lastFinishedMatch.analysis_json = JsonUtility.ToJson(res.analysis);
                        }
                        if (_resultEloTxt != null)
                        {
                            if (isAborted)
                            {
                                _resultEloTxt.text = $"<color=#80d8ff>Elo +0 ({AegisLocalization.T("Hủy ván", "Aborted")})</color> (Tổng: {res.new_elo})";
                            }
                            else if (isOnlinePvP)
                            {
                                string sign = res.elo_change >= 0 ? "+" : "";
                                string colorHex = res.elo_change > 0 ? "#00e676" : (res.elo_change < 0 ? "#ff5252" : "#80d8ff");
                                _resultEloTxt.text = $"<color={colorHex}>Elo {sign}{res.elo_change}</color> (Tổng: {res.new_elo})";
                            }
                        }
                    }
                    UpdateUserProfileUI();
                });
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

            // Centered Main Cyber Profile Card (1380 x 740) - Spacious, Royal, High-Legibility
            GameObject cardObj = new GameObject("ProfileCard");
            cardObj.transform.SetParent(_profileModal.transform, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1380f, 740f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned) ?? ArcaneAssetLoader.GetGlassBoxSprite(1380, 740, 22f, new Color(0.02f, 0.05f, 0.10f, 0.96f), ColCyanBorder, 2.5f);
            cardBg.type = Image.Type.Sliced;
            cardBg.color = Color.white;
            cardBg.raycastTarget = true; // Blocks click from closing modal when tapping inside

            // Close Button [ Crossed Swords Gold ] at Top-Right
            GameObject resClose = CreateRoyalCloseButton(cardObj.transform, new Vector2(-39f, -42f), () =>
            {
                CloseModal(_profileModal);
            }, 64f, "BtnClose");

            // =====================================================================
            // LEFT PANEL: Avatar + Info (Top) & Stats Table Card (Bottom)
            // =====================================================================
            GameObject leftPanel = new GameObject("LeftPanel");
            leftPanel.transform.SetParent(cardObj.transform, false);
            RectTransform lpRt = leftPanel.AddComponent<RectTransform>();
            lpRt.anchorMin = new Vector2(0.5f, 0.5f);
            lpRt.anchorMax = new Vector2(0.5f, 0.5f);
            lpRt.pivot = new Vector2(0.5f, 0.5f);
            lpRt.anchoredPosition = new Vector2(-340f, -15f);
            lpRt.sizeDelta = new Vector2(560f, 580f);

            // Circular Avatar Frame with glowing cyan ring (104x104)
            GameObject avFrame = new GameObject("AvatarFrame");
            avFrame.transform.SetParent(leftPanel.transform, false);
            Image avFrameImg = avFrame.AddComponent<Image>();
            avFrameImg.sprite = ArcaneAssetLoader.GetCircleSprite(104, new Color(0.04f, 0.12f, 0.22f, 0.95f), ColCyanGlow, 2.5f);
            RectTransform afRt = avFrame.GetComponent<RectTransform>();
            afRt.anchoredPosition = new Vector2(-200f, 200f);
            afRt.sizeDelta = new Vector2(104f, 104f);

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
            avRt.sizeDelta = new Vector2(92f, 92f);

            // Username: "Player001" (Bold 30 White)
            string currentName = "Player001";
            if (UserAccountManager.Instance != null && !string.IsNullOrEmpty(UserAccountManager.Instance.CurrentUsername))
                currentName = UserAccountManager.Instance.CurrentUsername;
            _profileModalNameComp = CreateTextComp(leftPanel.transform, currentName, 30, FontStyle.Bold, ColTextWhite, font, new Vector2(-20f, 225f), new Vector2(280f, 38f), TextAnchor.MiddleLeft);

            // Level: "Lv. 1" (Cyan 20 Bold)
            _profileModalLevelComp = CreateTextComp(leftPanel.transform, "Lv. 1", 20, FontStyle.Bold, ColCyanGlow, font, new Vector2(-20f, 192f), new Vector2(140f, 26f), TextAnchor.MiddleLeft);

            // EXP Progress Bar (Width 200px, height 16px)
            GameObject expBarObj = new GameObject("ExpBarBg");
            expBarObj.transform.SetParent(leftPanel.transform, false);
            Image expBg = expBarObj.AddComponent<Image>();
            expBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(200, 16, 8f, new Color(0.05f, 0.10f, 0.20f, 0.95f), new Color(0.10f, 0.35f, 0.55f, 0.8f), 1f);
            expBg.type = Image.Type.Sliced;
            RectTransform expRt = expBarObj.GetComponent<RectTransform>();
            expRt.anchoredPosition = new Vector2(-20f, 160f);
            expRt.sizeDelta = new Vector2(200f, 16f);

            GameObject expFillObj = new GameObject("ExpFill");
            expFillObj.transform.SetParent(expBarObj.transform, false);
            Image expFill = expFillObj.AddComponent<Image>();
            expFill.sprite = ArcaneAssetLoader.GetGlassBoxSprite(120, 16, 8f, ColCyanGlow, ColCyanGlow, 0f);
            expFill.type = Image.Type.Sliced;
            _profileModalExpFillRt = expFillObj.GetComponent<RectTransform>();
            _profileModalExpFillRt.anchorMin = new Vector2(0f, 0.5f);
            _profileModalExpFillRt.anchorMax = new Vector2(0f, 0.5f);
            _profileModalExpFillRt.pivot = new Vector2(0f, 0.5f);
            _profileModalExpFillRt.anchoredPosition = Vector2.zero;
            _profileModalExpFillRt.sizeDelta = new Vector2(0f, 16f);

            _profileModalExpComp = CreateTextComp(leftPanel.transform, "0 / 100", 17, FontStyle.Bold, new Color(0.88f, 0.94f, 1.0f), font, new Vector2(140f, 160f), new Vector2(110f, 26f), TextAnchor.MiddleLeft);

            // ---------------------------------------------------------------------
            // Stats Table Card (Lower Left - Matching Reference Design)
            // ---------------------------------------------------------------------
            GameObject statsCard = new GameObject("StatsCard");
            statsCard.transform.SetParent(leftPanel.transform, false);
            Image scBg = statsCard.AddComponent<Image>();
            scBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(530, 270, 16f, new Color(0.02f, 0.06f, 0.12f, 0.94f), ColMutedBorder, 1.2f);
            scBg.type = Image.Type.Sliced;
            RectTransform scRt = statsCard.GetComponent<RectTransform>();
            scRt.anchoredPosition = new Vector2(0f, -15f);
            scRt.sizeDelta = new Vector2(530f, 270f);

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
                drt.sizeDelta = new Vector2(1.5f, 210f);
            }

            // 1 Horizontal Divider
            GameObject hDivObj = new GameObject("RowDiv");
            hDivObj.transform.SetParent(statsCard.transform, false);
            Image hDivImg = hDivObj.AddComponent<Image>();
            hDivImg.color = new Color(0.08f, 0.28f, 0.45f, 0.50f);
            RectTransform hdrt = hDivObj.GetComponent<RectTransform>();
            hdrt.anchoredPosition = new Vector2(0f, -8f);
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
                CreateTextComp(statsCard.transform, statHeaders[i], 21, FontStyle.Bold, new Color(0.80f, 0.88f, 0.98f), font, new Vector2(colX[i], 82f), new Vector2(110f, 30f), TextAnchor.MiddleCenter);
            }

            // Row 1: Đấu Online (PvP) - Elo, Thắng, Thua, Hòa
            int currentElo = 1200;
            if (UserAccountManager.Instance != null && UserAccountManager.Instance.CurrentElo > 0)
                currentElo = UserAccountManager.Instance.CurrentElo;

            // Col 0: Elo + Subtitle "Đấu Online"
            _profileModalEloComp = CreateTextComp(statsCard.transform, currentElo.ToString(), 30, FontStyle.Bold, ColGoldBorder, font, new Vector2(colX[0], 38f), new Vector2(110f, 32f), TextAnchor.MiddleCenter);
            _profileModalSubPvpLabel = CreateTextComp(statsCard.transform, AegisLocalization.T("Đấu Online", "Online PvP"), 13, FontStyle.Bold, ColCyanGlow, font, new Vector2(colX[0], 14f), new Vector2(110f, 18f), TextAnchor.MiddleCenter);

            // Col 1, 2, 3: Thắng, Thua, Hòa Online
            _profileModalPvpWinsComp = CreateTextComp(statsCard.transform, "0", 26, FontStyle.Bold, ColTextWhite, font, new Vector2(colX[1], 28f), new Vector2(110f, 36f), TextAnchor.MiddleCenter);
            _profileModalPvpLossesComp = CreateTextComp(statsCard.transform, "0", 26, FontStyle.Bold, ColTextWhite, font, new Vector2(colX[2], 28f), new Vector2(110f, 36f), TextAnchor.MiddleCenter);
            _profileModalPvpDrawsComp = CreateTextComp(statsCard.transform, "0", 26, FontStyle.Bold, ColTextWhite, font, new Vector2(colX[3], 28f), new Vector2(110f, 36f), TextAnchor.MiddleCenter);

            _profileModalWinsComp = _profileModalPvpWinsComp;
            _profileModalLossesComp = _profileModalPvpLossesComp;
            _profileModalDrawsComp = _profileModalPvpDrawsComp;

            // Row 2: Đấu Với Máy (Vs AI) - Crest + Tier, Thắng, Thua, Hòa
            // Col 0: Rank Crest Icon + Subtitle "Đấu Với Máy"
            GameObject rankCrestObj = new GameObject("RankCrest");
            rankCrestObj.transform.SetParent(statsCard.transform, false);
            Image rankCrestImg = rankCrestObj.AddComponent<Image>();
            Sprite crestSp = ArcaneAssetLoader.LoadArcaneSprite("profile_rank_crest.png");
            if (crestSp != null) rankCrestImg.sprite = crestSp;
            else rankCrestImg.color = ColCyanGlow;
            rankCrestImg.preserveAspect = true;
            RectTransform rcRt = rankCrestObj.GetComponent<RectTransform>();
            rcRt.anchoredPosition = new Vector2(colX[0], -48f);
            rcRt.sizeDelta = new Vector2(32f, 32f);

            _profileModalRankTierComp = CreateTextComp(statsCard.transform, AegisLocalization.T("Đấu Với Máy", "Vs Computer"), 13, FontStyle.Bold, new Color(0.80f, 0.88f, 0.98f), font, new Vector2(colX[0], -72f), new Vector2(110f, 18f), TextAnchor.MiddleCenter);

            // Col 1, 2, 3: Thắng, Thua, Hòa Máy
            _profileModalAiWinsComp = CreateTextComp(statsCard.transform, "0", 26, FontStyle.Bold, ColTextWhite, font, new Vector2(colX[1], -60f), new Vector2(110f, 36f), TextAnchor.MiddleCenter);
            _profileModalAiLossesComp = CreateTextComp(statsCard.transform, "0", 26, FontStyle.Bold, ColTextWhite, font, new Vector2(colX[2], -60f), new Vector2(110f, 36f), TextAnchor.MiddleCenter);
            _profileModalAiDrawsComp = CreateTextComp(statsCard.transform, "0", 26, FontStyle.Bold, ColTextWhite, font, new Vector2(colX[3], -60f), new Vector2(110f, 36f), TextAnchor.MiddleCenter);

            // Switch Account / Logout Button
            _profileSwitchAccountBtnObj = CreateMenuActionButton(
                leftPanel.transform,
                "BtnSwitchAccount",
                AegisLocalization.T("⇄  ĐỔI TÀI KHOẢN / ĐĂNG XUẤT", "⇄  SWITCH ACCOUNT / LOGOUT"),
                new Vector2(0f, -220f),
                new Vector2(400f, 48f),
                new Color(0.10f, 0.18f, 0.30f, 0.95f),
                ColCyanGlow,
                16,
                () =>
                {
                    CloseModal(_profileModal);
                    if (UserAccountManager.Instance != null)
                    {
                        UserAccountManager.Instance.Logout();
                    }
                    ShowLoginModal();
                },
                font,
                ColCyanBorder,
                1.4f
            );

            // =====================================================================
            // RIGHT PANEL: Sub-Tabs & Match History / Achievements / Skin Cards
            // =====================================================================
            GameObject rightPanel = new GameObject("RightPanel");
            rightPanel.transform.SetParent(cardObj.transform, false);
            RectTransform rpRt = rightPanel.AddComponent<RectTransform>();
            rpRt.anchorMin = new Vector2(0.5f, 0.5f);
            rpRt.anchorMax = new Vector2(0.5f, 0.5f);
            rpRt.pivot = new Vector2(0.5f, 0.5f);
            rpRt.anchoredPosition = new Vector2(320f, -15f);
            rpRt.sizeDelta = new Vector2(640f, 580f);

            // Category Sub-Tabs: Lịch sử đấu, Thành tích, Skin (Royal Golden Tab Styling)
            GameObject profileTabs = new GameObject("ProfileTabs");
            profileTabs.transform.SetParent(rightPanel.transform, false);
            RectTransform ptRt = profileTabs.AddComponent<RectTransform>();
            ptRt.anchoredPosition = new Vector2(0f, 245f);
            ptRt.sizeDelta = new Vector2(620f, 52f);

            string[] pTabs = {
                AegisLocalization.T("Lịch sử đấu", "Match History"),
                AegisLocalization.T("Thành tích", "Achievements"),
                AegisLocalization.T("Skin", "Skins")
            };
            Sprite pTabActSp = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_active")
                               ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_active")
                               ?? ArcaneAssetLoader.LoadArcaneSprite("profile_tab_active.png");
            Sprite pTabInactSp = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_inactive")
                                 ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_inactive")
                                 ?? ArcaneAssetLoader.LoadArcaneSprite("profile_tab_inactive.png");

            float[] tabX = { -205f, 0f, 205f };
            float[] tabW = { 200f, 200f, 200f };
            for (int i = 0; i < 3; i++)
            {
                int tabIdx = i;
                bool active = (i == _selectedProfileTab);
                GameObject tObj = new GameObject("Tab_" + pTabs[i]);
                tObj.transform.SetParent(profileTabs.transform, false);

                Image bg = tObj.AddComponent<Image>();
                bg.sprite = active ? pTabActSp : pTabInactSp;
                bg.type = Image.Type.Sliced;
                _profileTabBgs[i] = bg;

                Button btn = tObj.AddComponent<Button>();
                btn.onClick.AddListener(() => SwitchProfileTab(tabIdx));
                tObj.AddComponent<MenuButtonEffect>();

                RectTransform rt = tObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(tabX[i], 0f);
                rt.sizeDelta = new Vector2(tabW[i], 48f);

                GameObject txtObj = new GameObject("Txt");
                txtObj.transform.SetParent(tObj.transform, false);
                Text txtComp = txtObj.AddComponent<Text>();
                txtComp.text = pTabs[i];
                txtComp.font = font;
                txtComp.fontSize = 20;
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
            _matchHistoryRowsParent = historyContainer.transform;
            Image hcBg = historyContainer.AddComponent<Image>();
            hcBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(620, 470, 18f, new Color(0.02f, 0.05f, 0.10f, 0.94f), ColMutedBorder, 1.2f);
            hcBg.type = Image.Type.Sliced;
            RectTransform hcRt = historyContainer.GetComponent<RectTransform>();
            hcRt.anchoredPosition = new Vector2(0f, -20f);
            hcRt.sizeDelta = new Vector2(620f, 470f);

            // Horizontal row dividers in Match History
            float[] rowDivY = { 60f, -60f };
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

            // Real dynamic match rows populated directly from player history
            LoadDynamicMatchHistory();

            // Tab 1 Content: Achievements (Thành tích)
            GameObject achContainer = new GameObject("Tab1_Achievements");
            achContainer.transform.SetParent(rightPanel.transform, false);
            _profileTabContainers[1] = achContainer;
            Image achBg = achContainer.AddComponent<Image>();
            achBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(620, 470, 18f, new Color(0.02f, 0.05f, 0.10f, 0.94f), ColMutedBorder, 1.2f);
            achBg.type = Image.Type.Sliced;
            RectTransform achRt = achContainer.GetComponent<RectTransform>();
            achRt.anchoredPosition = new Vector2(0f, -20f);
            achRt.sizeDelta = new Vector2(620f, 470f);

            string[] achTitles = {
                AegisLocalization.T("Chiến Tướng Khởi Nguyên", "Genesis Knight"),
                AegisLocalization.T("Đại Sư Đấu Máy", "AI Grandmaster"),
                AegisLocalization.T("Bậc Thầy AR", "AR Pioneer"),
                AegisLocalization.T("Huyền Thoại Bất Bại", "Undefeated Legend")
            };
            string[] achProgress = {
                AegisLocalization.T("100 / 100 (Đạt)", "100 / 100 (Done)"),
                AegisLocalization.T("Hoàn thành", "Completed"),
                "38 / 50",
                "12 / 20"
            };
            for (int i = 0; i < 4; i++)
            {
                CreateTextComp(achContainer.transform, "🏆  " + achTitles[i], 21, FontStyle.Bold, ColTextWhite, font, new Vector2(-110f, 135f - i * 90f), new Vector2(350f, 34f), TextAnchor.MiddleLeft);
                CreateTextComp(achContainer.transform, achProgress[i], 19, FontStyle.Bold, ColCyanGlow, font, new Vector2(190f, 135f - i * 90f), new Vector2(150f, 34f), TextAnchor.MiddleRight);
            }
            achContainer.SetActive(false);

            // Tab 2 Content: Interactive Skin Collection & Armory Chooser
            GameObject skinTabContainer = new GameObject("Tab2_Skins");
            skinTabContainer.transform.SetParent(rightPanel.transform, false);
            _profileTabContainers[2] = skinTabContainer;
            Image stkBg = skinTabContainer.AddComponent<Image>();
            stkBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(620, 470, 18f, new Color(0.02f, 0.05f, 0.10f, 0.94f), ColMutedBorder, 1.2f);
            stkBg.type = Image.Type.Sliced;
            RectTransform stkRt = skinTabContainer.GetComponent<RectTransform>();
            stkRt.anchoredPosition = new Vector2(0f, -20f);
            stkRt.sizeDelta = new Vector2(620f, 470f);

            // Scroll View for Skins
            GameObject skinScrollObj = new GameObject("SkinsScrollView");
            skinScrollObj.transform.SetParent(skinTabContainer.transform, false);
            RectTransform ssRt = skinScrollObj.AddComponent<RectTransform>();
            ssRt.anchorMin = new Vector2(0.5f, 0.5f);
            ssRt.anchorMax = new Vector2(0.5f, 0.5f);
            ssRt.pivot = new Vector2(0.5f, 0.5f);
            ssRt.anchoredPosition = new Vector2(0f, 32f);
            ssRt.sizeDelta = new Vector2(590f, 370f);

            Image ssImg = skinScrollObj.AddComponent<Image>();
            ssImg.color = Color.clear;
            ssImg.raycastTarget = true;
            skinScrollObj.AddComponent<RectMask2D>();

            ScrollRect skinSr = skinScrollObj.AddComponent<ScrollRect>();
            skinSr.horizontal = false;
            skinSr.vertical = true;
            skinSr.movementType = ScrollRect.MovementType.Clamped;
            skinSr.scrollSensitivity = 25f;

            GameObject skinContentObj = new GameObject("SkinsContent");
            skinContentObj.transform.SetParent(skinScrollObj.transform, false);
            RectTransform scContentRt = skinContentObj.AddComponent<RectTransform>();
            scContentRt.anchorMin = new Vector2(0f, 1f);
            scContentRt.anchorMax = new Vector2(1f, 1f);
            scContentRt.pivot = new Vector2(0.5f, 1f);
            scContentRt.anchoredPosition = Vector2.zero;
            scContentRt.sizeDelta = new Vector2(0f, 300f);

            VerticalLayoutGroup vlg = skinContentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(4, 4, 6, 6);

            ContentSizeFitter csf = skinContentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            skinSr.content = scContentRt;
            skinSr.viewport = ssRt;
            _profileSkinsScrollContent = scContentRt;

            // Populate skin cards initially
            PopulateProfileSkinsUI(font);

            // Bottom Full Collection Button
            GameObject openColBtnObj = new GameObject("BtnOpenFullCollection");
            openColBtnObj.transform.SetParent(skinTabContainer.transform, false);
            Image ocBg = openColBtnObj.AddComponent<Image>();
            ocBg.sprite = MedievalUIAssetLoader.GetSprite("btn_blank_parchment_gold")
                          ?? ArcaneAssetLoader.GetGlassBoxSprite(420, 46, 12f, ColGoldCta, ColGoldBorder, 1.5f);
            ocBg.type = Image.Type.Sliced;
            Button ocBtn = openColBtnObj.AddComponent<Button>();
            ocBtn.onClick.AddListener(() =>
            {
                CloseModal(_profileModal);
                OpenCollectionModal();
            });
            openColBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform ocRt = openColBtnObj.GetComponent<RectTransform>();
            ocRt.anchoredPosition = new Vector2(0f, -195f);
            ocRt.sizeDelta = new Vector2(420f, 46f);
            CreateTextComp(openColBtnObj.transform, AegisLocalization.T("🛡️  MỞ KHO BỘ SƯU TẬP TOÀN DIỆN", "🛡️  OPEN FULL COLLECTION"), 18, FontStyle.Bold, ColDarkBtnText, font, Vector2.zero, new Vector2(400f, 30f));

            skinTabContainer.SetActive(false);

            _profileModal.SetActive(false);
        }

        private void SwitchProfileTab(int tabIndex)
        {
            if (_selectedProfileTab != tabIndex) AegisAudioManager.PlayTabSwitch();
            _selectedProfileTab = tabIndex;
            Sprite activeSprite = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_active")
                                  ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_active")
                                  ?? ArcaneAssetLoader.LoadArcaneSprite("profile_tab_active.png");
            Sprite inactiveSprite = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_inactive")
                                    ?? MedievalUIAssetLoader.GetSprite("tab_parchment_gold_inactive")
                                    ?? ArcaneAssetLoader.LoadArcaneSprite("profile_tab_inactive.png");

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
                    _profileTabTexts[i].fontSize = 20;
                }
                if (_profileTabContainers[i] != null)
                {
                    _profileTabContainers[i].SetActive(i == tabIndex);
                }
            }

            if (tabIndex == 0)
            {
                LoadDynamicMatchHistory();
            }
            else if (tabIndex == 2)
            {
                PopulateProfileSkinsUI();
            }
        }

        private void PopulateProfileSkinsUI(Font font = null)
        {
            if (_profileSkinsScrollContent == null) return;
            if (font == null) font = GetDefaultFont();

            if (_allCollectionItems.Count == 0)
            {
                InitializeCollectionData();
            }
            RefreshCollectionUI();

            // Clear previous items
            for (int i = _profileSkinsScrollContent.childCount - 1; i >= 0; i--)
            {
                Destroy(_profileSkinsScrollContent.GetChild(i).gameObject);
            }

            List<CollectionItemData> skins = _allCollectionItems.FindAll(x => x.category == 1);
            if (skins == null || skins.Count == 0) return;

            Sprite cardBgSp = ArcaneAssetLoader.GetGlassBoxSprite(570, 72, 12f, new Color(0.03f, 0.07f, 0.14f, 0.95f), ColCyanBorder, 1.2f);
            Sprite activeBtnSp = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_active")
                                 ?? ArcaneAssetLoader.GetGlassBoxSprite(140, 36, 8f, ColGoldCta, ColGoldBorder, 1.5f);
            Sprite inactiveBtnSp = MedievalUIAssetLoader.GetSprite("tab_tutorial_gold_inactive")
                                   ?? ArcaneAssetLoader.GetGlassBoxSprite(140, 36, 8f, new Color(0.10f, 0.20f, 0.35f, 0.95f), ColCyanGlow, 1.2f);

            foreach (var item in skins)
            {
                if (item == null) continue;
                bool isEquipped = IsCollectionItemEquipped(item);

                GameObject cardObj = new GameObject("SkinCard_" + item.id);
                cardObj.transform.SetParent(_profileSkinsScrollContent, false);

                Image bg = cardObj.AddComponent<Image>();
                bg.sprite = cardBgSp;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;

                LayoutElement le = cardObj.AddComponent<LayoutElement>();
                le.minHeight = 72f;
                le.preferredHeight = 72f;
                le.flexibleWidth = 1f;

                // Left: Icon Frame & Sprite
                GameObject iconFrame = new GameObject("IconFrame");
                iconFrame.transform.SetParent(cardObj.transform, false);
                Image frameImg = iconFrame.AddComponent<Image>();
                frameImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(56, 56, 8f, new Color(0.02f, 0.05f, 0.10f, 0.9f), item.rarityColor, 1.2f);
                frameImg.type = Image.Type.Sliced;
                RectTransform ifRt = iconFrame.GetComponent<RectTransform>();
                ifRt.anchorMin = new Vector2(0f, 0.5f);
                ifRt.anchorMax = new Vector2(0f, 0.5f);
                ifRt.pivot = new Vector2(0f, 0.5f);
                ifRt.anchoredPosition = new Vector2(10f, 0f);
                ifRt.sizeDelta = new Vector2(56f, 56f);

                GameObject iconArt = new GameObject("IconArt");
                iconArt.transform.SetParent(iconFrame.transform, false);
                Image artImg = iconArt.AddComponent<Image>();
                artImg.sprite = GetCollectionItemSprite(item);
                artImg.preserveAspect = true;
                RectTransform iaRt = iconArt.GetComponent<RectTransform>();
                iaRt.anchorMin = new Vector2(0.5f, 0.5f);
                iaRt.anchorMax = new Vector2(0.5f, 0.5f);
                iaRt.pivot = new Vector2(0.5f, 0.5f);
                iaRt.anchoredPosition = Vector2.zero;
                iaRt.sizeDelta = new Vector2(48f, 48f);

                // Middle: Name & Rarity
                GameObject textBlock = new GameObject("TextBlock");
                textBlock.transform.SetParent(cardObj.transform, false);
                RectTransform tbRt = textBlock.AddComponent<RectTransform>();
                tbRt.anchorMin = new Vector2(0f, 0.5f);
                tbRt.anchorMax = new Vector2(1f, 0.5f);
                tbRt.pivot = new Vector2(0f, 0.5f);
                tbRt.anchoredPosition = new Vector2(76f, 0f);
                tbRt.sizeDelta = new Vector2(-235f, 54f);

                Text titleTxt = CreateTextComp(textBlock.transform, AegisLocalization.T(item.nameVi, item.nameEn), 18, FontStyle.Bold, ColTextWhite, font, new Vector2(0f, 12f), new Vector2(300f, 26f), TextAnchor.MiddleLeft);
                titleTxt.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                titleTxt.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                titleTxt.rectTransform.pivot = new Vector2(0f, 0.5f);
                titleTxt.rectTransform.anchoredPosition = new Vector2(0f, 12f);
                titleTxt.rectTransform.sizeDelta = new Vector2(0f, 26f);

                Text subTxt = CreateTextComp(textBlock.transform, item.rarity + "  •  " + AegisLocalization.T(item.typeVi, item.typeEn), 13, FontStyle.Bold, item.rarityColor, font, new Vector2(0f, -12f), new Vector2(300f, 22f), TextAnchor.MiddleLeft);
                subTxt.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                subTxt.rectTransform.anchorMax = new Vector2(1f, 0.5f);
                subTxt.rectTransform.pivot = new Vector2(0f, 0.5f);
                subTxt.rectTransform.anchoredPosition = new Vector2(0f, -12f);
                subTxt.rectTransform.sizeDelta = new Vector2(0f, 22f);

                // Right: Action Button or Equipped Badge
                GameObject actionObj = new GameObject("ActionSlot");
                actionObj.transform.SetParent(cardObj.transform, false);
                RectTransform actRt = actionObj.AddComponent<RectTransform>();
                actRt.anchorMin = new Vector2(1f, 0.5f);
                actRt.anchorMax = new Vector2(1f, 0.5f);
                actRt.pivot = new Vector2(1f, 0.5f);
                actRt.anchoredPosition = new Vector2(-12f, 0f);
                actRt.sizeDelta = new Vector2(142f, 38f);

                Image actBg = actionObj.AddComponent<Image>();
                actBg.type = Image.Type.Sliced;

                if (isEquipped)
                {
                    actBg.sprite = activeBtnSp;
                    actBg.color = Color.white;
                    CreateTextComp(actionObj.transform, AegisLocalization.T("✦ ĐANG DÙNG ✦", "✦ EQUIPPED ✦"), 14, FontStyle.Bold, ColDarkBtnText, font, Vector2.zero, new Vector2(138f, 32f));
                }
                else if (item.isOwned)
                {
                    actBg.sprite = inactiveBtnSp;
                    actBg.color = Color.white;
                    Button btn = actionObj.AddComponent<Button>();
                    actionObj.AddComponent<MenuButtonEffect>();

                    var currentItem = item;
                    btn.onClick.AddListener(() =>
                    {
                        PlayerPrefs.SetString(currentItem.prefsKey, currentItem.nameVi);
                        PlayerPrefs.Save();
                        AegisAudioManager.PlayButtonClick();
                        ShowToast(AegisLocalization.T($"Đã trang bị {currentItem.nameVi}!", $"Equipped {currentItem.nameEn}!"), ToastType.Success);
                        PopulateProfileSkinsUI(font);
                    });

                    CreateTextComp(actionObj.transform, AegisLocalization.T("TRANG BỊ", "EQUIP"), 15, FontStyle.Bold, ColTextWhite, font, Vector2.zero, new Vector2(138f, 32f));
                }
                else
                {
                    actBg.sprite = inactiveBtnSp;
                    actBg.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);
                    Button btn = actionObj.AddComponent<Button>();
                    actionObj.AddComponent<MenuButtonEffect>();

                    btn.onClick.AddListener(() =>
                    {
                        CloseModal(_profileModal);
                        OpenShopModal();
                    });

                    CreateTextComp(actionObj.transform, AegisLocalization.T("MUA TẠI SHOP", "SHOP"), 14, FontStyle.Bold, new Color(0.85f, 0.85f, 0.85f), font, Vector2.zero, new Vector2(138f, 32f));
                }
            }
        }

        public void ShowProfileDetailsModal()
        {
            if (_profileModal != null)
            {
                CloseAllModals();
                _profileModal.SetActive(true);
                AegisAudioManager.PlayModalOpen();
                if (_profileSwitchAccountBtnObj != null) _profileSwitchAccountBtnObj.SetActive(true);
                UpdateUserProfileUI();
                LoadDynamicMatchHistory();
                PopulateProfileSkinsUI();
                if (UserAccountManager.Instance != null && UserAccountManager.Instance.IsLoggedIn)
                {
                    UserAccountManager.Instance.FetchProfile((ok, prof) =>
                    {
                        if (ok) UpdateUserProfileUI();
                    });
                }
            }
        }

        public void ShowOtherPlayerProfile(string playerName, int elo, Sprite avatar = null)
        {
            if (_profileModal == null) return;
            CloseAllModals();
            _profileModal.SetActive(true);
            AegisAudioManager.PlayModalOpen();

            if (_profileSwitchAccountBtnObj != null) _profileSwitchAccountBtnObj.SetActive(false);

            if (_profileModalNameComp != null) _profileModalNameComp.text = playerName;
            int estLevel = Mathf.Max(1, elo / 250);
            if (_profileModalLevelComp != null) _profileModalLevelComp.text = $"Lv. {estLevel}";
            if (_profileModalEloComp != null) _profileModalEloComp.text = elo.ToString();

            int estWins = Mathf.Max(2, (elo - 400) / 35);
            int estLosses = Mathf.Max(1, estWins / 2);
            int estDraws = Mathf.Max(0, estWins / 8);

            if (_profileModalPvpWinsComp != null) _profileModalPvpWinsComp.text = estWins.ToString();
            if (_profileModalPvpLossesComp != null) _profileModalPvpLossesComp.text = estLosses.ToString();
            if (_profileModalPvpDrawsComp != null) _profileModalPvpDrawsComp.text = estDraws.ToString();

            if (_profileModalExpComp != null) _profileModalExpComp.text = AegisLocalization.T("Kỳ Thủ Trực Tuyến", "Online Master");
            if (_profileModalExpFillRt != null) _profileModalExpFillRt.sizeDelta = new Vector2(180f, 16f);
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

            // 1. Grand Royal Crowned Card Frame (1400 x 780)
            GameObject cardObj = new GameObject("LearnCard");
            cardObj.transform.SetParent(_learnScreenModal.transform, false);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned) 
                          ?? MedievalUIAssetLoader.GetSprite("frame_modal_crowned")
                          ?? ArcaneAssetLoader.GetGlassBoxSprite(1400, 780, 28f, ColGlassFill, ColCyanBorder, 2f);
            cardBg.type = Image.Type.Simple;
            cardBg.preserveAspect = false;
            cardBg.color = Color.white;

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1400f, 780f);

            // Royal Close Button [ Crossed Swords / Wood Close ] (Top-Right, Prominent 64x64)
            CreateRoyalCloseButton(cardObj.transform, new Vector2(-28f, -24f), () => _learnScreenModal.SetActive(false), 64f, "BtnCloseLearn");

            // Centered Title Banner (Royal Parchment Banner safely below the Crown arch)
            GameObject titleBannerObj = new GameObject("HeaderTitleBanner");
            titleBannerObj.transform.SetParent(cardObj.transform, false);
            RectTransform tbRt = titleBannerObj.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.5f, 1f);
            tbRt.anchorMax = new Vector2(0.5f, 1f);
            tbRt.pivot = new Vector2(0.5f, 1f);
            tbRt.anchoredPosition = new Vector2(0f, -80f);
            tbRt.sizeDelta = new Vector2(460f, 68f);

            Image tbImg = titleBannerObj.AddComponent<Image>();
            tbImg.sprite = MedievalUIAssetLoader.GetSprite("banner_parchment_title")
                        ?? MedievalUIAssetLoader.GetSprite("parchment_box_clean");
            tbImg.type = Image.Type.Simple;
            tbImg.preserveAspect = true;

            GameObject titleTxtObj = new GameObject("TitleText");
            titleTxtObj.transform.SetParent(titleBannerObj.transform, false);
            Text titleTxt = titleTxtObj.AddComponent<Text>();
            titleTxt.text = AegisLocalization.T("KHAI CUỘC - PHÒNG THỦ SICILIAN", "OPENING - SICILIAN DEFENSE");
            titleTxt.font = font;
            titleTxt.fontSize = 20;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.24f, 0.12f, 0.05f); // Deep antique imperial chestnut
            MakeTextCrisp(titleTxt, new Color(1.0f, 0.94f, 0.70f, 0.85f));
            RectTransform ttRt = titleTxtObj.GetComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.offsetMin = new Vector2(24f, 4f);
            ttRt.offsetMax = new Vector2(-24f, -4f);

            // ---------------------------------------------------------------------
            // 2. Left Column: Lessons List (Medieval Wood & Gothic Gold Cards)
            // ---------------------------------------------------------------------
            GameObject lessonsCol = new GameObject("LessonsColumn");
            lessonsCol.transform.SetParent(cardObj.transform, false);
            RectTransform lcRt = lessonsCol.AddComponent<RectTransform>();
            lcRt.anchorMin = new Vector2(0f, 0.5f);
            lcRt.anchorMax = new Vector2(0f, 0.5f);
            lcRt.pivot = new Vector2(0f, 0.5f);
            lcRt.anchoredPosition = new Vector2(60f, -35f);
            lcRt.sizeDelta = new Vector2(295f, 480f);

            // Column Header
            GameObject colHdrObj = new GameObject("ColHeader");
            colHdrObj.transform.SetParent(lessonsCol.transform, false);
            Text chTxt = colHdrObj.AddComponent<Text>();
            chTxt.text = AegisLocalization.T("📜  DANH SÁCH BÀI HỌC", "📜  LESSONS LIST");
            chTxt.font = font;
            chTxt.fontSize = 17;
            chTxt.fontStyle = FontStyle.Bold;
            chTxt.alignment = TextAnchor.MiddleCenter;
            chTxt.color = new Color(0.96f, 0.88f, 0.65f);
            MakeTextCrisp(chTxt, new Color(0.12f, 0.06f, 0.02f, 0.95f));
            RectTransform chRt = colHdrObj.GetComponent<RectTransform>();
            chRt.anchoredPosition = new Vector2(0f, 190f);
            chRt.sizeDelta = new Vector2(290f, 32f);

            string[] lessons = {
                AegisLocalization.T("Bài 1: Giới thiệu Sicilian", "Lesson 1: Sicilian Intro"),
                AegisLocalization.T("Bài 2: Các biến thể chính", "Lesson 2: Main Variations"),
                AegisLocalization.T("Bài 3: Luyện tập đối kháng", "Lesson 3: Practice Drills")
            };

            for (int i = 0; i < 3; i++)
            {
                bool active = (i == 1);
                GameObject lObj = new GameObject("Lesson_" + i);
                lObj.transform.SetParent(lessonsCol.transform, false);

                Image bg = lObj.AddComponent<Image>();
                Sprite btnSp = active
                    ? (MedievalUIAssetLoader.GetSprite("btn_gothic_gold_blank") ?? MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold"))
                    : (MedievalUIAssetLoader.GetSprite("btn_wood_blank_1") ?? MedievalUIAssetLoader.GetSprite("btn_wood_blank_2"));
                bg.sprite = btnSp;
                bg.type = Image.Type.Simple;
                bg.preserveAspect = true;

                Button btn = lObj.AddComponent<Button>();
                string lesName = lessons[i];
                btn.onClick.AddListener(() => ShowToast(AegisLocalization.T($"Đang mở: {lesName}", $"Opening: {lesName}"), ToastType.Info));
                lObj.AddComponent<MenuButtonEffect>();

                RectTransform rt = lObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, 110f - i * 85f);
                rt.sizeDelta = new Vector2(285f, 66f);

                GameObject lTxtObj = new GameObject("Text");
                lTxtObj.transform.SetParent(lObj.transform, false);
                Text lTxt = lTxtObj.AddComponent<Text>();
                lTxt.text = lessons[i];
                lTxt.font = font;
                lTxt.fontSize = 15;
                lTxt.fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                lTxt.alignment = TextAnchor.MiddleCenter;
                lTxt.color = active ? new Color(1.0f, 0.95f, 0.80f) : new Color(0.92f, 0.86f, 0.76f);
                lTxt.resizeTextForBestFit = true;
                lTxt.resizeTextMinSize = 12;
                lTxt.resizeTextMaxSize = 15;
                MakeTextCrisp(lTxt, new Color(0.12f, 0.06f, 0.02f, 0.95f));

                RectTransform ltRt = lTxtObj.GetComponent<RectTransform>();
                ltRt.anchorMin = Vector2.zero;
                ltRt.anchorMax = Vector2.one;
                ltRt.offsetMin = new Vector2(16f, 4f);
                ltRt.offsetMax = new Vector2(-16f, -4f);
            }

            // ---------------------------------------------------------------------
            // 3. Center: 2D Sicilian Board Diagram (Enlarged to 450x450 with Royal Border)
            // ---------------------------------------------------------------------
            GameObject boardContainer = new GameObject("BoardContainer");
            boardContainer.transform.SetParent(cardObj.transform, false);
            RectTransform bcRt = boardContainer.AddComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0.5f);
            bcRt.anchorMax = new Vector2(0.5f, 0.5f);
            bcRt.pivot = new Vector2(0.5f, 0.5f);
            bcRt.anchoredPosition = new Vector2(15f, -35f);
            bcRt.sizeDelta = new Vector2(460f, 480f);

            GameObject boardObj = new GameObject("SicilianBoard");
            boardObj.transform.SetParent(boardContainer.transform, false);
            Image bImg = boardObj.AddComponent<Image>();
            bImg.sprite = ArcaneAssetLoader.GetSicilianDefenseDiagram(440);
            bImg.preserveAspect = true;
            RectTransform bRt = boardObj.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(0f, 15f);
            bRt.sizeDelta = new Vector2(440f, 440f);

            // Subtitle below board
            GameObject bSubObj = new GameObject("BoardSubtitle");
            bSubObj.transform.SetParent(boardContainer.transform, false);
            Text bSubTxt = bSubObj.AddComponent<Text>();
            bSubTxt.text = AegisLocalization.T("Biến thể Najdorf (5...a6) • Nước đi then chốt", "Najdorf Variation (5...a6) • Key Move");
            bSubTxt.font = font;
            bSubTxt.fontSize = 15;
            bSubTxt.fontStyle = FontStyle.Bold;
            bSubTxt.alignment = TextAnchor.MiddleCenter;
            bSubTxt.color = new Color(0.96f, 0.90f, 0.74f);
            MakeTextCrisp(bSubTxt, new Color(0.12f, 0.06f, 0.02f, 0.95f));
            RectTransform bsRt = bSubObj.GetComponent<RectTransform>();
            bsRt.anchoredPosition = new Vector2(0f, -225f);
            bsRt.sizeDelta = new Vector2(440f, 30f);

            // ---------------------------------------------------------------------
            // 4. Right: Najdorf Explanation Parchment Card (High-Contrast Scribe Ink)
            // ---------------------------------------------------------------------
            GameObject descCard = new GameObject("DescCard");
            descCard.transform.SetParent(cardObj.transform, false);
            Image dcBg = descCard.AddComponent<Image>();
            dcBg.sprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ParchmentCardMid) 
                        ?? ArcaneAssetLoader.GetGlassBoxSprite(440, 500, 20f, new Color(0.06f, 0.12f, 0.22f, 0.95f), ColCyanBorder, 1.5f);
            dcBg.type = Image.Type.Simple;
            dcBg.preserveAspect = false;

            RectTransform dcRt = descCard.GetComponent<RectTransform>() ?? descCard.AddComponent<RectTransform>();
            dcRt.anchorMin = new Vector2(1f, 0.5f);
            dcRt.anchorMax = new Vector2(1f, 0.5f);
            dcRt.pivot = new Vector2(1f, 0.5f);
            dcRt.anchoredPosition = new Vector2(-55f, -35f);
            dcRt.sizeDelta = new Vector2(430f, 500f);

            // Title: Deep Antique Royal Chestnut Ink (High Contrast, Bold, Elegant)
            GameObject dTitleObj = new GameObject("CardTitle");
            dTitleObj.transform.SetParent(descCard.transform, false);
            Text dt = dTitleObj.AddComponent<Text>();
            dt.text = AegisLocalization.T("Biến thể Najdorf", "Najdorf Variation");
            dt.font = font;
            dt.fontSize = 24;
            dt.fontStyle = FontStyle.Bold;
            dt.alignment = TextAnchor.MiddleCenter;
            dt.color = new Color(0.24f, 0.12f, 0.05f); // Deep dark chestnut ink
            MakeTextCrisp(dt, new Color(1.0f, 0.94f, 0.70f, 0.85f));
            RectTransform dtRt = dTitleObj.GetComponent<RectTransform>();
            dtRt.anchoredPosition = new Vector2(0f, 195f);
            dtRt.sizeDelta = new Vector2(380f, 36f);

            // Ornate Divider
            GameObject dDivObj = new GameObject("CardDivider");
            dDivObj.transform.SetParent(descCard.transform, false);
            Image dDivImg = dDivObj.AddComponent<Image>();
            Sprite divSp = MedievalUIAssetLoader.GetSprite("divider_ornate_gold")
                        ?? MedievalUIAssetLoader.GetSprite("divider_gold_filigree_360");
            if (divSp != null)
            {
                dDivImg.sprite = divSp;
                dDivImg.preserveAspect = true;
            }
            else
            {
                dDivImg.color = new Color(0.65f, 0.45f, 0.20f, 0.45f);
            }
            RectTransform ddRt = dDivObj.GetComponent<RectTransform>();
            ddRt.anchoredPosition = new Vector2(0f, 165f);
            ddRt.sizeDelta = new Vector2(340f, 14f);

            // Body: Dark Scribe Charcoal Ink (100% Readable on Parchment)
            GameObject bodyTxtObj = new GameObject("Body");
            bodyTxtObj.transform.SetParent(descCard.transform, false);
            Text bt = bodyTxtObj.AddComponent<Text>();
            bt.text = AegisLocalization.T(
                "Đây là một trong những biến thể phổ biến và mạnh mẽ nhất của Phòng thủ Sicilian.\n\nĐặc trưng bởi nước đi 5...a6, ngăn chặn ô b5 của Trắng và chuẩn bị triển khai phản công cánh Hậu đầy uy lực.",
                "One of the most popular and sharp variations of the Sicilian Defense.\n\nCharacterized by 5...a6, denying the b5 square to White and preparing powerful queenside counter-play.");
            bt.font = font;
            bt.fontSize = 16;
            bt.lineSpacing = 1.35f;
            bt.alignment = TextAnchor.UpperLeft;
            bt.color = new Color(0.18f, 0.12f, 0.07f); // Deep antique charcoal scribe ink
            bt.horizontalOverflow = HorizontalWrapMode.Wrap;
            bt.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform btRt = bodyTxtObj.GetComponent<RectTransform>();
            btRt.anchorMin = new Vector2(0.5f, 0.5f);
            btRt.anchorMax = new Vector2(0.5f, 0.5f);
            btRt.pivot = new Vector2(0.5f, 0.5f);
            btRt.anchoredPosition = new Vector2(0f, 35f);
            btRt.sizeDelta = new Vector2(360f, 210f);

            // [Xem tiếp ❯] Action Button (Gothic Gold CTA)
            GameObject nextBtnObj = new GameObject("BtnNextLesson");
            nextBtnObj.transform.SetParent(descCard.transform, false);
            Image nbBg = nextBtnObj.AddComponent<Image>();
            nbBg.sprite = MedievalUIAssetLoader.GetSprite("btn_gothic_gold_blank") 
                        ?? MedievalUIAssetLoader.GetSprite("btn_blank_dark_gold");
            nbBg.type = Image.Type.Simple;
            nbBg.preserveAspect = true;

            Button nbBtn = nextBtnObj.AddComponent<Button>();
            nbBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                ShowToast(AegisLocalization.T("Đã hoàn thành bài học 2!", "Lesson 2 completed!"), ToastType.Success);
            });
            nextBtnObj.AddComponent<MenuButtonEffect>();

            RectTransform nbRt = nextBtnObj.GetComponent<RectTransform>();
            nbRt.anchoredPosition = new Vector2(0f, -185f);
            nbRt.sizeDelta = new Vector2(350f, 56f);

            GameObject nbTxtObj = new GameObject("Text");
            nbTxtObj.transform.SetParent(nextBtnObj.transform, false);
            Text nbTxt = nbTxtObj.AddComponent<Text>();
            nbTxt.text = AegisLocalization.T("Xem tiếp  ❯", "Continue  ❯");
            nbTxt.font = font;
            nbTxt.fontSize = 17;
            nbTxt.fontStyle = FontStyle.Bold;
            nbTxt.alignment = TextAnchor.MiddleCenter;
            nbTxt.color = new Color(1.0f, 0.95f, 0.80f);
            MakeTextCrisp(nbTxt, new Color(0.12f, 0.06f, 0.02f, 0.95f));
            RectTransform nbtRt = nbTxtObj.GetComponent<RectTransform>();
            nbtRt.anchorMin = Vector2.zero;
            nbtRt.anchorMax = Vector2.one;
            nbtRt.offsetMin = new Vector2(16f, 4f);
            nbtRt.offsetMax = new Vector2(-16f, -4f);

            _learnScreenModal.SetActive(false);
        }

        public void OpenLearnScreen()
        {
            CloseAllModals();
            if (!_isInGameMatch) EnsureBaseScreenActive();
            if (_learnScreenModal != null)
            {
                _learnScreenModal.transform.SetAsLastSibling();
                _learnScreenModal.SetActive(true);
                AegisAudioManager.PlayModalOpen();
            }
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
            cardBg.sprite = MedievalUIAssetLoader.GetFrameSprite(MedievalFrameStyle.ModalCrowned)
                          ?? MedievalUIAssetLoader.GetSprite("frame_shop_collection_1520")
                          ?? ArcaneAssetLoader.GetGlassBoxSprite(760, 460, 24f, ColGlassFill, ColCyanBorder, 2f);
            cardBg.type = Image.Type.Simple;
            cardBg.preserveAspect = false;
            cardBg.color = Color.white;

            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(760f, 460f);

            // Close Button [ Crossed Swords Gold ] (Top-Right, Enlarged 64x64)
            CreateRoyalCloseButton(cardObj.transform, new Vector2(-20f, -20f), () =>
            {
                _confirmDialogModal.SetActive(false);
            }, 64f, "BtnClose");

            // Title (Rich Antique Ink safely below the Royal Crown arch)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            _confirmTitleTxt = titleObj.AddComponent<Text>();
            _confirmTitleTxt.text = AegisLocalization.T("THOÁT TRÒ CHƠI", "EXIT GAME");
            _confirmTitleTxt.font = font;
            _confirmTitleTxt.fontSize = 32;
            _confirmTitleTxt.fontStyle = FontStyle.Bold;
            _confirmTitleTxt.alignment = TextAnchor.MiddleCenter;
            _confirmTitleTxt.color = new Color(0.24f, 0.14f, 0.06f, 1f); // Deep Antique Scribe Ink
            _confirmTitleTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _confirmTitleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_confirmTitleTxt, new Color(1f, 0.92f, 0.65f, 0.8f));
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0.5f);
            tRt.anchorMax = new Vector2(0.5f, 0.5f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.anchoredPosition = new Vector2(0f, 48f);
            tRt.sizeDelta = new Vector2(580f, 42f);

            // Decorative Filigree Divider below Title
            GameObject divObj = new GameObject("TitleDivider");
            divObj.transform.SetParent(cardObj.transform, false);
            Image divImg = divObj.AddComponent<Image>();
            Sprite divSp = MedievalUIAssetLoader.GetSprite("divider_gold_filigree_360")
                        ?? MedievalUIAssetLoader.GetSprite("ornament_crest_line");
            if (divSp != null)
            {
                divImg.sprite = divSp;
                divImg.preserveAspect = true;
            }
            else
            {
                divImg.color = new Color(0.72f, 0.55f, 0.28f, 0.45f);
            }
            RectTransform divRt = divObj.GetComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.5f, 0.5f);
            divRt.anchorMax = new Vector2(0.5f, 0.5f);
            divRt.pivot = new Vector2(0.5f, 0.5f);
            divRt.anchoredPosition = new Vector2(0f, 18f);
            divRt.sizeDelta = new Vector2(380f, 16f);

            // Body Message
            GameObject bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(cardObj.transform, false);
            _confirmBodyTxt = bodyObj.AddComponent<Text>();
            _confirmBodyTxt.text = AegisLocalization.T("Bạn có muốn thoát khỏi trò chơi?", "Are you sure you want to exit the game?");
            _confirmBodyTxt.font = font;
            _confirmBodyTxt.fontSize = 24;
            _confirmBodyTxt.fontStyle = FontStyle.Bold;
            _confirmBodyTxt.alignment = TextAnchor.MiddleCenter;
            _confirmBodyTxt.color = new Color(0.28f, 0.18f, 0.10f, 1f); // Antique Walnut Ink on Parchment
            _confirmBodyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _confirmBodyTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_confirmBodyTxt);
            RectTransform bRt = bodyObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0.5f);
            bRt.anchorMax = new Vector2(0.5f, 0.5f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.anchoredPosition = new Vector2(0f, -32f);
            bRt.sizeDelta = new Vector2(620f, 68f);

            // Cancel Button [ HỦY ] (Royal Wood / Walnut Button)
            GameObject cancelBtnObj = new GameObject("BtnCancel");
            cancelBtnObj.transform.SetParent(cardObj.transform, false);
            Image cancelImg = cancelBtnObj.AddComponent<Image>();
            cancelImg.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.WoodSmall)
                            ?? MedievalUIAssetLoader.GetSprite("btn_blank_black_banner");
            cancelImg.type = Image.Type.Sliced;
            Button cancelBtn = cancelBtnObj.AddComponent<Button>();
            cancelBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                _confirmDialogModal.SetActive(false);
            });
            cancelBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform cancelRt = cancelBtnObj.GetComponent<RectTransform>();
            cancelRt.anchorMin = new Vector2(0.5f, 0.5f);
            cancelRt.anchorMax = new Vector2(0.5f, 0.5f);
            cancelRt.pivot = new Vector2(0.5f, 0.5f);
            cancelRt.anchoredPosition = new Vector2(-135f, -112f);
            cancelRt.sizeDelta = new Vector2(215f, 56f);

            GameObject cTxtObj = new GameObject("Txt");
            cTxtObj.transform.SetParent(cancelBtnObj.transform, false);
            Text cTxt = cTxtObj.AddComponent<Text>();
            cTxt.text = AegisLocalization.T("HỦY", "CANCEL");
            cTxt.font = font;
            cTxt.fontSize = 22;
            cTxt.fontStyle = FontStyle.Bold;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = new Color(0.96f, 0.90f, 0.78f, 1f); // Warm parchment text
            cTxt.raycastTarget = false;
            MakeTextCrisp(cTxt, new Color(0.10f, 0.05f, 0.02f, 0.95f));
            RectTransform cTxtRt = cTxtObj.GetComponent<RectTransform>();
            cTxtRt.anchorMin = Vector2.zero;
            cTxtRt.anchorMax = Vector2.one;
            cTxtRt.offsetMin = Vector2.zero;
            cTxtRt.offsetMax = Vector2.zero;

            // Confirm Button [ XÁC NHẬN ] (Radiant Hex Gold Champion Button)
            GameObject confirmBtnObj = new GameObject("BtnConfirm");
            confirmBtnObj.transform.SetParent(cardObj.transform, false);
            Image confirmImg = confirmBtnObj.AddComponent<Image>();
            confirmImg.sprite = MedievalUIAssetLoader.GetButtonSprite(MedievalButtonStyle.GoldChampion)
                             ?? MedievalUIAssetLoader.GetSprite("btn_hex_gold_radiant");
            confirmImg.type = Image.Type.Sliced;
            Button confirmBtn = confirmBtnObj.AddComponent<Button>();
            confirmBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                _confirmDialogModal.SetActive(false);
                _onConfirmAction?.Invoke();
            });
            confirmBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform confirmRt = confirmBtnObj.GetComponent<RectTransform>();
            confirmRt.anchorMin = new Vector2(0.5f, 0.5f);
            confirmRt.anchorMax = new Vector2(0.5f, 0.5f);
            confirmRt.pivot = new Vector2(0.5f, 0.5f);
            confirmRt.anchoredPosition = new Vector2(135f, -112f);
            confirmRt.sizeDelta = new Vector2(215f, 56f);

            GameObject cfTxtObj = new GameObject("Txt");
            cfTxtObj.transform.SetParent(confirmBtnObj.transform, false);
            Text cfTxt = cfTxtObj.AddComponent<Text>();
            cfTxt.text = AegisLocalization.T("XÁC NHẬN", "CONFIRM");
            cfTxt.font = font;
            cfTxt.fontSize = 22;
            cfTxt.fontStyle = FontStyle.Bold;
            cfTxt.alignment = TextAnchor.MiddleCenter;
            cfTxt.color = new Color(0.20f, 0.09f, 0.03f, 1f); // Deep imperial brown on bright radiant gold
            cfTxt.raycastTarget = false;
            MakeTextCrisp(cfTxt, new Color(1f, 0.92f, 0.65f, 0.85f));
            RectTransform cfTxtRt = cfTxtObj.GetComponent<RectTransform>();
            cfTxtRt.anchorMin = Vector2.zero;
            cfTxtRt.anchorMax = Vector2.one;
            cfTxtRt.offsetMin = Vector2.zero;
            cfTxtRt.offsetMax = Vector2.zero;

            _confirmDialogModal.SetActive(false);
        }

        public void ShowConfirmDialog(string title, string body, Action onConfirm)
        {
            if (_confirmTitleTxt != null) _confirmTitleTxt.text = title;
            if (_confirmBodyTxt != null) _confirmBodyTxt.text = body;
            _onConfirmAction = onConfirm;
            if (_confirmDialogModal != null)
            {
                _confirmDialogModal.SetActive(true);
                AegisAudioManager.PlayModalOpen();
            }
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
            AegisAudioManager.PlayToast();
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
        // SCREEN 16: PHÂN TÍCH & ĐÁNH GIÁ VÁN ĐẤU (Game Review & Analysis)
        // =========================================================================
        private void BuildScreen16GameReview(Transform parent, Font font)
        {
            _gameReviewModal = CreateModalOverlay(parent, "Screen16_GameReviewModal");

            // Main Review Window Card (1320 x 740)
            GameObject cardObj = new GameObject("ReviewCard");
            cardObj.transform.SetParent(_gameReviewModal.transform, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1320f, 740f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1320, 740, 24f, new Color(0.02f, 0.05f, 0.12f, 0.96f), ColCyanBorder, 2f);
            cardBg.type = Image.Type.Sliced;

            // Modal Header
            CreateModalHeader(cardObj.transform, AegisLocalization.T("PHÂN TÍCH & ĐÁNH GIÁ VÁN ĐẤU", "GAME REVIEW & ANALYSIS"), () => CloseReviewModal(), font);

            // ==================== LEFT COLUMN: 2D Interactive Board (x: -380) ====================
            GameObject boardCard = new GameObject("ReviewBoardCard");
            boardCard.transform.SetParent(cardObj.transform, false);
            Image bcBg = boardCard.AddComponent<Image>();
            bcBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(500, 600, 18f, new Color(0.04f, 0.08f, 0.16f, 0.95f), ColCyanBorder, 1.5f);
            bcBg.type = Image.Type.Sliced;
            RectTransform bcRt = boardCard.GetComponent<RectTransform>();
            bcRt.anchoredPosition = new Vector2(-380f, -25f);
            bcRt.sizeDelta = new Vector2(500f, 600f);

            // 2D Board Image
            GameObject boardImgObj = new GameObject("BoardImage");
            boardImgObj.transform.SetParent(boardCard.transform, false);
            _reviewBoardImg = boardImgObj.AddComponent<Image>();
            _reviewBoardImg.sprite = ArcaneAssetLoader.GetMiniChessboardDiagram(420);
            RectTransform bImgRt = boardImgObj.GetComponent<RectTransform>();
            bImgRt.anchoredPosition = new Vector2(0f, 50f);
            bImgRt.sizeDelta = new Vector2(430f, 430f);

            // Navigation Stepper Bar
            GameObject navBarObj = new GameObject("NavBar");
            navBarObj.transform.SetParent(boardCard.transform, false);
            RectTransform nbRt = navBarObj.AddComponent<RectTransform>();
            nbRt.anchoredPosition = new Vector2(0f, -210f);
            nbRt.sizeDelta = new Vector2(450f, 50f);

            // Buttons: |< (First), < (Prev), AutoPlay, > (Next), >| (Last)
            CreateMenuActionButton(navBarObj.transform, "BtnFirst", "|◀", new Vector2(-170f, 0f), new Vector2(54f, 44f), ColGlassFill, ColCyanGlow, 18, () => JumpToReviewPly(0), font, ColCyanBorder, 1.2f);
            CreateMenuActionButton(navBarObj.transform, "BtnPrev", "◀", new Vector2(-105f, 0f), new Vector2(54f, 44f), ColGlassFill, ColCyanGlow, 18, () => StepReviewMove(-1), font, ColCyanBorder, 1.2f);

            // Auto Play Button
            GameObject autoBtnObj = new GameObject("BtnAutoPlay");
            autoBtnObj.transform.SetParent(navBarObj.transform, false);
            Image abBg = autoBtnObj.AddComponent<Image>();
            abBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(130, 44, 22f, ColGoldCta, ColGoldBorder, 1.4f);
            abBg.type = Image.Type.Sliced;
            Button abBtn = autoBtnObj.AddComponent<Button>();
            abBtn.onClick.AddListener(ToggleAutoPlay);
            autoBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform abRt = autoBtnObj.GetComponent<RectTransform>();
            abRt.anchoredPosition = new Vector2(0f, 0f);
            abRt.sizeDelta = new Vector2(130f, 44f);
            _btnAutoPlayTxt = CreateTextComp(autoBtnObj.transform, AegisLocalization.T("▶ Tự chạy", "▶ Auto Play"), 15, FontStyle.Bold, ColDarkBtnText, font, Vector2.zero, new Vector2(120f, 30f));

            CreateMenuActionButton(navBarObj.transform, "BtnNext", "▶", new Vector2(105f, 0f), new Vector2(54f, 44f), ColGlassFill, ColCyanGlow, 18, () => StepReviewMove(1), font, ColCyanBorder, 1.2f);
            CreateMenuActionButton(navBarObj.transform, "BtnLast", "▶|", new Vector2(170f, 0f), new Vector2(54f, 44f), ColGlassFill, ColCyanGlow, 18, () =>
            {
                if (_activeAnalysis != null && _activeAnalysis.moves != null)
                {
                    JumpToReviewPly(_activeAnalysis.moves.Length);
                }
            }, font, ColCyanBorder, 1.2f);

            // Move Navigation Indicator Text (e.g. "Nước 12 / 34 (12. e4)")
            GameObject navTxtObj = new GameObject("NavMoveTxt");
            navTxtObj.transform.SetParent(boardCard.transform, false);
            _reviewMoveNavTxt = navTxtObj.AddComponent<Text>();
            _reviewMoveNavTxt.text = AegisLocalization.T("Bắt đầu ván cờ", "Start Position");
            _reviewMoveNavTxt.font = font;
            _reviewMoveNavTxt.fontSize = 16;
            _reviewMoveNavTxt.fontStyle = FontStyle.Bold;
            _reviewMoveNavTxt.alignment = TextAnchor.MiddleCenter;
            _reviewMoveNavTxt.color = ColTextWhite;
            _reviewMoveNavTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _reviewMoveNavTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform ntRt = navTxtObj.GetComponent<RectTransform>();
            ntRt.anchoredPosition = new Vector2(0f, -258f);
            ntRt.sizeDelta = new Vector2(450f, 28f);

            // ==================== RIGHT COLUMN: Analysis & PGN Details (x: +240) ====================
            GameObject detailsCard = new GameObject("ReviewDetailsCard");
            detailsCard.transform.SetParent(cardObj.transform, false);
            Image dcBg = detailsCard.AddComponent<Image>();
            dcBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(720, 600, 18f, new Color(0.04f, 0.08f, 0.16f, 0.95f), ColCyanBorder, 1.5f);
            dcBg.type = Image.Type.Sliced;
            RectTransform dcRt = detailsCard.GetComponent<RectTransform>();
            dcRt.anchoredPosition = new Vector2(240f, -25f);
            dcRt.sizeDelta = new Vector2(720f, 600f);

            // Top Info Header: Match Players & Result
            GameObject titleObj = new GameObject("ReviewMatchTitle", typeof(RectTransform));
            titleObj.transform.SetParent(detailsCard.transform, false);
            _reviewTitleTxt = titleObj.AddComponent<Text>();
            _reviewTitleTxt.text = "Trắng: Player  VS  Đen: Đối thủ";
            _reviewTitleTxt.font = font;
            _reviewTitleTxt.fontSize = 20;
            _reviewTitleTxt.fontStyle = FontStyle.Bold;
            _reviewTitleTxt.alignment = TextAnchor.MiddleLeft;
            _reviewTitleTxt.color = ColTextWhite;
            _reviewTitleTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _reviewTitleTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_reviewTitleTxt);
            RectTransform ttRt = titleObj.GetComponent<RectTransform>();
            ttRt.anchoredPosition = new Vector2(0f, 260f);
            ttRt.sizeDelta = new Vector2(670f, 30f);

            // Accuracy CAPS Bar Header & Text
            GameObject accObj = new GameObject("ReviewAccuracy", typeof(RectTransform));
            accObj.transform.SetParent(detailsCard.transform, false);
            _reviewAccuracyTxt = accObj.AddComponent<Text>();
            _reviewAccuracyTxt.text = AegisLocalization.T("🎯 Độ chính xác: Đang phân tích...", "🎯 CAPS Accuracy: Analyzing...");
            _reviewAccuracyTxt.font = font;
            _reviewAccuracyTxt.fontSize = 17;
            _reviewAccuracyTxt.fontStyle = FontStyle.Bold;
            _reviewAccuracyTxt.alignment = TextAnchor.MiddleLeft;
            _reviewAccuracyTxt.color = ColGoldBorder;
            _reviewAccuracyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _reviewAccuracyTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_reviewAccuracyTxt);
            RectTransform accRt = accObj.GetComponent<RectTransform>();
            accRt.anchoredPosition = new Vector2(0f, 226f);
            accRt.sizeDelta = new Vector2(670f, 26f);

            // Visual Accuracy Progress Bar (White vs Black)
            GameObject accBarBg = new GameObject("ReviewAccBarBg", typeof(RectTransform));
            accBarBg.transform.SetParent(detailsCard.transform, false);
            Image accBgImg = accBarBg.AddComponent<Image>();
            accBgImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(670, 8, 4f, new Color(0.08f, 0.12f, 0.20f, 0.9f), ColCyanBorder, 1f);
            accBgImg.type = Image.Type.Sliced;
            RectTransform accBgRt = accBarBg.GetComponent<RectTransform>();
            accBgRt.anchoredPosition = new Vector2(0f, 210f);
            accBgRt.sizeDelta = new Vector2(670f, 8f);

            GameObject whiteFill = new GameObject("WhiteFill", typeof(RectTransform));
            whiteFill.transform.SetParent(accBarBg.transform, false);
            Image wfImg = whiteFill.AddComponent<Image>();
            wfImg.color = new Color(0.92f, 0.96f, 1f, 0.95f);
            _reviewAccWhiteFill = whiteFill.GetComponent<RectTransform>();
            _reviewAccWhiteFill.anchorMin = new Vector2(0f, 0f);
            _reviewAccWhiteFill.anchorMax = new Vector2(0.5f, 1f);
            _reviewAccWhiteFill.offsetMin = Vector2.zero;
            _reviewAccWhiteFill.offsetMax = Vector2.zero;

            GameObject blackFill = new GameObject("BlackFill", typeof(RectTransform));
            blackFill.transform.SetParent(accBarBg.transform, false);
            Image bfImg = blackFill.AddComponent<Image>();
            bfImg.color = new Color(0.2f, 0.75f, 1f, 0.85f);
            _reviewAccBlackFill = blackFill.GetComponent<RectTransform>();
            _reviewAccBlackFill.anchorMin = new Vector2(0.5f, 0f);
            _reviewAccBlackFill.anchorMax = new Vector2(1f, 1f);
            _reviewAccBlackFill.offsetMin = Vector2.zero;
            _reviewAccBlackFill.offsetMax = Vector2.zero;

            // Move Quality Badges Summary (e.g. "⭐ Tốt nhất: 14  👍 Tốt: 6  ⁉️ Không chuẩn: 2  ❓ Sai lầm: 1  ⁇ Đại sai lầm: 0")
            GameObject pillsObj = new GameObject("ReviewPills", typeof(RectTransform));
            pillsObj.transform.SetParent(detailsCard.transform, false);
            _reviewPillsTxt = pillsObj.AddComponent<Text>();
            _reviewPillsTxt.text = "⭐ Tốt: 0  |  ❓ Sai sót: 0";
            _reviewPillsTxt.font = font;
            _reviewPillsTxt.fontSize = 15;
            _reviewPillsTxt.fontStyle = FontStyle.Bold;
            _reviewPillsTxt.alignment = TextAnchor.MiddleLeft;
            _reviewPillsTxt.color = new Color(0.85f, 0.95f, 1.0f);
            _reviewPillsTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _reviewPillsTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_reviewPillsTxt);
            RectTransform pillsRt = pillsObj.GetComponent<RectTransform>();
            pillsRt.anchoredPosition = new Vector2(0f, 186f);
            pillsRt.sizeDelta = new Vector2(670f, 26f);

            // Middle Box: Current Move Evaluation Card (Eval score, Best Move, Evaluation comment)
            GameObject evalBoxObj = new GameObject("EvalBox", typeof(RectTransform));
            evalBoxObj.transform.SetParent(detailsCard.transform, false);
            Image ebBg = evalBoxObj.AddComponent<Image>();
            ebBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(670, 136, 14f, new Color(0.02f, 0.06f, 0.14f, 0.95f), ColCyanBorder, 1.4f);
            ebBg.type = Image.Type.Sliced;
            RectTransform ebRt = evalBoxObj.GetComponent<RectTransform>();
            ebRt.anchoredPosition = new Vector2(0f, 96f);
            ebRt.sizeDelta = new Vector2(670f, 136f);

            // Badge + Eval Score
            GameObject badgeObj = new GameObject("EvalBadge", typeof(RectTransform));
            badgeObj.transform.SetParent(evalBoxObj.transform, false);
            _reviewEvalBadgeTxt = badgeObj.AddComponent<Text>();
            _reviewEvalBadgeTxt.text = "⭐ Nước đi tốt nhất (Best Move)";
            _reviewEvalBadgeTxt.font = font;
            _reviewEvalBadgeTxt.fontSize = 18;
            _reviewEvalBadgeTxt.fontStyle = FontStyle.Bold;
            _reviewEvalBadgeTxt.alignment = TextAnchor.MiddleLeft;
            _reviewEvalBadgeTxt.color = ColGoldBorder;
            _reviewEvalBadgeTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _reviewEvalBadgeTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_reviewEvalBadgeTxt);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            badgeRt.anchoredPosition = new Vector2(-105f, 40f);
            badgeRt.sizeDelta = new Vector2(420f, 28f);

            GameObject scoreObj = new GameObject("EvalScore", typeof(RectTransform));
            scoreObj.transform.SetParent(evalBoxObj.transform, false);
            _reviewEvalScoreTxt = scoreObj.AddComponent<Text>();
            _reviewEvalScoreTxt.text = "+0.20";
            _reviewEvalScoreTxt.font = font;
            _reviewEvalScoreTxt.fontSize = 18;
            _reviewEvalScoreTxt.fontStyle = FontStyle.Bold;
            _reviewEvalScoreTxt.alignment = TextAnchor.MiddleRight;
            _reviewEvalScoreTxt.color = ColCyanGlow;
            _reviewEvalScoreTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
            _reviewEvalScoreTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_reviewEvalScoreTxt);
            RectTransform scoreRt = scoreObj.GetComponent<RectTransform>();
            scoreRt.anchoredPosition = new Vector2(215f, 40f);
            scoreRt.sizeDelta = new Vector2(200f, 28f);

            // Best Move Suggestion Text
            GameObject bestMoveObj = new GameObject("BestMoveTxt", typeof(RectTransform));
            bestMoveObj.transform.SetParent(evalBoxObj.transform, false);
            _reviewBestMoveTxt = bestMoveObj.AddComponent<Text>();
            _reviewBestMoveTxt.text = "💡 Động cơ đề xuất: e2e4";
            _reviewBestMoveTxt.font = font;
            _reviewBestMoveTxt.fontSize = 16;
            _reviewBestMoveTxt.fontStyle = FontStyle.Bold;
            _reviewBestMoveTxt.alignment = TextAnchor.MiddleLeft;
            _reviewBestMoveTxt.color = new Color(0.2f, 0.98f, 0.65f);
            _reviewBestMoveTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _reviewBestMoveTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_reviewBestMoveTxt);
            RectTransform bmRt = bestMoveObj.GetComponent<RectTransform>();
            bmRt.anchoredPosition = new Vector2(0f, 8f);
            bmRt.sizeDelta = new Vector2(630f, 26f);

            // Comment text
            GameObject commentObj = new GameObject("CommentTxt", typeof(RectTransform));
            commentObj.transform.SetParent(evalBoxObj.transform, false);
            _reviewCommentTxt = commentObj.AddComponent<Text>();
            _reviewCommentTxt.text = "Nước đi mở đầu tiêu chuẩn kiểm soát trung tâm.";
            _reviewCommentTxt.font = font;
            _reviewCommentTxt.fontSize = 15;
            _reviewCommentTxt.fontStyle = FontStyle.Normal;
            _reviewCommentTxt.alignment = TextAnchor.UpperLeft;
            _reviewCommentTxt.color = new Color(0.92f, 0.96f, 1.0f);
            _reviewCommentTxt.lineSpacing = 1.25f;
            _reviewCommentTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _reviewCommentTxt.verticalOverflow = VerticalWrapMode.Overflow;
            MakeTextCrisp(_reviewCommentTxt);
            RectTransform cmRt = commentObj.GetComponent<RectTransform>();
            cmRt.anchoredPosition = new Vector2(0f, -34f);
            cmRt.sizeDelta = new Vector2(630f, 48f);

            // Move List Section Header
            GameObject mlHeaderObj = new GameObject("MoveListHeader", typeof(RectTransform));
            mlHeaderObj.transform.SetParent(detailsCard.transform, false);
            RectTransform mlhRt = mlHeaderObj.GetComponent<RectTransform>();
            mlhRt.anchoredPosition = new Vector2(0f, 12f);
            mlhRt.sizeDelta = new Vector2(670f, 26f);

            Text mlTitle = CreateTextComp(mlHeaderObj.transform, AegisLocalization.T("📋 LỊCH SỬ NƯỚC ĐI (MOVE LIST)", "📋 MOVE LIST"), 15, FontStyle.Bold, ColGoldBorder, font, new Vector2(-195f, 0f), new Vector2(250f, 24f), TextAnchor.MiddleLeft);
            MakeTextCrisp(mlTitle);
            Text mlWhite = CreateTextComp(mlHeaderObj.transform, AegisLocalization.T("⚪ TRẮNG (WHITE)", "⚪ WHITE"), 14, FontStyle.Bold, ColTextWhite, font, new Vector2(-40f, 0f), new Vector2(140f, 24f), TextAnchor.MiddleCenter);
            MakeTextCrisp(mlWhite);
            Text mlBlack = CreateTextComp(mlHeaderObj.transform, AegisLocalization.T("⚫ ĐEN (BLACK)", "⚫ BLACK"), 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(175f, 0f), new Vector2(140f, 24f), TextAnchor.MiddleCenter);
            MakeTextCrisp(mlBlack);

            // Bottom Move List Scroll Area (PGN moves)
            GameObject scrollObj = new GameObject("MoveListScrollView", typeof(RectTransform));
            scrollObj.transform.SetParent(detailsCard.transform, false);
            RectTransform sRt = scrollObj.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, -145f);
            sRt.sizeDelta = new Vector2(670f, 265f);

            Image sBg = scrollObj.AddComponent<Image>();
            sBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(670, 265, 12f, new Color(0.015f, 0.035f, 0.09f, 0.92f), ColCyanBorder, 1.2f);
            sBg.type = Image.Type.Sliced;
            scrollObj.AddComponent<RectMask2D>();

            GameObject contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(scrollObj.transform, false);
            _reviewMoveListParent = contentObj.transform;
            RectTransform ctRt = contentObj.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 1f);
            ctRt.anchorMax = new Vector2(1f, 1f);
            ctRt.pivot = new Vector2(0.5f, 1f);
            ctRt.anchoredPosition = Vector2.zero;
            ctRt.sizeDelta = new Vector2(0f, 300f);

            ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
            sr.content = ctRt;
            sr.viewport = sRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 25f;

            _gameReviewModal.SetActive(false);
        }

        public void OpenGameReview(MatchHistoryItem match)
        {
            if (_gameReviewModal == null) return;
            _gameReviewModal.SetActive(true);
            AegisAudioManager.PlayModalOpen();

            if (_autoPlayCoroutine != null)
            {
                StopCoroutine(_autoPlayCoroutine);
                _autoPlayCoroutine = null;
                _isAutoPlaying = false;
                if (_btnAutoPlayTxt != null) _btnAutoPlayTxt.text = AegisLocalization.T("▶ Tự chạy", "▶ Auto Play");
            }

            string myName = (UserAccountManager.Instance != null && UserAccountManager.Instance.IsLoggedIn)
                ? UserAccountManager.Instance.CurrentUsername
                : "Player";
            string oppName = !string.IsNullOrEmpty(match?.opponent_name) ? match.opponent_name : "Opponent";
            string resScore = (match != null && match.result == "win") ? "1 - 0" : ((match != null && match.result == "loss") ? "0 - 1" : "½ - ½");

            if (_reviewTitleTxt != null)
            {
                _reviewTitleTxt.text = $"<b>{myName}</b> ({resScore}) <b>{oppName}</b>";
            }
            if (_reviewAccuracyTxt != null)
            {
                _reviewAccuracyTxt.text = AegisLocalization.T("🎯 Đang phân tích động cơ AI (Stockfish Engine)...", "🎯 Running AI Engine Analysis...");
            }

            _currentReviewPly = 0;
            UpdateReviewBoardAndDetails(0);

            // Check if analysis_json already exists on match
            if (match != null && !string.IsNullOrEmpty(match.analysis_json))
            {
                try
                {
                    var data = JsonUtility.FromJson<GameAnalysisData>(match.analysis_json);
                    if (data != null && data.moves != null && data.moves.Length > 0)
                    {
                        ApplyAnalysisData(data);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameReview] Error parsing cached analysis: {ex.Message}");
                }
            }

            // Otherwise, request analysis from backend or local fallback
            string[] uciMoves = match?.moves;
            string[] sanMoves = match?.san_moves;
            string pgn = match?.pgn;

            // If empty, generate a demo/practice opening sequence so player can test
            if (uciMoves == null || uciMoves.Length == 0)
            {
                uciMoves = new[] { "e2e4", "c7c5", "g1f3", "d7d6", "d2d4", "c5d4", "f3d4", "g8f6", "b1c3", "a7a6" };
                sanMoves = new[] { "e4", "c5", "Nf3", "d6", "d4", "cxd4", "Nxd4", "Nf6", "Nc3", "a6" };
            }

            if (UserAccountManager.Instance != null)
            {
                UserAccountManager.Instance.RequestMatchAnalysis(uciMoves, sanMoves, pgn, (ok, data) =>
                {
                    if (ok && data != null)
                    {
                        ApplyAnalysisData(data);
                    }
                    else
                    {
                        ShowToast(AegisLocalization.T("Không thể kết nối máy chủ phân tích!", "Could not reach analysis engine!"), ToastType.Warning);
                    }
                });
            }
        }

        private void ApplyAnalysisData(GameAnalysisData data)
        {
            _activeAnalysis = data;
            if (_activeAnalysis == null || _activeAnalysis.moves == null) return;

            if (_reviewAccuracyTxt != null)
            {
                _reviewAccuracyTxt.text = string.Format(
                    AegisLocalization.T("🎯 Độ chính xác: Trắng {0:F1}%  |  Đen {1:F1}%", "🎯 Accuracy: White {0:F1}%  |  Black {1:F1}%"),
                    data.white_accuracy, data.black_accuracy);
            }

            if (_reviewAccWhiteFill != null && _reviewAccBlackFill != null)
            {
                float totalAcc = Mathf.Max(1f, data.white_accuracy + data.black_accuracy);
                float whiteRatio = Mathf.Clamp(data.white_accuracy / totalAcc, 0.10f, 0.90f);
                _reviewAccWhiteFill.anchorMax = new Vector2(whiteRatio, 1f);
                _reviewAccBlackFill.anchorMin = new Vector2(whiteRatio, 0f);
            }

            // Count badges (case-insensitive)
            int bestCount = 0, goodCount = 0, inaccCount = 0, mistCount = 0, blundCount = 0, brillCount = 0;
            foreach (var m in data.moves)
            {
                string j = (m.judgment ?? "").ToLowerInvariant();
                if (j == "brilliant") brillCount++;
                else if (j == "best") bestCount++;
                else if (j == "good" || j == "book") goodCount++;
                else if (j == "inaccuracy") inaccCount++;
                else if (j == "mistake") mistCount++;
                else if (j == "blunder" || j == "missed_win") blundCount++;
            }

            if (_reviewPillsTxt != null)
            {
                _reviewPillsTxt.text = $"‼️ {brillCount}   ⭐ {bestCount}   👍 {goodCount}   ⁉️ {inaccCount}   ❓ {mistCount}   ⁇ {blundCount}";
            }

            PopulateReviewMoveList();
            JumpToReviewPly(0);
        }

        private void CloseReviewModal()
        {
            if (_autoPlayCoroutine != null)
            {
                StopCoroutine(_autoPlayCoroutine);
                _autoPlayCoroutine = null;
                _isAutoPlaying = false;
            }
            if (_gameReviewModal != null) _gameReviewModal.SetActive(false);
            AegisAudioManager.PlayModalClose();
        }

        private void StepReviewMove(int step)
        {
            if (_activeAnalysis == null || _activeAnalysis.moves == null) return;
            int maxPly = _activeAnalysis.moves.Length;
            int nextPly = Mathf.Clamp(_currentReviewPly + step, 0, maxPly);
            if (nextPly != _currentReviewPly)
            {
                JumpToReviewPly(nextPly);
            }
        }

        private void JumpToReviewPly(int targetPly)
        {
            if (_activeAnalysis == null || _activeAnalysis.moves == null) return;
            int maxPly = _activeAnalysis.moves.Length;
            _currentReviewPly = Mathf.Clamp(targetPly, 0, maxPly);
            UpdateReviewBoardAndDetails(_currentReviewPly);
        }

        private void ToggleAutoPlay()
        {
            if (_isAutoPlaying)
            {
                if (_autoPlayCoroutine != null) StopCoroutine(_autoPlayCoroutine);
                _autoPlayCoroutine = null;
                _isAutoPlaying = false;
                if (_btnAutoPlayTxt != null) _btnAutoPlayTxt.text = AegisLocalization.T("▶ Tự chạy", "▶ Auto Play");
            }
            else
            {
                _isAutoPlaying = true;
                if (_btnAutoPlayTxt != null) _btnAutoPlayTxt.text = AegisLocalization.T("⏸ Dừng", "⏸ Pause");
                _autoPlayCoroutine = StartCoroutine(AutoPlayRoutine());
            }
        }

        private IEnumerator AutoPlayRoutine()
        {
            if (_activeAnalysis == null || _activeAnalysis.moves == null) yield break;
            while (_isAutoPlaying)
            {
                yield return new WaitForSecondsRealtime(1.8f);
                if (_currentReviewPly >= _activeAnalysis.moves.Length)
                {
                    _currentReviewPly = 0;
                }
                else
                {
                    _currentReviewPly++;
                }
                UpdateReviewBoardAndDetails(_currentReviewPly);
            }
        }

        private void UpdateReviewBoardAndDetails(int plyIndex)
        {
            if (plyIndex == 0)
            {
                // Starting position
                string startFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
                var startMarkers = ParseFenToMarkers(startFen);
                if (startMarkers == null || startMarkers.Count == 0)
                {
                    startMarkers = ArcaneAssetLoader.GetStartingPieces();
                }
                if (_reviewBoardImg != null)
                {
                    _reviewBoardImg.sprite = ArcaneAssetLoader.GetCustomBoardDiagram(430, false, new Vector2Int(-1, -1), null, null, null, startMarkers);
                }

                if (_reviewMoveNavTxt != null)
                {
                    _reviewMoveNavTxt.text = AegisLocalization.T("Vị trí khởi đầu (Start)", "Start Position");
                }
                if (_reviewEvalBadgeTxt != null)
                {
                    _reviewEvalBadgeTxt.text = "♟ " + AegisLocalization.T("Khởi đầu ván cờ", "Game Start");
                    _reviewEvalBadgeTxt.color = ColTextWhite;
                }
                if (_reviewEvalScoreTxt != null)
                {
                    _reviewEvalScoreTxt.text = "0.00";
                }
                if (_reviewBestMoveTxt != null)
                {
                    _reviewBestMoveTxt.text = AegisLocalization.T("Nước mở cờ phổ biến: e4, d4, Nf3, c4", "Standard opening moves: e4, d4, Nf3, c4");
                }
                if (_reviewCommentTxt != null)
                {
                    _reviewCommentTxt.text = AegisLocalization.T("Ván đấu bắt đầu. Cả hai bên triển khai quân và tranh giành trung tâm.", "Game begins. Both sides develop pieces and contest center.");
                }
                HighlightActiveMoveButton(-1);
                return;
            }

            if (_activeAnalysis == null || _activeAnalysis.moves == null) return;
            int total = _activeAnalysis.moves.Length;

            int mIdx = plyIndex - 1;
            if (mIdx < 0 || mIdx >= total) return;
            var item = _activeAnalysis.moves[mIdx];

            // Board with move highlighted
            var markers = ParseFenToMarkers(item.fen_after);
            if (markers == null || markers.Count == 0)
            {
                List<string> ucis = new List<string>();
                for (int i = 0; i <= mIdx; i++)
                {
                    if (!string.IsNullOrEmpty(_activeAnalysis.moves[i].uci))
                        ucis.Add(_activeAnalysis.moves[i].uci);
                }
                markers = ChessBoardSimulator.SimulateBoardUpToPly(ucis, ucis.Count);
            }
            if (markers == null || markers.Count == 0)
            {
                markers = ArcaneAssetLoader.GetStartingPieces();
            }

            Vector2Int fromCoord = SquareToCoord(item.from_square);
            Vector2Int toCoord = SquareToCoord(item.to_square);
            bool hasFrom = (fromCoord.x >= 0 && fromCoord.y >= 0);
            var moveList = (toCoord.x >= 0 && toCoord.y >= 0) ? new List<Vector2Int> { toCoord } : null;

            if (_reviewBoardImg != null)
            {
                _reviewBoardImg.sprite = ArcaneAssetLoader.GetCustomBoardDiagram(430, hasFrom, fromCoord, moveList, null, null, markers);
            }

            // Move Counter
            string movePrefix = (item.color == "white") ? $"{item.move_number}." : $"{item.move_number}...";
            if (_reviewMoveNavTxt != null)
            {
                string cleanMoveSan = UserAccountManager.FormatCleanMoveNotation(item.san, item.uci);
                _reviewMoveNavTxt.text = AegisLocalization.T($"Nước {plyIndex} / {total}  ({movePrefix} {cleanMoveSan})", $"Move {plyIndex} / {total}  ({movePrefix} {cleanMoveSan})");
            }

            // Evaluation Badge & Color (case-insensitive)
            string j = (item.judgment ?? "").ToLowerInvariant();
            Color badgeCol = ColGoldBorder;
            string symbol = GetMoveBadgeSymbol(j);
            string judgmentText = item.judgment_vi ?? item.judgment ?? "Tốt";

            if (j == "brilliant") { badgeCol = new Color(0.1f, 0.95f, 0.95f); symbol = "‼️"; }
            else if (j == "best") { badgeCol = new Color(0.1f, 0.95f, 0.45f); symbol = "⭐"; }
            else if (j == "great") { badgeCol = new Color(0.2f, 0.85f, 0.95f); symbol = "!"; }
            else if (j == "good" || j == "book") { badgeCol = new Color(0.55f, 0.85f, 1.0f); symbol = (j == "book") ? "📖" : "👍"; }
            else if (j == "inaccuracy") { badgeCol = new Color(1.0f, 0.85f, 0.2f); symbol = "⁉️"; }
            else if (j == "mistake") { badgeCol = new Color(1.0f, 0.55f, 0.15f); symbol = "❓"; }
            else if (j == "blunder") { badgeCol = new Color(1.0f, 0.25f, 0.25f); symbol = "⁇"; }
            else if (j == "missed_win") { badgeCol = new Color(0.95f, 0.15f, 0.35f); symbol = "❌"; }

            if (_reviewEvalBadgeTxt != null)
            {
                _reviewEvalBadgeTxt.text = $"{symbol}  {judgmentText}";
                _reviewEvalBadgeTxt.color = badgeCol;
            }

            // Score formatting
            if (_reviewEvalScoreTxt != null)
            {
                string sign = item.score > 0 ? "+" : "";
                _reviewEvalScoreTxt.text = $"{sign}{item.score:F2}";
                _reviewEvalScoreTxt.color = (item.score >= 0f) ? ColCyanGlow : new Color(1f, 0.4f, 0.5f);
            }

            // Best move suggestion
            if (_reviewBestMoveTxt != null)
            {
                string rawBest = !string.IsNullOrEmpty(item.best_move_san) ? item.best_move_san : item.best_move_uci;
                string bestSan = UserAccountManager.FormatCleanMoveNotation(rawBest, item.best_move_uci);
                if (!string.IsNullOrEmpty(bestSan) && j != "best" && j != "brilliant")
                {
                    _reviewBestMoveTxt.text = AegisLocalization.T($"💡 Động cơ gợi ý: <b>{bestSan}</b>", $"💡 Engine recommendation: <b>{bestSan}</b>");
                }
                else
                {
                    _reviewBestMoveTxt.text = AegisLocalization.T("✓ Nước đi tối ưu nhất theo đánh giá động cơ AI.", "✓ Optimal move according to AI engine.");
                }
            }

            // Comment explanation
            if (_reviewCommentTxt != null)
            {
                string comment = item.comment_vi;
                if (string.IsNullOrEmpty(comment))
                {
                    comment = (j == "blunder" || j == "missed_win") ? "Sai lầm nghiêm trọng làm thay đổi cục diện trận đấu." :
                              (j == "mistake") ? "Nước đi chưa tối ưu, đối thủ có thể khai thác thế cờ." :
                              (j == "inaccuracy") ? "Nước đi có phần thiếu chính xác, làm giảm ưu thế." :
                              (j == "brilliant") ? "Nước cờ thiên tài! Tạo đòn chiến thuật bất ngờ." :
                              "Duy trì thế cờ vững chắc và cân bằng.";
                }
                _reviewCommentTxt.text = comment;
            }

            HighlightActiveMoveButton(mIdx);
        }

        private static string GetMoveBadgeSymbol(string judgment)
        {
            string j = (judgment ?? "").ToLowerInvariant();
            if (j == "brilliant") return "‼️";
            if (j == "best") return "⭐";
            if (j == "book") return "📖";
            if (j == "good") return "👍";
            if (j == "inaccuracy") return "⁉️";
            if (j == "mistake") return "❓";
            if (j == "blunder") return "⁇";
            if (j == "missed_win") return "❌";
            return "👍";
        }

        private void PopulateReviewMoveList()
        {
            if (_reviewMoveListParent == null || _activeAnalysis == null || _activeAnalysis.moves == null) return;

            // Clear old buttons
            for (int i = _reviewMoveListParent.childCount - 1; i >= 0; i--)
            {
                Destroy(_reviewMoveListParent.GetChild(i).gameObject);
            }

            int count = _activeAnalysis.moves.Length;
            int numTurns = Mathf.CeilToInt(count / 2f);
            float rowHeight = 40f;
            float totalHeight = Mathf.Max(265f, numTurns * rowHeight + 15f);

            RectTransform parentRt = _reviewMoveListParent.GetComponent<RectTransform>();
            if (parentRt != null) parentRt.sizeDelta = new Vector2(0f, totalHeight);

            Font font = GetDefaultFont();

            for (int t = 0; t < numTurns; t++)
            {
                int turnNum = t + 1;
                int whiteIdx = t * 2;
                int blackIdx = t * 2 + 1;

                GameObject rowObj = new GameObject("TurnRow_" + turnNum, typeof(RectTransform));
                rowObj.transform.SetParent(_reviewMoveListParent, false);
                RectTransform rRt = rowObj.GetComponent<RectTransform>();
                rRt.anchorMin = new Vector2(0f, 1f);
                rRt.anchorMax = new Vector2(1f, 1f);
                rRt.pivot = new Vector2(0.5f, 1f);
                rRt.anchoredPosition = new Vector2(0f, -t * rowHeight - 6f);
                rRt.sizeDelta = new Vector2(0f, rowHeight);

                // Turn Number text
                Text turnTxt = CreateTextComp(rowObj.transform, $"{turnNum}.", 14, FontStyle.Bold, ColTextMuted, font, new Vector2(-285f, 0f), new Vector2(45f, 28f), TextAnchor.MiddleRight);
                MakeTextCrisp(turnTxt);

                // White Move button
                if (whiteIdx < count)
                {
                    var wItem = _activeAnalysis.moves[whiteIdx];
                    int targetPly = whiteIdx + 1;
                    string wSym = GetMoveBadgeSymbol(wItem.judgment);
                    string wScore = wItem.score >= 0 ? $"+{wItem.score:F1}" : $"{wItem.score:F1}";
                    string wClean = UserAccountManager.FormatCleanMoveNotation(wItem.san, wItem.uci);
                    string wText = $"{wSym} {wClean} <color=#94A3B8>({wScore})</color>";
                    CreateMoveButton(rowObj.transform, "BtnWhiteMove_" + whiteIdx, wText, new Vector2(-115f, 0f), new Vector2(230f, 32f), () => JumpToReviewPly(targetPly), font);
                }

                // Black Move button
                if (blackIdx < count)
                {
                    var bItem = _activeAnalysis.moves[blackIdx];
                    int targetPly = blackIdx + 1;
                    string bSym = GetMoveBadgeSymbol(bItem.judgment);
                    string bScore = bItem.score >= 0 ? $"+{bItem.score:F1}" : $"{bItem.score:F1}";
                    string bClean = UserAccountManager.FormatCleanMoveNotation(bItem.san, bItem.uci);
                    string bText = $"{bSym} {bClean} <color=#94A3B8>({bScore})</color>";
                    CreateMoveButton(rowObj.transform, "BtnBlackMove_" + blackIdx, bText, new Vector2(145f, 0f), new Vector2(230f, 32f), () => JumpToReviewPly(targetPly), font);
                }
            }
        }

        private void CreateMoveButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Action onClick, Font font)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);
            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)size.x, (int)size.y, 8f, ColGlassFill, ColCyanBorder, 1.2f);
            bg.type = Image.Type.Sliced;
            Button b = btnObj.AddComponent<Button>();
            b.onClick.AddListener(() => onClick?.Invoke());
            btnObj.AddComponent<MenuButtonEffect>();

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Text t = CreateTextComp(btnObj.transform, label, 13, FontStyle.Bold, ColTextWhite, font, Vector2.zero, size - new Vector2(16f, 4f), TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 11;
            t.resizeTextMaxSize = 13;
        }

        private void HighlightActiveMoveButton(int activeIdx)
        {
            if (_reviewMoveListParent == null) return;
            for (int i = 0; i < _reviewMoveListParent.childCount; i++)
            {
                Transform row = _reviewMoveListParent.GetChild(i);
                for (int c = 0; c < row.childCount; c++)
                {
                    Transform btn = row.GetChild(c);
                    Image bg = btn.GetComponent<Image>();
                    if (bg == null) continue;
                    bool isThisBtn = (activeIdx >= 0 && (btn.name == "BtnWhiteMove_" + activeIdx || btn.name == "BtnBlackMove_" + activeIdx));
                    bg.color = isThisBtn ? new Color(0f, 0.90f, 1f, 0.55f) : Color.white;
                    Text txt = btn.GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        txt.color = isThisBtn ? ColGoldBorder : ColTextWhite;
                    }
                }
            }
        }

        private Vector2Int SquareToCoord(string sq)
        {
            if (string.IsNullOrEmpty(sq) || sq.Trim().Length < 2) return new Vector2Int(-1, -1);
            string s = sq.Trim().ToLowerInvariant();
            int file = s[0] - 'a';
            int rank = s[1] - '1';
            return new Vector2Int(Mathf.Clamp(file, 0, 7), Mathf.Clamp(rank, 0, 7));
        }

        private List<TutorialPieceMarker> ParseFenToMarkers(string fen)
        {
            var list = new List<TutorialPieceMarker>();
            if (string.IsNullOrEmpty(fen)) return list;
            string[] parts = fen.Trim().Split(' ');
            string boardPart = parts[0];
            string[] ranks = boardPart.Split('/');
            if (ranks.Length != 8) return list;

            for (int r = 0; r < 8; r++)
            {
                int rankIdx = 7 - r;
                int fileIdx = 0;
                foreach (char c in ranks[r])
                {
                    if (char.IsDigit(c))
                    {
                        fileIdx += (c - '0');
                    }
                    else
                    {
                        bool isWhite = char.IsUpper(c);
                        char lower = char.ToLower(c);
                        string symbol = lower switch
                        {
                            'p' => isWhite ? "♙" : "♟",
                            'n' => isWhite ? "♘" : "♞",
                            'b' => isWhite ? "♗" : "♝",
                            'r' => isWhite ? "♖" : "♜",
                            'q' => isWhite ? "♕" : "♛",
                            'k' => isWhite ? "♔" : "♚",
                            _ => ""
                        };
                        if (fileIdx < 8)
                        {
                            list.Add(new TutorialPieceMarker(fileIdx, rankIdx, symbol, isWhite));
                        }
                        fileIdx++;
                    }
                }
            }
            return list;
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
                if (UserAccountManager.Instance != null && !string.IsNullOrEmpty(UserAccountManager.Instance.CurrentUsername))
                {
                    if (_loginAccountInput != null) _loginAccountInput.text = UserAccountManager.Instance.CurrentUsername;
                    if (_loginGmailInput != null) _loginGmailInput.text = UserAccountManager.Instance.CurrentGmail;
                }
                if (_loginPasswordInput != null) _loginPasswordInput.text = "";
                if (_loginErrorTxt != null) _loginErrorTxt.text = "";
                _loginModal.SetActive(true);
                AegisAudioManager.PlayModalOpen();
            }
        }

        private void BuildLoginModal(Transform parent, Font font)
        {
            _loginModal = CreateModalOverlay(parent, "LoginModalOverlay");

            GameObject cardObj = new GameObject("LoginCard");
            cardObj.transform.SetParent(_loginModal.transform, false);

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(640, 560, 24f, ColGlassFill, ColCyanBorder, 2f);
            cardImg.type = Image.Type.Sliced;

            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(640f, 560f);

            CreateModalHeader(cardObj.transform, AegisLocalization.T("ĐĂNG NHẬP / TÀI KHOẢN", "SIGN IN / ACCOUNT"), () => _loginModal.SetActive(false), font);

            _loginTabCredsPanel = new GameObject("TabCredsPanel");
            _loginTabCredsPanel.transform.SetParent(cardObj.transform, false);
            RectTransform credsPanelRt = _loginTabCredsPanel.AddComponent<RectTransform>();
            credsPanelRt.anchoredPosition = Vector2.zero;
            credsPanelRt.sizeDelta = new Vector2(600f, 400f);

            CreateTextComp(_loginTabCredsPanel.transform, AegisLocalization.T("TÀI KHOẢN HOẶC EMAIL:", "ACCOUNT OR EMAIL:"), 15, FontStyle.Bold, ColCyanBorder, font, new Vector2(0f, 135f), new Vector2(540f, 22f));
            _loginAccountInput = CreateInputField(_loginTabCredsPanel.transform, "AccountInput", AegisLocalization.T("Ví dụ: PhamVaA hoặc email@gmail.com...", "E.g.: Player01 or email@gmail.com..."), new Vector2(0f, 95f), new Vector2(540f, 46f), font, false);

            CreateTextComp(_loginTabCredsPanel.transform, AegisLocalization.T("MẬT KHẨU:", "PASSWORD:"), 15, FontStyle.Bold, ColCyanBorder, font, new Vector2(0f, 50f), new Vector2(540f, 22f));
            _loginPasswordInput = CreateInputField(_loginTabCredsPanel.transform, "PasswordInput", AegisLocalization.T("Nhập mật khẩu (tối thiểu 4 ký tự)...", "Enter password (min 4 chars)..."), new Vector2(0f, 10f), new Vector2(540f, 46f), font, true);

            CreateMenuActionButton(_loginTabCredsPanel.transform, "BtnSubmitCreds", AegisLocalization.T("ĐĂNG NHẬP", "SIGN IN"), new Vector2(-140f, -65f), new Vector2(260f, 48f), ColGoldCta, ColDarkBtnText, 18, () => ExecuteLoginWithCredentials(false), font, ColGoldBorder, 1.5f);
            CreateMenuActionButton(_loginTabCredsPanel.transform, "BtnRegisterCreds", AegisLocalization.T("TẠO TÀI KHOẢN", "CREATE ACCOUNT"), new Vector2(140f, -65f), new Vector2(260f, 48f), ColGlassFill, ColCyanGlow, 18, () => ExecuteLoginWithCredentials(true), font, ColCyanBorder, 1.5f);

            CreateMenuActionButton(_loginTabCredsPanel.transform, "BtnSubmitGoogle", AegisLocalization.T("G  TIẾP TỤC VỚI GOOGLE", "G  CONTINUE WITH GOOGLE"), new Vector2(0f, -145f), new Vector2(540f, 48f), new Color(0.96f, 0.97f, 0.98f, 1f), new Color(0.12f, 0.15f, 0.20f, 1f), 18, ExecuteLoginWithGoogle, font, new Color(0.75f, 0.78f, 0.82f, 1f), 1.2f);

            // =========================================================================
            // Error Text at Bottom of Card
            // =========================================================================
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
            errRt.anchoredPosition = new Vector2(0f, -195f);
            errRt.sizeDelta = new Vector2(560f, 32f);

            _loginModal.SetActive(false);
        }

        private void ExecuteLoginWithCredentials(bool isRegister)
        {
            string account = _loginAccountInput != null ? _loginAccountInput.text.Trim() : "";
            string password = _loginPasswordInput != null ? _loginPasswordInput.text.Trim() : "";

            if (string.IsNullOrEmpty(account))
            {
                if (_loginErrorTxt != null) _loginErrorTxt.text = AegisLocalization.T("Vui lòng nhập tên tài khoản hoặc email!", "Please enter account or email!");
                return;
            }

            if (string.IsNullOrEmpty(password) || password.Length < 4)
            {
                if (_loginErrorTxt != null) _loginErrorTxt.text = AegisLocalization.T("Mật khẩu phải có ít nhất 4 ký tự!", "Password must be at least 4 characters!");
                return;
            }

            if (UserAccountManager.Instance != null)
            {
                if (_loginErrorTxt != null) _loginErrorTxt.text = AegisLocalization.T("Đang xác thực...", "Authenticating...");
                UserAccountManager.Instance.LoginWithCredentials(account, password, isRegister, (success, msg) =>
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

        private void ExecuteLoginWithGoogle()
        {
            if (_loginErrorTxt != null)
            {
                _loginErrorTxt.color = new Color(0.4f, 0.85f, 1f);
                _loginErrorTxt.text = AegisLocalization.T("Đang mở trình duyệt để xác thực Google...", "Opening browser for Google Sign-in...");
            }

            if (UserAccountManager.Instance != null)
            {
                UserAccountManager.Instance.StartGoogleLogin((success, msg) =>
                {
                    if (_loginErrorTxt != null)
                    {
                        _loginErrorTxt.color = success ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
                        _loginErrorTxt.text = msg;
                    }

                    if (success)
                    {
                        if (_loginModal != null) _loginModal.SetActive(false);
                        UpdateUserProfileUI();
                        ShowToast(AegisLocalization.T($"Xin chào {UserAccountManager.Instance.CurrentUsername}!", $"Welcome {UserAccountManager.Instance.CurrentUsername}!"), ToastType.Success);
                        _postLoginAction?.Invoke();
                        _postLoginAction = null;
                    }
                });
            }
            else
            {
                if (_loginErrorTxt != null)
                {
                    _loginErrorTxt.color = new Color(1f, 0.4f, 0.4f);
                    _loginErrorTxt.text = AegisLocalization.T("Không tìm thấy trình quản lý tài khoản!", "Account manager not found!");
                }
            }
        }
        private void OpenOnlineMatchChoiceModal()
        {
            if (_onlineChoiceModal != null)
            {
                CloseAllModals();
                _onlineChoiceModal.SetActive(true);
                AegisAudioManager.PlayModalOpen();
            }
            else
            {
                StartFastOnlineMatchmaking();
            }
        }

        public void OpenOnlineChoiceModal()
        {
            OpenOnlineMatchChoiceModal();
        }

        private void BuildOnlineChoiceModal(Transform parent, Font font)
        {
            _onlineChoiceModal = CreateModalOverlay(parent, "OnlineChoiceModalOverlay");

            GameObject cardObj = new GameObject("OnlineChoiceCard");
            cardObj.transform.SetParent(_onlineChoiceModal.transform, false);

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(640, 390, 24f, ColGlassFill, ColCyanBorder, 2f);
            cardImg.type = Image.Type.Sliced;

            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(640f, 390f);

            CreateModalHeader(cardObj.transform, AegisLocalization.T("ĐẤU TRƯỜNG ONLINE MULTIPLAYER", "ONLINE MULTIPLAYER ARENA"), () => _onlineChoiceModal.SetActive(false), font);

            // Ranked Matchmaking Button (Card 1)
            GameObject rBtnObj = new GameObject("BtnRanked");
            rBtnObj.transform.SetParent(cardObj.transform, false);
            Image rImg = rBtnObj.AddComponent<Image>();
            rImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(560, 90, 14f, new Color(0.12f, 0.18f, 0.10f, 0.90f), ColGoldBorder, 1.5f);
            rImg.type = Image.Type.Sliced;
            Button rBtn = rBtnObj.AddComponent<Button>();
            rBtn.onClick.AddListener(() =>
            {
                _onlineChoiceModal.SetActive(false);
                ShowMatchmakingModal();
            });
            rBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform rRt = rBtnObj.GetComponent<RectTransform>();
            rRt.anchoredPosition = new Vector2(0f, 50f);
            rRt.sizeDelta = new Vector2(560f, 90f);

            CreateTextComp(rBtnObj.transform, AegisLocalization.T("⚔️  GHÉP ĐẤU XẾP HẠNG (AEGIS SERVER)", "⚔️  RANKED MATCHMAKING (AEGIS SERVER)"), 19, FontStyle.Bold, ColGoldCta, font, new Vector2(0f, 14f), new Vector2(520f, 26f), TextAnchor.MiddleCenter);
            CreateTextComp(rBtnObj.transform, AegisLocalization.T("Tự động tìm đối thủ online có mức Rank & Elo tương đồng", "Auto-match with online opponents with similar Elo & Rank"), 13, FontStyle.Normal, ColTextMuted, font, new Vector2(0f, -14f), new Vector2(520f, 20f), TextAnchor.MiddleCenter);

            // Friend Duel Button (Card 2)
            GameObject fBtnObj = new GameObject("BtnFriend");
            fBtnObj.transform.SetParent(cardObj.transform, false);
            Image fImg = fBtnObj.AddComponent<Image>();
            fImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(560, 90, 14f, new Color(0.06f, 0.14f, 0.22f, 0.90f), ColCyanBorder, 1.5f);
            fImg.type = Image.Type.Sliced;
            Button fBtn = fBtnObj.AddComponent<Button>();
            fBtn.onClick.AddListener(() =>
            {
                _onlineChoiceModal.SetActive(false);
                ShowFriendModal();
            });
            fBtnObj.AddComponent<MenuButtonEffect>();
            RectTransform fRt = fBtnObj.GetComponent<RectTransform>();
            fRt.anchoredPosition = new Vector2(0f, -55f);
            fRt.sizeDelta = new Vector2(560f, 90f);

            CreateTextComp(fBtnObj.transform, AegisLocalization.T("👥  THÁCH ĐẤU BẠN BÈ (FRIEND DUEL)", "👥  CHALLENGE FRIEND (DUEL)"), 19, FontStyle.Bold, ColCyanGlow, font, new Vector2(0f, 14f), new Vector2(520f, 26f), TextAnchor.MiddleCenter);
            CreateTextComp(fBtnObj.transform, AegisLocalization.T("Xem người chơi trực tuyến hoặc nhập tên tài khoản để mời", "View online players or invite directly by player username"), 13, FontStyle.Normal, ColTextMuted, font, new Vector2(0f, -14f), new Vector2(520f, 20f), TextAnchor.MiddleCenter);

            // Close Button
            CreateMenuActionButton(cardObj.transform, "BtnCloseChoice", AegisLocalization.T("ĐÓNG", "CLOSE"), new Vector2(0f, -145f), new Vector2(160f, 44f), ColGlassFill, ColTextMuted, 16, () => _onlineChoiceModal.SetActive(false), font, ColMutedBorder, 1.2f);

            _onlineChoiceModal.SetActive(false);
        }

        public void StartFastOnlineMatchmaking()
        {
            CloseAllModals();
            if (_matchmakingModal == null) return;

            // Queue on Aegis server (Unity & Web real players pool)
            if (OnlineMultiplayerManager.Instance != null)
            {
                OnlineMultiplayerManager.Instance.StartRandomMatchmaking("10 + 0");
            }

            if (_matchmakingStatusTxt != null)
            {
                _matchmakingStatusTxt.text = AegisLocalization.T(
                    "Đang tìm kiếm đối thủ online trên hệ thống máy chủ Aegis...",
                    "Searching for online opponent on Aegis Server..."
                );
            }

            if (_matchmakingTimerCoroutine != null) StopCoroutine(_matchmakingTimerCoroutine);
            _matchmakingTimerCoroutine = StartCoroutine(AnimateMatchmakingTimer());
            _matchmakingModal.SetActive(true);
            AegisAudioManager.PlayModalOpen();
        }

        private void ShowMatchmakingModal()
        {
            StartFastOnlineMatchmaking();
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
            cardRt.sizeDelta = new Vector2(620f, 430f);

            // Radar Icon
            GameObject radarObj = new GameObject("RadarIcon");
            radarObj.transform.SetParent(cardObj.transform, false);
            Text radarTxt = radarObj.AddComponent<Text>();
            radarTxt.text = "◎";
            radarTxt.font = font;
            radarTxt.fontSize = 52;
            radarTxt.fontStyle = FontStyle.Bold;
            radarTxt.alignment = TextAnchor.MiddleCenter;
            radarTxt.color = ColCyanBorder;
            _radarIconRt = radarObj.GetComponent<RectTransform>();
            _radarIconRt.anchoredPosition = new Vector2(0f, 120f);
            _radarIconRt.sizeDelta = new Vector2(80f, 80f);

            CreateTextComp(cardObj.transform, AegisLocalization.T("ĐANG TÌM ĐỐI THỦ ONLINE...", "SEARCHING FOR ONLINE OPPONENT..."), 22, FontStyle.Bold, ColTextWhite, font, new Vector2(0f, 55f), new Vector2(560f, 36f));

            GameObject sObj = new GameObject("Status");
            sObj.transform.SetParent(cardObj.transform, false);
            _matchmakingStatusTxt = sObj.AddComponent<Text>();
            _matchmakingStatusTxt.text = AegisLocalization.T("Ghép ngẫu nhiên người chơi trực tuyến có mức Rank tương đồng.", "Matching online players with similar Elo ratings.");
            _matchmakingStatusTxt.font = font;
            _matchmakingStatusTxt.fontSize = 13;
            _matchmakingStatusTxt.alignment = TextAnchor.MiddleCenter;
            _matchmakingStatusTxt.color = ColTextMuted;
            _matchmakingStatusTxt.lineSpacing = 1.25f;
            _matchmakingStatusTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            _matchmakingStatusTxt.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform sRt = sObj.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, 0f);
            sRt.sizeDelta = new Vector2(480f, 65f);

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
            tRt.anchoredPosition = new Vector2(0f, -50f);
            tRt.sizeDelta = new Vector2(520f, 32f);

            CreateMenuActionButton(cardObj.transform, "BtnCancelMatchmaking", AegisLocalization.T("HỦY TÌM TRẬN", "CANCEL QUEUE"), new Vector2(0f, -125f), new Vector2(280f, 48f), new Color(0.28f, 0.08f, 0.08f, 0.95f), new Color(1f, 0.45f, 0.45f), 17, () =>
            {
                if (OnlineMultiplayerManager.Instance != null) OnlineMultiplayerManager.Instance.CancelRandomMatchmaking();
                if (LichessBridge.Instance != null) LichessBridge.Instance.CancelMatch();
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
            AegisAudioManager.PlayModalOpen();
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
            UpdateRightFriendsDrawerUI(players);

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
            _challengeBodyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
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

            // Cancel Lichess search since Server match was found first
            if (LichessBridge.Instance != null && !LichessBridge.Instance.IsInLichessMatch)
            {
                LichessBridge.Instance.CancelMatch();
            }

            if (OnlineMultiplayerManager.Instance != null)
            {
                OnlineMultiplayerManager.Instance.PrepareOnlineMatch(role, oppName, oppElo);
            }

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
                UIManager2D.Instance.SetOnlineOpponent(oppName, oppElo);
                string colorStr = (role == PieceColor.White) ? AegisLocalization.T("TRẮNG (đi trước)", "WHITE (plays first)") : AegisLocalization.T("ĐEN (đi sau)", "BLACK (plays second)");
                UIManager2D.Instance.RecordMove(AegisLocalization.T($"[TRẬN ĐẤU ONLINE] Bạn cầm quân {colorStr} đấu với {oppName} (Elo {oppElo}) | Phòng: {roomId}", $"[ONLINE MATCH] You play {colorStr} vs {oppName} (Elo {oppElo}) | Room: {roomId}"));
                UIManager2D.Instance.SetHUDVisible(true);
            }

            ShowToast(AegisLocalization.T($"Trận đấu bắt đầu! Đối thủ: {oppName}", $"Match started! Opponent: {oppName}"), ToastType.Success);
        }

        private void HandleLichessMatchFound(string oppName, int oppElo, bool isWhite)
        {
            if (OnlineMultiplayerManager.Instance != null && OnlineMultiplayerManager.Instance.IsOnlineMatch)
            {
                return; // An Aegis game already started
            }

            // Cancel server matchmaking since Lichess matched first
            if (OnlineMultiplayerManager.Instance != null && OnlineMultiplayerManager.Instance.IsSearchingMatch)
            {
                OnlineMultiplayerManager.Instance.CancelRandomMatchmaking();
            }

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

            if (!isWhite && OnlineMultiplayerManager.Instance != null)
            {
                OnlineMultiplayerManager.Instance.AlignCameraForBlackPlayer();
            }
            else if (isWhite && OnlineMultiplayerManager.Instance != null)
            {
                OnlineMultiplayerManager.Instance.AlignCameraForWhitePlayer();
            }

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.isRankedMode = true;
                UIManager2D.Instance.ConfigureTimeControl("10 + 0");
                UIManager2D.Instance.SetOnlineOpponent(oppName, oppElo);
                string colorStr = isWhite ? AegisLocalization.T("TRẮNG (đi trước)", "WHITE (plays first)") : AegisLocalization.T("ĐEN (đi sau)", "BLACK (plays second)");
                UIManager2D.Instance.RecordMove(AegisLocalization.T($"[TRẬN ĐẤU LICHESS] Bạn cầm quân {colorStr} đấu với {oppName} (Elo {oppElo})", $"[LICHESS MATCH] You play {colorStr} vs {oppName} (Elo {oppElo})"));
                UIManager2D.Instance.SetHUDVisible(true);
            }

            ShowToast(AegisLocalization.T($"Trận đấu bắt đầu! Đối thủ Lichess: {oppName} ({oppElo})", $"Match started! Lichess Opponent: {oppName} ({oppElo})"), ToastType.Success);
        }

        private void HandleLichessGameEnded(string result, int eloChange, int goldGain, string endReason)
        {
            bool isAborted = (endReason == "aborted" || result == "aborted");
            bool victory = !isAborted && (result == "win");
            bool isDraw = !isAborted && (result == "draw");

            string reasonStr = "";
            if (isAborted)
            {
                reasonStr = AegisLocalization.T("Hủy ván cờ!", "Match aborted!");
            }
            else if (endReason == "outoftime")
            {
                reasonStr = victory ? AegisLocalization.T("Đối thủ hết thời gian!", "Opponent ran out of time!") : AegisLocalization.T("Hết thời gian trận đấu!", "Out of match time!");
            }
            else if (endReason == "mate")
            {
                reasonStr = victory ? AegisLocalization.T("Chiếu bí đối thủ!", "Checkmate!") : AegisLocalization.T("Bị chiếu bí!", "Checkmated!");
            }
            else if (endReason == "resign")
            {
                reasonStr = victory ? AegisLocalization.T("Đối thủ đã đầu hàng!", "Opponent resigned!") : AegisLocalization.T("Bạn đã đầu hàng!", "You resigned!");
            }
            else
            {
                reasonStr = isDraw ? AegisLocalization.T("Hòa cờ", "Draw") : (victory ? AegisLocalization.T("Chiến thắng!", "Victory!") : AegisLocalization.T("Thất bại!", "Defeat!"));
            }

            PieceColor myColor = (LichessBridge.Instance != null && LichessBridge.Instance.IsPlayerWhite) ? PieceColor.White : PieceColor.Black;
            PieceColor winnerColor = (isDraw || isAborted) ? PieceColor.White : (victory ? myColor : (myColor == PieceColor.White ? PieceColor.Black : PieceColor.White));

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.TriggerGameOver(winnerColor, reasonStr, isDraw, isAborted);
            }
            else
            {
                ShowResultScreen(victory, reasonStr, LichessBridge.Instance != null ? LichessBridge.Instance.LastMoveCount : 0, 120f, isDraw, isAborted);
            }
        }

        // =========================================================================
        // GAME LAUNCHER
        // =========================================================================
        public void LaunchGame(GameLaunchMode mode)
        {
            _lastLaunchMode = mode;
            if (mode != GameLaunchMode.OnlineMultiplayer)
            {
                HideMenu();
            }

            if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
            {
                MechChess.Chess3D.ChessBoard3DManager.Instance.ResetBoard();
            }

            switch (mode)
            {
                case GameLaunchMode.VersusAI:
                    if (UIManager2D.Instance != null)
                    {
                        UIManager2D.Instance.SetARHUDVisible(false);
                        UIManager2D.Instance.SetHUDVisible(true);
                    }
                    StartGameVsAI(2);
                    break;

                case GameLaunchMode.LocalPassAndPlay:
                    if (MechChess.Chess3D.ChessBoard3DManager.Instance != null)
                        MechChess.Chess3D.ChessBoard3DManager.Instance.isVsAI = false;
                    if (UIManager2D.Instance != null)
                    {
                        UIManager2D.Instance.SetARHUDVisible(false);
                        UIManager2D.Instance.isRankedMode = false;
                        UIManager2D.Instance.ConfigureTimeControl("10 + 0");
                        UIManager2D.Instance.RecordMove(AegisLocalization.T("[BẮT ĐẦU] Chế độ: Hai người chơi luân phiên (Pass & Play)", "[START] Mode: Local Pass & Play"));
                        UIManager2D.Instance.SetHUDVisible(true);
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
                    if (_menuCanvasObj != null) _menuCanvasObj.SetActive(true);
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
                   (_collectionModal != null && _collectionModal.activeSelf) ||
                   (_pauseModal != null && _pauseModal.activeSelf) ||
                   (_resultScreenModal != null && _resultScreenModal.activeSelf) ||
                   (_profileModal != null && _profileModal.activeSelf) ||
                   (_learnScreenModal != null && _learnScreenModal.activeSelf) ||
                   (_confirmDialogModal != null && _confirmDialogModal.activeSelf) ||
                   (_loginModal != null && _loginModal.activeSelf) ||
                   (_matchmakingModal != null && _matchmakingModal.activeSelf) ||
                   (_friendModal != null && _friendModal.activeSelf) ||
                   (_challengeModal != null && _challengeModal.activeSelf) ||
                   (_leaderboardModal != null && _leaderboardModal.activeSelf) ||
                   (_gameReviewModal != null && _gameReviewModal.activeSelf);
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
            if (modal != null && modal.activeSelf) AegisAudioManager.PlayModalClose();
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
            if (_aiDifficultyModal != null)
            {
                SetAITimeDropdownOpen(false);
                _aiDifficultyModal.SetActive(true);
                AegisAudioManager.PlayModalOpen();
            }
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
                AegisAudioManager.PlayModalOpen();
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
                AegisAudioManager.PlayModalOpen();
                UpdateTutorialUI();
            }
        }

        public void OpenShopModal()
        {
            CloseAllModals();
            if (!_isInGameMatch) EnsureBaseScreenActive();
            if (_shopModal != null)
            {
                _shopModal.SetActive(true);
                AegisAudioManager.PlayModalOpen();
            }
        }

        public void OpenSeasonLeaderboardModal()
        {
            CloseAllModals();
            if (!_isInGameMatch) EnsureBaseScreenActive();
            if (_leaderboardModal != null)
            {
                _leaderboardModal.SetActive(true);
                AegisAudioManager.PlayModalOpen();
                SwitchLeaderboardTab(0);
            }
        }

        private void SwitchLeaderboardTab(int tabIndex)
        {
            if (_selectedLeaderboardTab != tabIndex) AegisAudioManager.PlayTabSwitch();
            _selectedLeaderboardTab = tabIndex;
            Sprite activeSprite = ArcaneAssetLoader.LoadArcaneSprite("settings_tab_active.png") ?? ArcaneAssetLoader.LoadArcaneSprite("shop_tab_active.png");

            for (int i = 0; i < 3; i++)
            {
                bool active = (i == tabIndex);
                if (_leaderboardTabBgs[i] != null)
                {
                    _leaderboardTabBgs[i].sprite = active ? activeSprite : null;
                    _leaderboardTabBgs[i].color = active ? Color.white : new Color(0.04f, 0.12f, 0.24f, 0.70f);
                }
                if (_leaderboardTabTexts[i] != null)
                {
                    _leaderboardTabTexts[i].fontStyle = active ? FontStyle.Bold : FontStyle.Normal;
                    _leaderboardTabTexts[i].color = active ? ColCyanGlow : new Color(0.78f, 0.88f, 0.98f, 0.9f);
                }
                if (_leaderboardTabPanels[i] != null)
                {
                    _leaderboardTabPanels[i].SetActive(active);
                }
            }
        }

        private void BuildSeasonRankLeaderboardWidget(Transform parent, Font font)
        {
            GameObject widgetObj = new GameObject("SeasonRankWidget");
            widgetObj.transform.SetParent(parent, false);

            RectTransform wRt = widgetObj.AddComponent<RectTransform>();
            wRt.anchorMin = new Vector2(1f, 1f);
            wRt.anchorMax = new Vector2(1f, 1f);
            wRt.pivot = new Vector2(1f, 1f);
            wRt.anchoredPosition = new Vector2(-60f, -140f);
            wRt.sizeDelta = new Vector2(460f, 185f);

            // Card Background: Arcane Obsidian Glass with glowing Antique Gold / Cyan border
            Image cardBg = widgetObj.AddComponent<Image>();
            cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(460, 185, 20f, new Color(0.04f, 0.08f, 0.16f, 0.90f), ColGoldBorder, 1.8f);
            cardBg.type = Image.Type.Sliced;

            // Whole widget button interaction with tactile click and hover sound
            Button widgetBtn = widgetObj.AddComponent<Button>();
            widgetBtn.targetGraphic = cardBg;
            widgetBtn.onClick.AddListener(OpenSeasonLeaderboardModal);
            widgetObj.AddComponent<MenuButtonEffect>();

            // 1. Top Bar: Golden Season Ribbon & Season countdown
            GameObject seasonPill = new GameObject("SeasonPill");
            seasonPill.transform.SetParent(widgetObj.transform, false);
            Image pillImg = seasonPill.AddComponent<Image>();
            pillImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(195, 28, 8f, new Color(0.72f, 0.58f, 0.28f, 0.25f), ColGoldBorder, 1.2f);
            pillImg.type = Image.Type.Sliced;
            RectTransform pRt = seasonPill.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0f, 1f);
            pRt.anchorMax = new Vector2(0f, 1f);
            pRt.pivot = new Vector2(0f, 1f);
            pRt.anchoredPosition = new Vector2(16f, -12f);
            pRt.sizeDelta = new Vector2(195f, 28f);

            CreateTextComp(seasonPill.transform, AegisLocalization.T("✦ MÙA GIẢI MỚI • S1 ✦", "✦ NEW SEASON • S1 ✦"), 13, FontStyle.Bold, new Color(1f, 0.92f, 0.65f), font, Vector2.zero, new Vector2(190f, 26f), TextAnchor.MiddleCenter);

            // Countdown Timer:
            CreateTextComp(widgetObj.transform, AegisLocalization.T("⏳ Còn 28 ngày", "⏳ 28d Left"), 13, FontStyle.Normal, ColCyanGlow, font, new Vector2(130f, 66f), new Vector2(160f, 26f), TextAnchor.MiddleRight);

            // 2. Middle Row: Big Trophy / Crest + Rank Details (Mobile Legends Style)
            GameObject crestObj = new GameObject("RankCrest");
            crestObj.transform.SetParent(widgetObj.transform, false);
            Image crestImg = crestObj.AddComponent<Image>();
            Sprite crestSp = ArcaneAssetLoader.LoadArcaneSprite("dock_icon_rank.png") ?? ArcaneAssetLoader.LoadArcaneSprite("profile_rank_crest.png");
            if (crestSp != null) crestImg.sprite = crestSp;
            else crestImg.color = ColAntiqueGold;
            crestImg.preserveAspect = true;
            RectTransform crt = crestObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0.5f);
            crt.anchorMax = new Vector2(0f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(56f, 5f);
            crt.sizeDelta = new Vector2(64f, 64f);

            // Rank Name:
            string currentRankName = AegisLocalization.T("HUYỀN THOẠI III", "LEGEND III");
            int userElo = (UserAccountManager.Instance != null && UserAccountManager.Instance.IsLoggedIn) ? UserAccountManager.Instance.CurrentElo : 1200;
            if (userElo >= 2000) currentRankName = AegisLocalization.T("CHIẾN TƯỚNG", "MYTHIC");
            else if (userElo >= 1600) currentRankName = AegisLocalization.T("HUYỀN THOẠI I", "LEGEND I");
            else if (userElo >= 1400) currentRankName = AegisLocalization.T("HUYỀN THOẠI II", "LEGEND II");

            CreateTextComp(widgetObj.transform, currentRankName, 22, FontStyle.Bold, ColTextWhite, font, new Vector2(60f, 24f), new Vector2(280f, 28f), TextAnchor.MiddleLeft);

            // Stars & Elo:
            CreateTextComp(widgetObj.transform, AegisLocalization.T($"⭐⭐⭐⭐☆ • Elo {userElo}", $"⭐⭐⭐⭐☆ • Elo {userElo}"), 14, FontStyle.Bold, new Color(1f, 0.85f, 0.25f), font, new Vector2(60f, -2f), new Vector2(280f, 22f), TextAnchor.MiddleLeft);

            // Server Standing / Top Ranking:
            CreateTextComp(widgetObj.transform, AegisLocalization.T("🏆 Hạng #12 Toàn Server", "🏆 Server Rank #12"), 14, FontStyle.Normal, ColCyanGlow, font, new Vector2(60f, -24f), new Vector2(280f, 20f), TextAnchor.MiddleLeft);

            // 3. Bottom CTA Row: "BẢNG XẾP HẠNG ❯" with smooth arrow & glass button bar
            GameObject btnBar = new GameObject("BtnLeaderboardBar");
            btnBar.transform.SetParent(widgetObj.transform, false);
            Image bbImg = btnBar.AddComponent<Image>();
            bbImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(428, 34, 10f, new Color(0.06f, 0.16f, 0.28f, 0.95f), ColCyanBorder, 1.2f);
            bbImg.type = Image.Type.Sliced;
            RectTransform bbrt = btnBar.GetComponent<RectTransform>();
            bbrt.anchorMin = new Vector2(0.5f, 0f);
            bbrt.anchorMax = new Vector2(0.5f, 0f);
            bbrt.pivot = new Vector2(0.5f, 0f);
            bbrt.anchoredPosition = new Vector2(0f, 10f);
            bbrt.sizeDelta = new Vector2(428f, 34f);

            CreateTextComp(btnBar.transform, AegisLocalization.T("XEM BẢNG XẾP HẠNG  ❯", "VIEW LEADERBOARD  ❯"), 14, FontStyle.Bold, Color.white, font, Vector2.zero, new Vector2(420f, 30f), TextAnchor.MiddleCenter);
        }

        private void BuildSeasonLeaderboardModal(Transform parent, Font font)
        {
            _leaderboardModal = CreateModalOverlay(parent, "SeasonLeaderboardModal");

            // Main Window Card (1440 x 820)
            GameObject cardObj = new GameObject("LeaderboardCard");
            cardObj.transform.SetParent(_leaderboardModal.transform, false);
            RectTransform cRt = cardObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(1440f, 820f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1440, 820, 24f, new Color(0.03f, 0.07f, 0.15f, 0.96f), ColGoldBorder, 2f);
            cardBg.type = Image.Type.Sliced;

            // Header with Close Button
            CreateModalHeader(cardObj.transform, AegisLocalization.T("🏆 BẢNG XẾP HẠNG MÙA GIẢI MỚI — S1: KHỞI NGUYÊN", "🏆 NEW SEASON LEADERBOARD — S1: AEGIS ASCENT"), () => CloseModal(_leaderboardModal), font);

            // Season Info Strip below Header (anchored top)
            GameObject infoStrip = new GameObject("SeasonInfoStrip");
            infoStrip.transform.SetParent(cardObj.transform, false);
            RectTransform isRt = infoStrip.AddComponent<RectTransform>();
            isRt.anchorMin = new Vector2(0.5f, 1f);
            isRt.anchorMax = new Vector2(0.5f, 1f);
            isRt.pivot = new Vector2(0.5f, 1f);
            isRt.anchoredPosition = new Vector2(0f, -68f);
            isRt.sizeDelta = new Vector2(1360f, 38f);

            Image isBg = infoStrip.AddComponent<Image>();
            isBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1360, 38, 8f, new Color(0.06f, 0.12f, 0.22f, 0.75f), ColGoldBorder, 1f);
            isBg.type = Image.Type.Sliced;

            CreateTextComp(infoStrip.transform, AegisLocalization.T("✦ MÙA GIẢI 1 • KẾT THÚC SAU: 28 NGÀY 14 GIỜ", "✦ SEASON 1 • ENDS IN: 28 DAYS 14 HOURS"), 14, FontStyle.Bold, new Color(1f, 0.88f, 0.45f), font, new Vector2(-280f, 0f), new Vector2(600f, 30f), TextAnchor.MiddleLeft);
            CreateTextComp(infoStrip.transform, AegisLocalization.T("Cơ chế ghép đấu: Elo Chuẩn Quốc Tế FIDE AR", "Matchmaking: FIDE AR Standard Elo"), 13, FontStyle.Normal, ColCyanGlow, font, new Vector2(300f, 0f), new Vector2(500f, 30f), TextAnchor.MiddleRight);

            // Sub-Tabs Bar (Height 48px)
            GameObject tabsBar = new GameObject("TabsBar");
            tabsBar.transform.SetParent(cardObj.transform, false);
            RectTransform tbRt = tabsBar.AddComponent<RectTransform>();
            tbRt.anchorMin = new Vector2(0.5f, 1f);
            tbRt.anchorMax = new Vector2(0.5f, 1f);
            tbRt.pivot = new Vector2(0.5f, 1f);
            tbRt.anchoredPosition = new Vector2(0f, -114f);
            tbRt.sizeDelta = new Vector2(1360f, 48f);

            string[] tabNames = {
                AegisLocalization.T("Toàn Máy Chủ", "Top Server"),
                AegisLocalization.T("Bạn Bè", "Friends"),
                AegisLocalization.T("Phần Thưởng Mùa", "Season Rewards")
            };

            float tabWidth = 260f;
            for (int i = 0; i < 3; i++)
            {
                int tabIdx = i;
                GameObject tObj = new GameObject("Tab_" + i);
                tObj.transform.SetParent(tabsBar.transform, false);

                RectTransform trt = tObj.AddComponent<RectTransform>();
                trt.anchorMin = new Vector2(0f, 0.5f);
                trt.anchorMax = new Vector2(0f, 0.5f);
                trt.pivot = new Vector2(0f, 0.5f);
                trt.anchoredPosition = new Vector2(i * (tabWidth + 14f), 0f);
                trt.sizeDelta = new Vector2(tabWidth, 44f);

                Image tbg = tObj.AddComponent<Image>();
                tbg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)tabWidth, 44, 10f, new Color(0.04f, 0.12f, 0.24f, 0.70f), ColCyanBorder, 1.2f);
                tbg.type = Image.Type.Sliced;
                _leaderboardTabBgs[i] = tbg;

                Button tb = tObj.AddComponent<Button>();
                tb.targetGraphic = tbg;
                tb.onClick.AddListener(() => SwitchLeaderboardTab(tabIdx));
                tObj.AddComponent<MenuButtonEffect>();

                GameObject ttObj = new GameObject("Text");
                ttObj.transform.SetParent(tObj.transform, false);
                Text tt = ttObj.AddComponent<Text>();
                tt.text = tabNames[i];
                tt.font = font;
                tt.fontSize = 18;
                tt.fontStyle = (i == 0) ? FontStyle.Bold : FontStyle.Normal;
                tt.alignment = TextAnchor.MiddleCenter;
                tt.color = (i == 0) ? ColCyanGlow : ColTextWhite;
                RectTransform ttrt = ttObj.GetComponent<RectTransform>();
                ttrt.anchorMin = Vector2.zero;
                ttrt.anchorMax = Vector2.one;
                ttrt.offsetMin = Vector2.zero;
                ttrt.offsetMax = Vector2.zero;
                _leaderboardTabTexts[i] = tt;
            }

            // Tab 0: Server
            GameObject panelServer = new GameObject("Panel_ServerRank");
            panelServer.transform.SetParent(cardObj.transform, false);
            _leaderboardTabPanels[0] = panelServer;
            RectTransform psRt = panelServer.AddComponent<RectTransform>();
            psRt.anchorMin = new Vector2(0.5f, 0f);
            psRt.anchorMax = new Vector2(0.5f, 1f);
            psRt.pivot = new Vector2(0.5f, 0.5f);
            psRt.anchoredPosition = new Vector2(0f, -80f);
            psRt.sizeDelta = new Vector2(1360f, -180f);

            BuildServerRankTable(panelServer.transform, font);

            // Tab 1: Friends
            GameObject panelFriends = new GameObject("Panel_FriendsRank");
            panelFriends.transform.SetParent(cardObj.transform, false);
            _leaderboardTabPanels[1] = panelFriends;
            RectTransform pfRt = panelFriends.AddComponent<RectTransform>();
            pfRt.anchorMin = new Vector2(0.5f, 0f);
            pfRt.anchorMax = new Vector2(0.5f, 1f);
            pfRt.pivot = new Vector2(0.5f, 0.5f);
            pfRt.anchoredPosition = new Vector2(0f, -80f);
            pfRt.sizeDelta = new Vector2(1360f, -180f);

            BuildFriendsRankTable(panelFriends.transform, font);
            panelFriends.SetActive(false);

            // Tab 2: Rewards
            GameObject panelRewards = new GameObject("Panel_SeasonRewards");
            panelRewards.transform.SetParent(cardObj.transform, false);
            _leaderboardTabPanels[2] = panelRewards;
            RectTransform prRt = panelRewards.AddComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0.5f, 0f);
            prRt.anchorMax = new Vector2(0.5f, 1f);
            prRt.pivot = new Vector2(0.5f, 0.5f);
            prRt.anchoredPosition = new Vector2(0f, -80f);
            prRt.sizeDelta = new Vector2(1360f, -180f);

            BuildSeasonRewardsPanel(panelRewards.transform, font);
            panelRewards.SetActive(false);

            _leaderboardModal.SetActive(false);
        }

        private void BuildServerRankTable(Transform parent, Font font)
        {
            // Table Header Bar (Height 36px)
            GameObject thObj = new GameObject("TableHeader");
            thObj.transform.SetParent(parent, false);
            RectTransform thRt = thObj.AddComponent<RectTransform>();
            thRt.anchorMin = new Vector2(0.5f, 1f);
            thRt.anchorMax = new Vector2(0.5f, 1f);
            thRt.pivot = new Vector2(0.5f, 1f);
            thRt.anchoredPosition = new Vector2(0f, -5f);
            thRt.sizeDelta = new Vector2(1360f, 36f);

            Image thBg = thObj.AddComponent<Image>();
            thBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1360, 36, 6f, new Color(0.04f, 0.10f, 0.20f, 0.85f), ColCyanBorder, 1f);
            thBg.type = Image.Type.Sliced;

            CreateTextComp(thObj.transform, AegisLocalization.T("HẠNG", "RANK"), 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(-600f, 0f), new Vector2(100f, 30f), TextAnchor.MiddleCenter);
            CreateTextComp(thObj.transform, AegisLocalization.T("KỲ THỦ", "PLAYER"), 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(-360f, 0f), new Vector2(260f, 30f), TextAnchor.MiddleLeft);
            CreateTextComp(thObj.transform, AegisLocalization.T("BẬC RANK", "TIER"), 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(-40f, 0f), new Vector2(220f, 30f), TextAnchor.MiddleLeft);
            CreateTextComp(thObj.transform, AegisLocalization.T("ĐIỂM ELO", "ELO RATING"), 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(240f, 0f), new Vector2(160f, 30f), TextAnchor.MiddleCenter);
            CreateTextComp(thObj.transform, AegisLocalization.T("TỶ LỆ THẮNG", "WIN RATE"), 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(490f, 0f), new Vector2(200f, 30f), TextAnchor.MiddleCenter);

            // Top Players List (7 rows, step 56f)
            string[] topNames = { "Aegis_Vanguard", "Cyber_Knight99", "Arcane_Queen", "Shadow_Bishop", "Grandmaster_VN", "Steel_Rook", "PawnStormer" };
            string[] topTiers = { "ĐẠI CAO THỦ I", "HUYỀN THOẠI I", "HUYỀN THOẠI II", "KIM CƯƠNG I", "KIM CƯƠNG II", "BẠCH KIM I", "BẠCH KIM II" };
            int[] topElos = { 2450, 2320, 2210, 2050, 1980, 1890, 1780 };
            string[] topWinRates = { "78% (340 trận)", "72% (295 trận)", "69% (280 trận)", "65% (210 trận)", "64% (190 trận)", "62% (165 trận)", "59% (140 trận)" };

            float startY = -48f;
            float rowHeight = 52f;
            float rowStep = 56f;

            for (int i = 0; i < 7; i++)
            {
                GameObject rowObj = new GameObject("RankRow_" + (i + 1));
                rowObj.transform.SetParent(parent, false);
                RectTransform rRt = rowObj.AddComponent<RectTransform>();
                rRt.anchorMin = new Vector2(0.5f, 1f);
                rRt.anchorMax = new Vector2(0.5f, 1f);
                rRt.pivot = new Vector2(0.5f, 1f);
                rRt.anchoredPosition = new Vector2(0f, startY - i * rowStep);
                rRt.sizeDelta = new Vector2(1360f, rowHeight);

                Color rowBorder = (i == 0) ? ColGoldBorder : (i == 1) ? new Color(0.85f, 0.90f, 0.98f) : (i == 2) ? new Color(0.82f, 0.55f, 0.35f) : ColMutedBorder;
                Color rowFill = (i == 0) ? new Color(0.72f, 0.55f, 0.20f, 0.15f) : (i == 1) ? new Color(0.40f, 0.50f, 0.65f, 0.12f) : (i == 2) ? new Color(0.55f, 0.35f, 0.20f, 0.12f) : new Color(0.02f, 0.07f, 0.14f, 0.65f);

                Image rBg = rowObj.AddComponent<Image>();
                rBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1360, (int)rowHeight, 8f, rowFill, rowBorder, 1.2f);
                rBg.type = Image.Type.Sliced;

                // Rank badge: Crown / Number
                string rankGlyph = (i == 0) ? "🥇 1" : (i == 1) ? "🥈 2" : (i == 2) ? "🥉 3" : $"#{i + 1}";
                Color rankColor = (i == 0) ? ColAntiqueGold : (i == 1) ? Color.white : (i == 2) ? new Color(0.92f, 0.65f, 0.45f) : ColParchment;
                CreateTextComp(rowObj.transform, rankGlyph, 18, FontStyle.Bold, rankColor, font, new Vector2(-600f, 0f), new Vector2(100f, 36f), TextAnchor.MiddleCenter);

                // Player name
                CreateTextComp(rowObj.transform, topNames[i], 17, FontStyle.Bold, Color.white, font, new Vector2(-360f, 0f), new Vector2(260f, 36f), TextAnchor.MiddleLeft);

                // Tier
                CreateTextComp(rowObj.transform, topTiers[i], 16, FontStyle.Bold, (i < 3) ? ColAntiqueGold : ColCyanGlow, font, new Vector2(-40f, 0f), new Vector2(220f, 36f), TextAnchor.MiddleLeft);

                // Elo Rating
                CreateTextComp(rowObj.transform, topElos[i].ToString(), 17, FontStyle.Bold, new Color(1f, 0.88f, 0.35f), font, new Vector2(240f, 0f), new Vector2(160f, 36f), TextAnchor.MiddleCenter);

                // Win rate
                CreateTextComp(rowObj.transform, topWinRates[i], 15, FontStyle.Normal, new Color(0.85f, 0.92f, 1f), font, new Vector2(490f, 0f), new Vector2(200f, 36f), TextAnchor.MiddleCenter);
            }

            // Fixed Sticky Bottom Player Card (User Standings)
            GameObject userCard = new GameObject("UserStandingCard");
            userCard.transform.SetParent(parent, false);
            RectTransform ucRt = userCard.AddComponent<RectTransform>();
            ucRt.anchorMin = new Vector2(0.5f, 0f);
            ucRt.anchorMax = new Vector2(0.5f, 0f);
            ucRt.pivot = new Vector2(0.5f, 0f);
            ucRt.anchoredPosition = new Vector2(0f, 8f);
            ucRt.sizeDelta = new Vector2(1360f, 66f);

            Image ucBg = userCard.AddComponent<Image>();
            ucBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1360, 66, 12f, new Color(0.04f, 0.14f, 0.28f, 0.95f), ColCyanBorder, 2f);
            ucBg.type = Image.Type.Sliced;

            string myName = (UserAccountManager.Instance != null && UserAccountManager.Instance.IsLoggedIn) ? UserAccountManager.Instance.CurrentUsername : "Player001";
            int myElo = (UserAccountManager.Instance != null && UserAccountManager.Instance.IsLoggedIn) ? UserAccountManager.Instance.CurrentElo : 1200;

            CreateTextComp(userCard.transform, AegisLocalization.T("🏆 HẠNG #12", "🏆 RANK #12"), 19, FontStyle.Bold, ColCyanGlow, font, new Vector2(-570f, 0f), new Vector2(160f, 40f), TextAnchor.MiddleCenter);
            CreateTextComp(userCard.transform, $"{myName} " + AegisLocalization.T("(Bạn)", "(You)"), 18, FontStyle.Bold, Color.white, font, new Vector2(-360f, 0f), new Vector2(240f, 40f), TextAnchor.MiddleLeft);
            CreateTextComp(userCard.transform, AegisLocalization.T("HUYỀN THOẠI III", "LEGEND III"), 17, FontStyle.Bold, ColAntiqueGold, font, new Vector2(-40f, 0f), new Vector2(220f, 40f), TextAnchor.MiddleLeft);
            CreateTextComp(userCard.transform, $"{myElo} Elo", 18, FontStyle.Bold, new Color(1f, 0.88f, 0.35f), font, new Vector2(240f, 0f), new Vector2(160f, 40f), TextAnchor.MiddleCenter);
            CreateTextComp(userCard.transform, "68% (125 trận)", 15, FontStyle.Normal, new Color(0.85f, 0.95f, 1f), font, new Vector2(440f, 0f), new Vector2(160f, 40f), TextAnchor.MiddleCenter);

            // Quick Play Button
            CreateMenuActionButton(userCard.transform, "BtnPlayRanked", AegisLocalization.T("ĐẤU HẠNG ❯", "PLAY RANK ❯"), new Vector2(590f, 0f), new Vector2(150f, 44f), ColGoldCta, ColDarkBtnText, 14, () =>
            {
                CloseModal(_leaderboardModal);
                NavigateToChooseMode();
            }, font, ColGoldBorder, 1.2f);
        }

        private void BuildFriendsRankTable(Transform parent, Font font)
        {
            // Table Header Bar (Height 36px)
            GameObject thObj = new GameObject("TableHeader");
            thObj.transform.SetParent(parent, false);
            RectTransform thRt = thObj.AddComponent<RectTransform>();
            thRt.anchorMin = new Vector2(0.5f, 1f);
            thRt.anchorMax = new Vector2(0.5f, 1f);
            thRt.pivot = new Vector2(0.5f, 1f);
            thRt.anchoredPosition = new Vector2(0f, -5f);
            thRt.sizeDelta = new Vector2(1360f, 36f);

            Image thBg = thObj.AddComponent<Image>();
            thBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1360, 36, 6f, new Color(0.04f, 0.10f, 0.20f, 0.85f), ColCyanBorder, 1f);
            thBg.type = Image.Type.Sliced;

            CreateTextComp(thObj.transform, AegisLocalization.T("HẠNG BẠN BÈ", "FRIEND RANK"), 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(-580f, 0f), new Vector2(140f, 30f), TextAnchor.MiddleCenter);
            CreateTextComp(thObj.transform, AegisLocalization.T("TÊN BẠN BÈ", "FRIEND NAME"), 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(-360f, 0f), new Vector2(260f, 30f), TextAnchor.MiddleLeft);
            CreateTextComp(thObj.transform, AegisLocalization.T("BẬC RANK", "TIER"), 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(-40f, 0f), new Vector2(220f, 30f), TextAnchor.MiddleLeft);
            CreateTextComp(thObj.transform, AegisLocalization.T("ĐIỂM ELO", "ELO RATING"), 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(220f, 0f), new Vector2(160f, 30f), TextAnchor.MiddleCenter);
            CreateTextComp(thObj.transform, AegisLocalization.T("TƯƠNG TÁC", "ACTION"), 14, FontStyle.Bold, ColCyanGlow, font, new Vector2(520f, 0f), new Vector2(200f, 30f), TextAnchor.MiddleCenter);

            string[] friendNames = { "Mech_Titan", "Player001 (Bạn)", "Friend_Rookie", "Chess_Master22" };
            string[] friendTiers = { "BẠCH KIM III", "HUYỀN THOẠI III", "VÀNG I", "VÀNG III" };
            int[] friendElos = { 1690, 1200, 1120, 980 };
            bool[] isMe = { false, true, false, false };

            float startY = -52f;
            float rowHeight = 62f;
            float rowStep = 68f;

            for (int i = 0; i < 4; i++)
            {
                int fIdx = i;
                GameObject rowObj = new GameObject("FriendRow_" + (i + 1));
                rowObj.transform.SetParent(parent, false);
                RectTransform rRt = rowObj.AddComponent<RectTransform>();
                rRt.anchorMin = new Vector2(0.5f, 1f);
                rRt.anchorMax = new Vector2(0.5f, 1f);
                rRt.pivot = new Vector2(0.5f, 1f);
                rRt.anchoredPosition = new Vector2(0f, startY - i * rowStep);
                rRt.sizeDelta = new Vector2(1360f, rowHeight);

                Color rowBorder = isMe[i] ? ColCyanBorder : ColMutedBorder;
                Color rowFill = isMe[i] ? new Color(0.04f, 0.14f, 0.28f, 0.85f) : new Color(0.02f, 0.07f, 0.14f, 0.65f);

                Image rBg = rowObj.AddComponent<Image>();
                rBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1360, (int)rowHeight, 10f, rowFill, rowBorder, 1.2f);
                rBg.type = Image.Type.Sliced;

                CreateTextComp(rowObj.transform, $"#{i + 1}", 18, FontStyle.Bold, isMe[i] ? ColCyanGlow : ColAntiqueGold, font, new Vector2(-580f, 0f), new Vector2(140f, 36f), TextAnchor.MiddleCenter);
                CreateTextComp(rowObj.transform, friendNames[i], 18, FontStyle.Bold, Color.white, font, new Vector2(-360f, 0f), new Vector2(260f, 36f), TextAnchor.MiddleLeft);
                CreateTextComp(rowObj.transform, friendTiers[i], 16, FontStyle.Bold, isMe[i] ? ColAntiqueGold : ColCyanGlow, font, new Vector2(-40f, 0f), new Vector2(220f, 36f), TextAnchor.MiddleLeft);
                CreateTextComp(rowObj.transform, $"{friendElos[i]} Elo", 17, FontStyle.Bold, new Color(1f, 0.88f, 0.35f), font, new Vector2(220f, 0f), new Vector2(160f, 36f), TextAnchor.MiddleCenter);

                if (!isMe[i])
                {
                    CreateMenuActionButton(rowObj.transform, "BtnDuel_" + i, AegisLocalization.T("THÁCH ĐẤU ⚔️", "DUEL ⚔️"), new Vector2(520f, 0f), new Vector2(170f, 42f), ColGlassFill, ColCyanGlow, 14, () =>
                    {
                        CloseModal(_leaderboardModal);
                        ShowFriendModal();
                    }, font, ColCyanBorder, 1.2f);
                }
                else
                {
                    CreateTextComp(rowObj.transform, AegisLocalization.T("ĐANG ONLINE", "ONLINE"), 14, FontStyle.Bold, new Color(0.2f, 0.95f, 0.45f), font, new Vector2(520f, 0f), new Vector2(170f, 36f), TextAnchor.MiddleCenter);
                }
            }
        }

        private void BuildSeasonRewardsPanel(Transform parent, Font font)
        {
            float cardWidth = 320f;
            float cardHeight = 480f;
            float gap = 24f;
            float startX = -((4 * cardWidth + 3 * gap) / 2f) + cardWidth / 2f;

            string[] rewardTitles = {
                AegisLocalization.T("CHIẾN THẦN S1\n(Top 1 - 10)", "WAR GOD S1\n(Top 1 - 10)"),
                AegisLocalization.T("HUYỀN THOẠI\n(Elo 2,000+)", "LEGEND TIER\n(Elo 2,000+)"),
                AegisLocalization.T("KIM CƯƠNG\n(Elo 1,500+)", "DIAMOND TIER\n(Elo 1,500+)"),
                AegisLocalization.T("THAM GIA MÙA 1\n(≥ 10 Trận Đấu)", "PARTICIPANT S1\n(≥ 10 Matches)")
            };

            string[] rewardIcons = { "profile_rank_crest.png", "profile_rank_crest.png", "skin_cyber_future.png", "profile_rank_crest.png" };
            Color[] borderColors = { ColGoldBorder, new Color(0.85f, 0.90f, 0.98f), ColCyanBorder, ColMutedBorder };
            string[] rewardPrizes = {
                AegisLocalization.T("• Khung Avatar Rồng Thần\n• Danh hiệu [Chiến Thần S1]\n• 10,000 Vàng & 500 Kim Cương", "• Divine Dragon Frame\n• Title [War God S1]\n• 10,000 Gold & 500 Gems"),
                AegisLocalization.T("• Khung Avatar Cyber Mech\n• Danh hiệu [Huyền Thoại S1]\n• 5,000 Vàng", "• Cyber Mech Frame\n• Title [Legend S1]\n• 5,000 Gold"),
                AegisLocalization.T("• Skin Quân Cờ Arcane Paladin\n• 2,500 Vàng\n• 100 Vé Đấu Hạng", "• Arcane Paladin Piece Skin\n• 2,500 Gold\n• 100 Ranked Tickets"),
                AegisLocalization.T("• Huy Hiệu Kỷ Niệm S1\n• 1,000 Vàng\n• Thẻ Đổi Tên", "• S1 Commemorative Badge\n• 1,000 Gold\n• Name Change Card")
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject card = new GameObject("RewardCard_" + i);
                card.transform.SetParent(parent, false);
                RectTransform crt = card.AddComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f);
                crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(startX + i * (cardWidth + gap), 0f);
                crt.sizeDelta = new Vector2(cardWidth, cardHeight);

                Image bg = card.AddComponent<Image>();
                bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)cardWidth, (int)cardHeight, 16f, new Color(0.03f, 0.08f, 0.16f, 0.88f), borderColors[i], 1.8f);
                bg.type = Image.Type.Sliced;

                // Card Title
                CreateTextComp(card.transform, rewardTitles[i], 18, FontStyle.Bold, (i == 0) ? ColAntiqueGold : (i == 1) ? Color.white : ColCyanGlow, font, new Vector2(0f, 185f), new Vector2(290f, 54f), TextAnchor.MiddleCenter);

                // Reward Icon Image
                GameObject icObj = new GameObject("RewardIcon");
                icObj.transform.SetParent(card.transform, false);
                Image icImg = icObj.AddComponent<Image>();
                Sprite rSp = ArcaneAssetLoader.LoadArcaneSprite(rewardIcons[i]) ?? ArcaneAssetLoader.LoadArcaneSprite("dock_icon_rank.png");
                if (rSp != null) icImg.sprite = rSp;
                else icImg.color = borderColors[i];
                icImg.preserveAspect = true;
                RectTransform icRt = icObj.GetComponent<RectTransform>();
                icRt.anchorMin = new Vector2(0.5f, 0.5f);
                icRt.anchorMax = new Vector2(0.5f, 0.5f);
                icRt.pivot = new Vector2(0.5f, 0.5f);
                icRt.anchoredPosition = new Vector2(0f, 75f);
                icRt.sizeDelta = new Vector2(100f, 100f);

                // Divider Line
                GameObject divObj = new GameObject("DivLine");
                divObj.transform.SetParent(card.transform, false);
                Image divImg = divObj.AddComponent<Image>();
                divImg.color = new Color(borderColors[i].r, borderColors[i].g, borderColors[i].b, 0.4f);
                RectTransform divRt = divObj.GetComponent<RectTransform>();
                divRt.anchoredPosition = new Vector2(0f, 5f);
                divRt.sizeDelta = new Vector2(260f, 1.5f);

                // Prize text list
                CreateTextComp(card.transform, rewardPrizes[i], 14, FontStyle.Normal, new Color(0.85f, 0.92f, 1f), font, new Vector2(0f, -80f), new Vector2(280f, 140f), TextAnchor.UpperLeft);

                // Claim status button
                string statusTxt = (i == 3) ? AegisLocalization.T("ĐÃ ĐẠT ĐIỀU KIỆN", "QUALIFIED") : AegisLocalization.T("ĐANG TRANH TÀI", "IN PROGRESS");
                Color statusFill = (i == 3) ? new Color(0.1f, 0.45f, 0.25f, 0.85f) : new Color(0.04f, 0.12f, 0.24f, 0.70f);
                CreateMenuActionButton(card.transform, "BtnRewardStatus_" + i, statusTxt, new Vector2(0f, -195f), new Vector2(260f, 40f), statusFill, Color.white, 13, () =>
                {
                    ShowToast(AegisLocalization.T("Phần thưởng sẽ tự động gửi qua Thư khi mùa giải kết thúc!", "Rewards will be delivered via Mail when season ends!"), ToastType.Info);
                }, font, borderColors[i], 1.2f);
            }
        }

        public void UpdateUserProfileUI()
        {
            var profile = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentProfile : null;
            string username = (UserAccountManager.Instance != null && !string.IsNullOrEmpty(UserAccountManager.Instance.CurrentUsername))
                ? UserAccountManager.Instance.CurrentUsername
                : "Player001";
            int elo = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentElo : PlayerPrefs.GetInt("MECH_USER_ELO", 400);
            int level = profile != null ? profile.level : (UserAccountManager.Instance != null ? UserAccountManager.Instance.CurrentLevel : PlayerPrefs.GetInt("MECH_USER_LEVEL", 1));
            int exp = profile != null ? profile.exp : (UserAccountManager.Instance != null ? UserAccountManager.Instance.CurrentExp : PlayerPrefs.GetInt("MECH_USER_EXP", 0));
            int maxExp = profile != null ? Mathf.Max(100, profile.max_exp) : (UserAccountManager.Instance != null ? UserAccountManager.Instance.CurrentMaxExp : PlayerPrefs.GetInt("MECH_USER_MAX_EXP", 100));

            int currentGold = (UserAccountManager.Instance != null) ? UserAccountManager.Instance.CurrentGold : _currentGold;
            _currentGold = currentGold;
            if (_shopGoldTxt != null) _shopGoldTxt.text = currentGold.ToString();
            if (_collectionGoldTxt != null) _collectionGoldTxt.text = currentGold.ToString();

            if (_profileNameTxt != null) _profileNameTxt.text = username;
            if (_profileLevelTxt != null) _profileLevelTxt.text = $"Lv. {level} • Elo {elo}";

            if (_profileModalNameComp != null) _profileModalNameComp.text = username;
            if (_profileModalLevelComp != null) _profileModalLevelComp.text = $"Lv. {level}";
            if (_profileModalExpComp != null) _profileModalExpComp.text = $"{exp} / {maxExp}";
            if (_profileModalExpFillRt != null)
            {
                float ratio = Mathf.Clamp01((float)exp / maxExp);
                _profileModalExpFillRt.sizeDelta = new Vector2(180f * ratio, 16f);
            }

            int pvpWins = UserAccountManager.Instance != null ? UserAccountManager.Instance.PvpWins : 0;
            int pvpLosses = UserAccountManager.Instance != null ? UserAccountManager.Instance.PvpLosses : 0;
            int pvpDraws = UserAccountManager.Instance != null ? UserAccountManager.Instance.PvpDraws : 0;

            int aiWins = UserAccountManager.Instance != null ? UserAccountManager.Instance.AiWins : 0;
            int aiLosses = UserAccountManager.Instance != null ? UserAccountManager.Instance.AiLosses : 0;
            int aiDraws = UserAccountManager.Instance != null ? UserAccountManager.Instance.AiDraws : 0;

            // Fallback sync if account has wins/losses from profile
            if (profile != null && (pvpWins + aiWins == 0) && profile.wins > 0)
            {
                aiWins = profile.wins;
            }
            if (profile != null && (pvpLosses + aiLosses == 0) && profile.losses > 0)
            {
                pvpLosses = profile.losses;
            }

            // Row 1 (Online PvP)
            if (_profileModalEloComp != null) _profileModalEloComp.text = elo.ToString();
            if (_profileModalPvpWinsComp != null) _profileModalPvpWinsComp.text = pvpWins.ToString();
            if (_profileModalPvpLossesComp != null) _profileModalPvpLossesComp.text = pvpLosses.ToString();
            if (_profileModalPvpDrawsComp != null) _profileModalPvpDrawsComp.text = pvpDraws.ToString();

            // Row 2 (Vs AI)
            if (_profileModalAiWinsComp != null) _profileModalAiWinsComp.text = aiWins.ToString();
            if (_profileModalAiLossesComp != null) _profileModalAiLossesComp.text = aiLosses.ToString();
            if (_profileModalAiDrawsComp != null) _profileModalAiDrawsComp.text = aiDraws.ToString();

            // Compatibility
            if (_profileModalWinsComp != null) _profileModalWinsComp.text = pvpWins.ToString();
            if (_profileModalLossesComp != null) _profileModalLossesComp.text = pvpLosses.ToString();
            if (_profileModalDrawsComp != null) _profileModalDrawsComp.text = pvpDraws.ToString();

            // Dynamic match history refresh
            LoadDynamicMatchHistory();
        }

        public void LoadDynamicMatchHistory()
        {
            if (_matchHistoryRowsParent == null) return;

            // Remove existing dynamic rows or notices (leave RowDiv_ intact)
            for (int c = _matchHistoryRowsParent.childCount - 1; c >= 0; c--)
            {
                Transform ch = _matchHistoryRowsParent.GetChild(c);
                if (ch != null && (ch.name.StartsWith("DynamicRow_") || ch.name.StartsWith("EmptyNotice")))
                {
                    Destroy(ch.gameObject);
                }
            }

            Font font = GetDefaultFont();
            List<MatchHistoryItem> localMatches = null;
            if (UserAccountManager.Instance != null)
            {
                localMatches = UserAccountManager.Instance.GetLocalMatchHistory();
            }

            if (localMatches == null || localMatches.Count == 0)
            {
                GameObject emptyNotice = new GameObject("EmptyNotice", typeof(RectTransform));
                emptyNotice.transform.SetParent(_matchHistoryRowsParent, false);
                RectTransform enRt = emptyNotice.GetComponent<RectTransform>();
                enRt.anchorMin = new Vector2(0.5f, 0.5f);
                enRt.anchorMax = new Vector2(0.5f, 0.5f);
                enRt.pivot = new Vector2(0.5f, 0.5f);
                enRt.anchoredPosition = new Vector2(0f, 0f);
                enRt.sizeDelta = new Vector2(500f, 160f);

                CreateTextComp(emptyNotice.transform, AegisLocalization.T("Chưa có ván đấu nào được ghi nhận.", "No match history recorded."), 20, FontStyle.Bold, new Color(0.85f, 0.90f, 0.98f), font, new Vector2(0f, 20f), new Vector2(460f, 32f));
                CreateTextComp(emptyNotice.transform, AegisLocalization.T("Hãy hoàn thành ván đấu để bắt đầu ghi danh sử sách!", "Complete matches to record your battle chronicles!"), 16, FontStyle.Italic, new Color(0.60f, 0.75f, 0.90f, 0.85f), font, new Vector2(0f, -20f), new Vector2(460f, 32f));
            }
            else
            {
                float startY = 85f;
                float rowSpacing = 95f;
                int count = Mathf.Min(localMatches.Count, 3);
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        var m = localMatches[i];
                        if (m == null) continue;

                        GameObject rowObj = new GameObject("DynamicRow_" + i, typeof(RectTransform));
                        rowObj.transform.SetParent(_matchHistoryRowsParent, false);
                        RectTransform rRt = rowObj.GetComponent<RectTransform>();
                        rRt.anchorMin = new Vector2(0.5f, 0.5f);
                        rRt.anchorMax = new Vector2(0.5f, 0.5f);
                        rRt.pivot = new Vector2(0.5f, 0.5f);
                        rRt.anchoredPosition = new Vector2(0f, startY - (i * rowSpacing));
                        rRt.sizeDelta = new Vector2(570f, 75f);

                        // Row Icon
                        GameObject rIconObj = new GameObject("Icon", typeof(RectTransform));
                        rIconObj.transform.SetParent(rowObj.transform, false);
                        Image rIconImg = rIconObj.AddComponent<Image>();
                        string iconName = m.is_ai ? "profile_vs_ai_icon.png" : "profile_vs_player_icon.png";
                        Sprite icSp = ArcaneAssetLoader.LoadArcaneSprite(iconName);
                        if (icSp != null) rIconImg.sprite = icSp;
                        else rIconImg.color = ColCyanGlow;
                        rIconImg.preserveAspect = true;
                        RectTransform riRt = rIconObj.GetComponent<RectTransform>();
                        riRt.anchorMin = new Vector2(0.5f, 0.5f);
                        riRt.anchorMax = new Vector2(0.5f, 0.5f);
                        riRt.pivot = new Vector2(0.5f, 0.5f);
                        riRt.anchoredPosition = new Vector2(-235f, 0f);
                        riRt.sizeDelta = new Vector2(46f, 46f);

                        // Opponent Name
                        string oppTitle = !string.IsNullOrEmpty(m.opponent_name) ? m.opponent_name : (m.is_ai ? "Cyber AI" : "Online Player");
                        CreateTextComp(rowObj.transform, oppTitle, 20, FontStyle.Bold, ColTextWhite, font, new Vector2(-75f, 12f), new Vector2(250f, 26f), TextAnchor.MiddleLeft);

                        // Real Date & Time
                        string endedAtStr = !string.IsNullOrEmpty(m.ended_at) ? m.ended_at : DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                        CreateTextComp(rowObj.transform, endedAtStr, 15, FontStyle.Normal, new Color(0.60f, 0.75f, 0.90f), font, new Vector2(-75f, -14f), new Vector2(250f, 22f), TextAnchor.MiddleLeft);

                        // Score + Result Color
                        string scoreStr = !string.IsNullOrEmpty(m.score) ? m.score : (m.result == "win" ? "1 - 0" : (m.result == "loss" ? "0 - 1" : "½ - ½"));
                        Color scoreCol = (m.result == "win") ? ColGoldBorder : ((m.result == "loss") ? new Color(0.95f, 0.4f, 0.4f) : new Color(0.8f, 0.85f, 0.9f));
                        CreateTextComp(rowObj.transform, scoreStr, 26, FontStyle.Bold, scoreCol, font, new Vector2(105f, 0f), new Vector2(90f, 36f), TextAnchor.MiddleCenter);

                        // Elo Change (+12 / -8)
                        string sign = m.elo_change > 0 ? "+" : "";
                        string eloStr = $"{sign}{m.elo_change}";
                        Color eloCol = m.elo_change > 0 ? new Color(0f, 0.90f, 0.45f) : (m.elo_change < 0 ? new Color(1f, 0.35f, 0.35f) : new Color(0.6f, 0.8f, 1f));
                        CreateTextComp(rowObj.transform, $"Elo {eloStr}", 16, FontStyle.Bold, eloCol, font, new Vector2(185f, 0f), new Vector2(80f, 30f), TextAnchor.MiddleCenter);

                        // Review button [🔍]
                        var curM = m;
                        CreateMenuActionButton(rowObj.transform, "BtnReview", "🔍", new Vector2(250f, 0f), new Vector2(42f, 36f), ColGlassFill, ColCyanGlow, 18, () =>
                        {
                            OpenGameReview(curM);
                        }, font, ColCyanBorder, 1.2f);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Profile] Error rendering match row {i}: {ex.Message}");
                    }
                }
            }

            // Also check for remote match updates in background
            if (UserAccountManager.Instance != null && UserAccountManager.Instance.IsLoggedIn)
            {
                UserAccountManager.Instance.FetchMatchHistory(5, (success, remoteMatches) =>
                {
                    if (success && remoteMatches != null && remoteMatches.Length > 0)
                    {
                        LoadDynamicMatchHistory();
                    }
                });
            }
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

        private static void MakeTextCrisp(Text t, Color? outlineCol = null)
        {
            if (t == null) return;
            if (t.fontSize <= 15)
            {
                var existingOutline = t.gameObject.GetComponent<Outline>();
                if (existingOutline != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(existingOutline);
                    else UnityEngine.Object.DestroyImmediate(existingOutline);
                }

                var shadow = t.gameObject.GetComponent<Shadow>() ?? t.gameObject.AddComponent<Shadow>();
                shadow.effectColor = outlineCol ?? new Color(0.01f, 0.02f, 0.05f, 0.70f);
                shadow.effectDistance = new Vector2(1f, -1f);
            }
            else
            {
                var outline = t.gameObject.GetComponent<Outline>() ?? t.gameObject.AddComponent<Outline>();
                outline.effectColor = outlineCol ?? new Color(0.01f, 0.02f, 0.06f, 0.85f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
        }

        private Text CreateTextComp(Transform parent, string content, int size, FontStyle style, Color color, Font font, Vector2 pos, Vector2 delta, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            string safeContent = content ?? "";
            GameObject txtObj = new GameObject("Txt_" + safeContent.GetHashCode(), typeof(RectTransform));
            txtObj.transform.SetParent(parent, false);
            Text t = txtObj.AddComponent<Text>();
            t.text = safeContent;
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = delta;
            MakeTextCrisp(t);
            return t;
        }

        private GameObject CreateMenuActionButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color fill, Color textCol, int fontSize, Action onClick, Font font, Color? border = null, float borderWidth = 1.2f)
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
        return btnObj;
        }

        private InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 pos, Vector2 size, Font font, bool isPassword = false)
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
            if (isPassword)
            {
                inputField.contentType = InputField.ContentType.Password;
                inputField.inputType = InputField.InputType.Password;
            }
            inputField.selectionColor = new Color(0.0f, 0.82f, 1.0f, 0.4f);

            return inputField;
        }

#if UNITY_EDITOR
        [ContextMenu("▶ Hiện toàn bộ UI lên Scene (Edit Mode)")]
        public void EditorBuildUIInScene()
        {
            var oldCanvas = transform.Find("ArcaneMenuCanvas");
            if (oldCanvas != null) DestroyImmediate(oldCanvas.gameObject);

            MedievalUIAssetLoader.ClearCache();
            AegisLocalization.LoadSavedLanguage();
            BuildAllScreens(false);

            if (_screenSplash != null) _screenSplash.SetActive(false);
            if (_screenMainMenu != null) _screenMainMenu.SetActive(true);

            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#00FF99>[Aegis UI Builder] Đã dựng toàn bộ giao diện MainMenu2D lên Scene thành công!</color>");
        }

        [ContextMenu("✕ Xóa toàn bộ UI khỏi Scene")]
        public void EditorClearUI()
        {
            var oldCanvas = transform.Find("ArcaneMenuCanvas");
            if (oldCanvas != null)
            {
                DestroyImmediate(oldCanvas.gameObject);
                UnityEditor.EditorUtility.SetDirty(gameObject);
                Debug.Log("<color=#FF9900>[Aegis UI Builder] Đã xóa toàn bộ UI Canvas khỏi Scene.</color>");
            }
        }
#endif

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Tahoma" }, 16);
            return font;
        }
    
                private GameObject CreateRoyalCloseButton(Transform parent, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClickAction, float size = 66f, string name = "BtnClose", Vector2? anchor = null, Vector2? pivot = null)
        {
            GameObject closeBtnObj = new GameObject(name);
            closeBtnObj.transform.SetParent(parent, false);
            closeBtnObj.transform.SetAsLastSibling();

            Image cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.sprite = MedievalUIAssetLoader.GetSprite("btn_royal_close_swords")
                          ?? MedievalUIAssetLoader.GetSprite("icon_crossed_swords_x")
                          ?? MedievalUIAssetLoader.GetIcon(MedievalIcon.Close);
            cbImg.type = Image.Type.Simple;
            cbImg.preserveAspect = true;
            cbImg.color = Color.white;

            Button cbBtn = closeBtnObj.AddComponent<Button>();
            cbBtn.targetGraphic = cbImg;
            cbBtn.onClick.AddListener(() =>
            {
                AegisAudioManager.PlayButtonClick();
                onClickAction?.Invoke();
            });
            closeBtnObj.AddComponent<MenuButtonEffect>();

            Vector2 a = anchor.HasValue ? anchor.Value : new Vector2(1f, 1f);
            Vector2 p = pivot.HasValue ? pivot.Value : new Vector2(0.5f, 0.5f);

            RectTransform cbRt = closeBtnObj.GetComponent<RectTransform>();
            cbRt.anchorMin = a;
            cbRt.anchorMax = a;
            cbRt.pivot = p;
            cbRt.anchoredPosition = anchoredPos;
            cbRt.sizeDelta = new Vector2(size, size);
            return closeBtnObj;
        }

        

        

        private void OpenMailboxModal()
        {
            if (_mailboxModal != null)
            {
                CloseAllModals();
                _mailboxModal.SetActive(true);
                _mailboxModal.transform.SetAsLastSibling();
                AegisAudioManager.PlayModalOpen();
            }
        }

    }
}
