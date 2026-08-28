namespace TicGame.Architecture
{
    public enum GolemChargerState
    {
        Idle = 0,
        Patrol = 10,
        Windup = 20,
        Charge = 30,
        Interrupted = 40,
        Recovery = 50,
        Dead = 60
    }
}
