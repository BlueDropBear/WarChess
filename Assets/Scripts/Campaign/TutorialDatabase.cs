using System;
using System.Collections.Generic;

namespace WarChess.Campaign
{
    /// <summary>
    /// Defines a tutorial step shown during campaign battles.
    /// Steps trigger on specific conditions and display contextual tooltips.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    [Serializable]
    public class TutorialStep
    {
        /// <summary>Unique identifier for this step.</summary>
        public string Id;

        /// <summary>Battle number where this tutorial appears.</summary>
        public int BattleNumber;

        /// <summary>When during the battle this triggers.</summary>
        public TutorialTrigger Trigger;

        /// <summary>Specific round or condition parameter.</summary>
        public int TriggerParam;

        /// <summary>Short title for the tooltip.</summary>
        public string Title;

        /// <summary>Body text explaining the concept.</summary>
        public string Body;

        /// <summary>Optional: highlight a specific UI element or grid area.</summary>
        public string HighlightTarget;

        /// <summary>Whether the player must dismiss this before continuing.</summary>
        public bool PausesBattle;
    }

    /// <summary>
    /// When a tutorial step activates.
    /// </summary>
    public enum TutorialTrigger
    {
        BeforeDeployment,  // Before player places units
        AfterDeployment,   // After confirming deployment, before battle starts
        RoundStart,        // At the start of a specific round (TriggerParam = round number)
        FirstCombat,       // When the first attack happens
        FirstDeath,        // When the first unit dies
        BattleEnd,         // After battle concludes
        UnitUnlocked,      // When a new unit type is unlocked
        CommanderUnlocked, // When a new commander is unlocked
    }

    /// <summary>
    /// Database of all tutorial steps for the campaign.
    /// Guides the player through mechanics as they're introduced.
    /// </summary>
    public static class TutorialDatabase
    {
        private static List<TutorialStep> _allSteps;

        public static IReadOnlyList<TutorialStep> AllSteps
        {
            get
            {
                if (_allSteps == null) Build();
                return _allSteps;
            }
        }

        /// <summary>Returns all tutorial steps for a specific battle.</summary>
        public static List<TutorialStep> GetStepsForBattle(int battleNumber)
        {
            var result = new List<TutorialStep>();
            foreach (var step in AllSteps)
                if (step.BattleNumber == battleNumber)
                    result.Add(step);
            return result;
        }

