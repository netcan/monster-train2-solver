# Natural Harvester birth and player enchantment capture

The original covenant-three Ring4 input is `Level4BattleHunterAttackers`,
seed424242, distance3, with three preceding native victories and no developer
cheat. `campaign-native-covenant-03-ring4.zip` restores all12 native payloads;
the input save SHA256 is
`6658e98dc5558fd12f8694f5c405d0ee297a211039a6c8d8e011e89321306301`.

The first default player run stopped after26 inputs at turn5 with no complete
continuation. Two independent guards contributed: `UpbeatWarbler`'s captured
`CardEffectEnchant` had no automatic lifecycle world, and the future Harvester
birth carried `Spawned effect modifiers`. Player aura detection previously
inspected only enemy wave definitions and cached the resulting false value.
It now also discovers player unit definitions, including subsequently discovered
or generated cards, before freezing the native state.

The failed run's supported prefix was extracted without another planner search.
A fresh native component process matched all26 inputs and observed the27th
EndTurn in87.09 seconds. Its unchanged native Harvester birth targets all enemy
units across the tower in physical collection order, with Bump amount-2, then
rearranges itself to the back of the bottom floor. Seven native targets included
four enemies on the top floor. `CardEffectBump` ignores its authored upgrade
`b778dc25-2f26-41ad-8379-26e97aa71837`; the native iterator does not read that
shared editor field. This component retains the historical rejection and does
not claim terminal battle acceptance.

The model admits only this original two-effect Harvester birth signature.
The existing character engine retains trigger tests, silence/count behavior,
marking and effect order; its birth Bump callback uses the complete train,
native room rules and existing tower target/movement implementation. Newly
arrived enemies enter the same room engine before the subsequent self/back
rearrangement. Changed target, movement amount or team payloads remain rejected.

`--harvester-birth-only <native-full-battle.mt2f>` independently checks the
player enchanter's early automatic world, ordered cross-floor enemy targets,
the complete native Bump return state/RNG, immutable source, signature guards,
all native spawn phases and complete EndTurns. The existing paid Bump matrix
(19 native cases plus32 branches) and original isolated enchanter card chain
(seven plays, five EndTurns and16 branches) also pass after this change.

Player acceptance requires a separate actual-default source and frozen replay;
the component observation alone does not establish a victory or campaign finish.

The repaired component is archived as `player-harvester-birth-calibration.mt2f`
and `full-battle-harvester-birth-calibration.mt2f`. Its independent gate passes
all27 native roots, inputs, predictions, successors and RNG;11 spawn phases
and six complete EndTurns match, with all four failure counters zero. The
native process exits normally in128.65 seconds and preserves protected files.
The Bump return retains91 queued child callbacks and dispatches none while the
parent birth trigger is running; the model now preserves that native deferral
instead of draining early. The count and complete settled successor are checked;
this capture does not record an individual snapshot of every queued child.
The self/back rearrangement finishes before those children settle.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --harvester-component-only tests/fixtures/player-harvester-birth-calibration.mt2f tests/fixtures/full-battle-harvester-birth-calibration.mt2f
```

The component driver pauses the actual advisor after its one initial search;
searches after pausing are zero and final background advice is absent. This
also fixes frozen replays without floor browsing, which previously started an
unused search on every input. Detailed natural Bump observations are compiled
only into the development probe. Player acceptance preflight now checks shared
DLL hashes across Model, Probe and Advisor before launching the native process.
