# Original chosen cleric tower Incant

The original rank-ten AllyBuffer scenario contains
`ClericT3_IncantAttack_Chosen`. Its original CardSpellPlayed trigger adds five
attack to attack-capable enemy units across the tower, excluding bosses.
Birth/room controls had validated the definition without casting on its floor.
Actual player sources also avoided those casts: their candidate frontier
rejected them because queued character callbacks lacked whole-train targets.
Passing those targets exposed a second local-only attack mutation lookup.
The queue now supplies complete remote actors and publishes their resulting
state, and attack mutation uses the target's actual floor.

`full-battle-chosen-incant-attack.mt2f` is captured from native profile
`full-battle-units-spells-and-junk-20261023-123812-afa25b5c` (174.92 seconds).
It uses original RunSetup rank ten, its original twenty-two-card startup,
the disclosed original AllyBuffer map jump and unchanged character/card
assets. `-ChosenIncantSpell` only selects an available legal spell on the
original chosen cleric's floor. No cards or units are injected, no stats
are changed and the actual interactive player advisor is not overridden.
The option is restricted to this separate fixed native policy control.

The original boss and cleric can occupy the same floor: the checker verifies
boss exclusion on every floor and separately requires positive remote normal
enemy buffs. Six actual casts produce thirty-six remote attack buffs and
six excluded bosses. Every actual cast remains in the searchable supported
frontier with its complete native successor, and sixteen parallel branches
per cast reproduce that successor without changing their shared root.
The thirteen recorded inputs and four EndTurns also compose from one root
in sixteen independent immutable full-battle branches.
All thirteen actual card inputs, four EndTurns, eight train phases and
thirty-three room stages match full native state/controller/RNG. The run
ends in a loss with Pyre0, normal exit, protected files unchanged and zero
capture/mismatch/unsupported/pending counters.

The ordinary fixture entry passes as well. Its older position invariant
now recognizes an observed outer-boss anchor separately from ordinary slot
occupancy and resolves copied points in their recorded preview plane;
non-boss actors still require their own occupied slot.

Failed positive controls `122616-f6b0876e` and `123030-751979c1` finished their
battles but had zero qualifying casts, so are not curated. The incomplete
repair run `123543-b8091ae8` was canceled after diagnostics exposed the second
lookup; it is not acceptance evidence. Earlier player source/replay pairs
also demonstrate advice correctness separately from this mechanism, with
zero qualifying chosen-floor casts. Their absence never counts as positive
trigger coverage.

Run `dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll
--chosen-incant-attack-only tests/fixtures/combat/status/full-battle-chosen-incant-attack.mt2f`.
The cumulative high-rank AllyBuffer player bundle additionally requires
this independently validated positive control. Its actual source/replay
must use genuine player publications and cannot enable the forced-input
control. The interactive assembly excludes the new probe policy class.

The runner writes `native-accepted-summary.json` only after requested native
checks and cleanup succeed, retaining exit/protected-file status, all failure
counters, native duration and trace hash. This preserves long-batch evidence
when console output is lost. Independent focused model checks remain required
before curation. Rank-six startup and rank-ten final-boss replay already
exercise this persisted result.
