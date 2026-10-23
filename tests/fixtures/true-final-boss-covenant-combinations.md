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

Run `scripts/Check-TrueFinalBossCovenants.ps1 -SkipBuild` for the exact matrix,
combined full battle and controlled relentless/terminal evidence. It does not run the
full repository regression.

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

Original 04 and the all-four combination additionally pass
`true-final-boss-relentless-calibration-{covenant04,covenants}.mt2f`, sources
`full-battle-no-cards-20261023-090520-756c21a4` and
`full-battle-no-cards-20261023-090634-297fdc26`. Each explicitly exhausts the
queue and enters the native relentless phase. All three EndTurns, four complete
phase boundaries, sixteen immutable full chains and 64 phase branches agree.
The wait latch, three disabled floors, five-body Savagery sacrifice, linked
health/controller/card cleanup and interrupted terminal coroutine match native
state/RNG. Both win with Pyre61, normal exits, protected files unchanged and all
four failure counters zero. This is a controlled phase transition and victory;
it does not prove a natural transition or surviving-body combat against the Pyre.

The critical-health controls separately cover real terminal combat:
`true-final-boss-lethal-calibration-covenant04.mt2f` and
`true-final-boss-lethal-preview-calibration-covenants.mt2f`, sources
`full-battle-no-cards-20261023-091014-18e6dd7d` and
`full-battle-no-cards-20261023-091014-264929e7`. The three independent owners
receive explicit debuffs 2054/3249/4499, after the original Covenant-adjusted
birth. Every linked body then has one HP. Two EndTurns, an ordinary lethal
Pyre attack, full five-body death, controller/link/card cleanup, kill-camera
state and sixteen immutable deployment-to-victory chains match native state/RNG.
Both win with Pyre77, normal exits, protected files unchanged and all four
failure counters zero. The combination additionally checks two UI previews,
five room previews and complete restoration of the five live one-HP bodies.
The original no-Covenant lethal-preview control also passes unchanged.

Diagnostic attempts `full-battle-no-cards-20261023-090830-ea572815` and
`full-battle-no-cards-20261023-090831-a05569a4` stopped at the old probe's fixed
no-Covenant HP assertion and produced no full battle archive. Neither is
promoted; accepted evidence comes from the fresh processes above.

All five paid-spell terminal targets also pass under the all-four combination:
`true-final-boss-paid-spell-covenants-{dominion,savagery,entropybot,entropymid,entropytop}.mt2f`.
Each uses the same explicit three-owner HP debuffs; only Dominion additionally
removes its four spell-shield stacks. The ordinary original spell costs one
energy and wins via the original native card path. Complete terminal state/RNG,
controller/link/group cleanup and sixteen immutable deployment-to-victory
chains match for every target (eighty total). Every native run exits normally
with protected files unchanged and capture/mismatch/unsupported/pending zero.
The retained original no-Covenant Dominion spell control still passes.

| Target | Native source (prefix `full-battle-units-spells-and-junk-`) |
| --- | --- |
| Dominion | `20261023-091224-444ee8a2` |
| Savagery | `20261023-091224-28d28ca4` |
| Entropy bottom | `20261023-091307-2ef0f4b9` |
| Entropy middle | `20261023-091307-4dbacb5f` |
| Entropy top | `20261023-091349-898d744c` |

This remains the explicitly disclosed native `Cheat_JumpToTFB` route. Natural
unlock, cumulative high-rank campaigns, winning play, natural relentless transitions
and other terminal signatures require their own evidence.