        private static void Build()
        {
            _allSteps = new List<TutorialStep>
            {
                // === Battle 1: First Muster — basics ===
                new TutorialStep
                {
                    Id = "b1_deploy", BattleNumber = 1,
                    Trigger = TutorialTrigger.BeforeDeployment, TriggerParam = 0,
                    Title = "Deployment Zone",
                    Body = "Place your units on the highlighted rows (1-3). Tap a unit type, then tap a tile to place it. Your army budget is shown at the top — each unit costs points.",
                    HighlightTarget = "DeploymentZone",
                    PausesBattle = true
                },
                new TutorialStep
                {
                    Id = "b1_battle_start", BattleNumber = 1,
                    Trigger = TutorialTrigger.AfterDeployment, TriggerParam = 0,
                    Title = "Auto-Battle",
                    Body = "Once deployed, the battle resolves automatically. Units move toward enemies, attack when in range, and fight until one side is eliminated — or 30 rounds pass.",
                    PausesBattle = true
                },
                new TutorialStep
                {
                    Id = "b1_first_combat", BattleNumber = 1,
                    Trigger = TutorialTrigger.FirstCombat, TriggerParam = 0,
                    Title = "Combat",
                    Body = "Damage = ATK - DEF/2 (minimum 1). The red numbers show damage dealt. Stronger attacks come from flanking — getting behind enemy units.",
                    PausesBattle = false
                },

                // === Battle 2: Terrain ===
                new TutorialStep
                {
                    Id = "b2_terrain", BattleNumber = 2,
                    Trigger = TutorialTrigger.BeforeDeployment, TriggerParam = 0,
                    Title = "Terrain: Forest",
                    Body = "Forest tiles (dark green) give defenders 25% damage reduction but cost 2 movement to enter. Use them to protect fragile units, or avoid them for speed.",
                    HighlightTarget = "ForestTiles",
                    PausesBattle = true
                },

                // === Battle 3: Cavalry unlock ===
                new TutorialStep
                {
                    Id = "b3_cavalry", BattleNumber = 3,
                    Trigger = TutorialTrigger.UnitUnlocked, TriggerParam = 0,
                    Title = "New Unit: Cavalry",
                    Body = "Cavalry are fast flankers (4 MOV). When they move 3+ tiles before attacking, they deal double damage with Charge. Use them to get behind enemy lines!",
                    PausesBattle = true
                },
                new TutorialStep
                {
                    Id = "b3_flanking", BattleNumber = 3,
                    Trigger = TutorialTrigger.RoundStart, TriggerParam = 2,
                    Title = "Flanking",
                    Body = "Attacks from the side deal x1.3 damage. Attacks from the rear deal x2.0 damage! Units face toward the enemy at battle start. Use fast units to circle around.",
                    PausesBattle = false
                },

                // === Battle 4: Hills ===
                new TutorialStep
                {
                    Id = "b4_hills", BattleNumber = 4,
                    Trigger = TutorialTrigger.BeforeDeployment, TriggerParam = 0,
                    Title = "Terrain: Hills",
                    Body = "Ranged units on hills deal 25% more damage and gain +1 range. Hills cost 2 movement to climb. The high ground is a powerful advantage!",
                    HighlightTarget = "HillTiles",
                    PausesBattle = true
                },

                // === Battle 5: Artillery unlock ===
                new TutorialStep
                {
                    Id = "b5_artillery", BattleNumber = 5,
                    Trigger = TutorialTrigger.UnitUnlocked, TriggerParam = 0,
                    Title = "New Unit: Artillery",
                    Body = "Artillery has 4 range but only 1 movement. Bombardment hits the target AND adjacent tiles for 50% splash damage. Devastating against packed formations, but helpless in melee.",
                    PausesBattle = true
                },

                // === Battle 6: Rivers ===
                new TutorialStep
                {
                    Id = "b6_river", BattleNumber = 6,
                    Trigger = TutorialTrigger.BeforeDeployment, TriggerParam = 0,
                    Title = "Terrain: River & Bridge",
                    Body = "Rivers cost 3 movement to cross, and units cannot attack on the round they cross. Bridges let you cross at normal speed. Control the bridge to control the battle.",
                    HighlightTarget = "RiverTiles",
                    PausesBattle = true
                },

                // === Battle 7: Commander ===
                new TutorialStep
                {
                    Id = "b7_commander", BattleNumber = 7,
                    Trigger = TutorialTrigger.CommanderUnlocked, TriggerParam = 0,
                    Title = "New Commander: Kutuzov",
                    Body = "Commanders provide one-time abilities during battle. Kutuzov's 'Strategic Patience' heals all your units by 25% on round 8. Choose your commander in the army builder.",
                    PausesBattle = true
                },

                // === Battle 8: Grenadier + Fortifications ===
                new TutorialStep
                {
                    Id = "b8_grenadier", BattleNumber = 8,
                    Trigger = TutorialTrigger.UnitUnlocked, TriggerParam = 0,
                    Title = "New Unit: Grenadier",
                    Body = "Grenadiers are elite heavy infantry (40 HP, 8 DEF). On their first combat round, they throw a grenade dealing 5 damage to all enemies within 2 tiles. Expensive but powerful.",
                    PausesBattle = true
                },
                new TutorialStep
                {
                    Id = "b8_fortification", BattleNumber = 8,
                    Trigger = TutorialTrigger.BeforeDeployment, TriggerParam = 0,
                    Title = "Terrain: Fortification & Town",
                    Body = "Fortifications reduce damage by 30% and block cavalry charge. Towns reduce by 20% and block line of sight. The enemy is dug in — bring artillery!",
                    PausesBattle = true
                },

                // === Battle 10: Rifleman ===
                new TutorialStep
                {
                    Id = "b10_rifleman", BattleNumber = 10,
                    Trigger = TutorialTrigger.UnitUnlocked, TriggerParam = 0,
                    Title = "New Unit: Rifleman",
                    Body = "Riflemen have 3 range and target the weakest enemy. If they don't move, Aimed Shot gives +50% ATK. Keep them still and isolated for Skirmish Screen (+20% ATK, +1 range).",
                    PausesBattle = true
                },

                // === Battle 12: Hussar + Blücher ===
                new TutorialStep
                {
                    Id = "b12_hussar", BattleNumber = 12,
                    Trigger = TutorialTrigger.UnitUnlocked, TriggerParam = 0,
                    Title = "New Unit: Hussar",
                    Body = "Hussars are the fastest unit (8 SPD, 5 MOV). Hit and Run lets them attack then retreat 2 tiles. Use them to harass artillery and pick off wounded units.",
                    PausesBattle = true
                },

                // === Battle 15: Cuirassier ===
                new TutorialStep
                {
                    Id = "b15_cuirassier", BattleNumber = 15,
                    Trigger = TutorialTrigger.UnitUnlocked, TriggerParam = 0,
                    Title = "New Unit: Cuirassier",
                    Body = "Heavy armored cavalry. Armored Charge deals double damage AND reduces damage taken by 50% on the charge round. Expensive (8 pts) but devastating.",
                    PausesBattle = true
                },

                // === Battle 19: Sapper ===
                new TutorialStep
                {
                    Id = "b19_sapper", BattleNumber = 19,
                    Trigger = TutorialTrigger.UnitUnlocked, TriggerParam = 0,
                    Title = "New Unit: Sapper",
                    Body = "Sappers create Fortifications mid-battle! Instead of attacking, they build earthworks on an adjacent tile (+30% DEF for any unit on it). Reshape the battlefield.",
                    PausesBattle = true
                },

                // === Battle 22: Old Guard ===
                new TutorialStep
                {
                    Id = "b22_oldguard", BattleNumber = 22,
                    Trigger = TutorialTrigger.UnitUnlocked, TriggerParam = 0,
                    Title = "New Unit: Old Guard",
                    Body = "The finest infantry in the game (45 HP, 14 ATK, 10 DEF). Unbreakable: when HP drops below 25%, they gain +25% ATK. They never retreat. Costs 10 points.",
                    PausesBattle = true
                },

                // === Battle 24: Rocket Battery + Ney ===
                new TutorialStep
                {
                    Id = "b24_rocket", BattleNumber = 24,
                    Trigger = TutorialTrigger.UnitUnlocked, TriggerParam = 0,
                    Title = "New Unit: Rocket Battery",
                    Body = "Highest ATK (16) and range (5) but wildly inaccurate. Congreve Barrage hits a random 3x3 area — and may hit your own troops! Targets randomly. High risk, high reward.",
                    PausesBattle = true
                },

                // === Battle 14: Fog of War ===
                new TutorialStep
                {
                    Id = "b14_fog", BattleNumber = 14,
                    Trigger = TutorialTrigger.BeforeDeployment, TriggerParam = 0,
                    Title = "Fog of War",
                    Body = "Enemy positions are completely hidden! You won't see their units until your troops get close (within 3 tiles). Build a flexible army that can handle any composition.",
                    PausesBattle = true
                },
            };
        }
    }
}
