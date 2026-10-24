# Arkion Pyre deployment advice · Windows 0.2.21

The real player PID74796 loaded 0.2.19. A read-only settled deployment capture
at 2026-10-24 22:20:55 found an unavailable session after only 87.55ms of its
10-second budget. The failure was `Outer or queued final boss spawning`, not
an exhausted search budget. The original encounter is
`Level3BattleHeavyHitter_PyreDamage`, cumulative Covenant10, with the original
`Enemy_Boss_Level3Rage_TitanTrial` relic enabled.

The birth owner gate admitted this original boss only with Covenant01/02.
It now uses the existing exact ordinary Covenant identity/effect/origin gate
for this boss at higher ranks. Changed signatures, duplicate owners and
unknown boss definitions remain rejected.

The trial has a separate native `OnSpawn` callback: an unchanged enemy of
the BigBossT1 subtype receives `Boss_Level3_Rage_AtkUpgrade`, adding five
attack with an applied upgrade record. Capture now retains this exact rule;
the existing direct unit-upgrade trigger engine applies it once, with native
clone checks and spawner propagation. The Arkion birth stat query sums the
native Covenant preview queries only. It excludes the later non-Covenant
trial upgrade, preserving both actual attack and internal modifier caches.

The immutable original backup was copied to
`.probe-runs/live-arkion-covenant10-20261024`, SHA256
`44147c00e08923ac4898e0a3d5afdf8cf9e1276fe72e217451ade0b21f4d96f7`.
This is a copy of the native BattleIntro checkpoint, without save/route/asset
edits. Diagnostics never played a card, ended a turn or restarted the real game.

Focused model checks restore only the omitted boss/trial definitions from the
independent process into the original live capture. No native successor,
hand, board or RNG is injected. The 10-second two-worker search reaches a
45-step, nine-turn modeled victory and passes independent full-state/RNG
replay. Missing definitions, changed birth origins, changed trial attack,
changed merchant effects, duplicate owners and unknown bosses are rejected.
The existing rank-two native birth checker also passes.

The player UI acceptance now accepts a correctly labelled partial forecast
when expanding it. Previously that assertion required the complete-forecast
label even when all available steps were displayed.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --live-arkion-pyre-only `
  tests/fixtures/player-live-arkion-pyre-unavailable.mt2f <accepted-native.mt2f>
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Policy units-spells-and-junk `
  -InputProfile .probe-runs/live-arkion-covenant10-20261024 -BattleDistance 2 `
  -OuterBossBirth -OuterBossTurns -PhysicalSpawnPoints -PlayerAdvisor `
  -RollingAdvice -PlannerWholeBattle -PlannerSearchWorkers 2 `
  -PlannerTimeLimitMs 10000 -AdvisorPresentation -SkipBuild -TimeoutSeconds 600
```
