using System;
using System.Collections.Generic;
using WarChess.Config;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.QA.Tests
{
    /// <summary>
    /// Correctness tests for grid coordinate validation and unit placement.
    /// </summary>
    public static class GridTests
    {
        public static List<QATestResult> RunAll(GameConfigData config)
        {
            return new List<QATestResult>
            {
                TestGridBoundsEnforced(config),
                TestPlacementThrowsOnOccupied(config)
            };
        }

        /// <summary>
        /// Invalid coordinates must be rejected by GridMap.IsValidCoord.
        /// </summary>
        public static QATestResult TestGridBoundsEnforced(GameConfigData config)
        {
            const string name = "Grid.BoundsEnforced";
            try
            {
                var grid = new GridMap(config.GridWidth, config.GridHeight);

                // Valid coordinates
                var validCoords = new[]
                {
                    new GridCoord(1, 1),
                    new GridCoord(config.GridWidth, config.GridHeight),
                    new GridCoord(5, 5)
                };

                foreach (var c in validCoords)
                {
                    if (!grid.IsValidCoord(c))
                        return QATestResult.Fail(name, $"Valid coord {c} rejected");
                }

                // Invalid coordinates
                var invalidCoords = new[]
                {
                    new GridCoord(0, 1),
                    new GridCoord(1, 0),
                    new GridCoord(config.GridWidth + 1, 1),
                    new GridCoord(1, config.GridHeight + 1),
                    new GridCoord(-1, -1),
                    new GridCoord(0, 0)
                };

                foreach (var c in invalidCoords)
                {
                    if (grid.IsValidCoord(c))
                        return QATestResult.Fail(name, $"Invalid coord {c} accepted");
                }

                // GetUnitAt on invalid coord should return null, not throw
                var unit = grid.GetUnitAt(new GridCoord(0, 0));
                if (unit != null)
                    return QATestResult.Fail(name, "GetUnitAt(0,0) returned non-null");

                // IsTileEmpty on invalid coord should return false
                if (grid.IsTileEmpty(new GridCoord(0, 0)))
                    return QATestResult.Fail(name, "IsTileEmpty(0,0) returned true");

                return QATestResult.Pass(name,
                    $"Bounds enforced: {validCoords.Length} valid, {invalidCoords.Length} invalid verified");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Placing a unit on an occupied tile must throw InvalidOperationException.
        /// </summary>
        public static QATestResult TestPlacementThrowsOnOccupied(GameConfigData config)
        {
            const string name = "Grid.PlacementThrowsOnOccupied";
            try
            {
                UnitFactory.ResetIds();
                var grid = new GridMap(config.GridWidth, config.GridHeight);

                var unit1 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 5));
                grid.PlaceUnit(unit1, unit1.Position);

                var unit2 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 5));

                try
                {
                    grid.PlaceUnit(unit2, unit2.Position);
                    return QATestResult.Fail(name,
                        "Expected InvalidOperationException when placing on occupied tile");
                }
                catch (InvalidOperationException)
                {
                    // Expected
                }

                // Verify PlaceUnit on invalid coord throws ArgumentException
                UnitFactory.ResetIds();
                var unit3 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(0, 0));
                try
                {
                    grid.PlaceUnit(unit3, new GridCoord(0, 0));
                    return QATestResult.Fail(name,
                        "Expected ArgumentException when placing at invalid coord");
                }
                catch (ArgumentException)
                {
                    // Expected
                }

                return QATestResult.Pass(name,
                    "Placement correctly throws on occupied tile and invalid coord");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }
    }
}
