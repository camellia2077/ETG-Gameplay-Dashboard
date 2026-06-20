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

            if (fileModel != null && fileModel.Rules != null)
            {
                if (!string.Equals(ActivePresetName, DefaultPresetName, StringComparison.OrdinalIgnoreCase) && messages != null)
                {
                    messages.Add("Rule file uses legacy top-level rules. Preset '" + ActivePresetName + "' is not available, so legacy rules are used as 'default'.");
                }

                return fileModel.Rules;
            }

            return new LoadoutRuleFileRuleModel[0];
        }

        internal LoadoutRuleFilePresetModel GetActivePreset(LoadoutRuleFileModel fileModel)
        {
            return GetPreset(fileModel, ActivePresetName);
        }

        internal LoadoutRuleFilePresetModel GetPreset(LoadoutRuleFileModel fileModel, string presetName)
        {
            if (fileModel == null || fileModel.Presets == null)
            {
                return null;
            }

            string normalizedName = NormalizePresetName(presetName);
            for (int i = 0; i < fileModel.Presets.Length; i++)
            {
                LoadoutRuleFilePresetModel preset = fileModel.Presets[i];
                if (preset != null && string.Equals(NormalizePresetName(preset.Name), normalizedName, StringComparison.OrdinalIgnoreCase))
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
                    new LoadoutRuleFilePresetModel
                    {
                        Name = DefaultPresetName,
                        Rules = fileModel.Rules ?? new LoadoutRuleFileRuleModel[0],
                    },
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
                Name = ActivePresetName,
                Rules = new LoadoutRuleFileRuleModel[0],
            };
            presets.Add(activePreset);
            fileModel.Presets = presets.ToArray();
            return activePreset;
        }
    }
}
