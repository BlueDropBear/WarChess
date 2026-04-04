using System;
using System.Collections.Generic;
using WarChess.Config;
using WarChess.Core;
using WarChess.Formations;
using WarChess.Units;

namespace WarChess.QA.Tests
{
    /// <summary>
    /// Correctness tests for all 5 formation detection patterns
    /// and dead unit exclusion.
    /// </summary>
    public static class FormationTests
    {
        public static List<QATestResult> RunAll(GameConfigData config)
        {
            return new List<QATestResult>
            {
                TestBattleLineDetection(config),
                TestBatteryDetection(config),
                TestSquareDetection(config),
                TestCavalryWedgeDetection(config),
                TestSkirmishScreenDetection(config),
                TestFormationExclusion(config)
            };
        }

        /// <summary>
        /// 3+ infantry in the same row should trigger BattleLine.
        /// </summary>
        public static QATestResult TestBattleLineDetection(GameConfigData config)
        {
            const string name = "Formation.BattleLine";
            try
            {
                UnitFactory.ResetIds();
                var grid = new GridMap(config.GridWidth, config.GridHeight);

                // Place 3 infantry in row 2
                var inf1 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(3, 2));
                var inf2 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(4, 2));
                var inf3 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 2));
                grid.PlaceUnit(inf1, inf1.Position);
                grid.PlaceUnit(inf2, inf2.Position);
                grid.PlaceUnit(inf3, inf3.Position);

                var bonus = FormationDetector.DetectFormation(inf2, grid,
                    config.BattleLineDefBonus, config.BatteryAtkBonus,
                    config.CavalryWedgeChargeBonus, config.SquareDefVsCavalryBonus,
                    config.SkirmishAtkBonus, config.SkirmishRangeBonus,
                    config.BattleLineMinUnits, config.SquareMinUnits,
                    config.CavalryWedgeMinUnits, config.CavalryWedgeMaxStep);

                if (bonus.Type != FormationType.BattleLine)
                    return QATestResult.Fail(name,
                        $"Expected BattleLine, got {bonus.Type}");
                if (bonus.DefMultiplier != config.BattleLineDefBonus)
                    return QATestResult.Fail(name,
                        $"Expected DEF bonus {config.BattleLineDefBonus}, got {bonus.DefMultiplier}");

                // 2 infantry should NOT trigger (need 3)
                UnitFactory.ResetIds();
                grid = new GridMap(config.GridWidth, config.GridHeight);
                var inf4 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(3, 2));
                var inf5 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(4, 2));
                grid.PlaceUnit(inf4, inf4.Position);
                grid.PlaceUnit(inf5, inf5.Position);

                var noBonus = FormationDetector.DetectFormation(inf4, grid,
                    config.BattleLineDefBonus, config.BatteryAtkBonus,
                    config.CavalryWedgeChargeBonus, config.SquareDefVsCavalryBonus,
                    config.SkirmishAtkBonus, config.SkirmishRangeBonus,
                    config.BattleLineMinUnits, config.SquareMinUnits,
                    config.CavalryWedgeMinUnits, config.CavalryWedgeMaxStep);

                if (noBonus.Type != FormationType.None)
                    return QATestResult.Fail(name,
                        $"2 infantry should not trigger BattleLine, got {noBonus.Type}");

                return QATestResult.Pass(name, "BattleLine detected with 3+ infantry, not with 2");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// 2+ artillery adjacent should trigger Battery.
        /// </summary>
        public static QATestResult TestBatteryDetection(GameConfigData config)
        {
            const string name = "Formation.Battery";
            try
            {
                UnitFactory.ResetIds();
                var grid = new GridMap(config.GridWidth, config.GridHeight);

                var art1 = UnitFactory.CreateArtillery(Owner.Player, new GridCoord(5, 1));
                var art2 = UnitFactory.CreateArtillery(Owner.Player, new GridCoord(6, 1));
                grid.PlaceUnit(art1, art1.Position);
                grid.PlaceUnit(art2, art2.Position);

                var bonus = FormationDetector.DetectFormation(art1, grid,
                    config.BattleLineDefBonus, config.BatteryAtkBonus,
                    config.CavalryWedgeChargeBonus, config.SquareDefVsCavalryBonus,
                    config.SkirmishAtkBonus, config.SkirmishRangeBonus,
                    config.BattleLineMinUnits, config.SquareMinUnits,
                    config.CavalryWedgeMinUnits, config.CavalryWedgeMaxStep);

                if (bonus.Type != FormationType.Battery)
                    return QATestResult.Fail(name, $"Expected Battery, got {bonus.Type}");
                if (bonus.AtkMultiplier != config.BatteryAtkBonus)
                    return QATestResult.Fail(name,
                        $"Expected ATK bonus {config.BatteryAtkBonus}, got {bonus.AtkMultiplier}");

                return QATestResult.Pass(name, "Battery detected with 2 adjacent artillery");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// 4 infantry in 2x2 should trigger Square with CannotBeFlanked.
        /// </summary>
        public static QATestResult TestSquareDetection(GameConfigData config)
        {
            const string name = "Formation.Square";
            try
            {
                UnitFactory.ResetIds();
                var grid = new GridMap(config.GridWidth, config.GridHeight);

                // 2x2 block at (4,1), (5,1), (4,2), (5,2)
                var inf1 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(4, 1));
                var inf2 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 1));
                var inf3 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(4, 2));
                var inf4 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 2));
                grid.PlaceUnit(inf1, inf1.Position);
                grid.PlaceUnit(inf2, inf2.Position);
                grid.PlaceUnit(inf3, inf3.Position);
                grid.PlaceUnit(inf4, inf4.Position);

                var bonus = FormationDetector.DetectFormation(inf1, grid,
                    config.BattleLineDefBonus, config.BatteryAtkBonus,
                    config.CavalryWedgeChargeBonus, config.SquareDefVsCavalryBonus,
                    config.SkirmishAtkBonus, config.SkirmishRangeBonus,
                    config.BattleLineMinUnits, config.SquareMinUnits,
                    config.CavalryWedgeMinUnits, config.CavalryWedgeMaxStep);

                if (bonus.Type != FormationType.Square)
                    return QATestResult.Fail(name, $"Expected Square, got {bonus.Type}");
                if (!bonus.CannotBeFlanked)
                    return QATestResult.Fail(name, "Square should grant CannotBeFlanked");
                if (bonus.DefMultiplier != config.SquareDefVsCavalryBonus)
                    return QATestResult.Fail(name,
                        $"Expected DEF bonus {config.SquareDefVsCavalryBonus}, got {bonus.DefMultiplier}");

                return QATestResult.Pass(name, "Square detected with 4 infantry in 2x2, CannotBeFlanked granted");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// 3+ cavalry in diagonal should trigger CavalryWedge.
        /// </summary>
        public static QATestResult TestCavalryWedgeDetection(GameConfigData config)
        {
            const string name = "Formation.CavalryWedge";
            try
            {
                UnitFactory.ResetIds();
                var grid = new GridMap(config.GridWidth, config.GridHeight);

                // Diagonal: (3,1), (4,2), (5,3)
                var cav1 = UnitFactory.CreateCavalry(Owner.Player, new GridCoord(3, 1));
                var cav2 = UnitFactory.CreateCavalry(Owner.Player, new GridCoord(4, 2));
                var cav3 = UnitFactory.CreateCavalry(Owner.Player, new GridCoord(5, 3));
                grid.PlaceUnit(cav1, cav1.Position);
                grid.PlaceUnit(cav2, cav2.Position);
                grid.PlaceUnit(cav3, cav3.Position);

                var bonus = FormationDetector.DetectFormation(cav2, grid,
                    config.BattleLineDefBonus, config.BatteryAtkBonus,
                    config.CavalryWedgeChargeBonus, config.SquareDefVsCavalryBonus,
                    config.SkirmishAtkBonus, config.SkirmishRangeBonus,
                    config.BattleLineMinUnits, config.SquareMinUnits,
                    config.CavalryWedgeMinUnits, config.CavalryWedgeMaxStep);

                if (bonus.Type != FormationType.CavalryWedge)
                    return QATestResult.Fail(name, $"Expected CavalryWedge, got {bonus.Type}");
                if (bonus.ChargeMultiplier != config.CavalryWedgeChargeBonus)
                    return QATestResult.Fail(name,
                        $"Expected charge bonus {config.CavalryWedgeChargeBonus}, got {bonus.ChargeMultiplier}");

                return QATestResult.Pass(name, "CavalryWedge detected with 3 diagonal cavalry");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Rifleman with no adjacent allies should trigger SkirmishScreen.
        /// </summary>
        public static QATestResult TestSkirmishScreenDetection(GameConfigData config)
        {
            const string name = "Formation.SkirmishScreen";
            try
            {
                UnitFactory.ResetIds();
                var grid = new GridMap(config.GridWidth, config.GridHeight);

                // Rifleman alone at (5,2), nearest ally at (8,2) — not adjacent
                var rifle = UnitFactory.CreateRifleman(Owner.Player, new GridCoord(5, 2));
                var ally = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(8, 2));
                grid.PlaceUnit(rifle, rifle.Position);
                grid.PlaceUnit(ally, ally.Position);

                var bonus = FormationDetector.DetectFormation(rifle, grid,
                    config.BattleLineDefBonus, config.BatteryAtkBonus,
                    config.CavalryWedgeChargeBonus, config.SquareDefVsCavalryBonus,
                    config.SkirmishAtkBonus, config.SkirmishRangeBonus,
                    config.BattleLineMinUnits, config.SquareMinUnits,
                    config.CavalryWedgeMinUnits, config.CavalryWedgeMaxStep);

                if (bonus.Type != FormationType.SkirmishScreen)
                    return QATestResult.Fail(name, $"Expected SkirmishScreen, got {bonus.Type}");
                if (bonus.AtkMultiplier != config.SkirmishAtkBonus)
                    return QATestResult.Fail(name,
                        $"Expected ATK bonus {config.SkirmishAtkBonus}, got {bonus.AtkMultiplier}");
                if (bonus.RangeBonus != config.SkirmishRangeBonus)
                    return QATestResult.Fail(name,
                        $"Expected range bonus {config.SkirmishRangeBonus}, got {bonus.RangeBonus}");

                // With adjacent ally — should NOT trigger
                UnitFactory.ResetIds();
                grid = new GridMap(config.GridWidth, config.GridHeight);
                var rifle2 = UnitFactory.CreateRifleman(Owner.Player, new GridCoord(5, 2));
                var adjacent = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(6, 2));
                grid.PlaceUnit(rifle2, rifle2.Position);
                grid.PlaceUnit(adjacent, adjacent.Position);

                var noBonus = FormationDetector.DetectFormation(rifle2, grid,
                    config.BattleLineDefBonus, config.BatteryAtkBonus,
                    config.CavalryWedgeChargeBonus, config.SquareDefVsCavalryBonus,
                    config.SkirmishAtkBonus, config.SkirmishRangeBonus,
                    config.BattleLineMinUnits, config.SquareMinUnits,
                    config.CavalryWedgeMinUnits, config.CavalryWedgeMaxStep);

                if (noBonus.Type == FormationType.SkirmishScreen)
                    return QATestResult.Fail(name,
                        "SkirmishScreen should not trigger with adjacent ally");

                return QATestResult.Pass(name, "SkirmishScreen detected when isolated, not when adjacent");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Dead units should not count toward formation requirements.
        /// </summary>
        public static QATestResult TestFormationExclusion(GameConfigData config)
        {
            const string name = "Formation.DeadUnitExclusion";
            try
            {
                UnitFactory.ResetIds();
                var grid = new GridMap(config.GridWidth, config.GridHeight);

                // 3 infantry in row 2 — should trigger BattleLine
                var inf1 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(3, 2));
                var inf2 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(4, 2));
                var inf3 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 2));
                grid.PlaceUnit(inf1, inf1.Position);
                grid.PlaceUnit(inf2, inf2.Position);
                grid.PlaceUnit(inf3, inf3.Position);

                // Kill one — now only 2 alive, shouldn't trigger
                inf3.TakeDamage(9999);

                var bonus = FormationDetector.DetectFormation(inf1, grid,
                    config.BattleLineDefBonus, config.BatteryAtkBonus,
                    config.CavalryWedgeChargeBonus, config.SquareDefVsCavalryBonus,
                    config.SkirmishAtkBonus, config.SkirmishRangeBonus,
                    config.BattleLineMinUnits, config.SquareMinUnits,
                    config.CavalryWedgeMinUnits, config.CavalryWedgeMaxStep);

                if (bonus.Type == FormationType.BattleLine)
                    return QATestResult.Fail(name,
                        "BattleLine triggered with only 2 alive infantry (dead unit counted)");

                return QATestResult.Pass(name, "Dead units correctly excluded from formation detection");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }
    }
}
