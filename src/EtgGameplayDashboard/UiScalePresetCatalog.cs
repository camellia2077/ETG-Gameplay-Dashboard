// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;

namespace EtgGameplayDashboard
{
    internal static class UiScalePresetCatalog
    {
        public const string DefaultPreset = "large";

        private static readonly string[] PresetNames =
        {
            "x-small",
            "small",
            "medium-small",
            "medium",
            "medium-large",
            "large",
            "x-large",
            "xx-large",
        };

        public static string Normalize(string presetName)
        {
            string normalized = string.IsNullOrEmpty(presetName)
                ? DefaultPreset
                : presetName.Trim().ToLowerInvariant();

            for (int i = 0; i < PresetNames.Length; i++)
            {
                if (string.Equals(PresetNames[i], normalized, StringComparison.Ordinal))
                {
                    return PresetNames[i];
                }
            }

            return DefaultPreset;
        }

        public static string GetNext(string currentPreset)
        {
            string normalized = Normalize(currentPreset);
            for (int i = 0; i < PresetNames.Length; i++)
            {
                if (string.Equals(PresetNames[i], normalized, StringComparison.Ordinal))
                {
                    return PresetNames[(i + 1) % PresetNames.Length];
                }
            }

            return DefaultPreset;
        }

        public static float GetScaleMultiplier(string presetName)
        {
            switch (Normalize(presetName))
            {
                case "x-small":
                    return 0.92f;
                case "small":
                    return 1.00f;
                case "medium-small":
                    return 1.04f;
                case "medium":
                    return 1.08f;
                case "medium-large":
                    return 1.12f;
                case "x-large":
                    return 1.22f;
                case "xx-large":
                    return 1.28f;
                case "large":
                default:
                    return 1.15f;
            }
        }

        public static string GetDisplayName(string presetName)
        {
            switch (Normalize(presetName))
            {
                case "x-small":
                    return GetLocalizedText("label.ui_scale.x_small", "Tiny", "极小");
                case "small":
                    return GetLocalizedText("label.ui_scale.small", "Small", "小");
                case "medium-small":
                    return GetLocalizedText("label.ui_scale.medium_small", "Mid-S", "中小");
                case "medium":
                    return GetLocalizedText("label.ui_scale.medium", "Mid", "中");
                case "medium-large":
                    return GetLocalizedText("label.ui_scale.medium_large", "Mid-L", "中大");
                case "x-large":
                    return GetLocalizedText("label.ui_scale.x_large", "XL", "超大");
                case "xx-large":
                    return GetLocalizedText("label.ui_scale.xx_large", "XXL", "特大");
                case "large":
                default:
                    return GetLocalizedText("label.ui_scale.large", "Large", "大");
            }
        }

        public static string GetEnglishDisplayName(string presetName)
        {
            switch (Normalize(presetName))
            {
                case "x-small":
                    return "Tiny";
                case "small":
                    return "Small";
                case "medium-small":
                    return "Mid-S";
                case "medium":
                    return "Mid";
                case "medium-large":
                    return "Mid-L";
                case "x-large":
                    return "XL";
                case "xx-large":
                    return "XXL";
                case "large":
                default:
                    return "Large";
            }
        }

        private static string GetLocalizedText(string key, string englishFallback, string simplifiedChineseFallback)
        {
            string value = GuiText.Get(key);
            if (!string.Equals(value, key, StringComparison.Ordinal))
            {
                return value;
            }

            return string.Equals(GuiText.CurrentLanguageCode, "zh-CN", StringComparison.OrdinalIgnoreCase)
                ? simplifiedChineseFallback
                : englishFallback;
        }
    }
}
