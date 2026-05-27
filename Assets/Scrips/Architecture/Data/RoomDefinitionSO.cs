using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Architecture/Room Definition", fileName = "Room_")]
    public sealed class RoomDefinitionSO : ScriptableObject, IIdentified
    {
        [Header("Identity")]
        [Tooltip("Stable room id used by saves, doors and map reveal data. Falls back to the asset name when empty.")]
        [SerializeField] private string id;

        [Tooltip("Stable id of the area or region that owns this room.")]
        [SerializeField] private string areaId;

        [Tooltip("Human-readable room name shown in tools. Falls back to id when empty.")]
        [SerializeField] private string displayName;

        [Header("Scene")]
        [Tooltip("Unity scene name or loading key associated with this room.")]
        [SerializeField] private string sceneName;

        [Tooltip("Id used to resolve camera bounds for this room.")]
        [SerializeField] private string cameraBoundsId;

        [Header("Room Points")]
        [Tooltip("Spawn point ids available in this room.")]
        [SerializeField] private List<string> spawnPointIds = new List<string>();

        [Tooltip("Door or transition ids available in this room.")]
        [SerializeField] private List<string> doorIds = new List<string>();

        [Tooltip("Checkpoint ids available in this room.")]
        [SerializeField] private List<string> checkpointIds = new List<string>();

        [Header("Map")]
        [Tooltip("Map reveal cell ids marked as discovered when this room is visited.")]
        [SerializeField] private List<string> mapRevealCellIds = new List<string>();

        [Header("Tags")]
        [Tooltip("Tags that describe this room for gates, debug filters and progression tooling.")]
        [SerializeField] private GameplayTagSet roomTags = new GameplayTagSet();

        public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
        public string AreaId => areaId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
        public string SceneName => sceneName;
        public string CameraBoundsId => cameraBoundsId;
        public IReadOnlyList<string> SpawnPointIds => spawnPointIds;
        public IReadOnlyList<string> DoorIds => doorIds;
        public IReadOnlyList<string> CheckpointIds => checkpointIds;
        public IReadOnlyList<string> MapRevealCellIds => mapRevealCellIds;
        public GameplayTagSet RoomTags => roomTags;
    }
}
