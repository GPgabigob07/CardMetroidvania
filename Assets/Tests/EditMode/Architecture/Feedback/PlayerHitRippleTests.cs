using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerHitRippleTests
    {
        private GameObject root;
        private SimpleHealth health;
        private Component presenter;
        private HitRippleProfileSO profile;
        private SpriteRenderer visual;
        private Texture2D texture;
        private Sprite sprite;
        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Player ripple"); root.transform.position = new Vector3(4, 2, 0);
            health = root.AddComponent<SimpleHealth>(); health.Initialize();
            texture = new Texture2D(32, 32); sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 32);
            var child = new GameObject("Animation"); child.transform.SetParent(root.transform, false);
            visual = child.AddComponent<SpriteRenderer>(); visual.sprite = sprite;
            profile = ScriptableObject.CreateInstance<HitRippleProfileSO>();
            JsonUtility.FromJsonOverwrite("{\"blueColor\":{\"r\":1,\"g\":0,\"b\":0,\"a\":1},\"fatalLeadingColor\":{\"r\":1,\"g\":0,\"b\":0,\"a\":1},\"fatalTrailingColor\":{\"r\":1,\"g\":0,\"b\":0,\"a\":1}}", profile);
            var type = typeof(SimpleHealth).Assembly.GetType("TicGame.Architecture.PlayerHitRipplePresenter");
            Assert.That(type, Is.Not.Null, "Player damage ripple presenter is missing.");
            presenter = root.AddComponent(type);
            type.GetMethod("Configure").Invoke(presenter, new object[] { health, profile, new[] { visual } });
        }
        private int Count => (int)presenter.GetType().GetProperty("ActiveCount").GetValue(presenter);
        private DamageContext Hit(float amount) => new(null, root, null, amount, new Vector2(3.75f, 2.2f), Vector2.right);
        [Test]
        public void PlayerPrefab_HasRedProfileAndRippleBodyMaterial()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            var configured = prefab.GetComponent<PlayerHitRipplePresenter>();
            Assert.That(configured, Is.Not.Null);
            Assert.That(configured.Owner, Is.EqualTo(prefab.GetComponent<SimpleHealth>()));
            Assert.That(configured.Profile.BlueColor, Is.EqualTo(Color.red));
            Assert.That(configured.Profile.FatalLeadingColor, Is.EqualTo(Color.red));
            Assert.That(configured.Profile.FatalTrailingColor, Is.EqualTo(Color.red));
            Assert.That(configured.Visuals.Count, Is.EqualTo(1));
            Assert.That(configured.Visuals[0].sharedMaterial.shader.name, Is.EqualTo("TicGame/2D/Enemy Hit Ripple"));
        }
        [Test]
        public void DirectHealthDamage_CreatesOneRedRipple_AtImpactPoint()
        {
            health.ApplyDamage(Hit(1)); Assert.That(Count, Is.EqualTo(1));
            var block = new MaterialPropertyBlock(); visual.GetPropertyBlock(block);
            Assert.That(block.GetColor("_RippleLeadingColor0"), Is.EqualTo(Color.red));
            Assert.That(block.GetColor("_RippleTrailingColor0"), Is.EqualTo(Color.red));
            var wave = block.GetVector("_RippleOriginRadius0");
            Assert.That(wave.x, Is.EqualTo(-.25f).Within(.0001)); Assert.That(wave.y, Is.EqualTo(.2f).Within(.0001));
        }
        [Test]
        public void FatalAndOrdinaryHits_AreBothFullRed_AndResetClears()
        {
            health.ApplyDamage(Hit(1)); health.ApplyDamage(Hit(10)); Assert.That(Count, Is.EqualTo(2));
            var block = new MaterialPropertyBlock(); visual.GetPropertyBlock(block);
            Assert.That(block.GetColor("_RippleLeadingColor1"), Is.EqualTo(Color.red));
            Assert.That(block.GetColor("_RippleTrailingColor1"), Is.EqualTo(Color.red));
            health.ApplyDamage(Hit(1)); Assert.That(Count, Is.EqualTo(2));
            health.Initialize(); Assert.That(Count, Is.Zero);
        }
        [Test]
        public void HealingHealthCostsAndZeroDamage_DoNotCreateRipples()
        {
            health.TrySpendNonlethal(1); health.Heal(1); health.ApplyDamage(Hit(0)); Assert.That(Count, Is.Zero);
            health.ApplyDamage(Hit(1)); health.Heal(1); Assert.That(Count, Is.EqualTo(1));
        }
        [Test]
        public void ResolverDamage_ProducesOneWave_NotTwo()
        {
            var instance = new DamageInstance("player-test", null, null, new DamageFormulaValues(1, 1, 0, 0, 0, 0, 1));
            DamageResolver.Resolve(new DamageRequest(instance, new[] { root }, new Vector2(3.75f, 2.2f), Vector2.right));
            Assert.That(Count, Is.EqualTo(1));
        }
        [TearDown]
        public void Cleanup()
        { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
    }
}
