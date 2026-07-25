using UnityEngine;

namespace WhoWiredThis.Core
{
    /// <summary>
    /// Computes the combined 0–100 team score from per-level time bands and total run attempts.
    /// Tunables live in <see cref="GameConfigSO"/>.
    /// </summary>
    public static class PlaytestTeamScoreCalculator
    {
        private static readonly string[] OrderedLevelNames =
        {
            "Tutorial",
            "Puzzle Pipes",
            "Puzzle Signal",
        };

        private static GameConfigSO Config => GameConfigProvider.Active;

        public static int CalculateTeamScore()
        {
            float avgTimeScore = CalculateAverageTimeScore();
            float attemptScore = CalculateAttemptScore();
            int combined = Mathf.RoundToInt(0.5f * avgTimeScore + 0.5f * attemptScore);
            return Mathf.Clamp(combined, 0, 100);
        }

        public static float CalculateAverageTimeScore()
        {
            float sum = 0f;
            for (int i = 0; i < OrderedLevelNames.Length; i++)
            {
                sum += CalculateLevelTimeScore(OrderedLevelNames[i]);
            }

            return sum / OrderedLevelNames.Length;
        }

        public static float CalculateAttemptScore()
        {
            int totalAttempts = ScoreManager.GetTotalAttemptsAcrossLevels();
            return Mathf.Max(0f, 100f - Config.AttemptPenalty * totalAttempts);
        }

        public static float CalculateLevelTimeScore(string levelName)
        {
            LevelPlayRecord record = ScoreManager.TryGetLevel(levelName);
            if (record == null)
            {
                return 0f;
            }

            float sceneSeconds = Mathf.Clamp(record.SceneTotalSeconds, 0f, Config.SceneTimeCapSeconds);
            return ScoreTimeSeconds(sceneSeconds);
        }

        public static float ScoreTimeSeconds(float sceneSeconds)
        {
            float expertSeconds = Config.ExpertSeconds;
            float newPlayerSeconds = Config.NewPlayerSeconds;
            float sceneTimeCapSeconds = Config.SceneTimeCapSeconds;
            float t = Mathf.Clamp(sceneSeconds, 0f, sceneTimeCapSeconds);

            if (t <= expertSeconds)
            {
                return 100f;
            }

            if (t <= newPlayerSeconds)
            {
                float ratio = (t - expertSeconds) / (newPlayerSeconds - expertSeconds);
                return Mathf.Lerp(100f, 50f, ratio);
            }

            if (t <= sceneTimeCapSeconds)
            {
                float ratio = (t - newPlayerSeconds) / (sceneTimeCapSeconds - newPlayerSeconds);
                return Mathf.Lerp(50f, 0f, ratio);
            }

            return 0f;
        }
    }
}
