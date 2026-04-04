using System;
using System.Collections.Generic;
using WarChess.Units;

namespace WarChess.Config
{
    /// <summary>
    /// Calculates unit costs algorithmically from stats, abilities, formations, and traits.
    /// Uses integer-only math (deterministic). All intermediate values are base-100
    /// (100 = 1.0 cost point) or base-1000 (weights per stat point).
    ///
    /// Algorithm:
    ///   1. StatScore = weighted sum of (HP, ATK, DEF, SPD, RNG, MOV), anchored to reference unit
    ///   2. AdjustedStat = StatScore * AbilityMultiplier / 100
    ///   3. Total = AdjustedStat + AbilityFlat + FormationValue + TraitAdjustment
    ///   4. FinalCost = clamp(round(Total / 100), MinCost, MaxCost)
    ///
    /// Anchoring: Line Infantry always produces its reference cost from stats alone.
    /// Every other unit is measured relative to it. This prevents drift when weights change.
    ///
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class UnitCostCalculator
    {
        /// <summary>
        /// Computes the raw stat score for a unit (base-1000 total, before normalization).
        /// This is the weighted sum of all combat stats.
        /// </summary>
        public static int ComputeRawStatScore(int hp, int atk, int def, int spd, int rng, int mov,
            UnitCostConfig config)
        {
            return hp  * config.WeightHp
                 + atk * config.WeightAtk
                 + def * config.WeightDef
                 + spd * config.WeightSpd
                 + rng * config.WeightRng
                 + mov * config.WeightMov;
        }

        /// <summary>
        /// Computes the reference raw score for Line Infantry with the given config.
        /// Call this once when config changes to update <see cref="UnitCostConfig.ReferenceRawScore"/>.
        /// </summary>
        public static int ComputeReferenceScore(UnitCostConfig config)
        {
            // Line Infantry stats: HP:30 ATK:8 DEF:6 SPD:3 RNG:1 MOV:2
            return ComputeRawStatScore(30, 8, 6, 3, 1, 2, config);
        }

        /// <summary>
        /// Computes the stat-only cost component (base-100), anchored to the reference unit.
        /// </summary>
        public static int CalculateStatScore(int hp, int atk, int def, int spd, int rng, int mov,
            UnitCostConfig config)
        {
            int rawScore = ComputeRawStatScore(hp, atk, def, spd, rng, mov, config);
            int delta = rawScore - config.ReferenceRawScore;
            // Convert from base-1000 delta to base-100 cost and add anchor
            return (delta * 100) / 1000 + config.ReferenceCostHundredths;
        }

        /// <summary>
        /// Returns the ability multiplier (base-100) for the given ability type.
        /// Falls back to 100 (no scaling) if not configured.
        /// </summary>
        public static int GetAbilityMultiplier(AbilityType ability, UnitCostConfig config)
        {
            if (config.AbilityMultipliers != null &&
                config.AbilityMultipliers.TryGetValue(ability, out int mult))
                return mult;
            return 100;
        }

        /// <summary>
        /// Returns the flat ability value (base-100) for the given ability type.
        /// Falls back to 0 if not configured.
        /// </summary>
        public static int GetAbilityFlatValue(AbilityType ability, UnitCostConfig config)
        {
            if (config.AbilityFlatValues != null &&
                config.AbilityFlatValues.TryGetValue(ability, out int flat))
                return flat;
            return 0;
        }

        /// <summary>
        /// Returns the formation access value (base-100) for the given formation type.
        /// Falls back to 0 if not configured.
        /// </summary>
        public static int CalculateFormationValue(FormationType formation, UnitCostConfig config)
        {
            if (config.FormationValues != null &&
                config.FormationValues.TryGetValue(formation, out int val))
                return val;
            return 0;
        }

        /// <summary>
        /// Calculates trait adjustments (base-100) for special unit properties:
        /// artillery strength-scaling exemption and flanking overrides.
        /// </summary>
        public static int CalculateTraitAdjustment(UnitType type, AbilityType ability,
            int flankRearMultiplier, UnitCostConfig config)
        {
            int adjustment = 0;

            // Artillery types are exempt from strength scaling (always deal full damage)
            if (DamageCalculatorCompat.IsArtilleryType(type))
                adjustment += config.ArtilleryExemptBonus;

            // Flanking overrides
            if (flankRearMultiplier < config.DefaultRearFlankMultiplier)
                adjustment += config.ReducedRearFlankBonus;
            else if (flankRearMultiplier > config.DefaultRearFlankMultiplier)
                adjustment -= config.IncreasedRearFlankPenalty;

            return adjustment;
        }

        /// <summary>
        /// Calculates the full cost for a unit, returning the final integer army point cost.
        /// </summary>
        public static int CalculateCost(
            string unitName, UnitType type, AbilityType ability, FormationType formation,
            int hp, int atk, int def, int spd, int rng, int mov,
            int flankRearMultiplier, UnitCostConfig config)
        {
            // Check for manual override first
            if (config.CostOverrides != null &&
                config.CostOverrides.TryGetValue(type, out int overrideCost))
                return overrideCost;

            var breakdown = GetCostBreakdown(unitName, type, ability, formation,
                hp, atk, def, spd, rng, mov, flankRearMultiplier, config);
            return breakdown.FinalCost;
        }

        /// <summary>
        /// Calculates the full cost for a UnitInstance.
        /// </summary>
        public static int CalculateCost(UnitInstance unit, UnitCostConfig config)
        {
            return CalculateCost(
                unit.Name, unit.Type, unit.Ability, unit.FormationType,
                unit.MaxHp, unit.Atk, unit.Def, unit.Spd, unit.Rng, unit.Mov,
                unit.FlankRearMultiplier, config);
        }

        /// <summary>
        /// Returns a detailed cost breakdown showing each component's contribution.
        /// Useful for balance debugging and QA reporting.
        /// </summary>
        public static CostBreakdown GetCostBreakdown(
            string unitName, UnitType type, AbilityType ability, FormationType formation,
            int hp, int atk, int def, int spd, int rng, int mov,
            int flankRearMultiplier, UnitCostConfig config)
        {
            // Check for override
            bool isOverridden = config.CostOverrides != null &&
                                config.CostOverrides.TryGetValue(type, out int overrideCost);

            // 1. Stat score (base-100, anchored to reference unit)
            int statScore = CalculateStatScore(hp, atk, def, spd, rng, mov, config);

            // 2. Apply ability multiplier
            int abilityMult = GetAbilityMultiplier(ability, config);
            int adjustedStatScore = (statScore * abilityMult) / 100;

            // 3. Add flat ability value
            int abilityFlat = GetAbilityFlatValue(ability, config);

            // 4. Formation access value
            int formationValue = CalculateFormationValue(formation, config);

            // 5. Trait adjustments
            int traitAdj = CalculateTraitAdjustment(type, ability, flankRearMultiplier, config);

            // 6. Sum all components
            int rawTotal = adjustedStatScore + abilityFlat + formationValue + traitAdj;

            // 7. Round and clamp
            int finalCost = (rawTotal + 50) / 100; // Round to nearest integer
            finalCost = Math.Max(config.MinCost, Math.Min(config.MaxCost, finalCost));

            // If overridden, use the override as final cost
            if (isOverridden)
                finalCost = overrideCost;

            return new CostBreakdown(
                unitName, statScore, adjustedStatScore, abilityMult,
                abilityFlat, formationValue, traitAdj, rawTotal, finalCost, isOverridden);
        }

        /// <summary>
        /// Returns a breakdown for a UnitInstance.
        /// </summary>
        public static CostBreakdown GetCostBreakdown(UnitInstance unit, UnitCostConfig config)
        {
            return GetCostBreakdown(
                unit.Name, unit.Type, unit.Ability, unit.FormationType,
                unit.MaxHp, unit.Atk, unit.Def, unit.Spd, unit.Rng, unit.Mov,
                unit.FlankRearMultiplier, config);
        }

        /// <summary>
        /// Calculates costs for all 14 unit types from the static unit data table.
        /// Returns a dictionary compatible with <see cref="GameConfigData.GetUnitCosts"/>.
        /// Does NOT use UnitFactory (avoids circular dependency since UnitFactory
        /// calls GetUnitCosts to look up cost values).
        /// </summary>
        public static Dictionary<string, int> CalculateAllCosts(UnitCostConfig config)
        {
            var costs = new Dictionary<string, int>();
            foreach (var data in UnitDataTable)
            {
                var breakdown = GetCostBreakdown(
                    data.Name, data.Type, data.Ability, data.Formation,
                    data.Hp, data.Atk, data.Def, data.Spd, data.Rng, data.Mov,
                    data.FlankRearMultiplier, config);
                costs[data.TypeName] = breakdown.FinalCost;
            }
            return costs;
        }

        /// <summary>
        /// Calculates cost breakdowns for all 14 unit types. Useful for QA reports.
        /// Does NOT use UnitFactory (avoids circular dependency).
        /// </summary>
        public static Dictionary<string, CostBreakdown> CalculateAllBreakdowns(UnitCostConfig config)
        {
            var breakdowns = new Dictionary<string, CostBreakdown>();
            foreach (var data in UnitDataTable)
            {
                breakdowns[data.TypeName] = GetCostBreakdown(
                    data.Name, data.Type, data.Ability, data.Formation,
                    data.Hp, data.Atk, data.Def, data.Spd, data.Rng, data.Mov,
                    data.FlankRearMultiplier, config);
            }
            return breakdowns;
        }

        /// <summary>
        /// Generates a formatted report of all unit cost breakdowns for logging.
        /// </summary>
        public static string GenerateCostReport(UnitCostConfig config)
        {
            var breakdowns = CalculateAllBreakdowns(config);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Unit Cost Algorithm Report ===");
            sb.AppendLine($"Weights: HP={config.WeightHp} ATK={config.WeightAtk} DEF={config.WeightDef} " +
                          $"SPD={config.WeightSpd} RNG={config.WeightRng} MOV={config.WeightMov}");
            sb.AppendLine($"Reference score: {config.ReferenceRawScore}, anchor cost: {config.ReferenceCostHundredths / 100.0:F2}");
            sb.AppendLine();

            foreach (var kvp in breakdowns)
            {
                sb.AppendLine(kvp.Value.ToString());
            }

            return sb.ToString();
        }

        /// <summary>All 14 unit type names for enumeration.</summary>
        public static readonly string[] AllUnitTypeNames =
        {
            "LineInfantry", "Militia", "Cavalry", "Artillery", "Grenadier",
            "Rifleman", "Hussar", "Cuirassier", "HorseArtillery", "Sapper",
            "OldGuard", "RocketBattery", "Lancer", "Dragoon"
        };

        // ─── Static Unit Data Table ───

        /// <summary>
        /// Lightweight record of unit stats for cost calculation. Mirrors UnitFactory
        /// data without creating UnitInstance objects (avoids circular dependency).
        /// </summary>
        public readonly struct UnitData
        {
            public readonly string TypeName;
            public readonly string Name;
            public readonly UnitType Type;
            public readonly AbilityType Ability;
            public readonly FormationType Formation;
            public readonly int Hp, Atk, Def, Spd, Rng, Mov;
            public readonly int FlankRearMultiplier;

            public UnitData(string typeName, string name, UnitType type,
                AbilityType ability, FormationType formation,
                int hp, int atk, int def, int spd, int rng, int mov,
                int flankRearMultiplier)
            {
                TypeName = typeName; Name = name; Type = type;
                Ability = ability; Formation = formation;
                Hp = hp; Atk = atk; Def = def; Spd = spd; Rng = rng; Mov = mov;
                FlankRearMultiplier = flankRearMultiplier;
            }
        }

        /// <summary>
        /// Static table of all 14 unit types with GDD stats. Used by CalculateAllCosts
        /// and CalculateAllBreakdowns to avoid circular dependency with UnitFactory.
        /// Stats here must be kept in sync with UnitFactory create methods.
        /// </summary>
        public static readonly UnitData[] UnitDataTable =
        {
            new UnitData("LineInfantry",   "Line Infantry",   UnitType.LineInfantry,   AbilityType.None,              FormationType.BattleLine,     30,  8,  6, 3, 1, 2, 200),
            new UnitData("Militia",        "Militia",         UnitType.Militia,        AbilityType.StrengthInNumbers, FormationType.None,           18,  5,  3, 4, 1, 2, 200),
            new UnitData("Cavalry",        "Cavalry",         UnitType.Cavalry,        AbilityType.Charge,            FormationType.CavalryWedge,   25, 10,  4, 6, 1, 4, 200),
            new UnitData("Artillery",      "Artillery",       UnitType.Artillery,      AbilityType.Bombardment,       FormationType.Battery,        15, 14,  2, 1, 4, 1, 200),
            new UnitData("Grenadier",      "Grenadier",       UnitType.Grenadier,      AbilityType.Grenade,           FormationType.BattleLine,     40, 12,  8, 2, 1, 2, 200),
            new UnitData("Rifleman",       "Rifleman",        UnitType.Rifleman,       AbilityType.AimedShot,         FormationType.SkirmishScreen, 20, 11,  3, 5, 3, 2, 200),
            new UnitData("Hussar",         "Hussar",          UnitType.Hussar,         AbilityType.HitAndRun,         FormationType.CavalryWedge,   20,  7,  3, 8, 1, 5, 200),
            new UnitData("Cuirassier",     "Cuirassier",      UnitType.Cuirassier,     AbilityType.ArmoredCharge,     FormationType.CavalryWedge,   35, 13,  7, 4, 1, 3, 200),
            new UnitData("HorseArtillery", "Horse Artillery",  UnitType.HorseArtillery, AbilityType.LimberedUp,       FormationType.Battery,        12, 10,  2, 5, 3, 3, 200),
            new UnitData("Sapper",         "Sapper",          UnitType.Sapper,         AbilityType.Entrench,          FormationType.None,           22,  6,  5, 3, 1, 2, 200),
            new UnitData("OldGuard",       "Old Guard",       UnitType.OldGuard,       AbilityType.Unbreakable,       FormationType.BattleLine,     45, 14, 10, 3, 1, 2, 150),
            new UnitData("RocketBattery",  "Rocket Battery",  UnitType.RocketBattery,  AbilityType.CongreveBarrage,   FormationType.None,           10, 16,  1, 2, 5, 1, 250),
            new UnitData("Lancer",         "Lancer",          UnitType.Lancer,         AbilityType.Brace,             FormationType.CavalryWedge,   28, 11,  5, 5, 1, 3, 200),
            new UnitData("Dragoon",        "Dragoon",         UnitType.Dragoon,        AbilityType.Dismount,          FormationType.CavalryWedge,   28,  9,  5, 5, 1, 4, 200),
        };

        /// <summary>
        /// Compatibility helper to check if a unit type is an artillery type
        /// (exempt from strength scaling). Avoids depending on DamageCalculator directly
        /// so the cost calculator can be used independently.
        /// </summary>
        private static class DamageCalculatorCompat
        {
            public static bool IsArtilleryType(UnitType type)
            {
                return type == UnitType.Artillery
                    || type == UnitType.HorseArtillery
                    || type == UnitType.RocketBattery;
            }
        }
    }
}
