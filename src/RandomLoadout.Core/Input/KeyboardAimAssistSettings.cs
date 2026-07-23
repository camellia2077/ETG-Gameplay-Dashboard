// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;

namespace RandomLoadout.Core.Input
{
    public enum KeyboardAimAssistMode
    {
        Off,
        AutoAim,
        SuperAutoAim,
    }

    public sealed class KeyboardAimAssistSettings
    {
        public KeyboardAimAssistSettings(KeyboardAimAssistMode mode, float multiplier)
        {
            Mode = mode;
            Multiplier = NormalizeMultiplier(multiplier);
        }

        public KeyboardAimAssistMode Mode { get; private set; }

        public float Multiplier { get; private set; }

        public bool IsEnabled
        {
            get { return Mode != KeyboardAimAssistMode.Off; }
        }

        public float AimAssistDegrees
        {
            get
            {
                if (Mode == KeyboardAimAssistMode.Off)
                {
                    return 0f;
                }

                return (Mode == KeyboardAimAssistMode.SuperAutoAim ? 25f : 15f) * Multiplier;
            }
        }

        public KeyboardAimAssistSettings WithMode(KeyboardAimAssistMode mode)
        {
            return new KeyboardAimAssistSettings(mode, Multiplier);
        }

        public KeyboardAimAssistSettings WithMultiplier(float multiplier)
        {
            return new KeyboardAimAssistSettings(Mode, multiplier);
        }

        public static KeyboardAimAssistSettings FromConfig(
            bool legacyEnabled,
            string legacyLevel,
            string modeValue,
            float multiplierValue)
        {
            KeyboardAimAssistMode mode = ParseMode(modeValue);
            if (mode == KeyboardAimAssistMode.Off && legacyEnabled)
            {
                mode = string.Equals(legacyLevel, "Strong", StringComparison.OrdinalIgnoreCase)
                    ? KeyboardAimAssistMode.SuperAutoAim
                    : KeyboardAimAssistMode.AutoAim;
            }

            return new KeyboardAimAssistSettings(mode, multiplierValue);
        }

        public static float NormalizeMultiplier(float value)
        {
            if (Approximately(value, 0.5f))
            {
                return 0.5f;
            }
            if (Approximately(value, 1.5f))
            {
                return 1.5f;
            }
            if (Approximately(value, 2f))
            {
                return 2f;
            }

            return 1f;
        }

        public static string GetModeConfigValue(KeyboardAimAssistMode mode)
        {
            return mode.ToString();
        }

        private static KeyboardAimAssistMode ParseMode(string value)
        {
            if (string.Equals(value, "AutoAim", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Normal", StringComparison.OrdinalIgnoreCase))
            {
                return KeyboardAimAssistMode.AutoAim;
            }
            if (string.Equals(value, "SuperAutoAim", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "Super", StringComparison.OrdinalIgnoreCase))
            {
                return KeyboardAimAssistMode.SuperAutoAim;
            }

            return KeyboardAimAssistMode.Off;
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) < 0.001f;
        }
    }
}
