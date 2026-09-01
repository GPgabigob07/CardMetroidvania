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
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(projectileObject);
            Object.DestroyImmediate(enemySource);
            Object.DestroyImmediate(playerSource);
            Object.DestroyImmediate(target);
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
    }
}
