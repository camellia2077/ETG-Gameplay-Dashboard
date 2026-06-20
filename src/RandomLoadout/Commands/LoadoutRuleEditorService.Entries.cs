using System;
using System.Collections.Generic;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed partial class LoadoutRuleEditorService
    {
        public LoadoutRuleEditorEntry[] GetEntries()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFileRuleModel[] rules = _ruleFileProvider.GetActivePresetRules(model, null);
            Dictionary<int, EtgPickupCatalogEntry> catalogById = BuildCatalogById();
            List<LoadoutRuleEditorEntry> entries = new List<LoadoutRuleEditorEntry>();

            for (int i = 0; i < rules.Length; i++)
            {
                LoadoutRuleFileRuleModel rule = rules[i];
                if (rule != null)
                {
                    EtgPickupCatalogEntry pickup = GetRulePickup(rule, catalogById);
                    entries.Add(new LoadoutRuleEditorEntry(i, BuildPrimaryText(rule, pickup), BuildSecondaryText(rule, pickup), GetRulePickupId(rule, pickup), IsRandomPoolRule(rule)));
                }
            }

            return entries.ToArray();
        }

        public LoadoutRandomPoolEditorEntry[] GetRandomPoolEntries(int ruleIndex)
        {
            LoadoutRuleFileRuleModel rule = GetActivePresetRuleAt(ruleIndex);
            if (!IsRandomPoolRule(rule))
            {
                return new LoadoutRandomPoolEditorEntry[0];
            }

            Dictionary<int, EtgPickupCatalogEntry> catalogById = BuildCatalogById();
            int[] poolIds = rule.PoolIds ?? new int[0];
            List<LoadoutRandomPoolEditorEntry> entries = new List<LoadoutRandomPoolEditorEntry>();
            for (int i = 0; i < poolIds.Length; i++)
            {
                int pickupId = poolIds[i];
                EtgPickupCatalogEntry pickup;
                if (catalogById.TryGetValue(pickupId, out pickup))
                {
                    string metadata = BuildPickupMetadata(pickup);
                    string secondaryText = GuiText.GetCategoryLabel(pickup.Category);
                    if (!string.IsNullOrEmpty(metadata))
                    {
                        secondaryText = secondaryText + " | " + metadata;
                    }

                    entries.Add(
                        new LoadoutRandomPoolEditorEntry(
                            i,
                            pickupId,
                            pickup.DisplayName + " (ID " + pickupId + ")",
                            secondaryText));
                    continue;
                }

                entries.Add(new LoadoutRandomPoolEditorEntry(i, pickupId, "ID " + pickupId, string.Empty));
            }

            return entries.ToArray();
        }

        private Dictionary<int, EtgPickupCatalogEntry> BuildCatalogById()
        {
            Dictionary<int, EtgPickupCatalogEntry> catalogById = new Dictionary<int, EtgPickupCatalogEntry>();
            EtgPickupCatalogEntry[] catalog = _pickupCatalogProvider != null ? _pickupCatalogProvider() : new EtgPickupCatalogEntry[0];
            for (int i = 0; i < catalog.Length; i++)
            {
                EtgPickupCatalogEntry entry = catalog[i];
                if (entry != null && !catalogById.ContainsKey(entry.PickupId))
                {
                    catalogById.Add(entry.PickupId, entry);
                }
            }

            return catalogById;
        }

        private static EtgPickupCatalogEntry GetRulePickup(LoadoutRuleFileRuleModel rule, Dictionary<int, EtgPickupCatalogEntry> catalogById)
        {
            if (rule == null || !rule.Id.HasValue || catalogById == null)
            {
                return null;
            }

            EtgPickupCatalogEntry pickup;
            return catalogById.TryGetValue(rule.Id.Value, out pickup) ? pickup : null;
        }

        private static int? GetRulePickupId(LoadoutRuleFileRuleModel rule, EtgPickupCatalogEntry pickup)
        {
            if (pickup != null)
            {
                return pickup.PickupId;
            }

            return rule != null ? rule.Id : null;
        }

        private static string BuildPrimaryText(LoadoutRuleFileRuleModel rule, EtgPickupCatalogEntry pickup)
        {
            if (IsUntypedRandomPoolRule(rule))
            {
                return GuiText.Get("gui.loadout_editor.rule.random_pool_title");
            }

            string prefix = BuildRulePrefix(rule);
            if (rule.Id.HasValue)
            {
                if (pickup != null)
                {
                    return GuiText.Get("gui.loadout_editor.rule.title_with_value", prefix, pickup.DisplayName + " (ID " + rule.Id.Value + ")");
                }

                return GuiText.Get("gui.loadout_editor.rule.title_with_value", prefix, "ID " + rule.Id.Value);
            }

            if (!string.IsNullOrEmpty(rule.Alias))
            {
                return GuiText.Get("gui.loadout_editor.rule.title_with_value", prefix, GuiText.Get("gui.loadout_editor.rule.alias_value", rule.Alias));
            }

            if (!string.IsNullOrEmpty(rule.Name))
            {
                return GuiText.Get("gui.loadout_editor.rule.title_with_value", prefix, rule.Name);
            }

            return prefix;
        }

        private static string BuildSecondaryText(LoadoutRuleFileRuleModel rule, EtgPickupCatalogEntry pickup)
        {
            string enabled = rule.Enabled
                ? GuiText.Get("gui.loadout_editor.rule.enabled")
                : GuiText.Get("gui.loadout_editor.rule.disabled");
            string count = GuiText.Get("gui.loadout_editor.rule.count", rule.Count);
            string metadata = BuildPickupMetadata(pickup);
            if (string.Equals(rule.Mode, "random", StringComparison.OrdinalIgnoreCase))
            {
                if (IsUntypedRandomPoolRule(rule))
                {
                    return AppendMetadata(
                        GuiText.Get("gui.loadout_editor.rule.random_pool_summary", rule.Count, GetArrayLength(rule.PoolIds)),
                        metadata);
                }

                return AppendMetadata(
                    enabled +
                    " | " +
                    count +
                    " | " +
                    GuiText.Get("gui.loadout_editor.rule.pool_ids", GetArrayLength(rule.PoolIds)) +
                    " | " +
                    GuiText.Get("gui.loadout_editor.rule.pool_aliases", GetArrayLength(rule.PoolAliases)),
                    metadata);
            }

            return AppendMetadata(enabled + " | " + count, metadata);
        }

        private static string BuildRulePrefix(LoadoutRuleFileRuleModel rule)
        {
            string category = GetRuleCategoryLabel(rule != null ? rule.Category : string.Empty);
            string mode = GetRuleModeLabel(rule != null ? rule.Mode : string.Empty);
            if (string.IsNullOrEmpty(category))
            {
                return mode;
            }

            if (string.IsNullOrEmpty(mode))
            {
                return category;
            }

            return GuiText.Get("gui.loadout_editor.rule.title", category, mode);
        }

        private static string GetRuleCategoryLabel(string category)
        {
            string normalized = category != null ? category.Trim().ToLowerInvariant() : string.Empty;
            switch (normalized)
            {
                case "gun":
                    return GuiText.GetCategoryLabel(PickupCategory.Gun);
                case "passive":
                    return GuiText.GetCategoryLabel(PickupCategory.Passive);
                case "active":
                    return GuiText.GetCategoryLabel(PickupCategory.Active);
                default:
                    return string.Empty;
            }
        }

        private static string GetRuleModeLabel(string mode)
        {
            string normalized = mode != null ? mode.Trim().ToLowerInvariant() : string.Empty;
            switch (normalized)
            {
                case "specific":
                    return GuiText.Get("gui.loadout_editor.rule.mode.specific");
                case "random":
                    return GuiText.Get("gui.loadout_editor.rule.mode.random");
                default:
                    return mode ?? string.Empty;
            }
        }

        private static int GetArrayLength(Array values)
        {
            return values != null ? values.Length : 0;
        }

        private static string BuildPickupMetadata(EtgPickupCatalogEntry pickup)
        {
            if (pickup == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(pickup.Quality))
            {
                parts.Add(GuiText.Get("gui.loadout_editor.rule.quality", pickup.Quality));
            }

            if (pickup.Category == PickupCategory.Gun && !string.IsNullOrEmpty(pickup.GunClass))
            {
                parts.Add(GuiText.Get("gui.loadout_editor.rule.gun_class", pickup.GunClass));
            }

            if (pickup.Category == PickupCategory.Active)
            {
                parts.Add(GuiText.Get("gui.loadout_editor.rule.cooldown", BuildActiveCooldownText(pickup)));
            }

            return string.Join(" | ", parts.ToArray());
        }

        private static string BuildActiveCooldownText(EtgPickupCatalogEntry pickup)
        {
            if (pickup.ActiveNumberOfUses > 0)
            {
                return GuiText.Get("gui.loadout_editor.rule.cooldown_uses", pickup.ActiveNumberOfUses);
            }

            if (pickup.ActiveDamageCooldown > 0f)
            {
                return GuiText.Get("gui.loadout_editor.rule.cooldown_damage", pickup.ActiveDamageCooldown.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            }

            if (pickup.ActiveTimeCooldown > 0f)
            {
                return GuiText.Get("gui.loadout_editor.rule.cooldown_time", pickup.ActiveTimeCooldown.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            }

            if (pickup.ActiveRoomCooldown > 0)
            {
                return GuiText.Get("gui.loadout_editor.rule.cooldown_rooms", pickup.ActiveRoomCooldown);
            }

            return GuiText.Get("gui.loadout_editor.rule.cooldown_none");
        }

        private static string AppendMetadata(string baseText, string metadata)
        {
            if (string.IsNullOrEmpty(metadata))
            {
                return baseText;
            }

            return baseText + " | " + metadata;
        }

        private LoadoutRuleFileRuleModel GetActivePresetRuleAt(int ruleIndex)
        {
            if (ruleIndex < 0)
            {
                return null;
            }

            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFileRuleModel[] rules = _ruleFileProvider.GetActivePresetRules(model, null);
            return rules != null && ruleIndex < rules.Length ? rules[ruleIndex] : null;
        }

        private static bool IsRandomPoolRule(LoadoutRuleFileRuleModel rule)
        {
            return rule != null && string.Equals(rule.Mode, "random", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUntypedRandomPoolRule(LoadoutRuleFileRuleModel rule)
        {
            return IsRandomPoolRule(rule) && string.IsNullOrEmpty(rule.Category);
        }
    }
}
