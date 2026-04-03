using System.Collections.Generic;

namespace WarChess.Config
{
    /// <summary>
    /// Dispatch Box tiers. Per GDD: symbolic/cosmetic rewards only.
    /// </summary>
    public enum DispatchBoxType
    {
        Bronze,
        Silver,
        Gold
    }

    /// <summary>
    /// A weighted entry in a Dispatch Box loot table.
    /// </summary>
    public class LootTableEntry
    {
        public CosmeticRarity Rarity;
        public int Weight;

        public LootTableEntry(CosmeticRarity rarity, int weight)
        {
            Rarity = rarity;
            Weight = weight;
        }
    }

    /// <summary>
    /// Static data for a Dispatch Box type: its loot table and item count.
    /// </summary>
    public class DispatchBoxDefinition
    {
        public DispatchBoxType BoxType;
        public string DisplayName;
        public int ItemCount;
        public List<LootTableEntry> LootTable;
    }

    /// <summary>
    /// Database of Dispatch Box definitions.
    /// </summary>
    public static class DispatchBoxDatabase
    {
        private static Dictionary<DispatchBoxType, DispatchBoxDefinition> _boxes;

        /// <summary>Returns the definition for a box type.</summary>
        public static DispatchBoxDefinition Get(DispatchBoxType type)
        {
            if (_boxes == null) Build();
            return _boxes.TryGetValue(type, out var def) ? def : null;
        }

        private static void Build()
        {
            _boxes = new Dictionary<DispatchBoxType, DispatchBoxDefinition>
            {
                {
                    DispatchBoxType.Bronze, new DispatchBoxDefinition
                    {
                        BoxType = DispatchBoxType.Bronze,
                        DisplayName = "Bronze Dispatch Box",
                        ItemCount = 1,
                        LootTable = new List<LootTableEntry>
                        {
                            new LootTableEntry(CosmeticRarity.Common, 70),
                            new LootTableEntry(CosmeticRarity.Uncommon, 25),
                            new LootTableEntry(CosmeticRarity.Rare, 4),
                            new LootTableEntry(CosmeticRarity.Epic, 1)
                        }
                    }
                },
                {
                    DispatchBoxType.Silver, new DispatchBoxDefinition
                    {
                        BoxType = DispatchBoxType.Silver,
                        DisplayName = "Silver Dispatch Box",
                        ItemCount = 2,
                        LootTable = new List<LootTableEntry>
                        {
                            new LootTableEntry(CosmeticRarity.Common, 40),
                            new LootTableEntry(CosmeticRarity.Uncommon, 40),
                            new LootTableEntry(CosmeticRarity.Rare, 15),
                            new LootTableEntry(CosmeticRarity.Epic, 5)
                        }
                    }
                },
                {
                    DispatchBoxType.Gold, new DispatchBoxDefinition
                    {
                        BoxType = DispatchBoxType.Gold,
                        DisplayName = "Gold Dispatch Box",
                        ItemCount = 3,
                        LootTable = new List<LootTableEntry>
                        {
                            new LootTableEntry(CosmeticRarity.Common, 15),
                            new LootTableEntry(CosmeticRarity.Uncommon, 35),
                            new LootTableEntry(CosmeticRarity.Rare, 35),
                            new LootTableEntry(CosmeticRarity.Epic, 15)
                        }
                    }
                }
            };
        }
    }
}
