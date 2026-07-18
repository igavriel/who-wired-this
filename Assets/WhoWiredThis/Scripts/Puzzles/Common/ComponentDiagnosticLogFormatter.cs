using System;
using System.Collections.Generic;
using System.Text;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Builds a fixed-width diagnostic log body (default 40×12) with computed dot leaders.
    /// Pure C# — no UnityEngine dependency.
    /// </summary>
    public static class ComponentDiagnosticLogFormatter
    {
        public const int DefaultWidth = 40;
        public const int DefaultTotalLines = 12;

        public static string FormatLabelStatus(string label, string status, int width = DefaultWidth)
        {
            label = label ?? string.Empty;
            status = status ?? string.Empty;
            if (width < 2)
            {
                width = DefaultWidth;
            }

            if (label.Length + status.Length >= width)
            {
                int maxStatus = Math.Max(1, width - label.Length - 1);
                if (status.Length > maxStatus)
                {
                    status = status.Substring(0, maxStatus);
                }

                if (label.Length + status.Length >= width)
                {
                    int maxLabel = Math.Max(1, width - status.Length - 1);
                    if (label.Length > maxLabel)
                    {
                        label = label.Substring(0, maxLabel);
                    }
                }
            }

            int gap = width - label.Length - status.Length;
            if (gap < 1)
            {
                gap = 1;
            }

            return label + new string('.', gap) + status;
        }

        public static string PadRight(string text, int width = DefaultWidth)
        {
            text = text ?? string.Empty;
            if (width < 1)
            {
                width = DefaultWidth;
            }

            if (text.Length >= width)
            {
                return text.Substring(0, width);
            }

            return text + new string('.', width - text.Length);
        }

        /// <summary>
        /// Assembles the locked 12-line log:
        /// header1, header2, revision, blank, status, blank, rows..., blank, footer, blank.
        /// </summary>
        public static string BuildLogBody(
            string headerLine1,
            string headerLine2,
            string logTitlePrefix,
            int revision,
            string statusLabel,
            string statusValue,
            IReadOnlyList<string> componentRows,
            string footerLine,
            int width = DefaultWidth,
            int totalLines = DefaultTotalLines)
        {
            if (width < 2)
            {
                width = DefaultWidth;
            }

            if (totalLines < 1)
            {
                totalLines = DefaultTotalLines;
            }

            var lines = new List<string>(totalLines)
            {
                PadRight(headerLine1, width),
                PadRight(headerLine2, width),
                PadRight($"{logTitlePrefix} {revision}", width),
                string.Empty,
                FormatLabelStatus(statusLabel, statusValue, width),
                string.Empty
            };

            if (componentRows != null)
            {
                for (int i = 0; i < componentRows.Count; i++)
                {
                    lines.Add(componentRows[i] ?? string.Empty);
                }
            }

            lines.Add(string.Empty);
            lines.Add(PadRight(footerLine, width));

            return FitToScreen(lines, width, totalLines);
        }

        public static string FitToScreen(IList<string> lines, int width, int totalLines)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < totalLines; i++)
            {
                string line = i < lines.Count ? (lines[i] ?? string.Empty) : string.Empty;
                if (line.Length > width)
                {
                    line = line.Substring(0, width);
                }

                sb.Append(line);
                if (i < totalLines - 1)
                {
                    sb.Append('\n');
                }
            }

            return sb.ToString();
        }
    }
}
