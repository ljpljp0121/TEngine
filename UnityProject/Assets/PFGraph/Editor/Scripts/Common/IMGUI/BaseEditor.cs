using UnityEditor;

namespace PFGraph
{
    public abstract class BaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            SerializedProperty iterator = serializedObject.GetIterator();

            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                EditorGUILayout.PropertyField(iterator, true);
            }

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnPropertyGUI(SerializedProperty property)
        {
            EditorGUILayout.PropertyField(property);
        }
    }
}