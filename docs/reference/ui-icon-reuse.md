# UI Icon Reuse

Use this page when changing command-panel or Start Items icons and you want to reuse ETG runtime art instead of shipping a separate icon bundle.

This page intentionally covers the reproducible paths that do not require new reverse engineering work.

## Two Reuse Paths

### Live pickup icons

Normal guns, passives, and actives in the pickup browser do not use hardcoded atlas coordinates.

They reuse the live pickup sprite that already exists on the runtime `PickupObject`:

- `PickupObjectDatabase.GetById(...)`
- `pickup.sprite.CurrentSprite`
- `tk2dSpriteDefinition.material.mainTexture`
- `tk2dSpriteDefinition.uvs`

Implementation reference:

- [InGameCommandController.PickupBrowser.cs](/C:/code/ETG-Gameplay-Dashboard/src/RandomLoadout/Commands/InGameCommandController.PickupBrowser.cs:849)

Practical consequence:

- if the thing is a real pickup with a normal ETG world/UI sprite, prefer reusing its runtime `tk2d` sprite data
- no separate atlas export is needed
- no hardcoded UV table is needed

### Start Items preset resource icons

Preset pickup rows such as health, key, rat key, blank, and casings are not normal pickup entries, so they cannot reuse `PickupObject.sprite`.

Those rows reuse ETG's `GameUIAtlas` at runtime through `dfAtlas`:

- find the runtime `dfAtlas` whose texture or GameObject is named `GameUIAtlas`
- resolve a sprite by name with `atlas[spriteName]`
- read `dfAtlas.ItemInfo.region`
- draw that region from `atlas.Texture`

Implementation reference:

- [InGameCommandController.LoadoutEditor.cs](/C:/code/ETG-Gameplay-Dashboard/src/RandomLoadout/Commands/InGameCommandController.LoadoutEditor.cs:898)
- [InGameCommandController.PickupIcons.cs](/C:/code/ETG-Gameplay-Dashboard/src/RandomLoadout/Commands/InGameCommandController.PickupIcons.cs:1)

Practical consequence:

- for these UI-only resource rows, prefer `dfAtlas` lookup by sprite name
- do not reintroduce hand-written atlas UV rectangles unless there is no runtime atlas path available
- this keeps sprite-name changes and atlas-region lookup in the same source of truth

## Current Reproducible Sprite Names

These mappings are safe to reuse today for the Start Items preset pickups UI:

- `key` -> `ui_keybullet_idle_002`
- `rat_key` -> `room_rat_reward_key_001`
- `max_health` -> `heart_full_001`
- `armor` -> `armor_shield_pickup_001`
- `blank` -> `blank_item_001`
- `casings` -> `ui_coin_idle_002`
- `hegemony` -> `hbux_text_icon`

Implementation reference:

- [InGameCommandController.PickupIcons.cs](/C:/code/ETG-Gameplay-Dashboard/src/RandomLoadout/Commands/InGameCommandController.PickupIcons.cs:1)

## Rat Key Note

For preset pickups, `room_rat_reward_key_001` is the intended rat-key icon.

This icon represents the key used to open the Rat Chests in the reward room after defeating the hidden Chamber 3 boss, the Resourceful Rat. That reward room contains four special Rat Chests, and this sprite is the chest-opening reward-key icon for that context.

Do not silently swap this mapping to `resourcefulrat_key_001` when working on the preset pickups UI. If a future UI surface means a different rat-key concept, document that distinction in code next to the mapping.

## Armor Note

For preset pickups, `armor_shield_pickup_001` is the intended armor icon.

This is the pickup-style armor sprite from `GameUIHeartController.armorSpritePrefab -> ArmorPiece`, so it matches the "resource row / pickup" meaning used by the Start Items preset pickups UI.

Do not silently swap this mapping back to `armor_shield_heart_idle_001`. That other sprite is still relevant in ETG, but it belongs to the HUD-style armor-heart presentation path rather than the pickup-style armor icon path.

## Hegemony Note

For the pickups/resources-style control-panel action row, `hbux_text_icon` is the intended Hegemony icon.

This is the ETG UI atlas icon used for Breach meta currency, so it is a valid runtime atlas resource and does not require shipping a separate icon asset.
