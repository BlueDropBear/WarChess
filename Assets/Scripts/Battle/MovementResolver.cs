using System;
using System.Collections.Generic;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle
{
    /// <summary>
    /// Resolves unit movement toward targets. V1 uses simple Manhattan distance
    /// reduction — no obstacle avoidance, no terrain movement costs.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class MovementResolver
    {
        /// <summary>
        /// Calculates the best destination for a unit moving toward a target.
        /// Moves up to unit's MOV tiles, stepping one tile at a time toward
        /// the target, preferring to reduce the largest axis difference first.
        /// Stops early if the tile is occupied by another unit.
        /// </summary>
        public static GridCoord ResolveMovement(UnitInstance unit, UnitInstance target, GridMap grid)
        {
            return ResolveMovementWithSteps(unit, target, grid, out _);
        }

        /// <summary>
        /// Resolves movement and also outputs the actual number of steps taken (for charge detection).
        /// </summary>
        public static GridCoord ResolveMovementWithSteps(UnitInstance unit, UnitInstance target, GridMap grid, out int stepsTaken)
        {
            var current = unit.Position;
            var goal = target.Position;
            int movRemaining = unit.Mov;
            stepsTaken = 0;

            // Don't move if already adjacent and melee, or in range for ranged
            int distToTarget = current.ManhattanDistance(goal);
            if (distToTarget <= unit.Rng)
                return current;

            var position = current;

            for (int step = 0; step < movRemaining; step++)
            {
                var nextStep = GetBestStep(position, goal, grid, unit);
                if (nextStep == position)
                    break; // No valid move available

                // Check if we're now in range — stop moving
                position = nextStep;
                stepsTaken++;
                if (position.ManhattanDistance(goal) <= unit.Rng)
                    break;
            }

            return position;
        }

        /// <summary>
        /// Returns the number of tiles between two positions using Manhattan distance.
        /// NOTE: For charge detection, prefer the stepsTaken output from ResolveMovementWithSteps
        /// which tracks actual path length including obstacle detours.
        /// </summary>
        public static int GetTilesMoved(GridCoord from, GridCoord to)
        {
            return from.ManhattanDistance(to);
        }

        private static GridCoord GetBestStep(GridCoord current, GridCoord goal, GridMap grid, UnitInstance unit)
        {
            int dx = goal.X - current.X;
            int dy = goal.Y - current.Y;

            // Try to reduce the largest distance first
            var candidates = new List<GridCoord>(2);

            if (Math.Abs(dx) >= Math.Abs(dy))
            {
                // Prefer horizontal, then vertical
                if (dx != 0) candidates.Add(new GridCoord(current.X + Math.Sign(dx), current.Y));
                if (dy != 0) candidates.Add(new GridCoord(current.X, current.Y + Math.Sign(dy)));
            }
            else
            {
                // Prefer vertical, then horizontal
                if (dy != 0) candidates.Add(new GridCoord(current.X, current.Y + Math.Sign(dy)));
                if (dx != 0) candidates.Add(new GridCoord(current.X + Math.Sign(dx), current.Y));
            }

            // Try each candidate — pick the first valid one
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (grid.IsValidCoord(c) && grid.IsTileEmpty(c))
                    return c;
            }

            // No valid move — stay put
            return current;
        }
    }
}
