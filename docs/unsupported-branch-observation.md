# Observe a rejected native branch

`FixtureTools extract-unsupported-branch <full-battle.mt2f> <request.mt2f> '<exact reason>'`
reuses recorded forecasts and actual input successors, including the last successor
where the advisor stopped. It can select either a rejected card play or an EndTurn.
The request retains the original root, verified supported prefix, exact failure
root/reason, source SHA256 and game assembly module ID when available. Old
requests without a module ID belong to the archived 2.2.1 game module.

For a rejected alternative seen during retained-suffix investigation, append its
exact reason to `improve-suffix` as the optional final argument. The diagnostic
still reports other rejection reasons, but writes only the first matching branch.

Pass the request to `Run-FullBattleProbe.ps1 -UnsupportedBranchInput` with the
unchanged native input profile, battle distance and physical/canonical capture
options. The development driver verifies the entire prefix before executing the
boundary once and records its actual successor. The launcher checks the worker's
game assembly module against the request and verifies the output module and
embedded request after exit.
Existing card requests without `IsEndTurn` remain compatible.

For a settled nonterminal boundary, run `ModelChecks --native-branch-replay-only
<native-observation.mt2f> <expected-MVID>` to check the completed archive and
replay its actions against the current model.

Validated on the natural covenant-three Ring4 input: extraction found the missing
`Spawned effect modifiers` boundary at turn5 after26 native inputs. A fresh game
matched the26 input prefix and completed the27th EndTurn in87.09 seconds, retaining
the original rejection and observing the Harvester's tower-wide movement. Native
process exit and protected source files were verified; model acceptance of that
successor is a separate requirement. No full regression was needed.

The 2.3.1 Judge’s Scales Ring 8 run exposed a legal third-floor Eager Conscript
play that the model rejected. The native probe replayed 23 preparatory inputs
from the original checkpoint and then accepted that play. The new Fun Guy went
from 4 to 7 HP and entered its troop aura’s target cache. The existing enemy
enchanters remained active, and the preview copy left the primary target cache
unchanged. The recorded successor and RNG match the corrected model. The
registered component is
`tests/compatibility/game-2.3.1/mechanics/troop-aura-enchantment/native-branch.mt2f`.

The same checkpoint exposed another legal play when Starter Fire Starter
killed a Titanskin source while a three-Horde Fun Guy entered the troop aura.
The native branch recorded 17 preparatory inputs and showed the pending source
retirement and three temporary health additions, from 12 to 21 HP. All 18
inputs, the settled successor and the RNG streams match the corrected model.
The registered component is `pending-source-death.mt2f` in the same directory.
