# Player Mod validation · 2026-10-21

The player entry point is `src/Advisor/AdvisorPlugin.cs`. The 0.2.0 baseline below
is followed by the 0.2.1 focused results.
It compiles against Mono game 2.2.1, module
`8fb07b96-f4db-4d2b-884d-c00536d6ccf4`, as an AnyCPU netstandard2.1 assembly.
The assembly excludes `ProbePlugin` and `NativeReplayScenario`. Startup selects
eight observation/invalidation patches explicitly, including spawn-point copy
identity tracking. It does not install every patch in its assembly.

## Focused results

- Both Advisor and the existing Probe Release builds succeed. The ordinary
  unsuppressed first build retains existing nullable warnings in reused code.
- `--battle-advice-only full-battle-planner-starter-search.mt2f` passes stale
  result rejection, latest-request coalescing, single-worker cancellation,
  replay-verified budget fallback, failures and nonblocking disposal.
- Both macOS `.command` files pass Bash syntax checking. Package validation
  checks exact payload membership, all internal SHA256 values, LF scripts,
  AnyCPU IL, absence of development entry points and a clean source revision.
- The unmodified official macOS BepInEx 5.4.23.5 ZIP matches GitHub's release
  asset digest `01c2ae782eb016dfd6c345a18dbd2dcafffb3d9d318449d6486689f426b4a323`.

## Windows native smoke

Temporary player DLLs were loaded alongside the isolated development driver in
the copied Windows game. The original save/log signatures remain unchanged.
The developer driver supplies game inputs; the player Mod only observes and
publishes recommendations. These inputs do not establish a separate native
replay of the player Mod's recommendations.

The first run omitted `-Planner`; the fast driver completed before advice was
published, so its advisor acceptance gate failed. This is not counted as a
successful advisor test. The corrected focused run used:

```powershell
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 `
  -Policy units-spells-and-junk -Planner -Advisor `
  -PlannerHorizon 3 -PlannerTimeLimitMs 3000 -BinaryCapture -SkipBuild
```

Source profile: `.probe-runs/full-battle-units-spells-and-junk-20261021-190926-e274fc76`.
The existing gate passes. The battle finishes in 80.70 seconds, with a native
win, 21 card actions, seven EndTurns, settled terminal effects and zero capture,
mismatch, unsupported and pending counters. The player Mod logs `POJU-READY`
and two `POJU-ADVICE` publications (12/11 steps, 3064.4/3162.5 ms), both with
`capturePurityFailures=0`. No player capture/overlay error is present.
The native panel render assertion passes for the development overlay; it is
not a separate render assertion for the player's canvas.

No full fixture regression was run for packaging. macOS loader startup,
Apple Silicon runtime compatibility, font rendering and actual Mac battles
remain unverified. `docs/macOS.md` and the package's `BUILD.json` preserve that
boundary; the installation preflight must not be reported as runtime acceptance.

## Version 0.2.1

The panel now has a persistent mouse toggle, a draggable title constrained to
the canvas, and configurable `PauseWhenHidden` (default true). Hiding cancels
the active advice request; showing invalidates its old snapshot before capture
resumes. Dynamic fonts are reused across canvas recreation and released on
disposal; player timing history retains only the latest 128 publications.

Both Release builds and the focused advice-session check pass. One development
advisor native run validates the shared updated overlay and existing planner:
`.probe-runs/full-battle-units-spells-and-junk-20261021-192259-9c8d2b52`.
It finishes in 76.80 seconds with a native win, 21 actions, seven EndTurns,
settled terminal effects, protected files unchanged and four zero counters.
Two advice publications have 12/11 steps and zero capture-purity failures.
The rendered panel has 352 text vertices and 85,699 nonbackground pixels;
visual inspection confirms the new mouse toggle and the recommendation text.
Mouse clicking/dragging and Mac font/loader behavior still require interactive
validation; this run does not assert those interactions.

`tests/packaging/check-macos-install.sh` runs against temporary file fixtures
under Git Bash, without a game or macOS runtime. It verifies first install,
upgrade preservation of config/extra files, Unicode/space paths, rollback after
an injected launcher replacement failure, temporary-directory cleanup, and
rejection of incomplete BepInEx, BepInEx 6 and renamed development probe DLLs.
All three product shell files pass Bash syntax checks. This verifies portable
file operations and failure handling, not actual macOS loader compatibility.

The installer performs existing-runtime checks even with `--check-only`, stages
replacement DLLs on the game's volume and retains the previous complete plugin
folder/launcher. Build version is read from `Advisor.csproj`; the package now
contains the shared `poju-common.sh` helper in its checksum coverage. No full
fixture regression was run for this increment.
