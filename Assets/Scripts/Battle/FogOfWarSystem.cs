using System.Collections.Generic;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle
{
    /// <summary>
    /// Manages fog of war for campaign battles. Hidden enemy units are not visible
    /// to the player until revealed by proximity or Scout Master officers.
    /// GDD Section 6.2: fog of war battles hide all enemy info regardless of difficulty.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class FogOfWarSystem
    {
        private readonly HashSet<int> _revealedUnitIds;
        private readonly int _defaultRevealRange;
        private bool _isEnabled;

        /// <summary>Whether fog of war is active for this battle.</summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>Set of unit IDs that have been revealed.</summary>
        public IReadOnlyCollection<int> RevealedUnits => _revealedUnitIds;

        /// <summary>
        /// Creates a new fog of war system.
        /// </summary>
        /// <param name="enabled">Whether fog is active for this battle.</param>
        /// <param name="defaultRevealRange">Default range at which units are revealed (tiles).</param>
        public FogOfWarSystem(bool enabled, int defaultRevealRange = 3)
        {
            _isEnabled = enabled;
            _defaultRevealRange = defaultRevealRange;
            _revealedUnitIds = new HashSet<int>();
        }

        /// <summary>
        /// Updates visibility based on player unit positions. Call at the start
        /// of each round and after movement. Enemy units within reveal range
        /// of any player unit become permanently revealed.
        /// </summary>
        public List<int> UpdateVisibility(
            List<UnitInstance> playerUnits,
            List<UnitInstance> enemyUnits,
            Dictionary<int, int> scoutRevealRanges)
        {
            if (!_isEnabled) return new List<int>();

            var newlyRevealed = new List<int>();

            foreach (var enemy in enemyUnits)
            {
                if (!enemy.IsAlive) continue;
                if (_revealedUnitIds.Contains(enemy.Id)) continue;

                foreach (var player in playerUnits)
                {
                    if (!player.IsAlive) continue;

                    // Check reveal range (default or scout bonus)
                    int revealRange = _defaultRevealRange;
                    if (scoutRevealRanges != null &&
                        scoutRevealRanges.TryGetValue(player.Id, out int scoutRange))
                    {
                        revealRange = System.Math.Max(revealRange, scoutRange);
                    }

                    int dist = player.Position.ManhattanDistance(enemy.Position);
                    if (dist <= revealRange)
                    {
                        _revealedUnitIds.Add(enemy.Id);
                        newlyRevealed.Add(enemy.Id);
                        break; // No need to check other player units
                    }
                }
            }

            return newlyRevealed;
        }

        /// <summary>
        /// Returns true if the given enemy unit is visible to the player.
        /// Always true if fog of war is disabled.
        /// </summary>
        public bool IsUnitVisible(int unitId)
        {
            if (!_isEnabled) return true;
            return _revealedUnitIds.Contains(unitId);
        }

        /// <summary>
        /// Reveals all enemy units. Called when battle starts (units reveal
        /// as they enter combat range) or when fog is lifted.
        /// </summary>
        public void RevealAll(List<UnitInstance> enemyUnits)
        {
            foreach (var enemy in enemyUnits)
                _revealedUnitIds.Add(enemy.Id);
        }

        /// <summary>
        /// Force-reveals a specific unit (e.g., when it attacks or is attacked).
        /// </summary>
        public void RevealUnit(int unitId)
        {
            _revealedUnitIds.Add(unitId);
        }
    }
}
