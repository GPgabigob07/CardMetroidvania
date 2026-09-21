using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SceneTriggerVolume : MonoBehaviour
    {
        [Header("Owner")]
        [SerializeField] private DirectionalSceneTrigger owner;
        [SerializeField] private SceneTriggerSide side;
        private readonly Dictionary<PlayerController, HashSet<Collider2D>> contacts = new();

        public void Configure(DirectionalSceneTrigger trigger, SceneTriggerSide triggerSide)
        {
            owner = trigger;
            side = triggerSide;
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnDisable() => contacts.Clear();

        /// <summary>
        /// Clears all player collider contacts after a respawn teleport.
        /// </summary>
        public void ResetContacts() => contacts.Clear();

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!SceneStreamingService.DirectionalRequestsAllowed)
            {
                return;
            }

            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || owner == null) return;
            if (!contacts.TryGetValue(player, out var colliders))
            {
                colliders = new HashSet<Collider2D>();
                contacts.Add(player, colliders);
            }
            if (colliders.Add(other) && colliders.Count == 1) owner.Enter(this, player);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null && contacts.TryGetValue(player, out var colliders))
            {
                colliders.Remove(other);
                if (colliders.Count == 0) contacts.Remove(player);
            }
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider2D>();
            if (box == null) return;
            var matrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = side == SceneTriggerSide.A ? Color.cyan : Color.magenta;
            Gizmos.DrawWireCube(box.offset, box.size);
            Gizmos.matrix = matrix;
        }
    }
}
