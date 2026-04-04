using System.Collections.Generic;
using WarChess.Config;
using WarChess.QA.Tests;

namespace WarChess.QA
{
    /// <summary>
    /// Orchestrates all correctness tests across every game system.
    /// Runs all test suites and produces a unified QA report.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class GameQATester
    {
        private readonly GameConfigData _config;

        public GameQATester(GameConfigData config)
        {
            _config = config;
        }

        /// <summary>
        /// Runs every correctness test and returns a full report.
        /// </summary>
        public QAReport RunAll()
        {
            var results = new List<QATestResult>();

            // Battle Engine
            results.AddRange(BattleEngineTests.RunAll(_config));

            // Damage System
            results.AddRange(DamageSystemTests.RunAll(_config));

            // Unit Stats
            results.AddRange(UnitStatTests.RunAll(_config));

            // Movement & Flanking
            results.AddRange(MovementAndFlankingTests.RunAll(_config));

            // Formations
            results.AddRange(FormationTests.RunAll(_config));

            // Commanders
            results.AddRange(CommanderTests.RunAll(_config));

            // Line of Sight
            results.AddRange(LineOfSightTests.RunAll(_config));

            // Campaign Data
            results.AddRange(CampaignDataTests.RunAll(_config));

            // Multiplayer
            results.AddRange(MultiplayerTests.RunAll(_config));

            // Grid
            results.AddRange(GridTests.RunAll(_config));

            // Build report
            var report = new QAReport();
            report.Results = results;
            report.TotalTests = results.Count;
            foreach (var r in results)
            {
                if (r.Passed) report.Passed++;
                else report.Failed++;
            }

            return report;
        }

        /// <summary>
        /// Convenience: run all tests and return formatted report string.
        /// </summary>
        public string RunAllAndFormat()
        {
            return QAReport.FormatReport(RunAll());
        }
    }
}
