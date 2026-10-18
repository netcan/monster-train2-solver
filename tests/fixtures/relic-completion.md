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
Current checkpoint: 214 original owners accepted (196 native battle owners,
seventeen actual campaign-interface owners and one explicit native no-op owner);
10 remain pending. The latest accepted owner is `Inkbound_WolosBloodletter`:
its unchanged effects add 12 attack to Monster cards and apply one damage per
eligible player unit at room end. Armor absorption, recurring health damage,
untouchable exclusion, empty/Pyre rooms and lethal spawner routing match native
evidence. Raw lethal damage leaves the spawner in Standby until outer room
cleanup exhausts it. Native preview, healing/team variants and extra combined
trigger/relic callbacks remain uncalibrated.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-wolos-bloodletter.mt2f`, captures
nineteen Tests, eight damage applications, eleven per-target notifications and
thirteen attack manager calls. Full state, target caches, return values, RNG,
repeat/reset and Spell rejection match independently in sixteen immutable
branches. The default multiplier of one is inert in the native additive attack
branch, also fixing replay of the existing relic-count attack fixture.

The 38-stage winning battle records five actions and five EndTurns, with all
eight differential counters zero. Focused checks, independent five-EndTurn suffix,
128-state serial/parallel search and ten affected archives pass, with zero
unsupported search attempts. Both regression workers finish with exit zero.
Regression summary: `.probe-runs/model-checks-20261018-133309-07737cb7/summary.json`.
The 26,140-byte archive has 3,674
unique nodes and SHA-256
`7a6f0d50f1695bab3725eb3cb128953f91a566243ea8e6eec6c56630d91d105d`; no JSON
companion is tracked. Inventory is 433 archives: 394 battles and 39 component
calibrations. Original relic coverage is 214/224 (95.54%): 196 battle owners,
seventeen campaign owners and one native no-op owner.
