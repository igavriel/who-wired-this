using System.Collections.Generic;
using System.Text;

namespace WhoWiredThis.Puzzles.Diagnostics
{
    /// <summary>Pure builder: (solution, attempt, seed) -> fixed 40x12 diagnostic string. No Unity deps.</summary>
    public sealed class TutorialDiagnosticReport
    {
        public const int Width = 40;
        public const int TotalLines = 12;
        private const int MatrixLines = 6;

        private readonly TutorialDiagnosticStrings strings;

        public TutorialDiagnosticReport(TutorialDiagnosticStrings copy)
        {
            strings = copy ?? new TutorialDiagnosticStrings();
        }

        public string Build(IReadOnlyList<int> solution, IReadOnlyList<int> attempt, int seed)
        {
            SymbolMatch[] matches = MastermindAnalyzer.EvaluateAll(solution, attempt);
            bool win = matches.Length == 2
                       && matches[0] == SymbolMatch.Exact
                       && matches[1] == SymbolMatch.Exact;

            var lines = new List<string>(TotalLines);
            if (win)
            {
                AppendWrapped(lines, strings.WinMessage);
            }
            else
            {
                AppendWrapped(lines, strings.InfoMessage);
                while (lines.Count < 3)
                {
                    lines.Add(string.Empty);
                }

                AppendWrapped(lines, string.Format(strings.ResultReadout, seed & 0xFFF));
                while (lines.Count < 5)
                {
                    lines.Add(string.Empty);
                }

                lines.Add(string.Empty);

                int sym1 = attempt != null && attempt.Count > 0 ? attempt[0] : 0;
                int sym2 = attempt != null && attempt.Count > 1 ? attempt[1] : 0;
                var rng = new System.Random(seed);
                for (int i = 0; i < MatrixLines; i++)
                {
                    lines.Add(MatrixLineEncoder.EncodeLine(
                        matches.Length > 0 ? matches[0] : SymbolMatch.Absent,
                        matches.Length > 1 ? matches[1] : SymbolMatch.Absent,
                        sym1,
                        sym2,
                        rng));
                }
            }

            return FitToScreen(lines);
        }

        private static void AppendWrapped(List<string> lines, string text)
        {
            foreach (string raw in (text ?? string.Empty).Split('\n'))
            {
                lines.Add(raw.Length <= Width ? raw : raw.Substring(0, Width));
            }
        }

        private static string FitToScreen(List<string> lines)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < TotalLines; i++)
            {
                string line = i < lines.Count ? lines[i] : string.Empty;
                if (line.Length > Width)
                {
                    line = line.Substring(0, Width);
                }

                sb.Append(line);
                if (i < TotalLines - 1)
                {
                    sb.Append('\n');
                }
            }

            return sb.ToString();
        }
    }
}
