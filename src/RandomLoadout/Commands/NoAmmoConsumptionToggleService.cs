namespace RandomLoadout
{
    internal sealed class NoAmmoConsumptionToggleService
    {
        private bool _isEnabled;

        public bool IsEnabled
        {
            get { return _isEnabled; }
        }

        public GrantCommandExecutionResult Toggle()
        {
            _isEnabled = !_isEnabled;
            return _isEnabled
                ? GrantCommandExecutionResult.Localized(true, "result.no_ammo_consumption.enable.success")
                : GrantCommandExecutionResult.Localized(true, "result.no_ammo_consumption.disable.success");
        }

        public void Update(PlayerController player)
        {
            if (!_isEnabled || (object)player == null)
            {
                return;
            }

            Gun currentGun = player.CurrentGun;
            if ((object)currentGun == null || currentGun.InfiniteAmmo)
            {
                return;
            }

            int targetAmmo = currentGun.AdjustedMaxAmmo;
            if (targetAmmo > 0 && currentGun.CurrentAmmo < targetAmmo)
            {
                currentGun.CurrentAmmo = targetAmmo;
            }

            if (currentGun.ClipCapacity > 0 && currentGun.ClipShotsRemaining < currentGun.ClipCapacity)
            {
                currentGun.ClipShotsRemaining = currentGun.ClipCapacity;
            }
        }

        public void Reset()
        {
            _isEnabled = false;
        }
    }
}
