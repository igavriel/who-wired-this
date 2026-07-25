using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using WhoWiredThis.Core;

namespace WhoWiredThis.Editor
{
    [CustomEditor(typeof(GameConfigSO))]
    public class GameConfigSOEditor : UnityEditor.Editor
    {
        private const string SceneEntriesPropertyName = "sceneEntries";

        private ReorderableList sceneEntriesList;

        private void OnEnable()
        {
            SerializedProperty sceneEntriesProperty = serializedObject.FindProperty(SceneEntriesPropertyName);
            if (sceneEntriesProperty == null)
            {
                Debug.LogWarning(
                    $"[GameConfigSOEditor] Serialized property '{SceneEntriesPropertyName}' was not found.",
                    target);
                return;
            }

            sceneEntriesList = GameConfigSceneEntryListDrawer.Create(sceneEntriesProperty);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (iterator.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    continue;
                }

                if (iterator.name == SceneEntriesPropertyName)
                {
                    if (sceneEntriesList != null)
                    {
                        sceneEntriesList.DoLayoutList();
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
