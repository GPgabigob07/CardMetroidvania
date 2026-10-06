using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests.Audio
{
    public sealed class AudioPolicyTests
    {
        [TestCase(1f, 0f)]
        [TestCase(0f, -80f)]
        [TestCase(0.1f, -20f)]
        [TestCase(-1f, -80f)]
        [TestCase(2f, 0f)]
        public void Volume_MapsLinearGainToDecibels(float gain, float expected)
        {
            Assert.That(AudioVolumeMath.ToDecibels(gain), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void Policy_OverlapAndCooldown_DoNotExtendOnDeniedRequests()
        {
            var cue = ScriptableObject.CreateInstance<SoundCueSO>();
            try
            {
                cue.ConfigurePolicy(10, 1, 2f);
                var policy = new AudioVoicePolicy(2);
                Assert.IsTrue(policy.TryAcquire(cue, 0, out var first));
                Assert.IsFalse(policy.TryAcquire(cue, 1, out _));
                policy.Release(first);
                Assert.IsFalse(policy.TryAcquire(cue, 1.5, out _));
                Assert.IsTrue(policy.TryAcquire(cue, 2, out _));
            }
            finally { Object.DestroyImmediate(cue); }
        }

        [Test]
        public void Policy_Saturation_ReplacesOnlyOlderLowerPriorityVoice()
        {
            var low = ScriptableObject.CreateInstance<SoundCueSO>();
            var high = ScriptableObject.CreateInstance<SoundCueSO>();
            try
            {
                low.ConfigurePolicy(20, 4, 0);
                high.ConfigurePolicy(10, 4, 0);
                var policy = new AudioVoicePolicy(2);
                Assert.IsTrue(policy.TryAcquire(low, 0, out var first));
                Assert.IsTrue(policy.TryAcquire(low, 1, out _));
                Assert.IsFalse(policy.TryAcquire(low, 2, out _));
                Assert.IsTrue(policy.TryAcquire(high, 3, out var replaced));
                Assert.AreEqual(first, replaced);
                Assert.IsTrue(policy.TryAcquire(high, 4, out _));
                Assert.IsFalse(policy.TryAcquire(low, 5, out _));
            }
            finally { Object.DestroyImmediate(low); Object.DestroyImmediate(high); }
        }
    }
}
