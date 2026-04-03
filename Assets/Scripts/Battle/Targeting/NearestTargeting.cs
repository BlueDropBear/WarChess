using System.Collections.Generic;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle.Targeting
{
    /// <summary>
    /// Selects the closest enemy by Manhattan distance. Tie-break: lowest Id.
    /// Used by: Line Infantry, Grenadier, Militia, Artillery, Horse Artillery, Sapper, Old Guard.
    /// </summary>
    public class NearestTargeting : ITargetingStrategy
    {
        public static readonly NearestTargeting Instance = new NearestTargeting();

        public UnitInstance SelectTarget(UnitInstance attacker, IReadOnlyList<UnitInstance> enemies, GridMap grid)
        {
            UnitInstance best = null;
            int bestDist = int.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy.IsAlive) continue;

                int dist = attacker.Position.ManhattanDistance(enemy.Position);
                if (dist < bestDist || (dist == bestDist && (best == null || enemy.Id < best.Id)))
                {
                    best = enemy;
                    bestDist = dist;
                }
            }

            return best;
        }
    }
}
