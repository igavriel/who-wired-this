using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Fixes broken prefab-instance component lists that crash player builds with
    /// "levelN is corrupted" / "Position out of bounds" during scene load.
    /// </summary>
    public static class PlaytestSceneSerializationRepair
    {
        private const string MenuRoot = "Who Wired This/Playtest/";
        private const string McpMenuRoot = MenuRoot + "MCP/";
        private const string MenuPath = MenuRoot + "Repair Build Scenes For Player";
        private const string McpMenuPath = McpMenuRoot + "Repair Build Scenes For Player";

        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/Tutorial.unity",
            "Assets/Scenes/Puzzle Pipes.unity",
            "Assets/Scenes/Puzzle Signal.unity",
        };

        [MenuItem(MenuPath)]
        public static void RepairBuildScenes()
        {
            int issues = RunRepair(out string report);
            EditorValidationConsoleReporter.Report("Playtest Scene Repair", issues, report, showDialog: true);
        }

        [MenuItem(McpMenuPath)]
        public static void RepairBuildScenesForMcp()
        {
            int issues = RunRepair(out string report);
            EditorValidationConsoleReporter.Report("Playtest Scene Repair", issues, report);
        }

        public static int RunRepair(out string report)
        {
            var sb = new StringBuilder();
            int issues = 0;

            sb.AppendLine("Playtest scene serialization repair");
            sb.AppendLine("===================================");

            string activeScenePath = SceneManager.GetActiveScene().path;

            foreach (string scenePath in ScenePaths)
            {
                if (!File.Exists(scenePath))
                {
                    sb.AppendLine($"FAIL: {scenePath} SKIPPED (file not found)");
                    issues++;
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int removedNullSlots = RemoveNullComponentSlots(scene);
                int revertedRoomPrefabs = RevertRoom5x5PrefabInstances(scene);
                int nullComponents = CountNullComponents(scene);

                EditorSceneManager.SaveScene(scene);

                sb.AppendLine(scenePath);
                sb.AppendLine($"  Removed null component slots: {removedNullSlots}");
                sb.AppendLine($"  Reverted Room5x5 prefab instances: {revertedRoomPrefabs}");
                sb.AppendLine($"  Null components after repair: {nullComponents}");

                if (nullComponents > 0)
                {
                    sb.AppendLine("  FAIL: Null runtime components remain.");
                    issues++;
                }
                else
                {
                    sb.AppendLine("  PASS: Scene is clean.");
                }
            }

            if (!string.IsNullOrEmpty(activeScenePath))
            {
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            }

            sb.AppendLine();
            sb.AppendLine(issues == 0
                ? "=== Playtest scene serialization repair: ALL CHECKS PASSED ==="
                : $"=== Playtest scene serialization repair: {issues} issue(s) ===");

            report = sb.ToString();
            return issues;
        }

        private static int RemoveNullComponentSlots(Scene scene)
        {
            int removed = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    var serializedObject = new SerializedObject(transform.gameObject);
                    SerializedProperty componentArray = serializedObject.FindProperty("m_Component");
                    if (componentArray == null || !componentArray.isArray)
                    {
                        continue;
                    }

                    bool changed = false;
                    for (int i = componentArray.arraySize - 1; i >= 0; i--)
                    {
                        if (componentArray.GetArrayElementAtIndex(i).objectReferenceValue != null)
                        {
                            continue;
                        }

                        componentArray.DeleteArrayElementAtIndex(i);
                        removed++;
                        changed = true;
                    }

                    if (changed)
                    {
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            return removed;
        }

        private static int RevertRoom5x5PrefabInstances(Scene scene)
        {
            int reverted = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (!string.Equals(root.name, "Room5x5", System.StringComparison.Ordinal) ||
                    !PrefabUtility.IsPartOfPrefabInstance(root))
                {
                    continue;
                }

                PrefabUtility.RevertPrefabInstance(root, InteractionMode.AutomatedAction);
                reverted++;
            }

            return reverted;
        }

        private static int CountNullComponents(Scene scene)
        {
            int nullCount = 0;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    Component[] components = transform.gameObject.GetComponents<Component>();
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (components[i] == null)
                        {
                            nullCount++;
                        }
                    }
                }
            }

            return nullCount;
        }
    }
}
