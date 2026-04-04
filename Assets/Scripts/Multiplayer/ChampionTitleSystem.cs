using System;
using System.Collections.Generic;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Champion title hierarchy. Once a player earns any champion title,
    /// they never go back to None — they become FormerChampion instead.
    /// </summary>
    public enum ChampionTitle
    {
        /// <summary>No ghost army in pool or below top percentile.</summary>
        None,

        /// <summary>Had an army in the top percentile, since fallen out. Permanent.</summary>
        FormerChampion,

        /// <summary>Currently has an army in the top percentile of a single tier.</summary>
        TierChampion,

        /// <summary>Top percentile in 2+ tiers simultaneously.</summary>
        MultiTierChampion,

        /// <summary>Top percentile in all 5 tiers simultaneously (extremely rare).</summary>
        GrandChampion
    }

    /// <summary>
    /// Tracks a player's champion status across all tiers.
    /// </summary>
    [Serializable]
    public class ChampionStatus
    {
        /// <summary>Player ID.</summary>
        public string PlayerId;

        /// <summary>Current active title.</summary>
        public ChampionTitle ActiveTitle;

        /// <summary>Tiers where the player currently has a top-percentile ghost.</summary>
        public List<int> ActiveChampionTiersList;

        /// <summary>Tiers where the player previously had a top-percentile ghost.</summary>
        public List<int> FormerChampionTiersList;

        /// <summary>When the current title was first earned (UTC ticks). 0 if None.</summary>
        public long TitleEarnedAtTicks;

        /// <summary>When the player was last demoted from an active title (UTC ticks). 0 if never.</summary>
        public long TitleLostAtTicks;

        /// <summary>Lifetime days spent as any champion tier. Integer accumulator.</summary>
        public int TotalDaysAsChampion;

        public ChampionStatus(string playerId)
        {
            PlayerId = playerId;
            ActiveTitle = ChampionTitle.None;
            ActiveChampionTiersList = new List<int>();
            FormerChampionTiersList = new List<int>();
            TitleEarnedAtTicks = 0;
            TitleLostAtTicks = 0;
            TotalDaysAsChampion = 0;
        }

        /// <summary>Whether this player has ever been a champion.</summary>
        public bool HasEverBeenChampion => FormerChampionTiersList.Count > 0
            || ActiveChampionTiersList.Count > 0;
    }

    /// <summary>
    /// Manages champion titles based on ghost army pool rankings.
    /// Listens to GhostArmyPool rerank events and updates player titles.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class ChampionTitleSystem
    {
        private readonly Dictionary<string, ChampionStatus> _statuses;

        /// <summary>Fired when a player's champion title changes.</summary>
        public event Action<string, ChampionTitle, ChampionTitle> OnTitleChanged;

        public ChampionTitleSystem()
        {
            _statuses = new Dictionary<string, ChampionStatus>();
        }

        /// <summary>Gets or creates the champion status for a player.</summary>
        public ChampionStatus GetStatus(string playerId)
        {
            if (!_statuses.TryGetValue(playerId, out var status))
            {
                status = new ChampionStatus(playerId);
                _statuses[playerId] = status;
            }
            return status;
        }

        /// <summary>
        /// Processes a rerank result, updating champion titles for affected players.
        /// Call this after GhostArmyPool.Rerank() for each tier.
        /// </summary>
        public void ProcessRerank(RerankResult result)
        {
            if (result == null) return;

            // Process promotions
            if (result.Promoted != null)
            {
                foreach (var ghost in result.Promoted)
                    PromotePlayer(ghost.OwnerId, ghost.Tier);
            }

            // Process demotions
            if (result.Demoted != null)
            {
                foreach (var ghost in result.Demoted)
                    DemotePlayer(ghost.OwnerId, ghost.Tier);
            }
        }

        /// <summary>Returns all tracked champion statuses (for save/load).</summary>
        public IReadOnlyDictionary<string, ChampionStatus> AllStatuses => _statuses;

        /// <summary>Loads champion statuses from save data.</summary>
        public void LoadStatuses(Dictionary<string, ChampionStatus> statuses)
        {
            _statuses.Clear();
            if (statuses != null)
            {
                foreach (var kvp in statuses)
                    _statuses[kvp.Key] = kvp.Value;
            }
        }

        private void PromotePlayer(string playerId, int tier)
        {
            var status = GetStatus(playerId);
            var oldTitle = status.ActiveTitle;

            if (!status.ActiveChampionTiersList.Contains(tier))
                status.ActiveChampionTiersList.Add(tier);

            // Track former tiers for legacy
            if (!status.FormerChampionTiersList.Contains(tier))
                status.FormerChampionTiersList.Add(tier);

            status.ActiveTitle = CalculateTitle(status.ActiveChampionTiersList.Count);

            if (status.TitleEarnedAtTicks == 0)
                status.TitleEarnedAtTicks = DateTime.UtcNow.Ticks;

            if (status.ActiveTitle != oldTitle)
                OnTitleChanged?.Invoke(playerId, oldTitle, status.ActiveTitle);
        }

        private void DemotePlayer(string playerId, int tier)
        {
            var status = GetStatus(playerId);
            var oldTitle = status.ActiveTitle;

            status.ActiveChampionTiersList.Remove(tier);

            if (status.ActiveChampionTiersList.Count > 0)
            {
                status.ActiveTitle = CalculateTitle(status.ActiveChampionTiersList.Count);
            }
            else
            {
                // No active champion tiers — demote to FormerChampion (never None)
                if (status.HasEverBeenChampion)
                {
                    status.ActiveTitle = ChampionTitle.FormerChampion;
                    status.TitleLostAtTicks = DateTime.UtcNow.Ticks;
                }
                else
                {
                    status.ActiveTitle = ChampionTitle.None;
                }
            }

            if (status.ActiveTitle != oldTitle)
                OnTitleChanged?.Invoke(playerId, oldTitle, status.ActiveTitle);
        }

        private static ChampionTitle CalculateTitle(int activeChampionTierCount)
        {
            return activeChampionTierCount switch
            {
                >= 5 => ChampionTitle.GrandChampion,
                >= 2 => ChampionTitle.MultiTierChampion,
                1 => ChampionTitle.TierChampion,
                _ => ChampionTitle.None
            };
        }
    }
}
