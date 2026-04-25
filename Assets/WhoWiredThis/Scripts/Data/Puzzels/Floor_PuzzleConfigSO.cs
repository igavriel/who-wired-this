using System;
using UnityEngine;
using WhoWiredThis.Enums;

namespace WhoWiredThis.Data.FloorColor
{
    [CreateAssetMenu(menuName = "WhoWiredThis/Floor Puzzle Config", fileName = "Floor_PuzzleConfig")]
    public class Floor_PuzzleConfigSO : ScriptableObject
    {
        [Serializable]
        public class PolaritySolutionRow
        {
            public PolarityState[] values = Array.Empty<PolarityState>();
        }

        [Header("Solution Matrix")]
        [Tooltip("Ordered rows of expected polarity states.")]
        public PolaritySolutionRow[] solutionRows = Array.Empty<PolaritySolutionRow>();

        [Header("Scoring")]
        public int startScore = 100;
        public int penaltyFreeAttempts = 5;
        public int penaltyPerAttempt = 10;
        public int minScore = 50;
        public int hintTriggerAttempt = 5;
    }
}
