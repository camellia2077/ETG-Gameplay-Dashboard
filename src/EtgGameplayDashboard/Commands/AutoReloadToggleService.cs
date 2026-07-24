// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;
using System;

namespace EtgGameplayDashboard
{
    internal enum AutoReloadMode
    {
        Off,
        Instant,
        Animated,
    }

    internal sealed class AutoReloadToggleService
    {
        private readonly HashSet<Gun> _reloadRequestedGuns = new HashSet<Gun>();
        private readonly Action<AutoReloadMode> _persistMode;
        private AutoReloadMode _mode;

        public AutoReloadToggleService(AutoReloadMode initiallyEnabledMode, Action<AutoReloadMode> persistMode)
        {
            _mode = initiallyEnabledMode;
            _persistMode = persistMode;
        }

        public GrantCommandExecutionResult Toggle()
        {
            if (_mode == AutoReloadMode.Off)
            {
                _mode = AutoReloadMode.Instant;
                PersistMode();
                return GrantCommandExecutionResult.Localized(true, "result.auto_reload.instant.success");
            }

            if (_mode == AutoReloadMode.Instant)
            {
                _mode = AutoReloadMode.Animated;
                _reloadRequestedGuns.Clear();
                PersistMode();
                return GrantCommandExecutionResult.Localized(true, "result.auto_reload.animated.success");
            }

            _mode = AutoReloadMode.Off;
            _reloadRequestedGuns.Clear();
            PersistMode();
            return GrantCommandExecutionResult.Localized(true, "result.auto_reload.off.success");
        }

        public AutoReloadMode Mode
        {
            get { return _mode; }
        }

        public void Update(PlayerController player)
        {
            if (_mode == AutoReloadMode.Off)
            {
                return;
            }

            Gun currentGun = GetCurrentGun(player);
            if ((object)currentGun == null)
            {
                return;
            }

            if (currentGun.ClipShotsRemaining > 0)
            {
                _reloadRequestedGuns.Remove(currentGun);
                return;
            }

            if (!ShouldReload(currentGun))
            {
                return;
            }

            if (_reloadRequestedGuns.Contains(currentGun))
            {
                if (currentGun.IsReloading)
                {
                    return;
                }

                _reloadRequestedGuns.Remove(currentGun);
            }

            if (_mode == AutoReloadMode.Instant)
            {
                currentGun.ForceImmediateReload(false);
                _reloadRequestedGuns.Add(currentGun);
                return;
            }

            if (_mode == AutoReloadMode.Animated && !currentGun.IsReloading && currentGun.Reload())
            {
                _reloadRequestedGuns.Add(currentGun);
            }
        }

        public void Reset()
        {
            _mode = AutoReloadMode.Off;
            _reloadRequestedGuns.Clear();
        }

        public void Disable()
        {
            _mode = AutoReloadMode.Off;
            _reloadRequestedGuns.Clear();
        }

        private void PersistMode()
        {
            if (_persistMode != null)
            {
                _persistMode(_mode);
            }
        }

        private static bool ShouldReload(Gun gun)
        {
            if ((object)gun == null || gun.ClipCapacity <= 0)
            {
                return false;
            }

            return gun.InfiniteAmmo || gun.CurrentAmmo > 0;
        }

        private static Gun GetCurrentGun(PlayerController player)
        {
            return (object)player != null ? player.CurrentGun : null;
        }
    }
}
