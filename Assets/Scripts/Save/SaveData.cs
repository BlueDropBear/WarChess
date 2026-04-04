using System;
using System.Collections.Generic;
using UnityEngine;
using WarChess.Account;
using WarChess.Army;
using WarChess.Config;
using WarChess.Economy;

namespace WarChess.Save
{
    /// <summary>
    /// Serializable key-value pair for int→int dictionaries.
    /// Unity's JsonUtility cannot serialize Dictionary, so we use lists of these.
    /// </summary>
    [Serializable]
    public class SerializableIntPair
    {
        public int Key;
        public int Value;
    }

    /// <summary>
    /// Serializable key-value pair for int→string dictionaries.
    /// </summary>
    [Serializable]
    public class SerializableIntStringPair
    {
        public int Key;
        public string Value;
    }

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

        /// <summary>Cached account identity for offline access.</summary>
        public AccountIdentity Account;

        /// <summary>Sovereign premium currency state.</summary>
        public SovereignSaveData Sovereigns;

        /// <summary>Battle Star progression currency state.</summary>
        public BattleStarSaveData BattleStars;

        /// <summary>Field Manual progress per manual.</summary>
        public List<FieldManualSaveData> FieldManuals;

        /// <summary>Weekly challenge progress.</summary>
        public WeeklyChallengeSaveData WeeklyChallenges;

