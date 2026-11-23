#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AegisMech.Editor
{
    /// <summary>
    /// Tự động tắt tính năng ghi log hiệu năng (Performance Instrumentation) của Unity AI Assistant
    /// nhằm ngăn chặn lỗi Windows File Sharing Violation trên file 'Logs/assistant-perf.jsonl'.
    /// </summary>
    [InitializeOnLoad]
    public static class DisableAssistantPerf
    {
        private const string SettingKey = "Assistant.Perf.Instrumentation";

        static DisableAssistantPerf()
        {
            ApplyDisable();
        }

        [MenuItem("Tools/AegisMech/Fix Assistant Perf Error", false, 100)]
        public static void ApplyDisable()
        {
            string currentVal = EditorUserSettings.GetConfigValue(SettingKey);
            if (currentVal == bool.TrueString)
            {
                EditorUserSettings.SetConfigValue(SettingKey, bool.FalseString);
                Debug.Log("[AegisMech] Đã tự động tắt Assistant.Perf.Instrumentation để ngăn lỗi Sharing Violation trên assistant-perf.jsonl.");
            }
        }
    }
}
#endif
