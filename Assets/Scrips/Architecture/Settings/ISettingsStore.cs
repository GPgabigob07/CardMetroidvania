namespace TicGame.Architecture
{
    public interface ISettingsStore
    {
        /// <summary>Reads a stored value; returns false when the key is absent.</summary>
        bool TryReadFloat(string key, out float value);
        /// <summary>Stages a value for the specified key without flushing storage.</summary>
        void WriteFloat(string key, float value);
        /// <summary>Flushes staged preferences; false leaves the caller responsible for retry.</summary>
        bool Flush();
    }
}
