using System;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle
{
    /// <summary>
    /// Calculates flank direction and damage multiplier based on attacker/defender
    /// positions and defender facing. Pure C# — no Unity dependencies.
    /// </summary>
    public static class FlankingCalculator
    {
        /// <summary>
        /// Determines whether an attack comes from the front, side, or rear
        /// relative to the defender's facing direction.
        /// </summary>
        public static FlankDirection GetFlankDirection(
            GridCoord attackerPos, GridCoord defenderPos, FacingDirection defenderFacing)
        {
            int dx = attackerPos.X - defenderPos.X;
            int dy = attackerPos.Y - defenderPos.Y;

            // If attacker is on the same tile (shouldn't happen), treat as front
            if (dx == 0 && dy == 0) return FlankDirection.Front;

            if (defenderFacing == FacingDirection.North)
            {
                // Defender faces North (toward row 10)
                // Front = attacker is north of defender (dy > 0)
                // Rear = attacker is south of defender (dy < 0)
                // Side = attacker is to the east/west
                if (dy > 0 && Math.Abs(dx) <= dy) return FlankDirection.Front;
                if (dy < 0 && Math.Abs(dx) <= Math.Abs(dy)) return FlankDirection.Rear;
                return FlankDirection.Side;
            }
            else
            {
                // Defender faces South (toward row 1)
                // Front = attacker is south (dy < 0)
                // Rear = attacker is north (dy > 0)
                if (dy < 0 && Math.Abs(dx) <= Math.Abs(dy)) return FlankDirection.Front;
                if (dy > 0 && Math.Abs(dx) <= dy) return FlankDirection.Rear;
                return FlankDirection.Side;
            }
        }

        /// <summary>
        /// Returns the flanking damage multiplier (base 100) for the given
        /// direction, using the defender's per-unit multipliers.
        /// Front = 100 (no bonus), Side and Rear use unit-specific values.
        /// </summary>
        public static int GetMultiplier(FlankDirection direction, UnitInstance defender)
        {
            return direction switch
            {
                FlankDirection.Side => defender.FlankSideMultiplier,
                FlankDirection.Rear => defender.FlankRearMultiplier,
                _ => 100
            };
        }
    }
}
