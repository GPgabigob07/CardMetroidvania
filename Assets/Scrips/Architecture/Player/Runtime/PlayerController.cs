using UnityEngine;
using UnityEngine.InputSystem;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(PlayerMotor2D))]
    [RequireComponent(requiredComponent: typeof(PlayerSensors2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header(header: "Components")]
        [Tooltip(tooltip: "Motor that applies final movement frames to the Rigidbody2D.")]
        [SerializeField] private PlayerMotor2D motor;

        [Tooltip(tooltip: "Sensor component used by locomotion states.")]
        [SerializeField] private PlayerSensors2D sensors;

        [Header(header: "Input Actions")]
        [Tooltip(tooltip: "Input System action used for player movement.")]
        [SerializeField] private InputActionReference moveAction;

        [Tooltip(tooltip: "Input System action used for jumping.")]
        [SerializeField] private InputActionReference jumpAction;

        [Tooltip(tooltip: "Input System action used for basic attack.")]
        [SerializeField] private InputActionReference attackAction;

        [Tooltip(tooltip: "Input System action used as dash in the first prototype slice.")]
        [SerializeField] private InputActionReference dashAction;

        [Header(header: "Definitions")]
        [Tooltip(tooltip: "Movement tuning used by locomotion states.")]
        [SerializeField] private PlayerMovementConfigSO movementConfig;

        [Tooltip(tooltip: "Dash tuning used by DashAction.")]
        [SerializeField] private PlayerDashDefinitionSO dashDefinition;

        [Tooltip(tooltip: "Attack shell tuning used by AttackAction.")]
        [SerializeField] private PlayerAttackDefinitionSO attackDefinition;

        private PlayerInputSnapshot input;

        public PlayerContext Context { get; private set; }
        public PlayerLocomotionController Locomotion { get; private set; }
        public PlayerActionRunner ActionRunner { get; private set; }

        private void Awake()
        {
            if (motor == null)
            {
                motor = GetComponent<PlayerMotor2D>();
            }

            if (sensors == null)
            {
                sensors = GetComponent<PlayerSensors2D>();
            }

            movementConfig = movementConfig != null ? movementConfig : ScriptableObject.CreateInstance<PlayerMovementConfigSO>();
            dashDefinition = dashDefinition != null ? dashDefinition : ScriptableObject.CreateInstance<PlayerDashDefinitionSO>();
            attackDefinition = attackDefinition != null ? attackDefinition : ScriptableObject.CreateInstance<PlayerAttackDefinitionSO>();

            Context = new PlayerContext(motor: motor, sensors: sensors, movementConfig: movementConfig, dashDefinition: dashDefinition, attackDefinition: attackDefinition);
            sensors.Refresh();
            Locomotion = new PlayerLocomotionController(context: Context);
            ActionRunner = new PlayerActionRunner();
            Context.AttachRuntime(locomotion: Locomotion, actionRunner: ActionRunner);
            Locomotion.EnterInitialState(context: Context);
        }

        private void OnEnable()
        {
            EnableAction(reference: moveAction);
            EnableAction(reference: jumpAction);
            EnableAction(reference: attackAction);
            EnableAction(reference: dashAction);
        }

        private void OnDisable()
        {
            DisableAction(reference: moveAction);
            DisableAction(reference: jumpAction);
            DisableAction(reference: attackAction);
            DisableAction(reference: dashAction);
        }

        private void Update()
        {
            input = ReadInputActions();
            SetInputSnapshot(snapshot: input);

            if (!ActionRunner.HasAction)
            {
                Context.RefreshFacingFromInput();
            }

            if (input.DashPressed)
            {
                ActionRunner.TryStartAction(context: Context, action: new DashAction(), replaceCurrent: false);
            }
            else if (input.AttackPressed)
            {
                ActionRunner.TryStartAction(context: Context, action: new AttackAction(state: PlayerActionState.Attack1), replaceCurrent: false);
            }

            Locomotion.Tick(context: Context, deltaTime: Time.deltaTime);
            ActionRunner.Tick(context: Context, deltaTime: Time.deltaTime);
        }

        private void FixedUpdate()
        {
            sensors.Refresh();
            Locomotion.FixedTick(context: Context, fixedDeltaTime: Time.fixedDeltaTime);
            ActionRunner.FixedTick(context: Context, fixedDeltaTime: Time.fixedDeltaTime);

            var frame = Locomotion.BuildFrame(context: Context, fixedDeltaTime: Time.fixedDeltaTime);
            ActionRunner.CurrentLocomotionOverride?.ModifyLocomotionFrame(frame: ref frame, context: Context, fixedDeltaTime: Time.fixedDeltaTime);
            motor.ApplyFrame(frame: frame);
        }

        public void SetInputSnapshot(PlayerInputSnapshot snapshot)
        {
            Context.SetInput(input: snapshot);
        }

        public void ApplyAnimationFrame(PlayerActionFrame frame)
        {
            Context.SetActionFrame(frame: frame);
        }

        private PlayerInputSnapshot ReadInputActions()
        {
            return new PlayerInputSnapshot(
                move: ReadVector2(reference: moveAction),
                jumpPressed: WasPressed(reference: jumpAction),
                jumpHeld: IsPressed(reference: jumpAction),
                jumpReleased: WasReleased(reference: jumpAction),
                attackPressed: WasPressed(reference: attackAction),
                dashPressed: WasPressed(reference: dashAction));
        }

        private static void EnableAction(InputActionReference reference)
        {
            reference?.action?.Enable();
        }

        private static void DisableAction(InputActionReference reference)
        {
            reference?.action?.Disable();
        }

        private static Vector2 ReadVector2(InputActionReference reference)
        {
            return reference != null && reference.action != null
                ? reference.action.ReadValue<Vector2>()
                : Vector2.zero;
        }

        private static bool WasPressed(InputActionReference reference)
        {
            return reference != null && reference.action != null && reference.action.WasPressedThisFrame();
        }

        private static bool IsPressed(InputActionReference reference)
        {
            return reference != null && reference.action != null && reference.action.IsPressed();
        }

        private static bool WasReleased(InputActionReference reference)
        {
            return reference != null && reference.action != null && reference.action.WasReleasedThisFrame();
        }
    }
}
