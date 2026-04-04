using System;
using System.Collections.Generic;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Stores snapshots of player armies for use as opponents when the live pool
    /// is thin. Only the top N% by win rate (configurable, default 10%) are marked
    /// as champions and used as ghost opponents.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class GhostArmyPool
    {
        private readonly List<GhostArmy> _ghosts;
        private int _matchesSinceLastRerank;

        /// <summary>Event fired when ghosts are promoted/demoted during reranking.</summary>
        public event Action<RerankResult> OnReranked;

        public GhostArmyPool()
        {
            _ghosts = new List<GhostArmy>();
            _matchesSinceLastRerank = 0;
        }

        /// <summary>Returns all ghost armies (for save/load).</summary>
        public IReadOnlyList<GhostArmy> AllGhosts => _ghosts;

        /// <summary>Returns ghost count for a specific tier.</summary>
        public int GetGhostCount(int tier)
        {
            int count = 0;
            foreach (var g in _ghosts)
                if (g.Tier == tier) count++;
            return count;
        }

        /// <summary>Returns champion count for a specific tier.</summary>
        public int GetChampionCount(int tier)
        {
            int count = 0;
            foreach (var g in _ghosts)
                if (g.Tier == tier && g.IsChampion) count++;
            return count;
        }

        /// <summary>
        /// Submits or updates a ghost army for a player at a tier.
        /// Limit: 1 ghost per player per tier (latest overwrites old).
        /// </summary>
        public void SubmitGhost(ArmySubmission army, string ownerId, int ownerElo, int tier)
        {
            // Remove existing ghost for this player at this tier
            for (int i = _ghosts.Count - 1; i >= 0; i--)
            {
                if (_ghosts[i].OwnerId == ownerId && _ghosts[i].Tier == tier)
                {
                    _ghosts.RemoveAt(i);
                    break;
                }
            }

            _ghosts.Add(new GhostArmy
            {
                Army = army,
                OwnerId = ownerId,
                OwnerElo = ownerElo,
                Tier = tier,
                WinCount = 0,
                LossCount = 0,
                IsChampion = false,
                AddedAtTicks = DateTime.UtcNow.Ticks,
                LastMatchTicks = DateTime.UtcNow.Ticks
            });
        }

        /// <summary>
        /// Records a match result for a ghost army. Updates win/loss counts.
        /// </summary>
        public void RecordGhostResult(string ghostSubmissionId, bool won)
        {
            for (int i = 0; i < _ghosts.Count; i++)
            {
                if (_ghosts[i].Army.SubmissionId == ghostSubmissionId)
                {
                    if (won)
                        _ghosts[i].WinCount++;
                    else
                        _ghosts[i].LossCount++;
                    _ghosts[i].LastMatchTicks = DateTime.UtcNow.Ticks;
                    break;
                }
            }
            _matchesSinceLastRerank++;
        }

        /// <summary>
        /// Checks if a rerank is due based on the configured interval.
        /// </summary>
        public bool IsRerankDue(int rerankInterval)
        {
            return _matchesSinceLastRerank >= rerankInterval;
        }

        /// <summary>
        /// Reranks ghosts for a tier. Top championPercentile% by win rate become champions.
        /// Ghosts with fewer than minMatchesForRank are not eligible.
        /// When fewer than 20 ghosts exist, uses earlyMinWinRate threshold instead.
        /// Returns lists of newly promoted and demoted ghosts.
        /// </summary>
        public RerankResult Rerank(int tier, int championPercentile, int minMatchesForRank,
            int earlyMinWinRate, int maxAgeDays)
        {
            _matchesSinceLastRerank = 0;

            // Retire stale ghosts first
            long maxAgeTicks = (long)maxAgeDays * TimeSpan.TicksPerDay;
            long now = DateTime.UtcNow.Ticks;
            _ghosts.RemoveAll(g => g.Tier == tier && (now - g.LastMatchTicks) > maxAgeTicks);

            // Get tier ghosts
            var tierGhosts = new List<GhostArmy>();
            foreach (var g in _ghosts)
                if (g.Tier == tier) tierGhosts.Add(g);

            var promoted = new List<GhostArmy>();
            var demoted = new List<GhostArmy>();

            if (tierGhosts.Count < 20)
            {
                // Early pool: use win rate threshold instead of percentile
                foreach (var g in tierGhosts)
                {
                    int totalMatches = g.WinCount + g.LossCount;
                    int winRate = totalMatches > 0 ? g.WinCount * 100 / totalMatches : 0;
                    bool shouldBeChampion = totalMatches >= minMatchesForRank
                        && winRate >= earlyMinWinRate;

                    if (shouldBeChampion && !g.IsChampion)
                    {
                        g.IsChampion = true;
                        promoted.Add(g);
                    }
                    else if (!shouldBeChampion && g.IsChampion)
                    {
                        g.IsChampion = false;
                        demoted.Add(g);
                    }
                }
            }
            else
            {
                // Normal pool: rank by win rate, top N% are champions
                var eligible = new List<GhostArmy>();
                foreach (var g in tierGhosts)
                {
                    if (g.WinCount + g.LossCount >= minMatchesForRank)
                        eligible.Add(g);
                }

                // Sort by win rate descending
                eligible.Sort((a, b) =>
                {
                    int wrA = a.WinCount * 100 / (a.WinCount + a.LossCount);
                    int wrB = b.WinCount * 100 / (b.WinCount + b.LossCount);
                    if (wrB != wrA) return wrB.CompareTo(wrA);
                    // Tiebreak: higher Elo first
                    return b.OwnerElo.CompareTo(a.OwnerElo);
                });

                // Top N% are champions (at least 1)
                int championCount = Math.Max(1, eligible.Count * championPercentile / 100);
                var championSet = new HashSet<string>();
                for (int i = 0; i < eligible.Count; i++)
                {
                    bool shouldBeChampion = i < championCount;
                    var g = eligible[i];

                    if (shouldBeChampion)
                        championSet.Add(g.Army.SubmissionId);

                    if (shouldBeChampion && !g.IsChampion)
                    {
                        g.IsChampion = true;
                        promoted.Add(g);
                    }
                    else if (!shouldBeChampion && g.IsChampion)
                    {
                        g.IsChampion = false;
                        demoted.Add(g);
                    }
                }

                // Also demote ineligible ghosts that were previously champion
                foreach (var g in tierGhosts)
                {
                    if (g.IsChampion && !championSet.Contains(g.Army.SubmissionId))
                    {
                        g.IsChampion = false;
                        demoted.Add(g);
                    }
                }
            }

            var result = new RerankResult { Promoted = promoted, Demoted = demoted };
            OnReranked?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Finds the best ghost opponent matching criteria.
        /// Returns null if no suitable ghost found.
        /// </summary>
        /// <param name="tier">Required tier.</param>
        /// <param name="playerElo">Player's current Elo for proximity matching.</param>
        /// <param name="eloRange">Maximum Elo difference.</param>
        /// <param name="excludeIds">Submission IDs and player IDs to exclude.</param>
        /// <param name="championsOnly">If true, only return champion ghosts.</param>
        public GhostArmy FindOpponent(int tier, int playerElo, int eloRange,
            HashSet<string> excludeIds, bool championsOnly = true)
        {
            GhostArmy best = null;
            int bestEloDiff = int.MaxValue;

            foreach (var g in _ghosts)
            {
                if (g.Tier != tier) continue;
                if (championsOnly && !g.IsChampion) continue;
                if (excludeIds != null && (excludeIds.Contains(g.Army.SubmissionId)
                    || excludeIds.Contains(g.OwnerId))) continue;

                int eloDiff = Math.Abs(playerElo - g.OwnerElo);
                if (eloDiff > eloRange) continue;

                if (eloDiff < bestEloDiff)
                {
                    bestEloDiff = eloDiff;
                    best = g;
                }
            }

            return best;
        }

        /// <summary>
        /// Finds the highest Elo champion ghost in a tier (for bonus rounds).
        /// Returns null if no champion found.
        /// </summary>
        public GhostArmy FindHighestEloChampion(int tier, HashSet<string> excludeIds)
        {
            GhostArmy best = null;
            int bestElo = -1;

            foreach (var g in _ghosts)
            {
                if (g.Tier != tier || !g.IsChampion) continue;
                if (excludeIds != null && (excludeIds.Contains(g.Army.SubmissionId)
                    || excludeIds.Contains(g.OwnerId))) continue;

                if (g.OwnerElo > bestElo)
                {
                    bestElo = g.OwnerElo;
                    best = g;
                }
            }

            return best;
        }

        /// <summary>
        /// Loads ghost armies from save data.
        /// </summary>
        public void LoadGhosts(List<GhostArmy> ghosts)
        {
            _ghosts.Clear();
            if (ghosts != null)
                _ghosts.AddRange(ghosts);
        }
    }

    /// <summary>
    /// A snapshot of a player's army stored in the ghost pool.
    /// </summary>
    [Serializable]
    public class GhostArmy
    {
        /// <summary>The army composition snapshot.</summary>
        public ArmySubmission Army;

        /// <summary>Player ID of the army's creator.</summary>
        public string OwnerId;

        /// <summary>Owner's Elo at the time the ghost was created.</summary>
        public int OwnerElo;

        /// <summary>Tier this ghost competes in.</summary>
        public int Tier;

        /// <summary>Number of battles this ghost has won.</summary>
        public int WinCount;

        /// <summary>Number of battles this ghost has lost.</summary>
        public int LossCount;

        /// <summary>Whether this ghost is currently in the top champion percentile.</summary>
        public bool IsChampion;

        /// <summary>When this ghost was added (UTC ticks).</summary>
        public long AddedAtTicks;

        /// <summary>When this ghost last participated in a match (UTC ticks).</summary>
        public long LastMatchTicks;

        /// <summary>Calculated win rate (0-100). Returns 0 if no matches played.</summary>
        public int WinRate => (WinCount + LossCount) > 0
            ? WinCount * 100 / (WinCount + LossCount)
            : 0;
    }

    /// <summary>
    /// Result of a ghost pool reranking operation.
    /// </summary>
    public class RerankResult
    {
        /// <summary>Ghosts newly promoted to champion status.</summary>
        public List<GhostArmy> Promoted;

        /// <summary>Ghosts demoted from champion status.</summary>
        public List<GhostArmy> Demoted;
    }
}
