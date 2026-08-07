// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private ControllerFocusEntry[] GetLoadoutEditorFocusEntries()
        {
            if (_loadoutEditorMode == LoadoutEditorMode.PresetDetail)
            {
                int dynamicCount = 0;
                for (int index = 0; index < _cachedLoadoutRuleEntries.Length; index++)
                {
                    dynamicCount++;
                    if (_cachedLoadoutRuleEntries[index] != null && !_cachedLoadoutRuleEntries[index].IsPresetPickupCollection)
                    {
                        dynamicCount++;
                    }
                    if (DoesLoadoutRuleEntryHaveEditAction(_cachedLoadoutRuleEntries[index]))
                    {
                        dynamicCount++;
                    }
                }

                ControllerFocusEntry[] entries = new ControllerFocusEntry[6 + dynamicCount];
                entries[0] = new ControllerFocusEntry("loadout.back", 0, 1);
                entries[1] = new ControllerFocusEntry("loadout.preset_detail.reload", 0, 0);
                entries[2] = new ControllerFocusEntry("loadout.preset_detail.add_item", 1, 0);
                entries[3] = new ControllerFocusEntry("loadout.preset_detail.add_random_pool", 1, 1);
                entries[4] = new ControllerFocusEntry("loadout.preset_detail.pickups", 1, 2);
                entries[5] = new ControllerFocusEntry("loadout.preset_detail.fill", 1, 3);
                int writeIndex = 6;
                for (int index = 0; index < _cachedLoadoutRuleEntries.Length; index++)
                {
                    LoadoutRuleEditorEntry entry = _cachedLoadoutRuleEntries[index];
                    int row = 2 + index;
                    if (entry != null && !entry.IsPresetPickupCollection)
                    {
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutRuleToggleControlId(entry), row, 0);
                    }

                    if (DoesLoadoutRuleEntryHaveEditAction(entry))
                    {
                        int editColumn = entry != null && !entry.IsPresetPickupCollection ? 1 : 0;
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutRuleEditControlId(entry), row, editColumn);
                    }

                    int removeColumn = entry != null && !entry.IsPresetPickupCollection ? 2 : 1;
                    entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutRuleRemoveControlId(entry), row, removeColumn);
                }

                return entries;
            }

            if (_loadoutEditorMode == LoadoutEditorMode.RandomPoolDetail)
            {
                ControllerFocusEntry[] entries = new ControllerFocusEntry[3 + _cachedLoadoutRandomPoolEntries.Length];
                entries[0] = new ControllerFocusEntry("loadout.random_pool.add_item", 0, 0);
                entries[1] = new ControllerFocusEntry("loadout.back", 0, 1);
                entries[2] = new ControllerFocusEntry("loadout.random_pool.rename", 1, 0);
                for (int index = 0; index < _cachedLoadoutRandomPoolEntries.Length; index++)
                {
                    entries[index + 3] = new ControllerFocusEntry(GetLoadoutRandomPoolRemoveControlId(_cachedLoadoutRandomPoolEntries[index]), 2 + index, 0);
                }

                return entries;
            }

            if (_loadoutEditorMode == LoadoutEditorMode.PresetPickupsDetail)
            {
                int dynamicCount = 0;
                for (int index = 0; index < _cachedLoadoutPickupEntries.Length; index++)
                {
                    dynamicCount += 4;
                    if (_cachedLoadoutPickupEntries[index] != null && _cachedLoadoutPickupEntries[index].Index == _loadoutPickupCountEditIndex)
                    {
                        dynamicCount++;
                    }
                }

                ControllerFocusEntry[] entries = new ControllerFocusEntry[7 + dynamicCount];
                entries[0] = new ControllerFocusEntry("loadout.back", 0, 0);
                entries[1] = new ControllerFocusEntry("loadout.pickups.add_max_health", 1, 0);
                entries[2] = new ControllerFocusEntry("loadout.pickups.add_armor", 2, 0);
                entries[3] = new ControllerFocusEntry("loadout.pickups.add_key", 3, 0);
                entries[4] = new ControllerFocusEntry("loadout.pickups.add_rat_key", 4, 0);
                entries[5] = new ControllerFocusEntry("loadout.pickups.add_blank", 5, 0);
                entries[6] = new ControllerFocusEntry("loadout.pickups.add_casings", 6, 0);
                int writeIndex = 7;
                for (int index = 0; index < _cachedLoadoutPickupEntries.Length; index++)
                {
                    LoadoutRuleEditorEntry entry = _cachedLoadoutPickupEntries[index];
                    int row = 7 + index;
                    entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupMinusControlId(entry), row, 0);
                    entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupCountControlId(entry), row, 1);
                    if (entry != null && entry.Index == _loadoutPickupCountEditIndex)
                    {
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupConfirmControlId(entry), row, 2);
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupPlusControlId(entry), row, 3);
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupRemoveControlId(entry), row, 4);
                    }
                    else
                    {
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupPlusControlId(entry), row, 2);
                        entries[writeIndex++] = new ControllerFocusEntry(GetLoadoutPickupRemoveControlId(entry), row, 3);
                    }
                }

                return entries;
            }

            int presetCount = _cachedLoadoutPresetEntries != null ? _cachedLoadoutPresetEntries.Length : 0;
            bool manualPresetSelectionEnabled = !IsLoadoutPresetRandomEnabled();
            ControllerFocusEntry[] presetListEntries = new ControllerFocusEntry[9 + (presetCount * (manualPresetSelectionEnabled ? 2 : 1))];
            presetListEntries[0] = new ControllerFocusEntry("loadout.back", 0, 1);
            presetListEntries[1] = new ControllerFocusEntry("loadout.preset_list.reload", 0, 0);
            presetListEntries[2] = new ControllerFocusEntry("loadout.preset_list.new", 1, 0);
            presetListEntries[3] = new ControllerFocusEntry("loadout.preset_list.duplicate", 1, 1);
            presetListEntries[4] = new ControllerFocusEntry("loadout.preset_list.delete", 1, 2);
            presetListEntries[5] = new ControllerFocusEntry("loadout.preset_list.fill", 1, 3);
            presetListEntries[6] = new ControllerFocusEntry("loadout.preset_list.random", 1, 4);
            presetListEntries[7] = new ControllerFocusEntry("loadout.preset_list.rename", 2, 0);
            presetListEntries[8] = new ControllerFocusEntry("loadout.preset_list.icons", 2, 1);
            for (int index = 0; index < presetCount; index++)
            {
                int baseIndex = 9 + (index * (manualPresetSelectionEnabled ? 2 : 1));
                int presetColumn = index % LoadoutPresetColumnCount;
                int presetRow = 3 + (index / LoadoutPresetColumnCount);
                int focusColumn = presetColumn * 2;
                if (manualPresetSelectionEnabled)
                {
                    presetListEntries[baseIndex++] = new ControllerFocusEntry(GetLoadoutPresetSelectControlId(_cachedLoadoutPresetEntries[index]), presetRow, focusColumn);
                }

                presetListEntries[baseIndex] = new ControllerFocusEntry(GetLoadoutPresetOpenControlId(_cachedLoadoutPresetEntries[index]), presetRow, focusColumn + 1);
            }

            return presetListEntries;
        }

        private void ExecuteLoadoutEditorFocusedControl(PlayerController player, ManualLogSource logger)
        {
            switch (_loadoutEditorFocusedControlId)
            {
                case "loadout.back":
                    HandleLoadoutEditorBackNavigation();
                    return;
                case "loadout.preset_list.reload":
                case "loadout.preset_detail.reload":
                    ExecuteLoadoutEditorReload(logger);
                    return;
                case "loadout.preset_list.new":
                    ExecuteLoadoutEditorCreatePreset(logger);
                    return;
                case "loadout.preset_list.duplicate":
                    ExecuteLoadoutEditorDuplicatePreset(logger);
                    return;
                case "loadout.preset_list.delete":
                    ExecuteLoadoutEditorDeletePreset(logger);
                    return;
                case "loadout.preset_list.fill":
                case "loadout.preset_detail.fill":
                    ExecuteLoadoutEditorFillCurrentPreset(player, logger);
                    return;
                case "loadout.preset_list.random":
                    ExecuteToggleLoadoutPresetRandom(logger);
                    return;
                case "loadout.preset_list.rename":
                    ExecuteLoadoutEditorRenamePreset(logger);
                    return;
                case "loadout.preset_list.icons":
                    ExecuteToggleStartItemsPresetIcons(logger);
                    return;
                case "loadout.preset_detail.add_item":
                    OpenPickupAddToStartItemsPage(logger);
                    return;
                case "loadout.preset_detail.add_random_pool":
                    ExecuteLoadoutEditorCreateRandomPool(logger);
                    return;
                case "loadout.preset_detail.pickups":
                    OpenLoadoutPresetPickupsDetail();
                    return;
                case "loadout.random_pool.add_item":
                    OpenPickupAddToRandomPoolPage(logger);
                    return;
                case "loadout.random_pool.rename":
                    ExecuteLoadoutEditorRenameRandomPool(logger);
                    return;
                case "loadout.pickups.add_key":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.KeyType, logger);
                    return;
                case "loadout.pickups.add_rat_key":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.RatKeyType, logger);
                    return;
                case "loadout.pickups.add_max_health":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.MaxHealthType, logger);
                    return;
                case "loadout.pickups.add_armor":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.ArmorType, logger);
                    return;
                case "loadout.pickups.add_blank":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.BlankType, logger);
                    return;
                case "loadout.pickups.add_casings":
                    ExecuteLoadoutEditorAddPresetPickup(StartItemPickupCatalog.CasingsType, logger);
                    return;
            }

            for (int index = 0; index < _cachedLoadoutRuleEntries.Length; index++)
            {
                LoadoutRuleEditorEntry entry = _cachedLoadoutRuleEntries[index];
                if (entry != null &&
                    !entry.IsPresetPickupCollection &&
                    string.Equals(_loadoutEditorFocusedControlId, GetLoadoutRuleToggleControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorToggleRule(entry.Index, logger);
                    return;
                }

                if (DoesLoadoutRuleEntryHaveEditAction(entry) &&
                    string.Equals(_loadoutEditorFocusedControlId, GetLoadoutRuleEditControlId(entry), System.StringComparison.Ordinal))
                {
                    if (entry != null && entry.IsRandomPool)
                    {
                        OpenLoadoutRandomPoolDetail(entry.Index);
                    }
                    else
                    {
                        OpenLoadoutPresetPickupsDetail();
                    }

                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutRuleRemoveControlId(entry), System.StringComparison.Ordinal))
                {
                    if (entry != null && entry.IsPresetPickupCollection)
                    {
                        ExecuteLoadoutEditorClearPresetPickups(logger);
                    }
                    else if (entry != null && !string.IsNullOrEmpty(entry.PickupType))
                    {
                        ExecuteLoadoutEditorRemovePresetPickup(entry.Index, logger);
                    }
                    else
                    {
                        ExecuteLoadoutEditorRemove(entry != null ? entry.Index : -1, logger);
                    }

                    return;
                }
            }

            for (int index = 0; index < _cachedLoadoutRandomPoolEntries.Length; index++)
            {
                LoadoutRandomPoolEditorEntry entry = _cachedLoadoutRandomPoolEntries[index];
                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutRandomPoolRemoveControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorRemoveFromRandomPool(entry != null ? entry.PoolIndex : -1, logger);
                    return;
                }
            }

            for (int index = 0; index < _cachedLoadoutPickupEntries.Length; index++)
            {
                LoadoutRuleEditorEntry entry = _cachedLoadoutPickupEntries[index];
                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPickupMinusControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorChangePresetPickupCount(entry != null ? entry.Index : -1, -1, logger);
                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPickupCountControlId(entry), System.StringComparison.Ordinal))
                {
                    _loadoutPickupCountEditIndex = entry != null ? entry.Index : -1;
                    _loadoutPickupCountEditText = entry != null ? entry.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) : "1";
                    _loadoutEditorFocusedControlId = GetLoadoutPickupConfirmControlId(entry);
                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPickupConfirmControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorSetPresetPickupCount(entry != null ? entry.Index : -1, _loadoutPickupCountEditText, logger);
                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPickupPlusControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorChangePresetPickupCount(entry != null ? entry.Index : -1, 1, logger);
                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPickupRemoveControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorRemovePresetPickup(entry != null ? entry.Index : -1, logger);
                    return;
                }
            }

            for (int index = 0; index < _cachedLoadoutPresetEntries.Length; index++)
            {
                LoadoutPresetEditorEntry entry = _cachedLoadoutPresetEntries[index];
                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPresetSelectControlId(entry), System.StringComparison.Ordinal))
                {
                    ExecuteLoadoutEditorSelectPreset(entry.Id, logger);
                    return;
                }

                if (string.Equals(_loadoutEditorFocusedControlId, GetLoadoutPresetOpenControlId(entry), System.StringComparison.Ordinal))
                {
                    OpenLoadoutPresetDetail(entry, logger);
                    return;
                }
            }
        }
    }
}
