#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using WhoWiredThis.Enums;
using WhoWiredThis.PanelFocus;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Tutorial;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Phase 1 wiring for Puzzle Signal: swap 4-state pipe inputs for 5-state signal prefabs and rebind managers.
    /// Menu: Who Wired This / Signal Calibration / Wire Puzzle Signal Phase 1
    /// </summary>
    public static class SignalCalibrationPuzzleSignalWireTool
    {
        private const string ScenePath = "Assets/Scenes/Puzzle Signal.unity";
        private const string MenuPath = "Who Wired This/Signal Calibration/Wire Puzzle Signal Phase 1";
        private const string Phase2MenuPath = "Who Wired This/Signal Calibration/Wire Puzzle Signal Phase 2 (Diagnostics)";
        private const string Phase3MenuPath = "Who Wired This/Signal Calibration/Wire Puzzle Signal Phase 3 (Random Solution)";

        private const string Knob5Path =
            "Assets/WhoWiredThis/Prefabs/MultiDimension/MultiDimension_Knob_5State.prefab";
        private const string Slider5Path =
            "Assets/WhoWiredThis/Prefabs/MultiDimension/MultiDimension_Slider_5State.prefab";
        private const string ButtonText5Path =
            "Assets/WhoWiredThis/Prefabs/MultiDimension/MultiDimension_ButtonText_5State.prefab";

        [MenuItem(MenuPath)]
        public static void WirePhase1()
        {
            if (!EnsurePuzzleSignalSceneActive())
            {
                return;
            }

            GameObject knob5 = LoadPrefab(Knob5Path);
            GameObject slider5 = LoadPrefab(Slider5Path);
            GameObject button5 = LoadPrefab(ButtonText5Path);
            if (knob5 == null || slider5 == null || button5 == null)
            {
                return;
            }

            ReplaceInput("Player1_Panel", "VALVE", "FREQ", knob5);
            ReplaceInput("Player1_Panel", "PRESS", "GAIN", slider5);
            ReplaceInput("Player1_Panel", "FLOW", "WAVE", button5);

            ReplaceInput("Player2_Panel", "GATE", "TUNE", knob5);
            ReplaceInput("Player2_Panel", "PUMP", "AMP", slider5);
            ReplaceInput("Player2_Panel", "ROUTE", "MODE", button5);

            WirePanel("Player1_Panel", new[] { "FREQ", "GAIN", "WAVE" }, new[] { 2, 2, 2 });
            WirePanel("Player2_Panel", new[] { "TUNE", "AMP", "MODE" }, new[] { 3, 2, 3 });

            RebindComponentDiagnostic("Player1_Panel", new[] { "FREQ", "GAIN", "WAVE" });
            RebindComponentDiagnostic("Player2_Panel", new[] { "TUNE", "AMP", "MODE" });

            RebindResultVisualizer("Player1_Panel", new[] { "FREQ", "GAIN", "WAVE" });
            RebindResultVisualizer("Player2_Panel", new[] { "TUNE", "AMP", "MODE" });

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[SignalCalibrationPuzzleSignalWireTool] Phase 1 complete on Puzzle Signal.");
        }

        [MenuItem(Phase2MenuPath)]
        public static void WirePhase2Diagnostics()
        {
            if (!EnsurePuzzleSignalSceneActive())
            {
                return;
            }

            WireComponentDiagnosticPanel(
                "Player1_Panel",
                new[] { "FREQ", "GAIN", "WAVE" },
                new[]
                {
                    (ComponentDiagnosticType.Ordered, "FREQ LOOKS STABLE.", "FREQ IS TOO LOW.", "FREQ IS TOO HIGH.", string.Empty),
                    (ComponentDiagnosticType.Ordered, "GAIN LOOKS STABLE.", "GAIN IS TOO LOW.", "GAIN IS TOO HIGH.", string.Empty),
                    (ComponentDiagnosticType.Categorical, "WAVE PATTERN MATCHES.", string.Empty, string.Empty, "WAVE PATTERN DOES NOT MATCH.")
                },
                "SIGNAL LINK CALIBRATED.",
                "SIGNAL IS UNSTABLE.",
                "ONE SIGNAL CHANNEL RESPONDS.",
                "SIGNAL IS CLOSE.");

            WireComponentDiagnosticPanel(
                "Player2_Panel",
                new[] { "TUNE", "AMP", "MODE" },
                new[]
                {
                    (ComponentDiagnosticType.Ordered, "TUNE LOOKS STABLE.", "TUNE IS TOO LOW.", "TUNE IS TOO HIGH.", string.Empty),
                    (ComponentDiagnosticType.Ordered, "AMP LOOKS STABLE.", "AMP IS TOO LOW.", "AMP IS TOO HIGH.", string.Empty),
                    (ComponentDiagnosticType.Categorical, "MODE PATTERN MATCHES.", string.Empty, string.Empty, "MODE PATTERN DOES NOT MATCH.")
                },
                "SIGNAL LINK CALIBRATED.",
                "SIGNAL IS UNSTABLE.",
                "ONE SIGNAL CHANNEL RESPONDS.",
                "SIGNAL IS CLOSE.");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[SignalCalibrationPuzzleSignalWireTool] Phase 2 signal diagnostics wired on Puzzle Signal.");
        }

        [MenuItem(Phase3MenuPath)]
        public static void WirePhase3RandomSolution()
        {
            if (!EnsurePuzzleSignalSceneActive())
            {
                return;
            }

            TutorialStageManager stageManager = Object.FindFirstObjectByType<TutorialStageManager>();
            if (stageManager == null)
            {
                Debug.LogError("[SignalCalibrationPuzzleSignalWireTool] TutorialStageManager not found in scene.");
                return;
            }

            RandomPuzzleSolutionAssigner assigner = stageManager.GetComponent<RandomPuzzleSolutionAssigner>();
            if (assigner == null)
            {
                assigner = Undo.AddComponent<RandomPuzzleSolutionAssigner>(stageManager.gameObject);
            }

            SerializedObject so = new SerializedObject(assigner);
            SerializedObject tsmSo = new SerializedObject(stageManager);
            so.FindProperty("playerAPuzzleManager").objectReferenceValue =
                tsmSo.FindProperty("playerAPuzzleManager").objectReferenceValue;
            so.FindProperty("playerBPuzzleManager").objectReferenceValue =
                tsmSo.FindProperty("playerBPuzzleManager").objectReferenceValue;
            so.FindProperty("enableRandomization").boolValue = true;
            so.FindProperty("useSeed").boolValue = false;
            so.FindProperty("seed").intValue = 0;
            so.FindProperty("logToConsole").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[SignalCalibrationPuzzleSignalWireTool] Phase 3 RandomPuzzleSolutionAssigner wired on Puzzle Signal.");
        }

        private static bool EnsurePuzzleSignalSceneActive()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning("[SignalCalibrationPuzzleSignalWireTool] Aborted — save cancelled.");
                    return false;
                }

                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            if (scene.path != ScenePath)
            {
                Debug.LogError(
                    $"[SignalCalibrationPuzzleSignalWireTool] Active scene must be '{ScenePath}', got '{scene.path}'.");
                return false;
            }

            return true;
        }

        private static GameObject LoadPrefab(string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogError($"[SignalCalibrationPuzzleSignalWireTool] Missing prefab at '{assetPath}'.");
            }

            return prefab;
        }

        private static void ReplaceInput(string panelName, string oldName, string newName, GameObject prefab)
        {
            string path = $"{panelName}/Buttons/{oldName}";
            GameObject existing = GameObject.Find(path);
            if (existing == null)
            {
                Debug.LogError($"[SignalCalibrationPuzzleSignalWireTool] Missing '{path}'.");
                return;
            }

            MultiDimension oldMd = existing.GetComponent<MultiDimension>();
            if (oldMd == null)
            {
                Debug.LogError($"[SignalCalibrationPuzzleSignalWireTool] No MultiDimension on '{path}'.");
                return;
            }

            SerializedObject oldSo = new SerializedObject(oldMd);
            var visibleToPlayer =
                (AllowedPlayerTag)oldSo.FindProperty("visibleToPlayer").enumValueIndex;
            int activeSubjectIndex = oldSo.FindProperty("activeSubjectIndex").intValue;

            Transform parent = existing.transform.parent;
            int siblingIndex = existing.transform.GetSiblingIndex();
            Vector3 localPosition = existing.transform.localPosition;
            Quaternion localRotation = existing.transform.localRotation;
            Vector3 localScale = existing.transform.localScale;
            int layer = existing.layer;

            Undo.DestroyObjectImmediate(existing);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "Signal 5-state input");
            Transform t = instance.transform;
            t.SetSiblingIndex(siblingIndex);
            t.localPosition = localPosition;
            t.localRotation = localRotation;
            t.localScale = localScale;
            instance.name = newName;
            ApplyLayerRecursively(instance, layer);

            MultiDimension md = instance.GetComponent<MultiDimension>();
            if (md != null)
            {
                SerializedObject mdSo = new SerializedObject(md);
                mdSo.FindProperty("visibleToPlayer").enumValueIndex = (int)visibleToPlayer;
                int max = Mathf.Max(md.SubjectCount - 1, 0);
                mdSo.FindProperty("activeSubjectIndex").intValue = Mathf.Clamp(activeSubjectIndex, 0, max);
                mdSo.ApplyModifiedPropertiesWithoutUndo();
            }

            SetPanelHeaderLabel(instance, newName);
        }

        private static void ApplyLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        private static void SetPanelHeaderLabel(GameObject inputRoot, string label)
        {
            foreach (TMP_Text tmp in inputRoot.GetComponentsInChildren<TMP_Text>(true))
            {
                string text = tmp.text?.Trim() ?? string.Empty;
                if (text.Length == 0)
                {
                    continue;
                }

                if (text == inputRoot.name || (text.Length <= 5 && char.IsUpper(text[0])))
                {
                    tmp.text = label;
                    EditorUtility.SetDirty(tmp);
                    return;
                }
            }
        }

        private static void WirePanel(string panelName, string[] inputNames, int[] correctIndices)
        {
            if (inputNames.Length != correctIndices.Length)
            {
                Debug.LogError("[SignalCalibrationPuzzleSignalWireTool] inputNames and correctIndices length mismatch.");
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
                    Debug.LogError($"[SignalCalibrationPuzzleSignalWireTool] Missing '{path}'.");
                    return;
                }

                dimensions[i] = inputGo.GetComponent<MultiDimension>();
                cyclers[i] = inputGo.GetComponent<MultiDimensionSubjectCycler>();
                if (dimensions[i] == null || cyclers[i] == null)
                {
                    Debug.LogError($"[SignalCalibrationPuzzleSignalWireTool] Missing MultiDimension/cycler on '{path}'.");
                    return;
                }
            }

            MultiDimensionPuzzleManager puzzleManager = GameObject.Find($"{panelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzleManager>();
            if (puzzleManager == null)
            {
                Debug.LogError($"[SignalCalibrationPuzzleSignalWireTool] Missing PuzzleManager on '{panelName}'.");
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

            WireTurnLockBundle(panelName, inputNames);
        }

        private static void WireTurnLockBundle(string panelName, string[] inputNames)
        {
            TutorialStageManager stageManager = Object.FindFirstObjectByType<TutorialStageManager>();
            if (stageManager == null)
            {
                Debug.LogWarning("[SignalCalibrationPuzzleSignalWireTool] TutorialStageManager not found; skipped turn locks.");
                return;
            }

            string bundleProperty = panelName == "Player1_Panel" ? "playerAPanelLock" : "playerBPanelLock";
            SerializedObject tsmSo = new SerializedObject(stageManager);
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
            tsmSo.ApplyModifiedPropertiesWithoutUndo();
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

        private static void WireComponentDiagnosticPanel(
            string panelName,
            string[] inputNames,
            (ComponentDiagnosticType type, string correct, string tooLow, string tooHigh, string mismatch)[] defs,
            string solvedMessage,
            string systemNoneCorrect,
            string systemOneCorrect,
            string systemTwoCorrect)
        {
            GameObject panel = GameObject.Find(panelName);
            if (panel == null)
            {
                Debug.LogError($"[SignalCalibrationPuzzleSignalWireTool] Missing '{panelName}'.");
                return;
            }

            ComponentDiagnosticAdapter adapter = panel.GetComponent<ComponentDiagnosticAdapter>();
            if (adapter == null)
            {
                Debug.LogError($"[SignalCalibrationPuzzleSignalWireTool] Missing ComponentDiagnosticAdapter on '{panelName}'.");
                return;
            }

            MultiDimensionPuzzleManager puzzleManager = GameObject.Find($"{panelName}/PuzzleManager")
                ?.GetComponent<MultiDimensionPuzzleManager>();
            SerializedObject adapterSo = new SerializedObject(adapter);
            DiagnosticDisplayController display =
                adapterSo.FindProperty("diagnosticDisplay").objectReferenceValue as DiagnosticDisplayController;

            if (puzzleManager == null || display == null)
            {
                Debug.LogError(
                    $"[SignalCalibrationPuzzleSignalWireTool] Missing puzzleManager or diagnosticDisplay for '{panelName}'.");
                return;
            }

            var dimensions = new MultiDimension[inputNames.Length];
            for (int i = 0; i < inputNames.Length; i++)
            {
                dimensions[i] = GameObject.Find($"{panelName}/Buttons/{inputNames[i]}")
                    ?.GetComponent<MultiDimension>();
            }

            adapterSo.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;
            adapterSo.FindProperty("diagnosticDisplay").objectReferenceValue = display;

            SerializedProperty componentsProp = adapterSo.FindProperty("components");
            componentsProp.arraySize = defs.Length;
            for (int i = 0; i < defs.Length; i++)
            {
                SerializedProperty entry = componentsProp.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("input").objectReferenceValue = dimensions[i];
                entry.FindPropertyRelative("diagnosticType").enumValueIndex = (int)defs[i].type;
                entry.FindPropertyRelative("correctText").stringValue = defs[i].correct;
                entry.FindPropertyRelative("tooLowText").stringValue = defs[i].tooLow;
                entry.FindPropertyRelative("tooHighText").stringValue = defs[i].tooHigh;
                entry.FindPropertyRelative("mismatchText").stringValue = defs[i].mismatch;
                entry.FindPropertyRelative("eligibleForHints").boolValue = true;
            }

            adapterSo.FindProperty("solvedMessage").stringValue = solvedMessage;
            adapterSo.FindProperty("systemNoneCorrect").stringValue = systemNoneCorrect;
            adapterSo.FindProperty("systemOneCorrect").stringValue = systemOneCorrect;
            adapterSo.FindProperty("systemTwoCorrect").stringValue = systemTwoCorrect;
            adapterSo.FindProperty("partnerLine").stringValue = "TELL YOUR PARTNER WHAT YOU LEARNED.";
            adapterSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RebindComponentDiagnostic(string panelName, string[] inputNames)
        {
            ComponentDiagnosticAdapter adapter = GameObject.Find(panelName)?.GetComponent<ComponentDiagnosticAdapter>();
            if (adapter == null)
            {
                return;
            }

            SerializedObject adapterSo = new SerializedObject(adapter);
            SerializedProperty componentsProp = adapterSo.FindProperty("components");
            if (componentsProp.arraySize != inputNames.Length)
            {
                componentsProp.arraySize = inputNames.Length;
            }

            for (int i = 0; i < inputNames.Length; i++)
            {
                MultiDimension md = GameObject.Find($"{panelName}/Buttons/{inputNames[i]}")
                    ?.GetComponent<MultiDimension>();
                componentsProp.GetArrayElementAtIndex(i).FindPropertyRelative("input").objectReferenceValue = md;
            }

            adapterSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RebindResultVisualizer(string panelName, string[] inputNames)
        {
            SubmittedCombinationVisualizer visualizer =
                GameObject.Find(panelName)?.GetComponent<SubmittedCombinationVisualizer>();
            if (visualizer == null)
            {
                return;
            }

            SerializedObject vizSo = new SerializedObject(visualizer);
            SerializedProperty slotsProp = vizSo.FindProperty("slots");
            if (slotsProp.arraySize != inputNames.Length)
            {
                slotsProp.arraySize = inputNames.Length;
            }

            for (int i = 0; i < inputNames.Length; i++)
            {
                MultiDimension md = GameObject.Find($"{panelName}/Buttons/{inputNames[i]}")
                    ?.GetComponent<MultiDimension>();
                SerializedProperty slot = slotsProp.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("label").stringValue = inputNames[i];
                slot.FindPropertyRelative("sourceInput").objectReferenceValue = md;
            }

            vizSo.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
