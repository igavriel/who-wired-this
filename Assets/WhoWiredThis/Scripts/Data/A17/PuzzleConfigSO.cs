using UnityEngine;

namespace WhoWiredThis.Data.A17
{
    [CreateAssetMenu(menuName = "WhoWiredThis/A17/Puzzle Config", fileName = "A17_PuzzleConfig")]
    public class PuzzleConfigSO : ScriptableObject
    {
        [Header("Solution")]
        [Tooltip("Five values: -1 = Negative, 0 = Off, 1 = Positive. Must include at least one of each.")]
        public PolarityState[] solution = {
            PolarityState.Negative,
            PolarityState.Off,
            PolarityState.Positive,
            PolarityState.Off,
            PolarityState.Positive
        };

        [Header("Scoring")]
        public int startScore = 100;
        public int penaltyFreeAttempts = 5;
        public int penaltyPerAttempt = 10;
        public int minScore = 50;
        public int hintTriggerAttempt = 5;
    }
}
