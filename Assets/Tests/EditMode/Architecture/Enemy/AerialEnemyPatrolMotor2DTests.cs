using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class AerialEnemyPatrolMotor2DTests
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
        public void Awake_DisablesGravity()
        {
            var motor = CreateMotor(out var body);

            Assert.NotNull(motor);
            Assert.AreEqual(0f, body.gravityScale);
        }

        [Test]
        public void MoveTowards_DiagonalTarget_UsesNormalizedVelocity()
        {
            var motor = CreateMotor(out var body);

            var result = motor.MoveTowards(
                target: new Vector2(x: 3f, y: 4f),
                speed: 2f,
                arrivalDistance: 0.05f,
                fixedDeltaTime: 0.02f);

            Assert.AreEqual(EnemyPatrolMoveResult.Moving, result);
            Assert.AreEqual(new Vector2(x: 1.2f, y: 1.6f), body.linearVelocity);
        }

        [Test]
        public void MoveTowards_FinalStep_SnapsWithoutOvershoot()
        {
            var motor = CreateMotor(out var body);
            var target = new Vector2(x: 0.03f, y: 0f);

            var result = motor.MoveTowards(
                target: target,
                speed: 2f,
                arrivalDistance: 0.001f,
                fixedDeltaTime: 0.02f);

            Assert.AreEqual(EnemyPatrolMoveResult.Arrived, result);
            Assert.AreEqual(target, body.position);
            Assert.AreEqual(Vector2.zero, body.linearVelocity);
        }

        [Test]
        public void MoveTowards_InsideArrivalDistance_ReturnsArrived()
        {
            var motor = CreateMotor(out var body);
            var target = new Vector2(x: 0.02f, y: 0.02f);

            var result = motor.MoveTowards(
                target: target,
                speed: 2f,
                arrivalDistance: 0.05f,
                fixedDeltaTime: 0.02f);

            Assert.AreEqual(EnemyPatrolMoveResult.Arrived, result);
            Assert.AreEqual(target, body.position);
        }

        [Test]
        public void MoveTowards_VerticalTarget_PreservesFacing()
        {
            var motor = CreateMotor(out _);
            motor.SetFacing(direction: -1);

            motor.MoveTowards(
                target: new Vector2(x: 0f, y: 2f),
                speed: 2f,
                arrivalDistance: 0.05f,
                fixedDeltaTime: 0.02f);

            Assert.AreEqual(-1, motor.FacingDirection);
        }

        [Test]
        public void Stop_ClearsVelocity()
        {
            var motor = CreateMotor(out var body);
            body.linearVelocity = new Vector2(x: 2f, y: -3f);

            motor.Stop();

            Assert.AreEqual(Vector2.zero, body.linearVelocity);
        }

        private AerialEnemyPatrolMotor2D CreateMotor(out Rigidbody2D body)
        {
            var owner = new GameObject(name: "Aerial Patrol Motor");
            objectsToDestroy.Add(owner);
            body = owner.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            var motor = owner.AddComponent<AerialEnemyPatrolMotor2D>();
            motor.SetBody(body);
            return motor;
        }
    }
}
