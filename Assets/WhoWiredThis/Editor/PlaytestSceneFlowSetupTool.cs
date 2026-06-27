#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Core;
using WhoWiredThis.Environment;
using WhoWiredThis.UI;

namespace WhoWiredThis.Editor
{
    public static class PlaytestSceneFlowSetupTool
    {
        private const string SetupMenuPath = "Who Wired This/Playtest/Setup Scene Flow Config";
        private const string MigrateMenuPath = "Who Wired This/Playtest/Migrate Playtest Scenes To Scene Flow";
        private const string ValidateMenuPath = "Who Wired This/Playtest/Validate Scene Flow Config";
        private const string TestLogicMenuPath = "Who Wired This/Playtest/Run Scene Flow Logic Tests";

        private const string ConfigAssetPath = "Assets/WhoWiredThis/Data/Playtest/PlaytestSceneFlowConfig.asset";
        private const string BootstrapPrefabPath = "Assets/WhoWiredThis/Prefabs/Game/PlaytestSceneFlowBootstrap.prefab";
        private const string ManagersPrefabPath = "Assets/WhoWiredThis/Prefabs/Game/Managers.prefab";

        private static readonly (string ScenePath, PlaytestSceneId SceneId)[] PlaytestScenes =
        {
            ("Assets/Scenes/Game/StartScene.unity", PlaytestSceneId.StartScene),
            ("Assets/Scenes/Game/CutScene-Start-Tutorial.unity", PlaytestSceneId.CutSceneStartTutorial),
            ("Assets/Scenes/Game/Tutorial.unity", PlaytestSceneId.Tutorial),
            ("Assets/Scenes/Game/CutScene-Tutorial-Pipe.unity", PlaytestSceneId.CutSceneTutorialPipe),
            ("Assets/Scenes/Game/Puzzle Pipes.unity", PlaytestSceneId.PuzzlePipes),
            ("Assets/Scenes/Game/CutScene-Pipe-Signal.unity", PlaytestSceneId.CutScenePipeSignal),
            ("Assets/Scenes/Game/Puzzle Signal.unity", PlaytestSceneId.PuzzleSignal),
            ("Assets/Scenes/Game/GameOverScene.unity", PlaytestSceneId.GameOverScene),
        };

        [MenuItem(SetupMenuPath)]
        public static void SetupSceneFlowConfig()
        {
            EnsureFolder("Assets/WhoWiredThis/Data/Playtest");
            PlaytestSceneFlowConfigSO config = LoadOrCreateConfig();
            CreateOrUpdateBootstrapPrefab(config);
            UpdateManagersHotkeys(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlaytestSceneFlowSetupTool] Setup complete.");
        }

        [MenuItem(MigrateMenuPath)]
        public static void MigratePlaytestScenes()
        {
            MigrateAllPlaytestScenes();
        }

        public static void MigrateAllPlaytestScenes()
        {
            PlaytestSceneFlowConfigSO config = LoadOrCreateConfig();
            GameObject bootstrapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BootstrapPrefabPath);
            if (bootstrapPrefab == null)
            {
                bootstrapPrefab = CreateOrUpdateBootstrapPrefab(config);
            }

            int migrated = 0;
            for (int i = 0; i < PlaytestScenes.Length; i++)
            {
                (string scenePath, PlaytestSceneId sceneId) = PlaytestScenes[i];
                if (!File.Exists(scenePath))
                {
                    Debug.LogWarning($"[PlaytestSceneFlowSetupTool] Scene missing: {scenePath}");
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                EnsureBootstrapInScene(config, bootstrapPrefab, sceneId);
                WireBookendControllers(config);
                migrated++;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            UpdateManagersHotkeys(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlaytestSceneFlowSetupTool] Migrated {migrated} playtest scenes.");
        }

        public static void EnsureCurrentSceneBootstrap(PlaytestSceneId sceneId)
        {
            PlaytestSceneFlowConfigSO config = LoadOrCreateConfig();
            GameObject bootstrapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BootstrapPrefabPath);
            if (bootstrapPrefab == null)
            {
                bootstrapPrefab = CreateOrUpdateBootstrapPrefab(config);
            }

            EnsureBootstrapInScene(config, bootstrapPrefab, sceneId);
            WireBookendControllers(config);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem(ValidateMenuPath)]
        public static void ValidateSceneFlowConfig()
        {
            PlaytestSceneFlowConfigSO config = AssetDatabase.LoadAssetAtPath<PlaytestSceneFlowConfigSO>(ConfigAssetPath);
            int errors = 0;
            int warnings = 0;
            var report = new StringBuilder();
            report.AppendLine("[PlaytestSceneFlowSetupTool] Validation report");

            if (config == null)
            {
                report.AppendLine("ERROR: PlaytestSceneFlowConfig asset not found.");
                Debug.LogError(report.ToString());
                return;
            }

            ValidateConfigAsset(config, report, ref errors, ref warnings);

            for (int i = 0; i < PlaytestScenes.Length; i++)
            {
                (string scenePath, PlaytestSceneId expectedId) = PlaytestScenes[i];
                if (!File.Exists(scenePath))
                {
                    report.AppendLine($"ERROR: Missing scene file {scenePath}");
                    errors++;
                    continue;
                }

                ValidateSceneFile(scenePath, expectedId, config, report, ref errors, ref warnings);
            }

            ValidateManagersPrefab(config, report, ref errors, ref warnings);

            report.AppendLine($"Summary: {errors} error(s), {warnings} warning(s).");
            string flatReport = report.ToString().Replace("\n", " | ").TrimEnd(' ', '|');
            if (errors > 0)
            {
                Debug.LogError(flatReport);
            }
            else
            {
                Debug.Log(flatReport);
            }
        }

