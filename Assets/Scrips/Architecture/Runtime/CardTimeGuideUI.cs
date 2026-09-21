using UnityEngine;
using UnityEngine.InputSystem;

namespace TicGame.Architecture
{
    /// <summary>
    /// Presents the Card Time guide owned by Gameplay while allowing standalone area scenes to host a local copy.
    /// </summary>
    public sealed class CardTimeGuideUI : MonoBehaviour
    {
        [Header("Guide")]
        [Tooltip("Guide content shown after Card Time discovery.")]
        [TextArea(8, 18)]
        [SerializeField] private string guide =
            "Activate: press J + K together, both mouse side buttons, or LB + RB on a gamepad.\n\n"
            + "Card Time slows the world while you choose. The HUD shows which window is available: "
            + "Neutral, Chain, or Finisher. Each window offers different cards.\n\n"
            + "Read each card's effect and Energy cost. Press its displayed slot command to play it immediately. "
            + "Attack or dash cancels selection; letting the timer expire also closes it. Release the activation buttons before trying again.\n\n"
            + "Watch the Energy meter, effect indicators and hit feedback. Playing a card and landing an enhanced attack are separate actions. "
            + "Experiment with positioning and timing.\n\n"
            + "Cyan seals respond to card-enhanced melee. Energy wells let you recharge and retry.";

        private RunProgress progress;
        private bool discovered;
        private bool visible;
        public bool IsDiscovered => discovered;
        public bool IsVisible => visible;

        /// <summary>
        /// Binds the presentation to in-memory progress for the current run.
        /// </summary>
        public void Bind(RunProgress runProgress)
        {
            progress = runProgress;
            discovered = progress?.GuideDiscovered == true;
        }

        /// <summary>
        /// Records guide discovery and opens the guide only for the first discovery in a run.
        /// </summary>
        public void Discover(string guideText)
        {
            if (!string.IsNullOrWhiteSpace(guideText)) guide = guideText;
            var firstDiscovery = !discovered && progress?.GuideDiscovered != true;
            progress?.DiscoverGuide();
            discovered = true;
            if (firstDiscovery) visible = true;
        }

        private void Update()
        {
            if (discovered && (Keyboard.current?.f1Key.wasPressedThisFrame == true
                || Gamepad.current?.startButton.wasPressedThisFrame == true)) visible = !visible;
        }

        private void OnGUI()
        {
            if (!discovered) return;
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            if (GUI.Button(new Rect(1060, 20, 200, 34), visible ? "Close Card Time guide" : "Card Time guide"))
                visible = !visible;
            if (visible)
            {
                GUI.Box(new Rect(660, 70, 590, 580), "Card Time unlocked - F1 / Start to close or reopen");
                var style = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 18 };
                GUI.Label(new Rect(680, 110, 550, 530), guide, style);
            }
            GUI.matrix = previous;
        }
    }
}
