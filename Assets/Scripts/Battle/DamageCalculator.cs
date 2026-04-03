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
