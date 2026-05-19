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
        private const string MenuPath = "Who Wired This/Pipe Pressure/Validate Phase 4 (Puzzel Pipes)";

        [MenuItem(MenuPath)]
        public static void Validate()
        {
            PipePressurePhase1ValidationTool.ResetPuzzelPipesSolveStateForValidationPublic();

            var sb = new StringBuilder();
            int issues = 0;

            issues += ValidateVisualizerSide(
                sb,
                "Player1_Panel",
                "Player2_Panel/DiagnosticPanel",
                new[] { "ValveGroup", "PressureGroup", "FlowGroup" });

            issues += ValidateVisualizerSide(
                sb,
                "Player2_Panel",
                "Player1_Panel/DiagnosticPanel",
                new[] { "GateGroup", "PumpGroup", "RouteGroup" });

            issues += ValidateTutorialSceneHasNoVisualizer(sb);

            sb.AppendLine(issues == 0
                ? "=== Phase 4 validation: ALL CHECKS PASSED ==="
                : $"=== Phase 4 validation: {issues} issue(s) ===");

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog(
                issues == 0 ? "Phase 4 OK" : "Phase 4 Issues",
                sb.ToString(),
                "OK");
        }

        private static int ValidateVisualizerSide(
            StringBuilder sb,
            string operatorPanelName,
            string partnerDiagnosticPath,
            string[] groupNames)
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
            MultiDimensionPuzzelManager manager = vizSo.FindProperty("puzzleManager").objectReferenceValue
                as MultiDimensionPuzzelManager;

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
                sb.AppendLine("PASS: visualRoot on partner DiagnosticPanel");
            }

            SerializedProperty slots = vizSo.FindProperty("slots");
            if (slots.arraySize != 3)
            {
                sb.AppendLine($"FAIL: slots count {slots.arraySize} expected 3");
                issues++;
            }

            for (int s = 0; s < slots.arraySize; s++)
            {
                SerializedProperty slot = slots.GetArrayElementAtIndex(s);
                SerializedProperty visuals = slot.FindPropertyRelative("stateVisuals");
                if (visuals.arraySize != 4)
                {
                    sb.AppendLine($"FAIL: slot {s} stateVisuals={visuals.arraySize} expected 4");
                    issues++;
                }
            }

            if (visualRoot == null || manager == null)
            {
                return issues;
            }

            for (int g = 0; g < groupNames.Length; g++)
            {
                Transform group = visualRoot.Find(groupNames[g]);
                if (group == null)
                {
                    sb.AppendLine($"FAIL: Missing group '{groupNames[g]}' under visual root");
                    issues++;
                    continue;
                }

                for (int state = 0; state < 4; state++)
                {
                    int[] indices = { 0, 0, 0 };
                    indices[g] = state;
                    visualizer.ApplySubmittedIndices(indices);

                    int activeInGroup = CountActiveChildren(group);
                    if (activeInGroup != 1)
                    {
                        sb.AppendLine(
                            $"FAIL: {groupNames[g]} state {state} has {activeInGroup} active children (expected 1)");
                        issues++;
                    }
                    else
                    {
                        Transform activeChild = FindActiveChild(group);
                        if (activeChild == null || activeChild.name != $"State{state}")
                        {
                            sb.AppendLine(
                                $"FAIL: {groupNames[g]} state {state} active child '{activeChild?.name}'");
                            issues++;
                        }
                    }
                }

                sb.AppendLine($"PASS: {groupNames[g]} states 0–3 map correctly");
            }

            int activeStateMeshes = CountActiveStateVisuals(visualRoot);
            if (activeStateMeshes != 3)
            {
                sb.AppendLine($"FAIL: visual root has {activeStateMeshes} active state meshes (expected 3)");
                issues++;
            }
            else
            {
                sb.AppendLine("PASS: exactly one active state mesh per group after ApplySubmittedIndices");
            }

            return issues;
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

        private static int CountActiveChildren(Transform parent)
        {
            int count = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        private static Transform FindActiveChild(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.gameObject.activeSelf)
                {
                    return child;
                }
            }

            return null;
        }

        private static int CountActiveStateVisuals(Transform root)
        {
            int count = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root || t.parent == root)
                {
                    continue;
                }

                if (t.gameObject.activeSelf && t.name.StartsWith("State"))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
#endif
