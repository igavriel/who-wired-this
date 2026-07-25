using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhoWiredThis.Core
{
    [CreateAssetMenu(
        fileName = "GameConfig",
        menuName = "Who Wired This/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Serializable]
        public class SceneEntry
        {
            [Tooltip("Stable scene id used by bootstrap and flow helpers.")]
            [SerializeField]
            private PlaytestSceneId id;

            [Tooltip("Exact Unity .unity file name (no path, no extension). Must match Build Settings — not a display label.")]
            [SerializeField]
            private string sceneName;

            public PlaytestSceneId Id => id;
            public string SceneName => sceneName;
            public string Label => sceneName;

            public SceneEntry()
            {
            }

            public SceneEntry(PlaytestSceneId id, string sceneName)
            {
                this.id = id;
                this.sceneName = sceneName;
            }
        }

        [Header("Team score")]
        [Tooltip("Time at or below this earns 100 time points (expert band).")]
        [SerializeField]
        private float expertSeconds = 120f;

        [Tooltip("Time at or below this lerps from 100 to 50 time points.")]
        [SerializeField]
        private float newPlayerSeconds = 300f;

        [Tooltip("Per-level countdown cap and time-score floor (0 points at this duration).")]
        [SerializeField]
        private float sceneTimeCapSeconds = 480f;

        [Tooltip("Points subtracted per attempt across the run.")]
        [SerializeField]
        private int attemptPenalty = 2;

        [Tooltip("Final seconds of the level countdown when HURRY UP! N is shown on the interact prompt.")]
        [SerializeField]
        private int hurryUpSeconds = 10;

        [Header("Scene flow")]
        [SerializeField]
        private SceneEntry[] sceneEntries = Array.Empty<SceneEntry>();

        [SerializeField]
        private PlaytestSceneId[] playtestChainOrder = Array.Empty<PlaytestSceneId>();

        public IReadOnlyList<SceneEntry> SceneEntries => sceneEntries;
        public IReadOnlyList<PlaytestSceneId> PlaytestChainOrder => playtestChainOrder;

        public float ExpertSeconds => expertSeconds;
        public float NewPlayerSeconds => newPlayerSeconds;
        public float SceneTimeCapSeconds => sceneTimeCapSeconds;
        public int AttemptPenalty => attemptPenalty;
        public int HurryUpSeconds => hurryUpSeconds;

        public bool TryGetSceneName(PlaytestSceneId id, out string sceneName)
        {
            sceneName = null;
            if (id == PlaytestSceneId.None)
            {
                return false;
            }

            for (int i = 0; i < sceneEntries.Length; i++)
            {
                SceneEntry entry = sceneEntries[i];
                if (entry != null && entry.Id == id && !string.IsNullOrWhiteSpace(entry.SceneName))
                {
                    sceneName = entry.SceneName;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetNext(PlaytestSceneId currentId, out PlaytestSceneId nextId)
        {
            nextId = PlaytestSceneId.None;
            if (currentId == PlaytestSceneId.None)
            {
                return false;
            }

            for (int i = 0; i < playtestChainOrder.Length; i++)
            {
                if (playtestChainOrder[i] != currentId)
                {
                    continue;
                }

                if (i + 1 >= playtestChainOrder.Length)
                {
                    return false;
                }

                nextId = playtestChainOrder[i + 1];
                return nextId != PlaytestSceneId.None;
            }

            return false;
        }

        public bool TryGetNextSceneName(PlaytestSceneId currentId, out string sceneName)
        {
            sceneName = null;
            if (!TryGetNext(currentId, out PlaytestSceneId nextId))
            {
                return false;
            }

            return TryGetSceneName(nextId, out sceneName);
        }

        public bool TryGetSceneIdForSceneName(string sceneName, out PlaytestSceneId id)
        {
            id = PlaytestSceneId.None;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            for (int i = 0; i < sceneEntries.Length; i++)
            {
                SceneEntry entry = sceneEntries[i];
                if (entry != null &&
                    string.Equals(entry.SceneName, sceneName, StringComparison.Ordinal))
                {
                    id = entry.Id;
                    return id != PlaytestSceneId.None;
                }
            }

            return false;
        }

        public void ResetScoreDefaults()
        {
            expertSeconds = 120f;
            newPlayerSeconds = 300f;
            sceneTimeCapSeconds = 480f;
            attemptPenalty = 2;
            hurryUpSeconds = 10;
        }

        public void SetDefaultsForCurrentChain()
        {
            sceneEntries = new[]
            {
                new SceneEntry(PlaytestSceneId.StartScene, "StartScene"),
                new SceneEntry(PlaytestSceneId.CutSceneStartTutorial, "CutScene-Start-Tutorial"),
                new SceneEntry(PlaytestSceneId.Tutorial, "Tutorial"),
                new SceneEntry(PlaytestSceneId.CutSceneTutorialSwap, "CutScene-Tutorial-Swap"),
                new SceneEntry(PlaytestSceneId.CutSceneTutorialPipe, "CutScene-Tutorial-Pipe"),
                new SceneEntry(PlaytestSceneId.PuzzlePipes, "Puzzle Pipes"),
                new SceneEntry(PlaytestSceneId.CutScenePipeSwap, "CutScene-Pipe-Swap"),
                new SceneEntry(PlaytestSceneId.CutScenePipeSignal, "CutScene-Pipe-Signal"),
                new SceneEntry(PlaytestSceneId.PuzzleSignal, "Puzzle Signal"),
                new SceneEntry(PlaytestSceneId.CutSceneSignalSwap, "CutScene-Signal-Swap"),
                new SceneEntry(PlaytestSceneId.GameOverScene, "GameOverScene"),
            };

            playtestChainOrder = new[]
            {
                PlaytestSceneId.StartScene,
                PlaytestSceneId.CutSceneStartTutorial,
                PlaytestSceneId.Tutorial,
                PlaytestSceneId.CutSceneTutorialPipe,
                PlaytestSceneId.PuzzlePipes,
                PlaytestSceneId.CutScenePipeSignal,
                PlaytestSceneId.PuzzleSignal,
                PlaytestSceneId.GameOverScene,
            };

            ResetScoreDefaults();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            expertSeconds = Mathf.Max(1f, expertSeconds);
            newPlayerSeconds = Mathf.Max(expertSeconds + 1f, newPlayerSeconds);
            sceneTimeCapSeconds = Mathf.Max(newPlayerSeconds + 1f, sceneTimeCapSeconds);
            attemptPenalty = Mathf.Max(0, attemptPenalty);
            hurryUpSeconds = Mathf.Clamp(hurryUpSeconds, 0, Mathf.FloorToInt(sceneTimeCapSeconds));
        }
#endif
    }
}
