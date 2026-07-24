// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed class CombatCursorColorOption
    {
        public CombatCursorColorOption(string id, string displayNameKey, string hex, Color color)
        {
            Id = id ?? string.Empty;
            DisplayNameKey = displayNameKey ?? string.Empty;
            Hex = hex ?? string.Empty;
            Color = color;
        }

        public string Id { get; private set; }

        public string DisplayNameKey { get; private set; }

        public string Hex { get; private set; }

        public Color Color { get; private set; }
    }

    internal static class CombatCursorColorCatalog
    {
        public const string DisabledId = "off";
        public const string DefaultPresetId = "preset_01";

        private static readonly CombatCursorColorOption[] Options =
        {
            new CombatCursorColorOption("preset_01", "gui.combat.cursor_color.option.cyan", "#00E5FF", new Color(0f, 0.898f, 1f, 1f)),
            new CombatCursorColorOption("preset_02", "gui.combat.cursor_color.option.lime", "#39FF14", new Color(0.224f, 1f, 0.078f, 1f)),
            new CombatCursorColorOption("preset_03", "gui.combat.cursor_color.option.yellow", "#FFF000", new Color(1f, 0.941f, 0f, 1f)),
            new CombatCursorColorOption("preset_04", "gui.combat.cursor_color.option.pink", "#FF1493", new Color(1f, 0.078f, 0.576f, 1f)),
            new CombatCursorColorOption("preset_05", "gui.combat.cursor_color.option.red", "#FF0000", Color.red),
            new CombatCursorColorOption("preset_06", "gui.combat.cursor_color.option.orange", "#FF8C00", new Color(1f, 0.549f, 0f, 1f)),
            new CombatCursorColorOption("preset_07", "gui.combat.cursor_color.option.violet", "#9900FF", new Color(0.6f, 0f, 1f, 1f)),
            new CombatCursorColorOption("preset_08", "gui.combat.cursor_color.option.electric_blue", "#0066FF", new Color(0f, 0.4f, 1f, 1f)),
        };

        public static CombatCursorColorOption[] GetOptions()
        {
            return Options;
        }

        public static string Normalize(string id)
        {
            string normalized = id == null ? string.Empty : id.Trim().ToLowerInvariant();
            if (string.Equals(normalized, DisabledId, System.StringComparison.OrdinalIgnoreCase))
            {
                return DisabledId;
            }

            for (int index = 0; index < Options.Length; index++)
            {
                if (string.Equals(Options[index].Id, normalized, System.StringComparison.OrdinalIgnoreCase))
                {
                    return Options[index].Id;
                }
            }

            return Options[0].Id;
        }

        public static bool IsEnabled(string id)
        {
            return !string.Equals(Normalize(id), DisabledId, System.StringComparison.OrdinalIgnoreCase);
        }

        public static CombatCursorColorOption Get(string id)
        {
            string normalized = Normalize(id);
            for (int index = 0; index < Options.Length; index++)
            {
                if (string.Equals(Options[index].Id, normalized, System.StringComparison.OrdinalIgnoreCase))
                {
                    return Options[index];
                }
            }

            return Options[0];
        }

    }
}
