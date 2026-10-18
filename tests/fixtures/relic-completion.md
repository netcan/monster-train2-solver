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
Current checkpoint: 216 original owners accepted (198 native battle owners,
seventeen actual campaign-interface owners and one explicit native no-op owner);
8 remain pending. The latest accepted owner is `IncantTriggeredByUnitAbilities`:
its actual effect applies -2 cooldown across friendly non-Pyre Tower targets on
the first Whelp spawn each turn. Deployment and next-turn positives, repeat
gates, cross-floor targets, zero clamping and trigger/team/subtype/null controls
match native evidence. A real ability carrier reaches zero without a ready
callback, gold reward or availability-marker change. Raw Apply and the manager
each notify once. The unused Room trigger remains metadata. Native preview,
eligible cardless or triggered Whelp births, retained dead targets, actual
ability activation and additional combined callbacks remain uncalibrated.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-whelp-cooldown.mt2f`, captures 285 Tests,
two applications and 383 manager calls. Full state, return values, RNG, raw and
managed notifications, per-turn reset and actual ability cooldown semantics
match independently in sixteen immutable branches.

The 43-stage winning battle records six actions and five EndTurns, with all
eight differential counters zero. Focused checks, an independent four-EndTurn
suffix, 128-state serial/parallel search and twelve affected archives pass.
The new fixture has zero unsupported search attempts. Both regression workers
finish with exit zero and all three tested assembly hashes remain unchanged.
Regression summary: `.probe-runs/model-checks-20261018-141829-bf8b1e80/summary.json`.
The 34,003-byte archive has 5,392
unique nodes and SHA-256
`96f0707e1114f1f1efc4199abc22df4d1ebffadc63e1218345dc9dc4714a6dbd`; no JSON
companion is tracked. Inventory is 435 archives: 396 battles and 39 component
calibrations. Original relic coverage is 216/224 (96.43%): 198 battle owners,
seventeen campaign owners and one native no-op owner.
