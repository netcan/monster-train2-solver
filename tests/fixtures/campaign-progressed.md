# Naturally progressed campaign checkpoints

These inputs use seed 424242, covenant 0 and the native Banished/Pyreborne
campaign. The driver selects offered rewards, purchases affordable upgrades,
chooses map branches and plays legal native actions. It does not author deck
contents or unit stats, grant arbitrary rewards, or jump map distances.
Preparation battles use a native heuristic policy or the accepted frozen
seventh-ring winning inputs. Model acceptance runs separately from each
immutable BattleIntro checkpoint.

`campaign-progressed-ring5.zip` retains the exact native save, preferences,
metagame and choice history. Its manifest hashes every member. The save has
four consecutive completed battle records, `wasDevCheatUsed=false`, and no
remaining boss HP in any completed battle. Native preparation outcomes were:

| Ring | Won | Pyre HP after battle |
| --- | --- | ---: |
| 1 | yes | 76 |
| 2 | yes | 76 |
| 3 | yes | 57 |
| 4 | yes | 77 |

The fifth battle is now accepted separately by the three
`full-battle-progressed-ring5-{search,replay,policy}.mt2f` archives. The rolling
advisor wins against the actually present `Boss_Level5_Reanimate`, finishing
with 59 Pyre HP after 17 card/ability plays and nine EndTurns. A fresh native
process searches zero nodes and reproduces all 26 decisions, complete state,
gameplay RNG and the same victory. The completed fixed policy loses. All
three captures settle with zero capture, mismatch, unsupported and pending
counters, and all launchers verify the original game files unchanged. The
source's 275 guarded captures have zero purity failures.

Check this group with `--rolling-advice-victory-pair-only` and the search,
replay and policy archives in that order. The input checkpoint alone does
not prove these results. These archives retain the naturally progressed
fifth-ring victory; the separately accepted seventh-ring group is below.

`campaign-progressed-ring7.zip` preserves six natural preparation wins. Its
fifth and sixth victories finished at 69 and 48 Pyre HP. Strict rolling
acceptance stopped at the explicit `CardTraitRetain` draw/opening-hand guard:
the optional `TraitRetain` spell upgrade had been purchased after ring five.
That historical guard is now closed for captured default installed Holdover.
The original purchased-upgrade input has its own completed fixed-policy loss
and a fresh frozen victory: 42 inputs, Pyre104, four zero failure counters,
48 detached native callback controls and uninterrupted full-state/RNG replay.
See [Holdover lifecycle evidence](holdover.md). Temporary/replaced Holdover,
cloning and in-battle installation retain their separate guards.

`campaign-progressed-ring7-supported.zip` is a separate native branch from the
same fifth-ring input. `-ExcludeUpgradeTrait CardTraitRetain` declines the
offered upgrade by its actual trait payload, records that choice, and otherwise
continues native play/rewards. Rings five and six finish at 69 and 55 Pyre HP;
the seventh-ring BattleIntro save has six completed wins and no cheat flag.
Its save SHA256 is
`BC8FB6302064275D0B4F9864493909E7D5D4CA6F3BABA481ED7BAE6C03E74BA2`.
The ZIP SHA256 is
`E22329195262C5C8864D462085A3D214B9718E977026F52ACEF0169E4B1A2D26`;
all 14 data/history members were verified on restore. A prepared checkpoint
does not itself establish a rolling-advisor victory.

The supported input now has its own strict victory group,
`full-battle-progressed-ring7-{search,replay,policy}.mt2f`. The rolling advisor
defeats the actually present `Boss_Level7_Rax`, retaining 111 Pyre HP after
32 card/ability plays and ten EndTurns. A fresh native process searches zero
nodes and reproduces all 42 decisions, full state, gameplay RNG and victory.
The completed fixed policy plays 30 actions/six EndTurns and loses. All three
runs finish with `DEPTH-PASS`, settled terminal effects and zero capture,
mismatch, unsupported and pending counters; all launchers verify original
files unchanged. The search has 839 guarded captures and zero purity failures.

