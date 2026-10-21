using UnityEngine;
using UnityEngine.UI;

namespace AegisMech.UI
{
    [RequireComponent(typeof(Toggle))]
    public class AegisSwitch : MonoBehaviour
    {
        public string prefKey;
        public bool defaultValue = true;
        public Image track;
        public RectTransform knob;
        public Color onColor = new Color(0.30f, 0.80f, 1f, 1f);
        public Color offColor = new Color(0.25f, 0.32f, 0.42f, 1f);
        Toggle _t;
        bool _wired;

        void OnEnable()
        {
            _t = GetComponent<Toggle>();
            if (!_wired) { _t.onValueChanged.AddListener(Apply); _wired = true; }
            bool v = string.IsNullOrEmpty(prefKey) ? _t.isOn : AegisPrefs.GetInt(prefKey, defaultValue ? 1 : 0) == 1;
            _t.SetIsOnWithoutNotify(v);
            Visual(v);
        }

        void Apply(bool v)
        {
            if (!string.IsNullOrEmpty(prefKey)) AegisPrefs.SetInt(prefKey, v ? 1 : 0);
            Visual(v);
        }

        void Visual(bool v)
        {
            if (track != null) track.color = v ? onColor : offColor;
            if (knob != null) knob.anchorMin = knob.anchorMax = new Vector2(v ? 0.75f : 0.25f, 0.5f);
        }
    }
}
