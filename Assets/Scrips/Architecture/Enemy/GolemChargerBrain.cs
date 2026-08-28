using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(EnemyActor))]
    [RequireComponent(requiredComponent: typeof(GolemChargeAttack2D))]
    [RequireComponent(requiredComponent: typeof(Rigidbody2D))]
    public sealed class GolemChargerBrain : MonoBehaviour
    {
        [Header(header: "Dependencies")]
        [Tooltip(tooltip: "Enemy lifecycle observed by this combat brain.")]
        [SerializeField] private EnemyActor actor;

        [Tooltip(tooltip: "Focused movement and damage collaborator used during Charge.")]
        [SerializeField] private GolemChargeAttack2D chargeAttack;

        [Tooltip(tooltip: "Rigidbody2D moved during the slow patrol phase.")]
        [SerializeField] private Rigidbody2D body;

        [Header(header: "Targeting")]
        [Tooltip(tooltip: "Optional authored target. When empty, the brain finds the nearest collider on target layers.")]
        [SerializeField] private Transform target;

        [Tooltip(tooltip: "Layers considered valid player targets by automatic detection.")]
        [SerializeField] private LayerMask targetLayers = ~0;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Maximum distance at which the golem begins its attack windup.")]
        [SerializeField] private float detectionRange = 4f;

        [Header(header: "Patrol")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Horizontal world speed applied while no target is in range.")]
        [SerializeField] private float patrolSpeed = 0.75f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Maximum horizontal distance traveled on either side of the patrol start point.")]
        [SerializeField] private float patrolHalfDistance = 2f;

        [Header(header: "State Timing")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Scaled gameplay seconds spent visibly preparing a charge.")]
        [SerializeField] private float windupSeconds = 0.75f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Scaled gameplay seconds an uninterrupted charge can remain active.")]
        [SerializeField] private float chargeSeconds = 0.45f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Scaled gameplay seconds the golem remains open after an interrupt.")]
        [SerializeField] private float interruptedSeconds = 1.25f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Scaled gameplay seconds before returning to idle after a charge or interruption.")]
        [SerializeField] private float recoverySeconds = 0.35f;

        private readonly StateMachine<GolemChargerState> stateMachine = new StateMachine<GolemChargerState>();

        private float stateTimeRemaining;
        private Vector2 patrolAnchor;
        private float patrolDirection = -1f;
        private bool statesRegistered;
        private bool subscribed;

        public GolemChargerState CurrentState => stateMachine.CurrentStateId;
        public Vector2 ChargeDirection { get; private set; } = Vector2.right;
        public bool IsInterruptedWindowActive => CurrentState == GolemChargerState.Interrupted;
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            ResolveDependencies();
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            FixedTick(Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            UnsubscribeFromActor();
        }

        public void Initialize()
        {
            ResolveDependencies();
            if (!ValidateDependencies())
            {
                IsInitialized = false;
                return;
            }

            SubscribeToActor();
            RegisterStates();
            stateTimeRemaining = 0f;
            patrolAnchor = body.position;
            patrolDirection = -1f;
            IsInitialized = true;
            stateMachine.TryChangeState(
                actor.IsDefeated ? GolemChargerState.Dead : GolemChargerState.Idle,
                restart: true);
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized)
            {
                return;
            }

            stateMachine.Tick(Mathf.Max(0f, deltaTime));
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (!IsInitialized)
            {
                return;
            }

            stateMachine.FixedTick(Mathf.Max(0f, fixedDeltaTime));
        }

        public bool TryInterrupt(in DamageContext context)
        {
            if (!IsInitialized
                || (CurrentState != GolemChargerState.Windup && CurrentState != GolemChargerState.Charge))
            {
                return false;
            }

            stateMachine.TryChangeState(GolemChargerState.Interrupted);
            return true;
        }

        public void SetDependenciesForTests(EnemyActor enemyActor, GolemChargeAttack2D attack)
        {
            UnsubscribeFromActor();
            actor = enemyActor;
            chargeAttack = attack;
            IsInitialized = false;
        }

        public void SetTargetForTests(Transform value)
        {
            target = value;
        }

        public void ConfigureTiming(
            float range,
            float windup,
            float charge,
            float interrupted,
            float recovery)
        {
            detectionRange = Mathf.Max(0f, range);
            windupSeconds = Mathf.Max(0f, windup);
            chargeSeconds = Mathf.Max(0f, charge);
            interruptedSeconds = Mathf.Max(0f, interrupted);
            recoverySeconds = Mathf.Max(0f, recovery);
        }

        private void RegisterStates()
        {
            if (statesRegistered)
            {
                return;
            }

            stateMachine.AddState(new IdleState(), this);
            stateMachine.AddState(new PatrolState(), this);
            stateMachine.AddState(new WindupState(), this);
            stateMachine.AddState(new ChargeState(), this);
            stateMachine.AddState(new InterruptedState(), this);
            stateMachine.AddState(new RecoveryState(), this);
            stateMachine.AddState(new DeadState(), this);
            statesRegistered = true;
        }

        private void EnterIdle()
        {
            stateTimeRemaining = 0f;
            chargeAttack.StopCharge();
        }

        private void TickIdle()
        {
            if (TryGetTarget(out var resolvedTarget))
            {
                LockChargeDirection(resolvedTarget.position);
                stateMachine.TryChangeState(GolemChargerState.Windup);
                return;
            }

            stateMachine.TryChangeState(GolemChargerState.Patrol);
        }

        private void EnterPatrol()
        {
            stateTimeRemaining = 0f;
            chargeAttack.StopCharge();
            patrolAnchor = body.position;
            patrolDirection = ChargeDirection.x >= 0f ? -1f : 1f;
        }

        private void TickPatrol()
        {
            if (TryGetTarget(out var resolvedTarget))
            {
                LockChargeDirection(resolvedTarget.position);
                stateMachine.TryChangeState(GolemChargerState.Windup);
                return;
            }

            var offset = body.position.x - patrolAnchor.x;
            if ((patrolDirection < 0f && offset <= -patrolHalfDistance)
                || (patrolDirection > 0f && offset >= patrolHalfDistance))
            {
                patrolDirection *= -1f;
            }
        }

        private void FixedTickPatrol()
        {
            body.linearVelocity = new Vector2(
                x: patrolDirection * patrolSpeed,
                y: body.linearVelocity.y);
        }

        private void EnterWindup()
        {
            stateTimeRemaining = windupSeconds;
            chargeAttack.StopCharge();
            if (TryGetTarget(out var resolvedTarget))
            {
                LockChargeDirection(resolvedTarget.position);
            }
        }

        private void TickWindup(float deltaTime)
        {
            if (TickStateTimer(deltaTime))
            {
                stateMachine.TryChangeState(GolemChargerState.Charge);
            }
        }

        private void EnterCharge()
        {
            stateTimeRemaining = chargeSeconds;
            chargeAttack.BeginCharge(ChargeDirection);
        }

        private void TickCharge(float deltaTime)
        {
            if (!chargeAttack.IsCharging || TickStateTimer(deltaTime))
            {
                chargeAttack.StopCharge();
                stateMachine.TryChangeState(GolemChargerState.Recovery);
            }
        }

        private void FixedTickCharge(float fixedDeltaTime)
        {
            chargeAttack.FixedTick(fixedDeltaTime);
        }

        private void EnterInterrupted()
        {
            stateTimeRemaining = interruptedSeconds;
            chargeAttack.StopCharge();
        }

        private void TickInterrupted(float deltaTime)
        {
            if (TickStateTimer(deltaTime))
            {
                stateMachine.TryChangeState(GolemChargerState.Recovery);
            }
        }

        private void EnterRecovery()
        {
            stateTimeRemaining = recoverySeconds;
            chargeAttack.StopCharge();
        }

        private void TickRecovery(float deltaTime)
        {
            if (TickStateTimer(deltaTime))
            {
                stateMachine.TryChangeState(GolemChargerState.Idle);
            }
        }

        private void EnterDead()
        {
            stateTimeRemaining = 0f;
            chargeAttack.StopCharge();
        }

        private bool TickStateTimer(float deltaTime)
        {
            stateTimeRemaining = Mathf.Max(0f, stateTimeRemaining - deltaTime);
            return stateTimeRemaining <= 0f;
        }

        private bool TryGetTarget(out Transform resolvedTarget)
        {
            if (target != null && target.gameObject.activeInHierarchy
                && Vector2.Distance(transform.position, target.position) <= detectionRange)
            {
                resolvedTarget = target;
                return true;
            }

            var closestDistance = float.MaxValue;
            resolvedTarget = null;
            foreach (var collider in Physics2D.OverlapCircleAll(transform.position, detectionRange, targetLayers))
            {
                if (collider == null || collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                var distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = distance;
                resolvedTarget = collider.transform;
            }

            return resolvedTarget != null;
        }

        private void LockChargeDirection(Vector2 targetPosition)
        {
            var horizontalDelta = targetPosition.x - transform.position.x;
            if (!Mathf.Approximately(horizontalDelta, 0f))
            {
                ChargeDirection = horizontalDelta > 0f ? Vector2.right : Vector2.left;
            }
        }

        private void ResolveDependencies()
        {
            if (actor == null)
            {
                actor = GetComponent<EnemyActor>();
            }

            if (chargeAttack == null)
            {
                chargeAttack = GetComponent<GolemChargeAttack2D>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        private bool ValidateDependencies()
        {
            if (actor == null || !actor.IsInitialized)
            {
                Debug.LogError(
                    $"{nameof(GolemChargerBrain)} on '{name}' requires an initialized {nameof(EnemyActor)}.",
                    this);
                return false;
            }

            if (chargeAttack == null || body == null)
            {
                Debug.LogError(
                    $"{nameof(GolemChargerBrain)} on '{name}' requires a {nameof(GolemChargeAttack2D)} and {nameof(Rigidbody2D)}.",
                    this);
                return false;
            }

            return true;
        }

        private void SubscribeToActor()
        {
            UnsubscribeFromActor();
            actor.Defeated += OnActorDefeated;
            actor.Restored += OnActorRestored;
            subscribed = true;
        }

        private void UnsubscribeFromActor()
        {
            if (!subscribed || actor == null)
            {
                return;
            }

            actor.Defeated -= OnActorDefeated;
            actor.Restored -= OnActorRestored;
            subscribed = false;
        }

        private void OnActorDefeated(EnemyDamageEvent payload)
        {
            if (IsInitialized && CurrentState != GolemChargerState.Dead)
            {
                stateMachine.TryChangeState(GolemChargerState.Dead);
            }
        }

        private void OnActorRestored(EnemyHealthChanged payload)
        {
            if (IsInitialized && actor.IsOperational && CurrentState == GolemChargerState.Dead)
            {
                stateMachine.TryChangeState(GolemChargerState.Idle);
            }
        }

        private sealed class IdleState : OwnedState<GolemChargerState, GolemChargerBrain>
        {
            public override GolemChargerState Id => GolemChargerState.Idle;

            protected override void OnEnter()
            {
                Owner.EnterPatrol();
            }

            public override void Tick(float deltaTime)
            {
                Owner.TickPatrol();
            }

            public override void FixedTick(float fixedDeltaTime)
            {
                Owner.FixedTickPatrol();
            }
        }

        private sealed class PatrolState : OwnedState<GolemChargerState, GolemChargerBrain>
        {
            public override GolemChargerState Id => GolemChargerState.Patrol;

            protected override void OnEnter()
            {
                Owner.EnterIdle();
            }

            public override void Tick(float deltaTime)
            {
                Owner.TickIdle();
            }
        }

        private sealed class WindupState : OwnedState<GolemChargerState, GolemChargerBrain>
        {
            public override GolemChargerState Id => GolemChargerState.Windup;

            protected override void OnEnter()
            {
                Owner.EnterWindup();
            }

            public override void Tick(float deltaTime)
            {
                Owner.TickWindup(deltaTime);
            }
        }

        private sealed class ChargeState : OwnedState<GolemChargerState, GolemChargerBrain>
        {
            public override GolemChargerState Id => GolemChargerState.Charge;

            protected override void OnEnter()
            {
                Owner.EnterCharge();
            }

            public override void Tick(float deltaTime)
            {
                Owner.TickCharge(deltaTime);
            }

            public override void FixedTick(float fixedDeltaTime)
            {
                Owner.FixedTickCharge(fixedDeltaTime);
            }
        }

        private sealed class InterruptedState : OwnedState<GolemChargerState, GolemChargerBrain>
        {
            public override GolemChargerState Id => GolemChargerState.Interrupted;

            protected override void OnEnter()
            {
                Owner.EnterInterrupted();
            }

            public override void Tick(float deltaTime)
            {
                Owner.TickInterrupted(deltaTime);
            }
        }

        private sealed class RecoveryState : OwnedState<GolemChargerState, GolemChargerBrain>
        {
            public override GolemChargerState Id => GolemChargerState.Recovery;

            protected override void OnEnter()
            {
                Owner.EnterRecovery();
            }

            public override void Tick(float deltaTime)
            {
                Owner.TickRecovery(deltaTime);
            }
        }

        private sealed class DeadState : OwnedState<GolemChargerState, GolemChargerBrain>
        {
            public override GolemChargerState Id => GolemChargerState.Dead;

            protected override void OnEnter()
            {
                Owner.EnterDead();
            }
        }
    }
}
