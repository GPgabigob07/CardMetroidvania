namespace TicGame.Architecture
{
    public enum PlayerAnimationState
    {
        Idle = 0,
        WalkBegin = 10,
        WalkLoop = 20,
        JumpUp = 30,
        Fall = 40,
        GroundedFall = 50,
        HardLanding = 60,
        Dash = 70,
        Attack1Reading = 100,
        Attack1Execution = 110,
        Attack1Recovery = 120,
        Attack2Reading = 130,
        Attack2Execution = 140,
        Attack2Recovery = 150,
        Attack3Reading = 160,
        Attack3Execution = 170,
        Attack3Recovery = 180,
        CardChain = 190,
        Finisher = 200,
        Hurt = 210,
        Dead = 220
    }
}
