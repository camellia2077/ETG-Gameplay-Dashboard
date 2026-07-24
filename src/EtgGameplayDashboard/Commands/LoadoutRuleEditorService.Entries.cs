// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using EtgGameplayDashboard.Core;

namespace EtgGameplayDashboard
{
    internal sealed partial class LoadoutRuleEditorService
    {
        public LoadoutRuleEditorEntry[] GetEntries()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFileRuleModel[] rules = _ruleFileProvider.GetActivePresetRules(model, null);
            LoadoutRuleFilePickupModel[] presetPickups = _ruleFileProvider.GetActivePresetPickups(model, null);
            Dictionary<int, EtgPickupCatalogEntry> catalogById = BuildCatalogById();
            List<LoadoutRuleEditorEntry> entries = new List<LoadoutRuleEditorEntry>();

            for (int i = 0; i < rules.Length; i++)
            {
                LoadoutRuleFileRuleModel rule = rules[i];
                if (rule != null)
                {
                    EtgPickupCatalogEntry pickup = GetRulePickup(rule, catalogById);
                    entries.Add(new LoadoutRuleEditorEntry(i, BuildPrimaryText(rule, pickup), BuildSecondaryText(rule, pickup), GetRuleRepresentativePickupId(rule, pickup), IsRandomPoolRule(rule), string.Empty, 1, false, rule.Enabled));
                }
            }

            if (presetPickups.Length > 0)
            {
                int? representativePickupId = StartItemPickupCatalog.GetFirstRepresentativePickupId(presetPickups);
                string representativePickupType = string.Empty;
                for (int index = 0; index < presetPickups.Length; index++)
                {
                    string normalizedType = StartItemPickupCatalog.NormalizeType(presetPickups[index] != null ? presetPickups[index].Type : string.Empty);
                    if (!string.IsNullOrEmpty(normalizedType))
                    {
                        representativePickupType = normalizedType;
                        break;
                    }
                }

                entries.Add(
                    new LoadoutRuleEditorEntry(
                        -1,
                        GuiText.Get("gui.loadout_editor.pickups_title"),
                        GuiText.Get("gui.loadout_editor.pickups_collection_summary", presetPickups.Length),
                        representativePickupId,
                        false,
                        representativePickupType,
                        presetPickups.Length,
                        true));
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
                            GetPickupDisplayName(pickup) + " (ID " + pickupId + ")",
                            secondaryText));
                    continue;
                }

                entries.Add(new LoadoutRandomPoolEditorEntry(i, pickupId, "ID " + pickupId, string.Empty));
            }

            return entries.ToArray();
        }

        public LoadoutRuleEditorEntry[] GetPresetPickupEntries()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePickupModel[] pickups = _ruleFileProvider.GetActivePresetPickups(model, null);
            List<LoadoutRuleEditorEntry> entries = new List<LoadoutRuleEditorEntry>();
            for (int i = 0; i < pickups.Length; i++)
            {
                string normalizedType = StartItemPickupCatalog.NormalizeType(pickups[i] != null ? pickups[i].Type : string.Empty);
                if (string.IsNullOrEmpty(normalizedType))
                {
                    continue;
                }

                entries.Add(
                    new LoadoutRuleEditorEntry(
                        i,
                        StartItemPickupCatalog.GetDisplayName(normalizedType),
                        GuiText.Get("gui.loadout_editor.pickups_entry_hint", StartItemPickupCatalog.GetDisplayCount(normalizedType, pickups[i].Count)),
                        null,
                        false,
                        normalizedType,
                        StartItemPickupCatalog.GetDisplayCount(normalizedType, pickups[i].Count)));
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

        private static int? GetRuleRepresentativePickupId(LoadoutRuleFileRuleModel rule, EtgPickupCatalogEntry pickup)
        {
            if (pickup != null)
            {
                return pickup.PickupId;
            }

            if (IsRandomPoolRule(rule))
            {
                int[] poolIds = rule != null ? rule.PoolIds : null;
                if (poolIds != null && poolIds.Length > 0)
                {
                    return poolIds[0];
                }
            }

            return rule != null ? rule.Id : null;
        }

        private static string BuildPrimaryText(LoadoutRuleFileRuleModel rule, EtgPickupCatalogEntry pickup)
        {
            if (IsUntypedRandomPoolRule(rule))
            {
                return !string.IsNullOrEmpty(rule.Name)
                    ? rule.Name
                    : GuiText.Get("gui.loadout_editor.rule.random_pool_title");
            }

            string prefix = BuildRulePrefix(rule);
            if (rule.Id.HasValue)
            {
                if (pickup != null)
                {
                    return GuiText.Get("gui.loadout_editor.rule.title_with_value", prefix, GetPickupDisplayName(pickup) + " (ID " + rule.Id.Value + ")");
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

        private static string GetPickupDisplayName(EtgPickupCatalogEntry pickup)
        {
            if (pickup == null)
            {
                return string.Empty;
            }

            if (string.Equals(GuiText.CurrentLanguageCode, "en", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(pickup.EnglishDisplayName))
            {
                return pickup.EnglishDisplayName;
            }

            if (!string.IsNullOrEmpty(pickup.DisplayName))
            {
                return pickup.DisplayName;
            }

            if (!string.IsNullOrEmpty(pickup.EnglishDisplayName))
            {
                return pickup.EnglishDisplayName;
            }

            return pickup.InternalName ?? string.Empty;
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
