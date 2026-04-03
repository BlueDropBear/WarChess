using UnityEngine;
using UnityEngine.SceneManagement;
using WarChess.Army;
using WarChess.Campaign;
using WarChess.Save;

namespace WarChess.Core
{
    /// <summary>
    /// Singleton that persists across scenes. Owns SaveManager, CampaignManager,
    /// and ArmyManager. Handles scene transitions and global game state.
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

        public SaveManager SaveManager { get; private set; }
        public CampaignManager CampaignManager { get; private set; }
        public ArmyManager ArmyManager { get; private set; }

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
            SaveManager = new SaveManager();
            SaveManager.Load();

            CampaignManager = new CampaignManager(SaveManager.Data.Campaign);

            // Unit costs for army validation (from GDD Section 3.2)
            var unitCosts = new System.Collections.Generic.Dictionary<string, int>
            {
                { "LineInfantry", 3 },
                { "Militia", 1 },
                { "Cavalry", 5 },
                { "Artillery", 6 },
                { "Grenadier", 7 },
                { "Rifleman", 5 },
                { "Hussar", 4 },
                { "Cuirassier", 8 },
                { "HorseArtillery", 6 },
                { "Sapper", 4 },
                { "OldGuard", 10 },
                { "RocketBattery", 7 },
                { "Lancer", 5 },
                { "Dragoon", 6 }
            };
            ArmyManager = new ArmyManager(unitCosts);
            ArmyManager.LoadArmies(SaveManager.Data.Armies);
        }

        /// <summary>
        /// Saves all game data to disk.
        /// </summary>
        public void SaveGame()
        {
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
            BattleSeed = System.Environment.TickCount;
            GoToScene("Battle");
        }

        public void GoToMainMenu() => GoToScene("MainMenu");
        public void GoToArmory() => GoToScene("Armory");
        public void GoToCampaign() => GoToScene("Campaign");

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
