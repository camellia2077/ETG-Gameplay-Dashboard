// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Owns the loadout editor's read model and keeps the four UI collections in sync
    /// after an edit or preset transition.
    /// </summary>
    internal sealed class LoadoutEditorDataCoordinator
    {
        private readonly LoadoutRuleEditorService _ruleEditorService;

        public LoadoutEditorDataCoordinator(LoadoutRuleEditorService ruleEditorService)
        {
            _ruleEditorService = ruleEditorService;
            RuleEntries = new LoadoutRuleEditorEntry[0];
            PresetEntries = new LoadoutPresetEditorEntry[0];
            RandomPoolEntries = new LoadoutRandomPoolEditorEntry[0];
            PickupEntries = new LoadoutRuleEditorEntry[0];
        }

        public LoadoutRuleEditorEntry[] RuleEntries { get; private set; }
        public LoadoutPresetEditorEntry[] PresetEntries { get; private set; }
        public LoadoutRandomPoolEditorEntry[] RandomPoolEntries { get; private set; }
        public LoadoutRuleEditorEntry[] PickupEntries { get; private set; }

        public void RefreshAll(int randomPoolRuleIndex)
        {
            if (_ruleEditorService == null)
            {
                RuleEntries = new LoadoutRuleEditorEntry[0];
                PresetEntries = new LoadoutPresetEditorEntry[0];
                RandomPoolEntries = new LoadoutRandomPoolEditorEntry[0];
                PickupEntries = new LoadoutRuleEditorEntry[0];
                return;
            }

            PresetEntries = _ruleEditorService.GetPresetEntries() ?? new LoadoutPresetEditorEntry[0];
            RuleEntries = _ruleEditorService.GetEntries() ?? new LoadoutRuleEditorEntry[0];
            RandomPoolEntries = randomPoolRuleIndex >= 0
                ? (_ruleEditorService.GetRandomPoolEntries(randomPoolRuleIndex) ?? new LoadoutRandomPoolEditorEntry[0])
                : new LoadoutRandomPoolEditorEntry[0];
            PickupEntries = _ruleEditorService.GetPresetPickupEntries() ?? new LoadoutRuleEditorEntry[0];
        }

        public string GetActivePresetDisplayName()
        {
            return _ruleEditorService != null ? _ruleEditorService.GetActivePresetDisplayName() : string.Empty;
        }

        public string GetRandomPoolDisplayName(int ruleIndex)
        {
            return _ruleEditorService != null ? _ruleEditorService.GetRandomPoolDisplayName(ruleIndex) : string.Empty;
        }
    }
}
