# Collapsed current-turn advice · Windows 0.2.19

The collapsed panel lists all remaining steps of the current turn, numbered
from one and including the first EndTurn. Later turns remain available through
the existing full-plan button. If the available forecast ends before EndTurn,
the collapsed panel displays its available steps without inventing an EndTurn.
The existing scroll area handles turns whose steps exceed the compact panel.

This changes text formatting only. Expanding/collapsing still preserves the
same advice object and starts no capture or search. Input and continuation
behavior are unchanged. The existing isolated UI acceptance now checks the
last current-turn step, exclusion of later steps, unchanged advice identity,
and unchanged capture/search counts.

## Focused acceptance

Advisor and Probe Release builds pass. The isolated actual 0.2.19 player run
`.probe-runs/full-battle-units-spells-and-junk-20261024-214736-0cbdf9ad` records
`PLAYER-ADVISOR-PLAN-PASS steps=11 currentTurnSteps=5 captures=0 searches=0`.
It checks the final current-turn row, absence of the next numbered row and
future-turn headings, retained advice identity and a scrollable full plan.
The battle then wins after four plays/seven EndTurns with all four native
failure counters zero, original player files unchanged and normal process exit.
This is one targeted native case, not a full model regression.

```powershell
./scripts/Run-FullBattleProbe.ps1 -NativeWorker 2 -Policy units-spells-and-junk `
  -OriginalConduitRelics -PlayerAdvisor -RollingAdvice -PlannerWholeBattle `
  -PlannerSearchWorkers 2 -SkipBuild -TimeoutSeconds 300
```

## Windows delivery

Installed at 2026-10-24 21:49:24 from `a5a45b39` to
`F:/SteamLibrary/steamapps/common/Monster Train 2/BepInEx/plugins/PojuAdvisor`.
All three installed DLL hashes match the accepted build; 28 other loader,
plugin and configuration files remain unchanged. Previous/accepted payloads
and installation.json are saved under
`BepInEx/PojuBackups/CurrentTurnAdvice-20261024-214923`.
The package is `results/mod-dist/PojuAdvisor-0.2.19-Windows-20261024-214923.zip`.
No Mac build or real player input/restart was performed. The running player
PID4972 still has 0.2.18 loaded and needs a user restart to load 0.2.19.

## Collapsed turn-boundary scroll fix

Collapsed verified continuations now stop scroll inertia and reset to the top
when the root turn changes. Same-turn continuation and expanded-plan reading
positions remain preserved. Resetting also applies when the rendered text is
identical, such as consecutive EndTurn-only turns. This adds no capture/search.
Unavailable publications now log their actual reason once per generation.

Isolated player acceptance
`.probe-runs/full-battle-units-spells-and-junk-20261024-222503-d61fa23d`
passes six collapsed native turn transitions, three preserved scrolled
publications, ten reused publications and four live budget changes. It wins
after four plays/seven EndTurns with 53 room stages, all four failure counters
zero, original player files unchanged and normal exit in 89.12 seconds.
The command above adds `-AdvisorPresentation`. The accepted initial scroll
build still identifies as 0.2.20; combined Windows delivery follows in 0.2.21.
