namespace RandomLoadout
{
    internal sealed class BossRushEncounter
    {
        public BossRushEncounter(string floorKey, string loadLevelToken)
        {
            FloorKey = floorKey ?? string.Empty;
            LoadLevelToken = loadLevelToken ?? string.Empty;
            SceneName = EtgFloorSceneResolver.ResolveNormalizedSceneName(LoadLevelToken);
        }

        public string FloorKey { get; private set; }

        public string LoadLevelToken { get; private set; }

        public string SceneName { get; private set; }
    }
}
