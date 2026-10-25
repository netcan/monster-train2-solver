# Damage-only casts against shields

Random10 Banished/Wurmkin Ring8,seed1895012796,Covenant0,contains an early
target-selection counterexample. At the eighth input,RevengeOfTheDamned13
deals two eight-damage hits. The old tactical seed prefers Horde8 at one
health,two Spell Shield stacks. The original complete0.2.99 loss records
both shields removed and health still one. Cleric10 in the same room has
fifteen health and no shield.

Two isolated native processes replay the unchanged first seven inputs and
cast the same card on Cleric10. Both kill it; all eight complete successors,
forecasts and RNG match. The profiles are20261026-041109-d647e724 and
20261026-041109-40892ff2; protected original files remain unchanged.

Version0.2.101 keeps a shield-breaking candidate as a fallback while trying
other legal targets for a damage-only card. A target whose complete modeled
successor loses health or armor retains its existing priority. Pure shield
removal yields to an effective hit. All-shielded targets still allow breaking
shields; mixed effects retain their existing ranking. The full enumerator
keeps every legal shield-breaking action. The focused check covers both
native processes,three deployment preferences,immutable parents and two
synthetic filter/mixed-effect controls. No combat rule or native guard changes.

```powershell
scripts/Check-Models.ps1 -Fixture player-spell-shield-target-calibration.mt2f,player-spell-shield-target-replay-calibration.mt2f -SkipBuild
```

This is a native-validated local decision,not a complete campaign victory.
The prior0.2.100 actual Ring8 run finishes in defeat after40 inputs,32 plays
and8 EndTurns,with all four failure counters zero. Offline refined routes
remain model predictions until independently validated in the game.
