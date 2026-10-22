using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using MechChess.UI;

namespace MechChess.Combat2D
{
    /// <summary>
    /// Holographic Sci-Fi 2D "Armor Hack" Minigame using Rigidbody2D and Collider2D.
    /// Triggered whenever a Mech piece captures an enemy piece, or tested via Tactical Drawer.
    /// Player guides a Quantum Electrical Impulse through moving laser firewall gates to the Core Reactor.
    /// </summary>
    public class CombatMinigame2D : MonoBehaviour
    {
        public static CombatMinigame2D Instance { get; private set; }

        [Header("Minigame State")]
        private bool _isActive = false;
        private float _timeRemaining = 5.0f;
        private const float TOTAL_TIME = 5.0f;
        private Action<bool> _onCompleteCallback;

        // Visual Theme Palette (Sci-Fi Cyberpunk Arcane)
        private readonly Color ColBackdrop = new Color(0.015f, 0.035f, 0.08f, 0.88f);
        private readonly Color ColCardFill = new Color(0.02f, 0.05f, 0.12f, 0.96f);
        private readonly Color ColCyanBorder = new Color(0.0f, 0.85f, 1.0f, 0.92f);
        private readonly Color ColCyanGlow = new Color(0.0f, 0.95f, 1.0f, 1.0f);
        private readonly Color ColGoldAccent = new Color(0.98f, 0.75f, 0.12f, 1.0f);
        private readonly Color ColCrimsonDanger = new Color(1.0f, 0.22f, 0.28f, 0.95f);
        private readonly Color ColEmeraldTarget = new Color(0.0f, 1.0f, 0.55f, 0.95f);
        private readonly Color ColTextMuted = new Color(0.60f, 0.72f, 0.85f, 0.90f);

        // UI & Physics GameObjects
        private GameObject _canvasRoot;
        private CanvasScaler _scaler;
        private GameObject _terminalCardObj;
        private Text _titleText;
        private Text _subtitleText;
        private Text _timerText;
        private Image _timerFillImg;
        private RectTransform _timerFillRt;
        private Text _statusText;
        private Text _controlsHintText;

        // Arena & Gameplay Elements
        private GameObject _arenaBounds;
        private RectTransform _arenaRt;
        private GameObject _impulsePlayer;
        private Rigidbody2D _playerRb;
        private GameObject _targetNode;
        private RectTransform _targetNodeRt;
        private GameObject _targetShieldRing;
        private GameObject _deployPadObj;

        // Mobile / Touch Virtual Direction Input
        private Vector2 _virtualDirection = Vector2.zero;

