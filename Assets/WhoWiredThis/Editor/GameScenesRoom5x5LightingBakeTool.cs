#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Opens each top-level <c>Assets/Scenes/Game/*.unity</c> scene that contains
    /// <c>Room5x5</c> and runs a full lightmap bake (same as Generate Lighting).
    /// </summary>
    public static class GameScenesRoom5x5LightingBakeTool
    {
        private const string MenuPath = "Who Wired This/Scenes/Bake Lighting (Active Scene, Room5x5)";
        private const string BatchMenuPath = "Who Wired This/Scenes/Bake Lighting (All Game Scenes With Room5x5)";
        private const string McpBatchMenuPath = "Who Wired This/Scenes/MCP/Bake Lighting (All Game Scenes With Room5x5)";

        private static readonly List<string> PendingScenePaths = new List<string>();
        private static string restoreActiveScenePath;
        private static int bakedSceneCount;
        private static int skippedSceneCount;
        private static bool batchRunning;

        [MenuItem(MenuPath)]
        public static void BakeActiveScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!Room5x5StaticGiSetupTool.SceneContainsRoom5x5(scene))
            {
                Debug.LogWarning(
                    $"[GameScenesRoom5x5LightingBakeTool] Active scene '{scene.path}' has no Room5x5. Skipped.");
                return;
            }

            if (!BakeSceneSynchronously(scene.path, showProgressBar: true))
            {
                return;
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GameScenesRoom5x5LightingBakeTool] Baked lighting for '{scene.path}'.");
        }

        [MenuItem(BatchMenuPath)]
        public static void BakeAllGameScenesInteractive()
        {
            List<string> scenePaths = CollectScenePathsWithRoom5x5();
            if (scenePaths.Count == 0)
            {
                Debug.Log("[GameScenesRoom5x5LightingBakeTool] No Game scenes with Room5x5 found.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Bake lighting for Game scenes",
                    $"Bake lighting for {scenePaths.Count} scene(s) with Room5x5?\n\n" +
                    "This can take several minutes per scene and will overwrite each scene's LightingData.",
                    "Bake",
                    "Cancel"))
            {
                return;
            }

            int baked = RunBatchSynchronously(scenePaths);
            Debug.Log(
                $"[GameScenesRoom5x5LightingBakeTool] Batch bake complete. Baked {baked} scene(s); " +
                $"skipped {skippedSceneCount}.");
        }

        [MenuItem(McpBatchMenuPath)]
        public static void BakeAllGameScenesForMcp()
        {
            if (batchRunning)
            {
                Debug.LogWarning("[GameScenesRoom5x5LightingBakeTool] Batch bake already running.");
                return;
            }

            List<string> scenePaths = CollectScenePathsWithRoom5x5();
            if (scenePaths.Count == 0)
            {
                Debug.Log("[GameScenesRoom5x5LightingBakeTool] MCP batch: no Game scenes with Room5x5 found.");
                return;
            }

            StartAsyncBatch(scenePaths);
        }

        [MenuItem(BatchMenuPath, true)]
        [MenuItem(McpBatchMenuPath, true)]
        private static bool ValidateBatchMenus()
        {
            return !batchRunning && !Lightmapping.isRunning;
        }

        public static int RunBatchSynchronously(IReadOnlyList<string> scenePaths)
        {
            restoreActiveScenePath = SceneManager.GetActiveScene().path;
            bakedSceneCount = 0;
            skippedSceneCount = 0;

            try
            {
                for (int i = 0; i < scenePaths.Count; i++)
                {
                    string scenePath = scenePaths[i];
                    float progress = (i + 1f) / scenePaths.Count;
                    if (!BakeSceneSynchronously(scenePath, showProgressBar: true, progress: progress))
                    {
                        skippedSceneCount++;
                        continue;
                    }

                    Scene bakedScene = SceneManager.GetSceneByPath(scenePath);
                    if (bakedScene.IsValid() && bakedScene.isLoaded)
                    {
                        EditorSceneManager.SaveScene(bakedScene);
                    }

                    bakedSceneCount++;
                    Debug.Log(
                        $"[GameScenesRoom5x5LightingBakeTool] ({bakedSceneCount}/{scenePaths.Count}) Baked '{scenePath}'.");
                }

                AssetDatabase.SaveAssets();
                return bakedSceneCount;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                RestoreActiveScene();
            }
        }

        private static void StartAsyncBatch(IReadOnlyList<string> scenePaths)
        {
            CancelRunningBakeIfNeeded();

            PendingScenePaths.Clear();
            PendingScenePaths.AddRange(scenePaths);
            restoreActiveScenePath = SceneManager.GetActiveScene().path;
            bakedSceneCount = 0;
            skippedSceneCount = 0;
            batchRunning = true;

            Lightmapping.bakeCompleted -= OnAsyncBakeCompleted;
            Lightmapping.bakeCancelled -= OnAsyncBakeCancelled;
            Lightmapping.bakeCompleted += OnAsyncBakeCompleted;
            Lightmapping.bakeCancelled += OnAsyncBakeCancelled;

            Debug.Log(
                $"[GameScenesRoom5x5LightingBakeTool] MCP batch started for {PendingScenePaths.Count} scene(s).");
            BakeNextSceneAsync();
        }

        private static void BakeNextSceneAsync()
        {
            if (PendingScenePaths.Count == 0)
            {
                FinishAsyncBatch();
                return;
            }

            string scenePath = PendingScenePaths[0];
            PendingScenePaths.RemoveAt(0);

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Scene scene = SceneManager.GetActiveScene();
            if (!Room5x5StaticGiSetupTool.SceneContainsRoom5x5(scene))
            {
                Debug.LogWarning(
                    $"[GameScenesRoom5x5LightingBakeTool] '{scenePath}' no longer has Room5x5. Skipped.");
                skippedSceneCount++;
                BakeNextSceneAsync();
                return;
            }

            Debug.Log(
                $"[GameScenesRoom5x5LightingBakeTool] Baking ({bakedSceneCount + skippedSceneCount + 1}) '{scenePath}'...");
            Lightmapping.BakeAsync();
        }

        private static void OnAsyncBakeCompleted()
        {
            string scenePath = SceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            bakedSceneCount++;
            Debug.Log(
                $"[GameScenesRoom5x5LightingBakeTool] Bake completed for '{scenePath}' ({bakedSceneCount} done).");
            BakeNextSceneAsync();
        }

        private static void OnAsyncBakeCancelled()
        {
            Debug.LogError(
                $"[GameScenesRoom5x5LightingBakeTool] Bake cancelled for '{SceneManager.GetActiveScene().path}'. " +
                "Stopping batch.");
            FinishAsyncBatch();
        }

        private static void FinishAsyncBatch()
        {
            Lightmapping.bakeCompleted -= OnAsyncBakeCompleted;
            Lightmapping.bakeCancelled -= OnAsyncBakeCancelled;
            batchRunning = false;
            PendingScenePaths.Clear();

            AssetDatabase.SaveAssets();
            RestoreActiveScene();

            Debug.Log(
                $"[GameScenesRoom5x5LightingBakeTool] MCP batch complete. Baked {bakedSceneCount} scene(s); " +
                $"skipped {skippedSceneCount}.");
        }

        private static bool BakeSceneSynchronously(
            string scenePath,
            bool showProgressBar,
            float progress = 0f)
        {
            CancelRunningBakeIfNeeded();

            if (SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!Room5x5StaticGiSetupTool.SceneContainsRoom5x5(scene))
            {
                Debug.LogWarning(
                    $"[GameScenesRoom5x5LightingBakeTool] '{scenePath}' has no Room5x5. Skipped.");
                return false;
            }

            if (showProgressBar)
            {
                EditorUtility.DisplayProgressBar(
                    "Bake Game scene lighting",
                    scenePath,
                    progress <= 0f ? 0.5f : progress);
            }

            Debug.Log($"[GameScenesRoom5x5LightingBakeTool] Baking '{scenePath}'...");
            Lightmapping.Bake();
            return true;
        }

        private static List<string> CollectScenePathsWithRoom5x5()
        {
            var scenePaths = new List<string>();
            foreach (string scenePath in Room5x5StaticGiSetupTool.EnumerateTopLevelGameScenePaths())
            {
                if (Room5x5StaticGiSetupTool.SceneFileContainsRoom5x5(scenePath))
                {
                    scenePaths.Add(scenePath);
                }
            }

            return scenePaths;
        }

        private static void CancelRunningBakeIfNeeded()
        {
            if (Lightmapping.isRunning)
            {
                Lightmapping.Cancel();
            }
        }

        private static void RestoreActiveScene()
        {
            if (string.IsNullOrEmpty(restoreActiveScenePath))
            {
                return;
            }

            EditorSceneManager.OpenScene(restoreActiveScenePath, OpenSceneMode.Single);
            restoreActiveScenePath = null;
        }
    }
}
#endif
