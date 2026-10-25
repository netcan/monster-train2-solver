# Guaranteed status preview retains its captured test RNG

Stygian/LazarusLeague seed972508374, third battle after keeping the champion:
five native plays followed by EndTurn expose Tethys's guaranteed15 Frostbite
preview. Its CardEffectAddStatusEffect chance is100. Native still consumes
Range(0,100) from BattleTest; the live Battle stream does not advance.
Unlike the earlier four-input guaranteed-status witness, this battle captures
the complete automatic enchantment world and therefore also records BattleTest.

0.2.50 advances that captured test stream for each guaranteed preview chance
draw and retains the observed stream when restoring the ordinary preview.
The explicit isolated preview protocol continues restoring its original stream.
No status outcome is resampled and no native RNG stream is ignored by comparison.

Independent native processes match all6 complete successors, forecasts and
RNG states, with immutable parents. The historical fixture's expected state
differs only in the complete test stream; the checker projects only that field
in memory when checking historical evidence. The repaired expected state must
match its native result without projection. The old four-input guaranteed
status pair and enchantment world/combat controls are checked separately.
This closes the first-turn mismatch; the new campaign branch still needs
whole-battle and full-campaign acceptance.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-guaranteed-status-test-rng-calibration.mt2f
```
