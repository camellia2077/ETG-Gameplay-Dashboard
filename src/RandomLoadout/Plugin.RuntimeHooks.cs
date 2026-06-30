using BepInEx.Logging;
using HarmonyLib;

namespace RandomLoadout
{
    public sealed partial class Plugin
    {
        private void CreateRuntimeHookRegistry()
        {
            _runtimeHookRegistry = new RuntimeHookRegistry(GUID, Logger);
            _runtimeHookRegistry.Register(".bossrush", InstallBossRushRuntimeHooks, null);
            _runtimeHookRegistry.Register(".ammonomicon_animation", InstallAmmonomiconRuntimeHooks, null);
            _runtimeHookRegistry.Register(".currency_no_consume", InstallCurrencyNoConsumeRuntimeHooks, ClearCurrencyNoConsumeRuntimeHookConfiguration);
        }

        private void InstallRuntimeHooks()
        {
            if (_runtimeHookRegistry != null)
            {
                _runtimeHookRegistry.InstallAll();
            }
        }

        private void UninstallRuntimeHooks()
        {
            if (_runtimeHookRegistry == null)
            {
                return;
            }

            _runtimeHookRegistry.UninstallAll();
            _runtimeHookRegistry = null;
        }

        private void InstallBossRushRuntimeHooks(Harmony harmony, ManualLogSource logger)
        {
            LogBossRushHookSelfCheck(BossRushHooks.Install(harmony, logger));
        }

        private static void InstallAmmonomiconRuntimeHooks(Harmony harmony, ManualLogSource logger)
        {
            AmmonomiconAnimationHooks.Install(harmony, logger);
        }

        private void InstallCurrencyNoConsumeRuntimeHooks(Harmony harmony, ManualLogSource logger)
        {
            CurrencyNoConsumeHooks.Configure(_currencyNoConsumeToggleService);
            CurrencyNoConsumeHooks.Install(harmony, logger);
        }

        private static void ClearCurrencyNoConsumeRuntimeHookConfiguration()
        {
            CurrencyNoConsumeHooks.Configure(null);
        }
    }
}
