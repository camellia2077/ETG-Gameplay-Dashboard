// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using EtgGameplayDashboard.Core;
using EtgGameplayDashboard.Core.Input;
using UnityEngine;

namespace EtgGameplayDashboard
{
    public sealed partial class Plugin
    {
        private void EnsureResolvedLoadoutConfig()
        {
            if (_hasResolvedLoadoutConfig)
            {
                return;
            }

            EnsureAliasRegistryLoaded();
            if (_ruleFileProvider != null)
            {
                _ruleFileProvider.ActivePresetName = GetActiveStartItemsPreset();
            }

            LoadoutRuleFileLoadResult ruleFileLoadResult = _ruleFileProvider.Load();
            _ruleDefinitions = ruleFileLoadResult.Definitions;
            _activePresetPickups = ruleFileLoadResult.ActivePresetPickups ?? new LoadoutRuleFilePickupModel[0];
            Logger.LogInfo(
                EtgGameplayDashboardLog.Init(
                    "Loaded start-loadout rules. File=" +
                    _ruleFileProvider.FilePath +
                    ", DefinitionCount=" +
                    (_ruleDefinitions != null ? _ruleDefinitions.Length : 0) +
                    ", PresetPickupCount=" +
                    _activePresetPickups.Length +
                    "."));

            for (int i = 0; i < ruleFileLoadResult.Messages.Length; i++)
            {
                Logger.LogInfo(EtgGameplayDashboardLog.Init(ruleFileLoadResult.Messages[i]));
            }

            for (int i = 0; i < ruleFileLoadResult.Warnings.Length; i++)
            {
                Logger.LogWarning(EtgGameplayDashboardLog.Init(ruleFileLoadResult.Warnings[i]));
            }

            LoadoutConfigResolutionResult resolutionResult = EtgLoadoutConfigResolver.Resolve(_ruleDefinitions, _aliasRegistry);
            _resolvedLoadoutConfig = resolutionResult.Config;
            _hasResolvedLoadoutConfig = true;
            int resolvedRuleCount = _resolvedLoadoutConfig != null && _resolvedLoadoutConfig.Rules != null
                ? _resolvedLoadoutConfig.Rules.Length
                : 0;
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Resolved start-loadout config. ResolvedRuleCount=" + resolvedRuleCount + "."));

            LogSelectionWarnings(resolutionResult.Warnings);
        }

        private void InvalidateResolvedLoadoutConfig()
        {
            _hasResolvedLoadoutConfig = false;
            _resolvedLoadoutConfig = null;
            _ruleDefinitions = new LoadoutRuleDefinition[0];
            _activePresetPickups = new LoadoutRuleFilePickupModel[0];
            Logger.LogInfo(EtgGameplayDashboardLog.Init("Invalidated cached start-loadout config. The next automatic grant will reload rules from disk."));
        }

        private void LogSelectionWarnings(SelectionWarning[] warnings)
        {
            for (int i = 0; i < warnings.Length; i++)
            {
                SelectionWarning warning = warnings[i];
                string categoryPrefix = warning.Category.HasValue ? warning.Category.Value + ": " : string.Empty;
                string message = EtgGameplayDashboardLog.Grant(categoryPrefix + warning.Message + " [Code=" + warning.Code + "]");
                if (string.Equals(warning.Code, "ConfigEmpty", StringComparison.Ordinal))
                {
                    Logger.LogInfo(message);
                }
                else
                {
                    Logger.LogWarning(message);
                }
            }
        }

        private PickupAliasRegistry GetAliasRegistry()
        {
            if (!_hasLoadedAliasRegistry)
            {
                EnsureAliasRegistryLoaded();
            }

            return _aliasRegistry ?? PickupAliasRegistry.Empty;
        }

        private string GetUiLanguage()
        {
            return _configuration.GetUiLanguage();
        }

        private void SetUiLanguage(string languageCode)
        {
            _configuration.SetUiLanguage(languageCode);
        }

        private void LogCommandInput(string message)
        {
            _configuration.LogCommandInput(message);
        }

        private KeyCode GetCommandPanelKey()
        {
            return _configuration.GetCommandPanelKey();
        }

        private string GetCommandPanelKeyName()
        {
            return _configuration.GetCommandPanelKeyName();
        }

        private void SetCommandPanelKey(string keyName)
        {
            _configuration.SetCommandPanelKey(keyName);
        }

        private string GetCommandPanelControllerShortcut()
        {
            return _configuration.GetCommandPanelControllerShortcut();
        }

        private void SetCommandPanelControllerShortcut(string shortcut)
        {
            _configuration.SetCommandPanelControllerShortcut(shortcut);
        }

