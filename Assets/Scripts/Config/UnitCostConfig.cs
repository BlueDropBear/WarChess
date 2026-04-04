using System.Collections.Generic;
using WarChess.Units;

namespace WarChess.Config
{
    /// <summary>
    /// Configuration for the unit cost algorithm. All stat weights are base-1000
    /// integers (per stat point). Ability multipliers are base-100. Ability flat
    /// values, formation values, and trait adjustments are base-100 (100 = 1.0 cost point).
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public sealed class UnitCostConfig
    {
        // ─── Stat Weights (per stat point, base-1000) ───

        /// <summary>Weight per HP point. HP provides raw durability but is less impactful than DEF.</summary>
        public int WeightHp { get; set; }

        /// <summary>Weight per ATK point. ATK is the primary offensive stat (linear damage increase).</summary>
        public int WeightAtk { get; set; }

        /// <summary>Weight per DEF point. DEF/2 reduces ALL incoming damage — most valuable defensive stat.</summary>
        public int WeightDef { get; set; }

        /// <summary>Weight per SPD point. Higher SPD = earlier initiative = compounding advantage.</summary>
        public int WeightSpd { get; set; }

        /// <summary>Weight per RNG point. Each range point enables safe damage and positional advantage.</summary>
        public int WeightRng { get; set; }

        /// <summary>Weight per MOV point. Enables charging, flanking, and tactical flexibility.</summary>
        public int WeightMov { get; set; }

        // ─── Reference Anchor ───

        /// <summary>
        /// The base cost (base-100) that the reference unit (Line Infantry) should produce
        /// from stats alone. All other units are measured relative to this anchor.
        /// Default 300 = 3.00 cost points.
        /// </summary>
        public int ReferenceCostHundredths { get; set; }

        /// <summary>
        /// Pre-computed raw stat score for the reference unit. Set by calling
        /// <see cref="UnitCostCalculator.ComputeReferenceScore"/>. This avoids
        /// recomputing the anchor every time a cost is calculated.
        /// </summary>
        public int ReferenceRawScore { get; set; }

        // ─── Ability Values ───

        /// <summary>
        /// Multiplicative ability scaling (base-100). Applied to the stat score before
        /// adding the flat component. Powerful abilities on expensive units scale up
        /// proportionally. 100 = no change, 120 = +20%.
        /// </summary>
        public Dictionary<AbilityType, int> AbilityMultipliers { get; set; }

        /// <summary>
        /// Flat additive ability value (base-100, 100 = 1.0 cost point). Added after
        /// the multiplier. Represents the inherent value of the ability independent
        /// of the unit's stat line.
        /// </summary>
        public Dictionary<AbilityType, int> AbilityFlatValues { get; set; }

        // ─── Formation Values ───

        /// <summary>
        /// Flat value for having access to a formation type (base-100).
        /// Prices the potential to form up, not the guarantee.
        /// </summary>
        public Dictionary<FormationType, int> FormationValues { get; set; }

        // ─── Trait Adjustments ───

        /// <summary>
        /// Bonus for artillery-type units exempt from strength scaling (base-100).
        /// These units always deal full damage regardless of HP loss.
        /// </summary>
        public int ArtilleryExemptBonus { get; set; }

        /// <summary>
        /// Bonus for units with reduced rear flank vulnerability (base-100).
        /// E.g., Old Guard has rear multiplier 150 instead of default 200.
        /// </summary>
        public int ReducedRearFlankBonus { get; set; }

        /// <summary>
        /// Penalty for units with increased rear flank vulnerability (base-100).
        /// E.g., Rocket Battery has rear multiplier 250 instead of default 200.
        /// Stored as a positive number; subtracted during calculation.
        /// </summary>
        public int IncreasedRearFlankPenalty { get; set; }

        /// <summary>Default rear flank multiplier from GameConfig (200 = x2.0).</summary>
        public int DefaultRearFlankMultiplier { get; set; }

        // ─── Cost Bounds ───

        /// <summary>Minimum allowed final cost (inclusive).</summary>
        public int MinCost { get; set; }

        /// <summary>Maximum allowed final cost (inclusive).</summary>
        public int MaxCost { get; set; }

        // ─── Manual Overrides ───

        /// <summary>
        /// Optional per-unit cost overrides. If a unit type has an entry here,
        /// the override is used as the final cost instead of the algorithm output.
        /// The algorithm still computes a "suggested" cost for comparison.
        /// Null entries or missing keys mean "use algorithm".
        /// </summary>
        public Dictionary<UnitType, int> CostOverrides { get; set; }

        /// <summary>
        /// Returns the default config tuned to match GDD Section 3.2 costs exactly.
        /// Stat weights were derived via grid search optimization against all 14 unit costs.
        /// </summary>
        public static UnitCostConfig Default
        {
            get
            {
                var config = new UnitCostConfig
                {
                    // Stat weights (base-1000, per stat point)
                    WeightHp  = 30,
                    WeightAtk = 240,
                    WeightDef = 180,
                    WeightSpd = 150,
                    WeightRng = 430,
                    WeightMov = 210,

                    // Reference: Line Infantry (HP:30 ATK:8 DEF:6 SPD:3 RNG:1 MOV:2 = cost 3)
                    ReferenceCostHundredths = 300,
                    ReferenceRawScore = 5200, // Pre-computed for LineInfantry with above weights

                    // Ability multipliers (base-100, applied to stat score)
                    AbilityMultipliers = new Dictionary<AbilityType, int>
                    {
                        { AbilityType.None,              100 }, // No scaling
                        { AbilityType.StrengthInNumbers,  95 }, // Slightly reduces — conditional ability on cheap unit
                        { AbilityType.Charge,            115 }, // x2 damage on 3+ tile charge
                        { AbilityType.Bombardment,       120 }, // AoE splash (50%) hits multiple targets
                        { AbilityType.Grenade,           115 }, // One-time 5 damage in 2-tile radius
                        { AbilityType.AimedShot,         110 }, // +50% ATK when stationary
                        { AbilityType.HitAndRun,         105 }, // Retreat after attacking
                        { AbilityType.ArmoredCharge,     125 }, // Charge + 50% damage reduction
                        { AbilityType.LimberedUp,        120 }, // Move AND attack same round
                        { AbilityType.Entrench,          100 }, // Creates Fortification tile — value is mostly flat
                        { AbilityType.Unbreakable,       140 }, // Linear strength scaling, floor 75%
                        { AbilityType.CongreveBarrage,   105 }, // Random 3x3, ignores fort/LoS, may friendly fire
                        { AbilityType.Brace,             105 }, // Counter-charge: attacks first at x1.5 vs cavalry
                        { AbilityType.Dismount,          115 }, // Permanent +3 DEF, +2 ATK after melee
                    },

                    // Ability flat values (base-100, added after multiplier)
                    AbilityFlatValues = new Dictionary<AbilityType, int>
                    {
                        { AbilityType.None,                0 }, // No ability
                        { AbilityType.StrengthInNumbers, -30 }, // Reduces cost — Militia is intentionally cheap filler
                        { AbilityType.Charge,             50 }, // Consistent value from charge mechanic
                        { AbilityType.Bombardment,        80 }, // AoE splash is reliably valuable
                        { AbilityType.Grenade,           170 }, // Large burst, reflects elite Grenadier status
                        { AbilityType.AimedShot,          30 }, // Reliable when positioned correctly
                        { AbilityType.HitAndRun,          30 }, // Survivability through retreat
                        { AbilityType.ArmoredCharge,     175 }, // Premium heavy cavalry ability
                        { AbilityType.LimberedUp,         90 }, // Huge flexibility for artillery platform
                        { AbilityType.Entrench,          150 }, // Creates permanent terrain advantage
                        { AbilityType.Unbreakable,       130 }, // Elite durability — never below 75% damage output
                        { AbilityType.CongreveBarrage,    60 }, // High damage offset by randomness and friendly fire risk
                        { AbilityType.Brace,              30 }, // Niche anti-cavalry — strong vs cavalry, useless otherwise
                        { AbilityType.Dismount,          110 }, // Significant permanent stat swing, dual-mode versatility
                    },

                    // Formation values (base-100)
                    FormationValues = new Dictionary<FormationType, int>
                    {
                        { FormationType.None,            0 },
                        { FormationType.BattleLine,     15 }, // +15% DEF, requires 3+ in row
                        { FormationType.Battery,        15 }, // +20% ATK, requires 2+ adjacent
                        { FormationType.CavalryWedge,   15 }, // +25% charge damage, diagonal pattern
                        { FormationType.Square,         10 }, // +30% DEF vs cav, no flank — needs 4 in 2x2, situational
                        { FormationType.SkirmishScreen,  20 }, // +20% ATK, +1 RNG — just be isolated, very strong
                    },

                    // Trait adjustments (base-100)
                    ArtilleryExemptBonus     = 25,  // Always full damage regardless of HP
                    ReducedRearFlankBonus    = 20,  // Less vulnerable from behind
                    IncreasedRearFlankPenalty = 20,  // More vulnerable from behind
                    DefaultRearFlankMultiplier = 200,

                    // Cost bounds
                    MinCost = 1,
                    MaxCost = 15,

                    // No manual overrides by default — algorithm handles everything
                    CostOverrides = new Dictionary<UnitType, int>()
                };

                return config;
            }
        }
    }
}
