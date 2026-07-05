using RandomLoadout.Core;
using UnityEngine;
using System;

namespace RandomLoadout
{
    internal sealed class EtgPickupGranter
    {
        private readonly PlayerActiveItemCapacityOverrideService _playerActiveItemCapacityOverrideService;
        private readonly System.Func<bool> _activeItemGrantVerboseLoggingEnabledProvider;

        public EtgPickupGranter(
            PlayerActiveItemCapacityOverrideService playerActiveItemCapacityOverrideService,
            System.Func<bool> activeItemGrantVerboseLoggingEnabledProvider)
        {
            _playerActiveItemCapacityOverrideService = playerActiveItemCapacityOverrideService;
            _activeItemGrantVerboseLoggingEnabledProvider = activeItemGrantVerboseLoggingEnabledProvider;
        }

        public EtgGrantOutcome Grant(PlayerController player, SelectedPickup selection)
        {
            PickupObject pickup = PickupObjectDatabase.GetById(selection.PickupId);
            if ((object)pickup == null)
            {
                return new EtgGrantOutcome(
                    selection.Category,
                    selection.PickupId,
                    "<missing>",
                    false,
                    "PickupObjectDatabase returned null.",
                    "resolve",
                    "PickupObjectDatabase.GetById returned null.");
            }

            string pickupLabel = GetPickupLabel(pickup);
            if (!MatchesCategory(selection.Category, pickup))
            {
                return new EtgGrantOutcome(
                    selection.Category,
                    selection.PickupId,
                    pickupLabel,
                    false,
                    "Selected pickup does not match the expected category.",
                    "category-check",
                    "Resolved object type did not match the requested category.");
            }

            string grantPath;
            string grantDetail;
            if (!GrantPickup(player, selection.Category, pickup, out grantPath, out grantDetail))
            {
                return new EtgGrantOutcome(
                    selection.Category,
                    selection.PickupId,
                    pickupLabel,
                    false,
                    "The ETG grant call returned false.",
                    grantPath,
                    grantDetail);
            }

            return new EtgGrantOutcome(
                selection.Category,
                selection.PickupId,
                pickupLabel,
                true,
                string.Empty,
                grantPath,
                grantDetail);
        }

        private static bool MatchesCategory(PickupCategory category, PickupObject pickup)
        {
            switch (category)
            {
                case PickupCategory.Gun:
                    return pickup is Gun;
                case PickupCategory.Passive:
                    return pickup is PassiveItem;
                case PickupCategory.Active:
                    return pickup is PlayerItem;
                default:
                    return false;
            }
        }

