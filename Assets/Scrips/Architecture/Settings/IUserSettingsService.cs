using System;

namespace TicGame.Architecture
{
    public interface IUserSettingsService
    {
        /// <summary>Gets the desired linear audio gain, independent of mixer readiness.</summary>
        float GetAudioVolume(AudioCategory category);
        /// <summary>Accepts a finite desired gain; audio is applied immediately when ready.</summary>
        bool TrySetAudioVolume(AudioCategory category, float gain);
        /// <summary>Restores every audio category to its default gain.</summary>
        void ResetAudioDefaults();
        /// <summary>Persists dirty values and returns whether the flush succeeded.</summary>
        bool Save();
        /// <summary>Gets whether preferences have changes awaiting a successful save.</summary>
        bool IsDirty { get; }
        /// <summary>Notifies views when the desired settings change.</summary>
        event Action Changed;
    }
}
