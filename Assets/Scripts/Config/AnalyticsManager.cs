using System;
using System.Collections.Generic;

namespace WarChess.Config
{
    /// <summary>
    /// Types of analytics events tracked by the game.
    /// </summary>
    public enum AnalyticsEventType
    {
        SessionStart,
        SessionEnd,
        BattleCompleted,
        UnitDeployed,
        CampaignProgress,
        PurchaseMade,
        ArmySubmitted,
        TierPromotion,
        OfficerLevelUp,
        DispatchBoxOpened,
        CosmeticEquipped,
        SettingsChanged,
        FieldManualPurchased,
        FieldManualRewardClaimed,
        SovereignsEarned,
        SovereignsSpent,
        BattleStarEarned,
        WeeklyChallengeCompleted,
        DeploymentRoundCompleted,
        GhostArmyUsed,
        BonusRoundTriggered,
        ChampionTitleChanged
    }

    /// <summary>
    /// A single analytics event with timestamp and parameters.
    /// Serializable for offline queue persistence.
    /// </summary>
    [Serializable]
    public class AnalyticsEvent
    {
        public AnalyticsEventType EventType;
        public long TimestampTicks;
        public Dictionary<string, string> Parameters;

        public AnalyticsEvent(AnalyticsEventType type)
        {
            EventType = type;
            TimestampTicks = DateTime.UtcNow.Ticks;
            Parameters = new Dictionary<string, string>();
        }

        /// <summary>
        /// Adds a parameter to this event. Returns self for chaining.
        /// </summary>
        public AnalyticsEvent With(string key, string value)
        {
            Parameters[key] = value;
            return this;
        }
    }

    /// <summary>
    /// Interface for analytics providers. David implements with Firebase or Unity Analytics.
    /// </summary>
    public interface IAnalyticsProvider
    {
        /// <summary>Sends a single event to the analytics backend.</summary>
        void SendEvent(AnalyticsEvent evt);

        /// <summary>Sends a batch of events.</summary>
        void SendBatch(List<AnalyticsEvent> events);

        /// <summary>Flushes any buffered events immediately.</summary>
        void Flush();
    }

    /// <summary>
    /// Manages analytics event queuing and dispatch.
    /// Pure C# — analytics provider is injected. Events queue locally and batch-send.
    /// </summary>
    public class AnalyticsManager
    {
        private readonly List<AnalyticsEvent> _eventQueue;
        private IAnalyticsProvider _provider;
        private readonly int _batchSize;
        private string _sessionId;

        /// <summary>
        /// Creates the analytics manager with a configurable batch size.
        /// </summary>
        public AnalyticsManager(int batchSize = 10)
        {
            _eventQueue = new List<AnalyticsEvent>();
            _batchSize = batchSize;
            _sessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
        }

        /// <summary>Sets the analytics provider (Firebase, Unity Analytics, etc.).</summary>
        public void SetProvider(IAnalyticsProvider provider)
        {
            _provider = provider;
        }

        /// <summary>Returns the pending event queue for save/load persistence.</summary>
        public IReadOnlyList<AnalyticsEvent> PendingEvents => _eventQueue;

        /// <summary>Loads pending events from a previous session (from save data).</summary>
        public void LoadPendingEvents(List<AnalyticsEvent> events)
        {
            if (events != null)
                _eventQueue.AddRange(events);
        }

