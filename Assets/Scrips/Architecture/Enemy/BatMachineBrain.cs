using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(EnemyActor))]
    [RequireComponent(requiredComponent: typeof(EnemyPoise))]
    [RequireComponent(requiredComponent: typeof(Rigidbody2D))]
    [RequireComponent(requiredComponent: typeof(AerialSteeringMotor2D))]
    public sealed class BatMachineBrain : MonoBehaviour
    {
        private const float TimerEpsilon = 0.0001f;

        [Header(header: "Dependencies")]
        [Tooltip(tooltip: "Enemy lifecycle observed by this combat brain.")]
        [SerializeField] private EnemyActor actor;

        [Tooltip(tooltip: "Health source used to select health-threshold fire behavior.")]
        [SerializeField] private EnemyHealth health;

        [Tooltip(tooltip: "Poise capability observed for stun and burst thresholds.")]
        [SerializeField] private EnemyPoise poise;

        [Tooltip(tooltip: "Rigidbody used for velocity facts and physical fall state.")]
        [SerializeField] private Rigidbody2D body;

        [Tooltip(tooltip: "Aerial motor that owns flight steering and gravity changes.")]
        [SerializeField] private AerialSteeringMotor2D motor;

        [Tooltip(tooltip: "Threat facts provider used for monitor and dodge decisions.")]
        [SerializeField] private BatThreatMonitor threatMonitor;

        [Tooltip(tooltip: "Projectile burst collaborator used after a committed windup.")]
        [SerializeField] private BatProjectileLauncher launcher;

        [Tooltip(tooltip: "Bat-specific damage path used for landing damage.")]
        [SerializeField] private BatMachineDamagePolicy damagePolicy;

        [Header(header: "Target")]
        [Tooltip(tooltip: "Player transform monitored and predicted by this bat.")]
        [SerializeField] private Transform target;

        [Tooltip(tooltip: "Optional target body used to measure velocity without estimating it.")]
        [SerializeField] private Rigidbody2D targetBody;

        [Header(header: "Random Patrol")]
        [Tooltip(tooltip: "Horizontal and vertical waypoint limits around the spawn anchor.")]
        [SerializeField] private Vector2 patrolHalfExtents = new Vector2(2f, 1f);

        [Min(min: 0f)]
        [Tooltip(tooltip: "Minimum distance between the bat and a newly selected patrol waypoint.")]
        [SerializeField] private float minimumPatrolWaypointDistance = 0.5f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Distance at which the current patrol waypoint counts as reached.")]
        [SerializeField] private float patrolArrivalDistance = 0.15f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Pause after reaching a waypoint before selecting the next one.")]
        [SerializeField] private float patrolWaitSeconds = 0.3f;

        [Tooltip(tooltip: "Deterministic seed used by this bat's patrol waypoint sequence.")]
        [SerializeField] private int patrolSeed = 1729;

        [Tooltip(tooltip: "Environment layers that invalidate patrol waypoints containing a collider.")]
        [SerializeField] private LayerMask patrolObstacleLayers;

        [Header(header: "Fire")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Cooldown used while health is above half.")]
        [SerializeField] private float normalFireCooldown = 4f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Cooldown used while health is at or below half.")]
        [SerializeField] private float shortFireCooldown = 2f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Visible windup duration before the firing solution locks.")]
        [SerializeField] private float fireWindupSeconds = 0.2f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Delay between shots in a low-poise burst.")]
        [SerializeField] private float interShotDelay = 0.1f;

        [Header(header: "Movement")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Maximum flight speed while following or evading.")]
        [SerializeField] private float flightSpeed = 5f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Velocity change per second while following or evading.")]
        [SerializeField] private float flightAcceleration = 12f;

        [Tooltip(tooltip: "Preferred offset from the monitored target while engaging.")]
        [SerializeField] private Vector2 engageOffset = new Vector2(-2f, 1f);

        [Min(min: 0f)]
        [Tooltip(tooltip: "Committed duration of one evade maneuver.")]
        [SerializeField] private float evadeSeconds = 0.25f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Cooldown between eligible evade evaluations.")]
        [SerializeField] private float evadeCooldownSeconds = 1f;

        [Header(header: "Stun Fall")]
        [Min(min: 0f)]
        [Tooltip(tooltip: "Downward speed tolerated before a terrain landing inflicts health damage.")]
        [SerializeField] private float safeImpactSpeed = 4f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Health damage applied for each downward speed unit above the safe threshold.")]
        [SerializeField] private float damagePerSpeedUnit = 1f;

        [Min(min: 0f)]
        [Tooltip(tooltip: "Grounded delay before the bat restores poise and resumes flight.")]
        [SerializeField] private float groundedRecoverySeconds = 0.5f;

        [Min(min: 0.0001f)]
        [Tooltip(tooltip: "Positive poise restored when grounded recovery completes.")]
        [SerializeField] private float postStunPoiseRestore = 12f;

        private readonly StateMachine<BatMachineState> stateMachine = new StateMachine<BatMachineState>();
        private readonly BatDodgeEvaluator dodgeEvaluator = new BatDodgeEvaluator();
        private readonly BatShotPredictor shotPredictor = new BatShotPredictor();

        private bool statesRegistered;
        private bool subscribed;
        private float stateTimeRemaining;
        private float fireCooldownRemaining;
        private float evadeCooldownRemaining;
        private float recordedDescendingSpeed;
        private float patrolWaitRemaining;
        private Vector2 patrolAnchor;
        private Vector2 currentPatrolWaypoint;
        private Vector2 evadeTarget;
        private Vector2 lockedFireDirection = Vector2.right;
        private System.Random patrolRandom;
        private bool hasPatrolWaypoint;
        private bool isWaitingAtPatrolWaypoint;

        public BatMachineState CurrentState => stateMachine.CurrentStateId;
        public bool IsInitialized { get; private set; }
        public float NormalFireCooldown => normalFireCooldown;
        public BatFirePlan CurrentFirePlan => BuildFirePlan(lockedFireDirection);
        public Vector2 CurrentPatrolWaypoint => currentPatrolWaypoint;

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
            Unsubscribe();
        }

        public void Initialize()
        {
            ResolveDependencies();
            if (!ValidateDependencies())
            {
                IsInitialized = false;
                return;
            }

            RegisterStates();
            Subscribe();
            shotPredictor.Configure(fireWindupSeconds, leadTime: 0.2f, maximumLeadDistance: 2f);
            fireCooldownRemaining = 0f;
            evadeCooldownRemaining = 0f;
            patrolAnchor = body.position;
            patrolRandom = new System.Random(patrolSeed);
            hasPatrolWaypoint = false;
            isWaitingAtPatrolWaypoint = false;
            patrolWaitRemaining = 0f;
            IsInitialized = true;
            stateMachine.TryChangeState(
                actor.IsDefeated ? BatMachineState.Dead : BatMachineState.PatrolRandom,
                restart: true);
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized)
            {
                return;
            }

            var clampedDeltaTime = Mathf.Max(0f, deltaTime);
            if (CurrentState != BatMachineState.Dead)
            {
                fireCooldownRemaining = Mathf.Max(0f, fireCooldownRemaining - clampedDeltaTime);
                evadeCooldownRemaining = Mathf.Max(0f, evadeCooldownRemaining - clampedDeltaTime);
                poise.Tick(clampedDeltaTime);
                launcher.Tick(clampedDeltaTime);
            }

            stateMachine.Tick(clampedDeltaTime);
        }

        public void FixedTick(float fixedDeltaTime)
        {
            if (IsInitialized)
            {
                stateMachine.FixedTick(Mathf.Max(0f, fixedDeltaTime));
            }
        }

        public void SetDependencies(
            EnemyActor enemyActor,
            EnemyHealth enemyHealth,
            EnemyPoise enemyPoise,
            Rigidbody2D rigidbody,
            AerialSteeringMotor2D steeringMotor,
            BatThreatMonitor monitor,
            BatProjectileLauncher projectileLauncher,
            BatMachineDamagePolicy policy)
        {
            Unsubscribe();
            actor = enemyActor;
            health = enemyHealth;
            poise = enemyPoise;
            body = rigidbody;
            motor = steeringMotor;
            threatMonitor = monitor;
            launcher = projectileLauncher;
            damagePolicy = policy;
            IsInitialized = false;
        }

        public void SetTarget(Transform value, Rigidbody2D valueBody)
        {
            target = value;
            targetBody = valueBody;
        }

        public void ConfigureFire(float normalCooldown, float shortCooldown, float windup, float interShotDelay)
        {
            normalFireCooldown = Mathf.Max(0f, normalCooldown);
            shortFireCooldown = Mathf.Max(0f, shortCooldown);
            fireWindupSeconds = Mathf.Max(0f, windup);
            this.interShotDelay = Mathf.Max(0f, interShotDelay);
            shotPredictor.Configure(fireWindupSeconds, leadTime: 0.2f, maximumLeadDistance: 2f);
        }

        public void ConfigureRecovery(
            float safeImpactSpeed,
            float damagePerSpeedUnit,
            float recoveryDuration,
            float restoredPoise)
        {
            this.safeImpactSpeed = Mathf.Max(0f, safeImpactSpeed);
            this.damagePerSpeedUnit = Mathf.Max(0f, damagePerSpeedUnit);
            groundedRecoverySeconds = Mathf.Max(0f, recoveryDuration);
            postStunPoiseRestore = Mathf.Max(0.0001f, restoredPoise);
        }

        public void ReportTerrainLanding(bool isTerrain)
        {
            if (!IsInitialized || CurrentState != BatMachineState.StunnedFall || !isTerrain)
            {
                return;
            }

            var fallDamage = Mathf.Max(0f, recordedDescendingSpeed - safeImpactSpeed) * damagePerSpeedUnit;
            damagePolicy.ApplyDamage(new DamageContext(
                source: gameObject,
                target: gameObject,
                profile: null,
                amount: fallDamage,
                hitPoint: body.position,
                direction: Vector2.down,
                poiseDamage: 0f));

            if (!health.IsDefeated)
            {
                stateMachine.TryChangeState(BatMachineState.GroundedRecovery);
            }
        }

        private void RegisterStates()
        {
            if (statesRegistered)
            {
                return;
            }

            stateMachine.AddState(new PatrolRandomState(), this);
            stateMachine.AddState(new EngageState(), this);
            stateMachine.AddState(new WindupFireState(), this);
            stateMachine.AddState(new EvadeState(), this);
            stateMachine.AddState(new StunnedFallState(), this);
            stateMachine.AddState(new GroundedRecoveryState(), this);
            stateMachine.AddState(new DeadState(), this);
            statesRegistered = true;
        }

        private void EnterPatrolRandom()
        {
            motor.ResumeFlight();
            if (!hasPatrolWaypoint)
            {
                SelectNextPatrolWaypoint();
            }
        }

        private void TickPatrolRandom(float deltaTime)
        {
            if (EvaluateThreat().IsMonitored)
            {
                stateMachine.TryChangeState(BatMachineState.Engage);
                return;
            }

            if (!isWaitingAtPatrolWaypoint)
            {
                return;
            }

            patrolWaitRemaining = Mathf.Max(0f, patrolWaitRemaining - deltaTime);
            if (patrolWaitRemaining <= TimerEpsilon)
            {
                isWaitingAtPatrolWaypoint = false;
                SelectNextPatrolWaypoint();
            }
        }

        private void FixedTickPatrolRandom(float fixedDeltaTime)
        {
            if (isWaitingAtPatrolWaypoint)
            {
                motor.Stop();
                return;
            }

            if (Vector2.Distance(body.position, currentPatrolWaypoint) <= patrolArrivalDistance)
            {
                motor.Stop();
                isWaitingAtPatrolWaypoint = true;
                patrolWaitRemaining = patrolWaitSeconds;
                return;
            }

            motor.MoveTowards(
                currentPatrolWaypoint,
                flightSpeed,
                flightAcceleration,
                fixedDeltaTime);
        }

        private void SelectNextPatrolWaypoint()
        {
            const int maximumAttempts = 16;
            patrolRandom ??= new System.Random(patrolSeed);
            var halfExtents = new Vector2(
                Mathf.Max(0f, patrolHalfExtents.x),
                Mathf.Max(0f, patrolHalfExtents.y));
            for (var attempt = 0; attempt < maximumAttempts; attempt++)
            {
                var candidate = patrolAnchor + new Vector2(
                    Mathf.Lerp(-halfExtents.x, halfExtents.x, (float)patrolRandom.NextDouble()),
                    Mathf.Lerp(-halfExtents.y, halfExtents.y, (float)patrolRandom.NextDouble()));
                if (Vector2.Distance(body.position, candidate) < minimumPatrolWaypointDistance)
                {
                    continue;
                }

                if (patrolObstacleLayers.value != 0
                    && Physics2D.OverlapPoint(candidate, patrolObstacleLayers) != null)
                {
                    continue;
                }

                currentPatrolWaypoint = candidate;
                hasPatrolWaypoint = true;
                return;
            }

            var fallbackDistance = Mathf.Min(halfExtents.x, Mathf.Max(minimumPatrolWaypointDistance, patrolArrivalDistance));
            currentPatrolWaypoint = patrolAnchor + Vector2.right * fallbackDistance;
            hasPatrolWaypoint = true;
        }

        private void TickEngage()
        {
            var facts = EvaluateThreat();
            if (!facts.IsMonitored)
            {
                stateMachine.TryChangeState(BatMachineState.PatrolRandom);
                return;
            }

            if (dodgeEvaluator.TryEvaluate(facts, poise.CurrentPoise, poise.MaximumPoise, evadeCooldownRemaining <= 0f))
            {
                stateMachine.TryChangeState(BatMachineState.Evade);
                return;
            }

            if (fireCooldownRemaining <= 0f)
            {
                stateMachine.TryChangeState(BatMachineState.WindupFire);
            }
        }

        private void FixedTickEngage(float fixedDeltaTime)
        {
            if (target != null)
            {
                motor.MoveTowards((Vector2)target.position + engageOffset, flightSpeed, flightAcceleration, fixedDeltaTime);
            }
        }

        private void EnterWindupFire()
        {
            stateTimeRemaining = fireWindupSeconds;
            motor.Stop();
            shotPredictor.BeginSample(target != null ? (Vector2)target.position : (Vector2)transform.position + Vector2.right);
        }

        private void TickWindupFire(float deltaTime)
        {
            if (target != null)
            {
                shotPredictor.Sample(target.position, deltaTime);
            }

            stateTimeRemaining = Mathf.Max(0f, stateTimeRemaining - deltaTime);
            if (stateTimeRemaining > 0f)
            {
                return;
            }

            var predictedPoint = shotPredictor.LockPrediction();
            var origin = launcher != null ? (Vector2)launcher.transform.position : (Vector2)transform.position;
            var direction = predictedPoint - origin;
            lockedFireDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            var plan = BuildFirePlan(lockedFireDirection);
            launcher.BeginBurst(plan);
            fireCooldownRemaining = plan.Cooldown;
            stateMachine.TryChangeState(BatMachineState.Engage);
        }

        private void EnterEvade()
        {
            stateTimeRemaining = evadeSeconds;
            evadeCooldownRemaining = evadeCooldownSeconds;
            var away = target != null ? (Vector2)transform.position - (Vector2)target.position : Vector2.left;
            evadeTarget = (Vector2)transform.position + (away.sqrMagnitude > 0f ? away.normalized : Vector2.left) * 2f;
        }

        private void TickEvade(float deltaTime)
        {
            stateTimeRemaining = Mathf.Max(0f, stateTimeRemaining - deltaTime);
            if (stateTimeRemaining <= 0f)
            {
                stateMachine.TryChangeState(BatMachineState.Engage);
            }
        }

        private void FixedTickEvade(float fixedDeltaTime)
        {
            motor.MoveTowards(evadeTarget, flightSpeed, flightAcceleration, fixedDeltaTime);
        }

        private void EnterStunnedFall()
        {
            launcher.CancelBurst();
            recordedDescendingSpeed = body != null ? Mathf.Max(0f, -body.linearVelocity.y) : 0f;
            motor.BeginFall();
        }

        private void EnterGroundedRecovery()
        {
            stateTimeRemaining = groundedRecoverySeconds;
        }

        private void TickGroundedRecovery(float deltaTime)
        {
            stateTimeRemaining = Mathf.Max(0f, stateTimeRemaining - deltaTime);
            if (stateTimeRemaining > 0f)
            {
                return;
            }

            poise.Restore(postStunPoiseRestore);
            motor.ResumeFlight();
            stateMachine.TryChangeState(
                EvaluateThreat().IsMonitored ? BatMachineState.Engage : BatMachineState.PatrolRandom);
        }

        private void EnterDead()
        {
            launcher.CancelBurst();
            motor.BeginFall();
        }

        private BatThreatFacts EvaluateThreat()
        {
            return threatMonitor.Evaluate(
                target,
                targetBody != null ? targetBody.linearVelocity : Vector2.zero,
                body != null ? body.linearVelocity : Vector2.zero);
        }

        private BatFirePlan BuildFirePlan(Vector2 direction)
        {
            var cooldown = health != null && health.NormalizedHealth <= 0.5f
                ? shortFireCooldown
                : normalFireCooldown;
            var projectileCount = poise != null && poise.NormalizedPoise <= 0.5f ? 2 : 1;
            return new BatFirePlan(cooldown, projectileCount, interShotDelay, direction);
        }

        private bool IsActiveAirborneState()
        {
            return CurrentState == BatMachineState.PatrolRandom
                || CurrentState == BatMachineState.Engage
                || CurrentState == BatMachineState.WindupFire
                || CurrentState == BatMachineState.Evade;
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            actor.Defeated += OnDefeated;
            actor.Restored += OnRestored;
            poise.Depleted += OnPoiseDepleted;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (actor != null)
            {
                actor.Defeated -= OnDefeated;
                actor.Restored -= OnRestored;
            }

            if (poise != null)
            {
                poise.Depleted -= OnPoiseDepleted;
            }

            subscribed = false;
        }

        private void OnDefeated(EnemyDamageEvent payload)
        {
            if (IsInitialized)
            {
                stateMachine.TryChangeState(BatMachineState.Dead);
            }
        }

        private void OnRestored(EnemyHealthChanged payload)
        {
            if (!IsInitialized || !actor.IsOperational)
            {
                return;
            }

            poise.RestoreToFull();
            motor.ResumeFlight();
            stateMachine.TryChangeState(BatMachineState.PatrolRandom);
        }

        private void OnPoiseDepleted()
        {
            if (IsInitialized && IsActiveAirborneState())
            {
                stateMachine.TryChangeState(BatMachineState.StunnedFall);
            }
        }

        private void ResolveDependencies()
        {
            if (actor == null)
            {
                actor = GetComponent<EnemyActor>();
            }

            if (health == null)
            {
                health = GetComponent<EnemyHealth>();
            }

            if (poise == null)
            {
                poise = GetComponent<EnemyPoise>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (motor == null)
            {
                motor = GetComponent<AerialSteeringMotor2D>();
            }

            if (threatMonitor == null)
            {
                threatMonitor = GetComponent<BatThreatMonitor>();
            }

            if (launcher == null)
            {
                launcher = GetComponent<BatProjectileLauncher>();
            }

            if (damagePolicy == null)
            {
                damagePolicy = GetComponent<BatMachineDamagePolicy>();
            }
        }

        private bool ValidateDependencies()
        {
            return actor != null
                && health != null
                && poise != null
                && body != null
                && motor != null
                && threatMonitor != null
                && launcher != null
                && damagePolicy != null;
        }

        private sealed class PatrolRandomState : OwnedState<BatMachineState, BatMachineBrain>
        {
            public override BatMachineState Id => BatMachineState.PatrolRandom;
            protected override void OnEnter() => Owner.EnterPatrolRandom();
            public override void Tick(float deltaTime) => Owner.TickPatrolRandom(deltaTime);
            public override void FixedTick(float fixedDeltaTime) => Owner.FixedTickPatrolRandom(fixedDeltaTime);
        }

        private sealed class EngageState : OwnedState<BatMachineState, BatMachineBrain>
        {
            public override BatMachineState Id => BatMachineState.Engage;
            public override void Tick(float deltaTime) => Owner.TickEngage();
            public override void FixedTick(float fixedDeltaTime) => Owner.FixedTickEngage(fixedDeltaTime);
        }

        private sealed class WindupFireState : OwnedState<BatMachineState, BatMachineBrain>
        {
            public override BatMachineState Id => BatMachineState.WindupFire;
            protected override void OnEnter() => Owner.EnterWindupFire();
            public override void Tick(float deltaTime) => Owner.TickWindupFire(deltaTime);
        }

        private sealed class EvadeState : OwnedState<BatMachineState, BatMachineBrain>
        {
            public override BatMachineState Id => BatMachineState.Evade;
            protected override void OnEnter() => Owner.EnterEvade();
            public override void Tick(float deltaTime) => Owner.TickEvade(deltaTime);
            public override void FixedTick(float fixedDeltaTime) => Owner.FixedTickEvade(fixedDeltaTime);
        }

        private sealed class StunnedFallState : OwnedState<BatMachineState, BatMachineBrain>
        {
            public override BatMachineState Id => BatMachineState.StunnedFall;
            protected override void OnEnter() => Owner.EnterStunnedFall();
        }

        private sealed class GroundedRecoveryState : OwnedState<BatMachineState, BatMachineBrain>
        {
            public override BatMachineState Id => BatMachineState.GroundedRecovery;
            protected override void OnEnter() => Owner.EnterGroundedRecovery();
            public override void Tick(float deltaTime) => Owner.TickGroundedRecovery(deltaTime);
        }

        private sealed class DeadState : OwnedState<BatMachineState, BatMachineBrain>
        {
            public override BatMachineState Id => BatMachineState.Dead;
            protected override void OnEnter() => Owner.EnterDead();
        }
    }
}
