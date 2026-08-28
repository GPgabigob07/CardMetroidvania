using UnityEngine;

namespace TicGame.Architecture
{
    public sealed class PlayerCardTimeDebugPresenter : MonoBehaviour
    {
        private static readonly Color AvailableColor = new Color(0.1f, 0.85f, 1f, 0.9f);
        private static readonly Color ActiveColor = new Color(1f, 0.75f, 0.1f, 0.95f);
        private static readonly Color InvalidColor = new Color(1f, 0.2f, 0.2f, 0.95f);

        private const float FeedbackDuration = 0.6f;

        private ICardTimeSession session;
        private float feedbackRemaining;
        private string feedbackText;
        private GUIStyle labelStyle;
        private GUIStyle boxStyle;

        public void Initialize(ICardTimeSession cardTimeSession)
        {
            session = cardTimeSession;
        }

        public void ShowInvalidActivation()
        {
            ShowFeedback("NO CARD TIME");
        }

        public void ShowRejectedCommit(string reason)
        {
            ShowFeedback("CARD COMMIT REJECTED " + reason);
        }

        private void Update()
        {
            feedbackRemaining = Mathf.Max(
                a: 0f,
                b: feedbackRemaining - Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (feedbackRemaining > 0f)
            {
                DrawPanel(
                    text: feedbackText,
                    color: InvalidColor,
                    rect: new Rect(x: 20f, y: 20f, width: 260f, height: 64f));
                return;
            }

            if (session == null)
            {
                return;
            }

            var snapshot = session.Current;
            if (snapshot.IsActive)
            {
                DrawPanel(
                    text: $"{snapshot.SessionCardTime} CARD TIME\n{snapshot.ActiveRemaining:0.0}s  |  ATTACK: COMMIT",
                    color: ActiveColor,
                    rect: new Rect(x: 20f, y: 20f, width: 360f, height: 76f));
                return;
            }

            if (snapshot.IsAvailable)
            {
                DrawPanel(
                    text: $"{snapshot.AvailableCardTime} CARD TIME AVAILABLE",
                    color: AvailableColor,
                    rect: new Rect(x: 20f, y: 20f, width: 340f, height: 64f));
            }
        }

        private void ShowFeedback(string text)
        {
            feedbackText = text;
            feedbackRemaining = FeedbackDuration;
        }

        private void DrawPanel(string text, Color color, Rect rect)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.Box(position: rect, text: string.Empty, style: boxStyle);
            GUI.color = Color.white;
            GUI.Label(position: rect, text: text, style: labelStyle);
            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            labelStyle.normal.textColor = Color.white;

            boxStyle = new GUIStyle(GUI.skin.box);
        }
    }
}
