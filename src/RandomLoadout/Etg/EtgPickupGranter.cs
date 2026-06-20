using RandomLoadout.Core;
using UnityEngine;
using System;

namespace RandomLoadout
{
    internal sealed class EtgPickupGranter
    {
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

        private static bool GrantPickup(PlayerController player, PickupCategory category, PickupObject pickup, out string grantPath, out string grantDetail)
        {
            grantPath = string.Empty;
            grantDetail = string.Empty;
            Component component = pickup as Component;
            if (component != null)
            {
                // Prefer the same prefab-to-player flow used by ModTheGungeonAPI's give command.
                // It is generally the most stable path across guns, passives, and actives because
                // the game handles the pickup as if it were granted from its real prefab.
                try
                {
                    if (LootEngine.TryGivePrefabToPlayer(component.gameObject, player, false))
                    {
                        grantPath = "primary";
                        grantDetail = "Granted via LootEngine.TryGivePrefabToPlayer.";
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    grantPath = "primary-exception";
                    grantDetail = "LootEngine.TryGivePrefabToPlayer threw " + exception.GetType().Name + ": " + exception.Message;
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

                    try
                    {
                        grantPath = "primary";
                        grantDetail = "Active item requires LootEngine.TryGivePrefabToPlayer.";
                        return LootEngine.TryGivePrefabToPlayer(component.gameObject, player, false);
                    }
                    catch (Exception exception)
                    {
                        grantPath = "primary-exception";
                        grantDetail = "Active item LootEngine.TryGivePrefabToPlayer threw " + exception.GetType().Name + ": " + exception.Message;
                        return false;
                    }
                default:
                    grantPath = "unsupported";
                    grantDetail = "No grant implementation was available for the resolved category.";
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
