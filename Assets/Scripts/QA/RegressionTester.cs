using System;
using System.Collections.Generic;
using System.Text;
using WarChess.Battle;
using WarChess.Commanders;
using WarChess.Config;
using WarChess.Core;
using WarChess.Terrain;
using WarChess.Units;

namespace WarChess.QA
{
    /// <summary>
    /// Snapshot of a single battle scenario's deterministic output.
    /// Used for regression comparison across code changes.
    /// </summary>
    public class BattleSnapshot
    {
        /// <summary>Human-readable label describing the scenario.</summary>
        public string Label;

        /// <summary>The fixed seed used for the battle RNG.</summary>
        public int Seed;

        /// <summary>The battle outcome (PlayerWin, EnemyWin, Draw).</summary>
        public BattleOutcome ExpectedOutcome;

        /// <summary>Total rounds played before the battle ended.</summary>
        public int ExpectedRounds;

        /// <summary>Deterministic hash of the full event log.</summary>
        public int EventLogHash;
    }

    /// <summary>
    /// A single divergence between expected and actual battle results.
    /// </summary>
    public class RegressionDivergence
    {
        /// <summary>Label of the scenario that diverged.</summary>
        public string Label;

        /// <summary>Description of the expected value.</summary>
        public string Expected;

        /// <summary>Description of the actual value.</summary>
        public string Actual;
    }

    /// <summary>
    /// Summary report of a regression test run.
    /// </summary>
    public class RegressionReport
    {
        /// <summary>Total number of scenarios tested.</summary>
        public int TotalScenarios;

        /// <summary>Number of scenarios that matched expectations.</summary>
        public int Matched;

        /// <summary>Number of scenarios that diverged from expectations.</summary>
        public int Diverged;

        /// <summary>Details of each divergence.</summary>
        public List<RegressionDivergence> Divergences;
    }

    /// <summary>
    /// Snapshot-based regression tester for the WarChess battle engine.
    /// Runs a fixed set of battle scenarios with known seeds and armies,
    /// captures deterministic snapshots, and compares across runs to detect
    /// unintended changes in battle logic.
    /// Pure C# -- no Unity dependencies.
    /// </summary>
    public class RegressionTester
    {
        private readonly GameConfigData _config;

        /// <summary>
        /// Creates a new regression tester with the given game config.
        /// </summary>
        /// <param name="config">Game configuration to use for all scenarios.</param>
        public RegressionTester(GameConfigData config)
        {
            _config = config;
        }

        /// <summary>
        /// Runs the full regression suite by generating a baseline and immediately
        /// re-running all scenarios to verify determinism. If any scenario produces
        /// different results on the second run, it is flagged as a divergence.
        /// </summary>
        /// <returns>A report showing whether all scenarios are deterministic.</returns>
        public RegressionReport RunRegressionSuite()
        {
            var baseline = GenerateBaseline();
            return CompareAgainstBaseline(baseline);
        }

        /// <summary>
        /// Runs all defined scenarios and captures a snapshot of each.
        /// </summary>
        /// <returns>List of battle snapshots representing the current baseline.</returns>
        public List<BattleSnapshot> GenerateBaseline()
        {
            var scenarios = BuildScenarios();
            var snapshots = new List<BattleSnapshot>(scenarios.Count);

            foreach (var scenario in scenarios)
            {
                var result = scenario.Run();
                int hash = ComputeEventLogHash(result.Events);

                snapshots.Add(new BattleSnapshot
                {
                    Label = scenario.Label,
                    Seed = scenario.Seed,
                    ExpectedOutcome = result.Outcome,
                    ExpectedRounds = result.RoundsPlayed,
                    EventLogHash = hash
                });
            }

            return snapshots;
        }

