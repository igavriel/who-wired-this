using UnityEngine;
using UnityEngine.SceneManagement;
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
                RunTimeSeconds = ScoreManager.GetTotalSecondsIncludingCurrentSegment(),
                SceneTimerSeconds = TimerManager.Instance != null ? TimerManager.Instance.ElapsedSeconds : 0f,
                CompletedPuzzleCount = ScoreManager.GetCompletedSceneCount(),
                AttemptCount = ScoreManager.GetTotalAttemptsAcrossLevels(),
                RetryCount = ScoreManager.GetTotalRetriesAcrossLevels()
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
