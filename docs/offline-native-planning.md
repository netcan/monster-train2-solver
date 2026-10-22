# Reuse a native deployment for search experiments

Player rolling probes now write `player-deployment.mt2f` once the real player
capture is initialized, before waiting for its first accepted publication.
The artifact retains that player's complete deployment state and RNG, including
its card/unit identity space. It requires one extra pure read and serialization;
it does not add native listeners or change game state. This exporter is compiled
only into the development probe.

The artifact is explicitly a `NativeDeploymentObservation`. It has no invented
terminal-settled, native-victory or failure-counter fields. Offline planning
checks deployment turn0, the actual player assembly, positive capture count,
zero purity/advisor failures and an original encounter. Ordinary complete
native archives still require all four zero counters and settled terminal
effects. Neither deployment observations nor model predictions are accepted by
the campaign continuation runner as a native victory.

After one native launch, compare candidate policies without repeating the
game or UI acceptance sequence:

```powershell
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll policy-battle <profile>/player-deployment.mt2f <floor0.mt2f> 0
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll policy-battle <profile>/player-deployment.mt2f <floor1.mt2f> 1
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll policy-battle <profile>/player-deployment.mt2f <floor2.mt2f> 2
dotnet src/FixtureTools/bin/Release/net8.0/FixtureTools.dll plan-battle <profile>/player-deployment.mt2f <searched.mt2f> 12 3000 1 --tactical
```

Each command uses a new output path. `OFFLINE-VERIFY PASS` establishes complete
model successors, terminal outcome and state/RNG consistency. Outputs remain
`ModelPrediction` artifacts: only a fresh original game process can prove native
acceptance. Preserve `-PlayerAdvisor` when replaying predictions from this
identity space. A native winning replay may then feed campaign continuation.

The seed42 Corrupter deployment in
`full-battle-units-spells-and-junk-20261023-055405-e160c30a` provided the first
runtime comparison. One observation produced three independently checked
policies: bottom loses in 71 inputs, middle wins in 67 with Pyre84, and top wins
in 74 with Pyre69. These were predictions before movement calibration: the
native top-deployment run stopped at input17 on a relic movement discrepancy,
so those predicted victories do not establish native acceptance. Its partial
archive supplies actual successors for the focused movement regression. The
older complete Ring7 native archive also passes the same
policy/freeze workflow, verifying input-format compatibility. These checks did
not rerun unrelated battles or the full regression.

The failed run exposed two separate timing errors. The casting outer boss's
action movement applies room-change relics but suppresses its character
ascend/descend trigger; ordinary Bump still fires that trigger. Otherwise the
floor-transition relic incorrectly deals 3 damage plus the boss's 10 Pyregel.
Ascension also leaves the vacancies of relic kills until the next
`RunMonsterTurn`, which centers every room before granting energy.

`ModelChecks --observed-successors-only <archive>` checks each captured actual
input and all train phases without accepting the archive's old predictions or
failure counters. After these fixes, the failed Corrupter archive matches all
17 observed successors and four train phases. The already accepted Ring7 and
Fragile sources also match their 50 and 54 successors; four existing Bump
archives retain their original damage, Pyregel and paired Boss checks.

The middle-deployment native run `20261023-061531-9847a7c1` then exposed a
separate triggered-status omission at input23: Firebrand's visible OnHit adds
2 Valor, which its Duality doubles to 4. Triggered status application now uses
the source character's Duality, preserves hidden-trigger and IgnoreDualism
exemptions, excludes the Corrupter multiplier's null character source, and
requires captured display categories. The partial archive matches all 23
actual inputs and six train phases after this correction. Its failed original
plan remains a diagnostic, not a native victory.
