using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace RandomLoadout
{
    internal static class CurrencyNoConsumeHooks
    {
        private static CurrencyNoConsumeToggleService s_service;

        public static void Configure(CurrencyNoConsumeToggleService service)
        {
            s_service = service;
        }

        public static void Install(Harmony harmony, ManualLogSource logger)
        {
            if (harmony == null)
            {
                return;
            }

            const string hookLabel = "ShopItemController.Interact -> CurrencyNoConsumeHooks";
            MethodInfo targetMethod = AccessTools.Method(typeof(ShopItemController), "Interact", new[] { typeof(PlayerController) });
            MethodInfo prefixMethod = AccessTools.Method(typeof(CurrencyNoConsumeHooks), "ShopItemInteractPrefix");
            MethodInfo postfixMethod = AccessTools.Method(typeof(CurrencyNoConsumeHooks), "ShopItemInteractPostfix");

            if (targetMethod == null || prefixMethod == null || postfixMethod == null)
            {
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Currency no-consume hook skipped: " + hookLabel + ". Target or patch method was not found."));
                }

                return;
            }

            try
            {
                harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefixMethod), postfix: new HarmonyMethod(postfixMethod));
                if (logger != null)
                {
                    logger.LogInfo(RandomLoadoutLog.Init("Currency no-consume hook ready: " + hookLabel));
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Init("Currency no-consume hook failed: " + hookLabel + ". " + ex.GetType().Name + ": " + ex.Message));
                }
            }
        }

        private static void ShopItemInteractPrefix(ShopItemController __instance, PlayerController player, ref CurrencyAffordabilityOverrideState __state)
        {
            __state = default(CurrencyAffordabilityOverrideState);

            if (__instance == null || player == null || s_service == null || !s_service.IsEnabled)
            {
                return;
            }

            if (!s_service.ShouldOverrideAffordability(__instance))
            {
                return;
            }

            PlayerConsumables consumables = player.carriedConsumables;
            if (consumables == null)
            {
                return;
            }

            int requiredCurrency = __instance.ModifiedPrice;
            if (requiredCurrency <= 0 || consumables.Currency >= requiredCurrency)
            {
                return;
            }

            // The shop affordability gate runs before currency is deducted. We temporarily lift the
            // displayed amount just for this interaction so low-currency purchases can proceed, then
            // restore the original casing total in the postfix without teaching the tracker a fake value.
            __state = new CurrencyAffordabilityOverrideState(true, consumables, consumables.Currency, requiredCurrency);
            consumables.Currency = requiredCurrency;
        }

        private static void ShopItemInteractPostfix(ref CurrencyAffordabilityOverrideState __state)
        {
            if (!__state.WasApplied || __state.Consumables == null)
            {
                return;
            }

            if (__state.Consumables.Currency == __state.TemporaryCurrency)
            {
                __state.Consumables.Currency = __state.OriginalCurrency;
                return;
            }

            __state.Consumables.Currency = __state.OriginalCurrency + __state.Consumables.Currency;
        }

        private struct CurrencyAffordabilityOverrideState
        {
            public readonly bool WasApplied;
            public readonly PlayerConsumables Consumables;
            public readonly int OriginalCurrency;
            public readonly int TemporaryCurrency;

            public CurrencyAffordabilityOverrideState(bool wasApplied, PlayerConsumables consumables, int originalCurrency, int temporaryCurrency)
            {
                WasApplied = wasApplied;
                Consumables = consumables;
                OriginalCurrency = originalCurrency;
                TemporaryCurrency = temporaryCurrency;
            }
        }
    }
}
