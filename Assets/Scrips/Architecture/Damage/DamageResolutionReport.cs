using System.Collections.Generic;
using System.Linq;

namespace TicGame.Architecture
{
    public sealed class DamageResolutionReport
    {
        private readonly IReadOnlyList<DamageTargetResult> targetResults;

        public DamageResolutionReport(DamageInstance instance, DamageRequest request, IReadOnlyList<DamageTargetResult> targetResults)
        {
            Instance = instance;
            Request = request;
            this.targetResults = targetResults;
            TotalAppliedAmount = targetResults.Sum(result => result.Result.AppliedAmount);
            EffectiveHitCount = targetResults.Count(result => result.Result.Accepted && result.Result.AppliedAmount > 0f);
            KilledTargets = targetResults.Count(result => result.Result.Killed);
        }

        public DamageInstance Instance { get; }
        public DamageRequest Request { get; }
        public IReadOnlyList<DamageTargetResult> TargetResults => targetResults;
        public float TotalAppliedAmount { get; }
        public int EffectiveHitCount { get; }
        public int KilledTargets { get; }
    }
}

