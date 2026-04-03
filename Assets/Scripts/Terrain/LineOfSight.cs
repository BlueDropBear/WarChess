using System;
using WarChess.Core;

namespace WarChess.Terrain
{
    /// <summary>
    /// Checks line of sight between two grid positions using the terrain map.
    /// GDD Section 4.2: ranged attacks blocked by Forest/Town tiles with units.
    /// Exceptions: Hill fires over one obstacle, Rocket Battery ignores LoS.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class LineOfSight
    {
        /// <summary>
        /// Returns true if the attacker has line of sight to the target.
        /// Uses Bresenham's line to check intervening tiles.
        /// </summary>
        public static bool HasLineOfSight(
            GridCoord from, GridCoord to,
            TerrainMap terrainMap, GridMap gridMap,
            bool attackerOnHill, bool ignoresLoS)
        {
            // Rocket Battery ignores LoS entirely
            if (ignoresLoS) return true;

            // Same tile or adjacent — always has LoS
            if (from.ManhattanDistance(to) <= 1) return true;

            int obstaclesAllowed = attackerOnHill ? 1 : 0;
            int obstaclesSeen = 0;

            // Walk Bresenham's line between from and to, skipping endpoints
            var points = BresenhamLine(from.X, from.Y, to.X, to.Y);

            for (int i = 1; i < points.Length - 1; i++) // Skip start and end
            {
                var coord = points[i];
                if (coord.X < 1 || coord.X > terrainMap.Width ||
                    coord.Y < 1 || coord.Y > terrainMap.Height)
                    continue;

                var terrain = terrainMap.GetTerrain(coord);

                if (TerrainData.BlocksLineOfSight(terrain))
                {
                    // Only blocks if there's a unit on the tile (per GDD)
                    var unitOnTile = gridMap.GetUnitAt(coord);
                    if (unitOnTile != null)
                    {
                        obstaclesSeen++;
                        if (obstaclesSeen > obstaclesAllowed)
                            return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Simplified check: does LoS path pass through any blocking terrain?
        /// Used when you don't care about the hill exception.
        /// </summary>
        public static bool HasClearPath(GridCoord from, GridCoord to, TerrainMap terrainMap)
        {
            var points = BresenhamLine(from.X, from.Y, to.X, to.Y);

            for (int i = 1; i < points.Length - 1; i++)
            {
                var coord = points[i];
                if (coord.X < 1 || coord.X > terrainMap.Width ||
                    coord.Y < 1 || coord.Y > terrainMap.Height)
                    continue;

                if (TerrainData.BlocksLineOfSight(terrainMap.GetTerrain(coord)))
                    return false;
            }

            return true;
        }

        private static GridCoord[] BresenhamLine(int x0, int y0, int x1, int y1)
        {
            var result = new System.Collections.Generic.List<GridCoord>();

            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                result.Add(new GridCoord(x0, y0));

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            return result.ToArray();
        }
    }
}
