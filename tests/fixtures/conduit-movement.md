# Conduit physical movement component

`conduit-movement-calibration.mt2f` records 38 primary `CharacterState.SetSpawnPoint`
calls in an isolated game 2.2.1 process. Native profile
`full-battle-no-cards-20261021-214325-31331eb1` completes in 52.94 seconds and
preserves the protected original files. The boundary is immediately after the
native call returns; this archive is a component calibration, not a battle win.

The controls use an original Steward carrier with four Conduit stacks, an
unchanged original damage spell, and an authored copy carrying the original
StrongerMagicPower base trait. A temporary trait has a separate guarded lifecycle
and is not substituted for this base-trait control. All three selected floors and
all nine source/destination floor pairs are exercised, followed by detach,
reattach and dead-character controls. Native upgrades are four damage/heal for
the ordinary spell and twenty for its multiplier control.

Run:

```powershell
scripts/Run-FullBattleProbe.ps1 -ConduitMovement -SkipBuild -TimeoutSeconds 180
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --conduit-movement-only tests/fixtures/conduit-movement-calibration.mt2f
```

The independent checker reproduces complete contexts, physical point ownership,
the contributor's private tracked-card and multiplied-card collections, and all
captured gameplay RNG streams. Parent snapshots stay immutable. The 38 rows have
eight tracking additions, nine clears, twenty-one unchanged controls, six
detach/reattach operations and four dead-character operations. Four controls
repeat exactly the same point (including null and an empty tracker on a selected
floor); these must not dispatch a movement callback.

Three additional controls use the original `SetTemporaryStateEnabled` lifecycle:
leave the selected floor, move within it, and detach while previewing. Their
copied modifiers have empty trackers, original hand upgrades and gameplay RNG
stay unchanged, and cancellation restores the complete context and combat actors.
The checker independently allocates copied point identities, applies the physical
move, and restores primary ownership; only the copied-point counter advances.

Native order matters: entering a selected floor adds hand tracking even for a
dead actor. Dead actors are excluded from the room contributor enumeration while
their private modifier still exists. Detached actors also retain that modifier.
The calibration records those modifiers in `CombatContext.ConduitRetained`.
Prediction resolves the contributor from the parent context alone, without an
extra tracker argument. All context-copy operations preserve this immutable
collection. A null collection identifies older captures; an empty collection
means the complete capture contains no retained modifier. Physical capture now
emits it whenever Conduit is enabled. `WithSpawnPoints` dispatches the calibrated
callback automatically, skips preview card changes and same-point no-ops, and
retains private state across detach/reattach. Incomplete captures remain guarded.
Opposing physical movement is calibrated below. Player Conduit capture is enabled in 0.2.3.

## Integrated physical battle

`full-battle-original-conduit-relics-physical.mt2f` comes from native profile
`full-battle-units-spells-and-junk-20261021-215255-3751b00b` (86.49 seconds).
It wins with 80 Pyre HP, 21 card plays, five EndTurns, 239 native compactions and
470 complete physical decision captures. All four failure counters are zero,
terminal effects settle and original files stay unchanged. Original relic owner
definitions are unchanged; Steward and equipment carriers are authored controls.

This exposed an ordering bug hidden by single-contributor controls: native
`RefreshCardInHand` refreshes UI without dispatching `CardUpdated`. The extra
model callback changed temporary upgrade order when a third Conduit spawned
before two existing contributors. Removing it preserves native upgrade order.
The accepted fixture includes this exact multi-contributor birth and independently
replays the entire action chain, all 14 births and the three owner ablations.

```powershell
scripts/Run-FullBattleProbe.ps1 -OriginalConduitRelics -PhysicalSpawnPoints -Policy units-spells-and-junk -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-conduit-relics-only tests/fixtures/full-battle-original-conduit-relics-physical.mt2f
```

This does not establish complete combat preview, mixed conduits, room-mask
callbacks or arbitrary multiplier/trait combinations.
The existing original Conduit relic fixture remains a separate affected check.

## Player action chain and independent replay

`full-battle-player-conduit-search.mt2f` records the actual 0.2.3 player entry
point (profile `full-battle-units-spells-and-junk-20261021-221018-514570eb`,
265.04 seconds): 24 card actions, six EndTurns, 32 publications, 398 pure
captures and a native win with 80 Pyre HP. The independent frozen replay
`full-battle-player-conduit-replay.mt2f` takes 93.11 seconds, performs the same
30 inputs with zero planner nodes and wins with 80 HP. Both settle terminal
effects and retain zero capture/mismatch/unsupported/pending counters.

The focused pair gate matches every input, complete root/successor, forecast
and gameplay RNG. It also independently checks all 14 relic births, 96 live
contributor snapshots, 37 multi-contributor rooms and 12 upgraded card plays.
The native relic owners are unchanged; their Steward/equipment carriers are
authored. This is functional player calibration, not a natural-deck encounter.

```powershell
scripts/Check-PlayerAdvisor.ps1 -Case Conduit -SkipBuild
```

## Opposing physical contributor

`conduit-enemy-movement-calibration.mt2f` comes from profile
`full-battle-no-cards-20261022-132351-2ff06300` (34.64 seconds, normal exit,
unchanged protected originals and gameplay RNG). Native HeroManager creates
an opposing carrier from unchanged Steward data; adding four Conduit stacks
is an authored control. The original Conduit status and room modifier run
without interception, including the private tracker and preview-copy lifecycle.

