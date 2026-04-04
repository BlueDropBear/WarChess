using System;
using System.Collections.Generic;
using WarChess.Economy;
using WarChess.Multiplayer;

namespace WarChess.Config
{
    /// <summary>
    /// Tracks progress and purchases for a single Field Manual.
    /// </summary>
    [Serializable]
    public class FieldManualProgress
    {
        /// <summary>Field Manual ID.</summary>
        public string ManualId;

        /// <summary>Whether the premium track has been purchased.</summary>
        public bool PremiumUnlocked;

        /// <summary>
        /// Claimed reward indices per page. Key = page index (0-based), Value = list of reward indices claimed.
        /// </summary>
        public Dictionary<int, List<int>> ClaimedRewards;

        public FieldManualProgress(string manualId)
        {
            ManualId = manualId;
            PremiumUnlocked = false;
            ClaimedRewards = new Dictionary<int, List<int>>();
        }

        /// <summary>Returns true if a specific reward has been claimed.</summary>
        public bool IsRewardClaimed(int pageIndex, int rewardIndex)
        {
            if (!ClaimedRewards.TryGetValue(pageIndex, out var list)) return false;
            return list.Contains(rewardIndex);
        }

        /// <summary>Marks a reward as claimed.</summary>
        public void MarkClaimed(int pageIndex, int rewardIndex)
        {
            if (!ClaimedRewards.ContainsKey(pageIndex))
                ClaimedRewards[pageIndex] = new List<int>();
            if (!ClaimedRewards[pageIndex].Contains(rewardIndex))
                ClaimedRewards[pageIndex].Add(rewardIndex);
        }
    }

