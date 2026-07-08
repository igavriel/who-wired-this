#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Puzzles.Diagnostics;
using WhoWiredThis.Scenes;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Wires <see cref="TutorialDiagnosticController"/> on Tutorial.unity operator panels with
    /// cross-partner diagnostic displays. Disables legacy <see cref="MultiDimensionDiagnosticAdapter"/>.
    /// </summary>
    public static class TutorialDiagnosticWireTool
    {
        private const string TutorialScenePath = "Assets/Scenes/Game/Tutorial.unity";
        private const string MenuPath = "Who Wired This/Tutorial/Wire Decode Matrix Diagnostic";

        [MenuItem(MenuPath)]
        public static void WireTutorialDecodeMatrixDiagnostic()
        {
            if (!EnsureTutorialSceneActive())
            {
                return;
            }

            SceneStageManager stageManager = Object.FindFirstObjectByType<SceneStageManager>();
            if (stageManager == null)
            {
                Debug.LogError("[TutorialDiagnosticWireTool] SceneStageManager not found in scene.");
                return;
            }

            SerializedObject stageSo = new SerializedObject(stageManager);
            MultiDimensionPuzzleManager playerAPuzzle = stageSo.FindProperty("playerAPuzzleManager").objectReferenceValue
                as MultiDimensionPuzzleManager;
            MultiDimensionPuzzleManager playerBPuzzle = stageSo.FindProperty("playerBPuzzleManager").objectReferenceValue
                as MultiDimensionPuzzleManager;

            if (playerAPuzzle == null || playerBPuzzle == null)
            {
                Debug.LogError("[TutorialDiagnosticWireTool] Missing puzzle managers on SceneStageManager.");
                return;
            }

            DiagnosticDisplayController playerAMonitor = ResolveMonitorDisplay(GetPanelRoot(playerAPuzzle));
            DiagnosticDisplayController playerBMonitor = ResolveMonitorDisplay(GetPanelRoot(playerBPuzzle));
            if (playerAMonitor == null || playerBMonitor == null)
            {
                Debug.LogError(
                    "[TutorialDiagnosticWireTool] Could not find active Monitor diagnostic display on one or both panels.");
                return;
            }

            stageSo.FindProperty("playerADiagnosticDisplay").objectReferenceValue = playerAMonitor;
            stageSo.FindProperty("playerBDiagnosticDisplay").objectReferenceValue = playerBMonitor;
            stageSo.ApplyModifiedPropertiesWithoutUndo();

            int issues = 0;
            issues += WireOperatorPanel(playerAPuzzle, playerBMonitor, isPlayerA: true);
            issues += WireOperatorPanel(playerBPuzzle, playerAMonitor, isPlayerA: false);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log(issues == 0
                ? "[TutorialDiagnosticWireTool] Tutorial decode-matrix diagnostic wired to Monitor displays."
                : $"[TutorialDiagnosticWireTool] Finished with {issues} warning(s). See console.");
        }

        /// <summary>
        /// Active diagnostic readout for a panel: prefers *Monitor* displays, skips _OLD_* and inactive objects.
        /// </summary>
        internal static DiagnosticDisplayController ResolveMonitorDisplay(GameObject panelRoot)
        {
            if (panelRoot == null)
            {
                return null;
            }

            DiagnosticDisplayController[] displays = panelRoot.GetComponentsInChildren<DiagnosticDisplayController>(true);
            DiagnosticDisplayController fallback = null;

            for (int i = 0; i < displays.Length; i++)
            {
                DiagnosticDisplayController display = displays[i];
                if (display == null)
                {
                    continue;
                }

                GameObject host = display.gameObject;
                if (host.name.StartsWith("_OLD_"))
                {
                    continue;
                }

                if (!host.activeInHierarchy)
                {
                    continue;
                }

                if (host.name.IndexOf("Monitor", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return display;
                }

                fallback ??= display;
            }

            return fallback;
        }

        internal static GameObject GetPanelRoot(MultiDimensionPuzzleManager puzzleManager)
        {
            GameObject panel = puzzleManager.gameObject;
            while (panel.transform.parent != null)
            {
                panel = panel.transform.parent.gameObject;
            }

            return panel;
        }

        private static bool EnsureTutorialSceneActive()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == TutorialScenePath)
            {
                return true;
            }

            if (!EditorUtility.DisplayDialog(
                    "Wire Decode Matrix Diagnostic",
                    "Open Tutorial.unity first. Open it now?",
                    "Open scene",
                    "Cancel"))
            {
                return false;
            }

            EditorSceneManager.OpenScene(TutorialScenePath);
            return true;
        }

        private static int WireOperatorPanel(
            MultiDimensionPuzzleManager puzzleManager,
            DiagnosticDisplayController partnerDisplay,
            bool isPlayerA)
        {
            if (puzzleManager == null || partnerDisplay == null)
            {
                return 1;
            }

            GameObject panel = GetPanelRoot(puzzleManager);
            DisableLegacyAdapter(panel);
            WireProcessingFeedback(panel, partnerDisplay);

            TutorialDiagnosticController controller = panel.GetComponent<TutorialDiagnosticController>();
            if (controller == null)
            {
                controller = panel.AddComponent<TutorialDiagnosticController>();
            }

            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;
            controllerSo.FindProperty("display").objectReferenceValue = partnerDisplay;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            controller.enabled = true;

            Debug.Log(
                $"[TutorialDiagnosticWireTool] Wired {(isPlayerA ? "Player A" : "Player B")} operator panel '{panel.name}' " +
                $"-> partner display '{partnerDisplay.name}'.");
            return 0;
        }

        private static void WireProcessingFeedback(GameObject operatorPanel, DiagnosticDisplayController partnerDisplay)
        {
            ProcessingFeedbackController processing = operatorPanel.GetComponent<ProcessingFeedbackController>();
            if (processing == null || partnerDisplay == null)
            {
                return;
            }

            SerializedObject processingSo = new SerializedObject(processing);
            processingSo.FindProperty("diagnosticDisplay").objectReferenceValue = partnerDisplay;
            processingSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DisableLegacyAdapter(GameObject panel)
        {
            MultiDimensionDiagnosticAdapter legacy = panel.GetComponent<MultiDimensionDiagnosticAdapter>();
            if (legacy == null)
            {
                return;
            }

            SerializedObject legacySo = new SerializedObject(legacy);
            legacySo.FindProperty("m_Enabled").boolValue = false;
            legacySo.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[TutorialDiagnosticWireTool] Disabled legacy MultiDimensionDiagnosticAdapter on '{panel.name}'.");
        }
    }
}
#endif
