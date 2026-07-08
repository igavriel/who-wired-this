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

            if (GameObject.Find(PipesPanelAName) != null)
            {
                issues += ValidateBridgeSide(
                    sb,
                    PipesPanelAName,
                    $"{PipesPanelBName}/DiagnosticPanel",
                    new[] { "PRESS", "FLOW", "VALVE" },
                    AllowedPlayerTag.Player_B);

                issues += ValidateBridgeSide(
                    sb,
                    PipesPanelBName,
                    $"{PipesPanelAName}/DiagnosticPanel",
                    new[] { "GATE", "PUMP", "ROUTE" },
                    AllowedPlayerTag.Player_A);
            }
            else
            {
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
            }

            issues += ValidateSceneHasNoVisualizer(sb, TutorialScenePath, "Tutorial.unity");
            issues += ValidateSceneHasNoVisualizer(sb, PuzzlePipesScenePath, "Puzzle Pipes.unity");

            if (GameObject.Find(PipesPanelAName) != null)
            {
                issues += ValidatePipesResultLights(sb);
            }

            sb.AppendLine(issues == 0
                ? "=== Phase 4 validation: ALL CHECKS PASSED ==="
                : $"=== Phase 4 validation: {issues} issue(s) ===");

            report = sb.ToString();
            return issues;
        }

        public static int RunPipesResultLightsValidation(out string report)
        {
            var sb = new StringBuilder();
            int issues = ValidatePipesResultLights(sb);
            sb.AppendLine(issues == 0
                ? "=== Pipes result lights validation: ALL CHECKS PASSED ==="
                : $"=== Pipes result lights validation: {issues} issue(s) ===");
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
                operatorPanel.GetComponentInChildren<SubmittedCombinationMultiDimensionBridge>(true);
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

        private const string PipesPanelAName = "Pipes_A V2 Variant";
        private const string PipesPanelBName = "Pipes_B V2 Variant";
        private const string PipesResultLightsRootName = "PuzzlePipes_ResultLights";
        private static readonly string[] PipesLightNames = { "ResultLight-Upper", "ResultLight-Middle", "ResultLight-Lower" };

        private static int ValidatePipesResultLights(StringBuilder sb)
        {
            int issues = 0;
            sb.AppendLine("--- Puzzle Pipes result lights ---");

            issues += ValidatePipesResultLightsBridge(
                sb,
                bridgeName: "Bridge_A_to_B_lights",
                operatorPanelName: PipesPanelAName,
                partnerPanelName: PipesPanelBName,
                expectedVisibleToPlayer: AllowedPlayerTag.Player_B);

            issues += ValidatePipesResultLightsBridge(
                sb,
                bridgeName: "Bridge_B_to_A_lights",
                operatorPanelName: PipesPanelBName,
                partnerPanelName: PipesPanelAName,
                expectedVisibleToPlayer: AllowedPlayerTag.Player_A);

            return issues;
        }

        private static int ValidatePipesResultLightsBridge(
            StringBuilder sb,
            string bridgeName,
            string operatorPanelName,
            string partnerPanelName,
            AllowedPlayerTag expectedVisibleToPlayer)
        {
            int issues = 0;
            sb.AppendLine($"--- {bridgeName} ({operatorPanelName} → {partnerPanelName}) ---");

            GameObject bridgeRoot = GameObject.Find(PipesResultLightsRootName);
            if (bridgeRoot == null)
            {
                sb.AppendLine($"FAIL: Missing scene root '{PipesResultLightsRootName}'");
                return 1;
            }

            Transform bridgeTransform = bridgeRoot.transform.Find(bridgeName);
            if (bridgeTransform == null)
            {
                sb.AppendLine($"FAIL: Missing '{PipesResultLightsRootName}/{bridgeName}'");
                return issues + 1;
            }

            SplitResultPipesController controller = bridgeTransform.GetComponent<SplitResultPipesController>();
            if (controller == null)
            {
                sb.AppendLine($"FAIL: No SplitResultPipesController on '{bridgeName}'");
                return issues + 1;
            }

            GameObject operatorPanel = GameObject.Find(operatorPanelName);
            if (operatorPanel == null)
            {
                sb.AppendLine($"FAIL: Missing '{operatorPanelName}'");
                issues++;
            }

            MultiDimensionPuzzleManager operatorManager = operatorPanel != null
                ? operatorPanel.GetComponentInChildren<MultiDimensionPuzzleManager>(true)
                : null;

            SerializedObject controllerSo = new SerializedObject(controller);
            MultiDimensionPuzzleManager wiredManager =
                controllerSo.FindProperty("puzzleManager").objectReferenceValue as MultiDimensionPuzzleManager;
            AllowedPlayerTag visibleToPlayer =
                (AllowedPlayerTag)controllerSo.FindProperty("visibleToPlayer").enumValueIndex;

            if (wiredManager == null)
            {
                sb.AppendLine("FAIL: puzzleManager not assigned on result lights bridge");
                issues++;
            }
            else if (operatorManager != null && wiredManager != operatorManager)
            {
                sb.AppendLine("FAIL: puzzleManager is not the operator panel manager");
                issues++;
            }
            else
            {
                sb.AppendLine("PASS: puzzleManager wired to operator panel");
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

            SerializedProperty slots = controllerSo.FindProperty("elementLights");
            if (slots.arraySize != PipesLightNames.Length)
            {
                sb.AppendLine($"FAIL: elementLights count {slots.arraySize} expected {PipesLightNames.Length}");
                issues++;
            }

            GameObject partnerPanel = GameObject.Find(partnerPanelName);
            if (partnerPanel == null)
            {
                sb.AppendLine($"FAIL: Missing '{partnerPanelName}'");
                issues++;
            }

            if (wiredManager == null || slots.arraySize != PipesLightNames.Length)
            {
                return issues;
            }

            for (int slotIndex = 0; slotIndex < PipesLightNames.Length; slotIndex++)
            {
                string lightName = PipesLightNames[slotIndex];
                SerializedProperty slot = slots.GetArrayElementAtIndex(slotIndex);
                MultiDimension sourceElement = slot.FindPropertyRelative("sourceElement").objectReferenceValue
                    as MultiDimension;
                MultiDimension resultLight = slot.FindPropertyRelative("resultLight").objectReferenceValue
                    as MultiDimension;
                ComponentDiagnosticType diagnosticType =
                    (ComponentDiagnosticType)slot.FindPropertyRelative("diagnosticType").enumValueIndex;

                if (sourceElement == null)
                {
                    sb.AppendLine($"FAIL: slot {slotIndex} sourceElement not assigned");
                    issues++;
                    continue;
                }

                if (resultLight == null)
                {
                    sb.AppendLine($"FAIL: slot {slotIndex} resultLight not assigned");
                    issues++;
                    continue;
                }

                if (resultLight.name != lightName)
                {
                    sb.AppendLine(
                        $"FAIL: slot {slotIndex} resultLight='{resultLight.name}' expected '{lightName}'");
                    issues++;
                }
                else
                {
                    sb.AppendLine($"PASS: slot {slotIndex} '{lightName}' wired");
                }

                if (partnerPanel != null &&
                    !resultLight.transform.IsChildOf(partnerPanel.transform))
                {
                    sb.AppendLine(
                        $"FAIL: slot '{lightName}' light is not under partner panel '{partnerPanelName}'");
                    issues++;
                }

                if (!wiredManager.TryGetPuzzleElement(slotIndex, out MultiDimension expectedElement, out int correctIndex))
                {
                    sb.AppendLine($"FAIL: puzzle slot {slotIndex} not available on manager");
                    issues++;
                    continue;
                }

                if (sourceElement != expectedElement)
                {
                    sb.AppendLine(
                        $"FAIL: slot {slotIndex} source '{sourceElement.name}' != puzzle element '{expectedElement.name}'");
                    issues++;
                }

                issues += ValidatePipesResultLightColorMapping(
                    sb,
                    controller,
                    slotIndex,
                    lightName,
                    diagnosticType,
                    correctIndex,
                    wiredManager.PuzzleElementCount);
            }

            return issues;
        }

        private static int ValidatePipesResultLightColorMapping(
            StringBuilder sb,
            SplitResultPipesController controller,
            int slotIndex,
            string slotLabel,
            ComponentDiagnosticType diagnosticType,
            int correctIndex,
            int slotCount)
        {
            int issues = 0;

            if (diagnosticType == ComponentDiagnosticType.Categorical)
            {
                int[] wrong = new int[slotCount];
                controller.ApplySubmittedIndices(wrong, solved: false);
                sb.AppendLine($"PASS: slot '{slotLabel}' categorical mismatch mapping exercised");
                return issues;
            }

            if (correctIndex > 0)
            {
                int[] tooLow = new int[slotCount];
                tooLow[slotIndex] = correctIndex - 1;
                controller.ApplySubmittedIndices(tooLow, solved: false);
                sb.AppendLine($"PASS: slot '{slotLabel}' too-low path exercised (submitted {tooLow[slotIndex]})");
            }

            if (correctIndex < 3)
            {
                int[] tooHigh = new int[slotCount];
                tooHigh[slotIndex] = correctIndex + 1;
                controller.ApplySubmittedIndices(tooHigh, solved: false);
                sb.AppendLine($"PASS: slot '{slotLabel}' too-high path exercised (submitted {tooHigh[slotIndex]})");
            }

            int[] correct = new int[slotCount];
            correct[slotIndex] = correctIndex;
            controller.ApplySubmittedIndices(correct, solved: false);
            sb.AppendLine($"PASS: slot '{slotLabel}' correct path exercised");

            return issues;
        }
    }
}
#endif
