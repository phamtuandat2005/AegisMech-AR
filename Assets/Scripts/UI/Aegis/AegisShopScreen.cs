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
                new Item("board_royal", "Thánh Địa Anh Đào", "🌸", 0, "#FF9FC8"),
                new Item("board_classic", "Đấu Trường Hy Lạp", "🏛️", 800, "#C9A24B"),
                new Item("board_magma", "Địa Ngục Dung Nham", "🔥", 1200, "#FF4500"),
                new Item("board_crystal", "Cực Quang Pha Lê", "❄️", 1600, "#7FD6FF"),
            },
            new Item[]
            {
                new Item("avatar_mech", "Cỗ Máy", "M", 500, "#33C8FF"),
                new Item("avatar_commander", "Chỉ Huy", "C", 600, "#D8B450"),
                new Item("avatar_guardian", "Hộ Vệ AI", "G", 800, "#9B4DCA"),
                new Item("avatar_victory", "Hiệp Sĩ", "V", 1500, "#FFE45C"),
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
                if (_tab == 1 || it.id.StartsWith("avatar_"))
                {
                    for (int a = 0; a < MechChess.UI.AvatarCatalog.All.Length; a++)
                    {
                        if (MechChess.UI.AvatarCatalog.All[a].id == it.id)
                        {
                            MechChess.UI.AvatarCatalog.Equip(MechChess.UI.AvatarCatalog.All[a]);
                            break;
                        }
                    }
                }
                else if (_tab == 0)
                {
                    PlayerPrefs.SetString(MechChess.UI.MainMenu2D.PrefsEquippedBoard, it.name);
                    PlayerPrefs.Save();
                    MechChess.Chess3D.ChessBoard3DManager.Instance?.ApplyBoardTheme(it.name);
                }
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
            if (_tab == 1 || it.id.StartsWith("avatar_"))
            {
                for (int a = 0; a < MechChess.UI.AvatarCatalog.All.Length; a++)
                {
                    if (MechChess.UI.AvatarCatalog.All[a].id == it.id)
                    {
                        MechChess.UI.AvatarCatalog.Equip(MechChess.UI.AvatarCatalog.All[a]);
                        break;
                    }
                }
            }
            else if (_tab == 0)
            {
                PlayerPrefs.SetString(MechChess.UI.MainMenu2D.PrefsEquippedBoard, it.name);
                PlayerPrefs.Save();
                MechChess.Chess3D.ChessBoard3DManager.Instance?.ApplyBoardTheme(it.name);
            }
            Bind();
            AegisUIManager.Toast("Đã mua: " + it.name, AegisToast.Kind.Success);
        }
    }
}
