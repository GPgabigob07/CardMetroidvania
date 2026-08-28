using TicGame.Architecture;
using UnityEditor;
using UnityEngine;

namespace TicGame.Architecture.EditorTools
{
    [CustomEditor(typeof(PlayerController))]
    public sealed class PlayerControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Card Time Runtime", EditorStyles.boldLabel);

            var controller = (PlayerController)target;
            DrawConfiguration(controller.CardTimeConfig);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to preview live Card Time values.",
                    MessageType.Info);
                return;
            }

            DrawRuntime(controller);
        }

        public override bool RequiresConstantRepaint()
        {
            return EditorApplication.isPlaying;
        }

        private static void DrawConfiguration(PlayerCardTimeConfigSO config)
        {
            using (new EditorGUI.DisabledScope(disabled: true))
            {
                EditorGUILayout.ObjectField(
                    "Configuration",
                    config,
                    typeof(PlayerCardTimeConfigSO),
                    allowSceneObjects: false);
            }

            if (config == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Maximum Duration", $"{config.MaximumActiveDuration:0.000} s");
            EditorGUILayout.LabelField("Active Time Scale", $"{config.ActiveTimeScale:0.000}");
            EditorGUILayout.LabelField("Input Buffer", $"{config.InputBufferDuration:0.000} s");
            EditorGUILayout.LabelField("Post-window Grace", $"{config.PostWindowGraceDuration:0.000} s");
            EditorGUILayout.LabelField("Chord Grace", $"{config.ChordInputGraceDuration:0.000} s");
        }

        private static void DrawRuntime(PlayerController controller)
        {
            var session = controller.CardTimeSession;
            if (session == null)
            {
                EditorGUILayout.HelpBox(
                    "Persistent Card Time services have not bound to this player.",
                    MessageType.Warning);
                return;
            }

            var snapshot = session.Current;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Session", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("State", snapshot.State.ToString());
            EditorGUILayout.LabelField("Available Window", snapshot.AvailableCardTime.ToString());
            EditorGUILayout.LabelField("Selected Window", snapshot.SessionCardTime.ToString());
            EditorGUILayout.Toggle("Is Available", snapshot.IsAvailable);
            EditorGUILayout.Toggle("Is Active", snapshot.IsActive);
            EditorGUILayout.LabelField("Active Elapsed", $"{snapshot.ActiveElapsed:0.000} s");
            EditorGUILayout.LabelField("Active Remaining", $"{snapshot.ActiveRemaining:0.000} s");

            var progress = snapshot.MaximumActiveDuration > 0f
                ? Mathf.Clamp01(snapshot.ActiveElapsed / snapshot.MaximumActiveDuration)
                : 0f;
            var progressRect = EditorGUILayout.GetControlRect(
                hasLabel: false,
                height: EditorGUIUtility.singleLineHeight);
            EditorGUI.ProgressBar(
                progressRect,
                progress,
                $"{progress * 100f:0.0}% elapsed");

            DrawAnimationWindow(controller);
            DrawGlobalTime();
        }

        private static void DrawAnimationWindow(PlayerController controller)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation And Action", EditorStyles.boldLabel);

            var actionFrame = controller.Context?.ActionFrame ?? PlayerActionFrame.Default;
            var actionState = controller.ActionRunner?.CurrentState ?? PlayerActionState.None;
            EditorGUILayout.LabelField("Current Action", actionState.ToString());
            EditorGUILayout.LabelField("Action Phase", actionFrame.Phase.ToString());
            EditorGUILayout.LabelField("Animation Window", actionFrame.CardTimeState.ToString());
            EditorGUILayout.LabelField("Normalized Phase", $"{actionFrame.NormalizedPhaseTime:0.000}");
            EditorGUILayout.Toggle("Animator Authority", actionFrame.HasAnimatorAuthority);
            EditorGUILayout.Toggle("Can Buffer Follow-up", actionFrame.CanBufferFollowUp);
            EditorGUILayout.Toggle("Can Commit Follow-up", actionFrame.CanCommitFollowUp);
        }

        private static void DrawGlobalTime()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Global Time", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Time.timeScale", $"{Time.timeScale:0.000}");
            EditorGUILayout.LabelField("Time.fixedDeltaTime", $"{Time.fixedDeltaTime:0.00000} s");
        }
    }
}
