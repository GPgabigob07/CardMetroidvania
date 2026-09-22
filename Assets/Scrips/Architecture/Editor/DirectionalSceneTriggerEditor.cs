using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    [CustomEditor(typeof(DirectionalSceneTrigger))]
    public sealed class DirectionalSceneTriggerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var path = serializedObject.FindProperty("targetScenePath");
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue);
            EditorGUI.BeginChangeCheck();
            var chosen = (SceneAsset)EditorGUILayout.ObjectField("Target Scene", asset, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck()) path.stringValue = chosen == null ? "" : AssetDatabase.GetAssetPath(chosen);
            DrawPropertiesExcluding(serializedObject, "m_Script", "targetScenePath");
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("Cross A then B to apply A To B; cross B then A for B To A. Place both volumes in the area that stays loaded. Keep them separated and ahead of the geometry that must change.", MessageType.Info);
            if (Application.isPlaying)
            {
                EditorGUILayout.Toggle("Target Loaded", SceneStreamingService.IsLoaded(path.stringValue));
                var status = SceneStreamingService.GetStatus(path.stringValue);
                EditorGUILayout.Toggle("Operation In Progress", status?.IsBusy == true);
                if (!string.IsNullOrEmpty(status?.LastError)) EditorGUILayout.HelpBox(status.LastError, MessageType.Error);
                Repaint();
            }
        }

        [MenuItem("GameObject/TIC/Directional Scene Trigger", false, 10)]
        private static void Create()
        {
            var layer = LayerMask.NameToLayer("Environment");
            if (layer < 0) { Debug.LogError("The Environment layer is required for player trigger contacts."); return; }
            var root = new GameObject("Directional Scene Trigger");
            Undo.RegisterCreatedObjectUndo(root, "Create directional scene trigger");
            var trigger = root.AddComponent<DirectionalSceneTrigger>();
            var a = CreateVolume(root.transform, "A", -2, layer);
            var b = CreateVolume(root.transform, "B", 2, layer);
            trigger.ConfigureVolumes(a, b);
            Selection.activeGameObject = root;
        }

        private static SceneTriggerVolume CreateVolume(Transform root, string name, float y, int layer)
        {
            var child = new GameObject(name);
            child.layer = layer;
            child.transform.SetParent(root, false);
            child.transform.localPosition = new Vector3(0, y, 0);
            var box = child.AddComponent<BoxCollider2D>();
            box.size = new Vector2(10, 1);
            box.isTrigger = true;
            return child.AddComponent<SceneTriggerVolume>();
        }
    }
}
