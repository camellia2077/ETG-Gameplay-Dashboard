# Boss Audio Diagnostics

The plugin always captures targeted Wwise events emitted by TankTreader and its child objects. It does not log unrelated game audio.

The diagnostics cover:

- `TankTreaderIntroDoer.StartIntro`, which should start `Play_BOSS_tank_idle_01`;
- `TankTreaderIntroDoer.OnCleanup` and `TankTreaderController.OnDestroy`, which should stop the Boss idle event;
- `TankTreaderDeathController.OnBossDeath`, which marks the start of the manual death-animation path;
- `AkSoundEngine.PostEvent(string, GameObject)` calls attached to the TankTreader hierarchy, including hurt, death, idle, and stop events.

The runtime also applies two TankTreader-specific corrections. It sets the death-audio override to `Play_BOSS_tank_death_01`, because the vanilla prefab currently falls back to the generic `Play_CHR_general_death_01`, and it restores `Play_BOSS_tank_idle_01` after a replayed Boss is spawned because replay intentionally skips the native intro coroutine.

Each event records the phase, object, Unity frame, scene, Wwise event name, and returned playing ID. A zero playing ID is useful evidence that Wwise rejected or failed to start an event.

Look for:

```text
Boss audio diagnostic. Phase=PostEvent, ... Event=..., PlayingId=...
Boss audio diagnostic. Phase=TankTreaderDeath.OnBossDeath, ... DeathAnimationStarted=True
```

For a rewind reproduction, compare the original Boss entry with the replayed Boss. Replay intentionally skips the native Boss intro coroutine, so the log can show whether the replay path starts or stops `Play_BOSS_tank_idle_01` and whether a stale original object later issues `Stop_BOSS_tank_idle_01`.
