using System.Collections.Generic;
using WarChess.Units;

namespace WarChess.Config
{
    /// <summary>
    /// Difficulty levels from GDD Section 6.6.
    /// </summary>
    public enum Difficulty
    {
        Recruit = 0,   // -15% enemy stats, basic AI, full info shown
        Veteran = 1,   // Normal stats, standard AI, placement hidden
        Marshal = 2    // +15% enemy stats, advanced AI, only unit count shown
    }

    /// <summary>
    /// Applies difficulty scaling to enemy units. Pure C# — no Unity dependencies.
    /// GDD Section 6.6: Recruit = -15%, Veteran = normal, Marshal = +15%.
    /// </summary>
    public static class DifficultyScaler
    {
        /// <summary>
        /// Returns the stat multiplier (base 100) for the given difficulty.
        /// </summary>
        public static int GetStatMultiplier(Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.Recruit => 85,   // -15%
                Difficulty.Veteran => 100,  // Normal
                Difficulty.Marshal => 115,  // +15%
                _ => 100
            };
        }

        /// <summary>
        /// Applies difficulty scaling to a list of enemy units.
        /// Modifies HP, ATK, and DEF. Should be called after creating enemy
        /// UnitInstances but before the battle starts.
        /// </summary>
        public static void ApplyDifficulty(List<UnitInstance> enemyUnits, Difficulty difficulty)
        {
            if (difficulty == Difficulty.Veteran) return; // No changes needed

            int multiplier = GetStatMultiplier(difficulty);

            foreach (var unit in enemyUnits)
            {
                // Scale HP, ATK, DEF by the multiplier
                int scaledHp = (unit.MaxHp * multiplier) / 100;
                int scaledAtk = (unit.Atk * multiplier) / 100;
                int scaledDef = (unit.Def * multiplier) / 100;

                // ApplyStatScale sets ATK, DEF, MaxHp and adjusts CurrentHp proportionally
                unit.ApplyStatScale(scaledAtk, scaledDef, scaledHp);
            }
        }

        /// <summary>
        /// Returns the info visibility level for the given difficulty.
        /// 0 = full enemy visible, 1 = types visible/placement hidden, 2 = only count visible.
        /// </summary>
        public static int GetInfoVisibility(Difficulty difficulty)
        {
            return (int)difficulty;
        }
    }
}
