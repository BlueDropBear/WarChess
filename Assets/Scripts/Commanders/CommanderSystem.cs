using System.Collections.Generic;
using WarChess.Units;

namespace WarChess.Commanders
{
    /// <summary>
    /// Manages commander ability activation during battle. Tracks whether the
    /// ability has been used and applies effects to units.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class CommanderSystem
    {
        private readonly CommanderAbilityData _playerCommander;
        private readonly CommanderAbilityData _enemyCommander;

        private bool _playerAbilityUsed;
        private bool _enemyAbilityUsed;
        private int _playerActivationRound; // For manual triggers
        private int _enemyActivationRound;

        // Track active buffs: (expiry round, or -1 for permanent)
        private readonly List<ActiveBuff> _activeBuffs = new List<ActiveBuff>();

        public CommanderSystem(
            CommanderId playerId, int playerActivationRound,
            CommanderId enemyId, int enemyActivationRound)
        {
            _playerCommander = CommanderDatabase.Get(playerId);
            _enemyCommander = CommanderDatabase.Get(enemyId);
            _playerActivationRound = playerActivationRound;
            _enemyActivationRound = enemyActivationRound;
        }

        /// <summary>
        /// Called at the start of each round. Checks triggers and applies effects.
        /// Returns a list of CommanderEvents for visualization.
        /// </summary>
        public List<CommanderEvent> ProcessRound(
            int round,
            List<UnitInstance> playerUnits,
            List<UnitInstance> enemyUnits,
            int initialPlayerCount,
            int initialEnemyCount)
        {
            var events = new List<CommanderEvent>();

            // Check player commander
            if (!_playerAbilityUsed && _playerCommander != null)
            {
                if (ShouldActivate(_playerCommander, round, _playerActivationRound,
                    playerUnits, initialPlayerCount))
                {
                    ApplyAbility(_playerCommander, playerUnits, round, Owner.Player);
                    _playerAbilityUsed = true;
                    events.Add(new CommanderEvent(round, _playerCommander.Id, Owner.Player));
                }
            }

            // Check enemy commander
            if (!_enemyAbilityUsed && _enemyCommander != null)
            {
                if (ShouldActivate(_enemyCommander, round, _enemyActivationRound,
                    enemyUnits, initialEnemyCount))
                {
                    ApplyAbility(_enemyCommander, enemyUnits, round, Owner.Enemy);
                    _enemyAbilityUsed = true;
                    events.Add(new CommanderEvent(round, _enemyCommander.Id, Owner.Enemy));
                }
            }

            // Expire old buffs
            ExpireBuffs(round, playerUnits, enemyUnits);

            return events;
        }

        private bool ShouldActivate(CommanderAbilityData cmd, int round, int activationRound,
            List<UnitInstance> units, int initialCount)
        {
            if (cmd.TriggerType == CommanderTriggerType.Manual)
            {
                return round == activationRound;
            }

            // Automatic triggers
            switch (cmd.Id)
            {
                case CommanderId.Kutuzov:
                    return round == cmd.TriggerParam;

                case CommanderId.Blucher:
                    return round == cmd.TriggerParam;

                case CommanderId.Moore:
                    // Triggers when 50% or more units have been lost
                    int alive = CountAlive(units);
                    return alive * 2 <= initialCount;

                default:
                    return false;
            }
        }

        private void ApplyAbility(CommanderAbilityData cmd, List<UnitInstance> units, int round, Owner owner)
        {
            int expiryRound = cmd.Duration > 0 ? round + cmd.Duration : -1; // -1 = permanent

            switch (cmd.Id)
            {
                case CommanderId.Wellington:
                    // All infantry +30% DEF for 2 rounds
                    foreach (var unit in units)
                    {
                        if (!unit.IsAlive) continue;
                        if (unit.CountsAsType == UnitType.LineInfantry)
                        {
                            _activeBuffs.Add(new ActiveBuff(unit.Id, BuffType.DefMultiplier, 130, expiryRound));
                        }
                    }
                    break;

                case CommanderId.Napoleon:
                    // All units +20% ATK, +1 MOV for 2 rounds
                    foreach (var unit in units)
                    {
                        if (!unit.IsAlive) continue;
                        _activeBuffs.Add(new ActiveBuff(unit.Id, BuffType.AtkMultiplier, 120, expiryRound));
                        _activeBuffs.Add(new ActiveBuff(unit.Id, BuffType.MovBonus, 1, expiryRound));
                    }
                    break;

                case CommanderId.Kutuzov:
                    // All units heal 25% of max HP (instant, no buff tracking needed)
                    foreach (var unit in units)
                    {
                        if (!unit.IsAlive) continue;
                        unit.Heal(unit.MaxHp / 4);
                    }
                    break;

                case CommanderId.Blucher:
                    // All cavalry +2 MOV and guaranteed charge
                    foreach (var unit in units)
                    {
                        if (!unit.IsAlive) continue;
                        if (unit.CountsAsType == UnitType.Cavalry)
                        {
                            _activeBuffs.Add(new ActiveBuff(unit.Id, BuffType.MovBonus, 2, -1));
                            // Guaranteed charge is handled by resetting HasChargedThisBattle
                            unit.HasChargedThisBattle = false;
                        }
                    }
                    break;

                case CommanderId.Moore:
                    // All remaining +40% ATK, +20% DEF for rest of battle
                    foreach (var unit in units)
                    {
                        if (!unit.IsAlive) continue;
                        _activeBuffs.Add(new ActiveBuff(unit.Id, BuffType.AtkMultiplier, 140, -1));
                        _activeBuffs.Add(new ActiveBuff(unit.Id, BuffType.DefMultiplier, 120, -1));
                    }
                    break;

                case CommanderId.Ney:
                    // One unit gets double action — mark the strongest alive unit
                    UnitInstance best = null;
                    foreach (var unit in units)
                    {
                        if (!unit.IsAlive) continue;
                        if (best == null || unit.Atk > best.Atk)
                            best = unit;
                    }
                    if (best != null)
                    {
                        // Expires next round so it lasts through this round's combat phase
                        _activeBuffs.Add(new ActiveBuff(best.Id, BuffType.DoubleAction, 1, round + 1));
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets the total ATK multiplier (base 100) from active commander buffs on a unit.
        /// </summary>
        public int GetAtkMultiplier(int unitId)
        {
            int total = 100;
            foreach (var buff in _activeBuffs)
            {
                if (buff.UnitId == unitId && buff.Type == BuffType.AtkMultiplier)
                    total = (total * buff.Value) / 100;
            }
            return total;
        }

        /// <summary>
        /// Gets the total DEF multiplier (base 100) from active commander buffs.
        /// </summary>
        public int GetDefMultiplier(int unitId)
        {
            int total = 100;
            foreach (var buff in _activeBuffs)
            {
                if (buff.UnitId == unitId && buff.Type == BuffType.DefMultiplier)
                    total = (total * buff.Value) / 100;
            }
            return total;
        }

        /// <summary>
        /// Gets bonus MOV from active commander buffs.
        /// </summary>
        public int GetMovBonus(int unitId)
        {
            int total = 0;
            foreach (var buff in _activeBuffs)
            {
                if (buff.UnitId == unitId && buff.Type == BuffType.MovBonus)
                    total += buff.Value;
            }
            return total;
        }

        /// <summary>
        /// Returns true if this unit has a double action buff this round.
        /// </summary>
        public bool HasDoubleAction(int unitId)
        {
            foreach (var buff in _activeBuffs)
            {
                if (buff.UnitId == unitId && buff.Type == BuffType.DoubleAction)
                    return true;
            }
            return false;
        }

        private void ExpireBuffs(int round, List<UnitInstance> playerUnits, List<UnitInstance> enemyUnits)
        {
            _activeBuffs.RemoveAll(b => b.ExpiryRound >= 0 && round >= b.ExpiryRound);
        }

        private int CountAlive(List<UnitInstance> units)
        {
            int count = 0;
            foreach (var u in units) if (u.IsAlive) count++;
            return count;
        }
    }

    public enum BuffType
    {
        AtkMultiplier,
        DefMultiplier,
        MovBonus,
        DoubleAction
    }

    public class ActiveBuff
    {
        public int UnitId;
        public BuffType Type;
        public int Value;
        public int ExpiryRound; // -1 = permanent

        public ActiveBuff(int unitId, BuffType type, int value, int expiryRound)
        {
            UnitId = unitId;
            Type = type;
            Value = value;
            ExpiryRound = expiryRound;
        }
    }

    /// <summary>
    /// Event emitted when a commander ability activates.
    /// </summary>
    public class CommanderEvent
    {
        public int Round;
        public CommanderId Commander;
        public Owner Owner;

        public CommanderEvent(int round, CommanderId commander, Owner owner)
        {
            Round = round;
            Commander = commander;
            Owner = owner;
        }
    }
}
