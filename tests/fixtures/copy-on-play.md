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
before CopyOnPlay. One ordinary temporary Freeze is also calibrated below.
Other callback combinations/orders, discard-removing temporary upgrades and
multiple copies remain guarded.
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

This fixed-policy archive closes the combined mechanism mismatch and remains
a loss. Subsequent `full-battle-natural-ring7-tactical-win.mt2f` and
`full-battle-player-natural-ring7-{search,replay}.mt2f` establish natural Ring7
victories separately. A natural Fragile victory remains outstanding.

## Copies created after native victory

The ordinary source/replay pair is
`full-battle-terminal-copy-on-play{,-replay}.mt2f`, profiles
`full-battle-units-spells-and-junk-20261022-143247-8e8a8190` (43.22 seconds) and
`full-battle-units-spells-and-junk-20261022-143651-9c5a4aad` (47.87 seconds).
The Consume pair is `full-battle-terminal-copy-on-play-consume{,-replay}.mt2f`,
profiles `full-battle-units-spells-and-junk-20261022-143247-bac98a0c` (44.73 seconds)
and `full-battle-units-spells-and-junk-20261022-143651-c61bafae` (49.20 seconds).
All four win at Pyre80 after three plays/four EndTurns, settle, exit normally,
preserve protected originals and retain four zero failure counters. Each fresh
process executes seven independently frozen inputs without searching.

The original Flicker receives the original copy upgrade, optional original
ChaosScepter before it, an authored +1000 numeric upgrade and one temporary
Freeze. Two authored durable Stewards keep the natural first boss reachable.
Freeze retains the unplayed card through four complete EndTurns; OnCardPlayed
removes it before discard/copy. A boss-killing cast still creates exactly one
new hand identity with the temporary purge upgrade. The original routes once
to discard or exhausted, and the copy inherits numeric/Consume modifiers while
excluding the original copying upgrade. Complete terminal state, statistics,
card registry and RNG match sixteen immutable branches and full native chains.

The model now preserves Freeze cleanup in the registry after terminal card
clearing and does not re-add hand play history already cleared by the game.
The frozen driver also reuses the settled StopCombatLoop snapshot: capturing
later during export observes native EndOfCombat instead of the modeled action
boundary. This changes observation timing, without dropping phase comparisons.
Earlier failed diagnostic profiles remain unaccepted whole battles. The original
nonterminal copy and original Permafrost fixtures pass focused regression.

These are controlled mechanic calibrations; natural final bosses, other
discard callbacks/orders/counts, terminal purge copies and actual player
terminal-copy recommendations require separate witnesses.

```powershell
scripts/Run-FullBattleProbe.ps1 -CopyOnPlayTerminal -SkipBuild
scripts/Run-FullBattleProbe.ps1 -CopyOnPlayTerminalConsume -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --terminal-copy-only tests/fixtures/full-battle-terminal-copy-on-play.mt2f
```

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
