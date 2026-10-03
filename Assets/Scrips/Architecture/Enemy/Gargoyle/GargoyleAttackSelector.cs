using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class GargoyleAttackSelector
    {
        private readonly GargoyleTuningSO tuning;
        private System.Random random;
        private readonly Queue<GargoyleAttackFamily> familyBag = new Queue<GargoyleAttackFamily>();
        private readonly Queue<int> volleyBag = new Queue<int>();
        private GargoyleAttackFamily[] validFamilies;
        private int[] validVolleyCounts;
        private GargoyleAttackFamily? previousFamily;
        private int? previousVolleyCount;
        private bool reportedInvalidConfiguration;

        public GargoyleAttackSelector(GargoyleTuningSO tuning, System.Random random)
        {
            this.tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            if (!tuning.TryReadSelectorConfiguration(out validFamilies, out validVolleyCounts))
                throw new ArgumentException("Gargoyle selector requires valid authored family and volley bags.", nameof(tuning));
        }

        public int RemainingFamilies => familyBag.Count;

        public GargoyleAttackFamily PeekFamily()
        {
            if (familyBag.Count == 0)
            {
                ReadLiveConfiguration();
                Refill(familyBag, validFamilies, previousFamily);
            }
            return familyBag.Peek();
        }

        public GargoyleAttackFamily CommitFamily()
        {
            PeekFamily();
            var family = familyBag.Dequeue();
            previousFamily = family;
            return family;
        }

        public int CommitVolleyCount()
        {
            if (volleyBag.Count == 0)
            {
                ReadLiveConfiguration();
                Refill(volleyBag, validVolleyCounts, previousVolleyCount);
            }
            var count = volleyBag.Dequeue();
            previousVolleyCount = count;
            return count;
        }

        public void Reset(System.Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            familyBag.Clear();
            volleyBag.Clear();
            previousFamily = null;
            previousVolleyCount = null;
            ReadLiveConfiguration();
        }

        private void ReadLiveConfiguration()
        {
            if (tuning.TryReadSelectorConfiguration(out var families, out var counts))
            {
                validFamilies = families;
                validVolleyCounts = counts;
                reportedInvalidConfiguration = false;
                return;
            }

            if (reportedInvalidConfiguration) return;
            reportedInvalidConfiguration = true;
            Debug.LogWarning("Invalid Gargoyle selector tuning; retaining the last valid configuration.", tuning);
        }

        private void Refill<T>(Queue<T> bag, T[] source, T? previous) where T : struct
        {
            var shuffled = (T[])source.Clone();
            for (var index = shuffled.Length - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (shuffled[index], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[index]);
            }

            if (previous.HasValue && EqualityComparer<T>.Default.Equals(shuffled[0], previous.Value))
            {
                var swapIndex = random.Next(1, shuffled.Length);
                (shuffled[0], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[0]);
            }
            foreach (var entry in shuffled) bag.Enqueue(entry);
        }
    }
}
