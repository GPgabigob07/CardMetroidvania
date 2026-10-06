using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TicGame.Architecture.Tests
{
    public sealed class HitRipplePresenterTests
    {
        private GameObject root;
        private EnemyActor actor;
        private EnemyHealth health;
        private HitRippleProfileSO profile;
        private EnemyDefinitionSO definition;
        private Sprite sprite;
        private Texture2D texture;
        private Component presenter;
        private SpriteRenderer renderer;

        [SetUp]
        public void Initialize()
        {
            root = new GameObject("Ripple enemy");
            health = root.AddComponent<EnemyHealth>();
            actor = root.AddComponent<EnemyActor>();
            definition = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            actor.SetDefinition(definition); actor.Initialize();
            profile = ScriptableObject.CreateInstance<HitRippleProfileSO>();
            texture = new Texture2D(16, 16);
            sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.25f, .5f), 100);
            var visual = new GameObject("Body"); visual.transform.SetParent(root.transform, false);
            renderer = visual.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            var type = typeof(EnemyActor).Assembly.GetType("TicGame.Architecture.EnemyHitRipplePresenter");
            Assert.That(type, Is.Not.Null, "Enemy ripple presenter is missing.");
            presenter = root.AddComponent(type);
            Call("Configure", actor, profile, new[] { renderer });
        }
        private object Call(string name, params object[] args) => presenter.GetType().GetMethod(name).Invoke(presenter, args);
        private int Count => (int)presenter.GetType().GetProperty("ActiveCount").GetValue(presenter);
        private void Hit(bool fatal = false) => Call("Present", new DamageContext(null, root, null, 1, Vector2.zero, Vector2.right), new DamageResult(true, fatal, 1, fatal ? 0 : 1));
        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(root); Object.DestroyImmediate(profile); Object.DestroyImmediate(definition);
            Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
        }
        [Test]
        public void ExplicitResetClears_ButHealingAndDefeatDoNot()
        {
            Hit(); health.ApplyDamage(new DamageContext(null, root, null, 1, Vector2.zero, Vector2.right));
            health.Restore(1); Assert.That(Count, Is.EqualTo(1));
            Hit(true); Assert.That(Count, Is.EqualTo(2));
            actor.ResetActor(); Assert.That(Count, Is.Zero);
            Hit(); ((Behaviour)presenter).enabled = false;
            // EditMode does not invoke MonoBehaviour lifecycle callbacks automatically.
            presenter.GetType().GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(presenter, null);
            Assert.That(Count, Is.Zero);
        }
        [Test]
        public void PauseAndClear_PreserveUnrelatedRendererProperties()
        {
            var block = new MaterialPropertyBlock(); block.SetFloat("_Unrelated", 42); renderer.SetPropertyBlock(block);
            Hit(); Call("Tick", .1f, false); renderer.GetPropertyBlock(block);
            var radius = block.GetVector("_RippleOriginRadius0").z;
            Assert.That(radius, Is.GreaterThan(0));
            Call("Tick", 1f, true); renderer.GetPropertyBlock(block);
            Assert.That(block.GetVector("_RippleOriginRadius0").z, Is.EqualTo(radius));
            Assert.That(block.GetVector("_RippleOriginRadius0").w, Is.EqualTo(.04f).Within(.00001));
            Assert.That(block.GetFloat("_Unrelated"), Is.EqualTo(42));
            Call("Clear"); renderer.GetPropertyBlock(block); Assert.That(block.GetFloat("_RippleCount"), Is.Zero);
        }
        [Test]
        public void RecipientRelay_CreatesExactlyOneWave_ForRootAndChild()
        {
            var listenerType = typeof(EnemyActor).Assembly.GetType("TicGame.Architecture.EnemyHitRippleDamageListener");
            Assert.That(listenerType, Is.Not.Null);
            var relay = root.AddComponent(listenerType); listenerType.GetMethod("Configure").Invoke(relay, new object[] { presenter });
            var child = new GameObject("Region"); child.transform.SetParent(root.transform, false);
            child.AddComponent<EnemyHealth>().Initialize(10);
            relay = child.AddComponent(listenerType); listenerType.GetMethod("Configure").Invoke(relay, new object[] { presenter });
            var instance = new DamageInstance("route", null, null, new DamageFormulaValues(1, 1, 0, 0, 0, 0, 1), maxTargets: 2);
            var report = DamageResolver.Resolve(new DamageRequest(instance, new[] { root, child }, Vector2.zero, Vector2.right, targetLimit: 2));
            Assert.That(report.TargetResults.Count, Is.EqualTo(2)); Assert.That(Count, Is.EqualTo(2));
        }
    }
}
