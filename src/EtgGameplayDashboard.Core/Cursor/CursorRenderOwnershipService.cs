// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard.Core.Cursor
{
    public enum CursorInputOwner
    {
        None = 0,
        Primary = 1,
        Secondary = 2,
    }

    public enum CursorColorMode
    {
        DefaultWhite = 0,
        Custom = 1,
    }

    public sealed class CursorRenderDecision
    {
        public CursorRenderDecision(CursorInputOwner inputOwner, CursorColorMode colorMode, bool pluginOwnsRender)
        {
            InputOwner = inputOwner;
            ColorMode = colorMode;
            PluginOwnsRender = pluginOwnsRender;
        }

        public CursorInputOwner InputOwner { get; private set; }
        public CursorColorMode ColorMode { get; private set; }
        public bool PluginOwnsRender { get; private set; }
    }

    public sealed class CursorRenderOwnershipService
    {
        public CursorRenderDecision Evaluate(
            bool panelVisible,
            bool primaryHasMouse,
            bool secondaryHasMouse,
            bool customColorEnabled,
            bool nativeOverrideAvailable)
        {
            CursorInputOwner inputOwner = primaryHasMouse
                ? CursorInputOwner.Primary
                : secondaryHasMouse
                    ? CursorInputOwner.Secondary
                    : CursorInputOwner.None;
            CursorColorMode colorMode = customColorEnabled
                ? CursorColorMode.Custom
                : CursorColorMode.DefaultWhite;

            // Panel visibility does not decide cursor ownership. The player that owns
            // the mouse does, so closing the panel cannot hand control back to ETG's
            // player-specific (for example, purple P2) cursor renderer.
            bool pluginOwnsRender = inputOwner != CursorInputOwner.None && nativeOverrideAvailable;
            return new CursorRenderDecision(inputOwner, colorMode, pluginOwnsRender);
        }
    }
}
