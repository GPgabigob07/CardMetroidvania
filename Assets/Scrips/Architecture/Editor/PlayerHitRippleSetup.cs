using System;
using UnityEditor;
using UnityEngine;
namespace TicGame.Architecture.EditorTools
{
    public static class PlayerHitRippleSetup
    {
        public const string ProfilePath = "Assets/Data/Feedback/PlayerHitRippleProfile.asset";
        [MenuItem("TicGame/Feedback/Setup Player Hit Ripples")]
        public static void Setup()
        {
            var profile = AssetDatabase.LoadAssetAtPath<HitRippleProfileSO>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<HitRippleProfileSO>();
                JsonUtility.FromJsonOverwrite("{\"blueColor\":{\"r\":1,\"g\":0,\"b\":0,\"a\":1},\"rejectedColor\":{\"r\":1,\"g\":0,\"b\":0,\"a\":1},\"fatalLeadingColor\":{\"r\":1,\"g\":0,\"b\":0,\"a\":1},\"fatalTrailingColor\":{\"r\":1,\"g\":0,\"b\":0,\"a\":1}}", profile);
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(EnemyHitRippleSetup.MaterialPath);
            if (material == null) throw new InvalidOperationException("Enemy ripple material is missing.");
            const string path = "Assets/Prefabs/Player/Player.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var health = root.GetComponent<SimpleHealth>();
                var body = root.transform.Find("Animation")?.GetComponent<SpriteRenderer>();
                if (health == null || body == null) throw new InvalidOperationException("Player health/body sprite missing.");
                var presenter = root.GetComponent<PlayerHitRipplePresenter>() ?? root.AddComponent<PlayerHitRipplePresenter>();
                presenter.Configure(health, profile, new[] { body });
                body.sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
    }
}
