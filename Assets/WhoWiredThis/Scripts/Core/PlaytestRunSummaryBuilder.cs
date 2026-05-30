using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Core
{
    public static class PlaytestRunSummaryBuilder
    {
        public static PlaytestRunSummaryData Build(bool abandoned)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            bool lastPuzzleCompleted = IsCurrentPuzzleSolved();

            return new PlaytestRunSummaryData
            {
                WasAbandoned = abandoned,
                LastPuzzleCompleted = lastPuzzleCompleted,
                LastPuzzleName = sceneName,
                RunTimeSeconds = PlaytestRunTotal.GetTotalSecondsIncludingCurrentSegment(),
                SceneTimerSeconds = TimerManager.Instance != null ? TimerManager.Instance.ElapsedSeconds : 0f,
                Score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0,
                CompletedPuzzleCount = PlaytestRunTotal.GetCompletedSceneCount(),
                AttemptCount = SharedHistorySO.GetMaxAttemptNumber()
            };
        }

        private static bool IsCurrentPuzzleSolved()
        {
            MultiDimensionPuzzleManager[] managers =
                Object.FindObjectsByType<MultiDimensionPuzzleManager>(FindObjectsSortMode.None);

            for (int i = 0; i < managers.Length; i++)
            {
                MultiDimensionPuzzleManager manager = managers[i];
                if (manager != null && manager.Solved)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
