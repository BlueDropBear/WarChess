using System;
using System.Collections.Generic;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Formations
{
    /// <summary>
    /// Result of formation detection for one unit.
    /// </summary>
    public class FormationBonus
    {
        public FormationType Type;
        public int AtkMultiplier; // base 100
        public int DefMultiplier; // base 100
        public int ChargeMultiplier; // base 100
        public int RangeBonus;
        public bool CannotBeFlanked;

        public static FormationBonus None => new FormationBonus
        {
            Type = FormationType.None,
            AtkMultiplier = 100,
            DefMultiplier = 100,
            ChargeMultiplier = 100,
            RangeBonus = 0,
            CannotBeFlanked = false
        };
    }

    /// <summary>
    /// Detects formation patterns among units on the grid and returns bonuses.
    /// GDD Section 2.8: formations recalculated each round.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class FormationDetector
    {
        /// <summary>
        /// Detects the formation bonus for a specific unit based on its allies' positions.
        /// Returns FormationBonus.None if no formation applies.
        /// </summary>
        public static FormationBonus DetectFormation(UnitInstance unit, GridMap grid, int battleLineDefBonus,
            int batteryAtkBonus, int cavalryWedgeChargeBonus, int squareDefVsCavalryBonus,
            int skirmishAtkBonus, int skirmishRangeBonus)
        {
            if (unit == null || grid == null) return FormationBonus.None;

            var allies = grid.GetAllUnits(unit.Owner);

            // Check each formation type the unit participates in
            var effectiveType = unit.CountsAsType;

            // Battle Line: 3+ LineInfantry (or counts-as) in a horizontal row
            if (effectiveType == UnitType.LineInfantry)
            {
                if (CheckBattleLine(unit, allies))
                    return new FormationBonus
                    {
                        Type = FormationType.BattleLine,
                        AtkMultiplier = 100,
                        DefMultiplier = battleLineDefBonus,
                        ChargeMultiplier = 100,
                        RangeBonus = 0,
                        CannotBeFlanked = false
                    };

                // Square: 4 infantry in 2x2 block
                if (CheckSquare(unit, allies))
                    return new FormationBonus
                    {
                        Type = FormationType.Square,
                        AtkMultiplier = 100,
                        DefMultiplier = squareDefVsCavalryBonus,
                        ChargeMultiplier = 100,
                        RangeBonus = 0,
                        CannotBeFlanked = true
                    };
            }

            // Artillery Battery: 2+ Artillery adjacent
            if (effectiveType == UnitType.Artillery)
            {
                if (CheckBattery(unit, allies))
                    return new FormationBonus
                    {
                        Type = FormationType.Battery,
                        AtkMultiplier = batteryAtkBonus,
                        DefMultiplier = 100,
                        ChargeMultiplier = 100,
                        RangeBonus = 0,
                        CannotBeFlanked = false
                    };
            }

            // Cavalry Wedge: 3+ cavalry in diagonal
            if (effectiveType == UnitType.Cavalry)
            {
                if (CheckCavalryWedge(unit, allies))
                    return new FormationBonus
                    {
                        Type = FormationType.CavalryWedge,
                        AtkMultiplier = 100,
                        DefMultiplier = 100,
                        ChargeMultiplier = cavalryWedgeChargeBonus,
                        RangeBonus = 0,
                        CannotBeFlanked = false
                    };
            }

            // Skirmish Screen: Rifleman with no adjacent friendlies
            if (unit.Type == UnitType.Rifleman)
            {
                if (CheckSkirmishScreen(unit, allies))
                    return new FormationBonus
                    {
                        Type = FormationType.SkirmishScreen,
                        AtkMultiplier = skirmishAtkBonus,
                        DefMultiplier = 100,
                        ChargeMultiplier = 100,
                        RangeBonus = skirmishRangeBonus,
                        CannotBeFlanked = false
                    };
            }

            return FormationBonus.None;
        }

        private static bool CheckBattleLine(UnitInstance unit, List<UnitInstance> allies)
        {
            // Count infantry (or counts-as) on the same row
            int row = unit.Position.Y;
            int countOnRow = 0;

            foreach (var ally in allies)
            {
                if (!ally.IsAlive) continue;
                if (ally.CountsAsType == UnitType.LineInfantry && ally.Position.Y == row)
                    countOnRow++;
            }

            return countOnRow >= 3;
        }

        private static bool CheckSquare(UnitInstance unit, List<UnitInstance> allies)
        {
            // Check all 4 possible 2x2 blocks this unit could be part of
            int x = unit.Position.X;
            int y = unit.Position.Y;

            int[][] offsets = new int[][]
            {
                new[] { 0, 0, 1, 0, 0, 1, 1, 1 },   // unit is top-left
                new[] { -1, 0, 0, 0, -1, 1, 0, 1 },  // unit is top-right
                new[] { 0, -1, 1, -1, 0, 0, 1, 0 },  // unit is bottom-left
                new[] { -1, -1, 0, -1, -1, 0, 0, 0 }, // unit is bottom-right
            };

            foreach (var off in offsets)
            {
                int count = 0;
                for (int i = 0; i < 8; i += 2)
                {
                    var checkPos = new GridCoord(x + off[i], y + off[i + 1]);
                    foreach (var ally in allies)
                    {
                        if (ally.IsAlive && ally.CountsAsType == UnitType.LineInfantry &&
                            ally.Position == checkPos)
                        {
                            count++;
                            break;
                        }
                    }
                }
                if (count >= 4) return true;
            }

            return false;
        }

        private static bool CheckBattery(UnitInstance unit, List<UnitInstance> allies)
        {
            // 2+ artillery adjacent (orthogonal)
            var neighbors = unit.Position.GetOrthogonalNeighbors();
            foreach (var neighbor in neighbors)
            {
                foreach (var ally in allies)
                {
                    if (ally.IsAlive && ally.Id != unit.Id &&
                        ally.CountsAsType == UnitType.Artillery &&
                        ally.Position == neighbor)
                        return true;
                }
            }
            return false;
        }

        private static bool CheckCavalryWedge(UnitInstance unit, List<UnitInstance> allies)
        {
            // 3+ cavalry in a diagonal line
            // Check 4 diagonal directions
            int[][] diagonals = { new[] { 1, 1 }, new[] { 1, -1 }, new[] { -1, 1 }, new[] { -1, -1 } };

            foreach (var dir in diagonals)
            {
                int count = 1; // Count self

                // Check forward along diagonal
                for (int step = 1; step <= 2; step++)
                {
                    var check = new GridCoord(
                        unit.Position.X + dir[0] * step,
                        unit.Position.Y + dir[1] * step);

                    if (HasCavalryAt(check, allies, unit.Id))
                        count++;
                    else
                        break;
                }

                // Check backward along diagonal
                for (int step = 1; step <= 2; step++)
                {
                    var check = new GridCoord(
                        unit.Position.X - dir[0] * step,
                        unit.Position.Y - dir[1] * step);

                    if (HasCavalryAt(check, allies, unit.Id))
                        count++;
                    else
                        break;
                }

                if (count >= 3) return true;
            }

            return false;
        }

        private static bool HasCavalryAt(GridCoord pos, List<UnitInstance> allies, int excludeId)
        {
            foreach (var ally in allies)
            {
                if (ally.IsAlive && ally.Id != excludeId &&
                    ally.CountsAsType == UnitType.Cavalry &&
                    ally.Position == pos)
                    return true;
            }
            return false;
        }

        private static bool CheckSkirmishScreen(UnitInstance unit, List<UnitInstance> allies)
        {
            // No adjacent friendly units
            var neighbors = unit.Position.GetOrthogonalNeighbors();
            foreach (var neighbor in neighbors)
            {
                foreach (var ally in allies)
                {
                    if (ally.IsAlive && ally.Id != unit.Id && ally.Position == neighbor)
                        return false;
                }
            }
            return true;
        }
    }
}
