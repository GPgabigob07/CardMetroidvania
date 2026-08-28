using System.Collections.Generic;
using NUnit.Framework;

namespace TicGame.Architecture.Tests
{
    public sealed class StateMachineTests
    {
        [Test]
        public void TryChangeState_EntersInitialStateOnce()
        {
            var machine = new StateMachine<TestStateId>();
            var state = new RecordingState(TestStateId.First);
            machine.AddState(state);

            var changed = machine.TryChangeState(TestStateId.First);

            Assert.IsTrue(changed);
            Assert.AreEqual(1, state.EnterCount);
            Assert.AreEqual(TestStateId.First, machine.CurrentStateId);
        }

        [Test]
        public void TryChangeState_ExitsCurrentStateBeforeEnteringNextState()
        {
            var log = new List<string>();
            var machine = new StateMachine<TestStateId>();
            var first = new RecordingState(TestStateId.First, log);
            var second = new RecordingState(TestStateId.Second, log);
            machine.AddState(first);
            machine.AddState(second);
            machine.TryChangeState(TestStateId.First);
            log.Clear();

            machine.TryChangeState(TestStateId.Second);

            CollectionAssert.AreEqual(new[] { "First.Exit", "Second.Enter" }, log);
        }

        [Test]
        public void TryChangeState_CurrentStateDoesNotRestartByDefault()
        {
            var machine = new StateMachine<TestStateId>();
            var state = new RecordingState(TestStateId.First);
            machine.AddState(state);
            machine.TryChangeState(TestStateId.First);

            var changed = machine.TryChangeState(TestStateId.First);

            Assert.IsTrue(changed);
            Assert.AreEqual(1, state.EnterCount);
            Assert.AreEqual(0, state.ExitCount);
        }

        [Test]
        public void TryChangeState_CurrentStateRestartsWhenRequested()
        {
            var log = new List<string>();
            var machine = new StateMachine<TestStateId>();
            var state = new RecordingState(TestStateId.First, log);
            machine.AddState(state);
            machine.TryChangeState(TestStateId.First);
            log.Clear();

            var changed = machine.TryChangeState(TestStateId.First, restart: true);

            Assert.IsTrue(changed);
            CollectionAssert.AreEqual(new[] { "First.Exit", "First.Enter" }, log);
        }

        [Test]
        public void TickAndFixedTick_ForwardToActiveState()
        {
            var machine = new StateMachine<TestStateId>();
            var state = new RecordingState(TestStateId.First);
            machine.AddState(state);
            machine.TryChangeState(TestStateId.First);

            machine.Tick(0.25f);
            machine.FixedTick(0.5f);

            Assert.AreEqual(0.25f, state.LastTickDelta);
            Assert.AreEqual(0.5f, state.LastFixedTickDelta);
        }

        [Test]
        public void ForwardAnimationNotifications_ReachAnimationAwareActiveState()
        {
            var machine = new StateMachine<TestStateId>();
            var state = new AnimationRecordingState(TestStateId.First);
            machine.AddState(state);
            machine.TryChangeState(TestStateId.First);

            machine.ForwardAnimationEvent("AttackActive");
            machine.ForwardAnimationFinished();

            Assert.AreEqual("AttackActive", state.LastAnimationEvent);
            Assert.AreEqual(1, state.AnimationFinishedCount);
        }

        [Test]
        public void ForwardAnimationNotifications_AreIgnoredByRegularState()
        {
            var machine = new StateMachine<TestStateId>();
            var state = new RecordingState(TestStateId.First);
            machine.AddState(state);
            machine.TryChangeState(TestStateId.First);

            Assert.DoesNotThrow(() =>
            {
                machine.ForwardAnimationEvent("AttackActive");
                machine.ForwardAnimationFinished();
            });
        }

        [Test]
        public void TryChangeState_MissingStateKeepsCurrentState()
        {
            var machine = new StateMachine<TestStateId>();
            var state = new RecordingState(TestStateId.First);
            machine.AddState(state);
            machine.TryChangeState(TestStateId.First);

            var changed = machine.TryChangeState(TestStateId.Second);

            Assert.IsFalse(changed);
            Assert.AreEqual(TestStateId.First, machine.CurrentStateId);
            Assert.AreEqual(0, state.ExitCount);
        }

        [Test]
        public void AddState_OwnedStateSuppliesOwnerWhenStateEnters()
        {
            var owner = new TestOwner();
            var machine = new StateMachine<TestStateId>();
            var state = new OwnedRecordingState(TestStateId.First);
            machine.AddState(state, owner);

            machine.TryChangeState(TestStateId.First);

            Assert.AreSame(owner, state.EnteredOwner);
            Assert.AreEqual(1, state.EnterCount);
        }

        private enum TestStateId
        {
            First,
            Second
        }

        private sealed class TestOwner
        {
        }

        private class RecordingState : IState<TestStateId>
        {
            private readonly List<string> log;

            public RecordingState(TestStateId id, List<string> log = null)
            {
                Id = id;
                this.log = log;
            }

            public TestStateId Id { get; }
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }
            public float LastTickDelta { get; private set; }
            public float LastFixedTickDelta { get; private set; }

            public void Enter()
            {
                EnterCount++;
                log?.Add($"{Id}.Enter");
            }

            public void Tick(float deltaTime)
            {
                LastTickDelta = deltaTime;
            }

            public void FixedTick(float fixedDeltaTime)
            {
                LastFixedTickDelta = fixedDeltaTime;
            }

            public void Exit()
            {
                ExitCount++;
                log?.Add($"{Id}.Exit");
            }
        }

        private sealed class AnimationRecordingState : RecordingState, IAnimationAwareState
        {
            public AnimationRecordingState(TestStateId id)
                : base(id)
            {
            }

            public string LastAnimationEvent { get; private set; }
            public int AnimationFinishedCount { get; private set; }

            public void OnAnimationEvent(string eventName)
            {
                LastAnimationEvent = eventName;
            }

            public void OnAnimationFinished()
            {
                AnimationFinishedCount++;
            }
        }

        private sealed class OwnedRecordingState : OwnedState<TestStateId, TestOwner>
        {
            public OwnedRecordingState(TestStateId id)
            {
                Id = id;
            }

            public override TestStateId Id { get; }
            public TestOwner EnteredOwner { get; private set; }
            public int EnterCount { get; private set; }

            protected override void OnEnter()
            {
                EnteredOwner = Owner;
                EnterCount++;
            }
        }
    }
}
