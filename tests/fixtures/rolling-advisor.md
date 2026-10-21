# Full-battle rolling advisor

Validated on 2026-10-21: four complete native encounters, each with a rolling
advisor source, an independent frozen-input replay and a completed native
`units-spells-and-junk` policy baseline. All twelve archives are settled
`DEPTH-PASS` captures with CaptureFailures, Mismatches, Unsupported and Pending
equal to zero. The launcher also verifies the original game files/preferences
remain unchanged.

The three distinct **actually present** boss identities are
`Boss_Level1_Basic`, `Boss_Level2_BigStats` and `Boss_Level5_Reanimate`.
The seventh-ring control ends before its boss enters; it contributes mechanic
evidence and does not count toward boss coverage.

## Replanning and independent verification

New branch-mask captures include the actual next-draw upgrade queue and its
allocation identity. Natural campaign cards can therefore use the existing
typed-draw model without an isolated scenario flag. Frozen legacy replays
retain their omitted/null queue schema rather than treating unknown state as
empty. A naturally progressed seventh-ring run at
`.probe-runs/full-battle-units-spells-and-junk-20261021-105845-d8f29b7d`
completed with zero mismatch/unsupported/capture/pending counters and 56
individual native draw records, but lost; it is diagnostic evidence only.

The native test driver waits for a newly published, read-only-capture-guarded
recommendation. It executes only that publication's first card/EndTurn, waits
for the native action to settle, compares the complete successor state and
gameplay RNG, then captures and computes again. Generations must increase
strictly. The interactive advisor remains advisory; this automation belongs
to the separate acceptance driver.

The sources contain 62 publications and 62 decisions: 35 card plays and 27
EndTurns, from deployment to actual victory/defeat. Every continuation is also
independently model-replayed before publication. The fresh native processes
search zero nodes and reproduce every frozen input, intermediate decision
state and terminal outcome. All 614 guarded source captures report zero purity
failures. The guard covers captured resources, piles, units/statuses, rooms,
native RNG streams including presentation streams, Unity RNG and frame.

The fixed policy runs to the actual battle end from the identical full initial
state, seed and encounter distance. Its complete action/EndTurn chain is also
replayed in the pure model without injecting later native states. The rolling
terminal score must be no worse than this completed policy, in addition to the
existing per-publication completed policy-seed guard.

Existing comparison rules normalize selected UI floor, terminal play-rule
caches and the preview-copy allocator; gameplay identities, histories, aura
state and RNG remain compared. These are the existing explicit comparison
boundaries, not byte equality of every private native object.

## Outcomes and scope

All cases use seed 424242 and the original starter deck, 80 initial Pyre HP and
native wave/Boss definitions. Distances 1, 4 and 6 use the native map's
`Cheat_SetDistance` plus normal branch/reward UI traversal in isolated profiles.
No battle stat/deck authoring or scenario card plays are used;
`InTestScenario=false` and the modifier scenario is empty. Map navigation
changes run progression/reward state: these are controlled encounter entries,
not naturally developed campaign decks or campaign-completion runs.

| Ring / distance | Native encounter | Actual boss | Decisions (cards / EndTurns) | Advisor result / Pyre HP | Completed policy result / HP |
| --- | --- | --- | ---: | --- | --- |
| 1 / 0 | Level1BattleJunker | Boss_Level1_Basic | 17 (10 / 7) | Win / 73 | Win / 66 |
| 2 / 1 | Level2BattleJunker | Boss_Level2_BigStats | 24 (17 / 7) | Win / 34 | Win / 33 |
| 5 / 4 | Level5BattleHeavyHarvester | Boss_Level5_Reanimate | 12 (5 / 7) | Defeat / 0 | Defeat / 0 |
| 7 / 6 | Level7BattleHordeSilence | None before defeat | 9 (3 / 6) | Defeat / 0 | Defeat / 0 |

The suite derives boss identities only from non-preview room occupants in
actual decision states. Captured future waves, predicted plans and preview-only
actors cannot satisfy the three-boss gate. The fifth-ring boss enters the
actual train; the loss does not prove its revival mechanic or a successful
middle-ring strategy. Equal defeat scores establish only a tie under the
terminal evaluator. The early wins improve Pyre HP by seven and one.

The runs additionally expose and verify unchanged native enemy
`CardEffectBuffDamage`: HeavyT2_Zealot Harvest buffs itself, while
ClericT3_IncantAttack Incant buffs eligible enemies in its room. Positive native
raw DamageBuff increases are required by the focused suite. Enemy fixed
Self/Room targeting without parent-card traits is supported; other trigger
owners, ranges and source-trait scaling retain explicit guards.

