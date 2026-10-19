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
            if (ChessBoard3DManager.Instance != null)
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
            controller.AddComponent<ChessBoard3DManager>();
            controller.AddComponent<UIManager2D>();
            controller.AddComponent<CombatMinigame2D>();
            controller.AddComponent<HybridARController>();
            controller.AddComponent<AdafruitIOCloudConnector>();

            // 3. Configure Main Camera and attach Mobile Touch Orbit
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.04f, 0.06f, 0.10f);

                MobileTouchInput touchInput = mainCam.GetComponent<MobileTouchInput>();
                if (touchInput == null)
                {
                    touchInput = mainCam.gameObject.AddComponent<MobileTouchInput>();
                }
            }

            Debug.Log("[MechChess Bootstrap] All Subsystems and EventSystem successfully initialized!");
        }
    }
}
