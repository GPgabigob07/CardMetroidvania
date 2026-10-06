using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerRepelRuntimeTests
    {
        private readonly List<Object> objects = new();
        private GameObject player;
        private Component repel;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1;
            player = Track(new GameObject("Repel Player"));
            player.AddComponent<SimpleHealth>().Initialize();
            var type = typeof(EnemyProjectile2D).Assembly.GetType("TicGame.Architecture.PlayerRepelRuntime");
            Assert.NotNull(type, "The player-local Repel capability must exist.");
            repel = player.AddComponent(type);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1;
            for (var index = objects.Count - 1; index >= 0; index--) Object.DestroyImmediate(objects[index]);
            objects.Clear();
        }

        private T Track<T>(T value) where T : Object { objects.Add(value); return value; }
        private object Call(string name, params object[] arguments) => repel.GetType().GetMethod(name).Invoke(repel, arguments);
        private bool Active => (bool)repel.GetType().GetProperty("IsActive").GetValue(repel);
        private EnemyProjectile2D Shot(Vector2 position, Vector2 direction)
        {
            var owner = Track(new GameObject("Shot")); owner.transform.position = position;
            owner.AddComponent<Rigidbody2D>();
            var shot = owner.AddComponent<EnemyProjectile2D>();
            shot.Launch(direction, Track(new GameObject("Enemy")), 4);
            return shot;
        }

        [Test]
        public void Duration_ExpiresAtThreeSeconds_AndRejectsRefresh()
        {
            Assert.IsTrue((bool)Call("Arm", 3f));
            Call("Tick", 2.9f); Assert.IsTrue(Active);
            Assert.IsFalse((bool)Call("Arm", 3f));
            Call("Tick", .1f); Assert.IsFalse(Active);
        }

        [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidDuration_DoesNotArm(float duration)
        {
            Assert.IsFalse((bool)Call("Arm", duration)); Assert.IsFalse(Active);
        }

        [Test]
        public void PauseAndWorldHold_FreezeExpiryAndSuppressReflection()
        {
            Call("Arm", 3f); var shot = Shot(Vector2.right, Vector2.left);
            Time.timeScale = 0; Call("Tick", 4f); Assert.IsTrue(Active);
            Assert.IsFalse((bool)Call("TryRepel", shot)); Time.timeScale = 1;
            using (player.AddComponent<PlayerWorldHold>().Acquire())
            { Call("Tick", 4f); Assert.IsTrue(Active); Assert.IsFalse((bool)Call("TryRepel", shot)); }
            Assert.IsTrue((bool)Call("TryRepel", shot));
        }

        [Test]
        public void Reflection_IsRadialOncePerShot_WithSeveralShotsAllowed()
        {
            Call("Arm", 3f);
            var shot = Shot(new Vector2(2, 1), Vector2.down);
            var root = shot.Provenance.RootInstanceId;
            Assert.IsTrue((bool)Call("TryRepel", shot));
            Assert.That(Vector2.Distance(new Vector2(2, 1).normalized, shot.Direction), Is.LessThan(.0001f));
            Assert.AreEqual(player, shot.SourceObject); Assert.AreEqual(4, shot.Speed);
            Assert.AreEqual(10, shot.PoiseDamage); Assert.AreEqual(root, shot.Provenance.RootInstanceId);
            Assert.AreEqual(DamageProcPolicy.None, shot.ProcPolicy);
            Assert.IsFalse((bool)Call("TryRepel", shot));
            Assert.IsTrue((bool)Call("TryRepel", Shot(Vector2.left, Vector2.right)));
        }

        [Test]
        public void CoincidentShot_ReversesIncomingDirection_WithoutResettingLifetime()
        {
            var shot = Shot(Vector2.zero, Vector2.up); shot.FixedTick(2.5f);
            Call("Arm", 3f); Assert.IsTrue((bool)Call("TryRepel", shot));
            Assert.AreEqual(Vector2.down, shot.Direction);
            shot.FixedTick(.5f); Assert.IsFalse(shot.IsLaunched);
        }

        [Test]
        public void UnarmedAndExpired_DoNotReflect_AndDeathClears()
        {
            var shot = Shot(Vector2.right, Vector2.left);
            Assert.IsFalse((bool)Call("TryRepel", shot));
            Call("Arm", 3f); Call("Tick", 3f); Assert.IsFalse((bool)Call("TryRepel", shot));
            Call("Arm", 3f);
            player.GetComponent<SimpleHealth>().ApplyDamage(new DamageContext(null, player, null, 5, Vector2.zero, Vector2.right));
            Assert.IsFalse(Active); Assert.IsFalse((bool)Call("Arm", 3f));
        }
    }
}
