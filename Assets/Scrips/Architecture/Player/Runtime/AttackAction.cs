using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class AttackAction : IPlayerAction, ILocomotionOverride
    {
        private float elapsed;
        private PlayerActionPhase timerPhase;

        public AttackAction(PlayerActionState state)
        {
            State = state;
        }

        public PlayerActionState State { get; }
        public bool IsComplete { get; private set; }
        public PlayerActionPhase CurrentPhase { get; private set; }

        public void Enter(PlayerContext context)
        {
            elapsed = 0f;
            timerPhase = PlayerActionPhase.Reading;
            CurrentPhase = PlayerActionPhase.Reading;
            IsComplete = false;
        }

        public void Tick(PlayerContext context, float deltaTime)
        {
            elapsed += deltaTime;

            if (context.ActionFrame.HasAnimatorAuthority)
            {
                CurrentPhase = context.ActionFrame.Phase;
                IsComplete = context.ActionFrame.EndAction;
                return;
            }

            timerPhase = ResolveTimerPhase(definition: context.AttackDefinition, elapsed: elapsed);
            CurrentPhase = timerPhase;
            IsComplete = elapsed >= context.AttackDefinition.TotalDuration;
        }

        public void FixedTick(PlayerContext context, float fixedDeltaTime)
        {
        }

        public void Exit(PlayerContext context)
        {
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

            if (CurrentPhase != PlayerActionPhase.Execution)
            {
                return;
            }

            frame.GravityScale *= definition.AirborneExecutionGravityMultiplier;
            frame.Velocity.y = Mathf.Max(a: frame.Velocity.y, b: definition.AirborneExecutionMinLift);
            frame.Velocity.x += context.FacingDirection * definition.AirborneExecutionNudge;
        }

        private static PlayerActionPhase ResolveTimerPhase(PlayerAttackDefinitionSO definition, float elapsed)
        {
            if (elapsed < definition.ReadingDuration)
            {
                return PlayerActionPhase.Reading;
            }

            if (elapsed < definition.ReadingDuration + definition.ExecutionDuration)
            {
                return PlayerActionPhase.Execution;
            }

            return PlayerActionPhase.Recovery;
        }
    }
}
