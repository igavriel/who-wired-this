#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Core;
using WhoWiredThis.Environment;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Tutorial;
using WhoWiredThis.UI;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Wires playtest scenes: hide Exit in focus, fade overlays on dual HUD, completion popup scene transition.
    /// </summary>
    public static class TutorialCompletionTransitionWireTool
    {
        private const string TutorialMenuPath = "Who Wired This/Playtest/Wire Tutorial Completion Transition";
        private const string PipesMenuPath = "Who Wired This/Playtest/Wire Puzzle Pipes Completion Transition";
        private const string SignalMenuPath = "Who Wired This/Playtest/Wire Puzzle Signal Completion Transition";
        private const string TutorialScenePath = "Assets/Scenes/Game/Tutorial.unity";
        private const string PuzzlePipesScenePath = "Assets/Scenes/Game/Puzzle Pipes.unity";
        private const string PuzzleSignalScenePath = "Assets/Scenes/Game/Puzzle Signal.unity";
        private const string UiCanvasPrefabPath = "Assets/WhoWiredThis/Prefabs/Game/UI_Canvas.prefab";

        [MenuItem(TutorialMenuPath)]
        public static void WireTutorialCompletionTransition()
        {
            WireScene(TutorialScenePath, "Puzzle Pipes", hideExit: true);
        }

        [MenuItem(PipesMenuPath)]
        public static void WirePuzzlePipesCompletionTransition()
        {
            WireScene(PuzzlePipesScenePath, "Puzzle Signal", hideExit: false);
        }

        [MenuItem(SignalMenuPath)]
        public static void WirePuzzleSignalCompletionTransition()
        {
            WireScene(PuzzleSignalScenePath, PlaytestFlowUtility.GameOverSceneName, hideExit: true);
        }

        private static void WireScene(string scenePath, string targetSceneName, bool hideExit)
        {
            if (!EnsureSceneActive(scenePath, out Scene scene))
            {
                return;
            }

            int exitHidden = hideExit ? HideExitButtonsInScene() : 0;
            int focusUpdated = hideExit ? DisableExitInFocusCycleOnPanels() : 0;
            int hudFadeCount = EnsureFadeOverlaysOnUiCanvasPrefab();
            int transitionWired = WireCompletionPopupTransition(targetSceneName);
            int triggersDisabled = DisableWalkThroughTransitionTriggers(targetSceneName);

            UpdateCompletionCopy();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[TutorialCompletionTransitionWireTool] Done on '{scene.name}' -> '{targetSceneName}': " +
                $"exitHidden={exitHidden}, focusUpdated={focusUpdated}, hudFadeOnPrefab={hudFadeCount}, " +
                $"transitionWired={transitionWired}, walkTriggersDisabled={triggersDisabled}.");
        }

        private static bool EnsureSceneActive(string scenePath, out Scene scene)
        {
            scene = SceneManager.GetActiveScene();
            if (scene.path == scenePath)
            {
                return true;
            }

            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError($"[TutorialCompletionTransitionWireTool] Scene not found: {scenePath}");
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            return true;
        }

        private static int HideExitButtonsInScene()
        {
            int count = 0;
            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                string objectName = transform.name;
                if (!objectName.StartsWith("ExitButton Variant"))
                {
                    continue;
                }

                if (transform.gameObject.activeSelf)
                {
                    transform.gameObject.SetActive(false);
                    count++;
                }
            }

            return count;
        }

        private static int DisableExitInFocusCycleOnPanels()
        {
            int count = 0;
            PanelFocusController[] controllers = Object.FindObjectsByType<PanelFocusController>(FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                PanelFocusController controller = controllers[i];
                if (controller == null)
                {
                    continue;
                }

                SerializedObject serializedObject = new SerializedObject(controller);
                SerializedProperty includeExit = serializedObject.FindProperty("includeExitInFocusCycle");
                if (includeExit == null)
                {
                    continue;
                }

                if (includeExit.boolValue)
                {
                    includeExit.boolValue = false;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    count++;
                }
            }

            return count;
        }

        private static int EnsureFadeOverlaysOnUiCanvasPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(UiCanvasPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[TutorialCompletionTransitionWireTool] Failed to load prefab: {UiCanvasPrefabPath}");
                return 0;
            }

            int count = 0;
            try
            {
                PlayerHudView[] hudViews = prefabRoot.GetComponentsInChildren<PlayerHudView>(true);
                for (int i = 0; i < hudViews.Length; i++)
                {
                    PlayerHudView hudView = hudViews[i];
                    if (hudView == null)
                    {
                        continue;
                    }

                    if (hudView.GetComponent<SceneTransitionFadeOverlay>() == null)
                    {
                        hudView.gameObject.AddComponent<SceneTransitionFadeOverlay>();
                        count++;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, UiCanvasPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            return count;
        }

        private static int WireCompletionPopupTransition(string targetSceneName)
        {
            TutorialStageManager stageManager = Object.FindFirstObjectByType<TutorialStageManager>();
            if (stageManager == null)
            {
                Debug.LogError("[TutorialCompletionTransitionWireTool] TutorialStageManager not found in scene.");
                return 0;
            }

            PlayerHudView[] hudViews = Object.FindObjectsByType<PlayerHudView>(FindObjectsSortMode.None);
            PlayerHudView hudA = null;
            PlayerHudView hudB = null;
            for (int i = 0; i < hudViews.Length; i++)
            {
                PlayerHudView hud = hudViews[i];
                if (hud == null)
                {
                    continue;
                }

                if (hud.name.Contains("_A") || hud.name.EndsWith("A"))
                {
                    hudA = hud;
                }
                else if (hud.name.Contains("_B") || hud.name.EndsWith("B"))
                {
                    hudB = hud;
                }
            }

            if (hudA == null || hudB == null)
            {
                Debug.LogError("[TutorialCompletionTransitionWireTool] Could not resolve PlayerHud_A / PlayerHud_B.");
                return 0;
            }

            CompletionPopupSceneTransition transition =
                stageManager.GetComponent<CompletionPopupSceneTransition>();
            if (transition == null)
            {
                transition = stageManager.gameObject.AddComponent<CompletionPopupSceneTransition>();
            }

            SerializedObject serializedObject = new SerializedObject(transition);
            serializedObject.FindProperty("tutorialStageManager").objectReferenceValue = stageManager;
            serializedObject.FindProperty("completionPopupPanelA").objectReferenceValue = hudA.GetComponentInChildren<MessagePanel>(true);
            serializedObject.FindProperty("completionPopupPanelB").objectReferenceValue = hudB.GetComponentInChildren<MessagePanel>(true);
            serializedObject.FindProperty("targetSceneName").stringValue = targetSceneName;
            serializedObject.FindProperty("fadeOutDurationSeconds").floatValue = 1f;
            serializedObject.FindProperty("fadeOverlays").arraySize = 2;
            serializedObject.FindProperty("fadeOverlays").GetArrayElementAtIndex(0).objectReferenceValue =
                hudA.GetComponent<SceneTransitionFadeOverlay>() ?? hudA.gameObject.AddComponent<SceneTransitionFadeOverlay>();
            serializedObject.FindProperty("fadeOverlays").GetArrayElementAtIndex(1).objectReferenceValue =
                hudB.GetComponent<SceneTransitionFadeOverlay>() ?? hudB.gameObject.AddComponent<SceneTransitionFadeOverlay>();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return 1;
        }

        private static int DisableWalkThroughTransitionTriggers(string targetSceneName)
        {
            int count = 0;
            SceneTransitionTrigger[] triggers = Object.FindObjectsByType<SceneTransitionTrigger>(FindObjectsSortMode.None);
            for (int i = 0; i < triggers.Length; i++)
            {
                SceneTransitionTrigger trigger = triggers[i];
                if (trigger == null || !trigger.enabled)
                {
                    continue;
                }

                SerializedObject serializedObject = new SerializedObject(trigger);
                SerializedProperty targetScene = serializedObject.FindProperty("targetSceneName");
                if (targetScene != null &&
                    string.Equals(targetScene.stringValue, targetSceneName, System.StringComparison.Ordinal))
                {
                    trigger.enabled = false;
                    count++;
                }
            }

            return count;
        }

        private static void UpdateCompletionCopy()
        {
            TutorialStageManager stageManager = Object.FindFirstObjectByType<TutorialStageManager>();
            if (stageManager == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(stageManager);
            SerializedProperty completionMessage = serializedObject.FindProperty("completionMessage");
            if (completionMessage != null)
            {
                completionMessage.stringValue =
                    "Synchronization confirmed.\n\nClose the summary popup to continue.";
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
