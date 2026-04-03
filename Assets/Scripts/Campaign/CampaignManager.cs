using System.Collections.Generic;
using WarChess.Save;

namespace WarChess.Campaign
{
    /// <summary>
    /// Manages campaign progression: tracks completed battles, unlocks,
    /// and validates what the player can access. Reads from CampaignDatabase
    /// and persists state via SaveManager.
    /// </summary>
    public class CampaignManager
    {
        private readonly CampaignSaveData _saveData;

        public CampaignManager(CampaignSaveData saveData)
        {
            _saveData = saveData;
        }

        /// <summary>Current difficulty level.</summary>
        public int Difficulty => _saveData.Difficulty;

        /// <summary>Highest completed battle number.</summary>
        public int HighestBattleCompleted => _saveData.HighestBattleCompleted;

        /// <summary>Next battle number the player should play.</summary>
        public int NextBattle => _saveData.HighestBattleCompleted + 1;

        /// <summary>Whether the full campaign (Acts 2-3) has been purchased.</summary>
        public bool IsFullCampaignUnlocked => _saveData.FullCampaignPurchased;

        /// <summary>
        /// Returns true if the player can access this battle.
        /// Must have completed the previous battle (or it's battle 1).
        /// Act 2-3 battles require purchase.
        /// </summary>
        public bool CanPlayBattle(int battleNumber)
        {
            if (battleNumber < 1 || battleNumber > 30) return false;
            if (battleNumber > _saveData.HighestBattleCompleted + 1) return false;

            var battle = CampaignDatabase.GetBattle(battleNumber);
            if (battle == null) return false;

            // Act 1 (battles 1-10) is free; Acts 2-3 require purchase
            if (battle.Act > 1 && !_saveData.FullCampaignPurchased)
                return false;

            return true;
        }

        /// <summary>
        /// Returns the star rating for a completed battle (0 if not completed).
        /// </summary>
        public int GetStars(int battleNumber)
        {
            return _saveData.BattleStars.TryGetValue(battleNumber, out int stars) ? stars : 0;
        }

        /// <summary>
        /// Returns total stars earned across all battles.
        /// </summary>
        public int GetTotalStars()
        {
            int total = 0;
            foreach (var kvp in _saveData.BattleStars)
                total += kvp.Value;
            return total;
        }

        /// <summary>
        /// Returns the set of unit type IDs currently unlocked.
        /// </summary>
        public HashSet<string> GetUnlockedUnits()
        {
            return new HashSet<string>(_saveData.UnlockedUnits);
        }

        /// <summary>
        /// Returns unlocked commander IDs.
        /// </summary>
        public List<string> GetUnlockedCommanders()
        {
            return new List<string>(_saveData.UnlockedCommanders);
        }

        /// <summary>
        /// Records a battle completion. Updates highest battle, stars, and unlocks.
        /// Returns a BattleCompletionResult describing what happened.
        /// </summary>
        public BattleCompletionResult CompleteBattle(int battleNumber, int stars)
        {
            var result = new BattleCompletionResult();
            result.BattleNumber = battleNumber;
            result.Stars = stars;

            // Update highest battle
            if (battleNumber > _saveData.HighestBattleCompleted)
            {
                _saveData.HighestBattleCompleted = battleNumber;
                result.IsFirstClear = true;
            }

            // Update stars (keep best)
            if (!_saveData.BattleStars.TryGetValue(battleNumber, out int existing) || stars > existing)
            {
                _saveData.BattleStars[battleNumber] = stars;
                result.IsNewBestStars = true;
            }

            // Process unlocks from this battle
            var battleData = CampaignDatabase.GetBattle(battleNumber);
            if (battleData != null && result.IsFirstClear)
            {
                foreach (var unitId in battleData.UnlocksUnitTypes)
                {
                    if (!_saveData.UnlockedUnits.Contains(unitId))
                    {
                        _saveData.UnlockedUnits.Add(unitId);
                        result.NewUnitsUnlocked.Add(unitId);
                    }
                }

                if (!string.IsNullOrEmpty(battleData.UnlocksCommander) &&
                    !_saveData.UnlockedCommanders.Contains(battleData.UnlocksCommander))
                {
                    _saveData.UnlockedCommanders.Add(battleData.UnlocksCommander);
                    result.NewCommanderUnlocked = battleData.UnlocksCommander;
                }
            }

            return result;
        }

        /// <summary>
        /// Sets the campaign difficulty. Only valid before any battles are played.
        /// </summary>
        public bool SetDifficulty(int difficulty)
        {
            if (_saveData.HighestBattleCompleted > 0) return false;
            if (difficulty < 0 || difficulty > 2) return false;
            _saveData.Difficulty = difficulty;
            return true;
        }
    }

    /// <summary>
    /// Result data from completing a campaign battle.
    /// </summary>
    public class BattleCompletionResult
    {
        public int BattleNumber;
        public int Stars;
        public bool IsFirstClear;
        public bool IsNewBestStars;
        public List<string> NewUnitsUnlocked = new List<string>();
        public string NewCommanderUnlocked = "";
    }
}
