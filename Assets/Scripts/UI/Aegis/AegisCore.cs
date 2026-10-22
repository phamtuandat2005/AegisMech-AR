using System;
using UnityEngine;

namespace AegisMech.UI
{
    public enum AegisScreenId { None, Splash, MainMenu, ModeSelect, AIDifficulty, Settings, Tutorial, Shop, Profile, Notification, AR, Pause, Result, Back, Quit }
    public enum AegisGameMode { VsAI, TwoPlayer, AR, Learn }

    public static class AegisTheme
    {
        public const string GameTitle = "CHESS 3D AR";
        public const string Tagline = "DARK FANTASY MEDIEVAL CHESS";
        public static readonly Color Cyan = new Color(0.725f, 0.604f, 0.380f, 1f);        // #B99A61 Antique Gold
        public static readonly Color CyanBright = new Color(0.92f, 0.80f, 0.45f, 1f);     // Radiant Antique Gold
        public static readonly Color Gold = new Color(0.725f, 0.604f, 0.380f, 1f);       // Imperial Gold
        public static readonly Color Ink = new Color(0.96f, 0.94f, 0.88f, 1f);            // #F5EEDB Warm Ivory Text
        public static readonly Color Muted = new Color(0.851f, 0.784f, 0.635f, 1f);       // #D9C8A2 Aged Parchment
        public static readonly Color Success = new Color(0.322f, 0.427f, 0.341f, 1f);     // #526D57 Magic Green
        public static readonly Color Danger = new Color(0.490f, 0.224f, 0.259f, 1f);      // #7D3942 Blood Red
        public static readonly Color Navy = new Color(0.090f, 0.137f, 0.176f, 1f);        // #17232D Midnight Slate

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
