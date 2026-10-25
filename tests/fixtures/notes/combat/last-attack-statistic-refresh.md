# Preview statistic queries retain empty native entries

Random campaign11,Stygian/Awoken,seed73751504,Covenant0,Ring7,
`Level7BattleHordeSilence`. The actual10000ms/two-worker policy reached input35,
an EndTurn from turn4 to5. Sting35 is generated and drawn but never played.
Native deckStats contains its empty entry; the original prediction only stores
cards1..34,while both predictions correctly track ownership1..35. The original
capture preserves that sole mismatch. Its request Reason is the historical
diagnostic label "Native enemy death card-statistics retention."; death is not
the mechanism.

A passive,opt-in Harmony trace identifies
CardEffectAddStatusEffectPerLastAttackDamage.ApplyEffect -> GetStatValue ->
UpdateDeckStats. The getter materializes owned cards even in combat preview.
Version0.2.107 performs the query and retains its storage membership when
restoring preview state,along with the already retained last-attack scalar.
It does not copy preview deaths,spawns or other event counters into live state.
Without a query,new ownership alone still does not materialize an entry.

Two independent native processes agree on all35 complete successors,forecasts
and RNG; the repaired process runs without tracing. Eight parallel branches
preserve the parent. Scalar/counter/no-query controls and existing preview birth,
binding,Boss test-RNG,terminal attribution and remote Harvest pairs pass.
This closes a rejected branch,not the entire seventh battle.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --last-attack-statistic-refresh-only `
  tests/fixtures/combat/status/player-last-attack-statistic-refresh-calibration.mt2f `
  tests/fixtures/combat/status/player-last-attack-statistic-refresh-replay-calibration.mt2f
```
