using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WarChess.Config;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle
{
    /// <summary>
    /// Top-level MonoBehaviour that orchestrates a battle. Wires the headless
    /// BattleEngine to the visual BattleVisualizer. This is the Bridge Layer.
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridView _gridView;
        [SerializeField] private BattleVisualizer _visualizer;
        [SerializeField] private GameConfigSO _gameConfig;
        [SerializeField] private GameObject _unitPrefab;

        [Header("Unit Data (Prototype)")]
        [SerializeField] private UnitStatsSO _lineInfantrySO;
        [SerializeField] private UnitStatsSO _cavalrySO;
        [SerializeField] private UnitStatsSO _artillerySO;

        [Header("Unit Colors (Prototype Placeholders)")]
        [SerializeField] private Color _playerColor = new Color(0.8f, 0.13f, 0.13f);
        [SerializeField] private Color _enemyColor = new Color(0.13f, 0.27f, 0.67f);

        /// <summary>Fired when the battle completes with the result.</summary>
        public event Action<BattleResult> OnBattleCompleted;

        private BattleEngine _engine;
        private GridMap _gridMap;
        private GameConfigData _configData;
        private Dictionary<int, UnitView> _unitViews;
        private int _nextUnitId;

        /// <summary>
        /// Starts a battle with the given player and enemy unit placements.
        /// Each tuple is (UnitStatsSO, GridCoord).
        /// </summary>
        public void StartBattle(
            List<(UnitStatsSO stats, GridCoord position)> playerArmy,
            List<(UnitStatsSO stats, GridCoord position)> enemyArmy,
            int seed)
        {
            _configData = _gameConfig.ToData();
            _gridMap = new GridMap(_configData.GridWidth, _configData.GridHeight);
            _unitViews = new Dictionary<int, UnitView>();
            _nextUnitId = 1;

            // Initialize grid view
            _gridView.Initialize(_gridMap);

            // Create player units
            var playerUnits = new List<UnitInstance>();
            foreach (var (stats, pos) in playerArmy)
            {
                var unit = stats.CreateInstance(_nextUnitId++, Owner.Player, pos);
                _gridMap.PlaceUnit(unit, pos);
                playerUnits.Add(unit);
                CreateUnitView(unit, _playerColor);
            }

            // Create enemy units
            var enemyUnits = new List<UnitInstance>();
            foreach (var (stats, pos) in enemyArmy)
            {
                var unit = stats.CreateInstance(_nextUnitId++, Owner.Enemy, pos);
                _gridMap.PlaceUnit(unit, pos);
                enemyUnits.Add(unit);
                CreateUnitView(unit, _enemyColor);
            }

            // Create engine and register views
            _engine = new BattleEngine(_gridMap, playerUnits, enemyUnits, _configData, seed);
            _visualizer.RegisterUnitViews(_unitViews);
            _visualizer.OnRoundVisualized += OnRoundVisualized;
            _visualizer.OnBattleVisualized += OnBattleVisualizationComplete;

            // Run the first round
            RunNextRound();
        }

        private void RunNextRound()
        {
            bool continuesBattle = _engine.RunRound();

            // Get events for this round
            var allEvents = _engine.Events;
            var roundEvents = new List<BattleEvent>();
            for (int i = 0; i < allEvents.Count; i++)
            {
                if (allEvents[i].Round == _engine.CurrentRound)
                    roundEvents.Add(allEvents[i]);
            }

            _visualizer.PlayRound(roundEvents);

            if (!continuesBattle)
            {
                // Battle is over — visualizer will fire OnBattleVisualized when done
            }
        }

        private void OnRoundVisualized()
        {
            if (!_engine.IsBattleOver)
            {
                RunNextRound();
            }
        }

        private void OnBattleVisualizationComplete()
        {
            _visualizer.OnRoundVisualized -= OnRoundVisualized;
            _visualizer.OnBattleVisualized -= OnBattleVisualizationComplete;

            // Build and emit result
            var events = _engine.Events;
            BattleEndedEvent endEvent = null;
            for (int i = events.Count - 1; i >= 0; i--)
            {
                if (events[i] is BattleEndedEvent bee)
                {
                    endEvent = bee;
                    break;
                }
            }

            if (endEvent != null)
            {
                var result = new BattleResult(
                    endEvent.Outcome,
                    endEvent.RoundsPlayed,
                    CountAlive(_gridMap.GetAllUnits(Owner.Player)),
                    CountAlive(_gridMap.GetAllUnits(Owner.Enemy)),
                    TotalHp(_gridMap.GetAllUnits(Owner.Player)),
                    TotalHp(_gridMap.GetAllUnits(Owner.Enemy)),
                    events);

                OnBattleCompleted?.Invoke(result);
            }
        }

        private void CreateUnitView(UnitInstance unit, Color color)
        {
            GameObject go;
            if (_unitPrefab != null)
            {
                go = Instantiate(_unitPrefab, _gridView.GridToWorld(unit.Position), Quaternion.identity);
            }
            else
            {
                go = new GameObject($"Unit_{unit.Name}_{unit.Id}");
                go.transform.position = _gridView.GridToWorld(unit.Position);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreatePlaceholderSprite();
                sr.color = color;
                sr.sortingOrder = 1;
            }

            var view = go.GetComponent<UnitView>();
            if (view == null)
                view = go.AddComponent<UnitView>();

            view.Initialize(unit, _gridView);
            _unitViews[unit.Id] = view;
        }

        private Sprite CreatePlaceholderSprite()
        {
            var tex = new Texture2D(128, 128);
            var pixels = new Color[128 * 128];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128f);
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
    }
}
