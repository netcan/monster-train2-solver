# Complete-state key performance · 2026-10-23

State keys still include every public state property, ordered collection,
card identity, history and RNG stream. The optimization shares only sorted
type layouts and compiled property getters across instances; snapshot hashes
and hash-algorithm instances remain private to each call. Hex encoding avoids
the intermediate dashed string. All key bytes retain their existing format.

The old player DLL was preserved before rebuilding. Under .NET 8 it produces
exactly the same twelve hashes as the optimized implementation, using four
native Ring7 snapshots in strict, unused-preview-allocator and floor-browsing
modes. The focused check retains these old hashes and checks concurrent fresh
key instances. Existing ordinary continuation, floor browsing, resource/RNG
divergence, backwards-input, cancellation and independent native-pair checks
also pass with the new implementation.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --state-key-only tests/fixtures/full-battle-player-natural-ring7-ordinary-search.mt2f
```

Hash values are scoped to one runtime: closed generic type names include BCL
assembly versions. The frozen check is for the checker's .NET 8 target; it
does not compare .NET hashes directly to Unity/Mono hashes. Old and new hashes
were also compared in PowerShell's .NET 10 runtime before recording the .NET 8
reference values.

Initial same-snapshot measurements of twenty fresh key instances were
659.2ms before / 490.5ms after under .NET 10. Under .NET 8, an old-DLL sample
took 781.1ms and optimized samples took 615.4/546.8ms. These are local component
measurements during other work, not a controlled whole-game response claim.
## Native player acceptance

`full-battle-player-state-key-{search,replay}.mt2f` retain fresh source/replay
profiles `230223-14a573ab` / `230559-88c446e9` under the usual
`full-battle-units-spells-and-junk-20261023-` prefix. Both load the optimized
player assembly in Unity/Mono, use actual defaults (three requested turns,
3000ms, extra tactical seed off), and win at Pyre69 after 28 native inputs:
21 plays and seven EndTurns. Native times are 182.20/90.22 seconds.

Every input, complete forecast, successor and RNG matches in independent
processes, including fourteen train phases per process. Both exit normally,
preserve protected originals, record DEPTH-PASS and have zero capture,
mismatch, unsupported and pending counters. The source makes 36 publications,
each guarded by a fresh full root, with 252 pure captures. Seven floor switches,
including one during search, add zero searches. Twelve ordinary continuations
and eleven winning continuations have zero search nodes/time.

Ordinary response median/nearest-rank P95 is 723/1544ms (range 550–1544ms),
including capture and verification. The prior first-battle route had 24 inputs
and Pyre76; these differing routes are not a controlled speed or strategy
comparison. The live result establishes compatibility and successful player
execution of the optimization.

Focused native entry: `--player-state-key-pair-only` followed by the two
registered archives. `Check-DefaultAdvice.ps1 -Case Keys,Native -SkipBuild`
also verifies hashes and the separately archived default-budget Ring7 win.
No full regression or Mac package is involved.
