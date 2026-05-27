using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhoWiredThis.Editor
{
    public static class PlaytestBuildSceneValidator
    {
        private const string MenuRoot = "Who Wired This/Playtest/";
        private const string McpMenuRoot = MenuRoot + "MCP/";
        private const string MenuPath = MenuRoot + "Validate Build Scenes For Player";
        private const string McpMenuPath = McpMenuRoot + "Validate Build Scenes For Player";

        private static readonly string[] GameplayScenePaths =
        {
            "Assets/Scenes/Tutorial.unity",
            "Assets/Scenes/Puzzle Pipes.unity",
            "Assets/Scenes/Puzzle Signal.unity",
        };

        [MenuItem(MenuPath)]
        public static void ValidateBuildScenes()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Playtest Build Scenes", issues, report, showDialog: true);
        }

        [MenuItem(McpMenuPath)]
        public static void ValidateBuildScenesForMcp()
        {
            int issues = RunValidation(out string report);
            EditorValidationConsoleReporter.Report("Playtest Build Scenes", issues, report);
        }

        public static int RunValidation(out string report)
        {
            var sb = new StringBuilder();
            int issues = 0;

            sb.AppendLine("Playtest build scene validation");
            sb.AppendLine("=========================");

            int count = SceneManager.sceneCountInBuildSettings;
            sb.AppendLine($"Enabled scenes in build: {count}");
            sb.AppendLine();

            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileNameWithoutExtension(path);
                bool canStream = Application.CanStreamedLevelBeLoaded(name);

                sb.AppendLine($"[{i}] {name}");
                sb.AppendLine($"    Path: {path}");
                sb.AppendLine($"    CanStreamedLevelBeLoaded: {canStream}");

                if (string.IsNullOrEmpty(path))
                {
                    sb.AppendLine("    FAIL: Missing scene path.");
                    issues++;
                    continue;
                }

                if (!File.Exists(path))
                {
                    sb.AppendLine("    FAIL: Scene file not found on disk.");
                    issues++;
                    continue;
                }

                if (!canStream)
                {
                    sb.AppendLine("    WARN: CanStreamedLevelBeLoaded is false in Editor (normal). Verify in a player build.");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Gameplay scene serialization (null components)");
            sb.AppendLine("--------------------------------------------");

            string activeScenePath = SceneManager.GetActiveScene().path;
            foreach (string scenePath in GameplayScenePaths)
            {
                if (!File.Exists(scenePath))
                {
                    sb.AppendLine($"FAIL: {scenePath} not found on disk.");
                    issues++;
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int nullComponents = CountNullComponents(scene);
                int nullSerializedSlots = CountNullSerializedComponentSlots(scene);

                sb.AppendLine(scenePath);
                sb.AppendLine($"  Null runtime components: {nullComponents}");
                sb.AppendLine($"  Null serialized m_Component slots: {nullSerializedSlots}");

                if (nullComponents > 0)
                {
                    sb.AppendLine("  FAIL: Null runtime components will crash player scene load.");
                    issues++;
                }
                else
                {
                    sb.AppendLine(nullSerializedSlots > 0
                        ? $"  PASS: Runtime components OK ({nullSerializedSlots} prefab serialized slots ignored)."
                        : "  PASS: No null component issues detected.");
                }
            }

            if (!string.IsNullOrEmpty(activeScenePath))
            {
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            }

            sb.AppendLine();
            sb.AppendLine(issues == 0
                ? "=== Playtest build scene validation: ALL CHECKS PASSED ==="
                : $"=== Playtest build scene validation: {issues} issue(s) ===");

            report = sb.ToString();
            return issues;
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

        private static int CountNullSerializedComponentSlots(Scene scene)
        {
            int nullSlots = 0;

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

                    for (int i = 0; i < componentArray.arraySize; i++)
                    {
                        if (componentArray.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        {
                            nullSlots++;
                        }
                    }
                }
            }

            return nullSlots;
        }
    }
}
