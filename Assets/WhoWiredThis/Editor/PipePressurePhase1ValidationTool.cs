#if UNITY_EDITOR
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Tutorial;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    public static class PipePressurePhase1ValidationTool
    {
        private const string MenuPath = "Who Wired This/Pipe Pressure/Validate Phase 1 (Puzzel Pipes)";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            ResetPuzzelPipesSolveStateForValidation();

            var sb = new StringBuilder();
            int issues = 0;

            issues += ValidatePanel(sb, "Player1_Panel", AllowedPlayerTag.Player_A,
                new[] { "VALVE", "PRESS", "FLOW" },
                new[]
                {
                    new[] { "SHUT", "LOW", "HALF", "OPEN" },
                    new[] { "LOW", "MID", "HIGH", "MAX" },
                    new[] { "LEFT", "MID", "RGHT", "LOOP" }
                },
                new[] { 2, 1, 2 });

            issues += ValidatePanel(sb, "Player2_Panel", AllowedPlayerTag.Player_B,
                new[] { "GATE", "PUMP", "ROUTE" },
                new[]
                {
                    new[] { "SHUT", "LOW", "HALF", "OPEN" },
                    new[] { "LOW", "MID", "HIGH", "MAX" },
                    new[] { "LEFT", "MID", "RGHT", "LOOP" }
                },
                new[] { 3, 2, 3 });

            issues += ValidateTurnLockColliders(sb, "playerAPanelLock", "Player1_Panel",
                new[] { "VALVE", "PRESS", "FLOW" });
            issues += ValidateTurnLockColliders(sb, "playerBPanelLock", "Player2_Panel",
                new[] { "GATE", "PUMP", "ROUTE" });

            issues += ValidateHistoryBoards(sb);

            sb.AppendLine(issues == 0
                ? "=== Phase 1 validation: ALL CHECKS PASSED (edit-mode structural) ==="
                : $"=== Phase 1 validation: {issues} issue(s) ===");

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog(
                issues == 0 ? "Phase 1 OK" : "Phase 1 Issues",
                sb.ToString(),
                "OK");
        }

        /// <summary>Clears solve lock left by Play Mode or TryCheckSolution so cycle/TMP checks are reliable.</summary>
        public static void ResetPuzzelPipesSolveStateForValidationPublic() =>
            ResetPuzzelPipesSolveStateForValidation();

        private static void ResetPuzzelPipesSolveStateForValidation()
        {
            string[] panelNames = { "Player1_Panel", "Player2_Panel" };
            string[][] inputs =
            {
                new[] { "VALVE", "PRESS", "FLOW" },
                new[] { "GATE", "PUMP", "ROUTE" }
            };

            for (int p = 0; p < panelNames.Length; p++)
            {
                MultiDimensionPuzzelManager pm = GameObject.Find($"{panelNames[p]}/PuzzleManager")
                    ?.GetComponent<MultiDimensionPuzzelManager>();
                if (pm != null)
                {
                    SerializedObject pmSo = new SerializedObject(pm);
                    pmSo.FindProperty("solved").boolValue = false;
                    pmSo.ApplyModifiedPropertiesWithoutUndo();
                }

                for (int i = 0; i < inputs[p].Length; i++)
                {
                    MultiDimension md = GameObject.Find($"{panelNames[p]}/Buttons/{inputs[p][i]}")
                        ?.GetComponent<MultiDimension>();
                    md?.SetSolved(false);
                }
            }
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

                if (dimensions[i].SubjectCount != 4)
                {
                    sb.AppendLine($"FAIL: {path} SubjectCount={dimensions[i].SubjectCount}");
                    issues++;
                }
                else
                {
                    sb.AppendLine($"PASS: {path} has 4 subjects");
                }

                for (int s = 0; s < 4; s++)
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

            MultiDimensionPuzzelManager pm = GameObject.Find($"{panelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzelManager>();
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
                    $"PASS: {path} symbolic TMP allowed (ButtonText_4State); history uses displayName");
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

        /// <summary>
        /// MultiDimension_ButtonText_4State may show symbolic LCD glyphs while displayName stays readable for history.
        /// </summary>
        private static bool UsesSymbolicButtonTextVisuals(GameObject inputRoot)
        {
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(inputRoot);
            if (!string.IsNullOrEmpty(prefabPath) &&
                prefabPath.Contains("MultiDimension_ButtonText_4State"))
            {
                return true;
            }

            string name = inputRoot != null ? inputRoot.name : string.Empty;
            return name == "FLOW" || name == "ROUTE";
        }

        private static int ValidateAdvanceCycle(
            StringBuilder sb,
            MultiDimension md,
            string path,
            AllowedPlayerTag player)
        {
            int issues = 0;
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int step = 0; step < 8; step++)
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
                sb.AppendLine($"FAIL: {path} advance cycle only reached {seen.Count}/4 indices");
                issues++;
            }
            else
            {
                sb.AppendLine($"PASS: {path} cycles through 4 states");
            }

            return issues;
        }

        private static int SimulateSolve(
            StringBuilder sb,
            MultiDimensionPuzzelManager pm,
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
                if (dimensions[i] != null)
                {
                    dimensions[i].SetSolved(false);
                }
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
            TutorialStageManager tsm = Object.FindFirstObjectByType<TutorialStageManager>();
            if (tsm == null)
            {
                sb.AppendLine("FAIL: TutorialStageManager not found");
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
            string[] panelHistoryPaths =
            {
                "Player1_Panel/HistoryPanel",
                "Player2_Panel/HistoryPanel"
            };

            foreach (string path in panelHistoryPaths)
            {
                HistoryBoardController board = GameObject.Find(path)?.GetComponent<HistoryBoardController>();
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

            string sampleBlue = "HALF MID RGHT";
            string sampleRed = "OPEN HIGH LOOP";
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
