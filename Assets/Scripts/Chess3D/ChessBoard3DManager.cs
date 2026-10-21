using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using MechChess.ProceduralMech;
using MechChess.Combat2D;
using MechChess.UI;
using MechChess.Cloud;

namespace MechChess.Chess3D
{
    /// <summary>
    /// Move history snapshot for Undo in practice mode.
    /// </summary>
    public class MoveHistoryRecord
    {
        public ChessPiece3D piece;
        public int fromF, fromR;
        public int toF, toR;
        public bool pieceHadMoved;
        public ChessPiece3D capturedPiece;
        public int capturedF, capturedR;

        // Castling details
        public bool isCastling;
        public ChessPiece3D castlingRook;
        public int castlingRookFromF, castlingRookToF;
        public int castlingRookRank;

        // En Passant details
        public bool isEnPassant;
        public Vector2Int? prevEnPassantSquare;

        // Promotion details
        public bool wasPromotion;
        public PieceType originalType;

        // Half-move clock
        public int prevHalfMoveClock;
    }

    /// <summary>
    /// Central 3D Chessboard Manager (Complete F01 - F12 Implementation):
    /// - Complete Chess rules: 6 piece types, check/checkmate detection, king safety/pins.
    /// - Special moves: Kingside/Queenside Castling (O-O, O-O-O), En Passant, Pawn Promotion selection.
    /// - End-game rules: Checkmate, Stalemate (hết nước đi), Insufficient Material, 50-move rule, Resign, Draw.
    /// - F04: 4 AI Levels (Easy, Medium, Hard, Expert) with autonomous reactions.
    /// - F06: Valid moves, King check warning tile, and Last move highlight.
    /// - F11: FEN generation/loading & persistent game auto-save.
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
        private Material _lastMoveMat;
        private Material _checkWarningMat;
        private Material _borderMat;

        [Header("Gameplay State")]
        public PieceColor currentTurn = PieceColor.White;
        public int turnCounter = 1;
        public bool isGameOver = false;

        // F06 Visual Highlights State
        private Vector2Int? _lastMoveFromTile = null;
        private Vector2Int? _lastMoveToTile = null;

        // F04 AI Configuration
        [Header("AI Configuration (F04)")]
        public bool isVsAI = false;
        public PieceColor aiColor = PieceColor.Black;
        public int aiLevel = 2; // 1 = Easy, 2 = Medium, 3 = Hard, 4 = Expert
        public string aiBackendUrl = "http://localhost:8000/api/ai-move";
        private bool _isAIThinking = false;

        // F01 Advanced Rules State
        private Vector2Int? _enPassantTargetSquare = null;
        private Vector2Int? _nextEnPassantSquare = null;
        private int _halfMoveClock = 0; // For 50-move rule (100 half-moves)
        private Stack<MoveHistoryRecord> _moveHistoryStack = new Stack<MoveHistoryRecord>();

        private ChessPiece3D _selectedPiece = null;
        private List<Vector2Int> _validMoves = new List<Vector2Int>();

        private Transform _boardRoot;
        private Transform _piecesRoot;
        public Transform BoardRootContainer { get; private set; }

