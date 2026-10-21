using System.Collections.Generic;
using UnityEngine;

namespace AegisMech.UI
{
    public class AegisUIManager : MonoBehaviour
    {
        public static AegisUIManager Instance { get; private set; }

        public AegisScreen[] screens;
        public AegisPopup popup;
        public AegisToast toast;
        public AegisScreenId startScreen = AegisScreenId.Splash;

        readonly Stack<AegisScreenId> _history = new Stack<AegisScreenId>();
        AegisScreenId _current = AegisScreenId.None;
        public AegisScreenId Current { get { return _current; } }

        void Awake()
        {
            Instance = this;
            if (screens != null) foreach (var s in screens) if (s != null) s.Hide(true);
            if (popup != null) popup.gameObject.SetActive(false);
            if (toast != null) toast.gameObject.SetActive(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start() { Go(startScreen, false); }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            bool esc = k != null && k.escapeKey.wasPressedThisFrame;
#else
            bool esc = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (!esc) return;
            if (popup != null && popup.IsOpen) popup.Close();
            else Back();
        }

        AegisScreen Find(AegisScreenId id)
        {
            if (screens != null) foreach (var s in screens) if (s != null && s.id == id) return s;
            return null;
        }

        public void Go(AegisScreenId target, bool push = true)
        {
            if (target == AegisScreenId.Back) { Back(); return; }
            if (target == AegisScreenId.Quit) { ConfirmQuit(); return; }
            if (target == AegisScreenId.None || target == _current) return;

            var next = Find(target);
            if (next == null) { Debug.LogWarning("[Aegis] Screen not found: " + target); return; }

            if (push && _current != AegisScreenId.None && _current != AegisScreenId.Splash) _history.Push(_current);
            var prev = Find(_current);
            if (prev != null) prev.Hide();
            next.Show();
            _current = target;
        }

        public void Back()
        {
            if (_history.Count == 0) { Go(AegisScreenId.MainMenu, false); return; }
            Go(_history.Pop(), false);
        }

        public void SelectMode(AegisGameMode mode)
        {
            AegisGameConfig.Mode = mode;
            switch (mode)
            {
                case AegisGameMode.VsAI: Go(AegisScreenId.AIDifficulty); break;
                case AegisGameMode.Learn: Go(AegisScreenId.Tutorial); break;
                default:
                    Toast(mode == AegisGameMode.AR ? "Chế độ AR: đang chuẩn bị camera..." : "Hai người: đang tạo ván đấu...", AegisToast.Kind.Info);
                    AegisGameConfig.RequestStart();
                    break;
            }
        }

        public void ConfirmQuit()
        {
            if (popup != null) popup.Show("Thông báo", "Bạn có muốn thoát trò chơi?", QuitNow);
            else QuitNow();
        }

        static void QuitNow()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public static void Toast(string message, AegisToast.Kind kind = AegisToast.Kind.Info)
        {
            if (Instance != null && Instance.toast != null) Instance.toast.Show(message, kind);
        }
    }
}
