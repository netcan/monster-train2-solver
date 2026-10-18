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
Current checkpoint: 195 original owners accepted (178 native battle owners,
sixteen actual campaign-interface owners and one explicit native no-op owner);
29 remain pending. The latest accepted owner is `CooldownOnDamage_DH`: its
unchanged `RelicEffectAddStatusEffectOnDamage` payload applies cooldown -1 to a
friendly Monster last damaged by a played card. Native evidence distinguishes
positive 2-damage Spell from an empty-Spell no-damage control and confirms one
cooldown transition from 3 to 2. Cooldown-zero/ability-ready callbacks, melee
and self-damage remain outside the model boundary.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-cooldown-on-damage-dh.mt2f`, captures
the unchanged owner, a cooldown-3 friendly Monster, positive Spell damage and an
empty-Spell control. The native callback and model agree on the full resulting
state and the target ends at cooldown 2; the status retains its Relentless-phase
prevention flag.

The 66-stage winning battle records 24 actions and seven EndTurns, with zero
differential counters. Focused native checks and 128-state serial/parallel
search pass. The 38,848-byte archive has 6,233 unique nodes and SHA-256
`ee148b997ad195ab93fcb8c7b60e53aae526256802f1603ff0c3f4e4bea285f3`; no JSON
companion is tracked. Inventory is 414 archives: 376 battles and 38 component
calibrations. Original relic coverage is 195/224 (87.05%): 178 battle owners,
sixteen campaign owners and one native no-op owner.
