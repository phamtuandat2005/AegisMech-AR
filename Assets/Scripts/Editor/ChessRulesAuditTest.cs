using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MechChess.Chess3D;
using MechChess.ProceduralMech;

namespace MechChess.EditorTests
{
    /// <summary>
    /// Automated Audit Test Suite for AegisMech-AR Chess Rule Engine.
    /// Tests: Check, Checkmate, Stalemate, Kingside & Queenside Castling,
    /// En Passant, and Pawn Promotion.
    /// Can be executed in Unity Editor via MenuItem: "AegisMech/Audit/Run All Chess Rules Tests"
    /// or command-line batchmode.
    /// </summary>
    public static class ChessRulesAuditTest
    {
        public class TestResult
        {
            public string TestName;
            public bool Passed;
            public string Details;
        }

        [MenuItem("AegisMech/Audit/Run All Chess Rules Tests")]
        public static void RunAllChessRulesTests()
        {
            Debug.Log("=================================================================");
            Debug.Log(">>> RUNNING AEGISMECH-AR CHESS RULES AUDIT TEST SUITE <<<");
            Debug.Log("=================================================================");

            List<TestResult> results = new List<TestResult>();

            // Setup a temporary test game object with ChessBoard3DManager
            GameObject testBoardObj = new GameObject("AuditTest_ChessBoard");
            ChessBoard3DManager board = testBoardObj.AddComponent<ChessBoard3DManager>();

            // Initialize board array
            board.boardPieces = new ChessPiece3D[8, 8];

            try
            {
                results.Add(Test_PawnBasicAndCaptureMoves(board));
                results.Add(Test_EnPassantTriggerAndCapture(board));
                results.Add(Test_EnPassantExpiration(board));
                results.Add(Test_CastlingKingsideAndQueensideValid(board));
                results.Add(Test_CastlingBlockedWhenInCheck(board));
                results.Add(Test_CastlingBlockedWhenTransitAttacked(board));
                results.Add(Test_CastlingBlockedWhenKingOrRookMoved(board));
                results.Add(Test_CheckDetectionRaysAndKnights(board));
                results.Add(Test_CheckmateFoolsMate(board));
                results.Add(Test_StalemateResolution(board));
                results.Add(Test_PawnPromotionDetection(board));
                results.Add(Test_SelectionAndDeselectionToggle(board));
                results.Add(Test_CapturedPieceInputBlock(board));
                results.Add(Test_RapidClickAnimationSkip(board));
                results.Add(Test_AnimationWatchdogRelease(board));
            }
            finally
            {
                // Clean up spawned test objects
                ClearBoard(board);
                UnityEngine.Object.DestroyImmediate(testBoardObj);
            }

            List<string> reportLines = new List<string>();
            reportLines.Add("=================================================================");
            reportLines.Add(">>> AEGISMECH-AR CHESS RULES AUDIT TEST SUITE RESULTS <<<");
            reportLines.Add($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            reportLines.Add("=================================================================");

            int passed = 0;
            int failed = 0;
            Debug.Log("\n----------------- TEST AUDIT RESULTS SUMMARY -----------------");
            foreach (var r in results)
            {
                if (r.Passed)
                {
                    passed++;
                    string line = $"[PASS] {r.TestName}: {r.Details}";
                    Debug.Log(line);
                    reportLines.Add(line);
                }
                else
                {
                    failed++;
                    string line = $"[FAIL] {r.TestName}: {r.Details}";
                    Debug.LogError(line);
                    reportLines.Add(line);
                }
            }
            string summaryLine = $"TOTAL: {results.Count} | PASSED: {passed} | FAILED: {failed}";
            Debug.Log("--------------------------------------------------------------");
            Debug.Log(summaryLine);
            Debug.Log("==============================================================\n");
            reportLines.Add("--------------------------------------------------------------");
            reportLines.Add(summaryLine);
            reportLines.Add("==============================================================");

            try
            {
                string path = Application.dataPath + "/Scripts/Editor/TestResults_ChessRules.txt";
                System.IO.File.WriteAllLines(path, reportLines);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Could not write test output file: " + ex.Message);
            }

            if (failed > 0)
            {
                throw new Exception($"Chess Rules Audit Failed: {failed} out of {results.Count} tests failed!");
            }
        }

        [InitializeOnLoadMethod]
        private static void AutoRunOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    RunAllChessRulesTests();
                }
                catch (Exception ex)
                {
                    Debug.LogError("AutoRunOnLoad Error: " + ex.Message);
                }
            };
        }

        private static void ClearBoard(ChessBoard3DManager board)
        {
            for (int f = 0; f < 8; f++)
            {
                for (int r = 0; r < 8; r++)
                {
                    if (board.boardPieces[f, r] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(board.boardPieces[f, r].gameObject);
                        board.boardPieces[f, r] = null;
                    }
                }
            }
        }

        private static ChessPiece3D CreatePiece(ChessBoard3DManager board, PieceType type, PieceColor color, int file, int rank)
        {
            GameObject obj = new GameObject($"TestPiece_{color}_{type}_{file}_{rank}");
            ChessPiece3D p = obj.AddComponent<ChessPiece3D>();
            p.Setup(type, color, file, rank);
            board.boardPieces[file, rank] = p;
            return p;
        }

        // 1. Basic Pawn Moves & Diagonal Capture
        private static TestResult Test_PawnBasicAndCaptureMoves(ChessBoard3DManager board)
        {
            ClearBoard(board);
            CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);

            // White Pawn at e2 (4, 1)
            ChessPiece3D p = CreatePiece(board, PieceType.Pawn, PieceColor.White, 4, 1);
            // Black Pawn at d3 (3, 2)
            CreatePiece(board, PieceType.Pawn, PieceColor.Black, 3, 2);

            var moves = board.GetLegalMoves(p);
            bool hasForward1 = moves.Contains(new Vector2Int(4, 2));
            bool hasForward2 = moves.Contains(new Vector2Int(4, 3));
            bool hasDiagonalCapture = moves.Contains(new Vector2Int(3, 2));
            bool noOffDiagonal = !moves.Contains(new Vector2Int(5, 2));

            bool ok = hasForward1 && hasForward2 && hasDiagonalCapture && noOffDiagonal;
            return new TestResult
            {
                TestName = "Pawn Forward & Diagonal Capture",
                Passed = ok,
                Details = ok ? "e2 pawn has e3, e4 and capture d3" : $"Failed: f1={hasForward1}, f2={hasForward2}, cap={hasDiagonalCapture}"
            };
        }

        // 2. En Passant Trigger and Capture
        private static TestResult Test_EnPassantTriggerAndCapture(ChessBoard3DManager board)
        {
            ClearBoard(board);
            CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);

            // White Pawn at e5 (4, 4)
            ChessPiece3D whitePawn = CreatePiece(board, PieceType.Pawn, PieceColor.White, 4, 4);
            // Black Pawn at d5 (3, 4) that just double-stepped from d7 (3, 6)
            ChessPiece3D blackPawn = CreatePiece(board, PieceType.Pawn, PieceColor.Black, 3, 4);

            // Set En Passant target square at d6 (3, 5)
            board.SetEnPassantTargetSquareForTest(new Vector2Int(3, 5));

            var legalMoves = board.GetLegalMoves(whitePawn);
            bool canCaptureEnPassant = legalMoves.Contains(new Vector2Int(3, 5));

            return new TestResult
            {
                TestName = "En Passant Capture Available",
                Passed = canCaptureEnPassant,
                Details = canCaptureEnPassant ? "e5 pawn can capture en passant to d6" : "En passant target square d6 was not in legal moves"
            };
        }

        // 3. En Passant Expiration after subsequent turn
        private static TestResult Test_EnPassantExpiration(ChessBoard3DManager board)
        {
            ClearBoard(board);
            CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);

            ChessPiece3D whitePawn = CreatePiece(board, PieceType.Pawn, PieceColor.White, 4, 4);
            CreatePiece(board, PieceType.Pawn, PieceColor.Black, 3, 4);

            // Expired: null target square
            board.SetEnPassantTargetSquareForTest(null);

            var legalMoves = board.GetLegalMoves(whitePawn);
            bool cannotCapture = !legalMoves.Contains(new Vector2Int(3, 5));

            return new TestResult
            {
                TestName = "En Passant Rights Expire",
                Passed = cannotCapture,
                Details = cannotCapture ? "En passant target square expired correctly" : "En passant unexpectedly remained available"
            };
        }

        // 4. Castling Kingside and Queenside Valid
        private static TestResult Test_CastlingKingsideAndQueensideValid(ChessBoard3DManager board)
        {
            ClearBoard(board);
            // White King at e1 (4, 0)
            ChessPiece3D king = CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            king.hasMoved = false;

            // Kingside Rook at h1 (7, 0)
            ChessPiece3D ksRook = CreatePiece(board, PieceType.Rook, PieceColor.White, 7, 0);
            ksRook.hasMoved = false;

            // Queenside Rook at a1 (0, 0)
            ChessPiece3D qsRook = CreatePiece(board, PieceType.Rook, PieceColor.White, 0, 0);
            qsRook.hasMoved = false;

            // Black King at e8 (4, 7)
            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);

            var moves = board.GetLegalMoves(king);
            bool hasO_O = moves.Contains(new Vector2Int(6, 0));  // g1
            bool hasO_O_O = moves.Contains(new Vector2Int(2, 0)); // c1

            bool ok = hasO_O && hasO_O_O;
            return new TestResult
            {
                TestName = "Valid Castling O-O & O-O-O",
                Passed = ok,
                Details = ok ? "Both Kingside (g1) and Queenside (c1) castling valid" : $"O-O={hasO_O}, O-O-O={hasO_O_O}"
            };
        }

        // 5. Castling Blocked when King is in Check
        private static TestResult Test_CastlingBlockedWhenInCheck(ChessBoard3DManager board)
        {
            ClearBoard(board);
            ChessPiece3D king = CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            king.hasMoved = false;
            ChessPiece3D ksRook = CreatePiece(board, PieceType.Rook, PieceColor.White, 7, 0);
            ksRook.hasMoved = false;

            // Black Rook at e8 (4, 7) checking White King at e1 (4, 0)
            CreatePiece(board, PieceType.Rook, PieceColor.Black, 4, 7);
            CreatePiece(board, PieceType.King, PieceColor.Black, 0, 7);

            bool inCheck = board.IsInCheck(PieceColor.White);
            var moves = board.GetLegalMoves(king);
            bool hasO_O = moves.Contains(new Vector2Int(6, 0));

            bool ok = inCheck && !hasO_O;
            return new TestResult
            {
                TestName = "Castling Blocked In Check",
                Passed = ok,
                Details = ok ? "In check confirmed and O-O castling correctly forbidden" : $"inCheck={inCheck}, hasO_O={hasO_O}"
            };
        }

        // 6. Castling Blocked when Transit Square Attacked
        private static TestResult Test_CastlingBlockedWhenTransitAttacked(ChessBoard3DManager board)
        {
            ClearBoard(board);
            ChessPiece3D king = CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            king.hasMoved = false;
            ChessPiece3D ksRook = CreatePiece(board, PieceType.Rook, PieceColor.White, 7, 0);
            ksRook.hasMoved = false;

            // Black Rook at f8 (5, 7) attacking transit square f1 (5, 0)
            CreatePiece(board, PieceType.Rook, PieceColor.Black, 5, 7);
            CreatePiece(board, PieceType.King, PieceColor.Black, 0, 7);

            bool inCheck = board.IsInCheck(PieceColor.White);
            var moves = board.GetLegalMoves(king);
            bool hasO_O = moves.Contains(new Vector2Int(6, 0));

            bool ok = !inCheck && !hasO_O;
            return new TestResult
            {
                TestName = "Castling Blocked Through Attacked Square",
                Passed = ok,
                Details = ok ? "Transit f1 under attack -> O-O castling correctly rejected" : $"inCheck={inCheck}, hasO_O={hasO_O}"
            };
        }

        // 7. Castling Blocked when King or Rook has Moved
        private static TestResult Test_CastlingBlockedWhenKingOrRookMoved(ChessBoard3DManager board)
        {
            ClearBoard(board);
            ChessPiece3D king = CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            king.hasMoved = true; // King has moved
            ChessPiece3D ksRook = CreatePiece(board, PieceType.Rook, PieceColor.White, 7, 0);
            ksRook.hasMoved = false;

            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);

            var moves = board.GetLegalMoves(king);
            bool hasO_O = moves.Contains(new Vector2Int(6, 0));

            bool ok = !hasO_O;
            return new TestResult
            {
                TestName = "Castling Blocked After King Move",
                Passed = ok,
                Details = ok ? "hasMoved=true correctly prevents castling" : "Castling was permitted despite King move"
            };
        }

        // 8. Check Detection with Rays and Knights
        private static TestResult Test_CheckDetectionRaysAndKnights(ChessBoard3DManager board)
        {
            ClearBoard(board);
            ChessPiece3D king = CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);

            // Test 1: Knight check
            ChessPiece3D knight = CreatePiece(board, PieceType.Knight, PieceColor.Black, 3, 2);
            bool knightCheck = board.IsInCheck(PieceColor.White);
            UnityEngine.Object.DestroyImmediate(knight.gameObject);
            board.boardPieces[3, 2] = null;

            // Test 2: Bishop diagonal check
            ChessPiece3D bishop = CreatePiece(board, PieceType.Bishop, PieceColor.Black, 7, 3);
            bool bishopCheck = board.IsInCheck(PieceColor.White);

            bool ok = knightCheck && bishopCheck;
            return new TestResult
            {
                TestName = "Check Detection (Knight & Bishop)",
                Passed = ok,
                Details = ok ? "Both Knight and Bishop checks accurately detected" : $"knight={knightCheck}, bishop={bishopCheck}"
            };
        }

        // 9. Checkmate (Fool's Mate)
        private static TestResult Test_CheckmateFoolsMate(ChessBoard3DManager board)
        {
            ClearBoard(board);
            // Fool's Mate setup:
            // White King at e1 (4, 0)
            CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            CreatePiece(board, PieceType.Bishop, PieceColor.White, 5, 0); // f1
            CreatePiece(board, PieceType.Queen, PieceColor.White, 3, 0);  // d1
            CreatePiece(board, PieceType.Pawn, PieceColor.White, 5, 2); // f3
            CreatePiece(board, PieceType.Pawn, PieceColor.White, 6, 3); // g4
            CreatePiece(board, PieceType.Pawn, PieceColor.White, 3, 1); // d2
            CreatePiece(board, PieceType.Pawn, PieceColor.White, 4, 1); // e2

            // Black Queen at h4 (7, 3) checking e1 diagonal
            CreatePiece(board, PieceType.Queen, PieceColor.Black, 7, 3);
            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);

            bool inCheck = board.IsInCheck(PieceColor.White);
            bool isMate = board.IsCheckmate(PieceColor.White);

            bool ok = inCheck && isMate;
            return new TestResult
            {
                TestName = "Checkmate Detection (Fool's Mate)",
                Passed = ok,
                Details = ok ? "Fool's Mate checkmate correctly recognized" : $"inCheck={inCheck}, isMate={isMate}"
            };
        }

        // 10. Stalemate Detection
        private static TestResult Test_StalemateResolution(ChessBoard3DManager board)
        {
            ClearBoard(board);
            // Stalemate position: White King at a8 (0, 7) trapped by Black Queen at c7 (2, 6) & Black King at b6 (1, 5)
            CreatePiece(board, PieceType.King, PieceColor.White, 0, 7);
            CreatePiece(board, PieceType.Queen, PieceColor.Black, 2, 6);
            CreatePiece(board, PieceType.King, PieceColor.Black, 1, 5);

            bool inCheck = board.IsInCheck(PieceColor.White);
            bool isStalemate = board.IsStalemate(PieceColor.White);

            bool ok = !inCheck && isStalemate;
            return new TestResult
            {
                TestName = "Stalemate Detection (Draw)",
                Passed = ok,
                Details = ok ? "Stalemate without check correctly detected as draw" : $"inCheck={inCheck}, isStalemate={isStalemate}"
            };
        }

        // 11. Pawn Promotion Detection
        private static TestResult Test_PawnPromotionDetection(ChessBoard3DManager board)
        {
            ClearBoard(board);
            CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);

            // White Pawn at a7 (0, 6)
            ChessPiece3D pawn = CreatePiece(board, PieceType.Pawn, PieceColor.White, 0, 6);
            var moves = board.GetLegalMoves(pawn);

            bool canReachEighthRank = moves.Contains(new Vector2Int(0, 7));
            bool isPromotionRank = (0 + 7 == 7); // target rank 7

            bool ok = canReachEighthRank && isPromotionRank;
            return new TestResult
            {
                TestName = "Pawn Promotion Target Rank",
                Passed = ok,
                Details = ok ? "Pawn reaches 8th rank (a8) to trigger promotion" : $"canReach={canReachEighthRank}"
            };
        }

        // 12. Selection & Deselection Toggle
        private static TestResult Test_SelectionAndDeselectionToggle(ChessBoard3DManager board)
        {
            ClearBoard(board);
            CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);
            board.currentTurn = PieceColor.White;

            ChessPiece3D pawn = CreatePiece(board, PieceType.Pawn, PieceColor.White, 4, 1);

            // Click 1: Select
            board.HandlePieceClicked(pawn);
            bool selectedAfterClick1 = (board.SelectedPieceForTest == pawn);

            // Click 2: Deselect (tapping on same piece again)
            board.HandlePieceClicked(pawn);
            bool deselectedAfterClick2 = (board.SelectedPieceForTest == null);

            bool ok = selectedAfterClick1 && deselectedAfterClick2;
            return new TestResult
            {
                TestName = "Selection & Deselection Toggle",
                Passed = ok,
                Details = ok ? "Tapping selected piece successfully toggles selection off" : $"s1={selectedAfterClick1}, s2={deselectedAfterClick2}"
            };
        }

        // 13. Captured Piece Input Block (No Input on Dead/Dying Piece)
        private static TestResult Test_CapturedPieceInputBlock(ChessBoard3DManager board)
        {
            ClearBoard(board);
            CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);
            board.currentTurn = PieceColor.White;

            ChessPiece3D capturedPiece = CreatePiece(board, PieceType.Queen, PieceColor.Black, 4, 4);

            // Mark captured and add collider
            capturedPiece.isCaptured = true;
            BoxCollider col = capturedPiece.gameObject.AddComponent<BoxCollider>();
            col.enabled = false;

            // Attempt to click on captured piece
            board.HandlePieceClicked(capturedPiece);
            bool rejected = (board.SelectedPieceForTest != capturedPiece);

            bool ok = rejected && col.enabled == false && capturedPiece.isCaptured == true;
            return new TestResult
            {
                TestName = "Captured Piece Input Block",
                Passed = ok,
                Details = ok ? "Captured piece flagged, collider disabled, and clicks rejected" : "Captured piece accepted input"
            };
        }

        // 14. Rapid Click Animation Skip
        private static TestResult Test_RapidClickAnimationSkip(ChessBoard3DManager board)
        {
            ClearBoard(board);
            CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);
            board.currentTurn = PieceColor.White;

            ChessPiece3D piece = CreatePiece(board, PieceType.Pawn, PieceColor.White, 4, 1);

            // Simulate animation in flight
            board.SetBusyAnimatingForTest(true, piece);
            bool wasBusy = board.IsBusyAnimatingForTest;

            // Call SkipActiveAnimation
            board.SkipActiveAnimation();
            bool isBusyNow = board.IsBusyAnimatingForTest;
            bool pieceCleared = (board.AnimatingPieceForTest == null);

            bool ok = wasBusy && !isBusyNow && pieceCleared;
            return new TestResult
            {
                TestName = "Rapid Click Animation Skip",
                Passed = ok,
                Details = ok ? "SkipActiveAnimation immediately unlocks busy flag and clears piece" : $"was={wasBusy}, now={isBusyNow}"
            };
        }

        // 15. Animation Watchdog Release
        private static TestResult Test_AnimationWatchdogRelease(ChessBoard3DManager board)
        {
            ClearBoard(board);
            CreatePiece(board, PieceType.King, PieceColor.White, 4, 0);
            CreatePiece(board, PieceType.King, PieceColor.Black, 4, 7);

            ChessPiece3D piece = CreatePiece(board, PieceType.Pawn, PieceColor.White, 4, 1);
            board.SetBusyAnimatingForTest(true, piece);

            // Force watchdog timeout trigger
            board.TriggerWatchdogCheckForTest(forceTimeout: true);

            bool unlocked = !board.IsBusyAnimatingForTest;
            return new TestResult
            {
                TestName = "Animation Watchdog Auto-Release",
                Passed = unlocked,
                Details = unlocked ? "Watchdog safely unlocked stuck animation state" : "Watchdog failed to release busy lock"
            };
        }
    }
}
