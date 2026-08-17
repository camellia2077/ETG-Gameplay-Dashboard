// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        // Keep these atlas sprite names centralized so command pages, Start Items, and future pickup UIs
        // all reuse the same ETG runtime-art identifiers instead of drifting through copy-pasted strings.
        private const string GameUiAtlasSpriteHealthPickup = "heart_full_001";
        private const string GameUiAtlasSpriteArmorPickup = "armor_shield_pickup_001";
        private const string GameUiAtlasSpriteBlankPickup = "blank_item_001";
        private const string GameUiAtlasSpriteKeyPickup = "ui_keybullet_idle_002";
        private const string GameUiAtlasSpriteRatRewardKeyPickup = "room_rat_reward_key_001";
        private const string GameUiAtlasSpriteCasingsPickup = "ui_coin_idle_002";
        private const string GameUiAtlasSpriteHegemonyPickup = "hbux_text_icon";

        // Cell Keys use the CellKey prefab's tk2dSprite instead of a GameUIAtlas icon.
        // Their pickup row resolves the live pickup sprite by PickupObjectId.

        private static string GetStartItemPickupSpriteName(string pickupType)
        {
            switch (StartItemPickupCatalog.NormalizeType(pickupType))
            {
                case StartItemPickupCatalog.KeyType:
                    return GameUiAtlasSpriteKeyPickup;
                case StartItemPickupCatalog.RatKeyType:
                    // This Start Items icon is the Rat Chest reward-room key icon, not the other Resourceful Rat key variant.
                    // Keep it on room_rat_reward_key_001 so the preset-pickups UI matches the "open Rat Chests after the boss"
                    // meaning and does not drift back to resourcefulrat_key_001 by accident.
                    return GameUiAtlasSpriteRatRewardKeyPickup;
                case StartItemPickupCatalog.MaxHealthType:
                    return GameUiAtlasSpriteHealthPickup;
                case StartItemPickupCatalog.ArmorType:
                    // This is the pickups-facing Armor icon for Start Items and command-panel resource rows.
                    // It comes from GameUIHeartController.armorSpritePrefab -> ArmorPiece, so it matches the
                    // pickup/resource meaning rather than the HUD armor-heart presentation variant.
                    return GameUiAtlasSpriteArmorPickup;
                case StartItemPickupCatalog.CasingsType:
                    return GameUiAtlasSpriteCasingsPickup;
                case StartItemPickupCatalog.BlankType:
                    return GameUiAtlasSpriteBlankPickup;
                default:
                    return string.Empty;
            }
        }

        private bool TryGetLoadoutEntryIcon(LoadoutRuleEditorEntry entry, out PickupIconData iconData)
        {
            iconData = PickupIconData.Empty;
            if (entry == null) return false;
            if (entry.PickupId.HasValue && TryGetPickupIcon(entry.PickupId.Value, out iconData)) return true;
            return TryGetStartItemPickupIcon(entry.PickupType, out iconData);
        }

        private bool TryGetStartItemPickupIcon(string pickupType, out PickupIconData iconData)
        {
            iconData = PickupIconData.Empty;
            string spriteName = GetStartItemPickupSpriteName(pickupType);
            return !string.IsNullOrEmpty(spriteName) && TryGetGameUiAtlasIcon(spriteName, out iconData);
        }

        private bool TryGetGameUiAtlasIcon(string spriteName, out PickupIconData iconData)
        {
            iconData = PickupIconData.Empty;
            dfAtlas atlas;
            if (string.IsNullOrEmpty(spriteName) || !TryGetGameUiAtlas(out atlas) || atlas == null) return false;

            dfAtlas.ItemInfo item = atlas[spriteName];
            Texture texture = atlas.Texture;
            if (item == null || texture == null) return false;
            Rect region = item.region;
            iconData = new PickupIconData(texture, Rect.MinMaxRect(region.xMin, region.yMin, region.xMax, region.yMax));
            return true;
        }

        private bool TryGetGameUiAtlas(out dfAtlas atlas)
        {
            atlas = _gameUiAtlas;
            if (_hasResolvedGameUiAtlas) return atlas != null;

            _hasResolvedGameUiAtlas = true;
            UnityEngine.Object[] atlases = Resources.FindObjectsOfTypeAll<dfAtlas>();
            if (atlases == null) return false;
            for (int index = 0; index < atlases.Length; index++)
            {
                dfAtlas candidate = atlases[index] as dfAtlas;
                if (candidate == null || candidate.Texture == null) continue;
                if (string.Equals(candidate.Texture.name, "GameUIAtlas", System.StringComparison.Ordinal) ||
                    string.Equals(candidate.gameObject.name, "GameUIAtlas", System.StringComparison.Ordinal))
                {
                    _gameUiAtlas = candidate;
                    atlas = candidate;
                    return true;
                }
            }

            return false;
        }

        private static string GetStartItemPickupFallbackLabel(string pickupType)
        {
            switch (StartItemPickupCatalog.NormalizeType(pickupType))
            {
                case StartItemPickupCatalog.KeyType: return "K";
                case StartItemPickupCatalog.RatKeyType: return "R";
                case StartItemPickupCatalog.MaxHealthType: return "H";
                case StartItemPickupCatalog.ArmorType: return "A";
                case StartItemPickupCatalog.CasingsType: return "C";
                case StartItemPickupCatalog.BlankType: return "B";
                default: return "?";
            }
        }
        private bool TryGetPickupIcon(int pickupId, out PickupIconData iconData)
        {
            if (_pickupIconCache.TryGetValue(pickupId, out iconData))
            {
                return iconData.Texture != null;
            }

            PickupObject pickup = PickupObjectDatabase.GetById(pickupId);
            iconData = CreatePickupIconData(pickup);
            _pickupIconCache[pickupId] = iconData;
            return iconData.Texture != null;
        }

        private PickupIconData CreatePickupIconData(PickupObject pickup)
        {
            // Reuse the game's live pickup sprite data so the browser does not need its own icon bundle.
            if ((object)pickup == null || (object)pickup.sprite == null)
            {
                return PickupIconData.Empty;
            }

            // Render the actual tk2d geometry instead of drawing the atlas UV bounding
            // rectangle. The latter loses rotated atlas regions and the original vertex
            // mapping, which can make long guns appear to point in the wrong direction.
            PickupIconData renderedIcon = RenderPickupIconData(pickup.sprite, pickup.PickupObjectId);
            if (renderedIcon.Texture != null)
            {
                return renderedIcon;
            }

            return CreateAtlasPickupIconData(pickup);
        }

        private PickupIconData RenderPickupIconData(tk2dBaseSprite sourceSprite, int pickupId)
        {
            if (sourceSprite == null || sourceSprite.Collection == null || sourceSprite.CurrentSprite == null)
            {
                LogPickupIconDiagnostic(
                    "Icon render skipped. PickupId=" + pickupId
                    + ", SourceSprite=" + (sourceSprite == null ? "null" : "present")
                    + ", Collection=" + (sourceSprite == null || sourceSprite.Collection == null ? "null" : "present")
                    + ", Definition=" + (sourceSprite == null || sourceSprite.CurrentSprite == null ? "null" : "present") + ".");
                return PickupIconData.Empty;
            }
            const int textureWidth = 128;
            const int textureHeight = 80;
            GameObject iconObject = null;
            GameObject cameraObject = null;
            RenderTexture renderTexture = null;
            RenderTexture previousActiveTexture = RenderTexture.active;
            string definitionName = sourceSprite.CurrentSprite.name;
            LogPickupIconDiagnostic(
                "Icon render begin. PickupId=" + pickupId
                + ", Sprite=" + definitionName
                + ", SpriteId=" + sourceSprite.spriteId
                + ", Collection=" + sourceSprite.Collection.name + ".");
            try
            {
                iconObject = new GameObject("EtgGameplayDashboard.PickupIcon");
                iconObject.hideFlags = HideFlags.HideAndDontSave;
                iconObject.layer = 31;
                tk2dSprite iconSprite = tk2dSprite.AddComponent(iconObject, sourceSprite.Collection, sourceSprite.spriteId);
                if (iconSprite == null)
                {
                    LogPickupIconDiagnostic("Icon render failed at tk2dSprite.AddComponent. PickupId=" + pickupId + ".");
                    return PickupIconData.Empty;
                }

                // tk2d applies the collection's render layer while building the sprite.
                // Reapply the private preview layer after AddComponent so the preview
                // camera can see only this temporary icon instead of the gameplay scene.
                iconObject.layer = 31;

                iconSprite.color = sourceSprite.color;
                iconSprite.scale = sourceSprite.scale;
                iconSprite.FlipX = sourceSprite.FlipX;
                iconSprite.FlipY = sourceSprite.FlipY;
                iconObject.transform.localEulerAngles = sourceSprite.transform.localEulerAngles;
                Bounds bounds = iconSprite.GetBounds();
                Renderer iconRenderer = iconObject.GetComponent<Renderer>();
                LogPickupIconDiagnostic(
                    "Icon render setup. PickupId=" + pickupId
                    + ", Renderer=" + (iconRenderer == null ? "null" : "present")
                    + ", RendererEnabled=" + (iconRenderer != null && iconRenderer.enabled)
                    + ", RendererVisible=" + (iconRenderer != null && iconRenderer.isVisible)
                    + ", ObjectLayer=" + iconObject.layer
                    + ", ObjectPosition=" + iconObject.transform.position
                    + ", RendererBounds=" + (iconRenderer == null ? "null" : iconRenderer.bounds.ToString()) + ".");
                float aspect = (float)textureWidth / textureHeight;
                float requiredHeight = Mathf.Max(bounds.size.y, bounds.size.x / aspect);
                if (requiredHeight <= 0.0001f)
                {
                    LogPickupIconDiagnostic(
                        "Icon render failed at bounds. PickupId=" + pickupId
                        + ", Bounds=" + bounds + ".");
                    return PickupIconData.Empty;
                }
                iconObject.transform.localPosition = -bounds.center;

                cameraObject = new GameObject("EtgGameplayDashboard.PickupIconCamera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                cameraObject.layer = 31;
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.cullingMask = 1 << 31;
                camera.orthographic = true;
                camera.aspect = aspect;
                camera.orthographicSize = requiredHeight * 0.55f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 20f;

                renderTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32);
                renderTexture.hideFlags = HideFlags.HideAndDontSave;
                renderTexture.filterMode = FilterMode.Bilinear;
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.filterMode = FilterMode.Bilinear;
                texture.ReadPixels(new Rect(0f, 0f, textureWidth, textureHeight), 0, 0, false);
                texture.Apply(false, false);
                Color[] pixels = texture.GetPixels();
                int visiblePixelCount = 0;
                float maximumAlpha = 0f;
                for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex++)
                {
                    maximumAlpha = Mathf.Max(maximumAlpha, pixels[pixelIndex].a);
                    if (pixels[pixelIndex].a > 0.01f)
                    {
                        visiblePixelCount++;
                    }
                }
                LogPickupIconDiagnostic(
                    "Icon render success. PickupId=" + pickupId
                    + ", Sprite=" + definitionName
                    + ", Bounds=" + bounds
                    + ", RequiredHeight=" + requiredHeight
                    + ", VisiblePixels=" + visiblePixelCount
                    + ", MaximumAlpha=" + maximumAlpha + ".");
                return new PickupIconData(texture, new Rect(0f, 0f, 1f, 1f));
            }
            catch (Exception exception)
            {
                LogPickupIconDiagnostic(
                    "Icon render exception. PickupId=" + pickupId
                    + ", Sprite=" + definitionName
                    + ", Type=" + exception.GetType().Name
                    + ", Message=" + exception.Message + ".");
                return PickupIconData.Empty;
            }
            finally
            {
                RenderTexture.active = previousActiveTexture;
                if (renderTexture != null)
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                if (cameraObject != null)
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                if (iconObject != null)
                    UnityEngine.Object.DestroyImmediate(iconObject);
            }
        }

        private void LogPickupIconDiagnostic(string message)
        {
            if (IsPickupBrowserPerformanceLoggingEnabled() && _performanceLogger != null)
            {
                _performanceLogger.LogInfo(EtgGameplayDashboardLog.Performance("PickupBrowserIcon: " + message));
            }
        }

        private static PickupIconData CreateAtlasPickupIconData(PickupObject pickup)
        {
            // Fallback for unusual sprites that cannot be instantiated by tk2d.

            tk2dSpriteDefinition definition = pickup.sprite.CurrentSprite;
            if (definition == null || definition.material == null || definition.uvs == null || definition.uvs.Length == 0)
            {
                return PickupIconData.Empty;
            }

            Texture texture = definition.material.mainTexture;
            if (texture == null)
            {
                return PickupIconData.Empty;
            }

            float minX = definition.uvs[0].x;
            float minY = definition.uvs[0].y;
            float maxX = minX;
            float maxY = minY;
            for (int index = 1; index < definition.uvs.Length; index++)
            {
                Vector2 uv = definition.uvs[index];
                minX = Mathf.Min(minX, uv.x);
                minY = Mathf.Min(minY, uv.y);
                maxX = Mathf.Max(maxX, uv.x);
                maxY = Mathf.Max(maxY, uv.y);
            }

            return new PickupIconData(texture, Rect.MinMaxRect(minX, minY, maxX, maxY));
        }
    }
}
