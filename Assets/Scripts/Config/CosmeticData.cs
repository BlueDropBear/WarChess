using System;
using System.Collections.Generic;

namespace WarChess.Config
{
    /// <summary>
    /// Types of cosmetic items available in the game.
    /// </summary>
    public enum CosmeticType
    {
        UnitSkin,
        GridTheme,
        CommanderPortrait,
        VictoryAnimation,
        ArmyBanner,
        OfficerPortraitFrame
    }

    /// <summary>
    /// Rarity tiers for cosmetic items. Affects drop rates in Dispatch Boxes.
    /// </summary>
    public enum CosmeticRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    /// <summary>
    /// Static data defining a cosmetic item.
    /// </summary>
    [Serializable]
    public class CosmeticData
    {
        public string Id;
        public string Name;
        public CosmeticType Type;
        public CosmeticRarity Rarity;

        /// <summary>Price in ammunition (soft currency). 0 = not purchasable with soft currency. Legacy — use SovereignPrice for new items.</summary>
        public int SoftCurrencyPrice;

        /// <summary>Price in Sovereigns (premium cosmetic currency). 0 = not purchasable in Quartermaster's Shop.</summary>
        public int SovereignPrice;

        /// <summary>Real-money price tier. 0=earned only, 1=$0.99, 2=$1.99, 3=$2.99.</summary>
        public int RealMoneyTier;

        /// <summary>Whether this cosmetic can drop from Dispatch Boxes.</summary>
        public bool AvailableInDispatchBox;

        /// <summary>
        /// If non-null, this cosmetic is exclusive to the specified Field Manual ID
        /// and will not appear in the Quartermaster's Shop daily rotation.
        /// </summary>
        public string FieldManualExclusiveId;
    }

    /// <summary>
    /// Database of all cosmetic items.
    /// </summary>
    public static class CosmeticDatabase
    {
        private static Dictionary<string, CosmeticData> _cosmetics;

        /// <summary>All cosmetic items keyed by ID.</summary>
        public static IReadOnlyDictionary<string, CosmeticData> All
        {
            get
            {
                if (_cosmetics == null) Build();
                return _cosmetics;
            }
        }

        /// <summary>Returns a cosmetic by ID, or null if not found.</summary>
        public static CosmeticData Get(string id)
        {
            var all = All;
            return ((Dictionary<string, CosmeticData>)all).TryGetValue(id, out var data) ? data : null;
        }

