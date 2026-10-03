using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class EnemyProjectilePatternLauncherTests
    {
        private readonly List<Object> objects = new();
        private readonly List<EnemyProjectile2D> shots = new();
        private EnemyActor actor;
        private EnemyProjectilePatternLauncher launcher;
        private EnemyAttackDefinitionSO definition;
        private EnemyAttackStep step;
        private DamageProfileSO profile;
        private GargoyleTuningSO tuning;
        private SimpleHealth player;
        [SetUp]
        public void SetUp()
        {
            var root = Track(new GameObject("Gargoyle")); actor = root.AddComponent<EnemyActor>();
            var identity = Track(ScriptableObject.CreateInstance<EnemyDefinitionSO>()); actor.SetDefinition(identity); actor.Initialize();
            tuning = Track(ScriptableObject.CreateInstance<GargoyleTuningSO>()); Set(tuning, "environmentLayer", (LayerMask)(1 << 8));
            launcher = root.AddComponent<EnemyProjectilePatternLauncher>(); launcher.Initialize(actor, tuning);
            launcher.ProjectileLaunched += shot => { shots.Add(shot); Track(shot.gameObject); };
            profile = Track(ScriptableObject.CreateInstance<DamageProfileSO>()); JsonUtility.FromJsonOverwrite("{\"baseDamage\":1}", profile);
            step = new EnemyAttackStep("volley", .45f, .12f, .4f, profile);
            JsonUtility.FromJsonOverwrite("{\"payload\":{\"kind\":1,\"lockedAimDuration\":0.15}}", step);
            definition = Track(ScriptableObject.CreateInstance<EnemyAttackDefinitionSO>()); definition.SetSteps(new[] { step });
            definition.SetPatterns(new[] { new EnemyProjectilePattern(1, .45f, new[] { 0f }),
                new EnemyProjectilePattern(3, .55f, new[] { -12f, 0, 12f }),
                new EnemyProjectilePattern(5, .65f, new[] { -30f, -15f, 0, 15f, 30f }) });
            var template = Track(new GameObject("ProjectileTemplate")); template.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            template.AddComponent<CircleCollider2D>().isTrigger = true;
            Set(definition, "projectilePrefab", template.AddComponent<EnemyProjectile2D>()); template.SetActive(false);
            var playerObject = Track(new GameObject("Player")); player = playerObject.AddComponent<SimpleHealth>(); player.Initialize();
        }
        [TearDown]
        public void TearDown() { Time.timeScale = 1; for (var i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]); objects.Clear(); shots.Clear(); }
        private T Track<T>(T value) where T : Object { objects.Add(value); return value; }
        private static void Set(object owner, string name, object value) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);

        [TestCase(1)] [TestCase(3)] [TestCase(5)]
        public void ReleaseUsesExactAuthoredCountAndAngles_AndDoesNotRepeatToken(int count)
        {
            launcher.Release(definition, count, 1, Vector2.zero, Vector2.right);
            Assert.AreEqual(count, shots.Count);
            var angles = definition.FindPattern(count).Angles;
            for (var i = 0; i < count; i++) Assert.AreEqual(angles[i], Vector2.SignedAngle(Vector2.right, shots[i].Direction), .001f);
            launcher.Release(definition, count, 1, Vector2.zero, Vector2.up); Assert.AreEqual(count, shots.Count);
        }

        [Test] public void AuthoredProjectileRadiusSetsColliderAtLaunch()
        {
            JsonUtility.FromJsonOverwrite("{\"payload\":{\"projectileRadius\":0.3}}", step);
            launcher.Release(definition, 1, 1, Vector2.zero, Vector2.right);
            Assert.That(shots[0].GetComponent<CircleCollider2D>().radius, Is.EqualTo(.3f));
        }

        [Test]
        public void WholeVolleySharesOneAcceptedHitBudget_AcrossChildColliders()
        {
            var child = Track(new GameObject("PlayerCollider")); child.transform.SetParent(player.transform, false); child.AddComponent<BoxCollider2D>();
            launcher.Release(definition, 5, 1, Vector2.zero, Vector2.right);
            foreach (var shot in shots) shot.TryResolveCollision(child);
            Assert.AreEqual(4, player.CurrentHealth); Assert.That(shots, Has.All.Matches<EnemyProjectile2D>(shot => !shot.IsLaunched));
        }

        [Test]
        public void ReentrantTargetCallback_CannotSpendTheBudgetTwice()
        {
            launcher.Release(definition, 3, 1, Vector2.zero, Vector2.right);
            player.Changed += _ => shots[1].TryResolveHit(player.gameObject);
            shots[0].TryResolveHit(player.gameObject); Assert.AreEqual(4, player.CurrentHealth);
        }

        [Test]
        public void RejectedBudgetReservationCanRetry_AndAcceptedReservationStaysSpent()
        {
            var budget = new EnemyCastHitBudget(1, 1);
            Assert.IsTrue(budget.TryReserve(player.gameObject)); Assert.IsFalse(budget.TryReserve(player.gameObject));
            budget.Complete(player.gameObject, false); Assert.IsTrue(budget.TryReserve(player.gameObject));
            budget.Complete(player.gameObject, true); Assert.IsFalse(budget.TryReserve(player.gameObject));
            budget.Complete(player.gameObject, false); Assert.IsFalse(budget.TryReserve(player.gameObject));
        }

        [Test]
        public void LaunchEditsApplyToNextShots_ExistingMotionIsFrozen_AndDamageRemainsLive()
        {
            launcher.Release(definition, 1, 1, Vector2.zero, Vector2.right);
            JsonUtility.FromJsonOverwrite("{\"payload\":{\"projectileSpeed\":10,\"projectileLifetime\":8}}", step);
            JsonUtility.FromJsonOverwrite("{\"baseDamage\":2}", profile);
            launcher.Release(definition, 1, 2, Vector2.zero, Vector2.right);
            Assert.AreEqual(6, shots[0].Speed); Assert.AreEqual(10, shots[1].Speed);
            shots[0].TryResolveHit(player.gameObject); Assert.AreEqual(3, player.CurrentHealth);
            shots[1].FixedTick(3); Assert.IsTrue(shots[1].IsLaunched);
            shots[1].FixedTick(5); Assert.IsFalse(shots[1].IsLaunched);
        }

        [Test]
        public void TerrainCollisionStopsAuthoredShots_WithoutTreatingWallsAsDamageTargets()
        {
            launcher.Release(definition, 1, 1, Vector2.zero, Vector2.right);
            var wall = Track(new GameObject("Wall")); wall.layer = 8; wall.AddComponent<BoxCollider2D>();
            shots[0].TryResolveCollision(wall); Assert.IsFalse(shots[0].IsLaunched); Assert.AreEqual(5, player.CurrentHealth);
        }

        [Test]
        public void CleanupAffectsOnlyOwnedShots_AndDeflectionTransfersOwnership()
        {
            launcher.Release(definition, 3, 1, Vector2.zero, Vector2.right);
            shots[0].Deflect(player.gameObject);
            var otherObject = Track(new GameObject("OtherLauncher")); var other = otherObject.AddComponent<EnemyProjectilePatternLauncher>(); other.Initialize(actor, tuning);
            EnemyProjectile2D foreign = null; other.ProjectileLaunched += shot => { foreign = shot; Track(shot.gameObject); };
            other.Release(definition, 1, 2, Vector2.zero, Vector2.right);
            launcher.CancelOwnedProjectiles();
            Assert.IsTrue(shots[0].IsLaunched); Assert.IsFalse(shots[1].IsLaunched); Assert.IsFalse(shots[2].IsLaunched); Assert.IsTrue(foreign.IsLaunched);
        }

        [Test]
        public void LaunchNotificationCancellationPreventsRemainingEmissions()
        {
            launcher.ProjectileLaunched += _ => launcher.CancelOwnedProjectiles();
            launcher.Release(definition, 5, 1, Vector2.zero, Vector2.right);
            Assert.AreEqual(1, shots.Count); Assert.IsFalse(shots[0].IsLaunched);
        }

        [Test]
        public void PauseAndWorldHoldSuppressQueries_AndLifetimeDoesNotElapse()
        {
            launcher.Release(definition, 1, 1, Vector2.zero, Vector2.right);
            Time.timeScale = 0; shots[0].FixedTick(10); shots[0].TryResolveHit(player.gameObject);
            Assert.IsTrue(shots[0].IsLaunched); Assert.AreEqual(5, player.CurrentHealth);
            launcher.Release(definition, 1, 2, Vector2.zero, Vector2.right); Assert.AreEqual(1, shots.Count);
            Time.timeScale = 1;
            using (player.gameObject.AddComponent<PlayerWorldHold>().Acquire())
            { shots[0].TryResolveHit(player.gameObject); Assert.AreEqual(5, player.CurrentHealth); }
        }

        [Test]
        public void MissingCountVariant_DoesNotFallBackToAnotherPattern()
        {
            launcher.Release(definition, 2, 1, Vector2.zero, Vector2.right); Assert.IsEmpty(shots);
        }

        [Test]
        public void SweptTerrainQueryCannotTunnelThroughWallOnLargeFixedDelta()
        {
            var wall = Track(new GameObject("Wall")); wall.layer = 8; wall.transform.position = new Vector2(2, 0); wall.AddComponent<BoxCollider2D>();
            launcher.Release(definition, 1, 1, Vector2.zero, Vector2.right); Physics2D.SyncTransforms();
            shots[0].FixedTick(.5f); Assert.IsFalse(shots[0].IsLaunched);
        }

        [Test]
        public void ActorDeathDuringLaunchNotificationStopsTheRemainingSpread()
        {
            launcher.ProjectileLaunched += _ => actor.Health.ApplyDamage(new DamageContext(null, actor.gameObject, null,
                actor.Health.MaximumHealth, Vector2.zero, Vector2.right));
            launcher.Release(definition, 5, 1, Vector2.zero, Vector2.right);
            Assert.AreEqual(1, shots.Count); Assert.IsFalse(shots[0].IsLaunched);
        }
    }
}
