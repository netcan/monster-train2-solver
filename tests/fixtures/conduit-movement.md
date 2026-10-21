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
retains private state across detach/reattach. Incomplete captures and enemy
Conduit physical movement remain guarded. Player Conduit capture is still disabled.

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
callbacks, arbitrary multiplier/trait combinations or an integrated player action chain.
The existing original Conduit relic fixture remains a separate affected check.
