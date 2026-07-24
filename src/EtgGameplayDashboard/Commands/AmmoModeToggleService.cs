// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    internal enum AmmoMode
    {
        Off,
        NoConsume,
        InfiniteReserve,
    }

    internal sealed class AmmoModeToggleService
    {
        private readonly System.Action<AmmoMode> _persistMode;
        private AmmoMode _mode;
        private Gun _trackedGun;
        private int _trackedAmmo;
        private int _trackedClipShotsRemaining;

        public AmmoModeToggleService(AmmoMode initiallyEnabledMode, System.Action<AmmoMode> persistMode)
        {
            _mode = initiallyEnabledMode;
            _persistMode = persistMode;
        }

        public AmmoMode Mode
        {
            get { return _mode; }
        }

        public GrantCommandExecutionResult Toggle()
        {
            if (_mode == AmmoMode.Off)
            {
                _mode = AmmoMode.InfiniteReserve;
                PersistMode();
                return GrantCommandExecutionResult.Localized(true, "result.ammo_mode.infinite_reserve.success");
            }

            if (_mode == AmmoMode.InfiniteReserve)
            {
                _mode = AmmoMode.NoConsume;
                PersistMode();
                return GrantCommandExecutionResult.Localized(true, "result.ammo_mode.no_consume.success");
            }

            _mode = AmmoMode.Off;
            ClearTrackedState();
            PersistMode();
            return GrantCommandExecutionResult.Localized(true, "result.ammo_mode.off.success");
        }

        private void PersistMode()
        {
            if (_persistMode != null)
            {
                _persistMode(_mode);
            }
        }

        public void Update(PlayerController player)
        {
            if (_mode == AmmoMode.Off || (object)player == null)
            {
                return;
            }

            Gun currentGun = player.CurrentGun;
            if ((object)currentGun == null || currentGun.InfiniteAmmo)
            {
                ClearTrackedState();
                return;
            }

            if (!ReferenceEquals(_trackedGun, currentGun))
            {
                CaptureCurrentGunState(currentGun);
                return;
            }

            if (_trackedAmmo >= 0 && currentGun.CurrentAmmo < _trackedAmmo)
            {
                currentGun.CurrentAmmo = _trackedAmmo;
            }
            else if (currentGun.CurrentAmmo > _trackedAmmo)
            {
                _trackedAmmo = currentGun.CurrentAmmo;
            }

            if (_mode == AmmoMode.NoConsume)
            {
                if (_trackedClipShotsRemaining >= 0 && currentGun.ClipShotsRemaining < _trackedClipShotsRemaining)
                {
                    currentGun.ClipShotsRemaining = _trackedClipShotsRemaining;
                }
                else if (currentGun.ClipShotsRemaining > _trackedClipShotsRemaining)
                {
                    _trackedClipShotsRemaining = currentGun.ClipShotsRemaining;
                }

                SyncLockedAmmoBehaviour(currentGun);
                return;
            }

            RemoveLockedAmmoBehaviour(currentGun);
        }

        public void Reset()
        {
            _mode = AmmoMode.Off;
            ClearTrackedState();
        }

        private void CaptureCurrentGunState(Gun gun)
        {
            RemoveLockedAmmoBehaviour(_trackedGun);
            _trackedGun = gun;
            _trackedAmmo = IsGunUsable(gun) ? gun.CurrentAmmo : 0;
            _trackedClipShotsRemaining = IsGunUsable(gun) ? gun.ClipShotsRemaining : 0;
            SyncLockedAmmoBehaviour(gun);
        }

        private void ClearTrackedState()
        {
            RemoveLockedAmmoBehaviour(_trackedGun);
            _trackedGun = null;
            _trackedAmmo = 0;
            _trackedClipShotsRemaining = 0;
        }

        private void SyncLockedAmmoBehaviour(Gun gun)
        {
            if (!IsGunUsable(gun))
            {
                return;
            }

            LockedAmmoGunBehaviour behaviour;
            if (!TryGetLockedAmmoBehaviour(gun, out behaviour))
            {
                return;
            }

            if ((object)behaviour == null)
            {
                if (!IsUnityObjectAlive(gun.gameObject))
                {
                    return;
                }

                behaviour = gun.gameObject.AddComponent<LockedAmmoGunBehaviour>();
            }

            behaviour.SetLockedState(_trackedAmmo, _trackedClipShotsRemaining);
        }

        private void RemoveLockedAmmoBehaviour(Gun gun)
        {
            if (!IsGunUsable(gun))
            {
                return;
            }

            LockedAmmoGunBehaviour behaviour;
            if (!TryGetLockedAmmoBehaviour(gun, out behaviour))
            {
                return;
            }

            if ((object)behaviour != null)
            {
                UnityEngine.Object.Destroy(behaviour);
            }
        }

        private static bool TryGetLockedAmmoBehaviour(Gun gun, out LockedAmmoGunBehaviour behaviour)
        {
            behaviour = null;
            if (!IsGunUsable(gun))
            {
                return false;
            }

            try
            {
                behaviour = gun.GetComponent<LockedAmmoGunBehaviour>();
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private static bool IsGunUsable(Gun gun)
        {
            return IsUnityObjectAlive(gun) && IsUnityObjectAlive(gun.gameObject);
        }

        private static bool IsUnityObjectAlive(UnityEngine.Object unityObject)
        {
            return (object)unityObject != null && unityObject != null;
        }
    }
}
