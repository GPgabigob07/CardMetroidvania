using System;
using System.Collections.Generic;
using System.Linq;

namespace TicGame.Architecture
{
    /// <summary>Serializable scene setup data used to survive an Editor play session.</summary>
    public readonly struct GameplaySceneSetupState
    {
        public GameplaySceneSetupState(string path, bool isLoaded, bool isActive)
        {
            Path = path;
            IsLoaded = isLoaded;
            IsActive = isActive;
        }

        public string Path { get; }
        public bool IsLoaded { get; }
        public bool IsActive { get; }
    }

    public static class GameplayScenePlayState
    {
        public static string Serialize(IEnumerable<GameplaySceneSetupState> setup)
        {
            return string.Join("|", setup.Select(item => string.Join("\t", Escape(item.Path), item.IsLoaded ? "1" : "0", item.IsActive ? "1" : "0")));
        }

        public static GameplaySceneSetupState[] Deserialize(string value)
        {
            if (string.IsNullOrEmpty(value)) return Array.Empty<GameplaySceneSetupState>();
            return value.Split('|').Select(item => item.Split('\t')).Where(parts => parts.Length == 3)
                .Select(parts => new GameplaySceneSetupState(Unescape(parts[0]), parts[1] == "1", parts[2] == "1")).ToArray();
        }

        private static string Escape(string value) => (value ?? string.Empty).Replace("%", "%25").Replace("|", "%7C").Replace("\t", "%09");
        private static string Unescape(string value) => value.Replace("%09", "\t").Replace("%7C", "|").Replace("%25", "%");
    }
}
