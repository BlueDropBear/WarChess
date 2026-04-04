using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WarChess.Account;
using WarChess.Army;
using WarChess.Audio;
using WarChess.Campaign;
using WarChess.Config;
using WarChess.Save;

namespace WarChess.Core
{
    /// <summary>
    /// Singleton that persists across scenes. Owns all managers and handles
    /// scene transitions and global game state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        /// <summary>Singleton instance. Lazily created if not found.</summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Core managers
        public SaveManager SaveManager { get; private set; }
        public CampaignManager CampaignManager { get; private set; }
        public ArmyManager ArmyManager { get; private set; }

        // Account
        public AccountManager Account { get; private set; }

        // Phase 5 managers
        public LocalizationManager Localization { get; private set; }
        public AnalyticsManager Analytics { get; private set; }
        public AccessibilityManager Accessibility { get; private set; }
        public CosmeticShop CosmeticShop { get; private set; }
        public DispatchBoxSystem DispatchBoxSystem { get; private set; }
        public MonetizationManager Monetization { get; private set; }
        public SoundManager SoundManager { get; private set; }
        public MusicController MusicController { get; private set; }

        // State passed between scenes
        public int CurrentBattleNumber { get; set; }
        public SavedArmy SelectedArmy { get; set; }
        public int BattleSeed { get; set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void Initialize()
        {
            // Core systems
            SaveManager = new SaveManager();
            SaveManager.Load();
            SaveManager.Data.Migrate();

            CampaignManager = new CampaignManager(SaveManager.Data.Campaign);

            ArmyManager = new ArmyManager(GameConfigData.GetUnitCosts());
            ArmyManager.LoadArmies(SaveManager.Data.Armies);

            // Account system
            var authProvider = CreatePlatformAuthProvider();
            var accountBackend = new StubAccountBackend();
            Account = new AccountManager(authProvider, accountBackend, SaveManager.Data.Account);
            Account.OnAccountResolved += identity =>
            {
                SaveManager.Data.Account = identity;
            };
            Account.Authenticate();

            // Localization
            Localization = new LocalizationManager();
            var lang = SaveManager.Data.Settings.Language ?? "en";
            if (lang != "en") Localization.LoadLanguage(lang, null);

            // Analytics
            Analytics = new AnalyticsManager();
            Analytics.LoadPendingEvents(SaveManager.Data.PendingAnalyticsEvents);
            Analytics.LogSessionStart();

            // Accessibility
            var settings = SaveManager.Data.Settings;
            Accessibility = new AccessibilityManager(
                settings.ColorblindMode,
                settings.ColorblindPaletteIndex,
                settings.TextSize);

            // Cosmetics
            var cosmeticData = SaveManager.Data.Cosmetics;
            CosmeticShop = new CosmeticShop(
                cosmeticData.OwnedCosmeticIds,
                cosmeticData.EquippedByType,
                cosmeticData.LastShopRefreshDate);

            // Dispatch Boxes
            var pendingBoxes = new List<DispatchBoxType>();
            foreach (int boxInt in SaveManager.Data.PendingDispatchBoxes)
            {
                if (System.Enum.IsDefined(typeof(DispatchBoxType), boxInt))
                    pendingBoxes.Add((DispatchBoxType)boxInt);
            }
            DispatchBoxSystem = new DispatchBoxSystem(CosmeticShop, pendingBoxes);

            // Monetization (depends on CampaignManager, CosmeticShop, DispatchBoxSystem, Analytics)
            Monetization = new MonetizationManager(
                purchased => SaveManager.Data.Campaign.FullCampaignPurchased = purchased,
                null, // AmmunitionSystem — set when PlayerProfile is available
                CosmeticShop,
                DispatchBoxSystem,
                Analytics);

            // Audio (MonoBehaviour components)
            SoundManager = gameObject.AddComponent<SoundManager>();
            SoundManager.Initialize(settings.SfxVolume);

            MusicController = gameObject.AddComponent<MusicController>();
            MusicController.Initialize(settings.MusicVolume);
        }

        /// <summary>
        /// Saves all game data to disk, including new system state.
        /// </summary>
        public void SaveGame()
        {
            // Persist account identity
            if (Account?.Identity != null)
                SaveManager.Data.Account = Account.Identity;

            // Persist dispatch box and analytics state back to save data
            SaveManager.Data.PendingDispatchBoxes = DispatchBoxSystem.GetPendingBoxesAsInts();
            SaveManager.Data.PendingAnalyticsEvents = new List<AnalyticsEvent>(Analytics.PendingEvents);
            SaveManager.Save();
        }

        /// <summary>
        /// Navigates to a scene by name.
        /// </summary>
        public void GoToScene(string sceneName)
        {
            SaveGame();
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Navigates to the battle scene with the given battle number and army.
        /// </summary>
        public void StartCampaignBattle(int battleNumber, SavedArmy army)
        {
            CurrentBattleNumber = battleNumber;
            SelectedArmy = army;
            BattleSeed = GenerateDeterministicSeed(battleNumber);
            GoToScene("Battle");
        }

        /// <summary>
        /// Creates the appropriate auth provider for the current platform.
        /// </summary>
        private IAuthProvider CreatePlatformAuthProvider()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return new AppleAuthProvider();
#elif UNITY_ANDROID && !UNITY_EDITOR
            return new GoogleAuthProvider();
#elif UNITY_STANDALONE && !UNITY_EDITOR
            return new SteamAuthProvider();
#else
            return new StubAuthProvider();
#endif
        }

        public void GoToMainMenu() => GoToScene("MainMenu");
        public void GoToArmory() => GoToScene("Armory");
        public void GoToCampaign() => GoToScene("Campaign");

        /// <summary>
        /// Generates a deterministic seed from battle number and attempt count.
        /// Ensures same battle + same attempt = same seed for replay consistency,
        /// while different attempts produce different seeds.
        /// </summary>
        private int GenerateDeterministicSeed(int battleNumber)
        {
            int attemptCount = SaveManager.Data.Campaign.GetBattleAttemptCount(battleNumber);
            SaveManager.Data.Campaign.IncrementBattleAttemptCount(battleNumber);
            // Combine battle number and attempt using a simple hash
            return battleNumber * 31 + attemptCount * 7919;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }
    }
}
