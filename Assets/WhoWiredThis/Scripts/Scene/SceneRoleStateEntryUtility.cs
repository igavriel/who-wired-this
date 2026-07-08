using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Core;

namespace WhoWiredThis.Scenes
{
    /// <summary>
    /// Tracks the previous loaded scene name and applies <see cref="SceneRoleState"/> entry rules on load.
    /// </summary>
    public static class SceneRoleStateEntryUtility
    {
        private static string currentSceneName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoaded()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string loadedSceneName = scene.name;
            PlaytestSceneId? previousSceneId = TryResolveSceneId(currentSceneName, out PlaytestSceneId previousId)
                ? previousId
                : null;

            if (TryResolveSceneId(loadedSceneName, out PlaytestSceneId loadedSceneId))
            {
                SceneRoleState.ConfigureForSceneLoad(loadedSceneId, previousSceneId);
            }

            currentSceneName = loadedSceneName;
        }

        private static bool TryResolveSceneId(string sceneName, out PlaytestSceneId id)
        {
            id = PlaytestSceneId.None;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            if (string.Equals(sceneName, "StartScene", StringComparison.Ordinal))
            {
                id = PlaytestSceneId.StartScene;
                return true;
            }

            if (string.Equals(sceneName, "CutScene-Start-Tutorial", StringComparison.Ordinal))
            {
                id = PlaytestSceneId.CutSceneStartTutorial;
                return true;
            }

            if (string.Equals(sceneName, "Tutorial", StringComparison.Ordinal))
            {
                id = PlaytestSceneId.Tutorial;
                return true;
            }

            if (string.Equals(sceneName, "CutScene-Tutorial-Swap", StringComparison.Ordinal))
            {
                id = PlaytestSceneId.CutSceneTutorialSwap;
                return true;
            }

            if (string.Equals(sceneName, "CutScene-Tutorial-Pipe", StringComparison.Ordinal))
            {
                id = PlaytestSceneId.CutSceneTutorialPipe;
                return true;
            }

            if (string.Equals(sceneName, "Puzzle Pipes", StringComparison.Ordinal))
            {
                id = PlaytestSceneId.PuzzlePipes;
                return true;
            }

            if (string.Equals(sceneName, "CutScene-Pipe-Swap", StringComparison.Ordinal))
            {
                id = PlaytestSceneId.CutScenePipeSwap;
                return true;
            }

            if (string.Equals(sceneName, "CutScene-Pipe-Signal", StringComparison.Ordinal))
            {
                id = PlaytestSceneId.CutScenePipeSignal;
                return true;
            }

            if (string.Equals(sceneName, "Puzzle Signal", StringComparison.Ordinal))
            {
                id = PlaytestSceneId.PuzzleSignal;
                return true;
            }

            if (string.Equals(sceneName, "GameOverScene", StringComparison.Ordinal))
            {
                id = PlaytestSceneId.GameOverScene;
                return true;
            }

            return false;
        }
    }
}
