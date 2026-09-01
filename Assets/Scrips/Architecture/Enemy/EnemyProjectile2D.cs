using System;
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

        private string instanceId;
        private DamageFormulaValues healthFormula;
        private float lifetimeRemaining;

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
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !IsTargetLayer(other.gameObject.layer))
            {
                return;
            }

            TryResolveCollision(other.gameObject);
        }

        public void Launch(Vector2 direction, GameObject sourceObject, float speed)
        {
            ResolveDependencies();
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            SourceObject = sourceObject;
            Speed = Mathf.Max(0f, speed);
            PoiseDamage = 0f;
            ProcPolicy = DamageProcPolicy.None;
            instanceId = Guid.NewGuid().ToString(format: "N");
            Provenance = DamageProvenance.Primary(instanceId);
            healthFormula = CreateHealthFormula(damageProfile);
            lifetimeRemaining = Mathf.Max(0f, lifetimeSeconds);
            IsLaunched = true;
            gameObject.SetActive(true);
        }

        public void Deflect(GameObject playerSource)
        {
            if (!IsLaunched || playerSource == null)
            {
                return;
            }

            Direction *= -1f;
            SourceObject = playerSource;
            PoiseDamage = 10f;
            Provenance = DamageProvenance.Converted(
                parentInstanceId: instanceId,
                rootInstanceId: Provenance.RootInstanceId,
                effectId: DeflectEffectId);
            ProcPolicy = DamageProcPolicy.None;
        }

        public void FixedTick(float fixedDeltaTime)
        {
            ResolveDependencies();
            if (!IsLaunched)
            {
                return;
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
            if (!IsLaunched || target == null || target == SourceObject)
            {
                return false;
            }

            var report = DamageResolver.Resolve(new DamageRequest(
                instance: new DamageInstance(
                    instanceId: instanceId,
                    sourceObject: SourceObject,
                    profile: damageProfile,
                    formula: healthFormula,
                    provenance: Provenance,
                    procPolicy: ProcPolicy,
                    poiseDamage: PoiseDamage),
                candidateTargets: new[] { target },
                hitPoint: transform.position,
                direction: Direction));
            if (!report.TargetResults.Any(targetResult => targetResult.Result.Accepted))
            {
                return false;
            }

            DisableProjectile();
            return true;
        }

        public bool TryResolveCollision(GameObject collisionObject)
        {
            return TryResolveHit(ResolveDamageTarget(collisionObject));
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
