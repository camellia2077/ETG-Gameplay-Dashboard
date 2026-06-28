# Muncher Spawn Logging

Use this page when you need the detailed runtime diagnostics for `Spawn Gunber Muncher` and `Spawn Evil Muncher`, which create `Gunber Muncher（常规吃枪怪）` and `Evil Muncher（邪恶吃枪怪）`.

## Enable

Set this in `BepInEx\config\ETG-Gameplay-Dashboard.cfg`:

```ini
[Debug]
EnableMuncherVerboseLogs = true
```

Default:

- `false`

## What It Adds

When enabled, the plugin emits detailed `[RandomLoadout][Command]` lines for:

- spawn request entry
- loading-scene queue and retry polling
- original room asset resolution
- prefab fallback resolution
- selected spawn cell and existing muncher count
- post-instantiate state snapshots
- successful room registration

Typical useful lines include:

- `Gunber Muncher spawn requested`
- `Gunber Muncher deferred spawn poll`
- `Gunber Muncher source behaviour resolved`
- `Gunber Muncher prefab resolved`
- `Gunber Muncher post-instantiate snapshot`
- `Gunber Muncher spawned successfully`
- `Evil Muncher spawn requested`
- `Evil Muncher source behaviour resolved`
- `Evil Muncher post-instantiate snapshot`
- `Evil Muncher spawned successfully`

## What Still Logs When Disabled

When `EnableMuncherVerboseLogs = false`, the high-frequency trace lines stay silent.

Failure-oriented warnings still remain visible, such as:

- spawn queue failure because `GameManager` is unavailable
- deferred spawn timeout
- room asset or prefab load exceptions
- instantiate exceptions
- rigidbody initialization exceptions
- spawn returned `null`

That means normal play logs stay much quieter, while broken spawns still leave actionable warning lines behind.

## Typical Workflow

1. Reproduce the spawn issue in-game.
2. Turn on `EnableMuncherVerboseLogs`.
3. Trigger `Spawn Gunber Muncher` or `Spawn Evil Muncher`.
4. Read `BepInEx\LogOutput.log`.
5. Check:
   - whether the request queued because the scene was still loading
   - whether the correct bundle and room asset resolved
   - whether a source `DungeonPlaceableBehaviour` was found
   - which spawn cell was chosen
   - whether the object registered into the room after spawn

## Read Next

- Logging overview:
  [./logging.md](./logging.md)
- Runtime internals reference:
  [../reference/runtime-internals/muncher-spawn.md](../reference/runtime-internals/muncher-spawn.md)
