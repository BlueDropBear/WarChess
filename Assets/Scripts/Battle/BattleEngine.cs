using System;
using System.Collections.Generic;
using WarChess.Battle.Targeting;
using WarChess.Config;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle
{
    /// <summary>
    /// Core auto-battle engine. Runs the full battle loop: initiative, movement,
    /// combat, cleanup, win check. Pure C# — no Unity dependencies.
    /// Deterministic: same inputs + same seed = same outputs.
    /// </summary>
    public class BattleEngine
    {
        private readonly GridMap _grid;
        private readonly GameConfigData _config;
        private readonly BattleRng _rng;
        private readonly List<UnitInstance> _playerUnits;
        private readonly List<UnitInstance> _enemyUnits;
        private readonly List<BattleEvent> _events;
        private readonly Dictionary<int, ITargetingStrategy> _targetingStrategies;

        private int _currentRound;
        private bool _battleEnded;
        private BattleOutcome _outcome;

        /// <summary>All events emitted during the battle so far.</summary>
        public IReadOnlyList<BattleEvent> Events => _events;

        /// <summary>Current round number (1-based).</summary>
        public int CurrentRound => _currentRound;

        /// <summary>Whether the battle has ended.</summary>
        public bool IsBattleOver => _battleEnded;

        /// <summary>
        /// Creates a new battle engine. Units must already be placed on the grid.
        /// </summary>
        public BattleEngine(
            GridMap grid,
            List<UnitInstance> playerUnits,
            List<UnitInstance> enemyUnits,
            GameConfigData config,
            int seed)
        {
            _grid = grid;
            _config = config;
            _rng = new BattleRng(seed);
            _playerUnits = new List<UnitInstance>(playerUnits);
            _enemyUnits = new List<UnitInstance>(enemyUnits);
            _events = new List<BattleEvent>();
            _currentRound = 0;
            _battleEnded = false;

            // Create targeting strategies for each unit
            _targetingStrategies = new Dictionary<int, ITargetingStrategy>();
            foreach (var unit in _playerUnits)
                _targetingStrategies[unit.Id] = TargetingFactory.Create(unit.TargetingPriority, _rng);
            foreach (var unit in _enemyUnits)
                _targetingStrategies[unit.Id] = TargetingFactory.Create(unit.TargetingPriority, _rng);
        }

        /// <summary>
        /// Runs the full battle to completion. Returns the final result.
        /// </summary>
        public BattleResult RunFullBattle()
        {
            while (!_battleEnded)
            {
                RunRound();
            }

            return BuildResult();
        }

        /// <summary>
        /// Runs a single round. Returns false if the battle ended this round.
        /// </summary>
        public bool RunRound()
        {
            if (_battleEnded) return false;

            _currentRound++;
            _events.Add(new RoundStartedEvent(_currentRound));

            // Reset per-round state for all living units
            ResetRoundState();

            // Get initiative order (all living units sorted by SPD desc, tie-break by Id desc)
            var initiativeOrder = GetInitiativeOrder();

            // Movement phase
            RunMovementPhase(initiativeOrder);

            // Combat phase
            RunCombatPhase(initiativeOrder);

            // Cleanup dead units
            CleanupDeadUnits();

            // Check win conditions
            CheckWinConditions();

            return !_battleEnded;
        }

        private void ResetRoundState()
        {
            foreach (var unit in _playerUnits)
                if (unit.IsAlive) unit.ResetRoundState();
            foreach (var unit in _enemyUnits)
                if (unit.IsAlive) unit.ResetRoundState();
        }

        private List<UnitInstance> GetInitiativeOrder()
        {
            var all = new List<UnitInstance>();
            foreach (var u in _playerUnits) if (u.IsAlive) all.Add(u);
            foreach (var u in _enemyUnits) if (u.IsAlive) all.Add(u);

            // Sort by SPD descending, then by seeded random for tie-break (GDD Section 2.4)
            // Assign random tiebreakers before sorting to keep sort deterministic
            var tieBreakers = new Dictionary<int, int>();
            foreach (var u in all)
                tieBreakers[u.Id] = _rng.Next();
            all.Sort((a, b) =>
            {
                int cmp = b.Spd.CompareTo(a.Spd);
                if (cmp != 0) return cmp;
                return tieBreakers[b.Id].CompareTo(tieBreakers[a.Id]);
            });

            return all;
        }

        private void RunMovementPhase(List<UnitInstance> initiativeOrder)
        {
            foreach (var unit in initiativeOrder)
            {
                if (!unit.IsAlive) continue;

                var enemies = GetEnemies(unit);
                if (enemies.Count == 0) continue;

                var strategy = _targetingStrategies[unit.Id];
                var target = strategy.SelectTarget(unit, enemies, _grid);
                if (target == null) continue;

                var from = unit.Position;
                var to = MovementResolver.ResolveMovement(unit, target, _grid);

                if (to != from)
                {
                    int tilesMoved = MovementResolver.GetTilesMoved(from, to);
                    _grid.MoveUnit(from, to);
                    unit.TilesMovedThisRound = tilesMoved;
                    unit.HasMovedThisRound = true;

                    _events.Add(new UnitMovedEvent(_currentRound, unit.Id, from, to, tilesMoved));
                }
            }
        }

        private void RunCombatPhase(List<UnitInstance> initiativeOrder)
        {
            foreach (var unit in initiativeOrder)
            {
                if (!unit.IsAlive) continue;

                var enemies = GetEnemies(unit);
                if (enemies.Count == 0) continue;

                var strategy = _targetingStrategies[unit.Id];
                var target = strategy.SelectTarget(unit, enemies, _grid);
                if (target == null || !target.IsAlive) continue;

                // Check if target is in range
                int dist = unit.Position.ManhattanDistance(target.Position);
                if (dist > unit.Rng) continue;

                // Determine flank direction
                var flankDir = FlankingCalculator.GetFlankDirection(
                    unit.Position, target.Position, target.Facing);

                // Check for charge (moved 3+ tiles this round, has Charge ability)
                bool isCharge = unit.TilesMovedThisRound >= _config.ChargeMinTilesMoved
                    && (unit.Ability == AbilityType.Charge || unit.Ability == AbilityType.ArmoredCharge)
                    && !unit.HasChargedThisBattle;

                // Calculate damage
                int damage = DamageCalculator.Calculate(
                    unit, target, flankDir,
                    isCharge, _config.ChargeMultiplier, _config.MinimumDamage);

                // Apply damage
                target.TakeDamage(damage);
                unit.HasAttackedThisRound = true;

                if (isCharge)
                    unit.HasChargedThisBattle = true;

                _events.Add(new UnitAttackedEvent(
                    _currentRound, unit.Id, target.Id,
                    damage, flankDir, isCharge, false));

                if (!target.IsAlive)
                {
                    _events.Add(new UnitDiedEvent(_currentRound, target.Id, unit.Id));
                }

                // Handle Artillery Bombardment AoE
                if (unit.Ability == AbilityType.Bombardment)
                {
                    ApplyBombardmentAoE(unit, target.Position, damage);
                }

                // Handle Dragoon dismount
                if (unit.Ability == AbilityType.Dismount && !unit.IsDismounted && unit.Rng == 1)
                {
                    unit.ApplyDismount(_config.DismountMov, _config.DismountDefBonus, _config.DismountAtkBonus);
                }
            }
        }

        private void ApplyBombardmentAoE(UnitInstance attacker, GridCoord targetPos, int primaryDamage)
        {
            int splashDamage = DamageCalculator.CalculateSplashDamage(primaryDamage, _config.BombardmentSplashPercentage);
            var adjacent = _grid.GetAdjacentCoords(targetPos);

            foreach (var coord in adjacent)
            {
                var splashTarget = _grid.GetUnitAt(coord);
                if (splashTarget != null && splashTarget.IsAlive && splashTarget.Id != attacker.Id
                    && splashTarget.Owner != attacker.Owner)
                {
                    splashTarget.TakeDamage(splashDamage);

                    _events.Add(new UnitAttackedEvent(
                        _currentRound, attacker.Id, splashTarget.Id,
                        splashDamage, FlankDirection.Front, false, true));

                    if (!splashTarget.IsAlive)
                    {
                        _events.Add(new UnitDiedEvent(_currentRound, splashTarget.Id, attacker.Id));
                    }
                }
            }
        }

        private void CleanupDeadUnits()
        {
            RemoveDeadFromGrid(_playerUnits);
            RemoveDeadFromGrid(_enemyUnits);
        }

        private void RemoveDeadFromGrid(List<UnitInstance> units)
        {
            foreach (var unit in units)
            {
                if (!unit.IsAlive)
                {
                    _grid.RemoveUnit(unit.Position);
                }
            }
        }

        private void CheckWinConditions()
        {
            int playerAlive = CountAlive(_playerUnits);
            int enemyAlive = CountAlive(_enemyUnits);

            if (playerAlive == 0 && enemyAlive == 0)
            {
                EndBattle(BattleOutcome.Draw);
            }
            else if (enemyAlive == 0)
            {
                EndBattle(BattleOutcome.PlayerWin);
            }
            else if (playerAlive == 0)
            {
                EndBattle(BattleOutcome.EnemyWin);
            }
            else if (_currentRound >= _config.MaxRounds)
            {
                // Time limit — compare total remaining HP
                int playerHp = TotalHp(_playerUnits);
                int enemyHp = TotalHp(_enemyUnits);

                if (playerHp > enemyHp)
                    EndBattle(BattleOutcome.PlayerWin);
                else if (enemyHp > playerHp)
                    EndBattle(BattleOutcome.EnemyWin);
                else
                    EndBattle(BattleOutcome.Draw);
            }
        }

        private void EndBattle(BattleOutcome outcome)
        {
            _battleEnded = true;
            _outcome = outcome;
            _events.Add(new BattleEndedEvent(_currentRound, outcome, _currentRound));
        }

        private IReadOnlyList<UnitInstance> GetEnemies(UnitInstance unit)
        {
            return unit.Owner == Owner.Player ? _enemyUnits : _playerUnits;
        }

        private int CountAlive(List<UnitInstance> units)
        {
            int count = 0;
            foreach (var u in units) if (u.IsAlive) count++;
            return count;
        }

        private int TotalHp(List<UnitInstance> units)
        {
            int total = 0;
            foreach (var u in units) if (u.IsAlive) total += u.CurrentHp;
            return total;
        }

        private BattleResult BuildResult()
        {
            return new BattleResult(
                _outcome,
                _currentRound,
                CountAlive(_playerUnits),
                CountAlive(_enemyUnits),
                TotalHp(_playerUnits),
                TotalHp(_enemyUnits),
                _events);
        }
    }
}
