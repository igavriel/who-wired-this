#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    public static class SignalCalibrationSignalSubmitValidationTool
    {
        private const string MenuPath = "Who Wired This/Signal Calibration/Validation/2. Signal Submit Lever And Focus";
        private const string McpMenuPath = "Who Wired This/Signal Calibration/MCP/2. Signal Submit Lever And Focus";

        private const string PanelAName = "Player1_Signal_Panel-A";
        private const string PanelBName = "Player2_Signal_Panel-B";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Signal Submit Lever", issues, report, showDialog: true);
        }

        [MenuItem(McpMenuPath)]
        public static void ValidateForMcp()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Signal Submit Lever", issues, report);
        }

        public static int RunValidation(out string report)
        {
            var sb = new StringBuilder();
            int issues = 0;

            issues += ValidatePanel(sb, PanelAName);
            issues += ValidatePanel(sb, PanelBName);

            sb.AppendLine(issues == 0
                ? "=== Signal submit lever validation: ALL CHECKS PASSED ==="
                : $"=== Signal submit lever validation: {issues} issue(s) ===");

            report = sb.ToString();
            return issues;
        }

        private static int ValidatePanel(StringBuilder sb, string panelName)
        {
            int issues = 0;
            sb.AppendLine($"--- {panelName} ---");

            GameObject panel = GameObject.Find(panelName);
            if (panel == null)
            {
                sb.AppendLine($"FAIL: Missing '{panelName}'");
                return 1;
            }

            SolveInteractProxy proxy = panel.GetComponentInChildren<SolveInteractProxy>(true);
            if (proxy == null)
            {
                sb.AppendLine("FAIL: No SolveInteractProxy on submit lever");
                issues++;
            }
            else
            {
                sb.AppendLine("PASS: SolveInteractProxy present");
            }

            SubmitLeverMultiDimensionFeedback leverFeedback =
                panel.GetComponentInChildren<SubmitLeverMultiDimensionFeedback>(true);
            if (leverFeedback == null)
            {
                sb.AppendLine("FAIL: No SubmitLeverMultiDimensionFeedback");
                issues++;
            }
            else
            {
                sb.AppendLine("PASS: SubmitLeverMultiDimensionFeedback present");
            }

            MultiDimensionPuzzleInteractableBridge bridge =
                panel.GetComponentInChildren<MultiDimensionPuzzleInteractableBridge>(true);
            if (bridge == null)
            {
                sb.AppendLine("FAIL: No MultiDimensionPuzzleInteractableBridge");
                issues++;
            }
            else
            {
                SerializedObject bridgeSo = new SerializedObject(bridge);
                if (bridgeSo.FindProperty("leverFeedback").objectReferenceValue == null)
                {
                    sb.AppendLine("FAIL: bridge.leverFeedback not assigned");
                    issues++;
                }
                else
                {
                    sb.AppendLine("PASS: bridge.leverFeedback assigned");
                }
            }

            PanelFocusController focus = panel.GetComponentInChildren<PanelFocusController>(true);
            if (focus == null)
            {
                sb.AppendLine("FAIL: No PanelFocusController");
                return issues + 1;
            }

            SerializedObject focusSo = new SerializedObject(focus);
            if (focusSo.FindProperty("includeExitInFocusCycle").boolValue)
            {
                sb.AppendLine("FAIL: includeExitInFocusCycle should be false in Puzzle Signal");
                issues++;
            }
            else
            {
                sb.AppendLine("PASS: Exit omitted from focus cycle");
            }

            SerializedProperty solveRef = focusSo.FindProperty("solveButton")
                .FindPropertyRelative("interactableReference");
            if (solveRef.objectReferenceValue == null)
            {
                sb.AppendLine("FAIL: solveButton.interactableReference is null");
                issues++;
            }
            else if (proxy != null && solveRef.objectReferenceValue != proxy)
            {
                sb.AppendLine("FAIL: solveButton does not reference SolveInteractProxy");
                issues++;
            }
            else
            {
                sb.AppendLine("PASS: solveButton wired to SolveInteractProxy");
            }

            return issues;
        }
    }
}
#endif
