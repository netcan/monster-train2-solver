# Replacing an identical attachment reinstalls unit upgrades

Fourth random campaign, Hellhorned/Wurmkin seed770862752, sixth battle,
input37 plays FightClub9 in room2. The room already has the same attachment
payload. Native removes its old upgrade object and appends a fresh one,
after the champion's later WurmEchoInfusionHealth upgrade. Creation count
advances1to2; champion HP40 and attack40 remain unchanged.

The old equality check compared payloads and skipped synchronization.
It now includes the captured attachment creation count. Existing complete
remove/install logic restores native upgrade-list order without duplicating
stats. Two independent native processes match all37 inputs, full successors,
RNG and immutable parents, preserving the old order mismatch. The existing
twenty-input original-relic replacement pair and pure attachment controls
also pass. No sixth-battle or full-campaign victory is inferred.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-attachment-reinstall-calibration.mt2f
```
