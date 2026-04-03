using System;
using System.Collections.Generic;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Army
{
    /// <summary>
    /// Serializable data representing a saved army. Stored as JSON.
    /// Includes unit composition, grid positions, and commander selection.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    [Serializable]
    public class SavedArmy
    {
        /// <summary>Unique ID for this army.</summary>
        public string Id;

        /// <summary>Player-chosen name (e.g., "Cavalry Rush", "Artillery Fort").</summary>
        public string Name;

        /// <summary>Whether this is a campaign or multiplayer army.</summary>
        public ArmyMode Mode;

        /// <summary>Total point cost of all units.</summary>
        public int TotalCost;

        /// <summary>The units in this army with their positions.</summary>
        public List<SavedUnitSlot> Units;

        /// <summary>Commander assigned to this army (empty string = none).</summary>
        public string CommanderId;

        /// <summary>When this army was last modified (UTC ticks).</summary>
        public long LastModifiedTicks;

        public SavedArmy()
        {
            Id = Guid.NewGuid().ToString("N");
            Units = new List<SavedUnitSlot>();
            CommanderId = "";
            LastModifiedTicks = DateTime.UtcNow.Ticks;
        }

        public SavedArmy(string name, ArmyMode mode) : this()
        {
            Name = name;
            Mode = mode;
        }

        /// <summary>
        /// Recalculates TotalCost from all unit slots, including officer assignment costs.
        /// GDD: Officer cost = Level 1 free, Level 2 = 1pt, Level 3 = 2pt, Level 4 = 3pt, Level 5 = 4pt.
        /// </summary>
        public void RecalculateCost(Dictionary<string, int> unitCosts,
            Dictionary<string, int> officerLevels = null)
        {
            TotalCost = 0;
            foreach (var slot in Units)
            {
                if (unitCosts.TryGetValue(slot.UnitTypeId, out int cost))
                    TotalCost += cost;

                // Add officer assignment cost based on level (GDD Section 2.9)
                if (!string.IsNullOrEmpty(slot.OfficerId) && officerLevels != null)
                {
                    if (officerLevels.TryGetValue(slot.OfficerId, out int level) && level > 1)
                        TotalCost += level - 1; // Level 2=1pt, Level 3=2pt, etc.
                }
            }
        }
    }

    /// <summary>
    /// A single unit slot in a saved army. References a unit type by string ID
    /// (matches UnitStatsSO asset name) and stores grid position.
    /// </summary>
    [Serializable]
    public class SavedUnitSlot
    {
        /// <summary>Unit type identifier (matches UnitStatsSO.unitName or asset name).</summary>
        public string UnitTypeId;

        /// <summary>Grid position (column, 1-10).</summary>
        public int X;

        /// <summary>Grid position (row, 1-10).</summary>
        public int Y;

        /// <summary>Officer ID assigned to this unit (empty = none).</summary>
        public string OfficerId;

        public SavedUnitSlot() { }

        public SavedUnitSlot(string unitTypeId, int x, int y)
        {
            UnitTypeId = unitTypeId;
            X = x;
            Y = y;
            OfficerId = "";
        }

        public GridCoord ToGridCoord() => new GridCoord(X, Y);
    }

    /// <summary>
    /// Whether an army is for campaign or multiplayer use.
    /// </summary>
    public enum ArmyMode
    {
        Campaign,
        Multiplayer
    }
}
