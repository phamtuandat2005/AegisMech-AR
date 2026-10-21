using System;
using UnityEngine;
using UnityEngine.UI;

namespace AegisMech.UI
{
    /// <summary>Click-to-cycle selector (compact replacement for a dropdown). Optionally saved in PlayerPrefs.</summary>
    [RequireComponent(typeof(Button))]
    public class AegisChoice : MonoBehaviour
    {
        public string prefKey;
        public string[] options;
        public int defaultIndex;
        public Text label;
        public int index;
        public Action<int> onChanged;
        bool _wired;

        void OnEnable()
        {
            if (!_wired) { GetComponent<Button>().onClick.AddListener(Step); _wired = true; }
            if (!string.IsNullOrEmpty(prefKey)) index = AegisPrefs.GetInt(prefKey, defaultIndex);
            Refresh();
        }

        public void Step()
        {
            if (options == null || options.Length == 0) return;
            SetIndex((index + 1) % options.Length);
        }

        public void SetIndex(int i)
        {
            index = i;
            if (!string.IsNullOrEmpty(prefKey)) AegisPrefs.SetInt(prefKey, i);
            Refresh();
            if (onChanged != null) onChanged(i);
        }

        void Refresh()
        {
            if (label == null || options == null || options.Length == 0) return;
            index = Mathf.Clamp(index, 0, options.Length - 1);
            label.text = options[index];
        }
    }
}