        public Action<ChessPiece3D, Action<PieceType>> OnPawnPromotionNeeded;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }

            InitializeBoardMaterials();
        }

        private void Start()
        {
            BuildProceduralBoard();
            SpawnAllPieces();

            // Keep the 3D chessboard hidden initially until the player actually launches a match from the Main Menu
            if (BoardRootContainer != null)
            {
                BoardRootContainer.gameObject.SetActive(false);
            }

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

            // Last Move Tile: Radiant Amber Gold
            _lastMoveMat = new Material(shader) { name = "Tile_LastMoveGold" };
            SetMatColor(_lastMoveMat, new Color(0.95f, 0.72f, 0.15f), 0.3f, 0.7f);
            _lastMoveMat.EnableKeyword("_EMISSION");
            if (_lastMoveMat.HasProperty("_EmissionColor"))
                _lastMoveMat.SetColor("_EmissionColor", new Color(1.8f, 1.2f, 0.2f) * 1.6f);

            // Check Warning Tile: Fiery Glowing Red for King under attack
            _checkWarningMat = new Material(shader) { name = "Tile_CheckWarningRed" };
            SetMatColor(_checkWarningMat, new Color(1.0f, 0.15f, 0.2f), 0.2f, 0.8f);
            _checkWarningMat.EnableKeyword("_EMISSION");
            if (_checkWarningMat.HasProperty("_EmissionColor"))
                _checkWarningMat.SetColor("_EmissionColor", new Color(2.8f, 0.2f, 0.3f) * 2.5f);

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
            if (BoardRootContainer == null)
            {
                GameObject container = new GameObject("ChessBoard_WorldContainer");
                container.transform.position = Vector3.zero;
                container.transform.rotation = Quaternion.identity;
                container.transform.localScale = Vector3.one;
                BoardRootContainer = container.transform;
            }

            if (_boardRoot != null) Destroy(_boardRoot.gameObject);

            _boardRoot = new GameObject("ProceduralChessBoard_3D").transform;
            _boardRoot.SetParent(BoardRootContainer, false);

            // Outer Arena Platform Pedestal
            GameObject arenaPedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arenaPedestal.name = "CyberArenaPedestal";
            arenaPedestal.transform.SetParent(_boardRoot, false);
            arenaPedestal.transform.localPosition = new Vector3(3.5f, -0.2f, 3.5f);
            arenaPedestal.transform.localScale = new Vector3(9.0f, 0.2f, 9.0f);
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
            if (BoardRootContainer == null)
            {
                GameObject container = new GameObject("ChessBoard_WorldContainer");
                container.transform.position = Vector3.zero;
                container.transform.rotation = Quaternion.identity;
                container.transform.localScale = Vector3.one;
                BoardRootContainer = container.transform;
            }

            if (_piecesRoot != null) Destroy(_piecesRoot.gameObject);
            _piecesRoot = new GameObject("MechPiecesRoot").transform;
            _piecesRoot.SetParent(BoardRootContainer, false);

            Array.Clear(boardPieces, 0, boardPieces.Length);
            _enPassantTargetSquare = null;
            _nextEnPassantSquare = null;
            _halfMoveClock = 0;
            _moveHistoryStack.Clear();

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
                Quaternion rot180 = Quaternion.Euler(0, 180f, 0);
                pieceObj.transform.localRotation = rot180;
                MechPieceAnimator anim = pieceObj.GetComponent<MechPieceAnimator>();
                if (anim != null) anim.SetBaseOrientation(rot180);
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

            // Online Multiplayer turn & role enforcement
            if (MechChess.Multiplayer.OnlineMultiplayerManager.Instance != null 
                && MechChess.Multiplayer.OnlineMultiplayerManager.Instance.IsOnlineMatch)
            {
                var online = MechChess.Multiplayer.OnlineMultiplayerManager.Instance;
                if (currentTurn != online.MyRole) return;
                if (_selectedPiece == null && piece.pieceColor != online.MyRole) return;
            }

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

            // Online Multiplayer turn enforcement
            if (MechChess.Multiplayer.OnlineMultiplayerManager.Instance != null 
                && MechChess.Multiplayer.OnlineMultiplayerManager.Instance.IsOnlineMatch)
            {
                var online = MechChess.Multiplayer.OnlineMultiplayerManager.Instance;
                if (currentTurn != online.MyRole) return;
            }

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

        private bool _isExecutingRemoteMove = false;

        public void ExecuteRemoteMove(string fromSquare, string toSquare)
        {
            _isExecutingRemoteMove = true;
            try
            {
                ExecuteAlgebraicMove(fromSquare, toSquare);
            }
            finally
            {
                _isExecutingRemoteMove = false;
            }
        }

        public void ExecuteMove(ChessPiece3D piece, int targetFile, int targetRank, PieceType promoteTo = PieceType.Queen)
        {
            ClearHighlights();
            int startFile = piece.file;
            int startRank = piece.rank;

            // F06: Record last move square coordinates
            _lastMoveFromTile = new Vector2Int(startFile, startRank);
            _lastMoveToTile = new Vector2Int(targetFile, targetRank);

            bool isPawn = (piece.pieceType == PieceType.Pawn);
            bool isKing = (piece.pieceType == PieceType.King);

            // Record snapshot for Undo
            var historyRecord = new MoveHistoryRecord
            {
                piece = piece,
                fromF = startFile,
                fromR = startRank,
                toF = targetFile,
                toR = targetRank,
                pieceHadMoved = piece.hasMoved,
                prevEnPassantSquare = _enPassantTargetSquare,
                prevHalfMoveClock = _halfMoveClock,
                originalType = piece.pieceType
            };

            // Detect En Passant Capture
            bool isEnPassant = isPawn && _enPassantTargetSquare.HasValue 
                && targetFile == _enPassantTargetSquare.Value.x 
                && targetRank == _enPassantTargetSquare.Value.y;

            ChessPiece3D captured = isEnPassant ? boardPieces[targetFile, startRank] : boardPieces[targetFile, targetRank];

            if (captured != null)
            {
                historyRecord.capturedPiece = captured;
                historyRecord.capturedF = captured.file;
                historyRecord.capturedR = captured.rank;
            }

            // Detect Castling
            bool isCastling = isKing && Mathf.Abs(targetFile - startFile) == 2;
            historyRecord.isCastling = isCastling;

            // 50-move clock: reset on pawn move or capture, otherwise increment
            if (isPawn || captured != null) _halfMoveClock = 0;
            else _halfMoveClock++;

            // Handle En Passant Next Square
            _nextEnPassantSquare = null;
            if (isPawn && Mathf.Abs(targetRank - startRank) == 2)
            {
                _nextEnPassantSquare = new Vector2Int(startFile, (startRank + targetRank) / 2);
            }

            string moveNotation = $"{piece.pieceColor} {piece.pieceType}: {GetAlgebraic(startFile, startRank)} -> {GetAlgebraic(targetFile, targetRank)}";
            if (isCastling)
            {
                moveNotation = (targetFile == 6) ? $"{piece.pieceColor} O-O (Kingside Castling)" : $"{piece.pieceColor} O-O-O (Queenside Castling)";
            }
            else if (isEnPassant)
            {
                moveNotation += " e.p. [EN PASSANT]";
            }

            // Capture resolution with 2D Minigame
            if (captured != null)
            {
                if (CombatMinigame2D.Instance != null && !_isExecutingRemoteMove)
                {
                    CombatMinigame2D.Instance.StartMinigame((success) =>
                    {
                        FinalizeCaptureAndMove(piece, startFile, startRank, targetFile, targetRank, captured, isEnPassant, isCastling, historyRecord, moveNotation, promoteTo);
                    });
                }
                else
                {
                    FinalizeCaptureAndMove(piece, startFile, startRank, targetFile, targetRank, captured, isEnPassant, isCastling, historyRecord, moveNotation, promoteTo);
                }
            }
            else
            {
                FinalizeMove(piece, startFile, startRank, targetFile, targetRank, isCastling, historyRecord, moveNotation, promoteTo);
            }
        }

        private void FinalizeCaptureAndMove(ChessPiece3D piece, int startFile, int startRank, int targetFile, int targetRank, ChessPiece3D captured, bool isEnPassant, bool isCastling, MoveHistoryRecord historyRecord, string notation, PieceType promoteTo)
        {
            if (isEnPassant)
            {
                boardPieces[targetFile, startRank] = null;
            }
            else
            {
                boardPieces[targetFile, targetRank] = null;
            }

            if (captured != null)
            {
                if (captured.Animator != null) captured.Animator.PlayCaptureDestruction();
                else captured.gameObject.SetActive(false);
            }

            FinalizeMove(piece, startFile, startRank, targetFile, targetRank, isCastling, historyRecord, notation + " [CAPTURED]", promoteTo);
        }

        private void FinalizeMove(ChessPiece3D piece, int startFile, int startRank, int targetFile, int targetRank, bool isCastling, MoveHistoryRecord historyRecord, string notation, PieceType promoteTo)
        {
            boardPieces[piece.file, piece.rank] = null;
            boardPieces[targetFile, targetRank] = piece;
            piece.file = targetFile;
            piece.rank = targetRank;
            piece.hasMoved = true;

            // Handle Castling Rook Relocation
            if (isCastling)
            {
                int rookRank = startRank;
                if (targetFile == 6) // Kingside: Rook moves from 7 to 5
                {
                    ChessPiece3D rook = boardPieces[7, rookRank];
                    if (rook != null)
                    {
                        historyRecord.castlingRook = rook;
                        historyRecord.castlingRookFromF = 7;
                        historyRecord.castlingRookToF = 5;
                        historyRecord.castlingRookRank = rookRank;

                        boardPieces[7, rookRank] = null;
                        boardPieces[5, rookRank] = rook;
                        rook.file = 5;
                        rook.hasMoved = true;

                        Vector3 rookPos = GetWorldPosition(5, rookRank);
                        if (rook.Animator != null) rook.Animator.AnimateMoveTo(rookPos, 0.40f);
                        else rook.transform.localPosition = rookPos;
                    }
                }
                else if (targetFile == 2) // Queenside: Rook moves from 0 to 3
                {
                    ChessPiece3D rook = boardPieces[0, rookRank];
                    if (rook != null)
                    {
                        historyRecord.castlingRook = rook;
                        historyRecord.castlingRookFromF = 0;
                        historyRecord.castlingRookToF = 3;
                        historyRecord.castlingRookRank = rookRank;

                        boardPieces[0, rookRank] = null;
                        boardPieces[3, rookRank] = rook;
                        rook.file = 3;
                        rook.hasMoved = true;

                        Vector3 rookPos = GetWorldPosition(3, rookRank);
                        if (rook.Animator != null) rook.Animator.AnimateMoveTo(rookPos, 0.40f);
                        else rook.transform.localPosition = rookPos;
                    }
                }
            }

            // Animate moving piece
            Vector3 targetPos = GetWorldPosition(targetFile, targetRank);
            if (piece.Animator != null) piece.Animator.AnimateMoveTo(targetPos, 0.45f);
            else piece.transform.localPosition = targetPos;

            _selectedPiece = null;
            _enPassantTargetSquare = _nextEnPassantSquare;
            _nextEnPassantSquare = null;

            // Check Pawn Promotion
            if (piece.pieceType == PieceType.Pawn && (targetRank == 7 || targetRank == 0))
            {
                historyRecord.wasPromotion = true;
                _moveHistoryStack.Push(historyRecord);

                // If Promotion callback is hooked up and not a remote/AI move -> show UI selection!
                if (OnPawnPromotionNeeded != null && !_isExecutingRemoteMove)
                {
                    OnPawnPromotionNeeded.Invoke(piece, (selectedType) =>
                    {
                        ApplyPawnPromotion(piece, selectedType, notation, startFile, startRank, targetFile, targetRank);
                    });
                    return;
                }
                else
                {
                    ApplyPawnPromotion(piece, promoteTo, notation, startFile, startRank, targetFile, targetRank);
                    return;
                }
            }

            _moveHistoryStack.Push(historyRecord);
            PostMoveStateEvaluation(notation, startFile, startRank, targetFile, targetRank);
        }

        private void ApplyPawnPromotion(ChessPiece3D pawn, PieceType targetType, string notation, int sF, int sR, int tF, int tR)
        {
            int f = pawn.file;
            int r = pawn.rank;
            PieceColor col = pawn.pieceColor;

            Destroy(pawn.gameObject);
            SpawnPiece(targetType, col, f, r);

            string promoNotation = $"{notation} ={targetType.ToString().ToUpper()} [PROMOTION]";
            PostMoveStateEvaluation(promoNotation, sF, sR, tF, tR);
        }

        private void PostMoveStateEvaluation(string notation, int startFile, int startRank, int targetFile, int targetRank)
        {
            // Log to HUD
            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.RecordMove(notation);
            }

            // Sync with Online Opponent over WebSocket
            if (MechChess.Multiplayer.OnlineMultiplayerManager.Instance != null 
                && MechChess.Multiplayer.OnlineMultiplayerManager.Instance.IsOnlineMatch 
                && !_isExecutingRemoteMove)
            {
                string fromSq = GetAlgebraic(startFile, startRank);
                string toSq = GetAlgebraic(targetFile, targetRank);
                MechChess.Multiplayer.OnlineMultiplayerManager.Instance.SendMove(fromSq, toSq, notation);
            }

            PieceColor opponent = (currentTurn == PieceColor.White) ? PieceColor.Black : PieceColor.White;

            // 1. Evaluate Check, Checkmate & Draw States for the opponent
            bool inCheck = IsInCheck(opponent);
            bool checkmate = inCheck && IsCheckmate(opponent);
            bool stalemate = !inCheck && IsStalemate(opponent);
            bool insufficientMaterial = IsInsufficientMaterial();
            bool fiftyMoveDraw = (_halfMoveClock >= 100);

            string statusStr = "Playing";

            if (checkmate)
            {
                statusStr = "Checkmate";
                isGameOver = true;
                string winnerName = (currentTurn == PieceColor.White) ? "LIÊN MINH (WHITE)" : "HẮC ÁM (BLACK)";
                if (UIManager2D.Instance != null)
                {
                    UIManager2D.Instance.RecordMove($"🏆 CHIẾU HẾT (CHECKMATE)! {winnerName} CHIẾN THẮNG!");
                    UIManager2D.Instance.RecordMove("===============================");
                    UIManager2D.Instance.TriggerGameOver(currentTurn, "Chiếu hết (Checkmate)!");
                }
            }
            else if (stalemate)
            {
                statusStr = "Stalemate";
                isGameOver = true;
                if (UIManager2D.Instance != null)
                {
                    UIManager2D.Instance.RecordMove($"⚖️ HÒA CỜ: STALEMATE (HẾT NƯỚC ĐI HỢP LỆ)!");
                    UIManager2D.Instance.RecordMove("===============================");
                }
            }
            else if (insufficientMaterial)
            {
                statusStr = "InsufficientMaterial_Draw";
                isGameOver = true;
                if (UIManager2D.Instance != null)
                {
                    UIManager2D.Instance.RecordMove($"⚖️ HÒA CỜ: KHÔNG ĐỦ LỰC LƯỢNG CHIẾU HẾT!");
                    UIManager2D.Instance.RecordMove("===============================");
                }
            }
            else if (fiftyMoveDraw)
            {
                statusStr = "50MoveRule_Draw";
                isGameOver = true;
                if (UIManager2D.Instance != null)
                {
                    UIManager2D.Instance.RecordMove($"⚖️ HÒA CỜ: LUẬT 50 NƯỚC KHÔNG ĂN QUÂN!");
                    UIManager2D.Instance.RecordMove("===============================");
                }
            }
            else if (inCheck)
            {
                statusStr = "Check";
                if (UIManager2D.Instance != null)
                {
                    UIManager2D.Instance.RecordMove($"⚠️ VUA {opponent.ToString().ToUpper()} ĐANG BỊ CHIẾU!");
                }
            }

            // Upstream Adafruit IO Telemetry
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

            // Update Check Warning Tile on King
            UpdateKingCheckHighlight();

            // F11: Auto-save board state for continue game card
            SaveCurrentGame();

            // F04: Autonomous AI reaction when AI turn is active
            if (!isGameOver && isVsAI && currentTurn == aiColor && !_isAIThinking)
            {
                StartCoroutine(PlayAIMoveRoutine());
            }
        }

        private void UpdateKingCheckHighlight()
        {
            if (isGameOver) return;
            if (IsInCheck(currentTurn))
            {
                ChessPiece3D king = FindKing(currentTurn);
                if (king != null && IsOnBoard(king.file, king.rank))
                {
                    _tileRenderers[king.file, king.rank].sharedMaterial = _checkWarningMat;
                }
            }
        }

        // =========================================================================
        // Legal Move Generation (F01 Complete Chess Rules + King Safety)
        // =========================================================================
        public List<Vector2Int> GetLegalMoves(ChessPiece3D piece)
        {
            List<Vector2Int> legal = new List<Vector2Int>();
            List<Vector2Int> candidates = GetPseudoLegalMoves(piece);

            // Filter out moves that leave own King in check (King Safety & Absolute Pins)
            foreach (var move in candidates)
            {
                if (!SimulateMoveLeavesKingInCheck(piece, move.x, move.y))
                {
                    legal.Add(move);
                }
            }

            return legal;
        }

        private bool SimulateMoveLeavesKingInCheck(ChessPiece3D piece, int toF, int toR)
        {
            int fromF = piece.file;
            int fromR = piece.rank;
            ChessPiece3D captured = boardPieces[toF, toR];

            bool isEnPassant = (piece.pieceType == PieceType.Pawn && _enPassantTargetSquare.HasValue && toF == _enPassantTargetSquare.Value.x && toR == _enPassantTargetSquare.Value.y);
            ChessPiece3D epCaptured = null;
            if (isEnPassant)
            {
                epCaptured = boardPieces[toF, fromR];
                boardPieces[toF, fromR] = null;
            }

            // Apply temporary move
            boardPieces[fromF, fromR] = null;
            boardPieces[toF, toR] = piece;
            piece.file = toF;
            piece.rank = toR;

            bool inCheck = IsInCheck(piece.pieceColor);

            // Revert temporary move
            piece.file = fromF;
            piece.rank = fromR;
            boardPieces[fromF, fromR] = piece;
            boardPieces[toF, toR] = captured;

            if (isEnPassant)
            {
                boardPieces[toF, fromR] = epCaptured;
            }

            return inCheck;
        }

        public List<Vector2Int> GetPseudoLegalMoves(ChessPiece3D piece)
        {
            List<Vector2Int> moves = new List<Vector2Int>();
            int f = piece.file;
            int r = piece.rank;
            bool isWhite = (piece.pieceColor == PieceColor.White);
            int pDir = isWhite ? 1 : -1;

            switch (piece.pieceType)
            {
                case PieceType.Pawn:
                    // Forward 1
                    if (IsOnBoard(f, r + pDir) && boardPieces[f, r + pDir] == null)
                    {
                        moves.Add(new Vector2Int(f, r + pDir));
                        // Forward 2
                        int startRank = isWhite ? 1 : 6;
                        if (r == startRank && boardPieces[f, r + 2 * pDir] == null)
                        {
                            moves.Add(new Vector2Int(f, r + 2 * pDir));
                        }
                    }
                    // Diagonal Captures
                    int[] sideDirs = { -1, 1 };
                    foreach (int df in sideDirs)
                    {
                        int tf = f + df;
                        int tr = r + pDir;
                        if (IsOnBoard(tf, tr))
                        {
                            ChessPiece3D target = boardPieces[tf, tr];
                            if (target != null && target.pieceColor != piece.pieceColor)
                            {
                                moves.Add(new Vector2Int(tf, tr));
                            }
                            // En Passant Capture
                            else if (_enPassantTargetSquare.HasValue && tf == _enPassantTargetSquare.Value.x && tr == _enPassantTargetSquare.Value.y)
                            {
                                moves.Add(new Vector2Int(tf, tr));
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
                                moves.Add(new Vector2Int(nf, nr));
                        }
                    }
                    break;

                case PieceType.Bishop:
                    AddRayMoves(moves, piece, 1, 1);
                    AddRayMoves(moves, piece, 1, -1);
                    AddRayMoves(moves, piece, -1, 1);
                    AddRayMoves(moves, piece, -1, -1);
                    break;

                case PieceType.Rook:
                    AddRayMoves(moves, piece, 1, 0);
                    AddRayMoves(moves, piece, -1, 0);
                    AddRayMoves(moves, piece, 0, 1);
                    AddRayMoves(moves, piece, 0, -1);
                    break;

                case PieceType.Queen:
                    AddRayMoves(moves, piece, 1, 0);
                    AddRayMoves(moves, piece, -1, 0);
                    AddRayMoves(moves, piece, 0, 1);
                    AddRayMoves(moves, piece, 0, -1);
                    AddRayMoves(moves, piece, 1, 1);
                    AddRayMoves(moves, piece, 1, -1);
                    AddRayMoves(moves, piece, -1, 1);
                    AddRayMoves(moves, piece, -1, -1);
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
                                    moves.Add(new Vector2Int(nf, nr));
                            }
                        }
                    }

                    // Castling Moves (Nhập thành O-O và O-O-O)
                    if (!piece.hasMoved && !IsInCheck(piece.pieceColor))
                    {
                        PieceColor oppColor = isWhite ? PieceColor.Black : PieceColor.White;
                        int baseRank = isWhite ? 0 : 7;

                        // Kingside (O-O): Rook at file 7, squares (5, baseRank) and (6, baseRank) empty and safe
                        ChessPiece3D ksRook = boardPieces[7, baseRank];
                        if (ksRook != null && ksRook.pieceType == PieceType.Rook && !ksRook.hasMoved && ksRook.pieceColor == piece.pieceColor)
                        {
                            if (boardPieces[5, baseRank] == null && boardPieces[6, baseRank] == null)
                            {
                                if (!IsSquareAttacked(5, baseRank, oppColor) && !IsSquareAttacked(6, baseRank, oppColor))
                                {
                                    moves.Add(new Vector2Int(6, baseRank));
                                }
                            }
                        }

                        // Queenside (O-O-O): Rook at file 0, squares (1, 2, 3) empty, (2, 3) safe
                        ChessPiece3D qsRook = boardPieces[0, baseRank];
                        if (qsRook != null && qsRook.pieceType == PieceType.Rook && !qsRook.hasMoved && qsRook.pieceColor == piece.pieceColor)
                        {
                            if (boardPieces[1, baseRank] == null && boardPieces[2, baseRank] == null && boardPieces[3, baseRank] == null)
                            {
                                if (!IsSquareAttacked(3, baseRank, oppColor) && !IsSquareAttacked(2, baseRank, oppColor))
                                {
                                    moves.Add(new Vector2Int(2, baseRank));
                                }
                            }
                        }
                    }
                    break;
            }

            return moves;
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

        // =========================================================================
        // Check, Checkmate & Square Attack Queries
        // =========================================================================
        public bool IsSquareAttacked(int targetFile, int targetRank, PieceColor attackerColor)
        {
            // 1. Attacked by enemy Pawns
            int pDir = (attackerColor == PieceColor.White) ? 1 : -1;
            int pRank = targetRank - pDir;
            if (pRank >= 0 && pRank < 8)
            {
                int[] pFiles = { targetFile - 1, targetFile + 1 };
                foreach (int pf in pFiles)
                {
                    if (pf >= 0 && pf < 8)
                    {
                        ChessPiece3D p = boardPieces[pf, pRank];
                        if (p != null && p.pieceColor == attackerColor && p.pieceType == PieceType.Pawn)
                            return true;
                    }
                }
            }

            // 2. Attacked by enemy Knights
            int[,] knightOffsets = { {-2,-1}, {-2,1}, {-1,-2}, {-1,2}, {1,-2}, {1,2}, {2,-1}, {2,1} };
            for (int i = 0; i < 8; i++)
            {
                int kf = targetFile + knightOffsets[i, 0];
                int kr = targetRank + knightOffsets[i, 1];
                if (IsOnBoard(kf, kr))
                {
                    ChessPiece3D p = boardPieces[kf, kr];
                    if (p != null && p.pieceColor == attackerColor && p.pieceType == PieceType.Knight)
                        return true;
                }
            }

            // 3. Attacked by enemy King (adjacent square)
            for (int df = -1; df <= 1; df++)
            {
                for (int dr = -1; dr <= 1; dr++)
                {
                    if (df == 0 && dr == 0) continue;
                    int kf = targetFile + df;
                    int kr = targetRank + dr;
                    if (IsOnBoard(kf, kr))
                    {
                        ChessPiece3D p = boardPieces[kf, kr];
                        if (p != null && p.pieceColor == attackerColor && p.pieceType == PieceType.King)
                            return true;
                    }
                }
            }

            // 4. Attacked by orthogonal rays (Rook or Queen)
            int[,] orthoDirs = { {1,0}, {-1,0}, {0,1}, {0,-1} };
            for (int i = 0; i < 4; i++)
            {
                int f = targetFile + orthoDirs[i, 0];
                int r = targetRank + orthoDirs[i, 1];
                while (IsOnBoard(f, r))
                {
                    ChessPiece3D p = boardPieces[f, r];
                    if (p != null)
                    {
                        if (p.pieceColor == attackerColor && (p.pieceType == PieceType.Rook || p.pieceType == PieceType.Queen))
                            return true;
                        break;
                    }
                    f += orthoDirs[i, 0];
                    r += orthoDirs[i, 1];
                }
            }

            // 5. Attacked by diagonal rays (Bishop or Queen)
            int[,] diagDirs = { {1,1}, {1,-1}, {-1,1}, {-1,-1} };
            for (int i = 0; i < 4; i++)
            {
                int f = targetFile + diagDirs[i, 0];
                int r = targetRank + diagDirs[i, 1];
                while (IsOnBoard(f, r))
                {
                    ChessPiece3D p = boardPieces[f, r];
                    if (p != null)
                    {
                        if (p.pieceColor == attackerColor && (p.pieceType == PieceType.Bishop || p.pieceType == PieceType.Queen))
                            return true;
                        break;
                    }
                    f += diagDirs[i, 0];
                    r += diagDirs[i, 1];
                }
            }

            return false;
        }

        public bool IsInCheck(PieceColor kingColor)
        {
            ChessPiece3D king = FindKing(kingColor);
            if (king == null) return false;
            PieceColor opponent = (kingColor == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            return IsSquareAttacked(king.file, king.rank, opponent);
        }

        public bool IsCheckmate(PieceColor kingColor)
        {
            if (!IsInCheck(kingColor)) return false;

            // Check if any legal move can resolve check
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

        public bool IsStalemate(PieceColor kingColor)
        {
            if (IsInCheck(kingColor)) return false;

            // If not in check, but has zero legal moves -> Stalemate (Hòa cờ)
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

        public bool IsInsufficientMaterial()
        {
            int whitePieces = 0, blackPieces = 0;
            int whiteBishops = 0, blackBishops = 0;
            int whiteKnights = 0, blackKnights = 0;

            foreach (var p in boardPieces)
            {
                if (p == null) continue;
                if (p.pieceType == PieceType.Pawn || p.pieceType == PieceType.Rook || p.pieceType == PieceType.Queen)
                    return false; // Pawns, rooks, queens can checkmate

                if (p.pieceColor == PieceColor.White)
                {
                    whitePieces++;
                    if (p.pieceType == PieceType.Bishop) whiteBishops++;
                    if (p.pieceType == PieceType.Knight) whiteKnights++;
                }
                else
                {
                    blackPieces++;
                    if (p.pieceType == PieceType.Bishop) blackBishops++;
                    if (p.pieceType == PieceType.Knight) blackKnights++;
                }
            }

            // K vs K
            if (whitePieces == 1 && blackPieces == 1) return true;
            // K+B vs K or K+N vs K
            if (whitePieces == 2 && blackPieces == 1 && (whiteBishops == 1 || whiteKnights == 1)) return true;
            if (blackPieces == 2 && whitePieces == 1 && (blackBishops == 1 || blackKnights == 1)) return true;

            return false;
        }

        private ChessPiece3D FindKing(PieceColor color)
        {
            foreach (var p in boardPieces)
            {
                if (p != null && p.pieceColor == color && p.pieceType == PieceType.King)
                    return p;
            }
            return null;
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

            // F06: Last move highlight on from and to squares
            if (_lastMoveFromTile.HasValue && IsOnBoard(_lastMoveFromTile.Value.x, _lastMoveFromTile.Value.y))
            {
                _tileRenderers[_lastMoveFromTile.Value.x, _lastMoveFromTile.Value.y].sharedMaterial = _lastMoveMat;
            }
            if (_lastMoveToTile.HasValue && IsOnBoard(_lastMoveToTile.Value.x, _lastMoveToTile.Value.y))
            {
                _tileRenderers[_lastMoveToTile.Value.x, _lastMoveToTile.Value.y].sharedMaterial = _lastMoveMat;
            }

            UpdateKingCheckHighlight();
        }

        // =========================================================================
        // Match Controls: Resign, Offer Draw, Undo (F01 & F03)
        // =========================================================================
        public void Resign(PieceColor resigningColor)
        {
            if (isGameOver) return;
            isGameOver = true;

            PieceColor winner = (resigningColor == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            string winStr = (winner == PieceColor.White) ? "LIÊN MINH (WHITE)" : "HẮC ÁM (BLACK)";
            string resignStr = (resigningColor == PieceColor.White) ? "LIÊN MINH (WHITE)" : "HẮC ÁM (BLACK)";

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.RecordMove("===============================");
                UIManager2D.Instance.RecordMove($"[ĐẦU HÀNG] {resignStr} đã đầu hàng!");
                UIManager2D.Instance.RecordMove($"🏆 {winStr} CHIẾN THẮNG!");
                UIManager2D.Instance.RecordMove("===============================");
                UIManager2D.Instance.TriggerGameOver(winner, $"{resignStr} đã đầu hàng!");
            }
        }

        public void OfferDraw()
        {
            if (isGameOver) return;
            isGameOver = true;

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.RecordMove("===============================");
                UIManager2D.Instance.RecordMove("[HÒA CỜ] Hai bên đã thỏa thuận hòa!");
                UIManager2D.Instance.RecordMove("===============================");
            }
        }

        public void UndoMove()
        {
            if (_moveHistoryStack.Count == 0) return;

            // In online multiplayer, undo is disabled for fair competition
            if (MechChess.Multiplayer.OnlineMultiplayerManager.Instance != null 
                && MechChess.Multiplayer.OnlineMultiplayerManager.Instance.IsOnlineMatch)
            {
                if (UIManager2D.Instance != null)
                    UIManager2D.Instance.RecordMove("[TỪ CHỐI] Không thể hoàn tác trong chế độ Online!");
                return;
            }

            var record = _moveHistoryStack.Pop();

            // 1. Move piece back
            boardPieces[record.toF, record.toR] = null;
            boardPieces[record.fromF, record.fromR] = record.piece;
            record.piece.file = record.fromF;
            record.piece.rank = record.fromR;
            record.piece.hasMoved = record.pieceHadMoved;
            record.piece.transform.localPosition = GetWorldPosition(record.fromF, record.fromR);

            // 2. If it was a promotion, revert back to Pawn
            if (record.wasPromotion)
            {
                Destroy(record.piece.gameObject);
                GameObject pawnObj = ProceduralMechFactory.CreateMechPiece(PieceType.Pawn, record.piece.pieceColor, _piecesRoot);
                pawnObj.transform.localPosition = GetWorldPosition(record.fromF, record.fromR);
                ChessPiece3D pawnPiece = pawnObj.AddComponent<ChessPiece3D>();
                pawnPiece.Setup(PieceType.Pawn, record.piece.pieceColor, record.fromF, record.fromR);
                pawnPiece.hasMoved = record.pieceHadMoved;
                boardPieces[record.fromF, record.fromR] = pawnPiece;
            }

            // 3. Restore captured piece
            if (record.capturedPiece != null)
            {
                record.capturedPiece.gameObject.SetActive(true);
                boardPieces[record.capturedF, record.capturedR] = record.capturedPiece;
                record.capturedPiece.file = record.capturedF;
                record.capturedPiece.rank = record.capturedR;
                record.capturedPiece.transform.localPosition = GetWorldPosition(record.capturedF, record.capturedR);
            }

            // 4. If castling, restore Rook
            if (record.isCastling && record.castlingRook != null)
            {
                boardPieces[record.castlingRookToF, record.castlingRookRank] = null;
                boardPieces[record.castlingRookFromF, record.castlingRookRank] = record.castlingRook;
                record.castlingRook.file = record.castlingRookFromF;
                record.castlingRook.hasMoved = false;
                record.castlingRook.transform.localPosition = GetWorldPosition(record.castlingRookFromF, record.castlingRookRank);
            }

            _enPassantTargetSquare = record.prevEnPassantSquare;
            _halfMoveClock = record.prevHalfMoveClock;
            isGameOver = false;

            // Revert turn
            currentTurn = (currentTurn == PieceColor.White) ? PieceColor.Black : PieceColor.White;
            turnCounter = Mathf.Max(1, turnCounter - 1);

            ClearHighlights();
            _selectedPiece = null;

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.SetActiveTurn(currentTurn);
                UIManager2D.Instance.RecordMove("[HOÀN TÁC] Nước đi gần nhất đã được hoàn lại.");
            }
        }

        // =========================================================================
        // Remote Commands & AI Execution
        // =========================================================================
        public void ResetBoard()
        {
            if (BoardRootContainer != null)
            {
                BoardRootContainer.localScale = Vector3.one;
                BoardRootContainer.localPosition = Vector3.zero;
                BoardRootContainer.localRotation = Quaternion.identity;
            }
            isGameOver = false;
            currentTurn = PieceColor.White;
            turnCounter = 1;
            _lastMoveFromTile = null;
            _lastMoveToTile = null;
            ClearHighlights();
            SpawnAllPieces();

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.ResetGameTimers();
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
            if (isGameOver || _isAIThinking) return;
            StartCoroutine(PlayAIMoveRoutine());
        }

        public IEnumerator PlayAIMoveRoutine()
        {
            if (isGameOver) yield break;
            _isAIThinking = true;

            string levelName = aiLevel == 1 ? "EASY (1)" : aiLevel == 2 ? "MEDIUM (2)" : aiLevel == 3 ? "HARD (3)" : "EXPERT (4)";
            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.RecordMove($"🤖 [AI {levelName}] Đang tính toán nước đi...");
            }

            // Natural thinking delay (0.6s - 1.2s)
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.65f, 1.15f));

            bool moveExecuted = false;

            // Level 4 (Expert): Query FastAPI backend if reachable
            if (aiLevel == 4)
            {
                string fen = GenerateFEN();
                string activeColorStr = (currentTurn == PieceColor.White) ? "white" : "black";
                string jsonPayload = $"{{\"fen\":\"{fen}\",\"active_color\":\"{activeColorStr}\",\"depth\":4}}";

                using (UnityWebRequest req = new UnityWebRequest(aiBackendUrl, "POST"))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                    req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.timeout = 2;

                    yield return req.SendWebRequest();

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        string resText = req.downloadHandler.text;
                        string fromSq = ExtractJsonField(resText, "from_square");
                        string toSq = ExtractJsonField(resText, "to_square");

                        if (!string.IsNullOrEmpty(fromSq) && !string.IsNullOrEmpty(toSq))
                        {
                            ExecuteAlgebraicMove(fromSq, toSq);
                            moveExecuted = true;
                            if (UIManager2D.Instance != null)
                                UIManager2D.Instance.UpdateAIServerStatus(true);
                        }
                    }
                    else
                    {
                        if (UIManager2D.Instance != null)
                            UIManager2D.Instance.UpdateAIServerStatus(false);
                    }
                }
            }

            // Fallback / Level 1-3: Embedded Minimax with Alpha-Beta Pruning
            if (!moveExecuted && !isGameOver)
            {
                AIMove bestMove = ComputeBestMoveMinimax(currentTurn, aiLevel);
                if (bestMove != null && bestMove.piece != null)
                {
                    ExecuteMove(bestMove.piece, bestMove.targetPos.x, bestMove.targetPos.y);
                    moveExecuted = true;
                }
                else
                {
                    TriggerAIMoveFallback();
                }
            }

            _isAIThinking = false;
        }

        private void TriggerAIMoveFallback()
        {
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

        private string ExtractJsonField(string json, string fieldName)
        {
            string pattern = $"\"{fieldName}\":\"";
            int idx = json.IndexOf(pattern);
            if (idx < 0) return null;
            int start = idx + pattern.Length;
            int end = json.IndexOf("\"", start);
            if (end < 0) return null;
            return json.Substring(start, end - start);
        }

        // =========================================================================
        // Embedded Minimax Engine with Alpha-Beta Pruning (F04)
        // =========================================================================
        public class AIMove
        {
            public ChessPiece3D piece;
            public Vector2Int targetPos;
            public int score;
        }

        private AIMove ComputeBestMoveMinimax(PieceColor color, int depth)
        {
            List<AIMove> allMoves = new List<AIMove>();
            foreach (var p in boardPieces)
            {
                if (p != null && p.pieceColor == color)
                {
                    var legals = GetLegalMoves(p);
                    foreach (var m in legals)
                    {
                        allMoves.Add(new AIMove { piece = p, targetPos = m });
                    }
                }
            }

            if (allMoves.Count == 0) return null;

            // Easy Level (1): 35% chance to blunder with random move
            if (depth == 1 && UnityEngine.Random.value < 0.35f)
            {
                return allMoves[UnityEngine.Random.Range(0, allMoves.Count)];
            }

            bool isWhite = (color == PieceColor.White);
            AIMove bestMove = allMoves[0];
            int bestScore = isWhite ? int.MinValue : int.MaxValue;
            int alpha = int.MinValue;
            int beta = int.MaxValue;

            int effectiveDepth = Mathf.Clamp(depth, 1, 3);

            foreach (var move in allMoves)
            {
                int score = SimulateAndEvaluate(move.piece, move.targetPos.x, move.targetPos.y, effectiveDepth - 1, alpha, beta, !isWhite);
                move.score = score;

                if (isWhite)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                    alpha = Mathf.Max(alpha, bestScore);
                }
                else
                {
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                    beta = Mathf.Min(beta, bestScore);
                }

                if (beta <= alpha) break; // Alpha-Beta Cutoff
            }

            return bestMove;
        }

        private int SimulateAndEvaluate(ChessPiece3D piece, int toF, int toR, int depth, int alpha, int beta, bool isWhiteTurn)
        {
            int fromF = piece.file;
            int fromR = piece.rank;
            ChessPiece3D captured = boardPieces[toF, toR];
            bool hadMoved = piece.hasMoved;

            // Apply virtual move
            boardPieces[fromF, fromR] = null;
            boardPieces[toF, toR] = piece;
            piece.file = toF;
            piece.rank = toR;
            piece.hasMoved = true;

            int eval;
            if (depth <= 0)
            {
                eval = EvaluateBoard();
            }
            else
            {
                PieceColor activeColor = isWhiteTurn ? PieceColor.White : PieceColor.Black;
                int currentBest = isWhiteTurn ? int.MinValue : int.MaxValue;

                List<AIMove> nextMoves = new List<AIMove>();
                foreach (var p in boardPieces)
                {
                    if (p != null && p.pieceColor == activeColor)
                    {
                        var legals = GetLegalMoves(p);
                        foreach (var m in legals)
                        {
                            nextMoves.Add(new AIMove { piece = p, targetPos = m });
                        }
                    }
                }

                if (nextMoves.Count == 0)
                {
                    if (IsInCheck(activeColor))
                    {
                        eval = isWhiteTurn ? -99999 : 99999;
                    }
                    else
                    {
                        eval = 0; // Stalemate
                    }
                }
                else
                {
                    foreach (var nm in nextMoves)
                    {
                        int score = SimulateAndEvaluate(nm.piece, nm.targetPos.x, nm.targetPos.y, depth - 1, alpha, beta, !isWhiteTurn);
                        if (isWhiteTurn)
                        {
                            currentBest = Mathf.Max(currentBest, score);
                            alpha = Mathf.Max(alpha, currentBest);
                        }
                        else
                        {
                            currentBest = Mathf.Min(currentBest, score);
                            beta = Mathf.Min(beta, currentBest);
                        }
                        if (beta <= alpha) break;
                    }
                    eval = currentBest;
                }
            }

            // Undo virtual move
            piece.file = fromF;
            piece.rank = fromR;
            piece.hasMoved = hadMoved;
            boardPieces[fromF, fromR] = piece;
            boardPieces[toF, toR] = captured;

            return eval;
        }

        private int EvaluateBoard()
        {
            int score = 0;
            for (int f = 0; f < 8; f++)
            {
                for (int r = 0; r < 8; r++)
                {
                    var p = boardPieces[f, r];
                    if (p == null) continue;

                    int val = GetPieceBaseValue(p.pieceType);

                    // Positional center dominance bonus
                    int centerDist = Mathf.Abs(f - 3) + Mathf.Abs(r - 3);
                    int posBonus = (7 - centerDist) * 4;

                    int total = val + posBonus;
                    score += (p.pieceColor == PieceColor.White) ? total : -total;
                }
            }
            return score;
        }

        private int GetPieceBaseValue(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn: return 100;
                case PieceType.Knight: return 320;
                case PieceType.Bishop: return 330;
                case PieceType.Rook: return 500;
                case PieceType.Queen: return 900;
                case PieceType.King: return 20000;
                default: return 0;
            }
        }

        // =========================================================================
        // FEN Generation & Persistent Game Save/Resume (F11)
        // =========================================================================
        public string GenerateFEN()
        {
            System.Text.StringBuilder fen = new System.Text.StringBuilder();

            // 1. Piece placement
            for (int r = 7; r >= 0; r--)
            {
                int emptyCount = 0;
                for (int f = 0; f < 8; f++)
                {
                    ChessPiece3D p = boardPieces[f, r];
                    if (p == null)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            fen.Append(emptyCount);
                            emptyCount = 0;
                        }
                        char c = GetPieceFenChar(p.pieceType, p.pieceColor);
                        fen.Append(c);
                    }
                }
                if (emptyCount > 0) fen.Append(emptyCount);
                if (r > 0) fen.Append('/');
            }

            // 2. Active turn
            fen.Append(' ').Append(currentTurn == PieceColor.White ? 'w' : 'b');

            // 3. Castling rights
            fen.Append(' ');
            bool whiteK = false, whiteQ = false, blackK = false, blackQ = false;
            ChessPiece3D wKing = FindKing(PieceColor.White);
            if (wKing != null && !wKing.hasMoved && wKing.file == 4 && wKing.rank == 0)
            {
                ChessPiece3D r1 = boardPieces[7, 0];
                if (r1 != null && r1.pieceType == PieceType.Rook && !r1.hasMoved) whiteK = true;
                ChessPiece3D r2 = boardPieces[0, 0];
                if (r2 != null && r2.pieceType == PieceType.Rook && !r2.hasMoved) whiteQ = true;
            }
            ChessPiece3D bKing = FindKing(PieceColor.Black);
            if (bKing != null && !bKing.hasMoved && bKing.file == 4 && bKing.rank == 7)
            {
                ChessPiece3D r1 = boardPieces[7, 7];
                if (r1 != null && r1.pieceType == PieceType.Rook && !r1.hasMoved) blackK = true;
                ChessPiece3D r2 = boardPieces[0, 7];
                if (r2 != null && r2.pieceType == PieceType.Rook && !r2.hasMoved) blackQ = true;
            }

            string castling = "";
            if (whiteK) castling += "K";
            if (whiteQ) castling += "Q";
            if (blackK) castling += "k";
            if (blackQ) castling += "q";
            fen.Append(string.IsNullOrEmpty(castling) ? "-" : castling);

            // 4. En Passant target square
            fen.Append(' ');
            if (_enPassantTargetSquare.HasValue)
            {
                char fileChar = (char)('a' + _enPassantTargetSquare.Value.x);
                int rankNum = _enPassantTargetSquare.Value.y + 1;
                fen.Append($"{fileChar}{rankNum}");
            }
            else
            {
                fen.Append('-');
            }

            // 5. Halfmove clock
            fen.Append(' ').Append(_halfMoveClock);

            // 6. Fullmove number
            int fullMove = Mathf.Max(1, (turnCounter + 1) / 2);
            fen.Append(' ').Append(fullMove);

            return fen.ToString();
        }

        private char GetPieceFenChar(PieceType type, PieceColor color)
        {
            char c = 'p';
            switch (type)
            {
                case PieceType.Pawn: c = 'p'; break;
                case PieceType.Knight: c = 'n'; break;
                case PieceType.Bishop: c = 'b'; break;
                case PieceType.Rook: c = 'r'; break;
                case PieceType.Queen: c = 'q'; break;
                case PieceType.King: c = 'k'; break;
            }
            return (color == PieceColor.White) ? char.ToUpper(c) : c;
        }

        public bool LoadFromFEN(string fen)
        {
            if (string.IsNullOrEmpty(fen)) return false;

            string[] parts = fen.Trim().Split(' ');
            if (parts.Length < 4) return false;

            // Clear existing pieces
            if (_piecesRoot != null) Destroy(_piecesRoot.gameObject);
            _piecesRoot = new GameObject("MechPiecesRoot").transform;
            _piecesRoot.SetParent(transform, false);
            Array.Clear(boardPieces, 0, boardPieces.Length);
            _moveHistoryStack.Clear();

            // 1. Piece placement
            string[] ranks = parts[0].Split('/');
            if (ranks.Length != 8) return false;

            for (int r = 0; r < 8; r++)
            {
                int rankIdx = 7 - r;
                int fileIdx = 0;
                string rankStr = ranks[r];

                foreach (char c in rankStr)
                {
                    if (char.IsDigit(c))
                    {
                        fileIdx += (c - '0');
                    }
                    else
                    {
                        PieceColor col = char.IsUpper(c) ? PieceColor.White : PieceColor.Black;
                        PieceType type = PieceType.Pawn;
                        char lower = char.ToLower(c);
                        if (lower == 'p') type = PieceType.Pawn;
                        else if (lower == 'n') type = PieceType.Knight;
                        else if (lower == 'b') type = PieceType.Bishop;
                        else if (lower == 'r') type = PieceType.Rook;
                        else if (lower == 'q') type = PieceType.Queen;
                        else if (lower == 'k') type = PieceType.King;

                        if (IsOnBoard(fileIdx, rankIdx))
                        {
                            SpawnPiece(type, col, fileIdx, rankIdx);
                        }
                        fileIdx++;
                    }
                }
            }

            // 2. Active turn
            currentTurn = (parts[1].ToLower() == "w") ? PieceColor.White : PieceColor.Black;

            // 3. En Passant
            if (parts[3] != "-" && parts[3].Length >= 2)
            {
                int ef = char.ToLower(parts[3][0]) - 'a';
                int er = parts[3][1] - '1';
                if (IsOnBoard(ef, er)) _enPassantTargetSquare = new Vector2Int(ef, er);
                else _enPassantTargetSquare = null;
            }
            else
            {
                _enPassantTargetSquare = null;
            }

            // 4. Halfmove clock
            if (parts.Length > 4 && int.TryParse(parts[4], out int hmc)) _halfMoveClock = hmc;
            else _halfMoveClock = 0;

            // 5. Fullmove number
            if (parts.Length > 5 && int.TryParse(parts[5], out int fmn))
            {
                turnCounter = (fmn * 2) - (currentTurn == PieceColor.White ? 1 : 0);
            }

            isGameOver = false;
            _lastMoveFromTile = null;
            _lastMoveToTile = null;
            ClearHighlights();

            if (UIManager2D.Instance != null)
            {
                UIManager2D.Instance.SetActiveTurn(currentTurn);
                UIManager2D.Instance.RecordMove($">>> LOADED GAME FROM FEN ({currentTurn}) <<<");
            }

            return true;
        }

        public void SaveCurrentGame()
        {
            if (isGameOver)
            {
                ClearSavedGame();
                return;
            }

            string fen = GenerateFEN();
            PlayerPrefs.SetString("AegisMech_Saved_FEN", fen);
            PlayerPrefs.SetInt("AegisMech_Has_Saved_Game", 1);
            PlayerPrefs.SetString("AegisMech_Saved_Mode", isVsAI ? "vs_ai" : "pass_play");
            PlayerPrefs.SetInt("AegisMech_Saved_AI_Level", aiLevel);
            if (UIManager2D.Instance != null)
            {
                PlayerPrefs.SetFloat("AegisMech_Saved_White_Time", UIManager2D.Instance.WhiteTime);
                PlayerPrefs.SetFloat("AegisMech_Saved_Black_Time", UIManager2D.Instance.BlackTime);
            }
            PlayerPrefs.Save();
        }

        public bool HasSavedGame()
        {
            return PlayerPrefs.GetInt("AegisMech_Has_Saved_Game", 0) == 1 &&
                   !string.IsNullOrEmpty(PlayerPrefs.GetString("AegisMech_Saved_FEN", ""));
        }

        public bool LoadSavedGame()
        {
            if (!HasSavedGame()) return false;
            string fen = PlayerPrefs.GetString("AegisMech_Saved_FEN", "");
            bool success = LoadFromFEN(fen);
            if (success)
            {
                string mode = PlayerPrefs.GetString("AegisMech_Saved_Mode", "vs_ai");
                isVsAI = (mode == "vs_ai");
                aiLevel = PlayerPrefs.GetInt("AegisMech_Saved_AI_Level", 2);
                float wTime = PlayerPrefs.GetFloat("AegisMech_Saved_White_Time", 600f);
                float bTime = PlayerPrefs.GetFloat("AegisMech_Saved_Black_Time", 600f);
                if (UIManager2D.Instance != null)
                {
                    UIManager2D.Instance.SetTimers(wTime, bTime);
                }
            }
            return success;
        }

        public void ClearSavedGame()
        {
            PlayerPrefs.DeleteKey("AegisMech_Saved_FEN");
            PlayerPrefs.SetInt("AegisMech_Has_Saved_Game", 0);
            PlayerPrefs.Save();
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
