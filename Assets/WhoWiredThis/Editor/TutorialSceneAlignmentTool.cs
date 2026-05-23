#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Aligns Tutorial.unity for the two-input tutorial: Left Knob (LOW/MID/HIGH), Right Slider (NEG/OFF/POS),
    /// Bulls-and-Cows diagnostics (MultiDimensionDiagnosticAdapter), and shared history wiring.
    /// Does not modify Puzzle Pipes.unity.
    /// </summary>
    public static class TutorialSceneAlignmentTool
    {
        private const string TutorialScenePath = "Assets/Scenes/Tutorial.unity";
        private const string MenuPath = "Who Wired This/Tutorial/Align Tutorial Scene";

        private const string HistoryHeaderLine = " # | SIDE | INPUT       | STATUS";
        private const string HistorySeparatorLine = "===+======+=============+========";

        private static readonly string[] LeftKnobDisplayNames = { "LOW", "MID", "HIGH" };
        private static readonly string[] RightSliderDisplayNames = { "NEG", "OFF", "POS" };
        private static readonly string[] PanelFocusLabels = { "Left Knob", "Right Slider" };

        [MenuItem(MenuPath)]
        public static void AlignTutorialScene()
        {
            if (!EnsureTutorialSceneActive())
            {
                return;
            }

            int issues = 0;
            issues += AlignPanel("Player1_Panel", isPlayerA: true);
            issues += AlignPanel("Player2_Panel", isPlayerA: false);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log(issues == 0
                ? "[TutorialSceneAlignmentTool] Tutorial.unity aligned (Bulls-and-Cows tutorial mode)."
                : $"[TutorialSceneAlignmentTool] Finished with {issues} warning(s). See console.");
        }

        private static bool EnsureTutorialSceneActive()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == TutorialScenePath)
            {
                return true;
            }

            if (!EditorUtility.DisplayDialog(
                    "Align Tutorial Scene",
                    "Open Tutorial.unity first. Open it now?",
                    "Open scene",
                    "Cancel"))
            {
                return false;
            }

            EditorSceneManager.OpenScene(TutorialScenePath);
            return true;
        }

        private static int AlignPanel(string panelName, bool isPlayerA)
        {
            int issues = 0;
            GameObject panel = GameObject.Find(panelName);
            if (panel == null)
            {
                Debug.LogError($"[TutorialSceneAlignmentTool] Missing '{panelName}'.");
                return 1;
            }

            MultiDimensionPuzzleManager puzzleManager = GameObject.Find($"{panelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzleManager>();
            if (puzzleManager == null)
            {
                Debug.LogError($"[TutorialSceneAlignmentTool] Missing PuzzleManager on '{panelName}'.");
                return 1;
            }

            MultiDimension[] dimensions = ResolvePuzzleDimensions(puzzleManager, PanelFocusLabels.Length);
            if (dimensions == null)
            {
                issues++;
            }
            else
            {
                for (int i = 0; i < dimensions.Length; i++)
                {
                    if (dimensions[i] == null)
                    {
                        Debug.LogWarning(
                            $"[TutorialSceneAlignmentTool] '{panelName}' puzzle element {i} is not assigned.");
                        issues++;
                        continue;
                    }

                    ApplyTutorialInputLabels(dimensions[i], i);
                }
            }

            issues += WireHistoryAdapter(panel, puzzleManager, dimensions);
            issues += WireBullsCowsDiagnostic(panel, puzzleManager, isPlayerA);
            issues += UpdatePanelFocusLabels(panel, PanelFocusLabels);

            return issues;
        }

        private static MultiDimension[] ResolvePuzzleDimensions(MultiDimensionPuzzleManager manager, int expectedCount)
        {
            int count = manager.PuzzleElementCount;
            if (count < expectedCount)
            {
                Debug.LogWarning(
                    $"[TutorialSceneAlignmentTool] '{manager.name}' has {count} elements; expected {expectedCount}.");
            }

            int n = Mathf.Min(expectedCount, count);
            var dimensions = new MultiDimension[expectedCount];
            for (int i = 0; i < n; i++)
            {
                if (manager.TryGetPuzzleElement(i, out MultiDimension element, out _))
                {
                    dimensions[i] = element;
                }
            }

            return dimensions;
        }

        private static void ApplyTutorialInputLabels(MultiDimension dimension, int slotIndex)
        {
            string[] labels = slotIndex == 0 ? LeftKnobDisplayNames : RightSliderDisplayNames;
            SerializedObject mdSo = new SerializedObject(dimension);
            SerializedProperty subjects = mdSo.FindProperty("subjects");
            int n = Mathf.Min(subjects.arraySize, labels.Length);
            for (int i = 0; i < n; i++)
            {
                subjects.GetArrayElementAtIndex(i).FindPropertyRelative("displayName").stringValue = labels[i];
            }

            mdSo.ApplyModifiedPropertiesWithoutUndo();
            dimension.ApplyConfiguration();
        }

        private static int WireHistoryAdapter(
            GameObject panel,
            MultiDimensionPuzzleManager puzzleManager,
            MultiDimension[] dimensions)
        {
            MultiDimensionHistoryAdapter adapter = panel.GetComponent<MultiDimensionHistoryAdapter>();
            if (adapter == null)
            {
                Debug.LogWarning($"[TutorialSceneAlignmentTool] No MultiDimensionHistoryAdapter on '{panel.name}'.");
                return 1;
            }

            SerializedObject adapterSo = new SerializedObject(adapter);
            adapterSo.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;
            adapterSo.FindProperty("solvedStatus").stringValue = "CALIBRATED";
            adapterSo.FindProperty("unsolvedStatus").stringValue = "UNSTABLE";

            SerializedProperty order = adapterSo.FindProperty("inputOrder");
            if (dimensions != null)
            {
                order.arraySize = dimensions.Length;
                for (int i = 0; i < dimensions.Length; i++)
                {
                    order.GetArrayElementAtIndex(i).objectReferenceValue = dimensions[i];
                }
            }

            adapterSo.ApplyModifiedPropertiesWithoutUndo();

            HistoryBoardController board = adapterSo.FindProperty("historyBoard").objectReferenceValue
                as HistoryBoardController;
            if (board == null)
            {
                board = panel.GetComponentInChildren<HistoryBoardController>(true);
            }

            if (board == null)
            {
                Debug.LogWarning($"[TutorialSceneAlignmentTool] No HistoryBoardController under '{panel.name}'.");
                return 1;
            }

            SerializedObject boardSo = new SerializedObject(board);
            boardSo.FindProperty("headerLine").stringValue = HistoryHeaderLine;
            boardSo.FindProperty("separatorLine").stringValue = HistorySeparatorLine;
            boardSo.ApplyModifiedPropertiesWithoutUndo();
            board.Render();
            return 0;
        }

        private static int WireBullsCowsDiagnostic(
            GameObject panel,
            MultiDimensionPuzzleManager puzzleManager,
            bool isPlayerA)
        {
            RemovePipeDiagnosticAdapter(panel);

            MultiDimensionDiagnosticAdapter adapter = panel.GetComponent<MultiDimensionDiagnosticAdapter>();
            if (adapter == null)
            {
                adapter = panel.AddComponent<MultiDimensionDiagnosticAdapter>();
            }

            DiagnosticDisplayController display = ResolvePartnerDiagnosticDisplay(panel, adapter);
            if (display == null)
            {
                Debug.LogWarning(
                    $"[TutorialSceneAlignmentTool] No DiagnosticDisplayController for '{panel.name}' (partner panel).");
                return 1;
            }

            SerializedObject adapterSo = new SerializedObject(adapter);
            adapterSo.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;
            adapterSo.FindProperty("diagnosticDisplay").objectReferenceValue = display;
            adapterSo.FindProperty("metric1Label").stringValue = "SETTINGS OK";
            adapterSo.FindProperty("metric2Label").stringValue = "PLACES OK";
            adapterSo.FindProperty("solvedMessage").stringValue = isPlayerA ? "A-SIDE CALIBRATED" : "B-SIDE CALIBRATED";
            adapterSo.FindProperty("perfectButMisalignedMessage").stringValue =
                "CORRECT SETTINGS,\nWRONG ORDER.";
            adapterSo.FindProperty("noMatchMessage").stringValue =
                "NO MATCHING SETTINGS.\nTRY DIFFERENT SETTINGS.";
            adapterSo.FindProperty("partialMessage").stringValue =
                "PARTIAL MATCH.\nKEEP ONE. CHANGE ONE.";
            adapterSo.FindProperty("updateContinuously").boolValue = false;
            adapterSo.ApplyModifiedPropertiesWithoutUndo();

            adapter.enabled = true;
            return 0;
        }

        private static void RemovePipeDiagnosticAdapter(GameObject panel)
        {
            ComponentDiagnosticAdapter pipeAdapter = panel.GetComponent<ComponentDiagnosticAdapter>();
            if (pipeAdapter == null)
            {
                return;
            }

            Object.DestroyImmediate(pipeAdapter);
        }

        private static DiagnosticDisplayController ResolvePartnerDiagnosticDisplay(
            GameObject operatorPanel,
            MultiDimensionDiagnosticAdapter adapter)
        {
            SerializedObject adapterSo = new SerializedObject(adapter);
            DiagnosticDisplayController display = adapterSo.FindProperty("diagnosticDisplay").objectReferenceValue
                as DiagnosticDisplayController;
            if (display != null)
            {
                return display;
            }

            string partnerPanelName = operatorPanel.name == "Player1_Panel" ? "Player2_Panel" : "Player1_Panel";
            GameObject partnerPanel = GameObject.Find(partnerPanelName);
            if (partnerPanel != null)
            {
                display = partnerPanel.GetComponentInChildren<DiagnosticDisplayController>(true);
            }

            return display;
        }

        private static int UpdatePanelFocusLabels(GameObject panel, string[] inputLabels)
        {
            PanelFocusController[] controllers = panel.GetComponentsInChildren<PanelFocusController>(true);
            PanelFocusController board = null;
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null && controllers[i].name == "Board")
                {
                    board = controllers[i];
                    break;
                }
            }

            if (board == null)
            {
                Debug.LogWarning($"[TutorialSceneAlignmentTool] No Board PanelFocusController under '{panel.name}'.");
                return 1;
            }

            SerializedObject pfcSo = new SerializedObject(board);
            SerializedProperty buttons = pfcSo.FindProperty("interactableButtons");
            int n = Mathf.Min(buttons.arraySize, inputLabels.Length);
            for (int i = 0; i < n; i++)
            {
                buttons.GetArrayElementAtIndex(i).FindPropertyRelative("label").stringValue = inputLabels[i];
            }

            pfcSo.ApplyModifiedPropertiesWithoutUndo();
            return 0;
        }
    }
}
#endif
