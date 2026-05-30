using System.Collections.Generic;
using UnityEngine;

namespace WhoWiredThis.Core
{
    public static class PlaytestRunTotal
    {
        private static readonly HashSet<string> CompletedScenes = new HashSet<string>();
        private static bool hasActiveRun;
        private static float runStartRealtime;
        private static float segmentStartRealtime;
        private static float totalSeconds;

        public static void BeginRun()
        {
            hasActiveRun = true;
            totalSeconds = 0f;
            CompletedScenes.Clear();
            runStartRealtime = Time.realtimeSinceStartup;
            segmentStartRealtime = runStartRealtime;
            Debug.Log("[PlaytestRunTotal] Run started. Total reset to 00:00.");
        }

        public static void ResetRun()
        {
            hasActiveRun = false;
            totalSeconds = 0f;
            CompletedScenes.Clear();
            runStartRealtime = 0f;
            segmentStartRealtime = 0f;
            Debug.Log("[PlaytestRunTotal] Run reset.");
        }

        public static void CompleteCurrentScene(string sceneName)
        {
            if (!hasActiveRun)
            {
                Debug.LogWarning($"[PlaytestRunTotal] Ignored scene completion '{sceneName}' because run is not active.");
                return;
            }

            if (CompletedScenes.Contains(sceneName))
            {
                Debug.Log($"[PlaytestRunTotal] Scene '{sceneName}' already counted; skipping duplicate.");
                return;
            }

            float now = Time.realtimeSinceStartup;
            float sceneSeconds = Mathf.Max(0f, now - segmentStartRealtime);
            totalSeconds += sceneSeconds;
            segmentStartRealtime = now;
            CompletedScenes.Add(sceneName);

            Debug.Log($"[PlaytestRunTotal] Added scene '{sceneName}' time {sceneSeconds:F2}s. Total {totalSeconds:F2}s ({FormatTime(totalSeconds)}).");
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

        public static bool HasActiveRun => hasActiveRun;

        public static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = total / 60;
            int remaining = total % 60;
            return $"{minutes:00}:{remaining:00}";
        }
    }
}
