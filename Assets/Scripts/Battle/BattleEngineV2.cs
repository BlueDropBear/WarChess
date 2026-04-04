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

            // Limbered Up: regular Artillery (Bombardment) cannot attack if it moved this round.
            // Horse Artillery (LimberedUp ability) is exempt from this restriction.
            if (unit.HasMovedThisRound && unit.Rng > 1
                && unit.Ability == AbilityType.Bombardment)
                return;

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

            // Brace: Lancer counter-charge — when charged by cavalry, Lancer attacks first
            // and deals x1.5 damage. We model this by applying Brace damage to the charger
            // before the charger's attack resolves.
            if (isCharge && target.Ability == AbilityType.Brace && target.IsAlive)
            {
                int braceDamage = target.Atk - (unit.Def / 2);
                braceDamage = System.Math.Max(braceDamage, 1);
                braceDamage = braceDamage * 150 / 100; // x1.5 damage
                // Apply strength scaling to Brace damage (Lancer's regiment strength)
                int braceStrength = DamageCalculator.GetStrengthMultiplier(target, _config.StrengthScalingFloor);
                if (braceStrength < 100)
                    braceDamage = System.Math.Max(braceDamage * braceStrength / 100, _config.MinimumDamage);
                unit.TakeDamage(braceDamage);
                _events.Add(new UnitAttackedEvent(
                    _currentRound, target.Id, unit.Id, braceDamage,
                    FlankDirection.Front, false, false));
                if (!unit.IsAlive)
                {
                    _events.Add(new UnitDiedEvent(_currentRound, unit.Id, target.Id));
                    RemoveFromGrid(unit);
                    return; // Charger died to Brace, attack does not proceed
                }
            }

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

            // Unbreakable: Old Guard gains +25% ATK when HP below 25%
            int unbreakableBonus = 100;
            if (unit.Ability == AbilityType.Unbreakable && unit.CurrentHp * 4 <= unit.MaxHp)
                unbreakableBonus = 125;

            // Fold all ATK-side multipliers (formation ATK, commander ATK, aimed shot, unbreakable) into
            // one base-100 value for the formationMultiplier slot
            int atkFormation = (int)((long)formationMult * cmdAtk * aimedShotBonus * unbreakableBonus / (100L * 100L * 100L));

            // Fold target's DEF modifiers (commander DEF, formation DEF) into terrainDef so
            // they are batched with other multipliers BEFORE flanking inside DamageCalculator.
            int targetDefFormation = targetFormation.DefMultiplier;

            // Congreve Barrage ignores Fortification defense bonus per GDD
            if (unit.Ability == AbilityType.CongreveBarrage
                && _terrainMap.GetTerrain(target.Position) == TerrainType.Fortification)
                terrainDef = 100;

            int adjustedTerrainDef = (int)((long)terrainDef * cmdDef * targetDefFormation / (100L * 100L));

            // Calculate damage with all modifiers batched in correct GDD order
            int damage = DamageCalculator.Calculate(
                unit, target, flankDir,
                adjustedTerrainDef, terrainAtk,
                atkFormation,
                isCharge, chargeMultiplier > 100 ? chargeMultiplier : (isCharge ? _config.ChargeMultiplier : 100),
                _config.MinimumDamage);

            // Strength scaling: damaged units deal less damage (sqrt curve)
            int strengthMult = DamageCalculator.GetStrengthMultiplier(unit, _config.StrengthScalingFloor);
            if (strengthMult < 100)
                damage = System.Math.Max(damage * strengthMult / 100, _config.MinimumDamage);

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
            {
                _events.Add(new UnitDiedEvent(_currentRound, target.Id, unit.Id));
                RemoveFromGrid(target);
            }

            // AoE abilities
            if (unit.Ability == AbilityType.Bombardment)
                ApplyBombardmentAoE(unit, target.Position, damage);
            else if (unit.Ability == AbilityType.CongreveBarrage)
                ApplyCongreveBarrage(unit, target.Position, damage);

            // Grenade: first combat round of the battle, Grenadier deals 5 flat damage
            // to all enemies within 2 tiles
            if (unit.Ability == AbilityType.Grenade && !unit.HasUsedGrenadeThisBattle)
            {
                unit.HasUsedGrenadeThisBattle = true;
                ApplyGrenadeAoE(unit);
            }

            // Hit and Run: Hussar moves 2 tiles away from target after attacking
            if (unit.Ability == AbilityType.HitAndRun && unit.IsAlive)
                ApplyHitAndRun(unit, target);

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
                    {
                        _events.Add(new UnitDiedEvent(_currentRound, victim.Id, attacker.Id));
                        RemoveFromGrid(victim);
                    }
                }
            }
        }

        /// <summary>
        /// Congreve Barrage: hits a random 3x3 area near the target. May hit friendly units.
        /// Ignores Fortification defense. Per GDD, targeting does not account for friendly fire.
        /// </summary>
        private void ApplyCongreveBarrage(UnitInstance attacker, GridCoord targetPos, int primaryDmg)
        {
            int splash = DamageCalculator.CalculateSplashDamage(primaryDmg, _config.BombardmentSplashPercentage);

            // Select a random center within 1 tile of the target for the 3x3 blast
            var possibleCenters = new System.Collections.Generic.List<GridCoord>();
            possibleCenters.Add(targetPos);
            foreach (var adj in _grid.GetAdjacentCoords(targetPos))
                possibleCenters.Add(adj);
            var blastCenter = possibleCenters[_rng.Next(possibleCenters.Count)];

            // Hit all units in 3x3 area around blast center (includes friendlies per GDD)
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var coord = new GridCoord(blastCenter.X + dx, blastCenter.Y + dy);
                    if (!_grid.IsValidCoord(coord)) continue;
                    if (coord == attacker.Position) continue; // Don't hit self

                    var victim = _grid.GetUnitAt(coord);
                    if (victim == null || !victim.IsAlive) continue;
                    if (coord == targetPos) continue; // Primary target already hit

                    victim.TakeDamage(splash);
                    _events.Add(new UnitAttackedEvent(
                        _currentRound, attacker.Id, victim.Id, splash,
                        FlankDirection.Front, false, true));
                    if (!victim.IsAlive)
                    {
                        _events.Add(new UnitDiedEvent(_currentRound, victim.Id, attacker.Id));
                        RemoveFromGrid(victim);
                    }
                }
            }
        }

        /// <summary>
        /// Grenade: Grenadier deals 5 flat damage to all enemies within 2 tiles on first combat.
        /// </summary>
        private void ApplyGrenadeAoE(UnitInstance attacker)
        {
            const int grenadeDamage = 5;
            var coordsInRange = _grid.GetCoordsInRange(attacker.Position, 2);

            foreach (var coord in coordsInRange)
            {
                if (coord == attacker.Position) continue;
                var victim = _grid.GetUnitAt(coord);
                if (victim == null || !victim.IsAlive || victim.Owner == attacker.Owner) continue;

                victim.TakeDamage(grenadeDamage);
                _events.Add(new UnitAttackedEvent(
                    _currentRound, attacker.Id, victim.Id, grenadeDamage,
                    FlankDirection.Front, false, true));
                if (!victim.IsAlive)
                {
                    _events.Add(new UnitDiedEvent(_currentRound, victim.Id, attacker.Id));
                    RemoveFromGrid(victim);
                }
            }
        }

        /// <summary>
        /// Hit and Run: after attacking, Hussar moves 2 tiles away from the target.
        /// </summary>
        private void ApplyHitAndRun(UnitInstance unit, UnitInstance target)
        {
            int dx = unit.Position.X - target.Position.X;
            int dy = unit.Position.Y - target.Position.Y;

            // Normalize direction away from target
            int sx = dx > 0 ? 1 : (dx < 0 ? -1 : 0);
            int sy = dy > 0 ? 1 : (dy < 0 ? -1 : 0);

            // If on same tile as target (shouldn't happen), retreat toward own deployment
            if (sx == 0 && sy == 0)
                sy = unit.Owner == Owner.Player ? -1 : 1;

            var startPos = unit.Position;

            for (int step = 0; step < 2; step++)
            {
                var pos = unit.Position;
                // Try primary direction, then fallback
                var primary = new GridCoord(pos.X + sx, pos.Y + sy);
                var fallbackX = new GridCoord(pos.X + sx, pos.Y);
                var fallbackY = new GridCoord(pos.X, pos.Y + sy);

                if (_grid.IsValidCoord(primary) && _grid.IsTileEmpty(primary))
                    _grid.MoveUnit(pos, primary);
                else if (sx != 0 && _grid.IsValidCoord(fallbackX) && _grid.IsTileEmpty(fallbackX))
                    _grid.MoveUnit(pos, fallbackX);
                else if (sy != 0 && _grid.IsValidCoord(fallbackY) && _grid.IsTileEmpty(fallbackY))
                    _grid.MoveUnit(pos, fallbackY);
                else
                    break; // No valid retreat tile
            }

            if (unit.Position != startPos)
                _events.Add(new UnitMovedEvent(_currentRound, unit.Id, startPos, unit.Position, 0));
        }

        /// <summary>
        /// Immediately removes a dead unit from the grid so it doesn't block tiles mid-round.
        /// </summary>
        private void RemoveFromGrid(UnitInstance unit)
        {
            if (_removedFromGrid.Add(unit.Id))
                _grid.RemoveUnit(unit.Position);
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
