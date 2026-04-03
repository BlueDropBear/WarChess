using System;
using System.Collections.Generic;
using WarChess.Army;

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

        public SaveData()
        {
            Version = 1;
            Campaign = new CampaignSaveData();
            Armies = new List<SavedArmy>();
            Settings = new PlayerSettings();
            LastSavedTicks = DateTime.UtcNow.Ticks;
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

        public CampaignSaveData()
        {
            Difficulty = 0;
            HighestBattleCompleted = 0;
            BattleStars = new Dictionary<int, int>();
            UnlockedUnits = new List<string> { "LineInfantry", "Militia" };
            UnlockedCommanders = new List<string> { "Wellington", "Napoleon" };
            FullCampaignPurchased = false;
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

        public PlayerSettings()
        {
            MusicVolume = 0.7f;
            SfxVolume = 0.8f;
            ScreenShakeEnabled = true;
            ColorblindMode = false;
            TextSize = 1;
            BattleSpeed = 1f;
        }
    }
}
