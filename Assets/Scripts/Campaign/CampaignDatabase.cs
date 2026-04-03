using System.Collections.Generic;

namespace WarChess.Campaign
{
    /// <summary>
    /// Contains all 30 campaign battle definitions from the GDD.
    /// Enemy army compositions for Act 1 (battles 1-10) are fully specified.
    /// Acts 2-3 compositions will be added as development progresses.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class CampaignDatabase
    {
        private static List<CampaignBattleData> _allBattles;

        /// <summary>Returns all 30 campaign battles.</summary>
        public static IReadOnlyList<CampaignBattleData> AllBattles
        {
            get
            {
                if (_allBattles == null) BuildDatabase();
                return _allBattles;
            }
        }

        /// <summary>Returns a specific battle by number (1-30).</summary>
        public static CampaignBattleData GetBattle(int battleNumber)
        {
            var battles = AllBattles;
            if (battleNumber < 1 || battleNumber > battles.Count) return null;
            return battles[battleNumber - 1];
        }

        /// <summary>Returns all battles in a specific act (1-3).</summary>
        public static List<CampaignBattleData> GetBattlesInAct(int act)
        {
            var result = new List<CampaignBattleData>();
            foreach (var b in AllBattles)
            {
                if (b.Act == act) result.Add(b);
            }
            return result;
        }

        private static void BuildDatabase()
        {
            _allBattles = new List<CampaignBattleData>(30);

            // ===== ACT 1: The Rising Storm (Battles 1-10) =====

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 1, Name = "First Muster", Act = 1, PointBudget = 10,
                TerrainType = "Open",
                TeachingFocus = "Basic placement and auto-battle",
                NarrativeIntro = "Your first command. A ragtag company of infantry awaits your orders. The enemy approaches across an open field — there is nowhere to hide. Place your men and hold the line.",
                EnemyArmy = new List<EnemyUnitPlacement>
                {
                    new EnemyUnitPlacement("LineInfantry", 4, 9),
                    new EnemyUnitPlacement("LineInfantry", 5, 9),
                    new EnemyUnitPlacement("LineInfantry", 6, 9),
                    new EnemyUnitPlacement("Militia", 3, 10),
                    new EnemyUnitPlacement("Militia", 7, 10),
                }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 2, Name = "The Crossroads", Act = 1, PointBudget = 12,
                TerrainType = "Open + Forest",
                TeachingFocus = "Terrain: forest defense bonus",
                NarrativeIntro = "The crossroads must be held. The enemy has taken position in the treeline — their forest cover will blunt your attacks. Use the terrain wisely, or find a way around.",
                EnemyArmy = new List<EnemyUnitPlacement>
                {
                    new EnemyUnitPlacement("LineInfantry", 3, 8),
                    new EnemyUnitPlacement("LineInfantry", 5, 8),
                    new EnemyUnitPlacement("LineInfantry", 7, 8),
                    new EnemyUnitPlacement("Militia", 4, 9),
                    new EnemyUnitPlacement("Militia", 6, 9),
                    new EnemyUnitPlacement("Militia", 5, 10),
                }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 3, Name = "Cavalry Arrives", Act = 1, PointBudget = 14,
                TerrainType = "Open",
                TeachingFocus = "Using fast units to flank",
                NarrativeIntro = "Reinforcements! A squadron of cavalry joins your ranks. Their speed can turn the enemy's flank — but they're fragile if caught in a prolonged fight.",
                UnlocksUnitTypes = new List<string> { "Cavalry" },
                EnemyArmy = new List<EnemyUnitPlacement>
                {
                    new EnemyUnitPlacement("LineInfantry", 3, 9),
                    new EnemyUnitPlacement("LineInfantry", 4, 9),
                    new EnemyUnitPlacement("LineInfantry", 5, 9),
                    new EnemyUnitPlacement("LineInfantry", 6, 9),
                    new EnemyUnitPlacement("LineInfantry", 7, 9),
                    new EnemyUnitPlacement("Militia", 2, 10),
                    new EnemyUnitPlacement("Militia", 8, 10),
                }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 4, Name = "Hold the Ridge", Act = 1, PointBudget = 14,
                TerrainType = "Hills",
                TeachingFocus = "Terrain: elevation advantage",
                NarrativeIntro = "The high ground is ours — for now. The enemy marches uphill toward your position. Use the ridge to rain fire down upon them.",
                EnemyArmy = new List<EnemyUnitPlacement>
                {
                    new EnemyUnitPlacement("LineInfantry", 3, 7),
                    new EnemyUnitPlacement("LineInfantry", 5, 7),
                    new EnemyUnitPlacement("LineInfantry", 7, 7),
                    new EnemyUnitPlacement("Cavalry", 2, 8),
                    new EnemyUnitPlacement("Militia", 4, 8),
                    new EnemyUnitPlacement("Militia", 6, 8),
                }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 5, Name = "Under the Guns", Act = 1, PointBudget = 16,
                TerrainType = "Open + Hill",
                TeachingFocus = "Ranged bombardment, line of sight",
                NarrativeIntro = "The thunder of cannon joins the fray. Your new artillery battery can devastate packed formations from afar — but protect them, for they cannot protect themselves.",
                UnlocksUnitTypes = new List<string> { "Artillery" },
                EnemyArmy = new List<EnemyUnitPlacement>
                {
                    new EnemyUnitPlacement("LineInfantry", 4, 8),
                    new EnemyUnitPlacement("LineInfantry", 5, 8),
                    new EnemyUnitPlacement("LineInfantry", 6, 8),
                    new EnemyUnitPlacement("Cavalry", 2, 9),
                    new EnemyUnitPlacement("Cavalry", 8, 9),
                    new EnemyUnitPlacement("Militia", 3, 9),
                    new EnemyUnitPlacement("Militia", 7, 9),
                }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 6, Name = "River Crossing", Act = 1, PointBudget = 16,
                TerrainType = "River + Bridge",
                TeachingFocus = "Terrain: rivers and chokepoints",
                NarrativeIntro = "A river bars your path. The bridge is narrow — a chokepoint that favors the defender. But perhaps your cavalry can find a ford upstream...",
                EnemyArmy = new List<EnemyUnitPlacement>
                {
                    new EnemyUnitPlacement("LineInfantry", 5, 8),
                    new EnemyUnitPlacement("LineInfantry", 6, 8),
                    new EnemyUnitPlacement("LineInfantry", 4, 9),
                    new EnemyUnitPlacement("LineInfantry", 5, 9),
                    new EnemyUnitPlacement("Artillery", 5, 10),
                    new EnemyUnitPlacement("Militia", 3, 8),
                    new EnemyUnitPlacement("Militia", 7, 8),
                }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 7, Name = "The Long Game", Act = 1, PointBudget = 18,
                TerrainType = "Forest + Hill",
                TeachingFocus = "Attrition strategy, commander abilities",
                NarrativeIntro = "General Kutuzov sends his regards — and his strategy: patience. The enemy is well-entrenched in the forest. This will be a war of attrition.",
                UnlocksCommander = "Kutuzov",
                EnemyArmy = new List<EnemyUnitPlacement>
                {
                    new EnemyUnitPlacement("LineInfantry", 3, 7),
                    new EnemyUnitPlacement("LineInfantry", 4, 7),
                    new EnemyUnitPlacement("LineInfantry", 6, 7),
                    new EnemyUnitPlacement("LineInfantry", 7, 7),
                    new EnemyUnitPlacement("Cavalry", 2, 8),
                    new EnemyUnitPlacement("Artillery", 5, 9),
                    new EnemyUnitPlacement("Militia", 5, 8),
                    new EnemyUnitPlacement("Militia", 8, 8),
                }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 8, Name = "Storm the Walls", Act = 1, PointBudget = 18,
                TerrainType = "Fortification + Town",
                TeachingFocus = "Fortification assault, grenade ability",
                NarrativeIntro = "The enemy has fortified a town. Your new Grenadiers carry grenades that can crack even the stoutest walls. But the approach will be costly.",
                UnlocksUnitTypes = new List<string> { "Grenadier" },
                EnemyArmy = new List<EnemyUnitPlacement>
                {
                    new EnemyUnitPlacement("LineInfantry", 4, 7),
                    new EnemyUnitPlacement("LineInfantry", 5, 7),
                    new EnemyUnitPlacement("LineInfantry", 6, 7),
                    new EnemyUnitPlacement("LineInfantry", 5, 8),
                    new EnemyUnitPlacement("Artillery", 4, 9),
                    new EnemyUnitPlacement("Artillery", 6, 9),
                    new EnemyUnitPlacement("Militia", 3, 8),
                    new EnemyUnitPlacement("Militia", 7, 8),
                }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 9, Name = "Ambush!", Act = 1, PointBudget = 20,
                TerrainType = "Dense Forest",
                TeachingFocus = "Enemy uses flanking and terrain",
                NarrativeIntro = "Intelligence was wrong — the enemy is everywhere! Cavalry bursts from the treeline on both flanks. Form square and hold!",
                EnemyArmy = new List<EnemyUnitPlacement>
                {
                    new EnemyUnitPlacement("Cavalry", 1, 7),
                    new EnemyUnitPlacement("Cavalry", 10, 7),
                    new EnemyUnitPlacement("Cavalry", 2, 8),
                    new EnemyUnitPlacement("Cavalry", 9, 8),
                    new EnemyUnitPlacement("LineInfantry", 5, 9),
                    new EnemyUnitPlacement("LineInfantry", 6, 9),
                    new EnemyUnitPlacement("Militia", 4, 10),
                    new EnemyUnitPlacement("Militia", 5, 10),
                    new EnemyUnitPlacement("Militia", 6, 10),
                    new EnemyUnitPlacement("Militia", 7, 10),
                }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 10, Name = "Battle of the Pass", Act = 1, PointBudget = 22,
                TerrainType = "Narrow (river both sides)",
                TeachingFocus = "Long-range precision, choke defense",
                NarrativeIntro = "The mountain pass is narrow — rivers on both sides funnel the enemy into your guns. Your new Riflemen can pick off officers at extreme range. Make every shot count.",
                UnlocksUnitTypes = new List<string> { "Rifleman" },
                EnemyArmy = new List<EnemyUnitPlacement>
                {
                    new EnemyUnitPlacement("Grenadier", 5, 7),
                    new EnemyUnitPlacement("Grenadier", 6, 7),
                    new EnemyUnitPlacement("LineInfantry", 4, 8),
                    new EnemyUnitPlacement("LineInfantry", 5, 8),
                    new EnemyUnitPlacement("LineInfantry", 6, 8),
                    new EnemyUnitPlacement("LineInfantry", 7, 8),
                    new EnemyUnitPlacement("Cavalry", 5, 9),
                    new EnemyUnitPlacement("Artillery", 5, 10),
                    new EnemyUnitPlacement("Artillery", 6, 10),
                }
            });

