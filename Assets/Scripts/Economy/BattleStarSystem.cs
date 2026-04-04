using System;

namespace WarChess.Economy
{
    /// <summary>
    /// Battle Star progression currency system.
    /// Battle Stars are spent to unlock items in Field Manuals.
    /// Earned through: campaign battles, multiplayer wins, tier promotions,
    /// login streaks, and weekly challenges.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    [Serializable]
    public class BattleStarSystem
    {
        /// <summary>Current Battle Star balance.</summary>
        public int Balance { get; private set; }

        /// <summary>Total Battle Stars earned all-time.</summary>
        public int TotalEarned { get; private set; }

        /// <summary>Total Battle Stars spent all-time.</summary>
        public int TotalSpent { get; private set; }

        /// <summary>Whether a Battle Star Booster is active (2x earning).</summary>
        public bool BoosterActive { get; private set; }

        /// <summary>UTC ticks when the booster expires.</summary>
        public long BoosterExpiresTicks { get; private set; }

        // Earning rates per monetization strategy
        private const int PerCampaignBattle = 1;
        private const int PerMultiplayerWin = 1;
        private const int PerTierPromotion = 2;
        private const int PerLoginStreak3Days = 1;
        private const int PerWeeklyChallenge = 3;

        /// <summary>
        /// Creates the Battle Star system with an optional initial balance.
        /// </summary>
        public BattleStarSystem(int initialBalance = 0)
        {
            Balance = initialBalance;
        }

        /// <summary>
        /// Restores state from save data.
        /// </summary>
        public BattleStarSystem(int balance, int totalEarned, int totalSpent,
            bool boosterActive, long boosterExpiresTicks)
        {
            Balance = balance;
            TotalEarned = totalEarned;
            TotalSpent = totalSpent;
            BoosterActive = boosterActive;
            BoosterExpiresTicks = boosterExpiresTicks;
        }

        /// <summary>Awards Battle Stars for completing a campaign battle (1 Star, repeatable daily).</summary>
        public void RewardCampaignBattle()
        {
            Earn(PerCampaignBattle);
        }

        /// <summary>Awards Battle Stars for a multiplayer win (1 Star).</summary>
        public void RewardMultiplayerWin()
        {
            Earn(PerMultiplayerWin);
        }

        /// <summary>Awards Battle Stars for a tier promotion (2 Stars).</summary>
        public void RewardTierPromotion()
        {
            Earn(PerTierPromotion);
        }

        /// <summary>Awards Battle Stars for a 3-day login streak (1 Star).</summary>
        public void RewardLoginStreak()
        {
            Earn(PerLoginStreak3Days);
        }

        /// <summary>Awards Battle Stars for completing a weekly challenge (3 Stars).</summary>
        public void RewardWeeklyChallenge()
        {
            Earn(PerWeeklyChallenge);
        }

        /// <summary>
        /// Spends Battle Stars to unlock a Field Manual item.
        /// Returns false if insufficient balance.
        /// </summary>
        public bool Spend(int amount)
        {
            if (amount <= 0 || Balance < amount) return false;
            Balance -= amount;
            TotalSpent += amount;
            return true;
        }

        /// <summary>
        /// Returns true if the player can afford the given star cost.
        /// </summary>
        public bool CanAfford(int amount)
        {
            return amount > 0 && Balance >= amount;
        }

        /// <summary>
        /// Activates a Battle Star Booster for the specified duration.
        /// While active, all earnings are doubled.
        /// </summary>
        /// <param name="durationHours">Duration in hours (typically 24).</param>
        public void ActivateBooster(int durationHours = 24)
        {
            BoosterActive = true;
            BoosterExpiresTicks = DateTime.UtcNow.AddHours(durationHours).Ticks;
        }

        /// <summary>
        /// Checks and deactivates expired boosters. Call periodically.
        /// </summary>
        public void UpdateBooster()
        {
            if (BoosterActive && DateTime.UtcNow.Ticks >= BoosterExpiresTicks)
            {
                BoosterActive = false;
                BoosterExpiresTicks = 0;
            }
        }

        private void Earn(int baseAmount)
        {
            UpdateBooster();
            int amount = BoosterActive ? baseAmount * 2 : baseAmount;
            Balance += amount;
            TotalEarned += amount;
        }
    }
}
