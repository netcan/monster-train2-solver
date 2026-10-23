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
not a natural deck.

The actual player source `070616-bc51e413` and independent frozen replay
`071436-b0eabdad`, both under `full-battle-units-spells-and-junk-20261024-`,
complete the same 48 inputs (41 plays and seven EndTurns), win at Pyre80 and
exit normally in 487.24/196.63 seconds. Both preserve protected originals,
settle terminal effects and have four zero failure counters. Complete roots,
forecasts, successors and RNG match. The source publishes fifty times with
fifty fresh complete-root checks and 397 pure captures. Two consumed searches
produce the route; nine ordinary and thirty-seven winning continuations reuse
verified suffixes with zero search work. The first search takes 10.13 seconds,
with 15.28 seconds total response. Settings are six turns/10000ms, top-room
preference and tactical seed enabled; this is not default-budget acceptance.

The dedicated pair gate requires the exact unchanged healing owner in every
captured root/successor, positive healing offsets on all five original moon
spells, both room-mask modes, paid Lunacy, native pending-effect settlement,
real player UI and sixteen independent immutable whole-battle branches. The
pair covers 1,662 settled trait snapshots, 24 Exclusive and sixty Additive
masked-Full witnesses, sixteen Lunacy casts/84 paid advances and 7,409 pending
effect waits. Its
negative control rejects the earlier Lunacy pair without healing provenance.

```powershell
scripts/Check-PlayerAdvisor.ps1 -Case MoonRoomHealingRelic -SkipBuild
```

Archives: `full-battle-player-moon-room-healing-relic-{search,replay}.mt2f`.

The immutable original 668-card catalog contains five cards using
`CardTraitAddCardUpgradeOnMoonPhase`, one using
`CardTraitScalingAdvanceMoonPhase`, and none using
`CardTraitScalingMagicPowerOnMoonPhase`. The last runtime type remains guarded;
its empty original-card set is not a missing playable-card implementation.
Additional authored/event-granted traits still require their own provenance.

Other relic signatures and uncalibrated conditions remain protected. Natural
decks and further callback combinations require separate evidence. No Mac
package or full regression is required.
