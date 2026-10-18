# Remaining original relic coverage

User objective: finish original collectible relic coverage, with small commits
after each independently validated batch. The accepted owner table remains
[relic-coverage.md](relic-coverage.md); the exact 224-owner checklist is
[relic-inventory.tsv](relic-inventory.tsv). A pending row is not evidence.

The complete native catalog was refreshed on 2026-10-18 with
`Run-FullBattleProbe.ps1 -RelicCatalog -BinaryCapture -GameSpeed Instant -SkipBuild`.
It contains 662 definitions from eight collections, including 224 unique
collectible relics, with identical native frame/RNG before and after enumeration.
All 224 owners have local source files in `.game-reference/native-source/`.
Catalog path:
`.probe-runs/full-battle-steward-once-20261018-064136-77e7b659/relic-catalog.mt2f`.
Schema 9 retains the native definition and callback data needed by the current
coverage inventory checks.

Regenerate the checklist with `scripts/Get-RelicCoverageInventory.ps1 -Catalog
<relic-catalog.mt2f> -OutputPath tests/fixtures/relic-inventory.tsv`. This verifies
catalog identity, unique owner IDs, accepted table identities and source presence.
Current checkpoint: 215 original owners accepted (197 native battle owners,
seventeen actual campaign-interface owners and one explicit native no-op owner);
9 remain pending. The latest accepted owner is `GainBonusOnThreeCardsPlayed`:
its unchanged effect records distinct non-ability card types, notifies each
successful step and grants +1 energy plus one draw on the third type. Independent
ability/duplicate/cap controls and Spell/Junk/Blight sequences on two natural
turns match native evidence. An empty draw/discard pile still permits the energy
bonus. Raw start clears the type list separately from condition-counter duration
resets. Native Monster/Equipment sequences, actual ability plays, preview
execution and additional combined card/draw/relic callbacks remain uncalibrated.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-card-type-sequence.mt2f`, captures
fifteen Tests, nine applications and six start callbacks. Full state, private
type lists, return values, RNG, first/second-step notifications, empty-pile
energy and list/counter reset semantics match independently in sixteen immutable
branches. The ability control precedes the first ordinary Spell, avoiding a
confounded duplicate-type rejection. Native DrawCards does not receive the
played card, and replay retains that hand-capacity behavior.

The 35-stage winning battle records ten actions and five EndTurns, with all
eight differential counters zero. Focused checks, independent three-EndTurn suffix,
128-state serial/parallel search and ten affected archives pass, with zero
unsupported search attempts. Both regression workers finish with exit zero.
Regression summary: `.probe-runs/model-checks-20261018-135135-d423338c/summary.json`.
The 28,469-byte archive has 4,091
unique nodes and SHA-256
`15890ab33b3500655efde69a3eba776de0bdac91b8a6f3fc59aa8da76a382fa6`; no JSON
companion is tracked. Inventory is 434 archives: 395 battles and 39 component
calibrations. Original relic coverage is 215/224 (95.98%): 197 battle owners,
seventeen campaign owners and one native no-op owner.