        /// <summary>
        /// Re-runs all scenarios and compares outcomes, round counts, and event
        /// log hashes against the provided baseline. Any mismatch is recorded
        /// as a divergence.
        /// </summary>
        /// <param name="baseline">Previously captured snapshots to compare against.</param>
        /// <returns>A report detailing matched and diverged scenarios.</returns>
        public RegressionReport CompareAgainstBaseline(List<BattleSnapshot> baseline)
        {
            var scenarios = BuildScenarios();
            var report = new RegressionReport
            {
                TotalScenarios = baseline.Count,
                Matched = 0,
                Diverged = 0,
                Divergences = new List<RegressionDivergence>()
            };

            for (int i = 0; i < baseline.Count && i < scenarios.Count; i++)
            {
                var expected = baseline[i];
                var scenario = scenarios[i];
                var result = scenario.Run();
                int hash = ComputeEventLogHash(result.Events);

                bool outcomeMatch = result.Outcome == expected.ExpectedOutcome;
                bool roundsMatch = result.RoundsPlayed == expected.ExpectedRounds;
                bool hashMatch = hash == expected.EventLogHash;

                if (outcomeMatch && roundsMatch && hashMatch)
                {
                    report.Matched++;
                }
                else
                {
                    report.Diverged++;

                    if (!outcomeMatch)
                    {
                        report.Divergences.Add(new RegressionDivergence
                        {
                            Label = expected.Label,
                            Expected = $"Outcome={expected.ExpectedOutcome}",
                            Actual = $"Outcome={result.Outcome}"
                        });
                    }

                    if (!roundsMatch)
                    {
                        report.Divergences.Add(new RegressionDivergence
                        {
                            Label = expected.Label,
                            Expected = $"Rounds={expected.ExpectedRounds}",
                            Actual = $"Rounds={result.RoundsPlayed}"
                        });
                    }

                    if (!hashMatch)
                    {
                        report.Divergences.Add(new RegressionDivergence
                        {
                            Label = expected.Label,
                            Expected = $"EventHash={expected.EventLogHash}",
                            Actual = $"EventHash={hash}"
                        });
                    }
                }
            }

            return report;
        }

