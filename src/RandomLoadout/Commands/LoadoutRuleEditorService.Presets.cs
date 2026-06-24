using System;
using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed partial class LoadoutRuleEditorService
    {
        public string GetActivePresetId()
        {
            return _activePresetProvider != null ? _activePresetProvider() : StartItemsPresetNames.DefaultPresetId;
        }

        public string GetActivePresetDisplayName()
        {
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider != null
                ? _ruleFileProvider.GetActivePreset(LoadEditableModel())
                : null;
            return activePreset != null
                ? StartItemsPresetNames.GetDisplayName(activePreset)
                : StartItemsPresetNames.GetDisplayName(StartItemsPresetNames.DefaultPresetId, string.Empty, StartItemsPresetNames.DefaultPresetDisplayNameKey);
        }

        public string[] GetPresetIds()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            if (model.Presets == null || model.Presets.Length == 0)
            {
                return new[] { StartItemsPresetNames.DefaultPresetId };
            }

            List<string> ids = new List<string>();
            for (int i = 0; i < model.Presets.Length; i++)
            {
                LoadoutRuleFilePresetModel preset = model.Presets[i];
                if (preset != null)
                {
                    ids.Add(StartItemsPresetNames.NormalizePresetId(preset.Id));
                }
            }

            return ids.Count > 0 ? ids.ToArray() : new[] { StartItemsPresetNames.DefaultPresetId };
        }

        public LoadoutPresetEditorEntry[] GetPresetEntries()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            string activePresetId = GetActivePresetId();
            if (model.Presets == null || model.Presets.Length == 0)
            {
                LoadoutRuleFileRuleModel[] legacyRules = model.Rules ?? new LoadoutRuleFileRuleModel[0];
                return new[]
                {
                    BuildPresetEntry(
                        StartItemsPresetNames.CreateBuiltInPreset(StartItemsPresetNames.DefaultPresetId, legacyRules),
                        activePresetId),
                };
            }

            List<LoadoutPresetEditorEntry> entries = new List<LoadoutPresetEditorEntry>();
            for (int i = 0; i < model.Presets.Length; i++)
            {
                LoadoutRuleFilePresetModel preset = model.Presets[i];
                if (preset != null)
                {
                    entries.Add(BuildPresetEntry(preset, activePresetId));
                }
            }

            return entries.Count > 0
                ? entries.ToArray()
                : new[]
                {
                    BuildPresetEntry(
                        StartItemsPresetNames.CreateBuiltInPreset(StartItemsPresetNames.DefaultPresetId, new LoadoutRuleFileRuleModel[0]),
                        activePresetId),
                };
        }

        public GrantCommandExecutionResult SelectNextPreset()
        {
            string[] presetIds = GetPresetIds();
            string currentPresetId = GetActivePresetId();
            int currentIndex = -1;
            for (int i = 0; i < presetIds.Length; i++)
            {
                if (string.Equals(presetIds[i], currentPresetId, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                    break;
                }
            }

            string nextPresetId = presetIds[(currentIndex + 1) % presetIds.Length];
            SetActivePresetId(nextPresetId);
            LoadoutRuleFilePresetModel nextPreset = GetPresetById(LoadEditableModel(), nextPresetId);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.changed", GetPresetDisplayName(nextPreset, nextPresetId)),
                GuiText.GetEnglish("result.start_items.preset.changed", GetPresetEnglishDisplayName(nextPreset, nextPresetId)) +
                " [RuleFile=" + GetRuleFilePath() + "; PresetCount=" + presetIds.Length + "; PresetId=" + nextPresetId + "]");
        }

        public GrantCommandExecutionResult SelectPreset(string presetId)
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            bool isLegacyDefaultPreset = (model.Presets == null || model.Presets.Length == 0) &&
                string.Equals(presetId, StartItemsPresetNames.DefaultPresetId, StringComparison.OrdinalIgnoreCase);
            LoadoutRuleFilePresetModel preset = _ruleFileProvider.GetPreset(model, presetId);
            if (!isLegacyDefaultPreset && preset == null)
            {
                return new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("result.start_items.preset.missing", StartItemsPresetNames.NormalizePresetId(presetId)),
                    GuiText.GetEnglish("result.start_items.preset.missing", StartItemsPresetNames.NormalizePresetId(presetId)) +
                    " [RuleFile=" + GetRuleFilePath() + "; PresetId=" + StartItemsPresetNames.NormalizePresetId(presetId) + "]");
            }

            SetActivePresetId(presetId);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.changed", GetPresetDisplayName(preset, presetId)),
                GuiText.GetEnglish("result.start_items.preset.changed", GetPresetEnglishDisplayName(preset, presetId)) +
                " [RuleFile=" + GetRuleFilePath() + "; PresetId=" + StartItemsPresetNames.NormalizePresetId(presetId) + "]");
        }

        public GrantCommandExecutionResult CreatePreset()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            _ruleFileProvider.EnsureActivePreset(model);
            string newPresetName = CreateUniquePresetName(model, GetNewPresetBaseName());
            string newPresetId = CreateUniquePresetId(model, newPresetName);
            List<LoadoutRuleFilePresetModel> presets = new List<LoadoutRuleFilePresetModel>(model.Presets);
            LoadoutRuleFilePresetModel preset = new LoadoutRuleFilePresetModel
            {
                Id = newPresetId,
                Name = newPresetName,
                DisplayNameKey = string.Empty,
                Rules = new LoadoutRuleFileRuleModel[0],
            };
            presets.Add(preset);

            model.Presets = presets.ToArray();
            SaveEditableModel(model);
            SetActivePresetId(newPresetId);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.created", StartItemsPresetNames.GetDisplayName(preset)),
                GuiText.GetEnglish("result.start_items.preset.created", StartItemsPresetNames.GetEnglishDisplayName(preset)) +
                " [RuleFile=" + GetRuleFilePath() + "; PresetCount=" + model.Presets.Length + "; PresetId=" + newPresetId + "]");
        }

        public GrantCommandExecutionResult DuplicateActivePreset()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            string sourcePresetDisplayName = StartItemsPresetNames.GetDisplayName(activePreset);
            string sourcePresetEnglishDisplayName = StartItemsPresetNames.GetEnglishDisplayName(activePreset);
            string newPresetName = CreateUniquePresetName(model, sourcePresetDisplayName + "-copy");
            string newPresetId = CreateUniquePresetId(model, StartItemsPresetNames.NormalizePresetId(activePreset.Id) + "-copy");
            List<LoadoutRuleFilePresetModel> presets = new List<LoadoutRuleFilePresetModel>(model.Presets);
            LoadoutRuleFilePresetModel duplicatedPreset = new LoadoutRuleFilePresetModel
            {
                Id = newPresetId,
                Name = newPresetName,
                DisplayNameKey = string.Empty,
                Rules = CloneRules(activePreset.Rules),
            };
            presets.Add(duplicatedPreset);

            model.Presets = presets.ToArray();
            SaveEditableModel(model);
            SetActivePresetId(newPresetId);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.duplicated", sourcePresetDisplayName, StartItemsPresetNames.GetDisplayName(duplicatedPreset)),
                GuiText.GetEnglish("result.start_items.preset.duplicated", sourcePresetEnglishDisplayName, StartItemsPresetNames.GetEnglishDisplayName(duplicatedPreset)) +
                " [RuleFile=" + GetRuleFilePath() + "; PresetCount=" + model.Presets.Length + "; PresetId=" + newPresetId + "]");
        }

        public GrantCommandExecutionResult DeleteActivePreset()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            _ruleFileProvider.EnsureActivePreset(model);
            List<LoadoutRuleFilePresetModel> presets = new List<LoadoutRuleFilePresetModel>(model.Presets ?? new LoadoutRuleFilePresetModel[0]);
            if (presets.Count <= 1)
            {
                return new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("result.start_items.preset.delete_last"),
                    GuiText.GetEnglish("result.start_items.preset.delete_last") +
                    " [RuleFile=" + GetRuleFilePath() + "; PresetCount=" + presets.Count + "]");
            }

            string activePresetId = GetActivePresetId();
            int removeIndex = FindPresetIndex(presets, activePresetId);
            if (removeIndex < 0)
            {
                removeIndex = 0;
            }

            LoadoutRuleFilePresetModel deletedPreset = presets[removeIndex];
            presets.RemoveAt(removeIndex);
            model.Presets = presets.ToArray();
            string nextPresetId = GetFallbackPresetId(presets, removeIndex);
            SaveEditableModel(model);
            SetActivePresetId(nextPresetId);
            LoadoutRuleFilePresetModel nextPreset = GetPresetById(model, nextPresetId);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.deleted", GetPresetDisplayName(deletedPreset, activePresetId), GetPresetDisplayName(nextPreset, nextPresetId)),
                GuiText.GetEnglish("result.start_items.preset.deleted", GetPresetEnglishDisplayName(deletedPreset, activePresetId), GetPresetEnglishDisplayName(nextPreset, nextPresetId)) +
                " [RuleFile=" + GetRuleFilePath() + "; PresetCount=" + model.Presets.Length + "; PresetId=" + nextPresetId + "]");
        }

        public GrantCommandExecutionResult RenameActivePreset(string newName)
        {
            string normalizedName = StartItemsPresetNames.NormalizePresetName(newName);
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
            string activePresetId = StartItemsPresetNames.NormalizePresetId(activePreset.Id);
            string oldDisplayName = StartItemsPresetNames.GetDisplayName(activePreset);
            string oldEnglishDisplayName = StartItemsPresetNames.GetEnglishDisplayName(activePreset);
            if (string.Equals(oldDisplayName, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return new GrantCommandExecutionResult(
                    true,
                    GuiText.Get("result.start_items.preset.renamed", oldDisplayName, normalizedName),
                    GuiText.GetEnglish("result.start_items.preset.renamed", oldEnglishDisplayName, normalizedName) +
                    " [RuleFile=" + GetRuleFilePath() + "; PresetId=" + activePresetId + "]");
            }

            if (PresetDisplayNameExists(model, normalizedName, activePresetId))
            {
                return new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("result.start_items.preset.rename_duplicate", normalizedName),
                    GuiText.GetEnglish("result.start_items.preset.rename_duplicate", normalizedName) +
                    " [RuleFile=" + GetRuleFilePath() + "; PresetId=" + activePresetId + "]");
            }

            activePreset.Name = normalizedName;
            activePreset.DisplayNameKey = string.Empty;
            SaveEditableModel(model);
            SetActivePresetId(activePresetId);
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.renamed", oldDisplayName, normalizedName),
                GuiText.GetEnglish("result.start_items.preset.renamed", oldEnglishDisplayName, normalizedName) +
                " [RuleFile=" + GetRuleFilePath() + "; PresetId=" + activePresetId + "]");
        }

        private void SetActivePresetId(string presetId)
        {
            string normalizedId = StartItemsPresetNames.NormalizePresetId(presetId);
            if (_activePresetSetter != null)
            {
                _activePresetSetter(normalizedId);
                return;
            }

            InvalidateResolvedConfig();
        }

        private static LoadoutPresetEditorEntry BuildPresetEntry(LoadoutRuleFilePresetModel preset, string activePresetId)
        {
            LoadoutRuleFileRuleModel[] safeRules = preset != null && preset.Rules != null
                ? preset.Rules
                : new LoadoutRuleFileRuleModel[0];
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

            string presetId = StartItemsPresetNames.NormalizePresetId(preset != null ? preset.Id : string.Empty);
            return new LoadoutPresetEditorEntry(
                presetId,
                StartItemsPresetNames.GetDisplayName(preset),
                string.Equals(presetId, activePresetId, StringComparison.OrdinalIgnoreCase),
                safeRules.Length,
                specificCount,
                randomCount);
        }

        private static int FindPresetIndex(List<LoadoutRuleFilePresetModel> presets, string presetId)
        {
            string normalizedPresetId = StartItemsPresetNames.NormalizePresetId(presetId);
            for (int i = 0; i < presets.Count; i++)
            {
                LoadoutRuleFilePresetModel preset = presets[i];
                if (preset != null && string.Equals(StartItemsPresetNames.NormalizePresetId(preset.Id), normalizedPresetId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string GetFallbackPresetId(List<LoadoutRuleFilePresetModel> presets, int removedIndex)
        {
            int fallbackIndex = removedIndex;
            if (fallbackIndex >= presets.Count)
            {
                fallbackIndex = presets.Count - 1;
            }

            if (fallbackIndex < 0)
            {
                return StartItemsPresetNames.DefaultPresetId;
            }

            LoadoutRuleFilePresetModel fallbackPreset = presets[fallbackIndex];
            return StartItemsPresetNames.NormalizePresetId(fallbackPreset != null ? fallbackPreset.Id : string.Empty);
        }

        private static string CreateUniquePresetId(LoadoutRuleFileModel model, string baseId)
        {
            string normalizedBaseId = StartItemsPresetNames.NormalizePresetId(baseId);
            if (!PresetIdExists(model, normalizedBaseId))
            {
                return normalizedBaseId;
            }

            for (int index = 2; index < 1000; index++)
            {
                string candidate = normalizedBaseId + "-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (!PresetIdExists(model, candidate))
                {
                    return candidate;
                }
            }

            return normalizedBaseId + "-" + DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string CreateUniquePresetName(LoadoutRuleFileModel model, string baseName)
        {
            string normalizedBaseName = !string.IsNullOrEmpty(baseName) ? baseName.Trim() : "preset";
            if (!PresetDisplayNameExists(model, normalizedBaseName, string.Empty))
            {
                return normalizedBaseName;
            }

            for (int index = 2; index < 1000; index++)
            {
                string candidate = normalizedBaseName + "-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (!PresetDisplayNameExists(model, candidate, string.Empty))
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
                : "Preset";
        }

        private static bool PresetIdExists(LoadoutRuleFileModel model, string presetId)
        {
            if (model == null || model.Presets == null)
            {
                return false;
            }

            string normalizedPresetId = StartItemsPresetNames.NormalizePresetId(presetId);
            for (int i = 0; i < model.Presets.Length; i++)
            {
                LoadoutRuleFilePresetModel preset = model.Presets[i];
                if (preset != null && string.Equals(StartItemsPresetNames.NormalizePresetId(preset.Id), normalizedPresetId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PresetDisplayNameExists(LoadoutRuleFileModel model, string presetDisplayName, string excludePresetId)
        {
            if (model == null || model.Presets == null)
            {
                return false;
            }

            string normalizedDisplayName = StartItemsPresetNames.NormalizePresetName(presetDisplayName);
            string normalizedExcludeId = StartItemsPresetNames.NormalizePresetId(excludePresetId);
            for (int i = 0; i < model.Presets.Length; i++)
            {
                LoadoutRuleFilePresetModel preset = model.Presets[i];
                if (preset == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(normalizedExcludeId) &&
                    string.Equals(StartItemsPresetNames.NormalizePresetId(preset.Id), normalizedExcludeId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(StartItemsPresetNames.GetDisplayName(preset), normalizedDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetPresetDisplayName(LoadoutRuleFilePresetModel preset, string presetId)
        {
            return preset != null
                ? StartItemsPresetNames.GetDisplayName(preset)
                : StartItemsPresetNames.NormalizePresetId(presetId);
        }

        private static string GetPresetEnglishDisplayName(LoadoutRuleFilePresetModel preset, string presetId)
        {
            return preset != null
                ? StartItemsPresetNames.GetEnglishDisplayName(preset)
                : StartItemsPresetNames.NormalizePresetId(presetId);
        }

        private LoadoutRuleFilePresetModel GetPresetById(LoadoutRuleFileModel model, string presetId)
        {
            return _ruleFileProvider != null ? _ruleFileProvider.GetPreset(model, presetId) : null;
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
