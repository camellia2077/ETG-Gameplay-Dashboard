namespace RandomLoadout
{
    internal sealed class LoadoutPresetEditorEntry
    {
        public LoadoutPresetEditorEntry(string name, bool isActive, int ruleCount, int specificCount, int randomCount)
        {
            Name = name ?? string.Empty;
            IsActive = isActive;
            RuleCount = ruleCount;
            SpecificCount = specificCount;
            RandomCount = randomCount;
        }

        public string Name { get; private set; }

        public bool IsActive { get; private set; }

        public int RuleCount { get; private set; }

        public int SpecificCount { get; private set; }

        public int RandomCount { get; private set; }
    }
}
