# Original outer-boss variants

The full native definition catalog now records each outer boss's action groups,
repeats, room behavior, target team, payloads and room modifiers without consuming
gameplay RNG. In the current game module
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`, all six Level8 definitions use one
zero-repeat group with one `RandomOccupiedBossActionBehavior`, targeting Monsters
and carrying no action effects. Their unit triggers differ. The three Level9
Entropy actors have no action groups and remain outside this model.

## Fragile baseline

Profile `full-battle-no-cards-20261022-073027-ae4eaf17` uses seed 424244 and native
`Level8FinalBossMeleeWeakness`. The isolated driver jumps to distance 7 with the
original starting deck and no card inputs. It changes no boss definition or
statistics. This is a map-jump calibration, not a naturally progressed encounter.

The game loses in 49.77 seconds. Its four EndTurns, authored instantiation,
deferred aura binding, initial action, five settled phase boundaries and enemy
buff aura all match. Two casting moves and one initial latch are observed.
The real controller contributes eight operations; 1152 detached native controls
also match. Capture, mismatch, unsupported and pending counts are zero, terminal
effects settle, and protected original files remain unchanged.

The baseline exposed stale remote Pyre health in the model's shared enchantment
world. Room combat now publishes explicit external actor snapshots into their
original physical rooms. It retains dead-unit handling and does not relocate an
external target into the active room. The checker replays the full chain from
one root and four concurrent immutable branches per EndTurn, comparing complete
states and gameplay RNG. The existing AllyBuffer no-card chain and the thirty
player moon-room movement inputs also pass after this fix.

Run the focused gate after building ModelChecks:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --outer-boss-fragile-only `
  tests/fixtures/full-battle-outer-boss-fragile-no-cards.mt2f `
  tests/fixtures/outer-boss-fragile-birth-calibration.mt2f `
  tests/fixtures/outer-boss-fragile-phase-calibration.mt2f `
  tests/fixtures/boss-action-fragile-controller-calibration.mt2f
```

## Controlled Fragile Relentless and Pyre route

Profile `full-battle-no-cards-20261022-083835-c99210be` explicitly empties the
spawn queue before turn-two pre-combat and sets live/saved Pyre HP and max HP to
10000. Both mutations are recorded in `QueueControls`. This isolated native
victory (Pyre7866) is not a natural encounter. The full harness requires exactly
one mismatch at the mid-turn control, with zero capture/unsupported/pending
failures. The eight settled phase boundaries, one original Pyre-entry combat
and three complete post-control EndTurns match all state, physical positions,
controller progress and RNG. Four concurrent chains replay from the same
immutable post-control root without native reinjection.
This repeat enables physical spawn points, branch card masks, death dissolves
and card-animation settlement together. It verifies the deferred source-removal
fix across the full Fragile floor-destruction/Pyre route. The expected control
mismatch still makes the generic harness report `DEPTH-FAIL`; the dedicated
gate validates exactly that one mutation and every subsequent transition.

This exposed an over-broad preview cleanup: ordinary Boss pending aura actions
were also being cleared for the separately managed outer-train boss. Its aura
map now retains the native pending action. The existing ordinary-boss
persistent-enchantment and AllyBuffer checks remain separate regression gates.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --outer-boss-fragile-relentless-only `
  tests/fixtures/outer-boss-fragile-relentless-phase-calibration.mt2f `
  tests/fixtures/outer-boss-fragile-pyre-calibration.mt2f `
  tests/fixtures/outer-boss-fragile-pyre-turn-calibration.mt2f
```

The player entry remains limited to AllyBuffer pending Fragile winning-player
and independent native replay acceptance. Natural progression is now available:
the unchanged strong Ring7 deck wins in native frozen replay with Pyre62 and
continues through native campaign UI into Ring8. The checkpoint, choice history,
seventh-battle replay and all thirteen verified restore members are retained in
`campaign-progressed-ring8-fragile.zip`. See [Ring7 tactics](natural-ring7-tactics.md).

The natural Fragile no-card baseline preserves Pyre62 and seven preceding wins.
Five EndTurns reach a real loss with zero capture/mismatch/unsupported/pending
failures. A card-playing route exposed a separate synchronization bug: temporary
local status scopes included an unrelated remote Pyre. The aura world could
then relocate and lose it while retaining its physical spawn-point occupant.
Those scopes now exclude unrelated external actors, retaining the actual actor
and selected targets. `full-battle-natural-fragile-status.mt2f` matches all 46
frozen inputs: 39 plays and seven EndTurns, with zero failure counters and
settled terminal effects. The affected turn-three boundary retains its live
Pyre in physical room three and passes two independent immutable replays.
Both natural archives are losses; they do not establish a Fragile victory.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-fragile-only `
  tests/fixtures/full-battle-natural-fragile-no-cards.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --natural-fragile-only `
  tests/fixtures/full-battle-natural-fragile-status.mt2f
```

The explicit native "leave" route still loses at ring six on all three preferred
deployment floors and remains diagnostic. No full regression or Mac build is
used for these focused increments.
