using UnityEngine;

namespace TicGame.Architecture
{
    [RequireComponent(requiredComponent: typeof(EnemyPoise))]
    public sealed class EnemyPoiseDebugPresentation : MonoBehaviour
    {
        private EnemyPoise poise;

        private void Awake()
        {
            poise = GetComponent<EnemyPoise>();
        }

        private void OnGUI()
        {
            var camera = Camera.main;
            if (camera == null || poise == null)
            {
                return;
            }

            var screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 1.43f);
            if (screen.z < 0f)
            {
                return;
            }

            const float width = 64f;
            const float height = 6f;
            var rect = new Rect(screen.x - width * 0.5f, Screen.height - screen.y, width, height);
            GUI.color = Color.black;
            GUI.Box(rect, string.Empty);
            GUI.color = new Color(1f, 0.7f, 0.15f);
            GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, (rect.width - 2f) * poise.NormalizedPoise, rect.height - 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
