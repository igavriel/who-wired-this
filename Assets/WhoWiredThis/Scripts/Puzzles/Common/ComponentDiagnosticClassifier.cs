namespace WhoWiredThis.Puzzles.Common
{
    public enum ComponentSlotDiagnosticStatus
    {
        Correct,
        TooLow,
        TooHigh,
        Mismatch
    }

    /// <summary>
    /// Shared per-slot diagnostic classification for pipe puzzles (text + result lights).
    /// Result light subject indices: 0=red, 1=orange, 2=green.
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

            if (submitted < correctIndex)
            {
                return ComponentSlotDiagnosticStatus.TooLow;
            }

            return ComponentSlotDiagnosticStatus.TooHigh;
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
                case ComponentSlotDiagnosticStatus.TooLow:
                    return ColorOrange;
                case ComponentSlotDiagnosticStatus.TooHigh:
                case ComponentSlotDiagnosticStatus.Mismatch:
                default:
                    return ColorRed;
            }
        }
    }
}