        /// <summary>
        /// Formats a regression report as a human-readable text summary.
        /// </summary>
        /// <param name="report">The report to format.</param>
        /// <returns>Multi-line text summary of the regression results.</returns>
        public static string FormatReport(RegressionReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== WarChess Regression Test Report ===");
            sb.AppendLine();
            sb.AppendLine($"Total Scenarios: {report.TotalScenarios}");
            sb.AppendLine($"Matched:         {report.Matched}");
            sb.AppendLine($"Diverged:        {report.Diverged}");
            sb.AppendLine();

            if (report.Diverged == 0)
            {
                sb.AppendLine("ALL SCENARIOS PASSED -- battle engine is deterministic.");
            }
            else
            {
                sb.AppendLine("DIVERGENCES DETECTED:");
                sb.AppendLine(new string('-', 60));

                foreach (var div in report.Divergences)
                {
                    sb.AppendLine($"  Scenario: {div.Label}");
                    sb.AppendLine($"    Expected: {div.Expected}");
                    sb.AppendLine($"    Actual:   {div.Actual}");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        // =====================================================================
        //  Event Log Hashing
        // =====================================================================

        /// <summary>
        /// Computes a deterministic hash over the full event log. Incorporates
        /// event type, round number, and all key fields for each event subclass.
        /// Uses a simple multiply-and-add scheme with a large prime.
        /// </summary>
        private static int ComputeEventLogHash(IReadOnlyList<BattleEvent> events)
        {
            unchecked
            {
                int hash = 17;

                for (int i = 0; i < events.Count; i++)
                {
                    var evt = events[i];
                    hash = hash * 31 + i;
                    hash = hash * 31 + evt.Round;

                    switch (evt)
                    {
                        case RoundStartedEvent rse:
                            hash = hash * 31 + 1;
                            hash = hash * 31 + rse.RoundNumber;
                            break;

                        case UnitMovedEvent ume:
                            hash = hash * 31 + 2;
                            hash = hash * 31 + ume.UnitId;
                            hash = hash * 31 + ume.From.X;
                            hash = hash * 31 + ume.From.Y;
                            hash = hash * 31 + ume.To.X;
                            hash = hash * 31 + ume.To.Y;
                            hash = hash * 31 + ume.TilesMoved;
                            break;

                        case UnitAttackedEvent uae:
                            hash = hash * 31 + 3;
                            hash = hash * 31 + uae.AttackerId;
                            hash = hash * 31 + uae.DefenderId;
                            hash = hash * 31 + uae.DamageDealt;
                            hash = hash * 31 + (int)uae.FlankDirection;
                            hash = hash * 31 + (uae.IsChargeAttack ? 1 : 0);
                            hash = hash * 31 + (uae.IsAoE ? 1 : 0);
                            break;

                        case UnitDiedEvent ude:
                            hash = hash * 31 + 4;
                            hash = hash * 31 + ude.UnitId;
                            hash = hash * 31 + ude.KillerId;
                            break;

                        case BattleEndedEvent bee:
                            hash = hash * 31 + 5;
                            hash = hash * 31 + (int)bee.Outcome;
                            hash = hash * 31 + bee.RoundsPlayed;
                            break;

                        default:
                            hash = hash * 31 + 0;
                            break;
                    }
                }

                return hash;
            }
        }

        // =====================================================================
        //  Scenario Infrastructure
        // =====================================================================

        /// <summary>
        /// Internal representation of a test scenario. Encapsulates setup and
        /// execution so each scenario can be replayed identically.
        /// </summary>
        private class BattleScenario
        {
            public string Label;
            public int Seed;
            public Func<GameConfigData, BattleResult> Execute;

            public BattleResult Run()
            {
                return Execute(default);
            }
        }

        /// <summary>
        /// Builds the full list of regression scenarios. Each scenario resets
        /// UnitFactory IDs, creates a grid, places units, and runs the battle.
        /// </summary>
        private List<BattleScenario> BuildScenarios()
        {
            var scenarios = new List<BattleScenario>();

            // ------------------------------------------------------------------
            // Category 1: Mirror matches (same units on both sides)
            // ------------------------------------------------------------------

            scenarios.Add(MirrorMatch("Mirror: 3x LineInfantry", 1001,
                new[] { "LineInfantry", "LineInfantry", "LineInfantry" },
                new GridCoord[] { new GridCoord(4, 1), new GridCoord(5, 1), new GridCoord(6, 1) },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(5, 10), new GridCoord(6, 10) }));

            scenarios.Add(MirrorMatch("Mirror: 2x Cavalry", 1002,
                new[] { "Cavalry", "Cavalry" },
                new GridCoord[] { new GridCoord(3, 2), new GridCoord(7, 2) },
                new GridCoord[] { new GridCoord(3, 9), new GridCoord(7, 9) }));

            scenarios.Add(MirrorMatch("Mirror: 2x Artillery", 1003,
                new[] { "Artillery", "Artillery" },
                new GridCoord[] { new GridCoord(4, 1), new GridCoord(6, 1) },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(6, 10) }));

            scenarios.Add(MirrorMatch("Mirror: 3x Grenadier", 1004,
                new[] { "Grenadier", "Grenadier", "Grenadier" },
                new GridCoord[] { new GridCoord(4, 1), new GridCoord(5, 1), new GridCoord(6, 1) },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(5, 10), new GridCoord(6, 10) }));

            // ------------------------------------------------------------------
            // Category 2: Asymmetric matchups
            // ------------------------------------------------------------------

            scenarios.Add(AsymmetricMatch("Cavalry Rush vs Artillery Fort", 2001,
                new[] { "Cavalry", "Cavalry", "Hussar", "Hussar" },
                new GridCoord[] { new GridCoord(2, 1), new GridCoord(4, 1), new GridCoord(6, 2), new GridCoord(8, 2) },
                new[] { "Artillery", "Artillery", "LineInfantry", "LineInfantry" },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(6, 10), new GridCoord(4, 9), new GridCoord(6, 9) }));

            scenarios.Add(AsymmetricMatch("Riflemen Skirmish vs Grenadier Line", 2002,
                new[] { "Rifleman", "Rifleman", "Rifleman", "Rifleman" },
                new GridCoord[] { new GridCoord(3, 1), new GridCoord(5, 1), new GridCoord(7, 1), new GridCoord(9, 1) },
                new[] { "Grenadier", "Grenadier", "Grenadier" },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(5, 10), new GridCoord(6, 10) }));

            scenarios.Add(AsymmetricMatch("Old Guard vs Militia Swarm", 2003,
                new[] { "OldGuard", "OldGuard" },
                new GridCoord[] { new GridCoord(5, 1), new GridCoord(6, 1) },
                new[] { "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia" },
                new GridCoord[]
                {
                    new GridCoord(1, 10), new GridCoord(2, 10), new GridCoord(3, 10), new GridCoord(4, 10), new GridCoord(5, 10),
                    new GridCoord(6, 10), new GridCoord(7, 10), new GridCoord(8, 10), new GridCoord(9, 10), new GridCoord(10, 10)
                }));

