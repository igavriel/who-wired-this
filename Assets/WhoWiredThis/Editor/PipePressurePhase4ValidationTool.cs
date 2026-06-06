#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using WhoWiredThis.Enums;
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

        private const string TutorialScenePath = "Assets/Scenes/Game/Tutorial.unity";
        private const string PuzzlePipesScenePath = "Assets/Scenes/Game/Puzzle Pipes.unity";

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

            issues += ValidateBridgeSide(
                sb,
                "Player1_Panel",
                "Player2_Panel/DiagnosticPanel",
                new[] { "VALVE", "PRESS", "FLOW" },
                AllowedPlayerTag.Player_B);

            issues += ValidateBridgeSide(
                sb,
                "Player2_Panel",
                "Player1_Panel/DiagnosticPanel",
                new[] { "GATE", "PUMP", "ROUTE" },
                AllowedPlayerTag.Player_A);

            issues += ValidateSceneHasNoVisualizer(sb, TutorialScenePath, "Tutorial.unity");
            issues += ValidateSceneHasNoVisualizer(sb, PuzzlePipesScenePath, "Puzzle Pipes.unity");

            sb.AppendLine(issues == 0
                ? "=== Phase 4 validation: ALL CHECKS PASSED ==="
                : $"=== Phase 4 validation: {issues} issue(s) ===");

            report = sb.ToString();
            return issues;
        }

        private static int ValidateBridgeSide(
            StringBuilder sb,
            string operatorPanelName,
            string partnerDiagnosticPath,
            string[] expectedInputLabels,
            AllowedPlayerTag expectedVisibleToPlayer)
        {
            int issues = 0;
            sb.AppendLine($"--- {operatorPanelName} → {partnerDiagnosticPath} ---");

            GameObject operatorPanel = GameObject.Find(operatorPanelName);
            if (operatorPanel == null)
            {
                sb.AppendLine($"FAIL: Missing '{operatorPanelName}'");
                return 1;
            }

            if (operatorPanel.GetComponent<SubmittedCombinationVisualizer>() != null)
            {
                sb.AppendLine($"FAIL: Legacy SubmittedCombinationVisualizer still on '{operatorPanelName}'");
                issues++;
            }

            SubmittedCombinationMultiDimensionBridge bridge =
                operatorPanel.GetComponent<SubmittedCombinationMultiDimensionBridge>();
            if (bridge == null)
            {
                sb.AppendLine($"FAIL: No SubmittedCombinationMultiDimensionBridge on '{operatorPanelName}'");
                return issues + 1;
            }

            SerializedObject bridgeSo = new SerializedObject(bridge);
            MultiDimensionPuzzleManager manager = bridgeSo.FindProperty("puzzleManager").objectReferenceValue
                as MultiDimensionPuzzleManager;
            AllowedPlayerTag visibleToPlayer =
                (AllowedPlayerTag)bridgeSo.FindProperty("visibleToPlayer").enumValueIndex;

            if (manager == null)
            {
                sb.AppendLine("FAIL: puzzleManager not assigned");
                issues++;
            }

            if (visibleToPlayer != expectedVisibleToPlayer)
            {
                sb.AppendLine(
                    $"FAIL: visibleToPlayer={visibleToPlayer} expected {expectedVisibleToPlayer}");
                issues++;
            }
            else
            {
                sb.AppendLine($"PASS: visibleToPlayer={visibleToPlayer}");
            }

            GameObject partnerDiag = GameObject.Find(partnerDiagnosticPath);
            if (partnerDiag == null)
            {
                sb.AppendLine($"FAIL: Missing '{partnerDiagnosticPath}'");
                issues++;
            }

            SerializedProperty slots = bridgeSo.FindProperty("slots");
            if (slots.arraySize != expectedInputLabels.Length)
            {
                sb.AppendLine($"FAIL: slots count {slots.arraySize} expected {expectedInputLabels.Length}");
                issues++;
            }

            if (manager == null || slots.arraySize != expectedInputLabels.Length)
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
                MultiDimension display = slot.FindPropertyRelative("display").objectReferenceValue
                    as MultiDimension;

                if (sourceInput == null)
                {
                    sb.AppendLine($"FAIL: slot '{expectedLabel}' sourceInput not assigned");
                    issues++;
                    continue;
                }

                if (display == null)
                {
                    sb.AppendLine($"FAIL: slot '{expectedLabel}' display not assigned");
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

                if (partnerDiag != null &&
                    !display.transform.IsChildOf(partnerDiag.transform))
                {
                    sb.AppendLine(
                        $"FAIL: slot '{expectedLabel}' display '{display.name}' is not under partner DiagnosticPanel");
                    issues++;
                }

                int slotIssues = ValidateSlotDisplayMapping(
                    sb, bridge, slotIndex, expectedLabel, display, expectedInputLabels.Length);
                if (slotIssues == 0)
                {
                    sb.AppendLine($"PASS: slot '{expectedLabel}' display indices map correctly");
                }

                issues += slotIssues;
            }

            int[] defaultIndices = new int[expectedInputLabels.Length];
            bridge.ApplySubmittedIndices(defaultIndices);
            sb.AppendLine("PASS: ApplySubmittedIndices(default) completed without error");

            return issues;
        }

        private static int ValidateSlotDisplayMapping(
            StringBuilder sb,
            SubmittedCombinationMultiDimensionBridge bridge,
            int slotIndex,
            string slotLabel,
            MultiDimension display,
            int slotCount)
        {
            int issues = 0;
            int stateCount = display.SubjectCount;
            if (stateCount <= 0)
            {
                sb.AppendLine($"FAIL: slot '{slotLabel}' display '{display.name}' has no subjects");
                return 1;
            }

            for (int state = 0; state < stateCount; state++)
            {
                int[] indices = new int[slotCount];
                indices[slotIndex] = state;
                bridge.ApplySubmittedIndices(indices);

                int currentIndex = display.GetCurrentIndexForSolutionCheck();
                if (currentIndex != state)
                {
                    sb.AppendLine(
                        $"FAIL: slot '{slotLabel}' state {state} display index {currentIndex} (expected {state})");
                    issues++;
                }
            }

            return issues;
        }

        private static int ValidateSceneHasNoVisualizer(StringBuilder sb, string scenePath, string sceneLabel)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                sb.AppendLine($"WARN: {sceneLabel} not found at '{scenePath}'");
                return 0;
            }

            string text = System.IO.File.ReadAllText(scenePath);
            if (text.Contains("SubmittedCombinationVisualizer"))
            {
                sb.AppendLine($"FAIL: {sceneLabel} references SubmittedCombinationVisualizer");
                return 1;
            }

            sb.AppendLine($"PASS: {sceneLabel} has no SubmittedCombinationVisualizer");
            return 0;
        }
    }
}
#endif
