namespace TicGame.Architecture
{
    public static class PlayerAttackSequence
    {
        public static PlayerActionState GetNext(PlayerActionState current)
        {
            switch (current)
            {
                case PlayerActionState.Attack1:
                    return PlayerActionState.Attack2;
                case PlayerActionState.Attack2:
                    return PlayerActionState.Attack3;
                default:
                    return PlayerActionState.None;
            }
        }

        public static PlayerCardTimeState GetCardTime(PlayerActionState state)
        {
            return state switch
            {
                PlayerActionState.Attack1 => PlayerCardTimeState.Chain,
                PlayerActionState.Attack2 => PlayerCardTimeState.Chain,
                PlayerActionState.Attack3 => PlayerCardTimeState.Finisher,
                _ => PlayerCardTimeState.Neutral
            };
        }
    }
}
