using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class BatProjectileLauncherTests
    {
        private readonly List<Object> objectsToDestroy = new List<Object>();
        private GameObject owner;
        private GameObject projectileTemplate;
        private BatProjectileLauncher launcher;
        private readonly List<EnemyProjectile2D> launched = new List<EnemyProjectile2D>();

        [SetUp]
        public void SetUp()
        {
            owner = CreateObject("Bat Launcher");
            projectileTemplate = CreateObject("Projectile Template");
            projectileTemplate.AddComponent<Rigidbody2D>();
            var projectile = projectileTemplate.AddComponent<EnemyProjectile2D>();
            projectileTemplate.SetActive(false);
            launcher = owner.AddComponent<BatProjectileLauncher>();
            launcher.Configure(projectile, owner.transform, owner, projectileSpeed: 6f);
            launcher.ProjectileLaunched += launched.Add;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var projectile in launched)
            {
                if (projectile != null)
                {
                    Object.DestroyImmediate(projectile.gameObject);
                }
            }

            foreach (var instance in objectsToDestroy)
            {
                Object.DestroyImmediate(instance);
            }

            objectsToDestroy.Clear();
            launched.Clear();
        }

        [Test]
        public void Burst_TwoShotPlan_SpawnsAtMostTwoProjectiles()
        {
            var plan = new BatFirePlan(2f, 2, 0.1f, Vector2.left);

            launcher.BeginBurst(plan);
            launcher.Tick(0.1f);
            launcher.Tick(10f);

            Assert.AreEqual(2, launched.Count);
        }

        [Test]
        public void Burst_TargetMovementAfterFirstShot_DoesNotReaimLockedDirection()
        {
            var plan = new BatFirePlan(2f, 2, 0.1f, new Vector2(2f, 1f));

            launcher.BeginBurst(plan);
            owner.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            launcher.Tick(0.1f);

            Assert.AreEqual(2, launched.Count);
            Assert.AreEqual(launched[0].Direction, launched[1].Direction);
            Assert.AreEqual(new Vector2(2f, 1f).normalized, launched[1].Direction);
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            objectsToDestroy.Add(instance);
            return instance;
        }
    }
}
