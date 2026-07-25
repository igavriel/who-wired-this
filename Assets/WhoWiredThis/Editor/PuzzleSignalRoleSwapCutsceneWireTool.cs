#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Core;
using WhoWiredThis.Environment;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Scenes;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Wires Puzzle Signal for cut-scene role-swap round trip via <c>CutScene-Signal-Swap.unity</c>
    /// (mirrors Puzzle Pipes + <c>CutScene-Pipe-Swap</c>).
    /// </summary>
    public static class PuzzleSignalRoleSwapCutsceneWireTool
    {
        private const string PuzzleSignalScenePath = "Assets/Scenes/Game/Puzzle Signal.unity";
        private const string CutSceneSignalSwapPath = "Assets/Scenes/Game/CutScene-Signal-Swap.unity";
        private const string FlowConfigPath = "Assets/WhoWiredThis/Data/Playtest/GameConfig.asset";

        private const string WireMenuPath = "Who Wired This/Signal Calibration/Wire Puzzle Signal Role-Swap Cutscene";
        private const string McpWireMenuPath = "Who Wired This/Signal Calibration/MCP/Wire Puzzle Signal Role-Swap Cutscene";

        [MenuItem(WireMenuPath)]
        public static void WireInteractive()
        {
            if (!EditorUtility.DisplayDialog(
                    "Wire Puzzle Signal role-swap",
                    "Configure Puzzle Signal for CutSceneRoundTrip via CutScene-Signal-Swap?\n\n" +
                    "This sets turn-based operators (not simultaneous) and updates flow config + build settings.",
                    "Wire",
                    "Cancel"))
            {
                return;
            }

            int issues = WireAll();
            Debug.Log(issues == 0
                ? "[PuzzleSignalRoleSwapCutsceneWireTool] Puzzle Signal role-swap wiring complete."
                : $"[PuzzleSignalRoleSwapCutsceneWireTool] Finished with {issues} issue(s). See console.");
        }

        [MenuItem(McpWireMenuPath)]
        public static void WireForMcp()
        {
            int issues = WireAll();
            Debug.Log(issues == 0
                ? "[PuzzleSignalRoleSwapCutsceneWireTool] MCP wire complete."
                : $"[PuzzleSignalRoleSwapCutsceneWireTool] MCP wire finished with {issues} issue(s).");
        }

        public static int WireAll()
        {
            int issues = 0;
            issues += EnsureFlowConfigEntry();
            issues += EnsureBuildSettingsEntry();
            issues += WireCutSceneSignalSwapScene();
            issues += WirePuzzleSignalScene();
            AssetDatabase.SaveAssets();
            return issues;
        }

        private static int EnsureFlowConfigEntry()
        {
            GameConfigSO config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(FlowConfigPath);
            if (config == null)
            {
                Debug.LogError($"[PuzzleSignalRoleSwapCutsceneWireTool] Missing flow config at '{FlowConfigPath}'.");
                return 1;
            }

            SerializedObject so = new SerializedObject(config);
            SerializedProperty entries = so.FindProperty("sceneEntries");
            if (entries == null)
            {
                Debug.LogError("[PuzzleSignalRoleSwapCutsceneWireTool] sceneEntries not found on flow config.");
                return 1;
            }

            if (HasSceneEntry(entries, PlaytestSceneId.CutSceneSignalSwap))
            {
                return 0;
            }

            int insertIndex = entries.arraySize;
            entries.InsertArrayElementAtIndex(insertIndex);
            SerializedProperty entry = entries.GetArrayElementAtIndex(insertIndex);
            entry.FindPropertyRelative("id").enumValueIndex = (int)PlaytestSceneId.CutSceneSignalSwap;
            entry.FindPropertyRelative("sceneName").stringValue = "CutScene-Signal-Swap";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            Debug.Log("[PuzzleSignalRoleSwapCutsceneWireTool] Added CutSceneSignalSwap to GameConfig.");
            return 0;
        }

        private static bool HasSceneEntry(SerializedProperty entries, PlaytestSceneId id)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("id").enumValueIndex == (int)id)
                {
                    return true;
                }
            }

            return false;
        }

        private static int EnsureBuildSettingsEntry()
        {
            string[] scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            if (scenes.Any(path => path == CutSceneSignalSwapPath))
            {
                return 0;
            }

            var editorScenes = EditorBuildSettings.scenes.ToList();
            int insertAfter = editorScenes.FindIndex(s => s.path == PuzzleSignalScenePath);
            var newScene = new EditorBuildSettingsScene(CutSceneSignalSwapPath, true);
            if (insertAfter >= 0)
            {
                editorScenes.Insert(insertAfter, newScene);
            }
            else
            {
                editorScenes.Add(newScene);
            }

            EditorBuildSettings.scenes = editorScenes.ToArray();
            Debug.Log("[PuzzleSignalRoleSwapCutsceneWireTool] Added CutScene-Signal-Swap to Editor Build Settings.");
            return 0;
        }

        private static int WireCutSceneSignalSwapScene()
        {
            if (!File.Exists(CutSceneSignalSwapPath))
            {
                Debug.LogError($"[PuzzleSignalRoleSwapCutsceneWireTool] Missing '{CutSceneSignalSwapPath}'.");
                return 1;
            }

            Scene scene = EditorSceneManager.OpenScene(CutSceneSignalSwapPath, OpenSceneMode.Single);
            SceneFlowBootstrapConfig bootstrap = Object.FindFirstObjectByType<SceneFlowBootstrapConfig>();
            if (bootstrap != null)
            {
                SerializedObject bootstrapSo = new SerializedObject(bootstrap);
                SerializedProperty sceneIdProp = bootstrapSo.FindProperty("sceneId");
                if (sceneIdProp != null)
                {
                    sceneIdProp.enumValueIndex = (int)PlaytestSceneId.CutSceneSignalSwap;
                    bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            CinemachinePrioritySceneTransition[] transitions =
                Object.FindObjectsByType<CinemachinePrioritySceneTransition>(FindObjectsSortMode.None);
            foreach (CinemachinePrioritySceneTransition transition in transitions)
            {
                SerializedObject transitionSo = new SerializedObject(transition);
                SerializedProperty overrideProp = transitionSo.FindProperty("overrideTargetSceneId");
                if (overrideProp != null)
                {
                    overrideProp.enumValueIndex = (int)PlaytestSceneId.PuzzleSignal;
                    transitionSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PuzzleSignalRoleSwapCutsceneWireTool] Retargeted CutScene-Signal-Swap scene ids.");
            return 0;
        }

        private static int WirePuzzleSignalScene()
        {
            if (!File.Exists(PuzzleSignalScenePath))
            {
                Debug.LogError($"[PuzzleSignalRoleSwapCutsceneWireTool] Missing '{PuzzleSignalScenePath}'.");
                return 1;
            }

            Scene scene = EditorSceneManager.OpenScene(PuzzleSignalScenePath, OpenSceneMode.Single);
            int issues = 0;

            SceneStageManager stageManager = Object.FindFirstObjectByType<SceneStageManager>();
            if (stageManager == null)
            {
                Debug.LogError("[PuzzleSignalRoleSwapCutsceneWireTool] No SceneStageManager in Puzzle Signal.");
                return 1;
            }

            SerializedObject tsmSo = new SerializedObject(stageManager);
            SerializedProperty simultaneousProp = tsmSo.FindProperty("simultaneousOperators");
            SerializedProperty roleSwapProp = tsmSo.FindProperty("roleSwapMode");
            if (simultaneousProp != null)
            {
                simultaneousProp.boolValue = false;
            }

            if (roleSwapProp != null)
            {
                roleSwapProp.enumValueIndex = (int)SceneRoleSwapMode.CutSceneRoundTrip;
            }

            tsmSo.ApplyModifiedPropertiesWithoutUndo();

            SceneRoleSwapCutsceneTransition swapTransition =
                stageManager.GetComponent<SceneRoleSwapCutsceneTransition>();
            if (swapTransition == null)
            {
                swapTransition = Undo.AddComponent<SceneRoleSwapCutsceneTransition>(stageManager.gameObject);
            }

            SerializedObject swapSo = new SerializedObject(swapTransition);
            swapSo.FindProperty("sceneStageManager").objectReferenceValue = stageManager;
            swapSo.FindProperty("delaySeconds").floatValue = 3f;
            swapSo.FindProperty("useUnscaledTime").boolValue = true;
            swapSo.FindProperty("targetCutScene").enumValueIndex = (int)PlaytestSceneId.CutSceneSignalSwap;
            swapSo.FindProperty("ignoreWhenAlreadyInTargetScene").boolValue = true;
            swapSo.FindProperty("loadOnce").boolValue = true;
            swapSo.FindProperty("fadeOutDurationSeconds").floatValue = 1f;

            SceneFlowBootstrapConfig bootstrap = Object.FindFirstObjectByType<SceneFlowBootstrapConfig>();
            if (bootstrap != null)
            {
                swapSo.FindProperty("flowBootstrap").objectReferenceValue = bootstrap;
            }
            else
            {
                Debug.LogWarning("[PuzzleSignalRoleSwapCutsceneWireTool] No SceneFlowBootstrapConfig in scene.");
                issues++;
            }

            swapSo.ApplyModifiedPropertiesWithoutUndo();

            InitialPanelFocusBootstrap focusBootstrap = Object.FindFirstObjectByType<InitialPanelFocusBootstrap>();
            if (focusBootstrap != null)
            {
                SerializedObject focusSo = new SerializedObject(focusBootstrap);
                SerializedProperty useRoleStateProp = focusSo.FindProperty("useSceneRoleStateOperator");
                if (useRoleStateProp != null)
                {
                    useRoleStateProp.boolValue = true;
                    focusSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                Debug.LogWarning("[PuzzleSignalRoleSwapCutsceneWireTool] No InitialPanelFocusBootstrap in scene.");
                issues++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PuzzleSignalRoleSwapCutsceneWireTool] Wired Puzzle Signal SceneStageManager + swap transition.");
            return issues;
        }
    }
}
#endif
