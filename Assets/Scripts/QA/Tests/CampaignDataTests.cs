using System;
using System.Collections.Generic;
using WarChess.Campaign;
using WarChess.Commanders;
using WarChess.Config;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.QA.Tests
{
    /// <summary>
    /// Data integrity tests for campaign battles: existence, placements,
    /// unit type validity, and unlock references.
    /// </summary>
    public static class CampaignDataTests
    {
        public static List<QATestResult> RunAll(GameConfigData config)
        {
            return new List<QATestResult>
            {
                TestAllBattlesExist(config),
                TestEnemyPlacements(config),
                TestUnitTypeIdsValid(config),
                TestUnlockRefsValid(config)
            };
        }

        /// <summary>
        /// All 30 campaign battles should exist with valid basic data.
        /// </summary>
        public static QATestResult TestAllBattlesExist(GameConfigData config)
        {
            const string name = "Campaign.AllBattlesExist";
            try
            {
                var battles = CampaignDatabase.AllBattles;
                if (battles.Count != 30)
                    return QATestResult.Fail(name,
                        $"Expected 30 battles, found {battles.Count}");

                for (int i = 0; i < battles.Count; i++)
                {
                    var b = battles[i];
                    if (b.BattleNumber != i + 1)
                        return QATestResult.Fail(name,
                            $"Battle at index {i} has number {b.BattleNumber}, expected {i + 1}");
                    if (string.IsNullOrEmpty(b.Name))
                        return QATestResult.Fail(name,
                            $"Battle {b.BattleNumber} has no name");
                    if (b.Act < 1 || b.Act > 3)
                        return QATestResult.Fail(name,
                            $"Battle {b.BattleNumber} has invalid act: {b.Act}");
                    if (b.PointBudget <= 0)
                        return QATestResult.Fail(name,
                            $"Battle {b.BattleNumber} has invalid budget: {b.PointBudget}");
                }

                return QATestResult.Pass(name, "All 30 battles exist with valid data");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Enemy placements must be within grid bounds, within deploy zone,
        /// and have no duplicate positions.
        /// </summary>
        public static QATestResult TestEnemyPlacements(GameConfigData config)
        {
            const string name = "Campaign.EnemyPlacements";
            try
            {
                foreach (var battle in CampaignDatabase.AllBattles)
                {
                    if (battle.EnemyArmy == null || battle.EnemyArmy.Count == 0)
                        continue; // Some battles may have placeholder data

                    var occupied = new HashSet<string>();
                    int deployMin = battle.EnemyDeployMinRow > 0 ? battle.EnemyDeployMinRow : 5;
                    int deployMax = battle.EnemyDeployMaxRow > 0 ? battle.EnemyDeployMaxRow : 10;

                    foreach (var placement in battle.EnemyArmy)
                    {
                        // Check grid bounds
                        if (placement.X < 1 || placement.X > config.GridWidth)
                            return QATestResult.Fail(name,
                                $"Battle {battle.BattleNumber}: unit at X={placement.X} outside grid (1-{config.GridWidth})");

                        if (placement.Y < 1 || placement.Y > config.GridHeight)
                            return QATestResult.Fail(name,
                                $"Battle {battle.BattleNumber}: unit at Y={placement.Y} outside grid (1-{config.GridHeight})");

                        // Check deploy zone
                        if (placement.Y < deployMin || placement.Y > deployMax)
                            return QATestResult.Fail(name,
                                $"Battle {battle.BattleNumber}: unit at ({placement.X},{placement.Y}) outside deploy zone ({deployMin}-{deployMax})");

                        // Check duplicates
                        string key = $"{placement.X},{placement.Y}";
                        if (!occupied.Add(key))
                            return QATestResult.Fail(name,
                                $"Battle {battle.BattleNumber}: duplicate placement at ({placement.X},{placement.Y})");
                    }
                }

                return QATestResult.Pass(name, "All enemy placements valid");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// All unit type IDs in campaign battles must be valid (createable by UnitFactory).
        /// </summary>
        public static QATestResult TestUnitTypeIdsValid(GameConfigData config)
        {
            const string name = "Campaign.UnitTypeIdsValid";
            try
            {
                UnitFactory.ResetIds();
                var validTypes = new HashSet<string>(BalanceTester.AllUnitTypes);

                foreach (var battle in CampaignDatabase.AllBattles)
                {
                    if (battle.EnemyArmy == null) continue;

                    foreach (var placement in battle.EnemyArmy)
                    {
                        if (!validTypes.Contains(placement.UnitTypeId))
                            return QATestResult.Fail(name,
                                $"Battle {battle.BattleNumber}: unknown unit type '{placement.UnitTypeId}'");

                        // Also verify UnitFactory can create it
                        var unit = UnitFactory.CreateByTypeName(
                            placement.UnitTypeId, Owner.Enemy, new GridCoord(1, 1));
                        if (unit == null)
                            return QATestResult.Fail(name,
                                $"Battle {battle.BattleNumber}: UnitFactory returned null for '{placement.UnitTypeId}'");
                    }
                }

                return QATestResult.Pass(name, "All unit type IDs valid and createable");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Unlock references (units and commanders) must be valid IDs.
        /// </summary>
        public static QATestResult TestUnlockRefsValid(GameConfigData config)
        {
            const string name = "Campaign.UnlockRefsValid";
            try
            {
                var validUnits = new HashSet<string>(BalanceTester.AllUnitTypes);
                var validCommanders = new HashSet<string>();
                foreach (CommanderId cmd in Enum.GetValues(typeof(CommanderId)))
                {
                    if (cmd != CommanderId.None)
                        validCommanders.Add(cmd.ToString());
                }

                foreach (var battle in CampaignDatabase.AllBattles)
                {
                    if (battle.UnlocksUnitTypes != null)
                    {
                        foreach (var unitId in battle.UnlocksUnitTypes)
                        {
                            if (!validUnits.Contains(unitId))
                                return QATestResult.Fail(name,
                                    $"Battle {battle.BattleNumber}: unlocks unknown unit type '{unitId}'");
                        }
                    }

                    if (!string.IsNullOrEmpty(battle.UnlocksCommander))
                    {
                        if (!validCommanders.Contains(battle.UnlocksCommander))
                            return QATestResult.Fail(name,
                                $"Battle {battle.BattleNumber}: unlocks unknown commander '{battle.UnlocksCommander}'");
                    }
                }

                return QATestResult.Pass(name, "All unlock references are valid");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }
    }
}
