using System;
using UnityEngine;

namespace WhoWiredThis.Puzzles.Diagnostics
{
    /// <summary>Inspector-editable copy for the tutorial diagnostic (Blue/Red configurable per panel).</summary>
    [Serializable]
    public class TutorialDiagnosticStrings
    {
        [TextArea(3, 3)]
        public string InfoMessage =
            "RUN A TEST, THEN READ THE GRID.\n" +
            "EACH ROW IS THE SAME RESULT.\n" +
            "STEADY DIGITS ARE THE SIGNAL.";

        [TextArea(2, 2)]
        public string ResultReadout = "DIAGNOSTIC LOG  REV {0}\nSTATUS .... ANALYZING";

        [TextArea(4, 8)]
        public string WinMessage =
            "ALL CHANNELS LOCKED.\nCALIBRATION COMPLETE.";
    }
}
