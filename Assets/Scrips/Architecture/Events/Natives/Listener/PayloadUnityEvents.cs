using System;
using UnityEngine.Events;

namespace TicGame.Architecture
{
    [Serializable]
    public sealed class BoolUnityEvent : UnityEvent<bool>
    {
    }

    [Serializable]
    public sealed class IntUnityEvent : UnityEvent<int>
    {
    }

    [Serializable]
    public sealed class FloatUnityEvent : UnityEvent<float>
    {
    }

    [Serializable]
    public sealed class StringUnityEvent : UnityEvent<string>
    {
    }

    [Serializable]
    public sealed class GameStateUnityEvent : UnityEvent<GameState>
    {
    }
}

