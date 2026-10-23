# Original Covenant combinations at the true final boss

The original native relic manager adds each active Covenant's enemy stat preview.
The model retains the authored bases and a separate birth adjustment containing
every active original Covenant ID, in native activation order. Unknown, duplicate,
missing and spoofed definitions remain unsupported. Only original definitions
00, 01, 04 and 08 are calibrated here; these are not cumulative Covenant ranks.

`true-final-boss-spawn-calibration-covenants.mt2f` activates all four original
definitions in a fresh isolated run, after startup. Native source
`full-battle-no-cards-20261023-085629-4a4e2232` passes complete five-body birth,
physical anchors, linked health, generated scourges, complete context and RNG,
plus 64 concurrent immutable model branches. Activation and all five native
stat previews preserve every RNG stream. No startup deck effects are reapplied.

The ten `true-final-boss-spawn-calibration-covenants-<mask>.mt2f` controls cover
all two-way and three-way subsets. Mask bits 1/2/4/8 mean original definitions
00/01/04/08. Sources are recorded individually in `manifest.tsv`. Together with
the four retained single-definition controls and the four-way control, the
focused checker requires all fifteen distinct nonempty subsets and 960
immutable branches. Missing, repeated or unknown subsets fail acceptance.

Run `scripts/Check-TrueFinalBossCovenants.ps1 -SkipBuild` for the exact matrix
and the combined full battle. It does not run the full repository regression.

`true-final-boss-covenant-battle-combined.mt2f`, source
`full-battle-no-cards-20261023-085629-47ceace2`, completes the original native
no-cards loss. All five EndTurns, ten train stages and forty room stages match
full state/RNG; sixteen independent immutable full chains agree. Thirteen pure
native stat previews cover 26 authored ordinary-wave definitions. The native
process exits normally with protected files unchanged and all four failure
counters zero. No encounter, wave, health or phase replacement is used.

The actual separate player assembly also passes the all-four combination:
`full-battle-player-true-final-boss-covenants-{search,replay}.mt2f`. Source
`full-battle-units-spells-and-junk-20261023-085858-3b3c10f4` executes seventeen
inputs (twelve card plays and five EndTurns), with nineteen advice publications,
238 pure captures and successful UI interactions. Independent fresh replay
`full-battle-units-spells-and-junk-20261023-090444-0b232ed3` executes the same
inputs with 59 pure captures, no searches and no advice publications. Every
input, root, successor, complete forecast, terminal state and RNG matches.
Both processes exit normally with settled effects, protected files unchanged
and all four native failure counters zero. Both finish in defeat. The source
uses a three-turn horizon, 3,000-ms soft search budget and tactical candidates;
its 306.08 seconds and replay's 165.14 seconds include the full native run and
capture validation. This does not establish a hard response deadline or wins.

Player acceptance is available through
`scripts/Check-PlayerAdvisor.ps1 -Case TrueFinalBossCovenants -SkipBuild`.

This remains the explicitly disclosed native `Cheat_JumpToTFB` route. Natural
unlock, cumulative high-rank campaigns, winning play, relentless transitions
and terminal signatures require their own evidence.
