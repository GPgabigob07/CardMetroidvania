using UnityEngine;

namespace TicGame.Architecture
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class AreaRespawnCheckpoint : MonoBehaviour
    {
        [Header("Destination")]
        [Tooltip("Spawn marker in this area that becomes the run's respawn destination.")]
        [SerializeField] private AreaSpawnPoint spawn;

        private RunProgress progress;
        private string areaId;
        private PlayerController boundPlayer;

        /// <summary>
        /// Binds this area checkpoint to the session progress and persistent player.
        /// </summary>
        public void Bind(RunProgress configuredProgress, string configuredAreaId, PlayerController player)
        {
            progress = configuredProgress;
            areaId = configuredAreaId;
            boundPlayer = player;
        }

        /// <summary>
        /// Assigns the area-local spawn marker used by this checkpoint.
        /// </summary>
        public void Configure(AreaSpawnPoint configuredSpawn)
        {
            spawn = configuredSpawn;
        }

        /// <summary>
        /// Activates this checkpoint for the bound player when its marker is valid in this scene.
        /// </summary>
        public bool TryActivate(PlayerController candidate)
        {
            var trigger = GetComponent<Collider2D>();
            if (!isActiveAndEnabled || progress == null || candidate == null || candidate != boundPlayer
                || spawn == null || spawn.gameObject.scene != gameObject.scene
                || string.IsNullOrWhiteSpace(spawn.SpawnId)
                || trigger == null || !trigger.isTrigger
                || string.IsNullOrWhiteSpace(areaId))
            {
                return false;
            }

            var address = new SpawnAddress(areaId, spawn.SpawnId);
            if (progress.Respawn != address)
            {
                progress.SetRespawn(address);
            }

            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryActivate(other.GetComponentInParent<PlayerController>());
        }

        private void OnDrawGizmos()
        {
            var trigger = GetComponent<Collider2D>();
            if (trigger == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(trigger.bounds.center, trigger.bounds.size);
            if (spawn != null)
            {
                Gizmos.DrawLine(trigger.bounds.center, spawn.transform.position);
                Gizmos.DrawWireSphere(spawn.transform.position, 0.15f);
            }
        }
    }
}
