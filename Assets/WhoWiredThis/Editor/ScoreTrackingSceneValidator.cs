#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WhoWiredThis.Scenes;
using WhoWiredThis.Tutorial;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Validates that every gameplay scene (Tutorial / Puzzle Pipes / Puzzle Signal)
    /// has a wired <see cref="TutorialMetricsTracker"/> feeding the static scoreboard.
    /// </summary>
    public static class ScoreTrackingSceneValidator
    {
        private static readonly string[] GameplayScenePaths =
        {
            "Assets/Scenes/Game/Tutorial.unity",
            "Assets/Scenes/Game/Puzzle Pipes.unity",
            "Assets/Scenes/Game/Puzzle Signal.unity",
        };

        private const string MenuPath = "Who Wired This/Scenes/Validate Score Tracking (Gameplay Scenes)";
        private const string McpMenuPath = "Who Wired This/Scenes/MCP/Validate Score Tracking (Gameplay Scenes)";

        [MenuItem(MenuPath)]
        public static void ValidateInteractive()
        {
            if (!EditorUtility.DisplayDialog(
                    "Validate score tracking",
                    "Open Tutorial, Puzzle Pipes, and Puzzle Signal and verify TutorialMetricsTracker wiring?",
                    "Run",
                    "Cancel"))
            {
                return;
            }

            RunValidation();
        }

        [MenuItem(McpMenuPath)]
        public static void ValidateForMcp()
        {
            RunValidation();
        }

        public static void RunValidation()
        {
            string activePath = SceneManager.GetActiveScene().path;
            var report = new StringBuilder();
            int failures = 0;

            report.AppendLine("[ScoreTrackingSceneValidator] Results:");

            foreach (string scenePath in GameplayScenePaths)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    report.AppendLine($"  FAIL  {scenePath} — file missing");
                    failures++;
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                List<string> issues = ValidateLoadedScene(scene);
                if (issues.Count == 0)
                {
                    report.AppendLine($"  OK    {scenePath}");
                }
                else
                {
                    failures++;
                    report.AppendLine($"  FAIL  {scenePath}");
                    for (int i = 0; i < issues.Count; i++)
                    {
                        report.AppendLine($"         - {issues[i]}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(activePath) && System.IO.File.Exists(activePath))
            {
                EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
            }

            report.AppendLine(failures == 0
                ? "[ScoreTrackingSceneValidator] All gameplay scenes OK."
                : $"[ScoreTrackingSceneValidator] Failures: {failures}.");

            string[] lines = report.ToString().Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd('\r');
                if (!string.IsNullOrEmpty(line))
                {
                    Debug.Log(line);
                }
            }
        }

        private static List<string> ValidateLoadedScene(Scene scene)
        {
            var issues = new List<string>();
            TutorialMetricsTracker[] trackers =
                Object.FindObjectsByType<TutorialMetricsTracker>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (trackers == null || trackers.Length == 0)
            {
                issues.Add("No TutorialMetricsTracker in scene");
                return issues;
            }

            if (trackers.Length > 1)
            {
                issues.Add($"Expected 1 TutorialMetricsTracker, found {trackers.Length}");
            }

            TutorialMetricsTracker tracker = trackers[0];
            SerializedObject so = new SerializedObject(tracker);
            if (so.FindProperty("sceneStageManager")?.objectReferenceValue == null)
            {
                issues.Add("TutorialMetricsTracker.sceneStageManager is unassigned");
            }

            if (so.FindProperty("playerAPuzzleManager")?.objectReferenceValue == null)
            {
                issues.Add("TutorialMetricsTracker.playerAPuzzleManager is unassigned");
            }

            if (so.FindProperty("playerBPuzzleManager")?.objectReferenceValue == null)
            {
                issues.Add("TutorialMetricsTracker.playerBPuzzleManager is unassigned");
            }

            if (Object.FindObjectsByType<SceneStageManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
            {
                issues.Add("No SceneStageManager in scene");
            }

            if (Object.FindObjectsByType<MultiDimensionPuzzleManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length < 2)
            {
                issues.Add("Expected at least 2 MultiDimensionPuzzleManager instances");
            }

            return issues;
        }
    }
}
#endif
