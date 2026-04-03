using System;
using System.Collections.Generic;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Star General tier system from GDD Section 7.1.
    /// 5 tiers gate unit availability in multiplayer.
    /// Each tier has independent Elo and win count tracking.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class TierSystem
    {
        public static readonly TierData[] Tiers = new[]
        {
            new TierData
            {
                Tier = 1, Name = "Brigadier", Badge = "Bronze",
                WinsRequired = 0,
                AvailableUnits = new[] { "LineInfantry", "Militia", "Cavalry", "Artillery" }
            },
            new TierData
            {
                Tier = 2, Name = "Major General", Badge = "Silver",
                WinsRequired = 10,
                AvailableUnits = new[] { "LineInfantry", "Militia", "Cavalry", "Artillery", "Grenadier", "Rifleman" }
            },
            new TierData
            {
                Tier = 3, Name = "Lieutenant General", Badge = "Gold",
                WinsRequired = 20,
                AvailableUnits = new[] { "LineInfantry", "Militia", "Cavalry", "Artillery", "Grenadier", "Rifleman",
                                         "Hussar", "Cuirassier", "HorseArtillery" }
            },
            new TierData
            {
                Tier = 4, Name = "General", Badge = "Platinum",
                WinsRequired = 30,
                AvailableUnits = new[] { "LineInfantry", "Militia", "Cavalry", "Artillery", "Grenadier", "Rifleman",
                                         "Hussar", "Cuirassier", "HorseArtillery", "Sapper", "Lancer", "Dragoon" }
            },
            new TierData
            {
                Tier = 5, Name = "Marshal of the Empire", Badge = "Diamond",
                WinsRequired = 50,
                AvailableUnits = new[] { "LineInfantry", "Militia", "Cavalry", "Artillery", "Grenadier", "Rifleman",
                                         "Hussar", "Cuirassier", "HorseArtillery", "Sapper", "Lancer", "Dragoon",
                                         "OldGuard", "RocketBattery" }
            }
        };

        /// <summary>Returns tier data by tier number (1-5).</summary>
        public static TierData GetTier(int tier)
        {
            if (tier < 1) return Tiers[0];
            if (tier > Tiers.Length) return Tiers[Tiers.Length - 1];
            return Tiers[tier - 1];
        }

        /// <summary>Returns the set of units available at a given tier.</summary>
        public static HashSet<string> GetAvailableUnits(int tier)
        {
            return new HashSet<string>(GetTier(tier).AvailableUnits);
        }

        /// <summary>
        /// Returns true if the unit type is allowed at this tier.
        /// </summary>
        public static bool IsUnitAllowedAtTier(string unitTypeId, int tier)
        {
            return GetAvailableUnits(tier).Contains(unitTypeId);
        }

        /// <summary>
        /// Returns the highest tier the player has unlocked based on wins at each tier.
        /// </summary>
        public static int GetHighestUnlockedTier(Dictionary<int, int> winsPerTier)
        {
            int highest = 1;
            for (int t = 2; t <= Tiers.Length; t++)
            {
                int previousTier = t - 1;
                int winsNeeded = Tiers[t - 1].WinsRequired;
                int winsAtPrevious = winsPerTier.TryGetValue(previousTier, out int w) ? w : 0;

                if (winsAtPrevious >= winsNeeded)
                    highest = t;
                else
                    break;
            }
            return highest;
        }
    }

    [Serializable]
    public class TierData
    {
        public int Tier;
        public string Name;
        public string Badge;
        public int WinsRequired;
        public string[] AvailableUnits;
    }
}
