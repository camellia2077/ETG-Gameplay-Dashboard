namespace RandomLoadout
{
    internal sealed class EtgFloorDefinition
    {
        public EtgFloorDefinition(string commandToken, string loadSceneName, string normalizedSceneName, bool canLoadFromFoyer)
        {
            CommandToken = commandToken ?? string.Empty;
            LoadSceneName = loadSceneName ?? string.Empty;
            NormalizedSceneName = normalizedSceneName ?? string.Empty;
            CanLoadFromFoyer = canLoadFromFoyer;
        }

        public string CommandToken { get; private set; }

        public string LoadSceneName { get; private set; }

        public string NormalizedSceneName { get; private set; }

        public bool CanLoadFromFoyer { get; private set; }
    }
}
