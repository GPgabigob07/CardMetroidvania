using System.Collections.Generic;
using NUnit.Framework;
using TicGame.Architecture;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class AerialSteeringMotor2DTests
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
        public void MoveTowards_AcceleratesVelocityTowardConfiguredSpeed()
        {
            var motor = CreateMotor(out var body);

            motor.MoveTowards(new Vector2(10f, 0f), maxSpeed: 8f, acceleration: 10f, fixedDeltaTime: 0.1f);

            Assert.AreEqual(new Vector2(1f, 0f), body.linearVelocity);
        }

        [Test]
        public void BeginFall_RestoresAuthoredGravityAndStopsControllingVelocity()
        {
            var motor = CreateMotor(out var body);
            body.linearVelocity = new Vector2(3f, 2f);

            motor.BeginFall();
            motor.MoveTowards(new Vector2(10f, 0f), maxSpeed: 8f, acceleration: 10f, fixedDeltaTime: 0.1f);

            Assert.AreEqual(2.5f, body.gravityScale);
            Assert.AreEqual(new Vector2(3f, 2f), body.linearVelocity);
        }

        [Test]
        public void Stop_AfterBeginFall_PreservesDescendingImpactVelocity()
        {
            var motor = CreateMotor(out var body);
            body.linearVelocity = new Vector2(1f, -12f);
            motor.BeginFall();

            motor.Stop();

            Assert.AreEqual(new Vector2(1f, -12f), body.linearVelocity);
        }

        [Test]
        public void ResumeFlight_DisablesGravityAfterFall()
        {
            var motor = CreateMotor(out var body);
            motor.BeginFall();

            motor.ResumeFlight();

            Assert.AreEqual(0f, body.gravityScale);
        }

        [Test]
        public void Stop_ClearsVelocityWhileFlying()
        {
            var motor = CreateMotor(out var body);
            body.linearVelocity = new Vector2(3f, -2f);

            motor.Stop();

            Assert.AreEqual(Vector2.zero, body.linearVelocity);
        }

        private AerialSteeringMotor2D CreateMotor(out Rigidbody2D body)
        {
            var owner = new GameObject("Aerial Steering Motor");
            objectsToDestroy.Add(owner);
            body = owner.AddComponent<Rigidbody2D>();
            body.gravityScale = 2.5f;
            var motor = owner.AddComponent<AerialSteeringMotor2D>();
            motor.SetBody(body);
            return motor;
        }
    }
}
