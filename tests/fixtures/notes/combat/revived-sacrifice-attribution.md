# Revived unit and retained sacrifice source · 0.2.116

Random12 Hellhorned/LazarusLeague final battle, input38 (turn4 EndTurn), exposes
one extra model kill event. MonsterMeathook11 has revived to1HP and retains
SacrificeCardId29 (ImpProvisation), but native IsSacrifice is false.

CombatManager.GetResponsibleCard checks current IsSacrifice before using
SacrificeCard. The model checked the retained reference alone and attributed
an otherwise uncredited enemy death to card29. It added HeroesKilled for that
card and AnyHeroKilled for every tracked card, including a second source-card
increment. No native unit health or battle RNG differed at this boundary.

The model now honors an explicitly captured IsSacrifice=false. Existing unit
ability attribution takes precedence and the legacy absent-field behavior is
unchanged. Two independent original-checkpoint processes replay37 inputs and
observe the38th. The first preserves the erroneous forecast; the second clean
fd3aa60f build matches every complete native state and RNG. The checker permits
exactly the historical two-duration kill-statistic delta and compares every
other field. Four parallel EndTurn branches preserve the same parent/state/RNG.

Source:20261026-103204-b6435246; repaired:20261026-103453-3d2b246d.
Fixtures: combat/status/player-revived-sacrifice-attribution[-replay]-calibration.mt2f.
The85-input model win is re-frozen with corrected statistics before continuing
native whole-battle acceptance. The38-input component alone is not a win.

```powershell
./scripts/Check-Models.ps1 -Fixture player-revived-sacrifice-attribution-calibration.mt2f -SkipBuild
```
