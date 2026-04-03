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
        /// 2 stars = decisive victory (50%+ units surviving)
        /// 3 stars = flawless (all units surviving)
        /// </summary>
        public static int CalculateStars(BattleResult result, int totalPlayerUnits)
        {
            if (result.Outcome != BattleOutcome.PlayerWin)
                return 0;

            if (result.PlayerUnitsRemaining >= totalPlayerUnits)
                return 3; // Flawless

            // 50%+ surviving = decisive (integer division, rounds down)
            int halfUnits = (totalPlayerUnits + 1) / 2; // Ceiling division
            if (result.PlayerUnitsRemaining >= halfUnits)
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
