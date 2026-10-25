# Repeated Infusion upgrade triggers

Natural random campaign06, Hellhorned/LazarusLeague, seed1848503146,
Covenant0, Ring2 exposed a prediction mismatch in0.2.61. The unchanged
checkpoint is `campaign-ring2-20261025-170537-7fb0f2c7`, saveSHA256
`2b5354ae41968d06038eb1f1313c81f563672c2c0ec54c1de77168bbbf6eff0d`.

BlastInfusion26 has permanent upgrades in the order UnstableSingle,
DamageSingle, UnstableSingle. At turn4 it targets1HP acolyte18 in room0,
alongside spearman17 and boss16 at260HP. Native merges the two equal upgrade
triggers first:10 unstable stacks, then4damage. The acolyte explodes, killing
the spearman and leaving the boss at250HP. The old model ran5stacks,4damage,
then5stacks against an already removed target: boss255HP and only5 recorded
status stacks in both ThisBattle and ThisTurn counters.

Native source `CardState.MergeUpgradeTriggers` groups equal trigger data in
first-occurrence order, multiplying ParamInt and status stack counts. Both
`ElixirHelper.TryGenerateUpgradedElixir` and CardManager's Infusion/CraftedSpike
pile insertion invoke it. The model now groups these cards' equal upgrade
definitions and scales base action payloads before spell-power resolution.
Ordinary cards retain separate upgrade triggers; numeric upgrade records and
their ordering remain intact.

Skip the additional effective-trait query if no upgrade has an OnCast trigger.
An isolated build replayed the same27-input native pair and resolved all26
card instances at the recorded failure root without changing the result hash
`39b9f2b7be07ca3a32abe1c0b8a70ca58c827bc15c901bd183847ae6f52dfb18`
or mutating the parent. Three warmed26000-query trials in separate PowerShell
processes measured baseline allocations693824768bytes versus565864768bytes
after the guard (18.4% less); last-two timings were802.95/816.50ms versus
688.62/705.34ms. This is a query microbenchmark including harness overhead,
not a game-frame or whole-search performance claim. Active native campaign
DLLs remained pinned while the isolated build was measured.

The original0.2.61 diagnostic and independent0.2.62 replay execute the same27
native inputs (26 prefix inputs plus the mismatching cast). Every current
successor/full state/RNG matches both native processes. The original255HP
prediction and5-stack counters remain in the historical archive; only those
three known differences are allowed when checking that historical prediction.
Parent states remain immutable. Both native runs preserve original files.
This is a mechanism calibration, not a terminal Ring2 win claim.

Additional controls reverse the interleaving to Damage/Unstable/Damage:
base4+4 becomes8 before a separate2 spell-power upgrade, yielding10damage
once. Removing the Infusion trait retains two separate6damage triggers.

```powershell
./scripts/Check-Models.ps1 -Fixture player-infusion-trigger-merge-calibration.mt2f -SkipBuild
```
