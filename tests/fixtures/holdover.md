# Native Holdover lifecycle

The original `campaign-progressed-ring7.zip` branch now completes its seventh
battle while keeping the naturally purchased `TraitRetain` upgrade on
`StarterFireStarter`. The input save SHA256 is
`C20FAFA6062AD503E693BDE718D60C4266EDA49FABFB9287F8A14739D226A5CF`.
Its encounter, deck, upgrades, relics and gameplay RNG are unchanged. The
separate earlier campaign-completion branch still records its historical
decision to decline Holdover.

| Archive | Plays / EndTurns | Native outcome | Bytes / unique nodes |
| --- | ---: | --- | ---: |
| `full-battle-progressed-ring7-holdover-policy.mt2f` | 26 / 6 | loss | 67,461 / 11,414 |
| `full-battle-progressed-ring7-holdover-win.mt2f` | 33 / 9 | win, Pyre 104 | 82,839 / 14,411 |
| `full-battle-progressed-ring7-holdover-exact-replay.mt2f` | 32 / 9 | win, Pyre 104 | 81,479 / 14,151 |

All three launches finish with `DEPTH-PASS`, settled terminal effects, four zero
capture/mismatch/unsupported/pending counters, and original files unchanged.
The manifest records each archive's exact SHA256. These are full native battles.

The winning sequence originated in a bounded offline search: 256 nodes,
three-turn horizon, beam eight, twelve actions and 10,000 ms per decision on the
middle floor. Fresh native replay exposed a starting-Duality interaction:
the upgraded Steward must receive temporary Soldier of Fortune's fifteen
armor/avarice at thirty. After correcting the initialization order, the same
42 inputs were recomputed and verified from the original deployment root.
A fresh game process executed every input with zero search nodes and won
against `Boss_Level7_Rax`. All frozen successors, actual states, forecasts and
gameplay RNG match. This is an offline plan and native frozen-replay witness;
it does not add a live advisor latency measurement or a global optimality claim.

The [exhaustive solver](exact-search.md) subsequently explores all 36 states
within the final card decision's one-EndTurn horizon: 30 terminal leaves and
six horizon leaves. Its selected EndTurn omits the last unnecessary play.
Freezing the other 40 original inputs and that EndTurn produces a 41-step plan
from the same deployment root. A fresh native process
`20261021-182647-a9f9b1fd` executes it in 174.05 seconds, wins with Pyre104 and
matches all 41 successors, full states/RNG and the independent offline replay.
The original input save hash is unchanged. This establishes the shortened
route's native validity; the six horizon continuations prevent a whole-battle
optimality certificate.

`CardTraitRetain.retained` is captured as nullable `HoldoverPending`; missing
metadata is rejected. Played discard arms the return. At normal pre-draw the
trait clears its pending state, returns the card before ordinary draws and
subtracts one normal draw slot. A return increments `TimesDrawn` once and
preserves draw RNG. An already-held card only clears its flag. Opening-hand
and trait reset clear the flag; recast copies leave it unchanged. Unplayed
cards arm Holdover only when both Unplayable and an unplayed trigger exist.

Each battle includes 48 detached native callback controls: four card states,
pending false/true, played/unplayed, recast false/true, opening reset and trait
reset. The controls use the original merchant owner's definition, Scrap Metal,
Blight Pyre Damage and Titan Curse. Holdover is added only to these detached
copies; Titan Curse additionally receives an explicitly authored Unplayable
marker to exercise the positive conjunction. The live decision, every native
and Unity RNG stream and frame remain unchanged. These controls never enter
the live deck or count as additional original-card plays. Natural battle
callbacks separately prove positive return and repeated no-op behavior.

The focused checker verifies every callback's complete card/context/RNG,
ordinary draw cycles, the uninterrupted action/EndTurn chain and sixteen
immutable branches. Starting Duality has its own complete native birth check.
`holdover-variants-calibration.mt2f` additionally retains 100 original native
operations on detached cards: permanent installation, temporary upgrade,
direct temporary trait, base replacement and temporary-upgrade replacement,
each with both remove-on-discard settings. Every path checks the full card
before/after discard/recast callbacks, cached queries, forced refresh, trait
reset, cloning and discard cleanup. Ten independent native clones start with
cleared pending state. Upgrades copied from original data restore native copy
flags; the copied replacement rules are retained.

Native `CardStateModifiers.CopyUpgrades` dirties the **source** modifier cache.
The next query recreates a source upgrade-owned Holdover instance and clears
its pending state, while directly installed instances retain theirs. The model
returns that change in its child context and preserves the immutable parent.
The calibration compares this source side effect separately from the clone.
No detached card enters the live deck. The complete decision, all native/Unity
RNG streams and frame stay unchanged. The host is the isolated no-card battle
`20261021-181338-b0a16581`: settled terminal result, all four counters zero,
original files unchanged. A separate [176-operation permanent calibration](permanent-traits.md)
also covers all four native stack modes for Holdover, Corrupt, Consume,
Distinct and Ephemeral, repeated installation,
clobber protection and exact native modified-trait flags. Missing private state
remains an explicit rejection boundary.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --holdover-only tests/fixtures/full-battle-progressed-ring7-holdover-policy.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --holdover-variants-only tests/fixtures/holdover-variants-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --holdover-only tests/fixtures/full-battle-progressed-ring7-holdover-win.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --starting-duality-only tests/fixtures/full-battle-progressed-ring7-holdover-win.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --rolling-advice-only tests/fixtures/full-battle-progressed-ring7-holdover-win.mt2f

# Focused upgrade, native replay, exact search and unknown-draw acceptance:
pwsh -NoProfile -File scripts/Check-SolverCompletion.ps1 -SkipBuild

# Restore the original purchased-upgrade checkpoint, then use its returned profile.
pwsh -NoProfile -File scripts/Save-CampaignCheckpoint.ps1 -Restore -Archive tests/fixtures/campaign-progressed-ring7.zip
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -InputProfile <RestoredProfile> -BattleDistance 6 -RollingAdvice -RollingInput tests/fixtures/full-battle-progressed-ring7-holdover-win.mt2f -HoldoverCalibration -BranchCardMasks -PhysicalSpawnPoints -SettleDeathDissolves -TimeoutSeconds 900 -SkipBuild
```

The reported validation ran these focused checks plus existing Permafrost,
Intrinsic opening, Soldier of Fortune, status callback and seventh-ring turn
checks. No full regression was run for this change.
