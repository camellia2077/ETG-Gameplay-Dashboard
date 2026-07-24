// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;

namespace EtgGameplayDashboard
{
    internal sealed class DashboardThemeOption
    {
        public DashboardThemeOption(string id, string displayNameKey)
        {
            Id = id ?? string.Empty;
            DisplayNameKey = displayNameKey ?? string.Empty;
        }

        public string Id { get; private set; }

        public string DisplayNameKey { get; private set; }
    }

    internal static class DashboardThemeCatalog
    {
        public const string DefaultThemeId = "theme1";

        private static readonly DashboardThemeOption[] Options =
        {
            new DashboardThemeOption(DefaultThemeId, "gui.theme.option.default"),
            new DashboardThemeOption("theme4", "gui.theme.option.snowfield"),
            new DashboardThemeOption("theme2", "gui.theme.option.mars_relic"),
            new DashboardThemeOption("theme3", "gui.theme.option.cyberpunk"),
            new DashboardThemeOption("theme5", "gui.theme.option.industrial_warning"),
        };

        public static string Normalize(string id)
        {
            string normalized = id == null ? string.Empty : id.Trim().ToLowerInvariant();
            for (int index = 0; index < Options.Length; index++)
            {
                if (string.Equals(Options[index].Id, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return Options[index].Id;
                }
            }

            return DefaultThemeId;
        }

        public static string GetNext(string id)
        {
            string normalized = Normalize(id);
            for (int index = 0; index < Options.Length; index++)
            {
                if (string.Equals(Options[index].Id, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return Options[(index + 1) % Options.Length].Id;
                }
            }

            return DefaultThemeId;
        }

        public static string GetDisplayName(string id)
        {
            string normalized = Normalize(id);
            for (int index = 0; index < Options.Length; index++)
            {
                if (string.Equals(Options[index].Id, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return GuiText.Get(Options[index].DisplayNameKey);
                }
            }

            return GuiText.Get("gui.theme.option.default");
        }
    }
}
