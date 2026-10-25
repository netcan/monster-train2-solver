# Deferred spell death and cross-floor Harvest

Retained metagame campaign,Banished/Pyreborne,seed424245,Covenant0,Ring6,
checkpoint `campaign-ring6-20261025-210057-b78f4bf9`,saveSHA256
`b90d38a7460c6a999a5c9f8c6cd3432450261942cd57e257689f97fa6a43ae2e`.
Late alternatives30..60 of the verifiedv3 model loss complete99/99
continuations in53.79s,29 unsupported attempts,no winning line. One rejected
branch supplies a34-input prefix and a final FireStarter24 cast at turn6.

The original native35-input observation preserves the exact Tower-targeting
refusal. Card24 (`fb0683b1-20ac-4dd2-b580-14b502e182f4`) kills1HP
Cleric20 in room0. Entropic8's OnAnyHeroDeathOnFloor triggers BuffMaxHealth50
on its Savage companion6 in room2: HP1022->1072,maxHP1250->1300. The first
0.2.77 roster repair handled the immediate native damage queue; the later
generic spell callback drain still used a room-only snapshot.

Version0.2.79 supplies remote units to this deferred queue only when the
caller owns a full train,merges retained remote updates into their original
rooms,and avoids exporting living borrowed units as dead/retained actors.
An independent original native process repeats all35 inputs and agrees on
every complete state/RNG,with immutable parents and pure capture. Source
runtime101.81s; the room-only direct damage API still rejects Tower callbacks.
The shared pure checks pass once. This is actual cross-floor effect evidence,
not a complete battle win or a demonstration of arbitrary remote callbacks.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --spell-harvest-tower-only `
  tests/fixtures/bosses/angel-duo/player-spell-harvest-tower-calibration.mt2f `
  tests/fixtures/bosses/angel-duo/player-spell-harvest-tower-replay-calibration.mt2f
```
