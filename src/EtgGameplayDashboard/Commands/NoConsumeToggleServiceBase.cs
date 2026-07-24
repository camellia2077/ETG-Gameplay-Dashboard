// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    internal abstract class NoConsumeToggleServiceBase
    {
        private bool _isEnabled;
        private PlayerController _trackedPlayer;
        private float _trackedValue;

        public bool IsEnabled
        {
            get { return _isEnabled; }
        }

        protected abstract string EnableResultKey { get; }

        protected abstract string DisableResultKey { get; }

        public GrantCommandExecutionResult Toggle(PlayerController player)
        {
            if (_isEnabled)
            {
                _isEnabled = false;
                ClearTrackedState();
                OnDisabled();
                return GrantCommandExecutionResult.Localized(true, DisableResultKey);
            }

            GrantCommandExecutionResult validationResult;
            if (!TryPrepareForEnable(player, out validationResult))
            {
                return validationResult;
            }

            OnEnabled(player);
            _isEnabled = true;
            CaptureTrackedState(player);
            return GrantCommandExecutionResult.Localized(true, EnableResultKey);
        }

        public void Update(PlayerController player)
        {
            if (!_isEnabled || player == null)
            {
                return;
            }

            float currentValue;
            if (!TryGetCurrentValue(player, out currentValue))
            {
                return;
            }

            if (!ReferenceEquals(_trackedPlayer, player))
            {
                CaptureTrackedState(player, currentValue);
                return;
            }

            if (currentValue < _trackedValue)
            {
                SetCurrentValue(player, _trackedValue);
            }
            else if (currentValue > _trackedValue)
            {
                _trackedValue = currentValue;
            }
        }

        public void Reset()
        {
            _isEnabled = false;
            ClearTrackedState();
            OnDisabled();
        }

        protected abstract bool TryPrepareForEnable(PlayerController player, out GrantCommandExecutionResult failureResult);

        protected virtual void OnEnabled(PlayerController player)
        {
        }

        protected virtual void OnDisabled()
        {
        }

        protected abstract bool TryGetCurrentValue(PlayerController player, out float currentValue);

        protected abstract void SetCurrentValue(PlayerController player, float value);

        private void CaptureTrackedState(PlayerController player)
        {
            float currentValue;
            if (!TryGetCurrentValue(player, out currentValue))
            {
                ClearTrackedState();
                return;
            }

            CaptureTrackedState(player, currentValue);
        }

        private void CaptureTrackedState(PlayerController player, float currentValue)
        {
            _trackedPlayer = player;
            _trackedValue = currentValue;
        }

        private void ClearTrackedState()
        {
            _trackedPlayer = null;
            _trackedValue = 0f;
        }
    }
}
