using System;
using UnityEngine;
using UnityEngine.UI;

namespace AegisMech.UI
{
    public class AegisTabs : MonoBehaviour
    {
        public Button[] tabs;
        public GameObject[] panels;
        public int selected;
        public Sprite onSprite, offSprite;
        public Color onText = Color.white;
        public Color offText = new Color(0.58f, 0.70f, 0.82f, 1f);
        public Action<int> onChanged;
        bool _wired;

        void Awake() { Select(selected, false); }

        void Wire()
        {
            if (_wired) return;
            _wired = true;
            for (int i = 0; i < tabs.Length; i++)
            {
                int c = i;
                tabs[i].onClick.AddListener(() => Select(c, true));
            }
        }

        public void Select(int index, bool notify = true)
        {
            Wire();
            selected = index;
            for (int k = 0; k < tabs.Length; k++)
            {
                bool on = k == index;
                var img = tabs[k].targetGraphic as Image;
                if (img != null) img.sprite = on ? onSprite : offSprite;
                var t = tabs[k].GetComponentInChildren<Text>();
                if (t != null) t.color = on ? onText : offText;
                if (panels != null && k < panels.Length && panels[k] != null) panels[k].SetActive(on);
            }
            if (notify && onChanged != null) onChanged(index);
        }
    }
}
