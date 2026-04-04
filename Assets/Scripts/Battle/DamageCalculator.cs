using System;
using WarChess.Units;

namespace WarChess.Battle
{
    /// <summary>
    /// Calculates combat damage using integer-only math. Implements GDD Section 2.6.
    /// All multipliers are base-100 integers (e.g., 130 = x1.3).
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Returns a base-100 strength multiplier representing a unit's reduced combat
        /// effectiveness as it takes casualties. Uses a square root curve so damage
        /// degrades gracefully rather than linearly (which would stall battles).
        ///
        /// Artillery types (Artillery, HorseArtillery, RocketBattery) are exempt —
        /// they represent a single gun piece, not a regiment of soldiers.
        ///
        /// Example values at different HP percentages:
        ///   100% HP → 100,  75% → 86,  50% → 70,  25% → 50,  10% → 31
        /// </summary>
        /// <param name="attacker">The unit dealing damage.</param>
        /// <param name="floorMultiplier">Minimum multiplier (base-100). Prevents damage
        /// from becoming negligible. E.g., 25 means a unit always deals at least 25% damage.</param>
        /// <returns>Base-100 multiplier (100 = full damage, 50 = half damage).</returns>
        public static int GetStrengthMultiplier(UnitInstance attacker, int floorMultiplier)
        {
            // Artillery types are exempt — single gun piece, not a regiment
            if (IsArtilleryType(attacker.Type))
                return 100;

            // Full health = no penalty
            if (attacker.CurrentHp >= attacker.MaxHp)
                return 100;

            // Dead units deal no damage (caller should check IsAlive, but be safe)
            if (attacker.CurrentHp <= 0)
                return floorMultiplier;

            // sqrt(currentHp / maxHp) using integer math:
            // Scale to 10000 first so sqrt gives a base-100 result
            int scaled = attacker.CurrentHp * 10000 / attacker.MaxHp;
            int mult = IntSqrt(scaled);

            return Math.Max(Math.Min(mult, 100), floorMultiplier);
        }

        /// <summary>
        /// Returns true for unit types that represent a single artillery piece
        /// rather than a regiment of soldiers. These are exempt from strength scaling.
        /// </summary>
        public static bool IsArtilleryType(UnitType type)
        {
            return type == UnitType.Artillery
                || type == UnitType.HorseArtillery
                || type == UnitType.RocketBattery;
        }

        /// <summary>
        /// Integer square root using Newton's method. Returns floor(sqrt(n)).
        /// Deterministic, no floating point.
        /// </summary>
        private static int IntSqrt(int n)
        {
            if (n <= 0) return 0;
            if (n == 1) return 1;
            int x = n;
            int y = (x + 1) / 2;
            while (y < x)
            {
                x = y;
                y = (x + n / x) / 2;
            }
            return x;
        }

        /// <summary>
        /// Calculates final damage from attacker to defender with all applicable modifiers.
        /// Modifiers are applied in GDD order: charge, terrain defense, terrain attack,
        /// formation, flanking. Result is always >= minimumDamage (default 1).
        /// </summary>
        public static int Calculate(
            UnitInstance attacker,
            UnitInstance defender,
            FlankDirection flankDir,
            int terrainDefenseMultiplier,
            int terrainAttackMultiplier,
            int formationMultiplier,
            bool isCharge,
            int chargeMultiplier,
            int minimumDamage)
        {
            // Step 1: Base damage = ATK - DEF/2, minimum 1
            int baseDamage = attacker.Atk - (defender.Def / 2);
            baseDamage = Math.Max(baseDamage, 1);

            // Step 2: Combine all multipliers then apply in a single division
            // to minimize integer rounding loss from sequential divisions.
            int flankMult = FlankingCalculator.GetMultiplier(flankDir, defender);
            int chargeMult = isCharge ? chargeMultiplier : 100;

            // Combined = charge * terrainDef * terrainAtk * formation * flank
            // Each is base-100, so we divide by 100^5 to normalize back to a scalar
            long combined = (long)chargeMult * terrainDefenseMultiplier * terrainAttackMultiplier
                          * formationMultiplier * flankMult;
            int damage = (int)(baseDamage * combined / 100_00_00_00_00L);

            return Math.Max(damage, minimumDamage);
        }

        /// <summary>
        /// Simplified overload using default values for terrain/formation (no modifiers).
        /// Useful for basic v1 battles without terrain.
        /// </summary>
        public static int Calculate(
            UnitInstance attacker,
            UnitInstance defender,
            FlankDirection flankDir,
            bool isCharge,
            int chargeMultiplier,
            int minimumDamage)
        {
            return Calculate(
                attacker, defender, flankDir,
                terrainDefenseMultiplier: 100,
                terrainAttackMultiplier: 100,
                formationMultiplier: 100,
                isCharge, chargeMultiplier, minimumDamage);
        }

        /// <summary>
        /// Calculates AoE splash damage (e.g., Artillery Bombardment).
        /// Splash targets take a percentage of the primary damage.
        /// </summary>
        public static int CalculateSplashDamage(int primaryDamage, int splashPercentage)
        {
            return Math.Max((primaryDamage * splashPercentage) / 100, 1);
        }
    }
}
