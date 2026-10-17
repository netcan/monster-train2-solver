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
Current checkpoint: 194 original owners accepted (177 native battle owners,
sixteen actual campaign-interface owners and one explicit native no-op owner);
30 remain pending. The latest accepted owner is `BalatroEventTriboulet`: one
Rare deck card adds exactly +3 temporary health to Monster cards, and a cardless
spell summon receives its two ordered +2 max/current-health callbacks to reach
15/15 from base 11. The 63-stage winning capture has 20 actions and zero capture
failures, mismatches, unsupported transitions or pending records. Focused
checks and independent 128-state serial/parallel search pass. Evidence is scoped
to one Rare seed and one cardless spell-spawn witness; other spawn shapes and
owner variants remain unsupported.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-balatro-event-triboulet.mt2f`, captures
the unchanged owner, one-Rare card pool and cardless spell summon. The spell
lands in Discard rather than Monster Standby, and the spawned Monster ends at
15/15 after the ordered callbacks. Pure controls reject missing deck metadata
rather than guessing and distinguish cardless-from-card behavior.

The 63-stage winning battle records 20 actions and seven EndTurns, with zero
differential counters. Focused native checks and 128-state serial/parallel
search pass. The 39,815-byte archive has 6,404 unique nodes and SHA-256
`12bfc35e28c1bb37348fd311e7c84cd5e9b08bb51a82edcbbbcd15d29a0938bf`; no JSON
companion is tracked. Inventory is 413 archives: 375 battles and 38 component
calibrations. Original relic coverage is 194/224 (86.61%): 177 battle owners,
sixteen campaign owners and one native no-op owner.
