using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerContext
    {
        public PlayerContext(
            PlayerMotor2D motor,
            PlayerSensors2D sensors,
            PlayerMovementConfigSO movementConfig,
            PlayerDashDefinitionSO dashDefinition,
            PlayerAttackDefinitionSO attackDefinition)
        {
            Motor = motor;
            Sensors = sensors;
            MovementConfig = movementConfig;
            DashDefinition = dashDefinition;
            AttackDefinition = attackDefinition;
            Input = PlayerInputSnapshot.None;
            ActionFrame = PlayerActionFrame.Default;
            FacingDirection = 1;
        }

        public PlayerMotor2D Motor { get; }
        public PlayerSensors2D Sensors { get; }
        public PlayerMovementConfigSO MovementConfig { get; }
        public PlayerDashDefinitionSO DashDefinition { get; }
        public PlayerAttackDefinitionSO AttackDefinition { get; }
        public PlayerLocomotionController Locomotion { get; private set; }
        public PlayerActionRunner ActionRunner { get; private set; }
        public PlayerInputSnapshot Input { get; private set; }
        public PlayerActionFrame ActionFrame { get; private set; }
        public int FacingDirection { get; private set; }

        public void AttachRuntime(PlayerLocomotionController locomotion, PlayerActionRunner actionRunner)
        {
            Locomotion = locomotion;
            ActionRunner = actionRunner;
        }

        public void SetInput(PlayerInputSnapshot input)
        {
            Input = input;

            if (!ActionFrame.HasAnimatorAuthority)
            {
                ActionFrame = PlayerActionFrame.Default;
            }
        }

        public void SetActionFrame(PlayerActionFrame frame)
        {
            ActionFrame = frame;
        }

        public void ClearAnimatorActionFrame()
        {
            ActionFrame = PlayerActionFrame.Default;
        }

        public void RefreshFacingFromInput()
        {
            if (Mathf.Abs(f: Input.Move.x) < 0.01f)
            {
                return;
            }

            FacingDirection = Input.Move.x > 0f ? 1 : -1;
            Motor.SetFacing(direction: FacingDirection);
        }
    }
}
