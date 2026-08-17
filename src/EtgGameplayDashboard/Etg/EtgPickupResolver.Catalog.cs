// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EtgGameplayDashboard.Core;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class EtgPickupResolver
    {
        public PickupObject ResolveShopDisplayPickup(ShopItemController shopItemController, Action<string> diagnosticLogger = null)
        {
            PickupObject shopItem = shopItemController != null ? shopItemController.item : null;
            if ((object)shopItem == null || !(shopItem is ItemBlueprintItem))
            {
                return shopItem;
            }

            // Some ETG meta shops use MetaShopController and wrap each sold pickup in
            // an ItemBlueprintItem. The blueprint's own PickupObjectId and copied
            // journal metadata are not the identity of the item being sold.
            // Keep this compatibility path before the foyer-NPC path below.
            PickupObject slotResolvedPickup = ResolveMetaShopSlotPickup(shopItemController, diagnosticLogger);
            if ((object)slotResolvedPickup != null)
            {
                return slotResolvedPickup;
            }

            // The three Breach NPC shops (Cadence & Ox, Doug, and Professor Goopton)
            // use BaseShopController with live stock stored in m_shopItems. Their
            // authoritative item is the entry matching the visible slot.
            PickupObject foyerMetaShopSlotPickup = ResolveFoyerMetaShopSlotPickup(shopItemController, diagnosticLogger);
            if ((object)foyerMetaShopSlotPickup != null)
            {
                return foyerMetaShopSlotPickup;
            }

            if (shopItem.encounterTrackable == null || shopItem.encounterTrackable.journalData == null)
            {
                return shopItem;
            }

            string primaryDisplayName = shopItem.encounterTrackable.journalData.PrimaryDisplayName;
            string notificationDescription = shopItem.encounterTrackable.journalData.NotificationPanelDescription;
            string ammonomiconSprite = shopItem.encounterTrackable.journalData.AmmonomiconSprite;
            GungeonFlags blueprintAcquisitionFlag = ((ItemBlueprintItem)shopItem).SaveFlagToSetOnAcquisition;
            PickupObject resolvedPickup = null;
            int matchCount = 0;

            LogShopDisplayDiagnostic(
                diagnosticLogger,
                "Blueprint: Type=" + shopItem.GetType().Name +
                ", PickupId=" + shopItem.PickupObjectId +
                ", InternalName=" + QuoteDiagnostic(shopItem.name) +
                ", PrimaryDisplayName=" + QuoteDiagnostic(primaryDisplayName) +
                ", NotificationDescription=" + QuoteDiagnostic(notificationDescription) +
                ", AmmonomiconSprite=" + QuoteDiagnostic(ammonomiconSprite) +
                ", SaveFlagToSetOnAcquisition=" + blueprintAcquisitionFlag + ".");

            foreach (PickupObject candidate in EnumeratePickups())
            {
                if ((object)candidate == null || candidate is ItemBlueprintItem || candidate.encounterTrackable == null || candidate.encounterTrackable.journalData == null)
                {
                    continue;
                }

                bool primaryMatches = string.Equals(candidate.encounterTrackable.journalData.PrimaryDisplayName, primaryDisplayName, StringComparison.Ordinal);
                bool descriptionMatches = string.Equals(candidate.encounterTrackable.journalData.NotificationPanelDescription, notificationDescription, StringComparison.Ordinal);
                bool spriteMatches = string.Equals(candidate.encounterTrackable.journalData.AmmonomiconSprite, ammonomiconSprite, StringComparison.Ordinal);
                GungeonFlags candidateAcquisitionFlag = GetFlagPrerequisite(candidate);
                bool acquisitionFlagMatches = candidateAcquisitionFlag == blueprintAcquisitionFlag;
                if (primaryMatches || descriptionMatches || spriteMatches)
                {
                    LogShopDisplayDiagnostic(
                        diagnosticLogger,
                        "Blueprint candidate: PickupId=" + candidate.PickupObjectId +
                        ", Type=" + candidate.GetType().Name +
                        ", InternalName=" + QuoteDiagnostic(candidate.name) +
                        ", Primary=" + QuoteDiagnostic(candidate.encounterTrackable.journalData.PrimaryDisplayName) +
                        ", Description=" + QuoteDiagnostic(candidate.encounterTrackable.journalData.NotificationPanelDescription) +
                        ", Sprite=" + QuoteDiagnostic(candidate.encounterTrackable.journalData.AmmonomiconSprite) +
                        ", SaveFlag=" + candidateAcquisitionFlag +
                        ", Matches=(Primary=" + primaryMatches +
                        ", Description=" + descriptionMatches +
                        ", Sprite=" + spriteMatches +
                        ", SaveFlag=" + acquisitionFlagMatches + ").");
                }

                if (!primaryMatches || !descriptionMatches || !spriteMatches || !acquisitionFlagMatches)
                {
                    continue;
                }

                resolvedPickup = candidate;
                matchCount++;
            }

            PickupObject result = matchCount == 1 ? resolvedPickup : shopItem;
            LogShopDisplayDiagnostic(
                diagnosticLogger,
                "Blueprint resolution result: MatchCount=" + matchCount +
                ", ResolvedPickupId=" + result.PickupObjectId +
                ", ResolvedType=" + result.GetType().Name +
                ", ResolvedInternalName=" + QuoteDiagnostic(result.name) + ".");
            return result;
        }

        private static PickupObject ResolveMetaShopSlotPickup(ShopItemController shopItemController, Action<string> diagnosticLogger)
        {
            if (shopItemController == null)
            {
                return null;
            }

            object parentShop = GetInstanceMemberValueAcrossBaseTypes(shopItemController, "m_parentShop");
            if (!(parentShop is MetaShopController))
            {
                return null;
            }

            // MetaShopController appends controllers in the same order in which vanilla
            // iterates the current-tier and proximate-tier item IDs. This preserves the
            // actual slot-to-item relationship when tier contents change; screen
            // coordinates are not an item identity.
            IList itemControllers = GetInstanceMemberValueAcrossBaseTypes(parentShop, "m_itemControllers") as IList;
            if (itemControllers == null)
            {
                LogShopDisplayDiagnostic(diagnosticLogger, "Meta shop slot resolution failed: item controller list was unavailable.");
                return null;
            }

            int controllerIndex = -1;
            for (int i = 0; i < itemControllers.Count; i++)
            {
                if (ReferenceEquals(itemControllers[i], shopItemController))
                {
                    controllerIndex = i;
                    break;
                }
            }

            if (controllerIndex < 0)
            {
                LogShopDisplayDiagnostic(diagnosticLogger, "Meta shop slot resolution failed: controller was not found in m_itemControllers.");
                return null;
            }

            object currentTier = InvokeNoArgumentMethod(parentShop, "GetCurrentTier");
            object proximateTier = InvokeNoArgumentMethod(parentShop, "GetProximateTier");
            int[] currentIds = ReadMetaShopTierItemIds(currentTier);
            int[] proximateIds = ReadMetaShopTierItemIds(proximateTier);
            int targetId = -1;
            string tierName = string.Empty;
            // Do not map by a fixed screen position. The controller index is the stable
            // runtime relationship established by the game's own meta-shop construction
            // path, and the current/proximate tier split follows vanilla ordering.
            int slotIndex = controllerIndex;
            if (slotIndex < currentIds.Length)
            {
                targetId = currentIds[slotIndex];
                tierName = "current";
            }
            else
            {
                slotIndex -= currentIds.Length;
                if (slotIndex < proximateIds.Length)
                {
                    targetId = proximateIds[slotIndex];
                    tierName = "proximate";
                }
            }

            LogShopDisplayDiagnostic(
                diagnosticLogger,
                "Meta shop slot mapping: ControllerIndex=" + controllerIndex +
                ", Tier=" + (string.IsNullOrEmpty(tierName) ? "<none>" : tierName) +
                ", SlotIndex=" + slotIndex +
                ", TargetPickupId=" + targetId + ".");
            if (targetId < 0)
            {
                return null;
            }

            PickupObject resolvedPickup = PickupObjectDatabase.GetById(targetId);
            if ((object)resolvedPickup == null)
            {
                LogShopDisplayDiagnostic(diagnosticLogger, "Meta shop slot mapping failed: PickupObjectDatabase returned null for TargetPickupId=" + targetId + ".");
            }

            return resolvedPickup;
        }

        private static PickupObject ResolveFoyerMetaShopSlotPickup(ShopItemController shopItemController, Action<string> diagnosticLogger)
        {
            if (shopItemController == null)
            {
                return null;
            }

            object parentShop = GetInstanceMemberValueAcrossBaseTypes(shopItemController, "m_baseParentShop");
            if (!(parentShop is BaseShopController))
            {
                return null;
            }

            // Breach NPC shops such as Doug and Professor Goopton are not
            // MetaShopController instances. They create blueprint display items from
            // BaseShopController.m_shopItems, so the authoritative item is the stock
            // entry at the same spawn-position slot as this ShopItemController.
            BaseShopController foyerMetaShop = (BaseShopController)parentShop;
            if (foyerMetaShop.baseShopType != BaseShopController.AdditionalShopType.FOYER_META)
            {
                return null;
            }

            IList shopItems = GetInstanceMemberValueAcrossBaseTypes(foyerMetaShop, "m_shopItems") as IList;
            if (shopItems == null)
            {
                LogShopDisplayDiagnostic(diagnosticLogger, "Foyer meta shop slot resolution failed: shop item list was unavailable.");
                return null;
            }

            int slotIndex = FindShopSlotIndex(shopItemController, foyerMetaShop);
            if (slotIndex < 0 || slotIndex >= shopItems.Count)
            {
                LogShopDisplayDiagnostic(
                    diagnosticLogger,
                    "Foyer meta shop slot resolution failed: controller slot was not found. Controller=" + shopItemController.GetInstanceID() + ".");
                return null;
            }

            GameObject shopItemObject = shopItems[slotIndex] as GameObject;
            PickupObject resolvedPickup = shopItemObject != null ? shopItemObject.GetComponent<PickupObject>() : null;
            LogShopDisplayDiagnostic(
                diagnosticLogger,
                "Foyer meta shop slot mapping: ShopType=" + foyerMetaShop.baseShopType +
                ", IsBeetleMerchant=" + foyerMetaShop.IsBeetleMerchant +
                ", SlotIndex=" + slotIndex +
                ", TargetPickupId=" + (resolvedPickup != null ? resolvedPickup.PickupObjectId.ToString() : "<null>") +
                ", TargetType=" + (resolvedPickup != null ? resolvedPickup.GetType().Name : "<null>") + ".");
            return resolvedPickup;
        }

        private static int FindShopSlotIndex(ShopItemController shopItemController, BaseShopController foyerMetaShop)
        {
            if (shopItemController == null || foyerMetaShop == null)
            {
                return -1;
            }

            // ShopItemController objects are parented directly to their spawn-position
            // transform. Matching that transform is resilient to stock refreshes and
            // avoids assuming that visual left-to-right order is permanent.
            Transform parent = shopItemController.transform != null ? shopItemController.transform.parent : null;
            if (parent == null)
            {
                return -1;
            }

            Transform[] primaryPositions = foyerMetaShop.spawnPositions;
            if (primaryPositions != null)
            {
                for (int index = 0; index < primaryPositions.Length; index++)
                {
                    if (ReferenceEquals(primaryPositions[index], parent))
                    {
                        return index;
                    }
                }
            }

            Transform[] secondaryPositions = foyerMetaShop.spawnPositionsGroup2;
            int primaryCount = primaryPositions != null ? primaryPositions.Length : 0;
            if (secondaryPositions != null)
            {
                for (int index = 0; index < secondaryPositions.Length; index++)
                {
                    if (ReferenceEquals(secondaryPositions[index], parent))
                    {
                        return primaryCount + index;
                    }
                }
            }

            return -1;
        }

        private static int[] ReadMetaShopTierItemIds(object tier)
        {
            if (tier == null)
            {
                return new int[0];
            }

            List<int> ids = new List<int>();
            for (int i = 1; i <= 3; i++)
            {
                object value = GetInstanceMemberValueAcrossBaseTypes(tier, "itemId" + i);
                if (value is int && (int)value >= 0)
                {
                    ids.Add((int)value);
                }
            }

            return ids.ToArray();
        }

        private static object InvokeNoArgumentMethod(object target, string methodName)
        {
            if (target == null)
            {
                return null;
            }

            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return method != null ? method.Invoke(target, null) : null;
        }

        private static object GetInstanceMemberValueAcrossBaseTypes(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            Type type = target.GetType();
            while (type != null)
            {
                PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property != null)
                {
                    return property.GetValue(target, null);
                }

                FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field.GetValue(target);
                }

                type = type.BaseType;
            }

            return null;
        }

        private static void LogShopDisplayDiagnostic(Action<string> diagnosticLogger, string message)
        {
            if (diagnosticLogger != null && !string.IsNullOrEmpty(message))
            {
                diagnosticLogger(message);
            }
        }

        private static string QuoteDiagnostic(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "'") + "\"";
        }

        private static GungeonFlags GetFlagPrerequisite(PickupObject pickup)
        {
            if ((object)pickup == null || pickup.encounterTrackable == null || pickup.encounterTrackable.prerequisites == null)
            {
                return GungeonFlags.NONE;
            }

            for (int i = 0; i < pickup.encounterTrackable.prerequisites.Length; i++)
            {
                DungeonPrerequisite prerequisite = pickup.encounterTrackable.prerequisites[i];
                if (prerequisite != null && prerequisite.prerequisiteType == DungeonPrerequisite.PrerequisiteType.FLAG)
                {
                    return prerequisite.saveFlagToCheck;
                }
            }

            return GungeonFlags.NONE;
        }

        private EtgPickupCatalogEntry[] _grantablePickupCatalogCache;
        private string _grantablePickupCatalogCacheLanguage;

        internal void InvalidateGrantablePickupCatalogCache()
        {
            _grantablePickupCatalogCache = null;
            _grantablePickupCatalogCacheLanguage = string.Empty;
        }

        public EtgPickupCatalogEntry[] GetGrantablePickupCatalog()
        {
            string currentLanguage = GuiText.CurrentLanguageCode ?? string.Empty;
            if (_grantablePickupCatalogCache != null &&
                string.Equals(_grantablePickupCatalogCacheLanguage, currentLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return _grantablePickupCatalogCache;
            }

            List<EtgPickupCatalogEntry> entries = new List<EtgPickupCatalogEntry>();
            foreach (PickupObject pickup in EnumeratePickups())
            {
                if ((object)pickup == null)
                {
                    continue;
                }

                PickupCategory? category = GetPickupCategory(pickup);
                if (!category.HasValue)
                {
                    continue;
                }

                entries.Add(
                    new EtgPickupCatalogEntry(
                        category.Value,
                        pickup.PickupObjectId,
                        GetPickupLabel(pickup),
                        GetEnglishPickupLabel(pickup),
                        GetPickupLabelForGameLanguage(pickup),
                        pickup.name ?? string.Empty,
                        GetEncounterGuid(pickup),
                        GetItemQualityLabel(pickup),
                        GetPurchasePrice(pickup),
                        pickup.CanBeDropped,
                        pickup.CanBeSold,
                        pickup.encounterTrackable != null && pickup.encounterTrackable.SuppressInInventory,
                        GetPrimaryDisplayName(pickup),
                        GetNotificationDescription(pickup),
                        GetAmmonomiconFullEntry(pickup),
                        GetContentSourceLabel(pickup),
                        pickup.ForcedPositionInAmmonomicon,
                        GetGunClassLabel(pickup as Gun),
                        GetIntMemberValue(pickup, "ammo", 0),
                        GetBoolMemberValue(pickup, "CanGainAmmo", false),
                        GetBoolMemberValue(pickup, "LocalInfiniteAmmo", false),
                        GetFloatMemberValue(pickup, "reloadTime", 0f),
                        GetIntMemberValue(pickup, "numberOfUses", 0),
                        GetFloatMemberValue(pickup, "timeCooldown", 0f),
                        GetFloatMemberValue(pickup, "damageCooldown", 0f),
                        GetIntMemberValue(pickup, "roomCooldown", 0)));
            }

            entries.Sort(CompareCatalogEntries);
            _grantablePickupCatalogCache = entries.ToArray();
            _grantablePickupCatalogCacheLanguage = currentLanguage;
            return _grantablePickupCatalogCache;
        }

        private static int CompareCatalogEntries(EtgPickupCatalogEntry left, EtgPickupCatalogEntry right)
        {
            int categoryComparison = left.Category.CompareTo(right.Category);
            if (categoryComparison != 0)
            {
                return categoryComparison;
            }

            int labelComparison = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
            if (labelComparison != 0)
            {
                return labelComparison;
            }

            return left.PickupId.CompareTo(right.PickupId);
        }

        private static string GetEncounterGuid(PickupObject pickup)
        {
            if ((object)pickup == null || pickup.encounterTrackable == null)
            {
                return string.Empty;
            }

            return pickup.encounterTrackable.EncounterGuid ?? string.Empty;
        }

        private static string GetPrimaryDisplayName(PickupObject pickup)
        {
            if ((object)pickup == null || pickup.encounterTrackable == null || pickup.encounterTrackable.journalData == null)
            {
                return string.Empty;
            }

            return ResolveLocalizedLabelForCurrentUiLanguage(pickup.encounterTrackable.journalData.PrimaryDisplayName);
        }

        private static string GetNotificationDescription(PickupObject pickup)
        {
            if ((object)pickup == null || pickup.encounterTrackable == null || pickup.encounterTrackable.journalData == null)
            {
                return string.Empty;
            }

            return ResolveLocalizedLabelForCurrentUiLanguage(pickup.encounterTrackable.journalData.NotificationPanelDescription);
        }

        private static string GetAmmonomiconFullEntry(PickupObject pickup)
        {
            if ((object)pickup == null || pickup.encounterTrackable == null || pickup.encounterTrackable.journalData == null)
            {
                return string.Empty;
            }

            return ResolveLocalizedLabelForCurrentUiLanguage(pickup.encounterTrackable.journalData.AmmonomiconFullEntry);
        }

        private static string GetItemQualityLabel(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return string.Empty;
            }

            return pickup.quality.ToString();
        }

        private static int GetPurchasePrice(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return 0;
            }

            // ETG's PurchasePrice calls GlobalDungeonData.GetBasePrice for non-custom
            // costs. SPECIAL and EXCLUDED are valid catalog qualities but are not valid
            // inputs for that ordinary shop-price lookup.
            if (pickup.UsesCustomCost)
            {
                return pickup.CustomCost;
            }

            if (pickup.quality == PickupObject.ItemQuality.SPECIAL ||
                pickup.quality == PickupObject.ItemQuality.EXCLUDED)
            {
                return 0;
            }

            return pickup.PurchasePrice;
        }

        private static string GetContentSourceLabel(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return string.Empty;
            }

            return pickup.contentSource.ToString();
        }

        private static string GetGunClassLabel(Gun gun)
        {
            if ((object)gun == null)
            {
                return string.Empty;
            }

            return gun.gunClass.ToString();
        }

        private static int GetIntMemberValue(object target, string memberName, int defaultValue)
        {
            object value = GetInstanceMemberValue(target, memberName);
            return value is int ? (int)value : defaultValue;
        }

        private static float GetFloatMemberValue(object target, string memberName, float defaultValue)
        {
            object value = GetInstanceMemberValue(target, memberName);
            return value is float ? (float)value : defaultValue;
        }

        private static bool GetBoolMemberValue(object target, string memberName, bool defaultValue)
        {
            object value = GetInstanceMemberValue(target, memberName);
            return value is bool ? (bool)value : defaultValue;
        }
    }
}
