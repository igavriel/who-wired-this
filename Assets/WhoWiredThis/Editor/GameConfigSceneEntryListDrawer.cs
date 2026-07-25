using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace WhoWiredThis.Editor
{
    /// <summary>
    /// Compact one-line rows for <c>GameConfigSO.SceneEntry[]</c> (id left, scene name right).
    /// </summary>
    public static class GameConfigSceneEntryListDrawer
    {
        private const float IdColumnWidth = 180f;
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

            SerializedProperty idProp = elementProperty.FindPropertyRelative("id");
            SerializedProperty sceneNameProp = elementProperty.FindPropertyRelative("sceneName");

            if (idProp == null || sceneNameProp == null)
            {
                EditorGUI.LabelField(rect, "Missing id or sceneName on SceneEntry.");
                return;
            }

            float y = rect.y + RowVerticalPadding;
            float height = EditorGUIUtility.singleLineHeight;

            Rect idRect = new Rect(rect.x, y, IdColumnWidth, height);
            Rect nameRect = new Rect(
                rect.x + IdColumnWidth + ColumnGap,
                y,
                rect.width - IdColumnWidth - ColumnGap,
                height);

            EditorGUI.BeginProperty(idRect, GUIContent.none, idProp);
            EditorGUI.PropertyField(idRect, idProp, GUIContent.none);
            EditorGUI.EndProperty();

            EditorGUI.BeginProperty(nameRect, GUIContent.none, sceneNameProp);
            EditorGUI.PropertyField(nameRect, sceneNameProp, GUIContent.none);
            EditorGUI.EndProperty();
        }

        private static void DrawHeader(Rect rect)
        {
            float height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, IdColumnWidth, height), "Id");
            EditorGUI.LabelField(
                new Rect(rect.x + IdColumnWidth + ColumnGap, rect.y, rect.width - IdColumnWidth - ColumnGap, height),
                "Scene Name");
        }
    }
}
