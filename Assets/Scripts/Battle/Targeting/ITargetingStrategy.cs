using System.Collections.Generic;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle.Targeting
{
    /// <summary>
    /// Strategy interface for unit target selection.
    /// Implementations define different AI behaviors (nearest, weakest, etc.).
    /// </summary>
    public interface ITargetingStrategy
    {
        /// <summary>
        /// Selects the best target from the list of living enemies.
        /// Returns null if no valid target exists.
        /// </summary>
        UnitInstance SelectTarget(UnitInstance attacker, IReadOnlyList<UnitInstance> enemies, GridMap grid);
    }
}
