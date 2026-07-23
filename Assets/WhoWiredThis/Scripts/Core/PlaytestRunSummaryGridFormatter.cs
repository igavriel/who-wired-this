using System.Collections.Generic;
using WhoWiredThis.Puzzles.Common;

namespace WhoWiredThis.Core
{
    /// <summary>
    /// Formats the Game Over run summary as a fixed monospace 50×12 character grid.
    /// </summary>
    public static class PlaytestRunSummaryGridFormatter
    {
        public const int Width = 50;
        public const int TotalLines = 12;

        private const int NameWidth = 18;
        private const int StatWidth = 10;
        private const int StatGap = 5;
        private const int TableLeftMargin = 3;
        private const int TableRightMargin = 4;

        private static readonly string[] OrderedLevelNames =
        {
            "Tutorial",
            "Puzzle Pipes",
            "Puzzle Signal",
        };

        public static string Format(PlaytestRunSummaryData data)
        {
            string status = data.WasAbandoned
                ? "Abandoned"
                : data.LastPuzzleCompleted
                    ? "Completed"
                    : "Ended";

            string lastPuzzle = Truncate(data.LastPuzzleName ?? "Unknown", 28);
            string runTime = ScoreManager.FormatTime(data.RunTimeSeconds);
            int teamScore = PlaytestTeamScoreCalculator.CalculateTeamScore();

            int attempts = ScoreManager.GetTotalAttemptsAcrossLevels();
            if (attempts <= 0 && data.AttemptCount > 0)
            {
                attempts = data.AttemptCount;
            }

            int scenesDone = data.CompletedPuzzleCount;
            int scenesTotal = OrderedLevelNames.Length;

            var lines = new List<string>(TotalLines)
            {
                ComponentDiagnosticLogFormatter.FormatLabelStatus("RUN SUMMARY", runTime, Width),
                ComponentDiagnosticLogFormatter.FormatLabelStatus("STATUS", status, Width),
                ComponentDiagnosticLogFormatter.FormatLabelStatus("LAST", lastPuzzle, Width),
                ComponentDiagnosticLogFormatter.FormatLabelStatus("SCORE", teamScore.ToString(), Width),
                new string('-', Width),
                FormatTableHeader(),
            };

            for (int i = 0; i < OrderedLevelNames.Length; i++)
            {
                lines.Add(FormatLevelRow(OrderedLevelNames[i]));
            }

            lines.Add(new string('-', Width));
            lines.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus(
                "ATTEMPTS",
                attempts.ToString(),
                Width));
            lines.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus(
                "SCENES",
                $"{scenesDone}/{scenesTotal}",
                Width));

            return ComponentDiagnosticLogFormatter.FitToScreen(lines, Width, TotalLines);
        }

        private static string FormatTableHeader()
        {
            string name = "LEVEL".PadRight(NameWidth);
            string blue = "BLUE".PadRight(StatWidth);
            string red = "RED".PadRight(StatWidth);
            string core = name + blue + new string(' ', StatGap) + red;
            return CenterTableCore(core);
        }

        private static string FormatLevelRow(string levelName)
        {
            LevelPlayRecord record = ScoreManager.TryGetLevel(levelName);
            string blue = FormatPlayerCell(record != null ? record.Blue : default, hasRecord: record != null);
            string red = FormatPlayerCell(record != null ? record.Red : default, hasRecord: record != null);
            string name = Truncate(levelName, NameWidth).PadRight(NameWidth);
            string core = name + blue + new string(' ', StatGap) + red;
            return CenterTableCore(core);
        }

        private static string FormatPlayerCell(PlayerPlayStats stats, bool hasRecord)
        {
            if (!hasRecord || (stats.Attempts <= 0 && stats.PlaySeconds <= 0f && !stats.Solved))
            {
                return "00 / --:--";
            }

            string time = ScoreManager.FormatTime(stats.PlaySeconds);
            return $"{ClampAttempts(stats.Attempts):00} / {time}";
        }

        private static int ClampAttempts(int attempts)
        {
            if (attempts < 0)
            {
                return 0;
            }

            if (attempts > 99)
            {
                return 99;
            }

            return attempts;
        }

        private static string CenterTableCore(string core)
        {
            if (core.Length > NameWidth + StatWidth + StatGap + StatWidth)
            {
                core = core.Substring(0, NameWidth + StatWidth + StatGap + StatWidth);
            }
            else if (core.Length < NameWidth + StatWidth + StatGap + StatWidth)
            {
                core = core.PadRight(NameWidth + StatWidth + StatGap + StatWidth);
            }

            return new string(' ', TableLeftMargin) + core + new string(' ', TableRightMargin);
        }

        private static string Truncate(string value, int maxLength)
        {
            value = value ?? string.Empty;
            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }
    }
}
