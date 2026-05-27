using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    public struct InteractionContext
    {
        [SerializeField] private GameObject actor;
        [SerializeField] private GameObject target;
        [SerializeField] private Vector2 worldPosition;
        [SerializeField] private GameplayTagSet tags;

        public InteractionContext(GameObject actor, GameObject target, Vector2 worldPosition, GameplayTagSet tags = null)
        {
            this.actor = actor;
            this.target = target;
            this.worldPosition = worldPosition;
            this.tags = tags;
        }

        public GameObject Actor => actor;
        public GameObject Target => target;
        public Vector2 WorldPosition => worldPosition;
        public GameplayTagSet Tags => tags;
    }
}

