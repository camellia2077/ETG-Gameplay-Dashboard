// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace EtgGameplayDashboard
{
    public sealed partial class Plugin
    {
        private static readonly string[] PickupWikiTipSectionLabels = new[]
        {
            "Effects:",
            "Notes:",
            "Trivia:",
            "效果：",
            "备注：",
            "趣味冷知识："
        };

        private const float PickupWikiTipReferenceScreenWidth = 1920f;
        private const float PickupWikiTipReferenceScreenHeight = 1080f;
        private const float PickupWikiTipMinimumScale = 0.75f;
        private const float PickupWikiTipMaximumScale = 1.35f;
        private const float PickupWikiTipMaxWidth = 520f;
        private const float PickupWikiTipMinHeight = 180f;
        private const float PickupWikiTipMaxHeight = 760f;
        private const float PickupWikiTipMarginTop = 220f;
        private const float PickupWikiTipMarginRight = 60f;
        private const float PickupWikiTipMarginBottom = 120f;
        private const int PickupWikiTipTitleFontSize = 26;
        private const int PickupWikiTipSectionLabelFontSize = 22;
        private const int PickupWikiTipBodyFontSize = 18;
        private const float PickupWikiTipSectionLabelValueGap = 2f;
        private const float PickupWikiTipSectionLabelOutlineSize = 1f;

        private sealed class PickupInfoTextBlock
        {
            public PickupInfoTextBlock(string sectionKey, string label, string text, int labelFontSize, int fontSize, float spacingBefore)
            {
                SectionKey = sectionKey ?? string.Empty;
                Label = label ?? string.Empty;
                Text = text ?? string.Empty;
                LabelFontSize = labelFontSize;
                FontSize = fontSize;
                SpacingBefore = spacingBefore;
            }

            // SectionKey selects only the localized label and its theme color.
            // It must never affect Text or the neutral body color used to render Text.
            public string SectionKey { get; private set; }
            public string Label { get; private set; }
            public string Text { get; private set; }
            public int LabelFontSize { get; private set; }
            public int FontSize { get; private set; }
            public float SpacingBefore { get; private set; }

            public bool HasLabel { get { return !string.IsNullOrEmpty(Label); } }
        }

        private sealed class PickupInfoTextBlockLayout
        {
            public PickupInfoTextBlockLayout(
                PickupInfoTextBlock block,
                GUIStyle labelStyle,
                GUIStyle labelOutlineStyle,
                GUIStyle textStyle,
                float labelHeight,
                float textHeight)
            {
                Block = block;
                LabelStyle = labelStyle;
                LabelOutlineStyle = labelOutlineStyle;
                TextStyle = textStyle;
                LabelHeight = labelHeight;
                TextHeight = textHeight;
            }

            public PickupInfoTextBlock Block { get; private set; }
            public GUIStyle LabelStyle { get; private set; }
            public GUIStyle LabelOutlineStyle { get; private set; }
            public GUIStyle TextStyle { get; private set; }
            public float LabelHeight { get; private set; }
            public float TextHeight { get; private set; }
        }

        private sealed class PickupInfoBodyLayout
        {
            public PickupInfoBodyLayout(PickupInfoTextBlockLayout[] blocks, float width, float height)
            {
                Blocks = blocks ?? new PickupInfoTextBlockLayout[0];
                Width = width;
                Height = height;
            }

            public PickupInfoTextBlockLayout[] Blocks { get; private set; }
            public float Width { get; private set; }
            public float Height { get; private set; }
        }

        private static Color PickupWikiTipPanelColor { get { return DashboardTheme.PanelBackground; } }
        private static Color PickupWikiTipOuterBorderColor { get { return DashboardTheme.PanelOuterBorder; } }
        private static Color PickupWikiTipInnerBorderColor { get { return DashboardTheme.PanelInnerBorder; } }
        private static Color PickupWikiTipTitleColor { get { return DashboardTheme.PickupInfoTitle; } }
        private static Color PickupWikiTipBodyColor { get { return DashboardTheme.PickupInfoBody; } }
        private static Color PickupInfoQualityLabelColor { get { return DashboardTheme.PickupInfoQualityLabel; } }
        private static Color PickupInfoTypeLabelColor { get { return DashboardTheme.PickupInfoTypeLabel; } }
        private static Color PickupInfoSummaryLabelColor { get { return DashboardTheme.PickupInfoSummaryLabel; } }
        private static Color PickupInfoEffectsLabelColor { get { return DashboardTheme.PickupInfoEffectsLabel; } }
        private static Color PickupInfoSynergiesLabelColor { get { return DashboardTheme.PickupInfoSynergiesLabel; } }
        private static Color PickupInfoNotesLabelColor { get { return DashboardTheme.PickupInfoNotesLabel; } }

        internal void RefreshPickupWikiTipTheme()
        {
            _pickupWikiTipPanelStyle = null;
            _pickupWikiTipTitleStyle = null;
            _pickupWikiTipBodyStyle = null;
        }

        private void DrawNearbyPickupTipOverlay()
        {
            if (!IsPickupInfoOverlayEnabled() || _nearbyPickupTipService == null || !_nearbyPickupTipService.HasVisibleTip)
            {
                return;
            }

            EnsurePickupWikiTipStyles();
            PickupGameplayEntry gameplayEntry = GetCurrentPickupGameplayEntry();
            if (gameplayEntry == null)
            {
                if (_nearbyPickupTipService != null &&
                    _nearbyPickupTipService.HasVisibleTip &&
                    Logger != null &&
                    IsNearbyPickupVerboseLoggingEnabled())
                {
                    Logger.LogWarning(
                        EtgGameplayDashboardLog.Run(
                            "Nearby pickup overlay had a visible tip source, but no gameplay entry was resolved for rendering. " +
                            "PickupId=" +
                            _nearbyPickupTipService.CurrentPickupId +
                            ", RuntimeLabel=" +
                            Quote(_nearbyPickupTipService.CurrentDisplayName) +
                            ", RegistryCount=" +
                            (_pickupGameplayRegistry != null ? _pickupGameplayRegistry.Count : 0) +
                            "."));
                }
                return;
            }

            float scale = GetPickupWikiTipScale();
            Matrix4x4 previousGuiMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            try
            {
                float scaledScreenWidth = Screen.width / scale;
                float scaledScreenHeight = Screen.height / scale;
                float contentWidth = Mathf.Min(PickupWikiTipMaxWidth - 24f, scaledScreenWidth - 48f);
                string displayName = GetPickupGameplayDisplayName(gameplayEntry);
                List<PickupInfoTextBlock> bodyBlocks = BuildPickupGameplayBodyBlocks(gameplayEntry);
                if (string.IsNullOrEmpty(displayName) || bodyBlocks.Count == 0)
                {
                    return;
                }

                float titleHeight = _pickupWikiTipTitleStyle.CalcHeight(new GUIContent(displayName), contentWidth);
                PickupInfoBodyLayout bodyLayout = BuildPickupInfoBodyLayout(bodyBlocks, contentWidth);
                float bodyHeight = bodyLayout.Height;
                float availableHeight = Mathf.Max(
                    PickupWikiTipMinHeight,
                    scaledScreenHeight - PickupWikiTipMarginTop - PickupWikiTipMarginBottom);
                float panelHeight = Mathf.Min(PickupWikiTipMaxHeight, availableHeight);
                float requiredPanelHeight = titleHeight + bodyHeight + 28f;
                if (requiredPanelHeight < panelHeight)
                {
                    panelHeight = Mathf.Max(PickupWikiTipMinHeight, requiredPanelHeight);
                }

                float bodyViewportHeight = panelHeight - titleHeight - 22f;
                bool needsScrollView = bodyHeight > bodyViewportHeight;
                float scrollContentWidth = Mathf.Max(
                    1f,
                    contentWidth - SharedScrollViewStyles.ViewportScrollbarReserveWidth);
                if (needsScrollView)
                {
                    bodyLayout = BuildPickupInfoBodyLayout(bodyBlocks, scrollContentWidth);
                    bodyHeight = bodyLayout.Height;
                }

                Rect panelRect = new Rect(
                    scaledScreenWidth - PickupWikiTipMarginRight - PickupWikiTipMaxWidth,
                    PickupWikiTipMarginTop,
                    PickupWikiTipMaxWidth,
                    panelHeight);

                float border = DashboardTheme.PanelBorderThickness;
                Rect panelBorderRect = new Rect(
                    panelRect.x - border,
                    panelRect.y - border,
                    panelRect.width + (border * 2f),
                    panelRect.height + (border * 2f));
                GUI.Box(panelBorderRect, GUIContent.none, _pickupWikiTipPanelStyle);
                Rect titleRect = new Rect(panelRect.x + 12f, panelRect.y + 10f, panelRect.width - 24f, titleHeight);
                Rect bodyRect = new Rect(
                    panelRect.x + 12f,
                    titleRect.yMax + 6f,
                    panelRect.width - 24f,
                    bodyViewportHeight);
                GUI.Label(titleRect, displayName, _pickupWikiTipTitleStyle);

                if (_pickupWikiTipScrollPickupId != _nearbyPickupTipService.CurrentPickupId)
                {
                    _pickupWikiTipScrollPickupId = _nearbyPickupTipService.CurrentPickupId;
                    _pickupWikiTipScrollPosition = Vector2.zero;
                }

                if (!needsScrollView)
                {
                    _pickupWikiTipScrollPosition = Vector2.zero;
                    GUI.BeginGroup(bodyRect);
                    try
                    {
                        DrawPickupInfoBodyLayout(Vector2.zero, bodyLayout);
                    }
                    finally
                    {
                        GUI.EndGroup();
                    }
                }
                else
                {
                    Rect viewRect = new Rect(0f, 0f, scrollContentWidth, bodyHeight + 4f);
                    _pickupWikiTipScrollPosition = SharedScrollViewStyles.Begin(bodyRect, _pickupWikiTipScrollPosition, viewRect);
                    try
                    {
                        DrawPickupInfoBodyLayout(Vector2.zero, bodyLayout);
                    }
                    finally
                    {
                        GUI.EndScrollView();
                    }
                }
            }
            finally
            {
                GUI.matrix = previousGuiMatrix;
            }
        }

        private PickupGameplayEntry GetCurrentPickupGameplayEntry()
        {
            if (_pickupGameplayRegistry == null || _nearbyPickupTipService == null)
            {
                return null;
            }

            PickupGameplayEntry entry;
            return _pickupGameplayRegistry.TryGetEntry(_nearbyPickupTipService.CurrentPickupId, out entry) ? entry : null;
        }

        private string GetPickupGameplayDisplayName(PickupGameplayEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            if (string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(entry.ChineseDisplayName))
                {
                    return entry.ChineseDisplayName;
                }

                if (_nearbyPickupTipService != null && !string.IsNullOrEmpty(_nearbyPickupTipService.CurrentDisplayName))
                {
                    return _nearbyPickupTipService.CurrentDisplayName;
                }
            }

            if (!string.IsNullOrEmpty(entry.EnglishDisplayName))
            {
                return entry.EnglishDisplayName;
            }

            return _nearbyPickupTipService != null ? _nearbyPickupTipService.CurrentDisplayName : string.Empty;
        }

        private List<PickupInfoTextBlock> BuildPickupGameplayBodyBlocks(PickupGameplayEntry entry)
        {
            List<PickupInfoTextBlock> blocks = new List<PickupInfoTextBlock>();
            if (entry == null)
            {
                return blocks;
            }

            if (IsPickupInfoQualityEnabled())
            {
                AppendPickupInfoSectionBlock(blocks, "quality", GetPickupInfoDisplayValue(entry.Quality));
            }

            if (IsPickupInfoTypeEnabled())
            {
                AppendPickupInfoSectionBlock(blocks, "type", GetPickupInfoDisplayValue(entry.Type));
            }

            AppendPickupInfoStatBlocks(blocks, entry.StatSections);
            if (IsPickupInfoEffectsEnabled())
            {
                AppendPickupInfoSectionBlock(blocks, "effects", GetLocalizedGameplayValue(entry.EnglishEffects, entry.ChineseEffects));
            }

            if (IsPickupInfoSynergiesEnabled())
            {
                AppendPickupInfoSectionBlock(blocks, "synergies", GetLocalizedGameplayValue(entry.EnglishSynergies, entry.ChineseSynergies));
            }
            if (IsPickupInfoSummaryEnabled())
            {
                AppendPickupInfoSectionBlock(blocks, "summary", GetLocalizedGameplayValue(entry.EnglishGameplaySummary, entry.ChineseGameplaySummary));
            }

            if (IsPickupInfoNotesEnabled())
            {
                AppendPickupInfoSectionBlock(blocks, "notes", GetLocalizedGameplayValue(entry.EnglishNotes, entry.ChineseNotes));
            }

            return blocks;
        }

        private void AppendPickupInfoSectionBlock(List<PickupInfoTextBlock> blocks, string sectionKey, string value)
        {
            string label = GetPickupInfoSectionLabel(sectionKey);
            string trimmedValue = value != null ? value.Trim() : string.Empty;
            if (blocks == null || string.IsNullOrEmpty(label) || string.IsNullOrEmpty(trimmedValue))
            {
                return;
            }

            blocks.Add(new PickupInfoTextBlock(
                sectionKey,
                label,
                trimmedValue,
                PickupWikiTipSectionLabelFontSize,
                PickupWikiTipBodyFontSize,
                blocks.Count > 0 ? 16f : 0f));
        }

        private void AppendPickupInfoStatBlocks(List<PickupInfoTextBlock> blocks, PickupGameplayStatSection[] statGroups)
        {
            if (blocks == null || statGroups == null || statGroups.Length == 0)
            {
                return;
            }

            for (int i = 0; i < statGroups.Length; i++)
            {
                PickupGameplayStatSection statGroup = statGroups[i];
                if (statGroup == null || statGroup.Stats == null || statGroup.Stats.Length == 0)
                {
                    continue;
                }

                List<string> values = new List<string>();
                for (int j = 0; j < statGroup.Stats.Length; j++)
                {
                    PickupGameplayStatEntry stat = statGroup.Stats[j];
                    if (stat == null || string.IsNullOrEmpty(stat.Key) || stat.Parts == null || stat.Parts.Length == 0)
                    {
                        continue;
                    }

                    values.Add(BuildLabeledValue(GetPickupInfoLabel(stat.Key), GetPickupInfoDisplayValue(stat.Parts)));
                }

                string statText = string.Join(" | ", values.ToArray());
                if (!string.IsNullOrEmpty(statText))
                {
                    blocks.Add(new PickupInfoTextBlock(
                        string.Empty,
                        string.Empty,
                        statText,
                        0,
                        PickupWikiTipBodyFontSize,
                        blocks.Count > 0 ? 16f : 0f));
                }
            }
        }

        private PickupInfoBodyLayout BuildPickupInfoBodyLayout(List<PickupInfoTextBlock> blocks, float width)
        {
            float layoutWidth = Mathf.Max(1f, width);
            List<PickupInfoTextBlockLayout> layouts = new List<PickupInfoTextBlockLayout>();
            float height = 0f;
            for (int i = 0; i < blocks.Count; i++)
            {
                PickupInfoTextBlock block = blocks[i];
                height += block.SpacingBefore;
                GUIStyle labelStyle = null;
                GUIStyle labelOutlineStyle = null;
                float labelHeight = 0f;
                if (block.HasLabel)
                {
                    labelStyle = CreatePickupInfoTextBlockStyle(
                        block.LabelFontSize,
                        GetPickupInfoSectionLabelColor(block.SectionKey),
                        false,
                        FontStyle.Bold);
                    if (DashboardTheme.UsePickupInfoLabelOutline)
                    {
                        labelOutlineStyle = CreatePickupInfoTextBlockStyle(
                            block.LabelFontSize,
                            DashboardTheme.PickupInfoLabelOutline,
                            false,
                            FontStyle.Bold);
                    }
                    labelHeight = labelStyle.CalcHeight(new GUIContent(block.Label), layoutWidth);
                    height += labelHeight + PickupWikiTipSectionLabelValueGap;
                }

                GUIStyle textStyle = CreatePickupInfoTextBlockStyle(
                    block.FontSize,
                    PickupWikiTipBodyColor,
                    true,
                    FontStyle.Normal);
                float textHeight = textStyle.CalcHeight(new GUIContent(block.Text), layoutWidth);
                height += textHeight;
                layouts.Add(new PickupInfoTextBlockLayout(
                    block,
                    labelStyle,
                    labelOutlineStyle,
                    textStyle,
                    labelHeight,
                    textHeight));
            }

            return new PickupInfoBodyLayout(layouts.ToArray(), layoutWidth, Mathf.Max(1f, height));
        }

        private static void DrawPickupInfoBodyLayout(Vector2 localPosition, PickupInfoBodyLayout bodyLayout)
        {
            float top = localPosition.y;
            for (int i = 0; i < bodyLayout.Blocks.Length; i++)
            {
                PickupInfoTextBlockLayout blockLayout = bodyLayout.Blocks[i];
                PickupInfoTextBlock block = blockLayout.Block;
                top += block.SpacingBefore;
                if (block.HasLabel)
                {
                    float outlineInset = blockLayout.LabelOutlineStyle != null
                        ? PickupWikiTipSectionLabelOutlineSize
                        : 0f;
                    DrawPickupInfoOutlinedLabel(
                        new Rect(
                            localPosition.x + outlineInset,
                            top + outlineInset,
                            Mathf.Max(1f, bodyLayout.Width - (outlineInset * 2f)),
                            blockLayout.LabelHeight),
                        block.Label,
                        blockLayout.LabelStyle,
                        blockLayout.LabelOutlineStyle);
                    top += blockLayout.LabelHeight + PickupWikiTipSectionLabelValueGap;
                }

                GUI.Label(
                    new Rect(localPosition.x, top, bodyLayout.Width, blockLayout.TextHeight),
                    block.Text,
                    blockLayout.TextStyle);
                top += blockLayout.TextHeight;
            }
        }

        private static void DrawPickupInfoOutlinedLabel(
            Rect rect,
            string text,
            GUIStyle labelStyle,
            GUIStyle outlineStyle)
        {
            if (outlineStyle != null)
            {
                for (int verticalOffset = -1; verticalOffset <= 1; verticalOffset++)
                {
                    for (int horizontalOffset = -1; horizontalOffset <= 1; horizontalOffset++)
                    {
                        if (horizontalOffset == 0 && verticalOffset == 0)
                        {
                            continue;
                        }

                        GUI.Label(
                            new Rect(
                                rect.x + (horizontalOffset * PickupWikiTipSectionLabelOutlineSize),
                                rect.y + (verticalOffset * PickupWikiTipSectionLabelOutlineSize),
                                rect.width,
                                rect.height),
                            text,
                            outlineStyle);
                    }
                }
            }

            GUI.Label(rect, text, labelStyle);
        }

        private GUIStyle CreatePickupInfoTextBlockStyle(
            int fontSize,
            Color textColor,
            bool wordWrap,
            FontStyle fontStyle)
        {
            GUIStyle style = new GUIStyle(_pickupWikiTipBodyStyle);
            style.fontSize = fontSize;
            style.fontStyle = fontStyle;
            style.normal.textColor = textColor;
            style.padding = new RectOffset(0, 0, 0, 0);
            style.margin = new RectOffset(0, 0, 0, 0);
            style.wordWrap = wordWrap;
            // If rich-text color tags are ever reintroduced, use stable #RRGGBB only.
            // Do not use #RRGGBBAA because this game's Unity IMGUI parser may ignore the eight-digit form.
            style.richText = false;
            return style;
        }

        private static string BuildLabeledValue(string label, string value)
        {
            return string.IsNullOrEmpty(label) || string.IsNullOrEmpty(value) ? string.Empty : label + " " + value;
        }

        private string GetPickupInfoDisplayValue(string value)
        {
            string trimmedValue = value != null ? value.Trim() : string.Empty;
            if (string.IsNullOrEmpty(trimmedValue))
            {
                return string.Empty;
            }

            return _pickupInfoTermsRegistry != null
                ? _pickupInfoTermsRegistry.ResolveDisplayValue(GuiText.CurrentLanguageCode, trimmedValue, trimmedValue)
                : trimmedValue;
        }

        private string GetPickupInfoDisplayValue(PickupGameplayStatPart[] parts)
        {
            if (parts == null || parts.Length == 0)
            {
                return string.Empty;
            }

            List<string> renderedParts = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                PickupGameplayStatPart part = parts[i];
                if (part == null || string.IsNullOrEmpty(part.Value))
                {
                    continue;
                }

                string value = GetPickupInfoDisplayValue(part.Value);
                string label = GetPickupInfoDisplayValue(part.Label);
                renderedParts.Add(string.IsNullOrEmpty(label) ? value : value + " (" + label + ")");
            }

            return string.Join(" ", renderedParts.ToArray());
        }

        private static string GetLocalizedGameplayValue(string englishValue, string chineseValue)
        {
            if (string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(chineseValue))
            {
                return chineseValue;
            }

            return englishValue ?? string.Empty;
        }

        private static string GetLocalizedGameplayValue(string[] englishValues, string[] chineseValues)
        {
            bool isChinese = string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase);
            string[] values = isChinese && chineseValues != null && chineseValues.Length > 0
                ? chineseValues
                : englishValues;
            return JoinGameplayLines(values);
        }

        private static string JoinGameplayLines(string[] values)
        {
            if (values == null || values.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i] != null ? values[i].Trim() : string.Empty;
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("\n");
                }

                builder.Append(value);
            }

            return builder.ToString();
        }

        private static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty) + "\"";
        }

        private string GetPickupInfoSectionLabel(string sectionKey)
        {
            string fallback;
            switch (sectionKey)
            {
                case "quality":
                    fallback = string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase) ? "品质：" : "Quality:";
                    break;
                case "type":
                    fallback = string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase) ? "类型：" : "Type:";
                    break;
                case "summary":
                    fallback = string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase) ? "摘要：" : "Summary:";
                    break;
                case "effects":
                    fallback = string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase) ? "效果：" : "Effects:";
                    break;
                case "synergies":
                    fallback = string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase) ? "协同：" : "Synergies:";
                    break;
                case "notes":
                    fallback = string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase) ? "备注：" : "Notes:";
                    break;
                default:
                    fallback = string.Empty;
                    break;
            }

            return _pickupInfoTermsRegistry != null
                ? _pickupInfoTermsRegistry.ResolveSectionLabel(GuiText.CurrentLanguageCode, sectionKey, fallback)
                : fallback;
        }

        private static Color GetPickupInfoSectionLabelColor(string sectionKey)
        {
            switch (sectionKey)
            {
                case "quality":
                    return PickupInfoQualityLabelColor;
                case "type":
                    return PickupInfoTypeLabelColor;
                case "summary":
                    return PickupInfoSummaryLabelColor;
                case "effects":
                    return PickupInfoEffectsLabelColor;
                case "synergies":
                    return PickupInfoSynergiesLabelColor;
                case "notes":
                    return PickupInfoNotesLabelColor;
                default:
                    return PickupWikiTipBodyColor;
            }
        }

        private string GetPickupInfoLabel(string labelKey)
        {
            bool isChinese = string.Equals(GuiText.CurrentLanguageCode, "zh-CN", System.StringComparison.OrdinalIgnoreCase);
            string fallback;
            switch (labelKey)
            {
                case "quality":
                    fallback = isChinese ? "品质" : "Q";
                    break;
                case "type":
                    fallback = isChinese ? "类型" : "Type";
                    break;
                case "class":
                    fallback = isChinese ? "类别" : "Class";
                    break;
                case "dps":
                    fallback = "DPS";
                    break;
                case "damage":
                    fallback = isChinese ? "伤害" : "DMG";
                    break;
                case "magazine":
                    fallback = isChinese ? "弹匣" : "Mag";
                    break;
                case "ammo":
                    fallback = isChinese ? "备弹" : "Ammo";
                    break;
                case "reload":
                    fallback = isChinese ? "换弹" : "Reload";
                    break;
                case "fire_rate":
                    fallback = isChinese ? "射速" : "Fire";
                    break;
                case "shot_speed":
                    fallback = isChinese ? "弹速" : "Speed";
                    break;
                case "range":
                    fallback = isChinese ? "射程" : "Range";
                    break;
                case "force":
                    fallback = isChinese ? "击退" : "Force";
                    break;
                case "spread":
                    fallback = isChinese ? "散布" : "Spread";
                    break;
                case "duration":
                    fallback = isChinese ? "持续" : "Duration";
                    break;
                case "recharge":
                    fallback = isChinese ? "充能" : "Recharge";
                    break;
                case "sell":
                    fallback = isChinese ? "售价" : "Sell";
                    break;
                default:
                    fallback = string.Empty;
                    break;
            }

            return _pickupInfoTermsRegistry != null
                ? _pickupInfoTermsRegistry.ResolveStatLabel(GuiText.CurrentLanguageCode, labelKey, fallback)
                : fallback;
        }

        private static string FormatPickupWikiTipBodyText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string formatted = text.Replace("\r\n", "\n").Replace('\r', '\n');
            for (int i = 0; i < PickupWikiTipSectionLabels.Length; i++)
            {
                formatted = InsertLineBreakBeforeSectionLabel(formatted, PickupWikiTipSectionLabels[i]);
            }

            return formatted;
        }

        private static string InsertLineBreakBeforeSectionLabel(string text, string label)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(label))
            {
                return text ?? string.Empty;
            }

            int searchIndex = 0;
            while (searchIndex < text.Length)
            {
                int labelIndex = text.IndexOf(label, searchIndex, System.StringComparison.Ordinal);
                if (labelIndex < 0)
                {
                    break;
                }

                if (labelIndex > 0 && text[labelIndex - 1] != '\n')
                {
                    int whitespaceStart = labelIndex;
                    while (whitespaceStart > 0 && (text[whitespaceStart - 1] == ' ' || text[whitespaceStart - 1] == '\t'))
                    {
                        whitespaceStart--;
                    }

                    text = text.Substring(0, whitespaceStart) + "\n\n" + text.Substring(labelIndex);
                    labelIndex = whitespaceStart + 2;
                }

                searchIndex = labelIndex + label.Length;
            }

            return text;
        }

        private void EnsurePickupWikiTipStyles()
        {
            if (_pickupWikiTipPanelStyle != null)
            {
                return;
            }

            _pickupWikiTipPanelStyle = new GUIStyle(GUI.skin.box);
            _pickupWikiTipPanelStyle.normal.background = MakePickupWikiTipTripleBorderedTexture(
                PickupWikiTipPanelColor,
                PickupWikiTipOuterBorderColor,
                DashboardTheme.PanelMiddleBorder,
                PickupWikiTipInnerBorderColor,
                5,
                7,
                5);
            _pickupWikiTipPanelStyle.border = new RectOffset(17, 17, 17, 17);
            _pickupWikiTipPanelStyle.padding = new RectOffset(12, 12, 10, 10);

            _pickupWikiTipTitleStyle = new GUIStyle(GUI.skin.label);
            _pickupWikiTipTitleStyle.normal.textColor = PickupWikiTipTitleColor;
            _pickupWikiTipTitleStyle.fontSize = PickupWikiTipTitleFontSize;
            _pickupWikiTipTitleStyle.fontStyle = FontStyle.Bold;
            _pickupWikiTipTitleStyle.wordWrap = true;

            _pickupWikiTipBodyStyle = new GUIStyle(GUI.skin.label);
            _pickupWikiTipBodyStyle.normal.textColor = PickupWikiTipBodyColor;
            _pickupWikiTipBodyStyle.fontSize = PickupWikiTipBodyFontSize;
            _pickupWikiTipBodyStyle.wordWrap = true;
            _pickupWikiTipBodyStyle.richText = true;
        }

        private static float GetPickupWikiTipScale()
        {
            float widthScale = Screen.width / PickupWikiTipReferenceScreenWidth;
            float heightScale = Screen.height / PickupWikiTipReferenceScreenHeight;
            return Mathf.Clamp(Mathf.Min(widthScale, heightScale), PickupWikiTipMinimumScale, PickupWikiTipMaximumScale);
        }

        private static Texture2D MakePickupWikiTipTripleBorderedTexture(
            Color fillColor,
            Color outerBorderColor,
            Color middleBorderColor,
            Color innerBorderColor,
            int outerBorderThickness,
            int middleBorderThickness,
            int innerBorderThickness)
        {
            Texture2D texture = new Texture2D(32, 32);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[1024];
            int outerThickness = Mathf.Clamp(outerBorderThickness, 1, 4);
            int middleThickness = Mathf.Clamp(middleBorderThickness, 1, 8);
            int innerThickness = Mathf.Clamp(innerBorderThickness, 1, 4);
            int middleStart = outerThickness;
            int innerStart = middleStart + middleThickness;
            int innerEnd = 32 - innerStart;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool isOuterBorder = x < outerThickness
                        || x >= 32 - outerThickness
                        || y < outerThickness
                        || y >= 32 - outerThickness;
                    bool isMiddleBorder = x >= middleStart
                        && x < middleStart + middleThickness
                        || x < 32 - middleStart
                        && x >= 32 - middleStart - middleThickness
                        || y >= middleStart
                        && y < middleStart + middleThickness
                        || y < 32 - middleStart
                        && y >= 32 - middleStart - middleThickness;
                    bool isInnerBorder = x >= innerStart
                        && x < innerStart + innerThickness
                        || x < innerEnd
                        && x >= innerEnd - innerThickness
                        || y >= innerStart
                        && y < innerStart + innerThickness
                        || y < innerEnd
                        && y >= innerEnd - innerThickness;

                    pixels[(y * 32) + x] = isOuterBorder
                        ? outerBorderColor
                        : (isMiddleBorder
                            ? middleBorderColor
                            : (isInnerBorder ? innerBorderColor : fillColor));
                }
            }

            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }
    }
}
