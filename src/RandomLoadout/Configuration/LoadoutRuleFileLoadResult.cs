namespace RandomLoadout
{
    internal sealed class LoadoutRuleFileLoadResult
    {
        public LoadoutRuleFileLoadResult(LoadoutRuleDefinition[] definitions, LoadoutRuleFilePickupModel[] activePresetPickups, string[] messages, string[] warnings)
        {
            Definitions = definitions;
            ActivePresetPickups = activePresetPickups;
            Messages = messages;
            Warnings = warnings;
        }

        public LoadoutRuleDefinition[] Definitions { get; private set; }

        public LoadoutRuleFilePickupModel[] ActivePresetPickups { get; private set; }

        public string[] Messages { get; private set; }

        public string[] Warnings { get; private set; }
    }
}
