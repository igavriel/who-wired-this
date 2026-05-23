#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    public static class PipePressurePhase4ValidationTool
    {
        private const string ValidationMenuRoot = "Who Wired This/Pipe Pressure/Validation/";
        private const string McpMenuRoot = "Who Wired This/Pipe Pressure/MCP/";
        private const string MenuPath = ValidationMenuRoot + "1. Phase 4 (Puzzle Pipes)";
        private const string McpMenuPath = McpMenuRoot + "1. Phase 4 (Puzzle Pipes)";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Phase 4", issues, report, showDialog: true);
        }

        [MenuItem(McpMenuPath)]
        public static void ValidateForMcp()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Phase 4", issues, report);
        }

        public static int RunValidation(out string report)
        {
            PipePressurePhase1ValidationTool.ResetPuzzlePipesSolveStateForValidationPublic();

            var sb = new StringBuilder();
            int issues = 0;

            issues += ValidateVisualizerSide(
                sb,
                "Player1_Panel",
                "Player2_Panel/DiagnosticPanel",
                new[] { "VALVE", "PRESS", "FLOW" });

            issues += ValidateVisualizerSide(
                sb,
                "Player2_Panel",
                "Player1_Panel/DiagnosticPanel",
                new[] { "GATE", "PUMP", "ROUTE" });

            issues += ValidateTutorialSceneHasNoVisualizer(sb);

            sb.AppendLine(issues == 0
                ? "=== Phase 4 validation: ALL CHECKS PASSED ==="
                : $"=== Phase 4 validation: {issues} issue(s) ===");

            report = sb.ToString();
            return issues;
        }

        private static int ValidateVisualizerSide(
            StringBuilder sb,
            string operatorPanelName,
            string partnerDiagnosticPath,
            string[] expectedInputLabels)
        {
            int issues = 0;
            sb.AppendLine($"--- {operatorPanelName} → {partnerDiagnosticPath} ---");

            GameObject operatorPanel = GameObject.Find(operatorPanelName);
            if (operatorPanel == null)
            {
                sb.AppendLine($"FAIL: Missing '{operatorPanelName}'");
                return 1;
            }

            SubmittedCombinationVisualizer visualizer = operatorPanel.GetComponent<SubmittedCombinationVisualizer>();
            if (visualizer == null)
            {
                sb.AppendLine($"FAIL: No SubmittedCombinationVisualizer on '{operatorPanelName}'");
                return 1;
            }

            SerializedObject vizSo = new SerializedObject(visualizer);
            Transform visualRoot = vizSo.FindProperty("visualRoot").objectReferenceValue as Transform;
            MultiDimensionPuzzleManager manager = vizSo.FindProperty("puzzleManager").objectReferenceValue
                as MultiDimensionPuzzleManager;

            if (manager == null)
            {
                sb.AppendLine("FAIL: puzzleManager not assigned");
                issues++;
            }

            GameObject partnerDiag = GameObject.Find(partnerDiagnosticPath);
            if (partnerDiag == null)
            {
                sb.AppendLine($"FAIL: Missing '{partnerDiagnosticPath}'");
                issues++;
            }
            else if (visualRoot == null || !visualRoot.IsChildOf(partnerDiag.transform))
            {
                sb.AppendLine("FAIL: visualRoot is not under partner DiagnosticPanel");
                issues++;
            }
            else
            {
                sb.AppendLine($"PASS: visualRoot '{visualRoot.name}' on partner DiagnosticPanel");
            }

            SerializedProperty slots = vizSo.FindProperty("slots");
            if (slots.arraySize != expectedInputLabels.Length)
            {
                sb.AppendLine($"FAIL: slots count {slots.arraySize} expected {expectedInputLabels.Length}");
                issues++;
            }

            if (visualRoot == null || manager == null || slots.arraySize != expectedInputLabels.Length)
            {
                return issues;
            }

            for (int slotIndex = 0; slotIndex < expectedInputLabels.Length; slotIndex++)
            {
                string expectedLabel = expectedInputLabels[slotIndex];
                if (slotIndex >= slots.arraySize)
                {
                    sb.AppendLine($"FAIL: missing slot for '{expectedLabel}'");
                    issues++;
                    continue;
                }

                SerializedProperty slot = slots.GetArrayElementAtIndex(slotIndex);
                string label = slot.FindPropertyRelative("label").stringValue;
                MultiDimension sourceInput = slot.FindPropertyRelative("sourceInput").objectReferenceValue
                    as MultiDimension;
                SerializedProperty visuals = slot.FindPropertyRelative("stateVisuals");

                if (sourceInput == null)
                {
                    sb.AppendLine($"FAIL: slot '{expectedLabel}' sourceInput not assigned");
                    issues++;
                    continue;
                }

                string sourceName = sourceInput.name;
                if (label != expectedLabel && sourceName != expectedLabel)
                {
                    sb.AppendLine(
                        $"FAIL: slot {slotIndex} label='{label}' source='{sourceName}' expected '{expectedLabel}'");
                    issues++;
                }
                else
                {
                    sb.AppendLine($"PASS: slot {slotIndex} '{expectedLabel}' wired to {sourceName}");
                }

                int expectedStates = sourceInput.SubjectCount;
                if (visuals.arraySize != expectedStates)
                {
                    sb.AppendLine(
                        $"FAIL: slot '{expectedLabel}' stateVisuals={visuals.arraySize} expected {expectedStates}");
                    issues++;
                    continue;
                }

                for (int v = 0; v < visuals.arraySize; v++)
                {
                    if (visuals.GetArrayElementAtIndex(v).objectReferenceValue == null)
                    {
                        sb.AppendLine($"FAIL: slot '{expectedLabel}' stateVisuals[{v}] is null");
                        issues++;
                    }
                }

                int slotIssues = ValidateSlotStateMapping(
                    sb, visualizer, slotIndex, expectedLabel, visuals, expectedInputLabels.Length);
                if (slotIssues == 0)
                {
                    sb.AppendLine($"PASS: slot '{expectedLabel}' states 0–{expectedStates - 1} map correctly");
                }

                issues += slotIssues;
            }

            int[] defaultIndices = new int[expectedInputLabels.Length];
            visualizer.ApplySubmittedIndices(defaultIndices);
            int activeStateMeshes = CountActiveAssignedStateVisuals(slots);
            if (activeStateMeshes != expectedInputLabels.Length)
            {
                sb.AppendLine(
                    $"FAIL: visual root has {activeStateMeshes} active state visuals (expected {expectedInputLabels.Length})");
                issues++;
            }
            else
            {
                sb.AppendLine("PASS: exactly one active state visual per slot after ApplySubmittedIndices");
            }

            return issues;
        }

        private static int ValidateSlotStateMapping(
            StringBuilder sb,
            SubmittedCombinationVisualizer visualizer,
            int slotIndex,
            string slotLabel,
            SerializedProperty visuals,
            int slotCount)
        {
            int issues = 0;
            int stateCount = visuals.arraySize;

            for (int state = 0; state < stateCount; state++)
            {
                int[] indices = new int[slotCount];
                indices[slotIndex] = state;
                visualizer.ApplySubmittedIndices(indices);

                int activeCount = 0;
                int activeIndex = -1;
                for (int v = 0; v < stateCount; v++)
                {
                    GameObject visual = visuals.GetArrayElementAtIndex(v).objectReferenceValue as GameObject;
                    if (visual != null && visual.activeSelf)
                    {
                        activeCount++;
                        activeIndex = v;
                    }
                }

                if (activeCount != 1)
                {
                    sb.AppendLine(
                        $"FAIL: slot '{slotLabel}' state {state} has {activeCount} active visuals (expected 1)");
                    issues++;
                    continue;
                }

                if (activeIndex != state)
                {
                    GameObject activeVisual = visuals.GetArrayElementAtIndex(activeIndex).objectReferenceValue as GameObject;
                    GameObject expectedVisual = visuals.GetArrayElementAtIndex(state).objectReferenceValue as GameObject;
                    sb.AppendLine(
                        $"FAIL: slot '{slotLabel}' state {state} active visual '{activeVisual?.name}' " +
                        $"(expected '{expectedVisual?.name}')");
                    issues++;
                }
            }

            return issues;
        }

        private static int CountActiveAssignedStateVisuals(SerializedProperty slots)
        {
            int count = 0;
            for (int s = 0; s < slots.arraySize; s++)
            {
                SerializedProperty visuals = slots.GetArrayElementAtIndex(s).FindPropertyRelative("stateVisuals");
                for (int v = 0; v < visuals.arraySize; v++)
                {
                    GameObject visual = visuals.GetArrayElementAtIndex(v).objectReferenceValue as GameObject;
                    if (visual != null && visual.activeSelf)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static int ValidateTutorialSceneHasNoVisualizer(StringBuilder sb)
        {
            const string tutorialPath = "Assets/Scenes/Tutorial.unity";
            string text = System.IO.File.ReadAllText(tutorialPath);
            if (text.Contains("SubmittedCombinationVisualizer"))
            {
                sb.AppendLine("FAIL: Tutorial.unity references SubmittedCombinationVisualizer");
                return 1;
            }

            sb.AppendLine("PASS: Tutorial.unity has no SubmittedCombinationVisualizer");
            return 0;
        }
    }
}
#endif
