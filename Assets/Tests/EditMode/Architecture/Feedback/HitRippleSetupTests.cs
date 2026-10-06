using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class HitRippleSetupTests
    {
        [Test]
        public void SetupTwice_WiresRecipientsWithoutDuplicates_AndKeepsProjectilesExcluded()
        {
            var type = Type.GetType("TicGame.Architecture.EditorTools.EnemyHitRippleSetup, TicGame.Architecture.Editor");
            Assert.That(type, Is.Not.Null, "Idempotent ripple setup is missing.");
            type.GetMethod("Setup").Invoke(null, null);
            var profileGuid = AssetDatabase.AssetPathToGUID("Assets/Data/Feedback/HitRippleProfile.asset");
            var materialGuid = AssetDatabase.AssetPathToGUID("Assets/Data/Feedback/EnemyHitRipple.mat");
            type.GetMethod("Setup").Invoke(null, null);
            Assert.That(AssetDatabase.AssetPathToGUID("Assets/Data/Feedback/HitRippleProfile.asset"), Is.EqualTo(profileGuid));
            Assert.That(AssetDatabase.AssetPathToGUID("Assets/Data/Feedback/EnemyHitRipple.mat"), Is.EqualTo(materialGuid));
            foreach (var name in new[] { "GolemCharger", "BatMachine", "GargoyleSentinel" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Enemies/{name}.prefab");
                var presenters = prefab.GetComponentsInChildren<EnemyHitRipplePresenter>(true);
                Assert.That(presenters.Length, Is.EqualTo(1), name);
                var presenter = presenters.Single();
                Assert.That(presenter.Profile.WidthPixels, Is.EqualTo(4));
                Assert.That(presenter.Profile.TraversalSeconds, Is.EqualTo(.65f));
                Assert.That(presenter.Visuals.Count, Is.GreaterThan(0));
                if (name == "BatMachine") Assert.That(presenter.Visuals.Count, Is.EqualTo(1), "Only VisualRoot belongs to the bat; its inactive projectile template must be excluded.");
                foreach (var visual in presenter.Visuals)
                {
                    Assert.That(visual.GetComponentInParent<EnemyProjectile2D>(true), Is.Null);
                    Assert.That(visual.sharedMaterial.shader.name, Is.EqualTo("TicGame/2D/Enemy Hit Ripple"));
                }
                foreach (var recipient in prefab.GetComponentsInChildren<MonoBehaviour>(true).Where(component => component is IDamageable))
                {
                    if (recipient.GetComponentInParent<EnemyProjectile2D>(true) != null) continue;
                    Assert.That(recipient.GetComponents<EnemyHitRippleDamageListener>().Length, Is.EqualTo(1), recipient.name);
                    Assert.That(recipient.GetComponent<EnemyHitRippleDamageListener>().Presenter, Is.EqualTo(presenter));
                }
                foreach (var projectile in prefab.GetComponentsInChildren<EnemyProjectile2D>(true))
                foreach (var visual in projectile.GetComponentsInChildren<SpriteRenderer>(true))
                    Assert.That(visual.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/2D/Sprite-Lit-Default"), "Projectile material must remain unchanged.");
            }
        }
    }
}
