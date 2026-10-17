# Remaining original relic coverage

User objective: finish original collectible relic coverage, with small commits
after each independently validated batch. The accepted owner table remains
[relic-coverage.md](relic-coverage.md); the exact 224-owner checklist is
[relic-inventory.tsv](relic-inventory.tsv). A pending row is not evidence.

The complete native catalog was refreshed on 2026-10-17 with
`Run-FullBattleProbe.ps1 -RelicCatalog -BinaryCapture -GameSpeed Instant -SkipBuild`.
It contains 662 definitions from eight collections, including 224 unique
collectible relics, with identical native frame/RNG before and after enumeration.
All 224 owners have local source files in `.game-reference/native-source/`.
Catalog path:
`.probe-runs/full-battle-no-cards-20261017-062824-ae5e430a/relic-catalog.mt2f`.
Schema 8 retains the required card traits introduced in schema 6.

Regenerate the checklist with `scripts/Get-RelicCoverageInventory.ps1 -Catalog
<relic-catalog.mt2f> -OutputPath tests/fixtures/relic-inventory.tsv`. This verifies
catalog identity, unique owner IDs, accepted table identities and source presence.
Current checkpoint: 180 original owners accepted (163 native battle owners,
sixteen actual campaign-interface owners and one explicit native no-op owner);
44 remain pending. The prior `ModifyDragonsHoardBattleStart` stage is reconciled
as accepted in the checklist alongside its committed schema-201 witness. The
latest accepted owner is `BalatroEventCavendish`: both card modifier reset modes,
cardless ordinary/Champion callbacks, deterministic 1% end-combat miss/hit,
tracked deck undo, the 48-stage full battle and independent 128-state search
pass. The final native BattleWon boundary preserves the owner and Battle RNG,
while the end-of-combat callback is modeled on cleared nonterminal combat only.
Other owners with attack multipliers and broader end-combat callback families
remain outside this witness. Campaign acceptance does not imply those effect
classes are integrated into battle branch state.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.
