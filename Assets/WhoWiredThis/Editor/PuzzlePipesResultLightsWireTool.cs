#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Enums;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Wires cross-opponent <see cref="SplitResultPipesController"/> bridges in Puzzle Pipes.unity.
    /// </summary>
    public static class PuzzlePipesResultLightsWireTool
    {
        private const string ScenePath = "Assets/Scenes/Game/Puzzle Pipes.unity";
        private const string MenuPath = "Who Wired This/Pipe Pressure/Wire Puzzle Pipes Result Lights";
        private const string McpMenuPath = "Who Wired This/Pipe Pressure/MCP/Wire Puzzle Pipes Result Lights";
        private const string BridgeRootName = "PuzzlePipes_ResultLights";

        private const string PanelAName = "Pipes_A V2 Variant";
        private const string PanelBName = "Pipes_B V2 Variant";

        private static readonly string[] LightNames = { "ResultLight-Upper", "ResultLight-Middle", "ResultLight-Lower" };

        public static void WirePuzzlePipesResultLightsBatch()
        {
            EditorSceneManager.OpenScene(ScenePath);
            int issues = WireAllBridges();
            if (issues > 0)
            {
                Debug.LogError($"[PuzzlePipesResultLightsWireTool] Batch wire finished with {issues} issue(s).");
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[PuzzlePipesResultLightsWireTool] Batch wire complete.");
            EditorApplication.Exit(0);
        }

        [MenuItem(MenuPath)]
        public static void WirePuzzlePipesResultLights()
        {
            if (!EnsureSceneActive())
            {
                return;
            }

            int issues = WireAllBridges();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log(issues == 0
                ? "[PuzzlePipesResultLightsWireTool] Puzzle Pipes result lights wired."
                : $"[PuzzlePipesResultLightsWireTool] Finished with {issues} warning(s). See console.");
        }

        [MenuItem(McpMenuPath)]
        public static void WirePuzzlePipesResultLightsForMcp()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            int issues = WireAllBridges();
            if (issues == 0)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log(issues == 0
                ? "[PuzzlePipesResultLightsWireTool] MCP wire complete."
                : $"[PuzzlePipesResultLightsWireTool] MCP wire finished with {issues} issue(s).");
        }

        private const string ValidationMenuPath =
            "Who Wired This/Pipe Pressure/Validation/3. Pipes Result Lights";
        private const string ValidationMcpMenuPath =
            "Who Wired This/Pipe Pressure/MCP/3. Pipes Result Lights";

        [MenuItem(ValidationMenuPath)]
        public static void ValidateResultLights()
        {
            int issues = PipePressurePhase4ValidationTool.RunPipesResultLightsValidation(out string report);
            EditorValidationConsoleReporter.Report("Pipes Result Lights", issues, report, showDialog: true);
        }

        [MenuItem(ValidationMcpMenuPath)]
        public static void ValidateResultLightsForMcp()
        {
            int issues = PipePressurePhase4ValidationTool.RunPipesResultLightsValidation(out string report);
            EditorValidationConsoleReporter.Report("Pipes Result Lights", issues, report);
        }

        private static int WireAllBridges()
        {
            RemovePrefabBridgeLightsFromPanels();

            int issues = 0;
            issues += WireBridge(
                bridgeName: "Bridge_A_to_B_lights",
                operatorPanelName: PanelAName,
                partnerPanelName: PanelBName,
                visibleToPlayer: AllowedPlayerTag.Player_B);

            issues += WireBridge(
                bridgeName: "Bridge_B_to_A_lights",
                operatorPanelName: PanelBName,
                partnerPanelName: PanelAName,
                visibleToPlayer: AllowedPlayerTag.Player_A);
            return issues;
        }

        private static bool EnsureSceneActive()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath)
            {
                return true;
            }

            if (!EditorUtility.DisplayDialog(
                    "Wire Puzzle Pipes Result Lights",
                    "Open Puzzle Pipes.unity first. Open it now?",
                    "Open scene",
                    "Cancel"))
            {
                return false;
            }

            EditorSceneManager.OpenScene(ScenePath);
            return true;
        }

        private static void RemovePrefabBridgeLightsFromPanels()
        {
            RemoveBridgeLightsChild(GameObject.Find(PanelAName));
            RemoveBridgeLightsChild(GameObject.Find(PanelBName));
        }

        private static void RemoveBridgeLightsChild(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            Transform resultLightRoot = panel.transform.Find("ResultLight");
            if (resultLightRoot == null)
            {
                return;
            }

            Transform bridge = resultLightRoot.Find("Bridge_lights");
            if (bridge != null)
            {
                Undo.DestroyObjectImmediate(bridge.gameObject);
            }
        }

        private static int WireBridge(
            string bridgeName,
            string operatorPanelName,
            string partnerPanelName,
            AllowedPlayerTag visibleToPlayer)
        {
            int issues = 0;

            GameObject bridgeRoot = GameObject.Find(BridgeRootName);
            if (bridgeRoot == null)
            {
                bridgeRoot = new GameObject(BridgeRootName);
            }

            Transform bridgeTransform = bridgeRoot.transform.Find(bridgeName);
            GameObject bridgeObject;
            if (bridgeTransform == null)
            {
                bridgeObject = new GameObject(bridgeName);
                bridgeObject.transform.SetParent(bridgeRoot.transform, worldPositionStays: false);
            }
            else
            {
                bridgeObject = bridgeTransform.gameObject;
            }

            SplitResultTutorialController legacyTutorial =
                bridgeObject.GetComponent<SplitResultTutorialController>();
            if (legacyTutorial != null)
            {
                Undo.DestroyObjectImmediate(legacyTutorial);
            }

            SplitResultPipesController controller =
                bridgeObject.GetComponent<SplitResultPipesController>();
            if (controller == null)
            {
                controller = Undo.AddComponent<SplitResultPipesController>(bridgeObject);
            }

            GameObject operatorPanel = GameObject.Find(operatorPanelName);
            GameObject partnerPanel = GameObject.Find(partnerPanelName);
            if (operatorPanel == null || partnerPanel == null)
            {
                Debug.LogError(
                    $"[PuzzlePipesResultLightsWireTool] Missing panel '{operatorPanelName}' or '{partnerPanelName}'.");
                return issues + 1;
            }

            MultiDimensionPuzzleManager puzzleManager =
                operatorPanel.GetComponentInChildren<MultiDimensionPuzzleManager>(true);
            if (puzzleManager == null)
            {
                Debug.LogError(
                    $"[PuzzlePipesResultLightsWireTool] Missing MultiDimensionPuzzleManager under '{operatorPanelName}'.");
                issues++;
            }

            ComponentDiagnosticAdapter adapter =
                operatorPanel.GetComponentInChildren<ComponentDiagnosticAdapter>(true);

            MultiDimension[] lights = new MultiDimension[LightNames.Length];
            for (int i = 0; i < LightNames.Length; i++)
            {
                lights[i] = FindResultLight(partnerPanelName, LightNames[i]);
                if (lights[i] == null)
                {
                    Debug.LogError(
                        $"[PuzzlePipesResultLightsWireTool] Missing '{LightNames[i]}' under '{partnerPanelName}/ResultLight'.");
                    issues++;
                }
            }

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;
            so.FindProperty("updateContinuously").boolValue = false;
            so.FindProperty("visibleToPlayer").enumValueIndex = (int)visibleToPlayer;

            SerializedProperty slotsProp = so.FindProperty("elementLights");
            slotsProp.arraySize = LightNames.Length;

            if (puzzleManager != null)
            {
                for (int slotIndex = 0; slotIndex < LightNames.Length; slotIndex++)
                {
                    if (!puzzleManager.TryGetPuzzleElement(slotIndex, out MultiDimension sourceElement, out _))
                    {
                        Debug.LogError(
                            $"[PuzzlePipesResultLightsWireTool] Puzzle slot {slotIndex} missing on '{operatorPanelName}'.");
                        issues++;
                        continue;
                    }

                    ComponentDiagnosticType diagnosticType = ResolveDiagnosticType(adapter, sourceElement);

                    SerializedProperty slot = slotsProp.GetArrayElementAtIndex(slotIndex);
                    slot.FindPropertyRelative("sourceElement").objectReferenceValue = sourceElement;
                    slot.FindPropertyRelative("resultLight").objectReferenceValue = lights[slotIndex];
                    slot.FindPropertyRelative("diagnosticType").enumValueIndex = (int)diagnosticType;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            if (puzzleManager != null)
            {
                int elementCount = puzzleManager.PuzzleElementCount;
                int[] defaultIndices = new int[elementCount];
                controller.ApplySubmittedIndices(defaultIndices, solved: false);
            }

            return issues;
        }

        private static ComponentDiagnosticType ResolveDiagnosticType(
            ComponentDiagnosticAdapter adapter,
            MultiDimension sourceElement)
        {
            if (adapter == null || sourceElement == null)
            {
                return ComponentDiagnosticType.Ordered;
            }

            SerializedObject adapterSo = new SerializedObject(adapter);
            SerializedProperty components = adapterSo.FindProperty("components");
            for (int i = 0; i < components.arraySize; i++)
            {
                SerializedProperty component = components.GetArrayElementAtIndex(i);
                MultiDimension input = component.FindPropertyRelative("input").objectReferenceValue as MultiDimension;
                if (input != sourceElement)
                {
                    continue;
                }

                return (ComponentDiagnosticType)component.FindPropertyRelative("diagnosticType").enumValueIndex;
            }

            return ComponentDiagnosticType.Ordered;
        }

        private static MultiDimension FindResultLight(string panelName, string lightName)
        {
            GameObject panel = GameObject.Find(panelName);
            if (panel == null)
            {
                return null;
            }

            Transform light = panel.transform.Find($"ResultLight/{lightName}");
            return light != null ? light.GetComponent<MultiDimension>() : null;
        }
    }
}
#endif
