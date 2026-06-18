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
        private const string ScenePath = "Assets/Scenes/Game/Puzzle Signal.unity";
        private const string SignalPanelAName = "Player1_Signal_Panel-A";
        private const string SignalPanelBName = "Player2_Signal_Panel-B";
        private const string FullWireMenuPath = "Who Wired This/Signal Calibration/Wire Puzzle Signal Full Scene";
        private const string FullWireMcpMenuPath = "Who Wired This/Signal Calibration/MCP/Wire Puzzle Signal Full Scene";
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

        [MenuItem(FullWireMenuPath)]
        public static void WirePuzzleSignalFullScene()
        {
            WirePuzzleSignalFullSceneInternal(showDialog: true);
        }

        [MenuItem(FullWireMcpMenuPath)]
        public static void WirePuzzleSignalFullSceneForMcp()
        {
            WirePuzzleSignalFullSceneInternal(showDialog: false);
            EditorSceneManager.SaveOpenScenes();
        }

        private static void WirePuzzleSignalFullSceneInternal(bool showDialog)
        {
            if (!EnsurePuzzleSignalSceneActive())
            {
                return;
            }

            if (!TryGetSignalPanels(out GameObject panelA, out GameObject panelB))
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Puzzle Signal Wire",
                        $"Open '{ScenePath}' with '{SignalPanelAName}' and '{SignalPanelBName}' in the hierarchy.",
                        "OK");
                }

                return;
            }

            WireCrossPartnerDiagnosticAndFocusInternal(markSceneDirty: false);
            WirePanelActionLocks(panelA);
            WirePanelActionLocks(panelB);
            EnsureLocalBridgePuzzleManager(panelA);
            EnsureLocalBridgePuzzleManager(panelB);
            EnsurePuzzleCorrectIndices(panelA, new[] { 2, 2, 2 });
            EnsurePuzzleCorrectIndices(panelB, new[] { 3, 2, 3 });
            EnsureLeverFeedback(panelA);
            EnsureLeverFeedback(panelB);
            WireSimultaneousOperatorMode();
            WireTurnLocksForSignalPanels(panelA, panelB);
            int resultIssues = SignalCalibrationPuzzleSignalResultWireTool.WireAllResultFeedback();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log(resultIssues == 0
                ? "[SignalCalibrationPuzzleSignalWireTool] Full Puzzle Signal wire complete (focus, diagnostics, turn locks, bridges, result feedback)."
                : $"[SignalCalibrationPuzzleSignalWireTool] Full wire finished with {resultIssues} result-feedback issue(s). See console.");
        }

        private static bool TryGetSignalPanels(out GameObject panelA, out GameObject panelB)
        {
            panelA = GameObject.Find(SignalPanelAName);
            panelB = GameObject.Find(SignalPanelBName);
            return panelA != null && panelB != null;
        }

        private static void WireCrossPartnerDiagnosticAndFocusInternal(bool markSceneDirty)
        {
            if (!TryGetSignalPanels(out GameObject panelA, out GameObject panelB))
            {
                Debug.LogError(
                    $"[SignalCalibrationPuzzleSignalWireTool] Missing '{SignalPanelAName}' or '{SignalPanelBName}'.");
                return;
            }

            WireCrossPartnerOperatorSide(
                panelA,
                panelB,
                new[] { "FREQ", "GAIN", "WAVE" },
                new[]
                {
                    (ComponentDiagnosticType.Ordered, "FREQ LOOKS STABLE.", "FREQ IS TOO LOW.", "FREQ IS TOO HIGH.", string.Empty),
                    (ComponentDiagnosticType.Ordered, "GAIN LOOKS STABLE.", "GAIN IS TOO LOW.", "GAIN IS TOO HIGH.", string.Empty),
                    (ComponentDiagnosticType.Categorical, "WAVE PATTERN MATCHES.", string.Empty, string.Empty, "WAVE PATTERN DOES NOT MATCH.")
                },
                AllowedPlayerTag.Player_B);

            WireCrossPartnerOperatorSide(
                panelB,
                panelA,
                new[] { "TUNE", "AMP", "MODE" },
                new[]
                {
                    (ComponentDiagnosticType.Ordered, "TUNE LOOKS STABLE.", "TUNE IS TOO LOW.", "TUNE IS TOO HIGH.", string.Empty),
                    (ComponentDiagnosticType.Ordered, "AMP LOOKS STABLE.", "AMP IS TOO LOW.", "AMP IS TOO HIGH.", string.Empty),
                    (ComponentDiagnosticType.Categorical, "MODE PATTERN MATCHES.", string.Empty, string.Empty, "MODE PATTERN DOES NOT MATCH.")
                },
                AllowedPlayerTag.Player_A);

            EnsurePanelFocusReady(panelA, AllowedPlayerTag.Player_A);
            EnsurePanelFocusReady(panelB, AllowedPlayerTag.Player_B);
            WireInitialPanelFocusBootstrap(panelA, panelB);

            if (markSceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            Debug.Log("[SignalCalibrationPuzzleSignalWireTool] Wired cross-partner diagnostics and startup panel focus.");
        }

        private static void WireCrossPartnerOperatorSide(
            GameObject operatorPanel,
            GameObject partnerPanel,
            string[] inputNames,
            (ComponentDiagnosticType type, string correct, string tooLow, string tooHigh, string mismatch)[] defs,
            AllowedPlayerTag partnerDiagnosticVisibleTo)
        {
            DiagnosticDisplayController partnerDisplay =
                partnerPanel.GetComponentInChildren<DiagnosticDisplayController>(true);
            if (partnerDisplay == null)
            {
                Debug.LogError(
                    $"[SignalCalibrationPuzzleSignalWireTool] No DiagnosticDisplayController under '{partnerPanel.name}'.");
                return;
            }

            ComponentDiagnosticAdapter adapter = operatorPanel.GetComponent<ComponentDiagnosticAdapter>();
            if (adapter == null && inputNames != null && defs != null)
            {
                WireComponentDiagnosticPanel(operatorPanel, inputNames, defs);
                adapter = operatorPanel.GetComponent<ComponentDiagnosticAdapter>();
            }

            ProcessingFeedbackController feedback =
                operatorPanel.GetComponentInChildren<ProcessingFeedbackController>(true);
            if (adapter != null)
            {
                SerializedObject adapterSo = new SerializedObject(adapter);
                adapterSo.FindProperty("diagnosticDisplay").objectReferenceValue = partnerDisplay;
                adapterSo.ApplyModifiedPropertiesWithoutUndo();
            }

            if (feedback != null)
            {
                SerializedObject feedbackSo = new SerializedObject(feedback);
                feedbackSo.FindProperty("diagnosticDisplay").objectReferenceValue = partnerDisplay;
                feedbackSo.ApplyModifiedPropertiesWithoutUndo();
            }

            MultiDimensionRecursive visibility =
                partnerDisplay.GetComponentInChildren<MultiDimensionRecursive>(true);
            if (visibility != null)
            {
                SerializedObject visSo = new SerializedObject(visibility);
                visSo.FindProperty("visibleToPlayer").enumValueIndex = (int)partnerDiagnosticVisibleTo;
                visSo.ApplyModifiedPropertiesWithoutUndo();
                visibility.ApplyConfiguration();
            }
        }

        private static void WirePanelActionLocks(GameObject panel)
        {
            PanelActionLock panelLock = panel.GetComponent<PanelActionLock>();
            if (panelLock == null)
            {
                Debug.LogWarning($"[SignalCalibrationPuzzleSignalWireTool] No PanelActionLock on '{panel.name}'.");
                return;
            }

            foreach (PanelFocusController focus in panel.GetComponentsInChildren<PanelFocusController>(true))
            {
                SerializedObject focusSo = new SerializedObject(focus);
                focusSo.FindProperty("panelActionLock").objectReferenceValue = panelLock;
                focusSo.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (MultiDimensionPuzzleInteractableBridge bridge in panel.GetComponentsInChildren<MultiDimensionPuzzleInteractableBridge>(true))
            {
                SerializedObject bridgeSo = new SerializedObject(bridge);
                bridgeSo.FindProperty("panelActionLock").objectReferenceValue = panelLock;
                bridgeSo.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach (SolveInteractProxy proxy in panel.GetComponentsInChildren<SolveInteractProxy>(true))
            {
                SerializedObject proxySo = new SerializedObject(proxy);
                proxySo.FindProperty("panelActionLock").objectReferenceValue = panelLock;
                proxySo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureLocalBridgePuzzleManager(GameObject panel)
        {
            SubmittedCombinationMultiDimensionBridge bridge =
                panel.GetComponentInChildren<SubmittedCombinationMultiDimensionBridge>(true);
            MultiDimensionPuzzleManager puzzleManager =
                panel.GetComponentInChildren<MultiDimensionPuzzleManager>(true);
            if (bridge == null || puzzleManager == null)
            {
                return;
            }

            SerializedObject bridgeSo = new SerializedObject(bridge);
            bridgeSo.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;
            bridgeSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireSimultaneousOperatorMode()
        {
            TutorialStageManager stageManager = Object.FindFirstObjectByType<TutorialStageManager>();
            if (stageManager == null)
            {
                Debug.LogWarning("[SignalCalibrationPuzzleSignalWireTool] TutorialStageManager not found; skipped simultaneous operator mode.");
                return;
            }

            SerializedObject tsmSo = new SerializedObject(stageManager);
            tsmSo.FindProperty("simultaneousOperators").boolValue = true;
            tsmSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureLeverFeedback(GameObject panel)
        {
            MultiDimensionPuzzleInteractableBridge bridge =
                panel.GetComponentInChildren<MultiDimensionPuzzleInteractableBridge>(true);
            SubmitLeverMultiDimensionFeedback leverFeedback =
                panel.GetComponentInChildren<SubmitLeverMultiDimensionFeedback>(true);
            if (bridge == null || leverFeedback == null)
            {
                return;
            }

            SerializedObject bridgeSo = new SerializedObject(bridge);
            if (bridgeSo.FindProperty("leverFeedback").objectReferenceValue == null)
            {
                bridgeSo.FindProperty("leverFeedback").objectReferenceValue = leverFeedback;
                bridgeSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsurePuzzleCorrectIndices(GameObject panel, int[] correctIndices)
        {
            MultiDimensionPuzzleManager puzzleManager =
                panel.GetComponentInChildren<MultiDimensionPuzzleManager>(true);
            if (puzzleManager == null || correctIndices == null)
            {
                return;
            }

            SerializedObject pmSo = new SerializedObject(puzzleManager);
            SerializedProperty elems = pmSo.FindProperty("puzzleElements");
            if (elems.arraySize != correctIndices.Length)
            {
                return;
            }

            for (int i = 0; i < correctIndices.Length; i++)
            {
                elems.GetArrayElementAtIndex(i).FindPropertyRelative("correctIndex").intValue = correctIndices[i];
            }

            pmSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireTurnLocksForSignalPanels(GameObject panelA, GameObject panelB)
        {
            TutorialStageManager stageManager = Object.FindFirstObjectByType<TutorialStageManager>();
            if (stageManager == null)
            {
                Debug.LogWarning("[SignalCalibrationPuzzleSignalWireTool] TutorialStageManager not found; skipped turn locks.");
                return;
            }

            SerializedObject tsmSo = new SerializedObject(stageManager);
            WireLockBundleFromPanel(tsmSo, "playerAPanelLock", panelA);
            WireLockBundleFromPanel(tsmSo, "playerBPanelLock", panelB);
            tsmSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireLockBundleFromPanel(SerializedObject tsmSo, string bundleProperty, GameObject panel)
        {
            PanelFocusController focus = panel.GetComponentInChildren<PanelFocusController>(true);
            if (focus == null)
            {
                Debug.LogWarning($"[SignalCalibrationPuzzleSignalWireTool] No PanelFocusController under '{panel.name}' for turn lock.");
                return;
            }

            PanelActionLock panelLock = panel.GetComponent<PanelActionLock>();
            SerializedObject focusSo = new SerializedObject(focus);
            SerializedProperty buttons = focusSo.FindProperty("interactableButtons");
            int inputCount = buttons.arraySize;

            SerializedProperty bundle = tsmSo.FindProperty(bundleProperty);
            SerializedProperty colliders = bundle.FindPropertyRelative("actionColliders");
            colliders.arraySize = inputCount + 1;
            bundle.FindPropertyRelative("panelActionLock").objectReferenceValue = panelLock;

            for (int i = 0; i < inputCount; i++)
            {
                var cycler = buttons.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("interactableReference")
                    .objectReferenceValue as MultiDimensionSubjectCycler;
                colliders.GetArrayElementAtIndex(i).objectReferenceValue = GetProbeColliderFromCycler(cycler);
            }

            SolveInteractProxy solveProxy = panel.GetComponentInChildren<SolveInteractProxy>(true);
            Collider solveCollider = solveProxy != null
                ? solveProxy.GetComponentInChildren<Collider>(true)
                : null;
            colliders.GetArrayElementAtIndex(inputCount).objectReferenceValue = solveCollider;
        }

        private static Collider GetProbeColliderFromCycler(MultiDimensionSubjectCycler cycler)
        {
            if (cycler == null)
            {
                return null;
            }

            SerializedObject so = new SerializedObject(cycler);
            return so.FindProperty("dimensionProbe").objectReferenceValue as Collider;
        }

        private static void EnsurePanelFocusReady(GameObject panel, AllowedPlayerTag allowedPlayer)
        {
            PanelFocusController focus = panel.GetComponentInChildren<PanelFocusController>(true);
            if (focus == null)
            {
                Debug.LogError($"[SignalCalibrationPuzzleSignalWireTool] Missing PanelFocusController under '{panel.name}'.");
                return;
            }

            SerializedObject focusSo = new SerializedObject(focus);
            focusSo.FindProperty("allowedPlayerId").enumValueIndex = (int)allowedPlayer;
            focusSo.FindProperty("includeExitInFocusCycle").boolValue = false;

            SerializedProperty boardRendererProp = focusSo.FindProperty("boardRenderer");
            if (boardRendererProp.objectReferenceValue == null)
            {
                Renderer boardRenderer = focus.GetComponent<MeshRenderer>();
                if (boardRenderer == null)
                {
                    boardRenderer = focus.GetComponentInChildren<MeshRenderer>(true);
                }

                if (boardRenderer != null)
                {
                    boardRendererProp.objectReferenceValue = boardRenderer;
                }
            }
            else if (boardRendererProp.objectReferenceValue is Renderer existing && !existing.enabled)
            {
                existing.enabled = true;
            }

            SolveInteractProxy solveProxy = panel.GetComponentInChildren<SolveInteractProxy>(true);
            if (solveProxy != null)
            {
                SerializedProperty solveButtonProp = focusSo.FindProperty("solveButton");
                SerializedProperty interactableRef = solveButtonProp.FindPropertyRelative("interactableReference");
                interactableRef.objectReferenceValue = solveProxy;
            }

            SerializedProperty selectionFrameProp = focusSo.FindProperty("selectionFrame");
            if (selectionFrameProp.objectReferenceValue == null)
            {
                Transform selectionBorder = FindChildTransform(panel.transform, "SelectionBorder");
                if (selectionBorder != null)
                {
                    selectionFrameProp.objectReferenceValue = selectionBorder.gameObject;
                }
            }

            SerializedProperty buttons = focusSo.FindProperty("interactableButtons");
            for (int i = 0; i < buttons.arraySize; i++)
            {
                SerializedProperty button = buttons.GetArrayElementAtIndex(i);
                string label = button.FindPropertyRelative("label").stringValue;
                if (string.IsNullOrEmpty(label))
                {
                    continue;
                }

                Transform inputTransform = FindChildTransform(panel.transform, label);
                if (inputTransform == null)
                {
                    continue;
                }

                SerializedProperty anchorProp = button.FindPropertyRelative("highlightAnchor");
                if (anchorProp.objectReferenceValue == null)
                {
                    anchorProp.objectReferenceValue = inputTransform;
                }

                SerializedProperty interactableRef = button.FindPropertyRelative("interactableReference");
                if (interactableRef.objectReferenceValue == null)
                {
                    MultiDimensionSubjectCycler cycler = inputTransform.GetComponent<MultiDimensionSubjectCycler>();
                    if (cycler != null)
                    {
                        interactableRef.objectReferenceValue = cycler;
                    }
                }
            }

            focusSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireInitialPanelFocusBootstrap(GameObject panelA, GameObject panelB)
        {
            InitialPanelFocusBootstrap bootstrap = Object.FindFirstObjectByType<InitialPanelFocusBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogWarning("[SignalCalibrationPuzzleSignalWireTool] No InitialPanelFocusBootstrap in scene.");
                return;
            }

            PlayerPanelFocusController playerAFocus =
                GameObject.Find("FirstPersonPlayer_A")?.GetComponent<PlayerPanelFocusController>();
            PlayerPanelFocusController playerBFocus =
                GameObject.Find("FirstPersonPlayer_B")?.GetComponent<PlayerPanelFocusController>();
            PanelFocusController panelAFocus = panelA.GetComponentInChildren<PanelFocusController>(true);
            PanelFocusController panelBFocus = panelB.GetComponentInChildren<PanelFocusController>(true);

            SerializedObject bootstrapSo = new SerializedObject(bootstrap);
            bootstrapSo.FindProperty("enterFocusOnStartup").boolValue = true;
            bootstrapSo.FindProperty("playerAFocus").objectReferenceValue = playerAFocus;
            bootstrapSo.FindProperty("playerAPanel").objectReferenceValue = panelAFocus;
            bootstrapSo.FindProperty("playerBFocus").objectReferenceValue = playerBFocus;
            bootstrapSo.FindProperty("playerBPanel").objectReferenceValue = panelBFocus;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
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
            GameObject panel,
            string[] inputNames,
            (ComponentDiagnosticType type, string correct, string tooLow, string tooHigh, string mismatch)[] defs)
        {
            if (panel == null)
            {
                return;
            }

            ComponentDiagnosticAdapter adapter = panel.GetComponent<ComponentDiagnosticAdapter>();
            if (adapter == null)
            {
                adapter = panel.AddComponent<ComponentDiagnosticAdapter>();
            }

            MultiDimensionPuzzleManager puzzleManager =
                panel.GetComponentInChildren<MultiDimensionPuzzleManager>(true);
            DiagnosticDisplayController display =
                panel.GetComponentInChildren<DiagnosticDisplayController>(true);

            if (puzzleManager == null || display == null)
            {
                Debug.LogError(
                    $"[SignalCalibrationPuzzleSignalWireTool] Missing puzzleManager or diagnosticDisplay for '{panel.name}'.");
                return;
            }

            var dimensions = new MultiDimension[inputNames.Length];
            for (int i = 0; i < inputNames.Length; i++)
            {
                Transform inputTransform = FindChildTransform(panel.transform, inputNames[i]);
                dimensions[i] = inputTransform?.GetComponent<MultiDimension>();
            }

            SerializedObject adapterSo = new SerializedObject(adapter);
            adapterSo.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;

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

            adapterSo.FindProperty("solvedMessage").stringValue = "SIGNAL LINK CALIBRATED.";
            adapterSo.FindProperty("systemNoneCorrect").stringValue = "SIGNAL IS UNSTABLE.";
            adapterSo.FindProperty("systemOneCorrect").stringValue = "ONE SIGNAL CHANNEL RESPONDS.";
            adapterSo.FindProperty("systemTwoCorrect").stringValue = "SIGNAL IS CLOSE.";
            adapterSo.FindProperty("partnerLine").stringValue = "TELL YOUR PARTNER WHAT YOU LEARNED.";
            adapterSo.ApplyModifiedPropertiesWithoutUndo();
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

            WireComponentDiagnosticPanel(panel, inputNames, defs);

            ComponentDiagnosticAdapter adapter = panel.GetComponent<ComponentDiagnosticAdapter>();
            if (adapter == null)
            {
                return;
            }

            SerializedObject adapterSo = new SerializedObject(adapter);
            adapterSo.FindProperty("solvedMessage").stringValue = solvedMessage;
            adapterSo.FindProperty("systemNoneCorrect").stringValue = systemNoneCorrect;
            adapterSo.FindProperty("systemOneCorrect").stringValue = systemOneCorrect;
            adapterSo.FindProperty("systemTwoCorrect").stringValue = systemTwoCorrect;
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
