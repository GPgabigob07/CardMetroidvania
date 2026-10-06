using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TicGame.Architecture
{
    public sealed class PlaytestMenuView : MonoBehaviour
    {
        [Header("Presentation")]
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private Text eyebrow;
        [SerializeField] private Text heading;
        [SerializeField] private Text body;
        [SerializeField] private Text footer;
        [SerializeField] private Button[] buttons;
        [SerializeField] private GameObject artwork;
        [SerializeField] private EventSystem inputSystem;
        [Header("Verified Controls")]
        [SerializeField, TextArea(8, 24)] private string controls;
        [Header("Settings")]
        [SerializeField] private AudioSettingsPresenter audioSettings;

        private PlaytestSessionController session;
        private PlaytestPauseController pause;
        private string panel = "";
        private string lastState;
        private bool quitting;
        private int restoreSelection;
        private int nextFocusFrame;
        private int focusIndex;
        private bool guideSeen;
        private Selectable pendingSelection;
        private int lastBackFrame = -1;
        public bool IsSettingsOpen => panel == "settings";

        public void ConfigureSettings(AudioSettingsPresenter presenter, Button[] choices)
        { audioSettings = presenter; buttons = choices; }

        public void Configure(GameObject canvas, Text overline, Text title, Text content, Text version,
            Button[] choices, GameObject decoration, EventSystem events, string controlsText)
        {
            canvasRoot = canvas; eyebrow = overline; heading = title; body = content; footer = version;
            buttons = choices; artwork = decoration; inputSystem = events; controls = controlsText;
        }

        private void Start()
        {
            session = GetComponent<PlaytestSessionController>();
            pause = GetComponent<PlaytestPauseController>();
            session.Changed += Invalidate;
            pause.PauseChanged += PauseChanged;
            if (audioSettings != null) audioSettings.BackRequested += Back;
            Invalidate();
        }

        private void Invalidate() => lastState = null;
        private void PauseChanged(bool value) { audioSettings?.Close(); panel = ""; Invalidate(); }

        private void Update()
        {
            if (session == null) return;
            if (session.Gameplay == null) guideSeen = false;
            var guide = session.Gameplay?.Guide;
            if (guide != null && guide.IsDiscovered && !guideSeen && session.Gameplay.IsReady)
            {
                if (pause.TryPause()) { guideSeen = true; panel = "guide"; Invalidate(); }
            }
            if (Keyboard.current?.f1Key.wasPressedThisFrame == true && guide?.IsDiscovered == true)
            {
                if (pause.IsPaused) Back();
                else if (pause.TryPause()) { panel = "guide"; Invalidate(); }
            }
            if (Gamepad.current?.buttonEast.wasPressedThisFrame == true) Back();
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true && session.IsTitle) Back();
            var loading = session.IsTransitioning || (!session.IsTitle && session.Gameplay != null
                && !session.Gameplay.IsReady && session.LastError == null);
            var state = session.LastError != null ? "error" : loading ? "loading"
                : session.IsTitle ? "title" : pause.IsPaused ? "pause" : "hidden";
            var key = state + ":" + panel + ":" + session.LastError;
            if (key != lastState) { Render(state); lastState = key; }
            if (nextFocusFrame > 0 && Time.frameCount >= nextFocusFrame)
            {
                nextFocusFrame = 0;
                if (pendingSelection != null && pendingSelection.gameObject.activeInHierarchy)
                    inputSystem.SetSelectedGameObject(pendingSelection.gameObject);
            }
            if (state == "loading") body.text = "Preparing the area" + new string('.', 1 + (int)(Time.unscaledTime * 2) % 3);
        }

        private void Render(string state)
        {
            if (panel == "settings" && state != "title" && state != "pause")
            { audioSettings?.Close(); panel = ""; }
            var visible = state != "hidden";
            canvasRoot.SetActive(visible);
            inputSystem.gameObject.SetActive(visible);
            if (!visible) return;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            artwork.SetActive(state == "title" && panel == "");
            var settingsVisible = panel == "settings";
            body.gameObject.SetActive(!settingsVisible);
            foreach (var button in buttons) { button.onClick.RemoveAllListeners(); button.gameObject.SetActive(false); }
            eyebrow.text = "PLAYTEST 01  /  WORK IN PROGRESS";
            footer.text = $"{Application.version}   •   Progress is not saved";
            if (state == "error")
            {
                heading.text = "Something interrupted\nthe playtest.";
                body.text = "Try again, or return to the title screen.\nIf it happens again, include Player.log in your report.\n\n" + session.LastError;
                Choice(0, "Retry", () => { _ = session.RetryAsync(); });
                Choice(1, "Return to title", () => { _ = session.ReturnToTitleAsync(); });
                Choice(2, "Quit", Quit);
            }
            else if (state == "loading")
            {
                heading.text = "A moment between worlds.";
                body.text = "Preparing the area…";
            }
            else if (settingsVisible)
            {
                eyebrow.text = "SETTINGS  /  AUDIO";
                heading.text = "Audio settings";
                footer.text = "Audio preferences are saved  •  Playtest progress is not saved";
                audioSettings.Open();
            }
            else if (panel == "controls")
            {
                heading.text = "Find your rhythm.";
                var guide = session.Gameplay?.Guide;
                body.text = "KEYBOARD & MOUSE  /  GAMEPAD (Xbox labels)\n\n" + controls
                    + "\n\nExplore the blue area, then follow the route into the pink area.";
                Choice(0, "Back", Back);
                if (guide?.IsDiscovered == true) Choice(1, "Read the Card Time guide", () => { panel = "guide"; Invalidate(); });
            }
            else if (panel == "guide")
            {
                heading.text = "A moment to choose.";
                body.text = session.Gameplay?.Guide?.Content ?? "Card Time is introduced along the tutorial route.";
                Choice(0, "Back to controls", () => { panel = "controls"; Invalidate(); });
            }
            else if (panel == "confirm")
            {
                heading.text = "Leave this run?";
                body.text = "Your progress will be reset.\nYou can start a fresh playtest from the title screen.";
                Choice(0, "Cancel", Back);
                Choice(1, quitting ? "Quit playtest" : "Return to title", () =>
                {
                    panel = "";
                    if (quitting) Quit(); else _ = session.ReturnToTitleAsync();
                });
            }
            else if (state == "title")
            {
                heading.text = "CARD\nMETROIDVANIA";
                body.text = "A traveller out of time.\nA castle that remembers.\n\nAn exploratory combat & traversal playtest.";
                Choice(0, "Start playtest", () => { _ = session.StartPlaytestAsync(); });
                Choice(1, "Settings", OpenSettings);
                Choice(2, "Controls", OpenControls);
                Choice(3, "Quit", Quit);
            }
            else
            {
                heading.text = "Take a breath.";
                body.text = "PAUSED\nThe world will wait.";
                Choice(0, "Resume", pause.Resume);
                Choice(1, "Settings", OpenSettings);
                Choice(2, "Controls / Card Time guide", OpenControls);
                Choice(3, "Return to title", () => Confirm(false));
                Choice(4, "Quit", () => Confirm(true));
            }
            if (settingsVisible) { pendingSelection = audioSettings.FirstSelectable; nextFocusFrame = Time.frameCount + 1; }
            else Focus(panel == "" ? restoreSelection : 0);
            if (panel == "") restoreSelection = 0;
        }

        private void Choice(int index, string label, Action action)
        {
            var button = buttons[index];
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<Text>().text = label;
            button.onClick.AddListener(() => action());
        }

        private void Focus(int index) { focusIndex = index; pendingSelection = buttons[index]; nextFocusFrame = Time.frameCount + 1; }
        private void RememberSelection()
        {
            for (var i = 0; i < buttons.Length; i++)
                if (inputSystem.currentSelectedGameObject == buttons[i].gameObject) restoreSelection = i;
        }

        private void OpenControls() { RememberSelection(); panel = "controls"; Invalidate(); }
        public void OpenSettings()
        {
            if (session == null || session.IsTransitioning || session.LastError != null || audioSettings == null || session.Services?.Settings == null) return;
            RememberSelection();
            audioSettings.Bind(session.Services.Settings);
            panel = "settings";
            Invalidate();
        }
        private void Confirm(bool quit) { RememberSelection(); quitting = quit; panel = "confirm"; Invalidate(); }

        public void Back()
        {
            if (session == null || session.IsTransitioning || session.LastError != null) return;
            if (lastBackFrame == Time.frameCount) return;
            lastBackFrame = Time.frameCount;
            if (panel == "settings") audioSettings?.Close();
            if (panel == "guide") { panel = "controls"; Invalidate(); }
            else if (panel != "") { panel = ""; Invalidate(); }
            else if (pause.IsPaused) pause.Resume();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            audioSettings?.Close();
            if (audioSettings != null) audioSettings.BackRequested -= Back;
            if (session != null) session.Changed -= Invalidate;
            if (pause != null) pause.PauseChanged -= PauseChanged;
        }
        private void OnDisable() => audioSettings?.Close();
    }
}
