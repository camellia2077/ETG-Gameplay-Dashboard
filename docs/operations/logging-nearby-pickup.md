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

When enabled, the mod writes `[RandomLoadout][Run]` lines for:

- refresh skips caused by missing player, room, or gameplay catalog data
- dropped-pickup scan counts and accepted candidates
- shop-item scan counts and accepted candidates
- reward-pedestal scan counts and accepted candidates
- gameplay-catalog hits and misses for nearby pickups
- candidate skip reasons such as out-of-range, already picked up, or invalid object state
- final overlay target selection, including whether the source was debris or a shop item

## Good Repro Cases

Use these to narrow the failure:

- stand near a normal dropped pickup in a combat room
- stand near an unpurchased shop item in Bello's shop
- stand near a boss reward pedestal item before picking it up
- compare a nearby item that does show overlay info with one that does not

## Lines To Watch

Healthy behavior usually includes:

- `Nearby pickup scan started. ...`
- `Nearby pickup candidate accepted. Source=ShopItem ...` or `Source=Debris ...`
- `Nearby pickup selected. Source=...`

Useful failure clues include:

- `Nearby pickup scan cleared because player, room, or gameplay registry was unavailable.`
- `Nearby pickup candidate missing gameplay entry. Source=ShopItem ...`
- `Nearby pickup scan completed without a visible target.`

## Follow-Up

If the target is detected but still does not render, inspect the overlay drawing path in:

- `src/RandomLoadout/Plugin.PickupWikiTips.cs`

If the target is never detected, inspect the runtime scan path in:

- `src/RandomLoadout/Runtime/NearbyPickupTipService.cs`