            scenarios.Add(AsymmetricMatch("Lancer Flank vs Cuirassier Front", 2004,
                new[] { "Lancer", "Lancer", "Lancer" },
                new GridCoord[] { new GridCoord(1, 1), new GridCoord(5, 1), new GridCoord(10, 1) },
                new[] { "Cuirassier", "Cuirassier" },
                new GridCoord[] { new GridCoord(5, 10), new GridCoord(6, 10) }));

            // ------------------------------------------------------------------
            // Category 3: Large armies (budget 40 and 60)
            // ------------------------------------------------------------------

            // Budget ~40: 5 LineInfantry(15) + 2 Cavalry(10) + 1 Artillery(6) + 1 Grenadier(7) = 38
            scenarios.Add(AsymmetricMatch("Large Army Budget 40 - Mixed vs Mixed", 3001,
                new[] { "LineInfantry", "LineInfantry", "LineInfantry", "LineInfantry", "LineInfantry", "Cavalry", "Cavalry", "Artillery", "Grenadier" },
                new GridCoord[]
                {
                    new GridCoord(1, 1), new GridCoord(2, 1), new GridCoord(3, 1), new GridCoord(4, 1), new GridCoord(5, 1),
                    new GridCoord(7, 2), new GridCoord(8, 2), new GridCoord(5, 3), new GridCoord(6, 1)
                },
                new[] { "LineInfantry", "LineInfantry", "LineInfantry", "LineInfantry", "LineInfantry", "Cavalry", "Cavalry", "Artillery", "Grenadier" },
                new GridCoord[]
                {
                    new GridCoord(1, 10), new GridCoord(2, 10), new GridCoord(3, 10), new GridCoord(4, 10), new GridCoord(5, 10),
                    new GridCoord(7, 9), new GridCoord(8, 9), new GridCoord(5, 8), new GridCoord(6, 10)
                }));

            // Budget ~60: 4 LineInfantry(12) + 2 Grenadier(14) + 2 Cavalry(10) + 2 Artillery(12) + 1 OldGuard(10) = 58
            scenarios.Add(AsymmetricMatch("Large Army Budget 60 - Full Roster", 3002,
                new[] { "LineInfantry", "LineInfantry", "LineInfantry", "LineInfantry", "Grenadier", "Grenadier", "Cavalry", "Cavalry", "Artillery", "Artillery", "OldGuard" },
                new GridCoord[]
                {
                    new GridCoord(1, 1), new GridCoord(2, 1), new GridCoord(3, 1), new GridCoord(4, 1),
                    new GridCoord(5, 1), new GridCoord(6, 1), new GridCoord(1, 2), new GridCoord(2, 2),
                    new GridCoord(4, 3), new GridCoord(6, 3), new GridCoord(7, 1)
                },
                new[] { "LineInfantry", "LineInfantry", "LineInfantry", "LineInfantry", "Grenadier", "Grenadier", "Cavalry", "Cavalry", "Artillery", "Artillery", "OldGuard" },
                new GridCoord[]
                {
                    new GridCoord(1, 10), new GridCoord(2, 10), new GridCoord(3, 10), new GridCoord(4, 10),
                    new GridCoord(5, 10), new GridCoord(6, 10), new GridCoord(1, 9), new GridCoord(2, 9),
                    new GridCoord(4, 8), new GridCoord(6, 8), new GridCoord(7, 10)
                }));

            // ------------------------------------------------------------------
            // Category 4: Mixed armies with various compositions
            // ------------------------------------------------------------------

