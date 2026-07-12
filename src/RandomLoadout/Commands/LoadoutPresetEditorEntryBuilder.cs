// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;

namespace RandomLoadout
{
    internal static class LoadoutPresetEditorEntryBuilder
    {
        public static LoadoutPresetEditorEntry Build(LoadoutRuleFilePresetModel preset, string activePresetId)
        {
            LoadoutRuleFileRuleModel[] safeRules = preset != null && preset.Rules != null
                ? preset.Rules
                : new LoadoutRuleFileRuleModel[0];
            int specificCount = 0;
            int randomCount = 0;
            int pickupCount = StartItemPickupCatalog.MergePickups(preset != null ? preset.Pickups : null).Length;
            List<int> gunPickupIds = new List<int>();
            List<int> activePickupIds = new List<int>();
            List<int> passivePickupIds = new List<int>();
            List<LoadoutPresetPreviewRow> randomPoolRows = new List<LoadoutPresetPreviewRow>();
            for (int index = 0; index < safeRules.Length; index++)
            {
                LoadoutRuleFileRuleModel rule = safeRules[index];
                if (rule == null)
                {
                    continue;
                }

                if (string.Equals(rule.Mode, "specific", StringComparison.OrdinalIgnoreCase))
                {
                    specificCount++;
                    AddPickupId(GetCategoryPickupIds(rule.Category, gunPickupIds, activePickupIds, passivePickupIds), rule.Id);
                    continue;
                }

                if (string.Equals(rule.Mode, "random", StringComparison.OrdinalIgnoreCase))
                {
                    randomCount++;
                    randomPoolRows.Add(BuildRandomPoolPreviewRow(rule));
                }
            }

            gunPickupIds.Sort();
            activePickupIds.Sort();
            passivePickupIds.Sort();

            List<LoadoutPresetPreviewRow> previewRows = new List<LoadoutPresetPreviewRow>();
            AddPreviewRow(previewRows, "gui.loadout_editor.preset_preview.gun", gunPickupIds);
            AddPreviewRow(previewRows, "gui.loadout_editor.preset_preview.active", activePickupIds);
            AddPreviewRow(previewRows, "gui.loadout_editor.preset_preview.passive", passivePickupIds);
            previewRows.AddRange(randomPoolRows);

            string presetId = StartItemsPresetNames.NormalizePresetId(preset != null ? preset.Id : string.Empty);
            return new LoadoutPresetEditorEntry(
                presetId,
                StartItemsPresetNames.GetDisplayName(preset),
                string.Equals(presetId, activePresetId, StringComparison.OrdinalIgnoreCase),
                safeRules.Length,
                specificCount,
                randomCount,
                pickupCount,
                previewRows.ToArray());
        }

        private static LoadoutPresetPreviewRow BuildRandomPoolPreviewRow(LoadoutRuleFileRuleModel rule)
        {
            List<int> poolPickupIds = new List<int>();
            if (rule.PoolIds != null)
            {
                for (int index = 0; index < rule.PoolIds.Length; index++)
                {
                    AddPickupId(poolPickupIds, rule.PoolIds[index]);
                }
            }

            poolPickupIds.Sort();
            return new LoadoutPresetPreviewRow("gui.loadout_editor.preset_preview.random", poolPickupIds.ToArray());
        }

        private static List<int> GetCategoryPickupIds(string category, List<int> gunPickupIds, List<int> activePickupIds, List<int> passivePickupIds)
        {
            switch ((category ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "active":
                    return activePickupIds;
                case "passive":
                    return passivePickupIds;
                case "gun":
                default:
                    return gunPickupIds;
            }
        }

        private static void AddPreviewRow(List<LoadoutPresetPreviewRow> previewRows, string labelKey, List<int> pickupIds)
        {
            if (previewRows == null || pickupIds == null || pickupIds.Count == 0)
            {
                return;
            }

            previewRows.Add(new LoadoutPresetPreviewRow(labelKey, pickupIds.ToArray()));
        }

        private static void AddPickupId(List<int> pickupIds, int? pickupId)
        {
            if (pickupIds == null || !pickupId.HasValue || pickupIds.Contains(pickupId.Value))
            {
                return;
            }

            pickupIds.Add(pickupId.Value);
        }
    }
}
