// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private struct PickupActionButtonDefinition
        {
            public PickupActionButtonDefinition(string controlId, string label, Action onClick, GUIStyle style)
            {
                ControlId = controlId ?? string.Empty;
                Label = label ?? string.Empty;
                OnClick = onClick;
                Style = style;
            }

            public readonly string ControlId;
            public readonly string Label;
            public readonly Action OnClick;
            public readonly GUIStyle Style;
        }

        private struct PickupActionRowDefinition
        {
            public PickupActionRowDefinition(string spriteName, string label, PickupActionButtonDefinition[] actions)
            {
                SpriteName = spriteName ?? string.Empty;
                Label = label ?? string.Empty;
                Actions = actions ?? EmptyPickupActionButtons;
            }

            public readonly string SpriteName;
            public readonly string Label;
            public readonly PickupActionButtonDefinition[] Actions;
        }

        private static readonly PickupActionButtonDefinition[] EmptyPickupActionButtons = new PickupActionButtonDefinition[0];

        private void DrawPickupActionRows(Rect contentRect, float top, float rowHeight, float rowGap, PickupActionRowDefinition[] rows)
        {
            if (rows == null)
            {
                return;
            }

            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                Rect rowRect = new Rect(contentRect.x, top + ((rowHeight + rowGap) * rowIndex), contentRect.width, rowHeight);
                DrawPickupActionRow(rowRect, rows[rowIndex]);
            }
        }

        private void DrawPickupActionRow(Rect rowRect, PickupActionRowDefinition row)
        {
            GUI.Box(rowRect, GUIContent.none, _pickupRowStyle);

            const float iconSize = 30f;
            const float rowPadding = 8f;
            // Keep the full pickup label readable (including "Hegemony (+50)")
            // while allowing the action buttons to stay compact.
            const float labelWidth = 140f;
            Rect iconRect = new Rect(rowRect.x + rowPadding, rowRect.y + ((rowRect.height - iconSize) * 0.5f), iconSize, iconSize);
            DrawPickupActionIcon(iconRect, row.SpriteName);

            GUI.Label(
                new Rect(iconRect.xMax + 10f, rowRect.y + 5f, labelWidth, rowRect.height - 10f),
                row.Label,
                _pickupPrimaryTextStyle);

            PickupActionButtonDefinition[] actions = row.Actions ?? EmptyPickupActionButtons;
            if (actions.Length == 0)
            {
                return;
            }

            float actionAreaRight = rowRect.x + rowRect.width - rowPadding;
            float actionAreaLeft = iconRect.xMax + 10f + labelWidth + 4f;
            float actionGap = 8f;
            float actionAreaWidth = actionAreaRight - actionAreaLeft;
            float actionButtonWidth = (actionAreaWidth - (actionGap * (actions.Length - 1))) / actions.Length;
            float actionButtonHeight = rowRect.height - 8f;
            for (int actionIndex = 0; actionIndex < actions.Length; actionIndex++)
            {
                PickupActionButtonDefinition action = actions[actionIndex];
                Rect actionButtonRect = new Rect(
                    actionAreaLeft + ((actionButtonWidth + actionGap) * actionIndex),
                    rowRect.y + 4f,
                    actionButtonWidth,
                    actionButtonHeight);
                GUIStyle actionStyle = action.Style ?? _buttonStyle;
                if (!DrawControllerButton(actionButtonRect, action.ControlId, action.Label, actionStyle))
                {
                    continue;
                }

                if (action.OnClick != null)
                {
                    action.OnClick();
                }
            }
        }

        private void DrawPickupActionIcon(Rect iconRect, string spriteName)
        {
            GUI.Box(iconRect, GUIContent.none, _pickupIconBackgroundStyle);

            PickupIconData iconData;
            if (TryGetGameUiAtlasIcon(spriteName, out iconData))
            {
                GUI.DrawTextureWithTexCoords(iconRect, iconData.Texture, iconData.TextureCoords, true);
                return;
            }

            GUI.Box(iconRect, "?", _pickupIconFallbackStyle);
        }
    }
}
