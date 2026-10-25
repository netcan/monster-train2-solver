# Consume relic damage and the complete Tower roster

Retained Banished/Pyreborne campaign,seed424245,Covenant0,Ring6,
checkpoint `campaign-ring6-20261025-210057-b78f4bf9`,saveSHA256
`b90d38a7460c6a999a5c9f8c6cd3432450261942cd57e257689f97fa6a43ae2e`.
The offline v5 alternative supplies 21 verified inputs followed by HeroicDive21
on champion2 in room0,turn4. This is a component calibration,not a battle win.

DamageOnConsume (`2d2450a5-4ed5-4d9a-bba2-15c9eb0eee6d`,
RelicEffectDamageOnExhaust) drains its damage callbacks through UnitCloneModel's
shared queue. That queue lacked remote actors,so Savage's Tower revenge was
rejected even though both companions currently occupy room0. The original
22-input capture retains that exact refusal. Version0.2.107 supplies the other
current floors and merges retained remote actors after each callback; the
room-only combat API still requires a complete roster for Tower targeting.

The independent replay matches every state,forecast and RNG. Savage6 loses
1086->927HP; Entropic8 gains 15->17attack; champion2 remains20HP. Eight parallel
branches preserve their parent. Both native processes preserve protected files.
The existing spell-Harvest pair separately covers an actual cross-floor update;
this new branch does not establish arbitrary remote effect support.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --relic-damage-tower-roster-only `
  tests/fixtures/bosses/angel-duo/player-relic-damage-tower-calibration.mt2f `
  tests/fixtures/bosses/angel-duo/player-relic-damage-tower-replay-calibration.mt2f
```
