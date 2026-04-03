using System;
using System.Collections.Generic;

namespace WarChess.Officers
{
    /// <summary>
    /// Officer IDs matching GDD Section 2.9.
    /// </summary>
    public enum OfficerId
    {
        VeteranSergeant,
        YoungLieutenant,
        Drillmaster,
        Sharpshooter,
        FearlessMajor,
        CautiousColonel,
        RecklessCaptain,
        SiegeExpert,
        ScoutMaster,
        RallyOfficer,
        Ironside,
        PowderMonkey
    }

    /// <summary>
    /// Static data for an officer's positive and negative traits.
    /// All multiplier values are base-100 at Level 1. Each level adds 10% of base.
    /// </summary>
    public class OfficerData
    {
        public OfficerId Id;
        public string Name;
        public string PositiveTrait;
        public string NegativeTrait;
        public string BestOn;

        // Positive trait values (base 100 where applicable)
        public OfficerStatMod PositiveMod;

        // Negative trait values
        public OfficerStatMod NegativeMod;

        /// <summary>Optional second negative mod for officers with compound penalties (e.g., Scout Master: -10% ATK and -10% DEF).</summary>
        public OfficerStatMod NegativeMod2;
    }

    /// <summary>
    /// A stat modification from an officer trait. Can modify ATK, DEF, HP, MOV, RNG, SPD
    /// as either a multiplier (base 100) or a flat additive value.
    /// </summary>
    public class OfficerStatMod
    {
        public OfficerModType Type;
        public int Value; // Base value at Level 1

        /// <summary>
        /// Returns the scaled value at the given level.
        /// Each level adds 10% of the base value.
        /// Level 1 = base, Level 2 = base * 1.1, Level 3 = base * 1.2, etc.
        /// </summary>
        public int GetValueAtLevel(int level)
        {
            if (level <= 1) return Value;
            // Scale: value * (100 + (level-1) * 10) / 100
            return (Value * (100 + (level - 1) * 10)) / 100;
        }
    }

    /// <summary>
    /// Types of stat modification an officer trait can apply.
    /// </summary>
    public enum OfficerModType
    {
        AtkMultiplier,      // Base 100: 120 = +20% ATK
        DefMultiplier,      // Base 100: 125 = +25% DEF
        HpMultiplier,       // Base 100: 85 = -15% HP
        MovFlat,            // Flat: +2 or -1 MOV
        RngFlat,            // Flat: +1 RNG
        SpdFlat,            // Flat: -1 SPD
        ChargeDamageMultiplier, // Base 100: 140 = +40% charge damage
        FlankDamageReceived,    // Base 100: 50 = -50% flanking damage
        AoERadiusMultiplier,    // Base 100: 125 = +25% AoE radius
        AdjacentAllyAtkBonus,   // Flat: +10% ATK to adjacent allies
        IgnoresMorale,          // Boolean (value ignored)
        OverrideTargeting,      // Forces nearest targeting
        MaxRowLimit,            // Will not advance past this row
        FriendlyFireChance,     // Base 100: 15 = 15% friendly fire
        LowHpDefBonus,         // Base 100: 130 = +30% DEF when HP < 50%
        RevealRange,            // Flat: reveal hidden units within N tiles
    }

    /// <summary>
    /// Database of all 12 officers from GDD Section 2.9.
    /// </summary>
    public static class OfficerDatabase
    {
        private static Dictionary<OfficerId, OfficerData> _officers;

        public static IReadOnlyDictionary<OfficerId, OfficerData> All
        {
            get
            {
                if (_officers == null) Build();
                return _officers;
            }
        }

        public static OfficerData Get(OfficerId id)
        {
            var all = All;
            return all.TryGetValue(id, out var data) ? data : null;
        }

