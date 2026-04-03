using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WarChess.Core;
using WarChess.Units;

namespace WarChess.Battle
{
    /// <summary>
    /// Replays BattleEvents as animated sequences in Unity. Consumes events
    /// from BattleEngine and drives UnitView animations.
    /// </summary>
    public class BattleVisualizer : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float _moveAnimDuration = 0.3f;
        [SerializeField] private float _attackAnimDuration = 0.2f;
        [SerializeField] private float _deathAnimDuration = 0.5f;
        [SerializeField] private float _pauseBetweenEvents = 0.1f;

        private Dictionary<int, UnitView> _unitViews;
        private float _speedMultiplier = 1f;
        private bool _isPlaying;

        /// <summary>Fired when all events for a round have finished playing.</summary>
        public event Action OnRoundVisualized;

        /// <summary>Fired when the entire battle visualization is complete.</summary>
        public event Action OnBattleVisualized;

        /// <summary>Whether animations are currently playing.</summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Registers unit views so the visualizer can animate them.
        /// Call before starting playback.
        /// </summary>
        public void RegisterUnitViews(Dictionary<int, UnitView> unitViews)
        {
            _unitViews = unitViews;
        }

        /// <summary>
        /// Sets the playback speed multiplier (1 = normal, 2 = fast, 4 = fastest).
        /// </summary>
        public void SetSpeed(float multiplier)
        {
            _speedMultiplier = Mathf.Max(multiplier, 0.1f);
        }

        /// <summary>
        /// Plays a list of events for one round as an animated sequence.
        /// </summary>
        public void PlayRound(IReadOnlyList<BattleEvent> events)
        {
            StartCoroutine(PlayEventsCoroutine(events));
        }

        /// <summary>
        /// Plays all events from a complete battle.
        /// </summary>
        public void PlayFullBattle(IReadOnlyList<BattleEvent> events)
        {
            StartCoroutine(PlayFullBattleCoroutine(events));
        }

        private IEnumerator PlayFullBattleCoroutine(IReadOnlyList<BattleEvent> events)
        {
            _isPlaying = true;

            for (int i = 0; i < events.Count; i++)
            {
                yield return PlaySingleEvent(events[i]);
            }

            _isPlaying = false;
            OnBattleVisualized?.Invoke();
        }

        private IEnumerator PlayEventsCoroutine(IReadOnlyList<BattleEvent> events)
        {
            _isPlaying = true;

            for (int i = 0; i < events.Count; i++)
            {
                yield return PlaySingleEvent(events[i]);
            }

            _isPlaying = false;
            OnRoundVisualized?.Invoke();
        }

        private IEnumerator PlaySingleEvent(BattleEvent evt)
        {
            float pause = _pauseBetweenEvents / _speedMultiplier;

            switch (evt)
            {
                case UnitMovedEvent moveEvt:
                    if (_unitViews.TryGetValue(moveEvt.UnitId, out var movingUnit))
                    {
                        movingUnit.MoveTo(moveEvt.To);
                        yield return new WaitForSeconds(_moveAnimDuration / _speedMultiplier);
                    }
                    break;

                case UnitAttackedEvent attackEvt:
                    if (_unitViews.TryGetValue(attackEvt.DefenderId, out var defendingUnit))
                    {
                        defendingUnit.PlayHitFlash();
                        defendingUnit.UpdateHealthBar();
                        yield return new WaitForSeconds(_attackAnimDuration / _speedMultiplier);
                    }
                    break;

                case UnitDiedEvent deathEvt:
                    if (_unitViews.TryGetValue(deathEvt.UnitId, out var deadUnit))
                    {
                        deadUnit.PlayDeath();
                        yield return new WaitForSeconds(_deathAnimDuration / _speedMultiplier);
                        _unitViews.Remove(deathEvt.UnitId);
                    }
                    break;

                case RoundStartedEvent:
                    yield return new WaitForSeconds(pause);
                    break;

                case BattleEndedEvent:
                    // Battle end handled by BattleController
                    break;
            }

            yield return new WaitForSeconds(pause);
        }
    }
}
