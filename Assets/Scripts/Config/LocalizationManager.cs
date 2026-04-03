using System;
using System.Collections.Generic;

namespace WarChess.Config
{
    /// <summary>
    /// Manages localized string lookup. English strings are embedded as default/fallback.
    /// Additional languages loaded from JSON string tables.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class LocalizationManager
    {
        private Dictionary<string, string> _currentStrings;
        private readonly Dictionary<string, string> _fallbackStrings;
        private string _currentLanguage;
        private readonly List<string> _availableLanguages;

        /// <summary>Current active language code (e.g., "en", "fr", "de").</summary>
        public string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// Initializes with English fallback strings.
        /// </summary>
        public LocalizationManager()
        {
            _fallbackStrings = new Dictionary<string, string>();
            _availableLanguages = new List<string> { "en" };
            BuildEnglishStrings();
            _currentStrings = _fallbackStrings;
            _currentLanguage = "en";
        }

        /// <summary>
        /// Returns the localized string for the given key, or the key itself if not found.
        /// </summary>
        public string Get(string key)
        {
            if (_currentStrings.TryGetValue(key, out string value))
                return value;
            if (_fallbackStrings.TryGetValue(key, out string fallback))
                return fallback;
            return key;
        }

        /// <summary>
        /// Returns a formatted localized string with parameters.
        /// Uses {0}, {1} placeholders via string.Format.
        /// </summary>
        public string GetFormatted(string key, params object[] args)
        {
            string template = Get(key);
            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        /// <summary>
        /// Loads a language from a JSON string containing key-value pairs.
        /// Falls back to English for missing keys. Pass null json to use embedded English.
        /// Expected JSON format: {"key": "value", ...}
        /// </summary>
        public void LoadLanguage(string languageCode, string json)
        {
            if (string.IsNullOrEmpty(languageCode)) languageCode = "en";

            _currentLanguage = languageCode;

            if (languageCode == "en" || string.IsNullOrEmpty(json))
            {
                _currentStrings = _fallbackStrings;
                return;
            }

            _currentStrings = ParseSimpleJson(json);

            if (!_availableLanguages.Contains(languageCode))
                _availableLanguages.Add(languageCode);
        }

        /// <summary>
        /// Resets to English.
        /// </summary>
        public void ResetToDefault()
        {
            _currentLanguage = "en";
            _currentStrings = _fallbackStrings;
        }

        /// <summary>
        /// Returns all available language codes.
        /// </summary>
        public IReadOnlyList<string> GetAvailableLanguages()
        {
            return _availableLanguages;
        }

        /// <summary>
        /// Returns true if a key exists in the current or fallback strings.
        /// </summary>
        public bool HasKey(string key)
        {
            return _currentStrings.ContainsKey(key) || _fallbackStrings.ContainsKey(key);
        }

        /// <summary>
        /// Minimal JSON parser for flat {"key": "value"} objects.
        /// No dependency on Unity's JsonUtility or external libraries.
        /// </summary>
        private static Dictionary<string, string> ParseSimpleJson(string json)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json)) return dict;

