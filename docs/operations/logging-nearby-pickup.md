# Nearby Pickup Logging

Use this guide when the nearby pickup info overlay fails to appear, shows the wrong item, or behaves differently for dropped loot, shop merchandise, or boss reward pedestals.

## Enable The Logs

Config file:

- `BepInEx\config\ETG-Gameplay-Dashboard.cfg`

Switch:

```ini
[Debug]
EnableNearbyPickupVerboseLogs = true
```

Turn it back off after reproducing the issue.

## What It Captures

When enabled, the mod writes nearby-pickup diagnostics for both startup file loading and in-run pickup detection.

Current nearby-pickup diagnostics include:

- startup input-file inspection for:
  - `EtgGameplayDashboard.pickup-gameplay.json`
  - `EtgGameplayDashboard.pickup-info-terms.json`
- file existence and byte-size snapshots for those inputs
- nearby-pickup registry hit / miss diagnostics when a pickup enters range
- overlay render-path warnings when the service has a visible pickup target but no gameplay entry can be resolved
- event-driven tip show / clear traces for:
  - dropped pickups
  - shop items
  - reward pedestals
- blueprint-resolution traces when the slot mapping cannot be used, including the
  blueprint ID, copied journal metadata, candidate pickup IDs, field-by-field match
  results, and the final resolved pickup
- Breach NPC shop slot-resolution traces, including the shop type, live
  spawn-position slot, merchant subtype, and pickup ID read from the current stock list
- dropped-pickup enter-range, exit-range, and consumed callback traces, including pickup type, ID, and Unity instance ID
- a fallback-clear warning when a visible tip's Unity range source has been destroyed without a matching consumed or exit-range callback; the service clears the tip immediately

## Good Repro Cases

Use these to narrow the failure:

- stand near a normal dropped pickup in a combat room
- stand near an unpurchased item in Cadence & Ox's Breach shop
- stand near an unpurchased item in Doug's Breach shop when Doug is present
- stand near an unpurchased item in Professor Goopton's Breach shop
- stand near a boss reward pedestal item before picking it up
- compare a nearby item that does show overlay info with one that does not

## Breach NPC Shop Resolution

Cadence & Ox, Doug, and Professor Goopton all use a `BaseShopController` configured as
`FOYER_META`. Each visible item is an `ItemBlueprintItem` wrapper whose copied journal
metadata can match multiple real pickups, including normal and synergy variants. The
blueprint's own `PickupObjectId` is therefore not the item identity.

The authoritative item is the live entry in the shop controller's `m_shopItems` list.
`IsBeetleMerchant` is logged for diagnostics only; it is not used to select this path.

1. Verify the parent shop is a foyer-meta `BaseShopController`.
2. Match the visible `ShopItemController` parent transform to the shop's
   `spawnPositions` array.
3. Read `m_shopItems[slotIndex]` and resolve its `PickupObject`.

This is intentionally slot-based rather than position-name-based. Shop stock can
change or be rebuilt, so the same visual position is not a permanent item identity.
The `Foyer meta shop slot mapping` verbose log records the shop type, merchant subtype,
slot index, and live target pickup ID.

The resolver still retains a compatibility path for other ETG `MetaShopController`
shops, but the three Breach NPC shops use this `m_shopItems` slot mapping.

## Lines To Watch

Healthy behavior usually includes:

- `Pickup gameplay Gameplay file: Path='...', Exists=True, SizeBytes=...`
- `Pickup gameplay Terms file: Path='...', Exists=True, SizeBytes=...`
- `Loaded pickup gameplay info v2 from '...' (668 entries).`
- `Loaded pickup gameplay terms v2 from '...'.`
- `Nearby pickup tip shown. Source=pickup ... HasGameplayEntry=True.`
- `Nearby pickup consumed callback. PickupType=... PickupId=...`
- `Nearby pickup tip cleared. Reason=pickup_consumed.`

Useful failure clues include:

- `Pickup gameplay info file was not found at '...'.`
- `Pickup info terms file was not found at '...'.`
- `Loaded pickup gameplay info v2 from '...' (0 entries).`
- `Nearby pickup entered range but gameplay entry was not found. Source=...`
- `Nearby pickup overlay had a visible tip source, but no gameplay entry was resolved for rendering.`
- `Blueprint resolution result: MatchCount=...`
- `Meta shop slot mapping: ControllerIndex=..., Tier=..., SlotIndex=..., TargetPickupId=...`
- `Foyer meta shop slot mapping: ShopType=..., IsBeetleMerchant=..., SlotIndex=..., TargetPickupId=..., TargetType=...`

Interpretation hints:

- file missing:
  the new schema-v2 runtime files were not deployed into `BepInEx\config\`
- `0 entries`:
  the runtime file was found, but the loader parsed no pickup records
- `entered range but gameplay entry was not found`:
  the ETG runtime event fired, but the loaded gameplay registry did not contain that `pickupId`
- `visible tip source, but no gameplay entry was resolved`:
  the service selected a target, but the overlay draw path still failed to resolve a matching gameplay record
- `range source was destroyed without a matching pickup-consumed or exit-range callback`:
  the pickup disappeared through an override method that bypassed the hooked base callback. The service clears the stale tip through its update fallback.

## Follow-Up

If the target is detected but still does not render, inspect the overlay drawing path in:

- `src/EtgGameplayDashboard/Plugin.PickupWikiTips.cs`

If the target is never detected, inspect the runtime scan path in:

- `src/EtgGameplayDashboard/Runtime/NearbyPickupTipService.cs`
