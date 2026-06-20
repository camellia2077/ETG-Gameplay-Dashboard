using System;
using System.Collections.Generic;

namespace RandomLoadout.Core
{
    public sealed class LoadoutSelectionService
    {
        public LoadoutSelectionResult SelectLoadout(LoadoutSelectionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            List<SelectedPickup> selections = new List<SelectedPickup>();
            List<SelectionWarning> warnings = new List<SelectionWarning>();

            if (request.Config.Rules.Length == 0)
            {
                warnings.Add(new SelectionWarning(null, "ConfigEmpty", "No loadout rules were configured."));
                return new LoadoutSelectionResult(request.Seed, selections, warnings);
            }

            Random rng = new Random(request.Seed);
            HashSet<int> ownedIds = new HashSet<int>(request.OwnedPickupIds);
            HashSet<int> selectedIds = new HashSet<int>();

            for (int i = 0; i < request.Config.Rules.Length; i++)
            {
                LoadoutRuleConfig rule = request.Config.Rules[i];
                if (rule == null)
                {
                    warnings.Add(new SelectionWarning(null, "NullRule", "Encountered a null loadout rule configuration."));
                    continue;
                }

                switch (rule.Mode)
                {
                    case GrantMode.Random:
                        SelectRandomRule(rule, rng, ownedIds, selectedIds, selections, warnings);
                        break;
                    case GrantMode.Specific:
                        SelectSpecificRule(rule, ownedIds, selectedIds, selections, warnings);
                        break;
                    default:
                        warnings.Add(new SelectionWarning(rule.Category, "UnsupportedGrantMode", "The loadout rule used an unsupported grant mode."));
                        break;
                }
            }

            return new LoadoutSelectionResult(request.Seed, selections, warnings);
        }

        private static void SelectRandomRule(
            LoadoutRuleConfig rule,
            Random rng,
            HashSet<int> ownedIds,
            HashSet<int> selectedIds,
            List<SelectedPickup> selections,
            List<SelectionWarning> warnings)
        {
            if (rule.Count <= 0)
            {
                return;
            }

            if (rule.PoolEntries.Length == 0)
            {
                warnings.Add(new SelectionWarning(rule.Category, "PoolEmpty", "The configured pickup pool is empty."));
                return;
            }

            List<LoadoutPoolEntryConfig> candidates = BuildCandidateEntries(rule, ownedIds, selectedIds);
            if (candidates.Count == 0)
            {
                warnings.Add(new SelectionWarning(rule.Category, "NoCandidates", "No valid pickup candidates remained after filtering."));
                return;
            }

            int selectedCount = 0;
            while (selectedCount < rule.Count && candidates.Count > 0)
            {
                int index = rng.Next(candidates.Count);
                LoadoutPoolEntryConfig pickup = candidates[index];
                candidates.RemoveAt(index);

                AddSelection(pickup.Category, pickup.PickupId, ownedIds, selectedIds, selections);
                selectedCount++;
            }

            if (selectedCount < rule.Count)
            {
                warnings.Add(
                    new SelectionWarning(
                        rule.Category,
                        "InsufficientCandidates",
                        "The configured pickup count exceeded the number of available candidates."));
            }
        }

        private static void SelectSpecificRule(
            LoadoutRuleConfig rule,
            HashSet<int> ownedIds,
            HashSet<int> selectedIds,
            List<SelectedPickup> selections,
            List<SelectionWarning> warnings)
        {
            if (rule.SpecificPickupId <= 0)
            {
                warnings.Add(new SelectionWarning(rule.Category, "SpecificInvalidPickup", "The configured specific pickup ID was invalid."));
                return;
            }

            if (selectedIds.Contains(rule.SpecificPickupId))
            {
                warnings.Add(new SelectionWarning(rule.Category, "SpecificAlreadySelected", "The configured specific pickup was already selected by an earlier rule."));
                return;
            }

            if (ownedIds.Contains(rule.SpecificPickupId))
            {
                warnings.Add(new SelectionWarning(rule.Category, "SpecificAlreadyOwned", "The configured specific pickup is already owned."));
                return;
            }

            AddSelection(rule.Category, rule.SpecificPickupId, ownedIds, selectedIds, selections);
        }

        private static void AddSelection(
            PickupCategory category,
            int pickupId,
            HashSet<int> ownedIds,
            HashSet<int> selectedIds,
            List<SelectedPickup> selections)
        {
            selections.Add(new SelectedPickup(category, pickupId));
            selectedIds.Add(pickupId);
            ownedIds.Add(pickupId);
        }

        private static List<LoadoutPoolEntryConfig> BuildCandidateEntries(LoadoutRuleConfig rule, HashSet<int> ownedIds, HashSet<int> selectedIds)
        {
            HashSet<int> seenIds = new HashSet<int>();
            List<LoadoutPoolEntryConfig> candidates = new List<LoadoutPoolEntryConfig>(rule.PoolEntries.Length);

            for (int i = 0; i < rule.PoolEntries.Length; i++)
            {
                LoadoutPoolEntryConfig pickup = rule.PoolEntries[i];
                if (pickup == null)
                {
                    continue;
                }

                int pickupId = pickup.PickupId;
                if (!seenIds.Add(pickupId))
                {
                    continue;
                }

                if (ownedIds.Contains(pickupId) || selectedIds.Contains(pickupId))
                {
                    continue;
                }

                candidates.Add(pickup);
            }

            return candidates;
        }
    }
}