This run explicitly uses the middle floor, three EndTurns of lookahead and
a 10,000 ms shared search budget per decision, with the existing 256-node
limit. Source/replay native elapsed times are 1,085.12/199.41 seconds; policy
elapsed time is 213.37 seconds. The default 3,000 ms budget is preserved and
its earlier zero-difference seventh-ring runs lost. This establishes a bounded
winning continuation; global optimality remains unproven. The later natural
eighth-ring outer-boss acceptance is described below.

The probe stops at settled combat before campaign victory/reward persistence.
Its output save still has six completed battles, so continuing to a legal
eighth-ring checkpoint requires the native campaign transitions separately.

`campaign-progressed-ring8.zip` now retains that continuation. The campaign
launcher replays the accepted seventh-ring sequence in a fresh native process,
checks every root/successor and gameplay RNG, then lets the original combat-end,
victory, reward and map coroutines continue. The replay searches zero nodes,
reproduces all 42 decisions and wins with 111 Pyre HP; its capture, mismatch,
unsupported and pending counters are zero. The campaign completes 52 audited
events and verifies original game files unchanged. It saves BattleIntro at
distance seven with all seven preceding native wins and no cheat flag.

The eighth-ring input save SHA256 is
`682051D891549EC76DC716A49119CAD67E9F8EF838A5BDF23C62499CB5231083`;
the ZIP SHA256 is
`3C02949FC5AEDD2B6D8A8AFAE60890FAB2F2B1E482BE0C12DB30DA7A94AA0406`.
All 25 data/history members were verified on restore. The package also retains
`campaign-battle-replay.mt2f`, the actual replay capture before campaign handoff.
The checkpoint itself is a legal eighth-ring input. Two separate full-battle
archives now accept model behavior on that input:
`full-battle-progressed-ring8-no-cards.mt2f` and
`full-battle-progressed-ring8-policy.mt2f`. Opt-in `-OuterBossBirth -OuterBossTurns
-PhysicalSpawnPoints` captures the original queued boss definition at deployment
and models creation, initial placement, casting actions and the complete EndTurn
sequence. Both captures settle in native losses with all four failure counters
zero and protected game files unchanged. Frozen model replays start at one
deployment root and match every successor, including legal deployment plays,
without substituting later native snapshots. The no-card/policy runs retain
seven/six EndTurns and zero/27 legal card actions, including five deployment
plays in the policy. This accepts the battle model for
these two continuations. Outer-boss Pyre entry now has separate controlled
component and frozen-EndTurn calibration; at that earlier milestone eighth-ring
advisor victory remained open. Default capture guards remain in place.

`campaign-progressed-ring8-gold.zip` retains a second legal preparation branch.
It replays the same 42 accepted seventh-ring decisions with zero search nodes
and zero differential counters, then declines purchases through the native
merchant close buttons. Its 54 audited events retain all visible merchant
offers, cached prices and availability. This preserves 245 gold for the deck's
50-gold armor spells, 31 cards, three artifacts and seven native victories;
the original branch instead spends 140/40 gold on `ExtraSpellUpgrades` and
`MeatShield`. The gold branch now has an accepted frozen native eighth-ring
victory with Pyre4: 50 decisions, including 39 plays and 11 EndTurns, all four
failure counters zero. Its archive is
`full-battle-progressed-ring8-gold-frozen-win.mt2f`; the later rolling-advisor
acceptance is described below. Protected game files remain unchanged and the cheat flag is false.
The input save SHA256 is
`FDE5F23D234C15C200187FCE899D5EB4598A260A72E37DBF2A6972850F717772`;
ZIP SHA256 is
`7F984A83965B016CC6E81168068A82CBE04A05AA62C1E4FAFB4491A50DA2CF15`.
All 25 data/history members are hash-verified on restore.

