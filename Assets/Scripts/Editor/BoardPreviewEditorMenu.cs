#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MechChess.Chess3D;

namespace MechChess.EditorTools
{
    public static class BoardPreviewEditorMenu
    {
        private const string PreviewRootName = "ChessBoard_EditorPreview";

        [MenuItem("Mech Chess/Preview 3D Board & Arena (Edit Mode)")]
        public static void GeneratePreviewInScene()
        {
            ClearPreview();

            GameObject previewGo = new GameObject(PreviewRootName);
            Undo.RegisterCreatedObjectUndo(previewGo, "Generate 3D Board Preview");

            var boardManager = previewGo.AddComponent<ChessBoard3DManager>();
            boardManager.BuildProceduralBoard();
            boardManager.SpawnAllPieces();

            // Frame camera on the board
            SceneView view = SceneView.lastActiveSceneView;
            if (view != null)
            {
                view.Frame(new Bounds(new Vector3(3.5f, 0.5f, 3.5f), new Vector3(12f, 8f, 12f)), false);
            }

            Debug.Log("[Mech Chess] 3D Chessboard & Spirit Blossom Arena preview generated in Scene View!");
        }

        [MenuItem("Mech Chess/Clear Board Preview")]
        public static void ClearPreview()
        {
            GameObject existing = GameObject.Find(PreviewRootName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            GameObject container = GameObject.Find("ChessBoard_WorldContainer");
            if (container != null && !Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(container);
            }

            Debug.Log("[Mech Chess] Cleaned up Board Preview objects from Scene.");
        }
    }
}
#endif
