using NUnit.Framework;
using UnityEngine;

namespace TicGame.Architecture.Tests
{
    public sealed class GargoyleConfigurationTests
    {
        private EnemyAttackDefinitionSO definition;

        [SetUp]
        public void SetUp() => definition = ScriptableObject.CreateInstance<EnemyAttackDefinitionSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(definition);

        [Test]
        public void EmptySteps_AreRejected()
        {
            Assert.IsNotEmpty(definition.GetValidationErrors());
        }

        [TestCase(-1f, .12f, .18f)]
        [TestCase(float.NaN, .12f, .18f)]
        [TestCase(float.PositiveInfinity, .12f, .18f)]
        [TestCase(.35f, 0f, .18f)]
        [TestCase(.35f, .12f, -.18f)]
        public void MalformedStepTimings_AreRejected(float windup, float active, float recovery)
        {
            var step = new EnemyAttackStep("sweep", windup, active, recovery, null);
            CollectionAssert.Contains(step.GetValidationErrors(),
                "Step phase durations must be finite; windup and active must be positive, recovery non-negative.");
        }

        [Test]
        public void MissingPayloadProfile_AndMissingIdentity_AreRejected()
        {
            Assert.IsNotEmpty(new EnemyAttackStep("sweep", .35f, .12f, .18f, null).GetValidationErrors());
            var profile = ScriptableObject.CreateInstance<DamageProfileSO>();
            try
            {
                Assert.IsNotEmpty(new EnemyAttackStep("", .35f, .12f, .18f, profile).GetValidationErrors());
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void ValidStep_PreservesAuthoredTimingsAndSingleProfileOwner()
        {
            var profile = ScriptableObject.CreateInstance<DamageProfileSO>();
            try
            {
                var step = new EnemyAttackStep("sweep", .35f, .12f, .18f, profile);
                Assert.IsEmpty(step.GetValidationErrors());
                Assert.AreEqual(.35f, step.WindupDuration);
                Assert.AreEqual(.12f, step.ActiveDuration);
                Assert.AreEqual(.18f, step.RecoveryDuration);
                Assert.AreSame(profile, step.DamageProfile);
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void Definition_RejectsDuplicateStableStepIds()
        {
            JsonUtility.FromJsonOverwrite("{\"steps\":[{\"id\":\"sweep\"},{\"id\":\"sweep\"}]}", definition);
            CollectionAssert.Contains(definition.GetValidationErrors(), "Attack step IDs must be unique.");
        }

        [Test]
        public void EncounterTuning_RejectsMissingRepertoireAndPresentationBindings()
        {
            var tuning = ScriptableObject.CreateInstance<GargoyleTuningSO>();
            try
            {
                CollectionAssert.Contains(tuning.GetValidationErrors(), "Basic attack definition is required.");
                CollectionAssert.Contains(tuning.GetValidationErrors(), "Each attack family requires a definition.");
                CollectionAssert.Contains(tuning.GetValidationErrors(), "Nova attack definition is required.");
                CollectionAssert.Contains(tuning.GetValidationErrors(), "Presentation definition is required.");
            }
            finally { Object.DestroyImmediate(tuning); }
        }
    }
}
