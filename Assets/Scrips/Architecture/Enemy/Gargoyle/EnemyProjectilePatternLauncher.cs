using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class EnemyProjectilePatternLauncher : MonoBehaviour
    {
        public event Action<EnemyProjectile2D> ProjectileLaunched;
        [Header("Encounter Bindings")]
        [SerializeField] private EnemyActor actor;
        [SerializeField] private GargoyleTuningSO tuning;
        [SerializeField] private GargoyleBrain brain;
        private readonly List<EnemyProjectile2D> registry = new();
        private readonly HashSet<long> releasedCasts = new();
        private int cancellationRevision;
        private EnemyActor subscribedActor;
        private GargoyleBrain subscribedBrain;
        public IReadOnlyList<EnemyProjectile2D> OwnedProjectiles => registry
            .Where(projectile => projectile != null && projectile.Owner == this && projectile.IsLaunched).ToArray();
        public bool CanSimulateProjectiles => isActiveAndEnabled && actor != null && actor.IsOperational
            && Time.timeScale > 0 && (brain == null || brain.CanSimulateProjectiles);

        public void Initialize(EnemyActor actor, GargoyleTuningSO tuning, GargoyleBrain brain = null)
        {
            Unsubscribe();
            this.actor = actor; this.tuning = tuning; this.brain = brain;
            subscribedActor = actor; subscribedBrain = brain;
            if (subscribedActor != null) subscribedActor.Defeated += OnDefeated;
            if (subscribedBrain != null) subscribedBrain.OffenseCancelled += CancelOwnedProjectiles;
        }
        public void Release(EnemyAttackDefinitionSO definition, int count, long castToken, Vector2 origin, Vector2 direction)
        {
            if (definition == null || !definition.TryCaptureSteps(out var captured)) return;
            var step = captured.FirstOrDefault(candidate => candidate.Payload.Kind == EnemyAttackPayloadKind.Volley);
            var pattern = definition.FindPattern(count);
            if (step == null || pattern == null || pattern.GetValidationErrors().Count != 0) return;
            Release(definition, step, pattern.Copy(), castToken, origin, direction);
        }
        public void Release(in EnemyAttackExecutionSnapshot execution, Vector2 origin)
        {
            if (!execution.IsRunning || execution.Phase != EnemyAttackPhase.Active || execution.Kind != EnemyAttackPayloadKind.Volley
                || execution.Pattern == null) return;
            Release(execution.Definition, execution.Step, execution.Pattern.Copy(), execution.ExecutionToken, origin, execution.Aim);
        }
        private void Release(EnemyAttackDefinitionSO definition, EnemyAttackStep step, EnemyProjectilePattern pattern,
            long token, Vector2 origin, Vector2 direction)
        {
            if (!CanSimulateProjectiles || (brain != null && !brain.CanEmit) || tuning == null || tuning.EnvironmentLayer.value == 0
                || definition.ProjectilePrefab == null || token <= 0 || releasedCasts.Contains(token)
                || !float.IsFinite(direction.x) || !float.IsFinite(direction.y) || direction.sqrMagnitude == 0) return;
            releasedCasts.Add(token);
            var revision = cancellationRevision;
            var payload = step.Payload;
            var budget = new EnemyCastHitBudget(token, payload.AcceptedHitLimit);
            registry.RemoveAll(projectile => projectile == null);
            foreach (var angle in pattern.Angles)
            {
                if (revision != cancellationRevision || !CanSimulateProjectiles || (brain != null && !brain.CanEmit)) return;
                var projectile = Instantiate(definition.ProjectilePrefab, origin, Quaternion.identity);
                projectile.transform.localScale = Vector3.one;
                var shape = projectile.GetComponent<CircleCollider2D>(); if (shape != null) shape.radius = payload.ProjectileRadius;
                var sprite = projectile.GetComponent<SpriteRenderer>();
                if (sprite != null) { sprite.drawMode = SpriteDrawMode.Sliced; sprite.size = Vector2.one * (payload.ProjectileRadius * 2); }
                registry.Add(projectile);
                var shotDirection = (Vector2)(Quaternion.Euler(0, 0, angle) * direction.normalized);
                projectile.Launch(shotDirection, actor.gameObject, payload.ProjectileSpeed, payload.ProjectileLifetime,
                    step.DamageProfile, tuning.EnvironmentLayer, budget, this);
                ProjectileLaunched?.Invoke(projectile);
            }
        }
        public void CancelOwnedProjectiles()
        {
            cancellationRevision++;
            foreach (var projectile in registry.ToArray())
                if (projectile != null && projectile.Owner == this) projectile.CancelOwnedLaunch(this);
        }
        private void OnDefeated(EnemyDamageEvent _) => CancelOwnedProjectiles();
        private void OnDisable() => CancelOwnedProjectiles();
        private void OnDestroy()
        {
            Unsubscribe();
            foreach (var projectile in registry)
                if (projectile != null && projectile.Owner == this) Destroy(projectile.gameObject);
        }
        private void Unsubscribe()
        {
            if (subscribedActor != null) subscribedActor.Defeated -= OnDefeated;
            if (subscribedBrain != null) subscribedBrain.OffenseCancelled -= CancelOwnedProjectiles;
            subscribedActor = null; subscribedBrain = null;
        }
    }
}
