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
Current checkpoint: 218 original owners accepted (200 native battle owners,
seventeen actual campaign-interface owners and one explicit native no-op owner);
6 remain pending. The latest accepted owner is `BalatroEventGrosMichel`:
its unchanged +10 non-Champion Monster attack, two saved waiting rounds then
10% destruction, and Banana Shake all execute. Two low waiting rolls advance
the counter; a mature miss retains the owner; a mature hit removes it and
undoes tracked deck attack while retaining live-unit attack. A later birth
loses the +10 bonus. The saved count remains two after removal, and the manager
still runs Banana Shake's attempt callback. Native preview, cardless/clone/
Champion births, cross-battle reacquisition, purged/non-deck cleanup,
simultaneous banana owners and additional callbacks remain uncalibrated.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects
need their actual campaign lifecycle evidence; definition enumeration and
inert battle presence do not validate those effects. Pure no-op definitions
must be explicitly identified without inventing activation evidence.

The latest fixture, `full-battle-original-gros-michel.mt2f`, captures four Tests,
four applications and four Banana Shake attempts. One initial seed produces
natural rolls 0, 7, 31 and 3. All callback states, RNG, notifications, saved count
and contribution undo match in sixteen immutable branches. Six accumulated/
reset Monster, Champion and Spell card-modifier controls also match.

The 40-stage winning battle records three model-selected actions and five
EndTurns, with all eight differential counters zero. Focused checks, independent
full policy replay, 128-state serial/parallel search and twelve affected archives pass.
The new fixture has zero unsupported search attempts. Both regression workers
finish with exit zero and all three tested assembly hashes remain unchanged.
Search matches all three recorded decisions and twelve root actions;
serial/parallel modes visit 128/128 states and return the same best result.
Regression summary: `.probe-runs/model-checks-20261018-150239-00a6ed67/summary.json`.
The 26,395-byte archive has 3,581
unique nodes and SHA-256
`9eef1d92102f4cbc2ee350702e990b1afee414a527cb7b79f010709bafb4a0ce`; no JSON
companion is tracked. Inventory is 437 archives: 398 battles and 39 component
calibrations. Original relic coverage is 218/224 (97.32%): 200 battle owners,
seventeen campaign owners and one native no-op owner.
