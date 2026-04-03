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
        ArmyBanner
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

        /// <summary>Price in ammunition (soft currency). 0 = not purchasable with soft currency.</summary>
        public int SoftCurrencyPrice;

        /// <summary>Real-money price tier. 0=earned only, 1=$0.99, 2=$1.99, 3=$2.99.</summary>
        public int RealMoneyTier;

        /// <summary>Whether this cosmetic can drop from Dispatch Boxes.</summary>
        public bool AvailableInDispatchBox;
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
        }

        private static void AddCosmetic(string id, string name, CosmeticType type,
            CosmeticRarity rarity, int softPrice, int realMoneyTier, bool inDispatchBox)
        {
            _cosmetics[id] = new CosmeticData
            {
                Id = id,
                Name = name,
                Type = type,
                Rarity = rarity,
                SoftCurrencyPrice = softPrice,
                RealMoneyTier = realMoneyTier,
                AvailableInDispatchBox = inDispatchBox
            };
        }
    }
}
