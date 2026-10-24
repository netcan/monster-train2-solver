# Advice capture and publication freshness

The player overlay no longer repeats full model captures while its search worker
runs. Card play, EndTurn, room selection, undo and restart still invalidate advice
immediately. A completed worker must pass a new full-state observation before
the panel publishes its result. If the root changed, observation discards the
old candidate and starts work for the current state. The publication also checks
that the observation belongs to the current Unity frame.

Periodic observation of displayed advice remains enabled. Its interval starts
after capture completes, so a slow capture cannot immediately schedule another.
The player evidence field `PublicationRootChecks` counts the mandatory
observations; shared advice checks require it to equal the publication count.
Older archived fixtures retain their original metadata.

## Native acceptance

Source `full-battle-units-spells-and-junk-20261023-165832-541cea69` and fresh replay
`full-battle-units-spells-and-junk-20261023-170328-505b9a84` use original cumulative
rank-two startup, seed 424242, native StartingConditions/deck callbacks, a target
horizon of three turns and a 3000 ms soft search budget. Both processes exit
normally, preserve protected files and persist zero capture failures, mismatches,
unsupported or pending records. Native durations are 293.14/129.77 seconds.

Each completes 31 inputs: 24 card plays and seven EndTurns, with 67 room and
14 train stages. All inputs, full forecasts, state and RNG match in independent
native processes; both win with Pyre 52. The source exercises the actual player
panel, including input invalidation and UI controls. Its 33 publications all
have a fresh observation, including the 11 retained winning continuations.
Frozen replay performs no search and publishes no new player advice.

The earlier reference is `full-battle-player-native-covenant-startup-02-search.mt2f`.
Both sources have exactly the same 31 native inputs and 33 publications, the
same search settings, 11 retained winning continuations and the same Pyre 52 win.

| Observation | Earlier source | Current source |
| --- | ---: | ---: |
| Full captures | 391 | 259 |
| Publications | 33 | 33 |
| Mandatory publication observations | Not recorded | 33 |
| First response, seconds | 4.42 | 4.39 |
| Response median, seconds | 4.42 | 4.59 |
| Response P95, seconds | 5.29 | 5.65 |

Full capture count falls 33.8%. These runs establish reduced capture work and
publication freshness; their measured response times are retained separately.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --player-native-covenant-startup-02-pair-only tests/fixtures/advisor/presentation/full-battle-player-advisor-capture-search.mt2f tests/fixtures/advisor/presentation/full-battle-player-advisor-capture-replay.mt2f
```

To reproduce, run `Run-NativeCovenantPlayerPairs.ps1 -Battle Startup -Rank 2
-PlannerHorizon 3 -PlannerTimeLimitMs 3000 -TimeoutSeconds 1200 -SkipBuild`.
The baseline and new pair remain separate archives.
