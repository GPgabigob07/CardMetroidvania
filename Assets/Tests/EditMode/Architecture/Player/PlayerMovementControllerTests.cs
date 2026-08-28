using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlayerMovementControllerTests
    {
        private GameObject owner;

        [TearDown]
        public void TearDown()
        {
            if (owner != null)
            {
                Object.DestroyImmediate(owner);
            }
        }

        [Test]
        public void LocomotionController_EntersGrounded_WhenSensorIsGrounded()
        {
            var context = CreateContext(grounded: true, Vector2.zero, out var locomotion);

            Assert.AreEqual(PlayerLocomotionState.Grounded, locomotion.CurrentStateId);
            Assert.Greater(locomotion.CoyoteTimer, 0f);
        }

        [Test]
        public void LocomotionController_EntersAirborne_WhenSensorIsNotGrounded()
        {
            var context = CreateContext(grounded: false, Vector2.zero, out var locomotion);

            Assert.AreEqual(PlayerLocomotionState.Airborne, locomotion.CurrentStateId);
            Assert.NotNull(context);
        }

        [Test]
        public void GroundedState_AcceleratesAndConsumesBufferedJump()
        {
            var context = CreateContext(grounded: true, Vector2.zero, out var locomotion);
            context.SetInput(new PlayerInputSnapshot(Vector2.right, true, true, false, false, false));

            locomotion.Tick(context, 0.016f);
            var frame = locomotion.BuildFrame(context, 0.1f);

            Assert.Greater(frame.Velocity.x, 0f);
            Assert.AreEqual(context.MovementConfig.JumpVelocity, frame.Velocity.y);
            Assert.AreEqual(PlayerLocomotionState.Airborne, locomotion.CurrentStateId);
            Assert.False(locomotion.HasBufferedJump);
        }

        [Test]
        public void AirborneState_AppliesAirControlFallGravityAndMaxFallSpeed()
        {
            var context = CreateContext(grounded: false, new Vector2(0f, -100f), out var locomotion);
            context.SetInput(new PlayerInputSnapshot(Vector2.right, false, false, false, false, false));

            var frame = locomotion.BuildFrame(context, 0.1f);

            Assert.Greater(frame.Velocity.x, 0f);
            Assert.AreEqual(context.MovementConfig.FallGravityScale, frame.GravityScale);
            Assert.AreEqual(-context.MovementConfig.MaxFallSpeed, frame.Velocity.y);
        }

        [Test]
        public void ActionRunner_CallsEnterExitAndClearsCompletedAction()
        {
            var context = CreateContext(grounded: true, Vector2.zero, out _);
            var runner = context.ActionRunner;
            var action = new CompletingAction();

            Assert.True(runner.TryStartAction(context, action));
            runner.Tick(context, 0.016f);

            Assert.AreEqual(1, action.EnterCount);
            Assert.AreEqual(1, action.ExitCount);
            Assert.False(runner.HasAction);
        }

        [Test]
        public void DashAction_OverridesVelocityAndDisablesGravity()
        {
            var context = CreateContext(grounded: true, Vector2.zero, out _);
            var action = new DashAction();
            var frame = new LocomotionFrame(new Vector2(1f, 5f), 3f, true, true, false);

            action.Enter(context);
            action.ModifyLocomotionFrame(ref frame, context, 0.016f);

            Assert.AreEqual(context.DashDefinition.Speed, frame.Velocity.x);
            Assert.AreEqual(0f, frame.Velocity.y);
            Assert.AreEqual(0f, frame.GravityScale);
            Assert.False(frame.AllowGravity);
            Assert.True(frame.LockFacing);
        }

        [Test]
        public void PlayerMotor_SetFacing_FlipsAssignedVisualRoot()
        {
            owner = new GameObject("Player Motor Test");
            var body = owner.AddComponent<Rigidbody2D>();
            var visual = new GameObject("Visual").transform;
            visual.SetParent(owner.transform);
            visual.localScale = new Vector3(2f, 3f, 1f);
            var motor = owner.AddComponent<PlayerMotor2D>();
            motor.SetBody(body);
            motor.SetVisualRoot(visual);

            motor.SetFacing(-1);

            Assert.AreEqual(-1, motor.FacingDirection);
            Assert.AreEqual(-2f, visual.localScale.x);

            motor.SetFacing(1);

            Assert.AreEqual(1, motor.FacingDirection);
            Assert.AreEqual(2f, visual.localScale.x);
        }

        [Test]
        public void PlayerMotor_SetFacingZero_PreservesFacingAndVisualScale()
        {
            owner = new GameObject("Player Motor Test");
            var body = owner.AddComponent<Rigidbody2D>();
            var visual = new GameObject("Visual").transform;
            visual.SetParent(owner.transform);
            visual.localScale = new Vector3(2f, 3f, 1f);
            var motor = owner.AddComponent<PlayerMotor2D>();
            motor.SetBody(body);
            motor.SetVisualRoot(visual);
            motor.SetFacing(-1);

            motor.SetFacing(0);

            Assert.AreEqual(-1, motor.FacingDirection);
            Assert.AreEqual(-2f, visual.localScale.x);
        }

        [Test]
        public void PlayerDeathRespawn_ResetsPositionVelocityAndHealth_WhenHealthReachesZero()
        {
            owner = new GameObject("Player Respawn Test");
            owner.transform.position = new Vector3(4f, 5f, 0f);
            var body = owner.AddComponent<Rigidbody2D>();
            var motor = owner.AddComponent<PlayerMotor2D>();
            motor.SetBody(body);
            motor.SetVelocity(new Vector2(3f, -2f));
            var health = owner.AddComponent<SimpleHealth>();
            var respawn = owner.AddComponent<PlayerDeathRespawn>();
            respawn.Configure(health, controller: null, playerMotor: motor);

            health.ApplyDamage(new DamageContext(
                source: null,
                target: owner,
                profile: null,
                amount: health.MaximumHealth,
                hitPoint: Vector2.zero,
                direction: Vector2.right));

            Assert.AreEqual(Vector3.zero, owner.transform.position);
            Assert.AreEqual(Vector2.zero, motor.Velocity);
            Assert.AreEqual(health.MaximumHealth, health.CurrentHealth);
        }

        [Test]
        public void AttackAction_AirborneMiss_PreservesGravityAndHorizontalVelocity()
        {
            var context = CreateContext(grounded: false, new Vector2(0f, -2f), out _);
            var action = new AttackAction(PlayerActionState.Attack1);
            var frame = new LocomotionFrame(new Vector2(3f, -2f), context.MovementConfig.FallGravityScale, true, true, false);

            action.Enter(context);
            action.Tick(context, context.AttackDefinition.ReadingDuration + 0.01f);
            action.ModifyLocomotionFrame(ref frame, context, 0.016f);

            Assert.AreEqual(PlayerActionPhase.Execution, action.CurrentPhase);
            Assert.AreEqual(context.MovementConfig.FallGravityScale, frame.GravityScale);
            Assert.AreEqual(3f, frame.Velocity.x);
            Assert.AreEqual(-2f, frame.Velocity.y);
        }

        [Test]
        public void AttackAction_ConfirmedAerialHit_ReducesGravityWithoutForwardNudge()
        {
            var context = CreateContext(grounded: false, new Vector2(0f, -2f), out _);
            var action = new AttackAction(PlayerActionState.Attack1);
            var frame = new LocomotionFrame(new Vector2(3f, -2f), context.MovementConfig.FallGravityScale, true, true, false);

            action.Enter(context);
            action.Tick(context, context.AttackDefinition.ReadingDuration + 0.01f);
            action.ConfirmHit();
            action.ModifyLocomotionFrame(ref frame, context, 0.016f);

            Assert.Less(frame.GravityScale, context.MovementConfig.FallGravityScale);
            Assert.AreEqual(3f, frame.Velocity.x);
            Assert.GreaterOrEqual(frame.Velocity.y, context.AttackDefinition.AirborneExecutionMinLift);
        }

        [Test]
        public void AnimationStateBehaviour_BuildsAnimatorAuthorityFrame()
        {
            var behaviour = ScriptableObject.CreateInstance<PlayerActionAnimationStateBehaviour>();

            var frame = behaviour.BuildFrame();

            Assert.True(frame.HasAnimatorAuthority);
            Assert.AreEqual(PlayerActionPhase.Reading, frame.Phase);
            Assert.AreEqual(PlayerCardTimeState.None, frame.CardTimeState);
            Object.DestroyImmediate(behaviour);
        }

        [Test]
        public void AttackAction_UsesTimerFallbackWithoutAnimatorFrame()
        {
            var context = CreateContext(grounded: true, Vector2.zero, out _);
            var action = new AttackAction(PlayerActionState.Attack1);

            action.Enter(context);
            action.Tick(context, context.AttackDefinition.ReadingDuration + context.AttackDefinition.ExecutionDuration + 0.01f);

            Assert.AreEqual(PlayerActionPhase.Recovery, action.CurrentPhase);
            Assert.False(action.IsComplete);
        }

        private PlayerContext CreateContext(bool grounded, Vector2 initialVelocity, out PlayerLocomotionController locomotion)
        {
            owner = new GameObject("Player Test");
            var body = owner.AddComponent<Rigidbody2D>();
            var motor = owner.AddComponent<PlayerMotor2D>();
            motor.SetBody(body);
            var sensors = owner.AddComponent<PlayerSensors2D>();
            sensors.SetManualGrounded(true, grounded);
            motor.SetVelocity(initialVelocity);

            var movementConfig = ScriptableObject.CreateInstance<PlayerMovementConfigSO>();
            var dashDefinition = ScriptableObject.CreateInstance<PlayerDashDefinitionSO>();
            var attackDefinition = ScriptableObject.CreateInstance<PlayerAttackDefinitionSO>();
            var context = new PlayerContext(motor, sensors, movementConfig, dashDefinition, attackDefinition);
            locomotion = new PlayerLocomotionController(context);
            var runner = new PlayerActionRunner();
            context.AttachRuntime(locomotion, runner);
            locomotion.EnterInitialState(context);
            return context;
        }

        private sealed class CompletingAction : IPlayerAction
        {
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }
            public PlayerActionState State => PlayerActionState.Attack1;
            public bool IsComplete { get; private set; }

            public void Enter(PlayerContext context)
            {
                EnterCount++;
            }

            public void Tick(PlayerContext context, float deltaTime)
            {
                IsComplete = true;
            }

            public void FixedTick(PlayerContext context, float fixedDeltaTime)
            {
            }

            public void Exit(PlayerContext context)
            {
                ExitCount++;
            }
        }
    }
}
