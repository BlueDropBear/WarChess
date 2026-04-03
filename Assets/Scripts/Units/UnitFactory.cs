using System.Collections.Generic;
using WarChess.Core;

namespace WarChess.Units
{
    /// <summary>
    /// Creates UnitInstance objects from hardcoded GDD stats. Used for prototype
    /// testing when ScriptableObject assets haven't been created in the editor yet.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class UnitFactory
    {
        private static int _nextId = 1;

        /// <summary>
        /// Resets the ID counter. Call at the start of each battle.
        /// </summary>
        public static void ResetIds()
        {
            _nextId = 1;
        }

        /// <summary>
        /// Creates a Line Infantry unit with GDD stats.
        /// HP:30 ATK:8 DEF:6 SPD:3 RNG:1 MOV:2 COST:3
        /// </summary>
        public static UnitInstance CreateLineInfantry(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Line Infantry", type: UnitType.LineInfantry, owner: owner,
                hp: 30, atk: 8, def: 6, spd: 3, rng: 1, mov: 2, cost: 3,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.Nearest,
                ability: AbilityType.None,
                formationType: FormationType.BattleLine,
                countsAsType: UnitType.LineInfantry,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a Militia unit with GDD stats.
        /// HP:18 ATK:5 DEF:3 SPD:4 RNG:1 MOV:2 COST:1
        /// </summary>
        public static UnitInstance CreateMilitia(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Militia", type: UnitType.Militia, owner: owner,
                hp: 18, atk: 5, def: 3, spd: 4, rng: 1, mov: 2, cost: 1,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.Nearest,
                ability: AbilityType.StrengthInNumbers,
                formationType: FormationType.None,
                countsAsType: UnitType.Militia,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a Cavalry unit with GDD stats.
        /// HP:25 ATK:10 DEF:4 SPD:6 RNG:1 MOV:4 COST:5
        /// </summary>
        public static UnitInstance CreateCavalry(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Cavalry", type: UnitType.Cavalry, owner: owner,
                hp: 25, atk: 10, def: 4, spd: 6, rng: 1, mov: 4, cost: 5,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.HighestThreat,
                ability: AbilityType.Charge,
                formationType: FormationType.CavalryWedge,
                countsAsType: UnitType.Cavalry,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates an Artillery unit with GDD stats.
        /// HP:15 ATK:14 DEF:2 SPD:1 RNG:4 MOV:1 COST:6
        /// </summary>
        public static UnitInstance CreateArtillery(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Artillery", type: UnitType.Artillery, owner: owner,
                hp: 15, atk: 14, def: 2, spd: 1, rng: 4, mov: 1, cost: 6,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.Nearest,
                ability: AbilityType.Bombardment,
                formationType: FormationType.Battery,
                countsAsType: UnitType.Artillery,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a predefined player army for testing. 3 Infantry + 1 Cavalry + 1 Artillery = 20 points.
        /// </summary>
        public static List<UnitInstance> CreateTestPlayerArmy()
        {
            return new List<UnitInstance>
            {
                CreateLineInfantry(Owner.Player, new GridCoord(4, 1)),
                CreateLineInfantry(Owner.Player, new GridCoord(5, 1)),
                CreateLineInfantry(Owner.Player, new GridCoord(6, 1)),
                CreateCavalry(Owner.Player, new GridCoord(2, 2)),
                CreateArtillery(Owner.Player, new GridCoord(5, 3)),
            };
        }

        /// <summary>
        /// Creates a predefined enemy army for testing. 3 Infantry + 1 Cavalry + 1 Artillery = 20 points.
        /// </summary>
        public static List<UnitInstance> CreateTestEnemyArmy()
        {
            return new List<UnitInstance>
            {
                CreateLineInfantry(Owner.Enemy, new GridCoord(4, 10)),
                CreateLineInfantry(Owner.Enemy, new GridCoord(5, 10)),
                CreateLineInfantry(Owner.Enemy, new GridCoord(6, 10)),
                CreateCavalry(Owner.Enemy, new GridCoord(8, 9)),
                CreateArtillery(Owner.Enemy, new GridCoord(5, 8)),
            };
        }
    }
}
