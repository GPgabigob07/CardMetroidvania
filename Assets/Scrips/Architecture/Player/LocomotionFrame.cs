using UnityEngine;

namespace TicGame.Architecture
{
    public struct LocomotionFrame
    {
        public LocomotionFrame(
            Vector2 velocity,
            float gravityScale,
            bool allowHorizontalInput,
            bool allowGravity,
            bool lockFacing)
        {
            Velocity = velocity;
            GravityScale = gravityScale;
            AllowHorizontalInput = allowHorizontalInput;
            AllowGravity = allowGravity;
            LockFacing = lockFacing;
        }

        public Vector2 Velocity;
        public float GravityScale;
        public bool AllowHorizontalInput;
        public bool AllowGravity;
        public bool LockFacing;
    }
}
