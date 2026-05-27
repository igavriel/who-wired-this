using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhoWiredThis.Core
{
    /// <summary>
    /// Validates scene names against Editor Build Settings before runtime loads.
    /// </summary>
    public static class PlaytestSceneLoadUtility
    {
        public static bool TryGetBuildIndex(string sceneName, out int buildIndex, out string error)
        {
            buildIndex = -1;
            error = null;

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                error = "Scene name is empty.";
                return false;
            }

            int sceneCount = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string nameFromPath = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileNameWithoutExtension(path);
                if (!string.Equals(nameFromPath, sceneName, StringComparison.Ordinal))
                {
                    continue;
                }

                buildIndex = i;
                return true;
            }

            error = $"Scene '{sceneName}' is not in Build Settings (checked {sceneCount} enabled scenes).";
            return false;
        }

        public static bool CanStreamScene(string sceneName)
        {
            return TryGetBuildIndex(sceneName, out _, out _) &&
                   Application.CanStreamedLevelBeLoaded(sceneName);
        }

        public static bool TryLoadSingleScene(string sceneName, out string error)
        {
            if (!TryGetBuildIndex(sceneName, out int buildIndex, out error))
            {
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                error = $"Scene '{sceneName}' cannot be streamed (build index {buildIndex}). Rebuild the player and verify Build Settings.";
                return false;
            }

            Debug.Log($"[PlaytestSceneLoadUtility] Loading scene '{sceneName}' (build index {buildIndex}).");
            SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
            return true;
        }
    }
}
