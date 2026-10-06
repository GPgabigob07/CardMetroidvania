using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(Rigidbody2D))]
    public sealed class EnemyProjectile2D : MonoBehaviour
    {
        private const string DeflectEffectId = "card.deflect";

        [Header(header: "Dependencies")]
        [Tooltip(tooltip: "Rigidbody moved by this projectile. Falls back to the Rigidbody2D on this GameObject.")]
        [SerializeField] private Rigidbody2D body;

        [Header(header: "Lifetime")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Seconds the projectile remains active after launch.")]
        [SerializeField] private float lifetimeSeconds = 3f;

        [Header(header: "Damage")]
        [Tooltip(tooltip: "Damage profile used to build the projectile's health damage formula.")]
        [SerializeField] private DamageProfileSO damageProfile;

        [Tooltip(tooltip: "Layers that can receive this projectile's damage.")]
        [SerializeField] private LayerMask targetLayers = ~0;

        public Vector2 Direction { get; private set; } = Vector2.right;
        public GameObject SourceObject { get; private set; }
        public float Speed { get; private set; }
        public float PoiseDamage { get; private set; }
        public DamageProvenance Provenance { get; private set; }
        public DamageProcPolicy ProcPolicy { get; private set; }
        public bool IsLaunched { get; private set; }
        public EnemyProjectilePatternLauncher Owner { get; private set; }

        private string instanceId;
        private DamageFormulaValues healthFormula;
        private float lifetimeRemaining;
        private DamageProfileSO activeProfile;
        private EnemyCastHitBudget hitBudget;
        private LayerMask environmentLayers;
        private bool authoredLaunch;
        private bool resolvingHit;
        private bool warnedDamage;
        private readonly List<RaycastHit2D> terrainHits = new();

        private void Awake()
        {
            ResolveDependencies();
        }

        private void FixedUpdate()
        {
            FixedTick(Time.fixedDeltaTime);
        }

        private void OnDisable()
        {
            IsLaunched = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || (!authoredLaunch && !IsTargetLayer(other.gameObject.layer)))
            {
                return;
            }

            TryResolveCollision(other.gameObject);
        }

        public void Launch(Vector2 direction, GameObject sourceObject, float speed)
        {
            BeginLaunch(direction, sourceObject, speed, lifetimeSeconds, damageProfile, false);
            Owner = null; hitBudget = null;
        }

        public void Launch(Vector2 direction, GameObject sourceObject, float speed, float lifetime,
            DamageProfileSO profile, LayerMask environment, EnemyCastHitBudget budget, EnemyProjectilePatternLauncher owner)
        {
            if (!float.IsFinite(speed) || speed <= 0 || !float.IsFinite(lifetime) || lifetime <= 0 || profile == null)
                throw new ArgumentException("Authored projectile motion and damage-profile bindings must be valid.");
            Owner = owner; hitBudget = budget; environmentLayers = environment;
            BeginLaunch(direction, sourceObject, speed, lifetime, profile, true);
        }

        private void BeginLaunch(Vector2 direction, GameObject sourceObject, float speed, float lifetime, DamageProfileSO profile, bool authored)
        {
            ResolveDependencies();
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            SourceObject = sourceObject;
            Speed = Mathf.Max(0f, speed);
            PoiseDamage = 0f;
            ProcPolicy = DamageProcPolicy.None;
            instanceId = Guid.NewGuid().ToString(format: "N");
            Provenance = DamageProvenance.Primary(instanceId);
            authoredLaunch = authored; activeProfile = profile; warnedDamage = resolvingHit = false;
            healthFormula = CreateHealthFormula(profile);
            lifetimeRemaining = Mathf.Max(0f, lifetime);
            IsLaunched = true;
            // Enemy bodies ignore players/enemies globally; projectiles must reach their damage targets.
            foreach (var collider in GetComponentsInChildren<Collider2D>(includeInactive: true))
                collider.includeLayers |= targetLayers;
            gameObject.SetActive(true);
        }

        public void Deflect(GameObject playerSource)
        {
            if (!IsLaunched || playerSource == null)
            {
                return;
            }

            ConvertToPlayer(playerSource, -Direction);
        }

        public bool TryDeflect(GameObject playerSource, Vector2 direction)
        {
            if (!IsLaunched || playerSource == null || SourceObject == playerSource
                || Provenance.OriginKind == DamageOriginKind.Converted
                || !float.IsFinite(direction.x) || !float.IsFinite(direction.y) || direction.sqrMagnitude <= 0f)
                return false;
            ConvertToPlayer(playerSource, direction.normalized);
            return true;
        }

        private void ConvertToPlayer(GameObject playerSource, Vector2 direction)
        {
            Direction = direction;
            if (body != null) body.linearVelocity = Direction * Speed;
            SourceObject = playerSource;
            PoiseDamage = 10f;
            Provenance = DamageProvenance.Converted(
                parentInstanceId: instanceId,
                rootInstanceId: Provenance.RootInstanceId,
                effectId: DeflectEffectId);
            ProcPolicy = DamageProcPolicy.None;
            Owner = null; hitBudget = null;
        }

        public void FixedTick(float fixedDeltaTime)
        {
            ResolveDependencies();
            if (!IsLaunched)
            {
                return;
            }

            if (Time.timeScale <= 0 || (Owner != null && !Owner.CanSimulateProjectiles) || fixedDeltaTime <= 0)
            {
                if (body != null) body.linearVelocity = Vector2.zero;
                return;
            }
            if (authoredLaunch && body != null)
            {
                var filter = new ContactFilter2D { useTriggers = false };
                filter.SetLayerMask(environmentLayers);
                terrainHits.Clear();
                // Rigidbody casts ignore attached trigger shapes; sweep the projectile's shape explicitly.
                var circle = GetComponent<CircleCollider2D>();
                var hits = circle != null
                    ? Physics2D.CircleCast(circle.transform.TransformPoint(circle.offset),
                        circle.radius * Mathf.Max(Mathf.Abs(circle.transform.lossyScale.x), Mathf.Abs(circle.transform.lossyScale.y)),
                        Direction, filter, terrainHits, Speed * fixedDeltaTime)
                    : body.Cast(Direction, filter, terrainHits, Speed * fixedDeltaTime);
                if (hits > 0)
                { DisableProjectile(); return; }
            }

            if (body != null)
            {
                body.linearVelocity = Direction * Speed;
            }

            lifetimeRemaining -= Mathf.Max(0f, fixedDeltaTime);
            if (lifetimeRemaining <= 0f)
            {
                DisableProjectile();
            }
        }

        public bool TryResolveHit(GameObject target)
        {
            if (!IsLaunched || resolvingHit || Time.timeScale <= 0 || target == null || target == SourceObject
                || (Owner != null && !Owner.CanSimulateProjectiles))
            {
                return false;
            }

            if (SourceObject != null && target.transform.IsChildOf(SourceObject.transform)) return false;
            if (Provenance.OriginKind != DamageOriginKind.Converted && target.GetComponentInParent<EnemyActor>() != null)
                return false;

            if (authoredLaunch && Provenance.OriginKind != DamageOriginKind.Converted)
            {
                var health = target.GetComponentInParent<SimpleHealth>();
                if (health == null || !EnemyPlayerTargeting.IsAvailable(health.gameObject)) return false;
                target = health.gameObject;
            }
            if (hitBudget != null && !hitBudget.TryReserve(target)) { DisableProjectile(); return false; }
            if (authoredLaunch)
            {
                if (activeProfile != null && float.IsFinite(activeProfile.BaseDamage) && activeProfile.BaseDamage >= 0)
                { healthFormula = CreateHealthFormula(activeProfile); warnedDamage = false; }
                else if (!warnedDamage)
                { warnedDamage = true; Debug.LogWarning("Invalid live enemy projectile damage profile; retaining last valid damage.", this); }
            }
            resolvingHit = true;

            var report = DamageResolver.Resolve(new DamageRequest(
                instance: new DamageInstance(
                    instanceId: instanceId,
                    sourceObject: SourceObject,
                    profile: activeProfile,
                    formula: healthFormula,
                    provenance: Provenance,
                    procPolicy: ProcPolicy,
                    poiseDamage: PoiseDamage),
                candidateTargets: new[] { target },
                hitPoint: transform.position,
                direction: Direction));
            resolvingHit = false;
            var accepted = report.TargetResults.Any(targetResult => targetResult.Result.Accepted);
            hitBudget?.Complete(target, accepted);
            if (!accepted)
            {
                return false;
            }

            DisableProjectile();
            return true;
        }

        public bool TryResolveCollision(GameObject collisionObject)
        {
            if (collisionObject != null && authoredLaunch && (environmentLayers.value & (1 << collisionObject.layer)) != 0)
            {
                if (Time.timeScale > 0 && (Owner == null || Owner.CanSimulateProjectiles)) DisableProjectile();
                return false;
            }
            return TryResolveHit(ResolveDamageTarget(collisionObject));
        }

        public void CancelOwnedLaunch(EnemyProjectilePatternLauncher owner)
        {
            if (Owner == owner) DisableProjectile();
        }

        private static DamageFormulaValues CreateHealthFormula(DamageProfileSO profile)
        {
            return new DamageFormulaValues(
                attack: 0f,
                strikePercent: 0f,
                strikeBonusPercent: 0f,
                attackBuffPercent: 0f,
                flatDamage: profile != null ? profile.BaseDamage : 0f,
                finalDamagePercent: 0f,
                critValue: 1f);
        }

        private void ResolveDependencies()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

        }

        private static GameObject ResolveDamageTarget(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            if (target.GetComponentInParent<EnemyActor>() != null)
            {
                var region = EnemyDamageRegionSelection.ResolveTargets(target.GetComponents<Collider2D>()).FirstOrDefault();
                return region.Recipient != null ? region.Recipient.gameObject : null;
            }

            if (target.GetComponents<MonoBehaviour>().Any(component => component is IDamageable))
            {
                return target;
            }

            var rigidbody = target.GetComponentInParent<Rigidbody2D>();
            return rigidbody != null ? rigidbody.gameObject : target;
        }

        private bool IsTargetLayer(int layer)
        {
            return (targetLayers.value & (1 << layer)) != 0;
        }

        private void DisableProjectile()
        {
            IsLaunched = false;
            gameObject.SetActive(false);
        }
    }
}
