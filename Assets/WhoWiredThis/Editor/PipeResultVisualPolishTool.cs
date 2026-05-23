#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WhoWiredThis.EditorTools
{
    /// <summary>
    /// Rebuilds child primitives under existing Result Visualizer State0–3 roots (scene-only polish).
    /// </summary>
    public static class PipeResultVisualPolishTool
    {
        private const string MenuPath = "Who Wired This/Pipe Pressure/Apply Result Visual Polish (Puzzle Pipes)";

        private const string MaterialsFolder = "Assets/WhoWiredThis/Materials/PipeVisualizer";

        private static readonly string[] BlueGroups = { "ValveGroup", "PressureGroup", "FlowGroup" };
        private static readonly string[] RedGroups = { "GateGroup", "PumpGroup", "RouteGroup" };

        [MenuItem(MenuPath)]
        public static void ApplyPolish()
        {
            if (!Application.isPlaying &&
                EditorSceneManager.GetActiveScene().path != "Assets/Scenes/Puzzle Pipes.unity")
            {
                if (!EditorUtility.DisplayDialog(
                        "Apply Result Visual Polish",
                        "Open Puzzle Pipes.unity first. Open it now?",
                        "Open scene",
                        "Cancel"))
                {
                    return;
                }

                EditorSceneManager.OpenScene("Assets/Scenes/Puzzle Pipes.unity");
            }

            if (!TryLoadMaterials(out Material body, out Material steel, out Material pressure, out Material flow))
            {
                return;
            }

            int polished = 0;
            foreach (Transform root in FindResultVisualRoots())
            {
                polished += PolishRig(root, body, steel, pressure, flow);
            }

            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            if (activeScene.IsValid() && activeScene.isDirty)
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log($"[PipeResultVisualPolishTool] Polished {polished} state roots on Puzzle Pipes (scene saved).");
        }

        private static bool TryLoadMaterials(
            out Material body,
            out Material steel,
            out Material pressure,
            out Material flow)
        {
            body = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/Mat_PipeVisualizer_Body.mat");
            steel = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/Mat_PipeVisualizer_Steel.mat");
            pressure = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/Mat_PipeVisualizer_Pressure.mat");
            flow = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/Mat_PipeVisualizer_Flow.mat");

            if (body == null || steel == null || pressure == null || flow == null)
            {
                Debug.LogError(
                    "[PipeResultVisualPolishTool] Missing materials under Assets/WhoWiredThis/Materials/PipeVisualizer/. " +
                    "Create Mat_PipeVisualizer_Body, Steel, Pressure, Flow first.");
                body = steel = pressure = flow = null;
                return false;
            }

            return true;
        }

        private static List<Transform> FindResultVisualRoots()
        {
            var roots = new List<Transform>();
            foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                CollectResultVisualRoots(go.transform, roots);
            }

            return roots;
        }

        private static void CollectResultVisualRoots(Transform node, List<Transform> roots)
        {
            if (node.name == "ResultVisual_Root")
            {
                roots.Add(node);
            }

            for (int i = 0; i < node.childCount; i++)
            {
                CollectResultVisualRoots(node.GetChild(i), roots);
            }
        }

        private static int PolishRig(Transform rigRoot, Material body, Material steel, Material pressure, Material flow)
        {
            int count = 0;
            for (int g = 0; g < rigRoot.childCount; g++)
            {
                Transform group = rigRoot.GetChild(g);
                VisualGroupKind kind = ResolveGroupKind(group.name);
                if (kind == VisualGroupKind.Unknown)
                {
                    continue;
                }

                float columnX = g * 0.42f - 0.42f;
                for (int s = 0; s < group.childCount; s++)
                {
                    Transform state = group.GetChild(s);
                    if (!state.name.StartsWith("State"))
                    {
                        continue;
                    }

                    if (!int.TryParse(state.name.Substring(5), out int stateIndex))
                    {
                        continue;
                    }

                    PolishStateRoot(state, kind, stateIndex, columnX, body, steel, pressure, flow);
                    count++;
                }
            }

            return count;
        }

        private static VisualGroupKind ResolveGroupKind(string groupName)
        {
            switch (groupName)
            {
                case "ValveGroup":
                case "GateGroup":
                    return VisualGroupKind.Valve;
                case "PressureGroup":
                case "PumpGroup":
                    return VisualGroupKind.Pressure;
                case "FlowGroup":
                case "RouteGroup":
                    return VisualGroupKind.Flow;
                default:
                    return VisualGroupKind.Unknown;
            }
        }

        private static void PolishStateRoot(
            Transform stateRoot,
            VisualGroupKind kind,
            int stateIndex,
            float columnX,
            Material body,
            Material steel,
            Material pressure,
            Material flow)
        {
            Undo.RecordObject(stateRoot, "Polish state root");

            for (int i = stateRoot.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(stateRoot.GetChild(i).gameObject);
            }

            StripRootMesh(stateRoot);

            stateRoot.localPosition = new Vector3(columnX, 0f, 0f);
            stateRoot.localRotation = Quaternion.identity;
            stateRoot.localScale = Vector3.one;

            switch (kind)
            {
                case VisualGroupKind.Valve:
                    BuildValveState(stateRoot, stateIndex, body, steel);
                    break;
                case VisualGroupKind.Pressure:
                    BuildPressureState(stateRoot, stateIndex, steel, pressure);
                    break;
                case VisualGroupKind.Flow:
                    BuildFlowState(stateRoot, stateIndex, body, flow);
                    break;
            }

            stateRoot.gameObject.SetActive(false);
        }

        private static void StripRootMesh(Transform stateRoot)
        {
            MeshRenderer renderer = stateRoot.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Undo.DestroyObjectImmediate(renderer);
            }

            MeshFilter filter = stateRoot.GetComponent<MeshFilter>();
            if (filter != null)
            {
                Undo.DestroyObjectImmediate(filter);
            }

            Collider collider = stateRoot.GetComponent<Collider>();
            if (collider != null)
            {
                Undo.DestroyObjectImmediate(collider);
            }
        }

        private static void BuildValveState(Transform root, int stateIndex, Material body, Material steel)
        {
            CreatePrimitive(root, "PipeBody", PrimitiveType.Cube, body, Vector3.zero, new Vector3(0.36f, 0.14f, 0.14f), Quaternion.identity);

            float angle = stateIndex switch
            {
                0 => 0f,
                1 => 28f,
                2 => 58f,
                _ => 88f
            };

            float slide = stateIndex * 0.02f;
            CreatePrimitive(
                root,
                "GatePlate",
                PrimitiveType.Cube,
                steel,
                new Vector3(0.08f + slide, 0f, 0f),
                new Vector3(0.035f, 0.16f, 0.16f),
                Quaternion.Euler(0f, angle, 0f));
        }

        private static void BuildPressureState(Transform root, int stateIndex, Material steel, Material pressure)
        {
            const float housingHeight = 0.32f;
            CreatePrimitive(
                root,
                "MeterHousing",
                PrimitiveType.Cube,
                steel,
                new Vector3(0f, housingHeight * 0.5f, 0f),
                new Vector3(0.2f, housingHeight, 0.2f),
                Quaternion.identity);

            float[] fillHeights = { 0.08f, 0.14f, 0.22f, 0.3f };
            float fillH = fillHeights[Mathf.Clamp(stateIndex, 0, 3)];
            CreatePrimitive(
                root,
                "BarFill",
                PrimitiveType.Cube,
                pressure,
                new Vector3(0f, fillH * 0.5f + 0.01f, 0f),
                new Vector3(0.12f, fillH, 0.12f),
                Quaternion.identity);

            if (stateIndex == 3)
            {
                CreatePrimitive(
                    root,
                    "CapRing",
                    PrimitiveType.Cube,
                    steel,
                    new Vector3(0f, housingHeight + 0.02f, 0f),
                    new Vector3(0.14f, 0.025f, 0.14f),
                    Quaternion.identity);
            }
        }

        private static void BuildFlowState(Transform root, int stateIndex, Material body, Material flow)
        {
            CreatePrimitive(
                root,
                "PipeSpine",
                PrimitiveType.Cube,
                body,
                new Vector3(0f, 0.12f, 0f),
                new Vector3(0.1f, 0.24f, 0.1f),
                Quaternion.identity);

            switch (stateIndex)
            {
                case 0:
                    CreatePrimitive(root, "PathA", PrimitiveType.Cube, flow, new Vector3(-0.14f, 0.12f, 0f), new Vector3(0.16f, 0.08f, 0.08f), Quaternion.identity);
                    CreatePrimitive(root, "PathB", PrimitiveType.Cube, flow, new Vector3(-0.08f, 0.2f, 0f), new Vector3(0.08f, 0.1f, 0.08f), Quaternion.identity);
                    break;
                case 1:
                    CreatePrimitive(root, "PathMid", PrimitiveType.Cube, flow, new Vector3(0f, 0.22f, 0f), new Vector3(0.08f, 0.14f, 0.08f), Quaternion.identity);
                    break;
                case 2:
                    CreatePrimitive(root, "PathA", PrimitiveType.Cube, flow, new Vector3(0.14f, 0.12f, 0f), new Vector3(0.16f, 0.08f, 0.08f), Quaternion.identity);
                    CreatePrimitive(root, "PathB", PrimitiveType.Cube, flow, new Vector3(0.08f, 0.2f, 0f), new Vector3(0.08f, 0.1f, 0.08f), Quaternion.identity);
                    break;
                default:
                    CreatePrimitive(root, "LoopL", PrimitiveType.Cube, flow, new Vector3(-0.1f, 0.12f, 0f), new Vector3(0.08f, 0.08f, 0.08f), Quaternion.identity);
                    CreatePrimitive(root, "LoopR", PrimitiveType.Cube, flow, new Vector3(0.1f, 0.12f, 0f), new Vector3(0.08f, 0.08f, 0.08f), Quaternion.identity);
                    CreatePrimitive(root, "LoopT", PrimitiveType.Cube, flow, new Vector3(0f, 0.2f, 0f), new Vector3(0.22f, 0.06f, 0.08f), Quaternion.identity);
                    CreatePrimitive(root, "LoopB", PrimitiveType.Cube, flow, new Vector3(0f, 0.04f, 0f), new Vector3(0.22f, 0.06f, 0.08f), Quaternion.identity);
                    break;
            }
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Material material,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation)
        {
            GameObject go = GameObject.CreatePrimitive(primitiveType);
            Undo.RegisterCreatedObjectUndo(go, "Polish result visual");
            go.name = objectName;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Undo.DestroyObjectImmediate(collider);
            }

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localScale = localScale;
            t.localRotation = localRotation;
            return go;
        }

        private enum VisualGroupKind
        {
            Unknown,
            Valve,
            Pressure,
            Flow
        }
    }
}
#endif
