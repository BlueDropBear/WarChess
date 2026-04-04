using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarChess.Battle;
using WarChess.Commanders;
using WarChess.Config;
using WarChess.Core;
using WarChess.Formations;
using WarChess.Multiplayer;
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

        // ======= MODE 8: Tier Balance Test =======

        /// <summary>
        /// Tests each multiplayer tier independently using that tier's unit pool.
        /// GDD Section 12: ensures no tier is internally imbalanced.
        /// </summary>
        public TierBalanceResult RunTierBalanceTest(
            int battlesPerTier = 200, int armyCount = 100, int battlesPerArmy = 20)
        {
            var result = new TierBalanceResult();

            for (int tier = 1; tier <= 5; tier++)
            {
                var tierData = TierSystem.GetTier(tier);
                var available = tierData.AvailableUnits;

                // Pick appropriate budget — smaller tiers use smaller budget
                int budget = tier <= 2 ? 25 : 40;

                var compResult = RunCompositionTest(budget, armyCount, battlesPerArmy, available);

                var entry = new TierBalanceEntry
                {
                    Tier = tier,
                    TierName = tierData.Name,
                    UnitCount = available.Length,
                    Budget = budget,
                    DominantCount = compResult.DominantCompositions.Count,
                    WeakCount = compResult.WeakCompositions.Count,
                    OverpoweredUnits = new List<string>(compResult.OverpoweredUnits),
                    UnderpoweredUnits = new List<string>(compResult.UnderpoweredUnits)
                };
                result.Entries.Add(entry);

                if (compResult.DominantCompositions.Count > armyCount / 10)
                {
                    result.Anomalies.Add(
                        $"Tier {tier} ({tierData.Name}): {compResult.DominantCompositions.Count} dominant compositions detected");
                }
            }

            return result;
        }

        // ======= MODE 9: Enhanced Matchup Test (GDD Spec) =======

        /// <summary>
        /// Enhanced Mode 1 that uses varied terrain and positioning per GDD spec:
        /// "1,000 times with varied terrain and positioning."
        /// Places units at random valid positions and applies random terrain.
        /// </summary>
        public MatchupResult RunMatchupTestV2(int battlesPerMatchup = 100)
        {
            int count = AllUnitTypes.Length;
            var matrix = new int[count, count];
            var anomalies = new List<string>();
            var terrainTypes = new[] { TerrainType.OpenField, TerrainType.Forest, TerrainType.Hill,
                                       TerrainType.Fortification, TerrainType.Town };

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    if (i == j)
                    {
                        matrix[i, j] = 50;
                        continue;
                    }

                    int wins = 0;
                    for (int b = 0; b < battlesPerMatchup; b++)
                    {
                        int seed = _seedGen.Next();
                        UnitFactory.ResetIds();

                        var grid = new GridMap(_config.GridWidth, _config.GridHeight);

                        // Random positions within deployment zones
                        int playerCol = _seedGen.Next(_config.GridWidth) + 1;
                        int playerRow = _config.PlayerDeployMinRow + _seedGen.Next(
                            _config.PlayerDeployMaxRow - _config.PlayerDeployMinRow + 1);
                        int enemyCol = _seedGen.Next(_config.GridWidth) + 1;
                        int enemyRow = _config.EnemyDeployMinRow + _seedGen.Next(
                            _config.EnemyDeployMaxRow - _config.EnemyDeployMinRow + 1);

                        var unitA = UnitFactory.CreateByTypeName(AllUnitTypes[i], Owner.Player,
                            new GridCoord(playerCol, playerRow));
                        var unitB = UnitFactory.CreateByTypeName(AllUnitTypes[j], Owner.Enemy,
                            new GridCoord(enemyCol, enemyRow));

                        if (unitA == null || unitB == null) continue;
                        grid.PlaceUnit(unitA, unitA.Position);
                        grid.PlaceUnit(unitB, unitB.Position);

                        // Random terrain in the center
                        var terrainMap = new TerrainMap(_config.GridWidth, _config.GridHeight);
                        var randTerrain = terrainTypes[_seedGen.Next(terrainTypes.Length)];
                        if (randTerrain != TerrainType.OpenField)
                        {
                            int midRow = (_config.PlayerDeployMaxRow + _config.EnemyDeployMinRow) / 2;
                            for (int x = 1; x <= _config.GridWidth; x++)
                                terrainMap.SetTerrain(new GridCoord(x, midRow), randTerrain);
                        }

                        var engine = new BattleEngineV2(grid, terrainMap,
                            new List<UnitInstance> { unitA },
                            new List<UnitInstance> { unitB },
                            _config, seed);
                        var result = engine.RunFullBattle();
                        if (result.Outcome == BattleOutcome.PlayerWin) wins++;
                    }

                    int winRate = (wins * 100) / battlesPerMatchup;
                    matrix[i, j] = winRate;

                    if (winRate > 70)
                        anomalies.Add($"{AllUnitTypes[i]} beats {AllUnitTypes[j]} {winRate}% (V2+terrain)");
                }
            }

            return new MatchupResult { Matrix = matrix, Anomalies = anomalies };
        }

        // ======= MODE 10: Strategy Test (GDD 12.3) =======

        /// <summary>
        /// Tests all 4 army builder strategies against each other per GDD 12.3.
        /// Ensures variety is viable — no single strategy dominates.
        /// </summary>
        public StrategyTestResult RunStrategyTest(
            int budget = 40, int battlesPerPairing = 100, string[] availableUnits = null)
        {
            if (availableUnits == null) availableUnits = AllUnitTypes;

            var strategies = new ArmyBuilderStrategies(_config, new Random(_seedGen.Next()));
            var result = new StrategyTestResult();

            // Generate matchup data from Mode 1 for counter-picking strategy
            var matchupResult = RunMatchupTest(50);
            var matchupData = new MatchupData { Matrix = matchupResult.Matrix };

            // Generate win rate data for meta strategy (from a quick composition test)
            var quickComp = RunCompositionTest(budget, 50, 20, availableUnits);
            var unitWinRates = new Dictionary<string, int>();
            foreach (var unitType in availableUnits)
                unitWinRates[unitType] = 50; // Default baseline

            var strategyNames = new[] { "Random", "CavalryRush", "ArtilleryFort", "BalancedLine", "InfantryWall", "Counter", "Meta" };
            int stratCount = strategyNames.Length;
            var winMatrix = new int[stratCount, stratCount];
            var totalMatrix = new int[stratCount, stratCount];

            for (int i = 0; i < stratCount; i++)
            {
                for (int j = i + 1; j < stratCount; j++)
                {
                    for (int b = 0; b < battlesPerPairing; b++)
                    {
                        var armyA = BuildStrategyArmy(strategies, i, budget, availableUnits, matchupData, unitWinRates, null);
                        var armyB = BuildStrategyArmy(strategies, j, budget, availableUnits, matchupData, unitWinRates, armyA);

                        if (armyA.Count == 0 || armyB.Count == 0) continue;

                        int seed = _seedGen.Next();
                        var outcome = BattleArmies(armyA, armyB, seed);

                        totalMatrix[i, j]++;
                        totalMatrix[j, i]++;

                        if (outcome == BattleOutcome.PlayerWin)
                            winMatrix[i, j]++;
                        else if (outcome == BattleOutcome.EnemyWin)
                            winMatrix[j, i]++;
                    }
                }
            }

            // Build result entries
            for (int i = 0; i < stratCount; i++)
            {
                int totalWins = 0;
                int totalGames = 0;
                for (int j = 0; j < stratCount; j++)
                {
                    if (i == j) continue;
                    totalWins += winMatrix[i, j];
                    totalGames += totalMatrix[i, j];
                }

                int winRate = totalGames > 0 ? (totalWins * 100) / totalGames : 50;
                var entry = new StrategyTestEntry
                {
                    StrategyName = strategyNames[i],
                    OverallWinRate = winRate,
                    TotalGames = totalGames
                };
                result.Entries.Add(entry);

                if (winRate > 65)
                    result.Anomalies.Add($"{strategyNames[i]} dominates with {winRate}% win rate");
                else if (winRate < 35)
                    result.Anomalies.Add($"{strategyNames[i]} is too weak with only {winRate}% win rate");
            }

            return result;
        }

        private List<string> BuildStrategyArmy(ArmyBuilderStrategies strategies, int stratIdx,
            int budget, string[] available, MatchupData matchupData,
            Dictionary<string, int> unitWinRates, List<string> opponentArmy)
        {
            return stratIdx switch
            {
                0 => strategies.BuildRandom(budget, available),
                1 => strategies.BuildArchetype(budget, available, ArmyArchetype.CavalryRush),
                2 => strategies.BuildArchetype(budget, available, ArmyArchetype.ArtilleryFort),
                3 => strategies.BuildArchetype(budget, available, ArmyArchetype.BalancedLine),
                4 => strategies.BuildArchetype(budget, available, ArmyArchetype.InfantryWall),
                5 => opponentArmy != null
                    ? strategies.BuildCounter(budget, available, opponentArmy, matchupData)
                    : strategies.BuildRandom(budget, available),
                6 => strategies.BuildMeta(budget, available, unitWinRates),
                _ => strategies.BuildRandom(budget, available)
            };
        }

        // ======= MODE 11: Cost Validation & Suggestion =======

        /// <summary>
        /// Validates unit costs by comparing algorithmic cost vs empirical win-rate
        /// efficiency. Measures "cost efficiency" = win contribution per army point
        /// for each unit type. Suggests cost adjustments when a unit significantly
        /// over- or under-performs relative to its cost.
        ///
        /// The algorithm:
        /// 1. Build random armies, track which unit types appear
        /// 2. Run battles, track wins per unit type per cost point
        /// 3. Compare each unit's cost efficiency to the average
        /// 4. Flag units >40% above/below average efficiency
        /// 5. Suggest +/- 1 cost adjustments and report breakdowns
        ///
        /// This mode helps the QA tester understand the cost algorithm and
        /// recommend specific config value changes based on simulation data.
        /// </summary>
        public CostValidationResult RunCostValidationTest(
            int budget = 40, int armyCount = 300, int battlesPerArmy = 30,
            UnitCostConfig costConfig = null)
        {
            if (costConfig == null) costConfig = UnitCostConfig.Default;
            var result = new CostValidationResult();
            var costs = UnitCostCalculator.CalculateAllCosts(costConfig);
            var breakdowns = UnitCostCalculator.CalculateAllBreakdowns(costConfig);

            // Track wins and appearances per unit type
            var unitWins = new Dictionary<string, int>();
            var unitAppearances = new Dictionary<string, int>();
            foreach (var t in AllUnitTypes)
            {
                unitWins[t] = 0;
                unitAppearances[t] = 0;
            }

            // Generate armies and battle them
            var armies = new List<List<string>>();
            for (int i = 0; i < armyCount; i++)
                armies.Add(GenerateRandomArmy(budget, AllUnitTypes));

            for (int i = 0; i < armyCount; i++)
            {
                for (int b = 0; b < battlesPerArmy; b++)
                {
                    int opponent = _seedGen.Next(armyCount);
                    if (opponent == i) opponent = (opponent + 1) % armyCount;

                    int seed = _seedGen.Next();
                    var outcome = BattleArmies(armies[i], armies[opponent], seed);

                    // Track appearances (count each type once per army per battle)
                    var seenI = new HashSet<string>();
                    foreach (var unit in armies[i])
                        if (seenI.Add(unit)) unitAppearances[unit]++;

                    var seenO = new HashSet<string>();
                    foreach (var unit in armies[opponent])
                        if (seenO.Add(unit)) unitAppearances[unit]++;

                    // Track wins
                    if (outcome == BattleOutcome.PlayerWin)
                    {
                        foreach (var unit in armies[i])
                            if (seenI.Contains(unit)) { unitWins[unit]++; seenI.Remove(unit); }
                    }
                    else if (outcome == BattleOutcome.EnemyWin)
                    {
                        foreach (var unit in armies[opponent])
                            if (seenO.Contains(unit)) { unitWins[unit]++; seenO.Remove(unit); }
                    }
                }
            }

            // Calculate cost efficiency for each unit
            // Efficiency = (winRate * 100) / cost — higher means more value per point
            int totalEfficiency = 0;
            int unitCount = 0;

            foreach (var unitType in AllUnitTypes)
            {
                int appearances = unitAppearances[unitType];
                int wins = unitWins[unitType];
                int cost = costs.TryGetValue(unitType, out int c) ? c : 1;
                int winRate = appearances > 0 ? (wins * 100) / appearances : 0;
                int efficiency = cost > 0 ? (winRate * 100) / cost : 0;

                totalEfficiency += efficiency;
                unitCount++;

                var breakdown = breakdowns.TryGetValue(unitType, out var bd)
                    ? bd : default;

                result.Entries[unitType] = new CostValidationEntry
                {
                    UnitType = unitType,
                    AlgorithmCost = cost,
                    WinRate = winRate,
                    CostEfficiency = efficiency,
                    Appearances = appearances,
                    Wins = wins,
                    Breakdown = breakdown
                };
            }

            int avgEfficiency = unitCount > 0 ? totalEfficiency / unitCount : 100;
            result.AverageEfficiency = avgEfficiency;

            // Generate suggestions
            foreach (var kvp in result.Entries)
            {
                var entry = kvp.Value;
                int cost = entry.AlgorithmCost;

                if (avgEfficiency <= 0) continue;

                int deviationPct = ((entry.CostEfficiency - avgEfficiency) * 100) / avgEfficiency;
                entry.EfficiencyDeviation = deviationPct;

                if (deviationPct > 40 && cost < 15)
                {
                    entry.SuggestedCost = cost + 1;
                    entry.Suggestion = $"{entry.UnitType} is {deviationPct}% above average efficiency " +
                                       $"(eff={entry.CostEfficiency}, avg={avgEfficiency}). " +
                                       $"Consider increasing cost from {cost} to {cost + 1}, " +
                                       $"or reducing AbilityFlatValue for {entry.Breakdown.AbilityMultiplier}x ability.";
                    result.Suggestions.Add(entry.Suggestion);
                }
                else if (deviationPct < -40 && cost > 1)
                {
                    entry.SuggestedCost = cost - 1;
                    entry.Suggestion = $"{entry.UnitType} is {-deviationPct}% below average efficiency " +
                                       $"(eff={entry.CostEfficiency}, avg={avgEfficiency}). " +
                                       $"Consider decreasing cost from {cost} to {cost - 1}, " +
                                       $"or increasing AbilityFlatValue.";
                    result.Suggestions.Add(entry.Suggestion);
                }
                else
                {
                    entry.SuggestedCost = cost;
                    entry.Suggestion = null;
                }
            }

            // Add algorithm report as reference
            result.AlgorithmReport = UnitCostCalculator.GenerateCostReport(costConfig);

            return result;
        }

        /// <summary>
        /// Generates a human-readable report from cost validation results,
        /// including the algorithm breakdown and specific adjustment suggestions.
        /// </summary>
        public static string FormatCostValidationReport(CostValidationResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== UNIT COST VALIDATION & SUGGESTIONS ===");
            sb.AppendLine();

            // Algorithm breakdown
            sb.AppendLine(result.AlgorithmReport);
            sb.AppendLine();

            // Efficiency table
            sb.AppendLine($"Average cost efficiency: {result.AverageEfficiency}");
            sb.AppendLine();
            sb.AppendLine($"{"Unit",-18} {"Cost",5} {"WinRate",8} {"Effic",6} {"Dev%",5} {"Suggest",8} {"Flag",4}");
            sb.AppendLine(new string('-', 60));

            foreach (var unitType in AllUnitTypes)
            {
                if (!result.Entries.TryGetValue(unitType, out var e)) continue;
                string flag = e.SuggestedCost != e.AlgorithmCost ? " !" : "";
                string suggest = e.SuggestedCost != e.AlgorithmCost
                    ? e.SuggestedCost.ToString()
                    : "-";
                sb.AppendLine($"{e.UnitType,-18} {e.AlgorithmCost,5} {e.WinRate,7}% {e.CostEfficiency,6} {e.EfficiencyDeviation,+4}% {suggest,8}{flag}");
            }

            if (result.Suggestions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== SUGGESTED ADJUSTMENTS ===");
                sb.AppendLine("To apply these, modify UnitCostConfig ability values or add overrides:");
                sb.AppendLine();
                foreach (var s in result.Suggestions)
                    sb.AppendLine($"  -> {s}");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("All unit costs are within acceptable efficiency bounds. No adjustments needed.");
            }

            return sb.ToString();
        }

        // ======= Full Report (GDD Section 12 Output) =======

        /// <summary>
        /// Runs all modes and generates the 6 GDD-specified reports plus additional modes
        /// in a single integrated output:
        /// 1. Unit Matchup Matrix
        /// 2. Composition Win Rates
        /// 3. Officer Impact
        /// 4. Commander Impact
        /// 5. Tier Balance
        /// 6. Terrain Bias
        /// 7. Grid Size Scaling
        /// 8. Formation Effectiveness
        /// 9. Strategy Viability
        /// Plus an Anomaly List aggregating all flagged issues.
        /// </summary>
        public FullQAReport RunFullReport(
            int matchupBattles = 100, int compBudget = 40, int compArmies = 200,
            int compBattles = 50, int officerBattles = 200, int commanderBattles = 200,
            int tierBattles = 200, int terrainBattles = 200, int gridBattles = 200,
            int formationBattles = 200, int strategyBattles = 100,
            int costValidationArmies = 300, int costValidationBattles = 30)
        {
            var report = new FullQAReport();

            report.Matchup = RunMatchupTest(matchupBattles);
            report.MatchupV2 = RunMatchupTestV2(matchupBattles);
            report.Composition = RunCompositionTest(compBudget, compArmies, compBattles);
            report.OfficerImpact = RunOfficerImpactTest(compBudget, officerBattles);
            report.CommanderImpact = RunCommanderImpactTest(compBudget, commanderBattles);
            report.TierBalance = RunTierBalanceTest(tierBattles);
            report.TerrainImpact = RunTerrainImpactTest(compBudget, terrainBattles);
            report.GridSize = RunGridSizeTest(compBudget, gridBattles);
            report.FormationEffectiveness = RunFormationTest(formationBattles);
            report.StrategyTest = RunStrategyTest(compBudget, strategyBattles);
            report.CostValidation = RunCostValidationTest(compBudget, costValidationArmies, costValidationBattles);

            // Aggregate anomalies
            report.AllAnomalies = new List<string>();
            report.AllAnomalies.AddRange(report.Matchup.Anomalies.Select(a => $"[Matchup] {a}"));
            report.AllAnomalies.AddRange(report.MatchupV2.Anomalies.Select(a => $"[MatchupV2] {a}"));
            if (report.Composition.DominantCompositions.Count > 0)
                report.AllAnomalies.AddRange(report.Composition.DominantCompositions.Select(a => $"[Composition] Dominant: {a}"));
            report.AllAnomalies.AddRange(report.OfficerImpact.Anomalies.Select(a => $"[Officer] {a}"));
            report.AllAnomalies.AddRange(report.CommanderImpact.Anomalies.Select(a => $"[Commander] {a}"));
            report.AllAnomalies.AddRange(report.TierBalance.Anomalies.Select(a => $"[Tier] {a}"));
            report.AllAnomalies.AddRange(report.TerrainImpact.Anomalies.Select(a => $"[Terrain] {a}"));
            report.AllAnomalies.AddRange(report.StrategyTest.Anomalies.Select(a => $"[Strategy] {a}"));
            report.AllAnomalies.AddRange(report.CostValidation.Suggestions.Select(s => $"[CostValidation] {s}"));

            return report;
        }

        /// <summary>
        /// Generates a full human-readable report from all modes.
        /// </summary>
        public static string FormatFullReport(FullQAReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("╔══════════════════════════════════════════════════╗");
            sb.AppendLine("║       WARCHESS FULL QA BALANCE REPORT           ║");
            sb.AppendLine("╚══════════════════════════════════════════════════╝");
            sb.AppendLine();

            sb.AppendLine(FormatMatchupReport(report.Matchup));
            sb.AppendLine();
            sb.AppendLine("--- Enhanced Matchup (V2 + Terrain) ---");
            sb.AppendLine(FormatMatchupReport(report.MatchupV2));
            sb.AppendLine();
            sb.AppendLine(FormatOfficerReport(report.OfficerImpact));
            sb.AppendLine();
            sb.AppendLine(FormatCommanderReport(report.CommanderImpact));
            sb.AppendLine();
            sb.AppendLine(FormatTierBalanceReport(report.TierBalance));
            sb.AppendLine();
            sb.AppendLine(FormatTerrainReport(report.TerrainImpact));
            sb.AppendLine();
            sb.AppendLine(FormatGridSizeReport(report.GridSize));
            sb.AppendLine();
            sb.AppendLine(FormatFormationReport(report.FormationEffectiveness));
            sb.AppendLine();
            sb.AppendLine(FormatStrategyReport(report.StrategyTest));
            sb.AppendLine();
            sb.AppendLine(FormatCostValidationReport(report.CostValidation));

            if (report.AllAnomalies.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("╔══════════════════════════════════════════════════╗");
                sb.AppendLine("║            AGGREGATED ANOMALY LIST              ║");
                sb.AppendLine("╚══════════════════════════════════════════════════╝");
                foreach (var a in report.AllAnomalies)
                    sb.AppendLine($"  ! {a}");
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("No anomalies detected across all test modes.");
            }

            return sb.ToString();
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
        /// Generates a human-readable report from tier balance results.
        /// </summary>
        public static string FormatTierBalanceReport(TierBalanceResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== TIER BALANCE ANALYSIS ===");
            sb.AppendLine();
            sb.AppendLine($"{"Tier",4} {"Name",-22} {"Units",6} {"Budget",7} {"Dominant",9} {"Weak",5}");
            sb.AppendLine(new string('-', 58));

            foreach (var e in result.Entries)
            {
                sb.AppendLine($"{e.Tier,4} {e.TierName,-22} {e.UnitCount,6} {e.Budget,7} {e.DominantCount,9} {e.WeakCount,5}");
                foreach (var op in e.OverpoweredUnits)
                    sb.AppendLine($"       OP: {op}");
                foreach (var up in e.UnderpoweredUnits)
                    sb.AppendLine($"       UP: {up}");
            }

            if (result.Anomalies.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== ANOMALIES ===");
                foreach (var a in result.Anomalies)
                    sb.AppendLine($"  ! {a}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a human-readable report from strategy test results.
        /// </summary>
        public static string FormatStrategyReport(StrategyTestResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== STRATEGY VIABILITY ANALYSIS ===");
            sb.AppendLine();
            sb.AppendLine($"{"Strategy",-18} {"WinRate",8} {"Games",6}");
            sb.AppendLine(new string('-', 36));

            foreach (var e in result.Entries)
            {
                sb.AppendLine($"{e.StrategyName,-18} {e.OverallWinRate,7}% {e.TotalGames,6}");
            }

            if (result.Anomalies.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== ANOMALIES ===");
                foreach (var a in result.Anomalies)
                    sb.AppendLine($"  ! {a}");
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

    /// <summary>Result of Mode 8 tier balance testing.</summary>
    public class TierBalanceResult
    {
        public List<TierBalanceEntry> Entries = new List<TierBalanceEntry>();
        public List<string> Anomalies = new List<string>();
    }

    /// <summary>Single tier's balance data.</summary>
    public class TierBalanceEntry
    {
        public int Tier;
        public string TierName;
        public int UnitCount;
        public int Budget;
        public int DominantCount;
        public int WeakCount;
        public List<string> OverpoweredUnits = new List<string>();
        public List<string> UnderpoweredUnits = new List<string>();
    }

    /// <summary>Result of Mode 10 strategy testing.</summary>
    public class StrategyTestResult
    {
        public List<StrategyTestEntry> Entries = new List<StrategyTestEntry>();
        public List<string> Anomalies = new List<string>();
    }

    /// <summary>Single strategy's performance data.</summary>
    public class StrategyTestEntry
    {
        public string StrategyName;
        public int OverallWinRate;
        public int TotalGames;
    }

    /// <summary>Full QA report aggregating all test modes.</summary>
    public class FullQAReport
    {
        public MatchupResult Matchup;
        public MatchupResult MatchupV2;
        public CompositionResult Composition;
        public OfficerImpactResult OfficerImpact;
        public CommanderImpactResult CommanderImpact;
        public TierBalanceResult TierBalance;
        public TerrainImpactResult TerrainImpact;
        public GridSizeResult GridSize;
        public FormationEffectivenessResult FormationEffectiveness;
        public StrategyTestResult StrategyTest;
        public CostValidationResult CostValidation;
        public List<string> AllAnomalies;
    }

    /// <summary>
    /// Result of cost validation testing. Contains per-unit efficiency data,
    /// the algorithm breakdown report, and specific cost adjustment suggestions.
    /// </summary>
    public class CostValidationResult
    {
        /// <summary>Per-unit validation entries keyed by unit type name.</summary>
        public Dictionary<string, CostValidationEntry> Entries = new Dictionary<string, CostValidationEntry>();

        /// <summary>Average cost efficiency across all units (higher = more value per point).</summary>
        public int AverageEfficiency;

        /// <summary>Human-readable suggestions for cost adjustments.</summary>
        public List<string> Suggestions = new List<string>();

        /// <summary>Full algorithm breakdown report for reference.</summary>
        public string AlgorithmReport;
    }

    /// <summary>
    /// Per-unit cost validation data showing algorithmic cost vs empirical performance.
    /// </summary>
    public class CostValidationEntry
    {
        /// <summary>Unit type name (e.g., "Cavalry").</summary>
        public string UnitType;

        /// <summary>Cost computed by the algorithm.</summary>
        public int AlgorithmCost;

        /// <summary>Win rate when this unit appears in armies (0-100).</summary>
        public int WinRate;

        /// <summary>Cost efficiency = winRate * 100 / cost. Higher = more value per point.</summary>
        public int CostEfficiency;

        /// <summary>Deviation from average efficiency (%). Positive = overperforming.</summary>
        public int EfficiencyDeviation;

        /// <summary>Number of armies this unit appeared in.</summary>
        public int Appearances;

        /// <summary>Number of wins in armies containing this unit.</summary>
        public int Wins;

        /// <summary>Suggested cost based on empirical performance. Same as AlgorithmCost if no change needed.</summary>
        public int SuggestedCost;

        /// <summary>Human-readable suggestion string, or null if no adjustment needed.</summary>
        public string Suggestion;

        /// <summary>Algorithm breakdown for this unit.</summary>
        public CostBreakdown Breakdown;
    }
}