            // ===== ACT 2: The Grand Campaign (Battles 11-20) =====

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 11, Name = "Open Plains", Act = 2, PointBudget = 24,
                TerrainType = "Wide Open",
                TeachingFocus = "Large-scale combined arms",
                NarrativeIntro = "The grand campaign begins. Open plains stretch in every direction — no terrain advantage for either side. Only tactics and composition will decide this fight."
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 12, Name = "The Vanguard", Act = 2, PointBudget = 26,
                TerrainType = "Open + Mud",
                TeachingFocus = "Hit-and-run, cavalry rush",
                NarrativeIntro = "Blücher's hussars ride at the vanguard. Their speed is unmatched — strike fast, strike hard, and vanish before the enemy can respond.",
                UnlocksUnitTypes = new List<string> { "Hussar" },
                UnlocksCommander = "Blücher"
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 13, Name = "Siege of the Fort", Act = 2, PointBudget = 26,
                TerrainType = "Heavy Fortification",
                TeachingFocus = "Breaking entrenched defenders",
                NarrativeIntro = "The fortress must fall. Stone walls and earthworks protect the garrison. Your artillery will need to soften them before the infantry goes in."
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 14, Name = "The Fog of War", Act = 2, PointBudget = 28,
                TerrainType = "Forest + River",
                TeachingFocus = "Fog of war: hidden enemy placement",
                NarrativeIntro = "Scouts report enemy movement in the mist, but their numbers and disposition are unknown. Build an army that can handle anything.",
                FogOfWar = true
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 15, Name = "Heavy Horse", Act = 2, PointBudget = 28,
                TerrainType = "Open + Hill",
                TeachingFocus = "Armored charge, heavy cavalry tactics",
                NarrativeIntro = "The ground shakes beneath iron-shod hooves. The Cuirassiers are the hammer — find the anvil, and nothing will survive between them.",
                UnlocksUnitTypes = new List<string> { "Cuirassier" }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 16, Name = "Town Fight", Act = 2, PointBudget = 30,
                TerrainType = "Dense Town",
                TeachingFocus = "Urban combat, close quarters",
                NarrativeIntro = "Street by street, house by house. The town must be taken. Range means nothing in these narrow lanes — this will be decided at bayonet point."
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 17, Name = "Running Battle", Act = 2, PointBudget = 30,
                TerrainType = "Varied",
                TeachingFocus = "Mobile artillery repositioning",
                NarrativeIntro = "The Horse Artillery gallops into position, fires, and moves before the enemy can respond. Mobile firepower changes everything.",
                UnlocksUnitTypes = new List<string> { "HorseArtillery" }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 18, Name = "Rearguard", Act = 2, PointBudget = 24,
                TerrainType = "River + Bridge + Forest",
                TeachingFocus = "Comeback mechanic, fighting outnumbered",
                NarrativeIntro = "The retreat is on. You command the rearguard — fewer troops, against a larger force. Moore's strategy is simple: the more you lose, the harder you fight.",
                UnlocksCommander = "Moore"
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 19, Name = "Dig In", Act = 2, PointBudget = 32,
                TerrainType = "Open + River",
                TeachingFocus = "Creating fortifications mid-battle",
                NarrativeIntro = "Your Sappers can reshape the battlefield itself. Earthworks rise where there were none — turn open ground into a fortress.",
                UnlocksUnitTypes = new List<string> { "Sapper" }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 20, Name = "The Grand Battery", Act = 2, PointBudget = 35,
                TerrainType = "Hill + Open",
                TeachingFocus = "Massive artillery duel, counter-battery",
                NarrativeIntro = "Sixty guns line the ridge. The enemy has matched you cannon for cannon. This will be the greatest artillery duel of the war."
            });

            // ===== ACT 3: The Final Act (Battles 21-30) =====

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 21, Name = "Winter March", Act = 3, PointBudget = 35,
                TerrainType = "Mud + River",
                TeachingFocus = "Terrain penalty gauntlet",
                NarrativeIntro = "The roads have turned to rivers of mud. Every step costs double. The enemy knows it too — they've chosen this ground to slow your advance."
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 22, Name = "The Emperor's Guard", Act = 3, PointBudget = 38,
                TerrainType = "Open + Hill",
                TeachingFocus = "Elite infantry, last stand mechanic",
                NarrativeIntro = "The Old Guard has never been defeated. These veterans of a hundred battles will not break, will not retreat, and will fight harder as they fall.",
                UnlocksUnitTypes = new List<string> { "OldGuard" }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 23, Name = "Desperate Defense", Act = 3, PointBudget = 38,
                TerrainType = "Fortification",
                TeachingFocus = "Outnumbered survival",
                NarrativeIntro = "They outnumber you three to one. The fortifications are your only hope. Hold until reinforcements arrive — or until there is no one left to hold."
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 24, Name = "Rockets' Red Glare", Act = 3, PointBudget = 40,
                TerrainType = "Open",
                TeachingFocus = "Unpredictable AoE, surgical strikes",
                NarrativeIntro = "The Congreve rockets are as terrifying as they are unreliable. They might devastate the enemy — or your own men. Ney's precision can balance the chaos.",
                UnlocksUnitTypes = new List<string> { "RocketBattery" },
                UnlocksCommander = "Ney"
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 25, Name = "The Hornet's Nest", Act = 3, PointBudget = 40,
                TerrainType = "Dense Forest + Town",
                TeachingFocus = "Complex terrain puzzle",
                NarrativeIntro = "Forest and town intertwine in a maze of cover and chokepoints. The enemy knows every alley and thicket. You'll need every trick you've learned."
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 26, Name = "Lancer's Charge", Act = 3, PointBudget = 42,
                TerrainType = "Open + Mud",
                TeachingFocus = "Anti-cavalry tactics",
                NarrativeIntro = "The Polish Lancers ride to war. Their lances brace against the charge — cavalry that kills cavalry. The rock-paper-scissors grows sharper.",
                UnlocksUnitTypes = new List<string> { "Lancer" }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 27, Name = "All Guns Blazing", Act = 3, PointBudget = 42,
                TerrainType = "Hill + Fortification",
                TeachingFocus = "Full roster mastery",
                NarrativeIntro = "Every weapon in your arsenal will be needed. The enemy has matched you unit for unit, formation for formation. Only superior generalship will prevail."
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 28, Name = "The Versatile Reserve", Act = 3, PointBudget = 45,
                TerrainType = "Varied",
                TeachingFocus = "Multi-role units, adaptability",
                NarrativeIntro = "The Dragoons can fight mounted or on foot — they adapt to any situation. Versatility is the key to this battle's shifting landscape.",
                UnlocksUnitTypes = new List<string> { "Dragoon" }
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 29, Name = "Eve of Battle", Act = 3, PointBudget = 48,
                TerrainType = "Complex (most terrain types)",
                TeachingFocus = "Everything combined",
                NarrativeIntro = "Tomorrow, the war ends. Tonight, you prepare. Every lesson, every loss, every hard-won victory has led to this moment. One more battle remains."
            });

            _allBattles.Add(new CampaignBattleData
            {
                BattleNumber = 30, Name = "Waterloo", Act = 3, PointBudget = 50,
                TerrainType = "Iconic recreation",
                TeachingFocus = "Final exam — the ultimate battle",
                NarrativeIntro = "Waterloo. The name that will echo through history. Massed infantry, thundering cavalry, roaring cannon — everything converges on this field. Command your finest army. Win the war."
            });
        }
    }
}
