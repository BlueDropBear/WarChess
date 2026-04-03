using System;
using System.Collections.Generic;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Elo rating system from GDD Section 7.2.
    /// Each tier has independent Elo starting at 1000.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class EloSystem
    {
        private const int DefaultRating = 1000;
        private const int KFactor = 32;

        /// <summary>Elo rank thresholds from GDD Section 7.2.</summary>
        public static readonly EloRank[] Ranks = new[]
        {
            new EloRank { Name = "Recruit", MinElo = 0, Icon = "Bronze musket" },
            new EloRank { Name = "Corporal", MinElo = 1000, Icon = "Silver musket" },
            new EloRank { Name = "Sergeant", MinElo = 1200, Icon = "Bronze sword" },
            new EloRank { Name = "Lieutenant", MinElo = 1400, Icon = "Silver sword" },
            new EloRank { Name = "Captain", MinElo = 1600, Icon = "Bronze eagle" },
            new EloRank { Name = "Colonel", MinElo = 1800, Icon = "Silver eagle" },
            new EloRank { Name = "General", MinElo = 2000, Icon = "Gold eagle" },
            new EloRank { Name = "Grand Marshal", MinElo = 2200, Icon = "Napoleon's bicorne hat" },
        };

        /// <summary>
        /// Calculates new Elo ratings after a match using integer-only math
        /// for cross-platform determinism. Returns (newRatingA, newRatingB).
        /// Uses a lookup table approximation for the expected score formula.
        /// </summary>
        public static (int, int) CalculateNewRatings(int ratingA, int ratingB, MatchResult result)
        {
            // Expected score as base-1000 integer (0-1000 representing 0.0-1.0)
            int expectedA1000 = GetExpectedScore1000(ratingA - ratingB);
            int expectedB1000 = 1000 - expectedA1000;

            int scoreA1000, scoreB1000;
            switch (result)
            {
                case MatchResult.PlayerAWins:
                    scoreA1000 = 1000;
                    scoreB1000 = 0;
                    break;
                case MatchResult.PlayerBWins:
                    scoreA1000 = 0;
                    scoreB1000 = 1000;
                    break;
                default:
                    scoreA1000 = 500;
                    scoreB1000 = 500;
                    break;
            }

            // K * (score - expected) / 1000, with rounding via +500
            int deltaA = (KFactor * (scoreA1000 - expectedA1000) + 500) / 1000;
            int deltaB = (KFactor * (scoreB1000 - expectedB1000) + 500) / 1000;

            return (Math.Max(ratingA + deltaA, 0), Math.Max(ratingB + deltaB, 0));
        }

        /// <summary>
        /// Integer approximation of expected score: 1000 / (1 + 10^(-diff/400)).
        /// Uses a piecewise linear lookup for determinism. Returns 0-1000.
        /// </summary>
        private static int GetExpectedScore1000(int ratingDiff)
        {
            // Clamp to reasonable range
            if (ratingDiff >= 800) return 990;
            if (ratingDiff <= -800) return 10;

            // Lookup table: expected score * 1000 for rating differences at 50-point intervals
            // Computed from: 1000 / (1 + 10^(-diff/400))
            int[] table = {
                // diff: -800 to +800 in steps of 50
                10, 14, 20, 28, 39, 53, 71, 95, 124, 159, // -800 to -350
                200, 247, 299, 355, 414, 475, 500,          // -300 to 0
                525, 586, 645, 701, 753, 800,               // +50 to +300
                841, 876, 905, 929, 947, 961, 972, 980, 986, 990 // +350 to +800
            };

            // Map diff to table index: (-800 => 0, -750 => 1, ..., 0 => 16, ..., 800 => 32)
            int index = (ratingDiff + 800) / 50;
            int remainder = (ratingDiff + 800) % 50;

            // Handle negative remainder from integer division
            if (remainder < 0)
            {
                index--;
                remainder += 50;
            }

            index = Math.Max(0, Math.Min(index, table.Length - 2));

            // Linear interpolation between table entries
            int lo = table[index];
            int hi = table[Math.Min(index + 1, table.Length - 1)];
            return lo + ((hi - lo) * remainder) / 50;
        }

        /// <summary>
        /// Returns the rank name for a given Elo rating.
        /// </summary>
        public static string GetRankName(int elo)
        {
            string rank = Ranks[0].Name;
            for (int i = 0; i < Ranks.Length; i++)
            {
                if (elo >= Ranks[i].MinElo)
                    rank = Ranks[i].Name;
            }
            return rank;
        }

        /// <summary>
        /// Returns the full rank data for a given Elo rating.
        /// </summary>
        public static EloRank GetRank(int elo)
        {
            var result = Ranks[0];
            for (int i = 0; i < Ranks.Length; i++)
            {
                if (elo >= Ranks[i].MinElo)
                    result = Ranks[i];
            }
            return result;
        }

        /// <summary>
        /// Returns the default starting Elo.
        /// </summary>
        public static int GetDefaultRating() => DefaultRating;
    }

    public enum MatchResult
    {
        PlayerAWins,
        PlayerBWins,
        Draw
    }

    [Serializable]
    public class EloRank
    {
        public string Name;
        public int MinElo;
        public string Icon;
    }
}
