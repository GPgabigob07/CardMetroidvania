using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerCardTimeRuntime
    {
        private readonly float maximumActiveDuration;
        private readonly float inputBufferDuration;
        private readonly float postWindowGraceDuration;
        private CardTimeSessionState state;
        private PlayerCardTimeState availableCardTime;
        private PlayerCardTimeState publishedCardTime;
        private long availableOpportunityId;
        private long publishedOpportunityId;
        private long consumedOpportunityId;
        private long legacyOpportunityId;
        private PlayerCardTimeState legacyCategory;
        private bool blockedByConsumedOpportunity;
        private CardTimeActiveSession activeSession;
        private long nextSessionId = 1;
        private float inputBufferRemaining;
        private float postWindowGraceRemaining;

        public PlayerCardTimeRuntime(
            float maximumActiveDuration = 5f,
            float inputBufferDuration = 0.15f,
            float postWindowGraceDuration = 0.15f
        ) {
            if (maximumActiveDuration <= 0f
                || float.IsNaN(maximumActiveDuration)
                || float.IsInfinity(maximumActiveDuration)) {
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(maximumActiveDuration),
                    message: "Card Time active duration must be greater than zero.");
            }

            this.maximumActiveDuration = maximumActiveDuration;
            this.inputBufferDuration = ValidateNonNegative(
                value: inputBufferDuration,
                parameterName: nameof(inputBufferDuration));
            this.postWindowGraceDuration = ValidateNonNegative(
                value: postWindowGraceDuration,
                parameterName: nameof(postWindowGraceDuration));
            state = CardTimeSessionState.Unavailable;
        }

        public event Action<CardTimeSessionTransition> Changed;

        public CardTimeSessionSnapshot Current => BuildSnapshot();

        /// <summary>
        /// Compatibility path for category-only EditMode callers. Gameplay publishes explicit opportunities.
        /// </summary>
        public void PublishAvailability(PlayerCardTimeState cardTimeState)
        {
            if (cardTimeState == PlayerCardTimeState.None)
            {
                PublishAvailability(CardTimeOpportunity.None);
                return;
            }

            if (legacyCategory != cardTimeState)
            {
                legacyCategory = cardTimeState;
                legacyOpportunityId++;
            }

            PublishAvailability(new CardTimeOpportunity(cardTimeState, legacyOpportunityId));
        }

        public void PublishAvailability(CardTimeOpportunity opportunity)
        {
            if (opportunity.IsValid && opportunity.OpportunityId <= consumedOpportunityId)
            {
                return;
            }

            var previous = BuildSnapshot();
            var previousPublishedCardTime = publishedCardTime;
            var previousPublishedOpportunityId = publishedOpportunityId;
            publishedCardTime = opportunity.Category;
            publishedOpportunityId = opportunity.OpportunityId;

            if (state == CardTimeSessionState.Active) {
                PublishChangedIfDifferent(previous: previous);
                return;
            }

            if (opportunity.IsValid)
            {
                blockedByConsumedOpportunity = false;
            }
            else if (blockedByConsumedOpportunity)
            {
                return;
            }

            if (opportunity.Category == previousPublishedCardTime
                && opportunity.OpportunityId == previousPublishedOpportunityId
                && state == CardTimeSessionState.Available
                && postWindowGraceRemaining > 0f) {
                return;
            }

            if (ShouldBeginGrace(
                    previous: previousPublishedCardTime,
                    next: opportunity.Category)
                && availableOpportunityId > consumedOpportunityId
                && postWindowGraceDuration > 0f) {
                availableCardTime = previousPublishedCardTime;
                availableOpportunityId = previousPublishedOpportunityId;
                postWindowGraceRemaining = postWindowGraceDuration;
                state = CardTimeSessionState.Available;
                PublishChangedIfDifferent(previous: previous);
                return;
            }

            availableCardTime = opportunity.Category;
            availableOpportunityId = opportunity.OpportunityId;
            postWindowGraceRemaining = 0f;
            state = opportunity.IsValid
                ? CardTimeSessionState.Available
                : CardTimeSessionState.Unavailable;

            if (state == CardTimeSessionState.Available
                && inputBufferRemaining > 0f) {
                inputBufferRemaining = 0f;
                Activate();
                return;
            }

            PublishChangedIfDifferent(previous: previous);
        }

        public bool TryActivate() {
            if (state != CardTimeSessionState.Available
                || availableCardTime == PlayerCardTimeState.None) {
                return false;
            }

            Activate();
            return true;
        }

        public CardTimeActivationRequestResult RequestActivation() {
            if (TryActivate()) {
                return CardTimeActivationRequestResult.Activated;
            }

            if (inputBufferDuration <= 0f
                || state == CardTimeSessionState.Active
                || blockedByConsumedOpportunity) {
                return CardTimeActivationRequestResult.Rejected;
            }

            inputBufferRemaining = inputBufferDuration;
            return CardTimeActivationRequestResult.Buffered;
        }

        public void Tick(
            float unscaledDeltaTime
        ) {
            if (unscaledDeltaTime < 0f
                || float.IsNaN(unscaledDeltaTime)
                || float.IsInfinity(unscaledDeltaTime)) {
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(unscaledDeltaTime),
                    message: "Card Time cannot tick with a negative duration.");
            }

            if (unscaledDeltaTime == 0f) {
                return;
            }

            inputBufferRemaining = Mathf.Max(
                a: 0f,
                b: inputBufferRemaining - unscaledDeltaTime);

            if (state == CardTimeSessionState.Available) {
                if (postWindowGraceRemaining > 0f) {
                    var gracePrevious = BuildSnapshot();
                    postWindowGraceRemaining = Mathf.Max(
                        a: 0f,
                        b: postWindowGraceRemaining - unscaledDeltaTime);

                    if (postWindowGraceRemaining <= 0f) {
                        availableCardTime = publishedCardTime;
                        availableOpportunityId = publishedOpportunityId;
                        state = publishedOpportunityId == 0
                            ? CardTimeSessionState.Unavailable
                            : CardTimeSessionState.Available;
                        PublishChanged(previous: gracePrevious);
                    }
                } else { }

                return;
            }

            if (state != CardTimeSessionState.Active) {
                return;
            }

            var previous = BuildSnapshot();
            activeSession.Tick(unscaledDeltaTime);

            if (activeSession.Elapsed >= activeSession.MaximumDuration) {
                EndSession(
                    outcome: CardTimeSessionOutcome.TimedOut,
                    previous: previous);
                return;
            }

            PublishChanged(previous: previous);
        }

        public bool TryCommit() {
            return TryCommit(AcceptCommitTransaction.Instance);
        }

        public bool TryCommit(ICardCommitTransaction transaction) {
            if (state != CardTimeSessionState.Active) {
                return false;
            }

            if (transaction == null || !transaction.TryApply()) {
                return false;
            }

            EndSession(CardTimeSessionOutcome.Committed);
            return true;
        }

        public bool Cancel() {
            if (state != CardTimeSessionState.Active) {
                return false;
            }

            EndSession(CardTimeSessionOutcome.Cancelled);
            return true;
        }

        private void EndSession(
            CardTimeSessionOutcome outcome
        ) {
            EndSession(
                outcome: outcome,
                previous: BuildSnapshot());
        }

        private void EndSession(
            CardTimeSessionOutcome outcome,
            CardTimeSessionSnapshot previous
        ) {
            consumedOpportunityId = Math.Max(
                consumedOpportunityId,
                activeSession.OpportunityId);
            blockedByConsumedOpportunity = true;
            activeSession = null;
            availableCardTime = PlayerCardTimeState.None;
            availableOpportunityId = 0;
            inputBufferRemaining = 0f;
            postWindowGraceRemaining = 0f;
            state = CardTimeSessionState.Unavailable;
            PublishChanged(
                previous: previous,
                outcome: outcome);
        }

        private void SetState(
            CardTimeSessionState nextState
        ) {
            var previous = BuildSnapshot();
            state = nextState;
            var current = BuildSnapshot();
            if (current != previous) {
                Changed?.Invoke(new CardTimeSessionTransition(
                    previous: previous,
                    current: current));
            }
        }

        private void Activate() {
            activeSession = new CardTimeActiveSession(
                id: nextSessionId++,
                category: availableCardTime,
                opportunityId: availableOpportunityId,
                maximumDuration: maximumActiveDuration);
            postWindowGraceRemaining = 0f;
            inputBufferRemaining = 0f;
            SetState(CardTimeSessionState.Active);
        }

        private void PublishChanged(
            CardTimeSessionSnapshot previous,
            CardTimeSessionOutcome outcome = CardTimeSessionOutcome.None
        ) {
            Changed?.Invoke(new CardTimeSessionTransition(
                previous: previous,
                current: BuildSnapshot(),
                outcome: outcome));
        }

        private void PublishChangedIfDifferent(
            CardTimeSessionSnapshot previous
        ) {
            var current = BuildSnapshot();
            if (current != previous) {
                Changed?.Invoke(new CardTimeSessionTransition(
                    previous: previous,
                    current: current));
            }
        }

        private CardTimeSessionSnapshot BuildSnapshot() {
            var hasActiveSession = state == CardTimeSessionState.Active
                && activeSession != null;
            return new CardTimeSessionSnapshot(
                state: state,
                availableCardTime: availableCardTime,
                sessionCardTime: hasActiveSession
                    ? activeSession.Category
                    : PlayerCardTimeState.None,
                activeElapsed: hasActiveSession
                    ? activeSession.Elapsed
                    : 0f,
                maximumActiveDuration: hasActiveSession
                    ? activeSession.MaximumDuration
                    : maximumActiveDuration,
                activeSessionId: hasActiveSession
                    ? activeSession.Id
                    : 0);
        }

        private static float ValidateNonNegative(
            float value,
            string parameterName
        ) {
            if (value < 0f || float.IsNaN(value) || float.IsInfinity(value)) {
                throw new ArgumentOutOfRangeException(
                    paramName: parameterName,
                    message: "Card Time leniency durations cannot be negative.");
            }

            return value;
        }

        private static bool ShouldBeginGrace(
            PlayerCardTimeState previous,
            PlayerCardTimeState next
        ) {
            if (previous == PlayerCardTimeState.None || previous == next) {
                return false;
            }

            return next is PlayerCardTimeState.None or PlayerCardTimeState.Neutral
                   && previous is PlayerCardTimeState.Chain or PlayerCardTimeState.Finisher;
        }

        public void ClearSource()
        {
            var previous = BuildSnapshot();
            activeSession = null;
            state = CardTimeSessionState.Unavailable;
            availableCardTime = PlayerCardTimeState.None;
            publishedCardTime = PlayerCardTimeState.None;
            availableOpportunityId = 0;
            publishedOpportunityId = 0;
            consumedOpportunityId = 0;
            legacyOpportunityId = 0;
            legacyCategory = PlayerCardTimeState.None;
            blockedByConsumedOpportunity = false;
            inputBufferRemaining = 0f;
            postWindowGraceRemaining = 0f;
            PublishChangedIfDifferent(previous);
        }

        private sealed class AcceptCommitTransaction : ICardCommitTransaction
        {
            public static readonly AcceptCommitTransaction Instance = new();

            public CardDefinitionSO Card => null;
            public PlayerCardTimeState Category => PlayerCardTimeState.None;
            public bool IsApplied { get; private set; }

            public bool TryApply()
            {
                IsApplied = true;
                return true;
            }
        }
    }
}
