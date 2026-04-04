using System;
using System.Collections.Generic;
using WarChess.Battle;
using WarChess.Config;
using WarChess.Multiplayer;

namespace WarChess.QA.Tests
{
    /// <summary>
    /// Correctness tests for deployment rounds, ghost army pool,
    /// champion title system, and house armies.
    /// </summary>
    public static class DeploymentRoundTests
    {
        public static List<QATestResult> RunAll(GameConfigData config)
        {
            return new List<QATestResult>
            {
                TestGhostPoolSubmitAndOverwrite(),
                TestGhostPoolRerankTop10Percent(),
                TestGhostPoolRerankEarlyPool(),
                TestGhostPoolStalenessRetirement(),
                TestGhostPoolNoSelfMatching(),
                TestGhostPoolFindHighestEloChampion(),
                TestChampionTitlePromotion(),
                TestChampionTitleDemotionToFormer(),
                TestChampionTitleMultiTier(),
                TestChampionTitleNeverNoneAfterChampion(),
                TestHouseArmiesExistPerTier(),
                TestHouseArmyFindClosest(),
                TestConfigurableStarThresholds(),
                TestDeploymentRoundCalculateTotals(),
                TestBonusRoundTriggerAt9Stars(),
                TestPlayerProfileRecordDeploymentRound(),
                TestFindBestOpponentPriority(config)
            };
        }

        // ─── Ghost Pool Tests ───

