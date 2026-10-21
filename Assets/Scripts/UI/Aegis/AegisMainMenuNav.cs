using UnityEngine;
using UnityEngine.UI;

namespace AegisMech.UI
{
    /// <summary>
    /// Dynamic Main Menu Navigation for Aegis UI.
    /// Whichever of the 5 menu items is clicked becomes the active glowing blue chevron button,
    /// while the others remain transparent sub-rows.
    /// </summary>
    public class AegisMainMenuNav : MonoBehaviour
    {
        public Button[] buttons;
        public Image[] icons;
        public Image[] backgrounds;
        public RectTransform[] itemRts;
        public Text[] labels;
        public GameObject[] chevrons;
        public Sprite activeSprite;
        public int selectedIndex = 0;

        void Start()
        {
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    int idx = i;
                    if (buttons[i] != null)
                    {
                        buttons[i].onClick.AddListener(() => SelectItem(idx));
                    }
                }
            }
            SelectItem(selectedIndex, false);
        }

        public void SelectItem(int idx, bool triggerAction = true)
        {
            selectedIndex = idx;
            if (buttons == null) return;

            for (int i = 0; i < buttons.Length; i++)
            {
                bool active = (i == idx);
                if (backgrounds != null && i < backgrounds.Length && backgrounds[i] != null)
                {
                    backgrounds[i].sprite = active ? activeSprite : null;
                    backgrounds[i].color = active ? Color.white : Color.clear;
                }
                if (icons != null && i < icons.Length && icons[i] != null)
                {
                    icons[i].color = active ? Color.white : new Color(0.0f, 0.85f, 1.0f, 1.0f);
                    icons[i].rectTransform.sizeDelta = active ? new Vector2(58f, 58f) : new Vector2(48f, 48f);
                }
                if (itemRts != null && i < itemRts.Length && itemRts[i] != null)
                {
                    itemRts[i].sizeDelta = active ? new Vector2(440f, 105f) : new Vector2(380f, 68f);
                }
                if (labels != null && i < labels.Length && labels[i] != null)
                {
                    labels[i].fontSize = active ? 32 : 28;
                    labels[i].color = active ? Color.white : new Color(0.95f, 0.98f, 1.0f, 0.95f);
                }
                if (chevrons != null && i < chevrons.Length && chevrons[i] != null)
                {
                    chevrons[i].SetActive(active);
                }
            }

            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
        }
    }
}
