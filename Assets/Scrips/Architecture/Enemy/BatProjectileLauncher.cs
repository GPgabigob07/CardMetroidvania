using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public readonly struct BatFirePlan
    {
        public BatFirePlan(float cooldown, int projectileCount, float interShotDelay, Vector2 lockedDirection)
        {
            Cooldown = Mathf.Max(0f, cooldown);
            ProjectileCount = Mathf.Max(0, projectileCount);
            InterShotDelay = Mathf.Max(0f, interShotDelay);
            LockedDirection = lockedDirection.sqrMagnitude > 0f ? lockedDirection.normalized : Vector2.right;
        }

        public float Cooldown { get; }
        public int ProjectileCount { get; }
        public float InterShotDelay { get; }
        public Vector2 LockedDirection { get; }
    }

    public sealed class BatProjectileLauncher : MonoBehaviour
    {
        [Header(header: "Projectile")]
        [Tooltip(tooltip: "Projectile template instantiated once per planned burst shot.")]
        [SerializeField] private EnemyProjectile2D projectilePrefab;

        [Tooltip(tooltip: "Transform used as the origin for spawned projectiles. Falls back to this transform.")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip(tooltip: "Object recorded as the source of launched enemy projectiles. Falls back to this GameObject.")]
        [SerializeField] private GameObject sourceObject;

        [Min(min: 0f)]
        [Tooltip(tooltip: "World speed assigned to every projectile in a burst.")]
        [SerializeField] private float projectileSpeed = 6f;

        private BatFirePlan activePlan;
        private float timeUntilNextShot;
        private int spawnedCount;

        public event Action<EnemyProjectile2D> ProjectileLaunched;

        public bool IsBurstActive { get; private set; }

        public void BeginBurst(in BatFirePlan plan)
        {
            activePlan = plan;
            spawnedCount = 0;
            timeUntilNextShot = 0f;
            IsBurstActive = activePlan.ProjectileCount > 0 && projectilePrefab != null;
            TrySpawnNext();
        }

        public void Tick(float deltaTime)
        {
            if (!IsBurstActive)
            {
                return;
            }

            timeUntilNextShot -= Mathf.Max(0f, deltaTime);
            if (timeUntilNextShot <= 0f)
            {
                TrySpawnNext();
            }
        }

        public void CancelBurst()
        {
            IsBurstActive = false;
        }

        public void Configure(
            EnemyProjectile2D projectile,
            Transform projectileSpawn,
            GameObject source,
            float projectileSpeed)
        {
            projectilePrefab = projectile;
            spawnPoint = projectileSpawn;
            sourceObject = source;
            this.projectileSpeed = Mathf.Max(0f, projectileSpeed);
        }

        private void TrySpawnNext()
        {
            if (!IsBurstActive || spawnedCount >= activePlan.ProjectileCount)
            {
                IsBurstActive = false;
                return;
            }

            var origin = spawnPoint != null ? spawnPoint : transform;
            var projectile = Instantiate(projectilePrefab, origin.position, Quaternion.identity);
            projectile.Launch(
                activePlan.LockedDirection,
                sourceObject != null ? sourceObject : gameObject,
                projectileSpeed);
            spawnedCount++;
            ProjectileLaunched?.Invoke(projectile);

            if (spawnedCount >= activePlan.ProjectileCount)
            {
                IsBurstActive = false;
                return;
            }

            timeUntilNextShot = activePlan.InterShotDelay;
        }
    }
}
