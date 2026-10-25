# Floor-transition relic piercing damage

Random09 Ring8,Wurmkin/Hellhorned,seed771950956,Covenant0. The preserved
checkpoint is `campaign-ring8-20261026-001033-9de97b88`,saveSHA256
`a654ab82627a23b7d506167d55488b524af7fd9ecdaab5e999f380f5cbd935af`.
The original 11-input observation preserves the incorrect forecast: two enemies
with Titanskin5 ascend from room0 to room1 after turn2. Native health changes
250->247 and 5->2 while the old forecast incorrectly leaves both unchanged.
An independent 0.2.86 process repeats the same request with corrected forecasts.

The read-only original balance/runtime witness identifies
`RelicEffectDamageEnemyOnAscend`, `RelicEffectDamageEnemyOnDescend` and
`RelicEffectDamageEnemyOnFriendlyTrigger` as piercing effect classes. The live
`DamageOnFloorTransition` relic owns the first two classes and reports piercing;
ordinary damage reports false. The model now passes piercing=true for the
validated floor-transition relic. A direct control keeps ordinary damage3
blocked by Titanskin5 while piercing damage3 removes health3.

Both observations match all 11 full successor states, forecasts, RNG and
immutable parents. The historical incorrect forecast remains in the source
archive. This is component calibration, not a complete eighth-battle victory.
The existing 22-operation up/down/rooted/immobile/player-movement fixture is
also checked separately; this pair specifically covers enemy ascension.

```powershell
./scripts/Check-Models.ps1 -Fixture player-floor-transition-piercing-calibration.mt2f -SkipBuild
```
