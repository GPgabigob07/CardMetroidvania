using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class HitRippleLabTests
    {
        [Test]
        public void SetupCreatesStandaloneScene_WithThreeSingleSpriteTargetsAndColliders()
        {
            // Unity's additive authoring API requires a saved base scene. In batch
            // validation the initial untitled scene is disposable; never discard a user's scene.
            if (string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
            {
                if (!Application.isBatchMode) Assert.Ignore("Open a saved scene before running this authoring fixture.");
                EditorSceneManager.OpenScene("Assets/Scenes/Test_BatMachine.unity", OpenSceneMode.Single);
            }
            var type = Type.GetType("TicGame.Architecture.EditorTools.HitRippleLabSetup, TicGame.Architecture.Editor");
            Assert.That(type, Is.Not.Null, "Standalone hit ripple test scene setup is missing.");
            type.GetMethod("CreateOrUpdate").Invoke(null, null);
            var guid = AssetDatabase.AssetPathToGUID("Assets/Scenes/Test_HitRipple.unity");
            type.GetMethod("CreateOrUpdate").Invoke(null, null);
            Assert.That(AssetDatabase.AssetPathToGUID("Assets/Scenes/Test_HitRipple.unity"), Is.EqualTo(guid));
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Test_HitRipple.unity", OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var actors = roots.SelectMany(root => root.GetComponentsInChildren<EnemyActor>(true)).ToArray();
                Assert.That(actors.Length, Is.EqualTo(3));
                foreach (var actor in actors)
                {
                    Assert.That(actor.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(1));
                    Assert.That(actor.GetComponentsInChildren<Collider2D>(true).Length, Is.EqualTo(1));
                    Assert.That(actor.GetComponent<EnemyHitRipplePresenter>(), Is.Not.Null);
                    Assert.That(actor.GetComponentsInChildren<EnemyHitRippleDamageListener>(true).Length, Is.EqualTo(1));
                }
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<PlayerController>(true)), Is.Empty);
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Count(), Is.EqualTo(1));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
