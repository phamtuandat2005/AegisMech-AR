using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MechChess.ProceduralMech;
using MechChess.Combat2D;
using MechChess.UI;
using MechChess.Cloud;

namespace MechChess.Chess3D
{
    /// <summary>
    /// Central 3D Chessboard Manager:
    /// - Procedurally generates 8x8 neon sci-fi chessboard.
    /// - Spawns 32 procedural 3D mech pieces.
    /// - Implements complete chess move verification, check/checkmate detection, and promotion.
    /// - Highlights valid move tiles in glowing green.
    /// - Integrates with 2D Combat Minigame on capture and Adafruit IO Cloud telemetry.
    /// </summary>
    public class ChessBoard3DManager : MonoBehaviour
    {
        public static ChessBoard3DManager Instance { get; private set; }

        [Header("Board Config")]
        public float tileSize = 1.0f;
        public float tileHeight = 0.15f;

        // 8x8 Board Data
        public ChessPiece3D[,] boardPieces = new ChessPiece3D[8, 8];
        private GameObject[,] _tileObjects = new GameObject[8, 8];
        private Renderer[,] _tileRenderers = new Renderer[8, 8];
        private Material _lightTileMat;
        private Material _darkTileMat;
        private Material _highlightTileMat;
        private Material _borderMat;

        [Header("Gameplay State")]
        public PieceColor currentTurn = PieceColor.White;
        public int turnCounter = 1;
        public bool isGameOver = false;

        private ChessPiece3D _selectedPiece = null;
        private List<Vector2Int> _validMoves = new List<Vector2Int>();

        private Transform _boardRoot;
        private Transform _piecesRoot;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializeBoardMaterials();
        }

