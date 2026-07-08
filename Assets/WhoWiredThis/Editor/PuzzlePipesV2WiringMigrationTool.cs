#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Backup / restore / migrate C# script wiring from inactive _OLD_Player* panels onto
    /// Pipes_A/B V2 Variant in Puzzle Pipes.unity. Does not modify transforms.
    /// </summary>
    public static class PuzzlePipesV2WiringMigrationTool
    {
        private const string ScenePath = "Assets/Scenes/Game/Puzzle Pipes.unity";
        private const string BackupDir = "Assets/Scenes/Game/_BACKUP_2026-07-09";
        private const string BackupFileName = "Puzzle Pipes.pre-v2-wiring.unity";

        private const string OldPanelAName = "_OLD_Player1_Pipes_Panel A";
        private const string OldPanelBName = "_OLD_Player2_Pipes_Panel B";
        private const string V2PanelAName = "Pipes_A V2 Variant";
        private const string V2PanelBName = "Pipes_B V2 Variant";

        private const string BackupMenuPath =
            "Who Wired This/Pipe Pressure/Backup Puzzle Pipes Pre-V2 Wiring";
        private const string RestoreMenuPath =
            "Who Wired This/Pipe Pressure/Restore Puzzle Pipes Pre-V2 Wiring Backup";
        private const string MigrateMenuPath =
            "Who Wired This/Pipe Pressure/Migrate OLD Panel Wiring To V2 Variants";
        private const string DeleteOldPanelsMenuPath =
            "Who Wired This/Pipe Pressure/Delete OLD Puzzle Pipes Panels (Post-Validation)";

        public static string BackupScenePath => $"{BackupDir}/{BackupFileName}";

        /// <summary>Non-interactive backup + migrate for MCP / batch runs.</summary>
        public static int MigrateOldPanelsToV2Batch(bool deleteOldPanelsAfter = false)
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            if (!File.Exists(BackupScenePath))
            {
                BackupScene();
            }

            GameObject oldA = FindSceneObjectByName(OldPanelAName);
            GameObject oldB = FindSceneObjectByName(OldPanelBName);
            GameObject v2A = FindSceneObjectByName(V2PanelAName);
            GameObject v2B = FindSceneObjectByName(V2PanelBName);

            if (oldA == null || oldB == null || v2A == null || v2B == null)
            {
                Debug.LogError(
                    "[PuzzlePipesV2WiringMigrationTool] Missing panel roots. Expected: " +
                    $"'{OldPanelAName}', '{OldPanelBName}', '{V2PanelAName}', '{V2PanelBName}'.");
                return 1;
            }

            int remapped = PuzzlePipesV2WiringMigrationCore.Migrate(
                oldA,
                oldB,
                v2A,
                v2B,
                deleteOldPanelsAfter: false);

            PipePressurePuzzlePipesWireTool.WireCrossPartnerDiagnosticAndFocusForPanels(v2A, v2B);
            PipePressurePuzzlePipesWireTool.WirePipesPanelPostMigration(v2A, v2B);

            if (deleteOldPanelsAfter)
            {
                DeleteOldPanelsBatch();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log(
                $"[PuzzlePipesV2WiringMigrationTool] Batch migration complete. Remapped {remapped} references.");
            return 0;
        }

        public static void RestoreFromBackupBatch()
        {
            if (!File.Exists(BackupScenePath))
            {
                Debug.LogError($"[PuzzlePipesV2WiringMigrationTool] No backup at '{BackupScenePath}'.");
                return;
            }

            File.Copy(BackupScenePath, ScenePath, overwrite: true);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"[PuzzlePipesV2WiringMigrationTool] Restored scene from '{BackupScenePath}' (batch).");
        }

        public static void DeleteOldPanelsBatch()
        {
            GameObject oldA = FindSceneObjectByName(OldPanelAName);
            GameObject oldB = FindSceneObjectByName(OldPanelBName);
            if (oldA != null)
            {
                Object.DestroyImmediate(oldA);
            }

            if (oldB != null)
            {
                Object.DestroyImmediate(oldB);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[PuzzlePipesV2WiringMigrationTool] Deleted _OLD puzzle pipes panels (batch).");
        }

        [MenuItem(BackupMenuPath)]
        public static void BackupScene()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[PuzzlePipesV2WiringMigrationTool] Scene not found: {ScenePath}");
                return;
            }

            Directory.CreateDirectory(BackupDir);
            File.Copy(ScenePath, BackupScenePath, overwrite: true);
            AssetDatabase.Refresh();

            Debug.Log(
                $"[PuzzlePipesV2WiringMigrationTool] Backup written to '{BackupScenePath}'. " +
                "Run migration only after confirming git working tree is safe.");
        }

        [MenuItem(RestoreMenuPath)]
        public static void RestoreFromBackup()
        {
            if (!File.Exists(BackupScenePath))
            {
                Debug.LogError(
                    $"[PuzzlePipesV2WiringMigrationTool] No backup at '{BackupScenePath}'. " +
                    "Run Backup first, or use scripts/rollback-puzzle-pipes-v2-wiring.sh for git restore.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Restore Puzzle Pipes backup",
                    $"Replace '{ScenePath}' with backup from {BackupFileName}?\n\n" +
                    "Unsaved scene changes will be lost.",
                    "Restore",
                    "Cancel"))
            {
                return;
            }

            if (SceneManager.GetActiveScene().path == ScenePath)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            }

            File.Copy(BackupScenePath, ScenePath, overwrite: true);
            AssetDatabase.Refresh();

            if (SceneManager.GetActiveScene().path == ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Debug.Log($"[PuzzlePipesV2WiringMigrationTool] Restored scene from '{BackupScenePath}'.");
        }

        [MenuItem(MigrateMenuPath)]
        public static void MigrateOldPanelsToV2()
        {
            if (!EnsureSceneOpen())
            {
                return;
            }

            if (!File.Exists(BackupScenePath))
            {
                bool proceed = EditorUtility.DisplayDialog(
                    "No backup found",
                    "No pre-migration backup exists. Create one now before migrating?",
                    "Backup and continue",
                    "Cancel");
                if (!proceed)
                {
                    return;
                }

                BackupScene();
            }

            GameObject oldA = FindSceneObjectByName(OldPanelAName);
            GameObject oldB = FindSceneObjectByName(OldPanelBName);
            GameObject v2A = FindSceneObjectByName(V2PanelAName);
            GameObject v2B = FindSceneObjectByName(V2PanelBName);

            if (oldA == null || oldB == null || v2A == null || v2B == null)
            {
                Debug.LogError(
                    "[PuzzlePipesV2WiringMigrationTool] Missing panel roots. Expected: " +
                    $"'{OldPanelAName}', '{OldPanelBName}', '{V2PanelAName}', '{V2PanelBName}'.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Migrate V2 panel wiring",
                    "Copy C# script references from _OLD panels to V2 variants?\n\n" +
                    "• Transforms are NOT changed\n" +
                    "• Diagnostic targets: DiagnosticPanel-A/B (legacy)\n" +
                    "• _OLD panels deleted after validation\n\n" +
                    "See .cursor/plan/puzzle-pipes-v2-panel-wiring-migration.md",
                    "Migrate",
                    "Cancel"))
            {
                return;
            }

            int remapped = PuzzlePipesV2WiringMigrationCore.Migrate(
                oldA,
                oldB,
                v2A,
                v2B,
                deleteOldPanelsAfter: false);

            PipePressurePuzzlePipesWireTool.WireCrossPartnerDiagnosticAndFocusForPanels(v2A, v2B);
            PipePressurePuzzlePipesWireTool.WirePipesPanelPostMigration(v2A, v2B);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log(
                $"[PuzzlePipesV2WiringMigrationTool] Migration complete. Remapped {remapped} references. " +
                "Run validation menus, then delete _OLD panels if approved.");
        }

        [MenuItem(DeleteOldPanelsMenuPath)]
        public static void DeleteOldPanels()
        {
            if (!EnsureSceneOpen())
            {
                return;
            }

            GameObject oldA = FindSceneObjectByName(OldPanelAName);
            GameObject oldB = FindSceneObjectByName(OldPanelBName);
            if (oldA == null && oldB == null)
            {
                Debug.Log("[PuzzlePipesV2WiringMigrationTool] No _OLD panels found.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Delete OLD panels",
                    $"Remove '{OldPanelAName}' and '{OldPanelBName}' from the scene?",
                    "Delete",
                    "Cancel"))
            {
                return;
            }

            if (oldA != null)
            {
                Object.DestroyImmediate(oldA);
            }

            if (oldB != null)
            {
                Object.DestroyImmediate(oldB);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[PuzzlePipesV2WiringMigrationTool] Deleted _OLD puzzle pipes panels.");
        }

        [MenuItem(RestoreMenuPath, true)]
        private static bool ValidateRestore()
        {
            return File.Exists(BackupScenePath);
        }

        private static bool EnsureSceneOpen()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (active.path == ScenePath)
            {
                return true;
            }

            bool open = EditorUtility.DisplayDialog(
                "Open Puzzle Pipes",
                $"Active scene is '{active.path}'. Open '{ScenePath}'?",
                "Open",
                "Cancel");
            if (!open)
            {
                return false;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return true;
        }

        private static GameObject FindSceneObjectByName(string objectName)
        {
            Transform[] transforms = Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Transform transform in transforms)
            {
                if (transform.name == objectName && transform.gameObject.scene.isLoaded)
                {
                    return transform.gameObject;
                }
            }

            return null;
        }
    }
}
#endif
