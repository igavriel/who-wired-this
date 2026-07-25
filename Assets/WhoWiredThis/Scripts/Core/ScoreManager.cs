using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhoWiredThis.Core
{
    /// <summary>
    /// Static run-scoped scoreboard: per-player retries/time per level, scene totals,
    /// and run lifecycle. Survives scene loads. Replaces the former MonoBehaviour score
    /// and <c>PlaytestRunTotal</c> timing store.
    /// </summary>
    public static class ScoreManager
    {
        private static readonly List<LevelPlayRecord> LevelRecords = new List<LevelPlayRecord>();
        private static readonly HashSet<string> CompletedScenes = new HashSet<string>();

        private static bool hasActiveRun;
        private static float runStartRealtime;
        private static float segmentStartRealtime;
        private static float totalSeconds;
        private static CurrentPlayStatus currentStatus;

        public static bool HasActiveRun => hasActiveRun;

        public static CurrentPlayStatus CurrentStatus => currentStatus;

        public static IReadOnlyList<LevelPlayRecord> Levels => LevelRecords;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnDomainReload()
        {
            ResetRunInternal(log: false);
        }

        public static void BeginRun()
        {
            ResetRunInternal(log: false);
            hasActiveRun = true;
            runStartRealtime = Time.realtimeSinceStartup;
            segmentStartRealtime = runStartRealtime;
            Debug.Log("[ScoreManager] Run started. Totals reset.");
        }

        public static void ResetRun()
        {
            ResetRunInternal(log: true);
        }

        public static void CompleteCurrentScene(string sceneName)
        {
            if (!hasActiveRun)
            {
                Debug.LogWarning($"[ScoreManager] Ignored scene completion '{sceneName}' because run is not active.");
                return;
            }

            if (CompletedScenes.Contains(sceneName))
            {
                Debug.Log($"[ScoreManager] Scene '{sceneName}' already counted; skipping duplicate.");
                return;
            }

            float now = Time.realtimeSinceStartup;
            float sceneSeconds = Mathf.Max(0f, now - segmentStartRealtime);
            totalSeconds += sceneSeconds;
            segmentStartRealtime = now;
            CompletedScenes.Add(sceneName);

            if (IsGameplayLevel(sceneName))
            {
                LevelPlayRecord record = GetOrCreateLevel(sceneName);
                float playSum = record.Blue.PlaySeconds + record.Red.PlaySeconds;
                // Prefer merged per-player play time for the level line; fall back to wall segment.
                record.SceneTotalSeconds = Mathf.Max(record.SceneTotalSeconds, Mathf.Max(playSum, sceneSeconds));
                record.Completed = true;
            }

            Debug.Log(
                $"[ScoreManager] Added scene '{sceneName}' time {sceneSeconds:F2}s. " +
                $"Total {totalSeconds:F2}s ({FormatTime(totalSeconds)}).");
        }

        public static float GetTotalSeconds()
        {
            return Mathf.Max(0f, totalSeconds);
        }

        public static float GetTotalSecondsIncludingCurrentSegment()
        {
            if (!hasActiveRun)
            {
                return GetTotalSeconds();
            }

            float currentSegment = Mathf.Max(0f, Time.realtimeSinceStartup - segmentStartRealtime);
            return totalSeconds + currentSegment;
        }

        public static int GetCompletedSceneCount()
        {
            return CompletedScenes.Count;
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = total / 60;
            int remaining = total % 60;
            return $"{minutes:00}:{remaining:00}";
        }

        public static bool IsGameplayLevel(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return false;
            }

            GameConfigSO config = GameConfigProvider.Active;
            if (config != null)
            {
                if (MatchesConfiguredScene(config, PlaytestSceneId.Tutorial, sceneName) ||
                    MatchesConfiguredScene(config, PlaytestSceneId.PuzzlePipes, sceneName) ||
                    MatchesConfiguredScene(config, PlaytestSceneId.PuzzleSignal, sceneName))
                {
                    return true;
                }
            }

            // Fallback when GameConfig is unavailable (domain reload / missing provider).
            return string.Equals(sceneName, "Tutorial", StringComparison.Ordinal) ||
                   string.Equals(sceneName, "Puzzle Pipes", StringComparison.Ordinal) ||
                   string.Equals(sceneName, "Puzzle Signal", StringComparison.Ordinal);
        }

        private static bool MatchesConfiguredScene(
            GameConfigSO config,
            PlaytestSceneId id,
            string sceneName)
        {
            return config.TryGetSceneName(id, out string configuredName) &&
                   string.Equals(sceneName, configuredName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Pushes live metrics from a scene tracker. Merges with any existing level record so a
        /// role-swap reload (fresh tracker at zero) cannot wipe earlier Blue/Red attempts or times.
        /// </summary>
        public static void UpdateLiveLevel(
            string sceneName,
            int blueAttempts,
            int blueRetries,
            float bluePlaySeconds,
            bool blueSolved,
            int redAttempts,
            int redRetries,
            float redPlaySeconds,
            bool redSolved,
            float sceneTotalSeconds,
            string activePlayerLabel,
            bool levelComplete)
        {
            if (string.IsNullOrEmpty(sceneName) || !IsGameplayLevel(sceneName))
            {
                return;
            }

            LevelPlayRecord record = GetOrCreateLevel(sceneName);
            record.Blue = MergePlayerStats(
                record.Blue,
                blueAttempts,
                blueRetries,
                bluePlaySeconds,
                blueSolved);
            record.Red = MergePlayerStats(
                record.Red,
                redAttempts,
                redRetries,
                redPlaySeconds,
                redSolved);

            float playSum = record.Blue.PlaySeconds + record.Red.PlaySeconds;
            record.SceneTotalSeconds = Mathf.Max(
                record.SceneTotalSeconds,
                Mathf.Max(sceneTotalSeconds, playSum));

            if (levelComplete)
            {
                record.Completed = true;
            }

            currentStatus = new CurrentPlayStatus
            {
                LevelName = sceneName,
                ActivePlayerLabel = activePlayerLabel ?? string.Empty,
                BlueRetries = record.Blue.Retries,
                RedRetries = record.Red.Retries,
                BlueTimeSeconds = record.Blue.PlaySeconds,
                RedTimeSeconds = record.Red.PlaySeconds,
                SceneTimeSeconds = record.SceneTotalSeconds,
                HasActiveLevel = true
            };
        }

        public static LevelPlayRecord TryGetLevel(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return null;
            }

            for (int i = 0; i < LevelRecords.Count; i++)
            {
                if (string.Equals(LevelRecords[i].SceneName, sceneName, StringComparison.Ordinal))
                {
                    return LevelRecords[i];
                }
            }

            return null;
        }

        private static PlayerPlayStats MergePlayerStats(
            PlayerPlayStats existing,
            int attempts,
            int retries,
            float playSeconds,
            bool solved)
        {
            return new PlayerPlayStats
            {
                Attempts = Mathf.Max(existing.Attempts, attempts),
                Retries = Mathf.Max(existing.Retries, retries),
                PlaySeconds = Mathf.Max(existing.PlaySeconds, playSeconds),
                Solved = existing.Solved || solved
            };
        }

        public static void ClearCurrentStatus()
        {
            currentStatus = default;
        }

        public static int GetTotalAttemptsAcrossLevels()
        {
            int total = 0;
            for (int i = 0; i < LevelRecords.Count; i++)
            {
                LevelPlayRecord record = LevelRecords[i];
                total += record.Blue.Attempts + record.Red.Attempts;
            }

            return total;
        }

        public static int GetTotalRetriesAcrossLevels()
        {
            int total = 0;
            for (int i = 0; i < LevelRecords.Count; i++)
            {
                LevelPlayRecord record = LevelRecords[i];
                total += record.Blue.Retries + record.Red.Retries;
            }

            return total;
        }

        private static LevelPlayRecord GetOrCreateLevel(string sceneName)
        {
            LevelPlayRecord existing = TryGetLevel(sceneName);
            if (existing != null)
            {
                return existing;
            }

            var created = new LevelPlayRecord { SceneName = sceneName };
            LevelRecords.Add(created);
            return created;
        }

        private static void ResetRunInternal(bool log)
        {
            hasActiveRun = false;
            totalSeconds = 0f;
            CompletedScenes.Clear();
            LevelRecords.Clear();
            runStartRealtime = 0f;
            segmentStartRealtime = 0f;
            currentStatus = default;

            if (log)
            {
                Debug.Log("[ScoreManager] Run reset.");
            }
        }
    }
}
