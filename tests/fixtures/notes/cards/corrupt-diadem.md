# Corrupt Exile's Diadem before equipment replacement

Random09 Wurmkin/Hellhorned Ring8, seed771950956, Covenant0, contains
ExilesDiadem22 with the original TraitCorruptwIcon upgrade. After31 unchanged
inputs, playing it on unit20 in room1 first raises corruption5->6 despite
max4, then spends energy4->3 and replaces equipment11. The host shrinks3->1.

The original0.2.102 process20261026-043713-d023c5ce observes the rejected
boundary with no forecast. Version0.2.103 adds this exact equipment ID to the
previously calibrated zero-parameter, removable, single-trait/single-upgrade
signature. It reuses the existing corruption callback and equipment rules.
No unknown owner, card type, ability, parameter, stack, removal flag or upgrade
is enabled. Eight negative signature controls and immutable parallel branches
pass; existing Ambient Charge and Rage Room native pairs still pass.

The independent0.2.103 process20261026-044128-05744d48 executes the identical
32 inputs. Every complete successor, forecast and RNG matches; protected
original files remain unchanged. Both archived observations preserve the
original rejection and repaired forecast.

```powershell
scripts/Check-Models.ps1 -Fixture player-corrupt-diadem-calibration.mt2f,player-corrupt-diadem-replay-calibration.mt2f -SkipBuild
```

This is a calibrated model branch, not an eight-battle campaign victory.
The installed Windows build remains the independently qualified0.2.102.
