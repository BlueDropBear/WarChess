using System.Collections.Generic;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle.Targeting
{
    /// <summary>
    /// Selects the enemy with lowest current HP. Tie-break: nearest.
    /// Used by: Rifleman, Hussar.
    /// </summary>
    public class WeakestTargeting : ITargetingStrategy
    {
        public static readonly WeakestTargeting Instance = new WeakestTargeting();

        public UnitInstance SelectTarget(UnitInstance attacker, IReadOnlyList<UnitInstance> enemies, GridMap grid)
        {
            UnitInstance best = null;
            int bestHp = int.MaxValue;
            int bestDist = int.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (!enemy.IsAlive) continue;

                int hp = enemy.CurrentHp;
                int dist = attacker.Position.ManhattanDistance(enemy.Position);

                if (hp < bestHp || (hp == bestHp && dist < bestDist)
                    || (hp == bestHp && dist == bestDist && enemy.Id < best.Id))
                {
                    best = enemy;
                    bestHp = hp;
                    bestDist = dist;
                }
            }

            return best;
        }
    }
}
