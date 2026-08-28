using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerAnimationProjectionTests
    {
        [Test]
        public void Publisher_FirstSnapshotPublishesWithoutPrevious()
        {
            var publisher = new PlayerAnimationSnapshotPublisher();
            var changeCount = 0;
            var publishedTransition = default(PlayerAnimationTransition);
            publisher.Changed += transition =>
            {
                changeCount++;
                publishedTransition = transition;
            };

            var snapshot = Snapshot(
                locomotion: PlayerLocomotionState.Grounded,
                horizontalMotion: PlayerHorizontalMotion.Idle);
            publisher.Publish(snapshot: snapshot);

            Assert.AreEqual(1, changeCount);
            Assert.False(publishedTransition.HasPrevious);
            Assert.AreEqual(snapshot, publishedTransition.Current);
            Assert.AreEqual(snapshot, publisher.Current);
        }

        [Test]
        public void Publisher_StructurallyEqualSnapshotRefreshesMetricsWithoutPublishing()
        {
            var publisher = new PlayerAnimationSnapshotPublisher();
            var changeCount = 0;
            publisher.Changed += _ => changeCount++;

            publisher.Publish(Snapshot(
                locomotion: PlayerLocomotionState.Airborne,
                verticalMotion: PlayerVerticalMotion.Falling,
                verticalSpeed: -5f));
            publisher.Publish(Snapshot(
                locomotion: PlayerLocomotionState.Airborne,
                verticalMotion: PlayerVerticalMotion.Falling,
                verticalSpeed: -18f));

            Assert.AreEqual(1, changeCount);
            Assert.AreEqual(-18f, publisher.Current.VerticalSpeed);
        }

        [Test]
        public void Mapper_IdleToMovingUsesWalkBeginThenWalkLoop()
        {
            var mapper = new PlayerAnimationMapper();
            var transition = new PlayerAnimationTransition(
                previous: Snapshot(
                    locomotion: PlayerLocomotionState.Grounded,
                    horizontalMotion: PlayerHorizontalMotion.Idle),
                current: Snapshot(
                    locomotion: PlayerLocomotionState.Grounded,
                    horizontalMotion: PlayerHorizontalMotion.Moving),
                hasPrevious: true);

            var command = mapper.Map(transition: transition);

            Assert.AreEqual(PlayerAnimationState.WalkBegin, command.State);
            Assert.True(command.HasFallback);
            Assert.AreEqual(PlayerAnimationState.WalkLoop, command.FallbackState);
            Assert.True(command.Restart);
        }

        [Test]
        public void Mapper_RisingAndFallingAirborneStatesResolveExactly()
        {
            var mapper = new PlayerAnimationMapper();

            var rising = mapper.Map(new PlayerAnimationTransition(
                previous: default,
                current: Snapshot(
                    locomotion: PlayerLocomotionState.Airborne,
                    verticalMotion: PlayerVerticalMotion.Rising),
                hasPrevious: false));
            var falling = mapper.Map(new PlayerAnimationTransition(
                previous: Snapshot(
                    locomotion: PlayerLocomotionState.Airborne,
                    verticalMotion: PlayerVerticalMotion.Rising),
                current: Snapshot(
                    locomotion: PlayerLocomotionState.Airborne,
                    verticalMotion: PlayerVerticalMotion.Falling),
                hasPrevious: true));

            Assert.AreEqual(PlayerAnimationState.JumpUp, rising.State);
            Assert.AreEqual(PlayerAnimationState.Fall, falling.State);
        }

        [Test]
        public void Mapper_LandingUsesLatestAirborneSpeed()
        {
            var publisher = new PlayerAnimationSnapshotPublisher();
            var mapper = new PlayerAnimationMapper(hardLandingSpeed: 14f);
            var command = default(PlayerAnimationCommand);
            publisher.Changed += transition => command = mapper.Map(transition: transition);

            publisher.Publish(Snapshot(
                locomotion: PlayerLocomotionState.Airborne,
                verticalMotion: PlayerVerticalMotion.Falling,
                verticalSpeed: -5f));
            publisher.Publish(Snapshot(
                locomotion: PlayerLocomotionState.Airborne,
                verticalMotion: PlayerVerticalMotion.Falling,
                verticalSpeed: -20f));
            publisher.Publish(Snapshot(
                locomotion: PlayerLocomotionState.Grounded,
                horizontalMotion: PlayerHorizontalMotion.Idle));

            Assert.AreEqual(PlayerAnimationState.HardLanding, command.State);
            Assert.True(command.HasFallback);
            Assert.AreEqual(PlayerAnimationState.Idle, command.FallbackState);
        }

        [Test]
        public void Mapper_AttackOverridesGroundedMovement()
        {
            var mapper = new PlayerAnimationMapper();
            var transition = new PlayerAnimationTransition(
                previous: Snapshot(
                    locomotion: PlayerLocomotionState.Grounded,
                    horizontalMotion: PlayerHorizontalMotion.Moving),
                current: Snapshot(
                    locomotion: PlayerLocomotionState.Grounded,
                    action: PlayerActionState.Attack1,
                    actionPhase: PlayerActionPhase.Execution,
                    horizontalMotion: PlayerHorizontalMotion.Moving),
                hasPrevious: true);

            var command = mapper.Map(transition: transition);

            Assert.AreEqual(PlayerAnimationState.Attack1Execution, command.State);
        }

        [TestCase(PlayerActionPhase.Reading, PlayerAnimationState.Attack1Reading)]
        [TestCase(PlayerActionPhase.Execution, PlayerAnimationState.Attack1Execution)]
        [TestCase(PlayerActionPhase.Recovery, PlayerAnimationState.Attack1Recovery)]
        public void AttackMapper_Attack1ResolvesCurrentPhase(
            PlayerActionPhase phase,
            PlayerAnimationState expectedState)
        {
            var mapper = new PlayerAttackAnimationMapper();
            var snapshot = Snapshot(
                locomotion: PlayerLocomotionState.Grounded,
                action: PlayerActionState.Attack1,
                actionPhase: phase);

            var state = mapper.Map(snapshot: snapshot);

            Assert.AreEqual(expectedState, state);
        }

        [Test]
        public void AttackMapper_NonAttackActionReturnsIdle()
        {
            var mapper = new PlayerAttackAnimationMapper();
            var snapshot = Snapshot(
                locomotion: PlayerLocomotionState.Airborne,
                action: PlayerActionState.Dash);

            var state = mapper.Map(snapshot: snapshot);

            Assert.AreEqual(PlayerAnimationState.Idle, state);
        }

        [TestCase(PlayerActionState.Attack1, PlayerActionState.Attack2)]
        [TestCase(PlayerActionState.Attack2, PlayerActionState.Attack3)]
        [TestCase(PlayerActionState.Attack3, PlayerActionState.None)]
        [TestCase(PlayerActionState.Dash, PlayerActionState.None)]
        public void AttackSequence_ReturnsExpectedNextState(
            PlayerActionState current,
            PlayerActionState expectedNext)
        {
            Assert.AreEqual(expectedNext, PlayerAttackSequence.GetNext(current: current));
        }

        [TestCase(0.49f, false)]
        [TestCase(0.5f, true)]
        [TestCase(1f, true)]
        public void ActionFrame_ExecutionBufferUsesNormalizedThreshold(
            float normalizedTime,
            bool expected)
        {
            var behaviour = ScriptableObject.CreateInstance<PlayerActionAnimationStateBehaviour>();
            SetPrivateField(behaviour, "phase", PlayerActionPhase.Execution);
            SetPrivateField(behaviour, "supportsChainBuffer", true);
            SetPrivateField(behaviour, "chainBufferStartNormalized", 0.5f);

            var frame = behaviour.BuildFrame(normalizedTime: normalizedTime);

            Assert.AreEqual(expected, frame.CanBufferFollowUp);
            Assert.False(frame.CanCommitFollowUp);
            Object.DestroyImmediate(obj: behaviour);
        }

        [TestCase(0.49f, false)]
        [TestCase(0.5f, true)]
        [TestCase(1f, true)]
        public void ActionFrame_RecoveryCommitUsesNormalizedThreshold(
            float normalizedTime,
            bool expected)
        {
            var behaviour = ScriptableObject.CreateInstance<PlayerActionAnimationStateBehaviour>();
            SetPrivateField(behaviour, "phase", PlayerActionPhase.Recovery);
            SetPrivateField(behaviour, "supportsChainBuffer", true);
            SetPrivateField(behaviour, "chainBufferStartNormalized", 0f);
            SetPrivateField(behaviour, "supportsFollowUpCommit", true);
            SetPrivateField(behaviour, "followUpCommitStartNormalized", 0.5f);
            SetPrivateField(behaviour, "postRecoveryBufferGraceDuration", 0.5f);
            SetPrivateField(behaviour, "sequenceRestartCooldown", 0.5f);

            var frame = behaviour.BuildFrame(normalizedTime: normalizedTime);

            Assert.True(frame.CanBufferFollowUp);
            Assert.AreEqual(expected, frame.CanCommitFollowUp);
            Assert.AreEqual(0.5f, frame.PostRecoveryBufferGraceDuration);
            Assert.AreEqual(0.5f, frame.SequenceRestartCooldown);
            Object.DestroyImmediate(obj: behaviour);
        }

        [Test]
        public void AttackCombo_BufferedFollowUpCommitsOnlyWhenAllowed()
        {
            var combo = new PlayerAttackComboRuntime();
            var source = new ChainBufferSource(canBuffer: true, canCommit: false);

            Assert.True(combo.TryBuffer(
                currentState: PlayerActionState.Attack1,
                chainBufferSource: source));
            Assert.False(combo.TryConsume(
                chainBufferSource: source,
                followUpState: out _));

            source.CanCommitFollowUp = true;

            Assert.True(combo.TryConsume(
                chainBufferSource: source,
                followUpState: out var followUp));
            Assert.AreEqual(PlayerActionState.Attack2, followUp);
            Assert.False(combo.HasBufferedFollowUp);
        }

        [Test]
        public void AttackCombo_InputDuringPostRecoveryGraceContinuesSequence()
        {
            var combo = new PlayerAttackComboRuntime();
            combo.NotifyAttackCompleted(
                completedState: PlayerActionState.Attack1,
                postRecoveryBufferGraceDuration: 0.5f,
                sequenceRestartCooldown: 0.5f);
            combo.Tick(deltaTime: 0.49f);

            Assert.True(combo.TryResolveIdleAttack(attackState: out var attackState));
            Assert.AreEqual(PlayerActionState.Attack2, attackState);
        }

        [Test]
        public void AttackCombo_InputAfterCooldownRestartsSequence()
        {
            var combo = new PlayerAttackComboRuntime();
            combo.NotifyAttackCompleted(
                completedState: PlayerActionState.Attack1,
                postRecoveryBufferGraceDuration: 0.5f,
                sequenceRestartCooldown: 0.5f);
            combo.Tick(deltaTime: 0.5f);

            Assert.True(combo.TryResolveIdleAttack(attackState: out var attackState));
            Assert.AreEqual(PlayerActionState.Attack1, attackState);
        }

        [Test]
        public void AttackCombo_Attack3WaitsForRestartCooldown()
        {
            var combo = new PlayerAttackComboRuntime();
            combo.NotifyAttackCompleted(
                completedState: PlayerActionState.Attack3,
                postRecoveryBufferGraceDuration: 0.5f,
                sequenceRestartCooldown: 0.5f);

            Assert.False(combo.TryResolveIdleAttack(attackState: out _));

            combo.Tick(deltaTime: 0.5f);

            Assert.True(combo.TryResolveIdleAttack(attackState: out var attackState));
            Assert.AreEqual(PlayerActionState.Attack1, attackState);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(
                name: fieldName,
                bindingAttr: System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.NonPublic);

            Assert.NotNull(field, message: $"Missing private field '{fieldName}'.");
            field.SetValue(obj: target, value: value);
        }

        private sealed class ChainBufferSource : IPlayerChainBufferSource
        {
            public ChainBufferSource(bool canBuffer, bool canCommit)
            {
                CanBufferFollowUp = canBuffer;
                CanCommitFollowUp = canCommit;
            }

            public bool CanBufferFollowUp { get; }
            public bool CanCommitFollowUp { get; set; }
            public float PostRecoveryBufferGraceDuration => 0.5f;
            public float SequenceRestartCooldown => 0.5f;
        }

        private static PlayerAnimationSnapshot Snapshot(
            PlayerLocomotionState locomotion,
            PlayerActionState action = PlayerActionState.None,
            PlayerActionPhase actionPhase = PlayerActionPhase.Reading,
            PlayerHorizontalMotion horizontalMotion = PlayerHorizontalMotion.Idle,
            PlayerVerticalMotion verticalMotion = PlayerVerticalMotion.Stable,
            PlayerCardTimeState cardTime = PlayerCardTimeState.None,
            int facingDirection = 1,
            float verticalSpeed = 0f)
        {
            return new PlayerAnimationSnapshot(
                locomotion: locomotion,
                action: action,
                actionPhase: actionPhase,
                horizontalMotion: horizontalMotion,
                verticalMotion: verticalMotion,
                cardTime: cardTime,
                facingDirection: facingDirection,
                verticalSpeed: verticalSpeed);
        }
    }
}
