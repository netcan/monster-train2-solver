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
Current checkpoint: 212 original owners accepted (194 native battle owners,
seventeen actual campaign-interface owners and one explicit native no-op owner);
12 remain pending. The latest accepted owner is `ChanceToNotConsume`: its
unchanged 50% Spell Consume suppression produces two natural hits into Discard
and two misses into Exhausted. Full state, private trait state, play count,
output suppression and Battle RNG match. Explicit controls cover unplayed cards,
wrong type, missing Consume, Persistent and Infusion, cached Tests and Apply
reset. Infusion and Monster carriers are controls only; arbitrary additional
card/trait callbacks, native preview and combined relic effects are uncalibrated.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-suppress-exhaust.mt2f`, captures the
unchanged owner and 47 native callback records (sixteen Pre, twenty-eight Test,
three Apply). Full native/model context and output suppression agree. Pure checks
cover the exact 49/50 boundary, unconditional Apply versus gated manager, prior
output, exclusions, absent owner and normal discard cleanup. Sixteen parallel
branches remain immutable.

The 35-stage winning battle records eight actions and five EndTurns, with zero
differential counters. Focused native checks, independent four-EndTurn suffix,
128-state serial/parallel search and eight affected archives pass, with zero
unsupported search attempts. Both regression workers finish with exit zero.
The 27,045-byte archive has 3,952
unique nodes and SHA-256
`260ccbe91230ed80a2e752cdebfc7d29365a3ed9f7b7aff7f7aa97727bfc24da`; no JSON
companion is tracked. Inventory is 431 archives: 392 battles and 39 component
calibrations. Original relic coverage is 212/224 (94.64%): 194 battle owners,
seventeen campaign owners and one native no-op owner.
