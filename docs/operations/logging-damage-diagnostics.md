# Damage Diagnostics Logging

Use this guide when comparing the x100 player damage multiplier across guns, especially when a gun appears unchanged or a single hit kills a Boss.

Enable in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableDamageDiagnosticsVerboseLogs = true
```

Each player damage event records the actual damage, fatal flag, target health after the hit, target max health, Boss state, current gun ID/name, configured damage multiplier, final player `Damage` stat, and the current projectile's base damage when available. `ProjectileSource=none` means no ordinary current-gun projectile was available at the callback, which is useful for identifying beams or special effects.

Look for `[RandomLoadout][Damage]` lines. Compare the same gun at x1 and x100: `Damage` is the value applied to the target, `ConfiguredDamageMultiplier` is the selected button value, `PlayerDamageStat` shows the final stat, and `ProjectileBaseDamage` helps identify special projectiles whose damage is fixed or calculated elsewhere. Boss DPS caps and other vanilla Boss rules can limit the observed result, so compare `IsBoss=True` hits separately.

Disable the switch after reproducing the issue because it logs every damage event.
