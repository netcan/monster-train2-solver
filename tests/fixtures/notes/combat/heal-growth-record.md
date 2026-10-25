# Steelsinger growth preserves previous battles' permanent records

Remnant/Awoken seed295400568 Ring5 has Steelsinger card7 with a previous-battle
Plus3AttackScalable permanent record of18 and a current-spawn record of3.
The27th input, StarterHeal12 targeting unit3, restores health and triggers its
permanent growth. Native scales the applied live record3 to6, retaining18;
the old model changed the first same-definition card record18 to21 instead.
Live unit attack55 to58 and total card attack already matched, but record
identity and later snapshot continuation did not.

The native effect searches the live character's applied upgrades. The persistent
spawner card shares that current-spawn object; earlier battles' upgrades only
contribute to its starting stats. The exact original Plus3AttackScalable
signature now follows the already calibrated current-spawn handling for the
Plus1AttackScalable and PyreHeartSavagery growth definitions. Other upgrade
definitions retain their existing behavior.

Two independent27-input component observations preserve the original wrong
record and the repaired forecast. Every complete successor/RNG, parent
immutability and native player purity passes. Profiles:
`full-battle-units-spells-and-junk-20261025-091622-3027667b` and
`full-battle-units-spells-and-junk-20261025-092117-9d22990f`.
The checker projects only the old21/3 versus actual18/6 attack-field changes
before requiring full historical equality; the error remains stored in its
original archive. Unit-modifier lifetime/preview/removal/clone controls also pass.
These are component observations; Ring5 acceptance remains a separate goal.

```powershell
pwsh -NoProfile -File scripts/Check-Models.ps1 -Fixture player-heal-growth-record-calibration.mt2f -SkipBuild
```
