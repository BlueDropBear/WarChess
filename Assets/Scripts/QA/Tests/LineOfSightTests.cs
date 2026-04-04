using System;
using System.Collections.Generic;
using WarChess.Config;
using WarChess.Core;
using WarChess.Terrain;
using WarChess.Units;

namespace WarChess.QA.Tests
{
    /// <summary>
    /// Correctness tests for line of sight: clear paths, forest blocking,
    /// hill bypass, and rocket battery ignoring LoS.
    /// </summary>
    public static class LineOfSightTests
    {
        public static List<QATestResult> RunAll(GameConfigData config)
        {
            return new List<QATestResult>
            {
                TestLoSClear(config),
                TestLoSForestBlocks(config),
                TestLoSHillBypass(config),
                TestLoSRocketIgnores(config)
            };
        }

        /// <summary>
        /// Open field should always have clear line of sight.
        /// </summary>
        public static QATestResult TestLoSClear(GameConfigData config)
        {
            const string name = "LoS.ClearOpenField";
            try
            {
                var terrainMap = new TerrainMap(config.GridWidth, config.GridHeight);
                var gridMap = new GridMap(config.GridWidth, config.GridHeight);

                // Test various positions across the grid
                var pairs = new[]
                {
                    (new GridCoord(1, 1), new GridCoord(10, 10)),
                    (new GridCoord(5, 1), new GridCoord(5, 10)),
                    (new GridCoord(1, 5), new GridCoord(10, 5)),
                    (new GridCoord(3, 3), new GridCoord(7, 7))
                };

                foreach (var (from, to) in pairs)
                {
                    if (!LineOfSight.HasLineOfSight(from, to, terrainMap, gridMap, false, false))
                        return QATestResult.Fail(name,
                            $"LoS blocked on open field from {from} to {to}");
                }

                return QATestResult.Pass(name, "Open field LoS clear for all test paths");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Forest tile with a unit on it should block line of sight.
        /// </summary>
        public static QATestResult TestLoSForestBlocks(GameConfigData config)
        {
            const string name = "LoS.ForestBlocks";
            try
            {
                UnitFactory.ResetIds();
                var terrainMap = new TerrainMap(config.GridWidth, config.GridHeight);
                var gridMap = new GridMap(config.GridWidth, config.GridHeight);

                // Place forest at (5, 5)
                terrainMap.SetTerrain(new GridCoord(5, 5), TerrainType.Forest);

                // Place a unit on the forest tile
                var blocker = UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 5));
                gridMap.PlaceUnit(blocker, blocker.Position);

                // LoS from (5,1) to (5,9) should be blocked (goes through (5,5) forest + unit)
                bool hasLos = LineOfSight.HasLineOfSight(
                    new GridCoord(5, 1), new GridCoord(5, 9),
                    terrainMap, gridMap, false, false);

                if (hasLos)
                    return QATestResult.Fail(name,
                        "LoS should be blocked by forest tile with unit");

                // Without unit on forest tile, LoS should be clear
                gridMap.RemoveUnit(new GridCoord(5, 5));
                bool hasLos2 = LineOfSight.HasLineOfSight(
                    new GridCoord(5, 1), new GridCoord(5, 9),
                    terrainMap, gridMap, false, false);

                if (!hasLos2)
                    return QATestResult.Fail(name,
                        "LoS should NOT be blocked by empty forest tile");

                return QATestResult.Pass(name, "Forest + unit blocks LoS, empty forest does not");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Hill grants +1 obstacle allowance for line of sight.
        /// </summary>
        public static QATestResult TestLoSHillBypass(GameConfigData config)
        {
            const string name = "LoS.HillBypass";
            try
            {
                UnitFactory.ResetIds();
                var terrainMap = new TerrainMap(config.GridWidth, config.GridHeight);
                var gridMap = new GridMap(config.GridWidth, config.GridHeight);

                // Place forest at (5, 5) with a unit
                terrainMap.SetTerrain(new GridCoord(5, 5), TerrainType.Forest);
                var blocker = UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 5));
                gridMap.PlaceUnit(blocker, blocker.Position);

                // Without hill: blocked
                bool blocked = LineOfSight.HasLineOfSight(
                    new GridCoord(5, 1), new GridCoord(5, 9),
                    terrainMap, gridMap, false, false);
                if (blocked)
                    return QATestResult.Fail(name, "Should be blocked without hill");

                // With hill (attackerOnHill=true): should pass through 1 obstacle
                bool bypassOne = LineOfSight.HasLineOfSight(
                    new GridCoord(5, 1), new GridCoord(5, 9),
                    terrainMap, gridMap, true, false);
                if (!bypassOne)
                    return QATestResult.Fail(name, "Hill should allow bypassing 1 obstacle");

                // Add second obstacle: even hill can't bypass 2
                terrainMap.SetTerrain(new GridCoord(5, 7), TerrainType.Forest);
                var blocker2 = UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 7));
                gridMap.PlaceUnit(blocker2, blocker2.Position);

                bool blocked2 = LineOfSight.HasLineOfSight(
                    new GridCoord(5, 1), new GridCoord(5, 9),
                    terrainMap, gridMap, true, false);
                if (blocked2)
                    return QATestResult.Fail(name, "Hill should not bypass 2 obstacles");

                return QATestResult.Pass(name, "Hill correctly bypasses 1 obstacle, not 2");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Rocket Battery (CongreveBarrage) should ignore line of sight entirely.
        /// </summary>
        public static QATestResult TestLoSRocketIgnores(GameConfigData config)
        {
            const string name = "LoS.RocketIgnoresLoS";
            try
            {
                UnitFactory.ResetIds();
                var terrainMap = new TerrainMap(config.GridWidth, config.GridHeight);
                var gridMap = new GridMap(config.GridWidth, config.GridHeight);

                // Place multiple blocking terrain + units
                for (int y = 3; y <= 7; y++)
                {
                    terrainMap.SetTerrain(new GridCoord(5, y), TerrainType.Forest);
                    var blocker = UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, y));
                    gridMap.PlaceUnit(blocker, blocker.Position);
                }

                // ignoresLoS=true should always return true
                bool hasLos = LineOfSight.HasLineOfSight(
                    new GridCoord(5, 1), new GridCoord(5, 9),
                    terrainMap, gridMap, false, true);

                if (!hasLos)
                    return QATestResult.Fail(name,
                        "Rocket Battery (ignoresLoS=true) should have LoS through any terrain");

                return QATestResult.Pass(name, "Rocket Battery correctly ignores all LoS blocking");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }
    }
}
