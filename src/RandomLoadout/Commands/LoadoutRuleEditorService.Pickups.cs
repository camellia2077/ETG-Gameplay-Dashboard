using System;
using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed partial class LoadoutRuleEditorService
    {
        public GrantCommandExecutionResult AddPresetPickup(string pickupType)
        {
            string normalizedType = StartItemPickupCatalog.NormalizeType(pickupType);
            if (string.IsNullOrEmpty(normalizedType))
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.pickups.invalid");
            }

            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            List<LoadoutRuleFilePickupModel> pickups = new List<LoadoutRuleFilePickupModel>(
                StartItemPickupCatalog.MergePickups(activePreset.Pickups));
            bool updatedExistingPickup = false;
            for (int i = 0; i < pickups.Count; i++)
            {
                if (!string.Equals(StartItemPickupCatalog.NormalizeType(pickups[i] != null ? pickups[i].Type : string.Empty), normalizedType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                pickups[i].Count = StartItemPickupCatalog.NormalizeCount(pickups[i].Count) + 1;
                updatedExistingPickup = true;
                break;
            }

            if (!updatedExistingPickup)
            {
                pickups.Add(new LoadoutRuleFilePickupModel { Type = normalizedType, Count = 1 });
            }

            activePreset.Pickups = pickups.ToArray();
            SaveEditableModel(model);
            InvalidateResolvedConfig();

            int displayCount = 1;
            for (int i = 0; i < pickups.Count; i++)
            {
                if (string.Equals(StartItemPickupCatalog.NormalizeType(pickups[i] != null ? pickups[i].Type : string.Empty), normalizedType, StringComparison.OrdinalIgnoreCase))
                {
                    displayCount = StartItemPickupCatalog.GetDisplayCount(normalizedType, pickups[i].Count);
                    break;
                }
            }

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.pickups.added", StartItemPickupCatalog.GetDisplayName(normalizedType)),
                GuiText.GetEnglish("result.start_items.pickups.added", StartItemPickupCatalog.GetEnglishDisplayName(normalizedType)) +
                " [RuleFile=" + GetRuleFilePath() + "; PickupType=" + normalizedType + "; PickupCount=" + pickups.Count + "; DisplayCount=" + displayCount + "]");
        }

        public GrantCommandExecutionResult ChangePresetPickupCount(int pickupIndex, int delta)
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            List<LoadoutRuleFilePickupModel> pickups = new List<LoadoutRuleFilePickupModel>(
                StartItemPickupCatalog.MergePickups(activePreset.Pickups));
            if (pickupIndex < 0 || pickupIndex >= pickups.Count)
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.pickups.missing");
            }

            LoadoutRuleFilePickupModel pickup = pickups[pickupIndex] ?? new LoadoutRuleFilePickupModel();
            pickup.Type = StartItemPickupCatalog.NormalizeType(pickup.Type);
            pickup.Count = StartItemPickupCatalog.NormalizeCount(pickup.Count + delta);
            pickups[pickupIndex] = pickup;
            activePreset.Pickups = pickups.ToArray();
            SaveEditableModel(model);
            InvalidateResolvedConfig();

            int displayCount = StartItemPickupCatalog.GetDisplayCount(pickup.Type, pickup.Count);

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.pickups.count_changed", StartItemPickupCatalog.GetDisplayName(pickup.Type), displayCount),
                GuiText.GetEnglish("result.start_items.pickups.count_changed", StartItemPickupCatalog.GetEnglishDisplayName(pickup.Type), displayCount) +
                " [RuleFile=" + GetRuleFilePath() + "; PickupType=" + pickup.Type + "; Count=" + pickup.Count + "]");
        }

        public GrantCommandExecutionResult SetPresetPickupCount(int pickupIndex, int count)
        {
            if (count <= 0)
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.pickups.invalid_count");
            }

            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            List<LoadoutRuleFilePickupModel> pickups = new List<LoadoutRuleFilePickupModel>(
                StartItemPickupCatalog.MergePickups(activePreset.Pickups));
            if (pickupIndex < 0 || pickupIndex >= pickups.Count)
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.pickups.missing");
            }

            LoadoutRuleFilePickupModel pickup = pickups[pickupIndex] ?? new LoadoutRuleFilePickupModel();
            pickup.Type = StartItemPickupCatalog.NormalizeType(pickup.Type);
            int storedCount;
            if (!StartItemPickupCatalog.TryGetStoredCount(pickup.Type, count, out storedCount))
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.pickups.invalid_count");
            }

            pickup.Count = storedCount;
            pickups[pickupIndex] = pickup;
            activePreset.Pickups = pickups.ToArray();
            SaveEditableModel(model);
            InvalidateResolvedConfig();

            int displayCount = StartItemPickupCatalog.GetDisplayCount(pickup.Type, pickup.Count);

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.pickups.count_changed", StartItemPickupCatalog.GetDisplayName(pickup.Type), displayCount),
                GuiText.GetEnglish("result.start_items.pickups.count_changed", StartItemPickupCatalog.GetEnglishDisplayName(pickup.Type), displayCount) +
                " [RuleFile=" + GetRuleFilePath() + "; PickupType=" + pickup.Type + "; Count=" + pickup.Count + "]");
        }

        public GrantCommandExecutionResult RemovePresetPickupAt(int pickupIndex)
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            List<LoadoutRuleFilePickupModel> pickups = new List<LoadoutRuleFilePickupModel>(
                StartItemPickupCatalog.MergePickups(activePreset.Pickups));
            if (pickupIndex < 0 || pickupIndex >= pickups.Count)
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.pickups.missing");
            }

            string removedType = StartItemPickupCatalog.NormalizeType(pickups[pickupIndex] != null ? pickups[pickupIndex].Type : string.Empty);
            pickups.RemoveAt(pickupIndex);
            activePreset.Pickups = pickups.ToArray();
            SaveEditableModel(model);
            InvalidateResolvedConfig();

            return new GrantCommandExecutionResult(
                true,
                GuiText.Get("result.start_items.pickups.removed"),
                GuiText.GetEnglish("result.start_items.pickups.removed") +
                " [RuleFile=" + GetRuleFilePath() + "; PickupType=" + removedType + "; PickupCount=" + pickups.Count + "]");
        }

        public GrantCommandExecutionResult ClearPresetPickups()
        {
            LoadoutRuleFileModel model = LoadEditableModel();
            LoadoutRuleFilePresetModel activePreset = _ruleFileProvider.EnsureActivePreset(model);
            LoadoutRuleFilePickupModel[] existingPickups = StartItemPickupCatalog.MergePickups(activePreset.Pickups);
            if (existingPickups.Length == 0)
            {
                return GrantCommandExecutionResult.Localized(false, "result.start_items.pickups.missing");
            }

            activePreset.Pickups = new LoadoutRuleFilePickupModel[0];
            SaveEditableModel(model);
            InvalidateResolvedConfig();

            return GrantCommandExecutionResult.Localized(true, "result.start_items.pickups.cleared");
        }
    }
}
