using System;
using System.Collections.Generic;
using UnityEngine;

namespace WhoWiredThis.Core
{
    [CreateAssetMenu(
        fileName = "PlaytestSceneFlowConfig",
        menuName = "Who Wired This/Playtest Scene Flow Config")]
    public class PlaytestSceneFlowConfigSO : ScriptableObject
    {
        [Tooltip("A list of scene entries.")]
        [Serializable]
        public struct SceneEntry
        {
            [Tooltip("The scene id for this entry.")]
            [SerializeField]
            public PlaytestSceneId id;

            [Tooltip("The name of the scene.")]
            [SerializeField]
            public string sceneName;

            public string Label => sceneName;
        }

        [SerializeField] private SceneEntry[] sceneEntries = Array.Empty<SceneEntry>();
        [SerializeField] private PlaytestSceneId[] playtestChainOrder = Array.Empty<PlaytestSceneId>();

        public IReadOnlyList<SceneEntry> SceneEntries => sceneEntries;
        public IReadOnlyList<PlaytestSceneId> PlaytestChainOrder => playtestChainOrder;

        public bool TryGetSceneName(PlaytestSceneId id, out string sceneName)
        {
            sceneName = null;
            if (id == PlaytestSceneId.None)
            {
                return false;
            }

            for (int i = 0; i < sceneEntries.Length; i++)
            {
                if (sceneEntries[i].id == id && !string.IsNullOrWhiteSpace(sceneEntries[i].sceneName))
                {
                    sceneName = sceneEntries[i].sceneName;
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
                if (string.Equals(sceneEntries[i].sceneName, sceneName, StringComparison.Ordinal))
                {
                    id = sceneEntries[i].id;
                    return id != PlaytestSceneId.None;
                }
            }

            return false;
        }

        public void SetDefaultsForCurrentPlaytestChain()
        {
            sceneEntries = new[]
            {
                new SceneEntry { id = PlaytestSceneId.StartScene, sceneName = "StartScene" },
                new SceneEntry { id = PlaytestSceneId.CutSceneStartTutorial, sceneName = "CutScene-Start-Tutorial" },
                new SceneEntry { id = PlaytestSceneId.Tutorial, sceneName = "Tutorial" },
                new SceneEntry { id = PlaytestSceneId.CutSceneTutorialSwap, sceneName = "CutScene-Tutorial-Swap" },
                new SceneEntry { id = PlaytestSceneId.CutSceneTutorialPipe, sceneName = "CutScene-Tutorial-Pipe" },
                new SceneEntry { id = PlaytestSceneId.PuzzlePipes, sceneName = "Puzzle Pipes" },
                new SceneEntry { id = PlaytestSceneId.CutScenePipeSwap, sceneName = "CutScene-Pipe-Swap" },
                new SceneEntry { id = PlaytestSceneId.CutScenePipeSignal, sceneName = "CutScene-Pipe-Signal" },
                new SceneEntry { id = PlaytestSceneId.PuzzleSignal, sceneName = "Puzzle Signal" },
                new SceneEntry { id = PlaytestSceneId.GameOverScene, sceneName = "GameOverScene" },
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
        }
    }
}
