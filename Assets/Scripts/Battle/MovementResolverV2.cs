using System;
using System.Collections.Generic;
using WarChess.Core;
using WarChess.Terrain;
using WarChess.Units;

namespace WarChess.Battle
{
    /// <summary>
    /// Terrain-aware movement resolver. Accounts for movement costs from terrain.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class MovementResolverV2
    {
        /// <summary>
        /// Resolves movement toward a target, spending movement points based on
        /// terrain cost. Stops when movement budget is exhausted or target is in range.
        /// </summary>
        public static GridCoord ResolveMovement(
            UnitInstance unit, UnitInstance target, GridMap grid,
            TerrainMap terrainMap, int effectiveMov)
        {
            return ResolveMovementWithSteps(unit, target, grid, terrainMap, effectiveMov, out _);
        }

        /// <summary>
        /// Resolves terrain-aware movement and also outputs the actual number of steps taken.
        /// </summary>
        public static GridCoord ResolveMovementWithSteps(
            UnitInstance unit, UnitInstance target, GridMap grid,
            TerrainMap terrainMap, int effectiveMov, out int stepsTaken)
        {
            var current = unit.Position;
            var goal = target.Position;
            stepsTaken = 0;

            if (current.ManhattanDistance(goal) <= unit.Rng)
                return current;

            int movBudget = effectiveMov;
            var position = current;

            while (movBudget > 0)
            {
                var nextStep = GetBestStep(position, goal, grid, terrainMap, movBudget);
                if (nextStep == position)
                    break;

                int cost = terrainMap.GetMovementCost(nextStep);
                if (cost > movBudget)
                    break;

                movBudget -= cost;
                position = nextStep;
                stepsTaken++;

                if (position.ManhattanDistance(goal) <= unit.Rng)
                    break;
            }

            return position;
        }

        private static GridCoord GetBestStep(
            GridCoord current, GridCoord goal, GridMap grid,
            TerrainMap terrainMap, int movBudget)
        {
            int dx = goal.X - current.X;
            int dy = goal.Y - current.Y;

            var candidates = new List<GridCoord>(2);

            if (Math.Abs(dx) >= Math.Abs(dy))
            {
                if (dx != 0) candidates.Add(new GridCoord(current.X + Math.Sign(dx), current.Y));
                if (dy != 0) candidates.Add(new GridCoord(current.X, current.Y + Math.Sign(dy)));
            }
            else
            {
                if (dy != 0) candidates.Add(new GridCoord(current.X, current.Y + Math.Sign(dy)));
                if (dx != 0) candidates.Add(new GridCoord(current.X + Math.Sign(dx), current.Y));
            }

            foreach (var c in candidates)
            {
                if (grid.IsValidCoord(c) && grid.IsTileEmpty(c))
                {
                    int cost = terrainMap.GetMovementCost(c);
                    if (cost <= movBudget)
                        return c;
                }
            }

            return current;
        }
    }
}
