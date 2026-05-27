namespace TicGame.Architecture
{
    public enum PlayerMovementState
    {
        Grounded = 0,
        Airborne = 10,
        WallSlide = 20,
        Dash = 30,
        Attack = 40,
        Hurt = 50,
        Dead = 60,
        Interact = 70
    }
}

