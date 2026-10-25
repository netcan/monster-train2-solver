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
