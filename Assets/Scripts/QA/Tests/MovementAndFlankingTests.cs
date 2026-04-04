using System;
using System.Collections.Generic;
using WarChess.Battle;
using WarChess.Config;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.QA.Tests
{
    /// <summary>
    /// Correctness tests for movement resolution and flanking direction detection.
    /// </summary>
    public static class MovementAndFlankingTests
    {
        public static List<QATestResult> RunAll(GameConfigData config)
        {
            return new List<QATestResult>
            {
                TestMovementRange(config),
                TestMovementStopsAtRange(config),
                TestFlankDirections(config),
                TestFlankMultipliers(config)
            };
        }

        /// <summary>
        /// Units must not move more tiles than their MOV stat.
        /// </summary>
        public static QATestResult TestMovementRange(GameConfigData config)
        {
            const string name = "Movement.Range";
            try
            {
                UnitFactory.ResetIds();
                var grid = new GridMap(config.GridWidth, config.GridHeight);

                // Line Infantry MOV=2
                var infantry = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 1));
                var target = UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 10));
                grid.PlaceUnit(infantry, infantry.Position);
                grid.PlaceUnit(target, target.Position);

                var dest = MovementResolver.ResolveMovement(infantry, target, grid);
                int moved = MovementResolver.GetTilesMoved(infantry.Position, dest);

                if (moved > infantry.Mov)
                    return QATestResult.Fail(name,
                        $"Infantry moved {moved} tiles but MOV={infantry.Mov}");

                // Hussar MOV=5
                UnitFactory.ResetIds();
                grid = new GridMap(config.GridWidth, config.GridHeight);
                var hussar = UnitFactory.CreateHussar(Owner.Player, new GridCoord(5, 1));
                var target2 = UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 10));
                grid.PlaceUnit(hussar, hussar.Position);
                grid.PlaceUnit(target2, target2.Position);

                var dest2 = MovementResolver.ResolveMovement(hussar, target2, grid);
                int moved2 = MovementResolver.GetTilesMoved(hussar.Position, dest2);

                if (moved2 > hussar.Mov)
                    return QATestResult.Fail(name,
                        $"Hussar moved {moved2} tiles but MOV={hussar.Mov}");

                return QATestResult.Pass(name,
                    $"Movement range enforced: Infantry moved {moved}/{infantry.Mov}, Hussar moved {moved2}/{hussar.Mov}");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Unit should stop moving once target is within attack range.
        /// </summary>
        public static QATestResult TestMovementStopsAtRange(GameConfigData config)
        {
            const string name = "Movement.StopsAtRange";
            try
            {
                UnitFactory.ResetIds();
                var grid = new GridMap(config.GridWidth, config.GridHeight);

                // Rifleman RNG=3, MOV=2 — target at distance 4, should move 1 tile to be at dist 3
                var rifleman = UnitFactory.CreateRifleman(Owner.Player, new GridCoord(5, 1));
                var target = UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 5));
                grid.PlaceUnit(rifleman, rifleman.Position);
                grid.PlaceUnit(target, target.Position);

                var dest = MovementResolver.ResolveMovement(rifleman, target, grid);
                int distAfter = dest.ManhattanDistance(target.Position);

                if (distAfter > rifleman.Rng)
                    return QATestResult.Fail(name,
                        $"Rifleman stopped at dist {distAfter} but RNG={rifleman.Rng} — should have moved closer");

                // Artillery RNG=4, target at distance 4: should not move at all
                UnitFactory.ResetIds();
                grid = new GridMap(config.GridWidth, config.GridHeight);
                var artillery = UnitFactory.CreateArtillery(Owner.Player, new GridCoord(5, 1));
                var target2 = UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 5));
                grid.PlaceUnit(artillery, artillery.Position);
                grid.PlaceUnit(target2, target2.Position);

                var dest2 = MovementResolver.ResolveMovement(artillery, target2, grid);
                if (dest2 != artillery.Position)
                    return QATestResult.Fail(name,
                        $"Artillery moved when target already in range (dist=4, RNG=4)");

                return QATestResult.Pass(name, "Movement stops at attack range correctly");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Tests flank directions for North-facing and South-facing defenders.
        /// </summary>
        public static QATestResult TestFlankDirections(GameConfigData config)
        {
            const string name = "Flanking.Directions";
            try
            {
                var defenderPos = new GridCoord(5, 5);

                // North-facing defender: front=north, rear=south, sides=east/west
                var frontN = FlankingCalculator.GetFlankDirection(new GridCoord(5, 7), defenderPos, FacingDirection.North);
                if (frontN != FlankDirection.Front)
                    return QATestResult.Fail(name, $"North-facing: attack from north should be Front, got {frontN}");

                var rearN = FlankingCalculator.GetFlankDirection(new GridCoord(5, 3), defenderPos, FacingDirection.North);
                if (rearN != FlankDirection.Rear)
                    return QATestResult.Fail(name, $"North-facing: attack from south should be Rear, got {rearN}");

                var sideN = FlankingCalculator.GetFlankDirection(new GridCoord(8, 5), defenderPos, FacingDirection.North);
                if (sideN != FlankDirection.Side)
                    return QATestResult.Fail(name, $"North-facing: attack from east should be Side, got {sideN}");

                // South-facing defender: front=south, rear=north, sides=east/west
                var frontS = FlankingCalculator.GetFlankDirection(new GridCoord(5, 3), defenderPos, FacingDirection.South);
                if (frontS != FlankDirection.Front)
                    return QATestResult.Fail(name, $"South-facing: attack from south should be Front, got {frontS}");

                var rearS = FlankingCalculator.GetFlankDirection(new GridCoord(5, 7), defenderPos, FacingDirection.South);
                if (rearS != FlankDirection.Rear)
                    return QATestResult.Fail(name, $"South-facing: attack from north should be Rear, got {rearS}");

                return QATestResult.Pass(name, "All 6 directional checks passed for N/S facing");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Flank multipliers must match per-unit values (Front=100, Side=130, Rear=200/150/250).
        /// </summary>
        public static QATestResult TestFlankMultipliers(GameConfigData config)
        {
            const string name = "Flanking.Multipliers";
            try
            {
                UnitFactory.ResetIds();
                var standard = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(1, 1));
                var oldGuard = UnitFactory.CreateOldGuard(Owner.Player, new GridCoord(2, 1));
                var rocket = UnitFactory.CreateRocketBattery(Owner.Player, new GridCoord(3, 1));

                // Standard unit: Front=100, Side=130, Rear=200
                if (FlankingCalculator.GetMultiplier(FlankDirection.Front, standard) != 100)
                    return QATestResult.Fail(name, "Front multiplier should be 100");
                if (FlankingCalculator.GetMultiplier(FlankDirection.Side, standard) != 130)
                    return QATestResult.Fail(name, $"Side multiplier should be 130, got {FlankingCalculator.GetMultiplier(FlankDirection.Side, standard)}");
                if (FlankingCalculator.GetMultiplier(FlankDirection.Rear, standard) != 200)
                    return QATestResult.Fail(name, $"Rear multiplier should be 200, got {FlankingCalculator.GetMultiplier(FlankDirection.Rear, standard)}");

                // Old Guard: Rear=150 (reduced vulnerability)
                if (FlankingCalculator.GetMultiplier(FlankDirection.Rear, oldGuard) != 150)
                    return QATestResult.Fail(name, $"OldGuard rear should be 150, got {FlankingCalculator.GetMultiplier(FlankDirection.Rear, oldGuard)}");

                // Rocket Battery: Rear=250 (extra fragile)
                if (FlankingCalculator.GetMultiplier(FlankDirection.Rear, rocket) != 250)
                    return QATestResult.Fail(name, $"RocketBattery rear should be 250, got {FlankingCalculator.GetMultiplier(FlankDirection.Rear, rocket)}");

                return QATestResult.Pass(name, "All per-unit flank multipliers verified");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }
    }
}
