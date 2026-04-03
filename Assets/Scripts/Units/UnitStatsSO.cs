using UnityEngine;
using WarChess.Core;

namespace WarChess.Units
{
    /// <summary>
    /// ScriptableObject defining a unit type's base stats. Create one asset per unit type
    /// in Data/Units/. These are the authoring-time data — at battle start, data is copied
    /// into UnitInstance objects for the Logic Layer.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnit", menuName = "WarChess/Unit Stats")]
    public class UnitStatsSO : ScriptableObject
    {
        [Header("Identity")]
        public string unitName;
        public UnitType unitType;
        [TextArea(2, 4)]
        public string description;

        [Header("Stats")]
        public int hp = 30;
        public int atk = 8;
        public int def = 6;
        public int spd = 3;
        public int rng = 1;
        public int mov = 2;
        public int cost = 3;

        [Header("Targeting")]
        public TargetingPriority targetingPriority = TargetingPriority.Nearest;

        [Header("Ability")]
        public AbilityType abilityType = AbilityType.None;

        [Header("Formation")]
        public FormationType formationType = FormationType.None;
        [Tooltip("For formation counting — e.g., Grenadier counts as LineInfantry")]
        public UnitType countsAsType;

        [Header("Flanking (base 100 = x1.0)")]
        [Tooltip("Damage multiplier when attacked from the side (default 130 = x1.3)")]
        public int flankSideMultiplier = 130;
        [Tooltip("Damage multiplier when attacked from the rear (default 200 = x2.0)")]
        public int flankRearMultiplier = 200;

        /// <summary>
        /// Creates a UnitInstance from this ScriptableObject's data.
        /// </summary>
        public UnitInstance CreateInstance(int id, Owner owner, GridCoord position)
        {
            var facing = owner == Owner.Player ? FacingDirection.North : FacingDirection.South;

            return new UnitInstance(
                id: id,
                name: unitName,
                type: unitType,
                owner: owner,
                hp: hp,
                atk: atk,
                def: def,
                spd: spd,
                rng: rng,
                mov: mov,
                cost: cost,
                flankSideMultiplier: flankSideMultiplier,
                flankRearMultiplier: flankRearMultiplier,
                targetingPriority: targetingPriority,
                ability: abilityType,
                formationType: formationType,
                countsAsType: countsAsType,
                position: position,
                facing: facing
            );
        }
    }
}
