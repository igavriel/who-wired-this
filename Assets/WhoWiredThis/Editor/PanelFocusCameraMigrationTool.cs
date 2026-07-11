using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Enums;
using WhoWiredThis.PanelFocus;

namespace WhoWiredThis.Editor
{
    public static class PanelFocusCameraMigrationTool
    {
        private const string TutorialScenePath = "Assets/Scenes/Game/Tutorial.unity";

        private static readonly string[] PrefabPaths =
        {
            "Assets/WhoWiredThis/Prefabs/Panels/Tutorial_A V1.prefab",
            "Assets/WhoWiredThis/Prefabs/Panels/Tutorial_B V1 Variant.prefab",
            "Assets/WhoWiredThis/Prefabs/Panels/Player1_Pipes_Panel.prefab",
            "Assets/WhoWiredThis/Prefabs/Panels/Player1_Signal_Panel.prefab",
        };

        [MenuItem("Who Wired This/Panel Focus/Migrate All PanelFocusCamera")]
        public static void MigrateAllPanelFocusCamera()
        {
            int prefabBoards = 0;
            foreach (string prefabPath in PrefabPaths)
            {
                if (File.Exists(prefabPath))
                {
                    prefabBoards += MigratePrefabBoards(prefabPath);
                }
            }

            int scenesProcessed = 0;
            int scenesWired = 0;
            foreach (string scenePath in FindBootstrapScenePaths())
            {
                if (!ProcessBootstrapScene(scenePath))
                {
                    continue;
                }

                scenesProcessed++;
                scenesWired++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[PanelFocusCameraMigrationTool] All-scenes migration complete. " +
                $"Prefab boards migrated: {prefabBoards}. Bootstrap scenes processed: {scenesProcessed}.");
        }

        [MenuItem("Who Wired This/Panel Focus/Wire Tutorial Bootstrap PanelFocusCamera")]
        public static void WireTutorialBootstrapPanelFocusCamera()
        {
            if (ProcessBootstrapScene(TutorialScenePath))
            {
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("Who Wired This/Panel Focus/Migrate Tutorial PanelFocusCamera")]
        public static void MigrateTutorialPanelFocusCamera()
        {
            int migratedV1 = MigratePrefabBoards(PrefabPaths[0]);
            int migratedB = MigratePrefabBoards(PrefabPaths[1]);
            ProcessBootstrapScene(TutorialScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"[PanelFocusCameraMigrationTool] Tutorial migration complete. Migrated boards: V1={migratedV1}, B variant={migratedB}.");
        }

        private static IEnumerable<string> FindBootstrapScenePaths()
        {
            var results = new HashSet<string>();

            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    continue;
                }

                if (File.ReadAllText(path).Contains("InitialPanelFocusBootstrap"))
                {
                    results.Add(path);
                }
            }

            foreach (string path in Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories))
            {
                string normalized = path.Replace('\\', '/');
                if (File.ReadAllText(normalized).Contains("InitialPanelFocusBootstrap"))
                {
                    results.Add(normalized);
                }
            }

