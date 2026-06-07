using System.Collections.Generic;
using UnityEngine;

namespace TicGame.Architecture
{
    [CreateAssetMenu(menuName = "TIC/Architecture/Room Definition", fileName = "Room_")]
    public sealed class RoomDefinitionSO : ScriptableObject, IIdentified
    {
        [Header(header: "Identity")]
        [Tooltip(tooltip: "Stable room id used by saves, doors and map reveal data. Falls back to the asset name when empty.")]
        [SerializeField] private string id;

        [Tooltip(tooltip: "Stable id of the area or region that owns this room.")]
        [SerializeField] private string areaId;

        [Tooltip(tooltip: "Human-readable room name shown in tools. Falls back to id when empty.")]
        [SerializeField] private string displayName;

        [Header(header: "Scene")]
        [Tooltip(tooltip: "Unity scene name or loading key associated with this room.")]
        [SerializeField] private string sceneName;

        [Tooltip(tooltip: "Id used to resolve camera bounds for this room.")]
        [SerializeField] private string cameraBoundsId;

        [Header(header: "Room Points")]
        [Tooltip(tooltip: "Spawn point ids available in this room.")]
        [SerializeField] private List<string> spawnPointIds = new List<string>();

        [Tooltip(tooltip: "Door or transition ids available in this room.")]
        [SerializeField] private List<string> doorIds = new List<string>();

        [Tooltip(tooltip: "Checkpoint ids available in this room.")]
        [SerializeField] private List<string> checkpointIds = new List<string>();

        [Header(header: "Map")]
        [Tooltip(tooltip: "Map reveal cell ids marked as discovered when this room is visited.")]
        [SerializeField] private List<string> mapRevealCellIds = new List<string>();

        [Header(header: "Tags")]
        [Tooltip(tooltip: "Tags that describe this room for gates, debug filters and progression tooling.")]
        [SerializeField] private GameplayTagSet roomTags = new GameplayTagSet();

        public string Id => string.IsNullOrWhiteSpace(value: id) ? name : id;
        public string AreaId => areaId;
        public string DisplayName => string.IsNullOrWhiteSpace(value: displayName) ? Id : displayName;
        public string SceneName => sceneName;
        public string CameraBoundsId => cameraBoundsId;
        public IReadOnlyList<string> SpawnPointIds => spawnPointIds;
        public IReadOnlyList<string> DoorIds => doorIds;
        public IReadOnlyList<string> CheckpointIds => checkpointIds;
        public IReadOnlyList<string> MapRevealCellIds => mapRevealCellIds;
        public GameplayTagSet RoomTags => roomTags;
    }
}
