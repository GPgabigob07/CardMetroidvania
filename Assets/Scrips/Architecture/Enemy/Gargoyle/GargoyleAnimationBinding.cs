using System;
using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class GargoyleAnimationBinding
    {
        [SerializeField, Tooltip("Stable step ID; empty matches any step in the chosen phase.")]
        private string stepId;
        [SerializeField] private EnemyAttackPhase phase;
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
        public string StepId => stepId;
        public EnemyAttackPhase Phase => phase;
        public IReadOnlyList<Sprite> Frames => Array.AsReadOnly(frames ?? Array.Empty<Sprite>());
        internal GargoyleAnimationBinding Copy()
        {
            var copy = (GargoyleAnimationBinding)MemberwiseClone();
            copy.frames = frames != null ? (Sprite[])frames.Clone() : Array.Empty<Sprite>();
            return copy;
        }
    }
}