All 38 position boundaries and three preview controls match complete contexts,
physical points and private tracked/multiplied-card collections. As for the
friendly carrier, entering a selected floor upgrades eligible player hand cards
by four damage/heal (twenty for the original stronger-magic trait). Team ownership
does not suppress the native modifier's hand tracking. There are eight additions,
nine clears, twenty-one unchanged controls, six detach operations, four dead
controls and seven retained contributor witnesses. Eight parallel immutable
model branches per position also match. Primary hand upgrades/RNG remain intact
through preview entry, movement and cancellation.

The blanket enemy physical-capture rejection and player-only movement check are
removed; complete capture, original upgrade definition and card-eligibility
checks remain. The original friendly 38/3 control still passes. This establishes
the isolated physical mechanism, not a complete opposing paid battle or real
player composition. The paid composition is validated below. No full regression or Mac package is built.

```powershell
scripts/Run-FullBattleProbe.ps1 -ConduitEnemyMovement -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --conduit-movement-only tests/fixtures/conduit-enemy-movement-calibration.mt2f
```

## Paid opposing Conduit and moon callbacks

`full-battle-eclipse-chamber-enemy-conduit{,-replay}.mt2f` retains profiles
`full-battle-units-spells-and-junk-20261022-133030-30ce487a` (111.52 seconds)
and `full-battle-units-spells-and-junk-20261022-133257-cc2211b1` (81.76 seconds).
Both win at Pyre80 after thirteen plays/five EndTurns, settle, exit normally,
preserve protected originals and have four zero failure counters. Eighteen
independently frozen inputs, complete state/RNG and sixteen full immutable
model chains match in the fresh zero-search process.

The authored first-wave carrier clones a shield guard with unchanged original
Selene/Nightingale callbacks, then gains four original Conduit stacks from an
OnSpawn AddStatus callback. Four unchanged paid Hidden Passage/Dripfall moves
change room understanding and remove/reapply New upgrades. Every move clears
its hand tracker after leaving the selected floor, with 22 tracked-card records.
Two character/thirteen support refreshes and all seven spawn boundaries match.

The first attempted scene gave Conduit as a starting status. Native Hero Setup
applies it off-train, and StatusEffectConduitState.OnStacksAdded dereferences
the missing physical room. That diagnostic is not curated; its two isolated
test processes were stopped after the native failure. The corrected scene
grants Conduit through the native post-entry OnSpawn pipeline. Enemy spawn
validation now explicitly rejects off-train starting Conduit rather than
forecasting a continuation the game cannot execute. A focused immutable guard
control passes alongside the accepted post-entry battle chain.

Only offline checker outputs were rebuilt while these native processes ran;
all four Probe/Advisor payload DLL hashes remained unchanged. This is an
authored composition, not a natural late-game deck or actual player acceptance.

```powershell
scripts/Run-FullBattleProbe.ps1 -EclipseChamberEnemyConduit -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --eclipse-enemy-conduit-only tests/fixtures/full-battle-eclipse-chamber-enemy-conduit.mt2f
```

## Actual player with opposing Conduit and moon callbacks

`full-battle-player-room-moon-enemy-conduit-{search,replay}.mt2f` retains source
`full-battle-units-spells-and-junk-20261022-133118-c10b8111` (242.58 seconds)
and fresh replay `full-battle-units-spells-and-junk-20261022-133551-0edd2927`
(101.42 seconds). Both use the real player assembly and win at Pyre80 after
25 plays/five EndTurns. Both settle, exit normally, preserve protected originals
and have four zero failure counters. All thirty inputs, complete native states,
frozen forecasts and gameplay RNG match between the independent processes.

The source publishes 32 times with pure capture/UI acceptance and retains its
verified winning continuation for 29 later decisions. The used initial search
takes 3.50 seconds; later response median/P95 is 1.04/1.77 seconds. Eight live
opposing contributor snapshots and sixteen tracked hand-upgrade witnesses match,
alongside the original opposing birth/phase callbacks and friendly room masks.
The checker requires complete retained tracking, native contributor power and
the actual temporary upgrade for each privately tracked card.

This uses an authored carrier/hand and the six-turn/10,000-ms tactical budget.
The player route does not Bump this opposing contributor; that operation is
validated by the separate paid source/replay chain. Natural late-game decks,
mixed opposing/friendly contributors and arbitrary trait combinations remain
separate acceptance boundaries. No full regression or Mac package is built.

```powershell
scripts/Check-PlayerAdvisor.ps1 -Case RoomMoonEnemyConduit -SkipBuild
```

## Mixed friendly and opposing contributors

`full-battle-eclipse-chamber-mixed-conduit{,-replay}.mt2f` retains source
`full-battle-units-spells-and-junk-20261022-134708-d033cbaf` (133.85 seconds)
and fresh replay `full-battle-units-spells-and-junk-20261022-135006-249c8e1e`
(82.40 seconds). Both win at Pyre80 after thirteen plays/five EndTurns, settle,
exit normally, preserve protected originals and retain four zero failure counters.
Eighteen independently frozen inputs, full state/RNG and sixteen immutable
model chains match. No production heuristic or Conduit semantics change.

The same authored moon carriers now gain three friendly Conduit stacks through
their OnSpawn callbacks; the opposing carrier still gains four. Thirty-seven
hand-card snapshot witnesses contain both teams' independent native upgrades on
the same card. Four original paid opposing Bumps clear their own trackers while
28 friendly upgrade records remain intact. The focused checker verifies each
contributor's distinct identity, power, damage/heal amount and preserved upgrade.
Two character/fourteen support refreshes and all seven spawn boundaries match.
This is a controlled composition; actual player and natural deck acceptance
remain separate. No full regression or Mac package is built.

```powershell
scripts/Run-FullBattleProbe.ps1 -EclipseChamberMixedConduit -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --eclipse-mixed-conduit-only tests/fixtures/full-battle-eclipse-chamber-mixed-conduit.mt2f
```
