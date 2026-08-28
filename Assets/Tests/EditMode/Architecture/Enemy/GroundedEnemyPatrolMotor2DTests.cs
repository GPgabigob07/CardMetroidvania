using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GroundedEnemyPatrolMotor2DTests
    {
        private const int EnvironmentLayer = 8;
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
        public void MoveTowards_TargetAhead_PreservesVerticalVelocity()
        {
            var motor = CreateMotor(out var body);
            CreateEnvironmentCollider(
                name: "Ground",
                position: new Vector2(x: 0.5f, y: -0.5f));
            body.linearVelocity = new Vector2(x: 0f, y: -3f);
            Physics2D.SyncTransforms();

            var result = motor.MoveTowards(
                target: new Vector2(x: 2f, y: 0f),
                speed: 2f,
                arrivalDistance: 0.05f,
                fixedDeltaTime: 0.02f);

            Assert.AreEqual(EnemyPatrolMoveResult.Moving, result);
            Assert.AreEqual(2f, body.linearVelocity.x);
            Assert.AreEqual(-3f, body.linearVelocity.y);
        }

        [Test]
        public void MoveTowards_InsideArrivalDistance_ReturnsArrived()
        {
            var motor = CreateMotor(out var body);
            body.linearVelocity = new Vector2(x: 2f, y: -1f);

            var result = motor.MoveTowards(
                target: new Vector2(x: 0.04f, y: 10f),
                speed: 2f,
                arrivalDistance: 0.05f,
                fixedDeltaTime: 0.02f);

            Assert.AreEqual(EnemyPatrolMoveResult.Arrived, result);
            Assert.AreEqual(0f, body.linearVelocity.x);
            Assert.AreEqual(-1f, body.linearVelocity.y);
        }

        [Test]
        public void MoveTowards_WallAhead_ReturnsBlocked()
        {
            var motor = CreateMotor(out _);
            CreateEnvironmentCollider(
                name: "Ground",
                position: new Vector2(x: 0.5f, y: -0.5f));
            CreateEnvironmentCollider(
                name: "Wall",
                position: new Vector2(x: 0.5f, y: 0f));
            Physics2D.SyncTransforms();

            var result = motor.MoveTowards(
                target: Vector2.right * 2f,
                speed: 2f,
                arrivalDistance: 0.05f,
                fixedDeltaTime: 0.02f);

            Assert.AreEqual(EnemyPatrolMoveResult.Blocked, result);
        }

        [Test]
        public void MoveTowards_NoLedgeSupport_ReturnsBlocked()
        {
            var motor = CreateMotor(out _);
            Physics2D.SyncTransforms();

            var result = motor.MoveTowards(
                target: Vector2.right * 2f,
                speed: 2f,
                arrivalDistance: 0.05f,
                fixedDeltaTime: 0.02f);

            Assert.AreEqual(EnemyPatrolMoveResult.Blocked, result);
        }

        [Test]
        public void Stop_PreservesVerticalVelocity()
        {
            var motor = CreateMotor(out var body);
            body.linearVelocity = new Vector2(x: 4f, y: -2f);

            motor.Stop();

            Assert.AreEqual(new Vector2(x: 0f, y: -2f), body.linearVelocity);
        }

        [Test]
        public void SetFacing_Zero_DoesNotChangeFacing()
        {
            var motor = CreateMotor(out _);
            motor.SetFacing(direction: -1);

            motor.SetFacing(direction: 0);

            Assert.AreEqual(-1, motor.FacingDirection);
        }

        private GroundedEnemyPatrolMotor2D CreateMotor(out Rigidbody2D body)
        {
            var owner = new GameObject(name: "Grounded Patrol Motor");
            objectsToDestroy.Add(owner);
            body = owner.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            var motor = owner.AddComponent<GroundedEnemyPatrolMotor2D>();
            motor.SetBody(body);
            motor.ConfigureProbes(
                layer: 1 << EnvironmentLayer,
                ledgeOffset: new Vector2(x: 0.5f, y: -0.5f),
                wallOffset: new Vector2(x: 0.5f, y: 0f),
                radius: 0.1f);
            return motor;
        }

        private void CreateEnvironmentCollider(string name, Vector2 position)
        {
            var environment = new GameObject(name);
            objectsToDestroy.Add(environment);
            environment.layer = EnvironmentLayer;
            environment.transform.position = position;
            var collider = environment.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(x: 0.15f, y: 0.15f);
        }
    }
}
