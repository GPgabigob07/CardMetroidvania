using UnityEngine;

namespace TicGame.Architecture
{
    public struct PlayerInputSnapshot
    {
        public PlayerInputSnapshot(
            Vector2 move,
            bool jumpPressed,
            bool jumpHeld,
            bool jumpReleased,
            bool attackPressed,
            bool dashPressed)
        {
            Move = move;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
            JumpReleased = jumpReleased;
            AttackPressed = attackPressed;
            DashPressed = dashPressed;
        }

        public Vector2 Move { get; }
        public bool JumpPressed { get; }
        public bool JumpHeld { get; }
        public bool JumpReleased { get; }
        public bool AttackPressed { get; }
        public bool DashPressed { get; }

        public static PlayerInputSnapshot None => new PlayerInputSnapshot(move: Vector2.zero, jumpPressed: false, jumpHeld: false, jumpReleased: false, attackPressed: false, dashPressed: false);
    }
}