        private bool IsCommandPanelControllerShortcutEnabled()
        {
            return _configuration.IsCommandPanelControllerShortcutEnabled();
        }

        private void SetCommandPanelControllerShortcutEnabled(bool isEnabled)
        {
            _configuration.SetCommandPanelControllerShortcutEnabled(isEnabled);
        }

        private static string NormalizeCommandPanelControllerShortcut(string shortcut)
        {
            return PluginConfigurationFacade.NormalizeCommandPanelControllerShortcut(shortcut);
        }

        private string GetUiScalePreset()
        {
            return _configuration.GetUiScalePreset();
        }

        private void SetUiScalePreset(string presetName)
        {
            _configuration.SetUiScalePreset(presetName);
        }

        private string GetThemePreset()
        {
            return _configuration.GetThemePreset();
        }

        private void SetThemePreset(string themeId)
        {
            _configuration.SetThemePreset(themeId);
        }

        private bool IsExperimentalModeEnabled()
        {
            return _configuration.IsExperimentalModeEnabled();
        }

        private bool IsPlayerStatsPanelShown()
        {
            return _configuration.IsPlayerStatsPanelShown();
        }

        private KeyboardShortcutRegistry GetKeyboardShortcutRegistry()
        {
            return _configuration.GetKeyboardShortcutRegistry();
        }

        private void SetKeyboardShortcuts(string serialized)
        {
            _configuration.SetKeyboardShortcuts(serialized);
        }

        private bool IsCommandPanelCloseButtonShown()
        {
            return _configuration.IsCommandPanelCloseButtonShown();
        }

        private bool IsPlayerRewindEnabled()
        {
            return _configuration.IsPlayerRewindEnabled();
        }

        private bool IsRoomEnemyRefreshRecordingEnabled()
        {
            return _configuration.IsRoomEnemyRefreshRecordingEnabled();
        }

        private void SetRoomEnemyRefreshRecordingEnabled(bool isEnabled)
        {
            _configuration.SetRoomEnemyRefreshRecordingEnabled(isEnabled);
        }

        private string GetRoomEnemyRefreshMethod()
        {
            return _configuration.GetRoomEnemyRefreshMethod();
        }

        private void SetRoomEnemyRefreshMethod(string method)
        {
            _configuration.SetRoomEnemyRefreshMethod(method);
        }

        private static string NormalizeRoomEnemyRefreshMethod(string method)
        {
            return PluginConfigurationFacade.NormalizeRoomEnemyRefreshMethod(method);
        }

        private void SetPlayerRewindEnabled(bool isEnabled)
        {
            _configuration.SetPlayerRewindEnabled(isEnabled);
        }

        private bool IsRoomRewindCleanupEnabled()
        {
            return _configuration.IsRoomRewindCleanupEnabled();
        }

        private void SetRoomRewindCleanupEnabled(bool isEnabled)
        {
            _configuration.SetRoomRewindCleanupEnabled(isEnabled);
        }

        private bool IsStartItemsPresetIconsEnabled()
        {
            return _configuration.IsStartItemsPresetIconsEnabled();
        }

        private bool IsPickupInfoOverlayEnabled()
        {
            return _configuration.IsPickupInfoOverlayEnabled();
        }

        private bool IsPickupInfoQualityEnabled()
        {
            return _configuration.IsPickupInfoQualityEnabled();
        }

        private bool IsPickupInfoTypeEnabled()
        {
            return _configuration.IsPickupInfoTypeEnabled();
        }

        private bool IsPickupInfoEffectsEnabled()
        {
            return _configuration.IsPickupInfoEffectsEnabled();
        }

        private bool IsPickupInfoSynergiesEnabled()
        {
            return _configuration.IsPickupInfoSynergiesEnabled();
        }

        private bool IsPickupInfoSummaryEnabled()
        {
            return _configuration.IsPickupInfoSummaryEnabled();
        }

        private bool IsPickupInfoNotesEnabled()
        {
            return _configuration.IsPickupInfoNotesEnabled();
        }

        private void SetPlayerStatsPanelShown(bool isEnabled)
        {
            _configuration.SetPlayerStatsPanelShown(isEnabled);
        }

        private void SetCommandPanelCloseButtonShown(bool isEnabled)
        {
            _configuration.SetCommandPanelCloseButtonShown(isEnabled);
        }

        private bool IsRevealMapEveryFloor()
        {
            return _configuration.IsRevealMapEveryFloor();
        }

        private void SetRevealMapEveryFloor(bool isEnabled)
        {
            _configuration.SetRevealMapEveryFloor(isEnabled);
        }

