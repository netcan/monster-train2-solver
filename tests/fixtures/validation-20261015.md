# Inventory validation, 2026-10-15

## Upgrade-count relic extension

The current inventory is 336 archives (305 battles and 31 calibrations), with
89/224 original collectible relic owners (39.73%). This extension validates the
new upgrade-count battle and six affected older archives with the current
model/checker; the complete 335-archive baseline below used the earlier model.
It is not a claim that all 336 archives were rerun after this extension.

Native source:
`.probe-runs/full-battle-units-spells-and-junk-20261015-174325-66c763b4/full-battle.mt2f`.
The unchanged `GiveSpellStatsForUpgrades_DH` and `GiveUnitStatsForUpgrades_DH`
definitions retain their original parameters and the spell owner's complete
`DevilishDetails` exclusion mask. The isolated game wins with Pyre health 80:
52 stages, 20 card plays, six EndTurns, 44 exact manager refreshes and zero
`CaptureFailures`, `Mismatches`, `Unsupported` and `Pending`. The launcher checks
that original Steam files remain unchanged.

The decompiled `RelicEffectModifyCardAttack`, `RelicEffectModifyCardHealth` and
`CardUpgradeState.ShouldShowIcon` implementations determine the rules. Permanent
visible upgrades give +3 each; hidden/no-icon upgrades and temporary upgrades do
not count. Visible region upgrades count here, unlike the separate upgrade-mask
counter. Counts 0/1/2, nonmatching card types, native excluded-pool rejection,
reset/repeat and individual owner ablations pass. The upgraded original Train
Steward and damage spell are played, with a later spell redraw retaining +6.
Cardless source-card scaling has pure boundary checks rather than a native
cardless witness in this capture.

Only the initial two witness plays are marked as scenario preparation. All
remaining 18 policy plays and six EndTurns run as one independent chain, with
a second mid-battle root and 16 immutable branches. Every native manager call
compares complete card state, dispatch/filter order, relic state and statistics.
The standard selected-fixture gates also exercise bounded serial/parallel search.

Curated archive `full-battle-original-upgrade-count-relics.mt2f`: 33,297 bytes,
5,417 unique nodes, direct-binary source length zero, SHA-256
`a601b943b86b373aa03c7e8a50b28ae3915684dc82a42bf37f3302285d11e7ec`.
All 336 archive lengths and hashes match the manifest; the owner table has 89
distinct entries. Regression evidence is
`.probe-runs/model-checks-20261015-174540-cec0ed4d/summary.json` (seven selected
fixtures, three workers).

| Runtime binary | SHA-256 |
| --- | --- |
| Model | `B97CCAE8E43F6AA5A34B80B098364F0400E5A363388476BA6AC6185E821678C1` |
| FixtureArchive | `9C875F79538C33A30367EA1E6A29694E868460FC109FE9341A72D6AD45AB3168` |
| Checker | `11E302F2FCD76A0C16BBC9212062B7689644753092D78AA674A654B211C02410` |

## Previous complete 335-archive baseline

All 335 baseline archives pass their curated general/focused regression gates:
304 full battles and 31 component calibrations. All 30 historical recordings
that played on disabled floors have fresh native replacements, preserving
their original scenarios and capture flags. Each replacement has fresh decision
snapshots, `NativeWon=true`, zero `CaptureFailures`/`Mismatches`/`Unsupported`/
`Pending`, and direct binary provenance without a JSON source. Its complete
independent policy and 16 immutable branches pass. The native workers verify
that original Steam files remain unchanged.

The final local audit checks all 335 archive lengths and SHA-256 values against
the manifest, the exact disjoint union of passed worker selections, terminal
exit records, no whole-policy skips, and all 30 replacements' freshness,
node counts and zero counters. It finishes with `COMPLETE-INVENTORY PASS` in
`.probe-runs/complete-inventory-20261015.json`.

| Accepted fixtures | Workers | Local evidence |
| --- | --- | --- |
| 306 | 11 | `.probe-runs/final-306-passed-shards-20261015.json` |
| 2 | 2 | `.probe-runs/model-checks-20261015-162633-4ece3f16/summary.json` |
| 27 | 8 | `.probe-runs/model-checks-20261015-165149-9b0688bf/summary.json` |

The first entry contains only the eleven fully passed shards of
`.probe-runs/model-checks-20261015-160823-d3d350bf/summary.json`.
That original 333-fixture run remains **Failed**: worker 10 rejected the fresh
original-resource recording because its specialized checker compared retired
terminal UI card views (six predicted definitions versus one native definition).
All 27 fixtures in that shard are rerun in the third entry, including the
resource recording. The two final recaptures in the second entry were excluded
from the original bulk selection. These three accepted selections have no gaps
or duplicates.

Commit `7983022` changes only `OriginalResourceRelicBatchChecks.cs` within the
model/archive/checker sources since model commit `d526e7c`. Its terminal
comparisons now use the same projection as the generic action/turn checker:
retired terminal `PlayRules` are excluded, while combat, resources, piles and RNG
remain compared. Nonterminal decision snapshots remain strict. The fix passes
the eight unchanged original owners/all 20 effects, eight real acquisitions,
48 energy bodies, and independent root/mid-battle policy replay. The audit
also reads the scenario of every reused old-checker fixture and rejects any
that could enter this changed resource-specific path.

| Runtime binary | SHA-256 |
| --- | --- |
| Model, identical in all accepted runs | `14984F85129B9A510DA25E4C08F298769A88E3484F412AAA323EF4E4C177F1A6` |
| FixtureArchive, identical in all accepted runs | `D131285A256F9182847716720122DCB844067F42C05CAC3F25CBC1FD6268F79F` |
| Checker used for the 306 + 2 unaffected fixtures | `3A23AA92DBC2259B465C063BC3448B7F3097454BEA9FA2AC2F6197FDDDCF9771` |
| Corrected checker used for all 27 rerun fixtures | `8F5E50238C146CB7CF3E7BDE0455F25CD1F863283D40BD745CD26E6210E52822` |

The parallel runner still invalidates binary changes during a run. The checker
switch occurs only after the original workers exit; only the checker is
replaced, retaining the exact tested model/archive binaries. The combined audit
explicitly permits this reviewed resource-only checker delta. It does not claim
that all 335 fixtures ran under one checker binary.

To repeat the complete curated regression from source:

```powershell
./scripts/Check-Models.ps1 -Workers 8
```

Historical archives may exclude the known-stale room `Enabled` field, while
fresh decision snapshots retain that comparison. Whole-policy legacy skips are
removed. This validates the recorded paths; unchanged original relic-owner
coverage remains 87/224 (38.84%), and full game/optimal solver coverage remains
unfinished.
