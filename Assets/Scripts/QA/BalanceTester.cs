using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarChess.Battle;
using WarChess.Commanders;
using WarChess.Config;
using WarChess.Core;
using WarChess.Formations;
using WarChess.Officers;
using WarChess.Terrain;
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

                var grid = new GridMap(_config.GridWidth, _config.GridHeight);
                int midCol = (_config.GridWidth + 1) / 2;
                var unitA = UnitFactory.CreateByTypeName(typeA, Owner.Player, new GridCoord(midCol, _config.PlayerDeployMinRow + 1));
                var unitB = UnitFactory.CreateByTypeName(typeB, Owner.Enemy, new GridCoord(midCol, _config.EnemyDeployMaxRow - 1));

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
            var grid = new GridMap(_config.GridWidth, _config.GridHeight);
            var playerUnits = new List<UnitInstance>();
            var enemyUnits = new List<UnitInstance>();

            PlaceArmy(armyA, Owner.Player, _config.PlayerDeployMinRow, _config.PlayerDeployMaxRow, grid, playerUnits);
            PlaceArmy(armyB, Owner.Enemy, _config.EnemyDeployMinRow, _config.EnemyDeployMaxRow, grid, enemyUnits);

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
                for (int col = 1; col <= _config.GridWidth && placed < unitTypes.Count; col++)
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
            var grid = new GridMap(_config.GridWidth, _config.GridHeight);
            var playerUnits = new List<UnitInstance>();
            var enemyUnits = new List<UnitInstance>();

            PlaceArmy(armyA, Owner.Player, _config.PlayerDeployMinRow, _config.PlayerDeployMaxRow, grid, playerUnits);
            PlaceArmy(armyB, Owner.Enemy, _config.EnemyDeployMinRow, _config.EnemyDeployMaxRow, grid, enemyUnits);

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

        // ======= MODE 4: Commander Impact Analysis =======

        /// <summary>
        /// Tests each commander's impact on battle outcomes.
        /// Compares win rates with and without each commander active.
        /// Flags commanders causing >15% win rate delta.
        /// </summary>
        public CommanderImpactResult RunCommanderImpactTest(
            int budget = 40, int battlesPerCommander = 200, int armySamples = 10)
        {
            var result = new CommanderImpactResult();
            var commanderIds = (CommanderId[])Enum.GetValues(typeof(CommanderId));

            foreach (var cmdId in commanderIds)
            {
                if (cmdId == CommanderId.None) continue;
                var cmdData = CommanderDatabase.Get(cmdId);
                if (cmdData == null) continue;

                int winsWithout = 0;
                int winsWith = 0;
                int totalBattles = 0;

                for (int s = 0; s < armySamples; s++)
                {
                    var armyTypes = GenerateRandomArmy(budget, AllUnitTypes);
                    if (armyTypes.Count == 0) continue;

                    int battlesThisSample = battlesPerCommander / armySamples;

                    for (int b = 0; b < battlesThisSample; b++)
                    {
                        int seed = _seedGen.Next();
                        var enemyTypes = GenerateRandomArmy(budget, AllUnitTypes);
                        if (enemyTypes.Count == 0) continue;

                        totalBattles++;

                        // Battle WITHOUT commander
                        var outcomeWithout = BattleArmiesV2(armyTypes, enemyTypes, seed,
                            CommanderId.None, 1, CommanderId.None, 1, null);
                        if (outcomeWithout == BattleOutcome.PlayerWin) winsWithout++;

                        // Battle WITH commander for player
                        int activationRound = cmdData.TriggerType == CommanderTriggerType.Manual
                            ? Math.Max(1, cmdData.TriggerParam / 2) : 1;
                        var outcomeWith = BattleArmiesV2(armyTypes, enemyTypes, seed,
                            cmdId, activationRound, CommanderId.None, 1, null);
                        if (outcomeWith == BattleOutcome.PlayerWin) winsWith++;
                    }
                }

                if (totalBattles == 0) continue;

                int rateWithout = (winsWithout * 100) / totalBattles;
                int rateWith = (winsWith * 100) / totalBattles;
                int delta = Math.Abs(rateWith - rateWithout);

                var entry = new CommanderImpactEntry
                {
                    CommanderId = cmdId,
                    CommanderName = cmdData.Name,
                    AbilityName = cmdData.AbilityName,
                    WinRateWithCommander = rateWith,
                    WinRateWithoutCommander = rateWithout,
                    Delta = delta
                };
                result.Entries[cmdId] = entry;

                if (delta > 15)
                {
                    string direction = rateWith > rateWithout ? "boost" : "penalty";
                    result.Anomalies.Add(
                        $"{cmdData.Name} ({cmdData.AbilityName}): {delta}% {direction} " +
                        $"(without={rateWithout}%, with={rateWith}%)");
                }
            }

            return result;
        }

        // ======= MODE 5: Terrain Impact Analysis =======

        /// <summary>
        /// Tests how different terrain layouts affect battle outcomes.
        /// Compares win rates on open field vs various terrain configurations.
        /// Flags terrain types causing >20% win rate shift.
        /// </summary>
        public TerrainImpactResult RunTerrainImpactTest(
            int budget = 40, int battlesPerTerrain = 200, int armySamples = 10)
        {
            var result = new TerrainImpactResult();
            int baselineWins = 0;
            int baselineTotal = 0;

            // Collect armies upfront so all terrain types test the same matchups
            var armyPairs = new List<(List<string> player, List<string> enemy, int seed)>();
            for (int s = 0; s < armySamples; s++)
            {
                var army = GenerateRandomArmy(budget, AllUnitTypes);
                if (army.Count == 0) continue;
                int battlesThisSample = battlesPerTerrain / armySamples;
                for (int b = 0; b < battlesThisSample; b++)
                {
                    var enemy = GenerateRandomArmy(budget, AllUnitTypes);
                    if (enemy.Count == 0) continue;
                    armyPairs.Add((army, enemy, _seedGen.Next()));
                }
            }

            // Baseline: open field (no terrain)
            foreach (var (player, enemy, seed) in armyPairs)
            {
                baselineTotal++;
                var outcome = BattleArmiesV2(player, enemy, seed,
                    CommanderId.None, 1, CommanderId.None, 1, null);
                if (outcome == BattleOutcome.PlayerWin) baselineWins++;
            }

            int baselineRate = baselineTotal > 0 ? (baselineWins * 100) / baselineTotal : 50;
            result.BaselineWinRate = baselineRate;

            // Test each terrain type placed in the center of the map
            var terrainTypes = (TerrainType[])Enum.GetValues(typeof(TerrainType));
            foreach (var terrainType in terrainTypes)
            {
                if (terrainType == TerrainType.OpenField) continue;

                int wins = 0;
                int total = 0;

                foreach (var (player, enemy, seed) in armyPairs)
                {
                    total++;
                    var terrainMap = CreateCenterTerrainMap(terrainType);
                    var outcome = BattleArmiesV2(player, enemy, seed,
                        CommanderId.None, 1, CommanderId.None, 1, terrainMap);
                    if (outcome == BattleOutcome.PlayerWin) wins++;
                }

                int winRate = total > 0 ? (wins * 100) / total : 50;
                int delta = Math.Abs(winRate - baselineRate);

                var entry = new TerrainImpactEntry
                {
                    Terrain = terrainType,
                    WinRate = winRate,
                    BaselineWinRate = baselineRate,
                    Delta = delta
                };
                result.Entries[terrainType] = entry;

                if (delta > 20)
                {
                    result.Anomalies.Add(
                        $"{terrainType}: {delta}% shift from baseline " +
                        $"(baseline={baselineRate}%, with terrain={winRate}%)");
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a terrain map with the given terrain type placed in the center rows
        /// (no-man's land between deployment zones) to test terrain's combat impact.
        /// </summary>
        private TerrainMap CreateCenterTerrainMap(TerrainType type)
        {
            var map = new TerrainMap(_config.GridWidth, _config.GridHeight);
            int centerMinRow = _config.PlayerDeployMaxRow + 1;
            int centerMaxRow = _config.EnemyDeployMinRow - 1;
            if (centerMaxRow < centerMinRow) centerMaxRow = centerMinRow;

            for (int y = centerMinRow; y <= centerMaxRow; y++)
            {
                for (int x = 1; x <= _config.GridWidth; x++)
                {
                    map.SetTerrain(new GridCoord(x, y), type);
                }
            }
            return map;
        }

        // ======= MODE 6: Grid Size Scaling Test =======

        /// <summary>
        /// Tests how different grid sizes affect battle balance.
        /// Runs the same army matchups across multiple grid dimensions
        /// and reports how win rates, average battle length, and draw
        /// rates shift. Useful for tuning the configurable grid size.
        /// </summary>
        public GridSizeResult RunGridSizeTest(
            int budget = 40, int battlesPerSize = 200, int armySamples = 10,
            int[] gridSizes = null)
        {
            if (gridSizes == null) gridSizes = new[] { 6, 8, 10, 12, 14 };

            var result = new GridSizeResult();

            // Generate army pairs once
            var armyPairs = new List<(List<string> player, List<string> enemy, int seed)>();
            for (int s = 0; s < armySamples; s++)
            {
                var army = GenerateRandomArmy(budget, AllUnitTypes);
                if (army.Count == 0) continue;
                int battlesThisSample = battlesPerSize / armySamples;
                for (int b = 0; b < battlesThisSample; b++)
                {
                    var enemy = GenerateRandomArmy(budget, AllUnitTypes);
                    if (enemy.Count == 0) continue;
                    armyPairs.Add((army, enemy, _seedGen.Next()));
                }
            }

            foreach (int size in gridSizes)
            {
                // Create a config scaled for this grid size
                int deployRows = Math.Max(1, (size * 3 + 9) / 10); // Scale deploy depth proportionally
                var scaledConfig = new GameConfigData(
                    gridWidth: size, gridHeight: size,
                    playerDeployMinRow: 1, playerDeployMaxRow: deployRows,
                    enemyDeployMinRow: size - deployRows + 1, enemyDeployMaxRow: size,
                    maxRounds: _config.MaxRounds,
                    minimumDamage: _config.MinimumDamage,
                    defaultFlankSideMultiplier: _config.DefaultFlankSideMultiplier,
                    defaultFlankRearMultiplier: _config.DefaultFlankRearMultiplier,
                    forestDefenseMultiplier: _config.ForestDefenseMultiplier,
                    hillAttackMultiplier: _config.HillAttackMultiplier,
                    fortificationDefenseMultiplier: _config.FortificationDefenseMultiplier,
                    townDefenseMultiplier: _config.TownDefenseMultiplier,
                    battleLineDefBonus: _config.BattleLineDefBonus,
                    batteryAtkBonus: _config.BatteryAtkBonus,
                    cavalryWedgeChargeBonus: _config.CavalryWedgeChargeBonus,
                    squareDefVsCavalryBonus: _config.SquareDefVsCavalryBonus,
                    skirmishAtkBonus: _config.SkirmishAtkBonus,
                    skirmishRangeBonus: _config.SkirmishRangeBonus,
                    chargeMinTilesMoved: _config.ChargeMinTilesMoved,
                    chargeMultiplier: _config.ChargeMultiplier,
                    bombardmentSplashPercentage: _config.BombardmentSplashPercentage,
                    dismountMov: _config.DismountMov,
                    dismountDefBonus: _config.DismountDefBonus,
                    dismountAtkBonus: _config.DismountAtkBonus,
                    battleLineMinUnits: _config.BattleLineMinUnits,
                    squareMinUnits: _config.SquareMinUnits,
                    cavalryWedgeMinUnits: _config.CavalryWedgeMinUnits,
                    cavalryWedgeMaxStep: _config.CavalryWedgeMaxStep
                );

                int wins = 0;
                int draws = 0;
                int totalRounds = 0;
                int total = 0;

                foreach (var (player, enemy, seed) in armyPairs)
                {
                    UnitFactory.ResetIds();
                    var grid = new GridMap(size, size);
                    var playerUnits = new List<UnitInstance>();
                    var enemyUnits = new List<UnitInstance>();

                    PlaceArmyWithConfig(player, Owner.Player, scaledConfig.PlayerDeployMinRow,
                        scaledConfig.PlayerDeployMaxRow, grid, playerUnits, size);
                    PlaceArmyWithConfig(enemy, Owner.Enemy, scaledConfig.EnemyDeployMinRow,
                        scaledConfig.EnemyDeployMaxRow, grid, enemyUnits, size);

                    if (playerUnits.Count == 0 || enemyUnits.Count == 0) continue;

                    total++;
                    var engine = new BattleEngine(grid, playerUnits, enemyUnits, scaledConfig, seed);
                    var battleResult = engine.RunFullBattle();

                    if (battleResult.Outcome == BattleOutcome.PlayerWin) wins++;
                    else if (battleResult.Outcome == BattleOutcome.Draw) draws++;
                    totalRounds += battleResult.RoundsPlayed;
                }

                if (total == 0) continue;

                result.Entries.Add(new GridSizeEntry
                {
                    GridSize = size,
                    WinRate = (wins * 100) / total,
                    DrawRate = (draws * 100) / total,
                    AvgRounds = totalRounds / total,
                    TotalBattles = total
                });
            }

            return result;
        }

        private void PlaceArmyWithConfig(List<string> unitTypes, Owner owner, int minRow, int maxRow,
            GridMap grid, List<UnitInstance> units, int gridWidth)
        {
            int placed = 0;
            for (int row = minRow; row <= maxRow && placed < unitTypes.Count; row++)
            {
                for (int col = 1; col <= gridWidth && placed < unitTypes.Count; col++)
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

        // ======= MODE 7: Formation Effectiveness Test =======

        /// <summary>
        /// Tests how much formations contribute to win rates.
        /// Compares armies placed in formation-friendly layouts vs scattered layouts.
        /// Reports per-formation effectiveness.
        /// </summary>
        public FormationEffectivenessResult RunFormationTest(int battlesPerFormation = 200)
        {
            var result = new FormationEffectivenessResult();

            // Test Battle Line: 3 infantry in a row vs 3 scattered
            result.Entries.Add(TestFormationVsScatter(
                "BattleLine", new[] { "LineInfantry", "LineInfantry", "LineInfantry", "Cavalry", "Artillery" },
                PlaceBattleLine, battlesPerFormation));

            // Test Battery: 2 artillery adjacent vs 2 apart
            result.Entries.Add(TestFormationVsScatter(
                "Battery", new[] { "Artillery", "Artillery", "LineInfantry", "LineInfantry", "Cavalry" },
                PlaceBattery, battlesPerFormation));

            // Test Cavalry Wedge: 3 cavalry diagonal vs 3 scattered
            result.Entries.Add(TestFormationVsScatter(
                "CavalryWedge", new[] { "Cavalry", "Cavalry", "Cavalry", "LineInfantry", "Artillery" },
                PlaceCavalryWedge, battlesPerFormation));

            // Test Square: 4 infantry in 2x2 vs 4 spread
            result.Entries.Add(TestFormationVsScatter(
                "Square", new[] { "LineInfantry", "LineInfantry", "Grenadier", "Grenadier", "Artillery" },
                PlaceSquare, battlesPerFormation));

            // Test Skirmish Screen: riflemen alone vs riflemen next to allies
            result.Entries.Add(TestFormationVsScatter(
                "SkirmishScreen", new[] { "Rifleman", "Rifleman", "Cavalry", "LineInfantry", "Artillery" },
                PlaceSkirmishScreen, battlesPerFormation));

            return result;
        }

        private FormationEffectivenessEntry TestFormationVsScatter(
            string formationName, string[] armyComp,
            Action<List<UnitInstance>, GridMap> formationPlacer,
            int battles)
        {
            int formationWins = 0;
            int scatterWins = 0;

            for (int b = 0; b < battles; b++)
            {
                int seed = _seedGen.Next();
                var enemyTypes = GenerateRandomArmy(40, AllUnitTypes);
                if (enemyTypes.Count == 0) continue;

                // Battle with formation placement
                {
                    UnitFactory.ResetIds();
                    var grid = new GridMap(_config.GridWidth, _config.GridHeight);
                    var playerUnits = new List<UnitInstance>();
                    foreach (var typeName in armyComp)
                    {
                        var unit = UnitFactory.CreateByTypeName(typeName, Owner.Player, new GridCoord(1, 1));
                        if (unit != null) playerUnits.Add(unit);
                    }
                    formationPlacer(playerUnits, grid);

                    var enemyUnits = new List<UnitInstance>();
                    PlaceArmy(enemyTypes.ToList(), Owner.Enemy, _config.EnemyDeployMinRow,
                        _config.EnemyDeployMaxRow, grid, enemyUnits);

                    if (playerUnits.Count > 0 && enemyUnits.Count > 0)
                    {
                        var engine = new BattleEngine(grid, playerUnits, enemyUnits, _config, seed);
                        if (engine.RunFullBattle().Outcome == BattleOutcome.PlayerWin)
                            formationWins++;
                    }
                }

                // Battle with scattered placement
                {
                    UnitFactory.ResetIds();
                    var grid = new GridMap(_config.GridWidth, _config.GridHeight);
                    var playerUnits = new List<UnitInstance>();
                    PlaceArmy(armyComp.ToList(), Owner.Player, _config.PlayerDeployMinRow,
                        _config.PlayerDeployMaxRow, grid, playerUnits);

                    var enemyUnits = new List<UnitInstance>();
                    PlaceArmy(enemyTypes.ToList(), Owner.Enemy, _config.EnemyDeployMinRow,
                        _config.EnemyDeployMaxRow, grid, enemyUnits);

                    if (playerUnits.Count > 0 && enemyUnits.Count > 0)
                    {
                        var engine = new BattleEngine(grid, playerUnits, enemyUnits, _config, seed);
                        if (engine.RunFullBattle().Outcome == BattleOutcome.PlayerWin)
                            scatterWins++;
                    }
                }
            }

            int formationRate = battles > 0 ? (formationWins * 100) / battles : 0;
            int scatterRate = battles > 0 ? (scatterWins * 100) / battles : 0;

            return new FormationEffectivenessEntry
            {
                FormationName = formationName,
                FormationWinRate = formationRate,
                ScatterWinRate = scatterRate,
                Delta = formationRate - scatterRate
            };
        }

        private void PlaceBattleLine(List<UnitInstance> units, GridMap grid)
        {
            int row = _config.PlayerDeployMinRow + 1;
            int startCol = Math.Max(1, (_config.GridWidth - units.Count) / 2 + 1);
            for (int i = 0; i < units.Count; i++)
            {
                var coord = new GridCoord(startCol + i, i < 3 ? row : row - 1);
                if (grid.IsValidCoord(coord) && grid.IsTileEmpty(coord))
                    grid.PlaceUnit(units[i], coord);
            }
        }

        private void PlaceBattery(List<UnitInstance> units, GridMap grid)
        {
            int row = _config.PlayerDeployMinRow;
            int midCol = (_config.GridWidth + 1) / 2;
            var positions = new[]
            {
                new GridCoord(midCol, row), new GridCoord(midCol + 1, row),        // 2 artillery adjacent
                new GridCoord(midCol - 1, row + 1), new GridCoord(midCol, row + 1), new GridCoord(midCol + 1, row + 1)
            };
            for (int i = 0; i < units.Count && i < positions.Length; i++)
            {
                if (grid.IsValidCoord(positions[i]) && grid.IsTileEmpty(positions[i]))
                    grid.PlaceUnit(units[i], positions[i]);
            }
        }

        private void PlaceCavalryWedge(List<UnitInstance> units, GridMap grid)
        {
            int baseRow = _config.PlayerDeployMinRow;
            int midCol = (_config.GridWidth + 1) / 2;
            var positions = new[]
            {
                new GridCoord(midCol, baseRow + 2),     // cavalry 1
                new GridCoord(midCol + 1, baseRow + 1), // cavalry 2 (diagonal)
                new GridCoord(midCol + 2, baseRow),     // cavalry 3 (diagonal)
                new GridCoord(midCol - 2, baseRow + 1), // infantry
                new GridCoord(midCol - 1, baseRow)      // artillery
            };
            for (int i = 0; i < units.Count && i < positions.Length; i++)
            {
                if (grid.IsValidCoord(positions[i]) && grid.IsTileEmpty(positions[i]))
                    grid.PlaceUnit(units[i], positions[i]);
            }
        }

        private void PlaceSquare(List<UnitInstance> units, GridMap grid)
        {
            int baseRow = _config.PlayerDeployMinRow;
            int midCol = (_config.GridWidth + 1) / 2;
            var positions = new[]
            {
                new GridCoord(midCol, baseRow),         // 2x2 square
                new GridCoord(midCol + 1, baseRow),
                new GridCoord(midCol, baseRow + 1),
                new GridCoord(midCol + 1, baseRow + 1),
                new GridCoord(midCol - 1, baseRow)      // artillery behind
            };
            for (int i = 0; i < units.Count && i < positions.Length; i++)
            {
                if (grid.IsValidCoord(positions[i]) && grid.IsTileEmpty(positions[i]))
                    grid.PlaceUnit(units[i], positions[i]);
            }
        }

        private void PlaceSkirmishScreen(List<UnitInstance> units, GridMap grid)
        {
            int row = _config.PlayerDeployMaxRow;
            // Spread riflemen with gaps so no adjacent friendlies
            var positions = new[]
            {
                new GridCoord(2, row),                  // rifleman alone
                new GridCoord(_config.GridWidth - 1, row), // rifleman alone
                new GridCoord(5, _config.PlayerDeployMinRow),
                new GridCoord(6, _config.PlayerDeployMinRow),
                new GridCoord(5, _config.PlayerDeployMinRow + 1)
            };
            for (int i = 0; i < units.Count && i < positions.Length; i++)
            {
                if (grid.IsValidCoord(positions[i]) && grid.IsTileEmpty(positions[i]))
                    grid.PlaceUnit(units[i], positions[i]);
            }
        }

        // ======= V2 Engine Helper =======

        /// <summary>
        /// Runs a battle using BattleEngineV2 with optional commanders and terrain.
        /// </summary>
        private BattleOutcome BattleArmiesV2(
            List<string> armyA, List<string> armyB, int seed,
            CommanderId playerCmd, int playerCmdRound,
            CommanderId enemyCmd, int enemyCmdRound,
            TerrainMap terrainMap)
        {
            UnitFactory.ResetIds();
            var grid = new GridMap(_config.GridWidth, _config.GridHeight);
            var playerUnits = new List<UnitInstance>();
            var enemyUnits = new List<UnitInstance>();

            PlaceArmy(armyA, Owner.Player, _config.PlayerDeployMinRow, _config.PlayerDeployMaxRow, grid, playerUnits);
            PlaceArmy(armyB, Owner.Enemy, _config.EnemyDeployMinRow, _config.EnemyDeployMaxRow, grid, enemyUnits);

            if (playerUnits.Count == 0 || enemyUnits.Count == 0)
                return BattleOutcome.Draw;

            var engine = new BattleEngineV2(grid, terrainMap, playerUnits, enemyUnits, _config, seed,
                playerCmd, playerCmdRound, enemyCmd, enemyCmdRound);
            return engine.RunFullBattle().Outcome;
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

        /// <summary>
        /// Generates a human-readable report from commander impact results.
        /// </summary>
        public static string FormatCommanderReport(CommanderImpactResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== COMMANDER IMPACT ANALYSIS ===");
            sb.AppendLine();
            sb.AppendLine($"{"Commander",-14} {"Ability",-26} {"Without",8} {"With",6} {"Delta",6} {"Flag",4}");
            sb.AppendLine(new string('-', 68));

            foreach (var kvp in result.Entries.OrderBy(e => e.Key))
            {
                var e = kvp.Value;
                string flag = e.Delta > 15 ? " !" : "";
                sb.AppendLine($"{e.CommanderName,-14} {e.AbilityName,-26} {e.WinRateWithoutCommander,7}% {e.WinRateWithCommander,5}% {e.Delta,5}%{flag}");
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

        /// <summary>
        /// Generates a human-readable report from terrain impact results.
        /// </summary>
        public static string FormatTerrainReport(TerrainImpactResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== TERRAIN IMPACT ANALYSIS ===");
            sb.AppendLine($"Baseline (open field) win rate: {result.BaselineWinRate}%");
            sb.AppendLine();
            sb.AppendLine($"{"Terrain",-16} {"WinRate",8} {"Baseline",9} {"Delta",6} {"Flag",4}");
            sb.AppendLine(new string('-', 48));

            foreach (var kvp in result.Entries.OrderByDescending(e => e.Value.Delta))
            {
                var e = kvp.Value;
                string flag = e.Delta > 20 ? " !" : "";
                sb.AppendLine($"{e.Terrain,-16} {e.WinRate,7}% {e.BaselineWinRate,8}% {e.Delta,5}%{flag}");
            }

            if (result.Anomalies.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== ANOMALIES (>20% win rate shift) ===");
                foreach (var a in result.Anomalies)
                    sb.AppendLine($"  ! {a}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a human-readable report from grid size scaling results.
        /// </summary>
        public static string FormatGridSizeReport(GridSizeResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== GRID SIZE SCALING ANALYSIS ===");
            sb.AppendLine();
            sb.AppendLine($"{"Size",6} {"WinRate",8} {"DrawRate",9} {"AvgRounds",10} {"Battles",8}");
            sb.AppendLine(new string('-', 46));

            foreach (var e in result.Entries)
            {
                sb.AppendLine($"{e.GridSize + "x" + e.GridSize,6} {e.WinRate,7}% {e.DrawRate,8}% {e.AvgRounds,10} {e.TotalBattles,8}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a human-readable report from formation effectiveness results.
        /// </summary>
        public static string FormatFormationReport(FormationEffectivenessResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== FORMATION EFFECTIVENESS ANALYSIS ===");
            sb.AppendLine();
            sb.AppendLine($"{"Formation",-18} {"Formed",8} {"Scatter",8} {"Delta",6}");
            sb.AppendLine(new string('-', 44));

            foreach (var e in result.Entries)
            {
                sb.AppendLine($"{e.FormationName,-18} {e.FormationWinRate,7}% {e.ScatterWinRate,7}% {e.Delta,+5}%");
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

    /// <summary>Result of Mode 4 commander impact testing.</summary>
    public class CommanderImpactResult
    {
        public Dictionary<CommanderId, CommanderImpactEntry> Entries = new Dictionary<CommanderId, CommanderImpactEntry>();
        public List<string> Anomalies = new List<string>();
    }

    /// <summary>Single commander's impact data.</summary>
    public class CommanderImpactEntry
    {
        public CommanderId CommanderId;
        public string CommanderName;
        public string AbilityName;
        public int WinRateWithCommander;
        public int WinRateWithoutCommander;
        public int Delta;
    }

    /// <summary>Result of Mode 5 terrain impact testing.</summary>
    public class TerrainImpactResult
    {
        public int BaselineWinRate;
        public Dictionary<TerrainType, TerrainImpactEntry> Entries = new Dictionary<TerrainType, TerrainImpactEntry>();
        public List<string> Anomalies = new List<string>();
    }

    /// <summary>Single terrain type's impact data.</summary>
    public class TerrainImpactEntry
    {
        public TerrainType Terrain;
        public int WinRate;
        public int BaselineWinRate;
        public int Delta;
    }

    /// <summary>Result of Mode 6 grid size scaling test.</summary>
    public class GridSizeResult
    {
        public List<GridSizeEntry> Entries = new List<GridSizeEntry>();
    }

    /// <summary>Data for a single grid size test.</summary>
    public class GridSizeEntry
    {
        public int GridSize;
        public int WinRate;
        public int DrawRate;
        public int AvgRounds;
        public int TotalBattles;
    }

    /// <summary>Result of Mode 7 formation effectiveness test.</summary>
    public class FormationEffectivenessResult
    {
        public List<FormationEffectivenessEntry> Entries = new List<FormationEffectivenessEntry>();
    }

    /// <summary>Single formation's effectiveness data.</summary>
    public class FormationEffectivenessEntry
    {
        public string FormationName;
        public int FormationWinRate;
        public int ScatterWinRate;
        public int Delta; // Positive = formation helps
    }
}
