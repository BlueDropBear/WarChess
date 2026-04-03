using System;
using System.Collections.Generic;
using WarChess.Units;

namespace WarChess.Commanders
{
    /// <summary>
    /// Commander IDs matching GDD Section 5.
    /// </summary>
    public enum CommanderId
    {
        None,
        Wellington,
        Napoleon,
        Kutuzov,
        Blucher,
        Moore,
        Ney
    }

    /// <summary>
    /// Trigger type for commander abilities.
    /// </summary>
    public enum CommanderTriggerType
    {
        Manual,    // Player pre-selects activation round
        Automatic  // Triggers on a condition
    }

    /// <summary>
    /// Static data for a commander's ability.
    /// </summary>
    public class CommanderAbilityData
    {
        public CommanderId Id;
        public string Name;
        public string AbilityName;
        public string Description;
        public CommanderTriggerType TriggerType;

        /// <summary>For Manual: valid activation round range. For Automatic: the condition round or threshold.</summary>
        public int TriggerParam;

        /// <summary>How many rounds the effect lasts (0 = rest of battle).</summary>
        public int Duration;

        /// <summary>Campaign battle number that unlocks this commander.</summary>
        public int UnlockBattle;
    }

    /// <summary>
    /// Database of all 6 commanders from GDD Section 5.2.
    /// </summary>
    public static class CommanderDatabase
    {
        private static Dictionary<CommanderId, CommanderAbilityData> _commanders;

        public static IReadOnlyDictionary<CommanderId, CommanderAbilityData> All
        {
            get
            {
                if (_commanders == null) Build();
                return _commanders;
            }
        }

        public static CommanderAbilityData Get(CommanderId id)
        {
            var all = All;
            return all.TryGetValue(id, out var data) ? data : null;
        }

        private static void Build()
        {
            _commanders = new Dictionary<CommanderId, CommanderAbilityData>
            {
                {
                    CommanderId.Wellington, new CommanderAbilityData
                    {
                        Id = CommanderId.Wellington,
                        Name = "Wellington",
                        AbilityName = "Hold the Line",
                        Description = "All infantry gain +30% DEF for 2 rounds.",
                        TriggerType = CommanderTriggerType.Manual,
                        TriggerParam = 5, // Max activation round
                        Duration = 2,
                        UnlockBattle = 0 // Available from start
                    }
                },
                {
                    CommanderId.Napoleon, new CommanderAbilityData
                    {
                        Id = CommanderId.Napoleon,
                        Name = "Napoleon",
                        AbilityName = "Vive l'Empereur",
                        Description = "All units gain +20% ATK and +1 MOV for 2 rounds.",
                        TriggerType = CommanderTriggerType.Manual,
                        TriggerParam = 10,
                        Duration = 2,
                        UnlockBattle = 0
                    }
                },
                {
                    CommanderId.Kutuzov, new CommanderAbilityData
                    {
                        Id = CommanderId.Kutuzov,
                        Name = "Kutuzov",
                        AbilityName = "Strategic Patience",
                        Description = "All units heal 25% of max HP. Activates round 8.",
                        TriggerType = CommanderTriggerType.Automatic,
                        TriggerParam = 8, // Activates on round 8
                        Duration = 0,
                        UnlockBattle = 7
                    }
                },
                {
                    CommanderId.Blucher, new CommanderAbilityData
                    {
                        Id = CommanderId.Blucher,
                        Name = "Blücher",
                        AbilityName = "Forward, March!",
                        Description = "All cavalry gain +2 MOV and guaranteed charge on first attack. Round 1.",
                        TriggerType = CommanderTriggerType.Automatic,
                        TriggerParam = 1, // Activates round 1
                        Duration = 0,
                        UnlockBattle = 12
                    }
                },
                {
                    CommanderId.Moore, new CommanderAbilityData
                    {
                        Id = CommanderId.Moore,
                        Name = "Moore",
                        AbilityName = "Rearguard Action",
                        Description = "When you lose 50% of your units, all remaining gain +40% ATK and +20% DEF for the rest of the battle.",
                        TriggerType = CommanderTriggerType.Automatic,
                        TriggerParam = 50, // 50% unit threshold
                        Duration = 0,
                        UnlockBattle = 18
                    }
                },
                {
                    CommanderId.Ney, new CommanderAbilityData
                    {
                        Id = CommanderId.Ney,
                        Name = "Ney",
                        AbilityName = "The Bravest of the Brave",
                        Description = "One unit takes two actions this round (double attack or double move+attack).",
                        TriggerType = CommanderTriggerType.Manual,
                        TriggerParam = 10,
                        Duration = 0,
                        UnlockBattle = 24
                    }
                }
            };
        }
    }
}
