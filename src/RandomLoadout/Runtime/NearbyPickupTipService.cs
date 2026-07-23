// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using Dungeonator;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed class NearbyPickupTipService
    {
        // This service intentionally stays event-driven. An earlier room-wide polling
        // implementation scanned every 0.15s and caused visible hitches in non-combat
        // rooms, especially around loot-heavy or shop-heavy scenes.
        private readonly PickupGameplayRegistry _gameplayRegistry;
        private readonly ManualLogSource _logger;
        private readonly System.Func<bool> _verboseLoggingEnabledProvider;
        private readonly System.Func<bool> _overlayEnabledProvider;

        private Object _currentRangeSource;

        public NearbyPickupTipService(
            PickupGameplayRegistry gameplayRegistry,
            ManualLogSource logger,
            System.Func<bool> verboseLoggingEnabledProvider,
            System.Func<bool> overlayEnabledProvider)
        {
            _gameplayRegistry = gameplayRegistry ?? PickupGameplayRegistry.Empty;
            _logger = logger;
            _verboseLoggingEnabledProvider = verboseLoggingEnabledProvider;
            _overlayEnabledProvider = overlayEnabledProvider;
        }

        public bool HasVisibleTip { get; private set; }

        public int CurrentPickupId { get; private set; }

        public string CurrentDisplayName { get; private set; }

        public void Update(PlayerController player, bool isOverlayEnabled)
        {
            ClearDestroyedVisibleSourceIfNeeded();

            if (!isOverlayEnabled)
            {
                ClearTip(null, "overlay_disabled");
                return;
            }

            if (player == null || player.CurrentRoom == null)
            {
                ClearTip(null, "player_or_room_unavailable");
                return;
            }

            if (player.CurrentRoom.HasActiveEnemies(RoomHandler.ActiveEnemyType.All))
            {
                ClearTip(null, "active_combat");
            }
        }

        public void HandlePickupEnteredRange(PickupObject pickup, PlayerController player)
        {
            LogInfo("Nearby pickup entered-range callback. " + DescribePickup(pickup) + ".");
            if (!CanShowTip(player) || pickup == null)
            {
                return;
            }

            ShowTip(pickup, pickup, "pickup");
        }

        public void HandlePickupExitedRange(PickupObject pickup)
        {
            LogInfo("Nearby pickup exited-range callback. " + DescribePickup(pickup) + ".");
            ClearTip(pickup, "pickup_exit_range");
        }

        public void HandlePickupConsumed(PickupObject pickup)
        {
            LogInfo("Nearby pickup consumed callback. " + DescribePickup(pickup) + ".");
            ClearTip(pickup, "pickup_consumed");
        }

        public void HandleShopItemEnteredRange(ShopItemController shopItem, PlayerController player)
        {
            if (!CanShowTip(player) || shopItem == null || shopItem.item == null)
            {
                return;
            }

            ShowTip(shopItem, shopItem.item, "shop_item");
        }

        public void HandleShopItemExitedRange(ShopItemController shopItem)
        {
            ClearTip(shopItem, "shop_item_exit_range");
        }

        public void HandleShopItemInteracted(ShopItemController shopItem)
        {
            ClearTip(shopItem, "shop_item_interacted");
        }

        public void HandleRewardPedestalEnteredRange(RewardPedestal rewardPedestal, PlayerController player)
        {
            if (!CanShowTip(player) || rewardPedestal == null || rewardPedestal.contents == null)
            {
                return;
            }

            ShowTip(rewardPedestal, rewardPedestal.contents, "reward_pedestal");
        }

        public void HandleRewardPedestalExitedRange(RewardPedestal rewardPedestal)
        {
            ClearTip(rewardPedestal, "reward_pedestal_exit_range");
        }

        public void HandleRewardPedestalInteracted(RewardPedestal rewardPedestal)
        {
            ClearTip(rewardPedestal, "reward_pedestal_interacted");
        }

        private bool CanShowTip(PlayerController player)
        {
            if (!IsOverlayEnabled())
            {
                return false;
            }

            if (player == null || player.CurrentRoom == null)
            {
                return false;
            }

            if (player.CurrentRoom.HasActiveEnemies(RoomHandler.ActiveEnemyType.All))
            {
                return false;
            }

            return _gameplayRegistry.Count > 0;
        }

        private void ShowTip(Object rangeSource, PickupObject pickup, string source)
        {
            if (rangeSource == null || pickup == null)
            {
                return;
            }

            _currentRangeSource = rangeSource;
            HasVisibleTip = true;
            CurrentPickupId = pickup.PickupObjectId;
            CurrentDisplayName = GetPickupLabelForGameLanguage(pickup);
            PickupGameplayEntry gameplayEntry;
            bool hasGameplayEntry = _gameplayRegistry.TryGetEntry(CurrentPickupId, out gameplayEntry);
            LogInfo(
                "Nearby pickup tip shown. " +
                "Source=" + source +
                ", PickupId=" + CurrentPickupId +
                ", Label=" + Quote(CurrentDisplayName) +
                ", HasGameplayEntry=" + hasGameplayEntry +
                ".");

            if (!hasGameplayEntry && IsVerboseLoggingEnabled() && _logger != null)
            {
                _logger.LogWarning(
                    RandomLoadoutLog.Run(
                        "Nearby pickup entered range but gameplay entry was not found. " +
                        "Source=" + source +
                        ", PickupId=" + CurrentPickupId +
                        ", Label=" + Quote(CurrentDisplayName) +
                        ", RegistryCount=" + _gameplayRegistry.Count +
                        "."));
            }
        }

        private void ClearTip(Object rangeSource, string reason)
        {
            // Multiple interactables can overlap in range. Only the currently active
            // source may clear the visible tip; otherwise an unrelated exit event can
            // hide a newer tip that the player is still standing next to.
            if (rangeSource != null && _currentRangeSource != null && !ReferenceEquals(rangeSource, _currentRangeSource))
            {
                return;
            }

            if (HasVisibleTip || _currentRangeSource != null)
            {
                LogInfo("Nearby pickup tip cleared. Reason=" + reason + ".");
            }

            HasVisibleTip = false;
            CurrentPickupId = 0;
            CurrentDisplayName = string.Empty;
            _currentRangeSource = null;
        }

        private void ClearDestroyedVisibleSourceIfNeeded()
        {
            if (!HasVisibleTip || (object)_currentRangeSource == null || _currentRangeSource != null)
            {
                return;
            }

            // Some pickup types, including the Blank's SilencerItem, override Pickup instead
            // of running PlayerItem.Pickup. That bypasses the base-method Harmony hook that
            // normally calls HandlePickupConsumed, leaving the overlay pointed at a destroyed
            // Unity object. Clear it here so custom pickup implementations cannot leave stale
            // nearby-item information on screen.
            LogInfo(
                "Nearby pickup range source was destroyed without a matching pickup-consumed or exit-range callback. " +
                "Clearing the visible tip. CurrentPickupId=" +
                CurrentPickupId +
                ", Label=" +
                Quote(CurrentDisplayName) +
                ".");

            ClearTip(null, "range_source_destroyed");
        }

        private static string GetPickupLabelForGameLanguage(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return string.Empty;
            }

            if (pickup.encounterTrackable != null)
            {
                string modifiedDisplayName = pickup.encounterTrackable.GetModifiedDisplayName();
                if (!string.IsNullOrEmpty(modifiedDisplayName))
                {
                    string resolvedModifiedDisplayName = ResolveLocalizedLabelForGameLanguage(modifiedDisplayName);
                    if (!string.IsNullOrEmpty(resolvedModifiedDisplayName))
                    {
                        return resolvedModifiedDisplayName;
                    }
                }

                if (pickup.encounterTrackable.journalData != null &&
                    !string.IsNullOrEmpty(pickup.encounterTrackable.journalData.PrimaryDisplayName))
                {
                    string resolvedPrimaryDisplayName = ResolveLocalizedLabelForGameLanguage(pickup.encounterTrackable.journalData.PrimaryDisplayName);
                    if (!string.IsNullOrEmpty(resolvedPrimaryDisplayName))
                    {
                        return resolvedPrimaryDisplayName;
                    }
                }
            }

            if (!string.IsNullOrEmpty(pickup.DisplayName))
            {
                return ResolveLocalizedLabelForGameLanguage(pickup.DisplayName);
            }

            return ResolveLocalizedLabelForGameLanguage(pickup.name);
        }

        private static string ResolveLocalizedLabelForGameLanguage(string rawLabel)
        {
            if (string.IsNullOrEmpty(rawLabel))
            {
                return string.Empty;
            }

            if (string.Equals(GuiText.GameLanguageCode, "en", System.StringComparison.OrdinalIgnoreCase))
            {
                return ResolveLocalizedLabelFromEnglishBackupTables(rawLabel);
            }

            return ResolveLocalizedLabel(rawLabel);
        }

        private static string ResolveLocalizedLabel(string rawLabel)
        {
            if (string.IsNullOrEmpty(rawLabel))
            {
                return string.Empty;
            }

            if (!rawLabel.StartsWith("#", System.StringComparison.Ordinal))
            {
                return rawLabel;
            }

            string localized = StringTableManager.GetString(rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, System.StringComparison.Ordinal))
            {
                return localized;
            }

            localized = StringTableManager.GetItemsString(rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, System.StringComparison.Ordinal))
            {
                return localized;
            }

            return rawLabel;
        }

        private static string ResolveLocalizedLabelFromEnglishBackupTables(string rawLabel)
        {
            if (string.IsNullOrEmpty(rawLabel) || !rawLabel.StartsWith("#", System.StringComparison.Ordinal))
            {
                return rawLabel ?? string.Empty;
            }

            string localized = ResolveLocalizedLabelFromBackupTable("m_backupCoreTable", rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, System.StringComparison.Ordinal))
            {
                return localized;
            }

            localized = ResolveLocalizedLabelFromBackupTable("m_backupItemsTable", rawLabel);
            if (!string.IsNullOrEmpty(localized) && !string.Equals(localized, rawLabel, System.StringComparison.Ordinal))
            {
                return localized;
            }

            return rawLabel;
        }

        private static string ResolveLocalizedLabelFromBackupTable(string tableFieldName, string rawLabel)
        {
            System.Collections.IDictionary table = GetStaticMemberValue(typeof(StringTableManager), tableFieldName) as System.Collections.IDictionary;
            if (table == null || !table.Contains(rawLabel))
            {
                return string.Empty;
            }

            object entry = table[rawLabel];
            if (entry == null)
            {
                return string.Empty;
            }

            System.Reflection.MethodInfo getExactString = entry.GetType().GetMethod(
                "GetExactString",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (getExactString == null)
            {
                return string.Empty;
            }

            object resolved = getExactString.Invoke(entry, new object[] { 0 });
            return resolved as string ?? string.Empty;
        }

        private static object GetStaticMemberValue(System.Type type, string memberName)
        {
            System.Reflection.PropertyInfo property = type.GetProperty(
                memberName,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(null, null);
            }

            System.Reflection.FieldInfo field = type.GetField(
                memberName,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(null);
            }

            return null;
        }

        private bool IsOverlayEnabled()
        {
            return _overlayEnabledProvider != null && _overlayEnabledProvider();
        }

        private bool IsVerboseLoggingEnabled()
        {
            return _verboseLoggingEnabledProvider != null && _verboseLoggingEnabledProvider();
        }

        private void LogInfo(string message)
        {
            if (!IsVerboseLoggingEnabled() || _logger == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            _logger.LogInfo(RandomLoadoutLog.Run(message));
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty) + "\"";
        }

        private static string DescribePickup(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return "Pickup=<null>";
            }

            return "PickupType=" + pickup.GetType().Name + ", PickupId=" + pickup.PickupObjectId + ", InstanceId=" + pickup.GetInstanceID();
        }
    }
}
