#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Backup / restore / migrate C# script wiring from Player1/2 Signal panels onto
    /// Signal_A/B V2 Variant in Puzzle Signal.unity. Does not modify transforms.
    /// </summary>
    public static class PuzzleSignalV2WiringMigrationTool
    {
        private const string ScenePath = "Assets/Scenes/Game/Puzzle Signal.unity";
        private const string BackupDir = "Assets/Scenes/Game/_BACKUP_2026-07-10";
        private const string BackupFileName = "Puzzle Signal.pre-v2-wiring.unity";

        private const string OldPanelAName = "Player1_Signal_Panel-A";
        private const string OldPanelBName = "Player2_Signal_Panel-B";
        private const string V2PanelAName = "Signal_A_V2 Variant";
        private const string V2PanelBName = "Signal_B_V2 Variant";

        private const string BackupMenuPath =
            "Who Wired This/Signal Calibration/Backup Puzzle Signal Pre-V2 Wiring";
        private const string RestoreMenuPath =
            "Who Wired This/Signal Calibration/Restore Puzzle Signal Pre-V2 Wiring Backup";
        private const string MigrateMenuPath =
            "Who Wired This/Signal Calibration/Migrate OLD Panel Wiring To V2 Variants";
        private const string DeleteOldPanelsMenuPath =
            "Who Wired This/Signal Calibration/Delete OLD Puzzle Signal Panels (Post-Validation)";
        private const string MigrateMcpMenuPath =
            "Who Wired This/Signal Calibration/MCP/Migrate OLD Panel Wiring To V2 Variants";
        private const string DeleteOldPanelsMcpMenuPath =
            "Who Wired This/Signal Calibration/MCP/Delete OLD Puzzle Signal Panels (Post-Validation)";

        public static string BackupScenePath => $"{BackupDir}/{BackupFileName}";

        public static int MigrateOldPanelsToV2Batch(bool deleteOldPanelsAfter = false)
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            if (!File.Exists(BackupScenePath))
            {
                BackupSceneBatch();
            }

            GameObject oldA = FindSceneObjectByName(OldPanelAName);
            GameObject oldB = FindSceneObjectByName(OldPanelBName);
            GameObject v2A = FindSceneObjectByName(V2PanelAName);
            GameObject v2B = FindSceneObjectByName(V2PanelBName);

            if (oldA == null || oldB == null || v2A == null || v2B == null)
            {
                Debug.LogError(
                    "[PuzzleSignalV2WiringMigrationTool] Missing panel roots. Expected: " +
                    $"'{OldPanelAName}', '{OldPanelBName}', '{V2PanelAName}', '{V2PanelBName}'.");
                return 1;
            }

            int remapped;
            try
            {
                remapped = PuzzleSignalV2WiringMigrationCore.Migrate(
                    oldA,
                    oldB,
                    v2A,
                    v2B,
                    deleteOldPanelsAfter: false);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("[PuzzleSignalV2WiringMigrationTool] Core migration failed.");
                return 1;
            }

            try
            {
                SignalCalibrationPuzzleSignalWireTool.WireCrossPartnerDiagnosticAndFocusForPanels(v2A, v2B);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("[PuzzleSignalV2WiringMigrationTool] Cross-partner diagnostic/focus wiring failed.");
                return 1;
            }

            try
            {
                SignalCalibrationPuzzleSignalWireTool.WireSignalPanelPostMigration(v2A, v2B);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("[PuzzleSignalV2WiringMigrationTool] Post-migration panel locks/bridges failed.");
                return 1;
            }

            int resultIssues;
            try
            {
                resultIssues = SignalCalibrationPuzzleSignalResultWireTool.WireAllResultFeedback(v2A, v2B);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError("[PuzzleSignalV2WiringMigrationTool] Result feedback wiring failed.");
                return 1;
            }
            if (resultIssues > 0)
            {
                Debug.LogWarning(
                    $"[PuzzleSignalV2WiringMigrationTool] Result feedback wire finished with {resultIssues} issue(s).");
            }

            if (deleteOldPanelsAfter)
            {
                DeleteOldPanelsBatch();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log(
                $"[PuzzleSignalV2WiringMigrationTool] Batch migration complete. Remapped {remapped} references.");
            return resultIssues > 0 ? 1 : 0;
        }

        public static void BackupSceneBatch()
        {
            Directory.CreateDirectory(BackupDir);
            File.Copy(ScenePath, BackupScenePath, overwrite: true);
            AssetDatabase.Refresh();
            Debug.Log($"[PuzzleSignalV2WiringMigrationTool] Backup written to '{BackupScenePath}'.");
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
            Debug.Log("[PuzzleSignalV2WiringMigrationTool] Deleted OLD puzzle signal panels (batch).");
        }

        [MenuItem(BackupMenuPath)]
        public static void BackupScene()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"[PuzzleSignalV2WiringMigrationTool] Scene not found: {ScenePath}");
                return;
            }

            BackupSceneBatch();
        }

        [MenuItem(RestoreMenuPath)]
        public static void RestoreFromBackup()
        {
            if (!File.Exists(BackupScenePath))
            {
                Debug.LogError($"[PuzzleSignalV2WiringMigrationTool] No backup at '{BackupScenePath}'.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Restore Puzzle Signal backup",
                    $"Replace '{ScenePath}' with backup from {BackupFileName}?",
                    "Restore",
                    "Cancel"))
            {
                return;
            }

            File.Copy(BackupScenePath, ScenePath, overwrite: true);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log($"[PuzzleSignalV2WiringMigrationTool] Restored scene from '{BackupScenePath}'.");
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
                if (!EditorUtility.DisplayDialog(
                        "No backup found",
                        "Create a pre-migration backup now?",
                        "Backup and continue",
                        "Cancel"))
                {
                    return;
                }

                BackupScene();
            }

            MigrateOldPanelsToV2Batch(deleteOldPanelsAfter: false);
        }

        [MenuItem(MigrateMcpMenuPath)]
        public static void MigrateOldPanelsToV2ForMcp()
        {
            MigrateOldPanelsToV2Batch(deleteOldPanelsAfter: false);
        }

        [MenuItem(DeleteOldPanelsMcpMenuPath)]
        public static void DeleteOldPanelsForMcp()
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            DeleteOldPanelsBatch();
            EditorSceneManager.SaveOpenScenes();
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
                Debug.Log("[PuzzleSignalV2WiringMigrationTool] No OLD panels found.");
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

            DeleteOldPanelsBatch();
        }

        private static bool EnsureSceneOpen()
        {
            if (EditorSceneManager.GetActiveScene().path == ScenePath)
            {
                return true;
            }

            if (!EditorUtility.DisplayDialog(
                    "Open Puzzle Signal",
                    $"Open '{ScenePath}'?",
                    "Open",
                    "Cancel"))
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
