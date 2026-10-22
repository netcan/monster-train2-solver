# Original copy-on-play upgrade

Native MVID: `8fb07b96-f4db-4d2b-884d-c00536d6ccf4`.
Source: `.probe-runs/full-battle-units-spells-and-junk-20261022-085557-d2942e14`.

The calibration gives two original Flicker cards the original
`CreateSpellCopyWithPurge` upgrade (`a75fc092-ca72-476b-b518-01e9ecb8d6a3`).
One carrier also receives original ChaosScepter before the copy upgrade.
Both are placed in the draw pile. These are explicit carrier/order controls;
original card/effect/upgrade definitions and battle statistics remain unchanged.

```powershell
./scripts/Run-FullBattleProbe.ps1 -CopyOnPlay -Seed 424244 -NativeWorker 1 `
  -SettleDeathDissolves -SettleCardAnimations -SkipBuild -TimeoutSeconds 240
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --copy-on-play-only `
  tests/fixtures/full-battle-copy-on-play.mt2f
```

The native game wins with Pyre80 after 27 plays and seven EndTurns. It creates
three real copies (two plain, one Consume), then plays and purges all three
without recursive copying. All four failure counters are zero, terminal
effects settle and protected originals remain unchanged. The complete serial
chain and sixteen independent immutable chains match full state and RNG.

The copy receives temporary `CopyOnPlayTraitUpgrade`
(`513224dd-223f-48ed-bf0a-afd486aef98b`): add Ephemeral, remove CopyOnPlay and
exclude CreateSpellCopyWithPurge from both copied modifier lists. Other numeric
upgrades and Consume are copied through the existing native add-card pipeline.
Each played temporary copy leaves the two native purge references.

Native discard statistics and an earlier Consume callback happen before copy
allocation. A new identity therefore misses those earlier broadcast counters;
subsequent standby-return events still reach it. The model now follows that
order rather than creating the card before recording the original discard.

Support is restricted to the original single-copy parameter upgrade on spells
without authored traits or other installed trait state, optionally with Consume
before CopyOnPlay. Other callback combinations/orders, discard-removing
temporary upgrades, multiple copies and terminal copy routing remain guarded.
This controlled victory does not establish natural Ring7 or player acceptance.

Focused affected checks also pass: 2,134 native trait refresh states, pure
composition controls, ten native clone/upgrade refresh generations and the
original first-spell-copy relic. No full regression or Mac build is run.
