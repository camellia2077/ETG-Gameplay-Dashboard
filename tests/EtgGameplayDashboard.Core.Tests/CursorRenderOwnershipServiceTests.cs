// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using EtgGameplayDashboard.Core.Cursor;

namespace EtgGameplayDashboard.Core.Tests
{
    internal static class CursorRenderOwnershipServiceTests
    {
        public static void PrimaryMouseOwnsCursorWithPanelClosed()
        {
            CursorRenderDecision decision = Evaluate(false, true, false, false);

            AssertEx.Equal(CursorInputOwner.Primary, decision.InputOwner, "P1 mouse should own the cursor.");
            AssertEx.Equal(CursorColorMode.DefaultWhite, decision.ColorMode, "Disabled custom color should be white.");
            AssertEx.True(decision.PluginOwnsRender, "Plugin should own the cursor when P1 has a mouse.");
        }

        public static void SecondaryMouseOwnsCursorWithPanelOpenOrClosed()
        {
            CursorRenderDecision openDecision = Evaluate(true, false, true, false);
            CursorRenderDecision closedDecision = Evaluate(false, false, true, false);

            AssertEx.Equal(CursorInputOwner.Secondary, openDecision.InputOwner, "P2 mouse should own the open-panel cursor.");
            AssertEx.Equal(CursorInputOwner.Secondary, closedDecision.InputOwner, "P2 mouse should own the closed-panel cursor.");
            AssertEx.True(openDecision.PluginOwnsRender, "P2 mouse should be plugin-owned while the panel is open.");
            AssertEx.True(closedDecision.PluginOwnsRender, "P2 mouse should remain plugin-owned after closing the panel.");
        }

        public static void PrimaryMouseWinsWhenBothPlayersHaveMouse()
        {
            CursorRenderDecision decision = Evaluate(false, true, true, false);

            AssertEx.Equal(CursorInputOwner.Primary, decision.InputOwner, "P1 should remain the stable owner when both inputs report a mouse.");
        }

        public static void CustomColorDoesNotChangeInputOwner()
        {
            CursorRenderDecision decision = Evaluate(false, false, true, true);

            AssertEx.Equal(CursorInputOwner.Secondary, decision.InputOwner, "Custom color must not change the mouse owner.");
            AssertEx.Equal(CursorColorMode.Custom, decision.ColorMode, "Custom color should be preserved independently.");
        }

        public static void NoMouseReturnsOwnershipToNativeRenderer()
        {
            CursorRenderDecision decision = Evaluate(false, false, false, false);

            AssertEx.Equal(CursorInputOwner.None, decision.InputOwner, "No mouse should have no cursor owner.");
            AssertEx.True(!decision.PluginOwnsRender, "Plugin should not render without a mouse input.");
        }

        private static CursorRenderDecision Evaluate(bool panelVisible, bool primaryHasMouse, bool secondaryHasMouse, bool customColorEnabled)
        {
            CursorRenderOwnershipService service = new CursorRenderOwnershipService();
            return service.Evaluate(panelVisible, primaryHasMouse, secondaryHasMouse, customColorEnabled, true);
        }
    }
}
