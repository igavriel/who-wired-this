#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    public static class SignalCalibrationPhase2ValidationTool
    {
        private const string ScenePath = "Assets/Scenes/Game/Puzzle Signal.unity";
        private const string ValidationMenuRoot = "Who Wired This/Signal Calibration/Validation/";
        private const string McpMenuRoot = "Who Wired This/Signal Calibration/MCP/";
        private const string MenuPath = ValidationMenuRoot + "1. Phase 2 (Signal Diagnostics)";
        private const string McpMenuPath = McpMenuRoot + "1. Phase 2 (Signal Diagnostics)";

        private const string SolvedMessage = "SIGNAL LINK CALIBRATED.";
        private const string SystemNone = "SIGNAL IS UNSTABLE.";
        private const string SystemOne = "ONE SIGNAL CHANNEL RESPONDS.";
        private const string SystemTwo = "SIGNAL IS CLOSE.";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Signal Phase 2", issues, report, showDialog: true);
        }

        [MenuItem(McpMenuPath)]
        public static void ValidateForMcp()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Signal Phase 2", issues, report);
        }

        public static int RunValidation(out string report)
        {
            var sb = new StringBuilder();
            int issues = 0;

            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                sb.AppendLine($"FAIL: Open '{ScenePath}' before validating.");
                report = sb.ToString();
                return 1;
            }

            issues += ValidatePanel(sb, "Player1_Panel",
                new[] { "FREQ", "GAIN", "WAVE" },
                new[]
                {
                    ("FREQ LOOKS STABLE.", "FREQ IS TOO LOW.", "FREQ IS TOO HIGH.", string.Empty),
                    ("GAIN LOOKS STABLE.", "GAIN IS TOO LOW.", "GAIN IS TOO HIGH.", string.Empty),
                    ("WAVE PATTERN MATCHES.", string.Empty, string.Empty, "WAVE PATTERN DOES NOT MATCH.")
                });

            issues += ValidatePanel(sb, "Player2_Panel",
                new[] { "TUNE", "AMP", "MODE" },
                new[]
                {
                    ("TUNE LOOKS STABLE.", "TUNE IS TOO LOW.", "TUNE IS TOO HIGH.", string.Empty),
                    ("AMP LOOKS STABLE.", "AMP IS TOO LOW.", "AMP IS TOO HIGH.", string.Empty),
                    ("MODE PATTERN MATCHES.", string.Empty, string.Empty, "MODE PATTERN DOES NOT MATCH.")
                });

            sb.AppendLine(issues == 0
                ? "=== Signal Phase 2 validation: ALL CHECKS PASSED ==="
                : $"=== Signal Phase 2 validation: {issues} issue(s) ===");

            report = sb.ToString();
            return issues;
        }

        private static int ValidatePanel(
            StringBuilder sb,
            string panelName,
            string[] inputNames,
            (string correct, string tooLow, string tooHigh, string mismatch)[] expected)
        {
            int issues = 0;
            ComponentDiagnosticAdapter adapter = GameObject.Find(panelName)?.GetComponent<ComponentDiagnosticAdapter>();
            if (adapter == null)
            {
                sb.AppendLine($"FAIL: Missing ComponentDiagnosticAdapter on {panelName}");
                return 1;
            }

            SerializedObject so = new SerializedObject(adapter);

            issues += ExpectString(sb, panelName, "solvedMessage", so, SolvedMessage);
            issues += ExpectString(sb, panelName, "systemNoneCorrect", so, SystemNone);
            issues += ExpectString(sb, panelName, "systemOneCorrect", so, SystemOne);
            issues += ExpectString(sb, panelName, "systemTwoCorrect", so, SystemTwo);

            if (ContainsPipeCopy(so.FindProperty("solvedMessage").stringValue) ||
                ContainsPipeCopy(so.FindProperty("systemNoneCorrect").stringValue))
            {
                sb.AppendLine($"FAIL: {panelName} still has pipe-themed system copy");
                issues++;
            }
            else
            {
                sb.AppendLine($"PASS: {panelName} system messages are signal-themed");
            }

            SerializedProperty components = so.FindProperty("components");
            if (components.arraySize != inputNames.Length)
            {
                sb.AppendLine($"FAIL: {panelName} components count={components.arraySize}");
                issues++;
                return issues;
            }

            for (int i = 0; i < inputNames.Length; i++)
            {
                SerializedProperty entry = components.GetArrayElementAtIndex(i);
                MultiDimension input = entry.FindPropertyRelative("input").objectReferenceValue as MultiDimension;
                string path = $"{panelName}/Buttons/{inputNames[i]}";
                if (input == null || input.gameObject.name != inputNames[i])
                {
                    sb.AppendLine($"FAIL: {panelName} component[{i}] input not bound to {path}");
                    issues++;
                }

                issues += ExpectString(sb, path, "correctText", entry, expected[i].correct);
                issues += ExpectString(sb, path, "tooLowText", entry, expected[i].tooLow);
                issues += ExpectString(sb, path, "tooHighText", entry, expected[i].tooHigh);
                issues += ExpectString(sb, path, "mismatchText", entry, expected[i].mismatch);

                string correct = entry.FindPropertyRelative("correctText").stringValue;
                if (ContainsPipeCopy(correct))
                {
                    sb.AppendLine($"FAIL: {path} correctText still pipe-themed: '{correct}'");
                    issues++;
                }
            }

            return issues;
        }

        private static int ExpectString(
            StringBuilder sb,
            string context,
            string propName,
            SerializedObject so,
            string expected)
        {
            string actual = so.FindProperty(propName).stringValue;
            if (actual != expected)
            {
                sb.AppendLine($"FAIL: {context} {propName}='{actual}' expected '{expected}'");
                return 1;
            }

            return 0;
        }

        private static int ExpectString(
            StringBuilder sb,
            string context,
            string propName,
            SerializedProperty parent,
            string expected)
        {
            string actual = parent.FindPropertyRelative(propName).stringValue;
            if (actual != expected)
            {
                sb.AppendLine($"FAIL: {context} {propName}='{actual}' expected '{expected}'");
                return 1;
            }

            return 0;
        }

        private static bool ContainsPipeCopy(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            return text.Contains("PIPE") ||
                   text.Contains("VALVE") ||
                   text.Contains("PRESSURE") ||
                   text.Contains("FLOW ROUTE") ||
                   text.Contains("GATE") ||
                   text.Contains("PUMP") ||
                   text.Contains("ROUTE LOOKS");
        }
    }
}
#endif
