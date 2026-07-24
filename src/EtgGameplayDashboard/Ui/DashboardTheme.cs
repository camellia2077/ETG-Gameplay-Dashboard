// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed class DashboardThemePalette
    {
        public Color Primary;
        public Color Secondary;
        public Color Outline;
        public Color CategoryUnselected;
        public Color CategorySelected;
        public Color ButtonUnselectedBorder;
        public Color ButtonSelectedBorder;
        public Color CategoryUnselectedBorder;
        public Color CategorySelectedBorder;
        public Color PanelBackground;
        public Color PanelRowBackground;
        public Color ItemRowBorder;
        public Color PanelOuterBorder;
        public Color PanelMiddleBorder;
        public Color PanelInnerBorder;
        public Color InputBackground;
        public Color ButtonBackground;
        public Color ButtonBorder;
        public Color ButtonHoverBackground;
        public Color ButtonActiveBackground;
        public Color EnabledButtonBackground;
        public Color EnabledButtonHoverBackground;
        public Color EnabledButtonActiveBackground;
        public Color EnabledButtonText;
        public Color PickupRowHoverBackground;
        public Color PickupRowActiveBackground;
        public Color CommandCategoryNormalBackground;
        public Color CommandCategoryNormalBorder;
        public Color CommandCategoryHoverBackground;
        public Color CommandCategoryHoverBorder;
        public Color CommandCategorySelectedBackground;
        public Color CommandCategorySelectedBorder;
        public Color CommandCategorySelectedText;
        public Color DisabledButtonBackground;
        public Color PrimaryText;
        public Color SecondaryText;
        public Color SuccessBackground;
        public Color ErrorBackground;
        public Color ModalOverlay;
        public Color PickupInfoTitle;
        public Color PickupInfoBody;
        public Color PickupInfoQualityLabel;
        public Color PickupInfoTypeLabel;
        public Color PickupInfoSummaryLabel;
        public Color PickupInfoEffectsLabel;
        public Color PickupInfoSynergiesLabel;
        public Color PickupInfoNotesLabel;
        public Color ScrollThumb;

        public DashboardThemePalette(
            string primaryHex,
            string secondaryHex,
            string outlineHex,
            string categoryUnselectedHex,
            string categorySelectedHex,
            string buttonUnselectedBorderHex,
            string buttonSelectedBorderHex,
            string categoryUnselectedBorderHex,
            string categorySelectedBorderHex,
            string pickupInfoQualityLabelHex,
            string pickupInfoTypeLabelHex,
            string pickupInfoSummaryLabelHex,
            string pickupInfoEffectsLabelHex,
            string pickupInfoSynergiesLabelHex,
            string pickupInfoNotesLabelHex)
        {
            Primary = Hex(primaryHex);
            Secondary = Hex(secondaryHex);
            Outline = Hex(outlineHex);
            CategoryUnselected = Hex(categoryUnselectedHex);
            CategorySelected = Hex(categorySelectedHex);
            ButtonUnselectedBorder = Hex(buttonUnselectedBorderHex);
            ButtonSelectedBorder = Hex(buttonSelectedBorderHex);
            CategoryUnselectedBorder = Hex(categoryUnselectedBorderHex);
            CategorySelectedBorder = Hex(categorySelectedBorderHex);

            Color lightText = Tint(Primary, 0.86f);
            Color darkText = Shade(Primary, 0.72f);
            bool primaryIsLight = Luminance(Primary) >= 0.5f;
            Color surfaceText = primaryIsLight ? darkText : lightText;
            Color secondaryText = primaryIsLight ? Shade(Primary, 0.52f) : Tint(Primary, 0.52f);
            Color secondaryTextColor = Luminance(Secondary) >= 0.5f ? darkText : lightText;

            PanelBackground = Primary;
            PanelRowBackground = Mix(Primary, Outline, 0.16f);
            ItemRowBorder = ButtonSelectedBorder;
            PanelOuterBorder = Shade(Secondary, 0.36f);
            PanelMiddleBorder = Mix(Shade(Primary, 0.48f), Outline, 0.20f);
            PanelInnerBorder = Outline;
            InputBackground = Mix(Primary, Secondary, 0.14f);
            ButtonBackground = Mix(Primary, Secondary, 0.20f);
            ButtonBorder = ButtonUnselectedBorder;
            ButtonHoverBackground = Tint(ButtonBackground, 0.14f);
            ButtonActiveBackground = Shade(ButtonBackground, 0.18f);
            EnabledButtonBackground = Secondary;
            EnabledButtonHoverBackground = Tint(Secondary, 0.12f);
            EnabledButtonActiveBackground = Shade(Secondary, 0.18f);
            EnabledButtonText = secondaryTextColor;
            PickupRowHoverBackground = ButtonHoverBackground;
            PickupRowActiveBackground = ButtonActiveBackground;
            CommandCategoryNormalBackground = CategoryUnselected;
            CommandCategoryNormalBorder = CategoryUnselectedBorder;
            CommandCategoryHoverBackground = CommandCategoryNormalBackground;
            CommandCategoryHoverBorder = Outline;
            CommandCategorySelectedBackground = CategorySelected;
            CommandCategorySelectedBorder = CategorySelectedBorder;
            CommandCategorySelectedText = secondaryTextColor;
            DisabledButtonBackground = Mix(Shade(Primary, 0.38f), Secondary, 0.12f);
            PrimaryText = surfaceText;
            SecondaryText = secondaryText;
            SuccessBackground = Secondary;
            ErrorBackground = Shade(Secondary, 0.24f);
            ModalOverlay = new Color(0f, 0f, 0f, 0.56f);
            PickupInfoTitle = primaryIsLight ? Color.black : Color.white;
            PickupInfoBody = primaryIsLight ? Color.black : Color.white;
            PickupInfoQualityLabel = Hex(pickupInfoQualityLabelHex);
            PickupInfoTypeLabel = Hex(pickupInfoTypeLabelHex);
            PickupInfoSummaryLabel = Hex(pickupInfoSummaryLabelHex);
            PickupInfoEffectsLabel = Hex(pickupInfoEffectsLabelHex);
            PickupInfoSynergiesLabel = Hex(pickupInfoSynergiesLabelHex);
            PickupInfoNotesLabel = Hex(pickupInfoNotesLabelHex);
            ScrollThumb = Outline;
        }

        private static Color Hex(string value)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(value, out color) ? color : Color.magenta;
        }

        private static Color Mix(Color first, Color second, float amount)
        {
            return Color.Lerp(first, second, amount);
        }

        private static Color Tint(Color color, float amount)
        {
            return Color.Lerp(color, Color.white, amount);
        }

        private static Color Shade(Color color, float amount)
        {
            return Color.Lerp(color, Color.black, amount);
        }

        private static float Luminance(Color color)
        {
            return (color.r * 0.2126f) + (color.g * 0.7152f) + (color.b * 0.0722f);
        }
    }

    internal static class DashboardTheme
    {
        internal const float PanelBorderThickness = 17f;

        private static DashboardThemePalette _current = new DashboardThemePalette(
            "#ADA9A4", "#8C1C13", "#F2AA3F", "#8C1C13", "#972CCE",
            "#949390", "#FBF0D2", "#F2AA3F", "#FFFFFF",
            "#8B1E3F", "#0B4F6C", "#2E7D32", "#9A3412", "#00695C", "#6A1B9A");
        private static string _currentId = "theme1";
        private static readonly Color DefaultNeutralButtonBackground = new Color(0.74f, 0.73f, 0.72f, 1f);
        private static readonly Color SharedNeutralButtonBackground = new Color(0.74f, 0.73f, 0.72f, 1f); // #BDBBB8
        private static readonly Color SharedNeutralButtonBorder = new Color(0.58f, 0.58f, 0.56f, 1f); // #949390
        private static readonly Color DefaultNeutralButtonSelectedBorder = new Color(0.98f, 0.94f, 0.82f, 1f);
        internal static string CurrentId { get { return _currentId; } }
        internal static Color NonFunctionalButtonBackground { get { return _currentId == "theme1" ? DefaultNeutralButtonBackground : _current.ButtonBackground; } }
        internal static Color NeutralButtonBackground { get { return SharedNeutralButtonBackground; } }
        internal static Color NeutralButtonBorder { get { return SharedNeutralButtonBorder; } }
        internal static Color NeutralButtonSelectedBorder { get { return DefaultNeutralButtonSelectedBorder; } }
        internal static Color HeaderActionBackground { get { return _currentId == "theme1" ? SharedNeutralButtonBackground : _current.ButtonBackground; } }
        internal static Color HeaderActionBorder { get { return _current.ButtonUnselectedBorder; } }
        internal static Color HeaderActionHoverBackground { get { return _currentId == "theme1" ? SharedNeutralButtonBackground : _current.ButtonHoverBackground; } }
        internal static Color HeaderActionHoverBorder { get { return _current.ButtonSelectedBorder; } }
        // Controller focus is deliberately stronger than pointer hover.  It reuses the
        // existing Secondary accent rather than adding a per-theme focus-color setting.
        internal static Color ControllerFocusButtonBackground { get { return Color.Lerp(NonFunctionalButtonBackground, _current.Secondary, 0.50f); } }
        internal static Color NonFunctionalButtonBorder { get { return _current.ButtonUnselectedBorder; } }
        internal static Color NonFunctionalButtonSelectedBorder { get { return _current.ButtonSelectedBorder; } }

        internal static Color PanelBackground { get { return _current.PanelBackground; } }
        internal static Color Secondary { get { return _current.Secondary; } }
        internal static Color Outline { get { return _current.Outline; } }
        internal static Color PanelRowBackground { get { return _current.PanelRowBackground; } }
        internal static Color ItemRowBorder { get { return _current.ItemRowBorder; } }
        internal static Color PanelOuterBorder { get { return _current.PanelOuterBorder; } }
        internal static Color PanelMiddleBorder { get { return _current.PanelMiddleBorder; } }
        internal static Color PanelInnerBorder { get { return _currentId == "theme1" ? DefaultNeutralButtonSelectedBorder : _currentId == "theme4" ? Color.Lerp(_current.Primary, Color.white, 0.35f) : _current.PanelInnerBorder; } }
        internal static Color InputBackground { get { return _current.InputBackground; } }
        internal static Color ButtonBackground { get { return _current.ButtonBackground; } }
        internal static Color ButtonBorder { get { return _current.ButtonUnselectedBorder; } }
        internal static Color ButtonSelectedBorder { get { return _current.ButtonSelectedBorder; } }
        internal static Color ButtonHoverBackground { get { return _current.ButtonHoverBackground; } }
        internal static Color ButtonActiveBackground { get { return _current.ButtonActiveBackground; } }
        internal static Color EnabledButtonBackground { get { return _current.EnabledButtonBackground; } }
        internal static Color EnabledButtonHoverBackground { get { return _current.EnabledButtonHoverBackground; } }
        internal static Color EnabledButtonActiveBackground { get { return _current.EnabledButtonActiveBackground; } }
        internal static Color EnabledButtonText { get { return _current.EnabledButtonText; } }
        internal static Color PickupRowHoverBackground { get { return _current.PickupRowHoverBackground; } }
        internal static Color PickupRowActiveBackground { get { return _current.PickupRowActiveBackground; } }
        internal static Color CommandCategoryNormalBackground { get { return _current.CategoryUnselected; } }
        internal static Color CommandCategoryNormalBorder { get { return _current.CategoryUnselectedBorder; } }
        internal static Color CommandCategoryHoverBackground { get { return _current.CommandCategoryHoverBackground; } }
        internal static Color CommandCategoryHoverBorder { get { return _current.CommandCategoryHoverBorder; } }
        internal static Color CommandCategorySelectedBackground { get { return _current.CategorySelected; } }
        internal static Color CommandCategorySelectedBorder { get { return _current.CategorySelectedBorder; } }
        internal static Color CommandCategoryNormalText { get { return GetContrastingText(CommandCategoryNormalBackground); } }
        internal static Color CommandCategorySelectedText { get { return GetContrastingText(CommandCategorySelectedBackground); } }
        internal static Color DisabledButtonBackground { get { return _current.DisabledButtonBackground; } }
        internal static Color PrimaryText { get { return _current.PrimaryText; } }
        internal static Color SecondaryText { get { return _current.SecondaryText; } }
        internal static Color SuccessBackground { get { return _current.SuccessBackground; } }
        internal static Color SuccessText { get { return GetContrastingText(_current.SuccessBackground); } }
        internal static Color ErrorBackground { get { return _current.ErrorBackground; } }
        internal static Color ErrorText { get { return GetContrastingText(_current.ErrorBackground); } }
        internal static Color ModalOverlay { get { return _current.ModalOverlay; } }
        internal static Color PickupInfoTitle { get { return _current.PickupInfoTitle; } }
        internal static Color PickupInfoBody { get { return _current.PickupInfoBody; } }
        internal static bool UsePickupInfoLabelOutline { get { return _currentId != "theme4" && _currentId != "theme2" && _currentId != "theme5"; } }
        internal static Color PickupInfoLabelOutline { get { return Color.Lerp(_current.Primary, Color.black, 0.82f); } }
        internal static Color PickupInfoQualityLabel { get { return _current.PickupInfoQualityLabel; } }
        internal static Color PickupInfoTypeLabel { get { return _current.PickupInfoTypeLabel; } }
        internal static Color PickupInfoSummaryLabel { get { return _current.PickupInfoSummaryLabel; } }
        internal static Color PickupInfoEffectsLabel { get { return _current.PickupInfoEffectsLabel; } }
        internal static Color PickupInfoSynergiesLabel { get { return _current.PickupInfoSynergiesLabel; } }
        internal static Color PickupInfoNotesLabel { get { return _current.PickupInfoNotesLabel; } }
        internal static Color ScrollThumb { get { return _current.ScrollThumb; } }

        internal static Color GetContrastingText(Color background)
        {
            float luminance = (background.r * 0.2126f) + (background.g * 0.7152f) + (background.b * 0.0722f);
            return luminance >= 0.5f ? Color.black : Color.white;
        }

        internal static void Select(string themeId)
        {
            _currentId = string.Equals(themeId, "theme5", System.StringComparison.OrdinalIgnoreCase)
                ? "theme5"
                : string.Equals(themeId, "theme4", System.StringComparison.OrdinalIgnoreCase)
                    ? "theme4"
                    : string.Equals(themeId, "theme3", System.StringComparison.OrdinalIgnoreCase)
                        ? "theme3"
                        : string.Equals(themeId, "theme2", System.StringComparison.OrdinalIgnoreCase) ? "theme2" : "theme1";
            _current = string.Equals(themeId, "theme5", System.StringComparison.OrdinalIgnoreCase)
                ? new DashboardThemePalette(
                    "#E0E0E0", "#D64011", "#3B444B", "#F2B39D", "#D64011",
                    "#9BA3A8", "#3B444B", "#D64011", "#FFFFFF",
                    "#C11540", "#11699C", "#0D7321", "#A14B12", "#0C6E64", "#9612E2")
                : string.Equals(themeId, "theme4", System.StringComparison.OrdinalIgnoreCase)
            ? new DashboardThemePalette(
                    "#E5E9F0", "#4C566A", "#2E3440", "#D8DEE9", "#4C566A",
                    "#B8C1D1", "#4C566A", "#81A1C1", "#FFFFFF",
                    "#D01141", "#0D6DA5", "#0D7722", "#AE4B09", "#0A766B", "#9E18EC")
                    : string.Equals(themeId, "theme3", System.StringComparison.OrdinalIgnoreCase)
                        ? new DashboardThemePalette(
                            "#2D005F", "#00FF00", "#BD00FF", "#66FF66", "#00FF00",
                            "#6F3FAF", "#00FF00", "#BD00FF", "#2D005F",
                            "#FFFF00", "#00D9FF", "#FF4DFF", "#7CFF00", "#00FFFF", "#FF8C00")
                        : string.Equals(themeId, "theme2", System.StringComparison.OrdinalIgnoreCase)
                            ? new DashboardThemePalette(
                                "#E8E2D0", "#9E1B1B", "#C78C25", "#D8C9B8", "#9E1B1B",
                                "#A68F78", "#F2E5C7", "#C78C25", "#FFF3D6",
                                "#C51642", "#0D6AA0", "#0D7321", "#A54A0D", "#097167", "#9819E1")
                            : new DashboardThemePalette(
                                "#ADA9A4", "#8C1C13", "#F2AA3F", "#8C1C13", "#972CCE",
                                "#949390", "#FBF0D2", "#F2AA3F", "#FFFFFF",
                                "#DE5F75", "#45B6DB", "#63DD70", "#DE9642", "#45D7C1", "#D144DB");
        }
    }
}
