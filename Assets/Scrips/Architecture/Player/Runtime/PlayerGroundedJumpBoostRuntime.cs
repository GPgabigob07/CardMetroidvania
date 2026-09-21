using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerGroundedJumpBoostRuntime : MonoBehaviour
    {
        private float multiplier = 1f;

        public bool IsArmed { get; private set; }
        public bool CanArm => !IsArmed;

        public bool Arm(float jumpMultiplier, CardDefinitionSO card)
        {
            if (!CanArm || !float.IsFinite(jumpMultiplier) || jumpMultiplier <= 0f)
            {
                return false;
            }

            multiplier = jumpMultiplier;
            IsArmed = true;
            return true;
        }

        public float ConsumeGroundedLaunch(float baseVelocity)
        {
            if (!IsArmed)
            {
                return baseVelocity;
            }

            var boostedVelocity = baseVelocity * multiplier;
            Clear();
            return boostedVelocity;
        }

        public void Clear()
        {
            multiplier = 1f;
            IsArmed = false;
        }
    }
}
