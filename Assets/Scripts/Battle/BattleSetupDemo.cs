using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WarChess.Config;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle
{
    /// <summary>
    /// Quick demo script for testing the battle system. Attach to a GameObject
    /// in the Battle scene. Automatically creates a grid, places test armies,
    /// and runs a battle with visualization.
    ///
    /// No ScriptableObject assets needed — uses UnitFactory hardcoded stats.
    /// Delete this script once proper scene setup is in place.
    /// </summary>
    public class BattleSetupDemo : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _battleSeed = 42;
        [SerializeField] private float _playbackSpeed = 1f;
        [SerializeField] private bool _autoStart = true;

        [Header("Colors")]
        [SerializeField] private Color _tileColorA = new Color(0.35f, 0.55f, 0.23f);
        [SerializeField] private Color _tileColorB = new Color(0.30f, 0.50f, 0.20f);
        [SerializeField] private Color _playerUnitColor = new Color(0.8f, 0.13f, 0.13f);
        [SerializeField] private Color _enemyUnitColor = new Color(0.13f, 0.27f, 0.67f);
        [SerializeField] private Color _cavalryTint = new Color(1f, 0.85f, 0.4f);
        [SerializeField] private Color _artilleryTint = new Color(0.5f, 0.5f, 0.5f);

        private GridMap _gridMap;
        private GridView _gridView;
        private BattleVisualizer _visualizer;
        private Dictionary<int, UnitView> _unitViews;
        private GameConfigData _config;

        private void Start()
        {
            if (_autoStart)
                StartCoroutine(RunDemoBattle());
        }

        /// <summary>
        /// Call this to manually start the demo battle.
        /// </summary>
        public void StartDemo()
        {
            StartCoroutine(RunDemoBattle());
        }

        private IEnumerator RunDemoBattle()
        {
            Debug.Log("=== WarChess Battle Demo ===");

            // Setup
            _config = GameConfigData.Default;
            _gridMap = new GridMap(_config.GridWidth, _config.GridHeight);
            _unitViews = new Dictionary<int, UnitView>();

            // Create grid view
            var gridGo = new GameObject("Grid");
            _gridView = gridGo.AddComponent<GridView>();
            _gridView.Initialize(_gridMap);

            // Create visualizer
            var vizGo = new GameObject("Visualizer");
            _visualizer = vizGo.AddComponent<BattleVisualizer>();
            _visualizer.SetSpeed(_playbackSpeed);

            // Position camera
            SetupCamera();

            yield return null; // Let grid render

            // Create armies
            UnitFactory.ResetIds();
            var playerArmy = UnitFactory.CreateTestPlayerArmy();
            var enemyArmy = UnitFactory.CreateTestEnemyArmy();

            // Place on grid and create views
            foreach (var unit in playerArmy)
            {
                _gridMap.PlaceUnit(unit, unit.Position);
                CreateUnitView(unit, GetUnitColor(unit, true));
            }
            foreach (var unit in enemyArmy)
            {
                _gridMap.PlaceUnit(unit, unit.Position);
                CreateUnitView(unit, GetUnitColor(unit, false));
            }

            Debug.Log($"Player army: {playerArmy.Count} units");
            Debug.Log($"Enemy army: {enemyArmy.Count} units");

            yield return new WaitForSeconds(0.5f);

            // Run battle
            var engine = new BattleEngine(_gridMap, playerArmy, enemyArmy, _config, _battleSeed);
            _visualizer.RegisterUnitViews(_unitViews);

            bool battleContinues = true;
            while (battleContinues)
            {
                battleContinues = engine.RunRound();

                // Get this round's events
                var allEvents = engine.Events;
                var roundEvents = new List<BattleEvent>();
                for (int i = 0; i < allEvents.Count; i++)
                {
                    if (allEvents[i].Round == engine.CurrentRound)
                        roundEvents.Add(allEvents[i]);
                }

                Debug.Log($"--- Round {engine.CurrentRound} ({roundEvents.Count} events) ---");

                // Play round visualization
                _visualizer.PlayRound(roundEvents);

                // Wait for visualization to finish
                while (_visualizer.IsPlaying)
                    yield return null;

                yield return new WaitForSeconds(0.2f);
            }

            // Report result
            var lastEvent = engine.Events[engine.Events.Count - 1] as BattleEndedEvent;
            if (lastEvent != null)
            {
                Debug.Log($"=== BATTLE ENDED: {lastEvent.Outcome} after {lastEvent.RoundsPlayed} rounds ===");
            }
        }

        private Color GetUnitColor(UnitInstance unit, bool isPlayer)
        {
            Color baseColor = isPlayer ? _playerUnitColor : _enemyUnitColor;

            // Tint by unit type for visual distinction
            switch (unit.Type)
            {
                case UnitType.Cavalry:
                    return Color.Lerp(baseColor, _cavalryTint, 0.4f);
                case UnitType.Artillery:
                    return Color.Lerp(baseColor, _artilleryTint, 0.4f);
                default:
                    return baseColor;
            }
        }

        private void CreateUnitView(UnitInstance unit, Color color)
        {
            var go = new GameObject($"{unit.Owner}_{unit.Name}_{unit.Id}");
            go.transform.position = _gridView.GridToWorld(unit.Position);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateUnitSprite(unit.Type);
            sr.color = color;
            sr.sortingOrder = 1;

            var view = go.AddComponent<UnitView>();
            view.Initialize(unit, _gridView);
            _unitViews[unit.Id] = view;
        }

        private Sprite CreateUnitSprite(UnitType type)
        {
            int size = 28;
            var tex = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];

            // Clear to transparent
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            // Draw different shapes per unit type
            switch (type)
            {
                case UnitType.LineInfantry:
                    // Square — solid frontline
                    FillRect(pixels, 4, 4, 28, 28, Color.white);
                    break;

                case UnitType.Cavalry:
                    // Diamond — fast flanker
                    for (int y = 0; y < 32; y++)
                    {
                        for (int x = 0; x < 32; x++)
                        {
                            int cx = x - 16, cy = y - 16;
                            if (Mathf.Abs(cx) + Mathf.Abs(cy) <= 14)
                                pixels[y * 32 + x] = Color.white;
                        }
                    }
                    break;

                case UnitType.Artillery:
                    // Circle — ranged bombardment
                    for (int y = 0; y < 32; y++)
                    {
                        for (int x = 0; x < 32; x++)
                        {
                            int cx = x - 16, cy = y - 16;
                            if (cx * cx + cy * cy <= 14 * 14)
                                pixels[y * 32 + x] = Color.white;
                        }
                    }
                    break;

                default:
                    FillRect(pixels, 2, 2, 30, 30, Color.white);
                    break;
            }

            tex.SetPixels(pixels);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        private void FillRect(Color[] pixels, int x0, int y0, int x1, int y1, Color color)
        {
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    pixels[y * 32 + x] = color;
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 6f;
                // Center on the grid (tiles at 0-9 in world coords)
                cam.transform.position = new Vector3(4.5f, 4.5f, -10f);
                cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            }
        }
    }
}
