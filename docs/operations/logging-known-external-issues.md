# Known External Runtime Log Issues

Use this page when reviewing `BepInEx\LogOutput.log` and separating game-owned resource or compatibility messages from
Control Panel defects.

## Current Findings

### `select_idle`

The game reports:

```text
Unable to find clip 'select_idle' in library
Calling clip.Play() with a null clip
```

`select_idle` is a tk2d character-selection/foyer idle animation clip, not an audio resource. Some character animation
libraries do not contain the clip. The current effect is limited to the affected idle animation; Control Panel
operations continue to work.

Status: **Not fixed for now.** This is a game animation-resource issue, not a Control Panel core-logic issue.

### `_StencilVal`

The game reports:

```text
Material doesn't have a float or range property '_StencilVal'
```

The message appears while the game is overriding the hand shader or rebuilding character-related UI/material state.
Control Panel's cursor material uses `UI/Default` and `_StencilComp`; the project does not use or set `_StencilVal`.

Status: **Not fixed for now.** This is a game material/shader compatibility issue, not a Control Panel core-logic issue.

### `XInputInterface32.dll`

The game reports that it cannot load:

```text
EtG_Data/Plugins/XInputInterface32.dll
expected x64 architecture, but was x86 architecture
```

This is the game's bundled 32-bit XInput compatibility DLL being loaded by the 64-bit game process. It is not a
Control Panel dependency and is not loaded by project code.

Status: **Not fixed or suppressed for now.** The message belongs to the game's own compatibility layer.

## Triage Rule

These messages should not be treated as Control Panel errors unless the same reproduction also contains an
`[EtgGameplayDashboard]` exception stack or a failed Control Panel operation. In the current baseline, character
switching, teleport, and pickup operations complete as expected, with no project exception stack associated with these
messages.
