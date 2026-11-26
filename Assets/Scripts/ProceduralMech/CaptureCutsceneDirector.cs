using System;
using UnityEngine;

namespace MechChess.ProceduralMech
{
    /// <summary>
    /// Capture cutscenes are disabled in favor of smooth, instantaneous in-place 3D board combat animations.
    /// </summary>
    public class CaptureCutsceneDirector : MonoBehaviour
    {
        public const string PrefKey = "Aegis_CaptureCutscene";

        public static bool Enabled
        {
            get => false;
            set { PlayerPrefs.SetInt(PrefKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool IsPlaying => false;

        public static bool TryPlay(object attacker, object defender, Action onImpact, Action onComplete)
        {
            return false;
        }

        public static void SkipActive()
        {
        }
    }
}
