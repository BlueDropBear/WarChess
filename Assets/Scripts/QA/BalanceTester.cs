using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarChess.Battle;
using WarChess.Config;
using WarChess.Core;
using WarChess.Officers;
using WarChess.Units;

namespace WarChess.QA
{
    /// <summary>
    /// AI QA Balance Tester. Runs headless battles to identify balance issues.
    /// GDD Section 12: reuses the same BattleEngine as the game. No rendering.
    /// Implements Mode 1 (matchup testing) and Mode 2 (composition stress test).
    /// Pure C# — can run in Unity Editor or as standalone console app.
    /// </summary>
    public class BalanceTester
    {
        private readonly GameConfigData _config;
        private readonly Random _seedGen;

        /// <summary>All 14 unit type names for enumeration.</summary>
        public static readonly string[] AllUnitTypes =
        {
            "LineInfantry", "Militia", "Cavalry", "Artillery", "Grenadier",
            "Rifleman", "Hussar", "Cuirassier", "HorseArtillery", "Sapper",
            "OldGuard", "RocketBattery", "Lancer", "Dragoon"
        };

        public BalanceTester(GameConfigData config, int masterSeed = 12345)
        {
            _config = config;
            _seedGen = new Random(masterSeed);
        }

        // ======= MODE 1: Unit Matchup Testing =======

        /// <summary>
        /// Runs every unit type vs every unit type at equal cost.
        /// Returns a 14x14 matrix of win rates (0-100).
        /// GDD Mode 1: flags any matchup where one side wins >70%.
        /// </summary>
        public MatchupResult RunMatchupTest(int battlesPerMatchup = 100)
        {
            int count = AllUnitTypes.Length;
            var matrix = new int[count, count]; // Win count for row vs column
            var anomalies = new List<string>();

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    if (i == j)
                    {
                        matrix[i, j] = 50; // Mirror match
                        continue;
                    }

                    int wins = RunMatchup(AllUnitTypes[i], AllUnitTypes[j], battlesPerMatchup);
                    int winRate = (wins * 100) / battlesPerMatchup;
                    matrix[i, j] = winRate;

                    if (winRate > 70)
                    {
                        anomalies.Add($"{AllUnitTypes[i]} beats {AllUnitTypes[j]} {winRate}% of the time");
                    }
                }
            }

