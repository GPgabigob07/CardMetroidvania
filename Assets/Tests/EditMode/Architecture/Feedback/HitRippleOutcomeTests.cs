using NUnit.Framework;

namespace TicGame.Architecture.Tests
{
    public sealed class HitRippleOutcomeTests
    {
        private static string Classify(DamageResult result) => HitRippleOutcome.Classify(result).ToString();

        [Test]
        public void AcceptedDamage_DistinguishesFatalFromOrdinary()
        {
            Assert.That(Classify(new DamageResult(true, false, 1, 5)), Is.EqualTo("Damage"));
            Assert.That(Classify(new DamageResult(true, true, 1, 0)), Is.EqualTo("Fatal"));
        }

        [Test]
        public void DeadAndUnspecifiedRejections_DoNotCreateRipples()
        {
            Assert.That(Classify(new DamageResult(false, true, 0, 0)), Is.EqualTo("None"));
            Assert.That(Classify(new DamageResult(false, false, 0, 5)), Is.EqualTo("None"));
            Assert.That(Classify(new DamageResult(true, false, 0, 5)), Is.EqualTo("None"));
        }

        [TestCase(DamageRejectionReason.GameplayBlocked, "Rejected")]
        [TestCase(DamageRejectionReason.AlreadyDefeated, "None")]
        [TestCase(DamageRejectionReason.InvalidTarget, "None")]
        [TestCase(DamageRejectionReason.NonPositiveDamage, "None")]
        [TestCase(DamageRejectionReason.DuplicateExecution, "None")]
        public void RejectionReason_ControlsWhiteWave(DamageRejectionReason reason, string expected)
        {
            Assert.That(Classify(new DamageResult(false, false, 0, 5, rejectionReason: reason)), Is.EqualTo(expected));
            Assert.That(Classify(new DamageResult(false, true, 0, 0, rejectionReason: reason)), Is.EqualTo("None"));
        }
    }
}
