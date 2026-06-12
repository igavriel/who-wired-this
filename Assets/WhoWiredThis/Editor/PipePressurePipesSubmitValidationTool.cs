#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    public static class PipePressurePipesSubmitValidationTool
    {
        private const string MenuPath = "Who Wired This/Pipe Pressure/Validation/2. Pipes Submit Lever And Focus";
        private const string McpMenuPath = "Who Wired This/Pipe Pressure/MCP/2. Pipes Submit Lever And Focus";

        private const string PanelAName = "Player1_Pipes_Panel A";
        private const string PanelBName = "Player2_Pipes_Panel B";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Pipes Submit Lever", issues, report, showDialog: true);
        }

        [MenuItem(McpMenuPath)]
        public static void ValidateForMcp()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Pipes Submit Lever", issues, report);
        }

        public static int RunValidation(out string report)
        {
            var sb = new StringBuilder();
            int issues = 0;

            issues += ValidatePanel(sb, PanelAName);
            issues += ValidatePanel(sb, PanelBName);

            sb.AppendLine(issues == 0
                ? "=== Pipes submit lever validation: ALL CHECKS PASSED ==="
                : $"=== Pipes submit lever validation: {issues} issue(s) ===");

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
                sb.AppendLine("FAIL: includeExitInFocusCycle should be false in Puzzle Pipes");
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
