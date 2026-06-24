using System.Reflection;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed class LockedAmmoGunBehaviour : GunBehaviour
    {
        private static readonly FieldInfo ReloadWhenDoneFiringField = typeof(Gun).GetField("m_reloadWhenDoneFiring", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo IsReloadingField = typeof(Gun).GetField("m_isReloading", BindingFlags.Instance | BindingFlags.NonPublic);
        private int _lockedAmmo;
        private int _lockedClipShotsRemaining;

        public void SetLockedState(int lockedAmmo, int lockedClipShotsRemaining)
        {
            _lockedAmmo = lockedAmmo;
            _lockedClipShotsRemaining = lockedClipShotsRemaining;
            ApplyLockedState(gun);
        }

        public override void AutoreloadOnEmptyClip(GameActor owner, Gun gun, ref bool autoreload)
        {
            autoreload = false;
        }

        public override void OnPostFired(PlayerController player, Gun gun)
        {
            ApplyLockedState(gun);
        }

        public override void OnAmmoChanged(PlayerController player, Gun gun)
        {
            ApplyLockedState(gun);
        }

        public override void OnFinishAttack(PlayerController player, Gun gun)
        {
            ApplyLockedState(gun);
        }

        public override void OnReloadPressed(PlayerController player, Gun gun, bool manual)
        {
            ApplyLockedState(gun);
        }

        private void ApplyLockedState(Gun gun)
        {
            if ((object)gun == null)
            {
                return;
            }

            if (_lockedAmmo >= 0 && gun.CurrentAmmo < _lockedAmmo)
            {
                gun.CurrentAmmo = _lockedAmmo;
            }

            bool restoredClip = false;
            if (_lockedClipShotsRemaining >= 0 && gun.ClipShotsRemaining < _lockedClipShotsRemaining)
            {
                gun.ClipShotsRemaining = _lockedClipShotsRemaining;
                restoredClip = true;
            }

            if (!restoredClip && gun.ClipShotsRemaining > 0 && !gun.IsReloading)
            {
                return;
            }

            // Restore the locked clip state immediately after firing so the gun never stays in the
            // engine's pending-reload path. This keeps "last bullet" effects intact while preventing
            // both our helper reload and the game's native empty-clip reload flow from starting.
            gun.ClearReloadData();
            if (ReloadWhenDoneFiringField != null)
            {
                ReloadWhenDoneFiringField.SetValue(gun, false);
            }

            if (IsReloadingField != null)
            {
                IsReloadingField.SetValue(gun, false);
            }

            PlayerController player = gun.CurrentOwner as PlayerController;
            if ((object)player != null && (object)GameUIRoot.Instance != null)
            {
                GameUIRoot.Instance.ForceClearReload(player.PlayerIDX);
            }
        }
    }
}
