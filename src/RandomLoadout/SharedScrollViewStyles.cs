// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using UnityEngine;

namespace RandomLoadout
{
    internal static class SharedScrollViewStyles
    {
        public const float ViewportScrollbarReserveWidth = 18f;

        private static string _themeId = string.Empty;

        private static GUIStyle _scrollViewStyle;
        private static GUIStyle _scrollbarBackgroundStyle;
        private static GUIStyle _verticalScrollbarThumbStyle;
        private static GUIStyle _horizontalScrollbarThumbStyle;
        private static GUIStyle _verticalScrollbarButtonStyle;
        private static GUIStyle _horizontalScrollbarButtonStyle;

        public static Vector2 Begin(Rect position, Vector2 scrollPosition, Rect viewRect)
        {
            EnsureStyles();

            GUISkin skin = GUI.skin;
            GUIStyle originalScrollView = skin.scrollView;
            GUIStyle originalVerticalScrollbar = skin.verticalScrollbar;
            GUIStyle originalVerticalScrollbarThumb = skin.verticalScrollbarThumb;
            GUIStyle originalHorizontalScrollbar = skin.horizontalScrollbar;
            GUIStyle originalHorizontalScrollbarThumb = skin.horizontalScrollbarThumb;
            GUIStyle originalVerticalScrollbarUpButton = skin.verticalScrollbarUpButton;
            GUIStyle originalVerticalScrollbarDownButton = skin.verticalScrollbarDownButton;
            GUIStyle originalHorizontalScrollbarLeftButton = skin.horizontalScrollbarLeftButton;
            GUIStyle originalHorizontalScrollbarRightButton = skin.horizontalScrollbarRightButton;

            skin.scrollView = _scrollViewStyle;
            skin.verticalScrollbar = _scrollbarBackgroundStyle;
            skin.verticalScrollbarThumb = _verticalScrollbarThumbStyle;
            skin.horizontalScrollbar = _scrollbarBackgroundStyle;
            skin.horizontalScrollbarThumb = _horizontalScrollbarThumbStyle;
            skin.verticalScrollbarUpButton = _verticalScrollbarButtonStyle;
            skin.verticalScrollbarDownButton = _verticalScrollbarButtonStyle;
            skin.horizontalScrollbarLeftButton = _horizontalScrollbarButtonStyle;
            skin.horizontalScrollbarRightButton = _horizontalScrollbarButtonStyle;
            try
            {
                return GUI.BeginScrollView(position, scrollPosition, viewRect);
            }
            finally
            {
                skin.scrollView = originalScrollView;
                skin.verticalScrollbar = originalVerticalScrollbar;
                skin.verticalScrollbarThumb = originalVerticalScrollbarThumb;
                skin.horizontalScrollbar = originalHorizontalScrollbar;
                skin.horizontalScrollbarThumb = originalHorizontalScrollbarThumb;
                skin.verticalScrollbarUpButton = originalVerticalScrollbarUpButton;
                skin.verticalScrollbarDownButton = originalVerticalScrollbarDownButton;
                skin.horizontalScrollbarLeftButton = originalHorizontalScrollbarLeftButton;
                skin.horizontalScrollbarRightButton = originalHorizontalScrollbarRightButton;
            }
        }

        private static void EnsureStyles()
        {
            if (_scrollViewStyle != null && string.Equals(_themeId, DashboardTheme.CurrentId, System.StringComparison.Ordinal))
            {
                return;
            }

            _themeId = DashboardTheme.CurrentId;

            _scrollViewStyle = new GUIStyle(GUI.skin.scrollView);
            _scrollViewStyle.normal.background = MakeTexture(1, 1, new Color(0f, 0f, 0f, 0f));
            _scrollViewStyle.border = new RectOffset(0, 0, 0, 0);
            _scrollViewStyle.padding = new RectOffset(0, 0, 0, 0);

            Texture2D transparentScrollbarTexture = MakeTexture(1, 1, new Color(0f, 0f, 0f, 0f));
            Texture2D selectedScrollbarTexture = MakeTexture(1, 1, DashboardTheme.ScrollThumb);

            _scrollbarBackgroundStyle = new GUIStyle(GUI.skin.verticalScrollbar);
            _scrollbarBackgroundStyle.normal.background = transparentScrollbarTexture;
            _scrollbarBackgroundStyle.hover.background = _scrollbarBackgroundStyle.normal.background;
            _scrollbarBackgroundStyle.active.background = _scrollbarBackgroundStyle.normal.background;
            _scrollbarBackgroundStyle.border = new RectOffset(0, 0, 0, 0);
            _scrollbarBackgroundStyle.fixedWidth = 14f;
            _scrollbarBackgroundStyle.fixedHeight = 14f;

            _verticalScrollbarThumbStyle = new GUIStyle(GUI.skin.verticalScrollbarThumb);
            _verticalScrollbarThumbStyle.normal.background = selectedScrollbarTexture;
            _verticalScrollbarThumbStyle.hover.background = _verticalScrollbarThumbStyle.normal.background;
            _verticalScrollbarThumbStyle.active.background = _verticalScrollbarThumbStyle.normal.background;
            _verticalScrollbarThumbStyle.border = new RectOffset(0, 0, 0, 0);
            _verticalScrollbarThumbStyle.fixedWidth = 14f;

            _horizontalScrollbarThumbStyle = new GUIStyle(GUI.skin.horizontalScrollbarThumb);
            _horizontalScrollbarThumbStyle.normal.background = selectedScrollbarTexture;
            _horizontalScrollbarThumbStyle.hover.background = _horizontalScrollbarThumbStyle.normal.background;
            _horizontalScrollbarThumbStyle.active.background = _horizontalScrollbarThumbStyle.normal.background;
            _horizontalScrollbarThumbStyle.border = new RectOffset(0, 0, 0, 0);
            _horizontalScrollbarThumbStyle.fixedHeight = 14f;

            _verticalScrollbarButtonStyle = new GUIStyle(GUIStyle.none);
            _verticalScrollbarButtonStyle.normal.background = transparentScrollbarTexture;
            _verticalScrollbarButtonStyle.hover.background = transparentScrollbarTexture;
            _verticalScrollbarButtonStyle.active.background = transparentScrollbarTexture;
            _verticalScrollbarButtonStyle.border = new RectOffset(0, 0, 0, 0);
            _verticalScrollbarButtonStyle.fixedWidth = 14f;
            _verticalScrollbarButtonStyle.fixedHeight = 0f;

            _horizontalScrollbarButtonStyle = new GUIStyle(GUIStyle.none);
            _horizontalScrollbarButtonStyle.normal.background = transparentScrollbarTexture;
            _horizontalScrollbarButtonStyle.hover.background = transparentScrollbarTexture;
            _horizontalScrollbarButtonStyle.active.background = transparentScrollbarTexture;
            _horizontalScrollbarButtonStyle.border = new RectOffset(0, 0, 0, 0);
            _horizontalScrollbarButtonStyle.fixedWidth = 0f;
            _horizontalScrollbarButtonStyle.fixedHeight = 14f;
        }

        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
