using System;
using System.Collections.Generic;
using WarChess.Config;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.QA.Tests
{
    /// <summary>
    /// Correctness tests for unit stat invariants: HP clamping,
    /// stat minimums after mods, and dismount-once behavior.
    /// </summary>
    public static class UnitStatTests
    {
        public static List<QATestResult> RunAll(GameConfigData config)
        {
            return new List<QATestResult>
            {
                TestHpClamping(config),
                TestStatMinimums(config),
                TestDismountOnce(config)
            };
        }

        /// <summary>
        /// HP must stay in [0, MaxHp] after TakeDamage and Heal.
        /// </summary>
        public static QATestResult TestHpClamping(GameConfigData config)
        {
            const string name = "UnitStats.HpClamping";
            try
            {
                UnitFactory.ResetIds();
                var unit = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(1, 1));

                // Damage more than current HP
                unit.TakeDamage(9999);
                if (unit.CurrentHp < 0)
                    return QATestResult.Fail(name, $"HP went below 0: {unit.CurrentHp}");
                if (unit.CurrentHp != 0)
                    return QATestResult.Fail(name, $"Expected HP=0, got {unit.CurrentHp}");

                // Create a fresh unit and test healing
                UnitFactory.ResetIds();
                var unit2 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(1, 1));
                unit2.TakeDamage(10);
                int hpAfterDamage = unit2.CurrentHp;
                unit2.Heal(9999);
                if (unit2.CurrentHp > unit2.MaxHp)
                    return QATestResult.Fail(name,
                        $"HP exceeded MaxHp: {unit2.CurrentHp} > {unit2.MaxHp}");
                if (unit2.CurrentHp != unit2.MaxHp)
                    return QATestResult.Fail(name,
                        $"Expected HP={unit2.MaxHp} after over-heal, got {unit2.CurrentHp}");

                // Test zero damage
                UnitFactory.ResetIds();
                var unit3 = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(1, 1));
                int hpBefore = unit3.CurrentHp;
                unit3.TakeDamage(0);
                if (unit3.CurrentHp != hpBefore)
                    return QATestResult.Fail(name,
                        $"Zero damage changed HP: {hpBefore} -> {unit3.CurrentHp}");

                return QATestResult.Pass(name, "HP clamping verified: floor=0, ceiling=MaxHp");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// After ApplyFlatMods, MOV>=1, SPD>=1, RNG>=0.
        /// </summary>
        public static QATestResult TestStatMinimums(GameConfigData config)
        {
            const string name = "UnitStats.StatMinimums";
            try
            {
                UnitFactory.ResetIds();
                var unit = UnitFactory.CreateLineInfantry(Owner.Player, new GridCoord(1, 1));

                // Apply extreme negative mods
                unit.ApplyFlatMods(-999, -999, -999);

                if (unit.Mov < 1)
                    return QATestResult.Fail(name, $"MOV went below 1: {unit.Mov}");
                if (unit.Spd < 1)
                    return QATestResult.Fail(name, $"SPD went below 1: {unit.Spd}");
                if (unit.Rng < 0)
                    return QATestResult.Fail(name, $"RNG went below 0: {unit.Rng}");

                return QATestResult.Pass(name,
                    $"Stats clamped correctly: MOV={unit.Mov}, SPD={unit.Spd}, RNG={unit.Rng}");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }

        /// <summary>
        /// ApplyDismount should only work once — second call is a no-op.
        /// </summary>
        public static QATestResult TestDismountOnce(GameConfigData config)
        {
            const string name = "UnitStats.DismountOnce";
            try
            {
                UnitFactory.ResetIds();
                var dragoon = UnitFactory.CreateDragoon(Owner.Player, new GridCoord(1, 1));

                int origMov = dragoon.Mov;
                int origDef = dragoon.Def;
                int origAtk = dragoon.Atk;

                // First dismount should apply
                dragoon.ApplyDismount(config.DismountMov, config.DismountDefBonus, config.DismountAtkBonus);
                if (!dragoon.IsDismounted)
                    return QATestResult.Fail(name, "IsDismounted not set after first dismount");

                int afterMov = dragoon.Mov;
                int afterDef = dragoon.Def;
                int afterAtk = dragoon.Atk;

                if (afterMov != config.DismountMov)
                    return QATestResult.Fail(name,
                        $"MOV after dismount: expected {config.DismountMov}, got {afterMov}");
                if (afterDef != origDef + config.DismountDefBonus)
                    return QATestResult.Fail(name,
                        $"DEF after dismount: expected {origDef + config.DismountDefBonus}, got {afterDef}");

                // Second dismount should be no-op
                dragoon.ApplyDismount(1, 999, 999);
                if (dragoon.Mov != afterMov || dragoon.Def != afterDef || dragoon.Atk != afterAtk)
                    return QATestResult.Fail(name,
                        "Second dismount changed stats — should be no-op");

                return QATestResult.Pass(name,
                    $"Dismount applied once: MOV {origMov}->{afterMov}, DEF {origDef}->{afterDef}");
            }
            catch (Exception ex)
            {
                return QATestResult.Fail(name, ex.Message);
            }
        }
    }
}
