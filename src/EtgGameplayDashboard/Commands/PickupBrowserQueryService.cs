// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using EtgGameplayDashboard.Core;

namespace EtgGameplayDashboard
{
    /// <summary>
    /// Builds and queries the pickup catalog used by the in-game browser.
    /// This class owns catalog transformation and filtering; the controller only owns page state and rendering.
    /// </summary>
    internal sealed class PickupBrowserQueryService
    {
        private readonly Func<EtgPickupCatalogEntry[]> _catalogProvider;
        private readonly Func<PickupAliasRegistry> _aliasRegistryProvider;
        private readonly Func<int, string> _gameplayNameProvider;

        public PickupBrowserQueryService(
            Func<EtgPickupCatalogEntry[]> catalogProvider,
            Func<PickupAliasRegistry> aliasRegistryProvider,
            Func<int, string> gameplayNameProvider)
        {
            _catalogProvider = catalogProvider;
            _aliasRegistryProvider = aliasRegistryProvider;
            _gameplayNameProvider = gameplayNameProvider;
        }

        public PickupBrowserEntry[] BuildEntries()
        {
            if (_catalogProvider == null)
            {
                return new PickupBrowserEntry[0];
            }

            EtgPickupCatalogEntry[] catalogEntries = _catalogProvider() ?? new EtgPickupCatalogEntry[0];
            Dictionary<int, List<string>> aliasesByPickupId = BuildAliasesByPickupId(
                _aliasRegistryProvider != null ? _aliasRegistryProvider() : PickupAliasRegistry.Empty);
            List<PickupBrowserEntry> browserEntries = new List<PickupBrowserEntry>(catalogEntries.Length);
            for (int index = 0; index < catalogEntries.Length; index++)
            {
                EtgPickupCatalogEntry entry = catalogEntries[index];
                if (entry == null)
                {
                    continue;
                }

                List<string> aliases;
                aliasesByPickupId.TryGetValue(entry.PickupId, out aliases);
                browserEntries.Add(new PickupBrowserEntry(entry, aliases, _gameplayNameProvider));
            }

            browserEntries.Sort(CompareEntries);
            return browserEntries.ToArray();
        }

        public PickupBrowserEntry[] Filter(
            PickupBrowserEntry[] entries,
            string searchText,
            PickupBrowserFilter categoryFilter,
            PickupQualityFilter qualityFilter,
            PickupGunClassFilter gunClassFilter,
            PickupPassiveSubcategoryFilter passiveFilter,
            PickupActiveCooldownFilter activeCooldownFilter)
        {
            if (entries == null || entries.Length == 0)
            {
                return new PickupBrowserEntry[0];
            }

            string normalizedSearch = NormalizeLookupValue(searchText);
            List<PickupBrowserEntry> matches = new List<PickupBrowserEntry>();
            for (int index = 0; index < entries.Length; index++)
            {
                PickupBrowserEntry entry = entries[index];
                if (entry == null || entry.CatalogEntry == null ||
                    !MatchesCategory(entry.CatalogEntry.Category, categoryFilter) ||
                    !MatchesQuality(entry.CatalogEntry.Quality, qualityFilter) ||
                    !MatchesGunClass(entry.CatalogEntry, gunClassFilter) ||
                    !MatchesPassive(entry.CatalogEntry, passiveFilter) ||
                    !MatchesActiveCooldown(entry.CatalogEntry, activeCooldownFilter))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(normalizedSearch) &&
                    entry.SearchText.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                matches.Add(entry);
            }

            return matches.ToArray();
        }

        private static bool MatchesCategory(PickupCategory category, PickupBrowserFilter filter)
        {
            switch (filter)
            {
                case PickupBrowserFilter.Gun:
                    return category == PickupCategory.Gun;
                case PickupBrowserFilter.Passive:
                    return category == PickupCategory.Passive;
                case PickupBrowserFilter.Active:
                    return category == PickupCategory.Active;
                default:
                    return true;
            }
        }

