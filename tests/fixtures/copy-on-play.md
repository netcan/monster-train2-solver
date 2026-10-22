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
discard callbacks/orders/counts and actual player
terminal-copy recommendations require separate witnesses.

```powershell
scripts/Run-FullBattleProbe.ps1 -CopyOnPlayTerminal -SkipBuild
scripts/Run-FullBattleProbe.ps1 -CopyOnPlayTerminalConsume -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --terminal-copy-only tests/fixtures/full-battle-terminal-copy-on-play.mt2f
```

Focused affected checks also pass: 2,134 native trait refresh states, pure
composition controls, ten native clone/upgrade refresh generations and the
original first-spell-copy relic. No full regression or Mac build is run.

## Temporary copies delivering the final blow

`full-battle-terminal-purge-copy{,-replay}.mt2f` records profiles
`full-battle-units-spells-and-junk-20261022-144734-394a7d5f` (48.28 seconds) and
`full-battle-units-spells-and-junk-20261022-144850-45a23e2f` (47.40 seconds).
The Consume pair is `full-battle-terminal-purge-copy-consume{,-replay}.mt2f`,
profiles `full-battle-units-spells-and-junk-20261022-144734-b7bcdbb4` (49.71 seconds)
and `full-battle-units-spells-and-junk-20261022-144850-04e9bac7` (48.63 seconds).
All four win at Pyre79 with four plays/four EndTurns, settle, exit normally,
preserve protected originals and retain four zero failure counters. Each fresh
process replays eight independently frozen inputs with zero search nodes.

The first boss has an explicit authored 2000 HP control. The retained original
Flicker first deals nonlethal damage and generates its real temporary copy;
that copy immediately delivers the killing cast. Both ordinary and Consume
copies retain two native Purged references, no standby/exhausted reference,
and create no recursive copy after victory. The complete terminal state,
statistics, registry and RNG match sixteen immutable branches and complete
native action/turn chains; seven enemy birth boundaries also match.

Terminal purge routing is enabled only for the captured original temporary
upgrade on trait-free spells, optionally with the calibrated earlier Consume
callback. Missing upgrade, additional discard callbacks and other terminal
destinations remain guarded. The ordinary terminal-copy pair and original
nonterminal copy fixture pass focused regression. Earlier failed experiments
remain diagnostic: Ephemeral forcibly discards at EndTurn even when Frozen,
so retaining an already-created copy until boss birth is not a valid control.
These remain controlled mechanic calibrations, with actual player suggestions
and natural final bosses requiring their own evidence.

```powershell
scripts/Run-FullBattleProbe.ps1 -CopyOnPlayTerminalPurge -SkipBuild
scripts/Run-FullBattleProbe.ps1 -CopyOnPlayTerminalPurge -CopyOnPlayTerminalConsume -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --terminal-purge-copy-only tests/fixtures/full-battle-terminal-purge-copy.mt2f
```

## Actual player terminal Consume recommendations

The separately loaded, unmodified Advisor now publishes and executes both
terminal paths through the native card UI. A controlled 2000-attack boss makes
three no-card turns lose. Earlier deployment and four EndTurns use the fixed
setup policy; the actual player forecast starts at turn4. These are bounded
forecast windows, not rolling recommendations from deployment.

`full-battle-player-terminal-copy-consume-{search,replay}.mt2f` comes from
profiles `full-battle-units-spells-and-junk-20261022-145908-746836e1` (59.16 seconds)
and `full-battle-units-spells-and-junk-20261022-150033-259257a3` (49.66 seconds).
Both win at Pyre79 with six plays/four EndTurns. Four frozen forecast steps
end in the original Flicker killing the125HP boss and creating its new hand
copy. This uses the existing opt-in tactical seed, horizon3 and configured
3000ms budget; the consumed winning search takes295.2ms.

`full-battle-player-terminal-purge-copy-consume-{search,replay}.mt2f` comes from
profiles `full-battle-units-spells-and-junk-20261022-145600-a36e9661` (69.51 seconds)
and `full-battle-units-spells-and-junk-20261022-145908-c50c7a70` (53.06 seconds).
Both win at Pyre79 with seven plays/four EndTurns. Five frozen forecast steps
include both casts against the2000HP boss, ending in the temporary copy's two
purge callbacks. This uses the default seed/horizon3 and configured3000ms
budget; the search actually takes3582.8ms because an atomic expansion can
overrun the soft deadline. Fresh processes replay the same canonical root,
actions, final state and RNG with zero search nodes.

Both pairs settle, exit normally, preserve originals and retain four zero
failure counters. Native UI pause/resume, hide/cancel/show, refresh, raycast
and drag checks pass; four publications and zero capture purity failures
prove real player observations. Complete raw action/turn chains, seven enemy
birth boundaries and sixteen immutable branches also match.

The first default search on the2000HP control lost after casting the original
at a low-HP crowd on another floor. Its settled, zero-mismatch defeat remains
`player-terminal-copy-target-calibration.mt2f`, not an accepted victory. It
checks the full losing native chain and the repaired model continuation.
Single-effect room damage candidates now use estimated useful health/armor
damage instead of enemy count or preferred floor. Targets still use native
filters and the chosen action executes through the unchanged model. Both
default and tactical seeds choose the boss in this diagnostic; the full
default plan wins and independently replays. Existing natural Ring7 and
AllyBuffer tactical wins, target-team controls and the score plateau check
pass focused regression. Unmodeled payloads remain guarded.

```powershell
scripts/Check-PlayerAdvisor.ps1 -Case TerminalCopyConsume,TerminalPurgeCopyConsume -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --terminal-copy-target-only tests/fixtures/player-terminal-copy-target-calibration.mt2f
```

Non-Consume player terminal paths, other callback orders/counts and natural
final-boss combinations remain separate acceptance targets. No full regression
or Mac package is built.

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
