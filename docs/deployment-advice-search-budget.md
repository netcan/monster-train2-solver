# Deployment advice and selectable search duration

Version 0.2.15 fixes the empty-opening recommendation observed with 0.2.14
on 2026-10-24. The player's actual turn-zero snapshot has four energy,
HotHead, two shielded stewards and the free ImpQueenChampion, plus the
Forsaken opening trial. The published 0.2.14 line contained five EndTurns
and forecast defeat. This was an executable line, not evidence that doing
nothing was preferable or that the battle was unwinnable.

Whole-battle search spent its cold runtime budget on the empty-tower
rollout before trying a deployment. The planner now tries useful tactical
and strength deployments first, retaining a verified complete turn when
the remaining battle cannot be searched in time. A one-node/one-step
caller still receives its bounded EndTurn fallback. Cancellation, hard
node/step caps, strict-horizon callers and the terminal win score remain
unchanged. No losing result is relabeled as a win or an unknown outcome.

The panel's upper-right **时长** button cycles through 3, 10 and 30 seconds.
Changing it preserves the readable plan and scroll position; press
**重新计算** to use the new budget. The same setting is available live in
the configuration manager under `Search / TimeLimitMs` (100–30000ms), and
persists across game launches. A budget change does not restart active
work or discard a verified continuation. The initial default remains
3000ms, with at most two background search workers and 256 total nodes.
The budget is soft around indivisible model operations and does not
guarantee a complete or optimal solution. Partial windows are now labeled
**部分方案**, with a prompt to increase duration and recalculate.

## Evidence

`player-live-empty-deployment-advice.mt2f` preserves the actual 0.2.14
read-only root, published five-EndTurn line and UI diagnostic from
PID 134544 at 19:20:09. It contains 3416 unique nodes in 36736 bytes,
SHA256 `b56354f6055408afbdf0fc3e4155e1b7aacac0ba6df4347de122d0bb0d9a2373`.

The focused checker requires a legal deployment as the first action and
independently replays every returned state and RNG. It also injects an
atomic candidate that crosses a 50ms soft deadline, requiring a complete
first-turn fallback instead of an empty defeat, and checks that a 10-second
budget retains the known winning route. The prior repaired live Forsaken
check now requires a usable deployed line and truthful complete/partial
metadata, rather than accepting completeness alone.

A separate assembly compiled from the updated model sources searched this
same archived root inside the running game's Mono runtime, without Unity
calls, live-state capture, input or patches. Its evidence is retained at
`.probe-runs/live-advice-ui-debug/deployment-015-mono.json`:

| Budget | First play | Result | Steps / turns | Expanded nodes | Search time |
| --- | --- | --- | --- | --- | --- |
| 3 seconds, cold | ImpQueenChampion | Partial, battle unresolved | 5 / 1 | 20 | 3.125s |
| 10 seconds | ImpQueenChampion | BattleWon | 26 / 5 | 88 | 10.229s |

Both pass full-state/RNG replay and leave the archived parent unchanged.
This is captured-state search under Mono; the winning line was not executed
in the user's game. Timings and explored nodes vary with JIT/cache state
and available CPU time.

The actual production 0.2.15 overlay also passed a separate native sandbox
run at `.probe-runs/full-battle-units-spells-and-junk-20261024-193400-df2f544f`:
103.03 seconds, seven EndTurns, 21 actions, native win with 73 Pyre HP,
59 recorded room stages, and zero capture failures, mismatches, unsupported
transitions or pending records. Four duration changes verified button
cycling, persistence and live configuration-manager updates without a new
search or generation. Seven floor switches preserved search identity;
33 reused publications and 19 scrolled publications preserved readable
advice across native inputs. Original player files remained unchanged.

Focused atomic/ordinary/shorter budget, whole-battle parallel search,
live moon continuation and live floor-scroll checks passed. The catalog
contains 1360 registered archives. No full regression or macOS build was
run for this repair.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --deployment-advice-budget-only tests/fixtures/player-live-empty-deployment-advice.mt2f
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Policy units-and-junk -PlayerAdvisor -RollingAdvice -AdvisorPresentation -AdvisorFloorBrowse -PlannerWholeBattle -PlannerSearchWorkers 2 -SkipBuild -TimeoutSeconds 300
./scripts/Check-FixtureCatalog.ps1
```
