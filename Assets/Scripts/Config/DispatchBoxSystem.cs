using System;
using System.Collections.Generic;

namespace WarChess.Config
{
    /// <summary>
    /// Result of opening a Dispatch Box.
    /// </summary>
    public class DispatchBoxOpenResult
    {
        public DispatchBoxType BoxType;
        public List<string> AwardedCosmeticIds = new List<string>();
        public List<string> DuplicateIds = new List<string>();

        /// <summary>Ammunition awarded for duplicate cosmetics (1 per duplicate).</summary>
        public int DuplicateCurrencyAwarded;
    }

    /// <summary>
    /// Manages earning and opening Dispatch Boxes.
    /// Uses seeded RNG for deterministic drops.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class DispatchBoxSystem
    {
        private readonly CosmeticShop _cosmeticShop;
        private readonly List<DispatchBoxType> _pendingBoxes;

        /// <summary>Ammunition awarded per duplicate cosmetic.</summary>
        public const int DuplicateRefundAmount = 1;

        public DispatchBoxSystem(CosmeticShop cosmeticShop, List<DispatchBoxType> pendingBoxes)
        {
            _cosmeticShop = cosmeticShop;
            _pendingBoxes = pendingBoxes ?? new List<DispatchBoxType>();
        }

        /// <summary>Number of pending unopened boxes.</summary>
        public int PendingCount => _pendingBoxes.Count;

        /// <summary>Returns the list of pending box types.</summary>
        public IReadOnlyList<DispatchBoxType> PendingBoxes => _pendingBoxes;

        /// <summary>
        /// Awards a Dispatch Box (from campaign stars, tier promotion, etc.).
        /// </summary>
        public void AwardBox(DispatchBoxType type)
        {
            _pendingBoxes.Add(type);
        }

        /// <summary>
        /// Opens the next pending box using seeded RNG.
        /// Rolls for cosmetics based on the box's loot table, grants them via CosmeticShop,
        /// and handles duplicates by converting to ammunition.
        /// Returns null if no pending boxes.
        /// </summary>
        public DispatchBoxOpenResult OpenBox(int seed)
        {
            if (_pendingBoxes.Count == 0) return null;

            var boxType = _pendingBoxes[0];
            _pendingBoxes.RemoveAt(0);

            var definition = DispatchBoxDatabase.Get(boxType);
            if (definition == null) return null;

            var rng = new Random(seed);
            var result = new DispatchBoxOpenResult { BoxType = boxType };
            var alreadyAwarded = new HashSet<string>();

            for (int i = 0; i < definition.ItemCount; i++)
            {
                // Roll for rarity
                var rarity = RollRarity(definition.LootTable, rng);

                // Roll for specific cosmetic of that rarity
                string cosmeticId = RollCosmetic(rarity, rng, alreadyAwarded);

                if (cosmeticId == null) continue;

                alreadyAwarded.Add(cosmeticId);

                if (_cosmeticShop.GrantCosmetic(cosmeticId))
                {
                    result.AwardedCosmeticIds.Add(cosmeticId);
                }
                else
                {
                    // Already owned — duplicate
                    result.DuplicateIds.Add(cosmeticId);
                    result.DuplicateCurrencyAwarded += DuplicateRefundAmount;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the pending boxes as a list of integers for serialization.
        /// </summary>
        public List<int> GetPendingBoxesAsInts()
        {
            var ints = new List<int>(_pendingBoxes.Count);
            foreach (var box in _pendingBoxes)
                ints.Add((int)box);
            return ints;
        }

        private CosmeticRarity RollRarity(List<LootTableEntry> lootTable, Random rng)
        {
            int totalWeight = 0;
            foreach (var entry in lootTable)
                totalWeight += entry.Weight;

            int roll = rng.Next(totalWeight);
            int cumulative = 0;

            foreach (var entry in lootTable)
            {
                cumulative += entry.Weight;
                if (roll < cumulative)
                    return entry.Rarity;
            }

            return lootTable[lootTable.Count - 1].Rarity;
        }

        /// <summary>
        /// Selects a random cosmetic ID from the given rarity pool.
        /// Prefers items the player doesn't own yet.
        /// </summary>
        private string RollCosmetic(CosmeticRarity rarity, Random rng, HashSet<string> alreadyAwarded)
        {
            var candidates = CosmeticDatabase.GetByRarity(rarity);

            // Filter to dispatch-box eligible items
            var eligible = new List<CosmeticData>();
            foreach (var c in candidates)
            {
                if (c.AvailableInDispatchBox && !alreadyAwarded.Contains(c.Id))
                    eligible.Add(c);
            }

            if (eligible.Count == 0)
            {
                // Fall back to any item of this rarity
                foreach (var c in candidates)
                {
                    if (!alreadyAwarded.Contains(c.Id))
                        eligible.Add(c);
                }
            }

            if (eligible.Count == 0) return null;

            // Prefer unowned items
            var unowned = new List<CosmeticData>();
            foreach (var c in eligible)
            {
                if (!_cosmeticShop.Owns(c.Id))
                    unowned.Add(c);
            }

            var pool = unowned.Count > 0 ? unowned : eligible;
            return pool[rng.Next(pool.Count)].Id;
        }
    }
}
