#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Tutorial;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// One-shot wiring for Puzzel Pipes: 3×4-state inputs, puzzle managers, history, panel focus, turn locks.
    /// Menu: Who Wired This / Pipe Pressure / Wire Puzzel Pipes Scene
    /// </summary>
    public static class PipePressurePuzzelPipesWireTool
    {
        private const string MenuPath = "Who Wired This/Pipe Pressure/Wire Puzzel Pipes Scene";

        [MenuItem(MenuPath)]
        public static void WireScene()
        {
            WirePanel(
                "Player1_Panel",
                new[] { "VALVE", "PRESS", "FLOW" },
                new[] { 2, 1, 2 });

            WirePanel(
                "Player2_Panel",
                new[] { "GATE", "PUMP", "ROUTE" },
                new[] { 3, 2, 3 });

            WireTurnLocks();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[PipePressurePuzzelPipesWireTool] Wired Player1_Panel and Player2_Panel (3 inputs each).");
        }

        private static void WirePanel(string panelName, string[] inputNames, int[] correctIndices)
        {
            if (inputNames.Length != correctIndices.Length)
            {
                Debug.LogError("[PipePressurePuzzelPipesWireTool] inputNames and correctIndices length mismatch.");
                return;
            }

            var dimensions = new MultiDimension[inputNames.Length];
            var cyclers = new MultiDimensionSubjectCycler[inputNames.Length];

            for (int i = 0; i < inputNames.Length; i++)
            {
                string path = $"{panelName}/Buttons/{inputNames[i]}";
                GameObject inputGo = GameObject.Find(path);
                if (inputGo == null)
                {
                    Debug.LogError($"[PipePressurePuzzelPipesWireTool] Missing GameObject at '{path}'.");
                    return;
                }

                dimensions[i] = inputGo.GetComponent<MultiDimension>();
                cyclers[i] = inputGo.GetComponent<MultiDimensionSubjectCycler>();
                if (dimensions[i] == null || cyclers[i] == null)
                {
                    Debug.LogError($"[PipePressurePuzzelPipesWireTool] Missing MultiDimension/cycler on '{path}'.");
                    return;
                }
            }

            MultiDimensionPuzzelManager puzzleManager = GameObject.Find($"{panelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzelManager>();
            if (puzzleManager == null)
            {
                Debug.LogError($"[PipePressurePuzzelPipesWireTool] Missing PuzzleManager on '{panelName}'.");
                return;
            }

            SerializedObject pmSo = new SerializedObject(puzzleManager);
            SerializedProperty elems = pmSo.FindProperty("puzzleElements");
            elems.arraySize = inputNames.Length;
            for (int i = 0; i < inputNames.Length; i++)
            {
                SerializedProperty el = elems.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("element").objectReferenceValue = dimensions[i];
                el.FindPropertyRelative("correctIndex").intValue = correctIndices[i];
            }

            MultiDimensionPuzzleInteractableBridge bridge =
                puzzleManager.GetComponent<MultiDimensionPuzzleInteractableBridge>();
            SolveInteractProxy solveProxy = GameObject.Find($"{panelName}/Buttons/SolveButton")
                ?.GetComponent<SolveInteractProxy>();

            SerializedProperty disableList = pmSo.FindProperty("interactionsToDisable");
            disableList.arraySize = inputNames.Length + 2;
            for (int i = 0; i < inputNames.Length; i++)
            {
                disableList.GetArrayElementAtIndex(i).objectReferenceValue = dimensions[i];
            }

            disableList.GetArrayElementAtIndex(inputNames.Length).objectReferenceValue = bridge;
            disableList.GetArrayElementAtIndex(inputNames.Length + 1).objectReferenceValue = solveProxy;
            pmSo.ApplyModifiedPropertiesWithoutUndo();

            MultiDimensionHistoryAdapter history = GameObject.Find(panelName)
                ?.GetComponent<MultiDimensionHistoryAdapter>();
            if (history != null)
            {
                SerializedObject histSo = new SerializedObject(history);
                SerializedProperty order = histSo.FindProperty("inputOrder");
                order.arraySize = inputNames.Length;
                for (int i = 0; i < inputNames.Length; i++)
                {
                    order.GetArrayElementAtIndex(i).objectReferenceValue = dimensions[i];
                }

                histSo.ApplyModifiedPropertiesWithoutUndo();
            }

            PanelFocusController focus = GameObject.Find($"{panelName}/Board")
                ?.GetComponent<PanelFocusController>();
            if (focus != null)
            {
                SerializedObject pfcSo = new SerializedObject(focus);
                SerializedProperty buttons = pfcSo.FindProperty("interactableButtons");
                buttons.arraySize = inputNames.Length;
                for (int i = 0; i < inputNames.Length; i++)
                {
                    GameObject inputGo = GameObject.Find($"{panelName}/Buttons/{inputNames[i]}");
                    SerializedProperty btn = buttons.GetArrayElementAtIndex(i);
                    btn.FindPropertyRelative("label").stringValue = inputNames[i];
                    btn.FindPropertyRelative("highlightAnchor").objectReferenceValue = inputGo.transform;
                    btn.FindPropertyRelative("interactableReference").objectReferenceValue = cyclers[i];
                }

                pfcSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void WireTurnLocks()
        {
            TutorialStageManager stageManager = Object.FindFirstObjectByType<TutorialStageManager>();
            if (stageManager == null)
            {
                Debug.LogWarning("[PipePressurePuzzelPipesWireTool] TutorialStageManager not found; skipped turn-lock colliders.");
                return;
            }

            SerializedObject tsmSo = new SerializedObject(stageManager);
            WireLockBundle(tsmSo, "playerAPanelLock", "Player1_Panel", new[] { "VALVE", "PRESS", "FLOW" });
            WireLockBundle(tsmSo, "playerBPanelLock", "Player2_Panel", new[] { "GATE", "PUMP", "ROUTE" });
            tsmSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireLockBundle(
            SerializedObject tsmSo,
            string bundleProperty,
            string panelName,
            string[] inputNames)
        {
            SerializedProperty bundle = tsmSo.FindProperty(bundleProperty);
            SerializedProperty colliders = bundle.FindPropertyRelative("actionColliders");
            colliders.arraySize = inputNames.Length + 1;

            for (int i = 0; i < inputNames.Length; i++)
            {
                colliders.GetArrayElementAtIndex(i).objectReferenceValue =
                    GetProbeCollider($"{panelName}/Buttons/{inputNames[i]}");
            }

            Collider solveCollider = GameObject.Find($"{panelName}/Buttons/SolveButton")
                ?.GetComponentInChildren<Collider>(true);
            colliders.GetArrayElementAtIndex(inputNames.Length).objectReferenceValue = solveCollider;
        }

        private static Collider GetProbeCollider(string inputPath)
        {
            MultiDimensionSubjectCycler cycler = GameObject.Find(inputPath)
                ?.GetComponent<MultiDimensionSubjectCycler>();
            if (cycler == null)
            {
                return null;
            }

            SerializedObject so = new SerializedObject(cycler);
            return so.FindProperty("dimensionProbe").objectReferenceValue as Collider;
        }
    }
}
#endif
