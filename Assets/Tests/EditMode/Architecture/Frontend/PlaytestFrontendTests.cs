using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class PlaytestFrontendTests
    {
        // These tests catch duplicate transitions and stale async completions restoring a closed run.
        [Test]
        public void TransitionGate_RejectsDoubleStart_AndStaleCompletion()
        {
            var type = typeof(GameplaySceneRoot).Assembly.GetType("TicGame.Architecture.PlaytestTransitionGate");
            Assert.NotNull(type, "The playtest needs a single-flight transition gate.");
            dynamic gate = System.Activator.CreateInstance(type);
            int first = gate.TryBegin();
            Assert.Greater(first, 0);
            Assert.AreEqual(0, (int)gate.TryBegin());
            Assert.IsTrue((bool)gate.Complete(first));
            int second = gate.TryBegin();
            Assert.Greater(second, first);
            Assert.IsFalse((bool)gate.Complete(first));
            Assert.IsTrue((bool)gate.IsBusy);
            Assert.IsTrue((bool)gate.Complete(second));
            Assert.IsFalse((bool)gate.IsBusy);
        }

        // This catches pause removing somebody else's time modifier on resume.
        [Test]
        public void PauseLease_RestoresCardTime_WithoutChangingBodyVelocity()
        {
            var owner = new GameObject("Pause test");
            var previousScale = Time.timeScale;
            var previousStep = Time.fixedDeltaTime;
            try
            {
                Time.timeScale = 1f;
                var time = owner.AddComponent<GameplayTimeCoordinator>();
                time.Initialize();
                var cardOwner = new object();
                time.SetModifier(cardOwner, new GameplayTimeModifier(GameplayTimeModifierKind.CardTime, .15f));
                var body = owner.AddComponent<Rigidbody2D>();
                body.linearVelocity = new Vector2(4, 7);
                var type = typeof(GameplaySceneRoot).Assembly.GetType("TicGame.Architecture.PlaytestPauseLease");
                Assert.NotNull(type, "Pause needs an owner-scoped lease.");
                var lease = (System.IDisposable)System.Activator.CreateInstance(type, new object[] { time });
                Assert.AreEqual(0f, time.EffectiveTimeScale);
                Assert.AreEqual(new Vector2(4, 7), body.linearVelocity);
                lease.Dispose();
                lease.Dispose();
                Assert.AreEqual(.15f, time.EffectiveTimeScale);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Time.timeScale = previousScale;
                Time.fixedDeltaTime = previousStep;
            }
        }
    }
}

