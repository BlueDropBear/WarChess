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
        /// Creates a Grenadier unit. HP:40 ATK:12 DEF:8 SPD:2 RNG:1 MOV:2 COST:7
        /// </summary>
        public static UnitInstance CreateGrenadier(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Grenadier", type: UnitType.Grenadier, owner: owner,
                hp: 40, atk: 12, def: 8, spd: 2, rng: 1, mov: 2, cost: 7,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.Nearest,
                ability: AbilityType.Grenade,
                formationType: FormationType.BattleLine,
                countsAsType: UnitType.LineInfantry,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a Rifleman unit. HP:20 ATK:11 DEF:3 SPD:5 RNG:3 MOV:2 COST:5
        /// </summary>
        public static UnitInstance CreateRifleman(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Rifleman", type: UnitType.Rifleman, owner: owner,
                hp: 20, atk: 11, def: 3, spd: 5, rng: 3, mov: 2, cost: 5,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.Weakest,
                ability: AbilityType.AimedShot,
                formationType: FormationType.SkirmishScreen,
                countsAsType: UnitType.Rifleman,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a Hussar unit. HP:20 ATK:7 DEF:3 SPD:8 RNG:1 MOV:5 COST:4
        /// </summary>
        public static UnitInstance CreateHussar(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Hussar", type: UnitType.Hussar, owner: owner,
                hp: 20, atk: 7, def: 3, spd: 8, rng: 1, mov: 5, cost: 4,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.Weakest,
                ability: AbilityType.HitAndRun,
                formationType: FormationType.CavalryWedge,
                countsAsType: UnitType.Cavalry,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a Cuirassier unit. HP:35 ATK:13 DEF:7 SPD:4 RNG:1 MOV:3 COST:8
        /// </summary>
        public static UnitInstance CreateCuirassier(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Cuirassier", type: UnitType.Cuirassier, owner: owner,
                hp: 35, atk: 13, def: 7, spd: 4, rng: 1, mov: 3, cost: 8,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.HighestThreat,
                ability: AbilityType.ArmoredCharge,
                formationType: FormationType.CavalryWedge,
                countsAsType: UnitType.Cavalry,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a Horse Artillery unit. HP:12 ATK:10 DEF:2 SPD:5 RNG:3 MOV:3 COST:6
        /// </summary>
        public static UnitInstance CreateHorseArtillery(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Horse Artillery", type: UnitType.HorseArtillery, owner: owner,
                hp: 12, atk: 10, def: 2, spd: 5, rng: 3, mov: 3, cost: 6,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.Nearest,
                ability: AbilityType.LimberedUp,
                formationType: FormationType.Battery,
                countsAsType: UnitType.Artillery,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a Sapper unit. HP:22 ATK:6 DEF:5 SPD:3 RNG:1 MOV:2 COST:4
        /// </summary>
        public static UnitInstance CreateSapper(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Sapper", type: UnitType.Sapper, owner: owner,
                hp: 22, atk: 6, def: 5, spd: 3, rng: 1, mov: 2, cost: 4,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.Nearest,
                ability: AbilityType.Entrench,
                formationType: FormationType.None,
                countsAsType: UnitType.Sapper,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates an Old Guard unit. HP:45 ATK:14 DEF:10 SPD:3 RNG:1 MOV:2 COST:10
        /// </summary>
        public static UnitInstance CreateOldGuard(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Old Guard", type: UnitType.OldGuard, owner: owner,
                hp: 45, atk: 14, def: 10, spd: 3, rng: 1, mov: 2, cost: 10,
                flankSideMultiplier: 130, flankRearMultiplier: 150, // Reduced rear vulnerability per GDD
                targetingPriority: TargetingPriority.Nearest,
                ability: AbilityType.Unbreakable,
                formationType: FormationType.BattleLine,
                countsAsType: UnitType.LineInfantry,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a Rocket Battery unit. HP:10 ATK:16 DEF:1 SPD:2 RNG:5 MOV:1 COST:7
        /// </summary>
        public static UnitInstance CreateRocketBattery(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Rocket Battery", type: UnitType.RocketBattery, owner: owner,
                hp: 10, atk: 16, def: 1, spd: 2, rng: 5, mov: 1, cost: 7,
                flankSideMultiplier: 130, flankRearMultiplier: 250, // Extra fragile from behind per GDD
                targetingPriority: TargetingPriority.Random,
                ability: AbilityType.CongreveBarrage,
                formationType: FormationType.None,
                countsAsType: UnitType.RocketBattery,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a Lancer unit. HP:28 ATK:11 DEF:5 SPD:5 RNG:1 MOV:3 COST:5
        /// </summary>
        public static UnitInstance CreateLancer(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Lancer", type: UnitType.Lancer, owner: owner,
                hp: 28, atk: 11, def: 5, spd: 5, rng: 1, mov: 3, cost: 5,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.HighestThreat,
                ability: AbilityType.Brace,
                formationType: FormationType.CavalryWedge,
                countsAsType: UnitType.Cavalry,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a Dragoon unit. HP:28 ATK:9 DEF:5 SPD:5 RNG:1 MOV:4 COST:6
        /// </summary>
        public static UnitInstance CreateDragoon(Owner owner, GridCoord position)
        {
            return new UnitInstance(
                id: _nextId++, name: "Dragoon", type: UnitType.Dragoon, owner: owner,
                hp: 28, atk: 9, def: 5, spd: 5, rng: 1, mov: 4, cost: 6,
                flankSideMultiplier: 130, flankRearMultiplier: 200,
                targetingPriority: TargetingPriority.ArtilleryFirst,
                ability: AbilityType.Dismount,
                formationType: FormationType.CavalryWedge,
                countsAsType: UnitType.Cavalry,
                position: position,
                facing: owner == Owner.Player ? FacingDirection.North : FacingDirection.South);
        }

        /// <summary>
        /// Creates a unit by type name string. Used by campaign system and save/load.
        /// Returns null if the type is unknown.
        /// </summary>
        public static UnitInstance CreateByTypeName(string typeName, Owner owner, GridCoord position)
        {
            return typeName switch
            {
                "LineInfantry" => CreateLineInfantry(owner, position),
                "Militia" => CreateMilitia(owner, position),
                "Cavalry" => CreateCavalry(owner, position),
                "Artillery" => CreateArtillery(owner, position),
                "Grenadier" => CreateGrenadier(owner, position),
                "Rifleman" => CreateRifleman(owner, position),
                "Hussar" => CreateHussar(owner, position),
                "Cuirassier" => CreateCuirassier(owner, position),
                "HorseArtillery" => CreateHorseArtillery(owner, position),
                "Sapper" => CreateSapper(owner, position),
                "OldGuard" => CreateOldGuard(owner, position),
                "RocketBattery" => CreateRocketBattery(owner, position),
                "Lancer" => CreateLancer(owner, position),
                "Dragoon" => CreateDragoon(owner, position),
                _ => null
            };
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
