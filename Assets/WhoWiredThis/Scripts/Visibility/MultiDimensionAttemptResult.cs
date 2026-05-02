using WhoWiredThis.Enums;

namespace WhoWiredThis.Visibility
{
    /// <summary>
    /// Payload emitted after a combination check attempt (success or failure). No private per-slot diagnostic strings.
    /// </summary>
    public class MultiDimensionAttemptResult
    {
        public AllowedPlayerTag Actor;
        public string ActorLabel;
        public int[] SubmittedIndices;
        public bool IsSolved;
        public string PublicStatus;
        public int? PhaseNumber;
        public string PhaseLabel;
    }
}
