using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class EnemyMeleeAttack2DTests
    {
        private readonly List<Object> objects = new();
        private EnemyActor actor;
        private EnemyMeleeAttack2D melee;
        private SimpleHealth health;
        private EnemyAttackRunner runner;
        private DamageProfileSO profile;
        [SetUp]
        public void SetUp()
        {
            var root = Track(new GameObject("Enemy")); actor = root.AddComponent<EnemyActor>();
            var identity = Track(ScriptableObject.CreateInstance<EnemyDefinitionSO>()); actor.SetDefinition(identity); actor.Initialize();
            melee = root.AddComponent<EnemyMeleeAttack2D>(); melee.Initialize(actor);
            var player = Track(new GameObject("Player")); player.transform.position = new Vector2(1, 1);
            health = player.AddComponent<SimpleHealth>(); health.Initialize();
            for (var i = 0; i < 2; i++)
            {
                var child = Track(new GameObject("PlayerCollider")); child.transform.SetParent(player.transform, false);
                child.AddComponent<BoxCollider2D>();
            }
            profile = Track(ScriptableObject.CreateInstance<DamageProfileSO>()); JsonUtility.FromJsonOverwrite("{\"baseDamage\":1}", profile);
            var definition = Track(ScriptableObject.CreateInstance<EnemyAttackDefinitionSO>());
            definition.SetSteps(new[] { new EnemyAttackStep("claw", .35f, .12f, .18f, profile) });
            runner = new EnemyAttackRunner(); runner.Begin(definition, 1); runner.SetAim(Vector2.right);
            Physics2D.SyncTransforms();
        }
        [TearDown]
        public void TearDown() { Time.timeScale = 1; for (var i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]); objects.Clear(); }
        private T Track<T>(T value) where T : Object { objects.Add(value); return value; }

        [Test]
        public void OverlappingPlayerCollidersResolveOncePerStrike_AndNumericDamageUsesCurrentProfile()
        {
            melee.Sample(runner.Current); Assert.AreEqual(5, health.CurrentHealth);
            runner.Tick(.35f); JsonUtility.FromJsonOverwrite("{\"baseDamage\":2}", profile);
            melee.Sample(runner.Current); melee.Sample(runner.Current); Assert.AreEqual(3, health.CurrentHealth);
        }

        [Test]
        public void DeathPauseAndHoldSuppressDamageQueries()
        {
            runner.Tick(.35f); Time.timeScale = 0; melee.Sample(runner.Current); Assert.AreEqual(5, health.CurrentHealth);
            Time.timeScale = 1;
            using (health.gameObject.AddComponent<PlayerWorldHold>().Acquire()) { melee.Sample(runner.Current); Assert.AreEqual(5, health.CurrentHealth); }
            actor.Health.ApplyDamage(new DamageContext(null, actor.gameObject, null, actor.Health.MaximumHealth, Vector2.zero, Vector2.right));
            melee.Sample(runner.Current); Assert.AreEqual(5, health.CurrentHealth);
        }

        [Test]
        public void CancelInvalidatesOldSamples_AndDoesNotReplayAcceptedDamage()
        {
            runner.Tick(.35f); melee.Sample(runner.Current); melee.Cancel(); melee.Sample(runner.Current);
            Assert.AreEqual(4, health.CurrentHealth);
        }

        [Test]
        public void ReentrantCancellationFromDamageNotification_SuppressesOtherTargets()
        {
            var other = Track(new GameObject("OtherPlayer")); other.transform.position = new Vector2(1.2f, 1);
            var otherHealth = other.AddComponent<SimpleHealth>(); otherHealth.Initialize(); other.AddComponent<BoxCollider2D>();
            health.Changed += _ => melee.Cancel(); otherHealth.Changed += _ => melee.Cancel();
            Physics2D.SyncTransforms(); runner.Tick(.35f); melee.Sample(runner.Current);
            Assert.AreEqual(9, health.CurrentHealth + otherHealth.CurrentHealth);
        }
    }
}
