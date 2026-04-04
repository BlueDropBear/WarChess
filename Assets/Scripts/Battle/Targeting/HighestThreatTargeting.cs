using System.Collections.Generic;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle.Targeting
{
    /// <summary>
    /// Selects the enemy with highest ATK stat. Tie-break: nearest.
    /// Used by: Cavalry, Cuirassier, Lancer.
    /// </summary>
    public class HighestThreatTargeting : ITargetingStrategy
    {
        public static readonly HighestThreatTargeting Instance = new HighestThreatTargeting();

        public UnitInstance SelectTarget(UnitInstance attacker, IReadOnlyList<UnitInstance> enemies, GridMap grid)
        {
            UnitInstance best = null;
            int bestAtk = -1;
            int bestDist = int.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy.IsAlive) continue;

                int atk = enemy.Atk;
                int dist = attacker.Position.ManhattanDistance(enemy.Position);

                if (atk > bestAtk || (atk == bestAtk && dist < bestDist)
                    || (atk == bestAtk && dist == bestDist && (best == null || enemy.Id < best.Id)))
                {
                    best = enemy;
                    bestAtk = atk;
                    bestDist = dist;
                }
            }

            return best;
        }
    }
}
