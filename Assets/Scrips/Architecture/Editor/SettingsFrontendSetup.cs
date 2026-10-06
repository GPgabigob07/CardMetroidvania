using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture.EditorTools
{
    public static class SettingsFrontendSetup
    {
        private const string ServicesPath = "Assets/Resources/Runtime/GameplayServices.prefab";
        private const string FrontendPath = "Assets/Resources/Runtime/PlaytestSession.prefab";
        private static readonly Color Paper = new(.953f, .937f, .898f);
        private static readonly Color Cyan = new(.486f, .847f, .922f);
        private static readonly Color ButtonColor = new(.10f, .17f, .22f);

        [MenuItem("TIC/Settings/Setup Audio Settings")]
        public static void Setup()
        {
            EditPrefab(ServicesPath, root =>
            {
                var audio = root.GetComponent<AudioService>();
                if (audio == null) throw new InvalidOperationException("Complete audio setup before settings setup.");
                var settings = root.GetComponent<UserSettingsService>() ?? root.AddComponent<UserSettingsService>();
                var serialized = new SerializedObject(settings);
                var dependency = serialized.FindProperty("audioSettingsSource");
                if (dependency.objectReferenceValue == null) { dependency.objectReferenceValue = audio; serialized.ApplyModifiedPropertiesWithoutUndo(); }
                var composition = new SerializedObject(root.GetComponent<GameplayServicesRoot>());
                var modules = composition.FindProperty("moduleComponents");
                var exists = false;
                for (var index = 0; index < modules.arraySize; index++) if (modules.GetArrayElementAtIndex(index).objectReferenceValue == settings) exists = true;
                if (!exists) { var index = modules.arraySize; modules.InsertArrayElementAtIndex(index); modules.GetArrayElementAtIndex(index).objectReferenceValue = settings; composition.ApplyModifiedPropertiesWithoutUndo(); }
            });
            EditPrefab(FrontendPath, ConfigureFrontend);
            AssetDatabase.SaveAssets();
            Debug.Log("Audio settings setup complete. Open MainMenu and test Settings from title and pause.");
        }

        public static void ConfigureFrontend(GameObject root)
        {
            var view = root.GetComponent<PlaytestMenuView>();
            if (view == null) throw new InvalidOperationException("PlaytestMenuView is required.");
            var serialized = new SerializedObject(view);
            var canvas = serialized.FindProperty("canvasRoot").objectReferenceValue as GameObject;
            var original = serialized.FindProperty("buttons");
            if (canvas == null || original.arraySize < 4) throw new InvalidOperationException("Complete the existing frontend layout before settings setup.");
            var choices = new Button[5];
            for (var index = 0; index < Math.Min(5, original.arraySize); index++) choices[index] = original.GetArrayElementAtIndex(index).objectReferenceValue as Button;
            if (original.arraySize < 5)
            {
                choices[4] = UnityEngine.Object.Instantiate(choices[3], choices[3].transform.parent);
                choices[4].name = "Choice 4";
                for (var index = 0; index < 5; index++)
                {
                    var rect = choices[index].GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(100, -(720 + index * 56));
                    rect.sizeDelta = new Vector2(700, 50);
                    var label = choices[index].GetComponentInChildren<Text>().GetComponent<RectTransform>();
                    label.sizeDelta = new Vector2(650, 40);
                }
            }

            var existing = canvas.transform.Find("Audio Settings");
            AudioSettingsPanel panel;
            AudioSettingsPresenter presenter;
            if (existing == null)
            {
                var panelRoot = Rect("Audio Settings", canvas.transform, new Vector2(100, 340), new Vector2(1150, 500));
                panel = panelRoot.gameObject.AddComponent<AudioSettingsPanel>();
                presenter = panelRoot.gameObject.AddComponent<AudioSettingsPresenter>();
                var sliders = new Slider[5];
                var percentages = new Text[5];
                var names = new[] { "Master", "SFX", "UI", "Music", "Ambience" };
                for (var index = 0; index < 5; index++)
                {
                    var y = index * 70;
                    Label(names[index] + " Label", panelRoot, names[index], new Vector2(0, y), new Vector2(260, 50));
                    sliders[index] = CreateSlider(names[index] + " Volume", panelRoot, new Vector2(330, y + 9));
                    percentages[index] = Label(names[index] + " Percentage", panelRoot, "100%", new Vector2(940, y), new Vector2(130, 50));
                }
                var reset = CreateButton("Reset Audio Defaults", panelRoot, new Vector2(0, 395), new Vector2(380, 58));
                var back = CreateButton("Back", panelRoot, new Vector2(430, 395), new Vector2(260, 58));
                for (var index = 0; index < 5; index++)
                {
                    var navigation = new Navigation { mode = Navigation.Mode.Explicit,
                        selectOnUp = index == 0 ? (Selectable)back : sliders[index - 1],
                        selectOnDown = index == 4 ? (Selectable)reset : sliders[index + 1] };
                    sliders[index].navigation = navigation;
                }
                reset.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = sliders[4], selectOnDown = back, selectOnRight = back };
                back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = reset, selectOnDown = sliders[0], selectOnLeft = reset };
                panel.Configure(sliders, percentages, reset, back);
                presenter.Configure(panel);
                panelRoot.gameObject.SetActive(false);
            }
            else
            {
                panel = existing.GetComponent<AudioSettingsPanel>();
                presenter = existing.GetComponent<AudioSettingsPresenter>();
                if (panel == null || presenter == null) throw new InvalidOperationException("Audio Settings panel is incomplete; inspect its components.");
            }
            view.ConfigureSettings(presenter, choices);
            EditorUtility.SetDirty(view);
        }

        private static Slider CreateSlider(string name, Transform parent, Vector2 position)
        {
            var rect = Rect(name, parent, position, new Vector2(550, 32));
            rect.gameObject.AddComponent<Image>().color = ButtonColor;
            var slider = rect.gameObject.AddComponent<Slider>();
            var fillArea = Stretch("Fill Area", rect, new Vector2(2, 6), new Vector2(-2, -6));
            var fill = Stretch("Fill", fillArea, Vector2.zero, Vector2.zero);
            fill.gameObject.AddComponent<Image>().color = Cyan;
            var handleArea = Stretch("Handle Area", rect, new Vector2(12, 0), new Vector2(-12, 0));
            var handle = Rect("Handle", handleArea, Vector2.zero, new Vector2(24, 44));
            handle.anchorMin = handle.anchorMax = new Vector2(0, .5f); handle.pivot = new Vector2(.5f, .5f);
            var graphic = handle.gameObject.AddComponent<Image>(); graphic.color = Paper;
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = graphic;
            slider.minValue = 0; slider.maxValue = 100; slider.wholeNumbers = true; slider.value = 100;
            FocusOutline(rect.gameObject);
            return slider;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = Rect(name, parent, position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.color = ButtonColor;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var label = Label("Label", rect, name, new Vector2(18, 4), new Vector2(size.x - 36, size.y - 8));
            label.alignment = TextAnchor.MiddleLeft;
            FocusOutline(rect.gameObject);
            return button;
        }

        private static void FocusOutline(GameObject target)
        {
            var outline = target.AddComponent<Outline>(); outline.effectColor = Cyan; outline.effectDistance = new Vector2(2, -2);
            target.AddComponent<PlaytestButtonFocus>();
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y); rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Stretch(string name, Transform parent, Vector2 minimum, Vector2 maximum)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = minimum; rect.offsetMax = maximum;
            return rect;
        }

        private static Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size)
        {
            var label = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 27;
            label.color = Paper; label.text = text; label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false;
            return label;
        }

        private static void EditPrefab(string path, Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
