# Original Angel Duo: birth and ordinary no-card battle

The input is the unmodified native cumulative rank-two Ring6 checkpoint in
`campaign-native-covenant-02-ring6.zip`: seed424242, five preceding original
victories, 34 cards and Pyre59. Its history is a separate fixed-policy branch
from the accepted Ring2 player victory; the two branches are not one playthrough.

The exact encounter is `Level6AngelDuo_RallyAttackIncantHealth`. The outer
`Boss_Level6_AngelDuo_Entropic_Variant2_IncantHealth` retains authored attack11,
HP1250 and titanskin10. The native cumulative owners add attack3/HP250. The
inner `Boss_Level6_AngelDuo_Savage_Variant2_SlayAttack` has attack16/HP1350,
multistrike1 and pyrebane1 after those same native adjustments.

Creation already points the outer actor at its companion, but registration
updates the inner actor's reciprocal pair and disables individual boss-death
victory. The original battle relic installs the outer Incant health trigger
only after registration. Non-Covenant character-added notifications and the
shared aura world must retain the newly installed triggers.

The no-card battle exercises ordinary paired movement, enemy spawning, retained
auras, complete turn/room/train state and RNG, including defeat at the Pyre.
The companion retains its pending aura preview actions across UI restoration.
Capture collects each floor separately: native `AddCharactersToList` sorts its
entire destination by local spawn-point index, so a shared cross-floor list
would corrupt the companion roster ordering.

Accepted native source: `full-battle-no-cards-20261023-143545-75219401`,
79.25 seconds, normal exit and protected files unchanged. Final persisted
acceptance has capture/mismatch/unsupported/pending all zero. Its six EndTurns,
51 room stages and 12 train stages match the model. Archives:
`full-battle-native-covenant-02-angel-duo.mt2f` (58102 bytes, 7676 nodes,
SHA256 `c080782e6444052e5fa15110d7c6fae56620cc5ff55033bcdaaa80e818cfb85f`)
and `outer-boss-birth-calibration-angel-duo.mt2f` (33521 bytes, 3579 nodes,
SHA256 `3c6a610be3fd3d2bb25b29f89ef43652d742e1e0d0da2089c83f619b2e853234`).

Focused commands:

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-birth-only tests/fixtures/outer-boss-birth-calibration-angel-duo.mt2f
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-battle-only tests/fixtures/full-battle-native-covenant-02-angel-duo.mt2f
```

These checks include sixteen independent immutable birth and complete battle
branches. A no-card loss does not establish player victory or forced Relentless
after companion death. This boss is not yet enabled in player outer-boss capture.

## Original cross-floor Incant and Slay

`full-battle-angel-duo-incant.mt2f` preserves a separate fixed-policy run from
the same original input. Only legal native spell selection is prioritized;
the deck, units and authored effects remain unchanged. Five original spells
cast on the outer boss's floor heal the remote Savage companion by30 and
increase its maximum by30 each time. The outer boss's maximum is unchanged.
An independent native exchange also witnesses the inner boss's original Slay
granting its remote Entropic companion +3 attack.

Triggered health applies in the target's physical room. Temporary kill previews
track the exact copied actors, including the attacking actor and any paired
boss on another floor, while other actors on that floor remain primary.
Aura updates copy shared effect caches back into borrowed actors before the
room's final synchronization. They preserve those actors' current numeric
state, including real Pyre damage.

Accepted source: `full-battle-units-spells-and-junk-20261023-150213-246ae9df`,
296.42 seconds, native loss, normal exit and protected files unchanged. Final
persisted acceptance has all four failure counters zero. All28 card plays,
seven EndTurns, 64 room stages, 14 train stages and complete state/RNG match.
The focused check also runs sixteen immutable branches per positive cast and
sixteen complete recorded chains. The archive is94013 bytes/15234 nodes,
SHA256 `38f2014f41f21c9277e9455402ca0e8a2abbbe35d5e7cdd0221bce7951fb4ff8`.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --angel-duo-incant-only tests/fixtures/full-battle-angel-duo-incant.mt2f
```

Existing chosen-cleric Tower attack casts, aura-world primitives, the cumulative
rank-two final-boss battle and ordinary unplayed lethal Pyre previews pass their
focused checks. No full catalog regression was run for this batch.
Companion death forcing the native turn/spawn queue, companion Relentless/Pyre
phases and actual player advice with a fresh frozen replay remain separate.
