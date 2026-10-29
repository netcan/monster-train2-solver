# Observe a rejected native branch

`FixtureTools extract-unsupported-branch <full-battle.mt2f> <request.mt2f> '<exact reason>'`
reuses recorded forecasts and actual input successors, including the last successor
where the advisor stopped. It can select either a rejected card play or an EndTurn.
The request retains the original root, verified supported prefix, exact failure
root/reason and source SHA256. Extraction does not claim a native successor.

For a rejected alternative seen during retained-suffix investigation, append its
exact reason to `improve-suffix` as the optional final argument. The diagnostic
still reports other rejection reasons, but writes only the first matching branch.

Pass the request to `Run-FullBattleProbe.ps1 -UnsupportedBranchInput` with the
unchanged native input profile, battle distance and physical/canonical capture
options. The development driver verifies the entire prefix before executing the
boundary once, records its actual successor and stops without asserting victory.
Existing card requests without `IsEndTurn` remain compatible.

Validated on the natural covenant-three Ring4 input: extraction found the missing
`Spawned effect modifiers` boundary at turn5 after26 native inputs. A fresh game
matched the26 input prefix and completed the27th EndTurn in87.09 seconds, retaining
the original rejection and observing the Harvester's tower-wide movement. Native
process exit and protected source files were verified; model acceptance of that
successor is a separate requirement. No full regression was needed.
