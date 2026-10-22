using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using MechChess.Chess3D;

namespace MechChess.Mobile
{
    /// <summary>
    /// Mobile Touch Input Handler using Unity's modern Input System:
    /// - 1-Finger Single Tap: Raycasts to select or move 3D Mech pieces.
    /// - 1-Finger Drag / Swipe: Smooth 360-degree orbit rotation around the chessboard center.
    /// - 2-Finger Pinch-to-Zoom: Dynamically zooms camera in/out.
    /// Also supports PC mouse/wheel controls for cross-platform debugging.
    /// </summary>
    public class MobileTouchInput : MonoBehaviour
    {
        [Header("Orbit Camera Settings")]
        public Transform targetBoardCenter;
        public float rotationSpeed = 0.25f;
        public float zoomSpeed = 0.015f;
        public float mouseZoomSpeed = 0.8f;
        public float minZoomDistance = 4.0f;
        public float maxZoomDistance = 16.0f;

        [Header("Touch Thresholds")]
        public float tapMaxDistance = 15f;
        public float tapMaxDuration = 0.35f;

        private Camera _mainCamera;
        private float _currentDistance = 8.8f;
        private float _yaw = 0f;
        private float _pitch = 38f;

        [Header("Smoothing & Ergonomics")]
        public float smoothDampingSpeed = 14f;
        private float _smoothYaw = 0f;
        private float _smoothPitch = 38f;
        private float _smoothDistance = 8.8f;

        public static MobileTouchInput Instance { get; private set; }

        public enum CameraPerspectiveMode
        {
            White3D = 0,      // Alliance Front POV (Yaw: 0°, Pitch: 38°)
            Black3D = 1,      // Syndicate Front POV (Yaw: 180°, Pitch: 38°)
            TopDown2D = 2,    // Tactical Satellite POV (Yaw: 0°, Pitch: 89.9°)
            IsometricHero = 3 // Command Cyber POV (Yaw: 45°, Pitch: 34°)
        }

        public CameraPerspectiveMode CurrentPerspective { get; private set; } = CameraPerspectiveMode.White3D;

        private Vector2 _touchStartPos;
        private float _touchStartTime;
        private bool _isSwiping = false;

        private void Awake()
        {
            Instance = this;
            _mainCamera = Camera.main ?? GetComponent<Camera>();
            _currentDistance = GetOptimalDistance();
            _smoothYaw = _yaw;
            _smoothPitch = _pitch;
            _smoothDistance = _currentDistance;
            EnsureTargetCenter();
            UpdateCameraTransform(immediate: true);
        }

        private void Start()
        {
            _currentDistance = GetOptimalDistance();
            _smoothYaw = _yaw;
            _smoothPitch = _pitch;
            _smoothDistance = _currentDistance;
            EnsureTargetCenter();
            UpdateCameraTransform(immediate: true);
        }

        /// <summary>
        /// Calculates optimal camera distance dynamically based on screen aspect ratio.
        /// In portrait mode (height > width), pulls camera back so the 8x8 board and peripheral notation are fully visible.
        /// </summary>
        public float GetOptimalDistance()
        {
            if (Screen.width < Screen.height && Screen.height > 0)
            {
                float aspect = (float)Screen.width / Screen.height;
                return Mathf.Clamp(8.8f / aspect * 0.62f, 10.5f, 13.5f);
            }
            return 8.8f;
        }

        private void EnsureTargetCenter()
        {
            if (targetBoardCenter == null)
            {
                if (ChessBoard3DManager.Instance != null && ChessBoard3DManager.Instance.BoardRootContainer != null)
                {
                    Transform existing = ChessBoard3DManager.Instance.BoardRootContainer.Find("BoardOrbitCenter");
                    if (existing != null)
                    {
                        targetBoardCenter = existing;
                    }
                    else
                    {
                        GameObject centerObj = new GameObject("BoardOrbitCenter");
                        centerObj.transform.SetParent(ChessBoard3DManager.Instance.BoardRootContainer, false);
                        centerObj.transform.localPosition = new Vector3(3.5f, 0.5f, 3.5f);
                        targetBoardCenter = centerObj.transform;
                    }
                }
                else
                {
                    GameObject centerObj = GameObject.Find("BoardOrbitCenter");
                    if (centerObj == null)
                    {
                        centerObj = new GameObject("BoardOrbitCenter");
                        centerObj.transform.position = new Vector3(3.5f, 0.5f, 3.5f);
                    }
                    targetBoardCenter = centerObj.transform;
                }
            }
        }

