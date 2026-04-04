using System;
using System.Collections.Generic;
using WarChess.Battle;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Where an opponent came from in a deployment round battle.
    /// </summary>
    public enum OpponentSource
    {
        /// <summary>A real player's army from the live pool.</summary>
        LivePlayer,

        /// <summary>A ghost army (snapshot of a previous player army).</summary>
        GhostArmy,

        /// <summary>A developer-crafted house army.</summary>
        HouseArmy
    }

    /// <summary>
    /// A single battle within a deployment round.
    /// </summary>
    [Serializable]
    public class RoundBattle
    {
        /// <summary>Unique match ID for this battle.</summary>
        public string MatchId;

        /// <summary>Where the opponent came from.</summary>
        public OpponentSource Source;

        /// <summary>The opponent's army submission.</summary>
        public ArmySubmission Opponent;

        /// <summary>Stars earned in this battle (0-3).</summary>
        public int Stars;

        /// <summary>Full battle result for replay.</summary>
        public BattleResult Result;

        /// <summary>RNG seed used for deterministic resolution.</summary>
        public int Seed;

        /// <summary>Whether the opponent was a champion ghost.</summary>
        public bool IsChampionChallenge;

        /// <summary>Player Elo before this battle.</summary>
        public int PlayerEloBefore;

        /// <summary>Player Elo after this battle.</summary>
        public int PlayerEloAfter;
    }

    /// <summary>
    /// A deployment round: 3 battles (+ optional bonus) from a single ammunition spend.
    /// This is the core multiplayer session unit.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    [Serializable]
    public class DeploymentRound
    {
        /// <summary>Unique round ID.</summary>
        public string RoundId;

        /// <summary>Player who deployed.</summary>
        public string PlayerId;

        /// <summary>Tier this round was played at.</summary>
        public int Tier;

        /// <summary>The player's submitted army.</summary>
        public ArmySubmission PlayerArmy;

        /// <summary>The 3 standard battles in this round.</summary>
        public List<RoundBattle> Battles;

        /// <summary>Bonus battle (null if not earned).</summary>
        public RoundBattle BonusBattle;

        /// <summary>Total stars earned across all battles (0-10).</summary>
        public int TotalStars;

        /// <summary>Whether this was a perfect 10-star run.</summary>
        public bool IsPerfectRun;

        /// <summary>When the round started (UTC ticks).</summary>
        public long StartedAtTicks;

        /// <summary>When the round completed (UTC ticks).</summary>
        public long CompletedAtTicks;

        public DeploymentRound()
        {
            Battles = new List<RoundBattle>();
        }

        /// <summary>
        /// Calculates total stars from all battles including bonus.
        /// </summary>
        public void CalculateTotals()
        {
            TotalStars = 0;
            foreach (var b in Battles)
                TotalStars += b.Stars;
            if (BonusBattle != null)
                TotalStars += BonusBattle.Stars;
            IsPerfectRun = TotalStars >= 10;
        }
    }
}
