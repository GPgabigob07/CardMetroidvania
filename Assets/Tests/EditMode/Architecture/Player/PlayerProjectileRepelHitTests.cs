using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerProjectileRepelHitTests
    {
        private readonly List<Object> objects = new();
        private GameObject player;
        private PlayerAttackHitDetector2D detector;
        private PlayerRepelRuntime repel;
        private MeleeAction action;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1;
            player = Track(new GameObject("Melee Player"));
            player.AddComponent<SimpleHealth>().Initialize();
            repel = player.AddComponent<PlayerRepelRuntime>();
            detector = player.AddComponent<PlayerAttackHitDetector2D>();
            var controller = player.AddComponent<PlayerController>();
            var context = new PlayerContext(null, null, null, null, null);
            var runner = new PlayerActionRunner(); context.AttachRuntime(null, runner);
            typeof(PlayerController).GetProperty("Context").SetValue(controller, context);
            typeof(PlayerController).GetProperty("ActionRunner").SetValue(controller, runner);
            action = new MeleeAction(); runner.TryStartAction(context, action);
            detector.Initialize(controller);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1;
            for (var index = objects.Count - 1; index >= 0; index--) Object.DestroyImmediate(objects[index]);
            objects.Clear();
        }

        private T Track<T>(T value) where T : Object { objects.Add(value); return value; }
        private void Resolve() => typeof(PlayerAttackHitDetector2D).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(detector, null);
        private EnemyProjectile2D Shot(Vector2 position, bool childCollider = false)
        {
            var owner = Track(new GameObject("Projectile")); owner.layer = LayerMask.NameToLayer("Enemy"); owner.transform.position = position;
            owner.AddComponent<Rigidbody2D>();
            var shot = owner.AddComponent<EnemyProjectile2D>();
            var shape = owner;
            if (childCollider) { shape = new GameObject("Projectile shape"); shape.layer = owner.layer; shape.transform.SetParent(owner.transform, false); }
            shape.AddComponent<CircleCollider2D>().isTrigger = true;
            shot.Launch(Vector2.left, Track(new GameObject("Enemy source")), 4f);
            Physics2D.SyncTransforms();
            return shot;
        }

        [TestCase(0)] [TestCase(8)]
        public void Execution_ProjectileOnlyHit_ReflectsWithoutConfirmingEnemyDamage(int projectileLayer)
        {
            var shot = Shot(Vector2.right); shot.gameObject.layer = projectileLayer;
            Physics2D.SyncTransforms(); repel.Arm(3f); Resolve();
            Assert.AreEqual(player, shot.SourceObject); Assert.AreEqual(Vector2.right, shot.Direction);
            Assert.IsFalse(action.HasConfirmedHit);
        }

        [TestCase(PlayerActionPhase.Reading)] [TestCase(PlayerActionPhase.Recovery)]
        public void NonExecutionPhase_DoesNotRepel(PlayerActionPhase phase)
        {
            var shot = Shot(Vector2.right); repel.Arm(3f); action.Phase = phase; Resolve();
            Assert.AreNotEqual(player, shot.SourceObject);
        }

        [Test]
        public void UnarmedPausedAndOutOfBox_DoNotRepel()
        {
            var near = Shot(Vector2.right); var far = Shot(new Vector2(4, 0)); Resolve();
            Assert.AreNotEqual(player, near.SourceObject);
            repel.Arm(3f); Time.timeScale = 0; Resolve(); Assert.AreNotEqual(player, near.SourceObject);
            Time.timeScale = 1; Resolve(); Assert.AreEqual(player, near.SourceObject); Assert.AreNotEqual(player, far.SourceObject);
        }

        [Test]
        public void ChildAndDuplicateColliders_MultipleShots_ConvertOnce()
        {
            var first = Shot(Vector2.right, childCollider: true);
            first.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            var second = Shot(new Vector2(1.5f, 0)); repel.Arm(3f); Resolve();
            Assert.AreEqual(player, first.SourceObject); Assert.AreEqual(player, second.SourceObject);
            var direction = first.Direction; Resolve(); Assert.AreEqual(direction, first.Direction);
        }

        [Test]
        public void ProjectileCoverage_DoesNotBroadenNormalDamageLayers()
        {
            typeof(PlayerAttackHitDetector2D).GetField("targetLayers", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(detector, (LayerMask)LayerMask.GetMask("Environment"));
            var shot = Shot(Vector2.right);
            var enemy = Track(new GameObject("Enemy health")); enemy.layer = LayerMask.NameToLayer("Enemy"); enemy.transform.position = Vector2.right;
            enemy.AddComponent<BoxCollider2D>(); var health = enemy.AddComponent<SimpleHealth>(); health.Initialize();
            Physics2D.SyncTransforms(); repel.Arm(3f); Resolve();
            Assert.AreEqual(player, shot.SourceObject); Assert.AreEqual(5, health.CurrentHealth); Assert.IsFalse(action.HasConfirmedHit);
        }

        [Test]
        public void SameSwing_RepelsProjectileAndStillDamagesEnemy()
        {
            var shot = Shot(Vector2.right);
            var enemy = Track(new GameObject("Damageable enemy")); enemy.transform.position = Vector2.right;
            enemy.AddComponent<BoxCollider2D>(); var health = enemy.AddComponent<SimpleHealth>(); health.Initialize();
            Physics2D.SyncTransforms(); repel.Arm(3f); Resolve();
            Assert.AreEqual(player, shot.SourceObject); Assert.AreEqual(4, health.CurrentHealth); Assert.IsTrue(action.HasConfirmedHit);
        }

        [Test]
        public void MultiEnemySwing_ReportsEachColliderContact_AndConfirmsOnce()
        {
            var first = Track(new GameObject("First enemy")); first.transform.position = new Vector2(.5f, -.3f);
            var second = Track(new GameObject("Second enemy")); second.transform.position = new Vector2(1.7f, .4f);
            var firstCollider = first.AddComponent<BoxCollider2D>(); firstCollider.size = new Vector2(.2f, .2f);
            var secondCollider = second.AddComponent<BoxCollider2D>(); secondCollider.size = new Vector2(.2f, .2f);
            var firstHealth = first.AddComponent<EnemyHealth>(); firstHealth.Initialize(10);
            var secondHealth = second.AddComponent<EnemyHealth>(); secondHealth.Initialize(10);
            Vector2? firstPoint = null, secondPoint = null;
            firstHealth.Damaged += hit => firstPoint = hit.Context.HitPoint;
            secondHealth.Damaged += hit => secondPoint = hit.Context.HitPoint;
            Physics2D.SyncTransforms(); Resolve();
            Assert.That(firstPoint.HasValue && secondPoint.HasValue, Is.True);
            Assert.That(firstPoint.Value, Is.EqualTo(firstCollider.ClosestPoint(new Vector2(0, .15f))), "Melee contact must be on the attacker-facing boundary, not at an overlap-query center inside the body.");
            Assert.That(secondPoint.Value, Is.EqualTo(secondCollider.ClosestPoint(new Vector2(0, .15f))));
            Assert.That(firstPoint.Value, Is.Not.EqualTo(secondPoint.Value));
            Assert.That(action.HasConfirmedHit, Is.True);
            Resolve(); Assert.That(firstHealth.CurrentHealth, Is.EqualTo(9)); Assert.That(secondHealth.CurrentHealth, Is.EqualTo(9));
        }

        private sealed class MeleeAction : IPlayerAction, IPlayerActionAnimationSource, IPlayerAttackHitConfirmation
        {
            public PlayerActionPhase Phase = PlayerActionPhase.Execution;
            public PlayerActionState State => PlayerActionState.Attack1;
            public bool IsComplete => false;
            public PlayerActionPhase AnimationPhase => Phase;
            public PlayerCardTimeState AnimationCardTime => PlayerCardTimeState.None;
            public bool HasConfirmedHit { get; private set; }
            public void ConfirmHit() => HasConfirmedHit = true;
            public void Enter(PlayerContext context) { }
            public void Tick(PlayerContext context, float deltaTime) { }
            public void FixedTick(PlayerContext context, float fixedDeltaTime) { }
            public void Exit(PlayerContext context) { }
        }
    }
}
