#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Ensures every <c>Room5x5</c> prefab instance in a scene has full static editor flags
    /// (Contribute GI, etc.) on all descendants — required for baked lighting to match Tutorial.
    /// </summary>
    public static class Room5x5StaticGiSetupTool
    {
        private const string Room5x5PrefabPath = "Assets/WhoWiredThis/Prefabs/Rooms/Room5x5.prefab";
        public const string Room5x5PrefabGuid = "1fa38ca0ec0bd4b2a9f949a51f345a12";
        private const string GameScenesFolder = "Assets/Scenes/Game";
        private const string MenuPath = "Who Wired This/Scenes/Ensure Room5x5 Static GI (Active Scene)";
        private const string BatchMenuPath = "Who Wired This/Scenes/Ensure Room5x5 Static GI (All Game Scenes)";
        private const string McpBatchMenuPath = "Who Wired This/Scenes/MCP/Ensure Room5x5 Static GI (All Game Scenes)";

        private static readonly StaticEditorFlags TargetFlags = (StaticEditorFlags)(-1);

        [MenuItem(MenuPath)]
        public static void EnsureActiveScene()
        {
            int updated = EnsureRoom5x5StaticInScene(SceneManager.GetActiveScene(), logScenePath: true);
            if (updated > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }

            Debug.Log(updated > 0
                ? $"[Room5x5StaticGiSetupTool] Updated static flags on {updated} object(s) in active scene."
                : "[Room5x5StaticGiSetupTool] Active scene already has Room5x5 static GI configured (or no Room5x5 found).");
        }

        [MenuItem(BatchMenuPath)]
        public static void EnsureAllGameScenesInteractive()
        {
            if (!EditorUtility.DisplayDialog(
                    "Ensure Room5x5 static GI",
                    $"Apply static GI flags to Room5x5 in all .unity files directly under '{GameScenesFolder}'?",
                    "Run",
                    "Cancel"))
            {
                return;
            }

            int scenesChanged = RunBatchOnGameScenes();
            Debug.Log($"[Room5x5StaticGiSetupTool] Batch complete. Scenes changed: {scenesChanged}.");
        }

        [MenuItem(McpBatchMenuPath)]
        public static void EnsureAllGameScenesForMcp()
        {
            int scenesChanged = RunBatchOnGameScenes();
            Debug.Log($"[Room5x5StaticGiSetupTool] MCP batch complete. Scenes changed: {scenesChanged}.");
        }

        public static int RunBatchOnGameScenes()
        {
            string activePath = SceneManager.GetActiveScene().path;
            int scenesChanged = 0;

            foreach (string unityPath in EnumerateTopLevelGameScenePaths())
            {
                Scene scene = EditorSceneManager.OpenScene(unityPath, OpenSceneMode.Single);
                int updated = EnsureRoom5x5StaticInScene(scene, logScenePath: true);
                if (updated <= 0)
                {
                    continue;
                }

                EditorSceneManager.SaveScene(scene);
                scenesChanged++;
            }

            if (!string.IsNullOrEmpty(activePath))
            {
                EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
            }

            return scenesChanged;
        }

        public static IEnumerable<string> EnumerateTopLevelGameScenePaths()
        {
            foreach (string scenePath in Directory.GetFiles(GameScenesFolder, "*.unity", SearchOption.TopDirectoryOnly))
            {
                yield return scenePath.Replace('\\', '/');
            }
        }

        public static bool SceneFileContainsRoom5x5(string scenePath)
        {
            return File.ReadAllText(scenePath).Contains(Room5x5PrefabGuid);
        }

        public static bool SceneContainsRoom5x5(Scene scene)
        {
            return FindRoom5x5Roots(scene).Count > 0;
        }

        public static int EnsureRoom5x5StaticInScene(Scene scene, bool logScenePath)
        {
            List<GameObject> roomRoots = FindRoom5x5Roots(scene);
            if (roomRoots.Count == 0)
            {
                if (logScenePath)
                {
                    Debug.Log($"[Room5x5StaticGiSetupTool] No Room5x5 in '{scene.path}'. Skipped.");
                }

                return 0;
            }

            int updated = 0;
            foreach (GameObject roomRoot in roomRoots)
            {
                updated += ApplyStaticFlagsUnderRoot(roomRoot);
            }

            if (logScenePath && updated > 0)
            {
                Debug.Log($"[Room5x5StaticGiSetupTool] '{scene.path}': updated {updated} object(s) under Room5x5.");
            }

            return updated;
        }

        private static List<GameObject> FindRoom5x5Roots(Scene scene)
        {
            var roots = new List<GameObject>();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return roots;
            }

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(rootObject) == Room5x5PrefabPath)
                {
                    roots.Add(rootObject);
                }
            }

            return roots;
        }

        private static int ApplyStaticFlagsUnderRoot(GameObject roomRoot)
        {
            int updated = 0;
            Transform[] transforms = roomRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                GameObject gameObject = transform.gameObject;
                StaticEditorFlags current = GameObjectUtility.GetStaticEditorFlags(gameObject);
                if (current == TargetFlags)
                {
                    continue;
                }

                GameObjectUtility.SetStaticEditorFlags(gameObject, TargetFlags);
                EditorUtility.SetDirty(gameObject);
                updated++;
            }

            if (updated > 0)
            {
                EditorUtility.SetDirty(roomRoot);
            }

            return updated;
        }
    }
}
#endif
