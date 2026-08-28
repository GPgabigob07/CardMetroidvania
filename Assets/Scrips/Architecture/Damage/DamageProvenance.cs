using System;

namespace TicGame.Architecture
{
    [Serializable]
    public readonly struct DamageProvenance
    {
        public DamageProvenance(
            DamageOriginKind originKind,
            string parentInstanceId,
            string rootInstanceId,
            string effectId,
            int chainDepth)
        {
            OriginKind = originKind;
            ParentInstanceId = parentInstanceId;
            RootInstanceId = rootInstanceId;
            EffectId = effectId;
            ChainDepth = Math.Max(0, chainDepth);
        }

        public DamageOriginKind OriginKind { get; }
        public string ParentInstanceId { get; }
        public string RootInstanceId { get; }
        public string EffectId { get; }
        public int ChainDepth { get; }

        public static DamageProvenance Primary(string instanceId)
        {
            return new DamageProvenance(
                originKind: DamageOriginKind.Primary,
                parentInstanceId: null,
                rootInstanceId: instanceId,
                effectId: null,
                chainDepth: 0);
        }

        public static DamageProvenance Supplemental(
            string parentInstanceId,
            string rootInstanceId,
            string effectId,
            int chainDepth = 1)
        {
            return new DamageProvenance(
                originKind: DamageOriginKind.Supplemental,
                parentInstanceId: parentInstanceId,
                rootInstanceId: rootInstanceId,
                effectId: effectId,
                chainDepth: chainDepth);
        }
    }
}
