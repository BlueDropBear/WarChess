using System.Collections.Generic;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle.Targeting
{
    /// <summary>
    /// Prioritizes enemies with highest RNG (ranged/artillery units).
    /// Tie-break: nearest. Falls back to Nearest if no ranged enemies exist.
    /// Used by: Dragoon.
    /// </summary>
    public class ArtilleryFirstTargeting : ITargetingStrategy
    {
        public static readonly ArtilleryFirstTargeting Instance = new ArtilleryFirstTargeting();

        public UnitInstance SelectTarget(UnitInstance attacker, IReadOnlyList<UnitInstance> enemies, GridMap grid)
        {
            UnitInstance best = null;
            int bestRng = -1;
            int bestDist = int.MaxValue;
            bool foundRanged = false;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy.IsAlive) continue;

                // Only consider ranged units (RNG > 1) in priority pass
                if (enemy.Rng > 1)
                {
                    foundRanged = true;
                    int dist = attacker.Position.ManhattanDistance(enemy.Position);

                    if (enemy.Rng > bestRng || (enemy.Rng == bestRng && dist < bestDist)
                        || (enemy.Rng == bestRng && dist == bestDist && enemy.Id < best.Id))
                    {
                        best = enemy;
                        bestRng = enemy.Rng;
                        bestDist = dist;
                    }
                }
            }

            // Fall back to nearest if no ranged enemies
            if (!foundRanged)
            {
                return NearestTargeting.Instance.SelectTarget(attacker, enemies, grid);
            }

            return best;
        }
    }
}
