using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TicGame.Architecture.Tests
{
    public sealed class GargoyleAttackSelectorTests
    {
        private GargoyleTuningSO tuning;

        [SetUp]
        public void SetUp() => tuning = ScriptableObject.CreateInstance<GargoyleTuningSO>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(tuning);

        [Test]
        public void Families_AreUniqueInEachBag_AndDoNotRepeatAcrossBoundary()
        {
            var selector = new GargoyleAttackSelector(tuning, new System.Random(14));
            GargoyleAttackFamily? previous = null;
            for (var bag = 0; bag < 100; bag++)
            {
                var seen = new HashSet<GargoyleAttackFamily>();
                for (var slot = 0; slot < 3; slot++)
                {
                    var family = selector.CommitFamily();
                    Assert.IsTrue(seen.Add(family));
                    if (slot == 0 && previous.HasValue) Assert.AreNotEqual(previous.Value, family);
                    previous = family;
                }
                Assert.AreEqual(0, selector.RemainingFamilies);
            }
        }

        [Test]
        public void AllSixOrders_AreReachable()
        {
            var orders = new HashSet<string>();
            for (var seed = 0; seed < 256; seed++)
            {
                var selector = new GargoyleAttackSelector(tuning, new System.Random(seed));
                orders.Add($"{selector.CommitFamily()}/{selector.CommitFamily()}/{selector.CommitFamily()}");
            }
            Assert.AreEqual(6, orders.Count);
        }

        [Test]
        public void MatchingSeeds_ReproduceBothIndependentBags()
        {
            var first = new GargoyleAttackSelector(tuning, new System.Random(23));
            var second = new GargoyleAttackSelector(tuning, new System.Random(23));
            for (var index = 0; index < 30; index++)
            {
                Assert.AreEqual(first.CommitFamily(), second.CommitFamily());
                Assert.AreEqual(first.CommitVolleyCount(), second.CommitVolleyCount());
            }
        }

        [Test]
        public void Peek_PreservesUncommittedFamilyAcrossRepositionAndInterrupt()
        {
            var selector = new GargoyleAttackSelector(tuning, new System.Random(18));
            var pending = selector.PeekFamily();
            Assert.AreEqual(3, selector.RemainingFamilies);
            Assert.AreEqual(pending, selector.PeekFamily());
            Assert.AreEqual(pending, selector.CommitFamily());
            Assert.AreEqual(2, selector.RemainingFamilies);
            var remaining = selector.PeekFamily();
            Assert.AreEqual(remaining, selector.PeekFamily());
            Assert.AreEqual(remaining, selector.CommitFamily());
            Assert.AreEqual(1, selector.RemainingFamilies);
        }

        [Test]
        public void VolleyBag_ContainsOneThreeFive_WithoutRepeatingAcrossBoundary()
        {
            var selector = new GargoyleAttackSelector(tuning, new System.Random(44));
            int? previous = null;
            for (var bag = 0; bag < 30; bag++)
            {
                var counts = new HashSet<int>();
                for (var slot = 0; slot < 3; slot++)
                {
                    var count = selector.CommitVolleyCount();
                    counts.Add(count);
                    if (slot == 0 && previous.HasValue) Assert.AreNotEqual(previous.Value, count);
                    previous = count;
                }
                CollectionAssert.AreEquivalent(new[] { 1, 3, 5 }, counts);
            }
        }

        [Test]
        public void Reset_RestartsAnExplicitEncounterWithSuppliedSeed()
        {
            var selector = new GargoyleAttackSelector(tuning, new System.Random(7));
            var expected = selector.CommitFamily();
            selector.CommitVolleyCount();
            selector.Reset(new System.Random(7));
            Assert.AreEqual(expected, selector.CommitFamily());
        }

        [Test]
        public void Constructor_RejectsMissingConfigurationAndRandomSource()
        {
            Assert.Throws<ArgumentNullException>(() => new GargoyleAttackSelector(null, new System.Random(1)));
            Assert.Throws<ArgumentNullException>(() => new GargoyleAttackSelector(tuning, null));
        }

        [Test]
        public void VolleyConfigurationEdits_ApplyAtNextRefill_PreservingPendingVariants()
        {
            var selector = new GargoyleAttackSelector(tuning, new System.Random(4));
            var original = new HashSet<int> { selector.CommitVolleyCount() };
            JsonUtility.FromJsonOverwrite("{\"volleyCounts\":[2,4,6]}", tuning);
            original.Add(selector.CommitVolleyCount());
            original.Add(selector.CommitVolleyCount());
            CollectionAssert.AreEquivalent(new[] { 1, 3, 5 }, original);
            var edited = new HashSet<int>
            {
                selector.CommitVolleyCount(), selector.CommitVolleyCount(), selector.CommitVolleyCount()
            };
            CollectionAssert.AreEquivalent(new[] { 2, 4, 6 }, edited);
        }

        [Test]
        public void InvalidLiveConfiguration_RetainsLastValidBag_AndReportsOnceUntilFixed()
        {
            var selector = new GargoyleAttackSelector(tuning, new System.Random(8));
            JsonUtility.FromJsonOverwrite("{\"volleyCounts\":[0,0]}", tuning);
            LogAssert.Expect(LogType.Warning, "Invalid Gargoyle selector tuning; retaining the last valid configuration.");
            for (var bag = 0; bag < 2; bag++)
            {
                var variants = new HashSet<int>
                {
                    selector.CommitVolleyCount(), selector.CommitVolleyCount(), selector.CommitVolleyCount()
                };
                CollectionAssert.AreEquivalent(new[] { 1, 3, 5 }, variants);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void SelectionAndReset_DoNotMutateSharedAssets()
        {
            var before = JsonUtility.ToJson(tuning);
            var selector = new GargoyleAttackSelector(tuning, new System.Random(2));
            for (var index = 0; index < 15; index++)
            {
                selector.CommitFamily();
                selector.CommitVolleyCount();
            }
            selector.Reset(new System.Random(3));
            Assert.AreEqual(before, JsonUtility.ToJson(tuning));
        }

        [Test]
        public void FamilyConfigurationEdit_DoesNotRemoveCurrentBagEntries()
        {
            var selector = new GargoyleAttackSelector(tuning, new System.Random(6));
            var original = new HashSet<GargoyleAttackFamily> { selector.CommitFamily() };
            JsonUtility.FromJsonOverwrite("{\"families\":[0,1]}", tuning);
            original.Add(selector.CommitFamily());
            original.Add(selector.CommitFamily());
            CollectionAssert.AreEquivalent(new[]
            {
                GargoyleAttackFamily.Wingbreaker, GargoyleAttackFamily.Volley, GargoyleAttackFamily.Beam
            }, original);
            CollectionAssert.AreEquivalent(new[] { GargoyleAttackFamily.Wingbreaker, GargoyleAttackFamily.Volley },
                new[] { selector.CommitFamily(), selector.CommitFamily() });
        }

        [Test]
        public void InvalidInitialConfiguration_RejectsRatherThanUsingBalanceFallbacks()
        {
            JsonUtility.FromJsonOverwrite("{\"families\":[0,0]}", tuning);
            Assert.Throws<ArgumentException>(() => new GargoyleAttackSelector(tuning, new System.Random(1)));
        }
    }
}
