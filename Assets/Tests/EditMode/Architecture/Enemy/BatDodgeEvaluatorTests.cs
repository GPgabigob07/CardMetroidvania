using NUnit.Framework;
using TicGame.Architecture;

namespace TicGame.Architecture.Tests
{
    public sealed class BatDodgeEvaluatorTests
    {
        private BatDodgeEvaluator evaluator;
        private BatThreatFacts threat;

        [SetUp]
        public void SetUp()
        {
            evaluator = new BatDodgeEvaluator();
            threat = new BatThreatFacts(isMonitored: true, relativeClosingSpeed: 4f, isWithinBaseMeleeReachOuterBand: false);
        }

        [Test]
        public void Evaluate_FullPoise_UsesSeventyPercentChance()
        {
            evaluator.SetRollSource(new FixedRollSource(0.69f));

            Assert.IsTrue(evaluator.TryEvaluate(threat, currentPoise: 30f, maximumPoise: 30f));
            Assert.IsTrue(evaluator.LastEvaluationConsumesCooldown);
        }

        [Test]
        public void Evaluate_LowPoise_RejectsRollAboveTwentyPercent()
        {
            evaluator.SetRollSource(new FixedRollSource(0.21f));

            Assert.IsFalse(evaluator.TryEvaluate(threat, currentPoise: 10f, maximumPoise: 30f));
            Assert.IsFalse(evaluator.LastEvaluationConsumesCooldown);
        }

        [Test]
        public void Evaluate_CooldownActive_DoesNotConsumeARollOrCooldown()
        {
            var rolls = new CountingRollSource(0f);
            evaluator.SetRollSource(rolls);

            var result = evaluator.TryEvaluate(threat, currentPoise: 30f, maximumPoise: 30f, cooldownReady: false);

            Assert.IsFalse(result);
            Assert.AreEqual(0, rolls.CallCount);
            Assert.IsFalse(evaluator.LastEvaluationConsumesCooldown);
        }

        private sealed class FixedRollSource : IRandomRollSource
        {
            private readonly float value;

            public FixedRollSource(float value)
            {
                this.value = value;
            }

            public float NextNormalized()
            {
                return value;
            }
        }

        private sealed class CountingRollSource : IRandomRollSource
        {
            private readonly float value;

            public CountingRollSource(float value)
            {
                this.value = value;
            }

            public int CallCount { get; private set; }

            public float NextNormalized()
            {
                CallCount++;
                return value;
            }
        }
    }
}
