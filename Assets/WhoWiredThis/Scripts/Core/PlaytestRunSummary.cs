using System.Text;

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
        public float RunTimeSeconds;
        public float SceneTimerSeconds;
        public int Score;
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

        public static string FormatDisplayText()
        {
            if (!current.HasData)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Run Summary");
            builder.AppendLine();

            string status = current.WasAbandoned
                ? "Status: Abandoned by players"
                : current.LastPuzzleCompleted
                    ? "Status: Run completed"
                    : "Status: Ended";

            builder.AppendLine(status);
            builder.AppendLine($"Last puzzle: {current.LastPuzzleName ?? "Unknown"}");

            if (current.LastPuzzleCompleted)
            {
                builder.AppendLine("Puzzle result: Completed");
            }
            else if (current.WasAbandoned)
            {
                builder.AppendLine("Puzzle result: Abandoned");
            }

            if (current.CompletedPuzzleCount > 0)
            {
                builder.AppendLine($"Completed puzzles: {current.CompletedPuzzleCount}");
            }

            if (current.AttemptCount > 0)
            {
                builder.AppendLine($"Attempts: {current.AttemptCount}");
            }

            builder.AppendLine($"Time: {PlaytestRunTotal.FormatTime(current.RunTimeSeconds)}");

            if (current.SceneTimerSeconds > 0f)
            {
                builder.AppendLine($"Scene timer: {PlaytestRunTotal.FormatTime(current.SceneTimerSeconds)}");
            }

            builder.AppendLine($"Score: {current.Score}");

            return builder.ToString().TrimEnd();
        }
    }
}
