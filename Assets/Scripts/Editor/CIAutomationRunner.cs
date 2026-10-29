#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MechChess.EditorTests
{
    /// <summary>
    /// Production CI Automation Test Runner for AegisMech-AR.
    /// Executes all Editor and Audit test suites in batchmode or inside the Unity Editor,
    /// formats test outputs into standard NUnit 3 XML reports for GitHub Actions and GameCI,
    /// and reliably exits with non-zero exit codes on failure.
    ///
    /// Batchmode command line invocation:
    /// -executeMethod MechChess.EditorTests.CIAutomationRunner.RunBatchAudit
    /// </summary>
    public static class CIAutomationRunner
    {
        [MenuItem("AegisMech/CI/Run All Automated Tests & Export XML", false, 10)]
        public static void RunBatchAudit()
        {
            DateTime startTime = DateTime.UtcNow;
            Debug.Log("================================================================================");
            Debug.Log(">>> [CI] STARTING AEGISMECH-AR AUTOMATED AUDIT & EDITMODE TEST RUNNER <<<");
            Debug.Log($"Timestamp (UTC): {startTime:yyyy-MM-dd HH:mm:ss}Z");
            Debug.Log($"Unity Editor: {Application.unityVersion} | BatchMode: {Application.isBatchMode}");
            Debug.Log("================================================================================");

            // 1. Run Chess Rules Test Suite (15 tests)
            List<ChessRulesAuditTest.TestResult> chessResults = new List<ChessRulesAuditTest.TestResult>();
            try
            {
                chessResults = ChessRulesAuditTest.ExecuteAllTests();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CI] Exception executing ChessRulesAuditTest: {ex}");
                chessResults.Add(new ChessRulesAuditTest.TestResult
                {
                    TestName = "ChessRulesExecutionCrash",
                    Passed = false,
                    Details = $"Unhandled exception in test harness: {ex.Message}"
                });
            }

            // 2. Run Tutorial System Test Suite (12 tests)
            List<TutorialSystemAuditTest.TestResult> tutorialResults = new List<TutorialSystemAuditTest.TestResult>();
            try
            {
                tutorialResults = TutorialSystemAuditTest.ExecuteAllTests();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CI] Exception executing TutorialSystemAuditTest: {ex}");
                tutorialResults.Add(new TutorialSystemAuditTest.TestResult
                {
                    TestName = "TutorialExecutionCrash",
                    Passed = false,
                    Details = $"Unhandled exception in test harness: {ex.Message}"
                });
            }

            DateTime endTime = DateTime.UtcNow;
            TimeSpan duration = endTime - startTime;

            // 3. Collate and summarize
            int totalTests = chessResults.Count + tutorialResults.Count;
            int totalPassed = 0;
            int totalFailed = 0;

            Debug.Log("\n------------------- CHESS RULES SUITE (15 TESTS) -------------------");
            foreach (var r in chessResults)
            {
                if (r.Passed)
                {
                    totalPassed++;
                    Debug.Log($"[PASS] {r.TestName} -> {r.Details}");
                }
                else
                {
                    totalFailed++;
                    Debug.LogError($"[FAIL] {r.TestName} -> {r.Details}");
                }
            }

            Debug.Log("\n----------------- TUTORIAL SYSTEM SUITE (12 TESTS) -----------------");
            foreach (var r in tutorialResults)
            {
                if (r.Passed)
                {
                    totalPassed++;
                    Debug.Log($"[PASS] {r.TestName} -> {r.Details}");
                }
                else
                {
                    totalFailed++;
                    Debug.LogError($"[FAIL] {r.TestName} -> {r.Details}");
                }
            }

            Debug.Log("\n================================================================================");
            Debug.Log($"[CI RESULT] TOTAL: {totalTests} | PASSED: {totalPassed} | FAILED: {totalFailed} | DURATION: {duration.TotalSeconds:F2}s");
            Debug.Log("================================================================================");

            // 4. Generate Standard NUnit 3 XML Test Results
            try
            {
                string xmlContent = GenerateNUnit3Xml(chessResults, tutorialResults, startTime, endTime, duration);

                // Export to both artifacts/ and TestResults/ for GameCI and GitHub Actions reporters
                string[] targetDirs = new string[]
                {
                    Path.Combine(Application.dataPath, "../artifacts"),
                    Path.Combine(Application.dataPath, "../TestResults")
                };

                foreach (var dir in targetDirs)
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    string xmlPath = Path.Combine(dir, "editmode-results.xml");
                    File.WriteAllText(xmlPath, xmlContent, Encoding.UTF8);
                    Debug.Log($"[CI] Exported NUnit 3 XML report to: {xmlPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CI] Failed to serialize NUnit 3 XML test report: {ex.Message}");
            }

            // 5. Exit or Notify
            if (Application.isBatchMode)
            {
                int exitCode = (totalFailed == 0) ? 0 : 1;
                Debug.Log($"[CI] Exiting Unity BatchMode with code: {exitCode}");
                EditorApplication.Exit(exitCode);
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Audit Test Complete",
                    $"Tests: {totalTests}\nPassed: {totalPassed}\nFailed: {totalFailed}\nDuration: {duration.TotalSeconds:F2}s",
                    "OK"
                );
            }
        }

        private static string GenerateNUnit3Xml(
            List<ChessRulesAuditTest.TestResult> chessResults,
            List<TutorialSystemAuditTest.TestResult> tutorialResults,
            DateTime startTime,
            DateTime endTime,
            TimeSpan duration)
        {
            int total = chessResults.Count + tutorialResults.Count;
            int failed = 0;
            foreach (var r in chessResults) if (!r.Passed) failed++;
            foreach (var r in tutorialResults) if (!r.Passed) failed++;
            int passed = total - failed;

            string overallResult = failed == 0 ? "Passed" : "Failed";

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = false
            };

            using (StringWriter sw = new StringWriter())
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("test-run");
                writer.WriteAttributeString("id", "2");
                writer.WriteAttributeString("testcasecount", total.ToString());
                writer.WriteAttributeString("result", overallResult);
                writer.WriteAttributeString("total", total.ToString());
                writer.WriteAttributeString("passed", passed.ToString());
                writer.WriteAttributeString("failed", failed.ToString());
                writer.WriteAttributeString("inconclusive", "0");
                writer.WriteAttributeString("skipped", "0");
                writer.WriteAttributeString("asserts", total.ToString());
                writer.WriteAttributeString("engine-version", "3.13.0");
                writer.WriteAttributeString("clr-version", Environment.Version.ToString());
                writer.WriteAttributeString("start-time", startTime.ToString("yyyy-MM-dd HH:mm:ssZ"));
                writer.WriteAttributeString("end-time", endTime.ToString("yyyy-MM-dd HH:mm:ssZ"));
                writer.WriteAttributeString("duration", duration.TotalSeconds.ToString("F3"));

                // Root TestSuite
                writer.WriteStartElement("test-suite");
                writer.WriteAttributeString("type", "Assembly");
                writer.WriteAttributeString("id", "100");
                writer.WriteAttributeString("name", "Assembly-CSharp-Editor.dll");
                writer.WriteAttributeString("fullname", "Assembly-CSharp-Editor.dll");
                writer.WriteAttributeString("testcasecount", total.ToString());
                writer.WriteAttributeString("result", overallResult);
                writer.WriteAttributeString("duration", duration.TotalSeconds.ToString("F3"));
                writer.WriteAttributeString("total", total.ToString());
                writer.WriteAttributeString("passed", passed.ToString());
                writer.WriteAttributeString("failed", failed.ToString());

                // Fixture 1: ChessRulesAuditTest
                WriteFixture(writer, "101", "ChessRulesAuditTest", "MechChess.EditorTests.ChessRulesAuditTest", chessResults);

                // Fixture 2: TutorialSystemAuditTest
                WriteFixture(writer, "102", "TutorialSystemAuditTest", "MechChess.EditorTests.TutorialSystemAuditTest", tutorialResults);

                writer.WriteEndElement(); // test-suite Assembly
                writer.WriteEndElement(); // test-run
                writer.WriteEndDocument();
                writer.Flush();

                return sw.ToString();
            }
        }

        private static void WriteFixture<T>(
            XmlWriter writer,
            string fixtureId,
            string fixtureName,
            string fixtureFullName,
            List<T> results)
        {
            int fTotal = results.Count;
            int fFailed = 0;
            foreach (var item in results)
            {
                dynamic r = item;
                if (!r.Passed) fFailed++;
            }
            int fPassed = fTotal - fFailed;
            string fResult = fFailed == 0 ? "Passed" : "Failed";

            writer.WriteStartElement("test-suite");
            writer.WriteAttributeString("type", "TestFixture");
            writer.WriteAttributeString("id", fixtureId);
            writer.WriteAttributeString("name", fixtureName);
            writer.WriteAttributeString("fullname", fixtureFullName);
            writer.WriteAttributeString("testcasecount", fTotal.ToString());
            writer.WriteAttributeString("result", fResult);
            writer.WriteAttributeString("total", fTotal.ToString());
            writer.WriteAttributeString("passed", fPassed.ToString());
            writer.WriteAttributeString("failed", fFailed.ToString());

            int caseIndex = 1;
            foreach (var item in results)
            {
                dynamic r = item;
                string testId = $"{fixtureId}_{caseIndex:D3}";
                string testName = (string)r.TestName;
                bool passed = (bool)r.Passed;
                string details = (string)r.Details ?? "";

                writer.WriteStartElement("test-case");
                writer.WriteAttributeString("id", testId);
                writer.WriteAttributeString("name", testName);
                writer.WriteAttributeString("fullname", $"{fixtureFullName}.{testName}");
                writer.WriteAttributeString("methodname", testName);
                writer.WriteAttributeString("classname", fixtureFullName);
                writer.WriteAttributeString("result", passed ? "Passed" : "Failed");
                writer.WriteAttributeString("duration", "0.005");
                writer.WriteAttributeString("asserts", "1");

                if (!passed)
                {
                    writer.WriteStartElement("failure");
                    writer.WriteStartElement("message");
                    writer.WriteCData(details);
                    writer.WriteEndElement(); // message
                    writer.WriteEndElement(); // failure
                }
                else
                {
                    writer.WriteStartElement("output");
                    writer.WriteCData(details);
                    writer.WriteEndElement(); // output
                }

                writer.WriteEndElement(); // test-case
                caseIndex++;
            }

            writer.WriteEndElement(); // test-suite TestFixture
        }
    }
}
#endif
