using UnityEngine;
using UnityEngine.InputSystem;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(PlayerMotor2D))]
    [RequireComponent(requiredComponent: typeof(PlayerSensors2D))]
    [RequireComponent(requiredComponent: typeof(PlayerCardSelectionInput))]
    public sealed class PlayerController :
        MonoBehaviour,
        IGameplayServicesConsumer,
        IPlayerCardTimeSourceConsumer
    {
        [Header(header: "Components")]
        [Tooltip(tooltip: "Motor that applies final movement frames to the Rigidbody2D.")]
        [SerializeField]
        private PlayerMotor2D motor;

        [Tooltip(tooltip: "Sensor component used by locomotion states.")] [SerializeField]
        private PlayerSensors2D sensors;

        [Tooltip(tooltip: "Player-local melee hit detector that publishes accepted hitstop requests.")]
        [SerializeField]
        private PlayerAttackHitDetector2D attackHitDetector;

        [Tooltip(tooltip: "Prototype Card Time debug presentation bound to the global session.")]
        [SerializeField]
        private PlayerCardTimeDebugPresenter cardTimePresenter;

        [Tooltip(tooltip: "Non-blocking card selection combat UI bound to the active transaction.")]
        [SerializeField]
        private CardTimeSelectionHudUI cardSelectionHud;

        [Tooltip(tooltip: "Moves the active Card Time selection transaction from player navigation input.")]
        [SerializeField]
        private PlayerCardSelectionInput cardSelectionInput;

        [Tooltip(tooltip: "Configures Card Time selection slot counts and direct slot input commands.")]
        [SerializeField]
        private CardTimeSelectionUiConfigSO cardSelectionUiConfig;

        [Tooltip(tooltip: "Available Card Time control schemes and serialized default selection.")]
        [SerializeField]
        private CardTimeControlSchemeProfileSO cardControlSchemeProfile;

        [Tooltip(tooltip: "Optional selected Card Time control scheme id. Empty uses the profile default.")]
        [SerializeField]
        private string selectedCardControlSchemeId;

        [Tooltip(tooltip: "Player-local resource wallet used by cards.")]
        [SerializeField]
        private PlayerResourceWallet resourceWallet;

        [Tooltip(tooltip: "Player-local combat effect and damage provider runtime.")]
        [SerializeField]
        private PlayerCombatEffects combatEffects;

        [Tooltip(tooltip: "Player-local card transaction and prototype selection runtime.")]
        [SerializeField]
        private PlayerCardRuntime cardRuntime;

        [Tooltip(tooltip: "Stable-id catalog used to resolve active Card Time loadout candidates.")]
        [SerializeField]
        private CardCatalogSO cardCatalog;

        [Tooltip(tooltip: "Inventory profile supplying equipped card ids for the active Card Time.")]
        [SerializeField]
        private PlayerCardInventoryProfileSO cardInventoryProfile;

        [Tooltip(tooltip: "Captures volatile player values immediately before card commit preparation.")]
        [SerializeField]
        private PlayerCardCommitSnapshotSource cardSnapshotSource;

        [Tooltip(tooltip: "Temporary card-granted extra jump state.")]
        [SerializeField]
        private PlayerExtraJumpRuntime extraJumpRuntime;

        [Header(header: "Input Actions")]
        [Tooltip(tooltip: "Input System action used for player movement.")]
        [SerializeField]
        private InputActionReference moveAction;

        [Tooltip(tooltip: "Input System action used for jumping.")] [SerializeField]
        private InputActionReference jumpAction;

        [Tooltip(tooltip: "Input System action used for basic attack.")] [SerializeField]
        private InputActionReference attackAction;

        [Tooltip(tooltip: "Input System action used as dash in the first prototype slice.")] [SerializeField]
        private InputActionReference dashAction;

        [Tooltip(tooltip: "Left half of the lenient Card Time chord.")] [SerializeField]
        private InputActionReference cardTimeLeftAction;

        [Tooltip(tooltip: "Right half of the lenient Card Time chord.")] [SerializeField]
        private InputActionReference cardTimeRightAction;

        [Header(header: "Definitions")]
        [Tooltip(tooltip: "Movement tuning used by locomotion states.")]
        [SerializeField]
        private PlayerMovementConfigSO movementConfig;

        [Tooltip(tooltip: "Dash tuning used by DashAction.")] [SerializeField]
        private PlayerDashDefinitionSO dashDefinition;

        [Tooltip(tooltip: "Attack shell tuning used by AttackAction.")] [SerializeField]
        private PlayerAttackDefinitionSO attackDefinition;

        private PlayerInputSnapshot input;
        private IPlayerAnimationSnapshotSource animationSnapshotSource;
        private readonly PlayerAttackComboRuntime attackCombo = new();
        private PlayerCardTimeChordRuntime cardTimeChord;
        private IGameplayServices gameplayServices;
        private IPlayerCardTimeSource cardTimeSource;
        private CardTimeSessionEventChannelSO cardTimeTransitionEvent;
        private InputAction resolvedCardTimeLeftAction;
        private InputAction resolvedCardTimeRightAction;
        private CardTimeSelectionTransaction activeCardSelection;
        private bool restoreNeutralCardTimeWhenGrounded;

        public PlayerContext Context { get; private set; }
        public PlayerLocomotionController Locomotion { get; private set; }
        public PlayerActionRunner ActionRunner { get; private set; }
        public PlayerAnimationSnapshotPublisher AnimationSnapshots { get; private set; }
        public ICardTimeSession CardTimeSession => gameplayServices?.CardTime;
        public PlayerCardTimeConfigSO CardTimeConfig => cardTimeSource?.Configuration;

        private void Awake() {
            if (motor == null) {
                motor = GetComponent<PlayerMotor2D>();
            }

            if (sensors == null) {
                sensors = GetComponent<PlayerSensors2D>();
            }

            movementConfig = movementConfig != null
                ? movementConfig
                : ScriptableObject.CreateInstance<PlayerMovementConfigSO>();
            dashDefinition = dashDefinition != null
                ? dashDefinition
                : ScriptableObject.CreateInstance<PlayerDashDefinitionSO>();
            attackDefinition = attackDefinition != null
                ? attackDefinition
                : ScriptableObject.CreateInstance<PlayerAttackDefinitionSO>();
            resourceWallet ??= GetComponent<PlayerResourceWallet>();
            combatEffects ??= GetComponent<PlayerCombatEffects>();
            cardRuntime ??= GetComponent<PlayerCardRuntime>();
            cardSnapshotSource ??= GetComponent<PlayerCardCommitSnapshotSource>();
            extraJumpRuntime ??= GetComponent<PlayerExtraJumpRuntime>();
            Context = new PlayerContext(motor: motor, sensors: sensors, movementConfig: movementConfig,
                dashDefinition: dashDefinition, attackDefinition: attackDefinition,
                extraJumpRuntime: extraJumpRuntime);
            sensors.Refresh();
            Locomotion = new PlayerLocomotionController(context: Context);
            ActionRunner = new PlayerActionRunner();
            Context.AttachRuntime(locomotion: Locomotion, actionRunner: ActionRunner);
            Locomotion.EnterInitialState(context: Context);
            animationSnapshotSource = new PlayerAnimationSnapshotSource(context: Context);
            AnimationSnapshots = new PlayerAnimationSnapshotPublisher();

            if (cardTimePresenter == null) {
                cardTimePresenter = GetComponent<PlayerCardTimeDebugPresenter>();
            }

            if (cardSelectionHud == null) {
                cardSelectionHud = GetComponentInChildren<CardTimeSelectionHudUI>(includeInactive: true);
            }

            if (cardSelectionInput == null) {
                cardSelectionInput = GetComponent<PlayerCardSelectionInput>();
            }

            if (cardSelectionInput == null) {
                cardSelectionInput = gameObject.AddComponent<PlayerCardSelectionInput>();
            }

            cardSelectionInput.Configure(
                cardSelectionUiConfig,
                cardControlSchemeProfile,
                selectedCardControlSchemeId,
                attackAction != null ? attackAction.action?.actionMap : null);
            cardSelectionInput.SlotCommanded += HandleCardSlotCommand;
            cardSelectionHud?.SetControlScheme(
                cardSelectionUiConfig,
                cardControlSchemeProfile,
                selectedCardControlSchemeId);

            if (attackHitDetector == null) {
                attackHitDetector = GetComponent<PlayerAttackHitDetector2D>();
            }

            if (attackHitDetector != null) {
                attackHitDetector.Initialize(controller: this, effects: combatEffects);
            } else {
                Debug.LogError(
                    message: "PlayerController requires an explicit PlayerAttackHitDetector2D reference.",
                    context: this);
            }

            resolvedCardTimeLeftAction = cardTimeLeftAction != null
                ? cardTimeLeftAction.action
                : attackAction?.action?.actionMap?.FindAction("CardTimeLeft");
            resolvedCardTimeRightAction = cardTimeRightAction != null
                ? cardTimeRightAction.action
                : attackAction?.action?.actionMap?.FindAction("CardTimeRight");
        }

        private void OnEnable() {
            EnableAction(reference: moveAction);
            EnableAction(reference: jumpAction);
            EnableAction(reference: attackAction);
            EnableAction(reference: dashAction);
            EnableAction(action: resolvedCardTimeLeftAction);
            EnableAction(action: resolvedCardTimeRightAction);
            SubscribeCardTimeTransitions();
        }

        private void OnDisable() {
            UnsubscribeCardTimeTransitions();
            DisableAction(reference: moveAction);
            DisableAction(reference: jumpAction);
            DisableAction(reference: attackAction);
            DisableAction(reference: dashAction);
            DisableAction(action: resolvedCardTimeLeftAction);
            DisableAction(action: resolvedCardTimeRightAction);
            cardTimeSource?.Cancel();
            cardTimeSource?.PublishAvailability(state: PlayerCardTimeState.None);
            DisposeActiveCardSelection();
            CompleteCurrentAttack();
        }

        private void Update() {
            attackCombo.Tick(deltaTime: Time.deltaTime);
            input = ReadInputActions();
            SetInputSnapshot(snapshot: input);

            if (!ActionRunner.HasAction) {
                Context.RefreshFacingFromInput();
            }

            PublishCardTimeAvailability();
            RestoreNeutralCardTimeAfterTimeoutIfGrounded();

            if (cardTimeChord != null && cardTimeChord.Tick(
                    unscaledDeltaTime: Time.unscaledDeltaTime,
                    leftPressed: WasPressed(action: resolvedCardTimeLeftAction),
                    leftHeld: IsPressed(action: resolvedCardTimeLeftAction),
                    rightPressed: WasPressed(action: resolvedCardTimeRightAction),
                    rightHeld: IsPressed(action: resolvedCardTimeRightAction))) {
                HandleCardTimePressed();
            }

            var slotCommanded = false;
            var cardTimeSnapshot = CardTimeSession?.Current ?? default;
            SynchronizeActiveCardSelection(cardTimeSnapshot);
            if (cardTimeSnapshot.IsActive) {
                slotCommanded = cardSelectionInput?.TickSlotCommands(
                    cardTimeSnapshot.SessionCardTime) == true;
                if (!slotCommanded) {
                    cardSelectionInput?.TickNavigation(input.Move, Time.unscaledDeltaTime);
                }
            }

            if (!slotCommanded
                && cardTimeSnapshot.IsActive
                && (input.DashPressed || input.AttackPressed)) {
                CancelActiveCardSelection();
                cardTimeSnapshot = CardTimeSession?.Current ?? default;
            }

            if (input.DashPressed) {
                if (ActionRunner.TryStartAction(
                        context: Context,
                        action: new DashAction(),
                        replaceCurrent: false)) {
                    attackCombo.Clear();
                }
            } else if (input.AttackPressed) {
                HandleAttackPressed();
            }

            Locomotion.Tick(context: Context, deltaTime: Time.deltaTime);
            ActionRunner.Tick(
                context: Context,
                deltaTime: Time.deltaTime,
                clearCompleted: false);

            if (!TryCommitBufferedAttack()) {
                RememberCompletedAttackTiming();
                if (ActionRunner.CurrentAction?.IsComplete == true) {
                    CompleteCurrentAttack();
                }
                ActionRunner.ClearCompletedAction(context: Context);
            }
        }

        private void OnDestroy() {
            UnsubscribeCardTimeTransitions();

            if (cardSelectionInput != null) {
                cardSelectionInput.SlotCommanded -= HandleCardSlotCommand;
            }

            cardTimeSource?.Unregister();
            cardTimeSource = null;
        }

        public void BindGameplayServices(IGameplayServices services) {
            UnsubscribeCardTimeTransitions();
            gameplayServices = services;
            cardTimeTransitionEvent = services?.CardTimeTransitions;
            SubscribeCardTimeTransitions();
            attackHitDetector?.BindGameplayServices(services);
            cardTimePresenter?.Initialize(services?.CardTime);
        }

        public void BindPlayerCardTimeSource(IPlayerCardTimeSource source) {
            cardTimeSource = source;
            var sourceConfig = cardTimeSource?.Configuration;
            if (sourceConfig != null) {
                cardTimeChord = new PlayerCardTimeChordRuntime(
                    graceDuration: sourceConfig.ChordInputGraceDuration);
            }

            if (!isActiveAndEnabled) {
                cardTimeSource?.PublishAvailability(state: PlayerCardTimeState.None);
            }
        }

        private void FixedUpdate() {
            sensors.Refresh();
            Locomotion.FixedTick(context: Context, fixedDeltaTime: Time.fixedDeltaTime);
            ActionRunner.FixedTick(
                context: Context,
                fixedDeltaTime: Time.fixedDeltaTime,
                clearCompleted: false);

            var frame = Locomotion.BuildFrame(context: Context, fixedDeltaTime: Time.fixedDeltaTime);
            ActionRunner.CurrentLocomotionOverride?.ModifyLocomotionFrame(frame: ref frame, context: Context,
                fixedDeltaTime: Time.fixedDeltaTime);
            AnimationSnapshots.Publish(snapshot: animationSnapshotSource.Capture(frame: frame));
            motor.ApplyFrame(frame: frame);
        }

        public void SetInputSnapshot(
            PlayerInputSnapshot snapshot
        ) {
            Context.SetInput(input: snapshot);
        }

        public void ApplyAnimationFrame(
            PlayerActionFrame frame
        ) {
            Context.SetActionFrame(frame: frame);
        }

        public void ApplyAnimationFrame(
            PlayerActionState actionState,
            PlayerActionFrame frame
        ) {
            if (ActionRunner == null || ActionRunner.CurrentState != actionState) {
                return;
            }

            ApplyAnimationFrame(frame: frame);
        }

        public void ResetTransientState() {
            CompleteCurrentAttack();
            ActionRunner?.Clear(context: Context);
            attackCombo.Clear();
            cardTimeSource?.Cancel();
            cardTimeSource?.PublishAvailability(state: PlayerCardTimeState.None);
            DisposeActiveCardSelection();
            input = PlayerInputSnapshot.None;
            SetInputSnapshot(snapshot: input);
        }

        private void HandleAttackPressed() {
            if (!ActionRunner.HasAction) {
                if (attackCombo.TryResolveIdleAttack(attackState: out var attackState)) {
                    StartAttack(state: attackState);
                }

                return;
            }

            var nextAttack = PlayerAttackSequence.GetNext(current: ActionRunner.CurrentState);
            if (nextAttack == PlayerActionState.None) {
                return;
            }

            if (ActionRunner.CurrentAction is not IPlayerChainBufferSource chainBufferSource) {
                return;
            }

            attackCombo.TryBuffer(
                currentState: ActionRunner.CurrentState,
                chainBufferSource: chainBufferSource);
            TryCommitBufferedAttack();
        }

        private bool TryCommitBufferedAttack() {
            if (ActionRunner.CurrentAction is not IPlayerChainBufferSource chainBufferSource
                || !attackCombo.TryConsume(
                    chainBufferSource: chainBufferSource,
                    followUpState: out var followUpState)) {
                return false;
            }

            return StartAttack(state: followUpState, replaceCurrent: true);
        }

        private bool StartAttack(
            PlayerActionState state,
            bool replaceCurrent = false
        ) {
            if (replaceCurrent) {
                CompleteCurrentAttack();
            }

            var attack = new AttackAction(state: state);
            var started = ActionRunner.TryStartAction(
                context: Context,
                action: attack,
                replaceCurrent: replaceCurrent);

            if (started) {
                attackCombo.NotifyAttackStarted(state);
                cardTimeSource?.PublishAvailability(attackCombo.AvailableCardTime);
                combatEffects?.BeginAttack(attack.ExecutionId);
            }

            return started;
        }

        private void RememberCompletedAttackTiming() {
            if (ActionRunner.CurrentAction is not IPlayerChainBufferSource chainBufferSource
                || !ActionRunner.CurrentAction.IsComplete) {
                return;
            }

            attackCombo.NotifyAttackCompleted(
                completedState: ActionRunner.CurrentState,
                postRecoveryBufferGraceDuration:
                chainBufferSource.PostRecoveryBufferGraceDuration,
                sequenceRestartCooldown: chainBufferSource.SequenceRestartCooldown);
        }

        private void PublishCardTimeAvailability() {
            cardTimeSource?.PublishAvailability(state: attackCombo.AvailableCardTime);
        }

        private void RestoreNeutralCardTimeAfterTimeoutIfGrounded() {
            if (!restoreNeutralCardTimeWhenGrounded || sensors?.IsGrounded != true) {
                return;
            }

            restoreNeutralCardTimeWhenGrounded = false;
            attackCombo.RestoreNeutralCardTime();
            cardTimeSource?.PublishAvailability(state: attackCombo.AvailableCardTime);
        }

        private void SubscribeCardTimeTransitions() {
            if (!isActiveAndEnabled || cardTimeTransitionEvent == null) {
                return;
            }

            cardTimeTransitionEvent.Raised -= HandleCardTimeTransition;
            cardTimeTransitionEvent.Raised += HandleCardTimeTransition;
        }

        private void UnsubscribeCardTimeTransitions() {
            if (cardTimeTransitionEvent == null) {
                return;
            }

            cardTimeTransitionEvent.Raised -= HandleCardTimeTransition;
        }

        private void HandleCardTimeTransition(CardTimeSessionTransition transition) {
            if (transition.Outcome == CardTimeSessionOutcome.TimedOut) {
                restoreNeutralCardTimeWhenGrounded = true;
            }
        }

        private void HandleCardTimePressed() {
            var result = cardTimeSource?.RequestActivation()
                ?? CardTimeActivationRequestResult.Rejected;
            if (result == CardTimeActivationRequestResult.Rejected) {
                cardTimePresenter?.ShowInvalidActivation();
                return;
            }

            TryCreateActiveCardSelection();
        }

        private void HandleCardSlotCommand(CardTimeSelectionSlotCommand command)
        {
            if (CardTimeSession?.Current.IsActive != true)
            {
                return;
            }

            if (!command.Selected)
            {
                RejectAndCloseCardTime("invalid slot", command.SlotIndex);
                return;
            }

            cardSelectionHud?.PlaySlotAnimation(
                command.SlotIndex,
                CardTimeSelectionSlotAnimation.Selected);
            HandleCardCommit(command.SlotIndex);
        }

        private void HandleCardCommit(int commandedSlotIndex = -1) {
            var snapshot = CardTimeSession?.Current ?? default;
            var executionId = (ActionRunner.CurrentAction as IPlayerAttackExecution)?.ExecutionId;
            var isAirborne = Locomotion.CurrentStateId == PlayerLocomotionState.Airborne;
            var feedbackSlotIndex = commandedSlotIndex >= 0
                ? commandedSlotIndex
                : activeCardSelection?.Current.SelectedIndex ?? -1;

            if (cardRuntime == null
                || activeCardSelection == null
                || !activeCardSelection.IsValid
                || activeCardSelection.SessionId != snapshot.ActiveSessionId
                || !activeCardSelection.TryGetSelectedCard(out var selectedCard)
                || cardSnapshotSource == null) {
                RejectAndCloseCardTime("something invalid", feedbackSlotIndex);
                return;
            }

            var commitSnapshot = cardSnapshotSource.Capture(
                snapshot.SessionCardTime,
                executionId,
                isAirborne);
            var readiness = cardRuntime.TryPrepare(
                selectedCard,
                activeCardSelection,
                commitSnapshot);
            if (!readiness.Succeeded) {
                RejectAndCloseCardTime(readiness.Failure.ToString(), feedbackSlotIndex);
                return;
            }

            if (cardTimeSource?.TryCommit(readiness.Commit) != true) {
                RejectAndCloseCardTime("did not commit", feedbackSlotIndex);
                return;
            }

            cardSelectionHud?.PlaySlotAnimation(
                feedbackSlotIndex,
                CardTimeSelectionSlotAnimation.Committed);
            DisposeActiveCardSelection();
            ConsumeCardTimeOpportunity();
        }

        private void RejectAndCloseCardTime(string reason, int slotIndex = -1)
        {
            PublishRejectedCardFeedback();
            cardSelectionHud?.PlaySlotAnimation(
                slotIndex,
                CardTimeSelectionSlotAnimation.Invalid);
            cardTimePresenter?.ShowRejectedCommit(reason);
            cardTimeSource?.Cancel();
            DisposeActiveCardSelection();
            ConsumeCardTimeOpportunity();
        }

        private void PublishRejectedCardFeedback()
        {
            if (activeCardSelection == null
                || !activeCardSelection.TryGetSelectedCard(out var card)
                || card == null)
            {
                return;
            }

            gameplayServices?.CardFeedback?.PublishWorldFeedback(
                new CardWorldFeedbackViewModel(
                    card: card,
                    sourceObject: gameObject,
                    kind: CardFeedbackKind.Failed));
        }

        private void CancelActiveCardSelection()
        {
            var hadSelection = activeCardSelection != null;
            var cancelled = cardTimeSource?.Cancel() == true;
            if (!cancelled && !hadSelection)
            {
                return;
            }

            DisposeActiveCardSelection();
            ConsumeCardTimeOpportunity();
        }

        private void SynchronizeActiveCardSelection(CardTimeSessionSnapshot snapshot)
        {
            if (activeCardSelection == null)
            {
                return;
            }

            if (!snapshot.IsActive
                || !activeCardSelection.IsValid
                || activeCardSelection.SessionId != snapshot.ActiveSessionId
                || activeCardSelection.Category != snapshot.SessionCardTime)
            {
                DisposeActiveCardSelection();
            }
        }

        private void ConsumeCardTimeOpportunity() {
            attackCombo.ConsumeCardTime();
            cardTimeSource?.PublishAvailability(PlayerCardTimeState.None);
        }

        private bool TryCreateActiveCardSelection()
        {
            DisposeActiveCardSelection();
            var snapshot = CardTimeSession?.Current ?? default;
            if (!snapshot.IsActive)
            {
                return false;
            }

            var category = snapshot.SessionCardTime;
            var ids = cardInventoryProfile != null
                ? cardInventoryProfile.GetEquippedCardIds(category)
                : null;
            var catalog = (ICardCatalog)cardCatalog;
            if (catalog == null)
            {
                var fallbackCard = cardRuntime != null
                    ? cardRuntime.GetEquippedCard(category)
                    : null;
                if (fallbackCard != null)
                {
                    ids = new[] { fallbackCard.Id };
                    catalog = new SingleCardCatalog(fallbackCard);
                }
            }

            return CardTimeSelectionTransaction.TryCreate(
                category,
                snapshot.ActiveSessionId,
                ids,
                catalog,
                out activeCardSelection)
                && activeCardSelection.TryGetSelectedCard(out _)
                && BindActiveSelectionHud();
        }

        private void DisposeActiveCardSelection()
        {
            cardSelectionHud?.ClearSelection(activeCardSelection);
            cardSelectionInput?.ClearSelection(activeCardSelection);
            activeCardSelection?.Dispose();
            activeCardSelection = null;
        }

        private bool BindActiveSelectionHud()
        {
            cardSelectionHud?.BindSelection(activeCardSelection);
            cardSelectionInput?.BindSelection(activeCardSelection);
            return true;
        }

        private void CompleteCurrentAttack() {
            if (ActionRunner?.CurrentAction is IPlayerAttackExecution attackExecution) {
                combatEffects?.CompleteAttack(attackExecution.ExecutionId);
            }
        }

        private PlayerInputSnapshot ReadInputActions() {
            return new PlayerInputSnapshot(
                move: ReadVector2(reference: moveAction),
                jumpPressed: WasPressed(reference: jumpAction),
                jumpHeld: IsPressed(reference: jumpAction),
                jumpReleased: WasReleased(reference: jumpAction),
                attackPressed: WasPressed(reference: attackAction),
                dashPressed: WasPressed(reference: dashAction));
        }

        private static void EnableAction(
            InputActionReference reference
        ) {
            reference?.action?.Enable();
        }

        private static void DisableAction(
            InputActionReference reference
        ) {
            reference?.action?.Disable();
        }

        private static void EnableAction(
            InputAction action
        ) {
            action?.Enable();
        }

        private static void DisableAction(
            InputAction action
        ) {
            action?.Disable();
        }

        private static Vector2 ReadVector2(
            InputActionReference reference
        ) {
            return reference && reference.action != null
                ? reference.action.ReadValue<Vector2>()
                : Vector2.zero;
        }

        private static bool WasPressed(
            InputActionReference reference
        ) {
            return reference && reference.action != null && reference.action.WasPressedThisFrame();
        }

        private static bool WasPressed(
            InputAction action
        ) {
            return action != null && action.WasPressedThisFrame();
        }

        private static bool IsPressed(
            InputActionReference reference
        ) {
            return reference && reference.action != null && reference.action.IsPressed();
        }

        private static bool IsPressed(
            InputAction action
        ) {
            return action != null && action.IsPressed();
        }

        private static bool WasReleased(
            InputActionReference reference
        ) {
            return reference != null && reference.action != null && reference.action.WasReleasedThisFrame();
        }

        private sealed class SingleCardCatalog : ICardCatalog
        {
            private readonly CardDefinitionSO card;

            public SingleCardCatalog(CardDefinitionSO card)
            {
                this.card = card;
            }

            public bool TryGetCard(string stableId, out CardDefinitionSO definition)
            {
                definition = card != null && card.Id == stableId
                    ? card
                    : null;
                return definition != null;
            }
        }
    }
}
