using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class TutorialEnergyRefill : MonoBehaviour
    {
        [Header("Tutorial Recovery")]
        [Tooltip("Resource replenished while the player rests inside this zone.")]
        [SerializeField] private ResourceDefinitionSO energy;
        [Min(0.1f)]
        [Tooltip("Energy replenished per game second. Repeatable for gate experimentation.")]
        [SerializeField] private float energyPerSecond = 10;

        public void Configure(ResourceDefinitionSO resource) => energy = resource;

        private void OnTriggerStay2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.CardTimeUnlocked) return;
            player.GetComponent<PlayerResourceWallet>()?.Gain(energy, energyPerSecond * Time.fixedDeltaTime);
        }
    }
}
