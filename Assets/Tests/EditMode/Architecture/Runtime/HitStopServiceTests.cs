using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class HitStopServiceTests
    {
        private GameObject owner;
        private HitStopRequestEventChannelSO requestEvent;
        private float originalTimeScale;
        private float originalFixedDeltaTime;

        [SetUp]
        public void SetUp()
        {
            originalTimeScale = Time.timeScale;
            originalFixedDeltaTime = Time.fixedDeltaTime;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        [TearDown]
        public void TearDown()
        {
            if (owner != null)
            {
                Object.DestroyImmediate(owner);
            }

            if (requestEvent != null)
            {
                Object.DestroyImmediate(requestEvent);
            }

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }

        [Test]
        public void RequestEvent_StopsGameplayThroughTimeCoordinator()
        {
            owner = new GameObject("Hit Stop Service Test");
            requestEvent = ScriptableObject.CreateInstance<HitStopRequestEventChannelSO>();
            var time = owner.AddComponent<GameplayTimeCoordinator>();
            var hitStop = owner.AddComponent<HitStopService>();
            time.Initialize();
            hitStop.Configure(time, requestEvent);
            hitStop.Initialize();

            requestEvent.Raise(
                new HitStopRequest(
                    duration: 0.1f,
                    sourceObject: owner,
                    damageInstanceId: "test"));

            Assert.IsTrue(hitStop.IsActive);
            Assert.AreEqual(0f, Time.timeScale);
        }

        [Test]
        public void NonPositiveRequest_DoesNotStopGameplay()
        {
            owner = new GameObject("Hit Stop Service Test");
            requestEvent = ScriptableObject.CreateInstance<HitStopRequestEventChannelSO>();
            var time = owner.AddComponent<GameplayTimeCoordinator>();
            var hitStop = owner.AddComponent<HitStopService>();
            time.Initialize();
            hitStop.Configure(time, requestEvent);
            hitStop.Initialize();

            requestEvent.Raise(
                new HitStopRequest(
                    duration: 0f,
                    sourceObject: owner,
                    damageInstanceId: "test"));

            Assert.IsFalse(hitStop.IsActive);
            Assert.AreEqual(1f, Time.timeScale);
        }
    }
}
