using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Compact one-line rows for MultiDimensionPuzzleElement[] (index left, object right).
    /// Reuse this pattern for other serialized struct arrays — see multi-dimension-puzzle-elements-inspector.md.
    /// </summary>
    public static class MultiDimensionPuzzleElementListDrawer
    {
        private const float IndexColumnWidth = 60f;
        private const float ColumnGap = 4f;
        private const float RowVerticalPadding = 2f;

        public static ReorderableList Create(SerializedProperty arrayProperty)
        {
            var list = new ReorderableList(
                arrayProperty.serializedObject,
                arrayProperty,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true);

            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                DrawElementRow(rect, arrayProperty.GetArrayElementAtIndex(index));
            };

            list.drawHeaderCallback = DrawHeader;

            list.elementHeight = EditorGUIUtility.singleLineHeight + RowVerticalPadding * 2f;

            return list;
        }

        private static void DrawElementRow(Rect rect, SerializedProperty elementProperty)
        {
            if (elementProperty == null)
            {
                return;
            }

            SerializedProperty correctIndexProp = elementProperty.FindPropertyRelative("correctIndex");
            SerializedProperty elementRefProp = elementProperty.FindPropertyRelative("element");

            if (correctIndexProp == null || elementRefProp == null)
            {
                EditorGUI.LabelField(rect, "Missing correctIndex or element on MultiDimensionPuzzleElement.");
                return;
            }

            float y = rect.y + RowVerticalPadding;
            float height = EditorGUIUtility.singleLineHeight;

            Rect indexRect = new Rect(rect.x, y, IndexColumnWidth, height);
            Rect objectRect = new Rect(
                rect.x + IndexColumnWidth + ColumnGap,
                y,
                rect.width - IndexColumnWidth - ColumnGap,
                height);

            EditorGUI.BeginProperty(indexRect, GUIContent.none, correctIndexProp);
            EditorGUI.PropertyField(indexRect, correctIndexProp, GUIContent.none);
            EditorGUI.EndProperty();

            EditorGUI.BeginProperty(objectRect, GUIContent.none, elementRefProp);
            EditorGUI.PropertyField(objectRect, elementRefProp, GUIContent.none);
            EditorGUI.EndProperty();
        }

        private static void DrawHeader(Rect rect)
        {
            float height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, IndexColumnWidth, height), "Index");
            EditorGUI.LabelField(
                new Rect(rect.x + IndexColumnWidth + ColumnGap, rect.y, rect.width - IndexColumnWidth - ColumnGap, height),
                "MultiDimension");
        }
    }
}
