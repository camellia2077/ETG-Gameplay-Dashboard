// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    internal sealed class LoadoutRuleFileModel
    {
        public LoadoutRuleFileModel()
        {
            Rules = new LoadoutRuleFileRuleModel[0];
            Presets = new LoadoutRuleFilePresetModel[0];
        }

        public LoadoutRuleFileRuleModel[] Rules { get; set; }

        public LoadoutRuleFilePresetModel[] Presets { get; set; }
    }

    internal sealed class LoadoutRuleFilePresetModel
    {
        public LoadoutRuleFilePresetModel()
        {
            Rules = new LoadoutRuleFileRuleModel[0];
            Pickups = new LoadoutRuleFilePickupModel[0];
        }

        public string Id { get; set; }

        public string DisplayNameKey { get; set; }

        public string Name { get; set; }

        public string SourcePath { get; set; }

        public LoadoutRuleFileRuleModel[] Rules { get; set; }

        public LoadoutRuleFilePickupModel[] Pickups { get; set; }
    }

    internal sealed class LoadoutRuleFilePickupModel
    {
        public string Type { get; set; }

        public int Count { get; set; }
    }

    internal sealed class LoadoutRuleFileRuleModel
    {
        public bool Enabled { get; set; }

        public string Mode { get; set; }

        public string Category { get; set; }

        public int Count { get; set; }

        public int? Id { get; set; }

        public string Alias { get; set; }

        public string Name { get; set; }

        public int[] PoolIds { get; set; }

        public string[] PoolAliases { get; set; }

        public string[] Pool { get; set; }
    }
}
