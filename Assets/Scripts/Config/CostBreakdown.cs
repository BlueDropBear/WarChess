namespace WarChess.Config
{
    /// <summary>
    /// Diagnostic breakdown of a unit's cost calculation. All component values are
    /// base-100 (100 = 1.0 cost point) except FinalCost which is the rounded integer.
    /// Used for balance debugging, QA reporting, and designer review.
    /// </summary>
    public readonly struct CostBreakdown
    {
        /// <summary>Unit type name for identification.</summary>
        public readonly string UnitName;

        /// <summary>
        /// Base cost from stats alone (base-100), anchored to reference unit.
        /// Before ability multiplier is applied.
        /// </summary>
        public readonly int StatScore;

        /// <summary>
        /// Stat score after ability multiplier is applied (base-100).
        /// = StatScore * AbilityMultiplier / 100
        /// </summary>
        public readonly int AdjustedStatScore;

        /// <summary>Ability multiplier that was applied (base-100, 100 = no change).</summary>
        public readonly int AbilityMultiplier;

        /// <summary>Flat ability value added after multiplier (base-100).</summary>
        public readonly int AbilityFlatValue;

        /// <summary>Value from formation access (base-100).</summary>
        public readonly int FormationValue;

        /// <summary>
        /// Adjustment from special traits: artillery exemption, flanking overrides (base-100).
        /// Positive = bonus, negative = penalty.
        /// </summary>
        public readonly int TraitAdjustment;

        /// <summary>Sum of all components before rounding (base-100).</summary>
        public readonly int RawTotal;

        /// <summary>Final rounded and clamped cost (integer army points).</summary>
        public readonly int FinalCost;

        /// <summary>Whether a manual override was applied instead of the algorithm.</summary>
        public readonly bool IsOverridden;

        public CostBreakdown(
            string unitName,
            int statScore,
            int adjustedStatScore,
            int abilityMultiplier,
            int abilityFlatValue,
            int formationValue,
            int traitAdjustment,
            int rawTotal,
            int finalCost,
            bool isOverridden)
        {
            UnitName = unitName;
            StatScore = statScore;
            AdjustedStatScore = adjustedStatScore;
            AbilityMultiplier = abilityMultiplier;
            AbilityFlatValue = abilityFlatValue;
            FormationValue = formationValue;
            TraitAdjustment = traitAdjustment;
            RawTotal = rawTotal;
            FinalCost = finalCost;
            IsOverridden = isOverridden;
        }

        /// <summary>
        /// Returns a human-readable breakdown string for logging and QA reports.
        /// </summary>
        public override string ToString()
        {
            string overrideTag = IsOverridden ? " [OVERRIDE]" : "";
            return $"{UnitName}{overrideTag}: " +
                   $"stat={StatScore / 100.0:F2} " +
                   $"x{AbilityMultiplier / 100.0:F2}={AdjustedStatScore / 100.0:F2} " +
                   $"+ability={AbilityFlatValue / 100.0:F2} " +
                   $"+form={FormationValue / 100.0:F2} " +
                   $"+trait={TraitAdjustment / 100.0:F2} " +
                   $"= {RawTotal / 100.0:F2} -> {FinalCost}";
        }
    }
}
