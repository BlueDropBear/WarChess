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
    /// Supports deployment rounds (3 battles + bonus per ammunition spend)
    /// and fallback to ghost/house armies for low player counts.
    /// Pure C# — server-side logic. No Unity dependencies.
    /// </summary>
    public class ArmyPool
    {
        private readonly List<ArmySubmission> _pool;
        private readonly Dictionary<string, List<MatchRecord>> _matchHistory;
        private readonly GhostArmyPool _ghostPool;
        private readonly ChampionTitleSystem _championSystem;

        public ArmyPool() : this(new GhostArmyPool(), new ChampionTitleSystem()) { }

        public ArmyPool(GhostArmyPool ghostPool, ChampionTitleSystem championSystem)
        {
            _pool = new List<ArmySubmission>();
            _matchHistory = new Dictionary<string, List<MatchRecord>>();
            _ghostPool = ghostPool;
            _championSystem = championSystem;
        }

        /// <summary>The ghost army pool for fallback opponents.</summary>
        public GhostArmyPool GhostPool => _ghostPool;

        /// <summary>The champion title system.</summary>
        public ChampionTitleSystem ChampionSystem => _championSystem;

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

        /// <summary>
        /// Runs a full deployment round: 3 battles + optional bonus round.
        /// Spends 1 ammunition. Finds opponents from live pool, ghost pool, or house armies.
        /// Updates Elo sequentially after each battle.
        /// </summary>
        public DeploymentRound RunDeploymentRound(
            ArmySubmission playerArmy,
            PlayerProfile profile,
            GameConfigData config,
            bool championsOnly = false)
        {
            var round = new DeploymentRound
            {
                RoundId = Guid.NewGuid().ToString("N"),
                PlayerId = playerArmy.PlayerId,
                Tier = playerArmy.Tier,
                PlayerArmy = playerArmy,
                StartedAtTicks = DateTime.UtcNow.Ticks
            };

            var excludeIds = new HashSet<string> { playerArmy.SubmissionId, playerArmy.PlayerId };
            int currentElo = profile.GetElo(playerArmy.Tier);

            // Run standard battles
            int battleCount = config.DeploymentRoundBattleCount;
            for (int i = 0; i < battleCount; i++)
            {
                var (opponent, source) = FindBestOpponent(
                    playerArmy.Tier, currentElo, 200, excludeIds, championsOnly);

                if (opponent == null) break; // No opponents available at all

                excludeIds.Add(opponent.SubmissionId);

                var battle = ResolveSingleBattle(
                    playerArmy, opponent, source, config, currentElo, profile);

                round.Battles.Add(battle);
                currentElo = battle.PlayerEloAfter;
            }

            // Check for bonus round (need max stars from standard battles)
            int standardStars = 0;
            foreach (var b in round.Battles) standardStars += b.Stars;

            if (standardStars >= config.BonusRoundStarThreshold)
            {
                // Bonus round: highest Elo champion ghost or best available
                var bonusOpponent = _ghostPool.FindHighestEloChampion(
                    playerArmy.Tier, excludeIds);

                ArmySubmission bonusArmy;
                OpponentSource bonusSource;

                if (bonusOpponent != null)
                {
                    bonusArmy = bonusOpponent.Army;
                    bonusSource = OpponentSource.GhostArmy;
                }
                else
                {
                    // Fallback to highest Elo house army
                    var house = HouseArmyDatabase.FindClosestArmy(
                        playerArmy.Tier, 9999, excludeIds);
                    if (house != null)
                    {
                        bonusArmy = house.Submission;
                        bonusSource = OpponentSource.HouseArmy;
                    }
                    else
                    {
                        bonusArmy = null;
                        bonusSource = OpponentSource.HouseArmy;
                    }
                }

                if (bonusArmy != null)
                {
                    var bonusBattle = ResolveSingleBattle(
                        playerArmy, bonusArmy, bonusSource, config, currentElo, profile);

                    // Bonus round: only 0 or 1 star (win = 1, loss/draw = 0)
                    bonusBattle.Stars = bonusBattle.Result.Outcome == BattleOutcome.PlayerWin ? 1 : 0;

                    round.BonusBattle = bonusBattle;
                    currentElo = bonusBattle.PlayerEloAfter;
                }
            }

            // Submit player's army as a ghost candidate
            _ghostPool.SubmitGhost(playerArmy, playerArmy.PlayerId, currentElo, playerArmy.Tier);

            // Check if reranking is due
            if (_ghostPool.IsRerankDue(config.GhostPoolRerankInterval))
            {
                var rerankResult = _ghostPool.Rerank(
                    playerArmy.Tier,
                    config.GhostPoolChampionPercentile,
                    config.GhostPoolMinMatchesForRank,
                    config.GhostPoolEarlyMinWinRate,
                    config.GhostPoolMaxAgeDays);
                _championSystem.ProcessRerank(rerankResult);
            }

            round.CompletedAtTicks = DateTime.UtcNow.Ticks;
            round.CalculateTotals();
            return round;
        }

        /// <summary>
        /// Finds the best available opponent from live pool, ghost pool, or house armies.
        /// Priority: live → ghost → house.
        /// </summary>
        public (ArmySubmission opponent, OpponentSource source) FindBestOpponent(
            int tier, int playerElo, int eloRange, HashSet<string> excludeIds,
            bool championsOnly = false)
        {
            // 1. Try live pool
            if (!championsOnly)
            {
                ArmySubmission bestLive = null;
                int bestLiveEloDiff = int.MaxValue;

                foreach (var s in _pool)
                {
                    if (s.Status != SubmissionStatus.InPool) continue;
                    if (s.Tier != tier) continue;
                    if (excludeIds != null && (excludeIds.Contains(s.SubmissionId)
                        || excludeIds.Contains(s.PlayerId))) continue;

                    int diff = Math.Abs(playerElo - EstimateElo(s));
                    if (diff <= eloRange && diff < bestLiveEloDiff)
                    {
                        bestLiveEloDiff = diff;
                        bestLive = s;
                    }
                }

                if (bestLive != null)
                {
                    bestLive.Status = SubmissionStatus.Matched;
                    _pool.Remove(bestLive);
                    return (bestLive, OpponentSource.LivePlayer);
                }
            }

            // 2. Try ghost pool
            var ghost = _ghostPool.FindOpponent(tier, playerElo, eloRange, excludeIds, championsOnly);
            if (ghost != null)
                return (ghost.Army, OpponentSource.GhostArmy);

            // 3. Fall back to house army (skip if champions only)
            if (!championsOnly)
            {
                var house = HouseArmyDatabase.FindClosestArmy(tier, playerElo, excludeIds);
                if (house != null)
                    return (house.Submission, OpponentSource.HouseArmy);
            }

            return (null, OpponentSource.HouseArmy);
        }

        /// <summary>
        /// Resolves a single battle between the player's army and an opponent.
        /// Updates Elo and records match history.
        /// </summary>
        private RoundBattle ResolveSingleBattle(
            ArmySubmission playerArmy, ArmySubmission opponent,
            OpponentSource source, GameConfigData config,
            int playerElo, PlayerProfile profile)
        {
            int seed = GenerateDeterministicSeed(playerArmy.SubmissionId, opponent.SubmissionId);

            // Create units and grid
            UnitFactory.ResetIds();
            var grid = new GridMap(config.GridWidth, config.GridHeight);

            var unitsA = ArmySerializer.ToUnitInstances(playerArmy, Owner.Player);
            var unitsB = ArmySerializer.ToUnitInstances(opponent, Owner.Enemy);

            foreach (var u in unitsA)
                if (grid.IsTileEmpty(u.Position)) grid.PlaceUnit(u, u.Position);
            foreach (var u in unitsB)
                if (grid.IsTileEmpty(u.Position)) grid.PlaceUnit(u, u.Position);

            // Run deterministic battle
            var engine = new BattleEngine(grid, unitsA, unitsB, config, seed);
            var battleResult = engine.RunFullBattle();

            // Calculate stars using configurable thresholds
            int stars = BattleResultCalculator.CalculateStars(
                battleResult, unitsA.Count,
                config.StarThresholdFlawless, config.StarThresholdDecisive);

            // Calculate Elo change
            int opponentElo = EstimateElo(opponent);
            var matchResult = battleResult.Outcome switch
            {
                BattleOutcome.PlayerWin => MatchResult.PlayerAWins,
                BattleOutcome.EnemyWin => MatchResult.PlayerBWins,
                _ => MatchResult.Draw
            };
            var (newPlayerElo, _) = EloSystem.CalculateNewRatings(playerElo, opponentElo, matchResult);

            // Record match in profile
            bool won = battleResult.Outcome == BattleOutcome.PlayerWin;
            profile.RecordMatch(playerArmy.Tier, newPlayerElo, won);

            // Update ghost army win/loss record
            if (source == OpponentSource.GhostArmy)
                _ghostPool.RecordGhostResult(opponent.SubmissionId, !won);

            // Check if opponent is a champion ghost
            bool isChampion = false;
            if (source == OpponentSource.GhostArmy)
            {
                foreach (var g in _ghostPool.AllGhosts)
                {
                    if (g.Army.SubmissionId == opponent.SubmissionId && g.IsChampion)
                    {
                        isChampion = true;
                        break;
                    }
                }
            }

            string matchId = Guid.NewGuid().ToString("N");

            // Record in match history
            var resolveResult = new MatchResolveResult
            {
                MatchId = matchId,
                Tier = playerArmy.Tier,
                Seed = seed,
                Outcome = battleResult.Outcome,
                RoundsPlayed = battleResult.RoundsPlayed,
                PlayerAId = playerArmy.PlayerId,
                PlayerBId = opponent.PlayerId,
                PlayerAOldElo = playerElo,
                PlayerBOldElo = opponentElo,
                PlayerANewElo = newPlayerElo,
                PlayerBNewElo = opponentElo, // Ghost/house Elo doesn't change
                Events = battleResult.Events,
                ResolvedAtTicks = DateTime.UtcNow.Ticks
            };
            AddToHistory(playerArmy.PlayerId, resolveResult);

            return new RoundBattle
            {
                MatchId = matchId,
                Source = source,
                Opponent = opponent,
                Stars = stars,
                Result = battleResult,
                Seed = seed,
                IsChampionChallenge = isChampion,
                PlayerEloBefore = playerElo,
                PlayerEloAfter = newPlayerElo
            };
        }

        /// <summary>
        /// Estimates the Elo of an army submission. For ghost/house armies,
        /// uses stored Elo data. For live players, uses default rating.
        /// </summary>
        private int EstimateElo(ArmySubmission submission)
        {
            // House armies have target Elo stored in the database
            if (submission.PlayerId != null && submission.PlayerId.StartsWith(HouseArmyDatabase.HousePlayerIdPrefix))
            {
                var house = HouseArmyDatabase.FindClosestArmy(submission.Tier, 0);
                foreach (var a in HouseArmyDatabase.AllArmies)
                {
                    if (a.Submission.SubmissionId == submission.SubmissionId)
                        return a.TargetElo;
                }
            }

            // Ghost armies: check pool for stored Elo
            foreach (var g in _ghostPool.AllGhosts)
            {
                if (g.Army.SubmissionId == submission.SubmissionId)
                    return g.OwnerElo;
            }

            return EloSystem.GetDefaultRating();
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