            var sorted = new List<string>(results);
            sorted.Sort();
            return sorted;
        }

        private static bool ProcessBootstrapScene(string scenePath)
        {
            if (!File.Exists(scenePath))
            {
                return false;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int syncedBoards = SyncSceneBoardCameraFieldsFromController();
            bool wired = WireBootstrapCameraRefsInOpenScene();
            ApplyTutorialPlayerAOverridesIfNeeded(scenePath);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(
                $"[PanelFocusCameraMigrationTool] Scene '{scenePath}': synced {syncedBoards} board camera(s), bootstrap wired={wired}.",
                AssetDatabase.LoadAssetAtPath<Object>(scenePath));
            return true;
        }

        private static int MigratePrefabBoards(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError($"[PanelFocusCameraMigrationTool] Failed to load prefab '{prefabPath}'.");
                return 0;
            }

            int count = 0;
            try
            {
                PanelFocusController[] controllers = root.GetComponentsInChildren<PanelFocusController>(true);
                foreach (PanelFocusController controller in controllers)
                {
                    if (MigrateBoardController(controller))
                    {
                        count++;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return count;
        }

        private static bool MigrateBoardController(PanelFocusController controller)
        {
            if (controller == null)
            {
                return false;
            }

            GameObject board = controller.gameObject;
            PanelFocusCamera camera = board.GetComponent<PanelFocusCamera>();
            if (camera == null)
            {
                camera = board.AddComponent<PanelFocusCamera>();
            }

            CopyCameraFieldsFromControllerToCamera(controller, camera);
            return true;
        }

        private static int SyncSceneBoardCameraFieldsFromController()
        {
            int count = 0;
            PanelFocusController[] controllers = Object.FindObjectsByType<PanelFocusController>(FindObjectsSortMode.None);
            foreach (PanelFocusController controller in controllers)
            {
                if (controller == null)
                {
                    continue;
                }

                PanelFocusCamera camera = controller.GetComponent<PanelFocusCamera>();
                if (camera == null)
                {
                    camera = controller.gameObject.AddComponent<PanelFocusCamera>();
                }

                CopyCameraFieldsFromControllerToCamera(controller, camera);
                EditorUtility.SetDirty(controller.gameObject);
                count++;
            }

            return count;
        }

        private static void CopyCameraFieldsFromControllerToCamera(PanelFocusController controller, PanelFocusCamera camera)
        {
            SerializedObject controllerSo = new SerializedObject(controller);
            SerializedObject cameraSo = new SerializedObject(camera);

            CopyCameraField(controllerSo, cameraSo, "frameFillPercent");
            CopyCameraField(controllerSo, cameraSo, "boardRenderer");
            CopyCameraField(controllerSo, cameraSo, "framingTransform");
            CopyCameraField(controllerSo, cameraSo, "viewAxis");
            CopyCameraField(controllerSo, cameraSo, "extraDistance");

            cameraSo.ApplyModifiedPropertiesWithoutUndo();
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(camera);
        }

        private static void CopyCameraField(SerializedObject sourceController, SerializedObject targetCamera, string propertyName)
        {
            SerializedProperty sourceProp = sourceController.FindProperty(propertyName);
            SerializedProperty destProp = targetCamera.FindProperty(propertyName);
            if (sourceProp == null || destProp == null)
            {
                return;
            }

            switch (sourceProp.propertyType)
            {
                case SerializedPropertyType.Float:
                    destProp.floatValue = sourceProp.floatValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    destProp.objectReferenceValue = sourceProp.objectReferenceValue;
                    break;
                case SerializedPropertyType.Enum:
                    destProp.enumValueIndex = sourceProp.enumValueIndex;
                    break;
                default:
                    Debug.LogWarning($"[PanelFocusCameraMigrationTool] Unsupported property type for '{propertyName}'.");
                    break;
            }
        }

        private static void ApplyTutorialPlayerAOverridesIfNeeded(string scenePath)
        {
            if (!scenePath.Contains("Tutorial"))
            {
                return;
            }

            PanelFocusController[] controllers = Object.FindObjectsByType<PanelFocusController>(FindObjectsSortMode.None);
            foreach (PanelFocusController controller in controllers)
            {
                if (controller == null || controller.AllowedPlayerId != AllowedPlayerTag.Player_A)
                {
                    continue;
                }

                PanelFocusCamera camera = controller.GetComponent<PanelFocusCamera>();
                if (camera == null)
                {
                    continue;
                }

                SerializedObject cameraSo = new SerializedObject(camera);
                SerializedProperty fill = cameraSo.FindProperty("frameFillPercent");
                SerializedObject controllerSo = new SerializedObject(controller);
                SerializedProperty legacyFill = controllerSo.FindProperty("frameFillPercent");
                if (fill != null && legacyFill != null && legacyFill.floatValue > fill.floatValue)
                {
                    fill.floatValue = legacyFill.floatValue;
                }

                cameraSo.ApplyModifiedPropertiesWithoutUndo();

                SerializedProperty includeExit = controllerSo.FindProperty("includeExitInFocusCycle");
                if (includeExit != null && !includeExit.boolValue)
                {
                    includeExit.boolValue = false;
                }

                controllerSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static bool WireBootstrapCameraRefsInOpenScene()
        {
            InitialPanelFocusBootstrap bootstrap = Object.FindFirstObjectByType<InitialPanelFocusBootstrap>();
            if (bootstrap == null)
            {
                return false;
            }

            SerializedObject bootstrapSo = new SerializedObject(bootstrap);
            WireBindingCameraRef(bootstrapSo, "playerA");
            WireBindingCameraRef(bootstrapSo, "playerB");
            WireFlatBootstrapCameraRef(bootstrapSo, "playerA", "playerAFocus", "playerAPanel");
            WireFlatBootstrapCameraRef(bootstrapSo, "playerB", "playerBFocus", "playerBPanel");
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bootstrap);
            return true;
        }

        private static void WireFlatBootstrapCameraRef(
            SerializedObject bootstrapSo,
            string bindingPropertyName,
            string legacyFocusPropertyName,
            string legacyPanelPropertyName)
        {
            SerializedProperty binding = bootstrapSo.FindProperty(bindingPropertyName);
            if (binding == null)
            {
                return;
            }

            SerializedProperty panelCamera = binding.FindPropertyRelative("panelCamera");
            SerializedProperty focus = binding.FindPropertyRelative("focus");
            SerializedProperty legacyPanel = binding.FindPropertyRelative("legacyPanel");
            if (panelCamera == null)
            {
                return;
            }

            SerializedProperty flatFocus = bootstrapSo.FindProperty(legacyFocusPropertyName);
            SerializedProperty flatPanel = bootstrapSo.FindProperty(legacyPanelPropertyName);
            if (focus != null && focus.objectReferenceValue == null && flatFocus != null)
            {
                focus.objectReferenceValue = flatFocus.objectReferenceValue;
            }

            if (legacyPanel != null && legacyPanel.objectReferenceValue == null && flatPanel != null)
            {
                legacyPanel.objectReferenceValue = flatPanel.objectReferenceValue;
            }

            if (panelCamera.objectReferenceValue != null)
            {
                return;
            }

            PanelFocusController panelController = ResolveLegacyPanelController(binding);
            if (panelController == null && flatPanel?.objectReferenceValue is PanelFocusController flatPanelController)
            {
                panelController = flatPanelController;
            }

            if (panelController == null)
            {
                return;
            }

            PanelFocusCamera camera = panelController.GetComponent<PanelFocusCamera>();
            if (camera != null)
            {
                panelCamera.objectReferenceValue = camera;
            }
        }

        private static void WireBindingCameraRef(SerializedObject bootstrapSo, string bindingPropertyName)
        {
            SerializedProperty binding = bootstrapSo.FindProperty(bindingPropertyName);
            if (binding == null)
            {
                return;
            }

            SerializedProperty panelCamera = binding.FindPropertyRelative("panelCamera");
            if (panelCamera == null)
            {
                return;
            }

            if (panelCamera.objectReferenceValue != null)
            {
                return;
            }

            PanelFocusController panelController = ResolveLegacyPanelController(binding);
            if (panelController == null)
            {
                return;
            }

            PanelFocusCamera camera = panelController.GetComponent<PanelFocusCamera>();
            if (camera != null)
            {
                panelCamera.objectReferenceValue = camera;
            }
        }

        private static PanelFocusController ResolveLegacyPanelController(SerializedProperty binding)
        {
            SerializedProperty legacyPanel = binding.FindPropertyRelative("legacyPanel");
            if (legacyPanel != null && legacyPanel.objectReferenceValue is PanelFocusController legacyController)
            {
                return legacyController;
            }

            SerializedProperty legacyPanelField = binding.FindPropertyRelative("panel");
            if (legacyPanelField != null && legacyPanelField.objectReferenceValue is PanelFocusController panelController)
            {
                return panelController;
            }

            return null;
        }
    }
}
