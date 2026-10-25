# Boss attack preview preserves the native test RNG

Stygian/LazarusLeague seed972508374, champion-preserving third battle:
the thirteenth input ends turn1. Native RunBossKillPreview enables temporary
character states, applies the attack and drains its trigger queue before
restoring those characters. Tethys's guaranteed Frostbite consumes BattleTest
there; restoring character states does not restore that stream.

0.2.51 preserves the observed test stream across this separate boss preview.
The explicit isolated preview protocol still restores both original streams.
This also retains the real combat's independent one-draw Battle advance.

Both independent native processes replay all13 full successors, forecasts and
RNG states with immutable parents. The original fixture retains its incorrect
forecast; its sole historical TestRng difference is projected only in memory.
The repaired forecast must exactly match native. An additive development-only
passive trace records8 Range(0,100) calls with complete before/after RNG,
PreviewMode and caller stacks; it does not alter random values or game state.

The existing6-input ordinary preview pair passes as a control. This closes
the second-turn mismatch, not whole-battle or full-campaign acceptance.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-boss-preview-test-rng-calibration.mt2f
```
