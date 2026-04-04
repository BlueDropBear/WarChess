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
        private readonly HashSet<int> _removedFromGrid;

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
            _removedFromGrid = new HashSet<int>();
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

            // Recalculate formations after movement — pre-movement bonuses are stale
            RecalculateFormations();

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

            // Sort by SPD descending, then by seeded random for tie-break (GDD Section 2.4)
            var tieBreakers = new Dictionary<int, int>();
            foreach (var u in all)
                tieBreakers[u.Id] = _rng.Next();
            all.Sort((a, b) =>
            {
                int cmp = b.Spd.CompareTo(a.Spd);
                return cmp != 0 ? cmp : tieBreakers[b.Id].CompareTo(tieBreakers[a.Id]);
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
                var to = MovementResolverV2.ResolveMovementWithSteps(
                    unit, target, _grid, _terrainMap, effectiveMov, out int stepsTaken);

                if (to != from)
                {
                    // Track if the unit crossed a river tile during movement
                    if (_terrainMap.GetTerrain(to) == Terrain.TerrainType.River ||
                        _terrainMap.GetTerrain(from) == Terrain.TerrainType.River)
                        unit.CrossedRiverThisRound = true;

                    _grid.MoveUnit(from, to);
                    unit.TilesMovedThisRound = stepsTaken;
                    unit.HasMovedThisRound = true;
                    _events.Add(new UnitMovedEvent(_currentRound, unit.Id, from, to, stepsTaken));
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

            // River crossing prevents attack on the same round
            if (unit.CrossedRiverThisRound)
                return;

            // Flanking
            var flankDir = FlankingCalculator.GetFlankDirection(
                unit.Position, target.Position, target.Facing);

            // Formation: cannot be flanked?
            var targetFormation = GetFormationBonus(target);
            if (targetFormation.CannotBeFlanked)
                flankDir = FlankDirection.Front;

            // Charge check (HasGuaranteedCharge bypasses tile-movement requirement for Blücher)
            bool isCharge = (unit.HasGuaranteedCharge || unit.TilesMovedThisRound >= _config.ChargeMinTilesMoved)
                && (unit.Ability == AbilityType.Charge || unit.Ability == AbilityType.ArmoredCharge)
                && !unit.HasChargedThisRound
                && !TerrainData.BlocksCharge(_terrainMap.GetTerrain(target.Position));

            // Terrain modifiers
            int terrainDef = _terrainMap.GetDefenseMultiplier(target.Position);
            int terrainAtk = _terrainMap.GetAttackMultiplier(unit.Position);

            // Formation modifiers
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

            // Fold all ATK-side multipliers (formation ATK, commander ATK, aimed shot) into
            // one base-100 value for the formationMultiplier slot
            int atkFormation = (int)((long)formationMult * cmdAtk * aimedShotBonus / (100L * 100L));

            // Fold target's DEF modifiers (commander DEF, formation DEF) into terrainDef so
            // they are batched with other multipliers BEFORE flanking inside DamageCalculator.
            // Previously these were applied post-hoc with sequential division, which changed
            // rounding and violated GDD modifier order (formation before flanking).
            int targetDefFormation = targetFormation.DefMultiplier;
            int adjustedTerrainDef = (int)((long)terrainDef * cmdDef * targetDefFormation / (100L * 100L));

            // Calculate damage with all modifiers batched in correct GDD order
            int damage = DamageCalculator.Calculate(
                unit, target, flankDir,
                adjustedTerrainDef, terrainAtk,
                atkFormation,
                isCharge, chargeMultiplier > 100 ? chargeMultiplier : (isCharge ? _config.ChargeMultiplier : 100),
                _config.MinimumDamage);

            target.TakeDamage(damage);
            unit.HasAttackedThisRound = true;
            if (isCharge)
            {
                unit.HasChargedThisRound = true;
                unit.HasGuaranteedCharge = false;
            }

            _events.Add(new UnitAttackedEvent(
                _currentRound, unit.Id, target.Id, damage, flankDir, isCharge, false));

            if (!target.IsAlive)
                _events.Add(new UnitDiedEvent(_currentRound, target.Id, unit.Id));

            // AoE: Bombardment
            if (unit.Ability == AbilityType.Bombardment)
                ApplyBombardmentAoE(unit, target.Position, damage);

            // Dragoon dismount (triggers after any melee attack, switches to LineInfantry for formations)
            if (unit.Ability == AbilityType.Dismount && !unit.IsDismounted)
                unit.ApplyDismount(_config.DismountMov, _config.DismountDefBonus, _config.DismountAtkBonus);
        }

        private void ApplyBombardmentAoE(UnitInstance attacker, GridCoord center, int primaryDmg)
        {
            int splash = DamageCalculator.CalculateSplashDamage(primaryDmg, _config.BombardmentSplashPercentage);
            var adjacent = _grid.GetAdjacentCoords(center);

            foreach (var coord in adjacent)
            {
                var victim = _grid.GetUnitAt(coord);
                if (victim != null && victim.IsAlive && victim.Id != attacker.Id
                    && victim.Owner != attacker.Owner)
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
                if (!u.IsAlive && _removedFromGrid.Add(u.Id)) _grid.RemoveUnit(u.Position);
            foreach (var u in _enemyUnits)
                if (!u.IsAlive && _removedFromGrid.Add(u.Id)) _grid.RemoveUnit(u.Position);
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