        private void LateUpdate()
        {
            EnsureTargetCenter();
            UpdateCameraTransform(immediate: false);
        }

        private void Update()
        {
            HandleModernTouchInput();
            HandleModernMouseInput();
        }

        private bool IsPointerOverUI(int fingerId = -1)
        {
            if (EventSystem.current == null) return false;
            if (fingerId >= 0)
                return EventSystem.current.IsPointerOverGameObject(fingerId);
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void HandleModernTouchInput()
        {
            Touchscreen ts = Touchscreen.current;
            if (ts == null) return;

            int activeTouches = 0;
            for (int i = 0; i < ts.touches.Count; i++)
            {
                if (ts.touches[i].press.isPressed) activeTouches++;
            }

            if (activeTouches == 1)
            {
                var primary = ts.primaryTouch;
                Vector2 curPos = primary.position.ReadValue();
                Vector2 delta = primary.delta.ReadValue();
                int touchId = primary.touchId.ReadValue();

                if (primary.press.wasPressedThisFrame)
                {
                    if (IsPointerOverUI(touchId)) return;
                    _touchStartPos = curPos;
                    _touchStartTime = Time.time;
                    _isSwiping = false;
                }
                else if (primary.press.isPressed)
                {
                    if (IsPointerOverUI(touchId) && !_isSwiping) return;

                    float dist = Vector2.Distance(curPos, _touchStartPos);
                    if (dist > tapMaxDistance)
                    {
                        _isSwiping = true;
                        _yaw += delta.x * rotationSpeed;
                        _pitch -= delta.y * rotationSpeed;
                        _pitch = Mathf.Clamp(_pitch, 15f, 85f);
                        UpdateCameraTransform();
                    }
                }

                if (primary.press.wasReleasedThisFrame)
                {
                    if (IsPointerOverUI(touchId) && !_isSwiping) return;

                    float duration = Time.time - _touchStartTime;
                    float dist = Vector2.Distance(curPos, _touchStartPos);

                    if (!_isSwiping && duration <= tapMaxDuration && dist <= tapMaxDistance)
                    {
                        ProcessTapRaycast(curPos);
                    }
                }
            }
            else if (activeTouches >= 2)
            {
                // 2-finger pinch-to-zoom
                var touch0 = ts.touches[0];
                var touch1 = ts.touches[1];

                Vector2 pos0 = touch0.position.ReadValue();
                Vector2 pos1 = touch1.position.ReadValue();
                Vector2 delta0 = touch0.delta.ReadValue();
                Vector2 delta1 = touch1.delta.ReadValue();

                Vector2 prevPos0 = pos0 - delta0;
                Vector2 prevPos1 = pos1 - delta1;

                float prevDist = Vector2.Distance(prevPos0, prevPos1);
                float curDist = Vector2.Distance(pos0, pos1);

                float deltaMagnitude = prevDist - curDist;
                _currentDistance += deltaMagnitude * zoomSpeed;
                _currentDistance = Mathf.Clamp(_currentDistance, minZoomDistance, maxZoomDistance);

                UpdateCameraTransform();
            }
        }

        private void HandleModernMouseInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            // Right-click drag to orbit
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _yaw += delta.x * 0.2f;
                _pitch -= delta.y * 0.2f;
                _pitch = Mathf.Clamp(_pitch, 15f, 85f);
                UpdateCameraTransform();
            }

