# Start-turn card-type sequence reset

The first battle of the native new run retaining the completed profile's metagame
exposed a lost context update. `GainBonusOnThreeCardsPlayed`
(`a67438aa-e1d7-45d6-bf62-fceddd2a723f`) clears its private played-type list
at StartTurn. After resetting it, the model resumed `train.Context` during boss
handling and discarded the reset when no companion boss action wrote it back.
The same path also contains the first-card cost reset.

Keep the updated context on the train before boss handling. The original and
fixed independent native observations contain the same nine inputs: the first
eight match; the final EndTurn moves turn 1 to 2 and clears Monster/Spell. The
original expected state still contains those two entries. The checker permits
only this exact historical difference and compares every other state field and
RNG, matching the two processes and checking immutable parents. Neither fixture
claims a complete battle win. The original archive is preserved unchanged.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll `
  --card-type-sequence-reset-pair-only `
  tests/fixtures/relics/modifiers/player-card-type-sequence-reset-calibration.mt2f `
  tests/fixtures/relics/modifiers/player-card-type-sequence-reset-replay-calibration.mt2f
```

Existing native sequence rewards and first-card cost fixtures, plus their pure
checks, pass. Full regression was not repeated. The component harness can use
`-NativeBuildRoot <isolated checkout> -SkipBuild` only with UnsupportedBranchInput;
profiles, worker locking, sandbox guards and protected real files remain rooted
in the main workspace. This prevents a component rebuild from changing payloads
in another running battle/replay pair.
