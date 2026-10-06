using UnityEngine;

namespace TicGame.Architecture
{
    public interface IAudioService
    {
        /// <summary>Plays a world cue at a fixed, explicitly captured world position, owned by the invoking object.</summary>
        bool TryPlayWorld(SoundCueSO cue, Vector3 position, GameObject owner);

        /// <summary>Plays a non-positional UI cue, owned by the invoking object.</summary>
        bool TryPlayUi(SoundCueSO cue, GameObject owner);

        /// <summary>Pauses world voices and suppresses new world requests while leaving UI playback available.</summary>
        void SetWorldPaused(bool paused);
    }
}
