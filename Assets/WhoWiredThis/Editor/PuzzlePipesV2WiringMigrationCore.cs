#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WhoWiredThis.Puzzles.Common;
using WhoWiredThis.Scenes;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Copies serialized object references from OLD panel hierarchy to V2 by relative child name.
    /// Skips Transform / RectTransform properties.
    /// </summary>
    internal static class PuzzlePipesV2WiringMigrationCore
    {
        private static readonly HashSet<string> SkippedPropertyNames = new HashSet<string>
        {
            "m_LocalRotation",
            "m_LocalPosition",
            "m_LocalScale",
            "m_AnchoredPosition",
            "m_SizeDelta",
            "m_AnchorMin",
            "m_AnchorMax",
            "m_Pivot",
            "m_RootOrder",
        };

        private static readonly Dictionary<string, string> ChildNameAliases = new Dictionary<string, string>
        {
            { "Diagnostic Adapter-A", "_OLD_DiagnosticPanel-A" },
            { "Diagnostic Adapter-B", "_OLD_Diagnostic Adapter-B" },
            { "_OLD_PuzzleManager-A", "PuzzleManager-A" },
            { "_OLD_PuzzleManager-B", "PuzzleManager-B" },
        };

        public static int Migrate(
            GameObject oldA,
            GameObject oldB,
            GameObject v2A,
            GameObject v2B,
            bool deleteOldPanelsAfter)
        {
            var oldToV2 = new Dictionary<GameObject, GameObject>
            {
                { oldA, v2A },
                { oldB, v2B },
            };

            int remapped = 0;
            foreach (KeyValuePair<GameObject, GameObject> pair in oldToV2)
            {
                remapped += CopyWiringUnderRoot(pair.Key, pair.Value, oldToV2);
            }

            RemapSceneStageManagerDiagnostics(v2A, v2B);

            if (deleteOldPanelsAfter)
            {
                Object.DestroyImmediate(oldA);
                Object.DestroyImmediate(oldB);
            }

            return remapped;
        }

        private static void RemapSceneStageManagerDiagnostics(GameObject v2A, GameObject v2B)
        {
            SceneStageManager stageManager = Object.FindFirstObjectByType<SceneStageManager>();
            if (stageManager == null)
            {
                return;
            }

            DiagnosticDisplayController displayA =
                FindChildByName(v2A.transform, "DiagnosticPanel-A")
                    ?.GetComponentInChildren<DiagnosticDisplayController>(true);
            DiagnosticDisplayController displayB =
                FindChildByName(v2B.transform, "DiagnosticPanel-B")
                    ?.GetComponentInChildren<DiagnosticDisplayController>(true);

            SerializedObject so = new SerializedObject(stageManager);
            if (displayA != null)
            {
                so.FindProperty("playerADiagnosticDisplay").objectReferenceValue = displayA;
            }

            if (displayB != null)
            {
                so.FindProperty("playerBDiagnosticDisplay").objectReferenceValue = displayB;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int CopyWiringUnderRoot(
            GameObject oldRoot,
            GameObject v2Root,
            IReadOnlyDictionary<GameObject, GameObject> panelMap)
        {
            int count = 0;
            var oldComponents = oldRoot.GetComponentsInChildren<Component>(true);
            foreach (Component oldComponent in oldComponents)
            {
                if (oldComponent == null || oldComponent is Transform)
                {
                    continue;
                }

                string relativePath = GetRelativePath(oldRoot.transform, oldComponent.transform);
                Transform v2Transform = ResolveV2Transform(v2Root.transform, relativePath);
                if (v2Transform == null)
                {
                    Debug.LogWarning(
                        $"[PuzzlePipesV2WiringMigrationCore] No V2 match for '{oldRoot.name}/{relativePath}'.");
                    continue;
                }

                Component v2Component = v2Transform.GetComponent(oldComponent.GetType());
                if (v2Component == null)
                {
                    continue;
                }

                count += CopySerializedReferences(oldComponent, v2Component, panelMap);
            }

            return count;
        }

        private static int CopySerializedReferences(
            Component source,
            Component target,
            IReadOnlyDictionary<GameObject, GameObject> panelMap)
        {
            SerializedObject sourceSo = new SerializedObject(source);
            SerializedObject targetSo = new SerializedObject(target);
            SerializedProperty prop = sourceSo.GetIterator();
            int remapped = 0;
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "m_Script")
                {
                    continue;
                }

                if (SkippedPropertyNames.Contains(prop.name))
                {
                    continue;
                }

                if (prop.propertyType != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                SerializedProperty targetProp = targetSo.FindProperty(prop.propertyPath);
                if (targetProp == null)
                {
                    continue;
                }

                Object mapped = RemapReference(prop.objectReferenceValue, panelMap);
                if (mapped != targetProp.objectReferenceValue)
                {
                    targetProp.objectReferenceValue = mapped;
                    remapped++;
                }
            }

            targetSo.ApplyModifiedPropertiesWithoutUndo();
            return remapped;
        }

        private static Object RemapReference(
            Object reference,
            IReadOnlyDictionary<GameObject, GameObject> panelMap)
        {
            if (reference == null)
            {
                return null;
            }

            if (reference is GameObject go)
            {
                return RemapGameObject(go, panelMap);
            }

            if (reference is Component component)
            {
                GameObject mappedGo = RemapGameObject(component.gameObject, panelMap);
                if (mappedGo == null)
                {
                    return null;
                }

                Component mappedComponent = mappedGo.GetComponent(component.GetType());
                return mappedComponent != null ? mappedComponent : reference;
            }

            return reference;
        }

        private static GameObject RemapGameObject(
            GameObject sourceGo,
            IReadOnlyDictionary<GameObject, GameObject> panelMap)
        {
            foreach (KeyValuePair<GameObject, GameObject> pair in panelMap)
            {
                if (sourceGo == pair.Key)
                {
                    return pair.Value;
                }

                if (!IsDescendantOf(sourceGo.transform, pair.Key.transform))
                {
                    continue;
                }

                string relativePath = GetRelativePath(pair.Key.transform, sourceGo.transform);
                Transform v2Transform = ResolveV2Transform(pair.Value.transform, relativePath);
                return v2Transform != null ? v2Transform.gameObject : null;
            }

            return sourceGo;
        }

        private static Transform ResolveV2Transform(Transform v2Root, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return v2Root;
            }

            string[] segments = relativePath.Split('/');
            Transform current = v2Root;
            foreach (string segment in segments)
            {
                string mappedName = ChildNameAliases.TryGetValue(segment, out string alias) ? alias : segment;
                Transform child = FindChildByName(current, mappedName);
                if (child == null)
                {
                    return null;
                }

                current = child;
            }

            return current;
        }

        private static Transform FindChildByName(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static string GetRelativePath(Transform root, Transform descendant)
        {
            if (descendant == root)
            {
                return string.Empty;
            }

            var segments = new List<string>();
            Transform current = descendant;
            while (current != null && current != root)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            if (current != root)
            {
                return null;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private static bool IsDescendantOf(Transform descendant, Transform ancestor)
        {
            Transform current = descendant;
            while (current != null)
            {
                if (current == ancestor)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
#endif