        /// <summary>Logs a simple event with no parameters.</summary>
        public void LogEvent(AnalyticsEventType type)
        {
            var evt = new AnalyticsEvent(type);
            evt.With("session_id", _sessionId);
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs an event with key-value parameters.</summary>
        public void LogEvent(AnalyticsEventType type, Dictionary<string, string> parameters)
        {
            var evt = new AnalyticsEvent(type);
            evt.With("session_id", _sessionId);
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                    evt.Parameters[kvp.Key] = kvp.Value;
            }
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs a battle completion event with outcome details.</summary>
        public void LogBattleCompleted(int battleNumber, string outcome, int stars, int roundsPlayed)
        {
            var evt = new AnalyticsEvent(AnalyticsEventType.BattleCompleted)
                .With("session_id", _sessionId)
                .With("battle_number", battleNumber.ToString())
                .With("outcome", outcome)
                .With("stars", stars.ToString())
                .With("rounds", roundsPlayed.ToString());
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs a unit deployment event.</summary>
        public void LogUnitDeployed(string unitType, int tier)
        {
            var evt = new AnalyticsEvent(AnalyticsEventType.UnitDeployed)
                .With("session_id", _sessionId)
                .With("unit_type", unitType)
                .With("tier", tier.ToString());
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs campaign progression.</summary>
        public void LogCampaignProgress(int battleNumber, bool isFirstClear)
        {
            var evt = new AnalyticsEvent(AnalyticsEventType.CampaignProgress)
                .With("session_id", _sessionId)
                .With("battle_number", battleNumber.ToString())
                .With("first_clear", isFirstClear.ToString());
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs a purchase event.</summary>
        public void LogPurchase(string productId, string priceUsd)
        {
            var evt = new AnalyticsEvent(AnalyticsEventType.PurchaseMade)
                .With("session_id", _sessionId)
                .With("product_id", productId)
                .With("price_usd", priceUsd);
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs a session start.</summary>
        public void LogSessionStart()
        {
            _sessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            var evt = new AnalyticsEvent(AnalyticsEventType.SessionStart)
                .With("session_id", _sessionId);
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs a session end with duration.</summary>
        public void LogSessionEnd(int sessionDurationSeconds)
        {
            var evt = new AnalyticsEvent(AnalyticsEventType.SessionEnd)
                .With("session_id", _sessionId)
                .With("duration_seconds", sessionDurationSeconds.ToString());
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs a tier promotion event.</summary>
        public void LogTierPromotion(int newTier)
        {
            var evt = new AnalyticsEvent(AnalyticsEventType.TierPromotion)
                .With("session_id", _sessionId)
                .With("new_tier", newTier.ToString());
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs a dispatch box opening.</summary>
        public void LogDispatchBoxOpened(string boxType, int itemsAwarded)
        {
            var evt = new AnalyticsEvent(AnalyticsEventType.DispatchBoxOpened)
                .With("session_id", _sessionId)
                .With("box_type", boxType)
                .With("items_awarded", itemsAwarded.ToString());
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs a deployment round completion.</summary>
        public void LogDeploymentRoundCompleted(int tier, int totalStars, bool isPerfectRun,
            int ghostBattles, int houseBattles, int liveBattles)
        {
            var evt = new AnalyticsEvent(AnalyticsEventType.DeploymentRoundCompleted)
                .With("session_id", _sessionId)
                .With("tier", tier.ToString())
                .With("total_stars", totalStars.ToString())
                .With("perfect_run", isPerfectRun.ToString())
                .With("ghost_battles", ghostBattles.ToString())
                .With("house_battles", houseBattles.ToString())
                .With("live_battles", liveBattles.ToString());
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs when a ghost army is used as an opponent.</summary>
        public void LogGhostArmyUsed(int tier, bool isChampion)
        {
            var evt = new AnalyticsEvent(AnalyticsEventType.GhostArmyUsed)
                .With("session_id", _sessionId)
                .With("tier", tier.ToString())
                .With("is_champion", isChampion.ToString());
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs when a bonus round is triggered.</summary>
        public void LogBonusRoundTriggered(int tier, int standardStars)
        {
            var evt = new AnalyticsEvent(AnalyticsEventType.BonusRoundTriggered)
                .With("session_id", _sessionId)
                .With("tier", tier.ToString())
                .With("standard_stars", standardStars.ToString());
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Logs a champion title change.</summary>
        public void LogChampionTitleChanged(string playerId, string oldTitle, string newTitle)
        {
            var evt = new AnalyticsEvent(AnalyticsEventType.ChampionTitleChanged)
                .With("session_id", _sessionId)
                .With("player_id", playerId)
                .With("old_title", oldTitle)
                .With("new_title", newTitle);
            EnqueueAndMaybeSend(evt);
        }

        /// <summary>Flushes all queued events to the provider.</summary>
        public void Flush()
        {
            if (_provider == null || _eventQueue.Count == 0) return;

            _provider.SendBatch(new List<AnalyticsEvent>(_eventQueue));
            _eventQueue.Clear();
        }

        private void EnqueueAndMaybeSend(AnalyticsEvent evt)
        {
            _eventQueue.Add(evt);

            if (_provider != null && _eventQueue.Count >= _batchSize)
            {
                Flush();
            }
        }
    }
}
