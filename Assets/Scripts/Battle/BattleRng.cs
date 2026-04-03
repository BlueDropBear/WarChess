using System;

namespace WarChess.Battle
{
    /// <summary>
    /// Seeded random number generator for deterministic battle resolution.
    /// Wraps System.Random to ensure same seed always produces same sequence.
    /// </summary>
    public class BattleRng
    {
        private readonly Random _rng;

        /// <summary>
        /// Creates a new deterministic RNG with the given seed.
        /// </summary>
        public BattleRng(int seed)
        {
            _rng = new Random(seed);
        }

        /// <summary>
        /// Returns a random integer in [minInclusive, maxExclusive).
        /// </summary>
        public int Next(int minInclusive, int maxExclusive)
        {
            return _rng.Next(minInclusive, maxExclusive);
        }

        /// <summary>
        /// Returns a random integer in [0, maxExclusive).
        /// </summary>
        public int Next(int maxExclusive)
        {
            return _rng.Next(maxExclusive);
        }

        /// <summary>
        /// Returns a non-negative random integer.
        /// </summary>
        public int Next()
        {
            return _rng.Next();
        }
    }
}
