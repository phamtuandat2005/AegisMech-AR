using UnityEngine;
using UnityEngine.UI;

namespace AegisMech.UI
{
    /// <summary>Attach to any Button: navigates to a screen (or selects a game mode) on click.</summary>
    [RequireComponent(typeof(Button))]
    public class AegisNavButton : MonoBehaviour
    {
        public AegisScreenId target = AegisScreenId.None;
        public bool setMode;
        public AegisGameMode mode;

        void Awake() { GetComponent<Button>().onClick.AddListener(OnClick); }

        void OnClick()
        {
            var m = AegisUIManager.Instance;
            if (m == null) return;
            if (setMode) m.SelectMode(mode);
            else m.Go(target);
        }
    }
}