        private void SetStartItemsPresetIconsEnabled(bool isEnabled)
        {
            _configuration.SetStartItemsPresetIconsEnabled(isEnabled);
        }

        private void SetPickupInfoOverlayEnabled(bool isEnabled)
        {
            _configuration.SetPickupInfoOverlayEnabled(isEnabled);
        }

        private void SetPickupInfoQualityEnabled(bool isEnabled)
        {
            _configuration.SetPickupInfoQualityEnabled(isEnabled);
        }

        private void SetPickupInfoTypeEnabled(bool isEnabled)
        {
            _configuration.SetPickupInfoTypeEnabled(isEnabled);
        }

        private void SetPickupInfoEffectsEnabled(bool isEnabled)
        {
            _configuration.SetPickupInfoEffectsEnabled(isEnabled);
        }

        private void SetPickupInfoSynergiesEnabled(bool isEnabled)
        {
            _configuration.SetPickupInfoSynergiesEnabled(isEnabled);
        }

        private void SetPickupInfoSummaryEnabled(bool isEnabled)
        {
            _configuration.SetPickupInfoSummaryEnabled(isEnabled);
        }

        private void SetPickupInfoNotesEnabled(bool isEnabled)
        {
            _configuration.SetPickupInfoNotesEnabled(isEnabled);
        }

        private void SetExperimentalModeEnabled(bool isEnabled)
        {
            _configuration.SetExperimentalModeEnabled(isEnabled);
        }

        private bool IsAmmonomiconFastOpenEnabled()
        {
            return _configuration.IsAmmonomiconFastOpenEnabled();
        }

        private void SetAmmonomiconFastOpenEnabled(bool isEnabled)
        {
            _configuration.SetAmmonomiconFastOpenEnabled(isEnabled);
        }

        private bool IsMapTeleportVerboseLoggingEnabled()
        {
            return _configuration.IsMapTeleportVerboseLoggingEnabled();
        }

        private bool IsMuncherVerboseLoggingEnabled()
        {
            return _configuration.IsMuncherVerboseLoggingEnabled();
        }

        private bool IsRoomEnemyReplayVerboseLoggingEnabled()
        {
            return _configuration.IsRoomEnemyReplayVerboseLoggingEnabled();
        }

        private bool IsBossIntroSkipVerboseLoggingEnabled()
        {
            return _configuration.IsBossIntroSkipVerboseLoggingEnabled();
        }

        private bool IsFloorTeleportVerboseLoggingEnabled()
        {
            return _configuration.IsFloorTeleportVerboseLoggingEnabled();
        }

        private bool IsBossRushVerboseLoggingEnabled()
        {
            return _configuration.IsBossRushVerboseLoggingEnabled();
        }

        private bool IsBossSelectionVerboseLoggingEnabled()
        {
            return PluginConfigurationFacade.IsBossSelectionVerboseLoggingEnabled();
        }

        private bool IsCommandPanelHealthVerboseLoggingEnabled()
        {
            return _configuration.IsCommandPanelHealthVerboseLoggingEnabled();
        }

        private bool IsCommandPanelCursorVerboseLoggingEnabled()
        {
            return _configuration.IsCommandPanelCursorVerboseLoggingEnabled();
        }

        private bool IsCommandPanelGameplayInputVerboseLoggingEnabled()
        {
            return _configuration.IsCommandPanelGameplayInputVerboseLoggingEnabled();
        }

        private bool IsCommandPanelControllerGameplayInputVerboseLoggingEnabled()
        {
            return _configuration.IsCommandPanelControllerGameplayInputVerboseLoggingEnabled();
        }

        private bool IsCommandPanelShortcutVerboseLoggingEnabled()
        {
            return _configuration.IsCommandPanelShortcutVerboseLoggingEnabled();
        }

        private bool IsCommandPanelCursorRenderVerboseLoggingEnabled()
        {
            return _configuration.IsCommandPanelCursorRenderVerboseLoggingEnabled();
        }

        private bool IsControllerAimVerboseLoggingEnabled()
        {
            return _configuration.IsControllerAimVerboseLoggingEnabled();
        }

        private bool IsCommandPanelCursorRenderProbeEnabled()
        {
            return _configuration.IsCommandPanelCursorRenderProbeEnabled();
        }

        private bool IsCommandPanelCursorAbovePanelEnabled()
        {
            return _configuration.IsCommandPanelCursorAbovePanelEnabled();
        }

        private bool IsActiveItemGrantVerboseLoggingEnabled()
        {
            return _configuration.IsActiveItemGrantVerboseLoggingEnabled();
        }

        private bool IsNearbyPickupVerboseLoggingEnabled()
        {
            return _configuration.IsNearbyPickupVerboseLoggingEnabled();
        }