The gold input now also has its own accepted rolling group,
`full-battle-progressed-ring8-gold-{search,replay,policy}.mt2f`. Live source
`full-battle-units-spells-and-junk-20261021-145947-7ab0de83` defeats the actual
`Boss_Level8_AllyBuffer` with Pyre21 in 50 decisions (39 plays/11 EndTurns).
All four failure counters and 930 capture-purity checks are zero; original
files are unchanged. The explicit budget is 12 EndTurns/10,000 ms/256 nodes,
with the top-floor tactical seed and opt-in isolated .NET planner. Full native
elapsed time is 1,075.27 seconds; capture-to-UI median/P95 are 11.1/12.9 seconds.
No queue/HP controls, map jumps, authored card definitions or combat stats are
used. The completed fixed policy from the same root loses after 32 plays and
six EndTurns (source `20261021-144046-60bef415`, 242.02 seconds).

Fresh native profile `campaign-ring9-20261021-151751-8086c74a` searches zero
nodes, preserves every frozen action and forecast, and reproduces the same
Pyre21 victory with full state/RNG equality. It then continues through the
game's own standard run completion. Focused
`Check-RollingAdvisor.ps1 -ProgressedRing 8 -SkipBuild` checks the complete
source/replay chains and the completed same-input policy comparison.
The strict pair checker passes all 50 decisions and every frozen continuation;
terminal evaluator scores are -1,000,000,000,021 for the advisor and
1,000,000,000,000 for the completed losing policy (lower is better).
This focused acceptance and the manifest/card-index checks do not rerun the
full regression inventory.

## Native run completion

`Run-CampaignProgress.ps1 -StopBeforeRing 9` can hand an accepted eighth-ring
frozen sequence back to the game's own victory, map and end-run flow. It observes
`GameStateManager.EndGame` before native `FinishRun` resets the active slot,
retains the actual victory reason/type and each completed battle, then waits
for the native GameOver/unlock UI. A standard victory is accepted using the
game's own victory API; a true-final-boss victory is a distinct native result.
No victory type is authored. The outer-boss replay needs the explicit
`-OuterBossBirth -OuterBossTurns` capture flags, as in battle acceptance.

`Save-CampaignCheckpoint.ps1 -CompletedRun` bundles the completed standard
eight-battle run after checking its native replay summary, victory callback,
eight ordered winning records and unchanged protected files. Native `FinishRun`
can replace both the active slot and its backup; the archive retains that slot
and a native eight-win snapshot serialized at EndGame before reset, together
with the battle replay, audit and preparation history. Its manifest
uses `StateKind=CompletedNativeRun`. Restore verifies every member hash and
labels it as a completed-run profile; the earlier BattleIntro archives remain
the inputs for reproducing battles.

The retained `campaign-completed-ring8.zip` is this native `Standard` victory:
all eight ordered native battle records are wins, the cheat flag is false,
the eighth replay retains Pyre21 and the original profile files are unchanged.
The active slot's native reset is preserved alongside
`completed-save-singlePlayer.json`, serialized using the game's own
`JsonUtility` at EndGame before that reset. The completed snapshot retains
31 deck cards, three relics and the original seed 424242. All 48 data/history
members are hash-verified on restore. ZIP SHA256 is
`249469AAFFF652AB00107B5556FA80C5D99C2412CFC435E29F0546946E0500F2`;
completed snapshot SHA256 is
`DA169B1E971EC6E5A7473F643505AE1F5A0A88F218A165DAE3BF2D28F2F982AC`.
This is an eight-battle standard run; the separate true-final-boss route is not
claimed by this result.

```powershell
pwsh -NoProfile -File scripts/Run-CampaignProgress.ps1 -InputProfile <GoldRing8Profile> -BattleReplay tests/fixtures/full-battle-progressed-ring8-gold-search.mt2f -StopBeforeRing 9 -OuterBossBirth -OuterBossTurns -TimeoutSeconds 1200
pwsh -NoProfile -File scripts/Save-CampaignCheckpoint.ps1 -CompletedRun -Profile <CompletedProfile> -Archive <NewCompletion.zip>
pwsh -NoProfile -File scripts/Save-CampaignCheckpoint.ps1 -Restore -Archive <NewCompletion.zip>
pwsh -NoProfile -File scripts/Save-CampaignCheckpoint.ps1 -Restore -Archive tests/fixtures/campaign-completed-ring8.zip
```

## Reproduce

