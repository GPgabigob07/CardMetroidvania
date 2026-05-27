using UnityEngine;

namespace TicGame.Architecture
{
    public readonly struct DamageModifierContext
    {
        public DamageModifierContext(
            DamageModifierPhase phase,
            DamageInstance instance,
            GameObject targetObject,
            int targetIndex)
        {
            Phase = phase;
            Instance = instance;
            TargetObject = targetObject;
            TargetIndex = targetIndex;
        }

        public DamageModifierPhase Phase { get; }
        public DamageInstance Instance { get; }
        public GameObject TargetObject { get; }
        public int TargetIndex { get; }
    }
}

