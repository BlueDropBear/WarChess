using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle
{
    /// <summary>
    /// Base class for all battle events. Events are emitted by BattleEngine
    /// and consumed by the Presentation Layer for animation/replay.
    /// Pure C# data objects — no Unity dependencies.
    /// </summary>
    public abstract class BattleEvent
    {
        /// <summary>The round number when this event occurred.</summary>
        public int Round { get; }

        protected BattleEvent(int round)
        {
            Round = round;
        }
    }

    /// <summary>Emitted at the start of each round.</summary>
    public class RoundStartedEvent : BattleEvent
    {
        public int RoundNumber { get; }

        public RoundStartedEvent(int round) : base(round)
        {
            RoundNumber = round;
        }
    }

    /// <summary>Emitted when a unit moves to a new tile.</summary>
    public class UnitMovedEvent : BattleEvent
    {
        public int UnitId { get; }
        public GridCoord From { get; }
        public GridCoord To { get; }
        public int TilesMoved { get; }

        public UnitMovedEvent(int round, int unitId, GridCoord from, GridCoord to, int tilesMoved)
            : base(round)
        {
            UnitId = unitId;
            From = from;
            To = to;
            TilesMoved = tilesMoved;
        }
    }

    /// <summary>Emitted when a unit attacks another unit.</summary>
    public class UnitAttackedEvent : BattleEvent
    {
        public int AttackerId { get; }
        public int DefenderId { get; }
        public int DamageDealt { get; }
        public FlankDirection FlankDirection { get; }
        public bool IsChargeAttack { get; }
        public bool IsAoE { get; }

        public UnitAttackedEvent(int round, int attackerId, int defenderId,
            int damageDealt, FlankDirection flankDirection, bool isChargeAttack, bool isAoE)
            : base(round)
        {
            AttackerId = attackerId;
            DefenderId = defenderId;
            DamageDealt = damageDealt;
            FlankDirection = flankDirection;
            IsChargeAttack = isChargeAttack;
            IsAoE = isAoE;
        }
    }

    /// <summary>Emitted when a unit dies.</summary>
    public class UnitDiedEvent : BattleEvent
    {
        public int UnitId { get; }
        public int KillerId { get; }

        public UnitDiedEvent(int round, int unitId, int killerId) : base(round)
        {
            UnitId = unitId;
            KillerId = killerId;
        }
    }

    /// <summary>Emitted when the battle ends.</summary>
    public class BattleEndedEvent : BattleEvent
    {
        public BattleOutcome Outcome { get; }
        public int RoundsPlayed { get; }

        public BattleEndedEvent(int round, BattleOutcome outcome, int roundsPlayed) : base(round)
        {
            Outcome = outcome;
            RoundsPlayed = roundsPlayed;
        }
    }
}
