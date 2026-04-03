using System.Collections.Generic;

namespace WarChess.Battle
{
    /// <summary>
    /// Possible battle outcomes.
    /// </summary>
    public enum BattleOutcome
    {
        PlayerWin,
        EnemyWin,
        Draw
    }

    /// <summary>
    /// Summary of a completed battle. Includes outcome, stats, and the full
    /// event log for replay. Pure C# — no Unity dependencies.
    /// </summary>
    public class BattleResult
    {
        /// <summary>Who won the battle.</summary>
        public BattleOutcome Outcome { get; }

        /// <summary>Total rounds played before the battle ended.</summary>
        public int RoundsPlayed { get; }

        /// <summary>Number of player units still alive at battle end.</summary>
        public int PlayerUnitsRemaining { get; }

        /// <summary>Number of enemy units still alive at battle end.</summary>
        public int EnemyUnitsRemaining { get; }

        /// <summary>Total HP remaining across all living player units.</summary>
        public int PlayerHpRemaining { get; }

        /// <summary>Total HP remaining across all living enemy units.</summary>
        public int EnemyHpRemaining { get; }

        /// <summary>Complete ordered list of all events for replay.</summary>
        public IReadOnlyList<BattleEvent> Events { get; }

        public BattleResult(
            BattleOutcome outcome,
            int roundsPlayed,
            int playerUnitsRemaining,
            int enemyUnitsRemaining,
            int playerHpRemaining,
            int enemyHpRemaining,
            IReadOnlyList<BattleEvent> events)
        {
            Outcome = outcome;
            RoundsPlayed = roundsPlayed;
            PlayerUnitsRemaining = playerUnitsRemaining;
            EnemyUnitsRemaining = enemyUnitsRemaining;
            PlayerHpRemaining = playerHpRemaining;
            EnemyHpRemaining = enemyHpRemaining;
            Events = events;
        }
    }
}
