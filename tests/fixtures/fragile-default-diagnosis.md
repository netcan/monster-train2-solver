# Natural Fragile default-budget diagnosis · 2026-10-23

The accepted natural Fragile victory still uses twelve turns/30000ms,
bottom deployment and the explicit tactical seed; see
[the native winning pair](player-fragile-winning.md). It does not establish
a default three-turn/3000ms victory.

Two focused model diagnostics now work from that unchanged captured root:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --deployment-forecast-only tests/fixtures/full-battle-player-boss-fragile-search.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --rolling-player-forecast-only tests/fixtures/full-battle-player-boss-fragile-search.mt2f 256 2
```

The deployment comparison plays each floor's tactical deployment, then ends
turns without additional cards until three complete EndTurns. Every branch
passes independent complete-state/RNG replay and root immutability. The
short-horizon public score is saturated at the same value on all three floors:

| Room index (bottom = 0) | Score | Unclamped positional ordering | Inputs |
| --- | --- | --- | --- |
| 0 | -61501 | 3840.5 | 8 |
| 1 | -61501 | 3851.5 | 8 |
| 2 | -61501 | 3873.2 | 8 |

The ordinary default planner still selects room2 in this diagnostic. The
table compares deployment-only continuations; its lower positional value
does not prove that changing floors will win a complete rolling battle.

The rolling diagnostic uses 256 nodes, three turns, beam8, twelve actions
per turn, a three-second soft deadline, preferred room2 and no extra tactical
seed. It verifies every complete forecast before consuming it. Ordinary
suffixes last through the current EndTurn, winning suffixes can cross turns,
and defeat forecasts consume only their first input. A final independent
replay checks all consumed inputs, the complete state/RNG and the original
root. The original per-input `--rolling-forecast-only` mode remains available.

The latest offline run loses during turn7 after 48 inputs and seventeen
searches (442 nodes, 66.20 seconds including verification). All seventeen
forecasts and the full consumed route pass state/RNG replay. An earlier run
also loses after 48 inputs/seventeen searches, with 410 nodes. Soft-budget
node counts and chosen paths can vary with runtime timing; neither run is
a native default-budget acceptance or a universal impossibility proof.

These commands diagnose model strategy, not live Unity scheduling or native
acceptance. A successful diagnostic exit means replay integrity passed;
read the printed outcome separately. This gap needs a default-budget winning
model route followed by a fresh native player source/replay pair. The
accepted Ring7 default victory and Fragile long-budget victory remain
separate evidence. No full regression or Mac build is required for diagnosis.
