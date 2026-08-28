using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(EnemyActor))]
    public sealed class EnemyContactAttack2D : MonoBehaviour
    {
        [Header(header: "Actor")]
        [Tooltip(tooltip: "Enemy lifecycle gate for contact attacks. Falls back to this GameObject when empty.")]
        [SerializeField] private EnemyActor actor;

        [Header(header: "Damage")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Damage applied to the player on each accepted contact attack.")]
        [SerializeField] private float damageAmount = 1f;

        [Tooltip(tooltip: "Collider bounds used for proactive overlap contact checks. Falls back to this enemy's first Collider2D.")]
        [SerializeField] private Collider2D attackCollider;

        [Tooltip(tooltip: "Layers queried by proactive overlap contact checks.")]
        [SerializeField] private LayerMask targetLayers = ~0;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Minimum scaled seconds between accepted contact attacks.")]
        [SerializeField] private float attackCooldown = 0.75f;

        [Header(header: "Presentation")]
        [Tooltip(tooltip: "SpriteRenderer flashed when this enemy attacks. Falls back to a child renderer when empty.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip(tooltip: "Sprite color while the contact attack flash is active.")]
        [SerializeField] private Color attackFlashColor = Color.red;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Scaled seconds the sprite remains in the attack flash color.")]
        [SerializeField] private float attackFlashDuration = 0.15f;

        private float cooldownRemaining;
        private float flashRemaining;
        private Color restingColor = Color.white;
        private bool hasRestingColor;

        public float CooldownRemaining => cooldownRemaining;
        public bool IsFlashing => flashRemaining > 0f;

        private void Awake()
        {
            ResolveDependencies();
            CacheRestingColor();
        }

        private void Update()
        {
            Tick(deltaTime: Time.deltaTime);
        }

        private void FixedUpdate()
        {
            TryAttackOverlaps();
        }

        private void OnDisable()
        {
            RestoreSpriteColor();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryAttack(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryAttack(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryAttack(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryAttack(other.gameObject);
        }

        public void Configure(
            EnemyActor enemyActor,
            SpriteRenderer renderer)
        {
            actor = enemyActor;
            spriteRenderer = renderer;
            CacheRestingColor();
        }

        public void ConfigureDamage(
            float amount,
            float cooldown,
            float flashDuration)
        {
            damageAmount = Mathf.Max(a: 0f, b: amount);
            attackCooldown = Mathf.Max(a: 0f, b: cooldown);
            attackFlashDuration = Mathf.Max(a: 0f, b: flashDuration);
        }

        public void ConfigureOverlap(
            Collider2D collider,
            LayerMask layers)
        {
            attackCollider = collider;
            targetLayers = layers;
        }

        public void Tick(float deltaTime)
        {
            var clampedDeltaTime = Mathf.Max(a: 0f, b: deltaTime);
            cooldownRemaining = Mathf.Max(a: 0f, b: cooldownRemaining - clampedDeltaTime);
            if (flashRemaining <= 0f)
            {
                return;
            }

            flashRemaining = Mathf.Max(a: 0f, b: flashRemaining - clampedDeltaTime);
            if (flashRemaining <= 0f)
            {
                RestoreSpriteColor();
            }
        }

        public bool TryAttack(GameObject target)
        {
            ResolveDependencies();
            if (!CanAttack() || target == null)
            {
                return false;
            }

            var targetRoot = ResolveDamageTarget(target);
            if (!HasDamageableTarget(targetRoot))
            {
                return false;
            }

            var instance = new DamageInstance(
                instanceId: $"{name}-contact",
                sourceObject: gameObject,
                profile: null,
                formula: new DamageFormulaValues(
                    attack: 0f,
                    strikePercent: 0f,
                    strikeBonusPercent: 0f,
                    attackBuffPercent: 0f,
                    flatDamage: damageAmount,
                    finalDamagePercent: 0f,
                    critValue: 1f));
            var report = DamageResolver.Resolve(new DamageRequest(
                instance: instance,
                candidateTargets: new[] { targetRoot },
                hitPoint: transform.position,
                direction: ResolveAttackDirection(targetRoot)));

            if (report.EffectiveHitCount <= 0)
            {
                return false;
            }

            cooldownRemaining = attackCooldown;
            FlashAttack();
            return true;
        }

        public bool TryAttackOverlaps()
        {
            ResolveDependencies();
            if (!CanAttack() || attackCollider == null)
            {
                return false;
            }

            var bounds = attackCollider.bounds;
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

                if (TryAttack(overlap.gameObject))
                {
                    return true;
                }
            }

            return false;
        }

        private void ResolveDependencies()
        {
            if (actor == null)
            {
                actor = GetComponent<EnemyActor>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentsInChildren<SpriteRenderer>(includeInactive: true)
                    .FirstOrDefault();
                CacheRestingColor();
            }

            if (attackCollider == null)
            {
                attackCollider = GetComponent<Collider2D>()
                    ?? GetComponentInChildren<Collider2D>(includeInactive: true);
            }
        }

        private bool CanAttack()
        {
            return cooldownRemaining <= 0f
                && damageAmount > 0f
                && (actor == null || actor.IsOperational);
        }

        private static GameObject ResolveDamageTarget(GameObject target)
        {
            var body = target.GetComponentInParent<Rigidbody2D>();
            return body != null ? body.gameObject : target;
        }

        private static bool HasDamageableTarget(GameObject target)
        {
            return target != null
                && target.GetComponents<MonoBehaviour>().OfType<IDamageable>().Any();
        }

        private Vector2 ResolveAttackDirection(GameObject target)
        {
            if (target == null)
            {
                return Vector2.zero;
            }

            var direction = (Vector2)(target.transform.position - transform.position);
            return direction.sqrMagnitude > 0f ? direction.normalized : Vector2.zero;
        }

        private void CacheRestingColor()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            restingColor = spriteRenderer.color;
            hasRestingColor = true;
        }

        private void FlashAttack()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (!hasRestingColor)
            {
                CacheRestingColor();
            }

            spriteRenderer.color = attackFlashColor;
            flashRemaining = attackFlashDuration;
        }

        private void RestoreSpriteColor()
        {
            if (spriteRenderer == null || !hasRestingColor)
            {
                return;
            }

            spriteRenderer.color = restingColor;
            flashRemaining = 0f;
        }
    }
}