Natural future-wave auras now enable the shared enchantment world and matching
birth/death lifecycle flags before the first spawn. The seventh-ring chain
checks EnchanterT3_Silence removal, retained actor destruction, room index and
binding loss. Native temporary lethal Pyre previews also update auras outside
the locally restored room. A failed seventh-ring baseline exposed that last
case; a fresh baseline passed after the fix. The failed trace is not curated.

Outer-train/queued final-boss spawning remains explicitly unsupported. Boss
mechanic combinations, later-ring wins with progressed decks, unknown draws,
random-branch probabilities and global optimality remain unproven.

## Response latency

`EndToEndMs` starts with the guarded native capture and ends when the verified
recommendation text is assigned on Unity's thread. It includes capture/full
state key, queue/worker scheduling, search, independent verification, publication
polling and text construction. It excludes waiting for earlier native animations
to settle, the initial half-second observation interval, the optional diagnostic
panel PNG and physical screen presentation. It is a capture-to-UI-publication
measurement, not an input-to-photon deadline.

Across all 62 decisions, the median is **3893.4 ms**, nearest-rank P95
**4492.3 ms**, range **3238.0–4572.8 ms**. The three-second search budget is
soft: atomic expansions may overrun, and the remaining stages add latency.
Native acceptance speed is Instant. These measurements are local observations,
not a performance guarantee on other machines.

| Ring | Capture mean ms | Queue mean ms | Search mean ms | Verify mean ms | Total mean ms | Total P95 ms |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 215.4 | 38.6 | 3266.4 | 416.7 | 3996.1 | 4572.8 |
| 2 | 233.0 | 64.4 | 3156.7 | 449.3 | 3946.0 | 4374.1 |
| 5 | 146.7 | 14.2 | 3257.4 | 336.0 | 3783.6 | 4536.9 |
| 7 | 223.9 | 6.7 | 3187.9 | 498.3 | 3933.4 | 4561.8 |

## Retained evidence and reproduction

All profile suffixes below use
`.probe-runs/full-battle-units-spells-and-junk-20261021-`.
Binary names are `full-battle-rolling-advisor-ring<ring>-search.mt2f`,
`-replay.mt2f` and `-policy.mt2f`; hashes/sizes/node counts are in `manifest.tsv`.

| Ring | Rolling source | Fresh replay | Completed policy |
| --- | --- | --- | --- |
| 1 | 090032-57754f0d | 090352-8e4bfbc6 | 090501-ec865e3f |
| 2 | 092115-b911705a | 092437-0cd97c17 | 092544-c83ecd37 |
| 5 | 091007-cb43c8cd | 091421-c81d9bef | 091512-13f301de |
| 7 | 091902-56d10b7f | 092032-def5eb42 | 092633-2bbf1afb |

Run the focused acceptance suite, or create a new complete triple:

```powershell
pwsh -NoProfile -File scripts/Check-RollingAdvisor.ps1
pwsh -NoProfile -File scripts/Run-RollingAdvisorBenchmark.ps1 -BattleDistance 4 -Seed 424242
```

`-SourceTrace`, `-ReplayTrace` and `-BaselineTrace` reuse completed captures;
`-NativeWorker 2` selects the second isolated game copy. The suite checks all
four triples, three actual boss identities including a middle/later distance,
positive Harvest/Incant buffs and retained aura destruction/unbinding.

Progressed-deck victory acceptance uses `-InputProfile` and `-RequireWin`.
The latter rejects a lost source before starting a fresh replay, requires both
native runs to win, and requires an actual room boss in the source. It retains
all existing full-state, gameplay-RNG and completed-policy checks. Historical
loss controls still use the ordinary pair checker. `-PlannerHorizon 2` or `3`
sets the bounded forecast without changing the native battle or its rewards.
Known fifth-ring loss captures were used to verify both rejection paths.

`-PlannerTimeLimitMs` explicitly selects the shared search budget (100-30,000 ms;
default 3,000). Native advice evidence records that budget, and rolling logs
report expanded nodes and search time. Larger naturally progressed decks can
consume the default budget on the initial no-card forecast before the card
seed completes; an explicit longer budget permits measuring that limit.
The node limit, legality, independent replay and victory gates still apply.
`-SettleDeathDissolves` enables the existing observation protocol: it waits for
original dissolve callbacks before the game's own turn-end removal flush.
Retained actors then reflect native destruction, including cleared upgrade
collections and unbound enchantments; no gameplay effects are suppressed.

Validation for this change used these focused checks, advice-session
cancellation/staleness checks, native enchantment-world/lifecycle calibrations,
and existing random-aura death/revival EndTurn witnesses. No full battle
regression was run. The card inventory/hash refresh does not run that regression.
