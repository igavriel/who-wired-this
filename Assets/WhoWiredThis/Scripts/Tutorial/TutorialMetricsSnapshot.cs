namespace WhoWiredThis.Tutorial
{
    /// <summary>
    /// Read-only metrics for future summary, scoring, and persistence.
    /// </summary>
    public readonly struct TutorialMetricsSnapshot
    {
        public TutorialMetricsSnapshot(
            int totalAttempts,
            int playerAAttempts,
            int playerBAttempts,
            float totalElapsedSeconds,
            float playerAElapsedSeconds,
            float playerBElapsedSeconds,
            bool playerASolved,
            bool playerBSolved,
            bool tutorialComplete)
        {
            TotalAttempts = totalAttempts;
            PlayerAAttempts = playerAAttempts;
            PlayerBAttempts = playerBAttempts;
            TotalElapsedSeconds = totalElapsedSeconds;
            PlayerAElapsedSeconds = playerAElapsedSeconds;
            PlayerBElapsedSeconds = playerBElapsedSeconds;
            PlayerASolved = playerASolved;
            PlayerBSolved = playerBSolved;
            TutorialComplete = tutorialComplete;
        }

        public int TotalAttempts { get; }
        public int PlayerAAttempts { get; }
        public int PlayerBAttempts { get; }
        public float TotalElapsedSeconds { get; }
        public float PlayerAElapsedSeconds { get; }
        public float PlayerBElapsedSeconds { get; }
        public bool PlayerASolved { get; }
        public bool PlayerBSolved { get; }
        public bool TutorialComplete { get; }
    }
}
