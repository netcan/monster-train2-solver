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
The unimplemented Holdover lifecycle remains guarded.

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
winning continuation; global optimality and outer-train bosses remain open.

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
these two continuations; eighth-ring advisor victory and outer-boss Pyre-entry
routing remain open. Default capture guards remain in place.

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
  seventh-ring checkpoint retaining the unsupported Holdover purchase.
- `campaign-ring7-20261021-104013-117d0c7a`: two native continuation wins,
  audited Holdover refusal, seventh-ring checkpoint; `CAMPAIGN-PASS`, 233
  events and original game files unchanged.

Checkpoint packaging and restore verified all nine retained members. Focused
validation also checked 840 native campaign draft selections and exact Rewards
RNG. No full regression was run for campaign preparation.
