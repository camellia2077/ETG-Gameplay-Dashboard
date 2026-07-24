// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;

namespace EtgGameplayDashboard.Core
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
            Dictionary<int, RandomPoolSelectionState> randomPoolStates = BuildRandomPoolStateMap(request.RandomPoolStates);
            List<RandomPoolSelectionState> updatedRandomPoolStates = new List<RandomPoolSelectionState>();

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
                        SelectRandomRule(rule, i, rng, ownedIds, selectedIds, selections, warnings, randomPoolStates, updatedRandomPoolStates);
                        break;
                    case GrantMode.Specific:
                        SelectSpecificRule(rule, ownedIds, selectedIds, selections, warnings);
                        break;
                    default:
                        warnings.Add(new SelectionWarning(rule.Category, "UnsupportedGrantMode", "The loadout rule used an unsupported grant mode."));
                        break;
                }
            }

            return new LoadoutSelectionResult(request.Seed, selections, warnings, updatedRandomPoolStates);
        }

        private static void SelectRandomRule(
            LoadoutRuleConfig rule,
            int ruleIndex,
            Random rng,
            HashSet<int> ownedIds,
            HashSet<int> selectedIds,
            List<SelectedPickup> selections,
            List<SelectionWarning> warnings,
            Dictionary<int, RandomPoolSelectionState> randomPoolStates,
            List<RandomPoolSelectionState> updatedRandomPoolStates)
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

            List<LoadoutPoolEntryConfig> poolEntries = BuildUniquePoolEntries(rule.PoolEntries);
            if (poolEntries.Count == 0)
            {
                warnings.Add(new SelectionWarning(rule.Category, "PoolEmpty", "The configured pickup pool is empty."));
                return;
            }

            string poolSignature = BuildPoolSignature(poolEntries);
            int[] shuffledPickupIds = CreateShuffledPickupIds(poolEntries, rng);
            int nextIndex = 0;
            RandomPoolSelectionState existingState;
            if (randomPoolStates != null &&
                randomPoolStates.TryGetValue(ruleIndex, out existingState) &&
                IsUsableState(existingState, poolSignature, poolEntries))
            {
                shuffledPickupIds = CopyIntArray(existingState.ShuffledPickupIds);
                nextIndex = existingState.NextIndex;
            }

            List<LoadoutPoolEntryConfig> candidates = BuildCandidateEntries(rule, ownedIds, selectedIds);
            if (candidates.Count == 0)
            {
                updatedRandomPoolStates.Add(new RandomPoolSelectionState(ruleIndex, poolSignature, shuffledPickupIds, nextIndex));
                warnings.Add(new SelectionWarning(rule.Category, "NoCandidates", "No valid pickup candidates remained after filtering."));
                return;
            }

            Dictionary<int, LoadoutPoolEntryConfig> candidateById = BuildEntryMap(candidates);
            HashSet<int> availableIds = new HashSet<int>(candidateById.Keys);
            int selectedCount = 0;
            int attemptsSinceSelection = 0;
            while (selectedCount < rule.Count && availableIds.Count > 0 && attemptsSinceSelection < shuffledPickupIds.Length)
            {
                if (nextIndex >= shuffledPickupIds.Length)
                {
                    shuffledPickupIds = CreateShuffledPickupIds(poolEntries, rng);
                    nextIndex = 0;
                    attemptsSinceSelection = 0;
                }

                int pickupId = shuffledPickupIds[nextIndex];
                nextIndex++;

                LoadoutPoolEntryConfig pickup;
                if (!availableIds.Contains(pickupId) || !candidateById.TryGetValue(pickupId, out pickup))
                {
                    attemptsSinceSelection++;
                    continue;
                }

                AddSelection(pickup.Category, pickup.PickupId, ownedIds, selectedIds, selections);
                availableIds.Remove(pickup.PickupId);
                selectedCount++;
                attemptsSinceSelection = 0;
            }

            updatedRandomPoolStates.Add(new RandomPoolSelectionState(ruleIndex, poolSignature, shuffledPickupIds, nextIndex));

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

        private static Dictionary<int, RandomPoolSelectionState> BuildRandomPoolStateMap(RandomPoolSelectionState[] states)
        {
            Dictionary<int, RandomPoolSelectionState> stateMap = new Dictionary<int, RandomPoolSelectionState>();
            if (states == null)
            {
                return stateMap;
            }

            for (int i = 0; i < states.Length; i++)
            {
                RandomPoolSelectionState state = states[i];
                if (state == null)
                {
                    continue;
                }

                stateMap[state.RuleIndex] = state;
            }

            return stateMap;
        }

        private static List<LoadoutPoolEntryConfig> BuildUniquePoolEntries(LoadoutPoolEntryConfig[] poolEntries)
        {
            List<LoadoutPoolEntryConfig> entries = new List<LoadoutPoolEntryConfig>();
            HashSet<int> seenIds = new HashSet<int>();
            if (poolEntries == null)
            {
                return entries;
            }

            for (int i = 0; i < poolEntries.Length; i++)
            {
                LoadoutPoolEntryConfig entry = poolEntries[i];
                if (entry == null || !seenIds.Add(entry.PickupId))
                {
                    continue;
                }

                entries.Add(entry);
            }

            return entries;
        }

        private static Dictionary<int, LoadoutPoolEntryConfig> BuildEntryMap(List<LoadoutPoolEntryConfig> entries)
        {
            Dictionary<int, LoadoutPoolEntryConfig> entryMap = new Dictionary<int, LoadoutPoolEntryConfig>();
            for (int i = 0; i < entries.Count; i++)
            {
                LoadoutPoolEntryConfig entry = entries[i];
                if (entry != null && !entryMap.ContainsKey(entry.PickupId))
                {
                    entryMap.Add(entry.PickupId, entry);
                }
            }

            return entryMap;
        }

        private static string BuildPoolSignature(List<LoadoutPoolEntryConfig> poolEntries)
        {
            string[] parts = new string[poolEntries.Count];
            for (int i = 0; i < poolEntries.Count; i++)
            {
                LoadoutPoolEntryConfig entry = poolEntries[i];
                parts[i] = entry.Category + ":" + entry.PickupId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return string.Join("|", parts);
        }

        private static bool IsUsableState(RandomPoolSelectionState state, string poolSignature, List<LoadoutPoolEntryConfig> poolEntries)
        {
            if (state == null ||
                !string.Equals(state.PoolSignature, poolSignature, StringComparison.Ordinal) ||
                state.ShuffledPickupIds == null ||
                state.ShuffledPickupIds.Length != poolEntries.Count)
            {
                return false;
            }

            HashSet<int> poolIds = new HashSet<int>();
            for (int i = 0; i < poolEntries.Count; i++)
            {
                poolIds.Add(poolEntries[i].PickupId);
            }

            HashSet<int> stateIds = new HashSet<int>();
            for (int i = 0; i < state.ShuffledPickupIds.Length; i++)
            {
                int pickupId = state.ShuffledPickupIds[i];
                if (!poolIds.Contains(pickupId) || !stateIds.Add(pickupId))
                {
                    return false;
                }
            }

            return true;
        }

        private static int[] CreateShuffledPickupIds(List<LoadoutPoolEntryConfig> poolEntries, Random rng)
        {
            int[] pickupIds = new int[poolEntries.Count];
            for (int i = 0; i < poolEntries.Count; i++)
            {
                pickupIds[i] = poolEntries[i].PickupId;
            }

            for (int i = pickupIds.Length - 1; i > 0; i--)
            {
                int swapIndex = rng.Next(i + 1);
                int temp = pickupIds[i];
                pickupIds[i] = pickupIds[swapIndex];
                pickupIds[swapIndex] = temp;
            }

            return pickupIds;
        }

        private static int[] CopyIntArray(int[] values)
        {
            if (values == null || values.Length == 0)
            {
                return new int[0];
            }

            int[] copy = new int[values.Length];
            Array.Copy(values, copy, values.Length);
            return copy;
        }
    }
}
