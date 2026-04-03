using System;
using System.Collections.Generic;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Player's multiplayer profile. Tracks per-tier Elo, wins, tier progress,
    /// and ammunition. Serializable for save/load and server storage.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string PlayerId;
        public string DisplayName;

        /// <summary>Elo rating per tier. Key = tier (1-5), Value = Elo.</summary>
        public Dictionary<int, int> EloPerTier;

        /// <summary>Wins per tier. Key = tier (1-5), Value = win count.</summary>
        public Dictionary<int, int> WinsPerTier;

        /// <summary>Losses per tier.</summary>
        public Dictionary<int, int> LossesPerTier;

        /// <summary>Highest tier unlocked.</summary>
        public int HighestTier;

        /// <summary>Ammunition balance.</summary>
        public AmmunitionSystem Ammunition;

        /// <summary>Total multiplayer matches played.</summary>
        public int TotalMatches;

        public PlayerProfile(string playerId, string displayName)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            EloPerTier = new Dictionary<int, int>();
            WinsPerTier = new Dictionary<int, int>();
            LossesPerTier = new Dictionary<int, int>();
            HighestTier = 1;
            Ammunition = new AmmunitionSystem();
            TotalMatches = 0;

            // Initialize tier 1 with default Elo
            EloPerTier[1] = EloSystem.GetDefaultRating();
            WinsPerTier[1] = 0;
            LossesPerTier[1] = 0;
        }

        /// <summary>Gets Elo for a specific tier (default if not yet active).</summary>
        public int GetElo(int tier)
        {
            return EloPerTier.TryGetValue(tier, out int elo) ? elo : EloSystem.GetDefaultRating();
        }

        /// <summary>Gets the rank name for a specific tier.</summary>
        public string GetRankName(int tier)
        {
            return EloSystem.GetRankName(GetElo(tier));
        }

        /// <summary>Gets total wins across all tiers.</summary>
        public int GetTotalWins()
        {
            int total = 0;
            foreach (var kv in WinsPerTier) total += kv.Value;
            return total;
        }

        /// <summary>
        /// Records a match result. Updates Elo, wins/losses, and checks tier promotion.
        /// Returns true if a tier promotion occurred.
        /// </summary>
        public bool RecordMatch(int tier, int newElo, bool won)
        {
            EloPerTier[tier] = newElo;
            TotalMatches++;

            if (won)
            {
                if (!WinsPerTier.ContainsKey(tier)) WinsPerTier[tier] = 0;
                WinsPerTier[tier]++;
                Ammunition.RewardWin();
            }
            else
            {
                if (!LossesPerTier.ContainsKey(tier)) LossesPerTier[tier] = 0;
                LossesPerTier[tier]++;
            }

            // Check tier promotion
            int newHighest = TierSystem.GetHighestUnlockedTier(WinsPerTier);
            if (newHighest > HighestTier)
            {
                HighestTier = newHighest;

                // Initialize new tier
                if (!EloPerTier.ContainsKey(newHighest))
                    EloPerTier[newHighest] = EloSystem.GetDefaultRating();
                if (!WinsPerTier.ContainsKey(newHighest))
                    WinsPerTier[newHighest] = 0;
                if (!LossesPerTier.ContainsKey(newHighest))
                    LossesPerTier[newHighest] = 0;

                Ammunition.RewardTierPromotion();
                return true;
            }

            return false;
        }
    }
}
