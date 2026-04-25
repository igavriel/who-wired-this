using System;

namespace WhoWiredThis.Interfaces
{
    public interface IPuzzleManager
    {
        bool IsSolved { get; }
        int Attempts { get; }
        int HintTriggerAttempt { get; }

        event Action OnSuccess;
        event Action<int> OnFailure;

        int ComputeCurrentScore();
        bool TryEngage();
    }
}
