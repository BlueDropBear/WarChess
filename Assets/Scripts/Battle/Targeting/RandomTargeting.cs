using System.Collections.Generic;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle.Targeting
{
    /// <summary>
    /// Selects a random enemy using the seeded BattleRng. Deterministic given same seed.
    /// Used by: Rocket Battery.
    /// </summary>
    public class RandomTargeting : ITargetingStrategy
    {
        private readonly BattleRng _rng;

        public RandomTargeting(BattleRng rng)
        {
            _rng = rng;
        }

        public UnitInstance SelectTarget(UnitInstance attacker, IReadOnlyList<UnitInstance> enemies, GridMap grid)
        {
            // Build list of living enemies
            var living = new List<UnitInstance>();
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsAlive)
                    living.Add(enemies[i]);
            }

            if (living.Count == 0) return null;

            int index = _rng.Next(living.Count);
            return living[index];
        }
    }
}
