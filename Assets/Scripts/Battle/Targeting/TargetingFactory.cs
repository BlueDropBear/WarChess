using WarChess.Units;

namespace WarChess.Battle.Targeting
{
    /// <summary>
    /// Maps TargetingPriority enum to ITargetingStrategy instances.
    /// Most strategies are stateless singletons; RandomTargeting needs a BattleRng.
    /// </summary>
    public static class TargetingFactory
    {
        /// <summary>
        /// Creates (or returns) the targeting strategy for the given priority.
        /// </summary>
        public static ITargetingStrategy Create(TargetingPriority priority, BattleRng rng)
        {
            return priority switch
            {
                TargetingPriority.Nearest => NearestTargeting.Instance,
                TargetingPriority.Weakest => WeakestTargeting.Instance,
                TargetingPriority.HighestThreat => HighestThreatTargeting.Instance,
                TargetingPriority.ArtilleryFirst => ArtilleryFirstTargeting.Instance,
                TargetingPriority.Random => new RandomTargeting(rng),
                _ => NearestTargeting.Instance
            };
        }
    }
}
