using System;
using System.Collections.Generic;
using WarChess.Multiplayer;

namespace WarChess.Config
{
    /// <summary>
    /// Types of in-app purchases.
    /// </summary>
    public enum IAPProductType
    {
        CampaignUnlock,
        AmmoPack,
        CosmeticItem,
        DispatchBox
    }

    /// <summary>
    /// Data for an IAP product in the catalog.
    /// </summary>
    [Serializable]
    public class IAPProduct
    {
        public string ProductId;
        public string DisplayName;
        public string Description;
        public IAPProductType Type;

        /// <summary>Price in USD cents. 99 = $0.99, 499 = $4.99.</summary>
        public int PriceCentsUSD;

        /// <summary>For AmmoPack type: amount of ammunition granted.</summary>
        public int AmmoAmount;

        /// <summary>For CosmeticItem type: the cosmetic ID to grant.</summary>
        public string CosmeticId;

        /// <summary>For DispatchBox type: the box tier to award.</summary>
        public DispatchBoxType BoxType;
    }

    /// <summary>
    /// Interface for purchase validation. David implements with server-side receipt validation.
    /// </summary>
    public interface IPurchaseValidator
    {
        /// <summary>Validates a purchase receipt. Returns true if valid.</summary>
        bool ValidateReceipt(string productId, string receiptData);
    }

    /// <summary>
    /// Stub validator for development. Always returns true.
    /// </summary>
    public class StubPurchaseValidator : IPurchaseValidator
    {
        /// <summary>Always validates successfully (development only).</summary>
        public bool ValidateReceipt(string productId, string receiptData) => true;
    }

    /// <summary>
    /// Manages the IAP product catalog and purchase processing.
    /// Pure C# logic — platform IAP integration (Apple/Google) is done by David.
    /// </summary>
    public class MonetizationManager
    {
        private readonly Dictionary<string, IAPProduct> _catalog;
        private IPurchaseValidator _validator;
        private readonly Action<bool> _setCampaignPurchased;
        private readonly AmmunitionSystem _ammoSystem;
        private readonly CosmeticShop _cosmeticShop;
        private readonly DispatchBoxSystem _dispatchBoxSystem;
        private readonly AnalyticsManager _analytics;

        /// <summary>
        /// Creates the monetization manager with references to dependent systems.
        /// </summary>
        /// <param name="setCampaignPurchased">Callback to set FullCampaignPurchased flag.</param>
        /// <param name="ammoSystem">Ammunition system for ammo pack delivery.</param>
        /// <param name="cosmeticShop">Cosmetic shop for cosmetic delivery.</param>
        /// <param name="dispatchBoxSystem">Dispatch box system for box delivery.</param>
        /// <param name="analytics">Analytics manager for purchase logging.</param>
        public MonetizationManager(
            Action<bool> setCampaignPurchased,
            AmmunitionSystem ammoSystem,
            CosmeticShop cosmeticShop,
            DispatchBoxSystem dispatchBoxSystem,
            AnalyticsManager analytics)
        {
            _setCampaignPurchased = setCampaignPurchased;
            _ammoSystem = ammoSystem;
            _cosmeticShop = cosmeticShop;
            _dispatchBoxSystem = dispatchBoxSystem;
            _analytics = analytics;
            _validator = new StubPurchaseValidator();
            _catalog = new Dictionary<string, IAPProduct>();
            BuildCatalog();
        }

        /// <summary>Sets the purchase validator (stub in dev, real in production).</summary>
        public void SetValidator(IPurchaseValidator validator)
        {
            _validator = validator ?? new StubPurchaseValidator();
        }

        /// <summary>Returns all available IAP products.</summary>
        public IReadOnlyDictionary<string, IAPProduct> Catalog => _catalog;

        /// <summary>Returns products of a specific type.</summary>
        public List<IAPProduct> GetProductsByType(IAPProductType type)
        {
            var result = new List<IAPProduct>();
            foreach (var kvp in _catalog)
            {
                if (kvp.Value.Type == type)
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>
        /// Processes a completed purchase. Validates receipt, then delivers the item.
        /// Returns true if purchase was processed successfully.
        /// </summary>
        public bool ProcessPurchase(string productId, string receiptData)
        {
            if (!_catalog.TryGetValue(productId, out var product))
                return false;

            if (!_validator.ValidateReceipt(productId, receiptData))
                return false;

            DeliverProduct(product);

            _analytics?.LogPurchase(productId, FormatPrice(product.PriceCentsUSD));

            return true;
        }

        /// <summary>
        /// Returns formatted price string from cents (e.g., 499 → "$4.99").
        /// </summary>
        public static string FormatPrice(int priceCentsUSD)
        {
            return $"${priceCentsUSD / 100}.{priceCentsUSD % 100:D2}";
        }

        private void DeliverProduct(IAPProduct product)
        {
            switch (product.Type)
            {
                case IAPProductType.CampaignUnlock:
                    _setCampaignPurchased?.Invoke(true);
                    break;

                case IAPProductType.AmmoPack:
                    _ammoSystem?.AddPurchased(product.AmmoAmount);
                    break;

                case IAPProductType.CosmeticItem:
                    _cosmeticShop?.GrantCosmetic(product.CosmeticId);
                    break;

                case IAPProductType.DispatchBox:
                    _dispatchBoxSystem?.AwardBox(product.BoxType);
                    break;
            }
        }

        private void BuildCatalog()
        {
            // Campaign unlock
            AddProduct(new IAPProduct
            {
                ProductId = "campaign_full",
                DisplayName = "Full Campaign",
                Description = "Unlock Acts II & III — 20 additional battles, new units, and the epic finale at Waterloo.",
                Type = IAPProductType.CampaignUnlock,
                PriceCentsUSD = 499
            });

            // Ammunition packs (per GDD pricing)
            AddProduct(new IAPProduct
            {
                ProductId = "ammo_10",
                DisplayName = "10 Ammunition",
                Description = "Deploy 10 additional armies to the battlefield.",
                Type = IAPProductType.AmmoPack,
                PriceCentsUSD = 99,
                AmmoAmount = 10
            });

            AddProduct(new IAPProduct
            {
                ProductId = "ammo_30",
                DisplayName = "30 Ammunition",
                Description = "A crate of ammunition for the dedicated general.",
                Type = IAPProductType.AmmoPack,
                PriceCentsUSD = 199,
                AmmoAmount = 30
            });

            AddProduct(new IAPProduct
            {
                ProductId = "ammo_100",
                DisplayName = "100 Ammunition",
                Description = "A full arsenal — best value for committed commanders.",
                Type = IAPProductType.AmmoPack,
                PriceCentsUSD = 499,
                AmmoAmount = 100
            });

            // Dispatch Boxes
            AddProduct(new IAPProduct
            {
                ProductId = "box_silver",
                DisplayName = "Silver Dispatch Box",
                Description = "Contains 2 cosmetic items. Higher chance of Uncommon+.",
                Type = IAPProductType.DispatchBox,
                PriceCentsUSD = 99,
                BoxType = DispatchBoxType.Silver
            });

            AddProduct(new IAPProduct
            {
                ProductId = "box_gold",
                DisplayName = "Gold Dispatch Box",
                Description = "Contains 3 cosmetic items. Guaranteed Rare or better.",
                Type = IAPProductType.DispatchBox,
                PriceCentsUSD = 199,
                BoxType = DispatchBoxType.Gold
            });
        }

        private void AddProduct(IAPProduct product)
        {
            _catalog[product.ProductId] = product;
        }
    }
}
