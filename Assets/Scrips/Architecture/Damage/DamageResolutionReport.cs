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
            TotalAppliedAmount = targetResults.Sum(selector: result => result.Result.AppliedAmount);
            EffectiveHitCount = targetResults.Count(predicate: result => result.Result.Accepted && result.Result.AppliedAmount > 0f);
            KilledTargets = targetResults.Count(predicate: result => result.Result.Killed);
            RequestedHitStopSeconds = targetResults
                .Where(predicate: result => result.Result is { Accepted: true, AppliedAmount: > 0f })
                .Select(selector: result => result.Result.HitStopSeconds)
                .DefaultIfEmpty()
                .Max();
        }

        public DamageInstance Instance { get; }
        public DamageRequest Request { get; }
        public IReadOnlyList<DamageTargetResult> TargetResults => targetResults;
        public float TotalAppliedAmount { get; }
        public int EffectiveHitCount { get; }
        public int KilledTargets { get; }
        public float RequestedHitStopSeconds { get; }
        public bool IsPrimary => Instance.Provenance.OriginKind == DamageOriginKind.Primary;
        public bool IsSupplemental => Instance.Provenance.OriginKind == DamageOriginKind.Supplemental;
        public bool Allows(DamageProcPolicy policy) => (Instance.ProcPolicy & policy) == policy;
    }
}
