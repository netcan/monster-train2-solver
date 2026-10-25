# X-cost Undying with the original copy upgrade

Stygian/LazarusLeague seed972508374, champion-preserving Ring4 naturally
upgraded card `7168ed36-de03-41db-a36f-79b54b121341` with original
`CreateSpellCopyWithPurge`. The card starts in the draw pile. Its authored
Consume and PlayedCost-scaled Undying were rejected even before deployment.

0.2.55 accepts only the captured two-trait signature on this original card.
The copy pipeline reconstructs all authored traits before adding copied
upgrades. Previously it retained only the first authored trait, dropping
status scaling from this combination. Other discard callbacks and terminal
purge combinations remain guarded.

Two independent native deployment observations preserve the original null
prediction and the repaired exact successor. The zero-prefix request is
intentional: validation of the unplayed draw-pile card blocked every action.
Four changed source parameters remain rejected.

Two further native processes execute the same eleven real inputs from the
unchanged deployment root, including a positive-cost original and a zero-cost
copy. Both retain all authored traits and status-scaling metadata. The original
goes to Exhausted; the copy has temporary Ephemeral, excludes the copy upgrade,
does not recurse and retains the two native Purged references after play.
Full successor, forecast and RNG chains match with immutable parents; original
save/config/mod files remain unchanged. These are component observations, not
a Ring4 victory or eight-battle campaign clear.

```powershell
./scripts/Check-Models.ps1 -SkipBuild -Fixture player-undying-copy-deployment-calibration.mt2f,player-undying-copy-effects-calibration.mt2f
```

Ordinary/Flicker Consume-copy and natural Ring7 copy controls pass.
