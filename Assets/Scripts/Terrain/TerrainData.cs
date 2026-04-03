using System;

namespace WarChess.Terrain
{
    /// <summary>
    /// Terrain types from GDD Section 4.1. Each tile on the grid has a terrain type.
    /// </summary>
    public enum TerrainType
    {
        OpenField,
        Forest,
        Hill,
        River,
        Bridge,
        Fortification,
        Mud,
        Town
    }

    /// <summary>
    /// Static terrain data from the GDD. All multipliers are base-100 integers.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class TerrainData
    {
        /// <summary>Movement cost for entering a terrain tile (1 = normal).</summary>
        public static int GetMovementCost(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.OpenField => 1,
                TerrainType.Forest => 2,
                TerrainType.Hill => 2,
                TerrainType.River => 3,
                TerrainType.Bridge => 1,
                TerrainType.Fortification => 1,
                TerrainType.Mud => 2,
                TerrainType.Town => 1,
                _ => 1
            };
        }

        /// <summary>
        /// Defense multiplier for a unit on this terrain (base 100).
        /// Lower = less damage taken. 100 = no modifier.
        /// </summary>
        public static int GetDefenseMultiplier(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Forest => 75,         // 25% less damage taken
                TerrainType.Fortification => 70,   // 30% less damage taken
                TerrainType.Town => 80,            // 20% less damage taken
                _ => 100
            };
        }

        /// <summary>
        /// Attack multiplier for a ranged unit on this terrain (base 100).
        /// Higher = more damage dealt. 100 = no modifier.
        /// </summary>
        public static int GetAttackMultiplier(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Hill => 125, // 25% more ranged damage dealt
                _ => 100
            };
        }

        /// <summary>
        /// Extra range for ranged units on this terrain.
        /// </summary>
        public static int GetRangeBonus(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Hill => 1,
                _ => 0
            };
        }

        /// <summary>
        /// Whether this terrain blocks line of sight for ranged attacks.
        /// </summary>
        public static bool BlocksLineOfSight(TerrainType terrain)
        {
            return terrain == TerrainType.Forest || terrain == TerrainType.Town;
        }

        /// <summary>
        /// Whether this terrain blocks cavalry charge bonus.
        /// </summary>
        public static bool BlocksCharge(TerrainType terrain)
        {
            return terrain == TerrainType.Fortification || terrain == TerrainType.Mud;
        }

        /// <summary>
        /// Whether a unit crossing this terrain cannot attack on the same round.
        /// </summary>
        public static bool PreventsAttackOnCross(TerrainType terrain)
        {
            return terrain == TerrainType.River;
        }
    }
}
