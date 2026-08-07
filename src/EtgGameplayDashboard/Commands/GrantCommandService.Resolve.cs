// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using EtgGameplayDashboard.Core;

namespace EtgGameplayDashboard
{
    internal sealed partial class GrantCommandService
    {
        private static readonly char[] LeadingPickupSeparators = { ' ', '\t' };

        private EtgPickupResolveResult ResolvePickup(GrantCommandRequest request)
        {
            int pickupId;
            if (TryParseLeadingPickupId(request.PickupName, out pickupId))
            {
                return ResolvePickupById(request.Target, pickupId);
            }

            PickupAliasRegistry aliasRegistry = GetAliasRegistry();
            if (aliasRegistry.TryResolve(request.PickupName, out pickupId))
            {
                return ResolvePickupById(request.Target, pickupId);
            }

            EtgPickupResolveResult resolveResult;
            switch (request.Target)
            {
                case GrantCommandTarget.Gun:
                    resolveResult = EtgPickupResolver.Resolve(PickupCategory.Gun, request.PickupName);
                    break;
                case GrantCommandTarget.Passive:
                    resolveResult = EtgPickupResolver.Resolve(PickupCategory.Passive, request.PickupName);
                    break;
                case GrantCommandTarget.Active:
                    resolveResult = EtgPickupResolver.Resolve(PickupCategory.Active, request.PickupName);
                    break;
                case GrantCommandTarget.Any:
                    resolveResult = EtgPickupResolver.ResolveAny(request.PickupName);
                    break;
                default:
                    return new EtgPickupResolveResult(false, null, 0, string.Empty, new SelectionWarning(null, "CommandTargetUnsupported", GuiText.GetEnglish("result.error.command_target_unsupported")));
            }

            return resolveResult;
        }

        private static EtgPickupResolveResult ResolvePickupById(GrantCommandTarget target, int pickupId)
        {
            switch (target)
            {
                case GrantCommandTarget.Gun:
                    return EtgPickupResolver.Resolve(PickupCategory.Gun, pickupId);
                case GrantCommandTarget.Passive:
                    return EtgPickupResolver.Resolve(PickupCategory.Passive, pickupId);
                case GrantCommandTarget.Active:
                    return EtgPickupResolver.Resolve(PickupCategory.Active, pickupId);
                case GrantCommandTarget.Any:
                    return EtgPickupResolver.ResolveAny(pickupId);
                default:
                    return new EtgPickupResolveResult(false, null, 0, string.Empty, new SelectionWarning(null, "CommandTargetUnsupported", GuiText.GetEnglish("result.error.command_target_unsupported")));
            }
        }

        private static bool TryParseLeadingPickupId(string rawValue, out int pickupId)
        {
            pickupId = 0;
            if (string.IsNullOrEmpty(rawValue))
            {
                return false;
            }

            string trimmed = rawValue.Trim();
            if (trimmed.Length == 0)
            {
                return false;
            }

            string[] parts = trimmed.Split(LeadingPickupSeparators, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return false;
            }

            return int.TryParse(parts[0], out pickupId);
        }

        private PickupAliasRegistry GetAliasRegistry()
        {
            PickupAliasRegistry aliasRegistry = _aliasRegistryProvider != null ? _aliasRegistryProvider() : PickupAliasRegistry.Empty;
            return aliasRegistry ?? PickupAliasRegistry.Empty;
        }
    }
}
