#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using MechChess.Chess3D;
using MechChess.UI;
using MechChess.Combat2D;
using MechChess.AR;
using MechChess.Cloud;
using MechChess.Mobile;
using MechChess.Multiplayer;

namespace MechChess.Editor
{
    /// <summary>
    /// Editor Script for 1-Click Mobile Build Configuration and Setup:
    /// Sets IL2CPP, ARM64, Min API Level 26, Camera + Internet permissions,
    /// and builds the production Android APK.
    /// </summary>
    public static class MobileBuildConfigurator
    {
        [MenuItem("MechChess/Configure Android Build", false, 1)]
        public static void ConfigureAndroidSettings()
        {
            Debug.Log("[MechChess Build] Configuring Android Player Settings...");

            // 1. Switch to Android Target
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            // 2. Set IL2CPP Scripting Backend
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            // 3. Set ARM64 64-bit Architecture
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // 4. Set Minimum API Level 26 (Android 8.0 Oreo)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // 5. Package Name & Orientation
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.aegismech.archess");
            PlayerSettings.productName = "3D Mech AR Chess";
            PlayerSettings.companyName = "AegisMech";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            // 6. Camera & Network Usage Descriptions
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.Android.ARCoreEnabled = true;

            AssetDatabase.SaveAssets();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Android Build Configured",
                    "Successfully configured:\n- Scripting Backend: IL2CPP\n- Architecture: ARM64\n- Minimum API Level: 26\n- Camera & Internet Permissions: Enabled",
                    "OK"
                );
            }

            Debug.Log("[MechChess Build] Android build settings successfully applied!");
        }

        [MenuItem("MechChess/Build Android APK (1-Click)", false, 2)]
        public static void BuildAndroidAPK()
        {
            ConfigureAndroidSettings();

            string buildDir = Path.Combine(Application.dataPath, "../Builds/Android");
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            string apkPath = Path.Combine(buildDir, "MechChessAR.apk");

            string[] scenes = new string[] { EditorBuildSettings.scenes.Length > 0 ? EditorBuildSettings.scenes[0].path : "Assets/Scenes/SampleScene.unity" };

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[MechChess Build] Starting Android APK build to: {apkPath}");
            var report = BuildPipeline.BuildPlayer(buildOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[MechChess Build] Build Succeeded: {apkPath} ({report.summary.totalSize / (1024 * 1024)} MB)");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Succeeded", $"APK generated successfully:\n{apkPath}\nTotal Size: {report.summary.totalSize / (1024 * 1024)} MB", "OK");
                }
            }
            else
            {
                Debug.LogError($"[MechChess Build] Build failed with result: {report.summary.result}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Build Result", $"Build ended with status: {report.summary.result}", "OK");
                }
            }
        }

        [MenuItem("MechChess/Setup Mech Chess Scene Hierarchy", false, 20)]
        public static void SetupSceneHierarchy()
        {
            // Find or create GameController
            GameObject controller = GameObject.Find("GameController");
            if (controller == null)
            {
                controller = new GameObject("GameController");
            }

            // Ensure all central systems are attached
            if (controller.GetComponent<MainMenu2D>() == null) controller.AddComponent<MainMenu2D>();
            if (controller.GetComponent<ChessBoard3DManager>() == null) controller.AddComponent<ChessBoard3DManager>();
            if (controller.GetComponent<UIManager2D>() == null) controller.AddComponent<UIManager2D>();
            if (controller.GetComponent<CombatMinigame2D>() == null) controller.AddComponent<CombatMinigame2D>();
            if (controller.GetComponent<HybridARController>() == null) controller.AddComponent<HybridARController>();
            if (controller.GetComponent<AdafruitIOCloudConnector>() == null) controller.AddComponent<AdafruitIOCloudConnector>();
            if (controller.GetComponent<UserAccountManager>() == null) controller.AddComponent<UserAccountManager>();
            if (controller.GetComponent<OnlineMultiplayerManager>() == null) controller.AddComponent<OnlineMultiplayerManager>();

            // Setup Main Camera Orbit
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<MobileTouchInput>() == null)
            {
                cam.gameObject.AddComponent<MobileTouchInput>();
            }

            Debug.Log("[MechChess Build] Scene setup complete: GameController and all 5 Subsystems linked.");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Scene Setup Complete", "GameController and all 5 Subsystems (3D Chess, 2D HUD, 2D Physics Combat, AR Controller, Adafruit IO, Mobile Touch) have been linked in the scene!", "OK");
            }
        }
    }
}
#endif
