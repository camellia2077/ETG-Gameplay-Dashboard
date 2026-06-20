using System.Collections.Generic;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class EtgLoadoutConfigResolver
    {
        private readonly EtgPickupResolver _pickupResolver;

        public EtgLoadoutConfigResolver(EtgPickupResolver pickupResolver)
        {
            _pickupResolver = pickupResolver;
        }

        public LoadoutConfigResolutionResult Resolve(LoadoutRuleDefinition[] definitions, PickupAliasRegistry aliasRegistry)
        {
            PickupAliasRegistry effectiveAliasRegistry = aliasRegistry ?? PickupAliasRegistry.Empty;
            List<LoadoutRuleConfig> rules = new List<LoadoutRuleConfig>();
            List<SelectionWarning> warnings = new List<SelectionWarning>();

            if (definitions == null)
            {
                warnings.Add(new SelectionWarning(null, "ConfigEmpty", "No loadout rules were configured."));
                return new LoadoutConfigResolutionResult(new LoadoutConfig(new LoadoutRuleConfig[0]), warnings.ToArray());
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                LoadoutRuleDefinition definition = definitions[i];
                if (definition == null)
                {
                    warnings.Add(new SelectionWarning(null, "NullRuleDefinition", "Encountered a null raw loadout rule definition."));
                    continue;
                }

                switch (definition.Mode)
                {
                    case GrantMode.Random:
                        List<LoadoutPoolEntryConfig> resolvedPoolEntries = new List<LoadoutPoolEntryConfig>();
                        HashSet<int> seenPoolIds = new HashSet<int>();

                        for (int poolIdIndex = 0; poolIdIndex < definition.PoolIds.Length; poolIdIndex++)
                        {
                            AddResolvedRandomPoolEntry(
                                _pickupResolver.ResolveAny(definition.PoolIds[poolIdIndex]),
                                resolvedPoolEntries,
                                seenPoolIds,
                                warnings);
                        }

                        for (int poolAliasIndex = 0; poolAliasIndex < definition.PoolAliases.Length; poolAliasIndex++)
                        {
                            string pickupAlias = definition.PoolAliases[poolAliasIndex];
                            int resolvedAliasPickupId;
                            if (!effectiveAliasRegistry.TryResolve(pickupAlias, out resolvedAliasPickupId))
                            {
                                warnings.Add(
                                    new SelectionWarning(
                                        definition.Category,
                                        "RandomAliasNotFound",
                                        "No pickup alias matched '" + pickupAlias + "'."));
                                continue;
                            }

                            AddResolvedRandomPoolEntry(
                                _pickupResolver.ResolveAny(resolvedAliasPickupId),
                                resolvedPoolEntries,
                                seenPoolIds,
                                warnings,
                                "Alias '" + pickupAlias + "' resolved to pickup ID " + resolvedAliasPickupId + ", but ");
                        }

                        for (int poolIndex = 0; poolIndex < definition.PoolNames.Length; poolIndex++)
                        {
                            string pickupName = definition.PoolNames[poolIndex];
                            // String pools now follow give-style resolution: internal name first,
                            // display name as a compatibility fallback.
                            AddResolvedRandomPoolEntry(
                                _pickupResolver.ResolveAny(pickupName),
                                resolvedPoolEntries,
                                seenPoolIds,
                                warnings);
                        }

                        rules.Add(LoadoutRuleConfig.CreateRandom(definition.Category, definition.Count, resolvedPoolEntries));
                        break;
                    case GrantMode.Specific:
                        EtgPickupResolveResult resolveResult = ResolveSpecificDefinition(definition, effectiveAliasRegistry);
                        if (resolveResult.Succeeded)
                        {
                            rules.Add(LoadoutRuleConfig.CreateSpecific(definition.Category, resolveResult.PickupId));
                        }
                        else if (resolveResult.Warning != null)
                        {
                            warnings.Add(resolveResult.Warning);
                        }

                        break;
                    default:
                        warnings.Add(new SelectionWarning(definition.Category, "UnsupportedGrantMode", "The raw loadout rule definition used an unsupported grant mode."));
                        break;
                }
            }

            return new LoadoutConfigResolutionResult(new LoadoutConfig(rules), warnings.ToArray());
        }

        private static void AddResolvedRandomPoolEntry(
            EtgPickupResolveResult resolveResult,
            List<LoadoutPoolEntryConfig> resolvedPoolEntries,
            HashSet<int> seenPoolIds,
            List<SelectionWarning> warnings)
        {
            AddResolvedRandomPoolEntry(resolveResult, resolvedPoolEntries, seenPoolIds, warnings, string.Empty);
        }

        private static void AddResolvedRandomPoolEntry(
            EtgPickupResolveResult resolveResult,
            List<LoadoutPoolEntryConfig> resolvedPoolEntries,
            HashSet<int> seenPoolIds,
            List<SelectionWarning> warnings,
            string warningPrefix)
        {
            if (resolveResult == null)
            {
                return;
            }

            if (resolveResult.Succeeded)
            {
                if (resolveResult.Category.HasValue && seenPoolIds.Add(resolveResult.PickupId))
                {
                    resolvedPoolEntries.Add(new LoadoutPoolEntryConfig(resolveResult.Category.Value, resolveResult.PickupId));
                }

                return;
            }

            if (resolveResult.Warning == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(warningPrefix))
            {
                warnings.Add(resolveResult.Warning);
                return;
            }

            warnings.Add(
                new SelectionWarning(
                    resolveResult.Warning.Category,
                    resolveResult.Warning.Code,
                    warningPrefix + resolveResult.Warning.Message));
        }

        private EtgPickupResolveResult ResolveSpecificDefinition(LoadoutRuleDefinition definition, PickupAliasRegistry aliasRegistry)
        {
            if (definition.SpecificPickupId.HasValue)
            {
                return _pickupResolver.Resolve(definition.Category, definition.SpecificPickupId.Value);
            }

            if (!string.IsNullOrEmpty(definition.SpecificAlias))
            {
                int resolvedAliasPickupId;
                if (!aliasRegistry.TryResolve(definition.SpecificAlias, out resolvedAliasPickupId))
                {
                    return new EtgPickupResolveResult(
                        false,
                        definition.Category,
                        0,
                        string.Empty,
                        new SelectionWarning(
                            definition.Category,
                            "SpecificAliasNotFound",
                            "No pickup alias matched '" + definition.SpecificAlias + "'."));
                }

                EtgPickupResolveResult aliasResolveResult = _pickupResolver.Resolve(definition.Category, resolvedAliasPickupId);
                if (!aliasResolveResult.Succeeded && aliasResolveResult.Warning != null)
                {
                    return new EtgPickupResolveResult(
                        false,
                        definition.Category,
                        0,
                        string.Empty,
                        new SelectionWarning(
                            definition.Category,
                            aliasResolveResult.Warning.Code,
                            "Alias '" + definition.SpecificAlias + "' resolved to pickup ID " + resolvedAliasPickupId + ", but " +
                            aliasResolveResult.Warning.Message));
                }

                return aliasResolveResult;
            }

            return _pickupResolver.Resolve(definition.Category, definition.SpecificName);
        }
    }
}
