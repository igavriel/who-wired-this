using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Compact one-line rows for <c>MultiDimensionSubject[]</c> (display name left, subject right).
    /// </summary>
    public static class MultiDimensionSubjectListDrawer
    {
        private const float NameColumnWidth = 140f;
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

            SerializedProperty displayNameProp = elementProperty.FindPropertyRelative("displayName");
            SerializedProperty subjectProp = elementProperty.FindPropertyRelative("subject");

            if (displayNameProp == null || subjectProp == null)
            {
                EditorGUI.LabelField(rect, "Missing displayName or subject on MultiDimensionSubject.");
                return;
            }

            float y = rect.y + RowVerticalPadding;
            float height = EditorGUIUtility.singleLineHeight;

            Rect nameRect = new Rect(rect.x, y, NameColumnWidth, height);
            Rect subjectRect = new Rect(
                rect.x + NameColumnWidth + ColumnGap,
                y,
                rect.width - NameColumnWidth - ColumnGap,
                height);

            EditorGUI.BeginProperty(nameRect, GUIContent.none, displayNameProp);
            EditorGUI.PropertyField(nameRect, displayNameProp, GUIContent.none);
            EditorGUI.EndProperty();

            EditorGUI.BeginProperty(subjectRect, GUIContent.none, subjectProp);
            EditorGUI.PropertyField(subjectRect, subjectProp, GUIContent.none);
            EditorGUI.EndProperty();
        }

        private static void DrawHeader(Rect rect)
        {
            float height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, NameColumnWidth, height), "Name");
            EditorGUI.LabelField(
                new Rect(rect.x + NameColumnWidth + ColumnGap, rect.y, rect.width - NameColumnWidth - ColumnGap, height),
                "Subject");
        }
    }
}
