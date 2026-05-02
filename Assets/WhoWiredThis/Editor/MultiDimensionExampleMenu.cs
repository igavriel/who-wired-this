#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using WhoWiredThis.Enums;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.EditorTools
{
    /// <summary>
    /// Spawns the plan example: host + MultiDimension, Box + Sphere subjects, Capsule general collider.
    /// </summary>
    public static class MultiDimensionExampleMenu
    {
        private const string MenuPath = "WhoWiredThis/Visibility/Create MultiDimension Example In Scene";

        [MenuItem(MenuPath, priority = 100)]
        private static void CreateExampleInScene()
        {
            GameObject root = new GameObject("MultiDimension_Example");
            Undo.RegisterCreatedObjectUndo(root, "Create MultiDimension Example");

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Subject_Box";
            box.transform.SetParent(root.transform, false);
            box.transform.localPosition = new Vector3(-0.6f, 0.5f, 0f);
            box.transform.localScale = Vector3.one * 0.45f;
            Undo.RegisterCreatedObjectUndo(box, "Create MultiDimension Example");

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Subject_Sphere";
            sphere.transform.SetParent(root.transform, false);
            sphere.transform.localPosition = new Vector3(0.6f, 0.5f, 0f);
            sphere.transform.localScale = Vector3.one * 0.45f;
            Undo.RegisterCreatedObjectUndo(sphere, "Create MultiDimension Example");

            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "General_Capsule";
            capsule.transform.SetParent(root.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            capsule.transform.localScale = new Vector3(0.35f, 0.6f, 0.35f);
            Undo.RegisterCreatedObjectUndo(capsule, "Create MultiDimension Example");

            MultiDimension md = Undo.AddComponent<MultiDimension>(root);

            SerializedObject serialized = new SerializedObject(md);
            SerializedProperty subjectsProp = serialized.FindProperty("subjects");
            subjectsProp.arraySize = 2;
            SerializedProperty e0 = subjectsProp.GetArrayElementAtIndex(0);
            e0.FindPropertyRelative("subject").objectReferenceValue = box;
            e0.FindPropertyRelative("displayName").stringValue = "Box";
            SerializedProperty e1 = subjectsProp.GetArrayElementAtIndex(1);
            e1.FindPropertyRelative("subject").objectReferenceValue = sphere;
            e1.FindPropertyRelative("displayName").stringValue = "Sphere";
            serialized.FindProperty("generalObject").objectReferenceValue = capsule;
            serialized.FindProperty("configurationMode").enumValueIndex = (int)MultiDimension.MultiDimensionMode.SplitPlayers;
            serialized.FindProperty("indexPlayerA").intValue = 0;
            serialized.FindProperty("indexPlayerB").intValue = 1;
            serialized.FindProperty("exclusivePlayer").enumValueIndex = (int)AllowedPlayerTag.Player_A;
            serialized.FindProperty("exclusiveSubjectIndex").intValue = 0;
            serialized.FindProperty("sharedSubjectIndex").intValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log($"[{nameof(MultiDimensionExampleMenu)}] Created '{root.name}'. Default mode: CASE 1 (Box index 0 → Player A view, Sphere index 1 → Player B view). General capsule stays on Default for all players.");
        }
    }
}
#endif
