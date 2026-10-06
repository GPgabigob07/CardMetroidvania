using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TicGame.Architecture.EditorTools
{
    public static class EnemyHitRippleSetup
    {
        public const string ProfilePath = "Assets/Data/Feedback/HitRippleProfile.asset";
        public const string MaterialPath = "Assets/Data/Feedback/EnemyHitRipple.mat";
        private static readonly string[] PrefabPaths =
        {
            "Assets/Prefabs/Enemies/GolemCharger.prefab",
            "Assets/Prefabs/Enemies/BatMachine.prefab",
            "Assets/Prefabs/Enemies/GargoyleSentinel.prefab"
        };

        [MenuItem("TicGame/Feedback/Setup Enemy Hit Ripples")]
        public static void Setup()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/EnemyHitRipple.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Import a valid enemy ripple shader before setup.");
            if (!AssetDatabase.IsValidFolder("Assets/Data/Feedback")) AssetDatabase.CreateFolder("Assets/Data", "Feedback");
            var profile = AssetDatabase.LoadAssetAtPath<HitRippleProfileSO>(ProfilePath);
            if (profile == null) { profile = ScriptableObject.CreateInstance<HitRippleProfileSO>(); AssetDatabase.CreateAsset(profile, ProfilePath); }
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, MaterialPath); }
            else if (material.shader != shader) { material.shader = shader; EditorUtility.SetDirty(material); }
            foreach (var path in PrefabPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) throw new InvalidOperationException("Missing enemy prefab: " + path);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var actor = root.GetComponent<EnemyActor>();
                    if (actor == null) throw new InvalidOperationException("EnemyActor missing on " + path);
                    var visuals = root.GetComponentsInChildren<SpriteRenderer>(true).Where(visual =>
                        visual.GetComponentInParent<EnemyActor>(true) == actor && visual.GetComponentInParent<EnemyProjectile2D>(true) == null).ToArray();
                    var presenter = root.GetComponent<EnemyHitRipplePresenter>() ?? root.AddComponent<EnemyHitRipplePresenter>();
                    presenter.Configure(actor, profile, visuals);
                    foreach (var visual in visuals) visual.sharedMaterial = material;
                    // Repair assets authored by earlier setup versions without changing custom projectile materials.
                    foreach (var projectile in root.GetComponentsInChildren<EnemyProjectile2D>(true))
                    foreach (var visual in projectile.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (visual.sharedMaterial != material) continue;
                        visual.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
                    }
                    foreach (var recipient in root.GetComponentsInChildren<MonoBehaviour>(true).Where(component => component is IDamageable))
                    {
                        if (recipient.GetComponentInParent<EnemyActor>(true) != actor || recipient.GetComponentInParent<EnemyProjectile2D>(true) != null) continue;
                        var relay = recipient.GetComponent<EnemyHitRippleDamageListener>() ?? recipient.gameObject.AddComponent<EnemyHitRippleDamageListener>();
                        relay.Configure(presenter);
                    }
                    foreach (var flash in root.GetComponentsInChildren<EnemyDebugPresentation>(true)) flash.SetHitFlashEnabled(false);
                    foreach (var flash in root.GetComponentsInChildren<GargoyleAnimationPresenter>(true)) flash.SetHitFlashEnabled(false);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            ConfigureReviewScenes(profile, material);
            AssetDatabase.SaveAssets();
            Debug.Log("Enemy hit ripple assets and prefabs saved. Run the existing enemy review scenes to tune the effect.");
        }

        private static void ConfigureReviewScenes(HitRippleProfileSO profile, Material material)
        {
            var scriptPath = "Assets/Scrips/Architecture/Enemy/TrainingDummyReviewBootstrap.cs";
            var scriptGuid = AssetDatabase.AssetPathToGUID(scriptPath);
            var activeScene = SceneManager.GetActiveScene();
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(scriptGuid) || !File.ReadAllText(path).Contains(scriptGuid)) continue;
                var scene = SceneManager.GetSceneByPath(path);
                var alreadyLoaded = scene.IsValid() && scene.isLoaded;
                if (alreadyLoaded && scene.isDirty) { Debug.LogWarning("Save this review scene before wiring ripple feedback: " + path); continue; }
                if (!alreadyLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var changed = false;
                    foreach (var root in scene.GetRootGameObjects())
                    foreach (var bootstrap in root.GetComponentsInChildren<TrainingDummyReviewBootstrap>(true))
                    {
                        if (!bootstrap.ConfigureFeedback(profile, material)) continue;
                        EditorUtility.SetDirty(bootstrap); changed = true;
                    }
                    if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                }
                finally { if (!alreadyLoaded) EditorSceneManager.CloseScene(scene, true); }
            }
            if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
        }
    }
}