        // Ghost Sparks Trail Effect
        private readonly List<GameObject> _trailDots = new List<GameObject>();
        private readonly List<Image> _trailDotImgs = new List<Image>();
        private float _trailTimer = 0f;
        private int _trailIndex = 0;
        private const int MAX_TRAIL_DOTS = 12;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            try
            {
                BuildHolographicTerminalUI();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CombatMinigame2D] Error building UI: {ex}");
            }
            finally
            {
                if (_canvasRoot != null) _canvasRoot.SetActive(false);
            }
        }

        private void Start()
        {
            if (_canvasRoot != null)
            {
                _canvasRoot.SetActive(false);
            }
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Tahoma" }, 16);
            return font;
        }

        private void BuildHolographicTerminalUI()
        {
            Font font = GetDefaultFont();

            // 1. Canvas Root
            _canvasRoot = new GameObject("Minigame2D_Canvas");
            _canvasRoot.transform.SetParent(transform, false);
            _canvasRoot.SetActive(false);

            Canvas canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Topmost layer over 3D board and game HUD

            _scaler = _canvasRoot.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;

            _canvasRoot.AddComponent<GraphicRaycaster>();

            // 2. Ambient Darkened Glass Backdrop
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(_canvasRoot.transform, false);
            Image bgImg = backdrop.AddComponent<Image>();
            bgImg.color = ColBackdrop;
            bgImg.raycastTarget = true;
            RectTransform bgRt = backdrop.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // 3. Central Holographic Terminal Window Card (1060 x 700)
            _terminalCardObj = new GameObject("TerminalCard");
            _terminalCardObj.transform.SetParent(backdrop.transform, false);
            RectTransform tcRt = _terminalCardObj.AddComponent<RectTransform>();
            tcRt.anchorMin = new Vector2(0.5f, 0.5f);
            tcRt.anchorMax = new Vector2(0.5f, 0.5f);
            tcRt.pivot = new Vector2(0.5f, 0.5f);
            tcRt.anchoredPosition = Vector2.zero;
            tcRt.sizeDelta = new Vector2(1060f, 700f);

            // Card Background: Sliced Glass Box with Cyan Border
            Image tcBg = _terminalCardObj.AddComponent<Image>();
            tcBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(1060, 700, 24f, ColCardFill, ColCyanBorder, 2.2f);
            tcBg.type = Image.Type.Sliced;

            // Card PCB Circuit Texture Overlay (Subtle holographic glow)
            GameObject pcbOverlay = new GameObject("PcbOverlay");
            pcbOverlay.transform.SetParent(_terminalCardObj.transform, false);
            RectTransform pcbRt = pcbOverlay.AddComponent<RectTransform>();
            pcbRt.anchorMin = Vector2.zero;
            pcbRt.anchorMax = Vector2.one;
            pcbRt.offsetMin = Vector2.zero;
            pcbRt.offsetMax = Vector2.zero;
            Image pcbImg = pcbOverlay.AddComponent<Image>();
            Sprite pcbSprite = ArcaneAssetLoader.LoadArcaneSprite("cyber_hack_arena_bg.png");
            if (pcbSprite != null)
            {
                pcbImg.sprite = pcbSprite;
                pcbImg.type = Image.Type.Simple;
                pcbImg.color = new Color(0.0f, 0.85f, 1.0f, 0.28f); // Soft cyan circuit glow
                pcbImg.preserveAspect = false;
            }
            else
            {
                pcbImg.color = Color.clear;
            }
            pcbImg.raycastTarget = false;

            // Corner Cyber Accents (◤ ◥ ◣ ◢)
            CreateCornerBracket(_terminalCardObj.transform, "◤ 01-OVERRIDE", new Vector2(24f, -20f), new Vector2(0f, 1f), font);
            CreateCornerBracket(_terminalCardObj.transform, "FREQ-99.8GHz ◥", new Vector2(-24f, -20f), new Vector2(1f, 1f), font);
            CreateCornerBracket(_terminalCardObj.transform, "◣ MATRIX-SYS", new Vector2(24f, 20f), new Vector2(0f, 0f), font);
            CreateCornerBracket(_terminalCardObj.transform, "TACTICAL-v2 ◢", new Vector2(-24f, 20f), new Vector2(1f, 0f), font);

            // 4. Header Bar (Title + Subtitle + Close Button)
            GameObject headerObj = new GameObject("HeaderBar");
            headerObj.transform.SetParent(_terminalCardObj.transform, false);
            RectTransform hRt = headerObj.AddComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 1f);
            hRt.anchorMax = new Vector2(0.5f, 1f);
            hRt.pivot = new Vector2(0.5f, 1f);
            hRt.anchoredPosition = new Vector2(0f, -16f);
            hRt.sizeDelta = new Vector2(980f, 64f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);
            _titleText = titleObj.AddComponent<Text>();
            _titleText.font = font;
            _titleText.fontSize = 23;
            _titleText.fontStyle = FontStyle.Bold;
            _titleText.alignment = TextAnchor.MiddleCenter;
            _titleText.color = ColCyanGlow;
            _titleText.text = AegisLocalization.T("⚡ ĐỘT NHẬP TÁC CHIẾN: HACK TƯỜNG LỬA GIÁP", "⚡ TACTICAL OVERRIDE: BREACH ENEMY ARMOR CIRCUIT");
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, 10f);
            tRt.sizeDelta = new Vector2(850f, 32f);

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(headerObj.transform, false);
            _subtitleText = subObj.AddComponent<Text>();
            _subtitleText.font = font;
            _subtitleText.fontSize = 13;
            _subtitleText.alignment = TextAnchor.MiddleCenter;
            _subtitleText.color = ColTextMuted;
            _subtitleText.text = AegisLocalization.T("/// GIAO THỨC NEURAL XUNG ĐIỆN: DẪN XUNG ĐẾN LÕI GIÁP ĐỐI PHƯƠNG ///", "/// NEURAL IMPULSE PROTOCOL: GUIDE IMPULSE INTO ENEMY CORE ///");
            RectTransform subRt = subObj.GetComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0f, -16f);
            subRt.sizeDelta = new Vector2(850f, 24f);

            // Close / Abort Button [✕] at Top-Right
            GameObject closeBtnObj = new GameObject("BtnAbort");
            closeBtnObj.transform.SetParent(_terminalCardObj.transform, false);
            RectTransform cbRt = closeBtnObj.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 1f);
            cbRt.anchorMax = new Vector2(1f, 1f);
            cbRt.pivot = new Vector2(1f, 1f);
            cbRt.anchoredPosition = new Vector2(-20f, -18f);
            cbRt.sizeDelta = new Vector2(40f, 40f);

            Image cbImg = closeBtnObj.AddComponent<Image>();
            cbImg.sprite = ArcaneAssetLoader.GetCircleSprite(40, new Color(0.12f, 0.04f, 0.06f, 0.90f), ColCrimsonDanger, 1.5f);
            Button cbBtn = closeBtnObj.AddComponent<Button>();
            cbBtn.onClick.AddListener(AbortMinigame);
            closeBtnObj.AddComponent<MenuButtonEffect>();

            GameObject cbTxtObj = new GameObject("Txt");
            cbTxtObj.transform.SetParent(closeBtnObj.transform, false);
            Text cbTxt = cbTxtObj.AddComponent<Text>();
            cbTxt.font = font;
            cbTxt.fontSize = 20;
            cbTxt.fontStyle = FontStyle.Bold;
            cbTxt.alignment = TextAnchor.MiddleCenter;
            cbTxt.color = new Color(1f, 0.45f, 0.45f);
            cbTxt.text = "✕";
            RectTransform cbtRt = cbTxtObj.GetComponent<RectTransform>();
            cbtRt.anchorMin = Vector2.zero;
            cbtRt.anchorMax = Vector2.one;
            cbtRt.offsetMin = Vector2.zero;
            cbtRt.offsetMax = Vector2.zero;

            // 5. High-Tech Segmented Energy Breach Timer Bar (Width: 680, Height: 24)
            GameObject timerBarPod = new GameObject("TimerBarPod");
            timerBarPod.transform.SetParent(_terminalCardObj.transform, false);
            RectTransform tbpRt = timerBarPod.AddComponent<RectTransform>();
            tbpRt.anchorMin = new Vector2(0.5f, 1f);
            tbpRt.anchorMax = new Vector2(0.5f, 1f);
            tbpRt.pivot = new Vector2(0.5f, 1f);
            tbpRt.anchoredPosition = new Vector2(0f, -88f);
            tbpRt.sizeDelta = new Vector2(680f, 26f);

            // Timer Bar Capsule Background
            Image tbpBg = timerBarPod.AddComponent<Image>();
            tbpBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(680, 26, 13f, new Color(0.01f, 0.03f, 0.07f, 0.92f), new Color(0.12f, 0.28f, 0.45f, 0.8f), 1.2f);
            tbpBg.type = Image.Type.Sliced;

            // Fill Bar
            GameObject fillObj = new GameObject("TimerFill");
            fillObj.transform.SetParent(timerBarPod.transform, false);
            _timerFillRt = fillObj.AddComponent<RectTransform>();
            _timerFillRt.anchorMin = new Vector2(0f, 0.5f);
            _timerFillRt.anchorMax = new Vector2(0f, 0.5f);
            _timerFillRt.pivot = new Vector2(0f, 0.5f);
            _timerFillRt.anchoredPosition = new Vector2(3f, 0f);
            _timerFillRt.sizeDelta = new Vector2(674f, 20f);

            _timerFillImg = fillObj.AddComponent<Image>();
            _timerFillImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(674, 20, 10f, ColCyanGlow, Color.clear, 0f);
            _timerFillImg.type = Image.Type.Sliced;
            _timerFillImg.color = ColCyanGlow;

            // Timer Text Overlay
            GameObject timerTxtObj = new GameObject("TimerText");
            timerTxtObj.transform.SetParent(timerBarPod.transform, false);
            _timerText = timerTxtObj.AddComponent<Text>();
            _timerText.font = font;
            _timerText.fontSize = 14;
            _timerText.fontStyle = FontStyle.Bold;
            _timerText.alignment = TextAnchor.MiddleCenter;
            _timerText.color = Color.white;
            _timerText.text = "⏱ CIRCUIT BREACH WINDOW: 5.00s";
            RectTransform ttRt = timerTxtObj.GetComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.offsetMin = Vector2.zero;
            ttRt.offsetMax = Vector2.zero;

            // 6. Physics 2D Circuit Arena Frame
            _arenaBounds = new GameObject("Arena2D_PhysicsRoot", typeof(RectTransform));
            _arenaBounds.transform.SetParent(_terminalCardObj.transform, false);
            _arenaRt = _arenaBounds.GetComponent<RectTransform>();
            _arenaRt.sizeDelta = new Vector2(760f, 410f);
            _arenaRt.anchoredPosition = new Vector2(0f, 15f);

            // Arena Background Glass Plate
            Image arenaBg = _arenaBounds.AddComponent<Image>();
            arenaBg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(760, 410, 18f, new Color(0.015f, 0.04f, 0.09f, 0.85f), new Color(0.0f, 0.65f, 0.90f, 0.65f), 1.5f);
            arenaBg.type = Image.Type.Sliced;

            // Arena Horizontal Tech Pathway Guide Lines
            CreateArenaGuideLines(_arenaBounds.transform);

            // Build Physics Walls, Gates, Emitter, Target, Player
            CreatePhysicsArena();

            // 7. Status Banner & Bottom Telemetry
            GameObject statusObj = new GameObject("StatusBanner");
            statusObj.transform.SetParent(_terminalCardObj.transform, false);
            RectTransform sbRt = statusObj.AddComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.5f, 0f);
            sbRt.anchorMax = new Vector2(0.5f, 0f);
            sbRt.pivot = new Vector2(0.5f, 0f);
            sbRt.anchoredPosition = new Vector2(0f, 65f);
            sbRt.sizeDelta = new Vector2(940f, 34f);

            _statusText = statusObj.AddComponent<Text>();
            _statusText.font = font;
            _statusText.fontSize = 16;
            _statusText.fontStyle = FontStyle.Bold;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = Color.white;
            _statusText.text = AegisLocalization.T("Vượt qua tường lửa vi mạch trong vòng 5 giây!", "Bypass circuit firewall within 5 seconds!");

            // 8. Instructions & Virtual D-Pad Touch Bar
            BuildControlsAndInstructionsPod(_terminalCardObj.transform, font);

            // Initially Inactive
            _canvasRoot.SetActive(false);
        }

        private void CreateCornerBracket(Transform parent, string text, Vector2 pos, Vector2 anchor, Font font)
        {
            GameObject obj = new GameObject("Bracket_" + text);
            obj.transform.SetParent(parent, false);
            Text t = obj.AddComponent<Text>();
            t.text = text;
            t.font = font;
            t.fontSize = 11;
            t.fontStyle = FontStyle.Bold;
            t.color = new Color(0.0f, 0.85f, 1.0f, 0.60f);
            t.alignment = (anchor.x == 0f) ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(160f, 22f);
        }

        private void CreateArenaGuideLines(Transform arena)
        {
            float[] ys = { -100f, 0f, 100f };
            for (int i = 0; i < ys.Length; i++)
            {
                GameObject line = new GameObject("GuideLine_" + i);
                line.transform.SetParent(arena, false);
                Image img = line.AddComponent<Image>();
                img.color = new Color(0.0f, 0.75f, 1.0f, (i == 1) ? 0.22f : 0.12f);
                RectTransform rt = line.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, ys[i]);
                rt.sizeDelta = new Vector2(-40f, 1.5f);
                img.raycastTarget = false;
            }
        }

        private void CreatePhysicsArena()
        {
            float width = 760f;
            float height = 410f;
            Font font = GetDefaultFont();

            // 1. 4 Solid Glowing Boundary Rails with BoxCollider2D
            CreateWall("WallTop", new Vector2(0, height / 2f - 6f), new Vector2(width - 20f, 12f), new Color(0.0f, 0.85f, 1.0f, 0.75f));
            CreateWall("WallBottom", new Vector2(0, -height / 2f + 6f), new Vector2(width - 20f, 12f), new Color(0.0f, 0.85f, 1.0f, 0.75f));
            CreateWall("WallLeft", new Vector2(-width / 2f + 6f, 0), new Vector2(12f, height - 20f), new Color(0.0f, 0.85f, 1.0f, 0.75f));
            CreateWall("WallRight", new Vector2(width / 2f - 6f, 0), new Vector2(12f, height - 20f), new Color(0.0f, 0.85f, 1.0f, 0.75f));

            // 2. Start Deploy Pad (x = -280, y = 0)
            _deployPadObj = new GameObject("DeployPad");
            _deployPadObj.transform.SetParent(_arenaBounds.transform, false);
            RectTransform dpRt = _deployPadObj.AddComponent<RectTransform>();
            dpRt.anchoredPosition = new Vector2(-280f, 0f);
            dpRt.sizeDelta = new Vector2(68f, 68f);

            Image dpImg = _deployPadObj.AddComponent<Image>();
            dpImg.sprite = ArcaneAssetLoader.GetCircleSprite(68, new Color(0.02f, 0.10f, 0.22f, 0.65f), new Color(0.0f, 0.85f, 1.0f, 0.75f), 1.5f);
            dpImg.raycastTarget = false;

            // Deploy Label below
            GameObject dplObj = new GameObject("DeployLabel");
            dplObj.transform.SetParent(_deployPadObj.transform, false);
            Text dpl = dplObj.AddComponent<Text>();
            dpl.text = AegisLocalization.T("[ KHỞI TẠO ]", "[ DEPLOY ]");
            dpl.font = font;
            dpl.fontSize = 11;
            dpl.fontStyle = FontStyle.Bold;
            dpl.alignment = TextAnchor.MiddleCenter;
            dpl.color = new Color(0.4f, 0.85f, 1.0f, 0.85f);
            RectTransform dplr = dplObj.GetComponent<RectTransform>();
            dplr.anchoredPosition = new Vector2(0f, -48f);
            dplr.sizeDelta = new Vector2(100f, 20f);

            // 3. Moving Laser Firewall Gates (Obstacles)
            // Gate 1: x = -130, moves vertically
            CreateLaserObstacle("LaserGate1", new Vector2(-130f, 0f), new Vector2(32f, 235f), 1.0f, 0f);
            // Gate 2: x = +125, moves vertically with phase offset
            CreateLaserObstacle("LaserGate2", new Vector2(125f, 0f), new Vector2(32f, 235f), 1.15f, Mathf.PI * 0.75f);

            // 4. Target Quantum Core Reactor (x = +275, y = 0)
            _targetNode = new GameObject("TargetCoreNode");
            _targetNode.transform.SetParent(_arenaBounds.transform, false);
            _targetNodeRt = _targetNode.AddComponent<RectTransform>();
            _targetNodeRt.sizeDelta = new Vector2(86f, 86f);
            _targetNodeRt.anchoredPosition = new Vector2(275f, 0f);

            // Reactor Core Image
            Image nodeImg = _targetNode.AddComponent<Image>();
            Sprite reactorSprite = ArcaneAssetLoader.LoadArcaneSprite("cyber_core_reactor.png");
            if (reactorSprite != null)
            {
                nodeImg.sprite = reactorSprite;
                nodeImg.color = Color.white;
            }
            else
            {
                nodeImg.sprite = ArcaneAssetLoader.GetCircleSprite(86, ColEmeraldTarget, ColCyanBorder, 2f);
            }
            nodeImg.preserveAspect = true;

            // Rotating Shield Bracket Ring behind/around Core
            _targetShieldRing = new GameObject("ShieldRing");
            _targetShieldRing.transform.SetParent(_targetNode.transform, false);
            RectTransform srRt = _targetShieldRing.AddComponent<RectTransform>();
            srRt.sizeDelta = new Vector2(104f, 104f);
            srRt.anchoredPosition = Vector2.zero;
            Image srImg = _targetShieldRing.AddComponent<Image>();
            srImg.sprite = ArcaneAssetLoader.GetCircleSprite(104, Color.clear, ColEmeraldTarget, 2f);
            srImg.raycastTarget = false;

            // Target Label below
            GameObject cLabel = new GameObject("CoreLabel");
            cLabel.transform.SetParent(_targetNode.transform, false);
            Text clText = cLabel.AddComponent<Text>();
            clText.text = AegisLocalization.T("[ LÕI GIÁP • 100% ]", "[ ENEMY CORE ]");
            clText.font = font;
            clText.fontSize = 11;
            clText.fontStyle = FontStyle.Bold;
            clText.alignment = TextAnchor.MiddleCenter;
            clText.color = ColEmeraldTarget;
            RectTransform clr = cLabel.GetComponent<RectTransform>();
            clr.anchoredPosition = new Vector2(0f, -56f);
            clr.sizeDelta = new Vector2(130f, 20f);

            // Core Trigger Collider
            CircleCollider2D nodeCol = _targetNode.AddComponent<CircleCollider2D>();
            nodeCol.radius = 38f;
            nodeCol.isTrigger = true;

            var detector = _targetNode.AddComponent<CircuitNodeDetector>();
            detector.onTriggerEnter = OnCoreReached;

            // 5. Electrical Impulse Player (Rigidbody2D + CircleCollider2D)
            _impulsePlayer = new GameObject("ImpulsePlayer");
            _impulsePlayer.transform.SetParent(_arenaBounds.transform, false);
            RectTransform pRt = _impulsePlayer.AddComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(50f, 50f);
            pRt.anchoredPosition = new Vector2(-280f, 0f);

            Image pImg = _impulsePlayer.AddComponent<Image>();
            Sprite impulseSprite = ArcaneAssetLoader.LoadArcaneSprite("cyber_impulse_orb.png");
            if (impulseSprite != null)
            {
                pImg.sprite = impulseSprite;
                pImg.color = Color.white;
            }
            else
            {
                pImg.sprite = ArcaneAssetLoader.GetCircleSprite(50, ColCyanGlow, Color.white, 2.5f);
            }
            pImg.preserveAspect = true;

            _playerRb = _impulsePlayer.AddComponent<Rigidbody2D>();
            _playerRb.gravityScale = 0f;
            _playerRb.linearDamping = 3.6f;

            CircleCollider2D pCol = _impulsePlayer.AddComponent<CircleCollider2D>();
            pCol.radius = 21f;

            // Create Spark Trail Dots Pool
            CreateTrailDotsPool(_arenaBounds.transform);
        }

        private void CreateTrailDotsPool(Transform parent)
        {
            _trailDots.Clear();
            _trailDotImgs.Clear();
            for (int i = 0; i < MAX_TRAIL_DOTS; i++)
            {
                GameObject dot = new GameObject("TrailDot_" + i);
                dot.transform.SetParent(parent, false);
                RectTransform rt = dot.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(16f, 16f);
                Image img = dot.AddComponent<Image>();
                img.sprite = ArcaneAssetLoader.GetCircleSprite(16, ColCyanGlow, Color.white, 1f);
                img.raycastTarget = false;
                dot.SetActive(false);

                _trailDots.Add(dot);
                _trailDotImgs.Add(img);
            }
        }

        private void CreateWall(string name, Vector2 pos, Vector2 size, Color color)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(_arenaBounds.transform, false);

            Image img = wall.AddComponent<Image>();
            img.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)Mathf.Max(size.x, 20), (int)Mathf.Max(size.y, 20), 4f, color, Color.white, 1.2f);
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = false;

            RectTransform rt = wall.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private void CreateLaserObstacle(string name, Vector2 pos, Vector2 size, float speed, float phase)
        {
            GameObject obs = new GameObject(name);
            obs.transform.SetParent(_arenaBounds.transform, false);
            RectTransform rt = obs.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            // Central Laser Beam
            Image beamImg = obs.AddComponent<Image>();
            Sprite laserSprite = ArcaneAssetLoader.LoadArcaneSprite("cyber_laser_barrier.png");
            if (laserSprite != null)
            {
                beamImg.sprite = laserSprite;
                beamImg.type = Image.Type.Simple;
                beamImg.preserveAspect = false;
                beamImg.color = Color.white;
            }
            else
            {
                beamImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)size.x, (int)size.y, 6f, ColCrimsonDanger, Color.white, 1.5f);
                beamImg.type = Image.Type.Sliced;
            }
            beamImg.raycastTarget = false;

            // Top Emitter Cap
            CreateEmitterCap(obs.transform, new Vector2(0f, size.y / 2f + 6f), size.x);
            // Bottom Emitter Cap
            CreateEmitterCap(obs.transform, new Vector2(0f, -size.y / 2f - 6f), size.x);

            // Physical Collider
            BoxCollider2D col = obs.AddComponent<BoxCollider2D>();
            col.size = new Vector2(size.x - 4f, size.y);

            // Moving Animation Component
            var mover = obs.AddComponent<MovingGate2D>();
            mover.speed = speed;
            mover.phaseOffset = phase;
            mover.amplitude = 82f;
        }

        private void CreateEmitterCap(Transform parent, Vector2 pos, float width)
        {
            GameObject cap = new GameObject("EmitterCap");
            cap.transform.SetParent(parent, false);
            RectTransform crt = cap.AddComponent<RectTransform>();
            crt.anchoredPosition = pos;
            crt.sizeDelta = new Vector2(width + 12f, 14f);

            Image cImg = cap.AddComponent<Image>();
            cImg.sprite = ArcaneAssetLoader.GetGlassBoxSprite((int)crt.sizeDelta.x, (int)crt.sizeDelta.y, 4f, new Color(0.18f, 0.22f, 0.30f, 0.95f), ColCrimsonDanger, 1.2f);
            cImg.type = Image.Type.Sliced;
            cImg.raycastTarget = false;

            // Glowing Diode
            GameObject diode = new GameObject("Diode");
            diode.transform.SetParent(cap.transform, false);
            Image dImg = diode.AddComponent<Image>();
            dImg.sprite = ArcaneAssetLoader.GetCircleSprite(8, Color.white, ColCrimsonDanger, 1f);
            dImg.raycastTarget = false;
            RectTransform drt = diode.GetComponent<RectTransform>();
            drt.anchoredPosition = Vector2.zero;
            drt.sizeDelta = new Vector2(8f, 8f);
        }

        private void BuildControlsAndInstructionsPod(Transform parent, Font font)
        {
            // Bottom Controls Bar (y = 18)
            GameObject pod = new GameObject("ControlsPod");
            pod.transform.SetParent(parent, false);
            RectTransform pRt = pod.AddComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0f);
            pRt.anchorMax = new Vector2(0.5f, 0f);
            pRt.pivot = new Vector2(0.5f, 0f);
            pRt.anchoredPosition = new Vector2(0f, 16f);
            pRt.sizeDelta = new Vector2(980f, 42f);

            // Instructions Pill Text
            _controlsHintText = pod.AddComponent<Text>();
            _controlsHintText.font = font;
            _controlsHintText.fontSize = 13;
            _controlsHintText.alignment = TextAnchor.MiddleCenter;
            _controlsHintText.color = ColTextMuted;
            _controlsHintText.text = AegisLocalization.T(
                "🎮 Điều khiển: Phím [W/A/S/D] • [Mũi Tên] hoặc Chạm & Kéo xung điện vào Lõi Giáp",
                "🎮 Controls: [WASD] • [Arrow Keys] or Touch & Drag Impulse into the Core Reactor"
            );

            // Optional On-Screen Touch Controls (Left / Right / Up / Down) for Touchscreens
            CreateVirtualArrowBtn(pod.transform, "◀", new Vector2(-420f, 0f), Vector2.left, font);
            CreateVirtualArrowBtn(pod.transform, "▲", new Vector2(-365f, 0f), Vector2.up, font);
            CreateVirtualArrowBtn(pod.transform, "▼", new Vector2(-310f, 0f), Vector2.down, font);
            CreateVirtualArrowBtn(pod.transform, "▶", new Vector2(-255f, 0f), Vector2.right, font);
        }

        private void CreateVirtualArrowBtn(Transform parent, string arrow, Vector2 pos, Vector2 dir, Font font)
        {
            GameObject btnObj = new GameObject("BtnArrow_" + arrow);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(46f, 34f);

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = ArcaneAssetLoader.GetGlassBoxSprite(46, 34, 8f, new Color(0.04f, 0.12f, 0.24f, 0.85f), ColCyanBorder, 1.2f);
            bg.type = Image.Type.Sliced;

            GameObject tObj = new GameObject("Txt");
            tObj.transform.SetParent(btnObj.transform, false);
            Text txt = tObj.AddComponent<Text>();
            txt.text = arrow;
            txt.font = font;
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = ColCyanGlow;

            RectTransform trt = tObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var touchHandler = btnObj.AddComponent<CyberTouchDirectionButton>();
            touchHandler.Direction = dir;
            touchHandler.OnHold = (d) => _virtualDirection = d;
            touchHandler.OnRelease = () => _virtualDirection = Vector2.zero;
        }

        // =========================================================================
        // PUBLIC API
        // =========================================================================
        public void StartMinigame(Action<bool> onComplete)
        {
            _onCompleteCallback = onComplete;
            _timeRemaining = TOTAL_TIME;
            _isActive = true;
            _virtualDirection = Vector2.zero;

            // Update text for current language
            if (_titleText != null)
                _titleText.text = AegisLocalization.T("⚡ ĐỘT NHẬP TÁC CHIẾN: HACK TƯỜNG LỬA GIÁP", "⚡ TACTICAL OVERRIDE: BREACH ENEMY ARMOR CIRCUIT");
            if (_subtitleText != null)
                _subtitleText.text = AegisLocalization.T("/// GIAO THỨC NEURAL XUNG ĐIỆN: DẪN XUNG ĐẾN LÕI GIÁP ĐỐI PHƯƠNG ///", "/// NEURAL IMPULSE PROTOCOL: GUIDE IMPULSE INTO ENEMY CORE ///");
            if (_statusText != null)
            {
                _statusText.text = AegisLocalization.T("Vượt qua tường lửa vi mạch trong vòng 5 giây!", "Bypass circuit firewall within 5 seconds!");
                _statusText.color = Color.white;
            }
            if (_controlsHintText != null)
            {
                _controlsHintText.text = AegisLocalization.T(
                    "🎮 Điều khiển: Phím [W/A/S/D] • [Mũi Tên] hoặc Chạm & Kéo xung điện vào Lõi Giáp",
                    "🎮 Controls: [WASD] • [Arrow Keys] or Touch & Drag Impulse into the Core Reactor"
                );
            }

            // Reset player position & physics
            if (_impulsePlayer != null)
            {
                RectTransform pRt = _impulsePlayer.GetComponent<RectTransform>();
                pRt.anchoredPosition = new Vector2(-280f, 0f);
                if (_playerRb != null)
                {
                    _playerRb.linearVelocity = Vector2.zero;
                    _playerRb.angularVelocity = 0f;
                }
            }

            // Reset trail dots
            for (int i = 0; i < _trailDots.Count; i++)
            {
                if (_trailDots[i] != null) _trailDots[i].SetActive(false);
            }

            _canvasRoot.SetActive(true);
        }

        public void AbortMinigame()
        {
            if (!_isActive) return;
            _isActive = false;

            if (_statusText != null)
            {
                _statusText.text = AegisLocalization.T("ĐÃ HỦY ĐỘT NHẬP: Thực hiện đòn đánh động năng cơ bản.", "BREACH ABORTED: Standard Kinetic Strike Executed.");
                _statusText.color = new Color(1.0f, 0.45f, 0.45f);
            }

            StartCoroutine(FinishDelay(false));
        }

        // =========================================================================
        // GAME LOOP
        // =========================================================================
        private void Update()
        {
            if (!_isActive) return;

            // 1. Countdown Timer & Dynamic Energy Bar Fill
            _timeRemaining -= Time.deltaTime;
            float progress = Mathf.Clamp01(_timeRemaining / TOTAL_TIME);

            if (_timerFillRt != null)
            {
                _timerFillRt.sizeDelta = new Vector2(progress * 674f, 20f);
            }

            if (_timerFillImg != null)
            {
                if (progress > 0.5f)
                {
                    float t = (progress - 0.5f) / 0.5f;
                    _timerFillImg.color = Color.Lerp(ColGoldAccent, ColCyanGlow, t);
                }
                else
                {
                    float t = progress / 0.5f;
                    Color alarmCol = (progress < 0.25f && Mathf.Sin(Time.time * 16f) > 0f) ? Color.white : ColCrimsonDanger;
                    _timerFillImg.color = Color.Lerp(alarmCol, ColGoldAccent, t);
                }
            }

            if (_timerText != null)
            {
                _timerText.text = AegisLocalization.T(
                    $"⏱ CỬA SỔ ĐỘT NHẬP: {Mathf.Max(0, _timeRemaining):0.00}s",
                    $"⏱ CIRCUIT BREACH WINDOW: {Mathf.Max(0, _timeRemaining):0.00}s"
                );
            }

            // 2. Rotate Quantum Core Shield Rings
            if (_targetShieldRing != null)
            {
                _targetShieldRing.transform.Rotate(0f, 0f, -60f * Time.deltaTime);
            }
            if (_targetNodeRt != null)
            {
                float coreScale = 1.0f + 0.06f * Mathf.Sin(Time.time * 5f);
                _targetNodeRt.localScale = new Vector3(coreScale, coreScale, 1f);
            }

            // 3. Animated Impulse Pulse & Trail Sparks
            if (_impulsePlayer != null)
            {
                float pScale = 1.0f + 0.08f * Mathf.Sin(Time.time * 8f);
                _impulsePlayer.transform.localScale = new Vector3(pScale, pScale, 1f);

                // Update Spark Trail
                _trailTimer += Time.deltaTime;
                if (_trailTimer > 0.045f)
                {
                    _trailTimer = 0f;
                    SpawnTrailSpark(_impulsePlayer.GetComponent<RectTransform>().anchoredPosition);
                }
            }

            // 4. Directional Controls (Keyboard + Touch D-Pad + Touch Drag)
            float h = _virtualDirection.x;
            float v = _virtualDirection.y;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h = -1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h = 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v = 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v = -1f;
            }

            // Mouse / Touch Drag Steering
            Mouse mouse = Mouse.current;
            Touchscreen ts = Touchscreen.current;
            Vector2 pointerPos = Vector2.zero;
            bool isPointerActive = false;

            if (mouse != null && mouse.leftButton.isPressed)
            {
                pointerPos = mouse.position.ReadValue();
                isPointerActive = true;
            }
            else if (ts != null && ts.primaryTouch.press.isPressed)
            {
                pointerPos = ts.primaryTouch.position.ReadValue();
                isPointerActive = true;
            }

            if (isPointerActive && _arenaRt != null && _impulsePlayer != null)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_arenaRt, pointerPos, null, out Vector2 localPt))
                {
                    // Only steer if pointer is within or near the arena
                    if (Mathf.Abs(localPt.x) <= _arenaRt.rect.width / 2f + 40f && Mathf.Abs(localPt.y) <= _arenaRt.rect.height / 2f + 40f)
                    {
                        Vector2 curPos = _impulsePlayer.GetComponent<RectTransform>().anchoredPosition;
                        Vector2 diff = localPt - curPos;
                        if (diff.magnitude > 15f)
                        {
                            Vector2 dir = diff.normalized;
                            h = dir.x;
                            v = dir.y;
                        }
                    }
                }
            }

            // Apply smooth physics force
            if (_playerRb != null)
            {
                Vector2 force = new Vector2(h, v).normalized * 1150f;
                _playerRb.AddForce(force);
            }

            // 5. Check Timeout
            if (_timeRemaining <= 0)
            {
                OnTimeout();
            }
        }

        private void SpawnTrailSpark(Vector2 pos)
        {
            if (_trailDots.Count == 0) return;
            GameObject dot = _trailDots[_trailIndex];
            Image img = _trailDotImgs[_trailIndex];
            _trailIndex = (_trailIndex + 1) % _trailDots.Count;

            if (dot != null)
            {
                dot.SetActive(true);
                RectTransform rt = dot.GetComponent<RectTransform>();
                rt.anchoredPosition = pos;
                rt.localScale = Vector3.one;
                if (img != null) img.color = new Color(0.0f, 0.95f, 1.0f, 0.75f);
                StartCoroutine(FadeOutDot(dot, img));
            }
        }

        private IEnumerator FadeOutDot(GameObject dot, Image img)
        {
            float elapsed = 0f;
            float dur = 0.28f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                if (dot != null)
                {
                    dot.transform.localScale = Vector3.one * (1f - t * 0.6f);
                }
                if (img != null)
                {
                    img.color = new Color(0.0f, 0.95f, 1.0f, (1f - t) * 0.75f);
                }
                yield return null;
            }
            if (dot != null) dot.SetActive(false);
        }

        // =========================================================================
        // WIN / LOSS SEQUENCES
        // =========================================================================
        private void OnCoreReached()
        {
            if (!_isActive) return;
            _isActive = false;

            if (_statusText != null)
            {
                _statusText.text = AegisLocalization.T(
                    ">>> ĐỘT NHẬP GIÁP THÀNH CÔNG! SÁT THƯƠNG CHÍ MẠNG 100% <<<",
                    ">>> ARMOR BREACH CONFIRMED! 100% LETHAL STRIKE <<<"
                );
                _statusText.color = ColEmeraldTarget;
            }

            StartCoroutine(PlayBreachCelebration());
        }

        private IEnumerator PlayBreachCelebration()
        {
            // Expanding Victory Shockwave Ring
            GameObject shockwave = new GameObject("ShockwaveRing");
            shockwave.transform.SetParent(_arenaBounds.transform, false);
            RectTransform sRt = shockwave.AddComponent<RectTransform>();
            sRt.anchoredPosition = (_targetNodeRt != null) ? _targetNodeRt.anchoredPosition : Vector2.zero;
            sRt.sizeDelta = new Vector2(80f, 80f);

            Image sImg = shockwave.AddComponent<Image>();
            sImg.sprite = ArcaneAssetLoader.GetCircleSprite(80, Color.clear, ColEmeraldTarget, 3f);
            sImg.raycastTarget = false;

            float elapsed = 0f;
            float dur = 0.85f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                float sz = Mathf.Lerp(80f, 380f, t);
                sRt.sizeDelta = new Vector2(sz, sz);
                sImg.color = new Color(0.0f, 1.0f, 0.55f, 1f - t);
                yield return null;
            }

            Destroy(shockwave);
            _canvasRoot.SetActive(false);
            _onCompleteCallback?.Invoke(true);
        }

        private void OnTimeout()
        {
            if (!_isActive) return;
            _isActive = false;

            if (_statusText != null)
            {
                _statusText.text = AegisLocalization.T(
                    "CẢNH BÁO: HẾT THỜI GIAN ĐỘT NHẬP - Thực hiện đòn đánh cơ bản.",
                    "CIRCUIT TIMEOUT: Standard Kinetic Strike Executed."
                );
                _statusText.color = ColGoldAccent;
            }

            StartCoroutine(FinishDelay(false));
        }

        private IEnumerator FinishDelay(bool success)
        {
            yield return new WaitForSeconds(0.85f);
            if (_canvasRoot != null) _canvasRoot.SetActive(false);
            _onCompleteCallback?.Invoke(success);
        }
    }

    /// <summary>
    /// Trigger detector on the target Core node.
    /// </summary>
    public class CircuitNodeDetector : MonoBehaviour
    {
        public Action onTriggerEnter;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null && other.name.Contains("ImpulsePlayer"))
            {
                onTriggerEnter?.Invoke();
            }
        }
    }

    /// <summary>
    /// Smooth vertical moving laser obstacle gate with electric hazard animation.
    /// </summary>
    public class MovingGate2D : MonoBehaviour
    {
        public float speed = 1.0f;
        public float phaseOffset = 0f;
        public float amplitude = 82f;
        private float _originY;
        private RectTransform _rt;

        private void Start()
        {
            _rt = GetComponent<RectTransform>();
            _originY = (_rt != null) ? _rt.anchoredPosition.y : 0f;
        }

        private void Update()
        {
            if (_rt == null) return;
            float newY = _originY + Mathf.Sin(Time.time * 2.3f * speed + phaseOffset) * amplitude;
            _rt.anchoredPosition = new Vector2(_rt.anchoredPosition.x, newY);

            // Subtle electric plasma width jitter for laser beam feel
            float jitter = 1.0f + 0.05f * Mathf.Sin(Time.time * 28f);
            _rt.localScale = new Vector3(jitter, 1f, 1f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Repulsion impulse on collision
            if (collision != null && collision.rigidbody != null)
            {
                Vector2 bounceDir = (collision.transform.position.x < transform.position.x) ? Vector2.left : Vector2.right;
                collision.rigidbody.AddForce(bounceDir * 350f, ForceMode2D.Impulse);
            }
        }
    }

    /// <summary>
    /// Virtual Touch Direction Button for touchscreen and mobile playability.
    /// </summary>
    public class CyberTouchDirectionButton : MonoBehaviour,
        UnityEngine.EventSystems.IPointerDownHandler,
        UnityEngine.EventSystems.IPointerUpHandler,
        UnityEngine.EventSystems.IPointerExitHandler
    {
        public Vector2 Direction;
        public Action<Vector2> OnHold;
        public Action OnRelease;

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            OnHold?.Invoke(Direction);
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            OnRelease?.Invoke();
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            OnRelease?.Invoke();
        }
    }
}
