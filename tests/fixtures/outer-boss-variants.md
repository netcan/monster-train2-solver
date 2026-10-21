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

The player entry remains limited to AllyBuffer until Fragile has a natural
progressed encounter and independent native replay. The baseline ends in Casting;
it does not establish a complete Relentless/Pyre-entry cycle for Fragile.
A separate forced-queue diagnostic reached Relentless but ended before Pyre
entry, so it is not curated as that acceptance. No full regression or Mac build
was run for this increment.
