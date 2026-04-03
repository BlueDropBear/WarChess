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
            int chargeMinTilesMoved, int chargeMultiplier)
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
        }

        /// <summary>
        /// Returns the default config matching GDD values.
        /// </summary>
        public static GameConfigData Default => new GameConfigData(
            gridWidth: 10, gridHeight: 10,
            playerDeployMinRow: 1, playerDeployMaxRow: 3,
            enemyDeployMinRow: 8, enemyDeployMaxRow: 10,
            maxRounds: 30, minimumDamage: 1,
            defaultFlankSideMultiplier: 130, defaultFlankRearMultiplier: 200,
            forestDefenseMultiplier: 75, hillAttackMultiplier: 125,
            fortificationDefenseMultiplier: 70, townDefenseMultiplier: 80,
            battleLineDefBonus: 115, batteryAtkBonus: 120,
            cavalryWedgeChargeBonus: 125, squareDefVsCavalryBonus: 130,
            skirmishAtkBonus: 120, skirmishRangeBonus: 1,
            chargeMinTilesMoved: 3, chargeMultiplier: 200
        );
    }
}
