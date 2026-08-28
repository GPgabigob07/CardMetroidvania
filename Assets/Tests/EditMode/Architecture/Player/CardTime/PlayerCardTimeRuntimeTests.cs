using NUnit.Framework;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerCardTimeRuntimeTests
    {
        [Test]
        public void Activate_WhenUnavailable_IsRejected()
        {
            var runtime = new PlayerCardTimeRuntime();

            Assert.False(runtime.TryActivate());
            Assert.AreEqual(CardTimeSessionState.Unavailable, runtime.Current.State);
        }

        [TestCase(PlayerCardTimeState.Neutral)]
        [TestCase(PlayerCardTimeState.Chain)]
        [TestCase(PlayerCardTimeState.Finisher)]
        public void Activate_WhenAvailable_StartsSession(PlayerCardTimeState cardTimeState)
        {
            var runtime = new PlayerCardTimeRuntime();
            runtime.PublishAvailability(cardTimeState: cardTimeState);

            Assert.True(runtime.TryActivate());
            Assert.AreEqual(CardTimeSessionState.Active, runtime.Current.State);
            Assert.AreEqual(cardTimeState, runtime.Current.SessionCardTime);
            Assert.Greater(runtime.Current.ActiveSessionId, 0);
        }

        [Test]
        public void ActiveSession_OutlivesAvailabilityWindow()
        {
            var runtime = new PlayerCardTimeRuntime();
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);
            runtime.TryActivate();
            var sessionId = runtime.Current.ActiveSessionId;

            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.None);

            Assert.True(runtime.Current.IsActive);
            Assert.AreEqual(PlayerCardTimeState.Chain, runtime.Current.SessionCardTime);
            Assert.AreEqual(sessionId, runtime.Current.ActiveSessionId);
        }

        [Test]
        public void Tick_UsesSuppliedUnscaledDelta_AndTimesOut()
        {
            var runtime = new PlayerCardTimeRuntime(maximumActiveDuration: 5f);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Neutral);
            runtime.TryActivate();
            var transition = default(CardTimeSessionTransition);
            runtime.Changed += value => transition = value;

            runtime.Tick(unscaledDeltaTime: 4.9f);
            Assert.AreEqual(CardTimeSessionState.Active, runtime.Current.State);
            Assert.AreEqual(0.1f, runtime.Current.ActiveRemaining, 0.0001f);

            runtime.Tick(unscaledDeltaTime: 0.1f);
            Assert.AreEqual(CardTimeSessionState.Unavailable, runtime.Current.State);
            Assert.AreEqual(CardTimeSessionOutcome.TimedOut, transition.Outcome);
            Assert.AreEqual(0f, runtime.Current.ActiveRemaining);
        }

        [Test]
        public void TimedOutNeutral_ReopensFromRepeatedNeutralPublication()
        {
            var runtime = new PlayerCardTimeRuntime(maximumActiveDuration: 1f);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Neutral);
            runtime.TryActivate();

            runtime.Tick(unscaledDeltaTime: 1f);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Neutral);

            Assert.AreEqual(CardTimeSessionState.Available, runtime.Current.State);
            Assert.AreEqual(PlayerCardTimeState.Neutral, runtime.Current.AvailableCardTime);
            Assert.AreEqual(
                CardTimeActivationRequestResult.Activated,
                runtime.RequestActivation());
        }

        [Test]
        public void TimedOutAttackWindow_ReopensWhenSourceReturnsToNeutral()
        {
            var runtime = new PlayerCardTimeRuntime(maximumActiveDuration: 1f);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);
            runtime.TryActivate();

            runtime.Tick(unscaledDeltaTime: 1f);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);
            Assert.AreEqual(CardTimeSessionState.Unavailable, runtime.Current.State);

            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Neutral);

            Assert.AreEqual(CardTimeSessionState.Available, runtime.Current.State);
            Assert.AreEqual(PlayerCardTimeState.Neutral, runtime.Current.AvailableCardTime);
        }

        [Test]
        public void Commit_EndsActiveSession()
        {
            var runtime = new PlayerCardTimeRuntime();
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Finisher);
            runtime.TryActivate();

            var transition = default(CardTimeSessionTransition);
            runtime.Changed += value => transition = value;

            Assert.True(runtime.TryCommit());
            Assert.AreEqual(CardTimeSessionState.Unavailable, runtime.Current.State);
            Assert.AreEqual(CardTimeSessionOutcome.Committed, transition.Outcome);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                runtime.Current.AvailableCardTime);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                runtime.Current.SessionCardTime);
            Assert.AreEqual(0, runtime.Current.ActiveSessionId);
            Assert.AreEqual(0f, runtime.Current.ActiveRemaining);
            Assert.False(runtime.TryCommit());
        }

        [Test]
        public void TransactionCommit_FailureLeavesActiveSession()
        {
            var runtime = new PlayerCardTimeRuntime();
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);
            runtime.TryActivate();
            var sessionId = runtime.Current.ActiveSessionId;
            var transitionCount = 0;
            runtime.Changed += transition =>
            {
                if (transition.Outcome == CardTimeSessionOutcome.Committed)
                {
                    transitionCount++;
                }
            };
            var transaction = new StubCommitTransaction(shouldApply: false);

            Assert.IsFalse(runtime.TryCommit(transaction));

            Assert.IsTrue(runtime.Current.IsActive);
            Assert.AreEqual(sessionId, runtime.Current.ActiveSessionId);
            Assert.AreEqual(0, transitionCount);
            Assert.AreEqual(1, transaction.ApplyCount);
        }

        [Test]
        public void TransactionCommit_SuccessInvokesTransactionOnceAndCommits()
        {
            var runtime = new PlayerCardTimeRuntime();
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);
            runtime.TryActivate();
            var transaction = new StubCommitTransaction(shouldApply: true);

            Assert.IsTrue(runtime.TryCommit(transaction));

            Assert.IsFalse(runtime.Current.IsActive);
            Assert.AreEqual(CardTimeSessionState.Unavailable, runtime.Current.State);
            Assert.AreEqual(1, transaction.ApplyCount);
        }

        [Test]
        public void Cancel_EndsActiveSession()
        {
            var runtime = new PlayerCardTimeRuntime();
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);
            runtime.TryActivate();

            var transition = default(CardTimeSessionTransition);
            runtime.Changed += value => transition = value;

            Assert.True(runtime.Cancel());
            Assert.AreEqual(CardTimeSessionState.Unavailable, runtime.Current.State);
            Assert.AreEqual(CardTimeSessionOutcome.Cancelled, transition.Outcome);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                runtime.Current.AvailableCardTime);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                runtime.Current.SessionCardTime);
            Assert.AreEqual(0, runtime.Current.ActiveSessionId);
            Assert.AreEqual(0f, runtime.Current.ActiveRemaining);
        }

        [Test]
        public void Changed_PublishesPreviousAndCurrentSnapshots()
        {
            var runtime = new PlayerCardTimeRuntime();
            var transitionCount = 0;
            var transition = default(CardTimeSessionTransition);
            runtime.Changed += value =>
            {
                transitionCount++;
                transition = value;
            };

            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Neutral);

            Assert.AreEqual(1, transitionCount);
            Assert.AreEqual(CardTimeSessionState.Unavailable, transition.Previous.State);
            Assert.AreEqual(CardTimeSessionState.Available, transition.Current.State);
        }

        [Test]
        public void Changed_PublishesWhenAvailableCardTimeChanges()
        {
            var runtime = new PlayerCardTimeRuntime();
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Neutral);
            var transitionCount = 0;
            runtime.Changed += _ => transitionCount++;

            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);

            Assert.AreEqual(1, transitionCount);
            Assert.AreEqual(PlayerCardTimeState.Chain, runtime.Current.AvailableCardTime);
        }

        [Test]
        public void PublishAvailability_AfterTerminalOutcome_RefreshesState()
        {
            var runtime = new PlayerCardTimeRuntime();
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Neutral);
            runtime.TryActivate();
            runtime.TryCommit();

            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);

            Assert.AreEqual(CardTimeSessionState.Available, runtime.Current.State);
            Assert.AreEqual(PlayerCardTimeState.Chain, runtime.Current.AvailableCardTime);
            Assert.AreEqual(PlayerCardTimeState.None, runtime.Current.SessionCardTime);
        }

        [Test]
        public void PublishAvailability_SameWindowAfterCommit_DoesNotReactivate()
        {
            var runtime = new PlayerCardTimeRuntime();
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);
            runtime.TryActivate();
            runtime.TryCommit();

            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);

            Assert.AreEqual(CardTimeSessionState.Unavailable, runtime.Current.State);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                runtime.Current.AvailableCardTime);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                runtime.Current.SessionCardTime);
            Assert.False(runtime.TryActivate());
        }

        [Test]
        public void PublishAvailability_TransientCloseAfterCommit_DoesNotReleaseTerminalLatch()
        {
            var runtime = new PlayerCardTimeRuntime();
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);
            runtime.TryActivate();
            runtime.TryCommit();
            var transitionCount = 0;
            runtime.Changed += _ => transitionCount++;

            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.None);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);

            Assert.AreEqual(0, transitionCount);
            Assert.AreEqual(CardTimeSessionState.Unavailable, runtime.Current.State);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                runtime.Current.AvailableCardTime);
            Assert.AreEqual(
                PlayerCardTimeState.None,
                runtime.Current.SessionCardTime);
            Assert.False(runtime.TryActivate());
        }

        [Test]
        public void PublishAvailability_DifferentCategoryAfterCommit_ReleasesTerminalLatch()
        {
            var runtime = new PlayerCardTimeRuntime();
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Finisher);
            runtime.TryActivate();
            var committedSessionId = runtime.Current.ActiveSessionId;
            runtime.TryCommit();

            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.None);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Neutral);

            Assert.AreEqual(CardTimeSessionState.Available, runtime.Current.State);
            Assert.AreEqual(PlayerCardTimeState.Neutral, runtime.Current.AvailableCardTime);
            Assert.True(runtime.TryActivate());
            Assert.Greater(runtime.Current.ActiveSessionId, committedSessionId);
        }

        [Test]
        public void ActiveSession_RepeatedAvailabilityDoesNotReplaceSessionOrResetTimer()
        {
            var runtime = new PlayerCardTimeRuntime(maximumActiveDuration: 5f);
            runtime.PublishAvailability(PlayerCardTimeState.Finisher);
            runtime.TryActivate();
            runtime.Tick(2f);
            var sessionId = runtime.Current.ActiveSessionId;

            runtime.PublishAvailability(PlayerCardTimeState.None);
            runtime.PublishAvailability(PlayerCardTimeState.Finisher);

            Assert.AreEqual(sessionId, runtime.Current.ActiveSessionId);
            Assert.AreEqual(2f, runtime.Current.ActiveElapsed);
            Assert.AreEqual(3f, runtime.Current.ActiveRemaining);
        }

        [Test]
        public void RequestActivation_BeforeWindow_BuffersAndActivatesWhenWindowOpens()
        {
            var runtime = new PlayerCardTimeRuntime(
                inputBufferDuration: 0.15f,
                postWindowGraceDuration: 0.15f);

            var result = runtime.RequestActivation();
            runtime.Tick(unscaledDeltaTime: 0.1f);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);

            Assert.AreEqual(CardTimeActivationRequestResult.Buffered, result);
            Assert.True(runtime.Current.IsActive);
            Assert.AreEqual(PlayerCardTimeState.Chain, runtime.Current.SessionCardTime);
        }

        [Test]
        public void BufferedActivation_ExpiresBeforeLateWindow()
        {
            var runtime = new PlayerCardTimeRuntime(inputBufferDuration: 0.15f);

            runtime.RequestActivation();
            runtime.Tick(unscaledDeltaTime: 0.16f);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);

            Assert.True(runtime.Current.IsAvailable);
            Assert.False(runtime.Current.IsActive);
        }

        [Test]
        public void WindowClose_PreservesLateGraceAcrossRepeatedNonePublications()
        {
            var runtime = new PlayerCardTimeRuntime(postWindowGraceDuration: 0.15f);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Finisher);

            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.None);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.None);
            runtime.Tick(unscaledDeltaTime: 0.1f);

            Assert.True(runtime.Current.IsAvailable);
            Assert.AreEqual(
                CardTimeActivationRequestResult.Activated,
                runtime.RequestActivation());
            Assert.AreEqual(PlayerCardTimeState.Finisher, runtime.Current.SessionCardTime);
        }

        [Test]
        public void WindowGrace_ExpiresAfterConfiguredDuration()
        {
            var runtime = new PlayerCardTimeRuntime(postWindowGraceDuration: 0.15f);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.None);

            runtime.Tick(unscaledDeltaTime: 0.15f);

            Assert.AreEqual(CardTimeSessionState.Unavailable, runtime.Current.State);
        }

        [Test]
        public void AttackWindow_ToNeutral_PreservesAttackStateUntilGraceExpires()
        {
            var runtime = new PlayerCardTimeRuntime(postWindowGraceDuration: 0.5f);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);

            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Neutral);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Neutral);
            runtime.Tick(unscaledDeltaTime: 0.49f);

            Assert.AreEqual(PlayerCardTimeState.Chain, runtime.Current.AvailableCardTime);

            runtime.Tick(unscaledDeltaTime: 0.01f);

            Assert.AreEqual(PlayerCardTimeState.Neutral, runtime.Current.AvailableCardTime);
            Assert.True(runtime.Current.IsAvailable);
        }

        [Test]
        public void NextAttackWindow_BeforeGraceTick_DoesNotPublishNeutral()
        {
            var runtime = new PlayerCardTimeRuntime(postWindowGraceDuration: 0.5f);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Chain);
            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Neutral);
            runtime.Tick(unscaledDeltaTime: 0.49f);

            var publishedNeutral = false;
            runtime.Changed += transition =>
            {
                publishedNeutral |= transition.Current.AvailableCardTime
                    == PlayerCardTimeState.Neutral;
            };

            runtime.PublishAvailability(cardTimeState: PlayerCardTimeState.Finisher);
            runtime.Tick(unscaledDeltaTime: 0.02f);

            Assert.False(publishedNeutral);
            Assert.AreEqual(PlayerCardTimeState.Finisher, runtime.Current.AvailableCardTime);
        }

        private sealed class StubCommitTransaction : ICardCommitTransaction
        {
            private readonly bool shouldApply;

            public StubCommitTransaction(bool shouldApply)
            {
                this.shouldApply = shouldApply;
            }

            public CardDefinitionSO Card => null;
            public PlayerCardTimeState Category => PlayerCardTimeState.Chain;
            public bool IsApplied { get; private set; }
            public int ApplyCount { get; private set; }

            public bool TryApply()
            {
                ApplyCount++;
                IsApplied = shouldApply;
                return shouldApply;
            }
        }
    }
}
