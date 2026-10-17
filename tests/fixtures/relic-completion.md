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
Current checkpoint: 193 original owners accepted (176 native battle owners,
sixteen actual campaign-interface owners and one explicit native no-op owner);
31 remain pending. The latest accepted owner is `MothersRemnant3Relic`: its
unchanged effect permanently copies the first eligible directly played card
each turn, excluding Champions. A full-hand native witness sends the copy to
the draw pile; a second eligible card in the same turn is not copied. The
64-stage winning capture has 36 actions and zero capture failures, mismatches,
unsupported transitions or pending records. Focused checks and independent
128-state serial/parallel search pass. Other card-effect payloads, Champion
play branches in a native battle and alternate owner definitions remain
unsupported.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-mothers-remnant3.mt2f`, captures the
unchanged owner, direct eligible Spell, exact once-per-turn condition and a
full-hand permanent copy in the draw pile. The same-turn second eligible play
does not produce another copy. Pure controls verify open-hand routing and that
an excluded Champion does not consume the trigger count; missing Champion
classification is rejected instead of guessed.

The 64-stage winning battle records 36 actions, seven EndTurns, 14 train phases
and 11 spawns, with zero differential counters. Focused native checks and
128-state serial/parallel search pass. The 46,570-byte archive has 7,925 unique
nodes and SHA-256
`2ed1b92f320734342a582bffccd013964d7920c58e1aefe8e8fa27b75a57378e`; no JSON
companion is tracked. Inventory is 412 archives: 374 battles and 38 component
calibrations. Original relic coverage is 193/224 (86.16%): 176 battle owners,
sixteen campaign owners and one native no-op owner.
