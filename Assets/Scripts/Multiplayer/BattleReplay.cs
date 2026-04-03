using System;
using System.Collections.Generic;
using WarChess.Battle;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Stores all data needed to replay a multiplayer battle.
    /// Since the battle engine is deterministic, a replay only needs:
    /// the two army submissions + seed. But we also store events for
    /// instant playback without re-simulation.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    [Serializable]
    public class BattleReplay
    {
        /// <summary>Match ID this replay belongs to.</summary>
        public string MatchId;

        /// <summary>Tier the match was played at.</summary>
        public int Tier;

        /// <summary>RNG seed for deterministic replay.</summary>
        public int Seed;

        /// <summary>Battle outcome.</summary>
        public BattleOutcome Outcome;

        /// <summary>Total rounds played.</summary>
        public int RoundsPlayed;

        /// <summary>Player A's army submission.</summary>
        public ArmySubmission PlayerAArmy;

        /// <summary>Player B's army submission.</summary>
        public ArmySubmission PlayerBArmy;

        /// <summary>Player A info.</summary>
        public ReplayPlayerInfo PlayerA;

        /// <summary>Player B info.</summary>
        public ReplayPlayerInfo PlayerB;

        /// <summary>
        /// Pre-computed event log for instant playback.
        /// If null, re-simulate from submissions + seed.
        /// </summary>
        public List<SerializedBattleEvent> EventLog;

        /// <summary>When the battle was resolved (UTC ticks).</summary>
        public long ResolvedAtTicks;
    }

    /// <summary>
    /// Player info displayed in replay viewer.
    /// </summary>
    [Serializable]
    public class ReplayPlayerInfo
    {
        public string PlayerId;
        public string DisplayName;
        public int EloBeforeMatch;
        public int EloAfterMatch;
        public string Rank;
    }

    /// <summary>
    /// Serializable version of a BattleEvent for storage/transmission.
    /// Full BattleEvent objects contain references that don't serialize;
    /// this flattened version stores only primitive data.
    /// </summary>
    [Serializable]
    public class SerializedBattleEvent
    {
        public int Round;
        public string EventType; // "Move", "Attack", "Death", "RoundStart", "BattleEnd"

        // UnitMoved
        public int UnitId;
        public int FromX, FromY;
        public int ToX, ToY;
        public int TilesMoved;

        // UnitAttacked
        public int AttackerId;
        public int DefenderId;
        public int DamageDealt;
        public string FlankDirection;
        public bool IsCharge;
        public bool IsAoE;

        // UnitDied
        public int KillerId;

        // BattleEnded
        public string Outcome;
        public int RoundsPlayed;
    }

    /// <summary>
    /// Converts between BattleEvent objects and serializable format.
    /// </summary>
    public static class BattleEventSerializer
    {
        public static List<SerializedBattleEvent> Serialize(IReadOnlyList<BattleEvent> events)
        {
            var result = new List<SerializedBattleEvent>(events.Count);

            foreach (var evt in events)
            {
                var se = new SerializedBattleEvent { Round = evt.Round };

                switch (evt)
                {
                    case RoundStartedEvent rse:
                        se.EventType = "RoundStart";
                        break;

                    case UnitMovedEvent ume:
                        se.EventType = "Move";
                        se.UnitId = ume.UnitId;
                        se.FromX = ume.From.X; se.FromY = ume.From.Y;
                        se.ToX = ume.To.X; se.ToY = ume.To.Y;
                        se.TilesMoved = ume.TilesMoved;
                        break;

                    case UnitAttackedEvent uae:
                        se.EventType = "Attack";
                        se.AttackerId = uae.AttackerId;
                        se.DefenderId = uae.DefenderId;
                        se.DamageDealt = uae.DamageDealt;
                        se.FlankDirection = uae.FlankDirection.ToString();
                        se.IsCharge = uae.IsChargeAttack;
                        se.IsAoE = uae.IsAoE;
                        break;

                    case UnitDiedEvent ude:
                        se.EventType = "Death";
                        se.UnitId = ude.UnitId;
                        se.KillerId = ude.KillerId;
                        break;

                    case BattleEndedEvent bee:
                        se.EventType = "BattleEnd";
                        se.Outcome = bee.Outcome.ToString();
                        se.RoundsPlayed = bee.RoundsPlayed;
                        break;
                }

                result.Add(se);
            }

            return result;
        }
    }

    /// <summary>
    /// Creates BattleReplay objects from match results.
    /// </summary>
    public static class ReplayFactory
    {
        public static BattleReplay CreateFromMatch(
            MatchResolveResult result,
            ArmySubmission armyA, ArmySubmission armyB,
            string playerAName, string playerBName)
        {
            return new BattleReplay
            {
                MatchId = result.MatchId,
                Tier = result.Tier,
                Seed = result.Seed,
                Outcome = result.Outcome,
                RoundsPlayed = result.RoundsPlayed,
                PlayerAArmy = armyA,
                PlayerBArmy = armyB,
                PlayerA = new ReplayPlayerInfo
                {
                    PlayerId = result.PlayerAId,
                    DisplayName = playerAName,
                    EloBeforeMatch = result.PlayerAOldElo,
                    EloAfterMatch = result.PlayerANewElo,
                    Rank = EloSystem.GetRankName(result.PlayerAOldElo)
                },
                PlayerB = new ReplayPlayerInfo
                {
                    PlayerId = result.PlayerBId,
                    DisplayName = playerBName,
                    EloBeforeMatch = result.PlayerBOldElo,
                    EloAfterMatch = result.PlayerBNewElo,
                    Rank = EloSystem.GetRankName(result.PlayerBOldElo)
                },
                EventLog = BattleEventSerializer.Serialize(result.Events),
                ResolvedAtTicks = result.ResolvedAtTicks
            };
        }
    }
}
