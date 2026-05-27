using System;

namespace TicGame.Architecture
{
    [Serializable]
    public readonly struct DamageTargetResult
    {
        public DamageTargetResult(DamageContext context, DamageResult result)
        {
            Context = context;
            Result = result;
        }

        public DamageContext Context { get; }
        public DamageResult Result { get; }
    }
}

