using UnityEditor;
using UnityEngine;
using WhoWiredThis.Util;

namespace WhoWiredThis.Editor
{
    [CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
    public class RequireInterfaceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            RequireInterfaceAttribute requireInterface = (RequireInterfaceAttribute)attribute;

            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            UnityEngine.Object previous = property.objectReferenceValue;
            UnityEngine.Object assigned = EditorGUI.ObjectField(position, label, previous, typeof(UnityEngine.Object), true);

            if (assigned == null)
            {
                property.objectReferenceValue = null;
                EditorGUI.EndProperty();
                return;
            }

            if (TryResolveCompatibleBehaviour(assigned, requireInterface.InterfaceType, out MonoBehaviour resolved))
            {
                property.objectReferenceValue = resolved;
            }
            else
            {
                property.objectReferenceValue = previous;
                Debug.LogWarning(
                    $"[RequireInterface] Field '{property.displayName}' only accepts components implementing " +
                    $"{requireInterface.InterfaceType.Name}. Assigned object was rejected.");
            }

            EditorGUI.EndProperty();
        }

        private static bool TryResolveCompatibleBehaviour(UnityEngine.Object assigned, System.Type interfaceType, out MonoBehaviour resolved)
        {
            resolved = null;

            if (assigned is MonoBehaviour mono && interfaceType.IsAssignableFrom(mono.GetType()))
            {
                resolved = mono;
                return true;
            }

            Component component = assigned as Component;
            GameObject gameObject = assigned as GameObject;

            if (component == null && gameObject == null)
            {
                return false;
            }

            MonoBehaviour[] candidates = component != null
                ? component.GetComponents<MonoBehaviour>()
                : gameObject.GetComponents<MonoBehaviour>();

            for (int i = 0; i < candidates.Length; i++)
            {
                MonoBehaviour candidate = candidates[i];
                if (candidate != null && interfaceType.IsAssignableFrom(candidate.GetType()))
                {
                    resolved = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
