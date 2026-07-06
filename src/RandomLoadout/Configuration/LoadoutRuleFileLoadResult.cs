// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

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
