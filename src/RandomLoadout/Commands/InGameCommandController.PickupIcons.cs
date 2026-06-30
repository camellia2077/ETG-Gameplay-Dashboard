namespace RandomLoadout
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
    }
}
