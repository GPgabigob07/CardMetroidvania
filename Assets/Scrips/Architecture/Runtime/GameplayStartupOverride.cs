namespace TicGame.Architecture
{
    /// <summary>One-shot startup address supplied by the Editor area-play workflow.</summary>
    public static class GameplayStartupOverride
    {
        private static string areaId;
        private static string spawnId;

        public static bool HasValue => !string.IsNullOrWhiteSpace(areaId) && !string.IsNullOrWhiteSpace(spawnId);

        public static void Set(string configuredAreaId, string configuredSpawnId)
        {
            areaId = configuredAreaId;
            spawnId = configuredSpawnId;
        }

        public static void Clear()
        {
            areaId = null;
            spawnId = null;
        }

        public static bool TryConsume(out string consumedAreaId, out string consumedSpawnId)
        {
            consumedAreaId = areaId;
            consumedSpawnId = spawnId;
            areaId = null;
            spawnId = null;
            return !string.IsNullOrWhiteSpace(consumedAreaId) && !string.IsNullOrWhiteSpace(consumedSpawnId);
        }
    }
}
