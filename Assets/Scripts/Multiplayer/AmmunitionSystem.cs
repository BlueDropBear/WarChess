using System;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Ammunition system from GDD Section 8.2.
    /// Ammo is spent to deploy armies into the multiplayer pool.
    /// Earned through daily login, wins, campaign clears, and tier promotions.
    /// Can be purchased with real money (IAP handled by platform layer).
    /// Pure C# — no Unity dependencies.
    /// </summary>
    [Serializable]
    public class AmmunitionSystem
    {
        /// <summary>Current ammunition balance.</summary>
        public int Balance { get; private set; }

        /// <summary>Total ammunition earned all-time (for analytics).</summary>
        public int TotalEarned { get; private set; }

        /// <summary>Total ammunition spent all-time.</summary>
        public int TotalSpent { get; private set; }

        /// <summary>Last daily login reward claim (UTC date as YYYYMMDD).</summary>
        public int LastDailyClaimDate { get; private set; }

        // GDD earning rates
        private const int DailyLoginReward = 3;
        private const int PerWinReward = 1;
        private const int PerCampaignBattleReward = 2;
        private const int TierPromotionBonus = 10;
        private const int DeployCost = 1;

        public AmmunitionSystem(int initialBalance = 5)
        {
            Balance = initialBalance;
        }

        /// <summary>
        /// Claims daily login reward (3 ammo). Returns false if already claimed today.
        /// </summary>
        public bool ClaimDailyReward()
        {
            int today = GetDateInt(DateTime.UtcNow);
            if (LastDailyClaimDate == today) return false;

            LastDailyClaimDate = today;
            Earn(DailyLoginReward, "Daily login");
            return true;
        }

        /// <summary>Awards ammunition for a multiplayer win.</summary>
        public void RewardWin()
        {
            Earn(PerWinReward, "Multiplayer win");
        }

        /// <summary>Awards ammunition for first-clearing a campaign battle.</summary>
        public void RewardCampaignClear()
        {
            Earn(PerCampaignBattleReward, "Campaign battle clear");
        }

        /// <summary>Awards bonus ammunition for a tier promotion.</summary>
        public void RewardTierPromotion()
        {
            Earn(TierPromotionBonus, "Tier promotion");
        }

        /// <summary>
        /// Spends 1 ammunition to deploy an army. Returns false if insufficient balance.
        /// </summary>
        public bool SpendForDeployment()
        {
            if (Balance < DeployCost) return false;
            Balance -= DeployCost;
            TotalSpent += DeployCost;
            return true;
        }

        /// <summary>
        /// Refunds 1 ammunition for a withdrawn army.
        /// </summary>
        public void RefundDeployment()
        {
            Balance += DeployCost;
            TotalSpent -= DeployCost;
        }

        /// <summary>
        /// Adds purchased ammunition (from IAP). Amount validated server-side.
        /// </summary>
        public void AddPurchased(int amount)
        {
            if (amount <= 0) return;
            Balance += amount;
            TotalEarned += amount;
        }

        /// <summary>
        /// Returns true if the player can afford to deploy an army.
        /// </summary>
        public bool CanDeploy => Balance >= DeployCost;

        private void Earn(int amount, string source)
        {
            Balance += amount;
            TotalEarned += amount;
        }

        private int GetDateInt(DateTime dt) => dt.Year * 10000 + dt.Month * 100 + dt.Day;
    }
}
