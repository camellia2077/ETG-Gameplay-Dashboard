using System;
using System.Collections.Generic;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed partial class LoadoutRuleEditorService
    {
        private readonly JsonLoadoutRuleFileProvider _ruleFileProvider;
        private readonly Func<EtgPickupCatalogEntry[]> _pickupCatalogProvider;
        private readonly Action _invalidateResolvedConfig;
        private readonly Func<string> _activePresetProvider;
        private readonly Action<string> _activePresetSetter;
        private readonly EtgOwnedPickupReader _ownedPickupReader;

        public LoadoutRuleEditorService(
            JsonLoadoutRuleFileProvider ruleFileProvider,
            Func<EtgPickupCatalogEntry[]> pickupCatalogProvider,
            Action invalidateResolvedConfig,
            Func<string> activePresetProvider,
            Action<string> activePresetSetter,
            EtgOwnedPickupReader ownedPickupReader)
        {
            _ruleFileProvider = ruleFileProvider;
            _pickupCatalogProvider = pickupCatalogProvider;
            _invalidateResolvedConfig = invalidateResolvedConfig;
            _activePresetProvider = activePresetProvider;
            _activePresetSetter = activePresetSetter;
            _ownedPickupReader = ownedPickupReader;
        }

        public GrantCommandExecutionResult AddSpecific(EtgPickupCatalogEntry pickup)
        {
            if (pickup == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.loadout_editor.pickup_missing");
            }

            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            List<LoadoutRuleFileRuleModel> rules = new List<LoadoutRuleFileRuleModel>(activePreset.Rules ?? new LoadoutRuleFileRuleModel[0]);
            if (ContainsSpecificPickupId(rules, pickup.PickupId))
            {
                return new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("result.start_items.add.duplicate", GetPickupDisplayName(pickup), pickup.PickupId),
                    GuiText.GetEnglish("result.start_items.add.duplicate", GetPickupDisplayName(pickup), pickup.PickupId) +
                    " [RuleFile=" + GetRuleFilePath() + "; PickupId=" + pickup.PickupId + "; RuleCount=" + rules.Count + "]");
            }

            rules.Add(CreateSpecificRule(pickup));

            activePreset.Rules = rules.ToArray();
            SaveEditableModel(model);
            InvalidateResolvedConfig();

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.add.success", GetPickupDisplayName(pickup), pickup.PickupId),
                GuiText.GetEnglish("result.start_items.add.success", GetPickupDisplayName(pickup), pickup.PickupId) +
                " [RuleFile=" + GetRuleFilePath() + "; RuleCount=" + rules.Count + "]");
        }

        public GrantCommandExecutionResult RemoveAt(int index)
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            List<LoadoutRuleFileRuleModel> rules = new List<LoadoutRuleFileRuleModel>(activePreset.Rules ?? new LoadoutRuleFileRuleModel[0]);
            if (index < 0 || index >= rules.Count)
            {
                return GrantCommandExecutionResult.Localized(false, "result.loadout_editor.remove.missing");
            }

            Dictionary<int, EtgPickupCatalogEntry> catalogById = BuildCatalogById();
            string removedRule = BuildPrimaryText(rules[index], GetRulePickup(rules[index], catalogById));
            rules.RemoveAt(index);
            activePreset.Rules = rules.ToArray();
            SaveEditableModel(model);
            InvalidateResolvedConfig();
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.remove.success"),
                GuiText.GetEnglish("result.start_items.remove.success") +
                " [RuleFile=" + GetRuleFilePath() + "; RemovedIndex=" + index + "; RemovedRule=" + removedRule + "; RuleCount=" + rules.Count + "]");
        }

        public GrantCommandExecutionResult AddRandomPoolRule()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            List<LoadoutRuleFileRuleModel> rules = new List<LoadoutRuleFileRuleModel>(activePreset.Rules ?? new LoadoutRuleFileRuleModel[0]);
            LoadoutRuleFileRuleModel newRule = CreateRandomPoolRule(CreateUniqueRandomPoolName(rules));
            rules.Add(newRule);

            activePreset.Rules = rules.ToArray();
            SaveEditableModel(model);
            InvalidateResolvedConfig();

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.random_pool.created", GetRandomPoolDisplayName(newRule)),
                GuiText.GetEnglish("result.start_items.random_pool.created", GetRandomPoolDisplayName(newRule)) +
                " [RuleFile=" + GetRuleFilePath() + "; RuleCount=" + rules.Count + "; Name=" + GetRandomPoolDisplayName(newRule) + "]");
        }

        public GrantCommandExecutionResult AddToRandomPool(int ruleIndex, EtgPickupCatalogEntry pickup)
        {
            if (pickup == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.loadout_editor.pickup_missing");
            }

            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            List<LoadoutRuleFileRuleModel> rules = new List<LoadoutRuleFileRuleModel>(activePreset.Rules ?? new LoadoutRuleFileRuleModel[0]);
            if (ruleIndex < 0 || ruleIndex >= rules.Count || !IsRandomPoolRule(rules[ruleIndex]))
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.random_pool.missing");
            }

            LoadoutRuleFileRuleModel rule = rules[ruleIndex];
            List<int> poolIds = new List<int>(rule.PoolIds ?? new int[0]);
            if (poolIds.Contains(pickup.PickupId))
            {
                return new GrantCommandExecutionResult(
                    false,
                    GuiText.Get("result.start_items.random_pool.duplicate", GetPickupDisplayName(pickup), pickup.PickupId),
                    GuiText.GetEnglish("result.start_items.random_pool.duplicate", GetPickupDisplayName(pickup), pickup.PickupId) +
                    " [RuleFile=" + GetRuleFilePath() + "; RuleIndex=" + ruleIndex + "; PickupId=" + pickup.PickupId + "]");
            }

            poolIds.Add(pickup.PickupId);
            rule.PoolIds = poolIds.ToArray();
            rule.PoolAliases = rule.PoolAliases ?? new string[0];
            rule.Pool = rule.Pool ?? new string[0];
            activePreset.Rules = rules.ToArray();
            SaveEditableModel(model);
            InvalidateResolvedConfig();

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.random_pool.added", GetPickupDisplayName(pickup), pickup.PickupId),
                GuiText.GetEnglish("result.start_items.random_pool.added", GetPickupDisplayName(pickup), pickup.PickupId) +
                " [RuleFile=" + GetRuleFilePath() + "; RuleIndex=" + ruleIndex + "; PickupId=" + pickup.PickupId + "; PoolCount=" + poolIds.Count + "]");
        }

        public GrantCommandExecutionResult RemoveFromRandomPool(int ruleIndex, int poolIndex)
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            List<LoadoutRuleFileRuleModel> rules = new List<LoadoutRuleFileRuleModel>(activePreset.Rules ?? new LoadoutRuleFileRuleModel[0]);
            if (ruleIndex < 0 || ruleIndex >= rules.Count || !IsRandomPoolRule(rules[ruleIndex]))
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.random_pool.missing");
            }

            LoadoutRuleFileRuleModel rule = rules[ruleIndex];
            List<int> poolIds = new List<int>(rule.PoolIds ?? new int[0]);
            if (poolIndex < 0 || poolIndex >= poolIds.Count)
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.random_pool.item_missing");
            }

            int removedPickupId = poolIds[poolIndex];
            poolIds.RemoveAt(poolIndex);
            rule.PoolIds = poolIds.ToArray();
            activePreset.Rules = rules.ToArray();
            SaveEditableModel(model);
            InvalidateResolvedConfig();

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.random_pool.removed"),
                GuiText.GetEnglish("result.start_items.random_pool.removed") +
                " [RuleFile=" + GetRuleFilePath() + "; RuleIndex=" + ruleIndex + "; RemovedPickupId=" + removedPickupId + "; PoolCount=" + poolIds.Count + "]");
        }

        public string GetRandomPoolDisplayName(int ruleIndex)
        {
            LoadoutRuleFileRuleModel rule = GetActivePresetRuleAt(ruleIndex);
            return GetRandomPoolDisplayName(rule);
        }

        public GrantCommandExecutionResult RenameRandomPool(int ruleIndex, string newName)
        {
            string normalizedName = StartItemsPresetNames.NormalizePresetName(newName);
            if (string.IsNullOrEmpty(normalizedName))
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.random_pool.rename_empty");
            }

            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            List<LoadoutRuleFileRuleModel> rules = new List<LoadoutRuleFileRuleModel>(activePreset.Rules ?? new LoadoutRuleFileRuleModel[0]);
            if (ruleIndex < 0 || ruleIndex >= rules.Count || !IsRandomPoolRule(rules[ruleIndex]))
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.random_pool.missing");
            }

            LoadoutRuleFileRuleModel rule = rules[ruleIndex];
            string oldDisplayName = GetRandomPoolDisplayName(rule);
            if (string.Equals(oldDisplayName, normalizedName, StringComparison.Ordinal))
            {
                return new GrantCommandExecutionResult(
                    true,
                    GuiText.Get("result.start_items.random_pool.renamed", oldDisplayName, normalizedName),
                    GuiText.GetEnglish("result.start_items.random_pool.renamed", oldDisplayName, normalizedName) +
                    " [RuleFile=" + GetRuleFilePath() + "; RuleIndex=" + ruleIndex + "]");
            }

            rule.Name = normalizedName;
            activePreset.Rules = rules.ToArray();
            SaveEditableModel(model);
            InvalidateResolvedConfig();

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.random_pool.renamed", oldDisplayName, normalizedName),
                GuiText.GetEnglish("result.start_items.random_pool.renamed", oldDisplayName, normalizedName) +
                " [RuleFile=" + GetRuleFilePath() + "; RuleIndex=" + ruleIndex + "; Name=" + normalizedName + "]");
        }

        public GrantCommandExecutionResult Reload()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFileRuleModel[] rules = _ruleFileProvider.GetActivePresetRules(model, null);
            int ruleCount = rules != null ? rules.Length : 0;
            InvalidateResolvedConfig();
            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.reload.success"),
                GuiText.GetEnglish("result.start_items.reload.success") +
                " [RuleFile=" + GetRuleFilePath() + "; RuleCount=" + ruleCount + "]");
        }

        public GrantCommandExecutionResult FillActivePresetFromCurrentItems(PlayerController player)
        {
            if ((object)player == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.preset.fill_current.no_player");
            }

            HashSet<int> ownedPickupIds = _ownedPickupReader != null
                ? _ownedPickupReader.CollectOwnedPickupIds(player)
                : new HashSet<int>();
            if (ownedPickupIds.Count == 0)
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.preset.fill_current.empty");
            }

            Dictionary<int, EtgPickupCatalogEntry> catalogById = BuildCatalogById();
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            string activePresetName = StartItemsPresetNames.GetDisplayName(activePreset);
            List<LoadoutRuleFileRuleModel> rules = new List<LoadoutRuleFileRuleModel>(activePreset.Rules ?? new LoadoutRuleFileRuleModel[0]);
            HashSet<int> existingSpecificIds = BuildSpecificPickupIdSet(rules);
            int addedCount = 0;
            int skippedDuplicateCount = 0;
            int skippedUnsupportedCount = 0;

            foreach (int pickupId in ownedPickupIds)
            {
                if (existingSpecificIds.Contains(pickupId))
                {
                    skippedDuplicateCount++;
                    continue;
                }

                EtgPickupCatalogEntry pickup;
                if (!catalogById.TryGetValue(pickupId, out pickup))
                {
                    skippedUnsupportedCount++;
                    continue;
                }

                rules.Add(CreateSpecificRule(pickup));
                existingSpecificIds.Add(pickupId);
                addedCount++;
            }

            if (addedCount == 0)
            {
                return new GrantCommandExecutionResult(
                    true,
                    GuiText.Get("result.start_items.preset.fill_current.no_new", activePresetName, skippedDuplicateCount, skippedUnsupportedCount),
                    GuiText.GetEnglish("result.start_items.preset.fill_current.no_new", activePresetName, skippedDuplicateCount, skippedUnsupportedCount) +
                    " [RuleFile=" + GetRuleFilePath() + "; Preset=" + activePresetName + "; OwnedPickupCount=" + ownedPickupIds.Count +
                    "; SkippedDuplicateCount=" + skippedDuplicateCount + "; SkippedUnsupportedCount=" + skippedUnsupportedCount + "]");
            }

            activePreset.Rules = rules.ToArray();
            SaveEditableModel(model);
            InvalidateResolvedConfig();

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.preset.fill_current.success", activePresetName, addedCount, skippedDuplicateCount, skippedUnsupportedCount),
                GuiText.GetEnglish("result.start_items.preset.fill_current.success", activePresetName, addedCount, skippedDuplicateCount, skippedUnsupportedCount) +
                " [RuleFile=" + GetRuleFilePath() + "; Preset=" + activePresetName + "; OwnedPickupCount=" + ownedPickupIds.Count +
                "; AddedCount=" + addedCount + "; SkippedDuplicateCount=" + skippedDuplicateCount +
                "; SkippedUnsupportedCount=" + skippedUnsupportedCount + "; RuleCount=" + rules.Count + "]");
        }

        private LoadoutRuleFileModel LoadEditableModel()
        {
            if (_ruleFileProvider == null)
            {
                return new LoadoutRuleFileModel();
            }

            _ruleFileProvider.ActivePresetName = GetActivePresetId();
            return _ruleFileProvider.LoadEditableModel();
        }

        private void SaveEditableModel(LoadoutRuleFileModel model)
        {
            if (_ruleFileProvider != null)
            {
                _ruleFileProvider.SaveEditableModel(model ?? new LoadoutRuleFileModel());
            }
        }

        private void InvalidateResolvedConfig()
        {
            if (_invalidateResolvedConfig != null)
            {
                _invalidateResolvedConfig();
            }
        }

        private string GetRuleFilePath()
        {
            return _ruleFileProvider != null ? _ruleFileProvider.FilePath : string.Empty;
        }

        private static bool ContainsSpecificPickupId(List<LoadoutRuleFileRuleModel> rules, int pickupId)
        {
            if (rules == null)
            {
                return false;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                LoadoutRuleFileRuleModel rule = rules[i];
                if (rule != null &&
                    rule.Id.HasValue &&
                    rule.Id.Value == pickupId &&
                    string.Equals(rule.Mode, "specific", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static HashSet<int> BuildSpecificPickupIdSet(List<LoadoutRuleFileRuleModel> rules)
        {
            HashSet<int> pickupIds = new HashSet<int>();
            if (rules == null)
            {
                return pickupIds;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                LoadoutRuleFileRuleModel rule = rules[i];
                if (rule != null &&
                    rule.Id.HasValue &&
                    string.Equals(rule.Mode, "specific", StringComparison.OrdinalIgnoreCase))
                {
                    pickupIds.Add(rule.Id.Value);
                }
            }

            return pickupIds;
        }

        private static LoadoutRuleFileRuleModel CreateSpecificRule(EtgPickupCatalogEntry pickup)
        {
            return new LoadoutRuleFileRuleModel
            {
                Enabled = true,
                Mode = "specific",
                Category = ToRuleCategory(pickup.Category),
                Count = 1,
                Id = pickup.PickupId,
                Pool = new string[0],
                PoolAliases = new string[0],
                PoolIds = new int[0],
            };
        }

        private static LoadoutRuleFileRuleModel CreateRandomPoolRule(string name)
        {
            return new LoadoutRuleFileRuleModel
            {
                Enabled = true,
                Mode = "random",
                Category = string.Empty,
                Count = 1,
                Name = StartItemsPresetNames.NormalizePresetName(name),
                Pool = new string[0],
                PoolAliases = new string[0],
                PoolIds = new int[0],
            };
        }

        private static string GetRandomPoolDisplayName(LoadoutRuleFileRuleModel rule)
        {
            return !string.IsNullOrEmpty(rule != null ? rule.Name : string.Empty)
                ? rule.Name
                : GuiText.Get("gui.loadout_editor.rule.random_pool_title");
        }

        private static string CreateUniqueRandomPoolName(List<LoadoutRuleFileRuleModel> rules)
        {
            string baseName = GuiText.Get("gui.loadout_editor.rule.random_pool_title");
            if (!RandomPoolDisplayNameExists(rules, baseName))
            {
                return baseName;
            }

            for (int index = 2; index < 1000; index++)
            {
                string candidate = baseName + "-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (!RandomPoolDisplayNameExists(rules, candidate))
                {
                    return candidate;
                }
            }

            return baseName + "-" + DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool RandomPoolDisplayNameExists(List<LoadoutRuleFileRuleModel> rules, string displayName)
        {
            if (rules == null)
            {
                return false;
            }

            string normalizedDisplayName = StartItemsPresetNames.NormalizePresetName(displayName);
            for (int i = 0; i < rules.Count; i++)
            {
                LoadoutRuleFileRuleModel rule = rules[i];
                if (!IsRandomPoolRule(rule))
                {
                    continue;
                }

                if (string.Equals(GetRandomPoolDisplayName(rule), normalizedDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ToRuleCategory(PickupCategory category)
        {
            switch (category)
            {
                case PickupCategory.Gun:
                    return "gun";
                case PickupCategory.Passive:
                    return "passive";
                case PickupCategory.Active:
                    return "active";
                default:
                    return "gun";
            }
        }
    }
}
