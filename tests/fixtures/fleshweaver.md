# Original Fleshweaver calibration

The three original Ring3 variants now pass separate native births and complete
no-card battles at Covenant2. These explicitly select original scenarios from
the seed424242 checkpoint after two native victories. The checkpoint save is
`f78cf7dc9b266910fd8df4eb70aaa907c5f2f0e3d2f7b10aaf0c40bb1e214b1f`.
Authored assets and route RNG remain unchanged; these are selected routes.

| Variant | Raw attack / HP / Mass | Native attack base / HP / effective attack | EndTurns / rooms / train phases | Seconds |
| --- | --- | --- | --- | ---: |
| MoreMass | 6 / 600 / 8 | 7 / 780 / 23 | 7 / 60 / 14 | 71.63 |
| Corruption | 6 / 500 / 5 | 7 / 680 / 17 | 6 / 51 / 12 | 58.64 |
| Spike | 6 / 500 / 5 | 7 / 680 / 17 | 7 / 59 / 14 | 70.29 |

The native Covenant01+02 adjustment is +1 attack/+180 HP for all three variants.
Each original Mass stack supplies two attack. Complete creation, placement,
controller, death ownership and RNG match immutable birth branches. All three
no-card battles lose; every EndTurn, room/train stage and sixteen independent
immutable full-battle chains match full state/RNG. Each process exits normally,
preserves protected originals and records four zero failure counters.

The original OnHit trigger requires Mass, spawns its exact enemy mini, then
removes one Mass. The miniature enemy enters without a card, has Rooted and an
OnSpawn self/front rearrangement. Definitions use a finite enemy birth catalog;
unknown variants, spawn pools, card upgrades and targeting remain guarded.
The ordinary wave also contains `HeavyT1_BigAttackUpFront`; its original
OnDeathwishLost self attack reduction ignores stale upgrade editor fields.

| Variant | Accepted native source |
| --- | --- |
| MoreMass | `full-battle-no-cards-20261023-195514-7757bedc` |
| Corruption | `full-battle-no-cards-20261023-195627-b3b5ad2a` |
| Spike | `full-battle-no-cards-20261023-195727-f2479551` |

Six registered archives retain each full battle and separate boss birth:
`full-battle-fleshweaver-{mass,corruption,spikes}-baseline.mt2f` and
`outer-boss-birth-calibration-fleshweaver-{mass,corruption,spikes}.mt2f`.
Exact bytes, hashes and node counts are in `manifest.tsv`. Focused checks:
`--fleshweaver-birth-only <archive> <Mass|Corruption|Spikes>` and
`--fleshweaver-baseline-only <archive> <variant>`.

Positive legal-card battles remain diagnostic. Individual native card actions
have exposed preview birth lifetime, identity registration and shared room-cache
boundaries; full native acceptance and independent complete chains are still
required. Those diagnostics are not registered as passing evidence. Actual
player advice remains guarded until positive mechanics are accepted; the three
player-source/fresh-replay pairs also remain unverified.
