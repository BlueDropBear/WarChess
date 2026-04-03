using System;
using System.Collections.Generic;
using WarChess.Army;
using WarChess.Commanders;
using WarChess.Core;
using WarChess.Officers;
using WarChess.Units;

namespace WarChess.Multiplayer
{
    /// <summary>
    /// Serializes and deserializes army compositions for multiplayer submission.
    /// Armies are encoded as JSON-compatible plain C# objects for server transmission.
    /// GDD Section 4.3: army submissions are locked after deployment — no edits.
    /// </summary>
    public static class ArmySerializer
    {
        /// <summary>
        /// Converts a SavedArmy into a multiplayer submission payload.
        /// Includes all data needed for server-side validation and battle resolution.
        /// </summary>
        public static ArmySubmission ToSubmission(
            SavedArmy army, int tier, string playerId)
        {
            var submission = new ArmySubmission
            {
                SubmissionId = Guid.NewGuid().ToString("N"),
                PlayerId = playerId,
                ArmyName = army.Name,
                Tier = tier,
                TotalCost = army.TotalCost,
                CommanderId = army.CommanderId,
                CommanderActivationRound = 1,
                SubmittedAtTicks = DateTime.UtcNow.Ticks,
                Units = new List<SubmittedUnit>()
            };

            foreach (var slot in army.Units)
            {
                submission.Units.Add(new SubmittedUnit
                {
                    UnitTypeId = slot.UnitTypeId,
                    X = slot.X,
                    Y = slot.Y,
                    OfficerId = slot.OfficerId
                });
            }

            return submission;
        }

        /// <summary>
        /// Converts a submission back into UnitInstances for battle resolution.
        /// </summary>
        /// <summary>
        /// Converts a submission back into UnitInstances for battle resolution.
        /// Caller is responsible for calling UnitFactory.ResetIds() before the first call.
        /// Do NOT reset IDs between multiple armies in the same battle.
        /// </summary>
        public static List<UnitInstance> ToUnitInstances(
            ArmySubmission submission, Owner owner)
        {
            var units = new List<UnitInstance>();

            foreach (var su in submission.Units)
            {
                var coord = new GridCoord(su.X, su.Y);
                var unit = UnitFactory.CreateByTypeName(su.UnitTypeId, owner, coord);
                if (unit != null)
                    units.Add(unit);
            }

            return units;
        }
    }

    /// <summary>
    /// A serializable army submission for the multiplayer pool.
    /// This is what gets sent to and stored on the server.
    /// </summary>
    [Serializable]
    public class ArmySubmission
    {
        public string SubmissionId;
        public string PlayerId;
        public string ArmyName;
        public int Tier;
        public int TotalCost;
        public string CommanderId;
        public int CommanderActivationRound;
        public long SubmittedAtTicks;
        public List<SubmittedUnit> Units;

        /// <summary>Status in the army pool.</summary>
        public SubmissionStatus Status;
    }

    [Serializable]
    public class SubmittedUnit
    {
        public string UnitTypeId;
        public int X;
        public int Y;
        public string OfficerId;
    }

    public enum SubmissionStatus
    {
        InPool,      // Waiting for a match
        Matched,     // Paired with an opponent, battle pending
        Resolved,    // Battle completed
        Withdrawn    // Player withdrew before matching
    }
}
