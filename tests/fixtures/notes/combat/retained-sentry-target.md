# Removed character referenced by a later Sentry callback

Natural random10 Banished/Wurmkin Ring8,seed1895012796,Covenant0,uses
checkpoint`campaign-ring8-20261026-032228-4e287604`,saveSHA256
`6e1a44d82a6c2c9977ed95a68ee9b0a844fc17878d5f189da7c39c81465a35f8`.
The0.2.97 actual default run stops at input11,JustCause36 targeting unit3
on floor2. Champion2 has15 health and Penitent shift-damage equipment.
Its shift kills it; removal completes before the later enemy Sentry callback.

The original callback holds the same mutable character reference. The model
queued an immutable override snapshot,then recreated that old target after
it left the live room. This overwrote the completed retained destruction:
IsDestroyed true became false and retained room-1 became2. Queued overrides
now prefer the current retained character before falling back to the queued
snapshot; existing live-room targets retain precedence.

Original and independent corrected native processes take46.16/45.37seconds.
All11 complete successor states and RNG match the correction; the original
wrong forecast is retained as a counterexample. The10-input prefix includes
the preceding nonlethal equipment shift. Eight parallel final-input branches
preserve the parent. Existing nonlethal/lethal Sentry native callbacks,pure
queue order/once/silence/dead-target gates and preview-binding checks pass.
This component pair is not a Ring8 win; the same checkpoint's actual default
full battle is being rerun with0.2.98.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --retained-sentry-target-only `
  tests/fixtures/combat/rooms/player-retained-sentry-target-calibration.mt2f `
  tests/fixtures/combat/rooms/player-retained-sentry-target-replay-calibration.mt2f
```