        [MenuItem(TestLogicMenuPath)]
        public static void RunSceneFlowLogicTests()
        {
            PlaytestSceneFlowConfigSO config = ScriptableObject.CreateInstance<PlaytestSceneFlowConfigSO>();
            config.SetDefaultsForCurrentPlaytestChain();

            int failed = 0;
            failed += AssertTrue(config.TryGetSceneName(PlaytestSceneId.Tutorial, out string tutorialName), "Tutorial name");
            failed += AssertEqual("Tutorial", tutorialName, "Tutorial name value");
            failed += AssertTrue(config.TryGetNext(PlaytestSceneId.Tutorial, out PlaytestSceneId nextId), "Tutorial next");
            failed += AssertEqual(PlaytestSceneId.CutSceneTutorialPipe, nextId, "Tutorial next id");
            failed += AssertTrue(config.TryGetNextSceneName(PlaytestSceneId.PuzzlePipes, out string pipesNext), "Pipes next name");
            failed += AssertEqual("CutScene-Pipe-Signal", pipesNext, "Pipes next name value");
            failed += AssertFalse(config.TryGetNext(PlaytestSceneId.GameOverScene, out _), "GameOver has no next");
            failed += AssertTrue(
                config.TryGetSceneIdForSceneName("CutScene-Start-Tutorial", out PlaytestSceneId reverseId),
                "Reverse lookup");
            failed += AssertEqual(PlaytestSceneId.CutSceneStartTutorial, reverseId, "Reverse lookup value");

            if (failed == 0)
            {
                Debug.Log("[PlaytestSceneFlowSetupTool] Scene flow logic tests PASSED (8 assertions).");
            }
            else
            {
                Debug.LogError($"[PlaytestSceneFlowSetupTool] Scene flow logic tests FAILED ({failed} assertion(s)).");
            }
        }

