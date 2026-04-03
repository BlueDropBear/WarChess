using System;
using System.Collections.Generic;

namespace WarChess.Officers
{
    /// <summary>
    /// Runtime state for a player-owned officer instance. Tracks level,
    /// experience (battles participated), and which unit it's assigned to.
    /// Pure C# — serializable for save/load.
    /// </summary>
    [Serializable]
    public class OfficerInstance
    {
        public OfficerId OfficerId;
        public int Level;
        public int BattlesParticipated;

        /// <summary>
        /// Returns the army budget cost to assign this officer.
        /// Level 1 = 0, Level 2 = 1, Level 3 = 2, Level 4 = 3, Level 5 = 4.
        /// </summary>
        public int AssignmentCost => Level <= 1 ? 0 : Level - 1;

        public OfficerInstance(OfficerId id)
        {
            OfficerId = id;
            Level = 1;
            BattlesParticipated = 0;
        }

        /// <summary>
        /// Records a battle participation. Levels up if threshold is reached.
        /// GDD: L1→L2: 5 battles, L2→L3: 15 total, L3→L4: 30 total, L4→L5: 50 total.
        /// </summary>
        public bool RecordBattle()
        {
            BattlesParticipated++;
            int newLevel = CalculateLevel();
            if (newLevel > Level)
            {
                Level = newLevel;
                return true; // Leveled up
            }
            return false;
        }

        private int CalculateLevel()
        {
            if (BattlesParticipated >= 50) return 5;
            if (BattlesParticipated >= 30) return 4;
            if (BattlesParticipated >= 15) return 3;
            if (BattlesParticipated >= 5) return 2;
            return 1;
        }
    }

    /// <summary>
    /// Manages the player's officer collection. Handles purchasing, leveling,
    /// and assignment validation. Pure C# — persistence via SaveManager.
    /// </summary>
    public class OfficerManager
    {
        private readonly List<OfficerInstance> _ownedOfficers;

        public IReadOnlyList<OfficerInstance> OwnedOfficers => _ownedOfficers;

        public OfficerManager()
        {
            _ownedOfficers = new List<OfficerInstance>();
        }

        public void LoadOfficers(List<OfficerInstance> officers)
        {
            _ownedOfficers.Clear();
            if (officers != null) _ownedOfficers.AddRange(officers);
        }

        /// <summary>
        /// Adds a new officer to the player's collection.
        /// </summary>
        public OfficerInstance AddOfficer(OfficerId id)
        {
            var officer = new OfficerInstance(id);
            _ownedOfficers.Add(officer);
            return officer;
        }

        /// <summary>
        /// Returns the officer at the given index, or null.
        /// </summary>
        public OfficerInstance GetOfficer(int index)
        {
            if (index < 0 || index >= _ownedOfficers.Count) return null;
            return _ownedOfficers[index];
        }

        /// <summary>
        /// Returns all officers of a given type.
        /// </summary>
        public List<OfficerInstance> GetOfficersOfType(OfficerId id)
        {
            var result = new List<OfficerInstance>();
            foreach (var o in _ownedOfficers)
                if (o.OfficerId == id) result.Add(o);
            return result;
        }

        /// <summary>
        /// Calculates the total budget cost of all assigned officers in an army.
        /// </summary>
        public int CalculateOfficerCost(List<OfficerInstance> assignedOfficers)
        {
            int total = 0;
            foreach (var o in assignedOfficers)
                total += o.AssignmentCost;
            return total;
        }

        /// <summary>
        /// Records a battle for all officers that participated.
        /// Returns list of officers that leveled up.
        /// </summary>
        public List<OfficerInstance> RecordBattleForOfficers(List<OfficerInstance> participants)
        {
            var leveledUp = new List<OfficerInstance>();
            foreach (var officer in participants)
            {
                if (officer.RecordBattle())
                    leveledUp.Add(officer);
            }
            return leveledUp;
        }
    }
}
