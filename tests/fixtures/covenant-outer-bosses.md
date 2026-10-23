# Covenant Ring8 outer bosses

The player capture used to reject every Covenant during outer-boss birth.
Original Covenant 01 and 00+01 now retain raw authored definitions and carry
the pure native attribute adjustment separately. Native creation precedes
initial action/placement, then `OnSpawnCovenantEffects` applies the adjustment
before normal spawn callbacks. Applying the adjustment at creation would
incorrectly change this observable boundary.

All three original Ring8 variants have complete isolated no-cards controls.
The original scenario is selected in a fresh disposable profile and reached
with native map navigation. The unchanged original Covenants are explicitly
activated after startup: starting deck and all sixteen RNG streams remain
unchanged, and startup effects are never replayed. These are controlled routes,
not evidence of naturally initialized Covenant campaigns or earned unlocks.

| Boss | Raw attack/HP | Covenant 01 HP adjustment | 00+01 HP adjustment | Turns 01 / 00+01 |
| --- | --- | --- | --- | --- |
| AllyBuffer | 35 / 3800 | +1000 | +3000 | 5 / 5 |
| Fragile | 30 / 3000 | +1000 | +3000 | 4 / 4 |
| Corrupter | 20 / 2800 | +1000 | +3000 | 6 / 5 |

Attack is unchanged in all six controls. Raw/Horde stats remain authored;
live max HP receives the adjustment exactly once after creation. Every pure
query preserves its frame and all sixteen RNG streams. All authored wave
definitions match their native projections. Spoofed Covenant origins and
missing outer-boss stat evidence are rejected.

Each control compares authored instantiation, the unplaced created actor,
initial boss action, physical placement, deferred aura, complete battle and
terminal state/RNG, plus sixteen independent immutable full chains. All six
native processes exit normally with protected files unchanged and
capture/mismatch/unsupported/pending zero. All six no-cards battles lose;
these controls make no claim about winning search quality.

| Context / boss | Native profile suffix (prefix `full-battle-no-cards-20261023-`) |
| --- | --- |
| 01 / AllyBuffer | `103635-229f0678` |
| 00+01 / AllyBuffer | `103635-ba62982b` |
| 01 / Fragile | `103730-080f5a2e` |
| 00+01 / Fragile | `103730-0f7e79ae` |
| 01 / Corrupter | `103817-9eee2791` |
| 00+01 / Corrupter | `103818-e5629943` |

Archive pairs are `full-battle-covenant-outer-boss-{variant}-{context}.mt2f`
and `outer-boss-birth-covenant-{variant}-{context}.mt2f`, using lowercase
variants and contexts `01` / `00-01`. Run the focused
`scripts/Check-CovenantOuterBosses.ps1 -SkipBuild` checker. Trial signatures,
other Covenants and natural Covenant startup remain separate acceptance
boundaries.

## Actual player advice and independent native replay

AllyBuffer passes in both original contexts. Each actual Advisor source
executes eleven inputs (seven card plays and four EndTurns), publishes
thirteen advice updates and passes UI rendering, pointer routing,
pause/resume, hide/cancel/show, refresh and drag/clamp interactions. Each
independent native replay executes the same eleven inputs without search
or advice publication. Every full forecast, root, successor, terminal state
and RNG matches. All four native processes exit normally with settled
effects, protected files unchanged and all four failure counters zero.

| Context | Source / replay profile suffix (prefix `full-battle-units-spells-and-junk-20261023-`) | Pure captures source / replay |
| --- | --- | --- |
| AllyBuffer 01 | `104043-2aeabbc3` / `104319-a56f6d9f` | 168 / 35 |
| AllyBuffer 00+01 | `104043-a13ed8f0` / `104324-09310096` | 174 / 38 |

The source uses three turns, a 3,000-ms soft budget and tactical candidates.
Both are complete native losses; neither winning quality nor a hard response
deadline is established. Source runs take 153.73/158.86 seconds and replay
runs 78.56/79.56 seconds. Archives use the prefix
`full-battle-player-covenant-outer-boss-allybuffer-{context}` with
`-search.mt2f` / `-replay.mt2f`. Run the focused
`scripts/Check-CovenantOuterBosses.ps1 -Player -Variant AllyBuffer -SkipBuild`
pair checker. Other variants require their own player/replay evidence.

Two earlier AllyBuffer diagnostics (`103410-639eeb30` and `103410-e19c6d4e`)
retained the creation guard and reported two unsupported boundaries. Their
native queries established the nonzero adjustment; they are not curated
acceptance archives.
