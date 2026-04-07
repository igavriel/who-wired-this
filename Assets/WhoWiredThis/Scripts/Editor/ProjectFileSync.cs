using System.IO;
using UnityEditor;
using UnityEngine;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Regenerates the .sln / .csproj files Unity needs so that Cursor (OmniSharp)
    /// can resolve UnityEngine types and give full IntelliSense.
    ///
    /// Runs automatically once on domain reload when the .sln is missing.
    /// Also exposed as WhoWiredThis → Regenerate Project Files.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectFileSync
    {
        static ProjectFileSync()
        {
            string slnPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!,
                $"{Application.productName}.sln");

            if (!File.Exists(slnPath))
            {
                Debug.Log("[WWI] .sln not found — regenerating project files for Cursor IntelliSense...");
                SyncSolution();
            }
        }

        [MenuItem("WhoWiredThis/Regenerate Project Files (Cursor IntelliSense)")]
        internal static void SyncSolution()
        {
            // UnityEditor.SyncVS.SyncSolution() is the public API for regenerating
            // .sln / .csproj from Unity's package/assembly graph.
#if UNITY_2021_1_OR_NEWER
            // Use reflection in case the type is moved in a future Unity version.
            var syncVsType = typeof(UnityEditor.Editor).Assembly
                .GetType("UnityEditor.SyncVS");

            if (syncVsType == null)
                syncVsType = System.Type.GetType("UnityEditor.SyncVS, UnityEditor");

            var method = syncVsType?.GetMethod(
                "SyncSolution",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            if (method != null)
            {
                method.Invoke(null, null);
                Debug.Log("[WWI] Project files regenerated. Cursor should now resolve Unity types.");
            }
            else
            {
                Debug.LogWarning(
                    "[WWI] SyncVS.SyncSolution() not found via reflection. " +
                    "Use Edit > Preferences > External Tools > Regenerate project files instead.");
            }
#else
            UnityEditor.SyncVS.SyncSolution();
#endif
        }
    }
}
