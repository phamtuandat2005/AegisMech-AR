using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MechChess.Multiplayer;

namespace MechChess.UI
{
    /// <summary>
    /// Avatar shop data: the list of avatars, ownership, the equipped avatar and round-cropped sprites.
    /// Every place that shows the local player's avatar calls AvatarCatalog.Bind(image) once and then
    /// updates automatically whenever the player equips a different avatar.
    /// </summary>
    public static class AvatarCatalog
    {
        public const string PrefsKey = "aegis.avatar";
        public const string DefaultFile = "profile_avatar_round.png";

        public class Entry
        {
            public string id, file, nameVi, nameEn;
            public int price;
            public Entry(string id, string file, string nameVi, string nameEn, int price)
            {
                this.id = id; this.file = file; this.nameVi = nameVi; this.nameEn = nameEn; this.price = price;
            }
            public string Name { get { return AegisLocalization.T(nameVi, nameEn); } }
        }

        // Portraits already shipped in Resources/Textures/ArcaneUI. Price 0 = free from the start.
        public static readonly Entry[] All = new Entry[]
        {
            new Entry("avatar_default",   "profile_avatar_round.png", "Mặc Định",            "Default",          0),
            new Entry("avatar_recruit",   "user_avatar.png",          "Tân Binh",             "Recruit",          0),
            new Entry("avatar_mech",      "profile_vs_ai_round.png",  "Cỗ Máy",               "Mech Unit",        500),
            new Entry("avatar_commander", "commander_portrait.png",   "Chỉ Huy",              "Commander",        600),
            new Entry("avatar_guardian",  "ai_portrait_full.png",     "Hộ Vệ AI",             "AI Guardian",      800),
            new Entry("avatar_light",     "skin_knight_light.png",    "Hiệp Sĩ Ánh Sáng",     "Knight of Light",  900),
            new Entry("avatar_dark",      "skin_dark_legion.png",     "Quân Đoàn Bóng Tối",   "Dark Legion",      900),
            new Entry("avatar_paladin",   "skin_cyber_future.png",    "Thánh Kỵ Arcane",      "Arcane Paladin",   1200),
            new Entry("avatar_forest",    "skin_forest_spirit.png",   "Tinh Linh Rừng",       "Forest Spirit",    1200),
            new Entry("avatar_victory",   "victory_knight.png",       "Hiệp Sĩ Chiến Thắng",  "Victory Knight",   1500),
        };

        public static event Action Changed;

        public static string EquippedFile
        {
            get { return PlayerPrefs.GetString(PrefsKey, DefaultFile); }
        }

        public static Entry Find(string file)
        {
            for (int i = 0; i < All.Length; i++) if (All[i].file == file) return All[i];
            return null;
        }

        public static bool IsOwned(Entry e)
        {
            return e.price <= 0 || AegisMech.UI.AegisPrefs.Owns(e.id);
        }

        public static bool IsEquipped(Entry e)
        {
            return EquippedFile == e.file;
        }

        public static void Equip(Entry e)
        {
            if (e == null || !IsOwned(e)) return;
            PlayerPrefs.SetString(PrefsKey, e.file);
            PlayerPrefs.Save();
            if (UserAccountManager.Instance != null && UserAccountManager.Instance.CurrentProfile != null)
                UserAccountManager.Instance.CurrentProfile.avatar_url = e.file;
            if (Changed != null) Changed();
        }

        // ------------------------------------------------------------------
        // Round sprites
        // ------------------------------------------------------------------
        private static readonly Dictionary<string, Sprite> _round = new Dictionary<string, Sprite>();

        /// <summary>Square centre crop of the portrait with a circular alpha edge (cached).</summary>
        public static Sprite GetRoundSprite(string file)
        {
            if (string.IsNullOrEmpty(file)) file = DefaultFile;
            Sprite cached;
            if (_round.TryGetValue(file, out cached) && cached != null) return cached;

            Sprite src = ArcaneAssetLoader.LoadArcaneSprite(file);
            if (src == null) return ArcaneAssetLoader.LoadArcaneSprite(DefaultFile);
            if (file == DefaultFile) { _round[file] = src; return src; }

            try
            {
                Texture2D t = src.texture;
                Rect r = src.textureRect;
                RenderTexture rt = RenderTexture.GetTemporary(t.width, t.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(t, rt);
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;
                Texture2D full = new Texture2D(t.width, t.height, TextureFormat.RGBA32, false);
                full.ReadPixels(new Rect(0, 0, t.width, t.height), 0, 0);
                full.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);

                const int N = 256;
                float side = Mathf.Min(r.width, r.height);
                float x0 = r.x + (r.width - side) * 0.5f;
                float y0 = r.y + (r.height - side) * 0.5f;
                Texture2D outT = new Texture2D(N, N, TextureFormat.RGBA32, false);
                Color[] px = new Color[N * N];
                for (int y = 0; y < N; y++)
                {
                    for (int x = 0; x < N; x++)
                    {
                        float u = (x + 0.5f) / N, v = (y + 0.5f) / N;
                        Color c = full.GetPixelBilinear((x0 + u * side) / t.width, (y0 + v * side) / t.height);
                        float dx = (x + 0.5f) - N * 0.5f, dy = (y + 0.5f) - N * 0.5f;
                        float edge = N * 0.5f - Mathf.Sqrt(dx * dx + dy * dy);
                        c.a *= Mathf.Clamp01(edge / 1.5f);
                        px[y * N + x] = c;
                    }
                }
                outT.SetPixels(px);
                outT.wrapMode = TextureWrapMode.Clamp;
                outT.filterMode = FilterMode.Bilinear;
                outT.Apply();
                UnityEngine.Object.Destroy(full);

                Sprite s = Sprite.Create(outT, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
                _round[file] = s;
                return s;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AvatarCatalog] Không cắt tròn được ảnh " + file + ": " + ex.Message);
                return src;
            }
        }

        public static Sprite GetEquippedSprite()
        {
            return GetRoundSprite(EquippedFile);
        }

        /// <summary>Makes an Image always show the equipped avatar (updates live when the player equips another).</summary>
        public static void Bind(Image img)
        {
            if (img == null) return;
            AvatarBinder b = img.GetComponent<AvatarBinder>();
            if (b == null) b = img.gameObject.AddComponent<AvatarBinder>();
            b.Refresh();
        }
    }

    public class AvatarBinder : MonoBehaviour
    {
        private Image _img;

        private void Awake() { _img = GetComponent<Image>(); }
        private void OnEnable() { AvatarCatalog.Changed += Refresh; Refresh(); }
        private void OnDisable() { AvatarCatalog.Changed -= Refresh; }

        public void Refresh()
        {
            if (_img == null) _img = GetComponent<Image>();
            if (_img == null) return;
            Sprite s = AvatarCatalog.GetEquippedSprite();
            if (s != null) { _img.sprite = s; _img.color = Color.white; _img.preserveAspect = true; }
        }
    }
}
