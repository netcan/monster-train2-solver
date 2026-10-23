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
Native player publication and a fresh independent replay are being captured
separately. No full regression or Mac package is involved.
