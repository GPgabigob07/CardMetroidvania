using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TicGame.Architecture
{
    public sealed class AudioSettingsPresenter : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private AudioSettingsPanel panel;
        private IUserSettingsService settings;
        private UnityAction<float>[] callbacks;
        public bool IsOpen { get; private set; }
        public Selectable FirstSelectable => panel?.FirstSelectable;
        public event Action BackRequested;
        public void Configure(AudioSettingsPanel view) => panel = view;

        public void Bind(IUserSettingsService service)
        {
            if (settings == service) return;
            Close();
            settings = service;
        }

        public void Open()
        {
            if (IsOpen || settings == null || panel == null) return;
            IsOpen = true;
            panel.gameObject.SetActive(true);
            callbacks = new UnityAction<float>[5];
            for (var index = 0; index < 5; index++)
            {
                var category = (AudioCategory)index;
                callbacks[index] = percent => settings.TrySetAudioVolume(category, percent / 100f);
                panel.Sliders[index].onValueChanged.AddListener(callbacks[index]);
            }
            panel.ResetButton.onClick.AddListener(ResetDefaults);
            panel.BackButton.onClick.AddListener(RequestBack);
            settings.Changed += Refresh;
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            settings.Changed -= Refresh;
            for (var index = 0; index < 5; index++) panel.Sliders[index].onValueChanged.RemoveListener(callbacks[index]);
            panel.ResetButton.onClick.RemoveListener(ResetDefaults);
            panel.BackButton.onClick.RemoveListener(RequestBack);
            settings.Save();
            panel.gameObject.SetActive(false);
        }

        public void ResetDefaults() => settings?.ResetAudioDefaults();
        private void RequestBack() => BackRequested?.Invoke();
        private void Refresh() { if (IsOpen) panel.Refresh(settings); }
        private void OnDisable() => Close();
        private void OnDestroy() => Close();
    }
}
