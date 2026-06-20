using System;
using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed partial class LoadoutRuleEditorService
    {
        public string GetActivePresetName()
        {
            return _activePresetProvider != null ? _activePresetProvider() : "default";
        }

        public string[] GetPresetNames()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            EnsureLocalizedDefaultPresetName(model);
            if (model.Presets == null || model.Presets.Length == 0)
            {
                return new[] { "default" };
            }

            List<string> names = new List<string>();
            for (int i = 0; i < model.Presets.Length; i++)
            {
                LoadoutRuleFilePresetModel preset = model.Presets[i];
                if (preset != null && !string.IsNullOrEmpty(preset.Name))
                {
                    names.Add(preset.Name);
                }
            }

            return names.Count > 0 ? names.ToArray() : new[] { "default" };
        }

        public LoadoutPresetEditorEntry[] GetPresetEntries()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            EnsureLocalizedDefaultPresetName(model);
            string activePresetName = GetActivePresetName();
            if (model.Presets == null || model.Presets.Length == 0)
            {
                LoadoutRuleFileRuleModel[] legacyRules = model.Rules ?? new LoadoutRuleFileRuleModel[0];
                return new[] { BuildPresetEntry("default", activePresetName, legacyRules) };
            }

            List<LoadoutPresetEditorEntry> entries = new List<LoadoutPresetEditorEntry>();
            for (int i = 0; i < model.Presets.Length; i++)
            {
                LoadoutRuleFilePresetModel preset = model.Presets[i];
                if (preset != null)
                {
                    entries.Add(BuildPresetEntry(!string.IsNullOrEmpty(preset.Name) ? preset.Name : "default", activePresetName, preset.Rules));
                }
            }

            return entries.Count > 0
                ? entries.ToArray()
                : new[] { BuildPresetEntry("default", activePresetName, new LoadoutRuleFileRuleModel[0]) };
        }

        public GrantCommandExecutionResult SelectNextPreset()
        {
            string[] presetNames = GetPresetNames();
            string currentPreset = GetActivePresetName();
            int currentIndex = -1;
            for (int i = 0; i < presetNames.Length; i++)
            {
                if (string.Equals(presetNames[i], currentPreset, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                    break;
                }
            }

            string nextPreset = presetNames[(currentIndex + 1) % presetNames.Length];
            SetActivePresetName(nextPreset);

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.changed", nextPreset),
                GuiText.GetEnglish("result.start_items.preset.changed", nextPreset) +
                " [RuleFile=" + GetRuleFilePath() + "; PresetCount=" + presetNames.Length + "]");
        }

        public GrantCommandExecutionResult SelectPreset(string presetName)
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            EnsureLocalizedDefaultPresetName(model);
            bool isLegacyDefaultPreset = (model.Presets == null || model.Presets.Length == 0) &&
                string.Equals(presetName, "default", StringComparison.OrdinalIgnoreCase);
            if (!isLegacyDefaultPreset && _ruleFileProvider.GetPreset(model, presetName) == null)
            {
                return new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("result.start_items.preset.missing", presetName),
                    GuiText.GetEnglish("result.start_items.preset.missing", presetName) +
                    " [RuleFile=" + GetRuleFilePath() + "]");
            }

            SetActivePresetName(presetName);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.changed", presetName),
                GuiText.GetEnglish("result.start_items.preset.changed", presetName) +
                " [RuleFile=" + GetRuleFilePath() + "]");
        }

        public GrantCommandExecutionResult CreatePreset()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            _ruleFileProvider.EnsureActivePreset(model);
            EnsureLocalizedDefaultPresetName(model);
            string newPresetName = CreateUniquePresetName(model, GetNewPresetBaseName());
            List<LoadoutRuleFilePresetModel> presets = new List<LoadoutRuleFilePresetModel>(model.Presets);
            presets.Add(
                new LoadoutRuleFilePresetModel
                {
                    Name = newPresetName,
                    Rules = new LoadoutRuleFileRuleModel[0],
                });

            model.Presets = presets.ToArray();
            SaveEditableModel(model);
            SetActivePresetName(newPresetName);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.created", newPresetName),
                GuiText.GetEnglish("result.start_items.preset.created", newPresetName) +
                " [RuleFile=" + GetRuleFilePath() + "; PresetCount=" + model.Presets.Length + "]");
        }

        public GrantCommandExecutionResult DuplicateActivePreset()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            EnsureLocalizedDefaultPresetName(model);
            activePreset = _ruleFileProvider.EnsureActivePreset(model);
            string sourcePresetName = !string.IsNullOrEmpty(activePreset.Name) ? activePreset.Name : GetActivePresetName();
            string newPresetName = CreateUniquePresetName(model, sourcePresetName + "-copy");
            List<LoadoutRuleFilePresetModel> presets = new List<LoadoutRuleFilePresetModel>(model.Presets);
            presets.Add(
                new LoadoutRuleFilePresetModel
                {
                    Name = newPresetName,
                    Rules = CloneRules(activePreset.Rules),
                });

            model.Presets = presets.ToArray();
            SaveEditableModel(model);
            SetActivePresetName(newPresetName);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.duplicated", sourcePresetName, newPresetName),
                GuiText.GetEnglish("result.start_items.preset.duplicated", sourcePresetName, newPresetName) +
                " [RuleFile=" + GetRuleFilePath() + "; PresetCount=" + model.Presets.Length + "]");
        }

        public GrantCommandExecutionResult DeleteActivePreset()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            _ruleFileProvider.EnsureActivePreset(model);
            EnsureLocalizedDefaultPresetName(model);
            List<LoadoutRuleFilePresetModel> presets = new List<LoadoutRuleFilePresetModel>(model.Presets ?? new LoadoutRuleFilePresetModel[0]);
            if (presets.Count <= 1)
            {
                return new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("result.start_items.preset.delete_last"),
                    GuiText.GetEnglish("result.start_items.preset.delete_last") +
                    " [RuleFile=" + GetRuleFilePath() + "; PresetCount=" + presets.Count + "]");
            }

            string activePresetName = GetActivePresetName();
            int removeIndex = FindPresetIndex(presets, activePresetName);
            if (removeIndex < 0)
            {
                removeIndex = 0;
            }

            string deletedPresetName = presets[removeIndex] != null && !string.IsNullOrEmpty(presets[removeIndex].Name)
                ? presets[removeIndex].Name
                : activePresetName;
            presets.RemoveAt(removeIndex);
            model.Presets = presets.ToArray();
            string nextPresetName = GetFallbackPresetName(presets, removeIndex);
            SaveEditableModel(model);
            SetActivePresetName(nextPresetName);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.deleted", deletedPresetName, nextPresetName),
                GuiText.GetEnglish("result.start_items.preset.deleted", deletedPresetName, nextPresetName) +
                " [RuleFile=" + GetRuleFilePath() + "; PresetCount=" + model.Presets.Length + "]");
        }

        public GrantCommandExecutionResult RenameActivePreset(string newName)
        {
            string normalizedName = (newName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalizedName))
            {
                return new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("result.start_items.preset.rename_empty"),
                    GuiText.GetEnglish("result.start_items.preset.rename_empty") +
                    " [RuleFile=" + GetRuleFilePath() + "]");
            }

            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            string oldName = !string.IsNullOrEmpty(activePreset.Name) ? activePreset.Name : GetActivePresetName();
            if (string.Equals(oldName, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return new GrantCommandExecutionResult(
                    true,
                    GuiText.Get("result.start_items.preset.renamed", oldName, normalizedName),
                    GuiText.GetEnglish("result.start_items.preset.renamed", oldName, normalizedName) +
                    " [RuleFile=" + GetRuleFilePath() + "]");
            }

            if (PresetNameExists(model, normalizedName))
            {
                return new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("result.start_items.preset.rename_duplicate", normalizedName),
                    GuiText.GetEnglish("result.start_items.preset.rename_duplicate", normalizedName) +
                    " [RuleFile=" + GetRuleFilePath() + "]");
            }

            activePreset.Name = normalizedName;
            SaveEditableModel(model);
            SetActivePresetName(normalizedName);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.renamed", oldName, normalizedName),
                GuiText.GetEnglish("result.start_items.preset.renamed", oldName, normalizedName) +
                " [RuleFile=" + GetRuleFilePath() + "]");
        }

        private void SetActivePresetName(string presetName)
        {
            if (_activePresetSetter != null)
            {
                _activePresetSetter(presetName);
                return;
            }

            InvalidateResolvedConfig();
        }

        private void EnsureLocalizedDefaultPresetName(LoadoutRuleFileModel model)
        {
            if (!string.Equals(GuiText.CurrentLanguageCode, "zh-CN", StringComparison.OrdinalIgnoreCase) ||
                model == null)
            {
                return;
            }

            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            LoadoutRuleFilePresetModel defaultPreset = _ruleFileProvider.GetPreset(model, "default");
            if (defaultPreset == null ||
                _ruleFileProvider.GetPreset(model, "预设") != null)
            {
                return;
            }

            defaultPreset.Name = "预设";
            SaveEditableModel(model);
            if (string.Equals(GetActivePresetName(), "default", StringComparison.OrdinalIgnoreCase) ||
                object.ReferenceEquals(activePreset, defaultPreset))
            {
                SetActivePresetName("预设");
            }
        }

        private static LoadoutPresetEditorEntry BuildPresetEntry(string presetName, string activePresetName, LoadoutRuleFileRuleModel[] rules)
        {
            LoadoutRuleFileRuleModel[] safeRules = rules ?? new LoadoutRuleFileRuleModel[0];
            int specificCount = 0;
            int randomCount = 0;
            for (int i = 0; i < safeRules.Length; i++)
            {
                LoadoutRuleFileRuleModel rule = safeRules[i];
                if (rule == null)
                {
                    continue;
                }

                if (string.Equals(rule.Mode, "specific", StringComparison.OrdinalIgnoreCase))
                {
                    specificCount++;
                }
                else if (string.Equals(rule.Mode, "random", StringComparison.OrdinalIgnoreCase))
                {
                    randomCount++;
                }
            }

            return new LoadoutPresetEditorEntry(
                presetName,
                string.Equals(presetName, activePresetName, StringComparison.OrdinalIgnoreCase),
                safeRules.Length,
                specificCount,
                randomCount);
        }

        private static int FindPresetIndex(List<LoadoutRuleFilePresetModel> presets, string presetName)
        {
            for (int i = 0; i < presets.Count; i++)
            {
                LoadoutRuleFilePresetModel preset = presets[i];
                if (preset != null && string.Equals(preset.Name, presetName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string GetFallbackPresetName(List<LoadoutRuleFilePresetModel> presets, int removedIndex)
        {
            int fallbackIndex = removedIndex;
            if (fallbackIndex >= presets.Count)
            {
                fallbackIndex = presets.Count - 1;
            }

            if (fallbackIndex < 0)
            {
                return "default";
            }

            LoadoutRuleFilePresetModel fallbackPreset = presets[fallbackIndex];
            return fallbackPreset != null && !string.IsNullOrEmpty(fallbackPreset.Name) ? fallbackPreset.Name : "default";
        }

        private static string CreateUniquePresetName(LoadoutRuleFileModel model, string baseName)
        {
            string normalizedBaseName = !string.IsNullOrEmpty(baseName) ? baseName.Trim() : "preset";
            if (!PresetNameExists(model, normalizedBaseName))
            {
                return normalizedBaseName;
            }

            for (int index = 2; index < 1000; index++)
            {
                string candidate = normalizedBaseName + "-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (!PresetNameExists(model, candidate))
                {
                    return candidate;
                }
            }

            return normalizedBaseName + "-" + DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string GetNewPresetBaseName()
        {
            return string.Equals(GuiText.CurrentLanguageCode, "zh-CN", StringComparison.OrdinalIgnoreCase)
                ? "预设"
                : "preset";
        }

        private static bool PresetNameExists(LoadoutRuleFileModel model, string presetName)
        {
            if (model == null || model.Presets == null)
            {
                return false;
            }

            for (int i = 0; i < model.Presets.Length; i++)
            {
                LoadoutRuleFilePresetModel preset = model.Presets[i];
                if (preset != null && string.Equals(preset.Name, presetName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static LoadoutRuleFileRuleModel[] CloneRules(LoadoutRuleFileRuleModel[] rules)
        {
            if (rules == null || rules.Length == 0)
            {
                return new LoadoutRuleFileRuleModel[0];
            }

            LoadoutRuleFileRuleModel[] clones = new LoadoutRuleFileRuleModel[rules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                LoadoutRuleFileRuleModel rule = rules[i] ?? new LoadoutRuleFileRuleModel();
                clones[i] = new LoadoutRuleFileRuleModel
                {
                    Enabled = rule.Enabled,
                    Mode = rule.Mode,
                    Category = rule.Category,
                    Count = rule.Count,
                    Id = rule.Id,
                    Alias = rule.Alias,
                    Name = rule.Name,
                    PoolIds = CloneIntArray(rule.PoolIds),
                    PoolAliases = CloneStringArray(rule.PoolAliases),
                    Pool = CloneStringArray(rule.Pool),
                };
            }

            return clones;
        }

        private static int[] CloneIntArray(int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return new int[0];
            }

            int[] clone = new int[values.Length];
            Array.Copy(values, clone, values.Length);
            return clone;
        }

        private static string[] CloneStringArray(string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return new string[0];
            }

            string[] clone = new string[values.Length];
            Array.Copy(values, clone, values.Length);
            return clone;
        }
    }
}
