# Dashboard UI Theme Rules

This document defines how the dashboard consumes theme colors. The built-in palettes and their core values are listed in the [Theme Catalog](./ui-theme-catalog.md).

## Color Roles

`Primary` is the panel surface and the background for content rows and item icon cells. `Secondary` is the semantic accent/state color, especially for functional actions and status backgrounds. `Outline` is the general structural color.

Each theme stores nine core tokens. Derived disabled, hover, active, status, section-label, and text colors are generated centrally from those tokens. Do not add per-state HEX values to individual UI pages.

## Ordinary And Category Buttons

- Ordinary command-panel and secondary-page buttons use the non-category command-button style.
- Selected ordinary buttons use the command-panel non-group selected state, including its selected border. Controller-focused ordinary buttons instead keep their normal border and use a clearly different fill: `ControllerFocusButtonBackground` is derived by mixing the existing non-functional button background with the theme's `Secondary` accent at 50%. When a focused button is also selected, it combines that focus fill with the selected border. This deliberately reuses existing theme colors; it does not add a per-theme focus-color HEX setting. The stronger controller-focus fill is separate from the lighter pointer-hover fill.
- Functional action buttons may use `Secondary` as their background when their role requires a semantic action state.
- Disabled buttons keep their disabled fill and text colors but do not render a border.
- The main `General`, `Combat`, `Player`, and `Room` category buttons use dedicated category backgrounds, borders, and text contrast. They must not reuse ordinary command-button roles.
- Category button text is selected as pure black or pure white from the background luminance.
- Selected category buttons are drawn 4 px wider and taller, and 2 px higher than their layout rectangles. The layout gap is 6 px so the enlarged selected button does not collide with neighbors.

The Default theme retains shared neutral constants for non-functional buttons: `#BDBBB8` for their background, `#949390` for their unselected border, and `#FBF0D2` for their controller-preview/selected border. The Default theme also uses `#FBF0D2` for the control-panel inner border.

## Text And Rows

- General command-panel and Settings text uses neutral text colors from `DashboardTheme`, not button background colors.
- Nearby pickup information text outside the six colored sections uses pure black or pure white selected from the `Primary` background luminance.
- `Settings`, `Language`, and `Theme` header actions use dedicated theme-aware roles. `About` is opened from the Settings page.
- Nearby pickup information section labels (`Quality`, `Type`, `Summary`, `Effects`, `Synergies`, and `Notes`) are bold and each uses a theme-specific high-saturation HEX selected against that theme's `Primary` background. Default and Cyberpunk render these labels with a 1 px dark outline derived by strongly shading `Primary`; Snowfield, Mars Relic, and Hazard render them without an outline. Section colors and outlines do not apply to their values.
- Nearby pickup information values, stat rows, and other body text use pure black or pure white selected from the theme's `Primary` background luminance. Body text must not reuse section-label, panel-border, or category-button colors.
- Item-browser rows use `Primary` with `ItemRowBorder` at 2 px. `ItemRowBorder` reuses the theme's `ButtonSelectedBorder`, matching the selected-state border of control-panel non-category buttons. Their 6 px spacing provides additional separation. Item icon cells use the same `Primary` background.
- Start Items and Preset Detail content rows use `Primary` with a 1 px `ButtonUnselectedBorder`; their selected Preset card uses the same background with a 1 px `ButtonSelectedBorder`.
- Selected buttons on these secondary pages reuse the ordinary non-category command-panel selected state. Controller-focused buttons use the shared controller-focus fill and retain their normal border, unless they are also selected. Quantity and other small adjustment controls remain ordinary buttons.
- The item-browser `Grant` action uses `Secondary` as its functional background, `Outline` for its normal border, and `ButtonSelectedBorder` for hover/active states. Its text automatically contrasts with `Secondary`.
- Semantic status backgrounds such as success and error remain separate from the normal button palette.
- Transient success/error status bars use pure black or pure white text selected from the status background luminance.

## Borders And Panels

Panel textures use point filtering and a 32x32 source texture so the three border bands remain crisp when nine-sliced. The outer band is a shaded `Secondary`, the middle band blends shaded `Primary` with `Outline`, and the inner band uses `Outline` (the Default theme overrides it with `#FBF0D2`). Their widths are 5 px, 7 px, and 5 px.

When adding a panel, keep the texture border and the `GUIStyle.border` inset synchronized with the total border width of 17 px. Panel drawing rectangles expand outward by this amount, so the background and content bounds remain unchanged while the border is added outside them.
