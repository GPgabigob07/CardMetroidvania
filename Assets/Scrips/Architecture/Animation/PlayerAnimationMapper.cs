using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerAnimationMapper : IPlayerAnimationMapper
    {
        private readonly float hardLandingSpeed;
        private readonly float crossFadeDuration;

        public PlayerAnimationMapper(
            float hardLandingSpeed = 14f,
            float crossFadeDuration = 0f)
        {
            this.hardLandingSpeed = Mathf.Max(a: 0f, b: hardLandingSpeed);
            this.crossFadeDuration = Mathf.Max(a: 0f, b: crossFadeDuration);
        }

        public PlayerAnimationCommand Map(in PlayerAnimationTransition transition)
        {
            var current = transition.Current;

            if (current.Action != PlayerActionState.None)
            {
                return ResolveAction(snapshot: current);
            }

            if (IsLanding(transition: transition))
            {
                var fallback = ResolveGroundedStableState(snapshot: current);
                var impactSpeed = Mathf.Abs(f: transition.Previous.VerticalSpeed);
                var landingState = impactSpeed >= hardLandingSpeed
                    ? PlayerAnimationState.HardLanding
                    : PlayerAnimationState.GroundedFall;

                return Transient(state: landingState, fallback: fallback);
            }

            if (current.Locomotion == PlayerLocomotionState.Airborne)
            {
                return Stable(state: current.VerticalMotion == PlayerVerticalMotion.Rising
                    ? PlayerAnimationState.JumpUp
                    : PlayerAnimationState.Fall);
            }

            if (current.Locomotion == PlayerLocomotionState.WallSlide)
            {
                return Stable(state: PlayerAnimationState.Fall);
            }

            if (StartedMoving(transition: transition))
            {
                return Transient(
                    state: PlayerAnimationState.WalkBegin,
                    fallback: PlayerAnimationState.WalkLoop);
            }

            return Stable(state: ResolveGroundedStableState(snapshot: current));
        }

        private PlayerAnimationCommand ResolveAction(PlayerAnimationSnapshot snapshot)
        {
            switch (snapshot.Action)
            {
                case PlayerActionState.Dead:
                    return Stable(state: PlayerAnimationState.Dead);
                case PlayerActionState.Hurt:
                    return Stable(state: PlayerAnimationState.Hurt);
                case PlayerActionState.Finisher:
                    return Stable(state: PlayerAnimationState.Finisher);
                case PlayerActionState.CardChain:
                    return Stable(state: PlayerAnimationState.CardChain);
                case PlayerActionState.Attack1:
                    return Stable(state: ResolveAttackPhase(
                        phase: snapshot.ActionPhase,
                        reading: PlayerAnimationState.Attack1Reading,
                        execution: PlayerAnimationState.Attack1Execution,
                        recovery: PlayerAnimationState.Attack1Recovery));
                case PlayerActionState.Attack2:
                    return Stable(state: ResolveAttackPhase(
                        phase: snapshot.ActionPhase,
                        reading: PlayerAnimationState.Attack2Reading,
                        execution: PlayerAnimationState.Attack2Execution,
                        recovery: PlayerAnimationState.Attack2Recovery));
                case PlayerActionState.Attack3:
                    return Stable(state: ResolveAttackPhase(
                        phase: snapshot.ActionPhase,
                        reading: PlayerAnimationState.Attack3Reading,
                        execution: PlayerAnimationState.Attack3Execution,
                        recovery: PlayerAnimationState.Attack3Recovery));
                case PlayerActionState.Dash:
                    return Stable(state: PlayerAnimationState.Dash);
                default:
                    return Stable(state: ResolveLocomotionFallback(snapshot: snapshot));
            }
        }

        private PlayerAnimationState ResolveLocomotionFallback(PlayerAnimationSnapshot snapshot)
        {
            if (snapshot.Locomotion == PlayerLocomotionState.Airborne)
            {
                return snapshot.VerticalMotion == PlayerVerticalMotion.Rising
                    ? PlayerAnimationState.JumpUp
                    : PlayerAnimationState.Fall;
            }

            return ResolveGroundedStableState(snapshot: snapshot);
        }

        private static PlayerAnimationState ResolveAttackPhase(
            PlayerActionPhase phase,
            PlayerAnimationState reading,
            PlayerAnimationState execution,
            PlayerAnimationState recovery)
        {
            switch (phase)
            {
                case PlayerActionPhase.Execution:
                    return execution;
                case PlayerActionPhase.Recovery:
                    return recovery;
                default:
                    return reading;
            }
        }

        private static PlayerAnimationState ResolveGroundedStableState(PlayerAnimationSnapshot snapshot)
        {
            return snapshot.HorizontalMotion == PlayerHorizontalMotion.Moving
                ? PlayerAnimationState.WalkLoop
                : PlayerAnimationState.Idle;
        }

        private static bool IsLanding(PlayerAnimationTransition transition)
        {
            return transition.HasPrevious
                && transition.Previous.Locomotion == PlayerLocomotionState.Airborne
                && transition.Current.Locomotion == PlayerLocomotionState.Grounded;
        }

        private static bool StartedMoving(PlayerAnimationTransition transition)
        {
            return transition.HasPrevious
                && transition.Previous.Locomotion == PlayerLocomotionState.Grounded
                && transition.Previous.HorizontalMotion == PlayerHorizontalMotion.Idle
                && transition.Current.Locomotion == PlayerLocomotionState.Grounded
                && transition.Current.HorizontalMotion == PlayerHorizontalMotion.Moving;
        }

        private PlayerAnimationCommand Stable(PlayerAnimationState state)
        {
            return new PlayerAnimationCommand(
                state: state,
                crossFadeDuration: crossFadeDuration,
                restart: false);
        }

        private PlayerAnimationCommand Transient(
            PlayerAnimationState state,
            PlayerAnimationState fallback)
        {
            return new PlayerAnimationCommand(
                state: state,
                fallbackState: fallback,
                hasFallback: true,
                crossFadeDuration: crossFadeDuration,
                restart: true);
        }
    }
}
