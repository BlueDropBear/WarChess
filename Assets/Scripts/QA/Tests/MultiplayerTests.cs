using System;
using System.Collections.Generic;
using WarChess.Army;
using WarChess.Config;
using WarChess.Core;
using WarChess.Multiplayer;
using WarChess.Units;

namespace WarChess.QA.Tests
{
    /// <summary>
    /// Correctness tests for multiplayer systems: army validation,
    /// serialization roundtrip, tier pools, and Elo invariants.
    /// </summary>
    public static class MultiplayerTests
    {
        public static List<QATestResult> RunAll(GameConfigData config)
        {
            return new List<QATestResult>
            {
                TestArmyValidatorRejects(config),
                TestArmyValidatorAccepts(config),
                TestSerializerRoundtrip(config),
                TestTierUnitPoolSupersets(config),
                TestEloNeverNegative(config),
                TestEloDrawSymmetry(config)
            };
        }

        /// <summary>
        /// Validator must reject: over-budget, wrong-tier, out-of-bounds, duplicate positions.
        /// </summary>
        public static QATestResult TestArmyValidatorRejects(GameConfigData config)
        {
            const string name = "Multiplayer.ValidatorRejects";
            try
            {
                // 1. Over-budget: Skirmish budget=25, place OldGuard(10)+Cuirassier(8)+Cuirassier(8) = 26
                var overBudget = new ArmySubmission
                {
                    Tier = 5,
                    Units = new List<SubmittedUnit>
                    {
                        new SubmittedUnit { UnitTypeId = "OldGuard", X = 1, Y = 1 },
                        new SubmittedUnit { UnitTypeId = "Cuirassier", X = 2, Y = 1 },
                        new SubmittedUnit { UnitTypeId = "Cuirassier", X = 3, Y = 1 }
                    }
                };
                string err = ArmyValidator.Validate(overBudget, MatchFormat.Skirmish, 5, config);
                if (err == null)
                    return QATestResult.Fail(name, "Over-budget army not rejected");

                // 2. Wrong-tier: Tier 1 army with Grenadier (tier 2+ only)
                var wrongTier = new ArmySubmission
                {
                    Tier = 1,
                    Units = new List<SubmittedUnit>
                    {
                        new SubmittedUnit { UnitTypeId = "Grenadier", X = 1, Y = 1 }
                    }
                };
                err = ArmyValidator.Validate(wrongTier, MatchFormat.Standard, 5, config);
                if (err == null)
                    return QATestResult.Fail(name, "Wrong-tier unit not rejected");

                // 3. Out-of-bounds: unit outside deployment zone
                var outOfBounds = new ArmySubmission
                {
                    Tier = 1,
                    Units = new List<SubmittedUnit>
                    {
                        new SubmittedUnit { UnitTypeId = "LineInfantry", X = 1, Y = 8 } // Row 8 is outside player deploy zone
                    }
                };
                err = ArmyValidator.Validate(outOfBounds, MatchFormat.Standard, 5, config);
                if (err == null)
                    return QATestResult.Fail(name, "Out-of-bounds placement not rejected");

                // 4. Duplicate positions
                var dupes = new ArmySubmission
                {
                    Tier = 1,
                    Units = new List<SubmittedUnit>
                    {
                        new SubmittedUnit { UnitTypeId = "LineInfantry", X = 5, Y = 1 },
                        new SubmittedUnit { UnitTypeId = "Militia", X = 5, Y = 1 }
                    }
                };
                err = ArmyValidator.Validate(dupes, MatchFormat.Standard, 5, config);
                if (err == null)
                    return QATestResult.Fail(name, "Duplicate position not rejected");

                // 5. Null submission
                err = ArmyValidator.Validate(null, MatchFormat.Standard, 5, config);
                if (err == null)
                    return QATestResult.Fail(name, "Null submission not rejected");

                // 6. Empty army
                var empty = new ArmySubmission { Tier = 1, Units = new List<SubmittedUnit>() };
                err = ArmyValidator.Validate(empty, MatchFormat.Standard, 5, config);
                if (err == null)
                    return QATestResult.Fail(name, "Empty army not rejected");

                // 7. Tier not unlocked
                var tierLocked = new ArmySubmission
                {
                    Tier = 5,
                    Units = new List<SubmittedUnit>
                    {
                        new SubmittedUnit { UnitTypeId = "LineInfantry", X = 1, Y = 1 }
                    }
                };
                err = ArmyValidator.Validate(tierLocked, MatchFormat.Standard, 2, config);
                if (err == null)
                    return QATestResult.Fail(name, "Locked tier not rejected");

                return QATestResult.Pass(name, "All 7 rejection cases verified");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Valid armies must be accepted at all format x tier combinations.
        /// </summary>
        public static QATestResult TestArmyValidatorAccepts(GameConfigData config)
        {
            const string name = "Multiplayer.ValidatorAccepts";
            try
            {
                var formats = new[] { MatchFormat.Skirmish, MatchFormat.Standard };
                var costs = GameConfigData.GetUnitCosts();

                for (int tier = 1; tier <= 5; tier++)
                {
                    var available = TierSystem.GetAvailableUnits(tier);

                    foreach (var format in formats)
                    {
                        int budget = ArmyValidator.FormatBudgets[format];

                        // Build a valid army within budget
                        var units = new List<SubmittedUnit>();
                        int spent = 0;
                        int col = 1;

                        foreach (var unitType in available)
                        {
                            int cost = costs.TryGetValue(unitType, out int c) ? c : 99;
                            if (spent + cost <= budget && col <= config.GridWidth)
                            {
                                units.Add(new SubmittedUnit { UnitTypeId = unitType, X = col, Y = 1 });
                                spent += cost;
                                col++;
                            }
                        }

                        if (units.Count == 0) continue;

                        var submission = new ArmySubmission { Tier = tier, Units = units };
                        string err = ArmyValidator.Validate(submission, format, tier, config);
                        if (err != null)
                            return QATestResult.Fail(name,
                                $"Valid army rejected at tier {tier} {format}: {err}");
                    }
                }

                // GrandBattle at tier 4+
                for (int tier = 4; tier <= 5; tier++)
                {
                    var submission = new ArmySubmission
                    {
                        Tier = tier,
                        Units = new List<SubmittedUnit>
                        {
                            new SubmittedUnit { UnitTypeId = "LineInfantry", X = 1, Y = 1 }
                        }
                    };
                    string err = ArmyValidator.Validate(submission, MatchFormat.GrandBattle, tier, config);
                    if (err != null)
                        return QATestResult.Fail(name,
                            $"Valid GrandBattle army rejected at tier {tier}: {err}");
                }

                return QATestResult.Pass(name, "Valid armies accepted at all format x tier combos");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// SavedArmy -> ArmySubmission -> UnitInstances roundtrip must preserve data.
        /// </summary>
        public static QATestResult TestSerializerRoundtrip(GameConfigData config)
        {
            const string name = "Multiplayer.SerializerRoundtrip";
            try
            {
                var army = new SavedArmy("TestArmy", ArmyMode.Multiplayer);
                army.Units.Add(new SavedUnitSlot("LineInfantry", 3, 1));
                army.Units.Add(new SavedUnitSlot("Cavalry", 5, 2));
                army.Units.Add(new SavedUnitSlot("Artillery", 7, 1));

                var submission = ArmySerializer.ToSubmission(army, 3, "player123");

                if (submission.Units.Count != army.Units.Count)
                    return QATestResult.Fail(name,
                        $"Unit count mismatch: {army.Units.Count} -> {submission.Units.Count}");
                if (submission.Tier != 3)
                    return QATestResult.Fail(name, $"Tier mismatch: expected 3, got {submission.Tier}");
                if (submission.ArmyName != "TestArmy")
                    return QATestResult.Fail(name, $"Name mismatch");

                // Check unit data preserved
                for (int i = 0; i < army.Units.Count; i++)
                {
                    if (submission.Units[i].UnitTypeId != army.Units[i].UnitTypeId)
                        return QATestResult.Fail(name,
                            $"Unit {i} type mismatch: {army.Units[i].UnitTypeId} vs {submission.Units[i].UnitTypeId}");
                    if (submission.Units[i].X != army.Units[i].X || submission.Units[i].Y != army.Units[i].Y)
                        return QATestResult.Fail(name,
                            $"Unit {i} position mismatch");
                }

                // Convert to UnitInstances
                UnitFactory.ResetIds();
                var instances = ArmySerializer.ToUnitInstances(submission, Owner.Player);

                if (instances.Count != army.Units.Count)
                    return QATestResult.Fail(name,
                        $"Instance count mismatch: expected {army.Units.Count}, got {instances.Count}");

                for (int i = 0; i < instances.Count; i++)
                {
                    if (instances[i].Position.X != army.Units[i].X ||
                        instances[i].Position.Y != army.Units[i].Y)
                        return QATestResult.Fail(name,
                            $"Instance {i} position mismatch after roundtrip");
                }

                return QATestResult.Pass(name, "Roundtrip preserved all data for 3-unit army");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Tier N+1 must contain all units from tier N (superset property).
        /// </summary>
        public static QATestResult TestTierUnitPoolSupersets(GameConfigData config)
        {
            const string name = "Multiplayer.TierPoolSupersets";
            try
            {
                for (int tier = 1; tier < 5; tier++)
                {
                    var current = TierSystem.GetAvailableUnits(tier);
                    var next = TierSystem.GetAvailableUnits(tier + 1);

                    foreach (var unit in current)
                    {
                        if (!next.Contains(unit))
                            return QATestResult.Fail(name,
                                $"Tier {tier + 1} missing '{unit}' from tier {tier}");
                    }

                    if (next.Count <= current.Count)
                        return QATestResult.Fail(name,
                            $"Tier {tier + 1} ({next.Count} units) not larger than tier {tier} ({current.Count} units)");
                }

                return QATestResult.Pass(name, "All tiers are strict supersets of previous");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Elo must never go below 0 after any result.
        /// </summary>
        public static QATestResult TestEloNeverNegative(GameConfigData config)
        {
            const string name = "Multiplayer.EloNeverNegative";
            try
            {
                // Test with very low Elo losing to high Elo
                int lowElo = 10;
                int highElo = 2000;

                var (newLow, newHigh) = EloSystem.CalculateNewRatings(lowElo, highElo, MatchResult.PlayerBWins);
                if (newLow < 0)
                    return QATestResult.Fail(name,
                        $"Elo went negative: {lowElo} -> {newLow} after loss to {highElo}");

                // Test with Elo=0 losing
                var (zero, _) = EloSystem.CalculateNewRatings(0, 1000, MatchResult.PlayerBWins);
                if (zero < 0)
                    return QATestResult.Fail(name, $"Elo went negative from 0: {zero}");

                // Stress test: 50 consecutive losses from low Elo
                int elo = 100;
                for (int i = 0; i < 50; i++)
                {
                    var (newElo, _2) = EloSystem.CalculateNewRatings(elo, 1500, MatchResult.PlayerBWins);
                    elo = newElo;
                    if (elo < 0)
                        return QATestResult.Fail(name,
                            $"Elo went negative after {i + 1} losses: {elo}");
                }

                return QATestResult.Pass(name, $"Elo never negative after stress test (final: {elo})");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Equal-rated draw should produce minimal or no change.
        /// </summary>
        public static QATestResult TestEloDrawSymmetry(GameConfigData config)
        {
            const string name = "Multiplayer.EloDrawSymmetry";
            try
            {
                int rating = 1000;
                var (newA, newB) = EloSystem.CalculateNewRatings(rating, rating, MatchResult.Draw);

                // Equal-rated draw: both should stay roughly the same
                int deltaA = Math.Abs(newA - rating);
                int deltaB = Math.Abs(newB - rating);

                if (deltaA > 2)
                    return QATestResult.Fail(name,
                        $"Equal-rated draw changed player A by {deltaA} (expected <=2)");
                if (deltaB > 2)
                    return QATestResult.Fail(name,
                        $"Equal-rated draw changed player B by {deltaB} (expected <=2)");

                // Symmetry: both players should change equally
                if (deltaA != deltaB)
                    return QATestResult.Fail(name,
                        $"Asymmetric draw result: deltaA={deltaA}, deltaB={deltaB}");

                return QATestResult.Pass(name,
                    $"Equal-rated draw: {rating} -> A:{newA}, B:{newB} (delta={deltaA})");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }
    }
}
