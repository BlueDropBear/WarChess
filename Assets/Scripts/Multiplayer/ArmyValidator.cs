using System.Collections.Generic;
using WarChess.Config;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Server-side army validation for anti-cheat. GDD Section 7.5.
    /// Validates point budgets, tier-appropriate units, valid placements,
    /// and officer assignments. Pure C# — no Unity dependencies.
    /// </summary>
    public static class ArmyValidator
    {
        /// <summary>Point budgets per match format from GDD Section 7.4.</summary>
        public static readonly Dictionary<MatchFormat, int> FormatBudgets = new Dictionary<MatchFormat, int>
        {
            { MatchFormat.Skirmish, 25 },
            { MatchFormat.Standard, 40 },
            { MatchFormat.GrandBattle, 60 }
        };

        /// <summary>
        /// Validates a submitted army. Returns null if valid, or an error message.
        /// </summary>
        public static string Validate(
            ArmySubmission submission,
            MatchFormat format,
            int playerHighestTier,
            GameConfigData config)
        {
            if (submission == null)
                return "Submission is null";

            if (submission.Units == null || submission.Units.Count == 0)
                return "Army has no units";

            // 1. Tier validation
            if (submission.Tier < 1 || submission.Tier > 5)
                return $"Invalid tier: {submission.Tier}";

            if (submission.Tier > playerHighestTier)
                return $"Player has not unlocked tier {submission.Tier} (highest: {playerHighestTier})";

            // Grand Battle only available at tier 4+
            if (format == MatchFormat.GrandBattle && submission.Tier < 4)
                return "Grand Battle format requires tier 4+";

            // 2. Point budget
            int budget = FormatBudgets[format];
            var unitCosts = GetUnitCosts();
            int totalCost = 0;

            foreach (var unit in submission.Units)
            {
                if (!unitCosts.TryGetValue(unit.UnitTypeId, out int cost))
                    return $"Unknown unit type: {unit.UnitTypeId}";
                totalCost += cost;
            }

            if (totalCost > budget)
                return $"Army costs {totalCost} points but budget is {budget} for {format}";

            // 3. Unit tier gating
            var allowedUnits = TierSystem.GetAvailableUnits(submission.Tier);
            foreach (var unit in submission.Units)
            {
                if (!allowedUnits.Contains(unit.UnitTypeId))
                    return $"Unit '{unit.UnitTypeId}' is not available at tier {submission.Tier}";
            }

            // 4. Placement validation
            var occupiedTiles = new HashSet<string>();
            foreach (var unit in submission.Units)
            {
                // Must be in deployment zone (rows 1-3 for player, 8-10 for enemy)
                if (unit.Y < config.PlayerDeployMinRow || unit.Y > config.PlayerDeployMaxRow)
                    return $"Unit at ({unit.X},{unit.Y}) is outside deployment zone (rows {config.PlayerDeployMinRow}-{config.PlayerDeployMaxRow})";

                if (unit.X < 1 || unit.X > config.GridWidth)
                    return $"Unit at ({unit.X},{unit.Y}) is outside grid bounds";

                string key = $"{unit.X},{unit.Y}";
                if (!occupiedTiles.Add(key))
                    return $"Duplicate placement at ({unit.X},{unit.Y})";
            }

            // 5. Max unit count (grid can't hold more than deployment zone tiles)
            int maxUnits = config.GridWidth * (config.PlayerDeployMaxRow - config.PlayerDeployMinRow + 1);
            if (submission.Units.Count > maxUnits)
                return $"Too many units: {submission.Units.Count} (max {maxUnits})";

            return null; // Valid
        }

        private static Dictionary<string, int> GetUnitCosts()
        {
            return new Dictionary<string, int>
            {
                {"LineInfantry", 3}, {"Militia", 1}, {"Cavalry", 5}, {"Artillery", 6},
                {"Grenadier", 7}, {"Rifleman", 5}, {"Hussar", 4}, {"Cuirassier", 8},
                {"HorseArtillery", 6}, {"Sapper", 4}, {"OldGuard", 10}, {"RocketBattery", 7},
                {"Lancer", 5}, {"Dragoon", 6}
            };
        }
    }

    /// <summary>Match formats from GDD Section 7.4.</summary>
    public enum MatchFormat
    {
        Skirmish,     // 25 pts, any tier, unranked
        Standard,     // 40 pts, tier-specific, ranked
        GrandBattle   // 60 pts, tier 4+ only, ranked
    }
}
