using System;
using System.Collections.Generic;
using WarChess.Economy;

namespace WarChess.Config
{
    /// <summary>
    /// Types of weekly challenges.
    /// </summary>
    public enum ChallengeType
    {
        WinBattles,
        DeployUnitType,
        AchieveFormation,
        CompleteCampaignBattles,
        WinWithCommander,
        WinInTier
    }

    /// <summary>
    /// Definition of a weekly challenge.
    /// </summary>
    [Serializable]
    public class WeeklyChallengeData
    {
        /// <summary>Unique ID for this challenge template.</summary>
        public string Id;

        /// <summary>Display name.</summary>
        public string Name;

        /// <summary>Description shown in UI.</summary>
        public string Description;

        /// <summary>Type of challenge.</summary>
        public ChallengeType Type;

        /// <summary>Target count to complete (e.g., win 5 battles).</summary>
        public int TargetCount;

        /// <summary>Optional filter (e.g., unit type ID, commander ID, tier number).</summary>
        public string Filter;

        /// <summary>Battle Stars awarded on completion.</summary>
        public int StarReward;
    }

    /// <summary>
    /// Tracks progress on a single active challenge.
    /// </summary>
    [Serializable]
    public class ActiveChallenge
    {
        /// <summary>Challenge template ID.</summary>
        public string ChallengeId;

        /// <summary>Current progress toward TargetCount.</summary>
        public int CurrentCount;

        /// <summary>Whether this challenge has been completed and reward claimed.</summary>
        public bool Completed;
    }

