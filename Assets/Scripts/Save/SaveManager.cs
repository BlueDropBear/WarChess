using System;
using System.IO;
using UnityEngine;

namespace WarChess.Save
{
    /// <summary>
    /// Handles reading and writing save data to persistent storage as JSON.
    /// Uses Application.persistentDataPath for cross-platform compatibility.
    /// </summary>
    public class SaveManager
    {
        private const string SaveFileName = "warchess_save.json";

        private readonly string _savePath;
        private SaveData _currentData;

        /// <summary>Current loaded save data.</summary>
        public SaveData Data => _currentData;

        public SaveManager()
        {
            _savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            _currentData = new SaveData();
        }

        /// <summary>
        /// Loads save data from disk. Returns true if a save was found and loaded.
        /// If no save exists or loading fails, initializes fresh data.
        /// </summary>
        public bool Load()
        {
            try
            {
                if (!File.Exists(_savePath))
                {
                    Debug.Log("No save file found, starting fresh.");
                    _currentData = new SaveData();
                    return false;
                }

                string json = File.ReadAllText(_savePath);
                _currentData = JsonUtility.FromJson<SaveData>(json);

                if (_currentData == null)
                {
                    Debug.LogWarning("Save file was corrupt, starting fresh.");
                    _currentData = new SaveData();
                    return false;
                }

                _currentData.Migrate();

                Debug.Log($"Save loaded: Battle {_currentData.Campaign.HighestBattleCompleted} completed, " +
                          $"{_currentData.Armies.Count} armies saved.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load save: {e.Message}");
                _currentData = new SaveData();
                return false;
            }
        }

        /// <summary>
        /// Saves current data to disk.
        /// </summary>
        public bool Save()
        {
            try
            {
                _currentData.LastSavedTicks = DateTime.UtcNow.Ticks;

                // Sync runtime dictionaries to serializable lists before saving
                _currentData.Campaign?.PrepareForSave();
                _currentData.Cosmetics?.PrepareForSave();

                string json = JsonUtility.ToJson(_currentData, true);

                // Write to temp file first, then move (atomic write)
                string tempPath = _savePath + ".tmp";
                File.WriteAllText(tempPath, json);
                if (File.Exists(_savePath))
                    File.Delete(_savePath);
                File.Move(tempPath, _savePath);

                Debug.Log("Game saved.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes the save file and resets to fresh data.
        /// </summary>
        public void DeleteSave()
        {
            try
            {
                if (File.Exists(_savePath))
                    File.Delete(_savePath);
                _currentData = new SaveData();
                Debug.Log("Save deleted.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save: {e.Message}");
            }
        }

        /// <summary>
        /// Returns true if a save file exists on disk.
        /// </summary>
        public bool SaveExists()
        {
            return File.Exists(_savePath);
        }
    }
}
