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
        /// Calculates new Elo ratings after a match.
        /// Returns (newRatingA, newRatingB).
        /// </summary>
        public static (int, int) CalculateNewRatings(int ratingA, int ratingB, MatchResult result)
        {
            double expectedA = 1.0 / (1.0 + Math.Pow(10, (ratingB - ratingA) / 400.0));
            double expectedB = 1.0 - expectedA;

            double scoreA, scoreB;
            switch (result)
            {
                case MatchResult.PlayerAWins:
                    scoreA = 1.0;
                    scoreB = 0.0;
                    break;
                case MatchResult.PlayerBWins:
                    scoreA = 0.0;
                    scoreB = 1.0;
                    break;
                default:
                    scoreA = 0.5;
                    scoreB = 0.5;
                    break;
            }

            int newA = ratingA + (int)Math.Round(KFactor * (scoreA - expectedA));
            int newB = ratingB + (int)Math.Round(KFactor * (scoreB - expectedB));

            // Floor at 0
            return (Math.Max(newA, 0), Math.Max(newB, 0));
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
