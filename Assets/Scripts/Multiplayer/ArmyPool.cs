using System;
using System.Collections.Generic;
using WarChess.Battle;
using WarChess.Config;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Army pool matchmaking system from GDD Section 7.3.
    /// Players submit armies to a pool; the server pairs by tier and Elo,
    /// resolves battles deterministically, and notifies both players.
    /// Pure C# — server-side logic. No Unity dependencies.
    /// </summary>
    public class ArmyPool
    {
        private readonly List<ArmySubmission> _pool;
        private readonly Dictionary<string, List<MatchRecord>> _matchHistory;

        public ArmyPool()
        {
            _pool = new List<ArmySubmission>();
            _matchHistory = new Dictionary<string, List<MatchRecord>>();
        }

        /// <summary>
        /// Submits an army to the pool. Returns the submission ID.
        /// </summary>
        public string Submit(ArmySubmission submission)
        {
            submission.Status = SubmissionStatus.InPool;
            _pool.Add(submission);
            return submission.SubmissionId;
        }

        /// <summary>
        /// Withdraws an unmatched army from the pool. Returns true if successful.
        /// Ammunition should be refunded by the caller.
        /// </summary>
        public bool Withdraw(string submissionId)
        {
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                if (_pool[i].SubmissionId == submissionId && _pool[i].Status == SubmissionStatus.InPool)
                {
                    _pool[i].Status = SubmissionStatus.Withdrawn;
                    _pool.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Attempts to match armies in the pool. Returns a list of matches found.
        /// Matching criteria: same tier, Elo within range, different players.
        /// </summary>
        public List<PoolMatch> RunMatchmaking(
            Dictionary<string, Dictionary<int, int>> playerElos,
            int eloRange = 200)
        {
            var matches = new List<PoolMatch>();
            var matched = new HashSet<string>();

            // Sort pool by submission time for fairness
            _pool.Sort((a, b) => a.SubmittedAtTicks.CompareTo(b.SubmittedAtTicks));

            for (int i = 0; i < _pool.Count; i++)
            {
                var a = _pool[i];
                if (a.Status != SubmissionStatus.InPool || matched.Contains(a.SubmissionId))
                    continue;

                for (int j = i + 1; j < _pool.Count; j++)
                {
                    var b = _pool[j];
                    if (b.Status != SubmissionStatus.InPool || matched.Contains(b.SubmissionId))
                        continue;

                    // Must be same tier, different players
                    if (a.Tier != b.Tier || a.PlayerId == b.PlayerId)
                        continue;

                    // Check Elo range
                    int eloA = GetElo(playerElos, a.PlayerId, a.Tier);
                    int eloB = GetElo(playerElos, b.PlayerId, b.Tier);
                    if (Math.Abs(eloA - eloB) > eloRange)
                        continue;

                    // Match found
                    a.Status = SubmissionStatus.Matched;
                    b.Status = SubmissionStatus.Matched;
                    matched.Add(a.SubmissionId);
                    matched.Add(b.SubmissionId);

                    matches.Add(new PoolMatch
                    {
                        MatchId = Guid.NewGuid().ToString("N"),
                        SubmissionA = a,
                        SubmissionB = b,
                        Tier = a.Tier,
                        Seed = GenerateDeterministicSeed(a.SubmissionId, b.SubmissionId)
                    });

                    break;
                }
            }

            // Remove matched entries from pool
            _pool.RemoveAll(s => s.Status != SubmissionStatus.InPool);

            return matches;
        }

        /// <summary>
        /// Resolves a match using the deterministic battle engine.
        /// Returns a MatchResult with the outcome, replay events, and Elo changes.
        /// </summary>
        public MatchResolveResult ResolveMatch(
            PoolMatch match,
            GameConfigData config,
            Dictionary<string, Dictionary<int, int>> playerElos)
        {
            // Create units from submissions
            UnitFactory.ResetIds();
            var grid = new GridMap(config.GridWidth, config.GridHeight);

            var unitsA = ArmySerializer.ToUnitInstances(match.SubmissionA, Owner.Player);
            var unitsB = ArmySerializer.ToUnitInstances(match.SubmissionB, Owner.Enemy);

            // Place on grid
            foreach (var u in unitsA)
                if (grid.IsTileEmpty(u.Position)) grid.PlaceUnit(u, u.Position);
            foreach (var u in unitsB)
                if (grid.IsTileEmpty(u.Position)) grid.PlaceUnit(u, u.Position);

            // Run battle
            var engine = new BattleEngine(grid, unitsA, unitsB, config, match.Seed);
            var battleResult = engine.RunFullBattle();

            // Map battle outcome to match result
            var matchResult = battleResult.Outcome switch
            {
                BattleOutcome.PlayerWin => MatchResult.PlayerAWins,
                BattleOutcome.EnemyWin => MatchResult.PlayerBWins,
                _ => MatchResult.Draw
            };

            // Calculate Elo changes
            int eloA = GetElo(playerElos, match.SubmissionA.PlayerId, match.Tier);
            int eloB = GetElo(playerElos, match.SubmissionB.PlayerId, match.Tier);
            var (newEloA, newEloB) = EloSystem.CalculateNewRatings(eloA, eloB, matchResult);

            // Update statuses
            match.SubmissionA.Status = SubmissionStatus.Resolved;
            match.SubmissionB.Status = SubmissionStatus.Resolved;

            var resolveResult = new MatchResolveResult
            {
                MatchId = match.MatchId,
                Tier = match.Tier,
                Seed = match.Seed,
                Outcome = battleResult.Outcome,
                RoundsPlayed = battleResult.RoundsPlayed,
                PlayerAId = match.SubmissionA.PlayerId,
                PlayerBId = match.SubmissionB.PlayerId,
                PlayerAOldElo = eloA,
                PlayerBOldElo = eloB,
                PlayerANewElo = newEloA,
                PlayerBNewElo = newEloB,
                Events = battleResult.Events,
                ResolvedAtTicks = DateTime.UtcNow.Ticks
            };

            // Record in match history
            AddToHistory(match.SubmissionA.PlayerId, resolveResult);
            AddToHistory(match.SubmissionB.PlayerId, resolveResult);

            return resolveResult;
        }

        /// <summary>Returns the number of armies currently in the pool.</summary>
        public int PoolSize => _pool.Count;

        /// <summary>Returns active submissions for a player.</summary>
        public List<ArmySubmission> GetPlayerSubmissions(string playerId)
        {
            var result = new List<ArmySubmission>();
            foreach (var s in _pool)
                if (s.PlayerId == playerId && s.Status == SubmissionStatus.InPool)
                    result.Add(s);
            return result;
        }

        /// <summary>Returns match history for a player.</summary>
        public List<MatchRecord> GetMatchHistory(string playerId)
        {
            return _matchHistory.TryGetValue(playerId, out var history)
                ? history : new List<MatchRecord>();
        }

        private int GetElo(Dictionary<string, Dictionary<int, int>> playerElos,
            string playerId, int tier)
        {
            if (playerElos.TryGetValue(playerId, out var tiers))
                if (tiers.TryGetValue(tier, out int elo))
                    return elo;
            return EloSystem.GetDefaultRating();
        }

        /// <summary>
        /// Generates a deterministic seed from two submission IDs.
        /// Uses a stable string hash (FNV-1a) to avoid .NET's non-deterministic GetHashCode().
        /// </summary>
        private static int GenerateDeterministicSeed(string idA, string idB)
        {
            string combined = idA + ":" + idB;
            unchecked
            {
                // FNV-1a hash — deterministic across all .NET runtimes
                int hash = (int)2166136261;
                for (int i = 0; i < combined.Length; i++)
                {
                    hash ^= combined[i];
                    hash *= 16777619;
                }
                return hash;
            }
        }

        private void AddToHistory(string playerId, MatchResolveResult result)
        {
            if (!_matchHistory.ContainsKey(playerId))
                _matchHistory[playerId] = new List<MatchRecord>();

            _matchHistory[playerId].Add(new MatchRecord
            {
                MatchId = result.MatchId,
                Tier = result.Tier,
                Outcome = result.Outcome,
                OpponentId = playerId == result.PlayerAId ? result.PlayerBId : result.PlayerAId,
                EloChange = playerId == result.PlayerAId
                    ? result.PlayerANewElo - result.PlayerAOldElo
                    : result.PlayerBNewElo - result.PlayerBOldElo,
                ResolvedAtTicks = result.ResolvedAtTicks
            });
        }
    }

    [Serializable]
    public class PoolMatch
    {
        public string MatchId;
        public ArmySubmission SubmissionA;
        public ArmySubmission SubmissionB;
        public int Tier;
        public int Seed;
    }

    [Serializable]
    public class MatchResolveResult
    {
        public string MatchId;
        public int Tier;
        public int Seed;
        public BattleOutcome Outcome;
        public int RoundsPlayed;
        public string PlayerAId;
        public string PlayerBId;
        public int PlayerAOldElo;
        public int PlayerBOldElo;
        public int PlayerANewElo;
        public int PlayerBNewElo;
        public System.Collections.Generic.IReadOnlyList<BattleEvent> Events;
        public long ResolvedAtTicks;
    }

    [Serializable]
    public class MatchRecord
    {
        public string MatchId;
        public int Tier;
        public BattleOutcome Outcome;
        public string OpponentId;
        public int EloChange;
        public long ResolvedAtTicks;
    }
}