        private bool GrantPickup(PlayerController player, PickupCategory category, PickupObject pickup, out string grantPath, out string grantDetail)
        {
            grantPath = string.Empty;
            grantDetail = string.Empty;
            Component component = pickup as Component;
            bool ensuredActiveCapacity = false;
            int activeCapacityTarget = 0;
            int activeCapacityBefore = 0;
            int activeCapacityAfter = 0;
            int activeItemCountBefore = 0;
            bool primaryGrantResult = false;
            if (category == PickupCategory.Active)
            {
                activeCapacityBefore = GetActiveItemCapacity(player);
                activeItemCountBefore = GetActiveItemCount(player);
                activeCapacityTarget = GetRequiredActiveItemCapacity(player);
                if (activeCapacityTarget > 0 && _playerActiveItemCapacityOverrideService != null)
                {
                    _playerActiveItemCapacityOverrideService.EnsureCapacity(player, activeCapacityTarget);
                    ensuredActiveCapacity = true;
                }

                activeCapacityAfter = GetActiveItemCapacity(player);
            }

            if (component != null)
            {
                // Prefer the same prefab-to-player flow used by ModTheGungeonAPI's give command.
                // It is generally the most stable path across guns, passives, and actives because
                // the game handles the pickup as if it were granted from its real prefab.
                try
                {
                    primaryGrantResult = LootEngine.TryGivePrefabToPlayer(component.gameObject, player, false);
                    if (primaryGrantResult)
                    {
                        grantPath = "primary";
                        grantDetail = ensuredActiveCapacity
                            ? BuildActiveCapacityGrantDetail(activeCapacityBefore, activeCapacityAfter, activeCapacityTarget, activeItemCountBefore, primaryGrantResult)
                            : "Granted via LootEngine.TryGivePrefabToPlayer.";
                        return true;
                    }

                    if (category == PickupCategory.Active)
                    {
                        grantDetail = BuildActiveGrantRejectedDetail(activeCapacityBefore, activeCapacityAfter, activeCapacityTarget, activeItemCountBefore, primaryGrantResult);
                    }
                }
                catch (Exception exception)
                {
                    grantPath = "primary-exception";
                    grantDetail = "LootEngine.TryGivePrefabToPlayer threw " + exception.GetType().Name + ": " + exception.Message;
                    if (category == PickupCategory.Active)
                    {
                        string activeCapacityStateSuffix = BuildActiveCapacityStateSuffix(activeCapacityBefore, activeCapacityAfter, activeCapacityTarget, activeItemCountBefore, primaryGrantResult);
                        if (!string.IsNullOrEmpty(activeCapacityStateSuffix))
                        {
                            grantDetail += " " + activeCapacityStateSuffix;
                        }
                    }
                }
            }

            switch (category)
            {
                case PickupCategory.Gun:
                    player.inventory.AddGunToInventory((Gun)pickup, false);
                    grantPath = "fallback";
                    grantDetail = AppendFallbackDetail(grantDetail, "Used AddGunToInventory.");
                    return true;
                case PickupCategory.Passive:
                    player.AcquirePassiveItem((PassiveItem)pickup);
                    grantPath = "fallback";
                    grantDetail = AppendFallbackDetail(grantDetail, "Used AcquirePassiveItem.");
                    return true;
                case PickupCategory.Active:
                    if (component == null)
                    {
                        grantPath = "unsupported";
                        grantDetail = "Active item requires a component prefab for LootEngine.TryGivePrefabToPlayer.";
                        return false;
                    }

                    bool activeSlotsFull = AreActiveItemSlotsFull(player);
                    if (!string.IsNullOrEmpty(grantDetail))
                    {
                        grantPath = activeSlotsFull
                            ? "primary_rejected_etg_spew_slots_full"
                            : "primary_rejected_etg_spew";
                        return true;
                    }

                    if (TrySpawnActiveItemNearPlayer(player, component.gameObject, pickup.PickupObjectId, out grantDetail))
                    {
                        grantPath = activeSlotsFull ? "spawn_near_player_slots_full" : "spawn_near_player";
                        return true;
                    }

                    grantPath = activeSlotsFull ? "spawn_near_player_slots_full_failed" : "spawn_near_player_failed";
                    grantDetail = AppendFallbackDetail(grantDetail, DescribeActiveItemState(player, pickup.PickupObjectId));
                    return false;
                default:
                    grantPath = "unsupported";
                    grantDetail = "No grant implementation was available for the resolved category.";
                    return false;
            }
        }