        private static void Build()
        {
            _officers = new Dictionary<OfficerId, OfficerData>
            {
                {
                    OfficerId.VeteranSergeant, new OfficerData
                    {
                        Id = OfficerId.VeteranSergeant,
                        Name = "Veteran Sergeant",
                        PositiveTrait = "+20% ATK",
                        NegativeTrait = "-1 MOV",
                        BestOn = "Slow units (Infantry, Artillery)",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.AtkMultiplier, Value = 120 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.MovFlat, Value = -1 }
                    }
                },
                {
                    OfficerId.YoungLieutenant, new OfficerData
                    {
                        Id = OfficerId.YoungLieutenant,
                        Name = "Young Lieutenant",
                        PositiveTrait = "+2 MOV",
                        NegativeTrait = "-15% DEF",
                        BestOn = "Fast units needing more reach",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.MovFlat, Value = 2 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.DefMultiplier, Value = 85 }
                    }
                },
                {
                    OfficerId.Drillmaster, new OfficerData
                    {
                        Id = OfficerId.Drillmaster,
                        Name = "Drillmaster",
                        PositiveTrait = "+25% DEF",
                        NegativeTrait = "-20% ATK",
                        BestOn = "Tanks holding a position",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.DefMultiplier, Value = 125 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.AtkMultiplier, Value = 80 }
                    }
                },
                {
                    OfficerId.Sharpshooter, new OfficerData
                    {
                        Id = OfficerId.Sharpshooter,
                        Name = "Sharpshooter",
                        PositiveTrait = "+1 RNG",
                        NegativeTrait = "-15% HP",
                        BestOn = "Ranged units wanting more reach",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.RngFlat, Value = 1 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.HpMultiplier, Value = 85 }
                    }
                },
                {
                    OfficerId.FearlessMajor, new OfficerData
                    {
                        Id = OfficerId.FearlessMajor,
                        Name = "Fearless Major",
                        PositiveTrait = "Immune to morale effects, +10% ATK when flanked",
                        NegativeTrait = "Always targets nearest (overrides unit AI)",
                        BestOn = "Aggressive front-line units",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.IgnoresMorale, Value = 1 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.OverrideTargeting, Value = 1 }
                    }
                },
                {
                    OfficerId.CautiousColonel, new OfficerData
                    {
                        Id = OfficerId.CautiousColonel,
                        Name = "Cautious Colonel",
                        PositiveTrait = "+30% DEF when HP below 50%",
                        NegativeTrait = "Will not advance past row 5",
                        BestOn = "Defensive line holders",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.LowHpDefBonus, Value = 130 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.MaxRowLimit, Value = 5 }
                    }
                },
                {
                    OfficerId.RecklessCaptain, new OfficerData
                    {
                        Id = OfficerId.RecklessCaptain,
                        Name = "Reckless Captain",
                        PositiveTrait = "+40% Charge damage",
                        NegativeTrait = "Takes +25% damage from all sources",
                        BestOn = "Cavalry glass cannon builds",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.ChargeDamageMultiplier, Value = 140 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.DefMultiplier, Value = 80 }
                    }
                },
                {
                    OfficerId.SiegeExpert, new OfficerData
                    {
                        Id = OfficerId.SiegeExpert,
                        Name = "Siege Expert",
                        PositiveTrait = "+30% ATK vs units in Fortifications",
                        NegativeTrait = "-2 MOV (minimum 1)",
                        BestOn = "Artillery and Grenadiers",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.AtkMultiplier, Value = 130 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.MovFlat, Value = -2 }
                    }
                },
                {
                    OfficerId.ScoutMaster, new OfficerData
                    {
                        Id = OfficerId.ScoutMaster,
                        Name = "Scout Master",
                        PositiveTrait = "Reveals hidden enemies within 3 tiles",
                        NegativeTrait = "-10% ATK, -10% DEF",
                        BestOn = "Hussars and light cavalry",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.RevealRange, Value = 3 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.AtkMultiplier, Value = 90 },
                        NegativeMod2 = new OfficerStatMod { Type = OfficerModType.DefMultiplier, Value = 90 }
                    }
                },
                {
                    OfficerId.RallyOfficer, new OfficerData
                    {
                        Id = OfficerId.RallyOfficer,
                        Name = "Rally Officer",
                        PositiveTrait = "Adjacent friendly units gain +10% ATK",
                        NegativeTrait = "Officer's unit has -20% HP",
                        BestOn = "Support units in formation",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.AdjacentAllyAtkBonus, Value = 10 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.HpMultiplier, Value = 80 }
                    }
                },
                {
                    OfficerId.Ironside, new OfficerData
                    {
                        Id = OfficerId.Ironside,
                        Name = "Ironside",
                        PositiveTrait = "-50% flanking damage taken (side and rear)",
                        NegativeTrait = "-1 SPD (acts later in initiative)",
                        BestOn = "High-value targets needing protection",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.FlankDamageReceived, Value = 50 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.SpdFlat, Value = -1 }
                    }
                },
                {
                    OfficerId.PowderMonkey, new OfficerData
                    {
                        Id = OfficerId.PowderMonkey,
                        Name = "Powder Monkey",
                        PositiveTrait = "+25% AoE radius (Artillery/Rocket Battery)",
                        NegativeTrait = "+15% chance of friendly fire splash",
                        BestOn = "Artillery that wants maximum coverage",
                        PositiveMod = new OfficerStatMod { Type = OfficerModType.AoERadiusMultiplier, Value = 125 },
                        NegativeMod = new OfficerStatMod { Type = OfficerModType.FriendlyFireChance, Value = 15 }
                    }
                }
            };
        }
    }
}
