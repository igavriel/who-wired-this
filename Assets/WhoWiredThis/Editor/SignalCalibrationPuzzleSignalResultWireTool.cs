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
    /// Wires Puzzle Signal submit feedback: operator sees ResultVisual_Root on their panel;
    /// diagnostic partner sees ResultLight on their panel (cross-panel, like Puzzle Pipes).
    /// </summary>
    public static class SignalCalibrationPuzzleSignalResultWireTool
    {
        private const string ScenePath = "Assets/Scenes/Game/Puzzle Signal.unity";
        private const string MenuPath = "Who Wired This/Signal Calibration/Wire Puzzle Signal Result Feedback";
        private const string McpMenuPath = "Who Wired This/Signal Calibration/MCP/Wire Puzzle Signal Result Feedback";
        private const string BridgeRootName = "PuzzleSignal_ResultLights";

        private const string PanelAName = "Signal_A_V2 Variant";
        private const string PanelBName = "Signal_B_V2 Variant";
        private const string LegacyPanelAName = "Player1_Signal_Panel-A";
        private const string LegacyPanelBName = "Player2_Signal_Panel-B";

        private static readonly string[] LightNames = { "ResultLight-Left", "ResultLight-Middle", "ResultLight-Right" };

        [MenuItem(MenuPath)]
        public static void WireResultFeedback()
        {
            if (!EnsureSceneActive())
            {
                return;
            }

            int issues = WireAllResultFeedback();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log(issues == 0
                ? "[SignalCalibrationPuzzleSignalResultWireTool] Puzzle Signal result feedback wired."
                : $"[SignalCalibrationPuzzleSignalResultWireTool] Finished with {issues} issue(s). See console.");
        }

        [MenuItem(McpMenuPath)]
        public static void WireResultFeedbackForMcp()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            int issues = WireAllResultFeedback();
            if (issues == 0)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log(issues == 0
                ? "[SignalCalibrationPuzzleSignalResultWireTool] MCP wire complete."
                : $"[SignalCalibrationPuzzleSignalResultWireTool] MCP wire finished with {issues} issue(s).");
        }

        public static int WireAllResultFeedback()
        {
            if (!TryGetSignalPanels(out GameObject panelA, out GameObject panelB))
            {
                Debug.LogError(
                    "[SignalCalibrationPuzzleSignalResultWireTool] Missing V2 or legacy signal panels in scene.");
                return 1;
            }

            return WireAllResultFeedback(panelA, panelB);
        }

        public static int WireAllResultFeedback(GameObject panelA, GameObject panelB)
        {
            if (panelA == null || panelB == null)
            {
                Debug.LogError(
                    "[SignalCalibrationPuzzleSignalResultWireTool] Missing panel references for result feedback wiring.");
                return 1;
            }

            int issues = 0;
            issues += WireOperatorResultVisual(
                panelA,
                AllowedPlayerTag.Player_A,
                new[] { "WAVE", "FREQ", "GAIN" });
            issues += WireOperatorResultVisual(
                panelB,
                AllowedPlayerTag.Player_B,
                new[] { "MODE", "TUNE", "AMP" });

            issues += WireResultLightsBridge(
                bridgeName: "Bridge_A_to_B_lights",
                operatorPanel: panelA,
                partnerPanel: panelB,
                visibleToPlayer: AllowedPlayerTag.Player_B);
            issues += WireResultLightsBridge(
                bridgeName: "Bridge_B_to_A_lights",
                operatorPanel: panelB,
                partnerPanel: panelA,
                visibleToPlayer: AllowedPlayerTag.Player_A);
            return issues;
        }

        private static bool TryGetSignalPanels(out GameObject panelA, out GameObject panelB)
        {
            panelA = FindSceneObjectByName(PanelAName);
            panelB = FindSceneObjectByName(PanelBName);
            if (panelA != null && panelB != null)
            {
                return true;
            }

            panelA = FindSceneObjectByName(LegacyPanelAName);
            panelB = FindSceneObjectByName(LegacyPanelBName);
            return panelA != null && panelB != null;
        }

        private static bool TryGetSignalPanels(out string panelAName, out string panelBName)
        {
            if (TryGetSignalPanels(out GameObject panelA, out GameObject panelB))
            {
                panelAName = panelA.name;
                panelBName = panelB.name;
                return true;
            }

            panelAName = null;
            panelBName = null;
            return false;
        }

        private static GameObject FindSceneObjectByName(string objectName)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Transform transform in transforms)
            {
                if (transform.name == objectName && transform.gameObject.scene.isLoaded)
                {
                    return transform.gameObject;
                }
            }

            return null;
        }

        private static bool EnsureSceneActive()
        {
            if (SceneManager.GetActiveScene().path == ScenePath)
            {
                return true;
            }

            if (!EditorUtility.DisplayDialog(
                    "Wire Puzzle Signal Result Feedback",
                    "Open Puzzle Signal.unity first. Open it now?",
                    "Open scene",
                    "Cancel"))
            {
                return false;
            }

            EditorSceneManager.OpenScene(ScenePath);
            return true;
        }

        private static int WireOperatorResultVisual(
            GameObject operatorPanel,
            AllowedPlayerTag operatorPlayer,
            string[] slotLabels)
        {
            int issues = 0;
            if (operatorPanel == null)
            {
                Debug.LogError("[SignalCalibrationPuzzleSignalResultWireTool] Missing operator panel.");
                return 1;
            }

            string operatorPanelName = operatorPanel.name;
            SubmittedCombinationMultiDimensionBridge bridge =
                operatorPanel.GetComponentInChildren<SubmittedCombinationMultiDimensionBridge>(true);
            if (bridge == null)
            {
                Debug.LogError(
                    $"[SignalCalibrationPuzzleSignalResultWireTool] No SubmittedCombinationMultiDimensionBridge under '{operatorPanelName}'.");
                return 1;
            }

            MultiDimensionPuzzleManager puzzleManager =
                operatorPanel.GetComponentInChildren<MultiDimensionPuzzleManager>(true);
            if (puzzleManager == null)
            {
                Debug.LogError(
                    $"[SignalCalibrationPuzzleSignalResultWireTool] No MultiDimensionPuzzleManager under '{operatorPanelName}'.");
                issues++;
            }

            Transform visualRoot = FindChildByNamePrefix(operatorPanel.transform, "ResultVisual_Root");
            if (visualRoot == null)
            {
                Debug.LogError(
                    $"[SignalCalibrationPuzzleSignalResultWireTool] No ResultVisual_Root under '{operatorPanelName}'.");
                issues++;
            }
            else
            {
                foreach (MultiDimension display in visualRoot.GetComponentsInChildren<MultiDimension>(true))
                {
                    SetMultiDimensionVisibleToPlayer(display, operatorPlayer);
                }
            }

            Transform operatorLightRoot = FindChildByNamePrefix(operatorPanel.transform, "ResultLight");
            if (operatorLightRoot != null)
            {
                foreach (MultiDimension lamp in operatorLightRoot.GetComponentsInChildren<MultiDimension>(true))
                {
                    SetMultiDimensionVisibleToPlayer(lamp, operatorPlayer);
                }
            }

            SerializedObject bridgeSo = new SerializedObject(bridge);
            bridgeSo.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;
            bridgeSo.FindProperty("visibleToPlayer").enumValueIndex = (int)operatorPlayer;

            SerializedProperty slotsProp = bridgeSo.FindProperty("slots");
            if (slotLabels != null)
            {
                if (slotsProp.arraySize != slotLabels.Length)
                {
                    slotsProp.arraySize = slotLabels.Length;
                }

                for (int i = 0; i < slotLabels.Length; i++)
                {
                    MultiDimension sourceInput = FindInputMultiDimension(operatorPanel.transform, slotLabels[i]);

                    SerializedProperty slot = slotsProp.GetArrayElementAtIndex(i);
                    slot.FindPropertyRelative("label").stringValue = slotLabels[i];
                    slot.FindPropertyRelative("sourceInput").objectReferenceValue = sourceInput;

                    MultiDimension display =
                        slot.FindPropertyRelative("display").objectReferenceValue as MultiDimension;
                    if (display == null)
                    {
                        display = FindDisplayForLabel(visualRoot, slotLabels[i]);
                        slot.FindPropertyRelative("display").objectReferenceValue = display;
                    }

                    if (sourceInput == null || display == null)
                    {
                        Debug.LogError(
                            $"[SignalCalibrationPuzzleSignalResultWireTool] Missing source or display for slot '{slotLabels[i]}' on '{operatorPanelName}'.");
                        issues++;
                    }
                    else
                    {
                        SetMultiDimensionVisibleToPlayer(display, operatorPlayer);
                    }
                }
            }

            bridgeSo.ApplyModifiedPropertiesWithoutUndo();

            if (puzzleManager != null && slotLabels != null)
            {
                int[] defaultIndices = new int[slotLabels.Length];
                bridge.ApplySubmittedIndices(defaultIndices);
            }

            return issues;
        }

        private static int WireResultLightsBridge(
            string bridgeName,
            GameObject operatorPanel,
            GameObject partnerPanel,
            AllowedPlayerTag visibleToPlayer)
        {
            int issues = 0;

            GameObject bridgeRoot = GameObject.Find(BridgeRootName);
            if (bridgeRoot == null)
            {
                bridgeRoot = new GameObject(BridgeRootName);
            }

            Transform legacyBridge = bridgeRoot.transform.Find("Bridge_A_lights");
            if (legacyBridge != null)
            {
                Undo.DestroyObjectImmediate(legacyBridge.gameObject);
            }

            legacyBridge = bridgeRoot.transform.Find("Bridge_B_lights");
            if (legacyBridge != null)
            {
                Undo.DestroyObjectImmediate(legacyBridge.gameObject);
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

            if (operatorPanel == null || partnerPanel == null)
            {
                Debug.LogError(
                    "[SignalCalibrationPuzzleSignalResultWireTool] Missing operator or partner panel for result lights bridge.");
                return issues + 1;
            }

            string operatorPanelName = operatorPanel.name;
            string partnerPanelName = partnerPanel.name;

            MultiDimensionPuzzleManager puzzleManager =
                operatorPanel.GetComponentInChildren<MultiDimensionPuzzleManager>(true);
            if (puzzleManager == null)
            {
                Debug.LogError(
                    $"[SignalCalibrationPuzzleSignalResultWireTool] Missing MultiDimensionPuzzleManager under '{operatorPanelName}'.");
                issues++;
            }

            ComponentDiagnosticAdapter adapter =
                operatorPanel.GetComponentInChildren<ComponentDiagnosticAdapter>(true);

            MultiDimension[] lights = new MultiDimension[LightNames.Length];
            for (int i = 0; i < LightNames.Length; i++)
            {
                lights[i] = FindSignalResultLight(partnerPanel.transform, LightNames[i]);
                if (lights[i] == null)
                {
                    Debug.LogError(
                        $"[SignalCalibrationPuzzleSignalResultWireTool] Missing '{LightNames[i]}' under '{partnerPanelName}'.");
                    issues++;
                }
                else
                {
                    SetMultiDimensionVisibleToPlayer(lights[i], visibleToPlayer);
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
                            $"[SignalCalibrationPuzzleSignalResultWireTool] Puzzle slot {slotIndex} missing on '{operatorPanelName}'.");
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

        private static Transform FindChildByNamePrefix(Transform root, string prefix)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith(prefix))
                {
                    return child;
                }
            }

            return null;
        }

        private static MultiDimension FindInputMultiDimension(Transform panelRoot, string inputName)
        {
            Transform buttonsRoot = panelRoot.Find("Buttons-A")
                ?? panelRoot.Find("Buttons-B")
                ?? panelRoot.Find("Buttons");
            if (buttonsRoot == null)
            {
                return null;
            }

            Transform input = buttonsRoot.Find(inputName);
            return input != null ? input.GetComponent<MultiDimension>() : null;
        }

        private static MultiDimension FindDisplayForLabel(Transform visualRoot, string label)
        {
            if (visualRoot == null || string.IsNullOrEmpty(label))
            {
                return null;
            }

            foreach (Transform child in visualRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child == visualRoot)
                {
                    continue;
                }

                if (!child.name.Contains(label, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                MultiDimension display = child.GetComponent<MultiDimension>();
                if (display != null)
                {
                    return display;
                }
            }

            return null;
        }

        private static MultiDimension FindSignalResultLight(Transform operatorPanel, string lightName)
        {
            Transform resultLightRoot = FindChildByNamePrefix(operatorPanel, "ResultLight");
            if (resultLightRoot == null)
            {
                return null;
            }

            Transform light = resultLightRoot.Find(lightName);
            return light != null ? light.GetComponent<MultiDimension>() : null;
        }

        private static void SetMultiDimensionVisibleToPlayer(MultiDimension dimension, AllowedPlayerTag player)
        {
            if (dimension == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(dimension);
            so.FindProperty("visibleToPlayer").enumValueIndex = (int)player;
            so.ApplyModifiedPropertiesWithoutUndo();
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
    }
}
#endif
