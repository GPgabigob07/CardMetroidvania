using System.Linq;
using UnityEngine;
namespace TicGame.Architecture
{
    public sealed class GargoyleAnimationPresenter : MonoBehaviour
    {
        [Header("Presentation Bindings")]
        [SerializeField] private GargoyleBrain brain;
        [SerializeField] private GargoylePresentationSO definition;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private LineRenderer attackCue;
        [SerializeField] private LineRenderer coreCue;
        [Header("Hit Feedback")]
        [Tooltip("Disable when the sprite ripple presenter owns hit feedback; state tints and cues remain active.")]
        [SerializeField] private bool hitFlashEnabled = true;
        private GargoylePresentationValues values;
        private bool valid;
        private EnemyActor actor;
        private float flashRemaining;
        public void Configure(GargoyleBrain brain, GargoylePresentationSO definition, SpriteRenderer sprite)
        {
            this.brain = brain; this.definition = definition; this.sprite = sprite;
        }
        private void OnEnable()
        {
            actor = GetComponent<EnemyActor>(); if (actor != null) actor.Damaged += OnHit;
        }
        private void OnDisable()
        {
            if (actor != null) actor.Damaged -= OnHit;
            if (attackCue != null) attackCue.enabled = false;
            if (coreCue != null) coreCue.enabled = false;
        }
        private void OnHit(EnemyDamageEvent hit) { if (hitFlashEnabled && hit.Result.Accepted) flashRemaining = values.FlashDuration; }
        public void SetHitFlashEnabled(bool enabled) { hitFlashEnabled = enabled; if (!enabled) flashRemaining = 0; }
        private void LateUpdate() => RefreshVisuals(Time.deltaTime);
        public void RefreshVisuals(float deltaTime)
        {
            if (definition != null && definition.TryReadPresentation(out var fresh)) { values = fresh; valid = true; }
            if (!valid || sprite == null) return;
            if (brain == null || brain.CanSimulateProjectiles) flashRemaining = Mathf.Max(0, flashRemaining - deltaTime);
            var attack = brain != null ? brain.CurrentAttack : default;
            var binding = values.AnimationBindings.FirstOrDefault(b => b.Phase == attack.Phase && b.StepId == attack.StepId)
                ?? values.AnimationBindings.FirstOrDefault(b => b.Phase == attack.Phase && string.IsNullOrEmpty(b.StepId));
            sprite.sprite = binding != null && binding.Frames.Count > 0
                ? binding.Frames[Mathf.FloorToInt(attack.Elapsed * values.FrameRate) % binding.Frames.Count] : values.IdleSprite;
            var state = brain != null ? brain.CurrentState : GargoyleState.Idle;
            var color = state == GargoyleState.Stunned || state == GargoyleState.Staggered || state == GargoyleState.Dead ? values.StunColor
                : brain != null && brain.IsFeinting ? values.FeintColor : attack.IsRunning && attack.Phase == EnemyAttackPhase.Windup ? values.WindupColor
                : attack.IsRunning && attack.Phase == EnemyAttackPhase.Active ? values.ActiveColor : values.NormalColor;
            sprite.color = hitFlashEnabled && flashRemaining > 0 ? values.HitColor : color;
            sprite.flipX = brain != null && brain.FacingDirection < 0;
            DrawCues(attack);
        }
        private void DrawCues(EnemyAttackExecutionSnapshot attack)
        {
            var visible = attack.IsRunning && (attack.Phase == EnemyAttackPhase.Windup || attack.Phase == EnemyAttackPhase.Active);
            if (attackCue != null)
            {
                attackCue.enabled = visible;
                if (visible)
                {
                    Style(attackCue, attack.Kind == EnemyAttackPayloadKind.Nova ? values.ReactorColor : sprite.color);
                    var origin = (Vector2)transform.position + attack.Step.Payload.Offset;
                    if (brain != null) origin.x = transform.position.x + attack.Step.Payload.Offset.x * brain.FacingDirection;
                    if (attack.Kind == EnemyAttackPayloadKind.Nova)
                    {
                        var segments = values.CircleSegments; attackCue.loop = true; attackCue.positionCount = segments;
                        for (var index = 0; index < segments; index++)
                        {
                            var angle = index * Mathf.PI * 2 / segments;
                            attackCue.SetPosition(index, (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * attack.Step.Payload.NovaRadius);
                        }
                    }
                    else if (attack.Kind == EnemyAttackPayloadKind.Melee)
                    {
                        var facing = brain != null ? brain.FacingDirection : attack.Aim.x < 0 ? -1 : 1;
                        Box(attackCue, origin, attack.Step.Payload.HitboxSize, attack.Step.Payload.HitboxAngle * facing);
                    }
                    else
                    {
                        attackCue.loop = false; attackCue.positionCount = 2; attackCue.SetPosition(0, origin);
                        if (attack.Kind == EnemyAttackPayloadKind.Beam && attack.Phase == EnemyAttackPhase.Active) attackCue.startWidth = attackCue.endWidth = attack.Step.Payload.BeamThickness;
                        var beam = GetComponent<EnemyBeamAttack2D>();
                        attackCue.SetPosition(1, attack.Kind == EnemyAttackPayloadKind.Beam && beam != null && beam.IsActive ? beam.EndPoint : origin + attack.Aim * attack.Step.Payload.BeamLength);
                    }
                }
            }
            if (coreCue != null)
            {
                var policy = GetComponent<GargoyleDamagePolicy>(); coreCue.enabled = policy != null && policy.CoreOpen;
                if (coreCue.enabled) { Style(coreCue, values.ReactorColor); Box(coreCue, (Vector2)transform.position + values.CoreRegion.Offset, values.CoreRegion.Size); }
            }
        }
        private void Style(LineRenderer line, Color color)
        {
            line.useWorldSpace = true; line.startColor = line.endColor = color; line.startWidth = line.endWidth = values.TelegraphLineWidth;
        }
        private static void Box(LineRenderer line, Vector2 center, Vector2 size, float angle = 0)
        {
            line.loop = true; line.positionCount = 4;
            var corners = new[] { new Vector2(-size.x, -size.y), new Vector2(-size.x, size.y), new Vector2(size.x, size.y), new Vector2(size.x, -size.y) };
            var rotation = Quaternion.Euler(0, 0, angle);
            for (var index = 0; index < corners.Length; index++) line.SetPosition(index, center + (Vector2)(rotation * (corners[index] * .5f)));
        }
    }
}
