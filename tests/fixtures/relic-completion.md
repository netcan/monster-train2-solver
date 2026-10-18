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
Current checkpoint: 222 original owners accepted (204 native battle owners,
seventeen actual campaign-interface owners and one explicit native no-op owner);
2 remain pending. The latest accepted owner is `RandomUnitEveryFloor`.

Acceptance requires an unchanged original owner, actual eligible and control
paths, exact model/native state and relevant ordering/callback checks. Battle
effects need a winning native capture with zero error counters, independent
policy replay or explicit replay of every recorded action and turn transition,
and affected-fixture regression. Merchant/reward/draft effects need their actual
campaign lifecycle evidence; enumeration and inert battle presence do not
validate effects. Pure no-op definitions require explicit identification.

The schema-244 `full-battle-original-opening-deck-spawn.mt2f` witness retains
unchanged `RandomUnitEveryFloor`, its Null effect and three ordered spawn effects.
Native Tests cache only Monster cards in draw-pile order, giving 4/3/2 pools
with a non-Monster exclusion control. Apply shuffles the cache with CardDraw
RNG and births a card-backed non-clone unit on floors 0,1,2. Source cards are
discarded as played, bound to their units in Standby and restored to drawable;
natural card play, energy spending and relic-manager notification do not occur.
An isolated seeded-cache negative Test clears four entries with no remaining
unit cards, while the original owner/global train remain unchanged.
The 39-stage Pyre80 win has one ordinary play, five EndTurns, nine card cycles,
nine train phases, seven spawns and all eight error counters zero. Focused
complete-state checks, independent full policy/mid-battle replay, sixteen
immutable branches and 127/127-state serial/parallel search pass. Search
matches one recorded decision/fifteen root actions, replays two steps to the
turn boundary and has zero unsupported attempts and equal best results.
All twenty affected archives pass, both workers terminate with zero exits and
the three tested assembly hashes remain unchanged:
`.probe-runs/model-checks-20261018-162024-f8e9f681/summary.json`.
The 23,972-byte binary has 3,199 unique nodes, zero source length/hash, no JSON
companion and SHA-256
`3bc876a8c5e00b6d87c131828ec3c552b4c2911f360b23a207e704cae0cec3b6`.
Current inventory is 441 archives: 402 battles and 39 components. Corrupt-trait
births, full physical slots, birth-time removal and additional combined
birth/discard callbacks need separate native evidence.

The preceding schema-243 `full-battle-original-opening-morsels.mt2f` witness retains the
unchanged owner, its three ordered effects and the original Miner OnEaten body.
Floors 2,1,0 receive cardless, non-clone Miners without fallback/source cards,
RNG or relic-manager notifications. Two native isolated invalid-floor Test
controls leave state unchanged. Forty actual feeder-manager calls include one
real +9 Attack/+9 Health until battle end and three food/no-feeder controls.
Food receives its last-feeder relationship before OnEaten dispatch, then
despawns without damage-death/Harvest; OnFeed follows. The native FoodUnitsEaten
counter increases by one. The 42-stage win records one ordinary model-selected
play, five EndTurns, nine card cycles, nine train phases and seven spawns, ending
at Pyre79 with all eight error counters zero and original files unchanged.
Focused checks, complete independent policy and mid-battle replay, sixteen
immutable branches and 14/14-state serial/parallel search pass. Search matches
one decision/three root actions, replays two steps to the turn boundary and has
zero unsupported attempts with equal best results. All 24 affected archives pass;
both workers exit zero and all tested assembly hashes remain unchanged:
`.probe-runs/model-checks-20261018-160603-bf7a4709/summary.json`.
The archive has 24,798 bytes/3,362 unique nodes, zero source length/hash, no JSON
companion and SHA-256
`3cd901fa0c00402357eac809c6827069ebb7c40c49de54a34640b98213717a7a`.
Current inventory is 440 archives: 401 battles and 39 components. Card-backed,
equipped or repeatedly edible food, forced-feeding card effects, additional
combined callbacks and metagame unlock consumers require separate evidence.

The preceding schema-242 `full-battle-original-decay-death.mt2f` witness exercises the
unchanged all-team lethal Decay OnHit test and explicit friendly Decay transfer.
It compares 320 native tests and four complete applications: both teams,
ceil(5/2)=3 on each of two recipients, source-stack retention, unrelated positive
and negative statuses, untouchable units, other rooms and an actual no-recipient
death. Living actors, OnDeath, other damage types and null actors are controls.
Even an empty eligible team still produces one relic notification without RNG.

The 39-stage win records six ordinary model-selected plays, five EndTurns,
nine card cycles, nine train phases and seven spawns, ending at Pyre78 with all
eight differential counters zero and original profile/log files unchanged.
Focused checks, independent full policy replay with sixteen immutable branches,
128-state serial/parallel search and twenty affected archives pass. Search matches
six recorded decisions and eighteen root actions with zero unsupported attempts
and the same best result. Both regression workers terminate with exit zero and
unchanged tested assembly hashes. Summary:
`.probe-runs/model-checks-20261018-154054-e3b5eb66/summary.json`.

The archive is 29,887 bytes with 4,563 unique nodes and SHA-256
`9d3568d3546a175c9658a6fce1884dea2e4d6e11635f711a4fa4602bf879ed85`.
Source length/hash are zero, with no tracked JSON companion. That checkpoint's inventory
was 439 archives: 400 battles and 39 component calibrations. Other transfer
definitions, preview-only lifecycles and additional combined death/status
callbacks require separate native evidence.

Pending owners: `BalatroEventPerkeo` and `CorruptedAngelsArtifact`.

The preceding schema-241 `full-battle-original-bone-dog.mt2f` witness exercises the
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
Source length/hash are zero and no JSON companion is tracked. At that checkpoint,
inventory was 438 archives: 399 battles and 39 component calibrations, and accepted
coverage was 219/224 (97.77%): 201 battle, seventeen campaign and one native no-op.