        private bool IsStartupWindowFocusVerboseLoggingEnabled()
        {
            return _configuration.IsStartupWindowFocusVerboseLoggingEnabled();
        }

        private bool IsPerformanceVerboseLoggingEnabled()
        {
            return _configuration.IsPerformanceVerboseLoggingEnabled();
        }

        private bool IsCharacterSwitchVerboseLoggingEnabled()
        {
            return _configuration.IsCharacterSwitchVerboseLoggingEnabled();
        }

        private bool IsDamageDiagnosticsVerboseLoggingEnabled()
        {
            return _configuration.IsDamageDiagnosticsVerboseLoggingEnabled();
        }

        private string NormalizeUiScalePreset(string presetName)
        {
            return _configuration.NormalizeUiScalePreset(presetName);
        }

        private string NormalizeCommandPanelKeyName(string keyName)
        {
            return _configuration.NormalizeCommandPanelKeyName(keyName);
        }

        private static KeyCode ParseCommandPanelKey(string keyName)
        {
            return PluginConfigurationFacade.ParseCommandPanelKey(keyName);
        }

        private string GetActiveStartItemsPreset()
        {
            return _configuration.GetActiveStartItemsPreset();
        }

        private void SetActiveStartItemsPreset(string presetName)
        {
            _configuration.SetActiveStartItemsPreset(presetName);
        }

        private string GetCombatCursorColor()
        {
            return _configuration.GetCombatCursorColor();
        }

        private void PersistEnemyHealthBarsEnabled(bool enabled)
        {
            _configuration.PersistEnemyHealthBarsEnabled(enabled);
        }

        private void PersistBossIntroSkipEnabled(bool enabled)
        {
            _configuration.PersistBossIntroSkipEnabled(enabled);
        }

        private void PersistSkipChargeEnabled(bool enabled)
        {
            _configuration.PersistSkipChargeEnabled(enabled);
        }

        private void PersistInvincibilityEnabled(bool enabled)
        {
            _configuration.PersistInvincibilityEnabled(enabled);
        }

        private void PersistFlightEnabled(bool enabled)
        {
            _configuration.PersistFlightEnabled(enabled);
        }

        private void PersistControllerAimLockEnabled(bool enabled)
        {
            _configuration.PersistControllerAimLockEnabled(enabled);
        }

        private void PersistKeyboardAimAssistSettings(KeyboardAimAssistSettings settings)
        {
            _configuration.PersistKeyboardAimAssistSettings(settings);
        }

        private void PersistRapidFireEnabled(bool enabled)
        {
            _configuration.PersistRapidFireEnabled(enabled);
        }

        private void PersistAutoReloadMode(AutoReloadMode mode)
        {
            _configuration.PersistAutoReloadMode(mode);
        }

        private void PersistAmmoMode(AmmoMode mode)
        {
            _configuration.PersistAmmoMode(mode);
        }

        private void PersistActiveItemNoCooldownEnabled(bool enabled)
        {
            _configuration.PersistActiveItemNoCooldownEnabled(enabled);
        }

        private static AutoReloadMode ParseAutoReloadMode(string value)
        {
            return PluginConfigurationFacade.ParseAutoReloadMode(value);
        }

        private static AmmoMode ParseAmmoMode(string value)
        {
            return PluginConfigurationFacade.ParseAmmoMode(value);
        }

        private Color GetCombatCursorColorValue()
        {
            return _configuration.GetCombatCursorColorValue();
        }

        private void SetCombatCursorColor(string colorId)
        {
            _configuration.SetCombatCursorColor(colorId);
        }


        private void EnsureAliasRegistryLoaded()
        {
            if (_hasLoadedAliasRegistry || _aliasFileProvider == null)
            {
                return;
            }

            if ((object)GameManager.Instance == null)
            {
                return;
            }

            AliasLoadResult aliasLoadResult = _aliasFileProvider.Load(IsSupportedGrantablePickupId);
            _aliasRegistry = aliasLoadResult.Registry ?? PickupAliasRegistry.Empty;
            _hasLoadedAliasRegistry = true;

            for (int i = 0; i < aliasLoadResult.Messages.Length; i++)
            {
                Logger.LogInfo(EtgGameplayDashboardLog.Alias(aliasLoadResult.Messages[i]));
            }

            for (int i = 0; i < aliasLoadResult.Warnings.Length; i++)
            {
                Logger.LogWarning(EtgGameplayDashboardLog.Alias(aliasLoadResult.Warnings[i]));
            }
        }

        private bool IsSupportedGrantablePickupId(int pickupId)
        {
            return EtgPickupResolver.ResolveAny(pickupId).Succeeded;
        }
    }
}
