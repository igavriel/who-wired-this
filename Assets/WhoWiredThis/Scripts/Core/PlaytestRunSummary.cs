namespace WhoWiredThis.Core
{
    /// <summary>
    /// Snapshot of a playtest run collected before loading GameOverScene.
    /// </summary>
    public struct PlaytestRunSummaryData
    {
        public bool HasData;
        public bool WasAbandoned;
        public bool LastPuzzleCompleted;
        public string LastPuzzleName;
        public int AttemptCount;
        public int RetryCount;
        public float RunTimeSeconds;
        public float SceneTimerSeconds;
        public int CompletedPuzzleCount;
    }

    /// <summary>
    /// Holds the latest run summary across a scene load into GameOverScene.
    /// Cleared when returning to StartScene or starting a new run.
    /// </summary>
    public static class PlaytestRunSummary
    {
        private static PlaytestRunSummaryData current;

        public static bool HasSummary => current.HasData;

        public static PlaytestRunSummaryData Current => current;

        public static void Set(PlaytestRunSummaryData data)
        {
            current = data;
            current.HasData = true;
        }

        public static void Clear()
        {
            current = default;
        }

        /// <summary>
        /// Formats the run as a fixed 50×12 Game Over grid.
        /// </summary>
        public static string FormatDisplayText()
        {
            if (!current.HasData)
            {
                return string.Empty;
            }

            return PlaytestRunSummaryGridFormatter.Format(current);
        }
    }
}
