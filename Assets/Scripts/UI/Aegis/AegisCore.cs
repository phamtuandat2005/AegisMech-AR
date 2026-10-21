using System;
using UnityEngine;

namespace AegisMech.UI
{
    public enum AegisScreenId { None, Splash, MainMenu, ModeSelect, AIDifficulty, Settings, Tutorial, Shop, Back, Quit }
    public enum AegisGameMode { VsAI, TwoPlayer, AR, Learn }

    public static class AegisTheme
    {
        public const string GameTitle = "CHESS 3D AR";
        public const string Tagline = "STRATEGY BEYOND REALITY";
        public static readonly Color Cyan = new Color(0.30f, 0.80f, 1f, 1f);
        public static readonly Color CyanBright = new Color(0.55f, 0.92f, 1f, 1f);
        public static readonly Color Gold = new Color(1f, 0.80f, 0.35f, 1f);
        public static readonly Color Ink = new Color(0.92f, 0.97f, 1f, 1f);
        public static readonly Color Muted = new Color(0.58f, 0.70f, 0.82f, 1f);
        public static readonly Color Success = new Color(0.30f, 0.90f, 0.50f, 1f);
        public static readonly Color Danger = new Color(1f, 0.32f, 0.38f, 1f);
        public static readonly Color Navy = new Color(0.02f, 0.05f, 0.10f, 1f);

        static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }
    }

    public static class AegisPrefs
    {
        const string P = "aegis.";
        public static event Action CoinsChanged;

        public static int GetInt(string key, int def) { return PlayerPrefs.GetInt(P + key, def); }
        public static void SetInt(string key, int v) { PlayerPrefs.SetInt(P + key, v); PlayerPrefs.Save(); }

        public static int Coins
        {
            get { return GetInt("coins", 1250); }
            set { SetInt("coins", Mathf.Max(0, value)); if (CoinsChanged != null) CoinsChanged(); }
        }

        public static bool Owns(string id)
        {
            return ("," + PlayerPrefs.GetString(P + "owned", "") + ",").Contains("," + id + ",");
        }

        public static void Own(string id)
        {
            if (Owns(id)) return;
            string s = PlayerPrefs.GetString(P + "owned", "");
            PlayerPrefs.SetString(P + "owned", string.IsNullOrEmpty(s) ? id : s + "," + id);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Selections made in the menu flow. The in-game scene reads these.</summary>
    public static class AegisGameConfig
    {
        public static AegisGameMode Mode = AegisGameMode.VsAI;
        public static int AiLevel = 1;                       // 0 Easy, 1 Medium, 2 Hard, 3 Expert
        public static readonly int[] AiElo = { 800, 1200, 1600, 2000 };
        public static int TimeMinutes = 10;                  // 0 = unlimited
        public static bool PlayerIsWhite = true;

        /// <summary>Raised when the menu wants to start a match. Hook your game scene loader here.</summary>
        public static event Action StartRequested;

        public static void RequestStart()
        {
            Debug.Log("[Aegis] Start match: mode=" + Mode + " aiLevel=" + AiLevel + " time=" + TimeMinutes + " white=" + PlayerIsWhite);
            if (StartRequested != null) StartRequested();
        }
    }
}
