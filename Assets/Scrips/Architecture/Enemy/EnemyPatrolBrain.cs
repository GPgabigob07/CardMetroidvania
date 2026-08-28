using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(EnemyActor))]
    public sealed class EnemyPatrolBrain : MonoBehaviour
    {
        [Header(header: "Actor")]
        [Tooltip(tooltip: "Enemy lifecycle observed by this brain. Falls back to this GameObject when empty.")]
        [SerializeField] private EnemyActor actor;

        [Header(header: "Motor")]
        [Tooltip(tooltip: "MonoBehaviour implementing IEnemyPatrolMotor2D.")]
        [SerializeField] private MonoBehaviour motorComponent;

        [Header(header: "Route")]
        [Tooltip(tooltip: "First patrol endpoint relative to the position captured during initialization.")]
        [SerializeField] private Vector2 firstPointOffset = new Vector2(x: -2f, y: 0f);

        [Tooltip(tooltip: "Second patrol endpoint relative to the position captured during initialization.")]
        [SerializeField] private Vector2 secondPointOffset = new Vector2(x: 2f, y: 0f);

        [Tooltip(tooltip: "Whether the first patrol movement targets the second endpoint.")]
        [SerializeField] private bool startTowardSecondPoint = true;

        [Header(header: "Movement")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Constant patrol speed in world units per second.")]
        [SerializeField] private float moveSpeed = 2f;

        [Min(min: 0.001f)]
        [Tooltip(tooltip: "Distance from an endpoint considered an arrival.")]
        [SerializeField] private float arrivalDistance = 0.05f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Scaled gameplay time spent waiting before reversing patrol direction.")]
        [SerializeField] private float turnDelay = 0.2f;

        private readonly StateMachine<EnemyAIState> stateMachine = new StateMachine<EnemyAIState>();

        private IEnemyPatrolMotor2D motor;
        private Vector2 routeOrigin;
        private Vector2 firstPoint;
        private Vector2 secondPoint;
        private bool targetingSecondPoint;
        private float turnRemaining;
        private bool statesRegistered;
        private bool subscribed;
        private float movementSuppressionRemaining;

        public EnemyAIState CurrentState => stateMachine.CurrentStateId;
        public Vector2 CurrentTarget => targetingSecondPoint ? secondPoint : firstPoint;
        public Vector2 FirstPoint => firstPoint;
        public Vector2 SecondPoint => secondPoint;
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
            Tick(deltaTime: Time.deltaTime);
        }

        private void FixedUpdate()
        {
            FixedTick(fixedDeltaTime: Time.fixedDeltaTime);
        }

        private void OnDisable()
        {
            if (IsInitialized)
            {
                motor.Stop();
            }
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

            routeOrigin = motor.Position;
            firstPoint = routeOrigin + firstPointOffset;
            secondPoint = routeOrigin + secondPointOffset;
            targetingSecondPoint = startTowardSecondPoint;
            turnRemaining = 0f;
            IsInitialized = true;

            stateMachine.TryChangeState(actor.IsDefeated
                ? EnemyAIState.Dead
                : EnemyAIState.Patrol);
        }

        public void Tick(float deltaTime)
        {
            movementSuppressionRemaining = Mathf.Max(
                a: 0f,
                b: movementSuppressionRemaining - Mathf.Max(a: 0f, b: deltaTime));
            if (IsInitialized && movementSuppressionRemaining <= 0f)
            {
                stateMachine.Tick(deltaTime: Mathf.Max(a: 0f, b: deltaTime));
            }
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (IsInitialized && movementSuppressionRemaining <= 0f)
            {
                stateMachine.FixedTick(fixedDeltaTime: Mathf.Max(a: 0f, b: fixedDeltaTime));
            }
        }

        public void ConfigureRoute(Vector2 firstOffset, Vector2 secondOffset)
        {
            firstPointOffset = firstOffset;
            secondPointOffset = secondOffset;
        }

        public void ConfigureMovement(float speed, float delay, float distance)
        {
            moveSpeed = Mathf.Max(a: 0f, b: speed);
            turnDelay = Mathf.Max(a: 0f, b: delay);
            arrivalDistance = Mathf.Max(a: 0.001f, b: distance);
        }

        public void SuppressMovement(float duration)
        {
            movementSuppressionRemaining = Mathf.Max(
                movementSuppressionRemaining,
                Mathf.Max(0f, duration));
        }

        public void SetDependenciesForTests(EnemyActor value, IEnemyPatrolMotor2D patrolMotor)
        {
            UnsubscribeFromActor();
            actor = value;
            motor = patrolMotor;
            motorComponent = patrolMotor as MonoBehaviour;
            IsInitialized = false;
        }

        private void RegisterStates()
        {
            if (statesRegistered)
            {
                return;
            }

            stateMachine.AddState(new PatrolState(this));
            stateMachine.AddState(new IdleState(this));
            stateMachine.AddState(new DeadState(this));
            statesRegistered = true;
        }

        private void ResolveDependencies()
        {
            if (actor == null)
            {
                actor = GetComponent<EnemyActor>();
            }

            if (motor != null)
            {
                return;
            }

            if (motorComponent != null)
            {
                motor = motorComponent as IEnemyPatrolMotor2D;
                return;
            }

            foreach (var component in GetComponents<MonoBehaviour>())
            {
                if (component is IEnemyPatrolMotor2D patrolMotor)
                {
                    motorComponent = component;
                    motor = patrolMotor;
                    return;
                }
            }
        }

        private bool ValidateDependencies()
        {
            if (actor == null)
            {
                Debug.LogError(
                    message: $"{nameof(EnemyPatrolBrain)} on '{name}' requires an {nameof(EnemyActor)}.",
                    context: this);
                return false;
            }

            if (!actor.IsInitialized)
            {
                Debug.LogError(
                    message: $"{nameof(EnemyPatrolBrain)} on '{name}' requires an initialized {nameof(EnemyActor)}.",
                    context: this);
                return false;
            }

            if (motor == null)
            {
                Debug.LogError(
                    message: $"{nameof(EnemyPatrolBrain)} on '{name}' requires a component implementing {nameof(IEnemyPatrolMotor2D)}.",
                    context: this);
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

        private void EnterPatrol()
        {
            turnRemaining = 0f;
            FaceCurrentTarget();
        }

        private void FixedTickPatrol(float fixedDeltaTime)
        {
            var result = motor.MoveTowards(
                target: CurrentTarget,
                speed: moveSpeed,
                arrivalDistance: arrivalDistance,
                fixedDeltaTime: fixedDeltaTime);
            if (result == EnemyPatrolMoveResult.Moving)
            {
                return;
            }

            motor.Stop();
            turnRemaining = turnDelay;
            stateMachine.TryChangeState(EnemyAIState.Idle);
        }

        private void EnterIdle()
        {
            motor.Stop();
        }

        private void TickIdle(float deltaTime)
        {
            turnRemaining = Mathf.Max(a: 0f, b: turnRemaining - deltaTime);
            if (turnRemaining > 0f)
            {
                return;
            }

            targetingSecondPoint = !targetingSecondPoint;
            FaceCurrentTarget();
            stateMachine.TryChangeState(EnemyAIState.Patrol);
        }

        private void EnterDead()
        {
            motor.Stop();
        }

        private void FaceCurrentTarget()
        {
            var horizontalDelta = CurrentTarget.x - motor.Position.x;
            if (!Mathf.Approximately(a: horizontalDelta, b: 0f))
            {
                motor.SetFacing(horizontalDelta > 0f ? 1 : -1);
            }
        }

        private void OnActorDefeated(EnemyDamageEvent payload)
        {
            if (IsInitialized && CurrentState != EnemyAIState.Dead)
            {
                stateMachine.TryChangeState(EnemyAIState.Dead);
            }
        }

        private void OnActorRestored(EnemyHealthChanged payload)
        {
            if (!IsInitialized
                || CurrentState != EnemyAIState.Dead
                || !actor.IsOperational)
            {
                return;
            }

            SelectRecoveryTarget();
            stateMachine.TryChangeState(EnemyAIState.Patrol);
        }

        private void SelectRecoveryTarget()
        {
            var position = motor.Position;
            var distanceToFirst = Vector2.Distance(a: position, b: firstPoint);
            var distanceToSecond = Vector2.Distance(a: position, b: secondPoint);

            if (distanceToFirst <= arrivalDistance)
            {
                targetingSecondPoint = true;
            }
            else if (distanceToSecond <= arrivalDistance)
            {
                targetingSecondPoint = false;
            }
            else
            {
                targetingSecondPoint = distanceToSecond <= distanceToFirst;
            }
        }

        private void OnDrawGizmosSelected()
        {
            var previewOrigin = Application.isPlaying && IsInitialized
                ? routeOrigin
                : (Vector2)transform.position;
            var previewFirst = Application.isPlaying && IsInitialized
                ? firstPoint
                : previewOrigin + firstPointOffset;
            var previewSecond = Application.isPlaying && IsInitialized
                ? secondPoint
                : previewOrigin + secondPointOffset;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(from: previewFirst, to: previewSecond);
            Gizmos.DrawWireSphere(center: previewFirst, radius: 0.1f);
            Gizmos.DrawWireSphere(center: previewSecond, radius: 0.1f);

            if (Application.isPlaying && IsInitialized)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(center: CurrentTarget, radius: 0.14f);
            }
        }

        private sealed class PatrolState : IState<EnemyAIState>
        {
            private readonly EnemyPatrolBrain owner;

            public PatrolState(EnemyPatrolBrain owner)
            {
                this.owner = owner;
            }

            public EnemyAIState Id => EnemyAIState.Patrol;

            public void Enter()
            {
                owner.EnterPatrol();
            }

            public void Tick(float deltaTime)
            {
            }

            public void FixedTick(float fixedDeltaTime)
            {
                owner.FixedTickPatrol(fixedDeltaTime);
            }

            public void Exit()
            {
            }
        }

        private sealed class IdleState : IState<EnemyAIState>
        {
            private readonly EnemyPatrolBrain owner;

            public IdleState(EnemyPatrolBrain owner)
            {
                this.owner = owner;
            }

            public EnemyAIState Id => EnemyAIState.Idle;

            public void Enter()
            {
                owner.EnterIdle();
            }

            public void Tick(float deltaTime)
            {
                owner.TickIdle(deltaTime);
            }

            public void FixedTick(float fixedDeltaTime)
            {
            }

            public void Exit()
            {
            }
        }

        private sealed class DeadState : IState<EnemyAIState>
        {
            private readonly EnemyPatrolBrain owner;

            public DeadState(EnemyPatrolBrain owner)
            {
                this.owner = owner;
            }

            public EnemyAIState Id => EnemyAIState.Dead;

            public void Enter()
            {
                owner.EnterDead();
            }

            public void Tick(float deltaTime)
            {
            }

            public void FixedTick(float fixedDeltaTime)
            {
            }

            public void Exit()
            {
            }
        }
    }
}
