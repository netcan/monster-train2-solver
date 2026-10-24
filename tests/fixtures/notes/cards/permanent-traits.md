# Native permanent trait upgrades

`permanent-trait-calibration.mt2f` records 176 operations on detached cards made
from the original StarterFireStarter definition. The trait parameters and
removal/clobber settings are explicit controls. These cards never join the
live deck or increase original-card play coverage.

| Matrix | Operations |
| --- | ---: |
| Holdover/Corrupt/Consume/Distinct/Ephemeral x four stack modes x existing/absent trait x clobber protection on/off x two installations | 160 |
| Ephemeral/CopyOnPlay removal x base/temporary owner x removable/nonremovable x clobber protection on/off | 16 |

The model compares each complete native card, including permanent/temporary
trait descriptors, runtime versus authored parameters, private Holdover flags,
modifier payloads and modified-trait names. Native `None` preserves an existing
trait; `ParamInt` increments its runtime parameter by one; `ParamIntLargest`
replaces a smaller removable base with a dummy slot and appends a fresh trait;
`Cumulative` appends an independent trait. Clobber protection suppresses parameter
changes and records the actual additions/removals. A permanent removal does not
erase a separate temporary trait object; nonremovable controls remain active.
Consume controls arm the active private exhausted flag and retain the detached
card's identity before each installation. Parameter changes and cumulative
installation preserve old trait instances; replacement drops the old private
state and initializes the new instance. Parent card snapshots stay immutable.

Source: `.probe-runs/full-battle-no-cards-20261021-185222-eb526dfa`.
The complete live decision, every native/Unity RNG stream and frame are
unchanged. The surrounding no-card battle completes in 45.00 seconds with
settled terminal effects, all four failure counters zero and protected original
files unchanged. The committed calibration has 7,059 bytes/1,346 unique nodes,
SHA256 `2f4d8587fa8c07a22f0d22d90b313cdaa5dcd5a7ef34d167750869941813394c`.

This establishes the captured installation/removal APIs. It does not add
arbitrary trait callback, copying or whole-battle combination evidence. Missing
branch-owned traits/private state remain rejected. All four native installation
stack modes are accepted for these five families. Their play, discard, draw and
copy callbacks retain separate evidence requirements.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --permanent-traits-only tests/fixtures/cards/traits/permanent-trait-calibration.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --typed-draw-pure
pwsh -NoProfile -File scripts/Run-FullBattleProbe.ps1 -Policy no-cards -PermanentTraits -TimeoutSeconds 600 -SkipBuild
```

Focused regression checks retain the prior 100-operation Holdover variant matrix,
native clone refresh, permanent Consume hand/temporary/generation paths, native
Ephemeral copy routing, typed draw and immutable branches. The full fixture
regression was not run.

The Consume pure checks now accept the four calibrated permanent installation
modes and verify base/private trait ownership, including cumulative instances.
The old rejection assertion predated this native matrix and was stale; this
correction changes only verification, with no expansion of production support.
