using UnityEngine;
using WarChess.Config;

namespace WarChess.Config
{
    /// <summary>
    /// ScriptableObject holding all tunable game configuration values.
    /// Create one asset at Data/Config/GameConfig.asset.
    /// At battle start, call ToData() to get a plain C# copy for the Logic Layer.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "WarChess/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Grid")]
        public int gridWidth = 10;
        public int gridHeight = 10;

        [Header("Deployment Zones")]
        public int playerDeployMinRow = 1;
        public int playerDeployMaxRow = 3;
        public int enemyDeployMinRowCampaign = 5;
        public int enemyDeployMaxRowCampaign = 10;
        public int enemyDeployMinRowMultiplayer = 8;
        public int enemyDeployMaxRowMultiplayer = 10;

        [Header("Battle Rules")]
        public int maxRounds = 30;
        public int minimumDamage = 1;

        [Header("Flanking Defaults (base 100)")]
        public int defaultFlankSideMultiplier = 130;
        public int defaultFlankRearMultiplier = 200;

        [Header("Terrain Multipliers (base 100)")]
        public int forestDefenseMultiplier = 75;
        public int hillAttackMultiplier = 125;
        public int fortificationDefenseMultiplier = 70;
        public int townDefenseMultiplier = 80;

        [Header("Formation Bonuses (base 100)")]
        public int battleLineDefBonus = 115;
        public int batteryAtkBonus = 120;
        public int cavalryWedgeChargeBonus = 125;
        public int squareDefVsCavalryBonus = 130;
        public int skirmishAtkBonus = 120;
        public int skirmishRangeBonus = 1;

        [Header("Charge")]
        public int chargeMinTilesMoved = 3;
        public int chargeMultiplier = 200;

        [Header("AoE")]
        public int bombardmentSplashPercentage = 50;

        [Header("Strength Scaling (base 100)")]
        [Tooltip("Minimum damage multiplier for damaged units. 100 = disabled, 25 = units always deal at least 25% damage. Artillery types are always exempt.")]
        public int strengthScalingFloor = 25;
        [Tooltip("Old Guard (Unbreakable) uses a gentle linear curve instead of sqrt. This is their minimum damage % at 0 HP. 75 = never below 75% damage.")]
        public int unbreakableStrengthFloor = 75;

        /// <summary>
        /// Converts this SO into a plain C# struct for the Logic Layer.
        /// Call with isMultiplayer to select the correct enemy deployment zone.
        /// </summary>
        public GameConfigData ToData(bool isMultiplayer = false)
        {
            return new GameConfigData(
                gridWidth: gridWidth,
                gridHeight: gridHeight,
                playerDeployMinRow: playerDeployMinRow,
                playerDeployMaxRow: playerDeployMaxRow,
                enemyDeployMinRow: isMultiplayer ? enemyDeployMinRowMultiplayer : enemyDeployMinRowCampaign,
                enemyDeployMaxRow: isMultiplayer ? enemyDeployMaxRowMultiplayer : enemyDeployMaxRowCampaign,
                maxRounds: maxRounds,
                minimumDamage: minimumDamage,
                defaultFlankSideMultiplier: defaultFlankSideMultiplier,
                defaultFlankRearMultiplier: defaultFlankRearMultiplier,
                forestDefenseMultiplier: forestDefenseMultiplier,
                hillAttackMultiplier: hillAttackMultiplier,
                fortificationDefenseMultiplier: fortificationDefenseMultiplier,
                townDefenseMultiplier: townDefenseMultiplier,
                battleLineDefBonus: battleLineDefBonus,
                batteryAtkBonus: batteryAtkBonus,
                cavalryWedgeChargeBonus: cavalryWedgeChargeBonus,
                squareDefVsCavalryBonus: squareDefVsCavalryBonus,
                skirmishAtkBonus: skirmishAtkBonus,
                skirmishRangeBonus: skirmishRangeBonus,
                chargeMinTilesMoved: chargeMinTilesMoved,
                chargeMultiplier: chargeMultiplier,
                bombardmentSplashPercentage: bombardmentSplashPercentage,
                strengthScalingFloor: strengthScalingFloor,
                unbreakableStrengthFloor: unbreakableStrengthFloor
            );
        }
    }
}
