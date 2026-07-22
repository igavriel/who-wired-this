using System.Collections.Generic;
using System.Text;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Pure formatter for the Signal puzzle diagnostic body (fixed 40x12, same contract as
    /// <see cref="ComponentDiagnosticLogFormatter"/>). Receives already-evaluated values and
    /// returns the final string — no UI references, no correctness lookups.
    /// Direction mapping assumes MIN(0)..MAX(4) control indices: submitted below target = LOW.
    /// </summary>
    public static class SignalDiagnosticFormatter
    {
        public const string StatusOk = "OK";
        public const string StatusABitLow = "A BIT LOW";
        public const string StatusABitHigh = "A BIT HIGH";
        public const string StatusTooLow = "TOO LOW";
        public const string StatusTooHigh = "TOO HIGH";
        public const string StatusIncorrect = "INCORRECT";

        /// <summary>Ordered status text for MIN..MAX knobs (submitted below correct reads LOW).</summary>
        public static string ResolveOrderedStatus(ComponentSlotDiagnosticStatus status)
        {
            switch (status)
            {
                case ComponentSlotDiagnosticStatus.Correct:
                    return StatusOk;
                case ComponentSlotDiagnosticStatus.CloseTooLow:
                    return StatusABitLow;
                case ComponentSlotDiagnosticStatus.CloseTooHigh:
                    return StatusABitHigh;
                case ComponentSlotDiagnosticStatus.TooLow:
                case ComponentSlotDiagnosticStatus.FarTooLow:
                    return StatusTooLow;
                case ComponentSlotDiagnosticStatus.TooHigh:
                case ComponentSlotDiagnosticStatus.FarTooHigh:
                    return StatusTooHigh;
                default:
                    return StatusIncorrect;
            }
        }

        /// <summary>
        /// Builds the full 40x12 Signal diagnostic body:
        /// header, revision, status, 3-line target waveform ASCII, three metric rows, footer.
        /// The target waveform name is never printed — only its shape.
        /// </summary>
        public static string BuildSignalDiagnostic(
            ComponentSlotDiagnosticStatus rateStatus,
            ComponentSlotDiagnosticStatus powerStatus,
            bool waveformCorrect,
            int targetWaveformIndex,
            int revision,
            string headerLine1 = "OTHER PLAYER SUBMITS // YOU READ",
            string headerLine2 = "### MATCH THE TARGET SIGNAL ###",
            string logTitlePrefix = "SIGNAL LOG // REVISION",
            string statusLabel = "STATUS",
            string statusValue = "ANALYZING",
            string rateLabel = "SIGNAL RATE",
            string powerLabel = "SIGNAL POWER",
            string waveformLabel = "WAVEFORM MATCH",
            string footerLine = "TELL YOUR PARTNER WHAT YOU SEE",
            int width = ComponentDiagnosticLogFormatter.DefaultWidth,
            int totalLines = ComponentDiagnosticLogFormatter.DefaultTotalLines)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ComponentDiagnosticLogFormatter.PadRight(headerLine1, width));
            sb.AppendLine(ComponentDiagnosticLogFormatter.PadRight(headerLine2, width));
            sb.AppendLine(ComponentDiagnosticLogFormatter.PadRight($"{logTitlePrefix} {revision}", width));
            sb.AppendLine(ComponentDiagnosticLogFormatter.FormatLabelStatus(statusLabel, statusValue, width));

            string[] waveLines = SignalWaveformAsciiLibrary.GetLines(targetWaveformIndex);
            for (int i = 0; i < waveLines.Length; i++)
            {
                sb.AppendLine(waveLines[i] ?? string.Empty);
            }

            sb.AppendLine(ComponentDiagnosticLogFormatter.FormatLabelStatus(
                rateLabel, ResolveOrderedStatus(rateStatus), width));
            sb.AppendLine(ComponentDiagnosticLogFormatter.FormatLabelStatus(
                powerLabel, ResolveOrderedStatus(powerStatus), width));
            sb.AppendLine(ComponentDiagnosticLogFormatter.FormatLabelStatus(
                waveformLabel, waveformCorrect ? StatusOk : StatusIncorrect, width));
            sb.AppendLine(string.Empty);
            sb.AppendLine(ComponentDiagnosticLogFormatter.PadRight(footerLine, width));

            return ComponentDiagnosticLogFormatter.FitToScreen(sb.ToString().Split('\n'), width, totalLines);
        }
    }
}