        private static PlaytestSceneFlowConfigSO LoadOrCreateConfig()
        {
            PlaytestSceneFlowConfigSO config = AssetDatabase.LoadAssetAtPath<PlaytestSceneFlowConfigSO>(ConfigAssetPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<PlaytestSceneFlowConfigSO>();
            config.SetDefaultsForCurrentPlaytestChain();
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            Debug.Log($"[PlaytestSceneFlowSetupTool] Created {ConfigAssetPath}");
            return config;
        }

        private static GameObject CreateOrUpdateBootstrapPrefab(PlaytestSceneFlowConfigSO config)
        {
            EnsureFolder("Assets/WhoWiredThis/Prefabs/Game");

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BootstrapPrefabPath);
            if (existing != null)
            {
                GameObject instance = PrefabUtility.LoadPrefabContents(BootstrapPrefabPath);
                try
                {
                    PlaytestSceneFlowBootstrap bootstrap = instance.GetComponent<PlaytestSceneFlowBootstrap>();
                    if (bootstrap == null)
                    {
                        bootstrap = instance.AddComponent<PlaytestSceneFlowBootstrap>();
                    }

                    SerializedObject serializedObject = new SerializedObject(bootstrap);
                    serializedObject.FindProperty("flowConfig").objectReferenceValue = config;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(instance, BootstrapPrefabPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(instance);
                }

                return existing;
            }

            GameObject root = new GameObject("PlaytestSceneFlowBootstrap");
            PlaytestSceneFlowBootstrap newBootstrap = root.AddComponent<PlaytestSceneFlowBootstrap>();
            SerializedObject bootstrapObject = new SerializedObject(newBootstrap);
            bootstrapObject.FindProperty("flowConfig").objectReferenceValue = config;
            bootstrapObject.ApplyModifiedPropertiesWithoutUndo();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BootstrapPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[PlaytestSceneFlowSetupTool] Created {BootstrapPrefabPath}");
            return prefab;
        }

        private static void EnsureBootstrapInScene(
            PlaytestSceneFlowConfigSO config,
            GameObject bootstrapPrefab,
            PlaytestSceneId sceneId)
        {
            PlaytestSceneFlowBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<PlaytestSceneFlowBootstrap>();
            if (bootstrap == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(bootstrapPrefab);
                instance.name = "PlaytestSceneFlowBootstrap";
                bootstrap = instance.GetComponent<PlaytestSceneFlowBootstrap>();
            }

            SerializedObject serializedObject = new SerializedObject(bootstrap);
            serializedObject.FindProperty("sceneId").enumValueIndex = (int)sceneId;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            if (PrefabUtility.IsPartOfPrefabInstance(bootstrap.gameObject))
            {
                SerializedProperty flowConfigProperty = serializedObject.FindProperty("flowConfig");
                PrefabUtility.RevertPropertyOverride(flowConfigProperty, InteractionMode.AutomatedAction);
            }
            else
            {
                serializedObject.FindProperty("flowConfig").objectReferenceValue = config;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void WireBookendControllers(PlaytestSceneFlowConfigSO config)
        {
            PlaytestSceneFlowBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<PlaytestSceneFlowBootstrap>();
            if (bootstrap == null)
            {
                return;
            }

            StartSceneController start = UnityEngine.Object.FindFirstObjectByType<StartSceneController>();
            if (start != null)
            {
                SerializedObject serializedObject = new SerializedObject(start);
                serializedObject.FindProperty("flowBootstrap").objectReferenceValue = bootstrap;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

            GameOverSceneController gameOver = UnityEngine.Object.FindFirstObjectByType<GameOverSceneController>();
            if (gameOver != null)
            {
                SerializedObject serializedObject = new SerializedObject(gameOver);
                serializedObject.FindProperty("flowBootstrap").objectReferenceValue = bootstrap;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void UpdateManagersHotkeys(PlaytestSceneFlowConfigSO config)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(ManagersPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"[PlaytestSceneFlowSetupTool] Managers prefab not found: {ManagersPrefabPath}");
                return;
            }

            try
            {
                SceneHotkeySwitcher switcher = prefabRoot.GetComponent<SceneHotkeySwitcher>();
                if (switcher == null)
                {
                    return;
                }

                SerializedObject serializedObject = new SerializedObject(switcher);
                serializedObject.FindProperty("flowConfig").objectReferenceValue = config;

                SerializedProperty bindings = serializedObject.FindProperty("bindings");
                bindings.arraySize = 6;
                SetHotkeyBinding(bindings.GetArrayElementAtIndex(0), PlaytestSceneId.StartScene, KeyCode.Alpha1);
                SetHotkeyBinding(bindings.GetArrayElementAtIndex(1), PlaytestSceneId.CutSceneStartTutorial, KeyCode.Alpha2);
                SetHotkeyBinding(bindings.GetArrayElementAtIndex(2), PlaytestSceneId.Tutorial, KeyCode.Alpha3);
                SetHotkeyBinding(bindings.GetArrayElementAtIndex(3), PlaytestSceneId.PuzzlePipes, KeyCode.Alpha4);
                SetHotkeyBinding(bindings.GetArrayElementAtIndex(4), PlaytestSceneId.PuzzleSignal, KeyCode.Alpha5);
                SetHotkeyBinding(bindings.GetArrayElementAtIndex(5), PlaytestSceneId.GameOverScene, KeyCode.Alpha6);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, ManagersPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void SetHotkeyBinding(SerializedProperty bindingProperty, PlaytestSceneId sceneId, KeyCode shortcut)
        {
            bindingProperty.FindPropertyRelative("sceneId").enumValueIndex = (int)sceneId;
            bindingProperty.FindPropertyRelative("shortcut").enumValueIndex = (int)shortcut;
        }

        private static void ValidateConfigAsset(
            PlaytestSceneFlowConfigSO config,
            StringBuilder report,
            ref int errors,
            ref int warnings)
        {
            IReadOnlyList<PlaytestSceneId> chain = config.PlaytestChainOrder;
            if (chain.Count < 2)
            {
                report.AppendLine("ERROR: playtestChainOrder is too short.");
                errors++;
            }

            for (int i = 0; i < chain.Count; i++)
            {
                PlaytestSceneId id = chain[i];
                if (!config.TryGetSceneName(id, out string sceneName))
                {
                    report.AppendLine($"ERROR: Chain id '{id}' has no scene name mapping.");
                    errors++;
                    continue;
                }

                if (!BuildSettingsContains(sceneName))
                {
                    report.AppendLine($"ERROR: Scene '{sceneName}' for '{id}' is not in Build Settings.");
                    errors++;
                }

                if (i + 1 < chain.Count &&
                    config.TryGetNext(id, out PlaytestSceneId nextId) &&
                    nextId != chain[i + 1])
                {
                    report.AppendLine($"ERROR: GetNext({id}) != chain[{i + 1}] ({nextId} != {chain[i + 1]}).");
                    errors++;
                }
            }
        }

        private static void ValidateSceneFile(
            string scenePath,
            PlaytestSceneId expectedId,
            PlaytestSceneFlowConfigSO config,
            StringBuilder report,
            ref int errors,
            ref int warnings)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
            PlaytestSceneFlowBootstrap bootstrap = UnityEngine.Object.FindFirstObjectByType<PlaytestSceneFlowBootstrap>();
            if (bootstrap == null)
            {
                report.AppendLine($"ERROR: {sceneName} — missing PlaytestSceneFlowBootstrap.");
                errors++;
            }
            else
            {
                SerializedObject serializedObject = new SerializedObject(bootstrap);
                PlaytestSceneId sceneId = (PlaytestSceneId)serializedObject.FindProperty("sceneId").enumValueIndex;
                if (sceneId != expectedId)
                {
                    report.AppendLine($"ERROR: {sceneName} — sceneId is {sceneId}, expected {expectedId}.");
                    errors++;
                }

                if (!config.TryGetNextSceneName(sceneId, out string nextName) && sceneId != PlaytestSceneId.GameOverScene)
                {
                    report.AppendLine($"ERROR: {sceneName} — no next scene configured for {sceneId}.");
                    errors++;
                }
                else if (config.TryGetNextSceneName(sceneId, out nextName))
                {
                    report.AppendLine($"OK: {sceneName} ({sceneId}) -> {nextName}");
                }
                else
                {
                    report.AppendLine($"OK: {sceneName} ({sceneId}) -> (terminal)");
                }
            }

            if (UnityEngine.Object.FindObjectsByType<CinemachinePrioritySceneTransition>(FindObjectsSortMode.None).Length > 0 ||
                UnityEngine.Object.FindObjectsByType<CompletionPopupSceneTransition>(FindObjectsSortMode.None).Length > 0 ||
                UnityEngine.Object.FindObjectsByType<SceneTransitionTrigger>(FindObjectsSortMode.None).Length > 0)
            {
                if (bootstrap == null)
                {
                    report.AppendLine($"ERROR: {sceneName} — transition components without bootstrap.");
                    errors++;
                }
            }
        }

        private static void ValidateManagersPrefab(
            PlaytestSceneFlowConfigSO config,
            StringBuilder report,
            ref int errors,
            ref int warnings)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ManagersPrefabPath);
            if (prefab == null)
            {
                report.AppendLine($"WARNING: Managers prefab missing at {ManagersPrefabPath}");
                warnings++;
                return;
            }

            SceneHotkeySwitcher switcher = prefab.GetComponentInChildren<SceneHotkeySwitcher>(true);
            if (switcher == null)
            {
                report.AppendLine("WARNING: SceneHotkeySwitcher missing on Managers prefab.");
                warnings++;
                return;
            }

            SerializedObject serializedObject = new SerializedObject(switcher);
            if (serializedObject.FindProperty("flowConfig").objectReferenceValue == null)
            {
                report.AppendLine("ERROR: Managers SceneHotkeySwitcher.flowConfig is not assigned.");
                errors++;
            }
            else
            {
                report.AppendLine("OK: Managers prefab hotkeys wired to flow config.");
            }
        }

        private static bool BuildSettingsContains(string sceneName)
        {
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                if (!EditorBuildSettings.scenes[i].enabled)
                {
                    continue;
                }

                string path = EditorBuildSettings.scenes[i].path;
                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static int AssertTrue(bool condition, string label)
        {
            if (condition)
            {
                return 0;
            }

            Debug.LogError($"[PlaytestSceneFlowSetupTool] FAIL: {label}");
            return 1;
        }

        private static int AssertFalse(bool condition, string label) => AssertTrue(!condition, label);

        private static int AssertEqual(PlaytestSceneId expected, PlaytestSceneId actual, string label)
        {
            if (expected == actual)
            {
                return 0;
            }

            Debug.LogError($"[PlaytestSceneFlowSetupTool] FAIL: {label} expected '{expected}' got '{actual}'");
            return 1;
        }

        private static int AssertEqual(string expected, string actual, string label)
        {
            if (string.Equals(expected, actual, StringComparison.Ordinal))
            {
                return 0;
            }

            Debug.LogError($"[PlaytestSceneFlowSetupTool] FAIL: {label} expected '{expected}' got '{actual}'");
            return 1;
        }
    }
}
#endif
