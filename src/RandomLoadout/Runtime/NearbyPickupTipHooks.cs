// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace RandomLoadout
{
    internal static class NearbyPickupTipHooks
    {
        private static NearbyPickupTipService s_service;

        public static void Configure(NearbyPickupTipService service)
        {
            s_service = service;
        }

        public static void Install(Harmony harmony, ManualLogSource logger)
        {
            if (harmony == null)
            {
                return;
            }

            Patch(harmony, logger, typeof(Gun), "OnEnteredRange", new[] { typeof(PlayerController) }, "Gun.OnEnteredRange", "GunOnEnteredRangePostfix");
            Patch(harmony, logger, typeof(Gun), "OnExitRange", new[] { typeof(PlayerController) }, "Gun.OnExitRange", "GunOnExitRangePostfix");
            Patch(harmony, logger, typeof(Gun), "Pickup", new[] { typeof(PlayerController) }, "Gun.Pickup", "GunPickupPostfix");

            Patch(harmony, logger, typeof(PassiveItem), "OnEnteredRange", new[] { typeof(PlayerController) }, "PassiveItem.OnEnteredRange", "PassiveItemOnEnteredRangePostfix");
            Patch(harmony, logger, typeof(PassiveItem), "OnExitRange", new[] { typeof(PlayerController) }, "PassiveItem.OnExitRange", "PassiveItemOnExitRangePostfix");
            Patch(harmony, logger, typeof(PassiveItem), "Pickup", new[] { typeof(PlayerController) }, "PassiveItem.Pickup", "PassiveItemPickupPostfix");

            Patch(harmony, logger, typeof(PlayerItem), "OnEnteredRange", new[] { typeof(PlayerController) }, "PlayerItem.OnEnteredRange", "PlayerItemOnEnteredRangePostfix");
            Patch(harmony, logger, typeof(PlayerItem), "OnExitRange", new[] { typeof(PlayerController) }, "PlayerItem.OnExitRange", "PlayerItemOnExitRangePostfix");
            Patch(harmony, logger, typeof(PlayerItem), "Pickup", new[] { typeof(PlayerController) }, "PlayerItem.Pickup", "PlayerItemPickupPostfix");

            Patch(harmony, logger, typeof(ShopItemController), "OnEnteredRange", new[] { typeof(PlayerController) }, "ShopItemController.OnEnteredRange", "ShopItemOnEnteredRangePostfix");
            Patch(harmony, logger, typeof(ShopItemController), "OnExitRange", new[] { typeof(PlayerController) }, "ShopItemController.OnExitRange", "ShopItemOnExitRangePostfix");
            Patch(harmony, logger, typeof(ShopItemController), "Interact", new[] { typeof(PlayerController) }, "ShopItemController.Interact", "ShopItemInteractPostfix");

            Patch(harmony, logger, typeof(RewardPedestal), "OnEnteredRange", new[] { typeof(PlayerController) }, "RewardPedestal.OnEnteredRange", "RewardPedestalOnEnteredRangePostfix");
            Patch(harmony, logger, typeof(RewardPedestal), "OnExitRange", new[] { typeof(PlayerController) }, "RewardPedestal.OnExitRange", "RewardPedestalOnExitRangePostfix");
            Patch(harmony, logger, typeof(RewardPedestal), "Interact", new[] { typeof(PlayerController) }, "RewardPedestal.Interact", "RewardPedestalInteractPostfix");
        }

        private static void Patch(Harmony harmony, ManualLogSource logger, Type targetType, string methodName, Type[] parameterTypes, string hookLabel, string postfixMethodName)
        {
            MethodInfo targetMethod = AccessTools.Method(targetType, methodName, parameterTypes);
            MethodInfo postfixMethod = AccessTools.Method(typeof(NearbyPickupTipHooks), postfixMethodName);
            if (targetMethod == null || postfixMethod == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Nearby pickup tip hook skipped: " + hookLabel + ". Target or patch method was not found."));
                }

                return;
            }

            try
            {
                harmony.Patch(targetMethod, postfix: new HarmonyMethod(postfixMethod));
                if (logger != null)
                {
                    logger.LogInfo(RandomLoadoutLog.Init("Nearby pickup tip hook ready: " + hookLabel));
                }
            }
            catch (Exception exception)
            {
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Nearby pickup tip hook failed: " + hookLabel + ". " + exception.GetType().Name + ": " + exception.Message));
                }
            }
        }

        private static void GunOnEnteredRangePostfix(Gun __instance, PlayerController interactor)
        {
            if (s_service != null)
            {
                s_service.HandlePickupEnteredRange(__instance, interactor);
            }
        }

        private static void GunOnExitRangePostfix(Gun __instance)
        {
            if (s_service != null)
            {
                s_service.HandlePickupExitedRange(__instance);
            }
        }

        private static void GunPickupPostfix(Gun __instance)
        {
            if (s_service != null)
            {
                s_service.HandlePickupConsumed(__instance);
            }
        }

        private static void PassiveItemOnEnteredRangePostfix(PassiveItem __instance, PlayerController interactor)
        {
            if (s_service != null)
            {
                s_service.HandlePickupEnteredRange(__instance, interactor);
            }
        }

        private static void PassiveItemOnExitRangePostfix(PassiveItem __instance)
        {
            if (s_service != null)
            {
                s_service.HandlePickupExitedRange(__instance);
            }
        }

        private static void PassiveItemPickupPostfix(PassiveItem __instance)
        {
            if (s_service != null)
            {
                s_service.HandlePickupConsumed(__instance);
            }
        }

        private static void PlayerItemOnEnteredRangePostfix(PlayerItem __instance, PlayerController interactor)
        {
            if (s_service != null)
            {
                s_service.HandlePickupEnteredRange(__instance, interactor);
            }
        }

        private static void PlayerItemOnExitRangePostfix(PlayerItem __instance)
        {
            if (s_service != null)
            {
                s_service.HandlePickupExitedRange(__instance);
            }
        }

        private static void PlayerItemPickupPostfix(PlayerItem __instance)
        {
            if (s_service != null)
            {
                s_service.HandlePickupConsumed(__instance);
            }
        }

        private static void ShopItemOnEnteredRangePostfix(ShopItemController __instance, PlayerController interactor)
        {
            // Keep the Harmony parameter name aligned with the ETG method signature.
            // Using a different name here caused the patch to compile but fail to bind
            // at runtime for shop entered-range events.
            if (s_service != null)
            {
                s_service.HandleShopItemEnteredRange(__instance, interactor);
            }
        }

        private static void ShopItemOnExitRangePostfix(ShopItemController __instance)
        {
            if (s_service != null)
            {
                s_service.HandleShopItemExitedRange(__instance);
            }
        }

        private static void ShopItemInteractPostfix(ShopItemController __instance)
        {
            if (s_service != null)
            {
                s_service.HandleShopItemInteracted(__instance);
            }
        }

        private static void RewardPedestalOnEnteredRangePostfix(RewardPedestal __instance, PlayerController interactor)
        {
            // RewardPedestal uses the same "interactor" parameter name requirement as
            // ShopItemController for Harmony argument binding.
            if (s_service != null)
            {
                s_service.HandleRewardPedestalEnteredRange(__instance, interactor);
            }
        }

        private static void RewardPedestalOnExitRangePostfix(RewardPedestal __instance)
        {
            if (s_service != null)
            {
                s_service.HandleRewardPedestalExitedRange(__instance);
            }
        }

        private static void RewardPedestalInteractPostfix(RewardPedestal __instance)
        {
            if (s_service != null)
            {
                s_service.HandleRewardPedestalInteracted(__instance);
            }
        }
    }
}
