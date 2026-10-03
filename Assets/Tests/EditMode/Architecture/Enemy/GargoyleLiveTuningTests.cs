using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TicGame.Architecture.Tests
{
    public sealed class GargoyleLiveTuningTests
    {
        private GargoyleTuningSO tuning;
        [SetUp] public void SetUp() => tuning = ScriptableObject.CreateInstance<GargoyleTuningSO>();
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(tuning);

        [Test]
        public void Defaults_UseApprovedCombatAndCadenceValues()
        {
            var live = new GargoyleLiveTuning(tuning);
            Assert.AreEqual(12, live.Values.MaximumPoise);
            Assert.AreEqual(0, live.Values.PoiseRegeneration);
            Assert.AreEqual(1.25f, live.Values.StunDuration);
            Assert.AreEqual(.65f, live.Values.BeamStaggerDuration);
            Assert.AreEqual(.75f, live.Values.PoiseResistanceDuration);
            Assert.AreEqual(2, live.Values.NovaBagCadence);
            Assert.AreEqual(12, live.Values.NovaCooldown);
            Assert.AreEqual(.2f, live.Values.FeintProbability);
            Assert.AreEqual(1, live.Values.FeintBudget);
            Assert.AreEqual(.15f, live.Values.FeintCueDelay);
            Assert.AreEqual(.35f, live.Values.FeintResponseDuration);
            Assert.AreEqual(1, live.Values.BodyDamageMultiplier);
            Assert.AreEqual(1.5f, live.Values.ExposedHeadMultiplier);
        }

        [Test]
        public void ValidLiveRevision_UpdatesCoherently_WithoutChangingThePreviousSnapshotOrAsset()
        {
            var live = new GargoyleLiveTuning(tuning);
            var previous = live.Values;
            JsonUtility.FromJsonOverwrite("{\"simulation\":{\"maximumPoise\":20,\"stunDuration\":2,\"movementSpeed\":3}}", tuning);
            var authored = JsonUtility.ToJson(tuning);
            Assert.IsTrue(live.Refresh());
            Assert.AreEqual(20, live.Values.MaximumPoise);
            Assert.AreEqual(2, live.Values.StunDuration);
            Assert.AreEqual(3, live.Values.MovementSpeed);
            Assert.AreEqual(12, previous.MaximumPoise);
            Assert.AreEqual(authored, JsonUtility.ToJson(tuning));
        }

        [Test]
        public void InvalidRevision_RetainsTheEntireLastValidConfiguration_AndDiagnosesOnce()
        {
            var live = new GargoyleLiveTuning(tuning);
            JsonUtility.FromJsonOverwrite("{\"simulation\":{\"maximumPoise\":20,\"stunDuration\":-1}}", tuning);
            LogAssert.Expect(LogType.Warning, "Invalid Gargoyle simulation tuning; retaining the last valid configuration.");
            Assert.IsFalse(live.Refresh());
            Assert.IsFalse(live.Refresh());
            Assert.AreEqual(12, live.Values.MaximumPoise);
            Assert.AreEqual(1.25f, live.Values.StunDuration);
            JsonUtility.FromJsonOverwrite("{\"simulation\":{\"stunDuration\":2}}", tuning);
            Assert.IsTrue(live.Refresh());
            Assert.AreEqual(20, live.Values.MaximumPoise);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void ZeroPoiseCeiling_IsAValidLiveEdit_ForDepletionReconciliationByBrain()
        {
            var live = new GargoyleLiveTuning(tuning);
            JsonUtility.FromJsonOverwrite("{\"simulation\":{\"maximumPoise\":0}}", tuning);
            Assert.IsTrue(live.Refresh());
            Assert.AreEqual(0, live.Values.MaximumPoise);
        }

        [Test]
        public void SeedEdits_DoNotRerollTheCurrentFamilyBag()
        {
            var selector = new GargoyleAttackSelector(tuning, new System.Random(5));
            selector.CommitFamily();
            var pending = selector.PeekFamily();
            JsonUtility.FromJsonOverwrite("{\"randomSeed\":42}", tuning);
            Assert.AreEqual(42, tuning.RandomSeed);
            Assert.AreEqual(pending, selector.CommitFamily());
        }
    }
}
