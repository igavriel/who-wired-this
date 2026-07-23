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
    /// Ensures every <c>Room5x5-Static</c> prefab instance in a scene has full static editor flags
    /// (Contribute GI, etc.) on all descendants — required for baked lighting to match Tutorial.
    /// </summary>
    public static class Room5x5StaticGiSetupTool
    {
        private const string Room5x5PrefabPath = "Assets/WhoWiredThis/Prefabs/Rooms/Room5x5-Static.prefab";
        public static string Room5x5PrefabGuid => AssetDatabase.AssetPathToGUID(Room5x5PrefabPath);
        private const string GameScenesFolder = "Assets/Scenes/Game";
        private const string MenuPath = "Who Wired This/Scenes/Ensure Room5x5 Static GI (Active Scene)";
        private const string BatchMenuPath = "Who Wired This/Scenes/Ensure Room5x5 Static GI (All Game Scenes)";
        private const string McpBatchMenuPath = "Who Wired This/Scenes/MCP/Ensure Room5x5 Static GI (All Game Scenes)";

        // Unity serializes "all static flags enabled" as 2147483647 (not (StaticEditorFlags)(-1)).
        private const int FullyStaticSerializedValue = 2147483647;
        private static readonly StaticEditorFlags TargetFlags = (StaticEditorFlags)FullyStaticSerializedValue;
        private static bool? room5x5PrefabIsFullyStatic;

        private static bool IsFullyStatic(StaticEditorFlags flags)
        {
            int value = (int)flags;
            return value == FullyStaticSerializedValue || value == -1;
        }

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
            string guid = Room5x5PrefabGuid;
            return !string.IsNullOrEmpty(guid) && File.ReadAllText(scenePath).Contains(guid);
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

            if (IsRoom5x5PrefabFullyStatic())
            {
                if (logScenePath)
                {
                    Debug.Log(
                        $"[Room5x5StaticGiSetupTool] '{scene.path}': Room5x5-Static prefab already has static GI flags. Skipped {roomRoots.Count} instance(s).");
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
                string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(rootObject);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                string guid = Room5x5PrefabGuid;
                if (!string.IsNullOrEmpty(guid) && AssetDatabase.AssetPathToGUID(assetPath) == guid)
                {
                    roots.Add(rootObject);
                }
            }

            return roots;
        }

        private static bool IsRoom5x5PrefabFullyStatic()
        {
            if (room5x5PrefabIsFullyStatic.HasValue)
            {
                return room5x5PrefabIsFullyStatic.Value;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(Room5x5PrefabPath);
            if (prefabRoot == null)
            {
                room5x5PrefabIsFullyStatic = false;
                return false;
            }

            try
            {
                foreach (Transform transform in prefabRoot.GetComponentsInChildren<Transform>(true))
                {
                    StaticEditorFlags current = GameObjectUtility.GetStaticEditorFlags(transform.gameObject);
                    if (!IsFullyStatic(current))
                    {
                        room5x5PrefabIsFullyStatic = false;
                        return false;
                    }
                }

                room5x5PrefabIsFullyStatic = true;
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static int ApplyStaticFlagsUnderRoot(GameObject roomRoot)
        {
            int updated = 0;
            Transform[] transforms = roomRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                GameObject gameObject = transform.gameObject;
                StaticEditorFlags current = GameObjectUtility.GetStaticEditorFlags(gameObject);
                if (IsFullyStatic(current))
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
