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
            // Clean up any temporary editor preview objects so the runtime bootstrapper initializes cleanly
            GameObject editorPreview = GameObject.Find("ChessBoard_EditorPreview");
            if (editorPreview != null)
            {
                Object.DestroyImmediate(editorPreview);
            }
            GameObject strayContainer = GameObject.Find("ChessBoard_WorldContainer");
            if (strayContainer != null && GameObject.Find("GameController") == null)
            {
                Object.DestroyImmediate(strayContainer);
            }

            if (GameObject.Find("GameController") != null)
            {
                Debug.Log("[MechChess Bootstrap] Game systems already present in scene.");
                return;
            }

            Debug.Log("[MechChess Bootstrap] Auto-initializing complete Mech Chess AR Architecture...");

            // 0. Ensure EventSystem with InputSystemUIInputModule exists for UI interactions
            if (EventSystem.current == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
                Object.DontDestroyOnLoad(esObj);
            }

            // 1. Create Core GameController
            GameObject controller = new GameObject("GameController");
            Object.DontDestroyOnLoad(controller);

            // 2. Attach Central Managers
            controller.AddComponent<MainMenu2D>();
            controller.AddComponent<ChessBoard3DManager>();
            controller.AddComponent<UIManager2D>();
            controller.AddComponent<CombatMinigame2D>();
            controller.AddComponent<HybridARController>();
            controller.AddComponent<AdafruitIOCloudConnector>();
            controller.AddComponent<MechChess.Multiplayer.UserAccountManager>();
            controller.AddComponent<MechChess.Multiplayer.OnlineMultiplayerManager>();

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

#if UNITY_EDITOR
            // In Unity Editor, launch directly into 3D match so developer immediately sees the board, mechs, and arena
            controller.AddComponent<EditorDirectMatchLauncher>();
#endif

            Debug.Log("[MechChess Bootstrap] All Subsystems and EventSystem successfully initialized!");
        }
    }

#if UNITY_EDITOR
    internal class EditorDirectMatchLauncher : MonoBehaviour
    {
        private System.Collections.IEnumerator Start()
        {
            yield return null; // Wait 1 frame for MainMenu2D and ChessBoard3DManager Start()

            var menu = GetComponent<MainMenu2D>();
            if (menu != null)
            {
                Debug.Log("[MechChess Bootstrap] Editor Auto-Launcher: Entering 3D match directly!");
                menu.LaunchGame(MainMenu2D.GameLaunchMode.VersusAI);
            }
        }
    }
#endif
}
