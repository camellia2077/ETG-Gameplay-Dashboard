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
- dropped-pickup enter-range, exit-range, and consumed callback traces, including pickup type, ID, and Unity instance ID
- a fallback-clear warning when a visible tip's Unity range source has been destroyed without a matching consumed or exit-range callback; the service clears the tip immediately

## Good Repro Cases

Use these to narrow the failure:

- stand near a normal dropped pickup in a combat room
- stand near an unpurchased shop item in Bello's shop
- stand near a boss reward pedestal item before picking it up
- compare a nearby item that does show overlay info with one that does not

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
