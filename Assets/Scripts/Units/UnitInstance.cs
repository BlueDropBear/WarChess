using System;
using WarChess.Core;

namespace WarChess.Units
{
    /// <summary>
    /// Runtime state of a unit during battle. Created from ScriptableObject data
    /// at battle start. Pure C# — no Unity dependencies.
    /// </summary>
    public class UnitInstance
    {
        /// <summary>Unique ID per battle, used for deterministic tie-breaking.</summary>
        public int Id { get; }

        public string Name { get; }
        public UnitType Type { get; }
        public Owner Owner { get; }

        // Base stats (copied from SO, may be modified by officers/difficulty)
        public int MaxHp { get; private set; }
        public int Atk { get; private set; }
        public int Def { get; private set; }
        public int Spd { get; private set; }
        public int Rng { get; private set; }
        public int Mov { get; private set; }
        public int Cost { get; }

        // Flanking multipliers (base 100, per-unit from SO)
        public int FlankSideMultiplier { get; }
        public int FlankRearMultiplier { get; }

        // Targeting and abilities
        public TargetingPriority TargetingPriority { get; }
        public AbilityType Ability { get; }
        public FormationType FormationType { get; }
        public UnitType CountsAsType { get; }

        // Mutable battle state
        public int CurrentHp { get; private set; }
        public GridCoord Position { get; set; }
        public FacingDirection Facing { get; set; }

        // Per-round tracking
        public bool HasMovedThisRound { get; set; }
        public bool HasAttackedThisRound { get; set; }
        public int TilesMovedThisRound { get; set; }

        // Persistent battle tracking
        public bool HasChargedThisBattle { get; set; }
        public bool IsDismounted { get; set; }

        public bool IsAlive => CurrentHp > 0;

        public UnitInstance(
            int id, string name, UnitType type, Owner owner,
            int hp, int atk, int def, int spd, int rng, int mov, int cost,
            int flankSideMultiplier, int flankRearMultiplier,
            TargetingPriority targetingPriority, AbilityType ability,
            FormationType formationType, UnitType countsAsType,
            GridCoord position, FacingDirection facing)
        {
            Id = id;
            Name = name;
            Type = type;
            Owner = owner;
            MaxHp = hp;
            CurrentHp = hp;
            Atk = atk;
            Def = def;
            Spd = spd;
            Rng = rng;
            Mov = mov;
            Cost = cost;
            FlankSideMultiplier = flankSideMultiplier;
            FlankRearMultiplier = flankRearMultiplier;
            TargetingPriority = targetingPriority;
            Ability = ability;
            FormationType = formationType;
            CountsAsType = countsAsType;
            Position = position;
            Facing = facing;
        }

        /// <summary>
        /// Applies damage to this unit. CurrentHp will not go below 0.
        /// </summary>
        public void TakeDamage(int amount)
        {
            CurrentHp = Math.Max(CurrentHp - amount, 0);
        }

        /// <summary>
        /// Heals this unit. CurrentHp will not exceed MaxHp.
        /// </summary>
        public void Heal(int amount)
        {
            CurrentHp = Math.Min(CurrentHp + amount, MaxHp);
        }

        /// <summary>
        /// Resets per-round flags at the start of each round.
        /// </summary>
        public void ResetRoundState()
        {
            HasMovedThisRound = false;
            HasAttackedThisRound = false;
            TilesMovedThisRound = 0;
        }

        /// <summary>
        /// Applies difficulty scaling or officer modifiers to base stats.
        /// Sets ATK, DEF, and MaxHp (also adjusts CurrentHp proportionally).
        /// </summary>
        public void ApplyStatScale(int newAtk, int newDef, int newMaxHp)
        {
            Atk = newAtk;
            Def = newDef;
            int oldMax = MaxHp;
            MaxHp = newMaxHp;
            // Scale current HP proportionally (rounded to nearest)
            CurrentHp = oldMax > 0 ? (CurrentHp * newMaxHp + oldMax / 2) / oldMax : newMaxHp;
        }

        /// <summary>
        /// Applies flat stat modifications for officers or testing.
        /// MOV clamped to minimum 1, RNG to minimum 0.
        /// </summary>
        public void ApplyFlatMods(int movDelta, int rngDelta, int spdDelta)
        {
            Mov = Math.Max(Mov + movDelta, 1);
            Rng = Math.Max(Rng + rngDelta, 0);
            Spd = Math.Max(Spd + spdDelta, 1);
        }

        /// <summary>
        /// Applies Dragoon dismount: reduces MOV, increases DEF and ATK permanently.
        /// Values are configurable via GameConfigData.
        /// </summary>
        public void ApplyDismount(int dismountMov = 2, int dismountDefBonus = 3, int dismountAtkBonus = 2)
        {
            if (IsDismounted) return;
            IsDismounted = true;
            Mov = dismountMov;
            Def += dismountDefBonus;
            Atk += dismountAtkBonus;
        }

        /// <summary>
        /// Returns effective range, accounting for bonuses (e.g., Skirmish Screen).
        /// </summary>
        public int GetEffectiveRange(int bonusRange)
        {
            return Rng + bonusRange;
        }
    }
}
