using System.Collections.Generic;
using System.Text;

namespace WarChess.QA
{
    /// <summary>
    /// Result of a single QA test.
    /// </summary>
    public class QATestResult
    {
        public string TestName;
        public bool Passed;
        public string Details;
        public string FailureReason;

        public static QATestResult Pass(string name, string details = "")
        {
            return new QATestResult { TestName = name, Passed = true, Details = details };
        }

        public static QATestResult Fail(string name, string reason, string details = "")
        {
            return new QATestResult { TestName = name, Passed = false, FailureReason = reason, Details = details };
        }
    }

    /// <summary>
    /// Aggregated report from running all QA tests.
    /// </summary>
    public class QAReport
    {
        public int TotalTests;
        public int Passed;
        public int Failed;
        public List<QATestResult> Results = new List<QATestResult>();

        /// <summary>
        /// Generates a human-readable report.
        /// </summary>
        public static string FormatReport(QAReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════╗");
            sb.AppendLine("║         WARCHESS QA CORRECTNESS REPORT          ║");
            sb.AppendLine("╚══════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Total: {report.TotalTests}  Passed: {report.Passed}  Failed: {report.Failed}");
            sb.AppendLine();

            if (report.Failed > 0)
            {
                sb.AppendLine("=== FAILURES ===");
                foreach (var r in report.Results)
                {
                    if (!r.Passed)
                    {
                        sb.AppendLine($"  FAIL: {r.TestName}");
                        sb.AppendLine($"        Reason: {r.FailureReason}");
                        if (!string.IsNullOrEmpty(r.Details))
                            sb.AppendLine($"        Details: {r.Details}");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("=== ALL RESULTS ===");
            foreach (var r in report.Results)
            {
                string status = r.Passed ? "PASS" : "FAIL";
                sb.AppendLine($"  [{status}] {r.TestName}");
            }

            return sb.ToString();
        }
    }
}
