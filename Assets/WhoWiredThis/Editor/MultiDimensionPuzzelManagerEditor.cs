using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using WhoWiredThis.Visibility;

namespace WhoWiredThis.Editor
{
    [CustomEditor(typeof(MultiDimensionPuzzelManager))]
    public class MultiDimensionPuzzelManagerEditor : UnityEditor.Editor
    {
        private const string PuzzleElementsPropertyName = "puzzleElements";

        private ReorderableList puzzleElementsList;

        private void OnEnable()
        {
            SerializedProperty puzzleElementsProperty = serializedObject.FindProperty(PuzzleElementsPropertyName);
            if (puzzleElementsProperty == null)
            {
                Debug.LogWarning(
                    $"[MultiDimensionPuzzelManagerEditor] Serialized property '{PuzzleElementsPropertyName}' was not found.",
                    target);
                return;
            }

            puzzleElementsList = MultiDimensionPuzzleElementListDrawer.Create(puzzleElementsProperty);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name == PuzzleElementsPropertyName)
                {
                    if (puzzleElementsList != null)
                    {
                        puzzleElementsList.DoLayoutList();
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
