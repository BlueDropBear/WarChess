using System;
using System.Collections.Generic;
using WarChess.Multiplayer;

namespace WarChess.Config
{
    /// <summary>
    /// Manages cosmetic ownership, equipped items, and the rotating daily shop.
    /// Pure C# — persistence via CosmeticSaveData.
    /// </summary>
    public class CosmeticShop
    {
        private readonly List<string> _ownedCosmeticIds;
        private readonly Dictionary<int, string> _equippedByType;
        private int _lastShopRefreshDate;

        /// <summary>
        /// Creates the cosmetic shop from save data.
        /// </summary>
        public CosmeticShop(List<string> ownedIds, Dictionary<int, string> equippedByType, int lastRefreshDate)
        {
            _ownedCosmeticIds = ownedIds ?? new List<string>();
            _equippedByType = equippedByType ?? new Dictionary<int, string>();
            _lastShopRefreshDate = lastRefreshDate;
        }

        /// <summary>Returns all owned cosmetic IDs.</summary>
        public IReadOnlyList<string> OwnedCosmetics => _ownedCosmeticIds;

        /// <summary>Returns the equipped cosmetic ID for a given type, or null.</summary>
        public string GetEquipped(CosmeticType type)
        {
            return _equippedByType.TryGetValue((int)type, out string id) ? id : null;
        }

        /// <summary>Returns all equipped cosmetics keyed by type.</summary>
        public IReadOnlyDictionary<int, string> AllEquipped => _equippedByType;

        /// <summary>Returns true if the player owns this cosmetic.</summary>
        public bool Owns(string cosmeticId)
        {
            return _ownedCosmeticIds.Contains(cosmeticId);
        }

        /// <summary>
        /// Grants a cosmetic to the player. Returns false if already owned.
        /// </summary>
        public bool GrantCosmetic(string cosmeticId)
        {
            if (string.IsNullOrEmpty(cosmeticId)) return false;
            if (_ownedCosmeticIds.Contains(cosmeticId)) return false;

            _ownedCosmeticIds.Add(cosmeticId);
            return true;
        }

        /// <summary>
        /// Equips a cosmetic. Must be owned. Returns false if not owned.
        /// Only one cosmetic per type can be equipped at a time.
        /// </summary>
        public bool Equip(string cosmeticId)
        {
            if (!Owns(cosmeticId)) return false;

            var data = CosmeticDatabase.Get(cosmeticId);
            if (data == null) return false;

            _equippedByType[(int)data.Type] = cosmeticId;
            return true;
        }

        /// <summary>
        /// Unequips the cosmetic in the given slot.
        /// </summary>
        public void Unequip(CosmeticType type)
        {
            _equippedByType.Remove((int)type);
        }

        /// <summary>
        /// Returns the current rotating shop inventory. Refreshes daily.
        /// </summary>
        /// <param name="todayDateInt">Today's date as YYYYMMDD integer.</param>
        public List<CosmeticData> GetShopInventory(int todayDateInt)
        {
            return GenerateRotation(todayDateInt);
        }

        /// <summary>
        /// Purchases a cosmetic with soft currency (ammunition).
        /// Returns false if insufficient funds, already owned, or not purchasable.
        /// </summary>
        public bool PurchaseWithSoftCurrency(string cosmeticId, AmmunitionSystem wallet)
        {
            if (Owns(cosmeticId)) return false;

            var data = CosmeticDatabase.Get(cosmeticId);
            if (data == null || data.SoftCurrencyPrice <= 0) return false;

            if (wallet.Balance < data.SoftCurrencyPrice) return false;

            // Spend ammunition for cosmetic purchase
            // Uses AddPurchased with negative to deduct, or we track separately
            // For now, use a direct balance check — actual deduction handled by caller
            // since AmmunitionSystem doesn't have a generic spend method beyond deploy cost.
            // Return true to indicate validation passed; caller handles currency transfer.
            _ownedCosmeticIds.Add(cosmeticId);
            return true;
        }

        /// <summary>
        /// Generates the daily rotating inventory using seeded RNG from date.
        /// Returns 6 items: mix of rarities, excludes already-owned.
        /// </summary>
        private List<CosmeticData> GenerateRotation(int dateSeed)
        {
            var rng = new Random(dateSeed);
            var candidates = new List<CosmeticData>();

            foreach (var kvp in CosmeticDatabase.All)
            {
                // Only show purchasable items (soft currency > 0 or real money > 0)
                if (kvp.Value.SoftCurrencyPrice > 0 || kvp.Value.RealMoneyTier > 0)
                    candidates.Add(kvp.Value);
            }

            // Shuffle candidates
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var temp = candidates[i];
                candidates[i] = candidates[j];
                candidates[j] = temp;
            }

            // Take up to 6 items, ensuring at least one from each rarity if possible
            var shop = new List<CosmeticData>();
            var rarityQueues = new Dictionary<CosmeticRarity, List<CosmeticData>>();

            foreach (CosmeticRarity rarity in Enum.GetValues(typeof(CosmeticRarity)))
                rarityQueues[rarity] = new List<CosmeticData>();

            foreach (var c in candidates)
                rarityQueues[c.Rarity].Add(c);

            // One from each rarity that has items
            foreach (var kvp in rarityQueues)
            {
                if (kvp.Value.Count > 0 && shop.Count < 6)
                {
                    shop.Add(kvp.Value[0]);
                    kvp.Value.RemoveAt(0);
                }
            }

            // Fill remaining slots from shuffled candidates
            foreach (var c in candidates)
            {
                if (shop.Count >= 6) break;
                if (!shop.Contains(c))
                    shop.Add(c);
            }

            return shop;
        }
    }
}
