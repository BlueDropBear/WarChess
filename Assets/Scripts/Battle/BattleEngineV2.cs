using System.Collections.Generic;
using WarChess.Battle.Targeting;
using WarChess.Commanders;
using WarChess.Config;
using WarChess.Core;
using WarChess.Formations;
using WarChess.Terrain;
using WarChess.Units;

namespace WarChess.Battle
{
    /// <summary>
    /// Full-featured battle engine with terrain, formations, commanders, and
    /// line-of-sight. Extends the core loop with Phase 3 systems.
    /// Pure C# — no Unity dependencies. Deterministic with seeded RNG.
    /// </summary>
    public class BattleEngineV2
    {
        private readonly GridMap _grid;
        private readonly TerrainMap _terrainMap;
        private readonly GameConfigData _config;
        private readonly BattleRng _rng;
        private readonly List<UnitInstance> _playerUnits;
        private readonly List<UnitInstance> _enemyUnits;
        private readonly List<BattleEvent> _events;
        private readonly Dictionary<int, ITargetingStrategy> _targetingStrategies;
        private readonly CommanderSystem _commanders;
        private readonly int _initialPlayerCount;
        private readonly int _initialEnemyCount;

        // Per-round formation bonuses (recalculated each round)
        private readonly Dictionary<int, FormationBonus> _formationBonuses;

        private int _currentRound;
        private bool _battleEnded;
        private BattleOutcome _outcome;

        public IReadOnlyList<BattleEvent> Events => _events;
        public int CurrentRound => _currentRound;
        public bool IsBattleOver => _battleEnded;

        public BattleEngineV2(
            GridMap grid,
            TerrainMap terrainMap,
            List<UnitInstance> playerUnits,
            List<UnitInstance> enemyUnits,
            GameConfigData config,
            int seed,
            CommanderId playerCommander = CommanderId.None,
            int playerCommanderRound = 1,
            CommanderId enemyCommander = CommanderId.None,
            int enemyCommanderRound = 1)
        {
            _grid = grid;
            _terrainMap = terrainMap ?? new TerrainMap();
            _config = config;
            _rng = new BattleRng(seed);
            _playerUnits = new List<UnitInstance>(playerUnits);
            _enemyUnits = new List<UnitInstance>(enemyUnits);
            _events = new List<BattleEvent>();
            _formationBonuses = new Dictionary<int, FormationBonus>();
            _currentRound = 0;
            _battleEnded = false;
            _initialPlayerCount = playerUnits.Count;
            _initialEnemyCount = enemyUnits.Count;

            _targetingStrategies = new Dictionary<int, ITargetingStrategy>();
            foreach (var unit in _playerUnits)
                _targetingStrategies[unit.Id] = TargetingFactory.Create(unit.TargetingPriority, _rng);
            foreach (var unit in _enemyUnits)
                _targetingStrategies[unit.Id] = TargetingFactory.Create(unit.TargetingPriority, _rng);

            _commanders = new CommanderSystem(
                playerCommander, playerCommanderRound,
                enemyCommander, enemyCommanderRound);
        }

        public BattleResult RunFullBattle()
        {
            while (!_battleEnded) RunRound();
            return BuildResult();
        }

        public bool RunRound()
        {
            if (_battleEnded) return false;
            _currentRound++;
            _events.Add(new RoundStartedEvent(_currentRound));

            // Reset round state
            foreach (var u in _playerUnits) if (u.IsAlive) u.ResetRoundState();
            foreach (var u in _enemyUnits) if (u.IsAlive) u.ResetRoundState();

            // Commander abilities
            _commanders.ProcessRound(_currentRound, _playerUnits, _enemyUnits,
                _initialPlayerCount, _initialEnemyCount);

            // Recalculate formations
            RecalculateFormations();

            // Initiative order
            var order = GetInitiativeOrder();

            // Movement
            RunMovement(order);

            // Combat
            RunCombat(order);

            // Cleanup
            CleanupDead();
            CheckWinConditions();

            return !_battleEnded;
        }

        private void RecalculateFormations()
        {
            _formationBonuses.Clear();
            var allUnits = _grid.GetAllLivingUnits();

            foreach (var unit in allUnits)
            {
                var bonus = FormationDetector.DetectFormation(unit, _grid,
                    _config.BattleLineDefBonus, _config.BatteryAtkBonus,
                    _config.CavalryWedgeChargeBonus, _config.SquareDefVsCavalryBonus,
                    _config.SkirmishAtkBonus, _config.SkirmishRangeBonus,
                    _config.BattleLineMinUnits, _config.SquareMinUnits,
                    _config.CavalryWedgeMinUnits, _config.CavalryWedgeMaxStep);

                _formationBonuses[unit.Id] = bonus;
            }
        }

