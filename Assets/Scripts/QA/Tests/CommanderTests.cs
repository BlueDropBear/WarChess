using System;
using System.Collections.Generic;
using WarChess.Commanders;
using WarChess.Config;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.QA.Tests
{
    /// <summary>
    /// Correctness tests for commander ability triggers, buff application,
    /// expiration, and once-per-battle activation.
    /// </summary>
    public static class CommanderTests
    {
        public static List<QATestResult> RunAll(GameConfigData config)
        {
            return new List<QATestResult>
            {
                TestManualTriggerTiming(config),
                TestAutoTriggerConditions(config),
                TestBuffExpiration(config),
                TestAbilityFiresOnce(config)
            };
        }

        /// <summary>
        /// Manual commanders should fire on the specified activation round.
        /// </summary>
        public static QATestResult TestManualTriggerTiming(GameConfigData config)
        {
            const string name = "Commander.ManualTriggerTiming";
            try
            {
                UnitFactory.ResetIds();
                int activationRound = 3;

                var playerUnits = new List<UnitInstance>
                {
                    UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 2))
                };
                var enemyUnits = new List<UnitInstance>
                {
                    UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 9))
                };

                var system = new CommanderSystem(
                    CommanderId.Wellington, activationRound,
                    CommanderId.None, 1);

                // Rounds before activation: no events
                for (int r = 1; r < activationRound; r++)
                {
                    var events = system.ProcessRound(r, playerUnits, enemyUnits, 1, 1);
                    if (events.Count > 0)
                        return QATestResult.Fail(name,
                            $"Wellington fired on round {r}, expected round {activationRound}");
                }

                // Activation round: should fire
                var activateEvents = system.ProcessRound(activationRound, playerUnits, enemyUnits, 1, 1);
                if (activateEvents.Count == 0)
                    return QATestResult.Fail(name,
                        $"Wellington did not fire on activation round {activationRound}");
                if (activateEvents[0].Commander != CommanderId.Wellington)
                    return QATestResult.Fail(name,
                        $"Wrong commander fired: {activateEvents[0].Commander}");

                return QATestResult.Pass(name,
                    $"Wellington fired exactly on round {activationRound}");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Automatic commanders: Kutuzov fires round 8, Blucher round 1,
        /// Moore at 50% casualties.
        /// </summary>
        public static QATestResult TestAutoTriggerConditions(GameConfigData config)
        {
            const string name = "Commander.AutoTriggerConditions";
            try
            {
                // Kutuzov: fires on round 8
                {
                    UnitFactory.ResetIds();
                    var units = new List<UnitInstance>
                    {
                        UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 2))
                    };
                    var enemies = new List<UnitInstance>
                    {
                        UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 9))
                    };

                    var system = new CommanderSystem(CommanderId.Kutuzov, 1, CommanderId.None, 1);

                    bool firedBefore8 = false;
                    for (int r = 1; r < 8; r++)
                    {
                        if (system.ProcessRound(r, units, enemies, 1, 1).Count > 0)
                            firedBefore8 = true;
                    }
                    if (firedBefore8)
                        return QATestResult.Fail(name, "Kutuzov fired before round 8");

                    var events8 = system.ProcessRound(8, units, enemies, 1, 1);
                    if (events8.Count == 0)
                        return QATestResult.Fail(name, "Kutuzov did not fire on round 8");
                }

                // Blucher: fires on round 1
                {
                    UnitFactory.ResetIds();
                    var units = new List<UnitInstance>
                    {
                        UnitFactory.CreateCavalry(Owner.Player, new GridCoord(5, 2))
                    };
                    var enemies = new List<UnitInstance>
                    {
                        UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 9))
                    };

                    var system = new CommanderSystem(CommanderId.Blucher, 1, CommanderId.None, 1);
                    var events1 = system.ProcessRound(1, units, enemies, 1, 1);
                    if (events1.Count == 0)
                        return QATestResult.Fail(name, "Blucher did not fire on round 1");
                }

                // Moore: fires when 50% units lost
                {
                    UnitFactory.ResetIds();
                    var units = new List<UnitInstance>
                    {
                        UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(3, 2)),
                        UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 2))
                    };
                    var enemies = new List<UnitInstance>
                    {
                        UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 9))
                    };

                    var system = new CommanderSystem(CommanderId.Moore, 1, CommanderId.None, 1);

                    // Both alive (2/2): should NOT fire
                    var events = system.ProcessRound(1, units, enemies, 2, 1);
                    if (events.Count > 0)
                        return QATestResult.Fail(name, "Moore fired with all units alive");

                    // Kill one (1/2 = 50%): should fire
                    units[0].TakeDamage(9999);
                    var events2 = system.ProcessRound(2, units, enemies, 2, 1);
                    if (events2.Count == 0)
                        return QATestResult.Fail(name, "Moore did not fire at 50% casualties");
                }

                return QATestResult.Pass(name, "All auto-trigger conditions verified");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Buffs with duration should expire after the specified rounds.
        /// </summary>
        public static QATestResult TestBuffExpiration(GameConfigData config)
        {
            const string name = "Commander.BuffExpiration";
            try
            {
                UnitFactory.ResetIds();
                var units = new List<UnitInstance>
                {
                    UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(5, 2))
                };
                var enemies = new List<UnitInstance>
                {
                    UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 9))
                };

                // Wellington: +30% DEF for 2 rounds, activate on round 1
                var system = new CommanderSystem(CommanderId.Wellington, 1, CommanderId.None, 1);

                // Round 1: activate
                system.ProcessRound(1, units, enemies, 1, 1);
                int defR1 = system.GetDefMultiplier(units[0].Id);
                if (defR1 != 130)
                    return QATestResult.Fail(name,
                        $"Expected DEF mult 130 on round 1, got {defR1}");

                // Round 2: still active
                system.ProcessRound(2, units, enemies, 1, 1);
                int defR2 = system.GetDefMultiplier(units[0].Id);
                if (defR2 != 130)
                    return QATestResult.Fail(name,
                        $"Expected DEF mult 130 on round 2, got {defR2}");

                // Round 3: buff should expire (duration=2, activated round 1, expires at round 1+2=3)
                system.ProcessRound(3, units, enemies, 1, 1);
                int defR3 = system.GetDefMultiplier(units[0].Id);
                if (defR3 != 100)
                    return QATestResult.Fail(name,
                        $"Expected DEF mult 100 after expiry on round 3, got {defR3}");

                return QATestResult.Pass(name, "Wellington buff expired correctly after 2 rounds");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// Commander abilities must only fire once per battle.
        /// </summary>
        public static QATestResult TestAbilityFiresOnce(GameConfigData config)
        {
            const string name = "Commander.FiresOnce";
            try
            {
                UnitFactory.ResetIds();
                var units = new List<UnitInstance>
                {
                    UnitFactory.CreateCavalry(Owner.Player, new GridCoord(5, 2))
                };
                var enemies = new List<UnitInstance>
                {
                    UnitFactory.CreateLineInfantry(Owner.Enemy, new GridCoord(5, 9))
                };

                // Blucher fires round 1 — should not fire again
                var system = new CommanderSystem(CommanderId.Blucher, 1, CommanderId.None, 1);

                var events1 = system.ProcessRound(1, units, enemies, 1, 1);
                if (events1.Count == 0)
                    return QATestResult.Fail(name, "Blucher did not fire on round 1");

                // Subsequent rounds should not fire
                for (int r = 2; r <= 5; r++)
                {
                    var events = system.ProcessRound(r, units, enemies, 1, 1);
                    foreach (var ev in events)
                    {
                        if (ev.Commander == CommanderId.Blucher)
                            return QATestResult.Fail(name,
                                $"Blucher fired again on round {r} — should only fire once");
                    }
                }

                return QATestResult.Pass(name, "Blucher correctly fired only once");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }
    }
}
