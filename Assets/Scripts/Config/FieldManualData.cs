using System;
using System.Collections.Generic;

namespace WarChess.Config
{
    /// <summary>
    /// Types of rewards that can appear in a Field Manual.
    /// </summary>
    public enum FieldManualRewardType
    {
        Cosmetic,
        Ammunition,
        Sovereigns,
        DispatchBox,
        BattleStarBooster
    }

    /// <summary>
    /// A single reward item within a Field Manual page.
    /// </summary>
    [Serializable]
    public class FieldManualReward
    {
        /// <summary>Type of this reward.</summary>
        public FieldManualRewardType Type;

        /// <summary>For Cosmetic type: the cosmetic ID to grant.</summary>
        public string CosmeticId;

        /// <summary>For Ammunition/Sovereigns: amount to grant. For BattleStarBooster: duration in hours.</summary>
        public int Amount;

        /// <summary>For DispatchBox type: the box tier.</summary>
        public DispatchBoxType BoxType;

        /// <summary>Whether this reward is on the premium track (requires manual purchase).</summary>
        public bool IsPremiumTrack;

        /// <summary>Battle Star cost to unlock this reward.</summary>
        public int StarCost;
    }

    /// <summary>
    /// A page (tier) within a Field Manual containing multiple rewards.
    /// </summary>
    [Serializable]
    public class FieldManualPage
    {
        /// <summary>Page number (1-based).</summary>
        public int PageNumber;

        /// <summary>Rewards on this page, unlocked sequentially.</summary>
        public List<FieldManualReward> Rewards;
    }

    /// <summary>
    /// Static data defining a Field Manual (Warbond equivalent).
    /// Field Manuals are themed content packs with free and premium tracks.
    /// They NEVER expire — once released, permanently available.
    /// </summary>
    [Serializable]
    public class FieldManualData
    {
        /// <summary>Unique identifier for this Field Manual.</summary>
        public string Id;

        /// <summary>Display name shown in UI.</summary>
        public string Name;

        /// <summary>Short description of the theme.</summary>
        public string Description;

        /// <summary>Historical campaign theme (e.g., "Egyptian Expedition", "Peninsular War").</summary>
        public string Theme;

        /// <summary>Cost in Sovereigns to unlock the premium track. 0 = free manual.</summary>
        public int PremiumCostSovereigns;

        /// <summary>Pages (tiers) in this manual, each containing sequential rewards.</summary>
        public List<FieldManualPage> Pages;

        /// <summary>Whether this is a free introductory manual (no premium track cost).</summary>
        public bool IsFree => PremiumCostSovereigns == 0;
    }

    /// <summary>
    /// Database of all Field Manuals. Follows the same pattern as CosmeticDatabase.
    /// </summary>
    public static class FieldManualDatabase
    {
        private static Dictionary<string, FieldManualData> _manuals;

        /// <summary>All Field Manuals keyed by ID.</summary>
        public static IReadOnlyDictionary<string, FieldManualData> All
        {
            get
            {
                if (_manuals == null) Build();
                return _manuals;
            }
        }

        /// <summary>Returns a Field Manual by ID, or null if not found.</summary>
        public static FieldManualData Get(string id)
        {
            var all = All;
            return ((Dictionary<string, FieldManualData>)all).TryGetValue(id, out var data) ? data : null;
        }

        /// <summary>Returns all available Field Manuals.</summary>
        public static List<FieldManualData> GetAll()
        {
            var result = new List<FieldManualData>();
            foreach (var kvp in All)
                result.Add(kvp.Value);
            return result;
        }

        private static void Build()
        {
            _manuals = new Dictionary<string, FieldManualData>();

            BuildMusteringTheTroops();
            BuildEgyptianExpedition();
            BuildPeninsularWar();
            BuildGrandeArmee();
        }

