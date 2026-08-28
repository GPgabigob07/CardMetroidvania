using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(Animator))]
    [RequireComponent(requiredComponent: typeof(GolemChargerBrain))]
    public sealed class GolemChargerAnimationPresenter : MonoBehaviour
    {
        private static readonly int StateId = Animator.StringToHash("State");

        [Header(header: "Dependencies")]
        [Tooltip(tooltip: "Golem state source translated into Animator states.")]
        [SerializeField] private GolemChargerBrain brain;

        [Tooltip(tooltip: "Animator that renders the configured Golem Charger clips.")]
        [SerializeField] private Animator animator;

        [Tooltip(tooltip: "Physics body observed while the golem patrols.")]
        [SerializeField] private Rigidbody2D body;

        [Tooltip(tooltip: "Only this visual renderer is mirrored; physics and hitboxes remain unchanged.")]
        [SerializeField] private SpriteRenderer visualRenderer;

        private int lastStateValue = int.MinValue;
        private int facingDirection = 1;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Update()
        {
            if (brain == null || animator == null)
            {
                return;
            }

            var stateValue = ResolveAnimatorState(brain.CurrentState);
            if (stateValue != lastStateValue)
            {
                animator.SetInteger(StateId, stateValue);
                lastStateValue = stateValue;
            }

            ApplyFacing();
        }

        public void Configure(
            GolemChargerBrain golemBrain,
            Animator targetAnimator,
            Rigidbody2D targetBody = null,
            SpriteRenderer targetVisualRenderer = null)
        {
            brain = golemBrain;
            animator = targetAnimator;
            body = targetBody;
            visualRenderer = targetVisualRenderer;
            lastStateValue = int.MinValue;
        }

        private void ApplyFacing()
        {
            if (visualRenderer == null)
            {
                return;
            }

            var horizontalDirection = ResolveHorizontalDirection();
            if (!Mathf.Approximately(horizontalDirection, 0f))
            {
                facingDirection = horizontalDirection > 0f ? 1 : -1;
            }

            visualRenderer.flipX = facingDirection < 0;
        }

        private float ResolveHorizontalDirection()
        {
            return brain.CurrentState switch
            {
                GolemChargerState.Windup => brain.ChargeDirection.x,
                GolemChargerState.Charge => brain.ChargeDirection.x,
                GolemChargerState.Idle => body != null ? body.linearVelocity.x : 0f,
                GolemChargerState.Patrol => body != null ? body.linearVelocity.x : 0f,
                _ => 0f
            };
        }

        private static int ResolveAnimatorState(GolemChargerState state)
        {
            return state switch
            {
                GolemChargerState.Windup => (int)GolemChargerState.Windup,
                GolemChargerState.Charge => (int)GolemChargerState.Charge,
                GolemChargerState.Dead => (int)GolemChargerState.Dead,
                GolemChargerState.Interrupted => (int)GolemChargerState.Recovery,
                GolemChargerState.Recovery => (int)GolemChargerState.Recovery,
                _ => (int)GolemChargerState.Patrol
            };
        }

        private void ResolveDependencies()
        {
            if (brain == null)
            {
                brain = GetComponent<GolemChargerBrain>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (visualRenderer == null)
            {
                visualRenderer = GetComponentsInChildren<SpriteRenderer>(includeInactive: true)
                    .FirstOrDefault(renderer => renderer.transform != transform);
            }
        }
    }
}
