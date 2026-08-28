using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class AttackAction :
        IPlayerAction,
        ILocomotionOverride,
        IPlayerActionAnimationSource,
        IPlayerChainBufferSource,
        IPlayerAttackHitConfirmation,
        IPlayerAttackExecution
    {
        private float elapsed;
        private PlayerContext context;
        private PlayerActionPhase timerPhase;
        private float timerPhaseProgress;

        public AttackAction(PlayerActionState state)
        {
            State = state;
        }

        public PlayerActionState State { get; }
        public string ExecutionId { get; } = Guid.NewGuid().ToString(format: "N");
        public bool IsComplete { get; private set; }
        public PlayerActionPhase CurrentPhase { get; private set; }
        public PlayerCardTimeState CurrentCardTime { get; private set; }
        public PlayerActionPhase AnimationPhase => CurrentPhase;
        public PlayerCardTimeState AnimationCardTime => CurrentCardTime;
        public bool CanBufferFollowUp => ResolveCanBufferFollowUp();
        public bool CanCommitFollowUp => ResolveCanCommitFollowUp();
        public float PostRecoveryBufferGraceDuration => ResolvePostRecoveryBufferGraceDuration();
        public float SequenceRestartCooldown => ResolveSequenceRestartCooldown();
        public bool HasConfirmedHit { get; private set; }

        public void Enter(PlayerContext context)
        {
            this.context = context;
            elapsed = 0f;
            timerPhase = PlayerActionPhase.Reading;
            timerPhaseProgress = 0f;
            CurrentPhase = PlayerActionPhase.Reading;
            CurrentCardTime = PlayerCardTimeState.None;
            IsComplete = false;
            HasConfirmedHit = false;
        }

        public void Tick(PlayerContext context, float deltaTime)
        {
            elapsed += deltaTime;

            if (context.ActionFrame.HasAnimatorAuthority)
            {
                CurrentPhase = context.ActionFrame.Phase;
                CurrentCardTime = context.ActionFrame.CardTimeState;
                IsComplete = context.ActionFrame.EndAction;
                return;
            }

            timerPhase = ResolveTimerPhase(
                definition: context.AttackDefinition,
                elapsed: elapsed,
                normalizedPhaseTime: out timerPhaseProgress);
            CurrentPhase = timerPhase;
            CurrentCardTime = PlayerCardTimeState.None;
            IsComplete = elapsed >= context.AttackDefinition.TotalDuration;
        }

        public void FixedTick(PlayerContext context, float fixedDeltaTime)
        {
        }

        public void Exit(PlayerContext context)
        {
            this.context = null;
        }

        public void ConfirmHit()
        {
            HasConfirmedHit = true;
        }

        public void ModifyLocomotionFrame(
            ref LocomotionFrame frame,
            PlayerContext context,
            float fixedDeltaTime)
        {
            var definition = context.AttackDefinition;
            frame.LockFacing = true;

            if (context.Locomotion.CurrentStateId == PlayerLocomotionState.Grounded)
            {
                frame.Velocity.x *= definition.GroundedHorizontalMultiplier;

                if (CurrentPhase == PlayerActionPhase.Execution)
                {
                    frame.Velocity.x += context.FacingDirection * definition.GroundedExecutionNudge;
                }

                return;
            }

            if (CurrentPhase != PlayerActionPhase.Execution || !HasConfirmedHit)
            {
                return;
            }

            frame.GravityScale *= definition.AirborneExecutionGravityMultiplier;
            frame.Velocity.y = Mathf.Max(a: frame.Velocity.y, b: definition.AirborneExecutionMinLift);
        }

        private bool ResolveCanBufferFollowUp()
        {
            if (context != null && context.ActionFrame.HasAnimatorAuthority)
            {
                return context.ActionFrame.CanBufferFollowUp;
            }

            return timerPhase == PlayerActionPhase.Execution && timerPhaseProgress >= 0.5f
                || timerPhase == PlayerActionPhase.Recovery;
        }

        private bool ResolveCanCommitFollowUp()
        {
            if (context != null && context.ActionFrame.HasAnimatorAuthority)
            {
                return context.ActionFrame.CanCommitFollowUp;
            }

            return timerPhase == PlayerActionPhase.Recovery && timerPhaseProgress >= 0.5f;
        }

        private float ResolvePostRecoveryBufferGraceDuration()
        {
            if (context != null && context.ActionFrame.HasAnimatorAuthority)
            {
                return context.ActionFrame.PostRecoveryBufferGraceDuration;
            }

            return context?.AttackDefinition.PostRecoveryBufferGraceDuration ?? 0f;
        }

        private float ResolveSequenceRestartCooldown()
        {
            if (context != null && context.ActionFrame.HasAnimatorAuthority)
            {
                return context.ActionFrame.SequenceRestartCooldown;
            }

            return context?.AttackDefinition.SequenceRestartCooldown ?? 0f;
        }

        private static PlayerActionPhase ResolveTimerPhase(
            PlayerAttackDefinitionSO definition,
            float elapsed,
            out float normalizedPhaseTime)
        {
            if (elapsed < definition.ReadingDuration)
            {
                normalizedPhaseTime = NormalizePhaseTime(
                    elapsed: elapsed,
                    phaseStart: 0f,
                    phaseDuration: definition.ReadingDuration);
                return PlayerActionPhase.Reading;
            }

            if (elapsed < definition.ReadingDuration + definition.ExecutionDuration)
            {
                normalizedPhaseTime = NormalizePhaseTime(
                    elapsed: elapsed,
                    phaseStart: definition.ReadingDuration,
                    phaseDuration: definition.ExecutionDuration);
                return PlayerActionPhase.Execution;
            }

            normalizedPhaseTime = NormalizePhaseTime(
                elapsed: elapsed,
                phaseStart: definition.ReadingDuration + definition.ExecutionDuration,
                phaseDuration: definition.RecoveryDuration);
            return PlayerActionPhase.Recovery;
        }

        private static float NormalizePhaseTime(float elapsed, float phaseStart, float phaseDuration)
        {
            if (phaseDuration <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(value: (elapsed - phaseStart) / phaseDuration);
        }
    }
}
