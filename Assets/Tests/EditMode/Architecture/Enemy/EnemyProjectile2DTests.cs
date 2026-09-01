using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class EnemyProjectile2DTests
    {
        private GameObject projectileObject;
        private GameObject enemySource;
        private GameObject playerSource;
        private GameObject target;
        private DamageProfileSO damageProfile;
        private EnemyProjectile2D projectile;

        [SetUp]
        public void SetUp()
        {
            projectileObject = new GameObject("Projectile");
            projectileObject.AddComponent<Rigidbody2D>();
            var collider = projectileObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            projectile = projectileObject.AddComponent<EnemyProjectile2D>();
            enemySource = new GameObject("Enemy Source");
            playerSource = new GameObject("Player Source");
            target = new GameObject("Target");
            damageProfile = ScriptableObject.CreateInstance<DamageProfileSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(projectileObject);
            Object.DestroyImmediate(enemySource);
            Object.DestroyImmediate(playerSource);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(damageProfile);
        }

        [Test]
        public void Deflect_ReversesDirectionAndBuildsPlayerOwnedTenPoiseDamage()
        {
            projectile.Launch(Vector2.right, enemySource, 4f);
            var originalRootInstanceId = projectile.Provenance.RootInstanceId;
            projectile.Deflect(playerSource);

            Assert.AreEqual(Vector2.left, projectile.Direction);
            Assert.AreEqual(playerSource, projectile.SourceObject);
            Assert.AreEqual(10f, projectile.PoiseDamage);
            Assert.AreEqual(DamageOriginKind.Converted, projectile.Provenance.OriginKind);
            Assert.AreEqual(originalRootInstanceId, projectile.Provenance.RootInstanceId);
            Assert.AreEqual(DamageProcPolicy.None, projectile.ProcPolicy);
        }

        [Test]
        public void TryResolveHit_AcceptedZeroDamage_DisablesProjectile()
        {
            target.AddComponent<AcceptingZeroDamage>();
            projectile.Launch(Vector2.right, enemySource, 4f);

            var resolved = projectile.TryResolveHit(target);

            Assert.IsTrue(resolved);
            Assert.IsFalse(projectileObject.activeSelf);
        }

        [Test]
        public void FixedTick_LifetimeExpires_DisablesProjectile()
        {
            projectile.Launch(Vector2.right, enemySource, 4f);

            projectile.FixedTick(3f);

            Assert.IsFalse(projectile.IsLaunched);
            Assert.IsFalse(projectileObject.activeSelf);
        }

        [Test]
        public void TryResolveHit_DeflectedProjectile_PreservesHealthDamageAndSuppressesPlayerProcs()
        {
            SetField(damageProfile, "baseDamage", 7f);
            SetField(projectile, "damageProfile", damageProfile);
            var effects = playerSource.AddComponent<PlayerCombatEffects>();
            effects.AddChainCapacity(2, damagePercentPerIncrement: 0.1f);
            effects.ArmSupplementalDamage("deflect", "overcharge", totalMultiplier: 2f);
            var receiver = target.AddComponent<RecordingDamageable>();
            projectile.Launch(Vector2.right, enemySource, 4f);
            projectile.Deflect(playerSource);

            var resolved = projectile.TryResolveHit(target);

            Assert.IsTrue(resolved);
            Assert.AreEqual(7f, receiver.LastContext.Amount);
            Assert.AreEqual(10f, receiver.LastContext.PoiseDamage);
            Assert.AreEqual(0, effects.ChainIncrements);
            Assert.IsNull(effects.LastSupplementalReport);
        }

        [Test]
        public void TryResolveCollision_DeflectedProjectileOnBatHurtbox_RoutesThroughBatPolicy()
        {
            SetField(damageProfile, "baseDamage", 2f);
            SetField(projectile, "damageProfile", damageProfile);
            var health = target.AddComponent<EnemyHealth>();
            health.Initialize(12f);
            var poise = target.AddComponent<EnemyPoise>();
            poise.Initialize(30f, 0f);
            var policy = target.AddComponent<BatMachineDamagePolicy>();
            policy.SetDependencies(health, poise, null);
            var hurtboxObject = new GameObject("Bat Hurtbox");
            hurtboxObject.transform.SetParent(target.transform);
            var hurtbox = hurtboxObject.AddComponent<BatMachineHurtbox>();
            hurtbox.Configure(policy);
            projectile.Launch(Vector2.right, enemySource, 4f);
            projectile.Deflect(playerSource);

            var resolved = projectile.TryResolveCollision(hurtboxObject);

            Assert.IsTrue(resolved);
            Assert.AreEqual(10f, health.CurrentHealth);
            Assert.AreEqual(20f, poise.CurrentPoise);
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private sealed class AcceptingZeroDamage : MonoBehaviour, IDamageable
        {
            public DamageResult ApplyDamage(in DamageContext context)
            {
                return new DamageResult(
                    accepted: true,
                    killed: false,
                    appliedAmount: 0f,
                    remainingHealth: 1f,
                    hitStopSeconds: 0f);
            }
        }

        private sealed class RecordingDamageable : MonoBehaviour, IDamageable
        {
            public DamageContext LastContext { get; private set; }

            public DamageResult ApplyDamage(in DamageContext context)
            {
                LastContext = context;
                return new DamageResult(
                    accepted: true,
                    killed: false,
                    appliedAmount: context.Amount,
                    remainingHealth: 100f - context.Amount,
                    hitStopSeconds: 0f);
            }
        }
    }
}
