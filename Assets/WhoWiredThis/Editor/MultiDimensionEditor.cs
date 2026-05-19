using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    [CustomEditor(typeof(MultiDimension))]
    public class MultiDimensionEditor : UnityEditor.Editor
    {
        private const string SubjectsPropertyName = "subjects";

        private ReorderableList subjectsList;

        private void OnEnable()
        {
            SerializedProperty subjectsProperty = serializedObject.FindProperty(SubjectsPropertyName);
            if (subjectsProperty == null)
            {
                Debug.LogWarning(
                    $"[MultiDimensionEditor] Serialized property '{SubjectsPropertyName}' was not found.",
                    target);
                return;
            }

            subjectsList = MultiDimensionSubjectListDrawer.Create(subjectsProperty);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name == SubjectsPropertyName)
                {
                    if (subjectsList != null)
                    {
                        subjectsList.DoLayoutList();
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
