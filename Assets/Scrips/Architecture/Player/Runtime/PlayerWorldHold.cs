using System;
using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    public sealed class PlayerWorldHold : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("Player physics body held while an area becomes ready. Falls back to this object's body.")]
        [SerializeField] private Rigidbody2D body;

        [Tooltip("Player input and ticks suppressed while the body is held. Falls back to this object.")]
        [SerializeField] private PlayerController player;

        private int leaseCount;
        private bool simulatedBeforeHold;

        public bool IsHeld => leaseCount > 0;

        private void Awake()
        {
            body ??= GetComponent<Rigidbody2D>();
            player ??= GetComponent<PlayerController>();
        }

        /// <summary>
        /// Holds player input and physics until the returned lease is disposed.
        /// </summary>
        public IDisposable Acquire()
        {
            if (leaseCount == 0)
            {
                simulatedBeforeHold = body != null && body.simulated;
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                    body.angularVelocity = 0f;
                    body.simulated = false;
                }

                player?.SetWorldHeld(true);
            }

            leaseCount++;
            return new HoldLease(this);
        }

        private void Release()
        {
            if (leaseCount == 0)
            {
                return;
            }

            leaseCount--;
            if (leaseCount > 0)
            {
                return;
            }

            if (body != null)
            {
                body.simulated = simulatedBeforeHold;
            }

            player?.SetWorldHeld(false);
        }

        private sealed class HoldLease : IDisposable
        {
            private PlayerWorldHold owner;

            public HoldLease(PlayerWorldHold owner)
            {
                this.owner = owner;
            }

            public void Dispose()
            {
                if (owner == null)
                {
                    return;
                }

                owner.Release();
                owner = null;
            }
        }
    }
}
