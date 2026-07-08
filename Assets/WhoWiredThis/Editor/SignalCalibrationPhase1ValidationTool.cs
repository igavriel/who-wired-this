#if UNITY_EDITOR
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Scenes;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    public static class SignalCalibrationPhase1ValidationTool
    {
        private const string ScenePath = "Assets/Scenes/Game/Puzzle Signal.unity";
        private const string SignalPanelAName = "Player1_Signal_Panel-A";
        private const string SignalPanelBName = "Player2_Signal_Panel-B";
        private const string ValidationMenuRoot = "Who Wired This/Signal Calibration/Validation/";
        private const string McpMenuRoot = "Who Wired This/Signal Calibration/MCP/";
        private const string MenuPath = ValidationMenuRoot + "0. Phase 1 (Puzzle Signal)";
        private const string McpMenuPath = McpMenuRoot + "0. Phase 1 (Puzzle Signal)";

        private static readonly string[] KnobStates = { "MIN", "LOW", "MID", "HIGH", "MAX" };
        private static readonly string[] ButtonStates = { "FLAT", "SINE", "PULS", "TRNG", "NOIS" };

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Signal Phase 1", issues, report, showDialog: true);
        }

        [MenuItem(McpMenuPath)]
        public static void ValidateForMcp()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Signal Phase 1", issues, report);
        }

        public static int RunValidation(out string report)
        {
            ResetSignalSolveStateForValidation();

            var sb = new StringBuilder();
            int issues = 0;

            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                sb.AppendLine($"FAIL: Open '{ScenePath}' before validating.");
                report = sb.ToString();
                return 1;
            }

            if (GameObject.Find(SignalPanelAName) != null && GameObject.Find(SignalPanelBName) != null)
            {
                issues += ValidateSignalPanelInstance(sb, SignalPanelAName, AllowedPlayerTag.Player_A,
                    new[] { "FREQ", "GAIN", "WAVE" },
                    new[] { KnobStates, KnobStates, ButtonStates },
                    new[] { 2, 2, 2 });
                issues += ValidateSignalPanelInstance(sb, SignalPanelBName, AllowedPlayerTag.Player_B,
                    new[] { "TUNE", "AMP", "MODE" },
                    new[] { KnobStates, KnobStates, ButtonStates },
                    new[] { 3, 2, 3 });
                issues += ValidateTurnLockCollidersForPanel(sb, "playerAPanelLock", SignalPanelAName);
                issues += ValidateTurnLockCollidersForPanel(sb, "playerBPanelLock", SignalPanelBName);
            }
            else
            {
                issues += ValidatePanel(sb, "Player1_Panel", AllowedPlayerTag.Player_A,
                    new[] { "FREQ", "GAIN", "WAVE" },
                    new[] { KnobStates, KnobStates, ButtonStates },
                    new[] { 2, 2, 2 });

                issues += ValidatePanel(sb, "Player2_Panel", AllowedPlayerTag.Player_B,
                    new[] { "TUNE", "AMP", "MODE" },
                    new[] { KnobStates, KnobStates, ButtonStates },
                    new[] { 3, 2, 3 });

                issues += ValidateTurnLockColliders(sb, "playerAPanelLock", "Player1_Panel",
                    new[] { "FREQ", "GAIN", "WAVE" });
                issues += ValidateTurnLockColliders(sb, "playerBPanelLock", "Player2_Panel",
                    new[] { "TUNE", "AMP", "MODE" });
            }

            issues += ValidateHistoryBoards(sb);

            sb.AppendLine(issues == 0
                ? "=== Signal Phase 1 validation: ALL CHECKS PASSED (edit-mode structural) ==="
                : $"=== Signal Phase 1 validation: {issues} issue(s) ===");

            report = sb.ToString();
            return issues;
        }

        public static void ResetSignalSolveStateForValidationPublic() => ResetSignalSolveStateForValidation();

        private static void ResetSignalSolveStateForValidation()
        {
            string[] panelNames =
            {
                SignalPanelAName,
                SignalPanelBName,
                "Player1_Panel",
                "Player2_Panel"
            };
            for (int p = 0; p < panelNames.Length; p++)
            {
                GameObject panel = GameObject.Find(panelNames[p]);
                MultiDimensionPuzzleManager pm =
                    panel?.GetComponentInChildren<MultiDimensionPuzzleManager>(true);
                pm?.ResetSessionForNewRun();
            }
        }

        private static int ValidateSignalPanelInstance(
            StringBuilder sb,
            string panelName,
            AllowedPlayerTag player,
            string[] inputNames,
            string[][] expectedStates,
            int[] expectedCorrect)
        {
            int issues = 0;
            sb.AppendLine($"--- {panelName} (Signal prefab instance) ---");

            GameObject panel = GameObject.Find(panelName);
            if (panel == null)
            {
                sb.AppendLine($"FAIL: Missing '{panelName}'");
                return 1;
            }

            PanelFocusController focus = panel.GetComponentInChildren<PanelFocusController>(true);
            if (focus == null)
            {
                sb.AppendLine("FAIL: No PanelFocusController");
                return issues + 1;
            }

            SerializedObject focusSo = new SerializedObject(focus);
            if ((AllowedPlayerTag)focusSo.FindProperty("allowedPlayerId").enumValueIndex != player)
            {
                sb.AppendLine($"FAIL: allowedPlayerId != {player}");
                issues++;
            }
            else
            {
                sb.AppendLine($"PASS: allowedPlayerId={player}");
            }

            int btnCount = focusSo.FindProperty("interactableButtons").arraySize;
            if (btnCount != 3)
            {
                sb.AppendLine($"FAIL: interactableButtons={btnCount}");
                issues++;
            }
            else
            {
                sb.AppendLine("PASS: interactableButtons=3");
            }

            var solveRef = focusSo.FindProperty("solveButton").FindPropertyRelative("interactableReference");
            if (solveRef.objectReferenceValue == null)
            {
                sb.AppendLine("FAIL: solveButton.interactableReference is null");
                issues++;
            }
            else
            {
                sb.AppendLine("PASS: solveButton wired");
            }

            MultiDimensionPuzzleManager pm = panel.GetComponentInChildren<MultiDimensionPuzzleManager>(true);
            if (pm == null)
            {
                sb.AppendLine("FAIL: Missing MultiDimensionPuzzleManager");
                issues++;
            }
            else
            {
                SerializedObject pmSo = new SerializedObject(pm);
                if (pmSo.FindProperty("puzzleElements").arraySize != 3)
                {
                    sb.AppendLine("FAIL: puzzleElements != 3");
                    issues++;
                }
                else
                {
                    sb.AppendLine("PASS: puzzleElements=3");
                    SerializedProperty elems = pmSo.FindProperty("puzzleElements");
                    for (int i = 0; i < expectedCorrect.Length; i++)
                    {
                        int ci = elems.GetArrayElementAtIndex(i).FindPropertyRelative("correctIndex").intValue;
                        if (ci != expectedCorrect[i])
                        {
                            sb.AppendLine($"FAIL: {panelName} correct[{i}]={ci} expected {expectedCorrect[i]}");
                            issues++;
                        }
                    }
                }
            }

            ComponentDiagnosticAdapter adapter = panel.GetComponentInChildren<ComponentDiagnosticAdapter>(true);
            if (adapter == null)
            {
                sb.AppendLine("FAIL: Missing ComponentDiagnosticAdapter");
                issues++;
            }
            else
            {
                SerializedObject adapterSo = new SerializedObject(adapter);
                if (adapterSo.FindProperty("diagnosticDisplay").objectReferenceValue == null)
                {
                    sb.AppendLine("FAIL: adapter.diagnosticDisplay is null");
                    issues++;
                }
                else
                {
                    sb.AppendLine("PASS: adapter.diagnosticDisplay assigned");
                }
            }

            var dimensions = new MultiDimension[inputNames.Length];
            for (int i = 0; i < inputNames.Length; i++)
            {
                Transform inputTransform = FindChildTransform(panel.transform, inputNames[i]);
                string path = inputTransform != null ? inputTransform.name : inputNames[i];
                if (inputTransform == null)
                {
                    sb.AppendLine($"FAIL: Missing input '{inputNames[i]}' under {panelName}");
                    issues++;
                    continue;
                }

                dimensions[i] = inputTransform.GetComponent<MultiDimension>();
                if (dimensions[i] == null)
                {
                    sb.AppendLine($"FAIL: No MultiDimension on {path}");
                    issues++;
                    continue;
                }

                if (dimensions[i].SubjectCount != 5)
                {
                    sb.AppendLine($"FAIL: {path} SubjectCount={dimensions[i].SubjectCount}");
                    issues++;
                }
                else
                {
                    sb.AppendLine($"PASS: {path} has 5 subjects");
                }

                issues += ValidateAdvanceCycle(sb, dimensions[i], path, player);
            }

            return issues;
        }

        private static int ValidateTurnLockCollidersForPanel(StringBuilder sb, string bundleProp, string panelName)
        {
            PanelFocusController focus = GameObject.Find(panelName)?.GetComponentInChildren<PanelFocusController>(true);
            if (focus == null)
            {
                sb.AppendLine($"FAIL: No PanelFocusController on {panelName} for turn-lock check");
                return 1;
            }

            int expectedInputs = new SerializedObject(focus).FindProperty("interactableButtons").arraySize;
            SceneStageManager tsm = Object.FindFirstObjectByType<SceneStageManager>();
            if (tsm == null)
            {
                sb.AppendLine("FAIL: SceneStageManager not found");
                return 1;
            }

            SerializedObject tsmSo = new SerializedObject(tsm);
            SerializedProperty colliders = tsmSo.FindProperty(bundleProp).FindPropertyRelative("actionColliders");
            if (colliders.arraySize != expectedInputs + 1)
            {
                sb.AppendLine($"FAIL: {bundleProp} actionColliders={colliders.arraySize} expected {expectedInputs + 1}");
                return 1;
            }

            sb.AppendLine($"PASS: {bundleProp} actionColliders={colliders.arraySize} (3 inputs + Send)");
            return 0;
        }

        private static Transform FindChildTransform(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindChildTransform(root.GetChild(i), childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static int ValidatePanel(
            StringBuilder sb,
            string panelName,
            AllowedPlayerTag player,
            string[] inputNames,
            string[][] expectedStates,
            int[] expectedCorrect)
        {
            int issues = 0;
            var dimensions = new MultiDimension[inputNames.Length];

            for (int i = 0; i < inputNames.Length; i++)
            {
                string path = $"{panelName}/Buttons/{inputNames[i]}";
                GameObject go = GameObject.Find(path);
                if (go == null)
                {
                    sb.AppendLine($"FAIL: Missing {path}");
                    issues++;
                    continue;
                }

                dimensions[i] = go.GetComponent<MultiDimension>();
                if (dimensions[i] == null)
                {
                    sb.AppendLine($"FAIL: No MultiDimension on {path}");
                    issues++;
                    continue;
                }

                if (dimensions[i].SubjectCount != 5)
                {
                    sb.AppendLine($"FAIL: {path} SubjectCount={dimensions[i].SubjectCount}");
                    issues++;
                }
                else
                {
                    sb.AppendLine($"PASS: {path} has 5 subjects");
                }

                for (int s = 0; s < 5; s++)
                {
                    string dn = dimensions[i].GetSubjectDisplayName(s);
                    if (dn != expectedStates[i][s])
                    {
                        sb.AppendLine($"FAIL: {path} displayName[{s}]='{dn}' expected '{expectedStates[i][s]}'");
                        issues++;
                    }
                }

                issues += ValidateTmpLabelsMatch(sb, dimensions[i], path, expectedStates[i], inputNames[i]);

                SerializedObject mdSo = new SerializedObject(dimensions[i]);
                var tag = (AllowedPlayerTag)mdSo.FindProperty("visibleToPlayer").enumValueIndex;
                if (tag != player)
                {
                    sb.AppendLine($"FAIL: {path} visibleToPlayer={tag} expected {player}");
                    issues++;
                }

                issues += ValidateAdvanceCycle(sb, dimensions[i], path, player);
            }

            MultiDimensionPuzzleManager pm = GameObject.Find($"{panelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzleManager>();
            if (pm == null)
            {
                sb.AppendLine($"FAIL: Missing PuzzleManager on {panelName}");
                return issues + 1;
            }

            SerializedObject pmSo = new SerializedObject(pm);
            SerializedProperty elems = pmSo.FindProperty("puzzleElements");
            if (elems.arraySize != 3)
            {
                sb.AppendLine($"FAIL: {panelName} puzzleElements={elems.arraySize}");
                issues++;
            }
            else
            {
                sb.AppendLine($"PASS: {panelName} puzzleElements=3");
                for (int i = 0; i < 3; i++)
                {
                    int ci = elems.GetArrayElementAtIndex(i).FindPropertyRelative("correctIndex").intValue;
                    if (ci != expectedCorrect[i])
                    {
                        sb.AppendLine($"FAIL: {panelName} correct[{i}]={ci} expected {expectedCorrect[i]}");
                        issues++;
                    }
                }
            }

            MultiDimensionHistoryAdapter hist = GameObject.Find(panelName)?.GetComponent<MultiDimensionHistoryAdapter>();
            if (hist == null)
            {
                sb.AppendLine($"FAIL: Missing MultiDimensionHistoryAdapter on {panelName}");
                issues++;
            }
            else
            {
                SerializedObject histSo = new SerializedObject(hist);
                if (histSo.FindProperty("inputOrder").arraySize != 3)
                {
                    sb.AppendLine($"FAIL: {panelName} inputOrder size");
                    issues++;
                }
                else
                {
                    sb.AppendLine($"PASS: {panelName} inputOrder=3");
                }
            }

            PanelFocusController pfc = GameObject.Find($"{panelName}/Board")?.GetComponent<PanelFocusController>();
            if (pfc == null)
            {
                sb.AppendLine($"FAIL: Missing PanelFocusController on {panelName}/Board");
                issues++;
            }
            else
            {
                SerializedObject pfcSo = new SerializedObject(pfc);
                int btnCount = pfcSo.FindProperty("interactableButtons").arraySize;
                if (btnCount != 3)
                {
                    sb.AppendLine($"FAIL: {panelName} interactableButtons={btnCount}");
                    issues++;
                }
                else
                {
                    sb.AppendLine($"PASS: {panelName} panel focus buttons=3 (+ Solve/Exit)");
                }
            }

            issues += SimulateSolve(sb, pm, dimensions, expectedCorrect, panelName);

            return issues;
        }

        private static int ValidateTmpLabelsMatch(
            StringBuilder sb,
            MultiDimension md,
            string path,
            string[] expectedStates,
            string inputName)
        {
            if (UsesSymbolicButtonTextVisuals(md.gameObject))
            {
                sb.AppendLine(
                    $"PASS: {path} symbolic TMP allowed (ButtonText_5State); history uses displayName");
                return 0;
            }

            if (UsesHiddenMinKnobOrSliderVisuals(md.gameObject))
            {
                sb.AppendLine(
                    $"PASS: {path} Knob/Slider_5State: MIN may be inactive; history uses displayName");
                return 0;
            }

            int issues = 0;
            TMP_Text[] labels = md.GetComponentsInChildren<TMP_Text>(true);
            var visibleTexts = new System.Collections.Generic.List<string>();
            for (int i = 0; i < labels.Length; i++)
            {
                string text = labels[i].text?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    visibleTexts.Add(text);
                }
            }

            for (int s = 0; s < expectedStates.Length; s++)
            {
                string expected = expectedStates[s];
                bool found = false;
                for (int i = 0; i < visibleTexts.Count; i++)
                {
                    if (visibleTexts[i] == expected)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    sb.AppendLine($"FAIL: {path} missing visible TMP for '{expected}' (found: {string.Join(", ", visibleTexts)})");
                    issues++;
                }
            }

            if (issues == 0)
            {
                sb.AppendLine($"PASS: {path} TMP labels match displayName");
            }

            return issues;
        }

        private static bool UsesSymbolicButtonTextVisuals(GameObject inputRoot)
        {
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(inputRoot);
            if (!string.IsNullOrEmpty(prefabPath) &&
                prefabPath.Contains("MultiDimension_ButtonText_5State"))
            {
                return true;
            }

            string name = inputRoot != null ? inputRoot.name : string.Empty;
            return name == "WAVE" || name == "MODE";
        }

        private static bool UsesHiddenMinKnobOrSliderVisuals(GameObject inputRoot)
        {
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(inputRoot);
            return !string.IsNullOrEmpty(prefabPath) &&
                   (prefabPath.Contains("MultiDimension_Knob_5State") ||
                    prefabPath.Contains("MultiDimension_Slider_5State"));
        }

        private static int ValidateAdvanceCycle(
            StringBuilder sb,
            MultiDimension md,
            string path,
            AllowedPlayerTag player)
        {
            int issues = 0;
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int step = 0; step < 10; step++)
            {
                md.AdvanceIndexForPlayer(player);
                int idx = md.GetCurrentIndexForSolutionCheck();
                if (idx < 0 || idx >= md.SubjectCount)
                {
                    sb.AppendLine($"FAIL: {path} invalid index after advance: {idx}");
                    issues++;
                    break;
                }

                if (!seen.Add(idx) && seen.Count >= md.SubjectCount)
                {
                    break;
                }
            }

            if (seen.Count < md.SubjectCount)
            {
                sb.AppendLine($"FAIL: {path} advance cycle only reached {seen.Count}/5 indices");
                issues++;
            }
            else
            {
                sb.AppendLine($"PASS: {path} cycles through 5 states");
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
                sb.AppendLine($"FAIL: {panelName} TryCheckSolution failed at correct indices");
                issues++;
            }
            else
            {
                sb.AppendLine($"PASS: {panelName} solves at configured correctIndex");
            }

            pmSo.FindProperty("solved").boolValue = false;
            pmSo.ApplyModifiedPropertiesWithoutUndo();
            for (int i = 0; i < dimensions.Length; i++)
            {
                dimensions[i]?.SetSolved(false);
            }

            return issues;
        }

        private static int ValidateTurnLockColliders(
            StringBuilder sb,
            string bundleProp,
            string panelName,
            string[] inputNames)
        {
            int issues = 0;
            SceneStageManager tsm = Object.FindFirstObjectByType<SceneStageManager>();
            if (tsm == null)
            {
                sb.AppendLine("FAIL: SceneStageManager not found");
                return 1;
            }

            SerializedObject tsmSo = new SerializedObject(tsm);
            SerializedProperty colliders = tsmSo.FindProperty(bundleProp).FindPropertyRelative("actionColliders");
            if (colliders.arraySize != inputNames.Length + 1)
            {
                sb.AppendLine($"FAIL: {bundleProp} actionColliders={colliders.arraySize} expected {inputNames.Length + 1}");
                issues++;
            }
            else
            {
                sb.AppendLine($"PASS: {bundleProp} actionColliders={colliders.arraySize} (3 inputs + Send)");
            }

            return issues;
        }

        private static int ValidateHistoryBoards(StringBuilder sb)
        {
            int issues = 0;
            const int requiredInputHeaderWidth = 17;
            var historyBoards = new System.Collections.Generic.List<(string label, HistoryBoardController board)>();

            GameObject signalPanelA = GameObject.Find(SignalPanelAName);
            GameObject signalPanelB = GameObject.Find(SignalPanelBName);
            if (signalPanelA != null && signalPanelB != null)
            {
                historyBoards.Add(($"{SignalPanelAName}/History", signalPanelA.GetComponentInChildren<HistoryBoardController>(true)));
                historyBoards.Add(($"{SignalPanelBName}/History", signalPanelB.GetComponentInChildren<HistoryBoardController>(true)));
            }
            else
            {
                string[] legacyPanelNames = { "Player1_Panel", "Player2_Panel" };
                foreach (string panelName in legacyPanelNames)
                {
                    GameObject panel = GameObject.Find(panelName);
                    HistoryBoardController board =
                        panel?.GetComponentInChildren<HistoryBoardController>(true);
                    if (board != null)
                    {
                        historyBoards.Add(($"{panelName}/HistoryPanel", board));
                    }
                }
            }

            foreach ((string path, HistoryBoardController board) in historyBoards)
            {
                if (board == null)
                {
                    sb.AppendLine($"FAIL: Missing HistoryBoardController at '{path}'");
                    issues++;
                    continue;
                }

                SerializedObject so = new SerializedObject(board);
                string header = so.FindProperty("headerLine").stringValue;
                string sep = so.FindProperty("separatorLine").stringValue;
                sb.AppendLine($"INFO: {path} header='{header}'");
                sb.AppendLine($"INFO: {path} separator='{sep}'");

                int inputColStart = header.IndexOf("INPUT", System.StringComparison.Ordinal);
                if (inputColStart >= 0)
                {
                    int afterInput = header.Length - inputColStart;
                    if (afterInput < requiredInputHeaderWidth)
                    {
                        sb.AppendLine(
                            $"FAIL: {path} INPUT header width {afterInput} < {requiredInputHeaderWidth} (3×5 tokens)");
                        issues++;
                    }
                    else
                    {
                        sb.AppendLine(
                            $"PASS: {path} INPUT header width {afterInput} (>= {requiredInputHeaderWidth})");
                    }
                }
            }

            string sampleBlue = "MID MID SINE";
            string sampleRed = "HIGH MID NOIS";
            string formattedBlue = FormatInputCellReflection(sampleBlue);
            string formattedRed = FormatInputCellReflection(sampleRed);
            sb.AppendLine($"INFO: FormatInputCell('{sampleBlue}') => '{formattedBlue}' (len {formattedBlue.Length})");
            sb.AppendLine($"INFO: FormatInputCell('{sampleRed}') => '{formattedRed}' (len {formattedRed.Length})");

            if (formattedBlue.Length < 17)
            {
                sb.AppendLine("WARN: Blue formatted input shorter than 17 chars — check padding");
            }

            return issues;
        }

        private static string FormatInputCellReflection(string raw)
        {
            var method = typeof(HistoryBoardController).GetMethod(
                "FormatInputCell",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (method == null)
            {
                return "(FormatInputCell not accessible)";
            }

            return (string)method.Invoke(null, new object[] { raw });
        }
    }
}
#endif
