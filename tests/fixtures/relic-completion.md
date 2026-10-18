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
Current checkpoint: 196 original owners accepted (179 native battle owners,
sixteen actual campaign-interface owners and one explicit native no-op owner);
28 remain pending. The latest accepted owner is `DrawCardPerDeckSize`: its
unchanged `RelicEffectCardDrawPerPileStartOfTurn` payload draws one extra card
per 25 visible save-deck cards after the ordinary player-turn draw. Seven native
threshold/reset Tests distinguish 0/24/25/49/50/24/50 cards, then the exact
50-card deck is restored for four natural callbacks that each draw two cards.
Other pile/owner payloads remain rejected; mixed after-draw relic combinations
and native positive applications at other deck sizes are not calibrated.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-deck-turn-draw.mt2f`, captures the
unchanged owner, threshold/control/reset Tests and repeated natural after-draw
callbacks. Full native/model context agrees, including hand/deck/discard order,
statistics and RNG. Pure checks cover hand limits, discard reshuffling and
deployment/preview/removed-owner gates; sixteen parallel branches remain immutable.

The 40-stage winning battle records 20 actions and five EndTurns, with zero
differential counters. Focused native checks and 128-state serial/parallel
search pass, with zero unsupported attempts. The 38,663-byte archive has 7,691
unique nodes and SHA-256
`7a522b92129d98429100f0978b04423e8ee6b863ae13f10647e41df1263b959c`; no JSON
companion is tracked. Inventory is 415 archives: 377 battles and 38 component
calibrations. Original relic coverage is 196/224 (87.50%): 179 battle owners,
sixteen campaign owners and one native no-op owner.
