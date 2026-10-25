# Shared companion roster at local death boundaries

The unchanged campaign10 Banished/Wurmkin Covenant0 Ring6 checkpoint,
seed1895012796, completes52 actual10000ms/default advice inputs in
`20261026-020722-5bdc2ce4`. All41 paid actions and11 EndTurns match their
complete native successors/forecasts/RNG, but room Exchange75 retains one
intermediate context mismatch. Companion6 dies on room0; each native room
then captures the same global live companion roster `[8]`. The model only
updates room0, leaving `[6,8]` on remote floors in the enchantment world.
Boss health149→removed and1015→912 already agree.

`FullBattleTrace.CaptureRoom` explicitly derives this roster from characters
across the entire train, using alive/undying, destruction and companion
predicates. The room engine removes the dying companion from its own
snapshot, but `EnchantmentWorldModel.Sync` formerly reused each remote
room's older roster. Sync now publishes the captured current roster to
remote rooms too, while retaining legacy unknown snapshots when absent.
It does not move remote units or borrow their room preview flags.

`player-companion-world-roster-calibration.mt2f` preserves the original
one-mismatch capture. Independent frozen replay
`20261026-021844-6c531c60` completes the same52 inputs with all four failure
counters zero and no searches/publications. Both remain native defeats.
Their98 room stages,22 train phases, paid inputs, EndTurns and card cycles
have identical native Before/Actual states. The focused checker recomputes
every native successor and all stages, including the full external-target
roster required by room combat. Four concurrent exchanges share an
immutable native parent and match the serial branch.

The prior natural56-input paired-boss cache calibration and the temporary
preview-birth pair pass targeted checks. No full regression or Mac build
was run. This closes a simulation boundary, not the remaining strategy
quality goal or an eight-battle random campaign clear.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll `
  --companion-world-roster-only `
  tests/fixtures/bosses/angel-duo/player-companion-world-roster-calibration.mt2f `
  tests/fixtures/bosses/angel-duo/player-companion-world-roster-replay-calibration.mt2f
```
