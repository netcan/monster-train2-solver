# Advice panel continuity · Windows0.2.12

Playing a card used to clear the panel, replace it with waiting/calculation
messages, then redraw the remaining plan and scroll to the top. Recent real
player logs already showed `source=verified-plan`, `elapsedMs=0.0` and eight,
seven, six remaining steps: the visible refresh was not another route search.

The panel now retains its previous forecast through input, native loading work,
animation settlement, detached preparation and proof verification. Its separate
status line reads `正在核对后续步骤 · 原方案待确认`, and its text is dimmed until
publication. The session still invalidates Current immediately; keeping readable
text does not bypass fresh-root/state/RNG checks or make pending advice valid.
ReadyFor also rejects outstanding capture, selection and preparation work.

Native card and turn animations use LoadingScreen's busy counter. Checking only
the input hook and ordinary animation branch missed an earlier loading branch
which cleared the forecast after every card. When the save still identifies the
same in-battle scene, that branch now retains pending text as well. Outside a
battle, pause/hide, reset, errors and genuine fallback searches keep their clear
or waiting behavior. Only a Planning session with a newly started search displays
`正在计算新方案`; snapshot/preparation/reuse checks display checking status.

A verified continuation or floor publication keeps the previous normalized
scroll position. Changed text rebuilds only its own layout before restoration,
so shrinking suffixes use the new content height. Identical text skips assignment
and layout rebuilding. Explicit view expansion/collapse and a fresh route still
start at the top.

## Native acceptance

The opt-in development-only AdvisorPresentationProbe observes the separately
loaded production player. It checks retained text and scroll after real native
input hooks, rejects silent clearing during in-battle loading/animation frames,
checks pending labeling and invalid Current, and compares scroll positions on
verified publications. It also exercises pause/reset clearing and expands a long
plan to a reading position of0.37. The helper is excluded from the player DLL.

```powershell
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Seed 424242 -BattleDistance 0 -Policy units-spells-and-junk -PlayerAdvisor -RollingAdvice -AdvisorFloorBrowse -AdvisorPresentation -PlannerWholeBattle -PlannerSearchWorkers 2 -PhysicalSpawnPoints -BranchCardMasks -SkipBuild -TimeoutSeconds 300
```

The accepted profile is
`.probe-runs/full-battle-units-spells-and-junk-20261024-174841-c42fd552`.
It runs the exact installed0.2.12 three-DLL payload, built from77111e05, in an
isolated native process with the player's whole-battle/two-worker/3000ms settings.
The native driver plays21 cards and seven EndTurns, wins with73 Pyre HP, records
28 decisions and36 publications, and exits normally in88.71s. CaptureFailures,
Mismatches, Unsupported and Pending are all zero; protected originals are
unchanged. Seven native wheel inputs, including one during search, add no
searches. Existing pause/resume, hide/show, refresh, pointer/drag, plan preview
and idle checks pass. Presentation counters are:

| Check | Accepted count |
| --- | ---: |
| Retained input notifications, including mutations | 111 |
| Pending frames with retained text | 8219 |
| Verified reuse publications retaining the view | 33 |
| Long-plan publications retaining a non-top reading position | 19 |
| Explicit resets clearing old advice | 2 |

The complete native trace is `full-battle.mt2f`, SHA256
`54da5f87e3e04b9ccf93fa9e695e226fe531e190274101c5fe4f6ff13679b454`.
These are focused UI/native route observations, not a new independent frozen
replay pair or a latency/FPS benchmark. No full regression or Mac package ran.
Existing live Conduit transition and ordinary continuation controls pass.

## Current running game

PID59976 remains the user's original0.2.10 process. Its existing Conduit fix is
retained. A separate temporary presentation patch was applied at17:49:46 without
restart, playing cards or ending turns. It intercepts only the old overlay's
presentation stores/text/scroll and pending readiness; model verification remains
active. An unchanged-root notification smoke check retains text through11 pending
frames, republishes the same six-step proof with search count4 unchanged, verifies
the complete gameplay state/RNG with the existing unused-preview-allocator
normalization, and restores the original view. This short plan has no overflow;
the19 non-top scroll checks above come from the isolated long-plan acceptance.

Local evidence is `.probe-runs/live-advice-ui-debug/{active,smoke}.json` and
`trace.log`. The smoke observer detached after acceptance; the presentation fix
and earlier Conduit fix remain active until exit. The running process has not
loaded the new DLLs. Installed0.2.12 loads normally on the next launch; exact
payload hashes and the previous-version backup are in [mod-validation.md](mod-validation.md).
