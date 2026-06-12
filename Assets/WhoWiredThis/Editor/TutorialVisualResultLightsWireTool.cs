#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Enums;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Wires cross-opponent <see cref="SplitResultTutorialController"/> bridges in Tutorial - Visual.unity only.
    /// </summary>
    public static class TutorialVisualResultLightsWireTool
    {
        private const string ScenePath = "Assets/Scenes/Tutorial - Visual.unity";
        private const string MenuPath = "Who Wired This/Tutorial/Wire Tutorial Visual Result Lights";
        private const string BridgeRootName = "TutorialVisual_ResultLights";

        public static void WireTutorialVisualResultLightsBatch()
        {
            EditorSceneManager.OpenScene(ScenePath);
            int issues = WireAllBridges();
            if (issues > 0)
            {
                Debug.LogError($"[TutorialVisualResultLightsWireTool] Batch wire finished with {issues} issue(s).");
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[TutorialVisualResultLightsWireTool] Batch wire complete.");
            EditorApplication.Exit(0);
        }

        [MenuItem(MenuPath)]
        public static void WireTutorialVisualResultLights()
        {
            if (!EnsureSceneActive())
            {
                return;
            }

            int issues = WireAllBridges();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log(issues == 0
                ? "[TutorialVisualResultLightsWireTool] Tutorial - Visual result lights wired."
                : $"[TutorialVisualResultLightsWireTool] Finished with {issues} warning(s). See console.");
        }

        private static int WireAllBridges()
        {
            int issues = 0;
            issues += WireBridge(
                bridgeName: "Bridge_A_to_B_lights",
                puzzlePanelName: "Player1_Panel-A",
                puzzleManagerChildName: "PuzzleManager-A",
                lightsPanelName: "Player2_Panel-B",
                visibleToPlayer: AllowedPlayerTag.Player_B);

            issues += WireBridge(
                bridgeName: "Bridge_B_to_A_lights",
                puzzlePanelName: "Player2_Panel-B",
                puzzleManagerChildName: "PuzzleManager-B",
                lightsPanelName: "Player1_Panel-A",
                visibleToPlayer: AllowedPlayerTag.Player_A);
            return issues;
        }

        private static bool EnsureSceneActive()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath)
            {
                return true;
            }

            if (!EditorUtility.DisplayDialog(
                    "Wire Tutorial Visual Result Lights",
                    "Open Tutorial - Visual.unity first. Open it now?",
                    "Open scene",
                    "Cancel"))
            {
                return false;
            }

            EditorSceneManager.OpenScene(ScenePath);
            return true;
        }

        private static int WireBridge(
            string bridgeName,
            string puzzlePanelName,
            string puzzleManagerChildName,
            string lightsPanelName,
            AllowedPlayerTag visibleToPlayer)
        {
            int issues = 0;

            GameObject bridgeRoot = GameObject.Find(BridgeRootName);
            if (bridgeRoot == null)
            {
                bridgeRoot = new GameObject(BridgeRootName);
            }

            Transform bridgeTransform = bridgeRoot.transform.Find(bridgeName);
            GameObject bridgeObject;
            if (bridgeTransform == null)
            {
                bridgeObject = new GameObject(bridgeName);
                bridgeObject.transform.SetParent(bridgeRoot.transform, worldPositionStays: false);
            }
            else
            {
                bridgeObject = bridgeTransform.gameObject;
            }

            SplitResultTutorialController controller =
                bridgeObject.GetComponent<SplitResultTutorialController>();
            if (controller == null)
            {
                controller = bridgeObject.AddComponent<SplitResultTutorialController>();
            }

            MultiDimensionPuzzleManager puzzleManager = FindChildComponent<MultiDimensionPuzzleManager>(
                puzzlePanelName,
                puzzleManagerChildName);
            if (puzzleManager == null)
            {
                Debug.LogError(
                    $"[TutorialVisualResultLightsWireTool] Missing {puzzleManagerChildName} under '{puzzlePanelName}'.");
                issues++;
            }

            MultiDimension settingsLight = FindResultLight(lightsPanelName, "ResultLight-Left");
            MultiDimension placesLight = FindResultLight(lightsPanelName, "ResultLight-Middle");
            if (settingsLight == null || placesLight == null)
            {
                Debug.LogError(
                    $"[TutorialVisualResultLightsWireTool] Missing ResultLight-Left/Middle under '{lightsPanelName}/ResultLight'.");
                issues++;
            }

            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("puzzleManager").objectReferenceValue = puzzleManager;
            so.FindProperty("settings").objectReferenceValue = settingsLight;
            so.FindProperty("places").objectReferenceValue = placesLight;
            so.FindProperty("updateContinuously").boolValue = false;
            so.FindProperty("visibleToPlayer").enumValueIndex = (int)visibleToPlayer;
            so.ApplyModifiedPropertiesWithoutUndo();

            return issues;
        }

        private static T FindChildComponent<T>(string rootName, string childName) where T : Component
        {
            GameObject root = GameObject.Find(rootName);
            if (root == null)
            {
                return null;
            }

            Transform child = root.transform.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static MultiDimension FindResultLight(string panelName, string lightName)
        {
            GameObject panel = GameObject.Find(panelName);
            if (panel == null)
            {
                return null;
            }

            Transform light = panel.transform.Find($"ResultLight/{lightName}");
            return light != null ? light.GetComponent<MultiDimension>() : null;
        }
    }
}
#endif