```powershell
pwsh -NoProfile -File scripts/Run-CampaignProgress.ps1 -StopBeforeRing 5
pwsh -NoProfile -File scripts/Save-CampaignCheckpoint.ps1 -Restore -Archive tests/fixtures/campaign-progressed-ring5.zip
# Use the restored Profile returned above:
pwsh -NoProfile -File scripts/Run-RollingAdvisorBenchmark.ps1 -InputProfile <Profile> -BattleDistance 4 -RequireWin
# Seventh-ring supported input and its explicit budget:
pwsh -NoProfile -File scripts/Save-CampaignCheckpoint.ps1 -Restore -Archive tests/fixtures/campaign-progressed-ring7-supported.zip
pwsh -NoProfile -File scripts/Run-RollingAdvisorBenchmark.ps1 -InputProfile <ReturnedProfile> -BattleDistance 6 -PlannerDeploymentRoom 1 -PlannerTimeLimitMs 10000 -SettleDeathDissolves -RequireWin
# Check only the retained seventh-ring triple:
pwsh -NoProfile -File scripts/Check-RollingAdvisor.ps1 -ProgressedRing 7 -SkipBuild
# Continue the accepted frozen inputs through native rewards/map to ring eight:
pwsh -NoProfile -File scripts/Run-CampaignProgress.ps1 -StopBeforeRing 8 -InputProfile <SupportedRing7Profile> -BattleReplay tests/fixtures/full-battle-progressed-ring7-search.mt2f -ExcludeUpgradeTrait CardTraitRetain -TimeoutSeconds 1200
pwsh -NoProfile -File scripts/Save-CampaignCheckpoint.ps1 -Restore -Archive tests/fixtures/campaign-progressed-ring8.zip
# Keep gold through the original merchant UI instead of buying the first offer:
pwsh -NoProfile -File scripts/Run-CampaignProgress.ps1 -StopBeforeRing 8 -InputProfile <SupportedRing7Profile> -BattleReplay tests/fixtures/full-battle-progressed-ring7-search.mt2f -ExcludeUpgradeTrait CardTraitRetain -SkipMerchantPurchases -TimeoutSeconds 1200
```

The campaign driver saves a separate direct child of `.probe-runs` before
every battle. `-InputProfile` copies and continues a retained native run; it
does not overwrite its input. The battle launcher requires a native
BattleIntro save at the requested distance and records the input save hash.
Export a newly completed checkpoint with `Save-CampaignCheckpoint.ps1
-Profile <Profile> -Archive <new.zip>`.
`-BattleReplay` requires an unchanged native BattleIntro input and stops before
the immediately following ring. It checks the replay before campaign handoff;
it does not repeat planning or substitute a terminal combat save for a new input.
Merchant auditing reads cached `MerchantGoodState.GetCost`, rather than the
randomized reward-price getter, and does not advance Rewards RNG.

## Retained preparation evidence

Profiles under `.probe-runs`:

- `campaign-ring5-20261021-095939-bae3b645`: first win and native merchant
  upgrades; interrupted at the map while correcting repeated shop visits.
- `campaign-ring5-20261021-100151-82c2420d`: native continuation, three more
  wins, completed fifth-ring checkpoint; `CAMPAIGN-PASS`, 346 audit events,
  original game files unchanged.
- `campaign-ring5-20261021-100151-82c2420d-before-ring5`: exported immutable
  input, save SHA256
  `3E380C8CCF98146227D97CAE6997BE8F0CBE16995F5A637431657D68A6347E46`.
- `campaign-ring7-20261021-103038-eaa75877`: native sixth victory, original
  seventh-ring checkpoint retaining the originally guarded Holdover purchase,
  now accepted by the separate Holdover full-battle fixtures.
- `campaign-ring7-20261021-104013-117d0c7a`: two native continuation wins,
  audited Holdover refusal, seventh-ring checkpoint; `CAMPAIGN-PASS`, 233
  events and original game files unchanged.

Checkpoint packaging and restore verified all nine retained members. Focused
validation also checked 840 native campaign draft selections and exact Rewards
RNG. No full regression was run for campaign preparation.
