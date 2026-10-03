using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class EnemyAttackDefinitionTests
    {
        private DamageProfileSO profile;
        private EnemyAttackDefinitionSO definition;
        [SetUp]
        public void SetUp()
        {
            profile = ScriptableObject.CreateInstance<DamageProfileSO>();
            definition = ScriptableObject.CreateInstance<EnemyAttackDefinitionSO>();
        }
        [TearDown]
        public void TearDown() { Object.DestroyImmediate(definition); Object.DestroyImmediate(profile); }

        [Test]
        public void PayloadDefaults_AreAssetOwned_AndMatchApprovedProjectileBeamNovaNumbers()
        {
            var payload = new EnemyAttackStep("sweep", .35f, .12f, .18f, profile).Payload;
            Assert.AreEqual(EnemyAttackPayloadKind.Melee, payload.Kind);
            Assert.AreEqual(1, payload.AcceptedHitLimit);
            Assert.AreEqual(6, payload.ProjectileSpeed);
            Assert.AreEqual(3, payload.ProjectileLifetime);
            Assert.AreEqual(.3f, payload.BeamThickness);
            Assert.AreEqual(.2f, payload.BeamOpeningDuration);
            Assert.AreEqual(3.5f, payload.NovaRadius);
            Assert.AreEqual(4.8f, payload.ReactorStability);
        }

        [Test]
        public void CaptureSteps_RetainsGraphIdentitiesAndValues_WithoutCloningDamageAssets()
        {
            var original = new EnemyAttackStep("sweep", .35f, .12f, .18f, profile);
            definition.SetSteps(new[] { original });
            Assert.IsTrue(definition.TryCaptureSteps(out var captured));
            JsonUtility.FromJsonOverwrite("{\"activeDuration\":0.2}", original);
            definition.SetSteps(new[] { new EnemyAttackStep("replacement", .4f, .2f, .3f, profile) });
            Assert.AreEqual("sweep", captured[0].Id);
            Assert.AreEqual(.12f, captured[0].ActiveDuration);
            Assert.AreSame(profile, captured[0].DamageProfile);
        }

        [TestCase("{\"payload\":{\"acceptedHitLimit\":0}}")]
        [TestCase("{\"payload\":{\"hitboxSize\":{\"x\":-1,\"y\":1}}}")]
        [TestCase("{\"payload\":{\"kind\":2,\"beamThickness\":0}}")]
        [TestCase("{\"payload\":{\"kind\":2,\"beamOpeningDuration\":1}}")]
        [TestCase("{\"payload\":{\"kind\":3,\"reactorStability\":0}}")]
        [TestCase("{\"payload\":{\"lockedAimDuration\":1}}")]
        public void InvalidPayloadGeometryOrTiming_RejectsCoherentStepCapture(string edit)
        {
            var step = new EnemyAttackStep("attack", .8f, .7f, .75f, profile);
            JsonUtility.FromJsonOverwrite(edit, step);
            definition.SetSteps(new[] { step });
            Assert.IsFalse(definition.TryCaptureSteps(out _));
        }

        [Test]
        public void Patterns_RejectMismatchedCountAndNonFiniteAngles()
        {
            Assert.IsNotEmpty(new EnemyProjectilePattern(3, .55f, new[] { -12f, 12f }).GetValidationErrors());
            Assert.IsNotEmpty(new EnemyProjectilePattern(1, .45f, new[] { float.NaN }).GetValidationErrors());
            Assert.IsEmpty(new EnemyProjectilePattern(3, .55f, new[] { -12f, 0, 12f }).GetValidationErrors());
        }

        [Test]
        public void LiveStepRead_ObservesEditedDurationAndGeometry_WithoutChangingCapturedStepGraph()
        {
            var step = new EnemyAttackStep("beam", .8f, .7f, .75f, profile);
            definition.SetSteps(new[] { step });
            Assert.IsTrue(definition.TryCaptureSteps(out var graph));
            JsonUtility.FromJsonOverwrite("{\"windupDuration\":1.2,\"payload\":{\"beamThickness\":0.5}}", step);
            Assert.IsTrue(definition.TryReadStep("beam", out var live));
            Assert.AreEqual(1.2f, live.WindupDuration);
            Assert.AreEqual(.5f, live.Payload.BeamThickness);
            Assert.AreEqual(.8f, graph[0].WindupDuration);
            Assert.AreEqual(.3f, graph[0].Payload.BeamThickness);
        }
    }
}
