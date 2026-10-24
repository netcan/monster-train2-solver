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
