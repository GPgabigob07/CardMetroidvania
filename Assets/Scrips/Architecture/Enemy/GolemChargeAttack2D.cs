using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(Rigidbody2D))]
    public sealed class GolemChargeAttack2D : MonoBehaviour
    {
        [Header(header: "Dependencies")]
        [Tooltip(tooltip: "Enemy lifecycle gate for this charge attack.")]
        [SerializeField] private EnemyActor actor;

        [Tooltip(tooltip: "Rigidbody2D moved during the active charge.")]
        [SerializeField] private Rigidbody2D body;

        [Tooltip(tooltip: "Trigger collider enabled only while the charge can damage targets.")]
        [SerializeField] private Collider2D chargeHitbox;

        [Header(header: "Movement")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Horizontal world speed applied while charging.")]
        [SerializeField] private float chargeSpeed = 8f;

        [Tooltip(tooltip: "Layers that stop an active charge after a collision.")]
        [SerializeField] private LayerMask blockingLayers = ~0;

        [Header(header: "Damage")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Damage applied once to each valid target during an active charge.")]
        [SerializeField] private float damageAmount = 1f;

        [Tooltip(tooltip: "Layers queried by the active charge hitbox.")]
        [SerializeField] private LayerMask targetLayers = ~0;

        private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>();

        private Vector3 authoredHitboxLocalPosition;
        private bool hasAuthoredHitboxPosition;

        public bool IsCharging { get; private set; }
        public bool WasBlocked { get; private set; }
        public Vector2 Direction { get; private set; } = Vector2.right;

        private void Awake()
        {
            ResolveDependencies();
            CacheAuthoredHitboxPosition();
            SetHitboxActive(false);
        }

        private void OnDisable()
        {
            StopCharge();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsCharging && collision != null && IsBlockingLayer(collision.gameObject.layer))
            {
                StopCharge(blocked: true);
            }
        }

        public void BeginCharge(Vector2 direction)
        {
            ResolveDependencies();
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            PositionHitboxForDirection();
            WasBlocked = false;
            hitTargets.Clear();
            IsCharging = body != null && (actor == null || actor.IsOperational);
            SetHitboxActive(IsCharging);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            ResolveDependencies();
            if (!IsCharging || body == null)
            {
                return;
            }

            body.linearVelocity = new Vector2(
                x: Direction.x * Mathf.Max(0f, chargeSpeed),
                y: body.linearVelocity.y);
            TryAttackOverlaps();
        }

        public void StopCharge(bool blocked = false)
        {
            WasBlocked |= blocked;
            IsCharging = false;
            SetHitboxActive(false);
            if (body != null)
            {
                body.linearVelocity = new Vector2(x: 0f, y: body.linearVelocity.y);
            }
        }

        public void ConfigureForTests(EnemyActor enemyActor, Rigidbody2D rigidbody)
        {
            actor = enemyActor;
            body = rigidbody;
        }

        public void ConfigureTuning(float speed, float damage)
        {
            chargeSpeed = Mathf.Max(0f, speed);
            damageAmount = Mathf.Max(0f, damage);
        }

        private void TryAttackOverlaps()
        {
            if (chargeHitbox == null || damageAmount <= 0f)
            {
                return;
            }

            var bounds = chargeHitbox.bounds;
            foreach (var overlap in Physics2D.OverlapBoxAll(
                         point: bounds.center,
                         size: bounds.size,
                         angle: 0f,
                         layerMask: targetLayers))
            {
                if (overlap == null || overlap.transform.IsChildOf(transform))
                {
                    continue;
                }

                var target = ResolveDamageTarget(overlap.gameObject);
                if (target == null || !hitTargets.Add(target))
                {
                    continue;
                }

                var report = DamageResolver.Resolve(new DamageRequest(
                    instance: new DamageInstance(
                        instanceId: $"{name}-charge",
                        sourceObject: gameObject,
                        profile: null,
                        formula: new DamageFormulaValues(
                            attack: 0f,
                            strikePercent: 0f,
                            strikeBonusPercent: 0f,
                            attackBuffPercent: 0f,
                            flatDamage: damageAmount,
                            finalDamagePercent: 0f,
                            critValue: 1f)),
                    candidateTargets: new[] { target },
                    hitPoint: bounds.center,
                    direction: Direction));
                if (report.EffectiveHitCount <= 0)
                {
                    hitTargets.Remove(target);
                }
            }
        }

        private static GameObject ResolveDamageTarget(GameObject target)
        {
            var rigidbody = target.GetComponentInParent<Rigidbody2D>();
            return rigidbody != null ? rigidbody.gameObject : target;
        }

        private bool IsBlockingLayer(int layer)
        {
            return (blockingLayers.value & (1 << layer)) != 0;
        }

        private void SetHitboxActive(bool active)
        {
            if (chargeHitbox != null)
            {
                chargeHitbox.enabled = active;
            }
        }

        private void CacheAuthoredHitboxPosition()
        {
            if (chargeHitbox == null || chargeHitbox.transform == transform)
            {
                return;
            }

            authoredHitboxLocalPosition = chargeHitbox.transform.localPosition;
            hasAuthoredHitboxPosition = true;
        }

        private void PositionHitboxForDirection()
        {
            if (!hasAuthoredHitboxPosition)
            {
                CacheAuthoredHitboxPosition();
            }

            if (!hasAuthoredHitboxPosition)
            {
                return;
            }

            chargeHitbox.transform.localPosition = new Vector3(
                x: Mathf.Abs(authoredHitboxLocalPosition.x) * (Direction.x >= 0f ? 1f : -1f),
                y: authoredHitboxLocalPosition.y,
                z: authoredHitboxLocalPosition.z);
        }

        private void ResolveDependencies()
        {
            if (actor == null)
            {
                actor = GetComponent<EnemyActor>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (chargeHitbox == null)
            {
                chargeHitbox = GetComponentsInChildren<Collider2D>(includeInactive: true)
                    .FirstOrDefault(collider => collider != GetComponent<Collider2D>());
                CacheAuthoredHitboxPosition();
            }
        }
    }
}