            // Mouse wheel scroll to zoom
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentDistance -= (scroll > 0 ? 1f : -1f) * mouseZoomSpeed;
                _currentDistance = Mathf.Clamp(_currentDistance, minZoomDistance, maxZoomDistance);
                UpdateCameraTransform();
            }

            // Left-click tap on PC
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (IsPointerOverUI()) return;

                Vector2 mousePos = mouse.position.ReadValue();
                ProcessTapRaycast(mousePos);
            }
        }

        private void ProcessTapRaycast(Vector2 screenPoint)
        {
            if (_mainCamera == null) return;
            if (IsPointerOverUI()) return;

            Ray ray = _mainCamera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                // Check if hit a ChessPiece3D
                ChessPiece3D piece = hit.collider.GetComponentInParent<ChessPiece3D>();
                if (piece != null && ChessBoard3DManager.Instance != null)
                {
                    ChessBoard3DManager.Instance.HandlePieceClicked(piece);
                    return;
                }

                // Check if hit a Tile
                TileClickReceiver tileReceiver = hit.collider.GetComponent<TileClickReceiver>();
                if (tileReceiver != null && ChessBoard3DManager.Instance != null)
                {
                    tileReceiver.OnTileClicked?.Invoke(tileReceiver.file, tileReceiver.rank);
                }
            }
        }

        public string CycleCameraPerspective()
        {
            int next = ((int)CurrentPerspective + 1) % 4;
            return SetCameraPerspective((CameraPerspectiveMode)next);
        }

        public string SetCameraPerspective(CameraPerspectiveMode mode)
        {
            CurrentPerspective = mode;
            float dist = GetOptimalDistance();

            switch (mode)
            {
                case CameraPerspectiveMode.White3D:
                    _yaw = 0f;
                    _pitch = 38f;
                    _currentDistance = dist;
                    return "Liên Minh (Trắng 3D)";

                case CameraPerspectiveMode.Black3D:
                    _yaw = 180f;
                    _pitch = 38f;
                    _currentDistance = dist;
                    return "Hắc Ám (Đen 3D)";

                case CameraPerspectiveMode.TopDown2D:
                    _yaw = 0f;
                    _pitch = 89.9f;
                    _currentDistance = dist * 1.06f;
                    return "Chiến thuật 2D (Top-down)";

                case CameraPerspectiveMode.IsometricHero:
                    _yaw = 45f;
                    _pitch = 34f;
                    _currentDistance = dist * 0.96f;
                    return "Đẳng cự Sci-Fi (Hero 3D)";

                default:
                    return string.Empty;
            }
        }

        public bool Toggle2DView()
        {
            if (CurrentPerspective == CameraPerspectiveMode.TopDown2D)
            {
                SetCameraPerspective(CameraPerspectiveMode.White3D);
                return false;
            }
            else
            {
                SetCameraPerspective(CameraPerspectiveMode.TopDown2D);
                return true;
            }
        }

        public void SetOrientation(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = pitch;
            UpdateCameraTransform();
        }

        private void UpdateCameraTransform(bool immediate = false)
        {
            if (targetBoardCenter == null || _mainCamera == null) return;

            if (immediate)
            {
                _smoothYaw = _yaw;
                _smoothPitch = _pitch;
                _smoothDistance = _currentDistance;
            }
            else
            {
                float dt = Time.unscaledDeltaTime;
                _smoothYaw = Mathf.LerpAngle(_smoothYaw, _yaw, dt * smoothDampingSpeed);
                _smoothPitch = Mathf.Lerp(_smoothPitch, _pitch, dt * smoothDampingSpeed);
                _smoothDistance = Mathf.Lerp(_smoothDistance, _currentDistance, dt * smoothDampingSpeed);
            }

            Quaternion rotation = Quaternion.Euler(_smoothPitch, _smoothYaw, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -_smoothDistance);

            _mainCamera.transform.position = targetBoardCenter.position + offset;
            _mainCamera.transform.LookAt(targetBoardCenter.position);
        }
    }
}
