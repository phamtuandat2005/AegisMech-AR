using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using MechChess.Chess3D;
using MechChess.UI;
using MechChess.Combat2D;
using MechChess.AR;
using MechChess.Cloud;
using MechChess.Mobile;

namespace MechChess
{
    /// <summary>
    /// Automatic Runtime Bootstrapper:
    /// Automatically provisions all game managers and systems upon hitting Play in Unity Editor
    /// or launching on mobile/PC build, ensuring zero manual scene wiring is required.
    /// </summary>
    public static class MechGameBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeGame()
        {
            // Clean up any temporary editor preview objects or pre-serialized scene board containers (including inactive ones)
            // so runtime initializes completely fresh with equipped skin (Classic Wood & Marble by default)
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                foreach (var root in activeScene.GetRootGameObjects())
                {
                    if (root == null) continue;
                    if (root.name == "ChessBoard_EditorPreview" || root.name == "ChessBoard_WorldContainer")
                    {
                        Object.DestroyImmediate(root);
                        continue;
                    }
                    var allTransforms = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in allTransforms)
                    {
                        if (t != null && t.gameObject != null && (t.name == "MechPiecesRoot" || t.name.EndsWith("_Mech")))
                        {
                            Object.DestroyImmediate(t.gameObject);
                        }
                    }
                }
            }

            // 0. Ensure EventSystem with InputSystemUIInputModule exists and remove legacy StandaloneInputModule
            EventSystem es = Object.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
                Object.DontDestroyOnLoad(esObj);
            }
            else
            {
                // Fix: Legacy StandaloneInputModule crashes with InvalidOperationException in Unity New Input System
                var legacyInput = es.GetComponent<StandaloneInputModule>();
                if (legacyInput != null)
                {
                    Object.DestroyImmediate(legacyInput);
                }
                if (es.GetComponent<InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
            }

            // 1. Find or create Core GameController
            GameObject controller = GameObject.Find("GameController");
            if (controller == null)
            {
                controller = new GameObject("GameController");
                Object.DontDestroyOnLoad(controller);
            }

#if UNITY_EDITOR
            // Clean up any missing/broken script references on GameController
            UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(controller);
#endif

            // 2. Attach Central Managers if not present
            if (controller.GetComponent<MainMenu2D>() == null) controller.AddComponent<MainMenu2D>();
            if (controller.GetComponent<ChessBoard3DManager>() == null) controller.AddComponent<ChessBoard3DManager>();
            if (controller.GetComponent<UIManager2D>() == null) controller.AddComponent<UIManager2D>();
            if (controller.GetComponent<CombatMinigame2D>() == null) controller.AddComponent<CombatMinigame2D>();
            if (controller.GetComponent<HybridARController>() == null) controller.AddComponent<HybridARController>();
            if (controller.GetComponent<AdafruitIOCloudConnector>() == null) controller.AddComponent<AdafruitIOCloudConnector>();
            if (controller.GetComponent<MechChess.Multiplayer.UserAccountManager>() == null) controller.AddComponent<MechChess.Multiplayer.UserAccountManager>();
            if (controller.GetComponent<MechChess.Multiplayer.OnlineMultiplayerManager>() == null) controller.AddComponent<MechChess.Multiplayer.OnlineMultiplayerManager>();
            if (controller.GetComponent<MechChess.Multiplayer.LichessBridge>() == null) controller.AddComponent<MechChess.Multiplayer.LichessBridge>();

            // 3. Configure Main Camera and attach Mobile Touch Orbit
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.18f, 0.56f, 0.64f); // Serene Spirit Blossom Celestial Sky

                MobileTouchInput touchInput = mainCam.GetComponent<MobileTouchInput>();
                if (touchInput == null)
                {
                    touchInput = mainCam.gameObject.AddComponent<MobileTouchInput>();
                }
            }

            // 4. Suppress URP runtime debug manager overlay UI
            try
            {
                UnityEngine.Rendering.DebugManager.instance.enableRuntimeUI = false;
            }
            catch { }

            Debug.Log("[MechChess Bootstrap] All Subsystems and EventSystem successfully initialized! Presenting Main Menu.");
        }
    }
}