            scenarios.Add(AsymmetricMatch("Dragoon Hunters vs Artillery Battery", 4001,
                new[] { "Dragoon", "Dragoon", "Hussar" },
                new GridCoord[] { new GridCoord(3, 1), new GridCoord(5, 2), new GridCoord(7, 1) },
                new[] { "Artillery", "HorseArtillery", "RocketBattery", "Sapper" },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(6, 10), new GridCoord(5, 9), new GridCoord(5, 10) }));

            scenarios.Add(AsymmetricMatch("Sapper Entrench vs Cavalry Charge", 4002,
                new[] { "Sapper", "Sapper", "Sapper", "LineInfantry", "LineInfantry" },
                new GridCoord[] { new GridCoord(3, 1), new GridCoord(5, 1), new GridCoord(7, 1), new GridCoord(4, 2), new GridCoord(6, 2) },
                new[] { "Cavalry", "Cavalry", "Lancer", "Lancer" },
                new GridCoord[] { new GridCoord(3, 10), new GridCoord(5, 10), new GridCoord(7, 10), new GridCoord(9, 10) }));

            scenarios.Add(AsymmetricMatch("Horse Artillery Mobile vs Static Defense", 4003,
                new[] { "HorseArtillery", "HorseArtillery", "Hussar", "Hussar" },
                new GridCoord[] { new GridCoord(2, 1), new GridCoord(8, 1), new GridCoord(4, 2), new GridCoord(6, 2) },
                new[] { "Grenadier", "Grenadier", "LineInfantry", "LineInfantry", "LineInfantry" },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(6, 10), new GridCoord(3, 9), new GridCoord(5, 9), new GridCoord(7, 9) }));

            // ------------------------------------------------------------------
            // Category 5: Battles with terrain (BattleEngineV2)
            // ------------------------------------------------------------------

            scenarios.Add(TerrainScenario("Terrain: Forest Defense", 5001,
                new[] { "LineInfantry", "LineInfantry", "LineInfantry" },
                new GridCoord[] { new GridCoord(4, 1), new GridCoord(5, 1), new GridCoord(6, 1) },
                new[] { "LineInfantry", "LineInfantry", "LineInfantry" },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(5, 10), new GridCoord(6, 10) },
                new Dictionary<GridCoord, TerrainType>
                {
                    { new GridCoord(4, 5), TerrainType.Forest },
                    { new GridCoord(5, 5), TerrainType.Forest },
                    { new GridCoord(6, 5), TerrainType.Forest },
                    { new GridCoord(4, 6), TerrainType.Forest },
                    { new GridCoord(5, 6), TerrainType.Forest },
                    { new GridCoord(6, 6), TerrainType.Forest }
                }));

            scenarios.Add(TerrainScenario("Terrain: Hill Artillery Advantage", 5002,
                new[] { "Artillery", "Artillery", "LineInfantry", "LineInfantry" },
                new GridCoord[] { new GridCoord(4, 3), new GridCoord(6, 3), new GridCoord(4, 1), new GridCoord(6, 1) },
                new[] { "Cavalry", "Cavalry", "Cavalry" },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(6, 10), new GridCoord(8, 10) },
                new Dictionary<GridCoord, TerrainType>
                {
                    { new GridCoord(4, 3), TerrainType.Hill },
                    { new GridCoord(5, 3), TerrainType.Hill },
                    { new GridCoord(6, 3), TerrainType.Hill }
                }));

            scenarios.Add(TerrainScenario("Terrain: River Crossing Bottleneck", 5003,
                new[] { "LineInfantry", "LineInfantry", "Cavalry" },
                new GridCoord[] { new GridCoord(5, 1), new GridCoord(6, 1), new GridCoord(3, 2) },
                new[] { "LineInfantry", "LineInfantry", "Cavalry" },
                new GridCoord[] { new GridCoord(5, 10), new GridCoord(6, 10), new GridCoord(3, 9) },
                new Dictionary<GridCoord, TerrainType>
                {
                    { new GridCoord(1, 5), TerrainType.River },
                    { new GridCoord(2, 5), TerrainType.River },
                    { new GridCoord(3, 5), TerrainType.River },
                    { new GridCoord(4, 5), TerrainType.River },
                    { new GridCoord(5, 5), TerrainType.Bridge },
                    { new GridCoord(6, 5), TerrainType.River },
                    { new GridCoord(7, 5), TerrainType.River },
                    { new GridCoord(8, 5), TerrainType.River },
                    { new GridCoord(9, 5), TerrainType.River },
                    { new GridCoord(10, 5), TerrainType.River }
                }));

            // ------------------------------------------------------------------
            // Category 6: Battles with commanders (BattleEngineV2)
            // ------------------------------------------------------------------

            scenarios.Add(CommanderScenario("Commander: Wellington Hold the Line", 6001,
                new[] { "LineInfantry", "LineInfantry", "LineInfantry", "Grenadier" },
                new GridCoord[] { new GridCoord(4, 1), new GridCoord(5, 1), new GridCoord(6, 1), new GridCoord(5, 2) },
                new[] { "Cavalry", "Cavalry", "Cavalry" },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(6, 10), new GridCoord(8, 10) },
                CommanderId.Wellington, 2,
                CommanderId.None, 1));

            scenarios.Add(CommanderScenario("Commander: Napoleon vs Kutuzov", 6002,
                new[] { "LineInfantry", "LineInfantry", "Cavalry", "Artillery" },
                new GridCoord[] { new GridCoord(4, 1), new GridCoord(6, 1), new GridCoord(2, 2), new GridCoord(5, 3) },
                new[] { "LineInfantry", "LineInfantry", "Grenadier", "Artillery" },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(6, 10), new GridCoord(5, 9), new GridCoord(5, 8) },
                CommanderId.Napoleon, 3,
                CommanderId.Kutuzov, 8));

            scenarios.Add(CommanderScenario("Commander: Blucher Cavalry Rush", 6003,
                new[] { "Cavalry", "Cavalry", "Hussar", "Lancer" },
                new GridCoord[] { new GridCoord(2, 1), new GridCoord(4, 1), new GridCoord(6, 2), new GridCoord(8, 2) },
                new[] { "LineInfantry", "LineInfantry", "LineInfantry", "LineInfantry", "LineInfantry" },
                new GridCoord[] { new GridCoord(3, 10), new GridCoord(4, 10), new GridCoord(5, 10), new GridCoord(6, 10), new GridCoord(7, 10) },
                CommanderId.Blucher, 1,
                CommanderId.None, 1));

            // ------------------------------------------------------------------
            // Category 7: Edge cases
            // ------------------------------------------------------------------

            // Single unit vs single unit
            scenarios.Add(AsymmetricMatch("Edge: 1v1 Militia", 7001,
                new[] { "Militia" },
                new GridCoord[] { new GridCoord(5, 1) },
                new[] { "Militia" },
                new GridCoord[] { new GridCoord(5, 10) }));

            // Single powerful unit vs single weak unit
            scenarios.Add(AsymmetricMatch("Edge: OldGuard vs Militia 1v1", 7002,
                new[] { "OldGuard" },
                new GridCoord[] { new GridCoord(5, 1) },
                new[] { "Militia" },
                new GridCoord[] { new GridCoord(5, 10) }));

            // Max budget stress: fill rows with Militia (cheapest, cost 1)
            scenarios.Add(AsymmetricMatch("Edge: Max Units - 10 Militia vs 10 Militia", 7003,
                new[] { "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia" },
                new GridCoord[]
                {
                    new GridCoord(1, 1), new GridCoord(2, 1), new GridCoord(3, 1), new GridCoord(4, 1), new GridCoord(5, 1),
                    new GridCoord(6, 1), new GridCoord(7, 1), new GridCoord(8, 1), new GridCoord(9, 1), new GridCoord(10, 1)
                },
                new[] { "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia", "Militia" },
                new GridCoord[]
                {
                    new GridCoord(1, 10), new GridCoord(2, 10), new GridCoord(3, 10), new GridCoord(4, 10), new GridCoord(5, 10),
                    new GridCoord(6, 10), new GridCoord(7, 10), new GridCoord(8, 10), new GridCoord(9, 10), new GridCoord(10, 10)
                }));

            // Ranged-only army: Rocket Batteries
            scenarios.Add(AsymmetricMatch("Edge: Rocket Battery Duel", 7004,
                new[] { "RocketBattery", "RocketBattery" },
                new GridCoord[] { new GridCoord(4, 1), new GridCoord(6, 1) },
                new[] { "RocketBattery", "RocketBattery" },
                new GridCoord[] { new GridCoord(4, 10), new GridCoord(6, 10) }));

            return scenarios;
        }

        // =====================================================================
        //  Scenario Builder Helpers
        // =====================================================================

        /// <summary>
        /// Creates a mirror match scenario where both sides use the same unit types.
        /// Player units are placed at playerPositions, enemy units at enemyPositions.
        /// </summary>
        private BattleScenario MirrorMatch(string label, int seed,
            string[] unitTypes, GridCoord[] playerPositions, GridCoord[] enemyPositions)
        {
            return AsymmetricMatch(label, seed, unitTypes, playerPositions, unitTypes, enemyPositions);
        }

        /// <summary>
        /// Creates an asymmetric scenario with different unit types on each side.
        /// Uses the v1 BattleEngine (no terrain, no commanders).
        /// </summary>
        private BattleScenario AsymmetricMatch(string label, int seed,
            string[] playerTypes, GridCoord[] playerPositions,
            string[] enemyTypes, GridCoord[] enemyPositions)
        {
            var config = _config;

            return new BattleScenario
            {
                Label = label,
                Seed = seed,
                Execute = _ =>
                {
                    UnitFactory.ResetIds();
                    var grid = new GridMap(config.GridWidth, config.GridHeight);

                    var playerUnits = new List<UnitInstance>();
                    for (int i = 0; i < playerTypes.Length; i++)
                    {
                        var unit = UnitFactory.CreateByTypeName(playerTypes[i], Owner.Player, playerPositions[i]);
                        grid.PlaceUnit(unit, playerPositions[i]);
                        playerUnits.Add(unit);
                    }

                    var enemyUnits = new List<UnitInstance>();
                    for (int i = 0; i < enemyTypes.Length; i++)
                    {
                        var unit = UnitFactory.CreateByTypeName(enemyTypes[i], Owner.Enemy, enemyPositions[i]);
                        grid.PlaceUnit(unit, enemyPositions[i]);
                        enemyUnits.Add(unit);
                    }

                    var engine = new BattleEngine(grid, playerUnits, enemyUnits, config, seed);
                    return engine.RunFullBattle();
                }
            };
        }

        /// <summary>
        /// Creates a scenario with terrain features using BattleEngineV2.
        /// </summary>
        private BattleScenario TerrainScenario(string label, int seed,
            string[] playerTypes, GridCoord[] playerPositions,
            string[] enemyTypes, GridCoord[] enemyPositions,
            Dictionary<GridCoord, TerrainType> terrainPlacements)
        {
            var config = _config;

            return new BattleScenario
            {
                Label = label,
                Seed = seed,
                Execute = _ =>
                {
                    UnitFactory.ResetIds();
                    var grid = new GridMap(config.GridWidth, config.GridHeight);
                    var terrainMap = new TerrainMap(config.GridWidth, config.GridHeight);

                    foreach (var kvp in terrainPlacements)
                    {
                        terrainMap.SetTerrain(kvp.Key, kvp.Value);
                    }

                    var playerUnits = new List<UnitInstance>();
                    for (int i = 0; i < playerTypes.Length; i++)
                    {
                        var unit = UnitFactory.CreateByTypeName(playerTypes[i], Owner.Player, playerPositions[i]);
                        grid.PlaceUnit(unit, playerPositions[i]);
                        playerUnits.Add(unit);
                    }

                    var enemyUnits = new List<UnitInstance>();
                    for (int i = 0; i < enemyTypes.Length; i++)
                    {
                        var unit = UnitFactory.CreateByTypeName(enemyTypes[i], Owner.Enemy, enemyPositions[i]);
                        grid.PlaceUnit(unit, enemyPositions[i]);
                        enemyUnits.Add(unit);
                    }

                    var engine = new BattleEngineV2(grid, terrainMap, playerUnits, enemyUnits, config, seed);
                    return engine.RunFullBattle();
                }
            };
        }

        /// <summary>
        /// Creates a scenario with commanders on one or both sides using BattleEngineV2.
        /// </summary>
        private BattleScenario CommanderScenario(string label, int seed,
            string[] playerTypes, GridCoord[] playerPositions,
            string[] enemyTypes, GridCoord[] enemyPositions,
            CommanderId playerCommander, int playerCmdRound,
            CommanderId enemyCommander, int enemyCmdRound)
        {
            var config = _config;

            return new BattleScenario
            {
                Label = label,
                Seed = seed,
                Execute = _ =>
                {
                    UnitFactory.ResetIds();
                    var grid = new GridMap(config.GridWidth, config.GridHeight);
                    var terrainMap = new TerrainMap(config.GridWidth, config.GridHeight);

                    var playerUnits = new List<UnitInstance>();
                    for (int i = 0; i < playerTypes.Length; i++)
                    {
                        var unit = UnitFactory.CreateByTypeName(playerTypes[i], Owner.Player, playerPositions[i]);
                        grid.PlaceUnit(unit, playerPositions[i]);
                        playerUnits.Add(unit);
                    }

                    var enemyUnits = new List<UnitInstance>();
                    for (int i = 0; i < enemyTypes.Length; i++)
                    {
                        var unit = UnitFactory.CreateByTypeName(enemyTypes[i], Owner.Enemy, enemyPositions[i]);
                        grid.PlaceUnit(unit, enemyPositions[i]);
                        enemyUnits.Add(unit);
                    }

                    var engine = new BattleEngineV2(
                        grid, terrainMap, playerUnits, enemyUnits, config, seed,
                        playerCommander, playerCmdRound,
                        enemyCommander, enemyCmdRound);
                    return engine.RunFullBattle();
                }
            };
        }
    }
}