        private void Start()
        {
            BuildProceduralBoard();
            SpawnAllPieces();

            // Link UI Manager Callbacks
            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.OnResetGameClicked += ResetBoard;
                UIManager2D.Instance.OnTriggerAIClicked += TriggerAIMove;
            }
        }

        private void InitializeBoardMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");

            // Light Tile: Cyber Silver Plate with Cyan glow border
            _lightTileMat = new Material(shader) { name = "Tile_LightCyber" };
            SetMatColor(_lightTileMat, new Color(0.85f, 0.90f, 0.95f), 0.7f, 0.5f);

            // Dark Tile: Obsidian Carbon Plate with subtle dark reflection
            _darkTileMat = new Material(shader) { name = "Tile_DarkCarbon" };
            SetMatColor(_darkTileMat, new Color(0.12f, 0.14f, 0.18f), 0.85f, 0.4f);

            // Highlight Tile: Radiant Neon Green
            _highlightTileMat = new Material(shader) { name = "Tile_HighlightGreen" };
            SetMatColor(_highlightTileMat, new Color(0.1f, 1.0f, 0.3f), 0.2f, 0.8f);
            _highlightTileMat.EnableKeyword("_EMISSION");
            if (_highlightTileMat.HasProperty("_EmissionColor"))
                _highlightTileMat.SetColor("_EmissionColor", new Color(0.1f, 1.5f, 0.3f) * 2.5f);

            // Board Perimeter Border: Gunmetal with neon cyan border
            _borderMat = new Material(shader) { name = "Board_BorderMat" };
            SetMatColor(_borderMat, new Color(0.08f, 0.10f, 0.12f), 0.9f, 0.3f);
        }

        private void SetMatColor(Material mat, Color col, float metallic, float smoothness)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        }

        /// <summary>
        /// Generates the 8x8 procedural chessboard grid and neon frame.
        /// </summary>
        public void BuildProceduralBoard()
        {
            if (_boardRoot != null) Destroy(_boardRoot.gameObject);

            _boardRoot = new GameObject("ProceduralChessBoard_3D").transform;
            _boardRoot.SetParent(transform, false);

            // Outer Arena Platform Pedestal
            GameObject arenaPedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arenaPedestal.name = "CyberArenaPedestal";
            arenaPedestal.transform.SetParent(_boardRoot, false);
            arenaPedestal.transform.localPosition = new Vector3(3.5f, -0.45f, 3.5f);
            arenaPedestal.transform.localScale = new Vector3(14.0f, 0.6f, 14.0f);
            arenaPedestal.GetComponent<Renderer>().sharedMaterial = _borderMat;

            // Outer Frame
            float totalSize = 8 * tileSize;
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "BoardPerimeterFrame";
            frame.transform.SetParent(_boardRoot, false);
            frame.transform.localPosition = new Vector3(totalSize / 2f - (tileSize / 2f), -tileHeight / 2f, totalSize / 2f - (tileSize / 2f));
            frame.transform.localScale = new Vector3(totalSize + 0.8f, tileHeight, totalSize + 0.8f);
            frame.GetComponent<Renderer>().sharedMaterial = _borderMat;

            // Dedicated Cinematic Arena Key Light
            GameObject keyLightObj = new GameObject("ArenaKeyLight");
            keyLightObj.transform.SetParent(_boardRoot, false);
            Light keyLight = keyLightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1.0f, 0.98f, 0.95f);
            keyLight.intensity = 1.6f;
            keyLightObj.transform.localRotation = Quaternion.Euler(55f, -30f, 0);

            // Dedicated Overhead Fill Point Light
            GameObject fillLightObj = new GameObject("ArenaOverheadFill");
            fillLightObj.transform.SetParent(_boardRoot, false);
            fillLightObj.transform.localPosition = new Vector3(3.5f, 6.0f, 3.5f);
            Light fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.6f, 0.9f, 1.0f);
            fillLight.intensity = 2.0f;
            fillLight.range = 25f;

            // 64 Tiles
            for (int file = 0; file < 8; file++)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    bool isLight = (file + rank) % 2 == 1;
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile_{GetAlgebraic(file, rank)}";
                    tile.transform.SetParent(_boardRoot, false);
                    tile.transform.localPosition = new Vector3(file * tileSize, 0, rank * tileSize);
                    tile.transform.localScale = new Vector3(tileSize * 0.96f, tileHeight, tileSize * 0.96f);

                    Renderer rend = tile.GetComponent<Renderer>();
                    rend.sharedMaterial = isLight ? _lightTileMat : _darkTileMat;

                    // Tile Raycast Trigger
                    BoxCollider col = tile.GetComponent<BoxCollider>();
                    if (col != null) col.isTrigger = false;

                    _tileObjects[file, rank] = tile;
                    _tileRenderers[file, rank] = rend;

                    // Click receiver
                    var tileClick = tile.AddComponent<TileClickReceiver>();
                    tileClick.file = file;
                    tileClick.rank = rank;
                    tileClick.OnTileClicked = HandleTileClicked;
                }
            }
        }

        /// <summary>
        /// Spawns standard 32 Mech pieces using ProceduralMechFactory.
        /// </summary>
        public void SpawnAllPieces()
        {
            if (_piecesRoot != null) Destroy(_piecesRoot.gameObject);
            _piecesRoot = new GameObject("MechPiecesRoot").transform;
            _piecesRoot.SetParent(transform, false);

            Array.Clear(boardPieces, 0, boardPieces.Length);

            // Setup White (Ranks 0 & 1)
            SetupBackRank(PieceColor.White, 0);
            for (int f = 0; f < 8; f++) SpawnPiece(PieceType.Pawn, PieceColor.White, f, 1);

            // Setup Black (Ranks 7 & 6)
            SetupBackRank(PieceColor.Black, 7);
            for (int f = 0; f < 8; f++) SpawnPiece(PieceType.Pawn, PieceColor.Black, f, 6);
        }

        private void SetupBackRank(PieceColor color, int rank)
        {
            SpawnPiece(PieceType.Rook,   color, 0, rank);
            SpawnPiece(PieceType.Knight, color, 1, rank);
            SpawnPiece(PieceType.Bishop, color, 2, rank);
            SpawnPiece(PieceType.Queen,  color, 3, rank);
            SpawnPiece(PieceType.King,   color, 4, rank);
            SpawnPiece(PieceType.Bishop, color, 5, rank);
            SpawnPiece(PieceType.Knight, color, 6, rank);
            SpawnPiece(PieceType.Rook,   color, 7, rank);
        }

        private void SpawnPiece(PieceType type, PieceColor color, int file, int rank)
        {
            GameObject pieceObj = ProceduralMechFactory.CreateMechPiece(type, color, _piecesRoot);
            pieceObj.transform.localPosition = GetWorldPosition(file, rank);

            // Rotate Black pieces 180 degrees to face White
            if (color == PieceColor.Black)
            {
                pieceObj.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            }

            ChessPiece3D piece = pieceObj.AddComponent<ChessPiece3D>();
            piece.Setup(type, color, file, rank);
            boardPieces[file, rank] = piece;
        }

        public Vector3 GetWorldPosition(int file, int rank)
        {
            return new Vector3(file * tileSize, tileHeight * 0.5f, rank * tileSize);
        }

        // =========================================================================
        // Selection & Move Logic
        // =========================================================================
        public void HandlePieceClicked(ChessPiece3D piece)
        {
            if (isGameOver || piece == null) return;

            // If clicking on own piece -> select it
            if (piece.pieceColor == currentTurn)
            {
                SelectPiece(piece);
                return;
            }

            // If a piece is already selected and clicked on an enemy piece in valid moves -> capture!
            if (_selectedPiece != null && _validMoves.Contains(new Vector2Int(piece.file, piece.rank)))
            {
                ExecuteMove(_selectedPiece, piece.file, piece.rank);
            }
        }

        public void SelectPiece(ChessPiece3D piece)
        {
            if (isGameOver || piece == null) return;
            if (piece.pieceColor != currentTurn) return;

            ClearHighlights();
            _selectedPiece = piece;
            _validMoves = GetLegalMoves(piece);

            // Highlight selected piece square and all valid move squares
            HighlightTile(piece.file, piece.rank);
            foreach (var move in _validMoves)
            {
                HighlightTile(move.x, move.y);
            }
        }

        private void HandleTileClicked(int file, int rank)
        {
            if (isGameOver) return;

            // If clicking on own piece -> select it
            ChessPiece3D clickedPiece = boardPieces[file, rank];
            if (clickedPiece != null && clickedPiece.pieceColor == currentTurn)
            {
                SelectPiece(clickedPiece);
                return;
            }

            // If a piece is already selected and target tile is in valid moves -> execute move
            if (_selectedPiece != null && _validMoves.Contains(new Vector2Int(file, rank)))
            {
                ExecuteMove(_selectedPiece, file, rank);
            }
            else
            {
                ClearHighlights();
                _selectedPiece = null;
            }
        }

        public void ExecuteMove(ChessPiece3D piece, int targetFile, int targetRank)
        {
            ClearHighlights();
            int startFile = piece.file;
            int startRank = piece.rank;
            ChessPiece3D captured = boardPieces[targetFile, targetRank];

            string moveNotation = $"{piece.pieceColor} {piece.pieceType}: {GetAlgebraic(startFile, startRank)} -> {GetAlgebraic(targetFile, targetRank)}";

            if (captured != null)
            {
                // Trigger 2D Minigame before finalizing capture!
                if (CombatMinigame2D.Instance != null)
                {
                    CombatMinigame2D.Instance.StartMinigame((success) =>
                    {
                        FinalizeCaptureAndMove(piece, targetFile, targetRank, captured, moveNotation);
                    });
                }
                else
                {
                    FinalizeCaptureAndMove(piece, targetFile, targetRank, captured, moveNotation);
                }
            }
            else
            {
                FinalizeMove(piece, targetFile, targetRank, moveNotation);
            }
        }

        private void FinalizeCaptureAndMove(ChessPiece3D piece, int targetFile, int targetRank, ChessPiece3D captured, string notation)
        {
            boardPieces[targetFile, targetRank] = null;
            if (captured.Animator != null)
            {
                captured.Animator.PlayCaptureDestruction();
            }
            else
            {
                Destroy(captured.gameObject);
            }

            FinalizeMove(piece, targetFile, targetRank, notation + " [CAPTURED]");
        }

        private void FinalizeMove(ChessPiece3D piece, int targetFile, int targetRank, string notation)
        {
            boardPieces[piece.file, piece.rank] = null;
            boardPieces[targetFile, targetRank] = piece;
            piece.file = targetFile;
            piece.rank = targetRank;
            piece.hasMoved = true;

            // Pawn Promotion to Queen if reached final rank
            if (piece.pieceType == PieceType.Pawn && (targetRank == 7 || targetRank == 0))
            {
                PromotePawn(piece);
            }

            // Animate piece movement in 3D
            Vector3 targetPos = GetWorldPosition(targetFile, targetRank);
            if (piece.Animator != null)
            {
                piece.Animator.AnimateMoveTo(targetPos, 0.45f);
            }
            else
            {
                piece.transform.localPosition = targetPos;
            }

            _selectedPiece = null;

            // Log to HUD
            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.RecordMove(notation);
            }

            // Upstream Adafruit IO Telemetry
            string statusStr = "Playing";
            PieceColor opponent = (currentTurn == PieceColor.White) ? PieceColor.Black : PieceColor.White;

            if (IsInCheck(opponent))
            {
                statusStr = IsCheckmate(opponent) ? "Checkmate" : "Check";
                if (statusStr == "Checkmate") isGameOver = true;
            }

            if (AdafruitIOCloudConnector.Instance != null)
            {
                AdafruitIOCloudConnector.Instance.PublishTelemetry(turnCounter, 0.95f, statusStr, notation);
            }

            // Switch Turns
            turnCounter++;
            currentTurn = opponent;
            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.SetActiveTurn(currentTurn);
            }
        }

        private void PromotePawn(ChessPiece3D pawn)
        {
            int f = pawn.file;
            int r = pawn.rank;
            PieceColor col = pawn.pieceColor;

            Destroy(pawn.gameObject);
            SpawnPiece(PieceType.Queen, col, f, r);
        }

        // =========================================================================
        // Legal Move Generation (Standard Chess Rules)
        // =========================================================================
        public List<Vector2Int> GetLegalMoves(ChessPiece3D piece)
        {
            List<Vector2Int> candidates = new List<Vector2Int>();
            int f = piece.file;
            int r = piece.rank;
            bool isWhite = (piece.pieceColor == PieceColor.White);

            switch (piece.pieceType)
            {
                case PieceType.Pawn:
                    int dir = isWhite ? 1 : -1;
                    int startRank = isWhite ? 1 : 6;
                    // Forward 1
                    if (IsOnBoard(f, r + dir) && boardPieces[f, r + dir] == null)
                    {
                        candidates.Add(new Vector2Int(f, r + dir));
                        // Forward 2
                        if (r == startRank && boardPieces[f, r + 2 * dir] == null)
                        {
                            candidates.Add(new Vector2Int(f, r + 2 * dir));
                        }
                    }
                    // Diagonal Captures
                    int[] sideDirs = { -1, 1 };
                    foreach (int df in sideDirs)
                    {
                        int targetF = f + df;
                        int targetR = r + dir;
                        if (IsOnBoard(targetF, targetR))
                        {
                            ChessPiece3D target = boardPieces[targetF, targetR];
                            if (target != null && target.pieceColor != piece.pieceColor)
                            {
                                candidates.Add(new Vector2Int(targetF, targetR));
                            }
                        }
                    }
                    break;

                case PieceType.Knight:
                    int[,] knightOffsets = { {-2,-1}, {-2,1}, {-1,-2}, {-1,2}, {1,-2}, {1,2}, {2,-1}, {2,1} };
                    for (int i = 0; i < 8; i++)
                    {
                        int nf = f + knightOffsets[i, 0];
                        int nr = r + knightOffsets[i, 1];
                        if (IsOnBoard(nf, nr))
                        {
                            if (boardPieces[nf, nr] == null || boardPieces[nf, nr].pieceColor != piece.pieceColor)
                                candidates.Add(new Vector2Int(nf, nr));
                        }
                    }
                    break;

                case PieceType.Bishop:
                    AddRayMoves(candidates, piece, 1, 1);
                    AddRayMoves(candidates, piece, 1, -1);
                    AddRayMoves(candidates, piece, -1, 1);
                    AddRayMoves(candidates, piece, -1, -1);
                    break;

                case PieceType.Rook:
                    AddRayMoves(candidates, piece, 1, 0);
                    AddRayMoves(candidates, piece, -1, 0);
                    AddRayMoves(candidates, piece, 0, 1);
                    AddRayMoves(candidates, piece, 0, -1);
                    break;

                case PieceType.Queen:
                    AddRayMoves(candidates, piece, 1, 0);
                    AddRayMoves(candidates, piece, -1, 0);
                    AddRayMoves(candidates, piece, 0, 1);
                    AddRayMoves(candidates, piece, 0, -1);
                    AddRayMoves(candidates, piece, 1, 1);
                    AddRayMoves(candidates, piece, 1, -1);
                    AddRayMoves(candidates, piece, -1, 1);
                    AddRayMoves(candidates, piece, -1, -1);
                    break;

                case PieceType.King:
                    for (int df = -1; df <= 1; df++)
                    {
                        for (int dr = -1; dr <= 1; dr++)
                        {
                            if (df == 0 && dr == 0) continue;
                            int nf = f + df;
                            int nr = r + dr;
                            if (IsOnBoard(nf, nr))
                            {
                                if (boardPieces[nf, nr] == null || boardPieces[nf, nr].pieceColor != piece.pieceColor)
                                    candidates.Add(new Vector2Int(nf, nr));
                            }
                        }
                    }
                    break;
            }

            return candidates;
        }

        private void AddRayMoves(List<Vector2Int> moves, ChessPiece3D piece, int stepF, int stepR)
        {
            int f = piece.file + stepF;
            int r = piece.rank + stepR;
            while (IsOnBoard(f, r))
            {
                if (boardPieces[f, r] == null)
                {
                    moves.Add(new Vector2Int(f, r));
                }
                else
                {
                    if (boardPieces[f, r].pieceColor != piece.pieceColor)
                    {
                        moves.Add(new Vector2Int(f, r));
                    }
                    break; // Blocked by piece
                }
                f += stepF;
                r += stepR;
            }
        }

        public bool IsInCheck(PieceColor kingColor)
        {
            // Find King
            ChessPiece3D king = null;
            foreach (var p in boardPieces)
            {
                if (p != null && p.pieceColor == kingColor && p.pieceType == PieceType.King)
                {
                    king = p;
                    break;
                }
            }
            if (king == null) return false;

            // Check if any opponent piece can hit king's position
            PieceColor opponent = (kingColor == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            foreach (var p in boardPieces)
            {
                if (p != null && p.pieceColor == opponent)
                {
                    var moves = GetLegalMoves(p);
                    if (moves.Contains(new Vector2Int(king.file, king.rank))) return true;
                }
            }
            return false;
        }

        public bool IsCheckmate(PieceColor kingColor)
        {
            if (!IsInCheck(kingColor)) return false;

            // Check if any move escapes check
            foreach (var p in boardPieces)
            {
                if (p != null && p.pieceColor == kingColor)
                {
                    var moves = GetLegalMoves(p);
                    if (moves.Count > 0) return false;
                }
            }
            return true;
        }

        private bool IsOnBoard(int file, int rank)
        {
            return file >= 0 && file < 8 && rank >= 0 && rank < 8;
        }

        private string GetAlgebraic(int file, int rank)
        {
            char col = (char)('A' + file);
            return $"{col}{rank + 1}";
        }

        // =========================================================================
        // Highlighting
        // =========================================================================
        private void HighlightTile(int file, int rank)
        {
            if (IsOnBoard(file, rank))
            {
                _tileRenderers[file, rank].sharedMaterial = _highlightTileMat;
            }
        }

        public void ClearHighlights()
        {
            for (int f = 0; f < 8; f++)
            {
                for (int r = 0; r < 8; r++)
                {
                    bool isLight = (f + r) % 2 == 1;
                    _tileRenderers[f, r].sharedMaterial = isLight ? _lightTileMat : _darkTileMat;
                }
            }
        }

        // =========================================================================
        // Remote Commands & AI Execution
        // =========================================================================
        public void ResetBoard()
        {
            isGameOver = false;
            currentTurn = PieceColor.White;
            turnCounter = 1;
            ClearHighlights();
            SpawnAllPieces();

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.SetActiveTurn(currentTurn);
                UIManager2D.Instance.RecordMove(">>> BOARD REINITIALIZED <<<");
            }
        }

        public void TriggerEMPShockwave()
        {
            Debug.Log("[ChessBoard] EMP Shockwave triggered across all Mech units!");
            foreach (var piece in boardPieces)
            {
                if (piece != null && piece.Animator != null)
                {
                    piece.Animator.TriggerEMPShock();
                }
            }
        }

        public void TriggerAIMove()
        {
            // Pick first valid legal move for active player
            foreach (var p in boardPieces)
            {
                if (p != null && p.pieceColor == currentTurn)
                {
                    var moves = GetLegalMoves(p);
                    if (moves.Count > 0)
                    {
                        var m = moves[0];
                        ExecuteMove(p, m.x, m.y);
                        return;
                    }
                }
            }
        }

        public void ExecuteAlgebraicMove(string fromSquare, string toSquare)
        {
            if (fromSquare.Length < 2 || toSquare.Length < 2) return;

            int f1 = char.ToUpper(fromSquare[0]) - 'A';
            int r1 = int.Parse(fromSquare[1].ToString()) - 1;
            int f2 = char.ToUpper(toSquare[0]) - 'A';
            int r2 = int.Parse(toSquare[1].ToString()) - 1;

            if (IsOnBoard(f1, r1) && IsOnBoard(f2, r2))
            {
                ChessPiece3D piece = boardPieces[f1, r1];
                if (piece != null)
                {
                    ExecuteMove(piece, f2, r2);
                }
            }
        }
    }

    /// <summary>
    /// Tile click handler for raycast detection.
    /// </summary>
    public class TileClickReceiver : MonoBehaviour
    {
        public int file;
        public int rank;
        public Action<int, int> OnTileClicked;

        private void OnMouseDown()
        {
            OnTileClicked?.Invoke(file, rank);
        }
    }
}
