using System;
using System.Collections.Generic;
using WarChess.Army;
using WarChess.Config;

namespace WarChess.Save
{
    /// <summary>
    /// Root save data structure. Serialized to/from JSON.
    /// Contains all persistent player state.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Save format version for migration.</summary>
        public int Version;

        /// <summary>Campaign progress data.</summary>
        public CampaignSaveData Campaign;

        /// <summary>All saved armies (campaign + multiplayer).</summary>
        public List<SavedArmy> Armies;

        /// <summary>Player settings and preferences.</summary>
        public PlayerSettings Settings;

        /// <summary>When this save was last written (UTC ticks).</summary>
        public long LastSavedTicks;

        /// <summary>Cosmetic ownership and equipped items.</summary>
        public CosmeticSaveData Cosmetics;

        /// <summary>Pending unopened Dispatch Boxes (stored as (int)DispatchBoxType).</summary>
        public List<int> PendingDispatchBoxes;

        /// <summary>Offline analytics events waiting to be sent.</summary>
        public List<AnalyticsEvent> PendingAnalyticsEvents;

        public SaveData()
        {
            Version = 3;
            Campaign = new CampaignSaveData();
            Armies = new List<SavedArmy>();
            Settings = new PlayerSettings();
            Cosmetics = new CosmeticSaveData();
            PendingDispatchBoxes = new List<int>();
            PendingAnalyticsEvents = new List<AnalyticsEvent>();
            LastSavedTicks = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Migrates save data from older versions. Call after deserialization.
        /// </summary>
        public void Migrate()
        {
            if (Version < 2)
            {
                if (Cosmetics == null) Cosmetics = new CosmeticSaveData();
                if (PendingDispatchBoxes == null) PendingDispatchBoxes = new List<int>();
                if (PendingAnalyticsEvents == null) PendingAnalyticsEvents = new List<AnalyticsEvent>();
                if (Settings != null)
                {
                    if (Settings.Language == null) Settings.Language = "en";
                }
                Version = 2;
            }
            if (Version < 3)
            {
                if (Campaign != null && Campaign.BattleAttemptCounts == null)
                    Campaign.BattleAttemptCounts = new Dictionary<int, int>();
                Version = 3;
            }
        }
    }

    /// <summary>
    /// Campaign progress: which battles are completed, stars earned, difficulty.
    /// </summary>
    [Serializable]
    public class CampaignSaveData
    {
        /// <summary>Selected difficulty for this campaign run.</summary>
        public int Difficulty; // 0=Recruit, 1=Veteran, 2=Marshal

        /// <summary>Highest completed battle number (0 = none).</summary>
        public int HighestBattleCompleted;

        /// <summary>Stars earned per battle. Key = battle number (1-30), Value = stars (0-3).</summary>
        public Dictionary<int, int> BattleStars;

        /// <summary>Unit types unlocked by campaign progress (string IDs).</summary>
        public List<string> UnlockedUnits;

        /// <summary>Commander IDs unlocked by campaign progress.</summary>
        public List<string> UnlockedCommanders;

        /// <summary>Whether the full campaign has been purchased (Acts 2-3).</summary>
        public bool FullCampaignPurchased;

        /// <summary>Number of attempts per battle, used for deterministic seed generation.</summary>
        public Dictionary<int, int> BattleAttemptCounts;

        public CampaignSaveData()
        {
            Difficulty = 0;
            HighestBattleCompleted = 0;
            BattleStars = new Dictionary<int, int>();
            UnlockedUnits = new List<string> { "LineInfantry", "Militia" };
            UnlockedCommanders = new List<string> { "Wellington", "Napoleon" };
            FullCampaignPurchased = false;
            BattleAttemptCounts = new Dictionary<int, int>();
        }

        /// <summary>Returns the number of attempts for a given battle.</summary>
        public int GetBattleAttemptCount(int battleNumber)
        {
            return BattleAttemptCounts.TryGetValue(battleNumber, out int count) ? count : 0;
        }

        /// <summary>Increments the attempt counter for a given battle.</summary>
        public void IncrementBattleAttemptCount(int battleNumber)
        {
            if (!BattleAttemptCounts.ContainsKey(battleNumber))
                BattleAttemptCounts[battleNumber] = 0;
            BattleAttemptCounts[battleNumber]++;
        }
    }

    /// <summary>
    /// Player settings saved to disk.
    /// </summary>
    [Serializable]
    public class PlayerSettings
    {
        public float MusicVolume;
        public float SfxVolume;
        public bool ScreenShakeEnabled;
        public bool ColorblindMode;
        public int TextSize; // 0=small, 1=medium, 2=large
        public float BattleSpeed; // 1=normal, 2=fast, 4=fastest

        /// <summary>Language code for localization (e.g., "en", "fr", "de").</summary>
        public string Language;

        /// <summary>Colorblind palette index. 0=Normal, 1=Deuteranopia, 2=Tritanopia.</summary>
        public int ColorblindPaletteIndex;

        public PlayerSettings()
        {
            MusicVolume = 0.7f;
            SfxVolume = 0.8f;
            ScreenShakeEnabled = true;
            ColorblindMode = false;
            TextSize = 1;
            BattleSpeed = 1f;
            Language = "en";
            ColorblindPaletteIndex = 0;
        }
    }

    /// <summary>
    /// Cosmetic ownership and equipped items.
    /// </summary>
    [Serializable]
    public class CosmeticSaveData
    {
        /// <summary>IDs of all owned cosmetic items.</summary>
        public List<string> OwnedCosmeticIds;

        /// <summary>Currently equipped cosmetic per type. Key = (int)CosmeticType, Value = cosmetic ID.</summary>
        public Dictionary<int, string> EquippedByType;

        /// <summary>Last shop refresh date (YYYYMMDD).</summary>
        public int LastShopRefreshDate;

        public CosmeticSaveData()
        {
            OwnedCosmeticIds = new List<string>();
            EquippedByType = new Dictionary<int, string>();
            LastShopRefreshDate = 0;
        }
    }
}
