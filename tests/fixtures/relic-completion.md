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
Current checkpoint: 213 original owners accepted (195 native battle owners,
seventeen actual campaign-interface owners and one explicit native no-op owner);
11 remain pending. The latest accepted owner is `ApplyDazedOnAscendOrDescend`:
its unchanged effect actually gives Permafrost to cost>=3 or X-cost cards.
Full state and return values match across six callback kinds, cost threshold
changes, unique manager reset, six queries and two explicit previews. Playing
ordinary/X spells clears Freeze while retaining Permafrost; two natural draws
restore Freeze. A four-cost control remains in hand across four ordinary turns.
Equipment/Junk, temporary/full costs and battle-exterior trait notification
gating have pure/source checks; Monster plays, native preview-mode execution,
unfrozen previews and arbitrary combined callbacks remain uncalibrated.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-expensive-card-upgrade.mt2f`, captures
the unchanged owner and 35 native callback records: ten Apply, eleven Added,
six Has, three Permanent, two Preview and three TraitDrawn records. Ten manager
calls also match ordered dispatches and five exact trigger notifications.
The two record sets overlap. Five upgrade additions, one cost-based removal
with residual Freeze and two natural re-freezes pass independent replay.
Sixteen parallel branches remain immutable; missing combat flags are rejected.

The 35-stage winning battle records four actions and five EndTurns, with zero
differential counters. Focused native checks, independent four-EndTurn suffix,
128-state serial/parallel search and ten affected archives pass, with zero
unsupported search attempts. Both regression workers finish with exit zero.
Regression summary: `.probe-runs/model-checks-20261018-130806-2732733f/summary.json`.
The 23,832-byte archive has 3,230
unique nodes and SHA-256
`965eeb0cbf688c87ebb665ccc6c1e65c8ec1c4770ef82b14bb0afea7d793441c`; no JSON
companion is tracked. Inventory is 432 archives: 393 battles and 39 component
calibrations. Original relic coverage is 213/224 (95.09%): 195 battle owners,
seventeen campaign owners and one native no-op owner.
