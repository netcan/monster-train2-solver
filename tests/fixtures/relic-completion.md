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
Current checkpoint: 191 original owners accepted (174 native battle owners,
sixteen actual campaign-interface owners and one explicit native no-op owner);
33 remain pending. The latest accepted owner is
`MoonPhaseRoomsGainMagicPower`: its unchanged end-turn pre-discard effect adds
+10 damage/+10 heal to matching hand spells on Full Moon only. Five native
callbacks verify both phases and filter controls; the 49-stage winning capture
has 16 actions and zero capture failures, mismatches, unsupported transitions
or pending records. Focused checks and independent 128-state serial/parallel
search pass. Other owners, preview execution and unrelated moon-phase card
traits remain unsupported.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-moon-phase-rooms-gain-magic-power.mt2f`,
captures the unchanged owner and its Full Moon mask, Ignore room-modifier mode,
+10 damage/+10 heal upgrade, and two native filters. Across five pre-discard
callbacks, three Full Moon positive witnesses receive the upgrade, two New Moon
positives do not, and all five nonmatching controls remain unchanged. The
upgrade is non-unique and remains after discard. The witness definitions avoid
unrelated scaling-energy and moon-phase card-upgrade callbacks; support for
those separate lifecycles is not claimed.

The 49-stage winning battle records 16 actions, six EndTurns, 11 train phases
and nine spawns, with zero differential counters. Focused native checks and
128-state serial/parallel search pass. The 36,489-byte archive has 5,570 unique
nodes and SHA-256
`5df0de2f1cbf1791033109eba70a73e396a9d870b37c0c74beb82f971d4226c8`; no JSON
companion is tracked. Inventory is 410 archives: 372 battles and 38 component
calibrations. Original relic coverage is 191/224 (85.27%): 174 battle owners,
sixteen campaign owners and one native no-op owner.
