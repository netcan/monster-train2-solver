# Moon spell upgrades with original healing relic

The unchanged `IncreaseSpellHealing` (`03613302-e55a-4e6c-99a1-c198c8d3175c`)
previously made moon-upgrade hand refresh unsupported. Its sole unconditional
`RelicEffectModifyCardHealingPower` adds ten healing points to spells; the
moon-resource callback does not reapply those offsets when stronger magic
power changes. Native source and independent captured-state replay establish
this boundary before enabling that exact original signature.

`room-moon-spell-healing-relic-calibration.mt2f` is captured by isolated profile
`full-battle-no-cards-20261024-070147-edd2e1a7` (35.47 seconds, normal exit,
protected originals unchanged). Fifteen settled native boundaries cover six
original spells, six room selections, both phases and Eclipse install/remove.
All 105 private trait snapshots, forty active changes, ten Exclusive and
twenty-five Additive masked-Full witnesses match complete model states/RNG.
Each boundary passes eight immutable branches; all six spells retain their
native +10 healing offset. Altered healing amounts and incomplete condition
definitions remain rejected. This component is not a completed player battle.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonSpellHealingRelic -NativeWorker 2 -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-spell-upgrades-only tests/fixtures/room-moon-spell-healing-relic-calibration.mt2f
```

The full controlled source is
`full-battle-units-spells-and-junk-20261024-070355-fee31ed9` (104.74 seconds).
Its curated `full-battle-original-moon-card-upgrades-healing-relic.mt2f` wins
at Pyre80 after twenty plays/five EndTurns, settles and exits normally with
four zero failure counters and protected originals unchanged. All twenty
native hand callbacks and sixteen independent immutable whole-battle model
chains match complete states/RNG, including paid Lunacy at 2/3/0 energy,
Conduit refresh, moon upgrades and stronger-magic permanent upgrades.
Spell healing offsets are positive; Monster and room-attachment controls stay
zero. This uses the existing authored durable units and energy/phase controls,
not a natural deck. The player source/replay acceptance remains the next gate.

Other relic signatures, uncalibrated conditions and combinations remain
protected. No Mac package or full regression is required.
