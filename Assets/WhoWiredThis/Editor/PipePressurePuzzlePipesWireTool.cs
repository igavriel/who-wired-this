#if UNITY_EDITOR
using System.Collections.Generic;
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
    /// One-shot wiring for Puzzle Pipes: 3×4-state inputs, puzzle managers, history, panel focus, turn locks.
    /// Menu: Who Wired This / Pipe Pressure / Wire Puzzle Pipes Scene
    /// </summary>
    public static class PipePressurePuzzlePipesWireTool
    {
        private const string MenuPath = "Who Wired This/Pipe Pressure/Wire Puzzle Pipes Scene";
        private const string HistoryHeadersMenuPath = "Who Wired This/Pipe Pressure/Apply Puzzle Pipes History Headers (Phase 2)";
        private const string ComponentDiagnosticMenuPath =
            "Who Wired This/Pipe Pressure/Wire Puzzle Pipes Component Diagnostic (Phase 3)";
        private const string ResultVisualizerMenuPath =
            "Who Wired This/Pipe Pressure/Wire Puzzle Pipes Result Display Bridge (Phase 4)";
        private const string RandomSolutionMenuPath =
            "Who Wired This/Pipe Pressure/Wire Random Solution Assigner (Phase 5)";

        private const string ActiveScenePath = "Assets/Scenes/Game/Puzzle Pipes.unity";
        private const string V1ScenePath = "Assets/Scenes/Game/OLD/Puzzle Pipes V1.unity";

        private const string ValveDisplayPrefabPath =
            "Assets/WhoWiredThis/Prefabs/MultiDimension/MultiDimension_ValveV2_4State.prefab";
        private const string FaderDisplayPrefabPath =
            "Assets/WhoWiredThis/Prefabs/MultiDimension/MultiDimension_Fader_4State.prefab";
        private const string FlowDisplayPrefabPath =
            "Assets/WhoWiredThis/Prefabs/MultiDimension/MultiDimension_ButtonText_4State.prefab";

        /// <summary>INPUT column width for three 5-char tokens plus two spaces (17).</summary>
        public const string PuzzlePipesHistoryHeaderLine = " # | SIDE | INPUT             | STATUS";

        public const string PuzzlePipesHistorySeparatorLine = "===+======+===================+========";

        [MenuItem(HistoryHeadersMenuPath)]
        public static void ApplyPuzzlePipesHistoryHeaders()
        {
            ApplyHistoryHeaders("Player1_Panel/HistoryPanel");
            ApplyHistoryHeaders("Player2_Panel/HistoryPanel");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log(
                "[PipePressurePuzzlePipesWireTool] Applied Phase 2 history header/separator to both Puzzle Pipes HistoryPanels.");
        }

        private static void ApplyHistoryHeaders(string historyPanelPath)
        {
            HistoryBoardController board = GameObject.Find(historyPanelPath)?.GetComponent<HistoryBoardController>();
            if (board == null)
            {
                Debug.LogError($"[PipePressurePuzzlePipesWireTool] Missing HistoryBoardController at '{historyPanelPath}'.");
                return;
            }

            SerializedObject so = new SerializedObject(board);
            so.FindProperty("headerLine").stringValue = PuzzlePipesHistoryHeaderLine;
            so.FindProperty("separatorLine").stringValue = PuzzlePipesHistorySeparatorLine;
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
            Debug.Log("[PipePressurePuzzlePipesWireTool] Wired Phase 3 component diagnostic on Puzzle Pipes.");
        }

        private static void WireComponentDiagnosticPanel(
            string panelName,
            string[] inputNames,
            (ComponentDiagnosticType type, string correct, string tooLow, string tooHigh, string mismatch)[] defs)
        {
            GameObject panel = GameObject.Find(panelName);
            if (panel == null)
            {
                Debug.LogError($"[PipePressurePuzzlePipesWireTool] Missing '{panelName}'.");
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

            MultiDimensionPuzzleManager puzzleManager = GameObject.Find($"{panelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzleManager>();

            if (puzzleManager == null || display == null)
            {
                Debug.LogError(
                    $"[PipePressurePuzzlePipesWireTool] Missing puzzleManager or diagnosticDisplay for '{panelName}'.");
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

        public static void WireResultVisualizerBatch()
        {
            int issues = 0;
            issues += WireResultVisualizerInScene(ActiveScenePath);
            issues += WireResultVisualizerInScene(V1ScenePath);
            if (issues > 0)
            {
                Debug.LogError(
                    $"[PipePressurePuzzlePipesWireTool] Batch Phase 4 wire finished with {issues} issue(s).");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[PipePressurePuzzlePipesWireTool] Batch Phase 4 result display bridge wire complete.");
            EditorApplication.Exit(0);
        }

        [MenuItem(ResultVisualizerMenuPath)]
        public static void WireResultVisualizer()
        {
            if (WireResultVisualizerActiveScene() > 0)
            {
                Debug.LogWarning("[PipePressurePuzzlePipesWireTool] Phase 4 wire finished with issue(s). See console.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[PipePressurePuzzlePipesWireTool] Wired Phase 4 result display bridge on Puzzle Pipes.");
        }

        private static int WireResultVisualizerInScene(string scenePath)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError($"[PipePressurePuzzlePipesWireTool] Scene not found: '{scenePath}'.");
                return 1;
            }

            EditorSceneManager.OpenScene(scenePath);
            int issues = WireResultVisualizerActiveScene();
            if (issues == 0)
            {
                EditorSceneManager.SaveOpenScenes();
            }

            return issues;
        }

        private static int WireResultVisualizerActiveScene()
        {
            int issues = 0;
            issues += WireResultVisualizerSide(
                "Player1_Panel",
                "Player2_Panel",
                new[] { "VALVE", "PRESS", "FLOW" },
                AllowedPlayerTag.Player_B);

            issues += WireResultVisualizerSide(
                "Player2_Panel",
                "Player1_Panel",
                new[] { "GATE", "PUMP", "ROUTE" },
                AllowedPlayerTag.Player_A);
            return issues;
        }

        private static int WireResultVisualizerSide(
            string operatorPanelName,
            string partnerPanelName,
            string[] inputNames,
            AllowedPlayerTag visibleToPlayer)
        {
            int issues = 0;
            GameObject operatorPanel = GameObject.Find(operatorPanelName);
            GameObject partnerPanel = GameObject.Find(partnerPanelName);
            if (operatorPanel == null || partnerPanel == null)
            {
                Debug.LogError(
                    $"[PipePressurePuzzlePipesWireTool] Missing panel '{operatorPanelName}' or '{partnerPanelName}'.");
                return 1;
            }

            DiagnosticDisplayController partnerDisplay =
                partnerPanel.GetComponentInChildren<DiagnosticDisplayController>(true);
            if (partnerDisplay == null)
            {
                Debug.LogError(
                    $"[PipePressurePuzzlePipesWireTool] No DiagnosticDisplayController under '{partnerPanelName}'.");
                return 1;
            }

            Transform rigRoot = EnsureResultVisualRig(partnerDisplay.transform);
            RemoveLegacyResultVisualGroups(rigRoot);

            MultiDimension[] displays = EnsureDisplayMultiDimensions(rigRoot, inputNames.Length);
            if (displays.Length < inputNames.Length)
            {
                Debug.LogError(
                    $"[PipePressurePuzzlePipesWireTool] Expected {inputNames.Length} display MultiDimensions under '{rigRoot.name}' on '{partnerPanelName}'.");
                issues++;
            }

            RemoveSubmittedCombinationVisualizer(operatorPanel);

            SubmittedCombinationMultiDimensionBridge bridge =
                operatorPanel.GetComponent<SubmittedCombinationMultiDimensionBridge>();
            if (bridge == null)
            {
                bridge = Undo.AddComponent<SubmittedCombinationMultiDimensionBridge>(operatorPanel);
            }

            MultiDimensionPuzzleManager puzzleManager = GameObject.Find($"{operatorPanelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzleManager>();

            SerializedObject bridgeSo = new SerializedObject(bridge);
            bridgeSo.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;
            bridgeSo.FindProperty("visibleToPlayer").enumValueIndex = (int)visibleToPlayer;

            SerializedProperty slotsProp = bridgeSo.FindProperty("slots");
            slotsProp.arraySize = inputNames.Length;
            for (int i = 0; i < inputNames.Length; i++)
            {
                MultiDimension sourceInput = GameObject.Find($"{operatorPanelName}/Buttons/{inputNames[i]}")
                    ?.GetComponent<MultiDimension>();

                SerializedProperty slot = slotsProp.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("label").stringValue = inputNames[i];
                slot.FindPropertyRelative("sourceInput").objectReferenceValue = sourceInput;
                slot.FindPropertyRelative("display").objectReferenceValue =
                    i < displays.Length ? displays[i] : null;

                if (sourceInput == null || (i < displays.Length && displays[i] == null))
                {
                    Debug.LogError(
                        $"[PipePressurePuzzlePipesWireTool] Missing source or display for slot '{inputNames[i]}' on '{operatorPanelName}'.");
                    issues++;
                }
            }

            bridgeSo.ApplyModifiedPropertiesWithoutUndo();

            int[] defaultIndices = new int[inputNames.Length];
            bridge.ApplySubmittedIndices(defaultIndices);
            return issues;
        }

        private static void RemoveSubmittedCombinationVisualizer(GameObject operatorPanel)
        {
            SubmittedCombinationVisualizer legacy = operatorPanel.GetComponent<SubmittedCombinationVisualizer>();
            if (legacy != null)
            {
                Undo.DestroyObjectImmediate(legacy);
            }
        }

        private static void RemoveLegacyResultVisualGroups(Transform rigRoot)
        {
            for (int i = rigRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = rigRoot.GetChild(i);
                if (child.GetComponentInChildren<MultiDimension>(true) != null)
                {
                    continue;
                }

                if (child.name.EndsWith("Group", System.StringComparison.Ordinal) ||
                    child.name.StartsWith("State", System.StringComparison.Ordinal))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }

        private static MultiDimension[] EnsureDisplayMultiDimensions(Transform rigRoot, int expectedCount)
        {
            var displays = CollectDisplayMultiDimensions(rigRoot);
            if (displays.Count >= expectedCount)
            {
                return displays.GetRange(0, expectedCount).ToArray();
            }

            string[] prefabPaths =
            {
                ValveDisplayPrefabPath,
                FaderDisplayPrefabPath,
                FlowDisplayPrefabPath
            };

            float[] xOffsets = { -0.45f, 0f, 0.45f };
            for (int i = displays.Count; i < expectedCount; i++)
            {
                string prefabPath = i < prefabPaths.Length ? prefabPaths[i] : FlowDisplayPrefabPath;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"[PipePressurePuzzlePipesWireTool] Missing display prefab at '{prefabPath}'.");
                    continue;
                }

                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, rigRoot) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                Undo.RegisterCreatedObjectUndo(instance, "Create result display MultiDimension");
                Transform t = instance.transform;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                t.localPosition = new Vector3(i < xOffsets.Length ? xOffsets[i] : 0f, 0f, 0f);

                MultiDimension md = instance.GetComponent<MultiDimension>() ??
                                    instance.GetComponentInChildren<MultiDimension>(true);
                if (md != null)
                {
                    displays.Add(md);
                }
            }

            return displays.Count >= expectedCount
                ? displays.GetRange(0, expectedCount).ToArray()
                : displays.ToArray();
        }

        private static List<MultiDimension> CollectDisplayMultiDimensions(Transform rigRoot)
        {
            var displays = new List<MultiDimension>();
            for (int i = 0; i < rigRoot.childCount; i++)
            {
                Transform child = rigRoot.GetChild(i);
                MultiDimension md = child.GetComponent<MultiDimension>();
                if (md == null)
                {
                    md = child.GetComponentInChildren<MultiDimension>(true);
                }

                if (md != null)
                {
                    displays.Add(md);
                }
            }

            return displays;
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
            Debug.Log("[PipePressurePuzzlePipesWireTool] Wired Player1_Panel and Player2_Panel (3 inputs each).");
        }

        private static void WirePanel(string panelName, string[] inputNames, int[] correctIndices)
        {
            if (inputNames.Length != correctIndices.Length)
            {
                Debug.LogError("[PipePressurePuzzlePipesWireTool] inputNames and correctIndices length mismatch.");
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
                    Debug.LogError($"[PipePressurePuzzlePipesWireTool] Missing GameObject at '{path}'.");
                    return;
                }

                dimensions[i] = inputGo.GetComponent<MultiDimension>();
                cyclers[i] = inputGo.GetComponent<MultiDimensionSubjectCycler>();
                if (dimensions[i] == null || cyclers[i] == null)
                {
                    Debug.LogError($"[PipePressurePuzzlePipesWireTool] Missing MultiDimension/cycler on '{path}'.");
                    return;
                }
            }

            MultiDimensionPuzzleManager puzzleManager = GameObject.Find($"{panelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzleManager>();
            if (puzzleManager == null)
            {
                Debug.LogError($"[PipePressurePuzzlePipesWireTool] Missing PuzzleManager on '{panelName}'.");
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
                Debug.LogWarning("[PipePressurePuzzlePipesWireTool] TutorialStageManager not found; skipped turn-lock colliders.");
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
                Debug.LogError("[PipePressurePuzzlePipesWireTool] TutorialStageManager not found in scene.");
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
            Debug.Log("[PipePressurePuzzlePipesWireTool] Wired RandomPuzzleSolutionAssigner (Phase 5).");
        }
    }
}
#endif
