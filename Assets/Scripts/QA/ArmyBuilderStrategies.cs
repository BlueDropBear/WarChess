using System;
using System.Collections.Generic;
using System.Linq;
using WarChess.Config;

namespace WarChess.QA
{
    /// <summary>
    /// Four army builder strategies for QA testing, per GDD Section 12.3.
    /// Used by BalanceTester to generate varied army compositions.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class ArmyBuilderStrategies
    {
        private readonly GameConfigData _config;
        private readonly Random _rng;

        public ArmyBuilderStrategies(GameConfigData config, Random seedGen)
        {
            _config = config;
            _rng = seedGen;
        }

        /// <summary>
        /// Strategy 1: Random — picks units at random until budget is exhausted.
        /// </summary>
        public List<string> BuildRandom(int budget, string[] available)
        {
            var army = new List<string>();
            var costs = GameConfigData.GetUnitCosts();
            int remaining = budget;
            int maxAttempts = 100;

            while (remaining > 0 && maxAttempts-- > 0)
            {
                string unit = available[_rng.Next(available.Length)];
                int cost = costs.TryGetValue(unit, out int c) ? c : 99;
                if (cost <= remaining && army.Count < 15)
                {
                    army.Add(unit);
                    remaining -= cost;
                }
            }

            return army;
        }

        /// <summary>
        /// Strategy 2: Archetype-based — builds armies around a core theme.
        /// </summary>
        public List<string> BuildArchetype(int budget, string[] available, ArmyArchetype archetype)
        {
            var costs = GameConfigData.GetUnitCosts();
            var availableSet = new HashSet<string>(available);

            // Define unit priorities per archetype
            string[] primary;
            string[] secondary;

            switch (archetype)
            {
                case ArmyArchetype.CavalryRush:
                    primary = new[] { "Cavalry", "Hussar", "Cuirassier", "Lancer" };
                    secondary = new[] { "Dragoon", "HorseArtillery", "LineInfantry" };
                    break;
                case ArmyArchetype.ArtilleryFort:
                    primary = new[] { "Artillery", "RocketBattery", "HorseArtillery" };
                    secondary = new[] { "Sapper", "LineInfantry", "Grenadier" };
                    break;
                case ArmyArchetype.BalancedLine:
                    primary = new[] { "LineInfantry", "Rifleman", "Cavalry" };
                    secondary = new[] { "Artillery", "Grenadier", "Hussar", "Sapper" };
                    break;
                case ArmyArchetype.InfantryWall:
                    primary = new[] { "Grenadier", "OldGuard", "LineInfantry" };
                    secondary = new[] { "Sapper", "Rifleman", "Artillery" };
                    break;
                default:
                    return BuildRandom(budget, available);
            }

            var army = new List<string>();
            int remaining = budget;

            // Fill 60-70% of budget with primary units
            int primaryBudget = (budget * 65) / 100;
            remaining = FillFromPool(army, primary, availableSet, costs, remaining, primaryBudget);

            // Fill the rest with secondary units
            remaining = FillFromPool(army, secondary, availableSet, costs, remaining, remaining);

            // If budget remains, add any affordable unit
            FillFromPool(army, available, availableSet, costs, remaining, remaining);

            return army;
        }

        /// <summary>
        /// Strategy 3: Counter-picking — given an opponent army, build the best counter.
        /// Uses matchup data to pick units that perform well against the opponent's composition.
        /// </summary>
        public List<string> BuildCounter(int budget, string[] available,
            List<string> opponentArmy, MatchupData matchupData)
        {
            var costs = GameConfigData.GetUnitCosts();
            var availableSet = new HashSet<string>(available);

            // Score each available unit by its average win rate against opponent units
            var unitScores = new Dictionary<string, int>();
            foreach (var unitType in available)
            {
                int totalScore = 0;
                int count = 0;
                int unitIdx = Array.IndexOf(BalanceTester.AllUnitTypes, unitType);
                if (unitIdx < 0) continue;

                foreach (var oppUnit in opponentArmy)
                {
                    int oppIdx = Array.IndexOf(BalanceTester.AllUnitTypes, oppUnit);
                    if (oppIdx < 0) continue;
                    totalScore += matchupData.Matrix[unitIdx, oppIdx];
                    count++;
                }

                unitScores[unitType] = count > 0 ? totalScore / count : 50;
            }

            // Sort by score descending
            var ranked = unitScores.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray();

            var army = new List<string>();
            int remaining = budget;

            // Greedily pick highest-scoring units
            foreach (var unitType in ranked)
            {
                if (!availableSet.Contains(unitType)) continue;
                int cost = costs.TryGetValue(unitType, out int c) ? c : 99;

                while (cost <= remaining && army.Count < 15)
                {
                    army.Add(unitType);
                    remaining -= cost;
                }
            }

            return army;
        }

        /// <summary>
        /// Strategy 4: Meta-gaming — uses historical win-rate data to pick
        /// units that appear most frequently in winning armies.
        /// </summary>
        public List<string> BuildMeta(int budget, string[] available,
            Dictionary<string, int> unitWinRates)
        {
            var costs = GameConfigData.GetUnitCosts();
            var availableSet = new HashSet<string>(available);

            // Rank units by historical win rate
            var ranked = available
                .Where(u => availableSet.Contains(u))
                .OrderByDescending(u => unitWinRates.TryGetValue(u, out int wr) ? wr : 50)
                .ToArray();

            var army = new List<string>();
            int remaining = budget;

            // Build army from highest-rated units, but with some variety
            // Take 2 of each top unit max to avoid mono-armies
            foreach (var unitType in ranked)
            {
                int cost = costs.TryGetValue(unitType, out int c) ? c : 99;
                int added = 0;

                while (cost <= remaining && army.Count < 15 && added < 2)
                {
                    army.Add(unitType);
                    remaining -= cost;
                    added++;
                }
            }

            // Fill remaining budget with random affordable units
            int attempts = 50;
            while (remaining > 0 && army.Count < 15 && attempts-- > 0)
            {
                string unit = ranked[_rng.Next(ranked.Length)];
                int cost = costs.TryGetValue(unit, out int c) ? c : 99;
                if (cost <= remaining)
                {
                    army.Add(unit);
                    remaining -= cost;
                }
            }

            return army;
        }

        private int FillFromPool(List<string> army, string[] pool, HashSet<string> availableSet,
            Dictionary<string, int> costs, int remaining, int maxSpend)
        {
            int spent = 0;
            int attempts = 50;

            while (spent < maxSpend && remaining > 0 && army.Count < 15 && attempts-- > 0)
            {
                string unit = pool[_rng.Next(pool.Length)];
                if (!availableSet.Contains(unit)) continue;
                int cost = costs.TryGetValue(unit, out int c) ? c : 99;
                if (cost <= remaining)
                {
                    army.Add(unit);
                    remaining -= cost;
                    spent += cost;
                }
            }

            return remaining;
        }
    }

    /// <summary>
    /// Army archetype templates for Strategy 2.
    /// </summary>
    public enum ArmyArchetype
    {
        CavalryRush,
        ArtilleryFort,
        BalancedLine,
        InfantryWall
    }

    /// <summary>
    /// Pre-computed matchup data: unit A win% vs unit B.
    /// Indexed by AllUnitTypes order.
    /// </summary>
    public class MatchupData
    {
        /// <summary>14x14 win rate matrix (0-100). Row beats column at matrix[row,col]%.</summary>
        public int[,] Matrix;
    }
}
