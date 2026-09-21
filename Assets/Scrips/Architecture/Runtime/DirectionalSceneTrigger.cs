using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class DirectionalSceneTrigger : MonoBehaviour
    {
        [Header("Target Scene")]
        [Tooltip("Full scene asset path, assigned with the scene picker in the Inspector.")]
        [SerializeField] private string targetScenePath;
        [Header("Crossing Actions")]
        [SerializeField] private SceneFlowAction aToB = SceneFlowAction.Unload;
        [SerializeField] private SceneFlowAction bToA = SceneFlowAction.Load;
        [Header("Trigger Volumes")]
        [SerializeField] private SceneTriggerVolume volumeA;
        [SerializeField] private SceneTriggerVolume volumeB;

        private readonly Dictionary<PlayerController, SceneCrossingTracker> crossings = new();
        public string TargetScenePath => targetScenePath;

        public void ConfigureVolumes(SceneTriggerVolume a, SceneTriggerVolume b)
        {
            volumeA = a;
            volumeB = b;
            a.Configure(this, SceneTriggerSide.A);
            b.Configure(this, SceneTriggerSide.B);
        }

        private void OnEnable() => crossings.Clear();

        /// <summary>
        /// Clears remembered direction and trigger contacts after a player teleport.
        /// </summary>
        public void ResetCrossing()
        {
            crossings.Clear();
            volumeA?.ResetContacts();
            if (volumeB != volumeA)
            {
                volumeB?.ResetContacts();
            }
        }

        public void Enter(SceneTriggerVolume volume, PlayerController player)
        {
            if (!SceneStreamingService.DirectionalRequestsAllowed
                || !isActiveAndEnabled
                || player == null
                || (volume != volumeA && volume != volumeB)) return;
            if (volumeA == null || volumeB == null || volumeA == volumeB)
            {
                Debug.LogError("Assign two distinct scene trigger volumes.", this);
                return;
            }
            if (!crossings.TryGetValue(player, out var tracker))
            {
                tracker = new SceneCrossingTracker();
                crossings.Add(player, tracker);
            }
            var action = tracker.Enter(volume == volumeA ? SceneTriggerSide.A : SceneTriggerSide.B, aToB, bToA);
            if (action == SceneFlowAction.None) return;
            if (string.IsNullOrWhiteSpace(targetScenePath) || targetScenePath == gameObject.scene.path)
            {
                Debug.LogError("Assign a target scene different from the scene containing this trigger.", this);
                return;
            }
            SceneStreamingService.Request(targetScenePath, action == SceneFlowAction.Load);
        }
    }
}