        public SaveData()
        {
            Version = 5;
            Campaign = new CampaignSaveData();
            Armies = new List<SavedArmy>();
            Settings = new PlayerSettings();
            Cosmetics = new CosmeticSaveData();
            PendingDispatchBoxes = new List<int>();
            PendingAnalyticsEvents = new List<AnalyticsEvent>();
            Account = new AccountIdentity();
            Sovereigns = new SovereignSaveData();
            BattleStars = new BattleStarSaveData();
            FieldManuals = new List<FieldManualSaveData>();
            WeeklyChallenges = new WeeklyChallengeSaveData();
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
                if (Campaign != null && Campaign.BattleAttemptCountsList == null)
                    Campaign.BattleAttemptCountsList = new List<SerializableIntPair>();
                Version = 3;
            }
            if (Version < 4)
            {
                if (Account == null) Account = new AccountIdentity();
                Version = 4;
            }
            if (Version < 5)
            {
                if (Sovereigns == null) Sovereigns = new SovereignSaveData();
                if (BattleStars == null) BattleStars = new BattleStarSaveData();
                if (FieldManuals == null) FieldManuals = new List<FieldManualSaveData>();
                if (WeeklyChallenges == null) WeeklyChallenges = new WeeklyChallengeSaveData();
                Version = 5;
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

        /// <summary>Stars earned per battle (serializable list). Key = battle number (1-30), Value = stars (0-3).</summary>
        public List<SerializableIntPair> BattleStarsList;

        /// <summary>Unit types unlocked by campaign progress (string IDs).</summary>
        public List<string> UnlockedUnits;

        /// <summary>Commander IDs unlocked by campaign progress.</summary>
        public List<string> UnlockedCommanders;

        /// <summary>Whether the full campaign has been purchased (Acts 2-3).</summary>
        public bool FullCampaignPurchased;

        /// <summary>Number of attempts per battle (serializable list).</summary>
        public List<SerializableIntPair> BattleAttemptCountsList;

        // Runtime dictionaries rebuilt from serialized lists
        [NonSerialized] private Dictionary<int, int> _battleStars;
        [NonSerialized] private Dictionary<int, int> _battleAttemptCounts;

        /// <summary>Stars dictionary, lazily built from serialized list.</summary>
        public Dictionary<int, int> BattleStars
        {
            get
            {
                if (_battleStars == null)
                    _battleStars = FromPairList(BattleStarsList);
                return _battleStars;
            }
        }

        /// <summary>Attempt counts dictionary, lazily built from serialized list.</summary>
        public Dictionary<int, int> BattleAttemptCounts
        {
            get
            {
                if (_battleAttemptCounts == null)
                    _battleAttemptCounts = FromPairList(BattleAttemptCountsList);
                return _battleAttemptCounts;
            }
        }

        public CampaignSaveData()
        {
            Difficulty = 0;
            HighestBattleCompleted = 0;
            BattleStarsList = new List<SerializableIntPair>();
            UnlockedUnits = new List<string> { "LineInfantry", "Militia" };
            UnlockedCommanders = new List<string> { "Wellington", "Napoleon" };
            FullCampaignPurchased = false;
            BattleAttemptCountsList = new List<SerializableIntPair>();
        }

        /// <summary>Syncs runtime dictionaries back into serializable lists. Call before saving.</summary>
        public void PrepareForSave()
        {
            if (_battleStars != null)
                BattleStarsList = ToPairList(_battleStars);
            if (_battleAttemptCounts != null)
                BattleAttemptCountsList = ToPairList(_battleAttemptCounts);
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

        private static Dictionary<int, int> FromPairList(List<SerializableIntPair> list)
        {
            var dict = new Dictionary<int, int>();
            if (list != null)
                foreach (var pair in list)
                    dict[pair.Key] = pair.Value;
            return dict;
        }

        private static List<SerializableIntPair> ToPairList(Dictionary<int, int> dict)
        {
            var list = new List<SerializableIntPair>();
            foreach (var kvp in dict)
                list.Add(new SerializableIntPair { Key = kvp.Key, Value = kvp.Value });
            return list;
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

        /// <summary>Currently equipped cosmetic per type (serializable list). Key = (int)CosmeticType, Value = cosmetic ID.</summary>
        public List<SerializableIntStringPair> EquippedByTypeList;

        /// <summary>Last shop refresh date (YYYYMMDD).</summary>
        public int LastShopRefreshDate;

        [NonSerialized] private Dictionary<int, string> _equippedByType;

        /// <summary>Equipped dictionary, lazily built from serialized list.</summary>
        public Dictionary<int, string> EquippedByType
        {
            get
            {
                if (_equippedByType == null)
                {
                    _equippedByType = new Dictionary<int, string>();
                    if (EquippedByTypeList != null)
                        foreach (var pair in EquippedByTypeList)
                            _equippedByType[pair.Key] = pair.Value;
                }
                return _equippedByType;
            }
        }

        public CosmeticSaveData()
        {
            OwnedCosmeticIds = new List<string>();
            EquippedByTypeList = new List<SerializableIntStringPair>();
            LastShopRefreshDate = 0;
        }

        /// <summary>Syncs runtime dictionary back into serializable list. Call before saving.</summary>
        public void PrepareForSave()
        {
            if (_equippedByType != null)
            {
                EquippedByTypeList = new List<SerializableIntStringPair>();
                foreach (var kvp in _equippedByType)
                    EquippedByTypeList.Add(new SerializableIntStringPair { Key = kvp.Key, Value = kvp.Value });
            }
        }
    }

    /// <summary>
    /// Sovereign premium currency save data.
    /// </summary>
    [Serializable]
    public class SovereignSaveData
    {
        public int Balance;
        public int TotalEarned;
        public int TotalSpent;
        public int ConsecutiveLoginDays;
        public int LastLoginDate;

        public SovereignSaveData()
        {
            Balance = 0;
            TotalEarned = 0;
            TotalSpent = 0;
            ConsecutiveLoginDays = 0;
            LastLoginDate = 0;
        }
    }

    /// <summary>
    /// Battle Star progression currency save data.
    /// </summary>
    [Serializable]
    public class BattleStarSaveData
    {
        public int Balance;
        public int TotalEarned;
        public int TotalSpent;
        public bool BoosterActive;
        public long BoosterExpiresTicks;

        public BattleStarSaveData()
        {
            Balance = 0;
            TotalEarned = 0;
            TotalSpent = 0;
            BoosterActive = false;
            BoosterExpiresTicks = 0;
        }
    }

    /// <summary>
    /// Save data for a single Field Manual's progress.
    /// </summary>
    [Serializable]
    public class FieldManualSaveData
    {
        /// <summary>Field Manual ID.</summary>
        public string ManualId;

        /// <summary>Whether the premium track is unlocked.</summary>
        public bool PremiumUnlocked;

        /// <summary>Claimed rewards. Each entry is "pageIndex:rewardIndex".</summary>
        public List<string> ClaimedRewardKeys;

        public FieldManualSaveData()
        {
            ManualId = "";
            PremiumUnlocked = false;
            ClaimedRewardKeys = new List<string>();
        }

        public FieldManualSaveData(string manualId, bool premiumUnlocked, List<string> claimedKeys)
        {
            ManualId = manualId;
            PremiumUnlocked = premiumUnlocked;
            ClaimedRewardKeys = claimedKeys ?? new List<string>();
        }
    }

    /// <summary>
    /// Weekly challenge progress save data.
    /// </summary>
    [Serializable]
    public class WeeklyChallengeSaveData
    {
        /// <summary>Current week number for challenge rotation.</summary>
        public int CurrentWeekNumber;

        /// <summary>Active challenge states.</summary>
        public List<ActiveChallengeSaveData> ActiveChallenges;

        public WeeklyChallengeSaveData()
        {
            CurrentWeekNumber = 0;
            ActiveChallenges = new List<ActiveChallengeSaveData>();
        }
    }

    /// <summary>
    /// Save data for a single active weekly challenge.
    /// </summary>
    [Serializable]
    public class ActiveChallengeSaveData
    {
        public string ChallengeId;
        public int CurrentCount;
        public bool Completed;
    }
}
