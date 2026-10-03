using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(typeof(EnemyActor), typeof(EnemyPoise), typeof(GroundedEnemyPatrolMotor2D))]
    [RequireComponent(typeof(EnemyMeleeAttack2D))]
    public sealed class GargoyleBrain : MonoBehaviour
    {
        [Header("Encounter Bindings")]
        [SerializeField] private EnemyActor actor;
        [SerializeField] private EnemyPoise poise;
        [SerializeField] private GargoyleTuningSO tuning;
        [SerializeField] private GroundedEnemyPatrolMotor2D groundedMotor;
        [SerializeField] private EnemyMeleeAttack2D melee;
        [SerializeField] private EnemyProjectilePatternLauncher volley;
        [SerializeField] private EnemyBeamAttack2D beam;
        [SerializeField] private EnemyNovaAttack2D nova;
        [SerializeField] private GargoyleDamagePolicy damagePolicy;
        [SerializeField, Tooltip("Optional player health root supplied by gameplay composition.")]
        private GameObject target;

        private static long nextExecutionToken;
        private readonly StateMachine<GargoyleState> stateMachine = new();
        private readonly EnemyAttackRunner runner = new();
        private readonly Dictionary<GargoyleAttackFamily, EnemyAttackDefinitionSO> familyDefinitions = new();
        private IEnemyPatrolMotor2D motor;
        private GargoyleAttackSelector selector;
        private GargoyleLiveTuning live;
        private EnemyAttackDefinitionSO basicDefinition;
        private EnemyActor subscribedActor;
        private EnemyPoise subscribedPoise;
        private float stateElapsed;
        private float blockedElapsed;
        private float resistanceElapsed;
        private float advanceTravelled;
        private string advanceStep;
        private bool resistanceActive;
        private bool basicPending;
        private bool executingBasic;
        private bool repositionReady;
        private bool movementBlocked;
        private bool statesRegistered;
        private bool offenseCancelled;
        private bool warnedBindings;
        private bool warnedHealth;
        private GargoyleAttackFamily queuedFamily;
        private int completedBagsSinceNova;
        private int novaAttemptCount;
        private float gameplayElapsed;
        private float lastNovaAttempt;
        private int feintsUsed;
        private System.Random feintRandom;
        private int feintSeed;
        private bool feintPlanned;
        private bool isFeinting;
        private bool executingNova;
        private bool refillBeforeBasic;
        private string openingStepId;
        private int pendingInterrupt;

        public event Action OffenseCancelled;
        public GargoyleState CurrentState => stateMachine.CurrentStateId;
        public EnemyAttackExecutionSnapshot CurrentAttack => runner.Current;
        public int RemainingFamilies => selector?.RemainingFamilies ?? 0;
        public GargoyleAttackFamily QueuedFamily => queuedFamily;
        public bool IsInitialized { get; private set; }
        public GameObject Target => target;
        public bool IsPoiseResistant => resistanceActive || CurrentState == GargoyleState.Stunned || CurrentState == GargoyleState.Staggered;
        public bool CanEmit => IsInitialized && isActiveAndEnabled && actor.IsOperational && pendingInterrupt == 0
            && CurrentState == GargoyleState.Attack && runner.Current.IsRunning && Time.timeScale > 0 && TargetAvailable();
        public int FacingDirection => motor?.FacingDirection ?? 1;
        public int CompletedBagsSinceNova => completedBagsSinceNova;
        public int NovaAttemptCount => novaAttemptCount;
        public bool IsFeinting => isFeinting && CurrentState == GargoyleState.Attack;
        public bool CanSimulateProjectiles => IsInitialized && isActiveAndEnabled && actor.IsOperational
            && pendingInterrupt == 0 && Time.timeScale > 0 && TargetAvailable();

        private void Awake()
        {
            actor ??= GetComponent<EnemyActor>(); poise ??= GetComponent<EnemyPoise>();
            groundedMotor ??= GetComponent<GroundedEnemyPatrolMotor2D>(); melee ??= GetComponent<EnemyMeleeAttack2D>();
            damagePolicy ??= GetComponent<GargoyleDamagePolicy>();
            volley ??= GetComponent<EnemyProjectilePatternLauncher>();
            beam ??= GetComponent<EnemyBeamAttack2D>();
            nova ??= GetComponent<EnemyNovaAttack2D>();
        }
        private void Start()
        {
            if (!IsInitialized && tuning != null && actor != null && actor.IsInitialized)
                Initialize(actor, poise, tuning, groundedMotor);
        }
        private void Update() => Tick(Time.deltaTime);
        private void FixedUpdate() => TickPhysics(Time.fixedDeltaTime);
        private void OnDisable() => StopDisabledSimulation();
        private void OnDestroy() => Unsubscribe();

        public void Initialize(EnemyActor actor, EnemyPoise poise, GargoyleTuningSO tuning, IEnemyPatrolMotor2D motor)
        {
            if (actor == null || !actor.IsInitialized || poise == null || tuning == null || motor == null)
                throw new ArgumentException("An initialized actor, poise, tuning asset and grounded motor are required.");
            if (tuning.GetValidationErrors().Count != 0) throw new ArgumentException("Gargoyle encounter requires valid authored bindings.", nameof(tuning));
            Unsubscribe();
            this.actor = actor; this.poise = poise; this.tuning = tuning; this.motor = motor;
            live = new GargoyleLiveTuning(tuning);
            if (!ReadBindings()) throw new ArgumentException("Gargoyle attacks require valid step graphs.", nameof(tuning));
            if (!IsInitialized) poise.Initialize(live.Values.MaximumPoise, live.Values.PoiseRegeneration);
            melee ??= GetComponent<EnemyMeleeAttack2D>(); melee?.Initialize(actor, this);
            damagePolicy ??= GetComponent<GargoyleDamagePolicy>(); damagePolicy?.Initialize(actor, poise, tuning);
            volley ??= GetComponent<EnemyProjectilePatternLauncher>(); volley?.Initialize(actor, tuning, this);
            beam ??= GetComponent<EnemyBeamAttack2D>(); beam?.Initialize(actor, tuning, this);
            nova ??= GetComponent<EnemyNovaAttack2D>(); nova?.Initialize(actor, this, damagePolicy);
            subscribedActor = actor; subscribedPoise = poise;
            actor.Defeated += OnDefeated; poise.Depleted += RequestStun;
            if (!statesRegistered)
            {
                foreach (GargoyleState id in Enum.GetValues(typeof(GargoyleState))) stateMachine.AddState(new BrainState(id), this);
                statesRegistered = true;
            }
            selector = new GargoyleAttackSelector(tuning, new System.Random(tuning.RandomSeed));
            queuedFamily = selector.PeekFamily();
            IsInitialized = true;
            ResetEncounter();
        }

        public void SetTarget(GameObject value)
        {
            if (target == value) return;
            if (IsInitialized) { CancelOffense(); basicPending = true; Change(GargoyleState.Idle); }
            target = value;
        }

        public void Tick(float scaledDelta)
        {
            ValidateDelta(scaledDelta);
            if (!IsInitialized) return;
            if (!isActiveAndEnabled) { StopDisabledSimulation(); return; }
            RefreshConfiguration();
            if (ReconcileInterrupt()) return;
            if (CurrentState == GargoyleState.Dead) return;
            if (!TargetAvailable())
            {
                CancelOffense(); basicPending = true;
                // A held target cannot erase an already entered response or refill its poise.
                if (CurrentState != GargoyleState.Stunned && CurrentState != GargoyleState.Staggered) Change(GargoyleState.Idle);
                return;
            }
            if (Time.timeScale <= 0) { motor.Stop(); return; }
            gameplayElapsed += scaledDelta;
            if (resistanceActive)
            {
                resistanceElapsed += scaledDelta;
                if (resistanceElapsed >= live.Values.PoiseResistanceDuration) resistanceActive = false;
            }
            UpdatePolicy();
            if (CurrentState != GargoyleState.Stunned && CurrentState != GargoyleState.Staggered) poise.Tick(scaledDelta);
            if (ReconcileInterrupt()) return;
            stateMachine.Tick(scaledDelta);
            ReconcileInterrupt();
            UpdatePolicy();
        }

        public void TickPhysics(float scaledDelta)
        {
            ValidateDelta(scaledDelta);
            if (!IsInitialized) return;
            if (!isActiveAndEnabled) { StopDisabledSimulation(); return; }
            if (ReconcileInterrupt()) return;
            if (!TargetAvailable()) { CancelOffense(); motor.Stop(); return; }
            if (Time.timeScale <= 0 || scaledDelta <= 0 || pendingInterrupt != 0 || !actor.IsOperational) { motor.Stop(); return; }
            RefreshConfiguration();
            if (ReconcileInterrupt()) return;
            stateMachine.FixedTick(scaledDelta);
            ReconcileInterrupt();
        }

        public void RequestStun()
        {
            if (!IsInitialized || !actor.IsOperational || CurrentState == GargoyleState.Stunned || CurrentState == GargoyleState.Dead) return;
            QueueInterrupt(2);
        }
        public void RequestBeamStagger(long castToken)
        {
            if (!IsInitialized || !actor.IsOperational || CurrentState != GargoyleState.Attack
                || castToken != runner.Current.ExecutionToken || castToken <= 0) return;
            QueueInterrupt(1);
        }

        public void ResetEncounter()
        {
            if (!IsInitialized) return;
            CancelOffense(); pendingInterrupt = 0; resistanceActive = false;
            selector.Reset(new System.Random(tuning.RandomSeed)); queuedFamily = selector.PeekFamily();
            completedBagsSinceNova = novaAttemptCount = feintsUsed = 0;
            gameplayElapsed = lastNovaAttempt = 0;
            feintSeed = tuning.RandomSeed; feintRandom = new System.Random(feintSeed);
            feintPlanned = isFeinting = executingNova = refillBeforeBasic = false;
            basicPending = true; damagePolicy?.ResetDamageIdentity();
            Change(actor.IsDefeated ? GargoyleState.Dead : GargoyleState.Idle, true);
        }

        private void TickState(GargoyleState state, float delta)
        {
            stateElapsed += delta;
            switch (state)
            {
                case GargoyleState.Idle: Change(GargoyleState.Reposition); break;
                case GargoyleState.Reposition:
                    if (movementBlocked) blockedElapsed += delta;
                    else blockedElapsed = 0;
                    if (blockedElapsed >= live.Values.BlockedRepositionTimeout) { basicPending = true; Change(GargoyleState.PassRecovery); }
                    else if (repositionReady) BeginNextAttack();
                    break;
                case GargoyleState.Attack:
                    if (executingNova) nova?.Tick(delta);
                    if (pendingInterrupt != 0 || !actor.IsOperational) return;
                    TrackAim(); TickAttack(delta);
                    if (runner.Current.Phase != EnemyAttackPhase.Active) beam?.Cancel();
                    if (!runner.Current.IsRunning)
                    {
                        if (executingNova)
                        {
                            executingNova = false; basicPending = true; refillBeforeBasic = selector.RemainingFamilies == 0;
                            feintsUsed = 0; nova?.Cancel(); Change(GargoyleState.Reposition);
                        }
                        else if (executingBasic)
                        {
                            basicPending = false; isFeinting = false;
                            if (selector.RemainingFamilies == 0) { basicPending = true; Change(GargoyleState.PassRecovery); }
                            else Change(GargoyleState.FamilyRecovery);
                        }
                        else if (selector.RemainingFamilies == 0) { basicPending = true; Change(GargoyleState.PassRecovery); }
                        else Change(GargoyleState.FamilyRecovery);
                    }
                    break;
                case GargoyleState.FamilyRecovery:
                    if (stateElapsed >= live.Values.FamilyRecovery) Change(GargoyleState.Reposition);
                    break;
                case GargoyleState.PassRecovery:
                    if (stateElapsed >= live.Values.PassRecovery)
                    {
                        if (TryBeginNova()) break;
                        refillBeforeBasic = selector.RemainingFamilies == 0; feintsUsed = 0;
                        Change(GargoyleState.Reposition);
                    }
                    break;
                case GargoyleState.Stunned:
                case GargoyleState.Staggered:
                    var duration = state == GargoyleState.Stunned ? live.Values.StunDuration : live.Values.BeamStaggerDuration;
                    if (stateElapsed >= duration)
                    {
                        poise.Restore(Mathf.Max(0, poise.MaximumPoise * live.Values.RestoredPoiseFraction - poise.CurrentPoise));
                        resistanceActive = true; resistanceElapsed = 0; basicPending = true;
                        Change(GargoyleState.Reposition);
                    }
                    break;
            }
        }

        private void FixedState(GargoyleState state, float delta)
        {
            if (state == GargoyleState.Reposition)
            {
                var distance = basicPending || QueuedFamily == GargoyleAttackFamily.Wingbreaker ? live.Values.MeleeDistance : live.Values.RangedDistance;
                var targetPosition = (Vector2)target.transform.position;
                var separation = Mathf.Abs(targetPosition.x - motor.Position.x);
                if (separation <= distance && Vector2.Distance(targetPosition, motor.Position) <= live.Values.EngageRange)
                { motor.Stop(); repositionReady = true; movementBlocked = false; return; }
                var destination = new Vector2(targetPosition.x - Mathf.Sign(targetPosition.x - motor.Position.x) * distance, motor.Position.y);
                var result = motor.MoveTowards(destination, live.Values.MovementSpeed, live.Values.ArrivalTolerance, delta);
                repositionReady = result == EnemyPatrolMoveResult.Arrived
                    && Vector2.Distance(targetPosition, motor.Position) <= live.Values.EngageRange;
                movementBlocked = result == EnemyPatrolMoveResult.Blocked
                    || (result == EnemyPatrolMoveResult.Arrived && !repositionReady);
                return;
            }
            if (state != GargoyleState.Attack || !CanEmit) { motor.Stop(); return; }
            var execution = runner.Current;
            if (execution.Phase != EnemyAttackPhase.Active) { motor.Stop(); return; }
            if (advanceStep != execution.StepId) { advanceStep = execution.StepId; advanceTravelled = 0; }
            var payload = execution.Step.Payload;
            var remaining = Mathf.Max(0, payload.AdvanceDistance - advanceTravelled);
            if (remaining > 0)
            {
                var speed = Mathf.Min(payload.AdvanceSpeed, remaining / delta);
                var move = motor.MoveTowards(motor.Position + new Vector2(FacingDirection * remaining, 0), speed, 0, delta);
                if (move == EnemyPatrolMoveResult.Moving) advanceTravelled += speed * delta;
                else motor.Stop();
            }
            else motor.Stop();
            if (execution.Kind == EnemyAttackPayloadKind.Melee) melee?.Sample(execution);
            else if (execution.Kind == EnemyAttackPayloadKind.Volley && runner.TryConsumeRelease(execution.ExecutionToken, execution.StepId))
                volley?.Release(execution, motor.Position + new Vector2(payload.Offset.x * FacingDirection, payload.Offset.y));
            else if (execution.Kind == EnemyAttackPayloadKind.Beam)
            {
                if (runner.TryConsumeRelease(execution.ExecutionToken, execution.StepId)) beam?.Begin(execution);
                beam?.Sample(motor.Position + new Vector2(payload.Offset.x * FacingDirection, payload.Offset.y), execution.Aim, execution.Elapsed);
            }
            else if (execution.Kind == EnemyAttackPayloadKind.Nova && runner.TryConsumeRelease(execution.ExecutionToken, execution.StepId))
                nova?.Release(motor.Position);
            if (CanEmit) runner.ConfirmPhysicsSample(execution.ExecutionToken, execution.StepId);
        }

        private void BeginNextAttack()
        {
            executingBasic = basicPending;
            if (executingBasic && refillBeforeBasic)
            {
                queuedFamily = selector.PeekFamily(); refillBeforeBasic = false;
                if (feintSeed != tuning.RandomSeed)
                { feintSeed = tuning.RandomSeed; feintRandom = new System.Random(feintSeed); }
            }
            var family = executingBasic ? queuedFamily : selector.PeekFamily();
            queuedFamily = family;
            var definition = executingBasic ? basicDefinition : familyDefinitions[family];
            if (definition == null || !definition.TryCaptureSteps(out var captured)) return;
            var count = !executingBasic && captured.Any(step => step.Payload.Kind == EnemyAttackPayloadKind.Volley) ? selector.CommitVolleyCount() : 0;
            if (!runner.Begin(definition, Interlocked.Increment(ref nextExecutionToken), count)) return;
            if (!executingBasic)
            {
                selector.CommitFamily();
                if (selector.RemainingFamilies == 0) completedBagsSinceNova++;
                if (selector.RemainingFamilies > 0) queuedFamily = selector.PeekFamily();
            }
            offenseCancelled = false; advanceStep = null;
            executingNova = isFeinting = false; openingStepId = captured[0].Id;
            feintPlanned = executingBasic && feintsUsed < live.Values.FeintBudget && tuning.FeintAttack != null
                && feintRandom.NextDouble() < live.Values.FeintProbability;
            if (feintPlanned) feintsUsed++;
            Change(GargoyleState.Attack); TrackAim();
        }

        private void TickAttack(float delta)
        {
            if (isFeinting) runner.SetMinimumWindup(live.Values.FeintResponseDuration);
            if (feintPlanned && runner.Current.StepId == openingStepId && runner.Current.Phase == EnemyAttackPhase.Windup
                && !runner.Current.IsAimLocked)
            {
                var untilCue = Mathf.Max(0, live.Values.FeintCueDelay - runner.Current.Elapsed);
                if (delta >= untilCue || Mathf.Approximately(delta, untilCue))
                {
                    var beforeCue = Mathf.Min(delta, untilCue);
                    runner.Tick(beforeCue); feintPlanned = false;
                    if (runner.Current.Phase == EnemyAttackPhase.Windup && !runner.Current.IsAimLocked
                        && tuning.FeintAttack.TryCaptureSteps(out _))
                    {
                        melee?.Cancel(); runner.Cancel();
                        if (runner.Begin(tuning.FeintAttack, Interlocked.Increment(ref nextExecutionToken)))
                        {
                            isFeinting = true; advanceStep = null;
                            runner.SetMinimumWindup(live.Values.FeintResponseDuration); TrackAim();
                        }
                    }
                    runner.Tick(delta - beforeCue); return;
                }
            }
            runner.Tick(delta);
        }

        private bool TryBeginNova()
        {
            if (nova == null || completedBagsSinceNova < live.Values.NovaBagCadence
                || gameplayElapsed - lastNovaAttempt < live.Values.NovaCooldown || tuning.NovaAttack == null
                || !tuning.NovaAttack.TryCaptureSteps(out var captured)
                || captured[0].Payload.Kind != EnemyAttackPayloadKind.Nova) return false;
            if (!runner.Begin(tuning.NovaAttack, Interlocked.Increment(ref nextExecutionToken))) return false;
            completedBagsSinceNova = 0; lastNovaAttempt = gameplayElapsed; novaAttemptCount++;
            executingNova = true; executingBasic = feintPlanned = isFeinting = false;
            offenseCancelled = false; advanceStep = null;
            Change(GargoyleState.Attack); nova.Begin(runner.Current); TrackAim(); UpdatePolicy();
            return true;
        }

        private void TrackAim()
        {
            if (!runner.Current.IsRunning || runner.Current.IsAimLocked || !TargetAvailable()) return;
            motor.SetFacing(Math.Sign(target.transform.position.x - motor.Position.x));
            var offset = runner.Current.Step.Payload.Offset;
            var origin = motor.Position + new Vector2(offset.x * FacingDirection, offset.y);
            runner.SetAim((Vector2)target.transform.position - origin);
        }

        private bool TargetAvailable() => EnemyPlayerTargeting.IsAvailable(target)
            && live != null && Vector2.Distance(target.transform.position, motor.Position) <= live.Values.MonitorRange;

        private void RefreshConfiguration()
        {
            live.Refresh(); ReadBindings();
            poise.UpdateConfiguration(live.Values.MaximumPoise, live.Values.PoiseRegeneration);
            var maximum = actor.Definition.MaxHealth;
            if (float.IsFinite(maximum) && maximum >= 1) { actor.Health.UpdateMaximumHealth(maximum); warnedHealth = false; }
            else if (!warnedHealth) { warnedHealth = true; Debug.LogWarning("Invalid Gargoyle maximum health; retaining the last valid ceiling.", actor); }
            if (motor is GroundedEnemyPatrolMotor2D grounded)
                grounded.ConfigureProbes(tuning.EnvironmentLayer, live.Values.LedgeProbeOffset, live.Values.WallProbeOffset, live.Values.ProbeRadius);
        }

        private bool ReadBindings()
        {
            var definitions = tuning.Families.Select(tuning.GetAttackDefinition).ToArray();
            if (tuning.BasicAttack == null || tuning.BasicAttack.GetValidationErrors().Count != 0
                || definitions.Any(definition => definition == null || definition.GetValidationErrors().Count != 0))
            {
                if (!warnedBindings) { warnedBindings = true; Debug.LogWarning("Invalid Gargoyle attack bindings; retaining last valid references.", tuning); }
                return false;
            }
            basicDefinition = tuning.BasicAttack;
            foreach (var family in tuning.Families) familyDefinitions[family] = tuning.GetAttackDefinition(family);
            warnedBindings = false;
            return true;
        }

        private void QueueInterrupt(int priority)
        {
            pendingInterrupt = Math.Max(pendingInterrupt, priority);
            CancelOffense();
        }
        private bool ReconcileInterrupt()
        {
            if (actor.IsDefeated) pendingInterrupt = 3;
            if (pendingInterrupt == 0) return false;
            var priority = pendingInterrupt; pendingInterrupt = 0;
            if (priority != 3)
            {
                refillBeforeBasic = executingNova && selector.RemainingFamilies == 0;
                if (executingNova) feintsUsed = 0;
            }
            executingNova = feintPlanned = isFeinting = false;
            CancelOffense();
            Change(priority == 3 ? GargoyleState.Dead : priority == 2 ? GargoyleState.Stunned : GargoyleState.Staggered);
            UpdatePolicy();
            return true;
        }
        private void CancelOffense()
        {
            melee?.Cancel(); runner.Cancel(); motor?.Stop();
            if (offenseCancelled) return;
            offenseCancelled = true;
            OffenseCancelled?.Invoke();
        }
        private void OnDefeated(EnemyDamageEvent _) => QueueInterrupt(3);
        private void StopDisabledSimulation()
        {
            if (!IsInitialized) return;
            CancelOffense(); basicPending = true;
            Change(actor.IsDefeated ? GargoyleState.Dead : GargoyleState.Idle);
        }
        private void UpdatePolicy() => damagePolicy?.SetResponseState(executingNova && nova != null && nova.IsTelegraphing
            && runner.Current.Phase == EnemyAttackPhase.Windup, CurrentState == GargoyleState.Staggered, IsPoiseResistant, FacingDirection);
        private void Change(GargoyleState state, bool restart = false) => stateMachine.TryChangeState(state, restart);
        private void EnterState() { stateElapsed = blockedElapsed = 0; repositionReady = movementBlocked = false; motor.Stop(); }
        private void Unsubscribe()
        {
            if (subscribedActor != null) subscribedActor.Defeated -= OnDefeated;
            if (subscribedPoise != null) subscribedPoise.Depleted -= RequestStun;
            subscribedActor = null; subscribedPoise = null;
        }
        private static void ValidateDelta(float delta)
        { if (!float.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta)); }

        private sealed class BrainState : OwnedState<GargoyleState, GargoyleBrain>
        {
            private readonly GargoyleState id;
            public BrainState(GargoyleState id) => this.id = id;
            /// <summary>Gets this encounter state's stable identity.</summary>
            public override GargoyleState Id => id;
            /// <summary>Starts the state's elapsed clock and stops previous movement.</summary>
            protected override void OnEnter() => Owner.EnterState();
            /// <summary>Runs the owner's decision or response clock for this state.</summary>
            public override void Tick(float deltaTime) => Owner.TickState(id, deltaTime);
            /// <summary>Runs grounded movement and phase-gated payload sampling.</summary>
            public override void FixedTick(float fixedDeltaTime) => Owner.FixedState(id, fixedDeltaTime);
        }
    }
}
