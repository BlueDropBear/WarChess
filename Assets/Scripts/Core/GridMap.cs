using System;
using System.Collections.Generic;
using WarChess.Config;
using WarChess.Units;

namespace WarChess.Core
{
    /// <summary>
    /// Logical grid managing unit positions. Dimensions are configurable (default 10x10).
    /// Pure C# — no Unity dependencies. Coordinates are 1-based.
    /// </summary>
    public class GridMap
    {
        private readonly int _width;
        private readonly int _height;
        private readonly UnitInstance[,] _grid; // 0-indexed internally

        public int Width => _width;
        public int Height => _height;

        public GridMap(int width, int height)
        {
            _width = width;
            _height = height;
            _grid = new UnitInstance[width, height];
        }

        /// <summary>Creates a standard 10x10 grid.</summary>
        public GridMap() : this(10, 10) { }

        /// <summary>Returns true if the coordinate is within grid bounds.</summary>
        public bool IsValidCoord(GridCoord coord)
        {
            return coord.X >= 1 && coord.X <= _width && coord.Y >= 1 && coord.Y <= _height;
        }

        /// <summary>Returns true if the coordinate is within the deployment zone for the given owner.</summary>
        public bool IsInDeploymentZone(GridCoord coord, Owner owner, GameConfigData config)
        {
            if (!IsValidCoord(coord)) return false;

            if (owner == Owner.Player)
                return coord.Y >= config.PlayerDeployMinRow && coord.Y <= config.PlayerDeployMaxRow;
            else
                return coord.Y >= config.EnemyDeployMinRow && coord.Y <= config.EnemyDeployMaxRow;
        }

        /// <summary>Places a unit at the given coordinate. Throws if tile is occupied.</summary>
        public void PlaceUnit(UnitInstance unit, GridCoord coord)
        {
            if (!IsValidCoord(coord))
                throw new ArgumentException($"Invalid coordinate: {coord}");

            if (GetUnitAt(coord) != null)
                throw new InvalidOperationException($"Tile {coord} is already occupied");

            _grid[coord.X - 1, coord.Y - 1] = unit;
            unit.Position = coord;
        }

        /// <summary>Removes the unit from the given coordinate.</summary>
        public void RemoveUnit(GridCoord coord)
        {
            if (!IsValidCoord(coord)) return;
            _grid[coord.X - 1, coord.Y - 1] = null;
        }

        /// <summary>Moves a unit from one coordinate to another. Destination must be valid and empty.</summary>
        public void MoveUnit(GridCoord from, GridCoord to)
        {
            var unit = GetUnitAt(from);
            if (unit == null) return;

            if (!IsValidCoord(to))
                throw new System.ArgumentException($"Destination {to} is outside the grid");

            if (GetUnitAt(to) != null && to != from)
                throw new System.InvalidOperationException($"Destination {to} is already occupied");

            _grid[from.X - 1, from.Y - 1] = null;
            _grid[to.X - 1, to.Y - 1] = unit;
            unit.Position = to;
        }

        /// <summary>Returns the unit at the given coordinate, or null.</summary>
        public UnitInstance GetUnitAt(GridCoord coord)
        {
            if (!IsValidCoord(coord)) return null;
            return _grid[coord.X - 1, coord.Y - 1];
        }

        /// <summary>Returns true if the tile is empty and within bounds.</summary>
        public bool IsTileEmpty(GridCoord coord)
        {
            return IsValidCoord(coord) && GetUnitAt(coord) == null;
        }

        /// <summary>Returns all valid coordinates within Manhattan distance of center.</summary>
        public List<GridCoord> GetCoordsInRange(GridCoord center, int range)
        {
            var result = new List<GridCoord>();
            for (int x = 1; x <= _width; x++)
            {
                for (int y = 1; y <= _height; y++)
                {
                    var coord = new GridCoord(x, y);
                    if (center.ManhattanDistance(coord) <= range)
                        result.Add(coord);
                }
            }
            return result;
        }

        /// <summary>Returns the 4 orthogonal neighbors that are within grid bounds.</summary>
        public List<GridCoord> GetAdjacentCoords(GridCoord coord)
        {
            var result = new List<GridCoord>(4);
            var neighbors = coord.GetOrthogonalNeighbors();
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (IsValidCoord(neighbors[i]))
                    result.Add(neighbors[i]);
            }
            return result;
        }

        /// <summary>Returns all living units belonging to the given owner.</summary>
        public List<UnitInstance> GetAllUnits(Owner owner)
        {
            var result = new List<UnitInstance>();
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var unit = _grid[x, y];
                    if (unit != null && unit.IsAlive && unit.Owner == owner)
                        result.Add(unit);
                }
            }
            return result;
        }

        /// <summary>Returns all living units on the grid.</summary>
        public List<UnitInstance> GetAllLivingUnits()
        {
            var result = new List<UnitInstance>();
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    var unit = _grid[x, y];
                    if (unit != null && unit.IsAlive)
                        result.Add(unit);
                }
            }
            return result;
        }
    }
}
