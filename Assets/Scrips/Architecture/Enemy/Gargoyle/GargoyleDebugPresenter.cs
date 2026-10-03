using UnityEngine;
namespace TicGame.Architecture
{
    public sealed class GargoyleDebugPresenter : MonoBehaviour
    {
        [Header("Test Arena Bindings")]
        [SerializeField] private GargoyleBrain brain;
        [SerializeField] private GameObject player;
        [SerializeField] private Vector3 enemyStart;
        [SerializeField] private Vector3 playerStart;
        public void Configure(GargoyleBrain brain, GameObject player)
        {
            this.brain = brain; this.player = player; enemyStart = brain.transform.position; playerStart = player.transform.position;
        }
        public void ResetArena()
        {
            if (brain == null || player == null || !brain.IsInitialized) return;
            var actor = brain.GetComponent<EnemyActor>(); actor.ResetActor(); brain.GetComponent<EnemyPoise>().RestoreToFull();
            brain.ResetEncounter(); brain.transform.position = enemyStart;
            var enemyBody = brain.GetComponent<Rigidbody2D>(); enemyBody.position = enemyStart; enemyBody.linearVelocity = Vector2.zero;
            player.GetComponent<PlayerController>().ResetTransientState(); player.GetComponent<PlayerController>().ResetCardTimeForFullRun();
            player.GetComponent<PlayerCardRuntime>().ClearNewCardEffects(); player.GetComponent<SimpleHealth>().Initialize();
            player.GetComponent<PlayerResourceWallet>().Initialize();
            player.transform.position = playerStart; var body = player.GetComponent<Rigidbody2D>(); body.position = playerStart; body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
        private void OnGUI()
        {
            if (brain == null || !brain.IsInitialized) return;
            GUILayout.BeginArea(new Rect(12, 12, 360, 210), GUI.skin.box);
            GUILayout.Label("Gargoyle Sentinel — single-frame draft; animations pending");
            var actor = brain.GetComponent<EnemyActor>(); var poise = brain.GetComponent<EnemyPoise>(); var attack = brain.CurrentAttack;
            GUILayout.Label($"HP {actor.Health.CurrentHealth:0.0}/{actor.Health.MaximumHealth:0.0} | Poise {poise.CurrentPoise:0.0}/{poise.MaximumPoise:0.0}");
            GUILayout.Label($"{brain.CurrentState} | {attack.StepId} {attack.Phase} {attack.Elapsed:0.00}/{attack.PhaseDuration:0.00}");
            GUILayout.Label($"Next {brain.QueuedFamily} | Bag remaining {brain.RemainingFamilies} | Nova attempts {brain.NovaAttemptCount}");
            GUILayout.Label("Cyan core: empowered primary Poise hits. Beam: face it with Ward.");
            GUILayout.Label("Movement remains a valid escape. Reset preserves consumed Mend stock.");
            if (GUILayout.Button("Reset encounter / restore HP and Energy")) ResetArena();
            GUILayout.EndArea();
        }
    }
}
