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
means the complete capture contains no retained modifier. Only the isolated
component currently emits it. `ConduitModel.Validate` continues to reject
physical whole-battle contexts until integrated callbacks and preview lifecycle
have independent native acceptance.

This does not establish complete combat preview, mixed conduits, room-mask
callbacks, arbitrary multiplier/trait combinations or an integrated player action chain.
The existing original Conduit relic fixture remains a separate affected check.
