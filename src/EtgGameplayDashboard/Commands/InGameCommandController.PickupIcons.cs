// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

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
            UnityEngine.Object[] atlases = Resources.FindObjectsOfTypeAll(typeof(dfAtlas));
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
    }
}
