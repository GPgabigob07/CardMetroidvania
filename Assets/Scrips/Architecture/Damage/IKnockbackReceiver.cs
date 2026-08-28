using UnityEngine;

namespace TicGame.Architecture
{
    public interface IKnockbackReceiver
    {
        /// <summary>
        /// Applies a resolved knockback impulse from a damage transaction.
        /// </summary>
        void ApplyKnockback(Vector2 direction, float force);
    }
}
