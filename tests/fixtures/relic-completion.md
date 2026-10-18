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
Current checkpoint: 217 original owners accepted (199 native battle owners,
seventeen actual campaign-interface owners and one explicit native no-op owner);
7 remain pending. The latest accepted owner is `ChanceRetreatEnemiesOnSpawn`:
its unchanged 100% enemy AfterSpawnEnchant effect moves the active back enemy
to the front once per room. Four room flags reset after each turn's four-room
combat resolution, while terminal victory skips the later reset. Original
Boss subtype exclusions restrict the actor, not the moved target. Independent
trigger/team/repeated-room/excluded-actor controls and pure controls for all
four exclusions match. Native preview, the other three native exclusion
carriers, retained-dead cleanup and additional combined movement, Sentry,
enchantment, card or relic callbacks remain uncalibrated.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-spawn-retreat.mt2f`, captures 248 Tests,
four applications, four formation changes and four resets. Full state, return
values, the four private room flags and unchanged Battle RNG match independently
in sixteen immutable branches. An authored ordinary enemy carrying the first
original Boss exclusion is rejected as an actor but can still be moved.

The 42-stage winning battle records two actions and five EndTurns, with all
eight differential counters zero. Focused checks, an independent five-EndTurn
suffix, bounded serial/parallel search and fourteen affected archives pass.
The new fixture has zero unsupported search attempts. Both regression workers
finish with exit zero and all three tested assembly hashes remain unchanged.
Search matches both recorded decisions and six root actions; serial/parallel
modes visit 116/103 states and return the same best result.
Regression summary: `.probe-runs/model-checks-20261018-144235-a9f39a86/summary.json`.
The 25,296-byte archive has 3,638
unique nodes and SHA-256
`d10919d1a0d081c9c540580e6cd6aec47e81ab7e28cea921cfab025b47d97f81`; no JSON
companion is tracked. Inventory is 436 archives: 397 battles and 39 component
calibrations. Original relic coverage is 217/224 (96.88%): 199 battle owners,
seventeen campaign owners and one native no-op owner.
