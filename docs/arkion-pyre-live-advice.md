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
hand, board or RNG is injected. After the lethal-target correction, the
10-second two-worker check supplies 40 verified steps across eight turns,
correctly labelled as a partial forecast, with independent full-state/RNG
replay. It is not evidence of a proven victory or optimum. Missing definitions,
changed birth origins, changed trial attack,
changed merchant effects, duplicate owners and unknown bosses are rejected.
The existing rank-two native birth checker also passes.

The player UI acceptance now accepts a correctly labelled partial forecast
when expanding it. Previously that assertion required the complete-forecast
label even when all available steps were displayed.

The original Pyre Heart also carries permanent same-ID growth records from
earlier battles. Its scaled upgrade must update the fresh record shared with
the current spawned unit, preserving older card records. The original relic's
spawn appends this record, so its scaling now selects the last matching card
entry. Other upgrade IDs retain their existing matching behavior.
The recorded diagnostic prefix
`full-battle-units-spells-and-junk-20261024-224211-2da0b60e` now replays all
20 actual inputs exactly, including the turn-four boundary that exposed this
issue. Its old forecasts/counters remain diagnostic, not accepted native
plan evidence. Its separate original boss instantiation/birth check passes.

The next diagnostic run (`20261024-224900-db5a16eb`) exposed a second-hit
targeting error: killing Spearman ID19 compacted the model's unit list, so
the second piercing hit incorrectly damaged Arkion by 18. Native drop-target
selection reads the same spawn point while that death is still queued; it
does not substitute the next enemy. Drop-target collection now uses the
existing retained positions during the effect sequence. All 23 actual
inputs of this diagnostic replay exactly, including that lethal first hit.
The archive is retained as observed-input evidence only; its old forecasts
and acceptance counters are deliberately not accepted.

The isolated presentation observer also now compares a cleared forecast
against the displayed publication's search counter. A new search after an
exhausted partial forecast can start asynchronously between UI ticks, so
comparing only the current frame's search counter produced a false alarm.

## Final native acceptance

The unchanged original checkpoint run
`full-battle-units-spells-and-junk-20261024-225650-11a47d14` passes with the
production 0.2.21 DLLs built from `b3fa480d`: 36 plays (including three unit
ability activations), nine EndTurns, 70 room stages and a native victory with
22 Pyre health. All 45 actual inputs follow published rolling advice.
Capture failures, mismatches, unsupported and pending counters are zero;
terminal effects settle, original player files remain unchanged and the
isolated game exits normally in 450.50 seconds.

The actual player panel passes 47 publications, 42 reused publications,
25 preserved scrolled publications, six collapsed turn resets, 237 retained
input callbacks, 4599 pending frames, two explicit resets and four live budget
changes. Idle and plan expansion checks add no captures or searches.

Curated fixtures retain the original unavailable live capture, this complete
native run, its exact boss instantiation/birth boundary and the separate
23-input diagnostic prefix. The last archive verifies observed successors
only. Focused original rearrangement and spell-composition immutable replay
checks pass; no full fixture regression was run for this change.
The final curated live-root check with the accepted native definitions also
finds a complete 45-step, nine-turn modeled victory in 10082.2ms and passes
independent full-state/RNG replay. Search scheduling can still return the
correctly labelled verified partial forecast described above.

## Windows delivery

Installed at 2026-10-24 23:05:38 to
`F:/SteamLibrary/steamapps/common/Monster Train 2/BepInEx/plugins/PojuAdvisor`.
All three DLL hashes match the accepted production build from `b3fa480d`;
28 other loader/plugin/configuration files remain unchanged. Previous and
accepted payloads plus the installation receipt are under
`BepInEx/PojuBackups/AdviceTurnFix-20261024-230537`.
The package is
`results/mod-dist/PojuAdvisor-0.2.21-Windows-20261024-230537.zip`.
The real player PID74796 remains running with 0.2.19 loaded and requires a
user restart to load 0.2.21. No real player inputs/restart or Mac build occurred.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --live-arkion-pyre-only `
  tests/fixtures/player-live-arkion-pyre-unavailable.mt2f <accepted-native.mt2f>
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Policy units-spells-and-junk `
  -InputProfile .probe-runs/live-arkion-covenant10-20261024 -BattleDistance 2 `
  -OuterBossBirth -OuterBossTurns -PhysicalSpawnPoints -PlayerAdvisor `
  -RollingAdvice -PlannerWholeBattle -PlannerSearchWorkers 2 `
  -PlannerTimeLimitMs 10000 -AdvisorPresentation -SkipBuild -TimeoutSeconds 600
```
