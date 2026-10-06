namespace TicGame.Architecture
{
    public interface IAudioSettingsService
    {
        /// <summary>Gets whether native mixer startup defaults have been applied.</summary>
        bool IsReady { get; }
        /// <summary>Signals mixer readiness once per module initialization.</summary>
        event System.Action Ready;
        /// <summary>Applies a finite linear gain clamped to [0,1]; returns false if the category cannot be applied.</summary>
        bool SetVolume(AudioCategory category, float normalizedGain);

        /// <summary>Gets the current linear gain for the category.</summary>
        float GetVolume(AudioCategory category);
    }
}
