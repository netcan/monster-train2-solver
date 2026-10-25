# Native removal timing

The natural Remnant/Awoken fifth battle heals MonsterHarvestAttack during room0
combat. Its heal relic kills enemy15; enemy14 remains in physical slot1 until
the room completes. The original raw room-stage30 predicted an early compact.
The original complete49-input trace is retained as a counterexample, not an
accepted complete loss. An independent19-input native component repeats the
same prefix and raw before/after, with complete successors/RNG and zero raw
capture/mismatch/unsupported/pending counts. The checker replays the raw room,
asserts enemy14's vacancy, and permits only physical references in the historical
counterexample projection. Played-card/ability death centering and previews
keep their separately calibrated behavior.

The natural Hellhorned/Wurmkin fifth battle also exposes a living
TreasureBagLvl2Gold vanishing at input27. Native destruction clears its permanent
and once statistics listeners without firing death or setting removal/death
flags. The original27-input rolling failure is compared with an independent
settled component. Only those two listener fields may differ in the old forecast;
all native inputs,roots,successors,RNG and immutable parents remain exact.

Component probes must use the same death-dissolve/card-animation settlement
protocol as the original source. Captures made before the native removal flush
retain a destroyed enchanter binding and are rejected. The observer also allows
two ready frames for Unity's deferred destruction callbacks; it changes no
gameplay values or RNG. These are component validations, not campaign wins.

A later complete39-input native run exposed a finer boundary: clearing the
vanishing bag's listeners inside the room was premature. Raw room-stage26
retains both listeners; the settled decision after the native removal flush
clears them. Cleanup now belongs to the existing frame-destruction transition.
The original premature raw forecast and an independent27-input component are
retained as `player-despawn-frame-removal-*`. The checker compares every native
input/root/successor/RNG, replays the raw room without mutating its parent,
and requires the two different listener states at the two observed times.
All four counters in the repaired raw component are zero. The original full
defeat has nonzero historical raw counters and remains a diagnostic archive.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-heal-combat-removal-calibration.mt2f,player-despawn-listeners-calibration.mt2f
```
