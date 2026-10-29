using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MechChess.UI;

namespace MechChess.EditorTests
{
    /// <summary>
    /// Automated Audit Test Suite for AegisMech-AR Tutorial System.
    /// Verifies all 4 Tabs (Cơ bản, Luật chơi, Các quân cờ, AR), 20 total pages,
    /// bilingual content, board diagram coordinate bounds, chess move accuracy,
    /// AR procedural graphics, pagination bounds, and caching.
    /// </summary>
    public static class TutorialSystemAuditTest
    {
        public class TestResult
        {
            public string TestName;
            public bool Passed;
            public string Details;
        }

        public static List<TestResult> ExecuteAllTests()
        {
            List<TestResult> results = new List<TestResult>();
            SafeRun(Test_TabCountsAndPageIntegrity, "TabCountsAndPageIntegrity", results);
            SafeRun(Test_BilingualContentNonEmpty, "BilingualContentNonEmpty", results);
            SafeRun(Test_StepByStepGuideIntegrity, "StepByStepGuideIntegrity", results);
            SafeRun(Test_BoardCoordinateBounds, "BoardCoordinateBounds", results);
            SafeRun(Test_PieceMovementRulesAccuracy, "PieceMovementRulesAccuracy", results);
            SafeRun(Test_SpecialChessRulesAccuracy, "SpecialChessRulesAccuracy", results);
            SafeRun(Test_ProceduralBoardDiagramGeneration, "ProceduralBoardDiagramGeneration", results);
            SafeRun(Test_ProceduralARDiagramGeneration, "ProceduralARDiagramGeneration", results);
            SafeRun(Test_SafePaginationClamping, "SafePaginationClamping", results);
            SafeRun(Test_BilingualLanguageToggle, "BilingualLanguageToggle", results);
            SafeRun(Test_ProceduralSpriteCaching, "ProceduralSpriteCaching", results);
            SafeRun(Test_UIInstantiationSafety, "UIInstantiationSafety", results);
            return results;
        }

