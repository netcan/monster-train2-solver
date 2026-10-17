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
Current checkpoint: 173 original owners accepted (156 native battle owners,
sixteen actual campaign-interface owners and one explicit native no-op owner);
51 remain pending. The latest accepted native battle owner is
`2ChargesOnCardConsumed`: an unchanged +1 corruption effect triggers only for an
Exhaust carrier, changes only its selected floor, and passes a non-Exhaust
control. Its 61-stage winning replay and independent 128-state search pass with
zero unsupported attempts. `CorruptionAdded` callback floors and values already
above maximum are outside this witness. Campaign acceptance does not imply those
effect classes are integrated into battle branch state.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.
