using System;
using System.Collections.Generic;
using WarChess.Battle;
using WarChess.Config;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.QA.Tests
{
    /// <summary>
    /// Correctness tests for the damage calculation system: minimum damage,
    /// formula verification, splash damage, and charge mechanics.
    /// </summary>
    public static class DamageSystemTests
    {
        public static List<QATestResult> RunAll(GameConfigData config)
        {
            return new List<QATestResult>
            {
                TestDamageMinimum(config),
                TestDamageFormula(config),
                TestSplashDamage(config),
                TestChargeDamage(config),
                TestStrengthScaling(config),
                TestStrengthScalingArtilleryExempt(config)
            };
        }

        /// <summary>
        /// Damage must always be >= MinimumDamage.
        /// </summary>
        public static QATestResult TestDamageMinimum(GameConfigData config)
        {
            const string name = "DamageSystem.Minimum";
            try
            {
                UnitFactory.ResetIds();
                // High DEF vs low ATK should still do minimum damage
                var attacker = UnitFactory.CreateMilitia(Owner.Player, new GridCoord(5, 2));
                var defender = UnitFactory.CreateOldGuard(Owner.Enemy, new GridCoord(5, 3));

                // Militia ATK=5, OldGuard DEF=10: base = max(5 - 10/2, 1) = max(0, 1) = 1
                int damage = DamageCalculator.Calculate(attacker, defender,
                    FlankDirection.Front, false, config.ChargeMultiplier, config.MinimumDamage);

                if (damage < config.MinimumDamage)
                    return QATestResult.Fail(name,
                        $"Damage {damage} < MinimumDamage {config.MinimumDamage}");

                // Also test with all multipliers at 100 (neutral)
                int damage2 = DamageCalculator.Calculate(attacker, defender,
                    FlankDirection.Front, 100, 100, 100, false, config.ChargeMultiplier, config.MinimumDamage);

                if (damage2 < config.MinimumDamage)
                    return QATestResult.Fail(name,
                        $"Full-overload damage {damage2} < MinimumDamage {config.MinimumDamage}");

                return QATestResult.Pass(name, $"Minimum damage enforced: {damage}, {damage2}");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Verify base damage formula: max(ATK - DEF/2, 1) with known values.
        /// </summary>
        public static QATestResult TestDamageFormula(GameConfigData config)
        {
            const string name = "DamageSystem.Formula";
            try
            {
                UnitFactory.ResetIds();

                // LineInfantry ATK=8, DEF=6 vs LineInfantry ATK=8, DEF=6
                // base = max(8 - 6/2, 1) = max(8 - 3, 1) = 5
                // Front flank = 100, no charge, no terrain/formation = 5
                var attacker = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 2));
                var defender = UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 3));

                int damage = DamageCalculator.Calculate(attacker, defender,
                    FlankDirection.Front, false, config.ChargeMultiplier, config.MinimumDamage);

                int expectedBase = Math.Max(attacker.Atk - defender.Def / 2, 1);
                if (damage != expectedBase)
                    return QATestResult.Fail(name,
                        $"Expected {expectedBase}, got {damage} for LineInfantry vs LineInfantry front");

                // Test flanking multiplier: side = 130
                int sideDmg = DamageCalculator.Calculate(attacker, defender,
                    FlankDirection.Side, false, config.ChargeMultiplier, config.MinimumDamage);

                int expectedSide = (expectedBase * 130) / 100;
                if (sideDmg != expectedSide)
                    return QATestResult.Fail(name,
                        $"Expected side damage {expectedSide}, got {sideDmg}");

                // Test rear multiplier: rear = 200
                int rearDmg = DamageCalculator.Calculate(attacker, defender,
                    FlankDirection.Rear, false, config.ChargeMultiplier, config.MinimumDamage);

                int expectedRear = (expectedBase * 200) / 100;
                if (rearDmg != expectedRear)
                    return QATestResult.Fail(name,
                        $"Expected rear damage {expectedRear}, got {rearDmg}");

                return QATestResult.Pass(name,
                    $"Formula verified: base={expectedBase}, side={sideDmg}, rear={rearDmg}");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Splash damage must be >= 1 and the correct percentage of primary.
        /// </summary>
        public static QATestResult TestSplashDamage(GameConfigData config)
        {
            const string name = "DamageSystem.Splash";
            try
            {
                // Test various primary damages
                int[] primaryDamages = { 1, 2, 5, 10, 20, 50, 100 };

                foreach (int primary in primaryDamages)
                {
                    int splash = DamageCalculator.CalculateSplashDamage(primary, config.BombardmentSplashPercentage);

                    if (splash < 1)
                        return QATestResult.Fail(name,
                            $"Splash damage {splash} < 1 for primary {primary}");

                    int expected = Math.Max((primary * config.BombardmentSplashPercentage) / 100, 1);
                    if (splash != expected)
                        return QATestResult.Fail(name,
                            $"Expected splash {expected}, got {splash} for primary {primary}");
                }

                return QATestResult.Pass(name,
                    $"Splash damage correct at {config.BombardmentSplashPercentage}% for all test values");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Charge multiplier applied only when unit moved 3+ tiles and has Charge ability.
        /// </summary>
        public static QATestResult TestChargeDamage(GameConfigData config)
        {
            const string name = "DamageSystem.Charge";
            try
            {
                UnitFactory.ResetIds();
                var cavalry = UnitFactory.CreateCavalry(Owner.Player, new GridCoord(5, 2));
                var target = UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 3));

                // Without charge
                int normalDmg = DamageCalculator.Calculate(cavalry, target,
                    FlankDirection.Front, false, config.ChargeMultiplier, config.MinimumDamage);

                // With charge
                int chargeDmg = DamageCalculator.Calculate(cavalry, target,
                    FlankDirection.Front, true, config.ChargeMultiplier, config.MinimumDamage);

                if (chargeDmg <= normalDmg)
                    return QATestResult.Fail(name,
                        $"Charge damage {chargeDmg} not greater than normal {normalDmg}");

                // Verify charge multiplier applied correctly
                int expectedCharge = (normalDmg * config.ChargeMultiplier) / 100;
                if (chargeDmg != expectedCharge)
                    return QATestResult.Fail(name,
                        $"Expected charge damage {expectedCharge}, got {chargeDmg}");

                return QATestResult.Pass(name,
                    $"Charge damage correct: normal={normalDmg}, charge={chargeDmg} ({config.ChargeMultiplier}%)");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Strength scaling: damaged units deal less damage following sqrt curve.
        /// Full HP = 100%, half HP ≈ 70%, quarter HP = 50%.
        /// </summary>
        public static QATestResult TestStrengthScaling(GameConfigData config)
        {
            const string name = "DamageSystem.StrengthScaling";
            try
            {
                UnitFactory.ResetIds();
                var unit = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 2));

                // Full HP: multiplier should be 100
                int fullMult = DamageCalculator.GetStrengthMultiplier(unit, config.StrengthScalingFloor);
                if (fullMult != 100)
                    return QATestResult.Fail(name, $"Full HP multiplier should be 100, got {fullMult}");

                // Half HP: sqrt(0.5) ≈ 0.707 → multiplier ~70
                unit.TakeDamage(unit.MaxHp / 2);
                int halfMult = DamageCalculator.GetStrengthMultiplier(unit, config.StrengthScalingFloor);
                if (halfMult < 65 || halfMult > 75)
                    return QATestResult.Fail(name,
                        $"Half HP multiplier should be ~70, got {halfMult}");

                // Quarter HP: sqrt(0.25) = 0.5 → multiplier = 50
                unit.TakeDamage(unit.CurrentHp - unit.MaxHp / 4);
                int quarterMult = DamageCalculator.GetStrengthMultiplier(unit, config.StrengthScalingFloor);
                if (quarterMult != 50)
                    return QATestResult.Fail(name,
                        $"Quarter HP multiplier should be 50, got {quarterMult}");

                // Very low HP: should be clamped to floor
                unit.TakeDamage(unit.CurrentHp - 1); // 1 HP remaining
                int lowMult = DamageCalculator.GetStrengthMultiplier(unit, config.StrengthScalingFloor);
                if (lowMult < config.StrengthScalingFloor)
                    return QATestResult.Fail(name,
                        $"Low HP multiplier {lowMult} below floor {config.StrengthScalingFloor}");

                // Multiplier should always decrease as HP decreases
                if (halfMult > fullMult || quarterMult > halfMult || lowMult > quarterMult)
                    return QATestResult.Fail(name,
                        $"Multipliers not monotonically decreasing: full={fullMult}, half={halfMult}, quarter={quarterMult}, low={lowMult}");

                return QATestResult.Pass(name,
                    $"Strength scaling correct: full={fullMult}, half={halfMult}, quarter={quarterMult}, low={lowMult} (floor={config.StrengthScalingFloor})");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Artillery types must be exempt from strength scaling (always 100).
        /// They represent a single gun piece, not a regiment.
        /// </summary>
        public static QATestResult TestStrengthScalingArtilleryExempt(GameConfigData config)
        {
            const string name = "DamageSystem.StrengthScaling.ArtilleryExempt";
            try
            {
                UnitFactory.ResetIds();

                // Test all three artillery types
                var artillery = UnitFactory.CreateArtillery(Owner.Player, new GridCoord(3, 2));
                var horseArt = UnitFactory.CreateHorseArtillery(Owner.Player, new GridCoord(5, 2));
                var rocket = UnitFactory.CreateRocketBattery(Owner.Player, new GridCoord(7, 2));

                var artilleryUnits = new[] { artillery, horseArt, rocket };
                var names = new[] { "Artillery", "HorseArtillery", "RocketBattery" };

                for (int i = 0; i < artilleryUnits.Length; i++)
                {
                    var unit = artilleryUnits[i];
                    // Damage the unit to half HP
                    unit.TakeDamage(unit.MaxHp / 2);

                    int mult = DamageCalculator.GetStrengthMultiplier(unit, config.StrengthScalingFloor);
                    if (mult != 100)
                        return QATestResult.Fail(name,
                            $"{names[i]} at {unit.CurrentHp}/{unit.MaxHp} HP should have multiplier 100 (exempt), got {mult}");
                }

                return QATestResult.Pass(name,
                    "All artillery types exempt from strength scaling at half HP");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }
    }
}
