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
Current checkpoint: 219 original owners accepted (201 native battle owners,
seventeen actual campaign-interface owners and one explicit native no-op owner);
5 remain pending. The latest accepted owner is `BoneDogTitanPyreLife`.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects need their actual
campaign lifecycle evidence; enumeration and inert battle presence do not
validate effects. Pure no-op definitions require explicit identification.

The schema-241 `full-battle-original-bone-dog.mt2f` witness exercises the
unchanged 25% once-per-run resurrection through actual lethal Pyre damage.
An authored one-HP Steward dies first without consuming eligibility. With
Pyre maxHP81/currentHP1/attack100 and authored enemy attack one, a lethal hit
marks the persistent run flag before notification, retains one HP through
callbacks, then restores ceil(81*.25)=21 HP without Heal. Nine actual Pyre hits
are observed. A later-turn manager call returns false after ThisTurn condition
bookkeeping resets. The internal attack preview also runs the manager:
its save flag is restored but shared relic conditions retain the notification,
so preview/actual advance the condition count to one/two. Passive birth/status
condition queries refresh card-stat membership even when the condition fails.

The 52-stage winning battle records one model-selected action, seven EndTurns,
thirteen card cycles, fourteen train phases and eleven spawns, with all eight
differential counters zero. Focused checks, full independent policy replay with
sixteen immutable branches, serial/parallel search and twenty affected archives
pass. Search matches one recorded decision and three root actions; both modes
visit eleven states, replay two steps and stop at the turn boundary, with zero
unsupported attempts and the same best result. Both regression workers finish
with exit zero and all three tested assembly hashes remain unchanged.
Regression summary: `.probe-runs/model-checks-20261018-152107-bb38cb51/summary.json`.

Lethal SaveManager HP changes, non-combat consumers, cross-battle reacquisition,
other preview lifecycles and additional combined damage/hit/death/relic callbacks
remain outside this witness. Known lethal Pyre card-cost paths remain rejected.

The archive has 26,329 bytes, 3,606 unique nodes and SHA-256
`1827d0a4d977333a34f6fb3684785f1f4fdfd38c3d939d96a5187ccb2d47255a`.
Source length/hash are zero and no JSON companion is tracked. Inventory is
438 archives: 399 battles and 39 component calibrations. Accepted original
coverage is 219/224 (97.77%): 201 battle, seventeen campaign and one native no-op.
