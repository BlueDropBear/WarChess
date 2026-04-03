using System.Collections.Generic;

namespace WarChess.Config
{
    /// <summary>
    /// Plain C# data copy of GameConfigSO. Used by the Logic Layer so it has
    /// no dependency on UnityEngine. All multipliers are base-100 integers.
    /// </summary>
    public readonly struct GameConfigData
    {
        // Grid dimensions
        public readonly int GridWidth;
        public readonly int GridHeight;

        // Deployment zones
        public readonly int PlayerDeployMinRow;
        public readonly int PlayerDeployMaxRow;
        public readonly int EnemyDeployMinRow;
        public readonly int EnemyDeployMaxRow;

        // Battle rules
        public readonly int MaxRounds;
        public readonly int MinimumDamage;

        // Flanking defaults (per-unit overrides in UnitInstance)
        public readonly int DefaultFlankSideMultiplier;
        public readonly int DefaultFlankRearMultiplier;

        // Terrain multipliers (base 100)
        public readonly int ForestDefenseMultiplier;
        public readonly int HillAttackMultiplier;
        public readonly int FortificationDefenseMultiplier;
        public readonly int TownDefenseMultiplier;

        // Formation bonuses (base 100)
        public readonly int BattleLineDefBonus;
        public readonly int BatteryAtkBonus;
        public readonly int CavalryWedgeChargeBonus;
        public readonly int SquareDefVsCavalryBonus;
        public readonly int SkirmishAtkBonus;
        public readonly int SkirmishRangeBonus;

        // Charge
        public readonly int ChargeMinTilesMoved;
        public readonly int ChargeMultiplier;

        // AoE
        public readonly int BombardmentSplashPercentage;

        public GameConfigData(
            int gridWidth, int gridHeight,
            int playerDeployMinRow, int playerDeployMaxRow,
            int enemyDeployMinRow, int enemyDeployMaxRow,
            int maxRounds, int minimumDamage,
            int defaultFlankSideMultiplier, int defaultFlankRearMultiplier,
            int forestDefenseMultiplier, int hillAttackMultiplier,
            int fortificationDefenseMultiplier, int townDefenseMultiplier,
            int battleLineDefBonus, int batteryAtkBonus,
            int cavalryWedgeChargeBonus, int squareDefVsCavalryBonus,
            int skirmishAtkBonus, int skirmishRangeBonus,
            int chargeMinTilesMoved, int chargeMultiplier,
            int bombardmentSplashPercentage = 50)
        {
            GridWidth = gridWidth;
            GridHeight = gridHeight;
            PlayerDeployMinRow = playerDeployMinRow;
            PlayerDeployMaxRow = playerDeployMaxRow;
            EnemyDeployMinRow = enemyDeployMinRow;
            EnemyDeployMaxRow = enemyDeployMaxRow;
            MaxRounds = maxRounds;
            MinimumDamage = minimumDamage;
            DefaultFlankSideMultiplier = defaultFlankSideMultiplier;
            DefaultFlankRearMultiplier = defaultFlankRearMultiplier;
            ForestDefenseMultiplier = forestDefenseMultiplier;
            HillAttackMultiplier = hillAttackMultiplier;
            FortificationDefenseMultiplier = fortificationDefenseMultiplier;
            TownDefenseMultiplier = townDefenseMultiplier;
            BattleLineDefBonus = battleLineDefBonus;
            BatteryAtkBonus = batteryAtkBonus;
            CavalryWedgeChargeBonus = cavalryWedgeChargeBonus;
            SquareDefVsCavalryBonus = squareDefVsCavalryBonus;
            SkirmishAtkBonus = skirmishAtkBonus;
            SkirmishRangeBonus = skirmishRangeBonus;
            ChargeMinTilesMoved = chargeMinTilesMoved;
            ChargeMultiplier = chargeMultiplier;
            BombardmentSplashPercentage = bombardmentSplashPercentage;
        }

        /// <summary>
        /// Returns the default config matching GDD campaign values.
        /// For multiplayer, use GameConfigSO.ToData(isMultiplayer: true) which sets enemy rows 8-10.
        /// </summary>
        public static GameConfigData Default => new GameConfigData(
            gridWidth: 10, gridHeight: 10,
            playerDeployMinRow: 1, playerDeployMaxRow: 3,
            enemyDeployMinRow: 5, enemyDeployMaxRow: 10,
            maxRounds: 30, minimumDamage: 1,
            defaultFlankSideMultiplier: 130, defaultFlankRearMultiplier: 200,
            forestDefenseMultiplier: 75, hillAttackMultiplier: 125,
            fortificationDefenseMultiplier: 70, townDefenseMultiplier: 80,
            battleLineDefBonus: 115, batteryAtkBonus: 120,
            cavalryWedgeChargeBonus: 125, squareDefVsCavalryBonus: 130,
            skirmishAtkBonus: 120, skirmishRangeBonus: 1,
            chargeMinTilesMoved: 3, chargeMultiplier: 200,
            bombardmentSplashPercentage: 50
        );

        /// <summary>
        /// Returns the canonical unit cost dictionary from GDD Section 3.2.
        /// Single source of truth — all systems should use this instead of local copies.
        /// </summary>
        public static Dictionary<string, int> GetUnitCosts()
        {
            return new Dictionary<string, int>
            {
                {"LineInfantry", 3}, {"Militia", 1}, {"Cavalry", 5}, {"Artillery", 6},
                {"Grenadier", 7}, {"Rifleman", 5}, {"Hussar", 4}, {"Cuirassier", 8},
                {"HorseArtillery", 6}, {"Sapper", 4}, {"OldGuard", 10}, {"RocketBattery", 7},
                {"Lancer", 5}, {"Dragoon", 6}
            };
        }
    }
}
