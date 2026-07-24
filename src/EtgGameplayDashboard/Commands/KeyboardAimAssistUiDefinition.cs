// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using EtgGameplayDashboard.Core.Input;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal static class KeyboardAimAssistUiDefinition
    {
        public const string SettingControlId = "cmd.combat.keyboard_aim_assist";
        public const string ModeControlId = SettingControlId + ".mode";
        public const string MultiplierControlId = SettingControlId + ".multiplier";
        public const string SettingLabelKey = "gui.command.setting.keyboard_aim_assist";

        public static string GetModeStatusKey(KeyboardAimAssistMode mode)
        {
            switch (mode)
            {
                case KeyboardAimAssistMode.AutoAim:
                    return "gui.command.status.keyboard_aim_assist.auto_aim";
                case KeyboardAimAssistMode.SuperAutoAim:
                    return "gui.command.status.keyboard_aim_assist.super_auto_aim";
                default:
                    return "gui.command.status.keyboard_aim_assist.off";
            }
        }

        public static string GetMultiplierStatusKey(float multiplier)
        {
            if (Mathf.Approximately(multiplier, 0.5f))
            {
                return "gui.command.status.keyboard_aim_assist.multiplier.05";
            }
            if (Mathf.Approximately(multiplier, 1.5f))
            {
                return "gui.command.status.keyboard_aim_assist.multiplier.15";
            }
            if (Mathf.Approximately(multiplier, 2f))
            {
                return "gui.command.status.keyboard_aim_assist.multiplier.2";
            }

            return "gui.command.status.keyboard_aim_assist.multiplier.1";
        }

        public static string GetModeResultKey(KeyboardAimAssistMode mode)
        {
            switch (mode)
            {
                case KeyboardAimAssistMode.AutoAim:
                    return "result.keyboard_aim_assist.mode.auto_aim";
                case KeyboardAimAssistMode.SuperAutoAim:
                    return "result.keyboard_aim_assist.mode.super_auto_aim";
                default:
                    return "result.keyboard_aim_assist.mode.off";
            }
        }

        public static string GetMultiplierResultKey(float multiplier)
        {
            if (Mathf.Approximately(multiplier, 0.5f))
            {
                return "result.keyboard_aim_assist.multiplier.05";
            }
            if (Mathf.Approximately(multiplier, 1.5f))
            {
                return "result.keyboard_aim_assist.multiplier.15";
            }
            if (Mathf.Approximately(multiplier, 2f))
            {
                return "result.keyboard_aim_assist.multiplier.2";
            }

            return "result.keyboard_aim_assist.multiplier.1";
        }
    }
}
