using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class EnemyPatrolBrainTests
    {
        private readonly List<Object> objectsToDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var instance in objectsToDestroy)
            {
                Object.DestroyImmediate(instance);
            }

            objectsToDestroy.Clear();
        }

        [Test]
        public void Initialize_OperationalActor_EntersPatrolAndSelectsConfiguredTarget()
        {
            var motor = new FakePatrolMotor { Position = new Vector2(x: 3f, y: 4f) };
            var brain = CreateBrain(motor);
            brain.ConfigureRoute(
                firstOffset: new Vector2(x: -2f, y: 0f),
                secondOffset: new Vector2(x: 2f, y: 1f));

            brain.Initialize();

            Assert.AreEqual(EnemyAIState.Patrol, brain.CurrentState);
            Assert.AreEqual(new Vector2(x: 5f, y: 5f), brain.CurrentTarget);
            Assert.AreEqual(1, motor.FacingDirection);
        }

        [TestCase(EnemyPatrolMoveResult.Arrived)]
        [TestCase(EnemyPatrolMoveResult.Blocked)]
        public void FixedTick_EndpointOrBlock_StopsAndEntersIdle(EnemyPatrolMoveResult result)
        {
            var motor = new FakePatrolMotor { NextResult = result };
            var brain = CreateInitializedBrain(motor);
            var previousStopCount = motor.StopCount;

            brain.FixedTick(fixedDeltaTime: 0.02f);

            Assert.AreEqual(EnemyAIState.Idle, brain.CurrentState);
            Assert.AreEqual(previousStopCount + 2, motor.StopCount);
        }

        [Test]
        public void Tick_BeforeTurnDelay_RemainsIdle()
        {
            var motor = new FakePatrolMotor { NextResult = EnemyPatrolMoveResult.Arrived };
            var brain = CreateInitializedBrain(motor);
            brain.FixedTick(fixedDeltaTime: 0.02f);

            brain.Tick(deltaTime: 0.19f);

            Assert.AreEqual(EnemyAIState.Idle, brain.CurrentState);
        }

        [Test]
        public void Tick_AfterTurnDelay_ReversesTargetAndEntersPatrol()
        {
            var motor = new FakePatrolMotor { NextResult = EnemyPatrolMoveResult.Arrived };
            var brain = CreateInitializedBrain(motor);
            var initialTarget = brain.CurrentTarget;
            brain.FixedTick(fixedDeltaTime: 0.02f);

            brain.Tick(deltaTime: 0.2f);

            Assert.AreEqual(EnemyAIState.Patrol, brain.CurrentState);
            Assert.AreNotEqual(initialTarget, brain.CurrentTarget);
            Assert.AreEqual(-1, motor.FacingDirection);
        }

        [Test]
        public void Defeat_StopsMotorAndEntersDead()
        {
            var motor = new FakePatrolMotor();
            var brain = CreateInitializedBrain(motor, out var health);
            var previousStopCount = motor.StopCount;

            ApplyDamage(health, amount: health.MaximumHealth);

            Assert.AreEqual(EnemyAIState.Dead, brain.CurrentState);
            Assert.AreEqual(previousStopCount + 1, motor.StopCount);
        }

        [Test]
        public void FixedTick_WhileDead_DoesNotMove()
        {
            var motor = new FakePatrolMotor();
            var brain = CreateInitializedBrain(motor, out var health);
            ApplyDamage(health, amount: health.MaximumHealth);
            var previousMoveCount = motor.MoveCount;

            brain.FixedTick(fixedDeltaTime: 0.02f);

            Assert.AreEqual(previousMoveCount, motor.MoveCount);
        }

        [Test]
        public void OrdinaryRestore_WhilePatrolling_DoesNotResetStateOrTarget()
        {
            var motor = new FakePatrolMotor();
            var brain = CreateInitializedBrain(motor, out var health);
            var target = brain.CurrentTarget;
            ApplyDamage(health, amount: 1f);

            health.Restore(amount: 1f);

            Assert.AreEqual(EnemyAIState.Patrol, brain.CurrentState);
            Assert.AreEqual(target, brain.CurrentTarget);
        }

        [Test]
        public void Restore_FromDead_ResumesPatrolTowardOppositeEndpoint()
        {
            var motor = new FakePatrolMotor();
            var brain = CreateInitializedBrain(motor, out var health);
            ApplyDamage(health, amount: health.MaximumHealth);
            motor.Position = brain.SecondPoint;

            health.RestoreToFull();

            Assert.AreEqual(EnemyAIState.Patrol, brain.CurrentState);
            Assert.AreEqual(brain.FirstPoint, brain.CurrentTarget);
        }

        [Test]
        public void Initialize_Repeated_LifecycleEventsProduceSingleTransitionEffects()
        {
            var motor = new FakePatrolMotor();
            var brain = CreateInitializedBrain(motor, out var health);
            brain.Initialize();
            var previousStopCount = motor.StopCount;

            ApplyDamage(health, amount: health.MaximumHealth);

            Assert.AreEqual(previousStopCount + 1, motor.StopCount);
        }

        private EnemyPatrolBrain CreateInitializedBrain(FakePatrolMotor motor)
        {
            return CreateInitializedBrain(motor, out _);
        }

        private EnemyPatrolBrain CreateInitializedBrain(FakePatrolMotor motor, out EnemyHealth health)
        {
            var brain = CreateBrain(motor, out health);
            brain.ConfigureMovement(speed: 2f, delay: 0.2f, distance: 0.05f);
            brain.Initialize();
            return brain;
        }

        private EnemyPatrolBrain CreateBrain(FakePatrolMotor motor)
        {
            return CreateBrain(motor, out _);
        }

        private EnemyPatrolBrain CreateBrain(FakePatrolMotor motor, out EnemyHealth health)
        {
            var owner = new GameObject(name: "Patrol Enemy");
            objectsToDestroy.Add(owner);
            health = owner.AddComponent<EnemyHealth>();
            var actor = owner.AddComponent<EnemyActor>();
            var definition = ScriptableObject.CreateInstance<EnemyDefinitionSO>();
            definition.name = "Patrol Enemy Definition";
            objectsToDestroy.Add(definition);
            actor.SetDefinition(value: definition);
            actor.Initialize();

            var brain = owner.AddComponent<EnemyPatrolBrain>();
            brain.SetDependenciesForTests(value: actor, patrolMotor: motor);
            return brain;
        }

        private static void ApplyDamage(EnemyHealth health, float amount)
        {
            var context = new DamageContext(
                source: null,
                target: health.gameObject,
                profile: null,
                amount: amount,
                hitPoint: Vector2.zero,
                direction: Vector2.right);
            health.ApplyDamage(context);
        }

        private sealed class FakePatrolMotor : IEnemyPatrolMotor2D
        {
            public Vector2 Position { get; set; }
            public int FacingDirection { get; private set; } = 1;
            public EnemyPatrolMoveResult NextResult { get; set; } = EnemyPatrolMoveResult.Moving;
            public int MoveCount { get; private set; }
            public int StopCount { get; private set; }

            public EnemyPatrolMoveResult MoveTowards(
                Vector2 target,
                float speed,
                float arrivalDistance,
                float fixedDeltaTime)
            {
                MoveCount++;
                return NextResult;
            }

            public void Stop()
            {
                StopCount++;
            }

            public void SetFacing(int direction)
            {
                if (direction != 0)
                {
                    FacingDirection = direction > 0 ? 1 : -1;
                }
            }
        }
    }
}
