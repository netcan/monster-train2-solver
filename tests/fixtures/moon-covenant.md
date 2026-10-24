# Moon spells with cumulative native Covenant 1-3

The previous moon-hand composition guard rejects the first resource refresh
with original cumulative Covenant owners. Native ranks 1, 2 and 3 now each
retain fifteen independent settled boundaries with the five unchanged moon
spells, original Lunacy, Eclipse room masks, resource refreshes and six floor
selections. Startup uses the original RunSetup callback and native purge/add
pair, retaining the actual twenty-card starting deck. No earned unlock or
probe-authored startup callback is claimed.

Only the ordered original cumulative 01/02/03 prefix is admitted, with the
complete rank-one effect signature, empty selection-only rank-two signature
and exact two merchant effects at rank three. Additional active relics and
later Covenant owners remain separate acceptance targets. Native captured
deck, gold and wave definitions remain authoritative; search does not replay
campaign startup or merchant operations.

All three component gates match complete train states and gameplay RNG,
105 trait snapshots, 40 active changes, both exclusive/additive room modes,
and eight immutable branches at each boundary. Five/six/seven malformed
owner, signature, order, duplication and additional-active-owner controls
reject at ranks 1/2/3. This also exposed a stale enchantment-world snapshot
after global moon changes: live character understanding masks now rebase
that world even when there are no character callbacks to drain.

| Rank | Native profile | Runtime | Archive |
| --- | --- | --- | --- |
| 1 | `full-battle-no-cards-20261024-115627-b97a131e` | 58.21 s | `room-moon-spell-covenant-01-calibration.mt2f` |
| 2 | `full-battle-no-cards-20261024-115747-5cddf29b` | 56.55 s | `room-moon-spell-covenant-02-calibration.mt2f` |
| 3 | `full-battle-no-cards-20261024-115303-c65543bf` | 32.90 s | `room-moon-spell-covenant-03-calibration.mt2f` |

All processes exit normally with protected originals unchanged.

```powershell
scripts/Run-FullBattleProbe.ps1 -RoomMoonSpellAdvance -StartingCovenantLevel 3 -SkipBuild
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --room-moon-spell-upgrades-only tests/fixtures/room-moon-spell-covenant-03-calibration.mt2f
```

`Run-MoonSpellRelicPlayerPair.ps1 -Relic Covenant03 -SkipBuild` selects the
original cumulative rank-three startup with the controlled moon/Lunacy scene.
It checks startup before starting the independent frozen replay and emits a
hashed receipt only after the dedicated `--player-moon-room-covenant03-pair-only`
gate passes. Existing source/replay reuse has the same original-rank check.
The scene uses durable controlled actors and a six-turn/10000-ms tactical
budget; it does not establish default-budget or natural campaign quality.

## Complete native battle

`full-battle-units-spells-and-junk-20261024-115616-e43cbbe2` wins at Pyre80
after 20 plays and five EndTurns in 228.40 seconds. It retains the original
rank-three native startup and all three ordered owners, original five moon
spells/Lunacy/Eclipse definitions and explicit durable controlled actors.
Capture, mismatch, unsupported and pending counters are zero, exit is normal
and protected originals are unchanged. The dedicated original-moon gate
matches every complete native action/turn successor and RNG, all 34 room
stages, nine train phases and sixteen immutable full-battle chains. Original
Lunacy retains its paid 2/3/0 controls. This is separate from actual player
advice and independent replay acceptance.

Archive: `full-battle-original-moon-card-upgrades-covenant03.mt2f`.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --original-moon-card-upgrades-only tests/fixtures/full-battle-original-moon-card-upgrades-covenant03.mt2f
```

## Retained native Heavenfall focus prefix

The first actual-player attempt, profile
`full-battle-units-spells-and-junk-20261024-115903-2055cd64`, deliberately
retains its failed-run counters: nineteen plays, two EndTurns, one model
mismatch, no terminal result. It is not a passing player battle or pair.
The mismatch shows original Heavenfall acquired its Full/Tower upgrade before
its New/Room upgrade. Cast from Eclipse floor 2 at global Full, the first
upgrade breaks 25 Armor on floor 1. The native notification selects floor 1
and refreshes both Gravity cards to the unmasked Full/Ascend upgrade; the next
separate OnCast trigger recaches floor 1's three occupants and deals its
additional 80 damage there. The previous model pins both triggers to the
original cast floor and misses the notification hand refresh.

The corrected model matches all nineteen complete native successors and RNG,
and sixteen immutable branches of the failing action. The focused checker
requires the original incomplete-run counters and never invokes a scripted
whole-battle policy for this actual-player prefix. It also checks the precise
trigger order, remote Armor, final selected floor, final played-room cache and
both Gravity cards. Other upgrade-trigger layouts remain separate boundaries.

Archive: `heavenfall-focus-native-prefix.mt2f`.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --heavenfall-focus-prefix-only tests/fixtures/heavenfall-focus-native-prefix.mt2f
```