        private static bool TrySpawnActiveItemNearPlayer(PlayerController player, GameObject prefab, int pickupId, out string grantDetail)
        {
            grantDetail = string.Empty;
            if ((object)player == null || prefab == null)
            {
                grantDetail = "Active item fallback requires both a player and prefab GameObject.";
                return false;
            }

            try
            {
                DebrisObject spawnedItem = LootEngine.SpawnItem(
                    prefab,
                    player.CenterPosition,
                    Vector2.zero,
                    0f,
                    false,
                    true,
                    false);
                if ((object)spawnedItem == null)
                {
                    grantDetail = "Active item fallback could not spawn the prefab near the player.";
                    return false;
                }

                grantDetail = "Primary prefab grant was rejected by ETG; spawned the active item near the player instead. TargetPickupId=" +
                    pickupId +
                    ".";
                return true;
            }
            catch (Exception exception)
            {
                grantDetail = "Active item fallback LootEngine.SpawnItem threw " + exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        private static string AppendFallbackDetail(string primaryDetail, string fallbackDetail)
        {
            if (string.IsNullOrEmpty(primaryDetail))
            {
                return "Primary prefab grant failed; " + fallbackDetail;
            }

            return primaryDetail + "; fallback: " + fallbackDetail;
        }

        private static string DescribeActiveItemState(PlayerController player, int pickupId)
        {
            if ((object)player == null)
            {
                return "Player was null while describing active-item state.";
            }

            string currentItemLabel = "<none>";
            PlayerItem currentItem = player.CurrentItem;
            if ((object)currentItem != null)
            {
                currentItemLabel = GetPickupLabel(currentItem) + " (" + currentItem.PickupObjectId + ")";
            }

            return "Active-state: CurrentItem=" + currentItemLabel +
                ", HasTarget=" + player.HasActiveItem(pickupId) +
                ".";
        }

        private static bool AreActiveItemSlotsFull(PlayerController player)
        {
            return (object)player != null &&
                player.activeItems != null &&
                player.activeItems.Count >= player.maxActiveItemsHeld;
        }

        private static int GetRequiredActiveItemCapacity(PlayerController player)
        {
            if ((object)player == null || player.activeItems == null)
            {
                return 0;
            }

            return player.activeItems.Count + 1;
        }

        private static int GetActiveItemCapacity(PlayerController player)
        {
            return (object)player != null ? player.maxActiveItemsHeld : 0;
        }

        private static int GetActiveItemCount(PlayerController player)
        {
            return (object)player != null && player.activeItems != null
                ? player.activeItems.Count
                : 0;
        }

        private static string DescribeActiveCapacityState(int capacityBefore, int capacityAfter, int targetCapacity, int activeItemCountBefore, bool primaryGrantResult)
        {
            return "ActiveCapacityBefore=" +
                capacityBefore +
                ", ActiveCapacityAfter=" +
                capacityAfter +
                ", ActiveCapacityTarget=" +
                targetCapacity +
                ", ActiveItemCountBefore=" +
                activeItemCountBefore +
                ", TryGivePrefabToPlayerResult=" +
                primaryGrantResult +
                ".";
        }

        private bool IsActiveItemGrantVerboseLoggingEnabled()
        {
            return _activeItemGrantVerboseLoggingEnabledProvider != null && _activeItemGrantVerboseLoggingEnabledProvider();
        }

        private string BuildActiveCapacityGrantDetail(int capacityBefore, int capacityAfter, int targetCapacity, int activeItemCountBefore, bool primaryGrantResult)
        {
            if (!IsActiveItemGrantVerboseLoggingEnabled())
            {
                return "Expanded active-item capacity before granting via LootEngine.TryGivePrefabToPlayer.";
            }

            return "Expanded active-item capacity before granting via LootEngine.TryGivePrefabToPlayer. " +
                DescribeActiveCapacityState(capacityBefore, capacityAfter, targetCapacity, activeItemCountBefore, primaryGrantResult);
        }

        private string BuildActiveGrantRejectedDetail(int capacityBefore, int capacityAfter, int targetCapacity, int activeItemCountBefore, bool primaryGrantResult)
        {
            if (!IsActiveItemGrantVerboseLoggingEnabled())
            {
                return "LootEngine.TryGivePrefabToPlayer returned false for the active item.";
            }

            return "LootEngine.TryGivePrefabToPlayer returned false. " +
                DescribeActiveCapacityState(capacityBefore, capacityAfter, targetCapacity, activeItemCountBefore, primaryGrantResult);
        }

        private string BuildActiveCapacityStateSuffix(int capacityBefore, int capacityAfter, int targetCapacity, int activeItemCountBefore, bool primaryGrantResult)
        {
            if (!IsActiveItemGrantVerboseLoggingEnabled())
            {
                return string.Empty;
            }

            return DescribeActiveCapacityState(capacityBefore, capacityAfter, targetCapacity, activeItemCountBefore, primaryGrantResult);
        }

        private static string GetPickupLabel(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return "<null>";
            }

            if (pickup.encounterTrackable != null &&
                pickup.encounterTrackable.journalData != null &&
                !string.IsNullOrEmpty(pickup.encounterTrackable.journalData.PrimaryDisplayName))
            {
                return pickup.encounterTrackable.journalData.PrimaryDisplayName;
            }

            return pickup.name;
        }
    }
}
