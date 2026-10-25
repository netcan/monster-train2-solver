# Upgraded Corrupt trait and copyability

A local alternative in the natural Hellhorned/Wurmkin fifth battle plays
card15, whose permanent TraitCorrupt upgrade adds CardTraitCorruptState.
The native result replaces it in hand with generated card27, preserving
the original data and copyability. The old model rejected that upgrade.

CardState.CanBeCopied queries each trait's GetIsCopyable. The original
CardTraitCorruptState inherits the true default and only adds a pre-play
room callback. It now joins the already calibrated copy-neutral traits;
CorruptRestricted and other traits retain their existing guards.

The original and independent repaired observations reproduce all five
inputs, complete successors and RNG without mutating parents. The old
rejection is retained. All four counters in the repaired raw trace are zero.
This closes the copyability boundary, not the other CorruptionAdded and
room-attachment branches or a fifth-battle win.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -SkipBuild -Fixture player-corrupt-copyability-calibration.mt2f
```
