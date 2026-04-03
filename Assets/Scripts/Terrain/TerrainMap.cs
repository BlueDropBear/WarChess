using WarChess.Core;

namespace WarChess.Terrain
{
    /// <summary>
    /// Stores the terrain type for each tile on the grid. Separate from GridMap
    /// to keep unit placement logic decoupled from terrain logic.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class TerrainMap
    {
        private readonly int _width;
        private readonly int _height;
        private readonly TerrainType[,] _terrain; // 0-indexed

        public int Width => _width;
        public int Height => _height;

        public TerrainMap(int width, int height)
        {
            _width = width;
            _height = height;
            _terrain = new TerrainType[width, height];

            // Default all tiles to open field
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _terrain[x, y] = TerrainType.OpenField;
        }

        /// <summary>Creates a default 10x10 all-open terrain map.</summary>
        public TerrainMap() : this(10, 10) { }

        /// <summary>Gets the terrain type at the given coordinate.</summary>
        public TerrainType GetTerrain(GridCoord coord)
        {
            if (coord.X < 1 || coord.X > _width || coord.Y < 1 || coord.Y > _height)
                return TerrainType.OpenField;
            return _terrain[coord.X - 1, coord.Y - 1];
        }

        /// <summary>Sets the terrain type at the given coordinate.</summary>
        public void SetTerrain(GridCoord coord, TerrainType type)
        {
            if (coord.X < 1 || coord.X > _width || coord.Y < 1 || coord.Y > _height)
                return;
            _terrain[coord.X - 1, coord.Y - 1] = type;
        }

        /// <summary>
        /// Returns the movement cost to enter the given tile.
        /// </summary>
        public int GetMovementCost(GridCoord coord)
        {
            return TerrainData.GetMovementCost(GetTerrain(coord));
        }

        /// <summary>
        /// Returns the defense multiplier (base 100) for a unit on this tile.
        /// </summary>
        public int GetDefenseMultiplier(GridCoord coord)
        {
            return TerrainData.GetDefenseMultiplier(GetTerrain(coord));
        }

        /// <summary>
        /// Returns the attack multiplier (base 100) for a ranged unit on this tile.
        /// </summary>
        public int GetAttackMultiplier(GridCoord coord)
        {
            return TerrainData.GetAttackMultiplier(GetTerrain(coord));
        }
    }
}
