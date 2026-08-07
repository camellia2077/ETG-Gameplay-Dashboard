// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using BepInEx.Logging;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private ControllerFocusEntry[] GetPickupPageFocusEntries()
        {
            PickupBrowserEntry[] matches = GetFilteredPickupEntries();
            int extraFilterEntryCount = 8;
            if (_pickupBrowserFilter == PickupBrowserFilter.Gun)
            {
                extraFilterEntryCount += 10;
            }
            else if (_pickupBrowserFilter == PickupBrowserFilter.Passive)
            {
                extraFilterEntryCount += 2;
            }
            else if (_pickupBrowserFilter == PickupBrowserFilter.Active)
            {
                extraFilterEntryCount += 5;
            }

            ControllerFocusEntry[] entries = new ControllerFocusEntry[12 + extraFilterEntryCount + matches.Length];
            entries[0] = new ControllerFocusEntry("pickups.back", 0, 0);
            entries[1] = new ControllerFocusEntry("pickups.target", 0, 1);
            entries[2] = new ControllerFocusEntry("pickups.search", 1, 0);
            entries[3] = new ControllerFocusEntry(PickupSearchClearControlId, 1, 1);
            entries[4] = new ControllerFocusEntry(GetPickupCategoryFilterControlId(PickupBrowserFilter.All), 2, 0);
            entries[5] = new ControllerFocusEntry(GetPickupCategoryFilterControlId(PickupBrowserFilter.Gun), 2, 1);
            entries[6] = new ControllerFocusEntry(GetPickupCategoryFilterControlId(PickupBrowserFilter.Passive), 2, 2);
            entries[7] = new ControllerFocusEntry(GetPickupCategoryFilterControlId(PickupBrowserFilter.Active), 2, 3);
            entries[8] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.S), 3, 0);
            entries[9] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.A), 3, 1);
            entries[10] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.B), 3, 2);
            entries[11] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.C), 3, 3);
            entries[12] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.D), 3, 4);
            entries[13] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.All), 3, 5);
            entries[14] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.Special), 3, 6);
            entries[15] = new ControllerFocusEntry(GetPickupQualityFilterControlId(PickupQualityFilter.Excluded), 3, 7);
            int writeIndex = 16;
            int listStartRow = 4;
            if (_pickupBrowserFilter == PickupBrowserFilter.Gun)
            {
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.All), 4, 0);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Pistol), 4, 1);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.FullAuto), 4, 2);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Shotgun), 4, 3);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Rifle), 4, 4);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Beam), 4, 5);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Charge), 4, 6);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Explosive), 5, 0);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Elemental), 5, 1);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupGunClassFilterControlId(PickupGunClassFilter.Special), 5, 2);
                listStartRow = 6;
            }
            else if (_pickupBrowserFilter == PickupBrowserFilter.Passive)
            {
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupPassiveFilterControlId(PickupPassiveSubcategoryFilter.All), 4, 0);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupPassiveFilterControlId(PickupPassiveSubcategoryFilter.Bullet), 4, 1);
                listStartRow = 5;
            }
            else if (_pickupBrowserFilter == PickupBrowserFilter.Active)
            {
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter.All), 4, 0);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter.Uses), 4, 1);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter.Damage), 4, 2);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter.Time), 4, 3);
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter.Room), 4, 4);
                listStartRow = 5;
            }

            for (int index = 0; index < matches.Length; index++)
            {
                entries[writeIndex++] = new ControllerFocusEntry(GetPickupRowActionControlId(matches[index]), listStartRow + index, 0);
            }

            return entries;
        }

        private void ExecutePickupPageFocusedControl(PlayerController player, ManualLogSource logger)
        {
            if (string.Equals(_pickupPageFocusedControlId, "pickups.back", StringComparison.Ordinal))
            {
                ReturnFromPickupPage();
                return;
            }

            if (string.Equals(_pickupPageFocusedControlId, "pickups.target", StringComparison.Ordinal))
            {
                ToggleCharacterSwitchTarget(logger);
                return;
            }

            if (string.Equals(_pickupPageFocusedControlId, "pickups.search", StringComparison.Ordinal))
            {
                _focusPickupSearchField = true;
                return;
            }

            if (string.Equals(_pickupPageFocusedControlId, PickupSearchClearControlId, StringComparison.Ordinal))
            {
                ClearPickupSearchText();
                return;
            }

            if (TryExecutePickupFilterFocusedControl())
            {
                return;
            }

            PickupBrowserEntry[] matches = GetFilteredPickupEntries();
            for (int index = 0; index < matches.Length; index++)
            {
                PickupBrowserEntry entry = matches[index];
                if (!string.Equals(_pickupPageFocusedControlId, GetPickupRowActionControlId(entry), StringComparison.Ordinal))
                {
                    continue;
                }

                if (_isPickupShortcutConfigurationMode)
                {
                    return;
                }

                if (_pickupBrowserMode == PickupBrowserMode.AddToStartItems)
                {
                    ExecuteLoadoutEditorAdd(entry.CatalogEntry, logger);
                }
                else if (_pickupBrowserMode == PickupBrowserMode.AddToRandomPool)
                {
                    ExecuteLoadoutEditorAddToRandomPool(entry.CatalogEntry, logger);
                }
                else
                {
                    ExecutePickupBrowserGrant(entry, player, logger);
                }

                return;
            }
        }

        private static string GetPickupRowActionControlId(PickupBrowserEntry entry)
        {
            int pickupId = entry != null && entry.CatalogEntry != null ? entry.CatalogEntry.PickupId : -1;
            return "pickups.entry." + pickupId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private bool TryExecutePickupFilterFocusedControl()
        {
            PickupBrowserFilter browserFilter;
            if (TryGetPickupCategoryFilterFromControlId(_pickupPageFocusedControlId, out browserFilter))
            {
                ApplyPickupBrowserFilter(browserFilter);
                return true;
            }

            PickupQualityFilter qualityFilter;
            if (TryGetPickupQualityFilterFromControlId(_pickupPageFocusedControlId, out qualityFilter))
            {
                ApplyPickupQualityFilter(qualityFilter);
                return true;
            }

            PickupGunClassFilter gunClassFilter;
            if (TryGetPickupGunClassFilterFromControlId(_pickupPageFocusedControlId, out gunClassFilter))
            {
                ApplyPickupGunClassFilter(gunClassFilter);
                return true;
            }

            PickupPassiveSubcategoryFilter passiveFilter;
            if (TryGetPickupPassiveFilterFromControlId(_pickupPageFocusedControlId, out passiveFilter))
            {
                ApplyPickupPassiveSubcategoryFilter(passiveFilter);
                return true;
            }

            PickupActiveCooldownFilter cooldownFilter;
            if (TryGetPickupActiveCooldownFilterFromControlId(_pickupPageFocusedControlId, out cooldownFilter))
            {
                ApplyPickupActiveCooldownFilter(cooldownFilter);
                return true;
            }

            return false;
        }

        private void ApplyPickupBrowserFilter(PickupBrowserFilter filter)
        {
            _pickupBrowserFilter = filter;
            if (_pickupBrowserFilter != PickupBrowserFilter.Gun)
            {
                _pickupGunClassFilter = PickupGunClassFilter.All;
            }

            if (_pickupBrowserFilter != PickupBrowserFilter.Passive)
            {
                _pickupPassiveSubcategoryFilter = PickupPassiveSubcategoryFilter.All;
            }

            if (_pickupBrowserFilter != PickupBrowserFilter.Active)
            {
                _pickupActiveCooldownFilter = PickupActiveCooldownFilter.All;
            }

            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = GetPickupCategoryFilterControlId(filter);
            RequestGuiFocusRelease();
        }

        private void ApplyPickupQualityFilter(PickupQualityFilter filter)
        {
            _pickupQualityFilter = filter;
            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = GetPickupQualityFilterControlId(filter);
            RequestGuiFocusRelease();
        }

        private void ApplyPickupGunClassFilter(PickupGunClassFilter filter)
        {
            _pickupGunClassFilter = filter;
            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = GetPickupGunClassFilterControlId(filter);
            RequestGuiFocusRelease();
        }

        private void ApplyPickupPassiveSubcategoryFilter(PickupPassiveSubcategoryFilter filter)
        {
            _pickupPassiveSubcategoryFilter = filter;
            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = GetPickupPassiveFilterControlId(filter);
            RequestGuiFocusRelease();
        }

        private void ApplyPickupActiveCooldownFilter(PickupActiveCooldownFilter filter)
        {
            _pickupActiveCooldownFilter = filter;
            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupPageFocusedControlId = GetPickupActiveCooldownFilterControlId(filter);
            RequestGuiFocusRelease();
        }

        private void EnsurePickupBrowserFocusedResultVisible(PickupBrowserEntry[] matches, float listHeight)
        {
            int focusedResultIndex = GetPickupFocusedResultIndex(matches);
            if (focusedResultIndex < 0)
            {
                return;
            }

            float itemTop = 2f + (focusedResultIndex * (PickupBrowserRowHeight + PickupBrowserRowGap));
            float itemBottom = itemTop + (PickupBrowserRowHeight - 4f);
            if (_pickupScrollPosition.y > itemTop)
            {
                _pickupScrollPosition.y = itemTop;
                return;
            }

            float visibleBottom = _pickupScrollPosition.y + listHeight;
            if (itemBottom > visibleBottom)
            {
                _pickupScrollPosition.y = itemBottom - listHeight;
            }
        }

        private int GetPickupFocusedResultIndex(PickupBrowserEntry[] matches)
        {
            if (matches == null)
            {
                return -1;
            }

            for (int index = 0; index < matches.Length; index++)
            {
                if (string.Equals(_pickupPageFocusedControlId, GetPickupRowActionControlId(matches[index]), StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private bool IsPickupFocusOnCategoryFilter(PickupBrowserFilter filter)
        {
            return string.Equals(_pickupPageFocusedControlId, GetPickupCategoryFilterControlId(filter), StringComparison.Ordinal);
        }

        private bool IsPickupFocusOnQualityFilter(PickupQualityFilter filter)
        {
            return string.Equals(_pickupPageFocusedControlId, GetPickupQualityFilterControlId(filter), StringComparison.Ordinal);
        }

        private bool IsPickupFocusOnGunClassFilter(PickupGunClassFilter filter)
        {
            return string.Equals(_pickupPageFocusedControlId, GetPickupGunClassFilterControlId(filter), StringComparison.Ordinal);
        }

        private bool IsPickupFocusOnPassiveFilter(PickupPassiveSubcategoryFilter filter)
        {
            return string.Equals(_pickupPageFocusedControlId, GetPickupPassiveFilterControlId(filter), StringComparison.Ordinal);
        }

        private bool IsPickupFocusOnActiveCooldownFilter(PickupActiveCooldownFilter filter)
        {
            return string.Equals(_pickupPageFocusedControlId, GetPickupActiveCooldownFilterControlId(filter), StringComparison.Ordinal);
        }

        private static string GetPickupCategoryFilterControlId(PickupBrowserFilter filter)
        {
            return "pickups.filter.category." + filter.ToString();
        }

        private static string GetPickupQualityFilterControlId(PickupQualityFilter filter)
        {
            return "pickups.filter.quality." + filter.ToString();
        }

        private static string GetPickupGunClassFilterControlId(PickupGunClassFilter filter)
        {
            return "pickups.filter.gunclass." + filter.ToString();
        }

        private static string GetPickupPassiveFilterControlId(PickupPassiveSubcategoryFilter filter)
        {
            return "pickups.filter.passive." + filter.ToString();
        }

        private static string GetPickupActiveCooldownFilterControlId(PickupActiveCooldownFilter filter)
        {
            return "pickups.filter.activecooldown." + filter.ToString();
        }

        private static bool TryGetPickupCategoryFilterFromControlId(string controlId, out PickupBrowserFilter filter)
        {
            return TryParsePickupFilterControlId(controlId, "pickups.filter.category.", out filter);
        }

        private static bool TryGetPickupQualityFilterFromControlId(string controlId, out PickupQualityFilter filter)
        {
            return TryParsePickupFilterControlId(controlId, "pickups.filter.quality.", out filter);
        }

        private static bool TryGetPickupGunClassFilterFromControlId(string controlId, out PickupGunClassFilter filter)
        {
            return TryParsePickupFilterControlId(controlId, "pickups.filter.gunclass.", out filter);
        }

        private static bool TryGetPickupPassiveFilterFromControlId(string controlId, out PickupPassiveSubcategoryFilter filter)
        {
            return TryParsePickupFilterControlId(controlId, "pickups.filter.passive.", out filter);
        }

        private static bool TryGetPickupActiveCooldownFilterFromControlId(string controlId, out PickupActiveCooldownFilter filter)
        {
            return TryParsePickupFilterControlId(controlId, "pickups.filter.activecooldown.", out filter);
        }

        private static bool TryParsePickupFilterControlId<TEnum>(string controlId, string prefix, out TEnum filter)
            where TEnum : struct
        {
            filter = default(TEnum);
            if (string.IsNullOrEmpty(controlId) || !controlId.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                object parsedValue = Enum.Parse(typeof(TEnum), controlId.Substring(prefix.Length), true);
                if (!(parsedValue is TEnum))
                {
                    return false;
                }

                filter = (TEnum)parsedValue;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
