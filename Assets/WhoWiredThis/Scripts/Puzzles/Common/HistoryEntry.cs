using System;

namespace WhoWiredThis.Puzzles.Common
{
    [Serializable]
    public class HistoryEntry
    {
        public int attemptNumber;
        public string actor;
        public string inputText;
        public string publicStatus;
    }
}
