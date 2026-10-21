# Native permanent trait upgrades

`permanent-trait-calibration.mt2f` records 80 operations on detached cards made
from the original StarterFireStarter definition. The trait parameters and
removal/clobber settings are explicit controls. These cards never join the
live deck or increase original-card play coverage.

| Matrix | Operations |
| --- | ---: |
| Holdover/Corrupt x four stack modes x existing/absent trait x clobber protection on/off x two installations | 64 |
| Ephemeral/CopyOnPlay removal x base/temporary owner x removable/nonremovable x clobber protection on/off | 16 |

The model compares each complete native card, including permanent/temporary
trait descriptors, runtime versus authored parameters, private Holdover flags,
modifier payloads and modified-trait names. Native `None` preserves an existing
trait; `ParamInt` increments its runtime parameter by one; `ParamIntLargest`
replaces a smaller removable base with a dummy slot and appends a fresh trait;
`Cumulative` appends an independent trait. Clobber protection suppresses parameter
changes and records the actual additions/removals. A permanent removal does not
erase a separate temporary trait object; nonremovable controls remain active.
Parent card snapshots stay immutable.

Source: `.probe-runs/full-battle-no-cards-20261021-184429-f70660a3`.
The complete live decision, every native/Unity RNG stream and frame are
unchanged. The surrounding no-card battle completes in 45.95 seconds with
settled terminal effects, all four failure counters zero and protected original
files unchanged. The committed calibration has 4,832 bytes/770 unique nodes,
SHA256 `7b2682feab52bdd0eaaf6861792605cb939ba3332a5f65ecb358bbf4387cf7cb`.

This establishes the captured installation/removal APIs. It does not add
arbitrary trait callback, copying or whole-battle combination evidence. Missing
branch-owned traits/private state remain rejected. Nondefault permanent Consume
stack modes and Distinct/Ephemeral installation modes retain their existing
guards; their defaults and existing routing are separately calibrated.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --permanent-traits-only tests/fixtures/permanent-trait-calibration.mt2f
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy no-cards -PermanentTraits -TimeoutSeconds 600 -SkipBuild
```

Focused regression checks retain the prior 100-operation Holdover variant matrix,
native clone refresh, permanent Consume hand/temporary/generation paths, native
Ephemeral copy routing, typed draw and immutable branches. The full fixture
regression was not run.
