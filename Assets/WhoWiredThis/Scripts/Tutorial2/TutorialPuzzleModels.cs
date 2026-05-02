using System;
using WhoWiredThis.Tutorial;

namespace WhoWiredThis.Tutorial2
{
    [Serializable]
    public struct PuzzleFeedback
    {
        public int Metric1Total;
        public int Metric2Aligned;
        public bool IsSuccess;
        public string Message;
    }

    [Serializable]
    public struct PuzzleAttemptRecord
    {
        public int PhaseNumber;
        public TutorialPlayerSlot Actor;
        public string GuessText;
        public string FeedbackText;
        public string Note;
    }
}