        private static bool MatchesQuality(string quality, PickupQualityFilter filter)
        {
            if (filter == PickupQualityFilter.All)
            {
                return true;
            }

            string normalizedQuality = (quality ?? string.Empty).Trim();
            string expected = filter.ToString().ToUpperInvariant();
            return string.Equals(normalizedQuality, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesGunClass(EtgPickupCatalogEntry entry, PickupGunClassFilter filter)
        {
            if (filter == PickupGunClassFilter.All)
            {
                return true;
            }

            if (entry == null || entry.Category != PickupCategory.Gun)
            {
                return false;
            }

            string gunClass = (entry.GunClass ?? string.Empty).Trim();
            switch (filter)
            {
                case PickupGunClassFilter.Elemental:
                    return string.Equals(gunClass, "FIRE", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(gunClass, "ICE", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(gunClass, "POISON", StringComparison.OrdinalIgnoreCase);
                case PickupGunClassFilter.Special:
                    return string.Equals(gunClass, "SILLY", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(gunClass, "SHITTY", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(gunClass, "CHARM", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(gunClass, "NONE", StringComparison.OrdinalIgnoreCase);
                default:
                    return string.Equals(gunClass, filter.ToString().ToUpperInvariant(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool MatchesPassive(EtgPickupCatalogEntry entry, PickupPassiveSubcategoryFilter filter)
        {
            if (filter == PickupPassiveSubcategoryFilter.All)
            {
                return true;
            }

            return entry != null && entry.Category == PickupCategory.Passive &&
                filter == PickupPassiveSubcategoryFilter.Bullet && IsBulletPassive(entry);
        }

        private static bool IsBulletPassive(EtgPickupCatalogEntry entry)
        {
            return ContainsBulletToken(entry.DisplayName) ||
                ContainsBulletToken(entry.InternalName) ||
                ContainsBulletToken(entry.PrimaryDisplayName) ||
                ContainsBulletToken(entry.ShortDescription) ||
                ContainsBulletToken(entry.LongDescription);
        }

        private static bool ContainsBulletToken(string value)
        {
            string lowerValue = (value ?? string.Empty).ToLowerInvariant();
            return lowerValue.IndexOf("bullet", StringComparison.Ordinal) >= 0 ||
                lowerValue.IndexOf("round", StringComparison.Ordinal) >= 0 ||
                lowerValue.IndexOf("lead", StringComparison.Ordinal) >= 0;
        }

        private static bool MatchesActiveCooldown(EtgPickupCatalogEntry entry, PickupActiveCooldownFilter filter)
        {
            if (filter == PickupActiveCooldownFilter.All)
            {
                return true;
            }

            if (entry == null || entry.Category != PickupCategory.Active)
            {
                return false;
            }

            switch (filter)
            {
                case PickupActiveCooldownFilter.Uses:
                    return entry.ActiveNumberOfUses > 0;
                case PickupActiveCooldownFilter.Damage:
                    return entry.ActiveDamageCooldown > 0f;
                case PickupActiveCooldownFilter.Time:
                    return entry.ActiveTimeCooldown > 0f;
                case PickupActiveCooldownFilter.Room:
                    return entry.ActiveRoomCooldown > 0;
                default:
                    return true;
            }
        }

        private static int CompareEntries(PickupBrowserEntry left, PickupBrowserEntry right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            int categoryComparison = left.CatalogEntry.Category.CompareTo(right.CatalogEntry.Category);
            if (categoryComparison != 0) return categoryComparison;
            int displayNameComparison = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            return displayNameComparison != 0
                ? displayNameComparison
                : left.CatalogEntry.PickupId.CompareTo(right.CatalogEntry.PickupId);
        }

        private static Dictionary<int, List<string>> BuildAliasesByPickupId(PickupAliasRegistry aliasRegistry)
        {
            Dictionary<int, List<string>> aliasesByPickupId = new Dictionary<int, List<string>>();
            PickupAliasRegistry effectiveRegistry = aliasRegistry ?? PickupAliasRegistry.Empty;
            for (int index = 0; index < effectiveRegistry.Entries.Length; index++)
            {
                PickupAliasEntry entry = effectiveRegistry.Entries[index];
                List<string> aliases;
                if (!aliasesByPickupId.TryGetValue(entry.PickupId, out aliases))
                {
                    aliases = new List<string>();
                    aliasesByPickupId.Add(entry.PickupId, aliases);
                }

                aliases.Add(entry.Alias);
            }

            return aliasesByPickupId;
        }

        private static string NormalizeLookupValue(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(rawValue.Length);
            for (int index = 0; index < rawValue.Length; index++)
            {
                char current = rawValue[index];
                if (char.IsLetterOrDigit(current))
                {
                    builder.Append(char.ToLowerInvariant(current));
                }
            }

            return builder.ToString();
        }
    }
}
