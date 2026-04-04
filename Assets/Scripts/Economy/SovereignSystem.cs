using System;

namespace WarChess.Economy
{
    /// <summary>
    /// Sovereign premium currency system.
    /// Sovereigns are the cosmetic-economy currency, completely separate from Ammunition.
    /// Used for: Field Manual premium tracks, Quartermaster's Shop purchases.
    /// Earned through: campaign clears, tier promotions, login streaks, Dispatch Box drops.
    /// Can be purchased with real money (IAP handled by platform layer).
    /// Pure C# — no Unity dependencies.
    /// </summary>
    [Serializable]
    public class SovereignSystem
    {
        /// <summary>Current sovereign balance.</summary>
        public int Balance { get; private set; }

        /// <summary>Total sovereigns earned all-time (for analytics).</summary>
        public int TotalEarned { get; private set; }

        /// <summary>Total sovereigns spent all-time.</summary>
        public int TotalSpent { get; private set; }

        /// <summary>Consecutive login days tracked for weekly bonus.</summary>
        public int ConsecutiveLoginDays { get; private set; }

        /// <summary>Last login date for streak tracking (UTC date as YYYYMMDD).</summary>
        public int LastLoginDate { get; private set; }

        // Earning rates per monetization strategy
        private const int PerCampaignBattleFirstClear = 5;
        private const int PerTierPromotion = 2;
        private const int PerActFullStars = 10;
        private const int Day7LoginBonus = 5;

        /// <summary>
        /// Creates the sovereign system with an optional initial balance.
        /// </summary>
        public SovereignSystem(int initialBalance = 0)
        {
            Balance = initialBalance;
        }

        /// <summary>
        /// Restores state from save data.
        /// </summary>
        public SovereignSystem(int balance, int totalEarned, int totalSpent,
            int consecutiveLoginDays, int lastLoginDate)
        {
            Balance = balance;
            TotalEarned = totalEarned;
            TotalSpent = totalSpent;
            ConsecutiveLoginDays = consecutiveLoginDays;
            LastLoginDate = lastLoginDate;
        }

        /// <summary>
        /// Awards sovereigns for first-clearing a campaign battle (5 per battle).
        /// </summary>
        public void RewardCampaignFirstClear()
        {
            Earn(PerCampaignBattleFirstClear);
        }

        /// <summary>
        /// Awards sovereigns for a multiplayer tier promotion (2 per tier).
        /// </summary>
        public void RewardTierPromotion()
        {
            Earn(PerTierPromotion);
        }

        /// <summary>
        /// Awards sovereigns for 3-starring all battles in a campaign act (10 per act).
        /// </summary>
        public void RewardActFullStars()
        {
            Earn(PerActFullStars);
        }

        /// <summary>
        /// Awards sovereigns from a Dispatch Box drop.
        /// </summary>
        /// <param name="amount">Amount dropped (typically 1-3).</param>
        public void RewardDispatchBoxDrop(int amount)
        {
            if (amount > 0) Earn(amount);
        }

        /// <summary>
        /// Tracks daily login for the 7-day streak bonus.
        /// Awards 5 Sovereigns on day 7 of consecutive login.
        /// Returns the number of sovereigns awarded (0 or 5).
        /// </summary>
        public int TrackDailyLogin()
        {
            int today = GetDateInt(DateTime.UtcNow);
            if (LastLoginDate == today) return 0;

            int yesterday = GetDateInt(DateTime.UtcNow.AddDays(-1));
            if (LastLoginDate == yesterday)
            {
                ConsecutiveLoginDays++;
            }
            else
            {
                ConsecutiveLoginDays = 1;
            }

            LastLoginDate = today;

            if (ConsecutiveLoginDays >= 7)
            {
                ConsecutiveLoginDays = 0;
                Earn(Day7LoginBonus);
                return Day7LoginBonus;
            }

            return 0;
        }

        /// <summary>
        /// Spends sovereigns. Returns false if insufficient balance.
        /// </summary>
        public bool Spend(int amount)
        {
            if (amount <= 0 || Balance < amount) return false;
            Balance -= amount;
            TotalSpent += amount;
            return true;
        }

        /// <summary>
        /// Returns true if the player can afford the given amount.
        /// </summary>
        public bool CanAfford(int amount)
        {
            return amount > 0 && Balance >= amount;
        }

        /// <summary>
        /// Adds purchased sovereigns (from IAP). Amount validated server-side.
        /// </summary>
        public void AddPurchased(int amount)
        {
            if (amount <= 0) return;
            Balance += amount;
            TotalEarned += amount;
        }

        /// <summary>
        /// Adds sovereigns earned through Field Manual premium track rewards.
        /// </summary>
        public void AddFieldManualReward(int amount)
        {
            if (amount > 0) Earn(amount);
        }

        private void Earn(int amount)
        {
            Balance += amount;
            TotalEarned += amount;
        }

        private static int GetDateInt(DateTime dt) => dt.Year * 10000 + dt.Month * 100 + dt.Day;
    }
}