        /// <summary>Returns all cosmetics of a specific type.</summary>
        public static List<CosmeticData> GetByType(CosmeticType type)
        {
            var result = new List<CosmeticData>();
            foreach (var kvp in All)
            {
                if (kvp.Value.Type == type)
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>Returns all cosmetics of a specific rarity.</summary>
        public static List<CosmeticData> GetByRarity(CosmeticRarity rarity)
        {
            var result = new List<CosmeticData>();
            foreach (var kvp in All)
            {
                if (kvp.Value.Rarity == rarity)
                    result.Add(kvp.Value);
            }
            return result;
        }

        private static void Build()
        {
            _cosmetics = new Dictionary<string, CosmeticData>();

            // === Unit Skins ===
            AddCosmetic("skin_infantry_redcoat", "Redcoat Infantry", CosmeticType.UnitSkin, CosmeticRarity.Common, 5, 0, true);
            AddCosmetic("skin_infantry_bluecoat", "Bluecoat Infantry", CosmeticType.UnitSkin, CosmeticRarity.Common, 5, 0, true);
            AddCosmetic("skin_infantry_whitecoat", "Whitecoat Infantry", CosmeticType.UnitSkin, CosmeticRarity.Common, 5, 0, true);
            AddCosmetic("skin_cavalry_hussar_blue", "Blue Hussar", CosmeticType.UnitSkin, CosmeticRarity.Uncommon, 10, 0, true);
            AddCosmetic("skin_cavalry_lancer_polish", "Polish Lancer", CosmeticType.UnitSkin, CosmeticRarity.Uncommon, 10, 0, true);
            AddCosmetic("skin_artillery_bronze", "Bronze Cannon", CosmeticType.UnitSkin, CosmeticRarity.Common, 5, 0, true);
            AddCosmetic("skin_artillery_iron", "Iron Cannon", CosmeticType.UnitSkin, CosmeticRarity.Uncommon, 10, 0, true);
            AddCosmetic("skin_grenadier_bearskin", "Bearskin Grenadier", CosmeticType.UnitSkin, CosmeticRarity.Rare, 20, 1, true);
            AddCosmetic("skin_oldguard_gold", "Golden Guard", CosmeticType.UnitSkin, CosmeticRarity.Epic, 0, 3, true);
            AddCosmetic("skin_dragoon_greencoat", "Greencoat Dragoon", CosmeticType.UnitSkin, CosmeticRarity.Uncommon, 10, 0, true);

            // === Grid Themes ===
            AddCosmetic("grid_classic", "Classic Battlefield", CosmeticType.GridTheme, CosmeticRarity.Common, 5, 0, true);
            AddCosmetic("grid_winter", "Winter Campaign", CosmeticType.GridTheme, CosmeticRarity.Uncommon, 15, 0, true);
            AddCosmetic("grid_desert", "Desert Sands", CosmeticType.GridTheme, CosmeticRarity.Uncommon, 15, 0, true);
            AddCosmetic("grid_autumn", "Autumn Fields", CosmeticType.GridTheme, CosmeticRarity.Uncommon, 15, 0, true);
            AddCosmetic("grid_night", "Night Battle", CosmeticType.GridTheme, CosmeticRarity.Rare, 25, 2, true);
            AddCosmetic("grid_waterloo", "Waterloo Memorial", CosmeticType.GridTheme, CosmeticRarity.Epic, 0, 3, true);

            // === Commander Portraits ===
            AddCosmetic("portrait_wellington_formal", "Wellington (Formal)", CosmeticType.CommanderPortrait, CosmeticRarity.Uncommon, 10, 0, true);
            AddCosmetic("portrait_napoleon_coronation", "Napoleon (Coronation)", CosmeticType.CommanderPortrait, CosmeticRarity.Rare, 20, 1, true);
            AddCosmetic("portrait_kutuzov_winter", "Kutuzov (Winter)", CosmeticType.CommanderPortrait, CosmeticRarity.Uncommon, 10, 0, true);
            AddCosmetic("portrait_blucher_charge", "Blücher (Charging)", CosmeticType.CommanderPortrait, CosmeticRarity.Uncommon, 10, 0, true);
            AddCosmetic("portrait_moore_retreat", "Moore (Corunna)", CosmeticType.CommanderPortrait, CosmeticRarity.Rare, 20, 1, true);
            AddCosmetic("portrait_ney_cavalry", "Ney (On Horseback)", CosmeticType.CommanderPortrait, CosmeticRarity.Rare, 20, 1, true);

            // === Victory Animations ===
            AddCosmetic("victory_standard", "Standard Victory", CosmeticType.VictoryAnimation, CosmeticRarity.Common, 0, 0, false); // Default, free
            AddCosmetic("victory_fireworks", "Fireworks", CosmeticType.VictoryAnimation, CosmeticRarity.Uncommon, 15, 0, true);
            AddCosmetic("victory_cannon_salute", "Cannon Salute", CosmeticType.VictoryAnimation, CosmeticRarity.Rare, 25, 2, true);
            AddCosmetic("victory_eagles_soar", "Eagles Soar", CosmeticType.VictoryAnimation, CosmeticRarity.Epic, 0, 3, true);

            // === Army Banners ===
            AddCosmetic("banner_union_jack", "Union Jack", CosmeticType.ArmyBanner, CosmeticRarity.Common, 5, 0, true);
            AddCosmetic("banner_tricolour", "French Tricolour", CosmeticType.ArmyBanner, CosmeticRarity.Common, 5, 0, true);
            AddCosmetic("banner_imperial_eagle", "Imperial Eagle", CosmeticType.ArmyBanner, CosmeticRarity.Uncommon, 10, 0, true);
            AddCosmetic("banner_prussian", "Prussian Standard", CosmeticType.ArmyBanner, CosmeticRarity.Uncommon, 10, 0, true);
            AddCosmetic("banner_russian", "Russian Imperial", CosmeticType.ArmyBanner, CosmeticRarity.Uncommon, 10, 0, true);
            AddCosmetic("banner_grand_marshal", "Grand Marshal", CosmeticType.ArmyBanner, CosmeticRarity.Epic, 0, 3, true);
            AddCosmetic("banner_skull", "Skull & Crossbones", CosmeticType.ArmyBanner, CosmeticRarity.Rare, 20, 1, true);

            // Field Manual exclusive cosmetics (Months 1-3)
            BuildFieldManualCosmetics();
        }

        private static void AddCosmetic(string id, string name, CosmeticType type,
            CosmeticRarity rarity, int softPrice, int realMoneyTier, bool inDispatchBox)
        {
            // Calculate Sovereign price from rarity: Common=50, Uncommon=100, Rare=250, Epic=500
            int sovereignPrice = GetSovereignPriceForRarity(rarity);

            _cosmetics[id] = new CosmeticData
            {
                Id = id,
                Name = name,
                Type = type,
                Rarity = rarity,
                SoftCurrencyPrice = softPrice,
                SovereignPrice = sovereignPrice,
                RealMoneyTier = realMoneyTier,
                AvailableInDispatchBox = inDispatchBox,
                FieldManualExclusiveId = null
            };
        }

        private static void AddFieldManualCosmetic(string id, string name, CosmeticType type,
            CosmeticRarity rarity, string fieldManualId)
        {
            _cosmetics[id] = new CosmeticData
            {
                Id = id,
                Name = name,
                Type = type,
                Rarity = rarity,
                SoftCurrencyPrice = 0,
                SovereignPrice = 0, // Not purchasable in shop — exclusive to Field Manual
                RealMoneyTier = 0,
                AvailableInDispatchBox = false,
                FieldManualExclusiveId = fieldManualId
            };
        }

        private static int GetSovereignPriceForRarity(CosmeticRarity rarity)
        {
            switch (rarity)
            {
                case CosmeticRarity.Common: return 50;
                case CosmeticRarity.Uncommon: return 100;
                case CosmeticRarity.Rare: return 250;
                case CosmeticRarity.Epic: return 500;
                default: return 0;
            }
        }

        private static void BuildFieldManualCosmetics()
        {
            // === Egyptian Expedition (fm_egypt) ===
            AddFieldManualCosmetic("skin_egypt_infantry_sand", "Sand Infantry", CosmeticType.UnitSkin, CosmeticRarity.Common, "fm_egypt");
            AddFieldManualCosmetic("banner_ottoman_crescent", "Ottoman Crescent", CosmeticType.ArmyBanner, CosmeticRarity.Common, "fm_egypt");
            AddFieldManualCosmetic("skin_egypt_mameluke_hussar", "Mameluke Hussar", CosmeticType.UnitSkin, CosmeticRarity.Rare, "fm_egypt");
            AddFieldManualCosmetic("grid_egypt_sandstorm", "Egyptian Sandstorm", CosmeticType.GridTheme, CosmeticRarity.Uncommon, "fm_egypt");
            AddFieldManualCosmetic("skin_egypt_rifleman_desert", "Desert Rifleman", CosmeticType.UnitSkin, CosmeticRarity.Common, "fm_egypt");
            AddFieldManualCosmetic("portrait_napoleon_egypt", "Napoleon in Egypt", CosmeticType.CommanderPortrait, CosmeticRarity.Rare, "fm_egypt");
            AddFieldManualCosmetic("banner_battle_nile", "Battle of the Nile", CosmeticType.ArmyBanner, CosmeticRarity.Rare, "fm_egypt");
            AddFieldManualCosmetic("grid_egypt_pyramids", "Pyramids at Giza", CosmeticType.GridTheme, CosmeticRarity.Epic, "fm_egypt");
            AddFieldManualCosmetic("victory_sphinx_gaze", "Sphinx's Gaze", CosmeticType.VictoryAnimation, CosmeticRarity.Epic, "fm_egypt");

            // === Peninsular War (fm_peninsular) ===
            AddFieldManualCosmetic("banner_guerrilla", "Guerrilla Standard", CosmeticType.ArmyBanner, CosmeticRarity.Common, "fm_peninsular");
            AddFieldManualCosmetic("skin_peninsular_militia_spanish", "Spanish Resistance Militia", CosmeticType.UnitSkin, CosmeticRarity.Common, "fm_peninsular");
            AddFieldManualCosmetic("frame_shako", "Shako Frame", CosmeticType.OfficerPortraitFrame, CosmeticRarity.Uncommon, "fm_peninsular");
            AddFieldManualCosmetic("grid_iberian_sun", "Iberian Sun", CosmeticType.GridTheme, CosmeticRarity.Uncommon, "fm_peninsular");
            AddFieldManualCosmetic("skin_peninsular_grenadier_redcoat", "Redcoat Elite Grenadier", CosmeticType.UnitSkin, CosmeticRarity.Rare, "fm_peninsular");
            AddFieldManualCosmetic("grid_torres_vedras", "Torres Vedras", CosmeticType.GridTheme, CosmeticRarity.Rare, "fm_peninsular");
            AddFieldManualCosmetic("banner_peninsula_standard", "Peninsula Standard", CosmeticType.ArmyBanner, CosmeticRarity.Rare, "fm_peninsular");
            AddFieldManualCosmetic("portrait_wellington_salamanca", "Wellington at Salamanca", CosmeticType.CommanderPortrait, CosmeticRarity.Epic, "fm_peninsular");
            AddFieldManualCosmetic("victory_guerrilla_ambush", "Guerrilla Ambush", CosmeticType.VictoryAnimation, CosmeticRarity.Rare, "fm_peninsular");

            // === Grande Armee / Russian Campaign (fm_russia) ===
            AddFieldManualCosmetic("skin_russia_cossack_cavalry", "Cossack Cavalry", CosmeticType.UnitSkin, CosmeticRarity.Common, "fm_russia");
            AddFieldManualCosmetic("banner_imperial_eagle_gold", "Imperial Eagle (Gold)", CosmeticType.ArmyBanner, CosmeticRarity.Uncommon, "fm_russia");
            AddFieldManualCosmetic("frame_bearskin", "Bearskin Frame", CosmeticType.OfficerPortraitFrame, CosmeticRarity.Rare, "fm_russia");
            AddFieldManualCosmetic("grid_russian_steppe", "Russian Steppe", CosmeticType.GridTheme, CosmeticRarity.Uncommon, "fm_russia");
            AddFieldManualCosmetic("portrait_napoleon_borodino", "Napoleon at Borodino", CosmeticType.CommanderPortrait, CosmeticRarity.Epic, "fm_russia");
            AddFieldManualCosmetic("skin_russia_cuirassier_ceremonial", "Grande Armee Cuirassier", CosmeticType.UnitSkin, CosmeticRarity.Epic, "fm_russia");
            AddFieldManualCosmetic("portrait_kutuzov_winter_fur", "Kutuzov in Winter (Fur Cloak)", CosmeticType.CommanderPortrait, CosmeticRarity.Epic, "fm_russia");
            AddFieldManualCosmetic("banner_borodino_standard", "Borodino Standard", CosmeticType.ArmyBanner, CosmeticRarity.Rare, "fm_russia");
            AddFieldManualCosmetic("grid_burning_moscow", "Burning Moscow", CosmeticType.GridTheme, CosmeticRarity.Epic, "fm_russia");
            AddFieldManualCosmetic("victory_russian_retreat", "Russian Retreat", CosmeticType.VictoryAnimation, CosmeticRarity.Epic, "fm_russia");
        }
    }
}