    /// <summary>
    /// Runtime manager for the Field Manual system.
    /// Handles premium track purchases, Battle Star spending, and reward claiming.
    /// Delivers rewards to the appropriate subsystems (CosmeticShop, AmmunitionSystem, etc.).
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class FieldManualSystem
    {
        private readonly Dictionary<string, FieldManualProgress> _progress;
        private readonly SovereignSystem _sovereigns;
        private readonly BattleStarSystem _battleStars;
        private readonly AmmunitionSystem _ammo;
        private readonly CosmeticShop _cosmeticShop;
        private readonly DispatchBoxSystem _dispatchBoxSystem;
        private readonly AnalyticsManager _analytics;

        /// <summary>Event fired when a Field Manual premium track is purchased.</summary>
        public event Action<string> OnManualPurchased;

        /// <summary>Event fired when a reward is claimed.</summary>
        public event Action<string, int, int> OnRewardClaimed;

        /// <summary>
        /// Creates the Field Manual system with references to all dependent systems.
        /// </summary>
        public FieldManualSystem(
            SovereignSystem sovereigns,
            BattleStarSystem battleStars,
            AmmunitionSystem ammo,
            CosmeticShop cosmeticShop,
            DispatchBoxSystem dispatchBoxSystem,
            AnalyticsManager analytics,
            Dictionary<string, FieldManualProgress> existingProgress = null)
        {
            _sovereigns = sovereigns;
            _battleStars = battleStars;
            _ammo = ammo;
            _cosmeticShop = cosmeticShop;
            _dispatchBoxSystem = dispatchBoxSystem;
            _analytics = analytics;
            _progress = existingProgress ?? new Dictionary<string, FieldManualProgress>();
        }

        /// <summary>Returns all Field Manual progress for save/load.</summary>
        public IReadOnlyDictionary<string, FieldManualProgress> AllProgress => _progress;

        /// <summary>
        /// Returns the progress for a specific Field Manual, creating it if needed.
        /// </summary>
        public FieldManualProgress GetProgress(string manualId)
        {
            if (!_progress.TryGetValue(manualId, out var progress))
            {
                progress = new FieldManualProgress(manualId);
                _progress[manualId] = progress;
            }
            return progress;
        }

        /// <summary>
        /// Purchases the premium track for a Field Manual.
        /// Costs Sovereigns. Returns false if already purchased, manual not found, or insufficient funds.
        /// </summary>
        public bool PurchasePremiumTrack(string manualId)
        {
            var manual = FieldManualDatabase.Get(manualId);
            if (manual == null) return false;
            if (manual.IsFree) return false; // Free manuals don't need purchasing

            var progress = GetProgress(manualId);
            if (progress.PremiumUnlocked) return false;

            if (!_sovereigns.Spend(manual.PremiumCostSovereigns)) return false;

            progress.PremiumUnlocked = true;

            _analytics?.LogEvent(AnalyticsEventType.FieldManualPurchased,
                new Dictionary<string, string>
                {
                    { "manual_id", manualId },
                    { "cost_sovereigns", manual.PremiumCostSovereigns.ToString() }
                });

            OnManualPurchased?.Invoke(manualId);
            return true;
        }

        /// <summary>
        /// Returns true if a reward can be claimed (stars available, prerequisites met, not already claimed).
        /// </summary>
        public bool CanClaimReward(string manualId, int pageIndex, int rewardIndex)
        {
            var manual = FieldManualDatabase.Get(manualId);
            if (manual == null || pageIndex < 0 || pageIndex >= manual.Pages.Count) return false;

            var page = manual.Pages[pageIndex];
            if (rewardIndex < 0 || rewardIndex >= page.Rewards.Count) return false;

            var reward = page.Rewards[rewardIndex];
            var progress = GetProgress(manualId);

            // Already claimed?
            if (progress.IsRewardClaimed(pageIndex, rewardIndex)) return false;

            // Premium track check
            if (reward.IsPremiumTrack && !progress.PremiumUnlocked && !manual.IsFree) return false;

            // Must claim previous rewards on this page first (sequential unlock)
            for (int i = 0; i < rewardIndex; i++)
            {
                var prevReward = page.Rewards[i];
                // Only check same-track prerequisites
                if (prevReward.IsPremiumTrack == reward.IsPremiumTrack && !progress.IsRewardClaimed(pageIndex, i))
                    return false;
            }

            // Must complete previous page's same-track rewards
            if (pageIndex > 0)
            {
                var prevPage = manual.Pages[pageIndex - 1];
                for (int i = 0; i < prevPage.Rewards.Count; i++)
                {
                    var prevReward = prevPage.Rewards[i];
                    if (prevReward.IsPremiumTrack == reward.IsPremiumTrack && !progress.IsRewardClaimed(pageIndex - 1, i))
                        return false;
                }
            }

            // Can afford stars?
            if (!_battleStars.CanAfford(reward.StarCost)) return false;

            return true;
        }

        /// <summary>
        /// Claims a reward from a Field Manual. Spends Battle Stars and delivers the reward.
        /// Returns false if the reward cannot be claimed.
        /// </summary>
        public bool ClaimReward(string manualId, int pageIndex, int rewardIndex)
        {
            if (!CanClaimReward(manualId, pageIndex, rewardIndex)) return false;

            var manual = FieldManualDatabase.Get(manualId);
            var page = manual.Pages[pageIndex];
            var reward = page.Rewards[rewardIndex];
            var progress = GetProgress(manualId);

            // Spend stars
            if (!_battleStars.Spend(reward.StarCost)) return false;

            // Deliver reward
            DeliverReward(reward);

            // Mark claimed
            progress.MarkClaimed(pageIndex, rewardIndex);

            _analytics?.LogEvent(AnalyticsEventType.FieldManualRewardClaimed,
                new Dictionary<string, string>
                {
                    { "manual_id", manualId },
                    { "page", pageIndex.ToString() },
                    { "reward", rewardIndex.ToString() },
                    { "reward_type", reward.Type.ToString() },
                    { "is_premium", reward.IsPremiumTrack.ToString() },
                    { "star_cost", reward.StarCost.ToString() }
                });

            OnRewardClaimed?.Invoke(manualId, pageIndex, rewardIndex);
            return true;
        }

        /// <summary>
        /// Returns the total number of rewards claimed across all manuals.
        /// </summary>
        public int GetTotalRewardsClaimed()
        {
            int total = 0;
            foreach (var kvp in _progress)
            {
                foreach (var pageKvp in kvp.Value.ClaimedRewards)
                    total += pageKvp.Value.Count;
            }
            return total;
        }

        /// <summary>
        /// Returns the completion percentage (0-100) for a specific Field Manual.
        /// </summary>
        public int GetCompletionPercent(string manualId)
        {
            var manual = FieldManualDatabase.Get(manualId);
            if (manual == null) return 0;

            int totalRewards = 0;
            int claimedRewards = 0;
            var progress = GetProgress(manualId);

            for (int p = 0; p < manual.Pages.Count; p++)
            {
                var page = manual.Pages[p];
                for (int r = 0; r < page.Rewards.Count; r++)
                {
                    totalRewards++;
                    if (progress.IsRewardClaimed(p, r))
                        claimedRewards++;
                }
            }

            return totalRewards == 0 ? 0 : (claimedRewards * 100) / totalRewards;
        }

        private void DeliverReward(FieldManualReward reward)
        {
            switch (reward.Type)
            {
                case FieldManualRewardType.Cosmetic:
                    _cosmeticShop?.GrantCosmetic(reward.CosmeticId);
                    break;

                case FieldManualRewardType.Ammunition:
                    _ammo?.AddPurchased(reward.Amount);
                    break;

                case FieldManualRewardType.Sovereigns:
                    _sovereigns?.AddFieldManualReward(reward.Amount);
                    break;

                case FieldManualRewardType.DispatchBox:
                    _dispatchBoxSystem?.AwardBox(reward.BoxType);
                    break;

                case FieldManualRewardType.BattleStarBooster:
                    _battleStars?.ActivateBooster(reward.Amount);
                    break;
            }
        }
    }
}