    /// <summary>
    /// Manages weekly challenge rotation and progress tracking.
    /// Awards Battle Stars on completion.
    /// Uses seeded RNG from week number for deterministic challenge selection.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public class WeeklyChallengeSystem
    {
        private readonly BattleStarSystem _battleStars;
        private readonly AnalyticsManager _analytics;
        private List<ActiveChallenge> _activeChallenges;
        private int _currentWeekNumber;

        /// <summary>Number of challenges active per week.</summary>
        public const int ChallengesPerWeek = 3;

        /// <summary>Event fired when a challenge is completed.</summary>
        public event Action<string> OnChallengeCompleted;

        /// <summary>
        /// Creates the weekly challenge system.
        /// </summary>
        public WeeklyChallengeSystem(
            BattleStarSystem battleStars,
            AnalyticsManager analytics,
            List<ActiveChallenge> existingChallenges = null,
            int currentWeekNumber = 0)
        {
            _battleStars = battleStars;
            _analytics = analytics;
            _activeChallenges = existingChallenges ?? new List<ActiveChallenge>();
            _currentWeekNumber = currentWeekNumber;
        }

        /// <summary>Returns active challenges for save/load.</summary>
        public IReadOnlyList<ActiveChallenge> ActiveChallenges => _activeChallenges;

        /// <summary>Returns the current week number.</summary>
        public int CurrentWeekNumber => _currentWeekNumber;

        /// <summary>
        /// Refreshes challenges for the current week. Call on session start.
        /// Generates new challenges if the week has changed.
        /// </summary>
        /// <param name="weekNumber">Current week number (e.g., week of year * year).</param>
        public void RefreshForWeek(int weekNumber)
        {
            if (weekNumber == _currentWeekNumber && _activeChallenges.Count > 0)
                return;

            _currentWeekNumber = weekNumber;
            _activeChallenges = new List<ActiveChallenge>();

            var templates = GetAllTemplates();
            var rng = new Random(weekNumber);

            // Shuffle templates
            for (int i = templates.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                var temp = templates[i];
                templates[i] = templates[j];
                templates[j] = temp;
            }

            // Pick first N
            for (int i = 0; i < ChallengesPerWeek && i < templates.Count; i++)
            {
                _activeChallenges.Add(new ActiveChallenge
                {
                    ChallengeId = templates[i].Id,
                    CurrentCount = 0,
                    Completed = false
                });
            }
        }

        /// <summary>
        /// Reports progress on challenges. Call when relevant events occur.
        /// </summary>
        /// <param name="type">The challenge type that progressed.</param>
        /// <param name="filter">Optional filter match (unit type, commander, etc.).</param>
        /// <param name="count">Amount of progress (default 1).</param>
        public void ReportProgress(ChallengeType type, string filter = null, int count = 1)
        {
            var templates = GetTemplateMap();

            foreach (var challenge in _activeChallenges)
            {
                if (challenge.Completed) continue;
                if (!templates.TryGetValue(challenge.ChallengeId, out var template)) continue;
                if (template.Type != type) continue;

                // Check filter match if the template has a filter
                if (!string.IsNullOrEmpty(template.Filter) && template.Filter != filter)
                    continue;

                challenge.CurrentCount += count;

                if (challenge.CurrentCount >= template.TargetCount)
                {
                    challenge.Completed = true;
                    _battleStars?.RewardWeeklyChallenge();

                    _analytics?.LogEvent(AnalyticsEventType.WeeklyChallengeCompleted,
                        new Dictionary<string, string>
                        {
                            { "challenge_id", challenge.ChallengeId },
                            { "week", _currentWeekNumber.ToString() }
                        });

                    OnChallengeCompleted?.Invoke(challenge.ChallengeId);
                }
            }
        }

        /// <summary>
        /// Returns the data for a challenge template by ID.
        /// </summary>
        public WeeklyChallengeData GetTemplate(string id)
        {
            var map = GetTemplateMap();
            return map.TryGetValue(id, out var data) ? data : null;
        }

        /// <summary>
        /// Returns the number of completed challenges this week.
        /// </summary>
        public int GetCompletedCount()
        {
            int count = 0;
            foreach (var c in _activeChallenges)
                if (c.Completed) count++;
            return count;
        }

        private Dictionary<string, WeeklyChallengeData> GetTemplateMap()
        {
            var map = new Dictionary<string, WeeklyChallengeData>();
            foreach (var t in GetAllTemplates())
                map[t.Id] = t;
            return map;
        }

        private static List<WeeklyChallengeData> GetAllTemplates()
        {
            return new List<WeeklyChallengeData>
            {
                new WeeklyChallengeData
                {
                    Id = "wc_win_5", Name = "Victorious General",
                    Description = "Win 5 multiplayer battles.",
                    Type = ChallengeType.WinBattles, TargetCount = 5, StarReward = 3
                },
                new WeeklyChallengeData
                {
                    Id = "wc_win_10", Name = "Unstoppable Force",
                    Description = "Win 10 multiplayer battles.",
                    Type = ChallengeType.WinBattles, TargetCount = 10, StarReward = 3
                },
                new WeeklyChallengeData
                {
                    Id = "wc_deploy_cavalry_10", Name = "Cavalry Charge",
                    Description = "Deploy Cavalry in 10 battles.",
                    Type = ChallengeType.DeployUnitType, TargetCount = 10, Filter = "Cavalry", StarReward = 3
                },
                new WeeklyChallengeData
                {
                    Id = "wc_deploy_artillery_8", Name = "Artillery Barrage",
                    Description = "Deploy Artillery in 8 battles.",
                    Type = ChallengeType.DeployUnitType, TargetCount = 8, Filter = "Artillery", StarReward = 3
                },
                new WeeklyChallengeData
                {
                    Id = "wc_formation_5", Name = "Formation Master",
                    Description = "Achieve any formation bonus in 5 battles.",
                    Type = ChallengeType.AchieveFormation, TargetCount = 5, StarReward = 3
                },
                new WeeklyChallengeData
                {
                    Id = "wc_campaign_3", Name = "Campaign Veteran",
                    Description = "Complete 3 campaign battles.",
                    Type = ChallengeType.CompleteCampaignBattles, TargetCount = 3, StarReward = 3
                },
                new WeeklyChallengeData
                {
                    Id = "wc_win_napoleon", Name = "Vive l'Empereur",
                    Description = "Win 3 battles using Napoleon as commander.",
                    Type = ChallengeType.WinWithCommander, TargetCount = 3, Filter = "Napoleon", StarReward = 3
                },
                new WeeklyChallengeData
                {
                    Id = "wc_win_wellington", Name = "The Iron Duke",
                    Description = "Win 3 battles using Wellington as commander.",
                    Type = ChallengeType.WinWithCommander, TargetCount = 3, Filter = "Wellington", StarReward = 3
                },
                new WeeklyChallengeData
                {
                    Id = "wc_deploy_grenadier_5", Name = "Grenadier Guard",
                    Description = "Deploy Grenadiers in 5 battles.",
                    Type = ChallengeType.DeployUnitType, TargetCount = 5, Filter = "Grenadier", StarReward = 3
                },
                new WeeklyChallengeData
                {
                    Id = "wc_win_tier2", Name = "Major General's Triumph",
                    Description = "Win 5 battles in Tier 2 or higher.",
                    Type = ChallengeType.WinInTier, TargetCount = 5, Filter = "2", StarReward = 3
                },
                new WeeklyChallengeData
                {
                    Id = "wc_deploy_oldguard_3", Name = "The Emperor's Finest",
                    Description = "Deploy the Old Guard in 3 battles.",
                    Type = ChallengeType.DeployUnitType, TargetCount = 3, Filter = "OldGuard", StarReward = 3
                },
                new WeeklyChallengeData
                {
                    Id = "wc_campaign_5", Name = "Seasoned Commander",
                    Description = "Complete 5 campaign battles.",
                    Type = ChallengeType.CompleteCampaignBattles, TargetCount = 5, StarReward = 3
                }
            };
        }
    }
}
