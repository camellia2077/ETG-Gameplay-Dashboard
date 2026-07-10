// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace RandomLoadout
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
        

        private static readonly Color PickupWikiTipPanelColor = new Color(0.07f, 0.08f, 0.10f, 0.94f);
        private static readonly Color PickupWikiTipBorderColor = new Color(0.97f, 0.63f, 0.10f, 0.96f);
        private static readonly Color PickupWikiTipTitleColor = new Color(0.98f, 0.96f, 0.90f, 1f);
        private static readonly Color PickupWikiTipBodyColor = new Color(0.93f, 0.92f, 0.88f, 1f);
        private static readonly Color PickupInfoQualityLabelColor = new Color(0.99f, 0.86f, 0.38f, 1f);
        private static readonly Color PickupInfoTypeLabelColor = new Color(0.67f, 0.84f, 0.98f, 1f);
        private static readonly Color PickupInfoSummaryLabelColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        private static readonly Color PickupInfoEffectsLabelColor = new Color(0.62f, 0.92f, 0.65f, 1f);
        private static readonly Color PickupInfoSynergiesLabelColor = new Color(0.52f, 0.92f, 0.88f, 1f);
        private static readonly Color PickupInfoNotesLabelColor = new Color(0.79f, 0.85f, 0.95f, 1f);

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
                        RandomLoadoutLog.Run(
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
                string bodyText = BuildPickupGameplayBodyText(gameplayEntry);
                if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(bodyText))
                {
                    return;
                }

                float titleHeight = _pickupWikiTipTitleStyle.CalcHeight(new GUIContent(displayName), contentWidth);
                float bodyHeight = _pickupWikiTipBodyStyle.CalcHeight(new GUIContent(bodyText), contentWidth);
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
                Rect panelRect = new Rect(
                    scaledScreenWidth - PickupWikiTipMarginRight - PickupWikiTipMaxWidth,
                    PickupWikiTipMarginTop,
                    PickupWikiTipMaxWidth,
                    panelHeight);

                GUI.Box(panelRect, GUIContent.none, _pickupWikiTipPanelStyle);
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
                    GUI.Label(bodyRect, bodyText, _pickupWikiTipBodyStyle);
                }
                else
                {
                    Rect viewRect = new Rect(0f, 0f, bodyRect.width - SharedScrollViewStyles.ViewportScrollbarReserveWidth, bodyHeight + 4f);
                    _pickupWikiTipScrollPosition = SharedScrollViewStyles.Begin(bodyRect, _pickupWikiTipScrollPosition, viewRect);
                    GUI.Label(new Rect(0f, 0f, viewRect.width, bodyHeight), bodyText, _pickupWikiTipBodyStyle);
                    GUI.EndScrollView();
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

        private string BuildPickupGameplayBodyText(PickupGameplayEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            if (IsPickupInfoQualityEnabled())
            {
                AppendSection(builder, "quality", GetPickupInfoDisplayValue(entry.Quality));
            }

            if (IsPickupInfoTypeEnabled())
            {
                AppendSection(builder, "type", GetPickupInfoDisplayValue(entry.Type));
            }

            AppendStatGroups(builder, entry.StatSections);
            if (IsPickupInfoEffectsEnabled())
            {
                AppendSection(builder, "effects", GetLocalizedGameplayValue(entry.EnglishEffects, entry.ChineseEffects));
            }

            if (IsPickupInfoSynergiesEnabled())
            {
                AppendSection(builder, "synergies", GetLocalizedGameplayValue(entry.EnglishSynergies, entry.ChineseSynergies));
            }
            if (IsPickupInfoSummaryEnabled())
            {
                AppendSection(builder, "summary", GetLocalizedGameplayValue(entry.EnglishGameplaySummary, entry.ChineseGameplaySummary));
            }

            if (IsPickupInfoNotesEnabled())
            {
                AppendSection(builder, "notes", GetLocalizedGameplayValue(entry.EnglishNotes, entry.ChineseNotes));
            }

            return builder.ToString().Trim();
        }

        private void AppendStatGroups(StringBuilder builder, PickupGameplayStatSection[] statGroups)
        {
            if (builder == null || statGroups == null || statGroups.Length == 0)
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
                    if (stat == null || string.IsNullOrEmpty(stat.Key) || string.IsNullOrEmpty(stat.Value))
                    {
                        continue;
                    }

                    values.Add(BuildLabeledValue(GetPickupInfoLabel(stat.Key), GetPickupInfoDisplayValue(stat.Value)));
                }

                AppendInlineStats(builder, values.ToArray());
            }
        }

        private static void AppendInlineStats(StringBuilder builder, string[] values)
        {
            if (builder == null || values == null || values.Length == 0)
            {
                return;
            }

            StringBuilder lineBuilder = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }

                if (lineBuilder.Length > 0)
                {
                    lineBuilder.Append(" | ");
                }

                lineBuilder.Append(value);
            }

            if (lineBuilder.Length == 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append("\n");
            }

            builder.Append(lineBuilder.ToString());
        }

        private void AppendSection(StringBuilder builder, string sectionKey, string value)
        {
            string label = GetPickupInfoSectionLabel(sectionKey);
            if (builder == null || string.IsNullOrEmpty(label) || string.IsNullOrEmpty(value))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append("\n\n");
            }

            builder.Append(GetPickupInfoColoredSectionLabel(sectionKey, label));
            builder.Append(" ");
            builder.Append(value.Trim());
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
                    builder.Append("; ");
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

        private string GetPickupInfoColoredSectionLabel(string sectionKey, string label)
        {
            return WrapWithColorAndSizeTag(label, GetPickupInfoSectionLabelColor(sectionKey), PickupWikiTipSectionLabelFontSize);
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

        private static string WrapWithColorAndSizeTag(string text, Color color, int fontSize)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            Color32 color32 = color;
            return "<size=" +
                   fontSize +
                   "><color=#" +
                   color32.r.ToString("X2") +
                   color32.g.ToString("X2") +
                   color32.b.ToString("X2") +
                   color32.a.ToString("X2") +
                   ">" +
                   text +
                   "</color></size>";
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
            _pickupWikiTipPanelStyle.normal.background = MakePickupWikiTipBorderedTexture(PickupWikiTipPanelColor, PickupWikiTipBorderColor);
            _pickupWikiTipPanelStyle.border = new RectOffset(2, 2, 2, 2);
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

        private static Texture2D MakePickupWikiTipBorderedTexture(Color fillColor, Color borderColor)
        {
            Texture2D texture = new Texture2D(8, 8);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[64];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool isBorderPixel = x == 0 || x == 7 || y == 0 || y == 7;
                    pixels[(y * 8) + x] = isBorderPixel ? borderColor : fillColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
