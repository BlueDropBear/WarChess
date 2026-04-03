using System;
using System.Collections.Generic;

namespace WarChess.Campaign
{
    /// <summary>
    /// Static data defining a single campaign battle. Authored in CampaignDatabase,
    /// not editable at runtime. Pure C# — no Unity dependencies.
    /// </summary>
    [Serializable]
    public class CampaignBattleData
    {
        /// <summary>Battle number (1-30).</summary>
        public int BattleNumber;

        /// <summary>Display name for the battle.</summary>
        public string Name;

        /// <summary>Which act this battle belongs to (1-3).</summary>
        public int Act;

        /// <summary>Player's army point budget for this battle.</summary>
        public int PointBudget;

        /// <summary>Brief narrative text shown before the battle.</summary>
        public string NarrativeIntro;

        /// <summary>Unit types unlocked after completing this battle (empty = none).</summary>
        public List<string> UnlocksUnitTypes;

        /// <summary>Commander unlocked after completing this battle (empty = none).</summary>
        public string UnlocksCommander;

        /// <summary>Terrain description for map generation.</summary>
        public string TerrainType;

        /// <summary>What this battle teaches the player.</summary>
        public string TeachingFocus;

        /// <summary>Enemy army composition — list of (unitTypeId, x, y).</summary>
        public List<EnemyUnitPlacement> EnemyArmy;

        /// <summary>Whether this battle uses fog of war.</summary>
        public bool FogOfWar;

        /// <summary>Minimum row the enemy can deploy to (default 5 for campaign).</summary>
        public int EnemyDeployMinRow;

        /// <summary>Maximum row the enemy can deploy to (default 10).</summary>
        public int EnemyDeployMaxRow;

        public CampaignBattleData()
        {
            UnlocksUnitTypes = new List<string>();
            UnlocksCommander = "";
            EnemyArmy = new List<EnemyUnitPlacement>();
            EnemyDeployMinRow = 5;
            EnemyDeployMaxRow = 10;
        }
    }

    /// <summary>
    /// An enemy unit placement in a campaign battle.
    /// </summary>
    [Serializable]
    public class EnemyUnitPlacement
    {
        public string UnitTypeId;
        public int X;
        public int Y;

        public EnemyUnitPlacement() { }

        public EnemyUnitPlacement(string unitTypeId, int x, int y)
        {
            UnitTypeId = unitTypeId;
            X = x;
            Y = y;
        }
    }
}
