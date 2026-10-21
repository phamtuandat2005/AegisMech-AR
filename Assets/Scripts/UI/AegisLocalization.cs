using System;
using UnityEngine;

namespace MechChess.UI
{
    public enum AppLanguage
    {
        Vietnamese = 0,
        English = 1
    }

    /// <summary>
    /// AegisLocalization: Central bilingual localization manager (Tiếng Việt & English).
    /// Provides instant language toggling, PlayerPrefs persistence, and event-driven UI refresh.
    /// </summary>
    public static class AegisLocalization
    {
        private const string PrefsKey = "MECH_CHESS_LANG";

        public static AppLanguage CurrentLanguage { get; private set; } = AppLanguage.Vietnamese;

        public static event Action OnLanguageChanged;

        static AegisLocalization()
        {
            LoadSavedLanguage();
        }

        public static void LoadSavedLanguage()
        {
            string saved = PlayerPrefs.GetString(PrefsKey, "vi");
            CurrentLanguage = (saved == "en") ? AppLanguage.English : AppLanguage.Vietnamese;
        }

        public static void SetLanguage(AppLanguage newLang)
        {
            if (CurrentLanguage == newLang) return;
            CurrentLanguage = newLang;
            PlayerPrefs.SetString(PrefsKey, (newLang == AppLanguage.English) ? "en" : "vi");
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }

        public static bool IsVietnamese => CurrentLanguage == AppLanguage.Vietnamese;
        public static bool IsEnglish => CurrentLanguage == AppLanguage.English;

        /// <summary>
        /// Translates between Vietnamese and English inline.
        /// Example: AegisLocalization.T("Cài đặt", "Settings")
        /// </summary>
        public static string T(string vi, string en)
        {
            return (CurrentLanguage == AppLanguage.English) ? en : vi;
        }

        public static string GetCurrentLanguageName()
        {
            return (CurrentLanguage == AppLanguage.English) ? "English" : "Tiếng Việt";
        }

        public static int GetCurrentLanguageIndex()
        {
            return (CurrentLanguage == AppLanguage.English) ? 1 : 0;
        }
    }
}
