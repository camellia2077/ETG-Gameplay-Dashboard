// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed partial class JsonLoadoutRuleFileProvider
    {
        internal LoadoutRuleFileRuleModel[] GetActivePresetRules(LoadoutRuleFileModel fileModel, List<string> messages)
        {
            LoadoutRuleFilePresetModel preset = GetActivePreset(fileModel);
            if (preset != null)
            {
                return preset.Rules ?? new LoadoutRuleFileRuleModel[0];
            }

            return new LoadoutRuleFileRuleModel[0];
        }

        internal LoadoutRuleFilePickupModel[] GetActivePresetPickups(LoadoutRuleFileModel fileModel, List<string> messages)
        {
            LoadoutRuleFilePresetModel preset = GetActivePreset(fileModel);
            if (preset != null)
            {
                return StartItemPickupCatalog.MergePickups(preset.Pickups);
            }

            return new LoadoutRuleFilePickupModel[0];
        }

        internal LoadoutRuleFilePresetModel GetActivePreset(LoadoutRuleFileModel fileModel)
        {
            LoadoutRuleFilePresetModel activePreset = GetPreset(fileModel, ActivePresetName);
            if (activePreset != null)
            {
                return activePreset;
            }

            activePreset = GetPreset(fileModel, DefaultPresetId);
            if (activePreset != null)
            {
                return activePreset;
            }

            if (fileModel == null || fileModel.Presets == null || fileModel.Presets.Length == 0)
            {
                return null;
            }

            return fileModel.Presets[0];
        }

        internal LoadoutRuleFilePresetModel GetPreset(LoadoutRuleFileModel fileModel, string presetName)
        {
            if (fileModel == null || fileModel.Presets == null)
            {
                return null;
            }

            string normalizedName = NormalizePresetId(presetName);
            for (int i = 0; i < fileModel.Presets.Length; i++)
            {
                LoadoutRuleFilePresetModel preset = fileModel.Presets[i];
                if (preset != null && string.Equals(NormalizePresetId(preset.Id), normalizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return preset;
                }
            }

            return null;
        }

        internal LoadoutRuleFilePresetModel EnsureActivePreset(LoadoutRuleFileModel fileModel)
        {
            if (fileModel.Presets == null || fileModel.Presets.Length == 0)
            {
                fileModel.Presets = new[]
                {
                    StartItemsPresetNames.CreateBuiltInPreset(DefaultPresetId, fileModel.Rules ?? new LoadoutRuleFileRuleModel[0]),
                };
                fileModel.Rules = new LoadoutRuleFileRuleModel[0];
            }

            LoadoutRuleFilePresetModel activePreset = GetActivePreset(fileModel);
            if (activePreset != null)
            {
                return activePreset;
            }

            List<LoadoutRuleFilePresetModel> presets = new List<LoadoutRuleFilePresetModel>(fileModel.Presets);
            activePreset = new LoadoutRuleFilePresetModel
            {
                Id = ActivePresetName,
                Name = string.Empty,
                DisplayNameKey = string.Empty,
                Rules = new LoadoutRuleFileRuleModel[0],
                Pickups = new LoadoutRuleFilePickupModel[0],
            };
            presets.Add(activePreset);
            fileModel.Presets = presets.ToArray();
            return activePreset;
        }
    }
}
