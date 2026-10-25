# Retired enemy slot during terminal combat

Stygian/LazarusLeague seed972508374, first battle, a37-input alternate
native route kills miniboss11 while ClericT1_OminousAcolyte12 still has1HP.
Native victory removes the Cleric from the room and its logical ownership,
but the physical enemy slot1 temporarily retains12 during visual cleanup.
The historical component preserves this exact stale-slot failure.

0.2.48 keeps all raw primary/preview/temporary planes and canonicalizes
only inactive enemy occupants during an observed won stopping/terminal
capture. Living occupants, player slots, defeat captures and ordinary
coroutine captures retain their existing behavior. The scoped player
terminal reader explicitly receives the native result because the player
assembly has a separate trace from the development observer.

Room/train wrappers retain their native in-flight query frames while the
stopping result scopes the slot projection. Delaying them until StopCombatLoop
instead would change RunningCombat and compare different native boundaries.
The full battle endpoint remains AfterStopCombatLoop, with effects settled.

Independent native processes match37 inputs, complete successors, forecasts,
RNG and immutable parents. The historical comparison projects only the proven
single stale slot in memory; the archived native source is unchanged. The
repaired companion archive independently passes40 room and9 train stages,
203 raw/canonical mappings and3742 primary/preview/temporary plane mappings.
CaptureFailures/Mismatches/Unsupported/Pending are all0.
This mechanism calibration does not establish a full campaign victory.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-terminal-enemy-slot-calibration.mt2f
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture full-battle-terminal-enemy-slot-calibration.mt2f
```