            return new MatchupResult { Matrix = matrix, Anomalies = anomalies };
        }

        private int RunMatchup(string typeA, string typeB, int battles)
        {
            int winsA = 0;

            for (int b = 0; b < battles; b++)
            {
                int seed = _seedGen.Next();
                UnitFactory.ResetIds();

                var grid = new GridMap();
                var unitA = UnitFactory.CreateByTypeName(typeA, Owner.Player, new GridCoord(5, 2));
                var unitB = UnitFactory.CreateByTypeName(typeB, Owner.Enemy, new GridCoord(5, 9));

                if (unitA == null || unitB == null) continue;

                grid.PlaceUnit(unitA, unitA.Position);
                grid.PlaceUnit(unitB, unitB.Position);

                var engine = new BattleEngine(
                    grid,
                    new List<UnitInstance> { unitA },
                    new List<UnitInstance> { unitB },
                    _config, seed);

                var result = engine.RunFullBattle();
                if (result.Outcome == BattleOutcome.PlayerWin) winsA++;
            }

            return winsA;
        }

        // ======= MODE 2: Composition Stress Test =======

        /// <summary>
        /// Generates random armies and battles them to find dominant compositions.
        /// GDD Mode 2: flags compositions with >60% win rate across all opponents.
        /// </summary>
        public CompositionResult RunCompositionTest(
            int budget, int armyCount = 200, int battlesPerArmy = 50,
            string[] availableUnits = null)
        {
            if (availableUnits == null) availableUnits = AllUnitTypes;

            // Generate random armies
            var armies = new List<List<string>>();
            for (int i = 0; i < armyCount; i++)
            {
                armies.Add(GenerateRandomArmy(budget, availableUnits));
            }

            // Battle each army against a sample of others
            var winCounts = new int[armyCount];
            var totalGames = new int[armyCount];
            var unitAppearances = new Dictionary<string, int>();
            var unitWinAppearances = new Dictionary<string, int>();

            for (int i = 0; i < armyCount; i++)
            {
                for (int j = 0; j < battlesPerArmy; j++)
                {
                    int opponent = _seedGen.Next(armyCount);
                    if (opponent == i) opponent = (opponent + 1) % armyCount;

                    int seed = _seedGen.Next();
                    var outcome = BattleArmies(armies[i], armies[opponent], seed);

                    totalGames[i]++;
                    totalGames[opponent]++;

                    if (outcome == BattleOutcome.PlayerWin)
                    {
                        winCounts[i]++;
                        TrackUnits(armies[i], unitWinAppearances);
                    }
                    else if (outcome == BattleOutcome.EnemyWin)
                    {
                        winCounts[opponent]++;
                        TrackUnits(armies[opponent], unitWinAppearances);
                    }

                    TrackUnits(armies[i], unitAppearances);
                    TrackUnits(armies[opponent], unitAppearances);
                }
            }

            // Analyze results
            var result = new CompositionResult();

            // Find dominant and weak compositions
            for (int i = 0; i < armyCount; i++)
            {
                if (totalGames[i] == 0) continue;
                int winRate = (winCounts[i] * 100) / totalGames[i];
                var comp = string.Join(", ", armies[i]);

                if (winRate > 60)
                    result.DominantCompositions.Add($"{comp} ({winRate}% win rate)");
                if (winRate < 20)
                    result.WeakCompositions.Add($"{comp} ({winRate}% win rate)");
            }

            // Find over/underpowered units
            foreach (var unitType in availableUnits)
            {
                int totalApp = unitAppearances.TryGetValue(unitType, out int ta) ? ta : 0;
                int winApp = unitWinAppearances.TryGetValue(unitType, out int wa) ? wa : 0;

                if (totalApp > 0)
                {
                    int presence = (winApp * 100) / totalApp;
                    if (presence > 80)
                        result.OverpoweredUnits.Add($"{unitType} appears in {presence}% of winning armies");
                    if (presence < 20)
                        result.UnderpoweredUnits.Add($"{unitType} appears in only {presence}% of winning armies");
                }
            }

            return result;
        }

        private List<string> GenerateRandomArmy(int budget, string[] available)
        {
            var army = new List<string>();
            int remaining = budget;

            // Unit costs lookup
            var costs = GetUnitCosts();

            // Keep adding random units until budget exhausted
            int maxAttempts = 100;
            while (remaining > 0 && maxAttempts-- > 0)
            {
                string unit = available[_seedGen.Next(available.Length)];
                int cost = costs.TryGetValue(unit, out int c) ? c : 99;

                if (cost <= remaining && army.Count < 15) // Max 15 units
                {
                    army.Add(unit);
                    remaining -= cost;
                }
            }

            return army;
        }

        private BattleOutcome BattleArmies(List<string> armyA, List<string> armyB, int seed)
        {
            UnitFactory.ResetIds();
            var grid = new GridMap();
            var playerUnits = new List<UnitInstance>();
            var enemyUnits = new List<UnitInstance>();

            // Place army A in rows 1-3
            PlaceArmy(armyA, Owner.Player, 1, 3, grid, playerUnits);

            // Place army B in rows 8-10
            PlaceArmy(armyB, Owner.Enemy, 8, 10, grid, enemyUnits);

            if (playerUnits.Count == 0 || enemyUnits.Count == 0)
                return BattleOutcome.Draw;

            var engine = new BattleEngine(grid, playerUnits, enemyUnits, _config, seed);
            return engine.RunFullBattle().Outcome;
        }

        private void PlaceArmy(List<string> unitTypes, Owner owner, int minRow, int maxRow,
            GridMap grid, List<UnitInstance> units)
        {
            int placed = 0;
            for (int row = minRow; row <= maxRow && placed < unitTypes.Count; row++)
            {
                for (int col = 1; col <= 10 && placed < unitTypes.Count; col++)
                {
                    var coord = new GridCoord(col, row);
                    if (grid.IsTileEmpty(coord))
                    {
                        var unit = UnitFactory.CreateByTypeName(unitTypes[placed], owner, coord);
                        if (unit != null)
                        {
                            grid.PlaceUnit(unit, coord);
                            units.Add(unit);
                            placed++;
                        }
                    }
                }
            }
        }

        private void TrackUnits(List<string> army, Dictionary<string, int> tracker)
        {
            var seen = new HashSet<string>();
            foreach (var unit in army)
            {
                if (seen.Add(unit)) // Count each type once per army
                {
                    if (!tracker.ContainsKey(unit)) tracker[unit] = 0;
                    tracker[unit]++;
                }
            }
        }

        private Dictionary<string, int> GetUnitCosts()
        {
            return GameConfigData.GetUnitCosts();
        }

        // ======= MODE 3: Officer Impact Analysis =======

        /// <summary>
        /// Tests each officer's impact on battle outcomes at each level.
        /// GDD Section 12: flags officers causing >15% win rate delta.
        /// Generates random armies, runs battles with and without officer mods applied
        /// to the first unit, and compares win rates.
        /// </summary>
        public OfficerImpactResult RunOfficerImpactTest(
            int budget = 40, int battlesPerOfficer = 200, int armySamples = 10)
        {
            var result = new OfficerImpactResult();
            var officerIds = (OfficerId[])Enum.GetValues(typeof(OfficerId));

            foreach (var officerId in officerIds)
            {
                var officerData = OfficerDatabase.Get(officerId);
                if (officerData == null) continue;

                for (int level = 1; level <= 5; level++)
                {
                    int winsWithout = 0;
                    int winsWith = 0;
                    int totalBattles = 0;

                    for (int s = 0; s < armySamples; s++)
                    {
                        var armyTypes = GenerateRandomArmy(budget, AllUnitTypes);
                        if (armyTypes.Count == 0) continue;

                        int battlesThisSample = battlesPerOfficer / armySamples;

                        for (int b = 0; b < battlesThisSample; b++)
                        {
                            int seed = _seedGen.Next();
                            var enemyTypes = GenerateRandomArmy(budget, AllUnitTypes);
                            if (enemyTypes.Count == 0) continue;

                            totalBattles++;

                            // Battle WITHOUT officer
                            var outcomeWithout = BattleArmies(armyTypes, enemyTypes, seed);
                            if (outcomeWithout == BattleOutcome.PlayerWin) winsWithout++;

                            // Battle WITH officer applied to first unit
                            var outcomeWith = BattleArmiesWithOfficer(
                                armyTypes, enemyTypes, seed, officerData, level);
                            if (outcomeWith == BattleOutcome.PlayerWin) winsWith++;
                        }
                    }

                    if (totalBattles == 0) continue;

                    int rateWithout = (winsWithout * 100) / totalBattles;
                    int rateWith = (winsWith * 100) / totalBattles;
                    int delta = Math.Abs(rateWith - rateWithout);

                    string key = $"{officerId}_L{level}";
                    var entry = new OfficerImpactEntry
                    {
                        OfficerId = officerId,
                        Level = level,
                        WinRateWithOfficer = rateWith,
                        WinRateWithoutOfficer = rateWithout,
                        Delta = delta
                    };
                    result.Entries[key] = entry;

                    if (delta > 15)
                    {
                        string direction = rateWith > rateWithout ? "boost" : "penalty";
                        result.Anomalies.Add(
                            $"{officerData.Name} L{level}: {delta}% {direction} " +
                            $"(without={rateWithout}%, with={rateWith}%)");
                    }
                }
            }

            return result;
        }

        private BattleOutcome BattleArmiesWithOfficer(
            List<string> armyA, List<string> armyB, int seed,
            OfficerData officer, int level)
        {
            UnitFactory.ResetIds();
            var grid = new GridMap();
            var playerUnits = new List<UnitInstance>();
            var enemyUnits = new List<UnitInstance>();

            PlaceArmy(armyA, Owner.Player, 1, 3, grid, playerUnits);
            PlaceArmy(armyB, Owner.Enemy, 8, 10, grid, enemyUnits);

            if (playerUnits.Count == 0 || enemyUnits.Count == 0)
                return BattleOutcome.Draw;

            // Apply officer mods to first player unit
            ApplyOfficerMods(playerUnits[0], officer, level);

            var engine = new BattleEngine(grid, playerUnits, enemyUnits, _config, seed);
            return engine.RunFullBattle().Outcome;
        }

        /// <summary>
        /// Applies an officer's stat mods to a unit at the given level.
        /// Handles both multiplier (base-100) and flat value mod types.
        /// </summary>
        private void ApplyOfficerMods(UnitInstance unit, OfficerData officer, int level)
        {
            ApplySingleMod(unit, officer.PositiveMod, level);
            ApplySingleMod(unit, officer.NegativeMod, level);
            if (officer.NegativeMod2 != null)
                ApplySingleMod(unit, officer.NegativeMod2, level);
        }

        private void ApplySingleMod(UnitInstance unit, OfficerStatMod mod, int level)
        {
            if (mod == null) return;
            int val = mod.GetValueAtLevel(level);

            switch (mod.Type)
            {
                case OfficerModType.AtkMultiplier:
                    unit.ApplyStatScale((unit.Atk * val) / 100, unit.Def, unit.MaxHp);
                    break;
                case OfficerModType.DefMultiplier:
                    unit.ApplyStatScale(unit.Atk, (unit.Def * val) / 100, unit.MaxHp);
                    break;
                case OfficerModType.HpMultiplier:
                    unit.ApplyStatScale(unit.Atk, unit.Def, (unit.MaxHp * val) / 100);
                    break;
                case OfficerModType.MovFlat:
                    unit.ApplyFlatMods(val, 0, 0);
                    break;
                case OfficerModType.RngFlat:
                    unit.ApplyFlatMods(0, val, 0);
                    break;
                case OfficerModType.SpdFlat:
                    unit.ApplyFlatMods(0, 0, val);
                    break;
                // Non-stat mods (ChargeDamageMultiplier, FlankDamageReceived, etc.)
                // affect battle calculations, not base stats. Skipped for stat-level testing.
                default:
                    break;
            }
        }

        // ======= Report Generation =======

        /// <summary>
        /// Generates a human-readable report from matchup results.
        /// </summary>
        public static string FormatMatchupReport(MatchupResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== UNIT MATCHUP MATRIX (win % for row vs column) ===");
            sb.AppendLine();

            // Header
            sb.Append("             ");
            for (int j = 0; j < AllUnitTypes.Length; j++)
                sb.Append($"{AllUnitTypes[j],5} ".Substring(0, 5) + " ");
            sb.AppendLine();

            // Rows
            for (int i = 0; i < AllUnitTypes.Length; i++)
            {
                sb.Append($"{AllUnitTypes[i],-13}");
                for (int j = 0; j < AllUnitTypes.Length; j++)
                    sb.Append($"{result.Matrix[i, j],5}% ");
                sb.AppendLine();
            }

            if (result.Anomalies.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== ANOMALIES (>70% win rate) ===");
                foreach (var a in result.Anomalies)
                    sb.AppendLine($"  ! {a}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a human-readable report from officer impact results.
        /// </summary>
        public static string FormatOfficerReport(OfficerImpactResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== OFFICER IMPACT ANALYSIS (win % without vs with officer) ===");
            sb.AppendLine();
            sb.AppendLine($"{"Officer",-22} {"Lvl",3} {"Without",8} {"With",6} {"Delta",6} {"Flag",4}");
            sb.AppendLine(new string('-', 54));

            foreach (var kvp in result.Entries.OrderBy(e => e.Value.OfficerId).ThenBy(e => e.Value.Level))
            {
                var e = kvp.Value;
                var officerData = OfficerDatabase.Get(e.OfficerId);
                string name = officerData?.Name ?? e.OfficerId.ToString();
                string flag = e.Delta > 15 ? " !" : "";
                sb.AppendLine($"{name,-22} {e.Level,3} {e.WinRateWithoutOfficer,7}% {e.WinRateWithOfficer,5}% {e.Delta,5}%{flag}");
            }

            if (result.Anomalies.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== ANOMALIES (>15% win rate delta) ===");
                foreach (var a in result.Anomalies)
                    sb.AppendLine($"  ! {a}");
            }

            return sb.ToString();
        }
    }

    /// <summary>Result of Mode 1 matchup testing.</summary>
    public class MatchupResult
    {
        /// <summary>14x14 win rate matrix (0-100).</summary>
        public int[,] Matrix;

        /// <summary>Matchups flagged as unbalanced (>70% win rate).</summary>
        public List<string> Anomalies;
    }

    /// <summary>Result of Mode 2 composition testing.</summary>
    public class CompositionResult
    {
        public List<string> DominantCompositions = new List<string>();
        public List<string> WeakCompositions = new List<string>();
        public List<string> OverpoweredUnits = new List<string>();
        public List<string> UnderpoweredUnits = new List<string>();
    }

    /// <summary>Result of Mode 3 officer impact testing.</summary>
    public class OfficerImpactResult
    {
        /// <summary>Per-officer win rate WITH vs WITHOUT. Key = "OfficerId_L{level}".</summary>
        public Dictionary<string, OfficerImpactEntry> Entries = new Dictionary<string, OfficerImpactEntry>();

        /// <summary>Officers flagged for >15% win rate delta per GDD.</summary>
        public List<string> Anomalies = new List<string>();
    }

    /// <summary>Single officer's impact data at a specific level.</summary>
    public class OfficerImpactEntry
    {
        public OfficerId OfficerId;
        public int Level;
        public int WinRateWithOfficer;
        public int WinRateWithoutOfficer;
        public int Delta;
    }
}
