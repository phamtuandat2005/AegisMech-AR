using System;
using UnityEngine;
using UnityEngine.UI;

namespace AegisMech.UI
{
    public class AegisShopScreen : AegisScreen
    {
        [Serializable]
        public class Card
        {
            public Button button;
            public Image art;
            public Text glyph, nameText, priceText;
            public Image coinDot;
        }

        public Card[] cards;
        public AegisTabs tabs;
        public Text coinText;

        int _tab;
        bool _wired;

        struct Item
        {
            public string id, name, glyph;
            public int price;
            public Color color;
            public Item(string i, string n, string g, int p, string hex)
            {
                id = i; name = n; glyph = g; price = p;
                Color c;
                color = ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.white;
            }
        }

        static readonly Item[][] Items = new Item[][]
        {
            new Item[]
            {
                new Item("skin_light", "Knight of Light", "K", 1000, "#D8B450"),
                new Item("skin_dark", "Dark Legion", "D", 1000, "#9B4DCA"),
                new Item("skin_cyber", "Arcane Paladin", "A", 1000, "#33C8FF"),
                new Item("skin_forest", "Forest Spirit", "F", 1000, "#3DBE6A"),
            },
            new Item[]
            {
                new Item("board_royal", "Hoàng Cung", "H", 800, "#C9A24B"),
                new Item("board_ice", "Pha Lê Băng", "P", 1200, "#7FD6FF"),
                new Item("board_neon", "Thành Trì Cổ", "T", 1500, "#B99A61"),
                new Item("board_wood", "Rừng Cổ Thụ", "R", 900, "#6B8F3E"),
            },
            new Item[]
            {
                new Item("fx_lightning", "Tia Sét", "S", 500, "#FFE45C"),
                new Item("fx_fire", "Lửa Thiêng", "L", 600, "#FF7A3D"),
                new Item("fx_star", "Sao Băng", "B", 700, "#8FA8FF"),
                new Item("fx_petal", "Cánh Hoa", "C", 400, "#FF9FC8"),
            },
        };

        void OnEnable() { AegisPrefs.CoinsChanged += UpdateCoins; }
        void OnDisable() { AegisPrefs.CoinsChanged -= UpdateCoins; }

        public override void OnShown()
        {
            if (!_wired) { Wire(); _wired = true; }
            _tab = 0;
            tabs.Select(0, false);
            Bind();
            UpdateCoins();
        }

        void Wire()
        {
            tabs.onChanged = i => { _tab = i; Bind(); };
            for (int i = 0; i < cards.Length; i++)
            {
                int c = i;
                cards[i].button.onClick.AddListener(() => OnCard(c));
            }
        }

        void UpdateCoins() { if (coinText != null) coinText.text = AegisPrefs.Coins.ToString(); }

        void Bind()
        {
            var items = Items[_tab];
            for (int i = 0; i < cards.Length && i < items.Length; i++)
            {
                var it = items[i];
                var c = cards[i];
                bool own = AegisPrefs.Owns(it.id);
                c.art.color = Color.Lerp(it.color, new Color(0.03f, 0.06f, 0.12f, 1f), 0.55f);
                c.glyph.text = it.glyph;
                c.glyph.color = new Color(it.color.r, it.color.g, it.color.b, 0.9f);
                c.nameText.text = it.name;
                c.priceText.text = own ? "Đã sở hữu" : it.price.ToString();
                c.coinDot.enabled = !own;
            }
        }

        void OnCard(int i)
        {
            var it = Items[_tab][i];
            if (AegisPrefs.Owns(it.id))
            {
                AegisUIManager.Toast("Đã trang bị: " + it.name, AegisToast.Kind.Info);
                return;
            }
            if (AegisPrefs.Coins < it.price)
            {
                AegisUIManager.Toast("Không đủ xu để mua vật phẩm này", AegisToast.Kind.Error);
                return;
            }
            var m = AegisUIManager.Instance;
            if (m == null || m.popup == null) return;
            m.popup.Show("Xác nhận mua", "Mua " + it.name + " với " + it.price + " xu?", () => Buy(it));
        }

        void Buy(Item it)
        {
            AegisPrefs.Coins = AegisPrefs.Coins - it.price;
            AegisPrefs.Own(it.id);
            Bind();
            AegisUIManager.Toast("Đã mua: " + it.name, AegisToast.Kind.Success);
        }
    }
}
