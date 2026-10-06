using System;
using UnityEngine;
using UnityEngine.UI;

namespace TicGame.Architecture
{
    public sealed class AudioSettingsPanel : MonoBehaviour
    {
        [Header("Audio Rows — Master, SFX, UI, Music, Ambience")]
        [SerializeField] private Slider[] sliders;
        [SerializeField] private Text[] percentages;
        [Header("Actions")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Button backButton;
        public Slider[] Sliders => sliders;
        public Button ResetButton => resetButton;
        public Button BackButton => backButton;
        public Selectable FirstSelectable => sliders != null && sliders.Length > 0 ? sliders[0] : null;

        public void Configure(Slider[] rows, Text[] labels, Button reset, Button back)
        {
            if (rows == null || labels == null || rows.Length != 5 || labels.Length != 5) throw new ArgumentException("Audio settings require five sliders and percentage labels.");
            sliders = rows; percentages = labels; resetButton = reset; backButton = back;
        }

        public void Refresh(IUserSettingsService settings)
        {
            for (var index = 0; index < 5; index++)
            {
                var percent = Mathf.RoundToInt(settings.GetAudioVolume((AudioCategory)index) * 100);
                sliders[index].SetValueWithoutNotify(percent);
                percentages[index].text = percent + "%";
            }
        }
    }
}
