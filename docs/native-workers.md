# Isolated native validation workers

`Run-FullBattleProbe.ps1` and `Run-NativeCovenantPlayerPairs.ps1` accept
`-NativeWorker 1` through `4`. Worker1 uses `.sandbox-game`; the others use
`.sandbox-game-worker2`, `3` and `4`. Each has private game/loader/plugin/config
files, a separate runtime profile and an exclusive `.probe-runs/native-worker-N.lock`.
The launcher also rejects a surviving game process using that worker's executable.

Prepare an additional worker once with
`scripts/Initialize-NativeWorker.ps1 -Worker 3` (or `4`). It validates the existing
isolated Worker1 template, rejects shared/reparse paths and existing destinations,
and copies private game files plus loader core/patchers and base configuration.
It verifies the copied executable, Unity player, loader entry and BepInEx core.
Plugins/configurations for each acceptance run are installed separately by the
existing runner. No original game install or user save is changed by initialization.

The first two workers retain their existing paths. Extra slots are explicit and
require initialization; this does not launch four games automatically. Use them
for independent focused cases while player-source/frozen-replay pairs occupy
other slots. Build dependencies before launching; do not rebuild a checker that
is currently validating a pair. Match concurrency to available RAM/CPU.

Each process still requires a normal exit, protected original-file signatures,
all four failure counters zero and `native-accepted-summary.json`. A completed
player pair additionally requires `player-pair-accepted.json`; process completion
alone does not establish acceptance. Parallel execution changes neither model
coverage nor these checks. No throughput or per-advice latency gain is claimed
without measurements on the same workload.
