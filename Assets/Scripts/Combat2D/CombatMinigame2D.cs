using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace MechChess.Combat2D
{
    /// <summary>
    /// 2D Physics-based "Armor Hack" Minigame using Rigidbody2D and Collider2D.
    /// Triggered whenever a Mech piece captures an enemy piece.
    /// Player guides an electrical impulse across circuit obstacles within 5 seconds to confirm kill.
    /// </summary>
    public class CombatMinigame2D : MonoBehaviour
    {
        public static CombatMinigame2D Instance { get; private set; }

        [Header("State")]
        private bool _isActive = false;
        private float _timeRemaining = 5.0f;
        private Action<bool> _onCompleteCallback;

        // UI & Physics GameObjects
        private GameObject _canvasRoot;
        private Text _timerText;
        private Text _statusText;
        private GameObject _impulsePlayer;
        private Rigidbody2D _playerRb;
        private GameObject _targetNode;
        private GameObject _arenaBounds;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            BuildMinigameUIAndArena();
        }

        private void BuildMinigameUIAndArena()
        {
            // Canvas Root
            _canvasRoot = new GameObject("Minigame2D_Canvas");
            _canvasRoot.transform.SetParent(transform, false);

            Canvas canvas = _canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = _canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _canvasRoot.AddComponent<GraphicRaycaster>();

            // Dark Glassmorphism Backdrop
            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(_canvasRoot.transform, false);
            Image bgImg = backdrop.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
            RectTransform bgRt = backdrop.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Header Banner
            GameObject header = new GameObject("HeaderTitle");
            header.transform.SetParent(backdrop.transform, false);
            Text titleTxt = header.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 36;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.0f, 0.95f, 1.0f);
            titleTxt.text = "[TACTICAL OVERRIDE] HACK ENEMY ARMOR CIRCUIT";
            RectTransform headerRt = header.GetComponent<RectTransform>();
            headerRt.anchoredPosition = new Vector2(0, 420);
            headerRt.sizeDelta = new Vector2(900, 60);

            // Timer Text
            GameObject timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(backdrop.transform, false);
            _timerText = timerObj.AddComponent<Text>();
            _timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _timerText.fontSize = 28;
            _timerText.alignment = TextAnchor.MiddleCenter;
            _timerText.color = new Color(1.0f, 0.85f, 0.1f);
            _timerText.text = "CIRCUIT BREACH WINDOW: 5.00s";
            RectTransform timerRt = timerObj.GetComponent<RectTransform>();
            timerRt.anchoredPosition = new Vector2(0, 360);
            timerRt.sizeDelta = new Vector2(600, 50);

            // Status Subtitle
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(backdrop.transform, false);
            _statusText = statusObj.AddComponent<Text>();
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _statusText.fontSize = 22;
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = Color.white;
            _statusText.text = "Use [WASD/Arrow Keys] or [Drag/Touch] to guide the Electrical Impulse to the Core Node!";
            RectTransform statusRt = statusObj.GetComponent<RectTransform>();
            statusRt.anchoredPosition = new Vector2(0, -420);
            statusRt.sizeDelta = new Vector2(950, 50);

            // Physics 2D Arena Frame
            _arenaBounds = new GameObject("Arena2D_PhysicsRoot");
            _arenaBounds.transform.SetParent(_canvasRoot.transform, false);

            CreatePhysicsArena();
            _canvasRoot.SetActive(false);
        }

        private void CreatePhysicsArena()
        {
            // Center UI Frame for circuit
            float width = 600f;
            float height = 400f;

            // 4 Wall Colliders
            CreateWall("WallTop", new Vector2(0, height / 2f), new Vector2(width, 20f));
            CreateWall("WallBottom", new Vector2(0, -height / 2f), new Vector2(width, 20f));
            CreateWall("WallLeft", new Vector2(-width / 2f, 0), new Vector2(20f, height));
            CreateWall("WallRight", new Vector2(width / 2f, 0), new Vector2(20f, height));

            // Obstacle Gates
            CreateObstacle("Gate1", new Vector2(-120f, 60f), new Vector2(30f, 220f), 0.8f);
            CreateObstacle("Gate2", new Vector2(120f, -60f), new Vector2(30f, 220f), -0.8f);

            // Target Core Node (Trigger Collider2D)
            _targetNode = new GameObject("TargetCoreNode");
            _targetNode.transform.SetParent(_arenaBounds.transform, false);
            Image nodeImg = _targetNode.AddComponent<Image>();
            nodeImg.color = new Color(0.0f, 1.0f, 0.4f, 0.85f);
            RectTransform nodeRt = _targetNode.GetComponent<RectTransform>();
            nodeRt.sizeDelta = new Vector2(60, 60);
            nodeRt.anchoredPosition = new Vector2(220f, 0);

            BoxCollider2D nodeCol = _targetNode.AddComponent<BoxCollider2D>();
            nodeCol.size = new Vector2(60, 60);
            nodeCol.isTrigger = true;

            // Electrical Impulse Player (Rigidbody2D + CircleCollider2D)
            _impulsePlayer = new GameObject("ImpulsePlayer");
            _impulsePlayer.transform.SetParent(_arenaBounds.transform, false);
            Image pImg = _impulsePlayer.AddComponent<Image>();
            pImg.color = new Color(0.1f, 0.9f, 1.0f);
            RectTransform pRt = _impulsePlayer.GetComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(35, 35);
            pRt.anchoredPosition = new Vector2(-220f, 0);

            _playerRb = _impulsePlayer.AddComponent<Rigidbody2D>();
            _playerRb.gravityScale = 0f;
            _playerRb.linearDamping = 3.5f;

            CircleCollider2D pCol = _impulsePlayer.AddComponent<CircleCollider2D>();
            pCol.radius = 17.5f;

            // Trigger Receiver on Target
            var detector = _targetNode.AddComponent<CircuitNodeDetector>();
            detector.onTriggerEnter = OnCoreReached;
        }

        private void CreateWall(string name, Vector2 pos, Vector2 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(_arenaBounds.transform, false);
            Image img = wall.AddComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.6f, 0.7f);
            RectTransform rt = wall.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private void CreateObstacle(string name, Vector2 pos, Vector2 size, float speed)
        {
            GameObject obs = new GameObject(name);
            obs.transform.SetParent(_arenaBounds.transform, false);
            Image img = obs.AddComponent<Image>();
            img.color = new Color(1.0f, 0.2f, 0.3f, 0.8f);
            RectTransform rt = obs.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            BoxCollider2D col = obs.AddComponent<BoxCollider2D>();
            col.size = size;

            var mover = obs.AddComponent<MovingGate2D>();
            mover.speed = speed;
        }

        public void StartMinigame(Action<bool> onComplete)
        {
            _onCompleteCallback = onComplete;
            _timeRemaining = 5.0f;
            _isActive = true;

            // Reset player position
            if (_impulsePlayer != null)
            {
                RectTransform pRt = _impulsePlayer.GetComponent<RectTransform>();
                pRt.anchoredPosition = new Vector2(-220f, 0);
                if (_playerRb != null) _playerRb.linearVelocity = Vector2.zero;
            }

            _canvasRoot.SetActive(true);
            _statusText.text = "Bypass circuit firewall within 5 seconds!";
            _statusText.color = Color.white;
        }

        private void Update()
        {
            if (!_isActive) return;

            // Countdown timer
            _timeRemaining -= Time.deltaTime;
            _timerText.text = $"CIRCUIT BREACH WINDOW: {Mathf.Max(0, _timeRemaining):0.00}s";

            // Keyboard / Virtual touch controls for Rigidbody2D using New Input System
            float h = 0f;
            float v = 0f;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h = -1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h = 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v = 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v = -1f;
            }

            // Touch / mouse drag support
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

            if (isPointerActive)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _arenaBounds.GetComponent<RectTransform>(), pointerPos, null, out Vector2 localPt);

                Vector2 curPos = _impulsePlayer.GetComponent<RectTransform>().anchoredPosition;
                Vector2 dir = (localPt - curPos).normalized;
                h = dir.x;
                v = dir.y;
            }

            if (_playerRb != null)
            {
                Vector2 force = new Vector2(h, v).normalized * 950f;
                _playerRb.AddForce(force);
            }

            // Check Timeout
            if (_timeRemaining <= 0)
            {
                OnTimeout();
            }
        }

        private void OnCoreReached()
        {
            if (!_isActive) return;
            _isActive = false;

            _statusText.text = ">>> ARMOR BREACH CONFIRMED! 100% LETHAL STRIKE <<<";
            _statusText.color = Color.green;

            StartCoroutine(FinishDelay(true));
        }

        private void OnTimeout()
        {
            if (!_isActive) return;
            _isActive = false;

            _statusText.text = "OVERRIDE PARTIAL: Standard Kinetic Strike Executed.";
            _statusText.color = Color.yellow;

            StartCoroutine(FinishDelay(true));
        }

        private IEnumerator FinishDelay(bool success)
        {
            yield return new WaitForSeconds(0.85f);
            _canvasRoot.SetActive(false);
            _onCompleteCallback?.Invoke(success);
        }
    }

    public class CircuitNodeDetector : MonoBehaviour
    {
        public Action onTriggerEnter;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.name.Contains("ImpulsePlayer"))
            {
                onTriggerEnter?.Invoke();
            }
        }
    }

    public class MovingGate2D : MonoBehaviour
    {
        public float speed = 1.0f;
        private float _originY;

        private void Start()
        {
            _originY = GetComponent<RectTransform>().anchoredPosition.y;
        }

        private void Update()
        {
            RectTransform rt = GetComponent<RectTransform>();
            float newY = _originY + Mathf.Sin(Time.time * 2.5f * speed) * 80f;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, newY);
        }
    }
}
