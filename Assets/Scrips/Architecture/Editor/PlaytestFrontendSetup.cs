using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TicGame.Architecture.EditorTools
{
    public static class PlaytestFrontendSetup
    {
        private const string PrefabPath = "Assets/Resources/Runtime/PlaytestSession.prefab";
        private static readonly Color Ink = new(.063f, .082f, .129f);
        private static readonly Color Paper = new(.953f, .937f, .898f);
        private static readonly Color Cyan = new(.486f, .847f, .922f);

        [MenuItem("TIC/Setup/Create Or Update Playtest Frontend")]
        public static void CreateOrUpdate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var previous = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                var root = new GameObject("Playtest Frontend");
                root.AddComponent<PlaytestSessionController>();
                root.AddComponent<PlaytestPauseController>();
                var view = root.AddComponent<PlaytestMenuView>();
                var canvas = new GameObject("Playtest Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas.transform.SetParent(root.transform, false);
                canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.GetComponent<Canvas>().sortingOrder = 30000;
                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
                var background = Rect("Background", canvas.transform, Vector2.zero, new Vector2(1920, 1080));
                background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one;
                background.offsetMin = Vector2.zero; background.offsetMax = Vector2.zero;
                background.gameObject.AddComponent<Image>().color = Ink;
                var art = Rect("Castle and Cards", canvas.transform, new Vector2(1100, 90), new Vector2(740, 800));
                for (var i = 0; i < 7; i++)
                {
                    var tower = Rect("Tower " + i, art, new Vector2(i * 100, 250 + (i % 3) * 65), new Vector2(76, 700));
                    tower.gameObject.AddComponent<Image>().color = new Color(.09f, .15f, .22f);
                }
                for (var i = 0; i < 3; i++)
                {
                    var card = Rect("Card " + i, art, new Vector2(145 + i * 92, 180 - i * 23), new Vector2(220, 340));
                    card.localRotation = Quaternion.Euler(0, 0, 16 - i * 14);
                    card.gameObject.AddComponent<Image>().color = new Color(.12f + i * .025f, .20f, .28f);
                    var outline = card.gameObject.AddComponent<Outline>();
                    outline.effectColor = i == 1 ? new Color(.84f, .56f, .71f) : Cyan;
                    outline.effectDistance = new Vector2(2, -2);
                    var rune = Label("Rune", card, "◇", new Vector2(25, 85), new Vector2(170, 170), 120);
                    rune.alignment = TextAnchor.MiddleCenter; rune.color = outline.effectColor;
                }
                var eyebrow = Label("Eyebrow", canvas.transform, "", new Vector2(100, 65), new Vector2(1700, 50), 24);
                eyebrow.color = Cyan;
                var heading = Label("Heading", canvas.transform, "", new Vector2(100, 140), new Vector2(1620, 175), 68);
                heading.fontStyle = FontStyle.Bold;
                var body = Label("Body", canvas.transform, "", new Vector2(100, 340), new Vector2(1590, 370), 27);
                body.resizeTextForBestFit = true; body.resizeTextMinSize = 22; body.resizeTextMaxSize = 27;
                var footer = Label("Build and save notice", canvas.transform, "", new Vector2(100, 1010), new Vector2(1700, 45), 22);
                footer.color = new Color(.68f, .74f, .81f);
                var buttons = new Button[4];
                for (var i = 0; i < buttons.Length; i++)
                {
                    var rect = Rect("Choice " + i, canvas.transform, new Vector2(100, 720 + i * 68), new Vector2(700, 58));
                    var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.10f, .17f, .22f);
                    var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                    var border = rect.gameObject.AddComponent<Outline>(); border.effectColor = Cyan;
                    border.effectDistance = new Vector2(2, -2);
                    rect.gameObject.AddComponent<PlaytestButtonFocus>();
                    var colors = button.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(.6f, .95f, 1f);
                    colors.selectedColor = new Color(.6f, .95f, 1f);
                    colors.pressedColor = new Color(.85f, .65f, .8f);
                    button.colors = colors;
                    var label = Label("Label", rect, "", new Vector2(25, 5), new Vector2(650, 48), 27);
                    label.alignment = TextAnchor.MiddleLeft;
                    buttons[i] = button;
                }
                var events = new GameObject("Menu Input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(root.transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                view.Configure(canvas, eyebrow, heading, body, footer, buttons, art.gameObject, events.GetComponent<EventSystem>(), ReadControls());
                SettingsFrontendSetup.ConfigureFrontend(root);
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
                var camera = new GameObject("Title Camera", typeof(Camera));
                camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
                camera.GetComponent<Camera>().backgroundColor = Ink;
                EditorSceneManager.SaveScene(scene, PlaytestSessionController.TitleScene);
                PlayerSettings.productName = "CardMetroidvania";
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(PlaytestSessionController.TitleScene, true),
                    new EditorBuildSettingsScene(PlaytestSessionController.GameplayScene, true),
                    new EditorBuildSettingsScene("Assets/Scenes/BlueArea_Tutorial.unity", true),
                    new EditorBuildSettingsScene("Assets/Scenes/PinkArea_Perimeters.unity", true)
                };
                AssetDatabase.SaveAssets();
                Debug.Log("Playtest frontend setup complete. Open MainMenu to preview.");
            }
            finally
            {
                if (!Application.isBatchMode && previous.Length > 0 && previous.All(item => !string.IsNullOrEmpty(item.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static string ReadControls()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            var player = new SerializedObject(prefab.GetComponent<PlayerController>());
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            string Binding(string field, string fallback = null)
            {
                var reference = player.FindProperty(field).objectReferenceValue as InputActionReference;
                var action = reference?.action ?? (fallback != null ? actions.FindAction("Player/" + fallback) : null);
                if (action == null) throw new System.InvalidOperationException("Missing player input: " + field);
                var keyboard = action.GetBindingDisplayString(group: "Keyboard&Mouse");
                if (string.IsNullOrWhiteSpace(keyboard))
                    keyboard = string.Join(", ", action.bindings.Where(binding => binding.isPartOfComposite
                        && binding.groups.Contains("Keyboard&Mouse")).Select(binding =>
                            InputControlPath.ToHumanReadableString(binding.effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice)));
                return keyboard + "   /   "
                    + action.GetBindingDisplayString(group: "Gamepad");
            }
            return $"Move: {Binding("moveAction")}\nJump: {Binding("jumpAction")}\nMelee: {Binding("attackAction")}\nDash: {Binding("dashAction")}\n"
                + "Pause / Resume: Esc or gamepad Start   •   Menu: arrows / D-pad, Enter / south button\n"
                + $"Card Time chord (after unlock): [{Binding("cardTimeLeftAction", "CardTimeLeft")}] + [{Binding("cardTimeRightAction", "CardTimeRight")}]\n"
                + "Choose a card with its displayed slot command. Attack or dash cancels selection.";
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
            return rect;
        }

        private static Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var label = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text; label.fontSize = fontSize; label.color = Paper;
            label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }
    }
}