            // Strip outer braces
            json = json.Trim();
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);

            bool inKey = false;
            bool inValue = false;
            bool escaped = false;
            string currentKey = null;
            var buffer = new System.Text.StringBuilder();

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (escaped)
                {
                    buffer.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    if (!inKey && !inValue)
                    {
                        if (currentKey == null)
                        {
                            inKey = true;
                            buffer.Clear();
                        }
                        else
                        {
                            inValue = true;
                            buffer.Clear();
                        }
                    }
                    else if (inKey)
                    {
                        currentKey = buffer.ToString();
                        inKey = false;
                    }
                    else if (inValue)
                    {
                        dict[currentKey] = buffer.ToString();
                        currentKey = null;
                        inValue = false;
                    }
                    continue;
                }

                if (inKey || inValue)
                {
                    buffer.Append(c);
                }
            }

            return dict;
        }

        private void BuildEnglishStrings()
        {
            var s = _fallbackStrings;

            // === UI Labels ===
            s["ui.button.play"] = "Play";
            s["ui.button.deploy"] = "Deploy";
            s["ui.button.save"] = "Save";
            s["ui.button.load"] = "Load";
            s["ui.button.back"] = "Back";
            s["ui.button.settings"] = "Settings";
            s["ui.button.quit"] = "Quit";
            s["ui.button.battle"] = "Battle!";
            s["ui.button.retry"] = "Retry";
            s["ui.button.continue"] = "Continue";
            s["ui.button.next"] = "Next";
            s["ui.button.pause"] = "Pause";
            s["ui.button.resume"] = "Resume";
            s["ui.button.mainmenu"] = "Main Menu";
            s["ui.button.armory"] = "Armory";
            s["ui.button.campaign"] = "Campaign";
            s["ui.button.multiplayer"] = "Multiplayer";
            s["ui.button.shop"] = "Shop";
            s["ui.button.purchase"] = "Purchase";
            s["ui.button.equip"] = "Equip";
            s["ui.button.unequip"] = "Unequip";
            s["ui.button.open"] = "Open";

            s["ui.label.budget"] = "Budget";
            s["ui.label.remaining"] = "Remaining";
            s["ui.label.army_name"] = "Army Name";
            s["ui.label.units"] = "Units";
            s["ui.label.round"] = "Round";
            s["ui.label.hp"] = "HP";
            s["ui.label.atk"] = "ATK";
            s["ui.label.def"] = "DEF";
            s["ui.label.spd"] = "SPD";
            s["ui.label.rng"] = "Range";
            s["ui.label.mov"] = "Movement";
            s["ui.label.cost"] = "Cost";
            s["ui.label.stars"] = "Stars";
            s["ui.label.difficulty"] = "Difficulty";
            s["ui.label.volume_music"] = "Music Volume";
            s["ui.label.volume_sfx"] = "SFX Volume";
            s["ui.label.language"] = "Language";
            s["ui.label.accessibility"] = "Accessibility";
            s["ui.label.colorblind"] = "Colorblind Mode";
            s["ui.label.text_size"] = "Text Size";
            s["ui.label.screen_shake"] = "Screen Shake";
            s["ui.label.battle_speed"] = "Battle Speed";
            s["ui.label.ammunition"] = "Ammunition";
            s["ui.label.elo"] = "Rating";
            s["ui.label.wins"] = "Wins";
            s["ui.label.losses"] = "Losses";
            s["ui.label.tier"] = "Tier";

            // === Battle Outcomes ===
            s["battle.outcome.victory"] = "Victory!";
            s["battle.outcome.defeat"] = "Defeat";
            s["battle.outcome.draw"] = "Draw";
            s["battle.result.stars_earned"] = "Stars Earned: {0}";
            s["battle.result.units_survived"] = "{0} of {1} units survived";
            s["battle.result.rounds"] = "Battle lasted {0} rounds";
            s["battle.result.unlocked_unit"] = "New unit unlocked: {0}!";
            s["battle.result.unlocked_commander"] = "New commander unlocked: {0}!";

            // === Difficulty Settings ===
            s["difficulty.recruit"] = "Recruit";
            s["difficulty.recruit.desc"] = "Enemy stats reduced by 15%. Full enemy army visible.";
            s["difficulty.veteran"] = "Veteran";
            s["difficulty.veteran.desc"] = "Normal enemy stats. Unit types visible, placement hidden.";
            s["difficulty.marshal"] = "Marshal";
            s["difficulty.marshal.desc"] = "Enemy stats increased by 15%. Only unit count visible.";

            // === Unit Type Names ===
            s["unit.LineInfantry.name"] = "Line Infantry";
            s["unit.Militia.name"] = "Militia";
            s["unit.Cavalry.name"] = "Cavalry";
            s["unit.Artillery.name"] = "Artillery";
            s["unit.Grenadier.name"] = "Grenadier";
            s["unit.Rifleman.name"] = "Rifleman";
            s["unit.Hussar.name"] = "Hussar";
            s["unit.Cuirassier.name"] = "Cuirassier";
            s["unit.HorseArtillery.name"] = "Horse Artillery";
            s["unit.Sapper.name"] = "Sapper";
            s["unit.OldGuard.name"] = "Old Guard";
            s["unit.RocketBattery.name"] = "Rocket Battery";
            s["unit.Lancer.name"] = "Lancer";
            s["unit.Dragoon.name"] = "Dragoon";

            // === Commander Names and Abilities ===
            s["commander.Wellington.name"] = "Wellington";
            s["commander.Wellington.ability"] = "Hold the Line";
            s["commander.Wellington.desc"] = "All infantry gain +30% DEF for 2 rounds.";
            s["commander.Napoleon.name"] = "Napoleon";
            s["commander.Napoleon.ability"] = "Vive l'Empereur";
            s["commander.Napoleon.desc"] = "All units gain +20% ATK and +1 MOV for 2 rounds.";
            s["commander.Kutuzov.name"] = "Kutuzov";
            s["commander.Kutuzov.ability"] = "Strategic Patience";
            s["commander.Kutuzov.desc"] = "At round 8, all units heal 25% of max HP.";
            s["commander.Blucher.name"] = "Blücher";
            s["commander.Blucher.ability"] = "Forward, March!";
            s["commander.Blucher.desc"] = "Round 1: all cavalry gain +2 MOV and guaranteed charge.";
            s["commander.Moore.name"] = "Moore";
            s["commander.Moore.ability"] = "Rearguard Action";
            s["commander.Moore.desc"] = "When 50% units lost, survivors gain +40% ATK and +20% DEF.";
            s["commander.Ney.name"] = "Ney";
            s["commander.Ney.ability"] = "The Bravest of the Brave";
            s["commander.Ney.desc"] = "One unit takes two actions this round (double attack or double move+attack).";

            // === Officer Names and Traits ===
            s["officer.VeteranSergeant.name"] = "Veteran Sergeant";
            s["officer.VeteranSergeant.positive"] = "+20% ATK";
            s["officer.VeteranSergeant.negative"] = "-1 MOV";
            s["officer.YoungLieutenant.name"] = "Young Lieutenant";
            s["officer.YoungLieutenant.positive"] = "+2 MOV";
            s["officer.YoungLieutenant.negative"] = "-15% DEF";
            s["officer.Drillmaster.name"] = "Drillmaster";
            s["officer.Drillmaster.positive"] = "+25% DEF";
            s["officer.Drillmaster.negative"] = "-20% ATK";
            s["officer.Sharpshooter.name"] = "Sharpshooter";
            s["officer.Sharpshooter.positive"] = "+1 RNG";
            s["officer.Sharpshooter.negative"] = "-15% HP";
            s["officer.FearlessMajor.name"] = "Fearless Major";
            s["officer.FearlessMajor.positive"] = "Immune to morale effects";
            s["officer.FearlessMajor.negative"] = "Always targets nearest";
            s["officer.CautiousColonel.name"] = "Cautious Colonel";
            s["officer.CautiousColonel.positive"] = "+30% DEF when HP below 50%";
            s["officer.CautiousColonel.negative"] = "Will not advance past row 5";
            s["officer.RecklessCaptain.name"] = "Reckless Captain";
            s["officer.RecklessCaptain.positive"] = "+40% Charge damage";
            s["officer.RecklessCaptain.negative"] = "Takes +25% damage from all sources";
            s["officer.SiegeExpert.name"] = "Siege Expert";
            s["officer.SiegeExpert.positive"] = "+30% ATK vs Fortifications";
            s["officer.SiegeExpert.negative"] = "-2 MOV";
            s["officer.ScoutMaster.name"] = "Scout Master";
            s["officer.ScoutMaster.positive"] = "Reveals hidden enemies within 3 tiles";
            s["officer.ScoutMaster.negative"] = "-10% ATK, -10% DEF";
            s["officer.RallyOfficer.name"] = "Rally Officer";
            s["officer.RallyOfficer.positive"] = "Adjacent allies gain +10% ATK";
            s["officer.RallyOfficer.negative"] = "-20% HP";
            s["officer.Ironside.name"] = "Ironside";
            s["officer.Ironside.positive"] = "-50% flanking damage taken";
            s["officer.Ironside.negative"] = "-1 SPD";
            s["officer.PowderMonkey.name"] = "Powder Monkey";
            s["officer.PowderMonkey.positive"] = "+25% AoE radius";
            s["officer.PowderMonkey.negative"] = "+15% friendly fire chance";

            // === Elo Rank Names ===
            s["rank.Recruit"] = "Recruit";
            s["rank.Corporal"] = "Corporal";
            s["rank.Sergeant"] = "Sergeant";
            s["rank.Lieutenant"] = "Lieutenant";
            s["rank.Captain"] = "Captain";
            s["rank.Colonel"] = "Colonel";
            s["rank.General"] = "General";
            s["rank.GrandMarshal"] = "Grand Marshal";

            // === Star General Tier Names ===
            s["tier.1.name"] = "1-Star Brigadier";
            s["tier.2.name"] = "2-Star Major General";
            s["tier.3.name"] = "3-Star Lieutenant General";
            s["tier.4.name"] = "4-Star General";
            s["tier.5.name"] = "5-Star Marshal of the Empire";

            // === Terrain Names ===
            s["terrain.OpenField"] = "Open Field";
            s["terrain.Forest"] = "Forest";
            s["terrain.Hill"] = "Hill";
            s["terrain.River"] = "River";
            s["terrain.Bridge"] = "Bridge";
            s["terrain.Fortification"] = "Fortification";
            s["terrain.Mud"] = "Mud";
            s["terrain.Town"] = "Town";

            // === Formation Names ===
            s["formation.BattleLine"] = "Battle Line";
            s["formation.Battery"] = "Artillery Battery";
            s["formation.CavalryWedge"] = "Cavalry Wedge";
            s["formation.Square"] = "Square";
            s["formation.SkirmishScreen"] = "Skirmish Screen";

            // === Campaign Act Names ===
            s["campaign.act1.name"] = "Act I: The Rising Storm";
            s["campaign.act2.name"] = "Act II: The Grand Campaign";
            s["campaign.act3.name"] = "Act III: The Final Act";
            s["campaign.act2.locked"] = "Purchase the full campaign to unlock Acts II & III.";

            // === Multiplayer ===
            s["mp.pool.submit"] = "Deploy Army";
            s["mp.pool.withdraw"] = "Withdraw";
            s["mp.pool.waiting"] = "Waiting for opponent...";
            s["mp.pool.matched"] = "Match found!";
            s["mp.format.skirmish"] = "Skirmish (25 pts)";
            s["mp.format.standard"] = "Standard (40 pts)";
            s["mp.format.grandbattle"] = "Grand Battle (60 pts)";
            s["mp.ammo.daily"] = "+{0} daily ammunition claimed!";
            s["mp.ammo.insufficient"] = "Not enough ammunition to deploy.";

            // === Dispatch Boxes ===
            s["dispatch.bronze.name"] = "Bronze Dispatch Box";
            s["dispatch.silver.name"] = "Silver Dispatch Box";
            s["dispatch.gold.name"] = "Gold Dispatch Box";
            s["dispatch.open_prompt"] = "Open Dispatch Box?";
            s["dispatch.duplicate"] = "Duplicate — converted to ammunition.";

            // === Cosmetics ===
            s["cosmetic.type.UnitSkin"] = "Unit Skin";
            s["cosmetic.type.GridTheme"] = "Grid Theme";
            s["cosmetic.type.CommanderPortrait"] = "Commander Portrait";
            s["cosmetic.type.VictoryAnimation"] = "Victory Animation";
            s["cosmetic.type.ArmyBanner"] = "Army Banner";
            s["cosmetic.rarity.Common"] = "Common";
            s["cosmetic.rarity.Uncommon"] = "Uncommon";
            s["cosmetic.rarity.Rare"] = "Rare";
            s["cosmetic.rarity.Epic"] = "Epic";
            s["cosmetic.shop.title"] = "Cosmetic Shop";
            s["cosmetic.shop.refresh"] = "New items in: {0}";
            s["cosmetic.already_owned"] = "Already owned";

            // === General Messages ===
            s["msg.save_success"] = "Game saved.";
            s["msg.load_success"] = "Game loaded.";
            s["msg.purchase_success"] = "Purchase successful!";
            s["msg.purchase_failed"] = "Purchase failed.";
            s["msg.no_army_selected"] = "Select an army to deploy.";
            s["msg.budget_exceeded"] = "Army exceeds budget limit.";
        }
    }
}