        /// <summary>
        /// Free introductory Field Manual — teaches players the system.
        /// </summary>
        private static void BuildMusteringTheTroops()
        {
            var manual = new FieldManualData
            {
                Id = "fm_mustering",
                Name = "Mustering the Troops",
                Description = "Your first orders have arrived. Learn the ways of the Field Manual.",
                Theme = "Tutorial",
                PremiumCostSovereigns = 0,
                Pages = new List<FieldManualPage>()
            };

            // Page 1 — introductory cosmetics
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 1,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("skin_infantry_redcoat", false, 5),
                    AmmunitionReward(5, false, 5),
                    CosmeticReward("banner_union_jack", false, 5)
                }
            });

            // Page 2 — a few more basics
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 2,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("grid_classic", false, 8),
                    AmmunitionReward(5, false, 8),
                    SovereignReward(10, false, 8)
                }
            });

            // Page 3 — nicer items
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 3,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("skin_infantry_bluecoat", false, 12),
                    CosmeticReward("banner_tricolour", false, 12),
                    AmmunitionReward(5, false, 12),
                    SovereignReward(15, false, 12)
                }
            });

            // Page 4 — capstone
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 4,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("victory_fireworks", false, 15),
                    CosmeticReward("skin_artillery_bronze", false, 15),
                    SovereignReward(25, false, 15)
                }
            });

            _manuals[manual.Id] = manual;
        }

        /// <summary>
        /// Premium Field Manual #1: The Egyptian Expedition (Month 1).
        /// Theme: Napoleon's 1798-1801 Egyptian Campaign.
        /// </summary>
        private static void BuildEgyptianExpedition()
        {
            var manual = new FieldManualData
            {
                Id = "fm_egypt",
                Name = "The Egyptian Expedition",
                Description = "Follow Bonaparte's daring campaign to the land of the Pharaohs.",
                Theme = "Egyptian Campaign 1798-1801",
                PremiumCostSovereigns = 1000,
                Pages = new List<FieldManualPage>()
            };

            // Page 1
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 1,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("skin_egypt_infantry_sand", false, 5),
                    CosmeticReward("banner_ottoman_crescent", false, 5),
                    AmmunitionReward(5, false, 5),
                    CosmeticReward("skin_egypt_mameluke_hussar", true, 5)
                }
            });

            // Page 2
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 2,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("grid_egypt_sandstorm", false, 8),
                    AmmunitionReward(5, false, 8),
                    CosmeticReward("skin_egypt_rifleman_desert", false, 8),
                    CosmeticReward("portrait_napoleon_egypt", true, 8)
                }
            });

            // Page 3
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 3,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("banner_battle_nile", true, 12),
                    DispatchBoxReward(DispatchBoxType.Bronze, false, 12),
                    SovereignReward(25, true, 12),
                    AmmunitionReward(10, true, 12)
                }
            });

            // Page 4 — capstone
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 4,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("grid_egypt_pyramids", true, 15),
                    CosmeticReward("victory_sphinx_gaze", true, 15),
                    DispatchBoxReward(DispatchBoxType.Gold, true, 15),
                    SovereignReward(50, true, 15)
                }
            });

            _manuals[manual.Id] = manual;
        }

        /// <summary>
        /// Premium Field Manual #2: The Peninsular War (Month 2).
        /// Theme: Wellington's 1807-1814 Iberian Campaign.
        /// </summary>
        private static void BuildPeninsularWar()
        {
            var manual = new FieldManualData
            {
                Id = "fm_peninsular",
                Name = "The Peninsular War",
                Description = "March through Iberia with Wellington and face Napoleon's marshals.",
                Theme = "Peninsular War 1807-1814",
                PremiumCostSovereigns = 1000,
                Pages = new List<FieldManualPage>()
            };

            // Page 1
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 1,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("banner_guerrilla", false, 5),
                    CosmeticReward("skin_peninsular_militia_spanish", false, 5),
                    AmmunitionReward(5, false, 5),
                    CosmeticReward("frame_shako", true, 5)
                }
            });

            // Page 2
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 2,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("grid_iberian_sun", false, 8),
                    SovereignReward(10, false, 8),
                    CosmeticReward("skin_peninsular_grenadier_redcoat", true, 8),
                    AmmunitionReward(5, false, 8)
                }
            });

            // Page 3
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 3,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("grid_torres_vedras", true, 12),
                    CosmeticReward("banner_peninsula_standard", true, 12),
                    DispatchBoxReward(DispatchBoxType.Bronze, false, 12),
                    SovereignReward(20, true, 12)
                }
            });

            // Page 4 — capstone
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 4,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("portrait_wellington_salamanca", true, 15),
                    CosmeticReward("victory_guerrilla_ambush", true, 15),
                    DispatchBoxReward(DispatchBoxType.Gold, true, 15),
                    SovereignReward(20, true, 15),
                    BoosterReward(24, true, 15)
                }
            });

            _manuals[manual.Id] = manual;
        }

        /// <summary>
        /// Premium Field Manual #3: The Grande Armee (Month 3).
        /// Theme: Napoleon's 1812 Russian Campaign.
        /// </summary>
        private static void BuildGrandeArmee()
        {
            var manual = new FieldManualData
            {
                Id = "fm_russia",
                Name = "The Grande Armee",
                Description = "March to Moscow and endure the terrible Russian winter.",
                Theme = "Russian Campaign 1812",
                PremiumCostSovereigns = 1000,
                Pages = new List<FieldManualPage>()
            };

            // Page 1
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 1,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("skin_russia_cossack_cavalry", false, 5),
                    CosmeticReward("banner_imperial_eagle_gold", false, 5),
                    AmmunitionReward(5, false, 5),
                    CosmeticReward("frame_bearskin", true, 5)
                }
            });

            // Page 2
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 2,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("grid_russian_steppe", false, 8),
                    SovereignReward(15, false, 8),
                    AmmunitionReward(10, false, 8),
                    CosmeticReward("portrait_napoleon_borodino", true, 8)
                }
            });

            // Page 3
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 3,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("skin_russia_cuirassier_ceremonial", true, 12),
                    CosmeticReward("portrait_kutuzov_winter_fur", true, 12),
                    CosmeticReward("banner_borodino_standard", true, 12),
                    DispatchBoxReward(DispatchBoxType.Bronze, false, 12),
                    DispatchBoxReward(DispatchBoxType.Silver, true, 12)
                }
            });

            // Page 4 — capstone
            manual.Pages.Add(new FieldManualPage
            {
                PageNumber = 4,
                Rewards = new List<FieldManualReward>
                {
                    CosmeticReward("grid_burning_moscow", true, 15),
                    CosmeticReward("victory_russian_retreat", true, 15),
                    DispatchBoxReward(DispatchBoxType.Gold, true, 15),
                    SovereignReward(60, true, 15),
                    BoosterReward(24, true, 15)
                }
            });

            _manuals[manual.Id] = manual;
        }

        // Helper methods for building rewards

        private static FieldManualReward CosmeticReward(string cosmeticId, bool premium, int starCost)
        {
            return new FieldManualReward
            {
                Type = FieldManualRewardType.Cosmetic,
                CosmeticId = cosmeticId,
                IsPremiumTrack = premium,
                StarCost = starCost
            };
        }

        private static FieldManualReward AmmunitionReward(int amount, bool premium, int starCost)
        {
            return new FieldManualReward
            {
                Type = FieldManualRewardType.Ammunition,
                Amount = amount,
                IsPremiumTrack = premium,
                StarCost = starCost
            };
        }

        private static FieldManualReward SovereignReward(int amount, bool premium, int starCost)
        {
            return new FieldManualReward
            {
                Type = FieldManualRewardType.Sovereigns,
                Amount = amount,
                IsPremiumTrack = premium,
                StarCost = starCost
            };
        }

        private static FieldManualReward DispatchBoxReward(DispatchBoxType boxType, bool premium, int starCost)
        {
            return new FieldManualReward
            {
                Type = FieldManualRewardType.DispatchBox,
                BoxType = boxType,
                IsPremiumTrack = premium,
                StarCost = starCost
            };
        }

        private static FieldManualReward BoosterReward(int durationHours, bool premium, int starCost)
        {
            return new FieldManualReward
            {
                Type = FieldManualRewardType.BattleStarBooster,
                Amount = durationHours,
                IsPremiumTrack = premium,
                StarCost = starCost
            };
        }
    }
}
