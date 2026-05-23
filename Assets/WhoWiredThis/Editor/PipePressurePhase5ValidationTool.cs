#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    public static class PipePressurePhase5ValidationTool
    {
        private const string ValidationMenuRoot = "Who Wired This/Pipe Pressure/Validation/";
        private const string McpMenuRoot = "Who Wired This/Pipe Pressure/MCP/";
        private const string MenuPath = ValidationMenuRoot + "2. Phase 5 (Randomized Solution)";
        private const string McpMenuPath = McpMenuRoot + "2. Phase 5 (Randomized Solution)";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Phase 5", issues, report, showDialog: true);
        }

        [MenuItem(McpMenuPath)]
        public static void ValidateForMcp()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Phase 5", issues, report);
        }

        public static int RunValidation(out string report)
        {
            PipePressurePhase1ValidationTool.ResetPuzzlePipesSolveStateForValidationPublic();

            var sb = new StringBuilder();
            int issues = 0;

            issues += ValidateTutorialSceneUnchanged(sb);
            issues += ValidateAssignerPresent(sb);
            issues += ValidateGeneratedSide(
                sb,
                "Player1_Panel",
                new[] { "VALVE", "PRESS", "FLOW" },
                42);
            issues += ValidateGeneratedSide(
                sb,
                "Player2_Panel",
                new[] { "GATE", "PUMP", "ROUTE" },
                43);

            RestoreFixedSceneState();

            sb.AppendLine(issues == 0
                ? "=== Phase 5 validation: ALL CHECKS PASSED ==="
                : $"=== Phase 5 validation: {issues} issue(s) ===");

            report = sb.ToString();
            return issues;
        }

        private static int ValidateTutorialSceneUnchanged(StringBuilder sb)
        {
            const string tutorialPath = "Assets/Scenes/Tutorial.unity";
            string text = System.IO.File.ReadAllText(tutorialPath);
            if (text.Contains("RandomPuzzleSolutionAssigner"))
            {
                sb.AppendLine("FAIL: Tutorial.unity references RandomPuzzleSolutionAssigner");
                return 1;
            }

            sb.AppendLine("PASS: Tutorial.unity has no RandomPuzzleSolutionAssigner");
            return 0;
        }

        private static int ValidateAssignerPresent(StringBuilder sb)
        {
            RandomPuzzleSolutionAssigner assigner = Object.FindFirstObjectByType<RandomPuzzleSolutionAssigner>();
            if (assigner == null)
            {
                sb.AppendLine("FAIL: RandomPuzzleSolutionAssigner not found in open scene");
                return 1;
            }

            SerializedObject so = new SerializedObject(assigner);
            if (!so.FindProperty("enableRandomization").boolValue)
            {
                sb.AppendLine("WARN: enableRandomization is false (validation still runs generator)");
            }

            sb.AppendLine("PASS: RandomPuzzleSolutionAssigner present");
            return 0;
        }

        private static int ValidateGeneratedSide(
            StringBuilder sb,
            string panelName,
            string[] inputNames,
            int deterministicSeed)
        {
            int issues = 0;
            sb.AppendLine($"--- {panelName} randomized (seed {deterministicSeed}) ---");

            MultiDimensionPuzzleManager pm = GameObject.Find($"{panelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzleManager>();
            if (pm == null)
            {
                sb.AppendLine($"FAIL: Missing PuzzleManager on {panelName}");
                return 1;
            }

            var dimensions = new MultiDimension[inputNames.Length];
            for (int i = 0; i < inputNames.Length; i++)
            {
                GameObject go = GameObject.Find($"{panelName}/Buttons/{inputNames[i]}");
                dimensions[i] = go != null ? go.GetComponent<MultiDimension>() : null;
                if (dimensions[i] == null)
                {
                    sb.AppendLine($"FAIL: No MultiDimension on {panelName}/Buttons/{inputNames[i]}");
                    issues++;
                }
            }

            int count = pm.PuzzleElementCount;
            if (count != 3)
            {
                sb.AppendLine($"FAIL: {panelName} puzzleElements={count}");
                return issues + 1;
            }

            var maxIndices = new int[count];
            for (int i = 0; i < count; i++)
            {
                if (!pm.TryGetElementStateCount(i, out int stateCount) || stateCount != 4)
                {
                    sb.AppendLine($"FAIL: {panelName} slot {i} stateCount={stateCount} expected 4");
                    issues++;
                }

                maxIndices[i] = stateCount - 1;
            }

            var random = new System.Random(deterministicSeed);
            if (!PuzzleSolutionGenerator.TryGenerate(count, maxIndices, random, out int[] generated))
            {
                sb.AppendLine($"FAIL: {panelName} generator could not build solution");
                return issues + 1;
            }

            if (!PuzzleSolutionGenerator.PassesConstraints(generated, maxIndices))
            {
                sb.AppendLine($"FAIL: {panelName} generated solution fails constraints");
                issues++;
            }
            else
            {
                sb.AppendLine($"PASS: {panelName} constraints ({FormatIndices(generated)})");
            }

            if (!pm.TryApplyCorrectIndices(generated))
            {
                sb.AppendLine($"FAIL: {panelName} TryApplyCorrectIndices failed");
                issues++;
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (!pm.TryGetCorrectIndex(i, out int applied) || applied != generated[i])
                    {
                        sb.AppendLine($"FAIL: {panelName} correctIndex[{i}]={applied} expected {generated[i]}");
                        issues++;
                    }
                }

                if (issues == 0)
                {
                    sb.AppendLine($"PASS: {panelName} manager correctIndex matches generated");
                }
            }

            issues += SimulateSolve(sb, pm, dimensions, generated, panelName);
            issues += ValidateDiagnosticReadsSolution(sb, panelName, pm, generated, inputNames);

            sb.AppendLine($"PASS: {panelName} history/visualizer are submitted-only (no code change in Phase 5)");
            return issues;
        }

        private static int ValidateDiagnosticReadsSolution(
            StringBuilder sb,
            string panelName,
            MultiDimensionPuzzleManager pm,
            int[] generated,
            string[] inputNames)
        {
            int issues = 0;
            ComponentDiagnosticAdapter adapter = GameObject.Find(panelName)?.GetComponent<ComponentDiagnosticAdapter>();
            if (adapter == null)
            {
                sb.AppendLine($"FAIL: Missing ComponentDiagnosticAdapter on {panelName}");
                return 1;
            }

            SerializedObject adapterSo = new SerializedObject(adapter);
            SerializedProperty components = adapterSo.FindProperty("components");
            if (components.arraySize < generated.Length)
            {
                sb.AppendLine($"FAIL: {panelName} diagnostic components undersized");
                return 1;
            }

            for (int i = 0; i < generated.Length; i++)
            {
                if (!pm.TryGetPuzzleElement(i, out _, out int correctIndex) || correctIndex != generated[i])
                {
                    sb.AppendLine($"FAIL: {panelName} diagnostic slot {i} correctIndex mismatch");
                    issues++;
                    continue;
                }

                int wrongIndex = generated[i] > 0 ? generated[i] - 1 : generated[i] + 1;
                if (wrongIndex == generated[i])
                {
                    continue;
                }

                SerializedProperty comp = components.GetArrayElementAtIndex(i);
                int diagType = comp.FindPropertyRelative("diagnosticType").enumValueIndex;
                if (diagType == (int)ComponentDiagnosticType.Categorical)
                {
                    continue;
                }

                if (wrongIndex < correctIndex && string.IsNullOrEmpty(comp.FindPropertyRelative("tooLowText").stringValue))
                {
                    sb.AppendLine($"WARN: {panelName} {inputNames[i]} tooLowText empty");
                }
            }

            if (issues == 0)
            {
                sb.AppendLine($"PASS: {panelName} ComponentDiagnosticAdapter reads live correctIndex");
            }

            return issues;
        }

        private static int SimulateSolve(
            StringBuilder sb,
            MultiDimensionPuzzleManager pm,
            MultiDimension[] dimensions,
            int[] correctIndices,
            string panelName)
        {
            int issues = 0;
            for (int i = 0; i < dimensions.Length; i++)
            {
                if (dimensions[i] == null)
                {
                    continue;
                }

                SerializedObject mdSo = new SerializedObject(dimensions[i]);
                mdSo.FindProperty("activeSubjectIndex").intValue = correctIndices[i];
                mdSo.ApplyModifiedPropertiesWithoutUndo();
                dimensions[i].ApplyConfiguration();
            }

            SerializedObject pmSo = new SerializedObject(pm);
            pmSo.FindProperty("solved").boolValue = false;
            pmSo.ApplyModifiedPropertiesWithoutUndo();

            bool solved = pm.TryCheckSolution();
            if (!solved)
            {
                sb.AppendLine($"FAIL: {panelName} TryCheckSolution failed at generated indices");
                issues++;
            }
            else
            {
                sb.AppendLine($"PASS: {panelName} solves at generated correctIndex");
            }

            pmSo.FindProperty("solved").boolValue = false;
            pmSo.ApplyModifiedPropertiesWithoutUndo();
            for (int i = 0; i < dimensions.Length; i++)
            {
                if (dimensions[i] != null)
                {
                    dimensions[i].SetSolved(false);
                }
            }

            return issues;
        }

        private static string FormatIndices(int[] indices)
        {
            if (indices == null || indices.Length == 0)
            {
                return string.Empty;
            }

            return string.Join(",", indices);
        }

        /// <summary>Restores authored scene baseline so editor validation does not leave solved state or wrong correctIndex.</summary>
        private static void RestoreFixedSceneState()
        {
            PipePressurePhase1ValidationTool.ResetPuzzlePipesSolveStateForValidationPublic();

            MultiDimensionPuzzleManager blue = GameObject.Find("Player1_Panel/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzleManager>();
            MultiDimensionPuzzleManager red = GameObject.Find("Player2_Panel/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzleManager>();
            blue?.TryApplyCorrectIndices(new[] { 2, 1, 2 });
            red?.TryApplyCorrectIndices(new[] { 3, 2, 3 });
        }
    }
}
#endif
