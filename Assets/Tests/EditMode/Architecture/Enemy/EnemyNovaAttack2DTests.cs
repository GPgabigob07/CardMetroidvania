using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class EnemyNovaAttack2DTests
    {
        private readonly List<Object> objects = new();
        private EnemyActor actor;
        private EnemyNovaAttack2D nova;
        private EnemyAttackDefinitionSO definition;
        private EnemyAttackStep step;
        private SimpleHealth player;
        [SetUp]
        public void SetUp()
        {
            var root = Track(new GameObject("Gargoyle")); actor = root.AddComponent<EnemyActor>();
            var identity = Track(ScriptableObject.CreateInstance<EnemyDefinitionSO>()); actor.SetDefinition(identity); actor.Initialize();
            nova = root.AddComponent<EnemyNovaAttack2D>(); nova.Initialize(actor);
            var profile = Track(ScriptableObject.CreateInstance<DamageProfileSO>()); JsonUtility.FromJsonOverwrite("{\"baseDamage\":2}", profile);
            step = new EnemyAttackStep("nova", 2.4f, .15f, .9f, profile); JsonUtility.FromJsonOverwrite("{\"payload\":{\"kind\":3}}", step);
            definition = Track(ScriptableObject.CreateInstance<EnemyAttackDefinitionSO>()); definition.SetSteps(new[] { step });
            var target = Track(new GameObject("Player")); target.transform.position = new Vector2(2, 0); player = target.AddComponent<SimpleHealth>(); player.Initialize();
            target.AddComponent<BoxCollider2D>(); Physics2D.SyncTransforms();
        }
        [TearDown]
        public void TearDown() { Time.timeScale = 1; for (var i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]); objects.Clear(); }
        private T Track<T>(T value) where T : Object { objects.Add(value); return value; }
        private DamageContext CoreHit(string id = "slash1", DamageOriginKind origin = DamageOriginKind.Primary, bool enhanced = true, float amount = 1)
            => new DamageContext(player.gameObject, actor.gameObject, null, amount, Vector2.zero, Vector2.right,
                poiseDamage: 2.4f, isCardEnhancedMelee: enhanced, provenance: new DamageProvenance(origin, null, "root", null, 0), attackExecutionId: id);
        [Test]
        public void TwoEmpoweredPrimaryHitsBreakDefaultCore_AndDuplicateIdentityCountsOnce()
        {
            nova.Begin(1, definition); Assert.AreEqual(4.8f, nova.RemainingStability, .001f);
            Assert.IsTrue(nova.TryApplyCoreHit(CoreHit())); Assert.AreEqual(2.4f, nova.RemainingStability, .001f);
            Assert.IsFalse(nova.TryApplyCoreHit(CoreHit())); Assert.IsTrue(nova.TryApplyCoreHit(CoreHit("slash2")));
            Assert.AreEqual(0, nova.RemainingStability); Assert.IsFalse(nova.IsTelegraphing);
            nova.Release(Vector2.zero); Assert.AreEqual(5, player.CurrentHealth);
        }
        [TestCase(DamageOriginKind.Supplemental)] [TestCase(DamageOriginKind.Converted)]
        public void SupplementalAndConvertedPacketsDoNotContribute(DamageOriginKind origin)
        { nova.Begin(1, definition); Assert.IsFalse(nova.TryApplyCoreHit(CoreHit(origin: origin))); Assert.AreEqual(4.8f, nova.RemainingStability); }
        [Test]
        public void OrdinaryMissingIdentityAndZeroHealthEvidenceDoNotContribute()
        {
            nova.Begin(1, definition); Assert.IsFalse(nova.TryApplyCoreHit(CoreHit(enhanced: false)));
            Assert.IsFalse(nova.TryApplyCoreHit(CoreHit(id: null))); Assert.IsFalse(nova.TryApplyCoreHit(CoreHit(amount: 0)));
            Assert.AreEqual(4.8f, nova.RemainingStability);
        }
        [Test]
        public void NonfiniteHealthEvidenceCannotQualifyForCoreCounter()
        { nova.Begin(1, definition); Assert.IsFalse(nova.TryApplyCoreHit(CoreHit(amount: float.NaN))); Assert.AreEqual(4.8f, nova.RemainingStability); }
        [Test]
        public void StabilityEditsPreserveAccumulatedDamage_AndShorteningBreaksAtNextTick()
        {
            nova.Begin(1, definition); nova.TryApplyCoreHit(CoreHit());
            JsonUtility.FromJsonOverwrite("{\"payload\":{\"reactorStability\":6}}", step); nova.Tick(0); Assert.AreEqual(3.6f, nova.RemainingStability, .001f);
            JsonUtility.FromJsonOverwrite("{\"payload\":{\"reactorStability\":2}}", step); nova.Tick(0); Assert.IsFalse(nova.IsTelegraphing);
        }
        [Test]
        public void ReleaseIsOncePerPlayer_AndEscapeOutsideLiveRadiusAvoidsDamage()
        {
            var child = Track(new GameObject("PlayerCollider")); child.transform.SetParent(player.transform, false); child.AddComponent<BoxCollider2D>();
            nova.Begin(1, definition); Physics2D.SyncTransforms(); nova.Release(Vector2.zero); nova.Release(Vector2.zero); Assert.AreEqual(3, player.CurrentHealth);
            nova.Begin(2, definition); JsonUtility.FromJsonOverwrite("{\"payload\":{\"novaRadius\":1}}", step);
            nova.Release(Vector2.zero); Assert.AreEqual(3, player.CurrentHealth);
        }
        [Test]
        public void TerrainDoesNotShieldNova_AndDeathSuppressesRelease()
        {
            var wall = Track(new GameObject("Wall")); wall.transform.position = Vector2.right; wall.AddComponent<BoxCollider2D>(); Physics2D.SyncTransforms();
            nova.Begin(1, definition); nova.Release(Vector2.zero); Assert.AreEqual(3, player.CurrentHealth);
            nova.Begin(2, definition); actor.Health.ApplyDamage(new DamageContext(null, actor.gameObject, null, actor.Health.MaximumHealth, Vector2.zero, Vector2.right));
            nova.Release(Vector2.zero); Assert.AreEqual(3, player.CurrentHealth);
        }
        [Test]
        public void PauseAndHoldSuppressRelease_AndCancellationInvalidatesOldTokens()
        {
            nova.Begin(1, definition); Time.timeScale = 0; nova.Release(Vector2.zero); Assert.AreEqual(5, player.CurrentHealth); Time.timeScale = 1;
            using (player.gameObject.AddComponent<PlayerWorldHold>().Acquire()) nova.Release(Vector2.zero);
            Assert.AreEqual(5, player.CurrentHealth); nova.Cancel(); nova.Begin(1, definition); nova.Release(Vector2.zero); Assert.AreEqual(5, player.CurrentHealth);
        }
    }
}
