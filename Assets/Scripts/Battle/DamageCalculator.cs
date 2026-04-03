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

            // Step 2: Apply modifiers in GDD Section 2.6 order
            int damage = baseDamage;

            // 1. Charge bonus
            if (isCharge)
                damage = (damage * chargeMultiplier) / 100;

            // 2. Terrain defense (applied to damage — lower multiplier = less damage taken)
            damage = (damage * terrainDefenseMultiplier) / 100;

            // 3. Terrain elevation (attacker on hill)
            damage = (damage * terrainAttackMultiplier) / 100;

            // 4. Formation bonus
            damage = (damage * formationMultiplier) / 100;

            // 5. Flanking multiplier (uses defender's per-unit values)
            int flankMult = FlankingCalculator.GetMultiplier(flankDir, defender);
            damage = (damage * flankMult) / 100;

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
