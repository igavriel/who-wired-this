namespace WhoWiredThis.Puzzles.Common
{
    public enum ComponentSlotDiagnosticStatus
    {
        Correct,
        /// <summary>Legacy: submitted below correct (prefer Close/FarTooLow).</summary>
        TooLow,
        /// <summary>Legacy: submitted above correct (prefer Close/FarTooHigh).</summary>
        TooHigh,
        Mismatch,
        CloseTooLow,
        CloseTooHigh,
        FarTooLow,
        FarTooHigh
    }

    /// <summary>
    /// Shared per-slot diagnostic classification for pipe puzzles (text + result lights).
    /// Result light subject indices: 0=red, 1=orange, 2=green.
    /// Ordered proximity: |delta|==1 close, |delta|&gt;=2 far.
    /// </summary>
    public static class ComponentDiagnosticClassifier
    {
        public const int ColorRed = 0;
        public const int ColorOrange = 1;
        public const int ColorGreen = 2;

        public static ComponentSlotDiagnosticStatus Classify(
            ComponentDiagnosticType diagnosticType,
            int submitted,
            int correctIndex)
        {
            if (submitted == correctIndex)
            {
                return ComponentSlotDiagnosticStatus.Correct;
            }

            if (diagnosticType == ComponentDiagnosticType.Categorical)
            {
                return ComponentSlotDiagnosticStatus.Mismatch;
            }

            int delta = submitted - correctIndex;
            int abs = delta < 0 ? -delta : delta;
            bool close = abs == 1;

            if (delta < 0)
            {
                return close
                    ? ComponentSlotDiagnosticStatus.CloseTooLow
                    : ComponentSlotDiagnosticStatus.FarTooLow;
            }

            return close
                ? ComponentSlotDiagnosticStatus.CloseTooHigh
                : ComponentSlotDiagnosticStatus.FarTooHigh;
        }

        public static int ResolveColorIndex(
            ComponentDiagnosticType diagnosticType,
            int submitted,
            int correctIndex)
        {
            switch (Classify(diagnosticType, submitted, correctIndex))
            {
                case ComponentSlotDiagnosticStatus.Correct:
                    return ColorGreen;
                case ComponentSlotDiagnosticStatus.CloseTooLow:
                case ComponentSlotDiagnosticStatus.CloseTooHigh:
                case ComponentSlotDiagnosticStatus.TooLow:
                    return ColorOrange;
                case ComponentSlotDiagnosticStatus.FarTooLow:
                case ComponentSlotDiagnosticStatus.FarTooHigh:
                case ComponentSlotDiagnosticStatus.TooHigh:
                case ComponentSlotDiagnosticStatus.Mismatch:
                default:
                    return ColorRed;
            }
        }
    }
}
