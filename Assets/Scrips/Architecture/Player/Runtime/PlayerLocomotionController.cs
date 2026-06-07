using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerLocomotionController
    {
        private readonly GroundedLocomotionState groundedState = new GroundedLocomotionState();
        private readonly AirborneLocomotionState airborneState = new AirborneLocomotionState();

        private float jumpBufferTimer;
        private float coyoteTimer;

        public PlayerLocomotionController(PlayerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(paramName: nameof(context));
            }

            CurrentState = context.Sensors.IsGrounded ? groundedState : airborneState;
        }

        public IPlayerLocomotionState CurrentState { get; private set; }
        public PlayerLocomotionState CurrentStateId => CurrentState.Id;
        public float JumpBufferTimer => jumpBufferTimer;
        public float CoyoteTimer => coyoteTimer;

        public void EnterInitialState(PlayerContext context)
        {
            CurrentState.Enter(context: context);
        }

        public void Tick(PlayerContext context, float deltaTime)
        {
            if (context.Input.JumpPressed)
            {
                jumpBufferTimer = context.MovementConfig.JumpBufferTime;
            }
            else
            {
                jumpBufferTimer = Mathf.Max(a: 0f, b: jumpBufferTimer - deltaTime);
            }

            coyoteTimer = Mathf.Max(a: 0f, b: coyoteTimer - deltaTime);
            CurrentState.Tick(context: context, deltaTime: deltaTime);
        }

        public void FixedTick(PlayerContext context, float fixedDeltaTime)
        {
            ResolveTransitions(context: context);
            CurrentState.FixedTick(context: context, fixedDeltaTime: fixedDeltaTime);
        }

        public LocomotionFrame BuildFrame(PlayerContext context, float fixedDeltaTime)
        {
            return CurrentState.BuildFrame(context: context, fixedDeltaTime: fixedDeltaTime);
        }

        public bool HasBufferedJump => jumpBufferTimer > 0f;
        public bool HasCoyoteJump => coyoteTimer > 0f;

        public void ResetCoyoteTime(PlayerContext context)
        {
            coyoteTimer = context.MovementConfig.CoyoteTime;
        }

        public void ConsumeJumpBuffer()
        {
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        public void ForceState(PlayerContext context, PlayerLocomotionState stateId)
        {
            IPlayerLocomotionState nextState = stateId == PlayerLocomotionState.Grounded
                ? groundedState
                : airborneState;

            ChangeState(context: context, nextState: nextState);
        }

        private void ResolveTransitions(PlayerContext context)
        {
            if (context.Sensors.IsGrounded && CurrentState.Id == PlayerLocomotionState.Airborne && context.Motor.Velocity.y <= 0.01f)
            {
                ChangeState(context: context, nextState: groundedState);
                return;
            }

            if (!context.Sensors.IsGrounded && CurrentState.Id == PlayerLocomotionState.Grounded)
            {
                ChangeState(context: context, nextState: airborneState);
            }
        }

        private void ChangeState(PlayerContext context, IPlayerLocomotionState nextState)
        {
            if (CurrentState == nextState || !nextState.CanEnter(context: context))
            {
                return;
            }

            CurrentState.Exit(context: context);
            CurrentState = nextState;
            CurrentState.Enter(context: context);
        }
    }
}
