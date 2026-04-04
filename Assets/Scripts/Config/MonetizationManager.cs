using System;
using System.Collections.Generic;
using WarChess.Economy;
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
        DispatchBox,
        SovereignPack,
        FieldManualPremium
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

        /// <summary>For SovereignPack type: amount of sovereigns granted.</summary>
        public int SovereignAmount;

        /// <summary>For FieldManualPremium type: the Field Manual ID to unlock.</summary>
        public string FieldManualId;
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
        private readonly SovereignSystem _sovereignSystem;
        private readonly CosmeticShop _cosmeticShop;
        private readonly DispatchBoxSystem _dispatchBoxSystem;
        private readonly FieldManualSystem _fieldManualSystem;
        private readonly AnalyticsManager _analytics;

        /// <summary>
        /// Creates the monetization manager with references to dependent systems.
        /// </summary>
        /// <param name="setCampaignPurchased">Callback to set FullCampaignPurchased flag.</param>
        /// <param name="ammoSystem">Ammunition system for ammo pack delivery.</param>
        /// <param name="sovereignSystem">Sovereign system for premium currency delivery.</param>
        /// <param name="cosmeticShop">Cosmetic shop for cosmetic delivery.</param>
        /// <param name="dispatchBoxSystem">Dispatch box system for box delivery.</param>
        /// <param name="fieldManualSystem">Field Manual system for manual premium track delivery.</param>
        /// <param name="analytics">Analytics manager for purchase logging.</param>
        public MonetizationManager(
            Action<bool> setCampaignPurchased,
            AmmunitionSystem ammoSystem,
            SovereignSystem sovereignSystem,
            CosmeticShop cosmeticShop,
            DispatchBoxSystem dispatchBoxSystem,
            FieldManualSystem fieldManualSystem,
            AnalyticsManager analytics)
        {
            _setCampaignPurchased = setCampaignPurchased;
            _ammoSystem = ammoSystem;
            _sovereignSystem = sovereignSystem;
            _cosmeticShop = cosmeticShop;
            _dispatchBoxSystem = dispatchBoxSystem;
            _fieldManualSystem = fieldManualSystem;
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

                case IAPProductType.SovereignPack:
                    _sovereignSystem?.AddPurchased(product.SovereignAmount);
                    break;

                case IAPProductType.FieldManualPremium:
                    _fieldManualSystem?.PurchasePremiumTrack(product.FieldManualId);
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

            // Dispatch Boxes — removed from direct sale per Field Manual monetization strategy.
            // Dispatch Boxes are now earned through gameplay or as Field Manual rewards.

            // Sovereign packs (premium cosmetic currency)
            AddProduct(new IAPProduct
            {
                ProductId = "sovereign_100",
                DisplayName = "100 Sovereigns",
                Description = "A purse of gold sovereigns for the Quartermaster's Shop.",
                Type = IAPProductType.SovereignPack,
                PriceCentsUSD = 99,
                SovereignAmount = 100
            });

            AddProduct(new IAPProduct
            {
                ProductId = "sovereign_500",
                DisplayName = "500 Sovereigns",
                Description = "A chest of sovereigns for the discerning officer.",
                Type = IAPProductType.SovereignPack,
                PriceCentsUSD = 399,
                SovereignAmount = 500
            });

            AddProduct(new IAPProduct
            {
                ProductId = "sovereign_1200",
                DisplayName = "1,200 Sovereigns",
                Description = "A war chest of sovereigns — 20% bonus!",
                Type = IAPProductType.SovereignPack,
                PriceCentsUSD = 799,
                SovereignAmount = 1200
            });

            AddProduct(new IAPProduct
            {
                ProductId = "sovereign_2500",
                DisplayName = "2,500 Sovereigns",
                Description = "The Imperial Treasury — 25% bonus!",
                Type = IAPProductType.SovereignPack,
                PriceCentsUSD = 1499,
                SovereignAmount = 2500
            });
        }

        private void AddProduct(IAPProduct product)
        {
            _catalog[product.ProductId] = product;
        }
    }
}
