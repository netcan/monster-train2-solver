# Self status effects after a lethal shift · 0.2.70

The retained metagame campaign's Ring3 checkpoint exposes a legal Penitent
ability alternative:champion2 has20HP and moves to the front. Its normal
OnShift self-damage upgrade kills it. The following Self Valor and
armor-per-Valor effects must finish without restoring or targeting the removed
champion. The old model rejected this legal branch as a missing retained actor.

Only scalar Self status collection with an explicit retained zero-HP actor,
finished death and removal state is calibrated here. Unknown missing actors
and other effects keep their existing rejection. No change to living Self
targets, native effects, death callbacks or targeting filters.

Two independent native observations preserve the same12-input prefix. The
original archive keeps its rejected prediction; the repaired one includes a
complete supported forecast. All roots,successors,RNG and parent immutability
match, and the repaired branch is present in search enumeration. Existing
unit-trigger target/filter/preview/32-parallel-branch checks and the earlier
30-input dead-caster room-Pyregel pair also pass. These components are not
terminal victories and do not imply this sacrifice is a good tactical choice.

```powershell
pwsh scripts/Check-Models.ps1 -SkipBuild -Fixture player-removed-ability-self-calibration.mt2f
```
