using UnityEngine;

namespace TicGame.Architecture
{
    public interface IEnemyPatrolMotor2D
    {
        /// <summary>
        /// Gets the current world-space position used by patrol decisions.
        /// </summary>
        Vector2 Position { get; }

        /// <summary>
        /// Gets the current horizontal facing as either -1 or 1.
        /// </summary>
        int FacingDirection { get; }

        /// <summary>
        /// Applies one fixed-rate movement step toward the target.
        /// </summary>
        EnemyPatrolMoveResult MoveTowards(
            Vector2 target,
            float speed,
            float arrivalDistance,
            float fixedDeltaTime);

        /// <summary>
        /// Stops patrol-owned velocity without violating the motor's vertical movement policy.
        /// </summary>
        void Stop();

        /// <summary>
        /// Updates horizontal facing when the supplied direction is nonzero.
        /// </summary>
        void SetFacing(int direction);
    }
}
