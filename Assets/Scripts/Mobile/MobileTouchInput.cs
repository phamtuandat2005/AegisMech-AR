using UnityEngine;
using UnityEngine.InputSystem;
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
        private float _currentDistance = 9.5f;
        private float _yaw = 0f;
        private float _pitch = 45f;

        private Vector2 _touchStartPos;
        private float _touchStartTime;
        private bool _isSwiping = false;

        private void Awake()
        {
            _mainCamera = Camera.main ?? GetComponent<Camera>();
            EnsureTargetCenter();
            UpdateCameraTransform();
        }

        private void Start()
        {
            EnsureTargetCenter();
            UpdateCameraTransform();
        }

        private void EnsureTargetCenter()
        {
            if (targetBoardCenter == null)
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

        private void Update()
        {
            HandleModernTouchInput();
            HandleModernMouseInput();
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

                if (primary.press.wasPressedThisFrame)
                {
                    _touchStartPos = curPos;
                    _touchStartTime = Time.time;
                    _isSwiping = false;
                }
                else if (primary.press.isPressed)
                {
                    float dist = Vector2.Distance(curPos, _touchStartPos);
                    if (dist > tapMaxDistance)
                    {
                        _isSwiping = true;
                        _yaw += delta.x * rotationSpeed;
                        _pitch -= delta.y * rotationSpeed;
                        _pitch = Mathf.Clamp(_pitch, 15f, 80f);
                        UpdateCameraTransform();
                    }
                }

                if (primary.press.wasReleasedThisFrame)
                {
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
                // 2-finger pinch
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
                _pitch = Mathf.Clamp(_pitch, 15f, 80f);
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
                Vector2 mousePos = mouse.position.ReadValue();
                ProcessTapRaycast(mousePos);
            }
        }

        private void ProcessTapRaycast(Vector2 screenPoint)
        {
            if (_mainCamera == null) return;

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

        private void UpdateCameraTransform()
        {
            if (targetBoardCenter == null || _mainCamera == null) return;

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
            Vector3 offset = rotation * new Vector3(0, 0, -_currentDistance);

            _mainCamera.transform.position = targetBoardCenter.position + offset;
            _mainCamera.transform.LookAt(targetBoardCenter.position);
        }
    }
}
