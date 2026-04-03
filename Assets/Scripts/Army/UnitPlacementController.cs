using System;
using System.Collections.Generic;
using UnityEngine;
using WarChess.Config;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Army
{
    /// <summary>
    /// Handles unit placement during the deployment phase. Validates placement
    /// against deployment zones and budget. Manages drag-and-drop from a unit
    /// palette onto the grid.
    /// </summary>
    public class UnitPlacementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridView _gridView;
        [SerializeField] private GameConfigSO _gameConfig;

        [Header("Budget")]
        [SerializeField] private int _pointBudget = 25;

        private GridMap _gridMap;
        private GameConfigData _configData;
        private List<PlacedUnit> _placedUnits = new List<PlacedUnit>();
        private UnitStatsSO _selectedUnitType;
        private int _pointsSpent;

        // Drag state
        private GameObject _dragPreview;
        private bool _isDragging;

        /// <summary>Fired when a unit is successfully placed.</summary>
        public event Action<UnitStatsSO, GridCoord> OnUnitPlaced;

        /// <summary>Fired when a unit is removed from the grid.</summary>
        public event Action<GridCoord> OnUnitRemoved;

        /// <summary>Fired when points spent changes.</summary>
        public event Action<int, int> OnBudgetChanged;

        /// <summary>Current points remaining.</summary>
        public int PointsRemaining => _pointBudget - _pointsSpent;

        /// <summary>All currently placed units.</summary>
        public IReadOnlyList<PlacedUnit> PlacedUnits => _placedUnits;

        /// <summary>
        /// Initializes the placement controller with a grid.
        /// </summary>
        public void Initialize(GridMap gridMap)
        {
            _gridMap = gridMap;
            _configData = _gameConfig.ToData();
            _placedUnits.Clear();
            _pointsSpent = 0;

            _gridView.OnTileClicked += HandleTileClicked;
        }

        /// <summary>
        /// Sets the unit type to place on next click/tap.
        /// </summary>
        public void SelectUnitType(UnitStatsSO unitType)
        {
            _selectedUnitType = unitType;
        }

        /// <summary>
        /// Clears the selected unit type (deselect).
        /// </summary>
        public void DeselectUnit()
        {
            _selectedUnitType = null;
        }

        /// <summary>
        /// Attempts to place the selected unit at the given coordinate.
        /// Returns true if placement succeeded.
        /// </summary>
        public bool TryPlaceUnit(UnitStatsSO unitType, GridCoord coord)
        {
            if (unitType == null) return false;

            // Validate placement
            if (!ValidatePlacement(unitType, coord, out string reason))
            {
                Debug.Log($"Cannot place {unitType.unitName} at {coord}: {reason}");
                return false;
            }

            _placedUnits.Add(new PlacedUnit(unitType, coord));
            _pointsSpent += unitType.cost;

            OnUnitPlaced?.Invoke(unitType, coord);
            OnBudgetChanged?.Invoke(_pointsSpent, _pointBudget);
            return true;
        }

        /// <summary>
        /// Removes a placed unit at the given coordinate.
        /// </summary>
        public bool TryRemoveUnit(GridCoord coord)
        {
            for (int i = _placedUnits.Count - 1; i >= 0; i--)
            {
                if (_placedUnits[i].Position == coord)
                {
                    _pointsSpent -= _placedUnits[i].UnitStats.cost;
                    _placedUnits.RemoveAt(i);

                    OnUnitRemoved?.Invoke(coord);
                    OnBudgetChanged?.Invoke(_pointsSpent, _pointBudget);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Validates whether a unit can be placed at the given coordinate.
        /// </summary>
        public bool ValidatePlacement(UnitStatsSO unitType, GridCoord coord, out string reason)
        {
            reason = null;

            if (!_gridMap.IsValidCoord(coord))
            {
                reason = "Invalid coordinate";
                return false;
            }

            if (!_gridMap.IsInDeploymentZone(coord, Owner.Player, _configData))
            {
                reason = $"Must place in rows {_configData.PlayerDeployMinRow}-{_configData.PlayerDeployMaxRow}";
                return false;
            }

            // Check if tile is already occupied by a placed unit
            foreach (var placed in _placedUnits)
            {
                if (placed.Position == coord)
                {
                    reason = "Tile already occupied";
                    return false;
                }
            }

            if (_pointsSpent + unitType.cost > _pointBudget)
            {
                reason = $"Not enough points ({unitType.cost} needed, {PointsRemaining} remaining)";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Clears all placed units and resets budget.
        /// </summary>
        public void ClearAll()
        {
            _placedUnits.Clear();
            _pointsSpent = 0;
            OnBudgetChanged?.Invoke(0, _pointBudget);
        }

        /// <summary>
        /// Sets the point budget for this deployment.
        /// </summary>
        public void SetBudget(int budget)
        {
            _pointBudget = budget;
            OnBudgetChanged?.Invoke(_pointsSpent, _pointBudget);
        }

        private void HandleTileClicked(GridCoord coord)
        {
            if (_selectedUnitType != null)
            {
                TryPlaceUnit(_selectedUnitType, coord);
            }
            else
            {
                // If no unit selected, clicking an occupied tile removes it
                TryRemoveUnit(coord);
            }
        }

        private void OnDestroy()
        {
            if (_gridView != null)
                _gridView.OnTileClicked -= HandleTileClicked;
        }
    }

    /// <summary>
    /// Data record for a unit placed during deployment.
    /// </summary>
    public class PlacedUnit
    {
        public UnitStatsSO UnitStats { get; }
        public GridCoord Position { get; }

        public PlacedUnit(UnitStatsSO unitStats, GridCoord position)
        {
            UnitStats = unitStats;
            Position = position;
        }
    }
}
