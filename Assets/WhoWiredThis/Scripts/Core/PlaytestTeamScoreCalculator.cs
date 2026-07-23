using UnityEngine;

namespace WhoWiredThis.Core
{
    /// <summary>
    /// Computes the combined 0–100 team score from per-level time bands and total run attempts.
    /// </summary>
    public static class PlaytestTeamScoreCalculator
    {
        public const float ExpertSeconds = 120f;
        public const float NewPlayerSeconds = 300f;
        public const float SceneTimeCapSeconds = 480f;
        public const int AttemptPenalty = 2;

        private static readonly string[] OrderedLevelNames =
        {
            "Tutorial",
            "Puzzle Pipes",
            "Puzzle Signal",
        };

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
            return Mathf.Max(0f, 100f - AttemptPenalty * totalAttempts);
        }

        public static float CalculateLevelTimeScore(string levelName)
        {
            LevelPlayRecord record = ScoreManager.TryGetLevel(levelName);
            if (record == null)
            {
                return 0f;
            }

            float sceneSeconds = Mathf.Clamp(record.SceneTotalSeconds, 0f, SceneTimeCapSeconds);
            return ScoreTimeSeconds(sceneSeconds);
        }

        public static float ScoreTimeSeconds(float sceneSeconds)
        {
            float t = Mathf.Clamp(sceneSeconds, 0f, SceneTimeCapSeconds);

            if (t <= ExpertSeconds)
            {
                return 100f;
            }

            if (t <= NewPlayerSeconds)
            {
                float ratio = (t - ExpertSeconds) / (NewPlayerSeconds - ExpertSeconds);
                return Mathf.Lerp(100f, 50f, ratio);
            }

            if (t <= SceneTimeCapSeconds)
            {
                float ratio = (t - NewPlayerSeconds) / (SceneTimeCapSeconds - NewPlayerSeconds);
                return Mathf.Lerp(50f, 0f, ratio);
            }

            return 0f;
        }
    }
}