        private List<UnitInstance> GetInitiativeOrder()
        {
            var all = new List<UnitInstance>();
            foreach (var u in _playerUnits) if (u.IsAlive) all.Add(u);
            foreach (var u in _enemyUnits) if (u.IsAlive) all.Add(u);

            all.Sort((a, b) =>
            {
                int cmp = b.Spd.CompareTo(a.Spd);
                return cmp != 0 ? cmp : b.Id.CompareTo(a.Id);
            });

            return all;
        }

        private void RunMovement(List<UnitInstance> order)
        {
            foreach (var unit in order)
            {
                if (!unit.IsAlive) continue;

                var enemies = GetEnemies(unit);
                if (CountAlive(enemies) == 0) continue;

                var strategy = _targetingStrategies[unit.Id];
                var target = strategy.SelectTarget(unit, enemies, _grid);
                if (target == null) continue;

                // Calculate effective MOV with commander bonus
                int effectiveMov = unit.Mov + _commanders.GetMovBonus(unit.Id);

                var from = unit.Position;
                var to = MovementResolverV2.ResolveMovement(unit, target, _grid, _terrainMap, effectiveMov);

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

        private void RunCombat(List<UnitInstance> order)
        {
            foreach (var unit in order)
            {
                if (!unit.IsAlive) continue;
                ProcessUnitCombat(unit);

                // Double action from Ney
                if (_commanders.HasDoubleAction(unit.Id) && unit.IsAlive)
                {
                    ProcessUnitCombat(unit);
                }
            }
        }

        private void ProcessUnitCombat(UnitInstance unit)
        {
            var enemies = GetEnemies(unit);
            if (CountAlive(enemies) == 0) return;

            var strategy = _targetingStrategies[unit.Id];
            var target = strategy.SelectTarget(unit, enemies, _grid);
            if (target == null || !target.IsAlive) return;

            // Check if target is in range (with formation range bonus)
            int rangeBonus = GetFormationBonus(unit).RangeBonus
                           + TerrainData.GetRangeBonus(_terrainMap.GetTerrain(unit.Position));
            int effectiveRange = unit.Rng + rangeBonus;
            int dist = unit.Position.ManhattanDistance(target.Position);
            if (dist > effectiveRange) return;

            // Line of sight check for ranged units
            if (unit.Rng > 1)
            {
                bool onHill = _terrainMap.GetTerrain(unit.Position) == TerrainType.Hill;
                bool ignoresLoS = unit.Ability == AbilityType.CongreveBarrage;
                if (!LineOfSight.HasLineOfSight(
                    unit.Position, target.Position, _terrainMap, _grid, onHill, ignoresLoS))
                    return;
            }

            // River crossing prevents attack
            if (unit.HasMovedThisRound &&
                TerrainData.PreventsAttackOnCross(_terrainMap.GetTerrain(unit.Position)))
                return;

            // Flanking
            var flankDir = FlankingCalculator.GetFlankDirection(
                unit.Position, target.Position, target.Facing);

            // Formation: cannot be flanked?
            var targetFormation = GetFormationBonus(target);
            if (targetFormation.CannotBeFlanked)
                flankDir = FlankDirection.Front;

            // Charge check
            bool isCharge = unit.TilesMovedThisRound >= _config.ChargeMinTilesMoved
                && (unit.Ability == AbilityType.Charge || unit.Ability == AbilityType.ArmoredCharge)
                && !unit.HasChargedThisBattle
                && !TerrainData.BlocksCharge(_terrainMap.GetTerrain(target.Position));

            // Terrain modifiers
            int terrainDef = _terrainMap.GetDefenseMultiplier(target.Position);
            int terrainAtk = unit.Rng > 1 ? _terrainMap.GetAttackMultiplier(unit.Position) : 100;

            // Formation modifier
            var unitFormation = GetFormationBonus(unit);
            int formationMult = unitFormation.AtkMultiplier;
            int chargeMultiplier = isCharge
                ? (_config.ChargeMultiplier * unitFormation.ChargeMultiplier) / 100
                : 100;

            // Commander multipliers
            int cmdAtk = _commanders.GetAtkMultiplier(unit.Id);
            int cmdDef = _commanders.GetDefMultiplier(target.Id);

            // Aimed Shot: +50% if rifleman didn't move
            int aimedShotBonus = 100;
            if (unit.Ability == AbilityType.AimedShot && !unit.HasMovedThisRound)
                aimedShotBonus = 150;

            // Calculate damage
            int damage = DamageCalculator.Calculate(
                unit, target, flankDir,
                terrainDef, terrainAtk,
                (formationMult * cmdAtk * aimedShotBonus) / (100 * 100), // 3 base-100 values → 1 base-100: divide by 100^2
                isCharge, chargeMultiplier > 100 ? chargeMultiplier : (isCharge ? _config.ChargeMultiplier : 100),
                _config.MinimumDamage);

            // Apply commander DEF buff to target
            if (cmdDef != 100)
                damage = (damage * 100) / cmdDef;

            // Apply formation DEF bonus
            int targetDefFormation = targetFormation.DefMultiplier;
            if (targetDefFormation != 100)
                damage = (damage * 100) / targetDefFormation;

            damage = System.Math.Max(damage, _config.MinimumDamage);

            target.TakeDamage(damage);
            unit.HasAttackedThisRound = true;
            if (isCharge) unit.HasChargedThisBattle = true;

            _events.Add(new UnitAttackedEvent(
                _currentRound, unit.Id, target.Id, damage, flankDir, isCharge, false));

            if (!target.IsAlive)
                _events.Add(new UnitDiedEvent(_currentRound, target.Id, unit.Id));

            // AoE: Bombardment
            if (unit.Ability == AbilityType.Bombardment)
                ApplyBombardmentAoE(unit, target.Position, damage);

            // Dragoon dismount
            if (unit.Ability == AbilityType.Dismount && !unit.IsDismounted && unit.Rng == 1)
                unit.ApplyDismount(_config.DismountMov, _config.DismountDefBonus, _config.DismountAtkBonus);
        }

        private void ApplyBombardmentAoE(UnitInstance attacker, GridCoord center, int primaryDmg)
        {
            int splash = DamageCalculator.CalculateSplashDamage(primaryDmg, _config.BombardmentSplashPercentage);
            var adjacent = _grid.GetAdjacentCoords(center);

            foreach (var coord in adjacent)
            {
                var victim = _grid.GetUnitAt(coord);
                if (victim != null && victim.IsAlive && victim.Id != attacker.Id)
                {
                    victim.TakeDamage(splash);
                    _events.Add(new UnitAttackedEvent(
                        _currentRound, attacker.Id, victim.Id, splash,
                        FlankDirection.Front, false, true));
                    if (!victim.IsAlive)
                        _events.Add(new UnitDiedEvent(_currentRound, victim.Id, attacker.Id));
                }
            }
        }

        private void CleanupDead()
        {
            foreach (var u in _playerUnits)
                if (!u.IsAlive) _grid.RemoveUnit(u.Position);
            foreach (var u in _enemyUnits)
                if (!u.IsAlive) _grid.RemoveUnit(u.Position);
        }

        private void CheckWinConditions()
        {
            int pAlive = CountAlive(_playerUnits);
            int eAlive = CountAlive(_enemyUnits);

            if (pAlive == 0 && eAlive == 0)
                EndBattle(BattleOutcome.Draw);
            else if (eAlive == 0)
                EndBattle(BattleOutcome.PlayerWin);
            else if (pAlive == 0)
                EndBattle(BattleOutcome.EnemyWin);
            else if (_currentRound >= _config.MaxRounds)
            {
                int pHp = TotalHp(_playerUnits);
                int eHp = TotalHp(_enemyUnits);
                EndBattle(pHp > eHp ? BattleOutcome.PlayerWin :
                          eHp > pHp ? BattleOutcome.EnemyWin : BattleOutcome.Draw);
            }
        }

        private void EndBattle(BattleOutcome outcome)
        {
            _battleEnded = true;
            _outcome = outcome;
            _events.Add(new BattleEndedEvent(_currentRound, outcome, _currentRound));
        }

        private FormationBonus GetFormationBonus(UnitInstance unit)
        {
            return _formationBonuses.TryGetValue(unit.Id, out var bonus) ? bonus : FormationBonus.None;
        }

        private IReadOnlyList<UnitInstance> GetEnemies(UnitInstance unit)
        {
            return unit.Owner == Owner.Player ? _enemyUnits : _playerUnits;
        }

        private int CountAlive(IReadOnlyList<UnitInstance> units)
        {
            int c = 0;
            for (int i = 0; i < units.Count; i++) if (units[i].IsAlive) c++;
            return c;
        }

        private int TotalHp(List<UnitInstance> units)
        {
            int t = 0;
            foreach (var u in units) if (u.IsAlive) t += u.CurrentHp;
            return t;
        }

        private BattleResult BuildResult()
        {
            return new BattleResult(
                _outcome, _currentRound,
                CountAlive(_playerUnits), CountAlive(_enemyUnits),
                TotalHp(_playerUnits), TotalHp(_enemyUnits),
                _events);
        }
    }
}
