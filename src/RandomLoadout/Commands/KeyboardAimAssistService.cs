// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using RandomLoadout.Core.Input;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed class KeyboardAimAssistService
    {
        private KeyboardAimAssistSettings _settings;
        private readonly Action<KeyboardAimAssistSettings> _persistSettings;
        private readonly KeyboardAimAssistTargetSelector _targetSelector;

        public KeyboardAimAssistService(
            KeyboardAimAssistSettings settings,
            Action<KeyboardAimAssistSettings> persistSettings,
            KeyboardAimAssistTargetSelector targetSelector)
        {
            _settings = settings ?? new KeyboardAimAssistSettings(KeyboardAimAssistMode.Off, 1f);
            _persistSettings = persistSettings;
            _targetSelector = targetSelector ?? new KeyboardAimAssistTargetSelector();
        }

        public KeyboardAimAssistMode Mode
        {
            get { return _settings.Mode; }
        }

        public float Multiplier
        {
            get { return _settings.Multiplier; }
        }

        public bool IsEnabled(PlayerController player)
        {
            return player != null && _settings.IsEnabled;
        }

        public bool TryCycleMode(PlayerController player, out KeyboardAimAssistMode mode)
        {
            mode = _settings.Mode;
            if (player == null)
            {
                return false;
            }

            KeyboardAimAssistMode nextMode;
            switch (_settings.Mode)
            {
                case KeyboardAimAssistMode.Off:
                    nextMode = KeyboardAimAssistMode.AutoAim;
                    break;
                case KeyboardAimAssistMode.AutoAim:
                    nextMode = KeyboardAimAssistMode.SuperAutoAim;
                    break;
                default:
                    nextMode = KeyboardAimAssistMode.Off;
                    break;
            }

            _settings = _settings.WithMode(nextMode);
            Persist();
            mode = nextMode;
            return true;
        }

        public bool TryCycleMultiplier(PlayerController player, out float multiplier)
        {
            multiplier = _settings.Multiplier;
            if (player == null)
            {
                return false;
            }

            float nextMultiplier;
            if (Mathf.Approximately(_settings.Multiplier, 0.5f))
            {
                nextMultiplier = 1f;
            }
            else if (Mathf.Approximately(_settings.Multiplier, 1f))
            {
                nextMultiplier = 1.5f;
            }
            else if (Mathf.Approximately(_settings.Multiplier, 1.5f))
            {
                nextMultiplier = 2f;
            }
            else
            {
                nextMultiplier = 0.5f;
            }

            _settings = _settings.WithMultiplier(nextMultiplier);
            Persist();
            multiplier = nextMultiplier;
            return true;
        }

        public float GetAimAssistDegrees()
        {
            return _settings.AimAssistDegrees;
        }

        public void TryApplyAssist(PlayerController player, ref Vector3 aimPoint)
        {
            if (!IsEnabled(player) || Time.timeScale == 0f || player.forceAimPoint.HasValue)
            {
                return;
            }

            BraveInput input = BraveInput.GetInstanceForPlayer(player.PlayerIDX);
            if (input == null || !input.IsKeyboardAndMouse())
            {
                return;
            }

            Vector2 selectedAimPoint;
            if (_targetSelector.TrySelectTarget(player, aimPoint, GetAimAssistDegrees(), out selectedAimPoint))
            {
                aimPoint = new Vector3(selectedAimPoint.x, selectedAimPoint.y, aimPoint.z);
            }
        }

        public void Reset()
        {
            _settings = _settings.WithMode(KeyboardAimAssistMode.Off);
        }

        private void Persist()
        {
            if (_persistSettings != null)
            {
                _persistSettings(_settings);
            }
        }


    }
}
