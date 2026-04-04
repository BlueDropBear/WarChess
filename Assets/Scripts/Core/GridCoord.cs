using System;

namespace WarChess.Core
{
    /// <summary>
    /// Immutable grid coordinate on the 10x10 battlefield.
    /// X = column (1-10), Y = row (1 = player back, 10 = enemy back).
    /// </summary>
    public readonly struct GridCoord : IEquatable<GridCoord>
    {
        public readonly int X;
        public readonly int Y;

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Returns true if this coordinate is within the given grid bounds.
        /// Prefer GridMap.IsValidCoord() when a GridMap is available.
        /// </summary>
        public bool IsWithinBounds(int gridWidth, int gridHeight)
        {
            return X >= 1 && X <= gridWidth && Y >= 1 && Y <= gridHeight;
        }

        /// <summary>
        /// Manhattan distance to another coordinate.
        /// </summary>
        public int ManhattanDistance(GridCoord other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        /// <summary>
        /// Returns the 4 orthogonal neighbors (up, down, left, right).
        /// Does not filter for grid bounds — caller should check IsValid.
        /// </summary>
        public GridCoord[] GetOrthogonalNeighbors()
        {
            return new[]
            {
                new GridCoord(X, Y + 1),
                new GridCoord(X, Y - 1),
                new GridCoord(X + 1, Y),
                new GridCoord(X - 1, Y)
            };
        }

        /// <summary>
        /// Returns all 8 neighbors (orthogonal + diagonal).
        /// Does not filter for grid bounds — caller should check IsValid.
        /// </summary>
        public GridCoord[] GetAllNeighbors()
        {
            return new[]
            {
                new GridCoord(X, Y + 1),
                new GridCoord(X, Y - 1),
                new GridCoord(X + 1, Y),
                new GridCoord(X - 1, Y),
                new GridCoord(X + 1, Y + 1),
                new GridCoord(X + 1, Y - 1),
                new GridCoord(X - 1, Y + 1),
                new GridCoord(X - 1, Y - 1)
            };
        }

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => X * 31 + Y;
        public override string ToString() => $"({X},{Y})";

        public static bool operator ==(GridCoord left, GridCoord right) => left.Equals(right);
        public static bool operator !=(GridCoord left, GridCoord right) => !left.Equals(right);
    }
}
