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

## Natural Ring7 combination

`full-battle-natural-ring7-copy-on-play.mt2f` uses the unchanged natural
`.probe-runs/campaign-ring8-20261022-072727-d98c7abf-before-ring7` checkpoint.
Source `full-battle-units-spells-and-junk-20261022-091602-81a91b88` runs with
physical positions, branch masks, death-dissolve and card-animation settlement.
No deck, upgrade, health, boss or spawn-queue edits are applied. The fixed
policy loses after 40 plays and eight EndTurns; all four failure counters are
zero and terminal effects settle. Full serial and sixteen independent chains
match state and RNG, including the original Consume/copy and played purge.

Two original upgraded StarterFireStarter plays each heal the Pyre by one HP
while a shared enchantment world exists. Previously the room actor healed,
but later callbacks reconstructed it from stale world HP. The heal now updates
that shared snapshot before subsequent callbacks. The focused gate requires
both HP deltas and agreement between the live and shared-world Pyre actors.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-copy-on-play-only `
  tests/fixtures/full-battle-natural-ring7-copy-on-play.mt2f
```

This closes the combined mechanism mismatch, not the Ring7 player-victory
goal. The natural checkpoint still needs a winning route before Fragile
player acceptance can proceed.

Focused affected checks also pass: 2,134 native trait refresh states, pure
composition controls, ten native clone/upgrade refresh generations and the
original first-spell-copy relic. No full regression or Mac build is run.

## Ordinary movement capture and tactical targets

The refreshed natural archive also includes Bump metadata whenever physical
positions are captured, including ordinary player sessions. Previously those
native getters were used only by dedicated effect scenarios: deployment and
future enemy definitions therefore lacked movement rules in normal search.
The native policy now actually plays original Demonize once, moving a unit
down one floor and applying two melee-weakness stacks. All forty plays match.
The dedicated gate checks 1,090 live actor snapshots, 23 future enemy and five
deployment definitions. Its affordable original Demonize frontier contains
thirteen legal targets across both teams with no unsupported attempts.

Tactical candidates also recognize negative status payloads in main effects
and OnCast callbacks when ranking enemy targets. Pure controls verify actual
debuff/buff application and source isolation for both callback locations.
The existing natural AllyBuffer tactical planning gate still wins with fifty
inputs and passes independent replay, cancellation and shared node limits.
Three cheap Ring7 policy trials still lose; neither capture nor target
classification fixes establish a natural Ring7 victory.

Two subsequent twelve-turn/30-second single searches on preferred floors one
and two report zero unsupported attempts (235/245 expanded nodes), but still
find only defeats. These are bounded model diagnostics, not an impossibility
proof or native player acceptance.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-movement-capture-only `
  tests/fixtures/full-battle-natural-ring7-copy-on-play.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --tactical-target-only
```