        [MenuItem("AegisMech/Audit/Run All Tutorial System Tests")]
        public static void RunAllTutorialSystemTests()
        {
            Debug.Log("=================================================================");
            Debug.Log(">>> RUNNING AEGISMECH-AR TUTORIAL SYSTEM AUDIT TEST SUITE <<<");
            Debug.Log("=================================================================");

            List<TestResult> results = ExecuteAllTests();

            List<string> reportLines = new List<string>();
            reportLines.Add("=================================================================");
            reportLines.Add(">>> AEGISMECH-AR TUTORIAL SYSTEM AUDIT TEST SUITE RESULTS <<<");
            reportLines.Add($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            reportLines.Add("=================================================================");

            int passed = 0;
            int failed = 0;
            Debug.Log("\n----------------- TUTORIAL AUDIT RESULTS SUMMARY -----------------");
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
            Debug.Log("------------------------------------------------------------------");
            Debug.Log(summaryLine);
            Debug.Log("==================================================================\n");
            reportLines.Add("------------------------------------------------------------------");
            reportLines.Add(summaryLine);
            reportLines.Add("==================================================================");

            // Write report to persistent file
            try
            {
                string reportPath = Path.Combine(Application.dataPath, "Scripts", "Editor", "TestResults_TutorialSystem.txt");
                File.WriteAllLines(reportPath, reportLines);
                Debug.Log($"[TutorialAudit] Audit report written to: {reportPath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TutorialAudit] Failed to write report file: {ex.Message}");
            }
        }

        private static void SafeRun(Func<TestResult> testFunc, string testName, List<TestResult> results)
        {
            try
            {
                results.Add(testFunc());
            }
            catch (Exception ex)
            {
                results.Add(new TestResult
                {
                    TestName = testName,
                    Passed = false,
                    Details = $"Threw exception: {ex.Message}"
                });
            }
        }

        // Test 1: Page counts across 4 tabs
        public static TestResult Test_TabCountsAndPageIntegrity()
        {
            int cBasics = TutorialRepository.GetPageCount(TutorialTabType.Basics);
            int cRules = TutorialRepository.GetPageCount(TutorialTabType.Rules);
            int cPieces = TutorialRepository.GetPageCount(TutorialTabType.Pieces);
            int cAR = TutorialRepository.GetPageCount(TutorialTabType.AR);
            int total = cBasics + cRules + cPieces + cAR;

            bool pass = (cBasics == 5 && cRules == 5 && cPieces == 6 && cAR == 4 && total == 20);
            return new TestResult
            {
                TestName = "TabCountsAndPageIntegrity",
                Passed = pass,
                Details = $"Basics={cBasics}/5, Rules={cRules}/5, Pieces={cPieces}/6, AR={cAR}/4 (Total={total}/20)"
            };
        }

        // Test 2: Bilingual content completeness
        public static TestResult Test_BilingualContentNonEmpty()
        {
            int verified = 0;
            foreach (TutorialTabType tab in Enum.GetValues(typeof(TutorialTabType)))
            {
                var pages = TutorialRepository.GetPages(tab);
                foreach (var p in pages)
                {
                    if (string.IsNullOrEmpty(p.titleVi) || string.IsNullOrEmpty(p.titleEn) ||
                        string.IsNullOrEmpty(p.overviewVi) || string.IsNullOrEmpty(p.overviewEn) ||
                        string.IsNullOrEmpty(p.categoryBadgeVi) || string.IsNullOrEmpty(p.categoryBadgeEn) ||
                        string.IsNullOrEmpty(p.captionVi) || string.IsNullOrEmpty(p.captionEn))
                    {
                        return new TestResult
                        {
                            TestName = "BilingualContentNonEmpty",
                            Passed = false,
                            Details = $"Empty bilingual string in tab {tab}, page {p.pageIndex} ({p.pageId})"
                        };
                    }
                    verified++;
                }
            }

            return new TestResult
            {
                TestName = "BilingualContentNonEmpty",
                Passed = true,
                Details = $"All {verified} pages have complete non-empty Vietnamese & English titles, badges, overviews, and captions."
            };
        }

        // Test 3: Steps guide completeness
        public static TestResult Test_StepByStepGuideIntegrity()
        {
            int totalSteps = 0;
            foreach (TutorialTabType tab in Enum.GetValues(typeof(TutorialTabType)))
            {
                var pages = TutorialRepository.GetPages(tab);
                foreach (var p in pages)
                {
                    if (p.steps == null || p.steps.Count != 3)
                    {
                        return new TestResult
                        {
                            TestName = "StepByStepGuideIntegrity",
                            Passed = false,
                            Details = $"Page {p.pageId} has {p.steps?.Count ?? 0} steps, expected exactly 3."
                        };
                    }

                    for (int s = 0; s < p.steps.Count; s++)
                    {
                        var step = p.steps[s];
                        if (string.IsNullOrEmpty(step.number) || string.IsNullOrEmpty(step.titleVi) || 
                            string.IsNullOrEmpty(step.titleEn) || string.IsNullOrEmpty(step.descVi) || 
                            string.IsNullOrEmpty(step.descEn))
                        {
                            return new TestResult
                            {
                                TestName = "StepByStepGuideIntegrity",
                                Passed = false,
                                Details = $"Incomplete step {s} in page {p.pageId}."
                            };
                        }
                        totalSteps++;
                    }
                }
            }

            return new TestResult
            {
                TestName = "StepByStepGuideIntegrity",
                Passed = true,
                Details = $"All {totalSteps} individual steps across all 20 pages contain complete numbers, titles, and descriptions."
            };
        }

        // Test 4: Coordinate bounds on 8x8 chessboard
        public static TestResult Test_BoardCoordinateBounds()
        {
            int totalCoords = 0;
            foreach (TutorialTabType tab in Enum.GetValues(typeof(TutorialTabType)))
            {
                if (tab == TutorialTabType.AR) continue; // AR uses dedicated AR graphics

                var pages = TutorialRepository.GetPages(tab);
                foreach (var p in pages)
                {
                    if (p.hasSelectedSquare)
                    {
                        if (p.selectedSquare.x < 0 || p.selectedSquare.x > 7 || p.selectedSquare.y < 0 || p.selectedSquare.y > 7)
                        {
                            return new TestResult
                            {
                                TestName = "BoardCoordinateBounds",
                                Passed = false,
                                Details = $"Selected square out of bounds in {p.pageId}: {p.selectedSquare}"
                            };
                        }
                        totalCoords++;
                    }

                    if (p.moveSquares != null)
                    {
                        foreach (var m in p.moveSquares)
                        {
                            if (m.x < 0 || m.x > 7 || m.y < 0 || m.y > 7)
                            {
                                return new TestResult
                                {
                                    TestName = "BoardCoordinateBounds",
                                    Passed = false,
                                    Details = $"Move square out of bounds in {p.pageId}: {m}"
                                };
                            }
                            totalCoords++;
                        }
                    }

                    if (p.captureSquares != null)
                    {
                        foreach (var c in p.captureSquares)
                        {
                            if (c.x < 0 || c.x > 7 || c.y < 0 || c.y > 7)
                            {
                                return new TestResult
                                {
                                    TestName = "BoardCoordinateBounds",
                                    Passed = false,
                                    Details = $"Capture square out of bounds in {p.pageId}: {c}"
                                };
                            }
                            totalCoords++;
                        }
                    }

                    if (p.pieces != null)
                    {
                        foreach (var piece in p.pieces)
                        {
                            if (piece.file < 0 || piece.file > 7 || piece.rank < 0 || piece.rank > 7)
                            {
                                return new TestResult
                                {
                                    TestName = "BoardCoordinateBounds",
                                    Passed = false,
                                    Details = $"Piece marker out of bounds in {p.pageId}: ({piece.file}, {piece.rank})"
                                };
                            }
                            totalCoords++;
                        }
                    }
                }
            }

            return new TestResult
            {
                TestName = "BoardCoordinateBounds",
                Passed = true,
                Details = $"All {totalCoords} tactical coordinates strictly satisfy 0 <= file <= 7 and 0 <= rank <= 7."
            };
        }

        // Test 5: Piece movement rules accuracy
        public static TestResult Test_PieceMovementRulesAccuracy()
        {
            var piecesPages = TutorialRepository.GetPages(TutorialTabType.Pieces);

            // King (page 0): 8 adjacent squares from d4 (3, 3)
            var king = piecesPages[0];
            bool kingValid = (king.hasSelectedSquare && king.selectedSquare == new Vector2Int(3, 3) && king.moveSquares.Count == 8);

            // Queen (page 1): 27 ray squares from d4 (3, 3)
            var queen = piecesPages[1];
            bool queenValid = (queen.hasSelectedSquare && queen.selectedSquare == new Vector2Int(3, 3) && queen.moveSquares.Count == 27);

            // Rook (page 2): 14 orthogonal ray squares from d4 (3, 3)
            var rook = piecesPages[2];
            bool rookValid = (rook.hasSelectedSquare && rook.selectedSquare == new Vector2Int(3, 3) && rook.moveSquares.Count == 14);

            // Bishop (page 3): 13 diagonal ray squares from d4 (3, 3)
            var bishop = piecesPages[3];
            bool bishopValid = (bishop.hasSelectedSquare && bishop.selectedSquare == new Vector2Int(3, 3) && bishop.moveSquares.Count == 13);

            // Knight (page 4): 8 L-shape jump destinations from d4 (3, 3)
            var knight = piecesPages[4];
            bool knightValid = (knight.hasSelectedSquare && knight.selectedSquare == new Vector2Int(3, 3) && knight.moveSquares.Count == 8);

            // Pawn (page 5): initial advance 2 squares from d2 (3, 1) to d3 & d4, and 2 capture squares
            var pawn = piecesPages[5];
            bool pawnValid = (pawn.hasSelectedSquare && pawn.selectedSquare == new Vector2Int(3, 1) && pawn.moveSquares.Count == 2 && pawn.captureSquares.Count == 2);

            bool pass = kingValid && queenValid && rookValid && bishopValid && knightValid && pawnValid;
            return new TestResult
            {
                TestName = "PieceMovementRulesAccuracy",
                Passed = pass,
                Details = $"King={kingValid} (8 moves), Queen={queenValid} (27 moves), Rook={rookValid} (14 moves), Bishop={bishopValid} (13 moves), Knight={knightValid} (8 moves), Pawn={pawnValid} (2 moves + 2 captures)."
            };
        }

        // Test 6: Special chess rules accuracy (Castling, En Passant, Promotion, Check)
        public static TestResult Test_SpecialChessRulesAccuracy()
        {
            var rulesPages = TutorialRepository.GetPages(TutorialTabType.Rules);

            // Check (page 1): Attacking bishop c5 threatening king f2
            var checkPage = rulesPages[1];
            bool checkPass = (checkPage.selectedSquare == new Vector2Int(5, 1) && checkPage.captureSquares.Contains(new Vector2Int(2, 4)));

            // Castling (page 3): King e1, castling destinations g1 (Kingside) and c1 (Queenside)
            var castlingPage = rulesPages[3];
            bool castlingPass = (castlingPage.selectedSquare == new Vector2Int(4, 0) && 
                                 castlingPage.specialSquares.Contains(new Vector2Int(6, 0)) && 
                                 castlingPage.specialSquares.Contains(new Vector2Int(2, 0)));

            // En Passant & Promotion (page 4): En Passant target square d6 (3, 5) and Promotion square e8 (4, 7)
            var epPage = rulesPages[4];
            bool epPass = (epPage.captureSquares.Contains(new Vector2Int(3, 5)) && 
                           epPage.specialSquares.Contains(new Vector2Int(4, 7)));

            bool pass = checkPass && castlingPass && epPass;
            return new TestResult
            {
                TestName = "SpecialChessRulesAccuracy",
                Passed = pass,
                Details = $"Check Threat={checkPass}, Castling Destinations (g1/c1)={castlingPass}, En Passant (d6) & Promotion (e8)={epPass}."
            };
        }

        // Test 7: Procedural board diagram generation
        public static TestResult Test_ProceduralBoardDiagramGeneration()
        {
            int diagramsGenerated = 0;
            foreach (TutorialTabType tab in Enum.GetValues(typeof(TutorialTabType)))
            {
                if (tab == TutorialTabType.AR) continue;
                var pages = TutorialRepository.GetPages(tab);
                foreach (var p in pages)
                {
                    Sprite s = ArcaneAssetLoader.GetCustomBoardDiagram(256, p.hasSelectedSquare, p.selectedSquare, p.moveSquares, p.captureSquares, p.specialSquares);
                    if (s == null || s.texture == null)
                    {
                        return new TestResult
                        {
                            TestName = "ProceduralBoardDiagramGeneration",
                            Passed = false,
                            Details = $"Failed to generate procedural diagram for page {p.pageId}."
                        };
                    }
                    diagramsGenerated++;
                }
            }

            return new TestResult
            {
                TestName = "ProceduralBoardDiagramGeneration",
                Passed = true,
                Details = $"Successfully generated {diagramsGenerated} crisp procedural chessboard diagrams."
            };
        }

        // Test 8: Procedural AR diagrams generation
        public static TestResult Test_ProceduralARDiagramGeneration()
        {
            string[] arTypes = { "ar_permissions", "ar_plane_scan", "ar_scale_controls", "ar_ai_scanner" };
            foreach (var type in arTypes)
            {
                Sprite s = ArcaneAssetLoader.GetARDiagramSprite(256, type);
                if (s == null || s.texture == null)
                {
                    return new TestResult
                    {
                        TestName = "ProceduralARDiagramGeneration",
                        Passed = false,
                        Details = $"Failed to generate AR diagram for type: {type}."
                    };
                }
            }

            return new TestResult
            {
                TestName = "ProceduralARDiagramGeneration",
                Passed = true,
                Details = $"All 4 specialized AR tactical diagrams generated with valid procedural textures."
            };
        }

        // Test 9: Safe pagination clamping
        public static TestResult Test_SafePaginationClamping()
        {
            var pNeg = TutorialRepository.GetPage(TutorialTabType.Basics, -5);
            var pPos = TutorialRepository.GetPage(TutorialTabType.Basics, 999);
            var pValid = TutorialRepository.GetPage(TutorialTabType.Basics, 2);

            bool pass = (pNeg != null && pNeg.pageIndex == 0 &&
                         pPos != null && pPos.pageIndex == 4 &&
                         pValid != null && pValid.pageIndex == 2);

            return new TestResult
            {
                TestName = "SafePaginationClamping",
                Passed = pass,
                Details = $"Underflow clamped to index 0 ({pNeg?.pageId}), overflow clamped to index 4 ({pPos?.pageId})."
            };
        }

        // Test 10: Bilingual language toggle
        public static TestResult Test_BilingualLanguageToggle()
        {
            string titleVi = TutorialRepository.GetTabTitle(TutorialTabType.Basics);
            AegisLocalization.SetLanguage(AppLanguage.English);
            string titleEn = TutorialRepository.GetTabTitle(TutorialTabType.Basics);

            // Reset back to Vietnamese
            AegisLocalization.SetLanguage(AppLanguage.Vietnamese);

            bool pass = (titleVi == "Cơ bản" && titleEn == "Basics");
            return new TestResult
            {
                TestName = "BilingualLanguageToggle",
                Passed = pass,
                Details = $"Vietnamese='{titleVi}', English='{titleEn}', toggling verified."
            };
        }

        // Test 11: Procedural sprite caching
        public static TestResult Test_ProceduralSpriteCaching()
        {
            Sprite s1 = ArcaneAssetLoader.GetCustomBoardDiagram(200, false, Vector2Int.zero, null, null, null);
            Sprite s2 = ArcaneAssetLoader.GetCustomBoardDiagram(200, false, Vector2Int.zero, null, null, null);

            bool pass = ReferenceEquals(s1, s2);
            return new TestResult
            {
                TestName = "ProceduralSpriteCaching",
                Passed = pass,
                Details = $"Identical parameters returned same cached Sprite reference: {pass}."
            };
        }

        // Test 12: UI Instantiation & Component safety
        public static TestResult Test_UIInstantiationSafety()
        {
            GameObject testCanvas = new GameObject("Test_Tutorial_Canvas");
            Canvas c = testCanvas.AddComponent<Canvas>();
            testCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            try
            {
                // Verify Arcane glass box generation
                Sprite box = ArcaneAssetLoader.GetGlassBoxSprite(300, 100, 12f, Color.black, Color.cyan, 1.5f);
                if (box == null) throw new Exception("GlassBox sprite returned null.");

                // Verify circle badge generation
                Sprite circle = ArcaneAssetLoader.GetCircleSprite(32, Color.cyan, Color.white, 1.5f);
                if (circle == null) throw new Exception("Circle sprite returned null.");

                return new TestResult
                {
                    TestName = "UIInstantiationSafety",
                    Passed = true,
                    Details = "Canvas, sprites, and procedural assets instantiate without throwing any exceptions."
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "UIInstantiationSafety",
                    Passed = false,
                    Details = $"Exception during UI instantiation test: {ex.Message}"
                };
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testCanvas);
            }
        }
    }
}
