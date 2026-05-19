#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Tutorial;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// One-shot wiring for Puzzel Pipes: 3×4-state inputs, puzzle managers, history, panel focus, turn locks.
    /// Menu: Who Wired This / Pipe Pressure / Wire Puzzel Pipes Scene
    /// </summary>
    public static class PipePressurePuzzelPipesWireTool
    {
        private const string MenuPath = "Who Wired This/Pipe Pressure/Wire Puzzel Pipes Scene";
        private const string HistoryHeadersMenuPath = "Who Wired This/Pipe Pressure/Apply Puzzel Pipes History Headers (Phase 2)";
        private const string ComponentDiagnosticMenuPath =
            "Who Wired This/Pipe Pressure/Wire Puzzel Pipes Component Diagnostic (Phase 3)";
        private const string ResultVisualizerMenuPath =
            "Who Wired This/Pipe Pressure/Wire Puzzel Pipes Result Visualizer (Phase 4)";
        private const string RandomSolutionMenuPath =
            "Who Wired This/Pipe Pressure/Wire Random Solution Assigner (Phase 5)";

        private static readonly Color NeutralVisualColor = new Color(0.55f, 0.58f, 0.62f, 1f);

        /// <summary>INPUT column width for three 5-char tokens plus two spaces (17).</summary>
        public const string PuzzelPipesHistoryHeaderLine = " # | SIDE | INPUT             | STATUS";

        public const string PuzzelPipesHistorySeparatorLine = "===+======+===================+========";

        [MenuItem(HistoryHeadersMenuPath)]
        public static void ApplyPuzzelPipesHistoryHeaders()
        {
            ApplyHistoryHeaders("Player1_Panel/HistoryPanel");
            ApplyHistoryHeaders("Player2_Panel/HistoryPanel");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log(
                "[PipePressurePuzzelPipesWireTool] Applied Phase 2 history header/separator to both Puzzel Pipes HistoryPanels.");
        }

        private static void ApplyHistoryHeaders(string historyPanelPath)
        {
            HistoryBoardController board = GameObject.Find(historyPanelPath)?.GetComponent<HistoryBoardController>();
            if (board == null)
            {
                Debug.LogError($"[PipePressurePuzzelPipesWireTool] Missing HistoryBoardController at '{historyPanelPath}'.");
                return;
            }

            SerializedObject so = new SerializedObject(board);
            so.FindProperty("headerLine").stringValue = PuzzelPipesHistoryHeaderLine;
            so.FindProperty("separatorLine").stringValue = PuzzelPipesHistorySeparatorLine;
            so.ApplyModifiedPropertiesWithoutUndo();
            board.Render();
        }

        [MenuItem(ComponentDiagnosticMenuPath)]
        public static void WireComponentDiagnostic()
        {
            WireComponentDiagnosticPanel(
                "Player1_Panel",
                new[] { "VALVE", "PRESS", "FLOW" },
                new[]
                {
                    (ComponentDiagnosticType.Ordered, "VALVE LOOKS STABLE.", "VALVE IS TOO CLOSED.", "VALVE IS TOO OPEN.", string.Empty),
                    (ComponentDiagnosticType.Ordered, "PRESSURE LOOKS STABLE.", "PRESSURE IS TOO LOW.", "PRESSURE IS TOO HIGH.", string.Empty),
                    (ComponentDiagnosticType.Categorical, "FLOW ROUTE LOOKS STABLE.", string.Empty, string.Empty, "FLOW ROUTE DOES NOT MATCH.")
                });

            WireComponentDiagnosticPanel(
                "Player2_Panel",
                new[] { "GATE", "PUMP", "ROUTE" },
                new[]
                {
                    (ComponentDiagnosticType.Ordered, "GATE LOOKS STABLE.", "GATE IS TOO CLOSED.", "GATE IS TOO OPEN.", string.Empty),
                    (ComponentDiagnosticType.Ordered, "PUMP LOOKS STABLE.", "PUMP IS TOO LOW.", "PUMP IS TOO HIGH.", string.Empty),
                    (ComponentDiagnosticType.Categorical, "ROUTE LOOKS STABLE.", string.Empty, string.Empty, "ROUTE DOES NOT MATCH.")
                });

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[PipePressurePuzzelPipesWireTool] Wired Phase 3 component diagnostic on Puzzel Pipes.");
        }

        private static void WireComponentDiagnosticPanel(
            string panelName,
            string[] inputNames,
            (ComponentDiagnosticType type, string correct, string tooLow, string tooHigh, string mismatch)[] defs)
        {
            GameObject panel = GameObject.Find(panelName);
            if (panel == null)
            {
                Debug.LogError($"[PipePressurePuzzelPipesWireTool] Missing '{panelName}'.");
                return;
            }

            MultiDimensionDiagnosticAdapter legacy = panel.GetComponent<MultiDimensionDiagnosticAdapter>();
            DiagnosticDisplayController display = legacy != null ? GetLegacyDiagnosticDisplay(legacy) : null;
            if (legacy != null)
            {
                legacy.enabled = false;
            }

            ComponentDiagnosticAdapter adapter = panel.GetComponent<ComponentDiagnosticAdapter>();
            if (adapter == null)
            {
                adapter = panel.AddComponent<ComponentDiagnosticAdapter>();
            }

            MultiDimensionPuzzelManager puzzleManager = GameObject.Find($"{panelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzelManager>();

            if (puzzleManager == null || display == null)
            {
                Debug.LogError(
                    $"[PipePressurePuzzelPipesWireTool] Missing puzzleManager or diagnosticDisplay for '{panelName}'.");
                return;
            }

            var dimensions = new MultiDimension[inputNames.Length];
            for (int i = 0; i < inputNames.Length; i++)
            {
                dimensions[i] = GameObject.Find($"{panelName}/Buttons/{inputNames[i]}")
                    ?.GetComponent<MultiDimension>();
            }

            SerializedObject adapterSo = new SerializedObject(adapter);
            adapterSo.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;
            adapterSo.FindProperty("diagnosticDisplay").objectReferenceValue = display;

            SerializedProperty componentsProp = adapterSo.FindProperty("components");
            componentsProp.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
            {
                SerializedProperty entry = componentsProp.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("input").objectReferenceValue = dimensions[i];
                entry.FindPropertyRelative("diagnosticType").enumValueIndex = (int)defs[i].type;
                entry.FindPropertyRelative("correctText").stringValue = defs[i].correct;
                entry.FindPropertyRelative("tooLowText").stringValue = defs[i].tooLow;
                entry.FindPropertyRelative("tooHighText").stringValue = defs[i].tooHigh;
                entry.FindPropertyRelative("mismatchText").stringValue = defs[i].mismatch;
                entry.FindPropertyRelative("eligibleForHints").boolValue = true;
            }

            adapterSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static DiagnosticDisplayController GetLegacyDiagnosticDisplay(MultiDimensionDiagnosticAdapter legacy)
        {
            SerializedObject so = new SerializedObject(legacy);
            return so.FindProperty("diagnosticDisplay").objectReferenceValue as DiagnosticDisplayController;
        }

        [MenuItem(ResultVisualizerMenuPath)]
        public static void WireResultVisualizer()
        {
            WireResultVisualizerSide(
                "Player1_Panel",
                "Player2_Panel",
                new[] { "VALVE", "PRESS", "FLOW" },
                new[] { "ValveGroup", "PressureGroup", "FlowGroup" },
                VisualRigLayout.Blue);

            WireResultVisualizerSide(
                "Player2_Panel",
                "Player1_Panel",
                new[] { "GATE", "PUMP", "ROUTE" },
                new[] { "GateGroup", "PumpGroup", "RouteGroup" },
                VisualRigLayout.Red);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[PipePressurePuzzelPipesWireTool] Wired Phase 4 result visualizer on Puzzel Pipes.");
        }

        private enum VisualRigLayout
        {
            Blue,
            Red
        }

        private static void WireResultVisualizerSide(
            string operatorPanelName,
            string partnerPanelName,
            string[] inputNames,
            string[] groupNames,
            VisualRigLayout layout)
        {
            GameObject operatorPanel = GameObject.Find(operatorPanelName);
            GameObject partnerPanel = GameObject.Find(partnerPanelName);
            if (operatorPanel == null || partnerPanel == null)
            {
                Debug.LogError(
                    $"[PipePressurePuzzelPipesWireTool] Missing panel '{operatorPanelName}' or '{partnerPanelName}'.");
                return;
            }

            DiagnosticDisplayController partnerDisplay =
                partnerPanel.GetComponentInChildren<DiagnosticDisplayController>(true);
            if (partnerDisplay == null)
            {
                Debug.LogError(
                    $"[PipePressurePuzzelPipesWireTool] No DiagnosticDisplayController under '{partnerPanelName}'.");
                return;
            }

            Transform rigRoot = EnsureResultVisualRig(partnerDisplay.transform);
            var dimensions = new MultiDimension[inputNames.Length];
            var stateVisualArrays = new GameObject[inputNames.Length][];

            for (int i = 0; i < inputNames.Length; i++)
            {
                dimensions[i] = GameObject.Find($"{operatorPanelName}/Buttons/{inputNames[i]}")
                    ?.GetComponent<MultiDimension>();
                Transform groupRoot = EnsureChild(rigRoot, groupNames[i]);
                stateVisualArrays[i] = BuildStateVisuals(groupRoot, layout, i);
            }

            SubmittedCombinationVisualizer visualizer = operatorPanel.GetComponent<SubmittedCombinationVisualizer>();
            if (visualizer == null)
            {
                visualizer = operatorPanel.AddComponent<SubmittedCombinationVisualizer>();
            }

            MultiDimensionPuzzelManager puzzleManager = GameObject.Find($"{operatorPanelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzelManager>();

            SerializedObject vizSo = new SerializedObject(visualizer);
            vizSo.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;
            vizSo.FindProperty("visualRoot").objectReferenceValue = rigRoot;

            SerializedProperty slotsProp = vizSo.FindProperty("slots");
            slotsProp.arraySize = inputNames.Length;
            for (int i = 0; i < inputNames.Length; i++)
            {
                SerializedProperty slot = slotsProp.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("sourceInput").objectReferenceValue = dimensions[i];
                slot.FindPropertyRelative("label").stringValue = inputNames[i];

                SerializedProperty visuals = slot.FindPropertyRelative("stateVisuals");
                GameObject[] states = stateVisualArrays[i];
                visuals.arraySize = states.Length;
                for (int v = 0; v < states.Length; v++)
                {
                    visuals.GetArrayElementAtIndex(v).objectReferenceValue = states[v];
                }
            }

            vizSo.ApplyModifiedPropertiesWithoutUndo();

            for (int i = 0; i < inputNames.Length; i++)
            {
                SetSlotActive(stateVisualArrays[i], 0);
            }
        }

        private static Transform EnsureResultVisualRig(Transform diagnosticRoot)
        {
            Transform existing = diagnosticRoot.Find("ResultVisual_Root");
            if (existing != null)
            {
                return existing;
            }

            var rootGo = new GameObject("ResultVisual_Root");
            Undo.RegisterCreatedObjectUndo(rootGo, "Create ResultVisual_Root");
            Transform root = rootGo.transform;
            root.SetParent(diagnosticRoot, false);
            root.localPosition = new Vector3(0f, 0.35f, -0.15f);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            return root;
        }

        private static Transform EnsureChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child != null)
            {
                return child;
            }

            var go = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(go, "Create visual group");
            child = go.transform;
            child.SetParent(parent, false);
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }

        private static GameObject[] BuildStateVisuals(Transform groupRoot, VisualRigLayout layout, int groupIndex)
        {
            var states = new GameObject[4];
            for (int s = 0; s < 4; s++)
            {
                states[s] = CreateNeutralPrimitive(groupRoot, $"State{s}", layout, groupIndex, s);
            }

            return states;
        }

        private static GameObject CreateNeutralPrimitive(
            Transform parent,
            string name,
            VisualRigLayout layout,
            int groupIndex,
            int stateIndex)
        {
            PrimitiveType primitive = layout == VisualRigLayout.Blue && groupIndex == 2
                ? PrimitiveType.Cylinder
                : PrimitiveType.Cube;

            GameObject go = GameObject.CreatePrimitive(primitive);
            Undo.RegisterCreatedObjectUndo(go, "Create visual state");
            go.name = name;
            Transform t = go.transform;
            t.SetParent(parent, false);

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                Undo.DestroyObjectImmediate(col);
            }

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = NeutralVisualColor;
                renderer.sharedMaterial = mat;
            }

            ApplyVisualLayout(t, layout, groupIndex, stateIndex);
            go.SetActive(false);
            return go;
        }

        private static void ApplyVisualLayout(Transform t, VisualRigLayout layout, int groupIndex, int stateIndex)
        {
            float xOffset = groupIndex * 0.45f - 0.45f;

            if (groupIndex == 0)
            {
                float open = 0.15f + stateIndex * 0.28f;
                t.localPosition = new Vector3(xOffset, 0f, 0f);
                t.localScale = new Vector3(open, 0.35f, 0.35f);
                return;
            }

            if (groupIndex == 1)
            {
                float height = 0.2f + stateIndex * 0.25f;
                t.localPosition = new Vector3(xOffset, height * 0.5f, 0f);
                t.localScale = new Vector3(0.25f, height, 0.25f);
                return;
            }

            float routeX = (stateIndex - 1.5f) * 0.22f;
            t.localPosition = new Vector3(xOffset + routeX, 0.1f, 0f);
            if (stateIndex == 3)
            {
                t.localScale = new Vector3(0.35f, 0.08f, 0.35f);
            }
            else
            {
                t.localScale = new Vector3(0.12f, 0.35f, 0.35f);
            }
        }

        private static void SetSlotActive(GameObject[] stateVisuals, int activeIndex)
        {
            if (stateVisuals == null)
            {
                return;
            }

            for (int i = 0; i < stateVisuals.Length; i++)
            {
                if (stateVisuals[i] != null)
                {
                    stateVisuals[i].SetActive(i == activeIndex);
                }
            }
        }

        [MenuItem(MenuPath)]
        public static void WireScene()
        {
            WirePanel(
                "Player1_Panel",
                new[] { "VALVE", "PRESS", "FLOW" },
                new[] { 2, 1, 2 });

            WirePanel(
                "Player2_Panel",
                new[] { "GATE", "PUMP", "ROUTE" },
                new[] { 3, 2, 3 });

            WireTurnLocks();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[PipePressurePuzzelPipesWireTool] Wired Player1_Panel and Player2_Panel (3 inputs each).");
        }

        private static void WirePanel(string panelName, string[] inputNames, int[] correctIndices)
        {
            if (inputNames.Length != correctIndices.Length)
            {
                Debug.LogError("[PipePressurePuzzelPipesWireTool] inputNames and correctIndices length mismatch.");
                return;
            }

            var dimensions = new MultiDimension[inputNames.Length];
            var cyclers = new MultiDimensionSubjectCycler[inputNames.Length];

            for (int i = 0; i < inputNames.Length; i++)
            {
                string path = $"{panelName}/Buttons/{inputNames[i]}";
                GameObject inputGo = GameObject.Find(path);
                if (inputGo == null)
                {
                    Debug.LogError($"[PipePressurePuzzelPipesWireTool] Missing GameObject at '{path}'.");
                    return;
                }

                dimensions[i] = inputGo.GetComponent<MultiDimension>();
                cyclers[i] = inputGo.GetComponent<MultiDimensionSubjectCycler>();
                if (dimensions[i] == null || cyclers[i] == null)
                {
                    Debug.LogError($"[PipePressurePuzzelPipesWireTool] Missing MultiDimension/cycler on '{path}'.");
                    return;
                }
            }

            MultiDimensionPuzzelManager puzzleManager = GameObject.Find($"{panelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzelManager>();
            if (puzzleManager == null)
            {
                Debug.LogError($"[PipePressurePuzzelPipesWireTool] Missing PuzzleManager on '{panelName}'.");
                return;
            }

            SerializedObject pmSo = new SerializedObject(puzzleManager);
            SerializedProperty elems = pmSo.FindProperty("puzzleElements");
            elems.arraySize = inputNames.Length;
            for (int i = 0; i < inputNames.Length; i++)
            {
                SerializedProperty el = elems.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("element").objectReferenceValue = dimensions[i];
                el.FindPropertyRelative("correctIndex").intValue = correctIndices[i];
            }

            MultiDimensionPuzzleInteractableBridge bridge =
                puzzleManager.GetComponent<MultiDimensionPuzzleInteractableBridge>();
            SolveInteractProxy solveProxy = GameObject.Find($"{panelName}/Buttons/SolveButton")
                ?.GetComponent<SolveInteractProxy>();

            SerializedProperty disableList = pmSo.FindProperty("interactionsToDisable");
            disableList.arraySize = inputNames.Length + 2;
            for (int i = 0; i < inputNames.Length; i++)
            {
                disableList.GetArrayElementAtIndex(i).objectReferenceValue = dimensions[i];
            }

            disableList.GetArrayElementAtIndex(inputNames.Length).objectReferenceValue = bridge;
            disableList.GetArrayElementAtIndex(inputNames.Length + 1).objectReferenceValue = solveProxy;
            pmSo.ApplyModifiedPropertiesWithoutUndo();

            MultiDimensionHistoryAdapter history = GameObject.Find(panelName)
                ?.GetComponent<MultiDimensionHistoryAdapter>();
            if (history != null)
            {
                SerializedObject histSo = new SerializedObject(history);
                SerializedProperty order = histSo.FindProperty("inputOrder");
                order.arraySize = inputNames.Length;
                for (int i = 0; i < inputNames.Length; i++)
                {
                    order.GetArrayElementAtIndex(i).objectReferenceValue = dimensions[i];
                }

                histSo.ApplyModifiedPropertiesWithoutUndo();
            }

            PanelFocusController focus = GameObject.Find($"{panelName}/Board")
                ?.GetComponent<PanelFocusController>();
            if (focus != null)
            {
                SerializedObject pfcSo = new SerializedObject(focus);
                SerializedProperty buttons = pfcSo.FindProperty("interactableButtons");
                buttons.arraySize = inputNames.Length;
                for (int i = 0; i < inputNames.Length; i++)
                {
                    GameObject inputGo = GameObject.Find($"{panelName}/Buttons/{inputNames[i]}");
                    SerializedProperty btn = buttons.GetArrayElementAtIndex(i);
                    btn.FindPropertyRelative("label").stringValue = inputNames[i];
                    btn.FindPropertyRelative("highlightAnchor").objectReferenceValue = inputGo.transform;
                    btn.FindPropertyRelative("interactableReference").objectReferenceValue = cyclers[i];
                }

                pfcSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void WireTurnLocks()
        {
            TutorialStageManager stageManager = Object.FindFirstObjectByType<TutorialStageManager>();
            if (stageManager == null)
            {
                Debug.LogWarning("[PipePressurePuzzelPipesWireTool] TutorialStageManager not found; skipped turn-lock colliders.");
                return;
            }

            SerializedObject tsmSo = new SerializedObject(stageManager);
            WireLockBundle(tsmSo, "playerAPanelLock", "Player1_Panel", new[] { "VALVE", "PRESS", "FLOW" });
            WireLockBundle(tsmSo, "playerBPanelLock", "Player2_Panel", new[] { "GATE", "PUMP", "ROUTE" });
            tsmSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireLockBundle(
            SerializedObject tsmSo,
            string bundleProperty,
            string panelName,
            string[] inputNames)
        {
            SerializedProperty bundle = tsmSo.FindProperty(bundleProperty);
            SerializedProperty colliders = bundle.FindPropertyRelative("actionColliders");
            colliders.arraySize = inputNames.Length + 1;

            for (int i = 0; i < inputNames.Length; i++)
            {
                colliders.GetArrayElementAtIndex(i).objectReferenceValue =
                    GetProbeCollider($"{panelName}/Buttons/{inputNames[i]}");
            }

            Collider solveCollider = GameObject.Find($"{panelName}/Buttons/SolveButton")
                ?.GetComponentInChildren<Collider>(true);
            colliders.GetArrayElementAtIndex(inputNames.Length).objectReferenceValue = solveCollider;
        }

        private static Collider GetProbeCollider(string inputPath)
        {
            MultiDimensionSubjectCycler cycler = GameObject.Find(inputPath)
                ?.GetComponent<MultiDimensionSubjectCycler>();
            if (cycler == null)
            {
                return null;
            }

            SerializedObject so = new SerializedObject(cycler);
            return so.FindProperty("dimensionProbe").objectReferenceValue as Collider;
        }

        [MenuItem(RandomSolutionMenuPath)]
        public static void WireRandomSolutionAssigner()
        {
            TutorialStageManager stageManager = Object.FindFirstObjectByType<TutorialStageManager>();
            if (stageManager == null)
            {
                Debug.LogError("[PipePressurePuzzelPipesWireTool] TutorialStageManager not found in scene.");
                return;
            }

            RandomPuzzleSolutionAssigner assigner = stageManager.GetComponent<RandomPuzzleSolutionAssigner>();
            if (assigner == null)
            {
                assigner = Undo.AddComponent<RandomPuzzleSolutionAssigner>(stageManager.gameObject);
            }

            SerializedObject so = new SerializedObject(assigner);
            SerializedObject tsmSo = new SerializedObject(stageManager);
            so.FindProperty("playerAPuzzleManager").objectReferenceValue =
                tsmSo.FindProperty("playerAPuzzleManager").objectReferenceValue;
            so.FindProperty("playerBPuzzleManager").objectReferenceValue =
                tsmSo.FindProperty("playerBPuzzleManager").objectReferenceValue;
            so.FindProperty("enableRandomization").boolValue = true;
            so.FindProperty("useSeed").boolValue = false;
            so.FindProperty("seed").intValue = 0;
            so.FindProperty("logToConsole").boolValue = false;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[PipePressurePuzzelPipesWireTool] Wired RandomPuzzleSolutionAssigner (Phase 5).");
        }
    }
}
#endif
