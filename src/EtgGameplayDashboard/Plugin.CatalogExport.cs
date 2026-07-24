// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    public sealed partial class Plugin
    {
        private void TryExportPickupCatalogOnce()
        {
            if (_hasExportedPickupCatalog || _pickupCatalogExporter == null)
            {
                return;
            }

            EtgPickupCatalogExportResult exportResult = _pickupCatalogExporter.Export(_pickupResolver);
            if (exportResult.Succeeded)
            {
                _hasExportedPickupCatalog = true;
                _lastPickupCatalogExportFailure = null;
                // Export can happen before ETG finishes preparing localized string
                // tables. Do not let that startup snapshot become the UI cache.
                _pickupResolver.InvalidateGrantablePickupCatalogCache();
                Logger.LogInfo(
                    EtgGameplayDashboardLog.Init(
                        "Exported grantable pickup catalog to '" + exportResult.TextOutputPath + "', '" + exportResult.JsonOutputPath + "', '" + exportResult.GroupedJsonOutputPath + "', '" + exportResult.NamesJsonOutputPath + "', and '" + exportResult.RulePoolOutputPath + "' (" + exportResult.EntryCount + " entries)."));
                return;
            }

            if (!string.Equals(_lastPickupCatalogExportFailure, exportResult.FailureReason, System.StringComparison.Ordinal))
            {
                _lastPickupCatalogExportFailure = exportResult.FailureReason;
                Logger.LogWarning(EtgGameplayDashboardLog.Init("Failed to export grantable pickup catalog: " + exportResult.FailureReason));
            }
        }
    }
}
