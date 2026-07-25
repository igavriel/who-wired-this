using System.Collections.Generic;
using WhoWiredThis.Enums;

namespace WhoWiredThis.Puzzles.Common
{
    /// <summary>
    /// Fixed 40×12 operation guides for Rules-panel diagnostic displays (operator vs reader roles).
    /// </summary>
    public static class PanelOperationGuideFormatter
    {
        public const int Width = 40;
        public const int GuideLines = 12;

        private static readonly string Separator = new string('-', Width);

        public static string BuildOperatorGuide(AllowedPlayerTag player)
        {
            bool isBlue = player != AllowedPlayerTag.Player_B;
            string header = isBlue ? "OPERATOR GUIDE - BLUE" : "OPERATOR GUIDE - RED";

            var lines = new List<string>(GuideLines)
            {
                ComponentDiagnosticLogFormatter.PadRight(header, Width),
                Separator
            };

            if (isBlue)
            {
                lines.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus("SELECT", "A / D", Width));
                lines.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus("ACTIVATE", "W / S OR LEFT CTRL", Width));
            }
            else
            {
                lines.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus("SELECT", "LEFT / RIGHT ARROWS", Width));
                lines.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus("ACTIVATE", "UP / DOWN OR RIGHT CTRL", Width));
            }

            lines.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus("SUBMIT", "SELECT SUBMIT, ACTIVATE", Width));
            lines.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus("TALK", "BEFORE EACH TEST", Width));
            lines.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus("CHECK", "HISTORY AFTER SEND", Width));
            lines.Add(Separator);
            lines.Add(ComponentDiagnosticLogFormatter.PadRight("TELL PARTNER EACH SETTING.", Width));
            lines.Add(ComponentDiagnosticLogFormatter.PadRight("PARTNER READS THEIR MONITOR.", Width));
            lines.Add(Separator);
            lines.Add(ComponentDiagnosticLogFormatter.FormatLabelStatus("STATUS", "READY", Width));

            return ComponentDiagnosticLogFormatter.FitToScreen(lines, Width, GuideLines);
        }

        public static string BuildReaderGuide(AllowedPlayerTag player)
        {
            bool isBlue = player != AllowedPlayerTag.Player_B;
            string header = isBlue ? "READER GUIDE - BLUE" : "READER GUIDE - RED";

            var lines = new List<string>(GuideLines)
            {
                Separator,
                ComponentDiagnosticLogFormatter.PadRight(header, Width),
                Separator,
                ComponentDiagnosticLogFormatter.FormatLabelStatus("WAIT", "FOR PARTNER SUBMIT", Width),
                ComponentDiagnosticLogFormatter.FormatLabelStatus("READ", "DIAGNOSTIC OUT LOUD", Width),
                ComponentDiagnosticLogFormatter.FormatLabelStatus("REPEAT", "KEY LINES TO PARTNER", Width),
                Separator,
                ComponentDiagnosticLogFormatter.PadRight("YOU CANNOT CHANGE CONTROLS.", Width),
                ComponentDiagnosticLogFormatter.PadRight("HELP OPERATOR FIX MISTAKES.", Width),
                Separator,
                ComponentDiagnosticLogFormatter.FormatLabelStatus("STATUS", "AWAITING SUBMIT", Width),
                Separator
            };

            return ComponentDiagnosticLogFormatter.FitToScreen(lines, Width, GuideLines);
        }
    }
}