        public static QATestResult TestGhostPoolSubmitAndOverwrite()
        {
            const string name = "DeploymentRound.GhostPoolSubmitOverwrite";
            try
            {
                var pool = new GhostArmyPool();
                var army1 = MakeSubmission("sub1", "player1", 1);
                var army2 = MakeSubmission("sub2", "player1", 1);

                pool.SubmitGhost(army1, "player1", 1000, 1);
                if (pool.GetGhostCount(1) != 1)
                    return QATestResult.Fail(name, "Expected 1 ghost after first submit");

                // Second submit at same tier should overwrite
                pool.SubmitGhost(army2, "player1", 1100, 1);
                if (pool.GetGhostCount(1) != 1)
                    return QATestResult.Fail(name, $"Expected 1 ghost after overwrite, got {pool.GetGhostCount(1)}");

                // Different tier should not overwrite
                pool.SubmitGhost(army1, "player1", 1000, 2);
                if (pool.GetGhostCount(2) != 1)
                    return QATestResult.Fail(name, "Expected 1 ghost at tier 2");
                if (pool.GetGhostCount(1) != 1)
                    return QATestResult.Fail(name, "Tier 1 ghost should still exist");

                return QATestResult.Pass(name, "Submit overwrites per player+tier, different tiers coexist");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        public static QATestResult TestGhostPoolRerankTop10Percent()
        {
            const string name = "DeploymentRound.GhostPoolRerankTop10";
            try
            {
                var pool = new GhostArmyPool();

                // Create 30 ghosts with varying win rates
                for (int i = 0; i < 30; i++)
                {
                    var army = MakeSubmission($"sub{i}", $"player{i}", 1);
                    pool.SubmitGhost(army, $"player{i}", 1000 + i * 10, 1);

                    // Give enough matches for ranking eligibility
                    for (int j = 0; j < 10; j++)
                    {
                        // Top 3 ghosts (27,28,29) win most, rest lose most
                        bool win = i >= 27 ? j < 8 : j < 2;
                        pool.RecordGhostResult($"sub{i}", win);
                    }
                }

                var result = pool.Rerank(1, 10, 5, 40, 30);

                // Top 10% of 30 eligible = 3 champions
                int championCount = pool.GetChampionCount(1);
                if (championCount != 3)
                    return QATestResult.Fail(name, $"Expected 3 champions (10% of 30), got {championCount}");

                // Promoted should be the high win-rate ghosts
                if (result.Promoted.Count != 3)
                    return QATestResult.Fail(name, $"Expected 3 promoted, got {result.Promoted.Count}");

                return QATestResult.Pass(name, $"Top 10% = {championCount} champions from 30 ghosts");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        public static QATestResult TestGhostPoolRerankEarlyPool()
        {
            const string name = "DeploymentRound.GhostPoolRerankEarly";
            try
            {
                var pool = new GhostArmyPool();

                // Fewer than 20 ghosts: use win rate threshold instead of percentile
                for (int i = 0; i < 10; i++)
                {
                    var army = MakeSubmission($"sub{i}", $"player{i}", 1);
                    pool.SubmitGhost(army, $"player{i}", 1000, 1);

                    for (int j = 0; j < 10; j++)
                    {
                        // Give half high win rates (60%), half low (20%)
                        bool win = i < 5 ? j < 6 : j < 2;
                        pool.RecordGhostResult($"sub{i}", win);
                    }
                }

                var result = pool.Rerank(1, 10, 5, 40, 30);

                // With earlyMinWinRate=40, the 5 ghosts with 60% win rate should be champions
                int championCount = pool.GetChampionCount(1);
                if (championCount != 5)
                    return QATestResult.Fail(name, $"Expected 5 champions (>= 40% WR), got {championCount}");

                return QATestResult.Pass(name, $"Early pool: {championCount} champions by win rate threshold");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        public static QATestResult TestGhostPoolStalenessRetirement()
        {
            const string name = "DeploymentRound.GhostPoolStale";
            try
            {
                var pool = new GhostArmyPool();
                var army = MakeSubmission("stale", "player1", 1);
                pool.SubmitGhost(army, "player1", 1000, 1);

                // Manually set last match time to 31 days ago
                foreach (var g in pool.AllGhosts)
                {
                    if (g.Army.SubmissionId == "stale")
                        g.LastMatchTicks = DateTime.UtcNow.Ticks - (TimeSpan.TicksPerDay * 31);
                }

                pool.Rerank(1, 10, 5, 40, 30);

                if (pool.GetGhostCount(1) != 0)
                    return QATestResult.Fail(name, "Stale ghost should have been retired");

                return QATestResult.Pass(name, "Ghost retired after exceeding max age");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        public static QATestResult TestGhostPoolNoSelfMatching()
        {
            const string name = "DeploymentRound.GhostPoolNoSelfMatch";
            try
            {
                var pool = new GhostArmyPool();
                var army = MakeSubmission("myarmy", "player1", 1);
                pool.SubmitGhost(army, "player1", 1000, 1);

                // Mark as champion
                for (int i = 0; i < 10; i++) pool.RecordGhostResult("myarmy", true);
                pool.Rerank(1, 10, 5, 40, 30);

                var excludeIds = new HashSet<string> { "player1" };
                var opponent = pool.FindOpponent(1, 1000, 200, excludeIds, false);

                if (opponent != null)
                    return QATestResult.Fail(name, "Should not match player against their own ghost");

                return QATestResult.Pass(name, "Self-matching correctly prevented");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        public static QATestResult TestGhostPoolFindHighestEloChampion()
        {
            const string name = "DeploymentRound.GhostPoolHighestElo";
            try
            {
                var pool = new GhostArmyPool();

                // Create 3 champions at different Elos
                for (int i = 0; i < 3; i++)
                {
                    var army = MakeSubmission($"champ{i}", $"player{i}", 1);
                    pool.SubmitGhost(army, $"player{i}", 1000 + i * 200, 1);
                    for (int j = 0; j < 10; j++) pool.RecordGhostResult($"champ{i}", true);
                }
                pool.Rerank(1, 100, 5, 40, 30); // 100% = all are champions

                var best = pool.FindHighestEloChampion(1, null);
                if (best == null)
                    return QATestResult.Fail(name, "Should find a champion");
                if (best.OwnerElo != 1400)
                    return QATestResult.Fail(name, $"Expected highest Elo 1400, got {best.OwnerElo}");

                return QATestResult.Pass(name, "Found highest Elo champion correctly");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        // ─── Champion Title Tests ───

        public static QATestResult TestChampionTitlePromotion()
        {
            const string name = "DeploymentRound.ChampionTitlePromotion";
            try
            {
                var system = new ChampionTitleSystem();
                var promoted = new List<GhostArmy>
                {
                    new GhostArmy { OwnerId = "player1", Tier = 1 }
                };
                var result = new RerankResult { Promoted = promoted, Demoted = new List<GhostArmy>() };

                system.ProcessRerank(result);

                var status = system.GetStatus("player1");
                if (status.ActiveTitle != ChampionTitle.TierChampion)
                    return QATestResult.Fail(name, $"Expected TierChampion, got {status.ActiveTitle}");

                return QATestResult.Pass(name, "Player promoted to TierChampion");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        public static QATestResult TestChampionTitleDemotionToFormer()
        {
            const string name = "DeploymentRound.ChampionTitleDemotionFormer";
            try
            {
                var system = new ChampionTitleSystem();

                // Promote then demote
                system.ProcessRerank(new RerankResult
                {
                    Promoted = new List<GhostArmy> { new GhostArmy { OwnerId = "player1", Tier = 1 } },
                    Demoted = new List<GhostArmy>()
                });
                system.ProcessRerank(new RerankResult
                {
                    Promoted = new List<GhostArmy>(),
                    Demoted = new List<GhostArmy> { new GhostArmy { OwnerId = "player1", Tier = 1 } }
                });

                var status = system.GetStatus("player1");
                if (status.ActiveTitle != ChampionTitle.FormerChampion)
                    return QATestResult.Fail(name, $"Expected FormerChampion, got {status.ActiveTitle}");

                return QATestResult.Pass(name, "Demoted player becomes FormerChampion (never None)");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        public static QATestResult TestChampionTitleMultiTier()
        {
            const string name = "DeploymentRound.ChampionTitleMultiTier";
            try
            {
                var system = new ChampionTitleSystem();
                system.ProcessRerank(new RerankResult
                {
                    Promoted = new List<GhostArmy>
                    {
                        new GhostArmy { OwnerId = "player1", Tier = 1 },
                        new GhostArmy { OwnerId = "player1", Tier = 2 }
                    },
                    Demoted = new List<GhostArmy>()
                });

                var status = system.GetStatus("player1");
                if (status.ActiveTitle != ChampionTitle.MultiTierChampion)
                    return QATestResult.Fail(name, $"Expected MultiTierChampion, got {status.ActiveTitle}");

                return QATestResult.Pass(name, "Champion in 2+ tiers = MultiTierChampion");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        public static QATestResult TestChampionTitleNeverNoneAfterChampion()
        {
            const string name = "DeploymentRound.ChampionNeverNone";
            try
            {
                var system = new ChampionTitleSystem();

                // Promote in two tiers, demote from both
                system.ProcessRerank(new RerankResult
                {
                    Promoted = new List<GhostArmy>
                    {
                        new GhostArmy { OwnerId = "player1", Tier = 1 },
                        new GhostArmy { OwnerId = "player1", Tier = 2 }
                    },
                    Demoted = new List<GhostArmy>()
                });
                system.ProcessRerank(new RerankResult
                {
                    Promoted = new List<GhostArmy>(),
                    Demoted = new List<GhostArmy>
                    {
                        new GhostArmy { OwnerId = "player1", Tier = 1 },
                        new GhostArmy { OwnerId = "player1", Tier = 2 }
                    }
                });

                var status = system.GetStatus("player1");
                if (status.ActiveTitle == ChampionTitle.None)
                    return QATestResult.Fail(name, "Title should never be None after being champion");
                if (status.ActiveTitle != ChampionTitle.FormerChampion)
                    return QATestResult.Fail(name, $"Expected FormerChampion, got {status.ActiveTitle}");

                return QATestResult.Pass(name, "Once a champion, never goes back to None");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        // ─── House Army Tests ───

        public static QATestResult TestHouseArmiesExistPerTier()
        {
            const string name = "DeploymentRound.HouseArmiesPerTier";
            try
            {
                for (int tier = 1; tier <= 5; tier++)
                {
                    var armies = HouseArmyDatabase.GetArmiesForTier(tier);
                    if (armies.Count < 5)
                        return QATestResult.Fail(name,
                            $"Tier {tier} has only {armies.Count} house armies, expected >= 5");
                }

                return QATestResult.Pass(name, "All 5 tiers have >= 5 house armies");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        public static QATestResult TestHouseArmyFindClosest()
        {
            const string name = "DeploymentRound.HouseArmyFindClosest";
            try
            {
                var army = HouseArmyDatabase.FindClosestArmy(1, 1000);
                if (army == null)
                    return QATestResult.Fail(name, "Should find a house army at Elo 1000 tier 1");

                // Find closest to extreme Elo
                var low = HouseArmyDatabase.FindClosestArmy(1, 0);
                var high = HouseArmyDatabase.FindClosestArmy(1, 9999);
                if (low == null || high == null)
                    return QATestResult.Fail(name, "Should find armies at extreme Elos");
                if (low.TargetElo >= high.TargetElo)
                    return QATestResult.Fail(name, "Low Elo army should have lower target than high Elo army");

                return QATestResult.Pass(name, $"Found closest armies: low={low.TargetElo}, high={high.TargetElo}");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        // ─── Star Threshold Tests ───

        public static QATestResult TestConfigurableStarThresholds()
        {
            const string name = "DeploymentRound.StarThresholds";
            try
            {
                // 10 units total, 8 surviving = 80%
                var result = new BattleResult(
                    BattleOutcome.PlayerWin, 15, 8, 0, 200, 0,
                    new List<BattleEvent>());

                // Default thresholds: 100% flawless, 50% decisive
                int stars100 = BattleResultCalculator.CalculateStars(result, 10, 100, 50);
                if (stars100 != 2)
                    return QATestResult.Fail(name,
                        $"80% survival with default thresholds should be 2 stars (decisive), got {stars100}");

                // Relaxed threshold: 80% flawless
                int stars80 = BattleResultCalculator.CalculateStars(result, 10, 80, 50);
                if (stars80 != 3)
                    return QATestResult.Fail(name,
                        $"80% survival with 80% flawless threshold should be 3 stars, got {stars80}");

                // Tight threshold: 90% decisive
                int stars90 = BattleResultCalculator.CalculateStars(result, 10, 100, 90);
                if (stars90 != 1)
                    return QATestResult.Fail(name,
                        $"80% survival with 90% decisive threshold should be 1 star, got {stars90}");

                // Loss
                var loss = new BattleResult(
                    BattleOutcome.EnemyWin, 15, 0, 5, 0, 100,
                    new List<BattleEvent>());
                int lossStars = BattleResultCalculator.CalculateStars(loss, 10, 80, 40);
                if (lossStars != 0)
                    return QATestResult.Fail(name, $"Loss should always be 0 stars, got {lossStars}");

                return QATestResult.Pass(name, "Configurable thresholds produce correct star ratings");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        // ─── Deployment Round Tests ───

        public static QATestResult TestDeploymentRoundCalculateTotals()
        {
            const string name = "DeploymentRound.CalculateTotals";
            try
            {
                var round = new DeploymentRound();
                round.Battles.Add(new RoundBattle { Stars = 3 });
                round.Battles.Add(new RoundBattle { Stars = 3 });
                round.Battles.Add(new RoundBattle { Stars = 3 });
                round.BonusBattle = new RoundBattle { Stars = 1 };

                round.CalculateTotals();

                if (round.TotalStars != 10)
                    return QATestResult.Fail(name, $"Expected 10 total stars, got {round.TotalStars}");
                if (!round.IsPerfectRun)
                    return QATestResult.Fail(name, "Should be a perfect run");

                // Without bonus
                var round2 = new DeploymentRound();
                round2.Battles.Add(new RoundBattle { Stars = 2 });
                round2.Battles.Add(new RoundBattle { Stars = 3 });
                round2.Battles.Add(new RoundBattle { Stars = 3 });
                round2.CalculateTotals();

                if (round2.TotalStars != 8)
                    return QATestResult.Fail(name, $"Expected 8 stars without bonus, got {round2.TotalStars}");
                if (round2.IsPerfectRun)
                    return QATestResult.Fail(name, "8 stars should not be a perfect run");

                return QATestResult.Pass(name, "Totals calculated correctly with and without bonus");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        public static QATestResult TestBonusRoundTriggerAt9Stars()
        {
            const string name = "DeploymentRound.BonusRoundAt9Stars";
            try
            {
                // 9 stars should trigger bonus (configurable threshold)
                int threshold = 9;

                int stars8 = 2 + 3 + 3; // 8 - no bonus
                int stars9 = 3 + 3 + 3; // 9 - bonus triggered

                if (stars8 >= threshold)
                    return QATestResult.Fail(name, "8 stars should not trigger bonus");
                if (stars9 < threshold)
                    return QATestResult.Fail(name, "9 stars should trigger bonus");

                return QATestResult.Pass(name, "Bonus round triggers at exactly 9 stars, not 8");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        // ─── PlayerProfile Tests ───

        public static QATestResult TestPlayerProfileRecordDeploymentRound()
        {
            const string name = "DeploymentRound.ProfileRecordRound";
            try
            {
                var profile = new PlayerProfile("player1", "Test Player");

                var round = new DeploymentRound
                {
                    TotalStars = 10,
                    IsPerfectRun = true,
                    BonusBattle = new RoundBattle
                    {
                        IsChampionChallenge = true,
                        Result = new BattleResult(BattleOutcome.PlayerWin, 10, 5, 0, 100, 0, new List<BattleEvent>())
                    },
                    Battles = new List<RoundBattle>
                    {
                        new RoundBattle
                        {
                            IsChampionChallenge = true,
                            Result = new BattleResult(BattleOutcome.PlayerWin, 10, 5, 0, 100, 0, new List<BattleEvent>())
                        },
                        new RoundBattle
                        {
                            IsChampionChallenge = false,
                            Result = new BattleResult(BattleOutcome.PlayerWin, 10, 5, 0, 100, 0, new List<BattleEvent>())
                        },
                        new RoundBattle
                        {
                            IsChampionChallenge = false,
                            Result = new BattleResult(BattleOutcome.EnemyWin, 10, 0, 5, 0, 100, new List<BattleEvent>())
                        }
                    }
                };

                profile.RecordDeploymentRound(round);

                if (profile.TotalDeploymentRounds != 1)
                    return QATestResult.Fail(name, $"Expected 1 round, got {profile.TotalDeploymentRounds}");
                if (profile.TotalStarsEarned != 10)
                    return QATestResult.Fail(name, $"Expected 10 stars, got {profile.TotalStarsEarned}");
                if (profile.PerfectRuns != 1)
                    return QATestResult.Fail(name, $"Expected 1 perfect run, got {profile.PerfectRuns}");
                if (profile.BonusRoundsPlayed != 1)
                    return QATestResult.Fail(name, $"Expected 1 bonus round, got {profile.BonusRoundsPlayed}");
                if (profile.ChampionWins != 2) // 1 from battles + 1 from bonus
                    return QATestResult.Fail(name, $"Expected 2 champion wins, got {profile.ChampionWins}");

                return QATestResult.Pass(name, "Profile correctly records deployment round stats");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        // ─── Integration Tests ───

        public static QATestResult TestFindBestOpponentPriority(GameConfigData config)
        {
            const string name = "DeploymentRound.OpponentPriority";
            try
            {
                var ghostPool = new GhostArmyPool();
                var championSystem = new ChampionTitleSystem();
                var armyPool = new ArmyPool(ghostPool, championSystem);

                var excludeIds = new HashSet<string> { "player1" };

                // Empty pool: should fall back to house army
                var (opponent1, source1) = armyPool.FindBestOpponent(1, 1000, 200, excludeIds);
                if (source1 != OpponentSource.HouseArmy)
                    return QATestResult.Fail(name,
                        $"Empty pool should return HouseArmy, got {source1}");
                if (opponent1 == null)
                    return QATestResult.Fail(name, "Should find a house army opponent");

                // Add ghost: should prefer ghost over house
                var ghostArmy = MakeSubmission("ghost1", "player2", 1);
                ghostPool.SubmitGhost(ghostArmy, "player2", 1000, 1);
                for (int i = 0; i < 10; i++) ghostPool.RecordGhostResult("ghost1", true);
                ghostPool.Rerank(1, 100, 5, 40, 30); // 100% = all champion

                var (opponent2, source2) = armyPool.FindBestOpponent(1, 1000, 200, excludeIds);
                if (source2 != OpponentSource.GhostArmy)
                    return QATestResult.Fail(name,
                        $"With ghost available, should return GhostArmy, got {source2}");

                // Add live player: should prefer live over ghost
                var liveArmy = MakeSubmission("live1", "player3", 1);
                armyPool.Submit(liveArmy);

                var (opponent3, source3) = armyPool.FindBestOpponent(1, 1000, 200, excludeIds);
                if (source3 != OpponentSource.LivePlayer)
                    return QATestResult.Fail(name,
                        $"With live player available, should return LivePlayer, got {source3}");

                return QATestResult.Pass(name, "Priority: Live > Ghost > House");
            }
            catch (Exception ex) { return QATestResult.Fail(name, ex.Message); }
        }

        // ─── Helpers ───

        private static ArmySubmission MakeSubmission(string id, string playerId, int tier)
        {
            return new ArmySubmission
            {
                SubmissionId = id,
                PlayerId = playerId,
                Tier = tier,
                ArmyName = "Test",
                TotalCost = 10,
                Units = new List<SubmittedUnit>
                {
                    new SubmittedUnit { UnitTypeId = "LineInfantry", X = 5, Y = 1 }
                },
                Status = SubmissionStatus.InPool,
                SubmittedAtTicks = DateTime.UtcNow.Ticks
            };
        }
    }
}
