using System;
using System.Collections.Generic;
using System.Linq;

namespace WarChess.Army
{
    /// <summary>
    /// Manages the player's collection of saved armies. Handles CRUD operations
    /// and validation. Pure C# — persistence handled by SaveManager.
    /// </summary>
    public class ArmyManager
    {
        private readonly List<SavedArmy> _armies;
        private readonly Dictionary<string, int> _unitCosts;

        /// <summary>All saved armies.</summary>
        public IReadOnlyList<SavedArmy> Armies => _armies;

        public ArmyManager(Dictionary<string, int> unitCosts)
        {
            _armies = new List<SavedArmy>();
            _unitCosts = unitCosts;
        }

        /// <summary>
        /// Loads armies from saved data (called by SaveManager on startup).
        /// </summary>
        public void LoadArmies(List<SavedArmy> armies)
        {
            _armies.Clear();
            if (armies != null)
                _armies.AddRange(armies);
        }

        /// <summary>
        /// Creates a new empty army with the given name and mode.
        /// </summary>
        public SavedArmy CreateArmy(string name, ArmyMode mode)
        {
            var army = new SavedArmy(name, mode);
            _armies.Add(army);
            return army;
        }

        /// <summary>
        /// Duplicates an existing army with a new name.
        /// </summary>
        public SavedArmy DuplicateArmy(string armyId, string newName)
        {
            var source = GetArmy(armyId);
            if (source == null) return null;

            var copy = new SavedArmy(newName, source.Mode);
            copy.CommanderId = source.CommanderId;
            foreach (var slot in source.Units)
            {
                copy.Units.Add(new SavedUnitSlot(slot.UnitTypeId, slot.X, slot.Y)
                {
                    OfficerId = slot.OfficerId
                });
            }
            copy.RecalculateCost(_unitCosts);
            _armies.Add(copy);
            return copy;
        }

        /// <summary>
        /// Deletes an army by ID.
        /// </summary>
        public bool DeleteArmy(string armyId)
        {
            return _armies.RemoveAll(a => a.Id == armyId) > 0;
        }

        /// <summary>
        /// Gets an army by ID.
        /// </summary>
        public SavedArmy GetArmy(string armyId)
        {
            for (int i = 0; i < _armies.Count; i++)
            {
                if (_armies[i].Id == armyId)
                    return _armies[i];
            }
            return null;
        }

        /// <summary>
        /// Returns all armies for the given mode.
        /// </summary>
        public List<SavedArmy> GetArmiesByMode(ArmyMode mode)
        {
            var result = new List<SavedArmy>();
            foreach (var army in _armies)
            {
                if (army.Mode == mode)
                    result.Add(army);
            }
            return result;
        }

        /// <summary>
        /// Validates an army against a point budget and available units.
        /// Returns null if valid, or an error message if invalid.
        /// </summary>
        public string ValidateArmy(SavedArmy army, int budget, HashSet<string> availableUnits)
        {
            if (army.Units.Count == 0)
                return "Army has no units";

            army.RecalculateCost(_unitCosts);

            if (army.TotalCost > budget)
                return $"Army costs {army.TotalCost} points but budget is {budget}";

            // Check for duplicate positions
            var positions = new HashSet<string>();
            foreach (var slot in army.Units)
            {
                string key = $"{slot.X},{slot.Y}";
                if (!positions.Add(key))
                    return $"Two units on same tile ({slot.X},{slot.Y})";
            }

            // Check unit availability
            if (availableUnits != null)
            {
                foreach (var slot in army.Units)
                {
                    if (!availableUnits.Contains(slot.UnitTypeId))
                        return $"Unit type '{slot.UnitTypeId}' is not available";
                }
            }

            return null;
        }
    }
}
