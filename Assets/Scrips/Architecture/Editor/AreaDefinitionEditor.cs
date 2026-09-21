using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    [CustomEditor(typeof(AreaDefinition))]
    public sealed class AreaDefinitionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var scenePath = serializedObject.FindProperty("scenePath");
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath.stringValue);
            EditorGUI.BeginChangeCheck();
            var selectedScene = (SceneAsset)EditorGUILayout.ObjectField(
                new GUIContent("Scene Asset", "Scene loaded for this area."),
                sceneAsset,
                typeof(SceneAsset),
                false);
            if (EditorGUI.EndChangeCheck())
            {
                scenePath.stringValue = selectedScene == null
                    ? string.Empty
                    : AssetDatabase.GetAssetPath(selectedScene);
            }

            DrawPropertiesExcluding(serializedObject, "m_Script", "scenePath");
            serializedObject.ApplyModifiedProperties();

            var definition = (AreaDefinition)target;
            if (!definition.TryValidate(out var error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
        }
    }
}
