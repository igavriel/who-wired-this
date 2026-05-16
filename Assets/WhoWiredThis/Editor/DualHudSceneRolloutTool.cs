using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Player;
using WhoWiredThis.UI;

namespace WhoWiredThis.Editor
{
    public static class DualHudSceneRolloutTool
    {
        private static readonly string[] TierAScenes =
        {
            "Assets/Scenes/Split Puzzle.unity",
            "Assets/Scenes/Tutorial.unity",
            "Assets/Scenes/TestPanelFocusMode.unity",
            "Assets/Scenes/Starter FirstPerson.unity",
            "Assets/Scenes/Duel/LocalDuel FirstPerson.unity",
            "Assets/Scenes/Split Tutorial Original.unity",
        };

        private static readonly string[] TierBScenes =
        {
            "Assets/Scenes/SampleScene.unity",
            "Assets/Scenes/Puzzles/RelayPuzzle.unity",
            "Assets/Scenes/Puzzles/Floor_Puzzle.unity",
            "Assets/Scenes/Puzzles/A17_PolarityPanel.unity",
            "Assets/Scenes/Puzzles/CombinedPuzzels.unity",
            "Assets/Scenes/Duel/LocalDuel ThirdPerson.unity",
        };

        [MenuItem("WhoWiredThis/Dual HUD/Rollout Tier A (wire playerHudView)")]
        public static void RolloutTierA()
        {
            foreach (string scenePath in TierAScenes)
            {
                WirePlayerHudViews(scenePath);
            }

            Debug.Log("[DualHudSceneRollout] Tier A rollout complete.");
        }

        [MenuItem("WhoWiredThis/Dual HUD/Rollout Tier B (disable PlayerHud_B)")]
        public static void RolloutTierB()
        {
            foreach (string scenePath in TierBScenes)
            {
                DisablePlayerHudB(scenePath);
            }

            Debug.Log("[DualHudSceneRollout] Tier B rollout complete.");
        }

        [MenuItem("WhoWiredThis/Dual HUD/Rollout All (Tier A + B)")]
        public static void RolloutAll()
        {
            RolloutTierA();
            RolloutTierB();
        }

        private static void WirePlayerHudViews(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var hudA = GameObject.Find("PlayerHud_A")?.GetComponent<PlayerHudView>();
            var hudB = GameObject.Find("PlayerHud_B")?.GetComponent<PlayerHudView>();
            if (hudA == null || hudB == null)
            {
                Debug.LogWarning($"[DualHudSceneRollout] Skip {scenePath}: missing PlayerHud_A or PlayerHud_B.");
                return;
            }

            int count = 0;
            var actions = Object.FindObjectsByType<PlayerActions>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var playerActions in actions)
            {
                if (!PlayerInteractorResolver.TryResolve(playerActions.transform, out AllowedPlayerTag playerTag))
                {
                    Debug.LogWarning($"[DualHudSceneRollout] {scenePath}: {playerActions.gameObject.name} has no PlayerA/B tag.");
                    continue;
                }

                PlayerHudView targetHud = playerTag == AllowedPlayerTag.Player_A ? hudA : hudB;
                var serializedObject = new SerializedObject(playerActions);
                SerializedProperty property = serializedObject.FindProperty("playerHudView");
                if (property == null)
                {
                    Debug.LogWarning($"[DualHudSceneRollout] {scenePath}: playerHudView property missing on {playerActions.gameObject.name}.");
                    continue;
                }

                property.objectReferenceValue = targetHud;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                count++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[DualHudSceneRollout] Wired {count} PlayerActions in {scenePath}.");
        }

        private static void DisablePlayerHudB(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var hudB = GameObject.Find("PlayerHud_B");
            if (hudB == null)
            {
                Debug.LogWarning($"[DualHudSceneRollout] Skip {scenePath}: no PlayerHud_B.");
                return;
            }

            if (hudB.activeSelf)
            {
                hudB.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[DualHudSceneRollout] Disabled PlayerHud_B in {scenePath}.");
            }
        }
    }
}
