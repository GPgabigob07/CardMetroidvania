using System;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class GargoyleLiveTuning
    {
        private readonly GargoyleTuningSO tuning;
        private bool reportedInvalid;
        public GargoyleTuningValues Values { get; private set; }

        public GargoyleLiveTuning(GargoyleTuningSO tuning)
        {
            this.tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (!tuning.TryReadSimulationValues(out var values))
                throw new ArgumentException("Initial Gargoyle simulation tuning is invalid.", nameof(tuning));
            Values = values;
        }

        public bool Refresh()
        {
            if (tuning.TryReadSimulationValues(out var values))
            {
                Values = values;
                reportedInvalid = false;
                return true;
            }
            if (!reportedInvalid)
                Debug.LogWarning("Invalid Gargoyle simulation tuning; retaining the last valid configuration.", tuning);
            reportedInvalid = true;
            return false;
        }
    }
}
