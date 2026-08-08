// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Collections.Generic;

namespace EtgGameplayDashboard
{
    internal sealed class SkipChargeToggleService
    {
        private readonly HashSet<PlayerController> _enabledPlayers = new HashSet<PlayerController>();

        public bool Toggle(PlayerController player)
        {
            if ((object)player == null)
            {
                return false;
            }

            if (!_enabledPlayers.Add(player))
            {
                _enabledPlayers.Remove(player);
            }

            return IsEnabledFor(player);
        }

        public bool IsEnabledFor(PlayerController player)
        {
            return (object)player != null && _enabledPlayers.Contains(player);
        }

        public void Reset()
        {
            _enabledPlayers.Clear();
        }

        public void PrepareGunForSkipCharge(Gun gun)
        {
            if ((object)gun == null || !(gun.CurrentOwner is PlayerController) || !IsEnabledFor(gun.CurrentOwner as PlayerController))
            {
                return;
            }

            Dictionary<ProjectileModule, ModuleShootData> moduleData =
                PrivateFieldAccessor.GetPrivateObject<Dictionary<ProjectileModule, ModuleShootData>>(gun, "m_moduleData");
            if (moduleData == null)
            {
                return;
            }

            foreach (KeyValuePair<ProjectileModule, ModuleShootData> pair in moduleData)
            {
                ProjectileModule module = pair.Key;
                ModuleShootData data = pair.Value;
                if ((object)module == null || data == null || module.shootStyle != ProjectileModule.ShootStyle.Charged)
                {
                    continue;
                }

                data.chargeTime = System.Math.Max(module.LongestChargeTime, module.maxChargeTime);
                data.lastChargeProjectile = module.GetChargeProjectile(data.chargeTime);
            }
        }
    }
}
