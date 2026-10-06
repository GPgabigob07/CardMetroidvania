using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class HitRippleRuntimeTests
    {
        private static HitRippleRuntime Create() => new();
        private static void Add(HitRippleRuntime runtime, Vector2 origin, float duration = .25f) => runtime.Add(origin, HitRippleKind.Damage, 10, duration, 4);
        private static void Tick(HitRippleRuntime runtime, float delta, bool paused = false) => runtime.Tick(delta, paused);
        private static int Count(HitRippleRuntime runtime) => runtime.ActiveCount;

        [Test]
        public void ThreeWaves_AgeIndependently_AndFourthReplacesOldest()
        {
            var runtime = Create();
            Add(runtime, Vector2.zero); Tick(runtime, .05f);
            Add(runtime, Vector2.one); Tick(runtime, .05f);
            Add(runtime, Vector2.right);
            Assert.That(Count(runtime), Is.EqualTo(3));
            Assert.That(runtime.GetWave(0).Radius, Is.EqualTo(4).Within(.0001));
            Assert.That(runtime.GetWave(1).Radius, Is.EqualTo(2).Within(.0001));
            Add(runtime, Vector2.up);
            Assert.That(Count(runtime), Is.EqualTo(3));
            Assert.That(runtime.GetWave(0).Origin, Is.EqualTo(Vector2.up));
        }
        [Test]
        public void PauseInvalidTimeExpiryAndClear_AreDeterministic()
        {
            var runtime = Create(); Add(runtime, Vector2.zero);
            Tick(runtime, .125f); Assert.That(runtime.GetWave(0).Radius, Is.EqualTo(5));
            Tick(runtime, 1, true); Tick(runtime, float.NaN); Tick(runtime, -1);
            Assert.That(runtime.GetWave(0).Radius, Is.EqualTo(5));
            Tick(runtime, .125f); Assert.That(Count(runtime), Is.Zero);
            Add(runtime, Vector2.zero, float.NaN); Assert.That(Count(runtime), Is.Zero);
            Add(runtime, Vector2.zero); runtime.Clear();
            Assert.That(Count(runtime), Is.Zero);
        }
    }
}
