namespace WarChess.Battle
{
    /// <summary>
    /// Calculates star ratings and other post-battle metrics from a BattleResult.
    /// Pure C# — no Unity dependencies.
    /// </summary>
    public static class BattleResultCalculator
    {
        /// <summary>
        /// Calculates star rating (0-3) per GDD Section 6.2:
        /// 0 stars = loss
        /// 1 star  = victory (any win)
        /// 2 stars = decisive victory (thresholdDecisive% units surviving)
        /// 3 stars = flawless (thresholdFlawless% units surviving)
        /// Thresholds are configurable via GameConfigData for balance tuning.
        /// </summary>
        /// <param name="result">The battle result.</param>
        /// <param name="totalPlayerUnits">Total player units at battle start.</param>
        /// <param name="thresholdFlawless">% of units surviving for 3 stars (0-100, default 100).</param>
        /// <param name="thresholdDecisive">% of units surviving for 2 stars (0-100, default 50).</param>
        public static int CalculateStars(BattleResult result, int totalPlayerUnits,
            int thresholdFlawless = 100, int thresholdDecisive = 50)
        {
            if (result.Outcome != BattleOutcome.PlayerWin)
                return 0;

            if (totalPlayerUnits <= 0)
                return 1;

            // Calculate survival percentage (integer math, base 100)
            int survivalPercent = result.PlayerUnitsRemaining * 100 / totalPlayerUnits;

            if (survivalPercent >= thresholdFlawless)
                return 3; // Flawless

            if (survivalPercent >= thresholdDecisive)
                return 2; // Decisive

            return 1; // Victory
        }

        /// <summary>
        /// Returns a display string for the star rating.
        /// </summary>
        public static string GetStarDisplay(int stars)
        {
            return stars switch
            {
                0 => "Defeat",
                1 => "Victory",
                2 => "Decisive Victory",
                3 => "Flawless Victory",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Returns a brief summary of the battle for the results screen.
        /// </summary>
        public static string GetBattleSummary(BattleResult result, int totalPlayerUnits)
        {
            int stars = CalculateStars(result, totalPlayerUnits);
            string starText = GetStarDisplay(stars);

            return $"{starText} - {result.RoundsPlayed} rounds, " +
                   $"{result.PlayerUnitsRemaining}/{totalPlayerUnits} units surviving, " +
                   $"{result.PlayerHpRemaining} HP remaining";
        }
    }
}
