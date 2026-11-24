# Observe a rejected native branch

For an existing full-battle capture, replay every recorded decision independently
before selecting a branch to observe again.

```powershell
dotnet src/ModelChecks/bin/Release/net8.0/ModelChecks.dll --recorded-decision-diagnose <full-battle.mt2f>
```

The command starts each decision from its native `Before` state and compares the
modeled successor with `Actual`. It groups unsupported decisions and state
differences by reason, reports the earliest affected input for each, and returns
exit code 1 when any decision fails. Use `--recorded-chain-diagnose` on the same
archive to verify continuity once those gaps are resolved. Append `--frontiers`
to check the recorded card and EndTurn candidates in the same run.

`Register-Game231NativeBattle.ps1 -Revalidate` accepts a complete native capture
with an older model verdict after continuous replay and both candidate checks
pass. Registration hashes the original binary and preserves its capture counters.

`FixtureTools extract-unsupported-branch <full-battle.mt2f> <request.mt2f> '<exact reason>'`
reuses recorded forecasts and actual input successors, including the last successor
where the advisor stopped. It can select either a rejected card play or an EndTurn.
The request retains the original root, verified supported prefix, exact failure
root/reason, source SHA256 and game assembly module ID when available. Old
requests without a module ID belong to the archived 2.2.1 game module.

For a rejected alternative seen during retained-suffix investigation, append its
exact reason to `improve-suffix` as the optional final argument. The diagnostic
still reports other rejection reasons, but writes only the first matching branch.

When direct extraction stops on the next preview-copy ID in a settled rolling
archive, freeze the recorded inputs and verify the entire frozen route against the
native archive before requesting the alternate branch. The verifier accounts for
transient preview-copy allocations and checks the recorded actions, settled
states and both RNG streams. Use the tool build that observed the rejection.
The example passes the input list through stdin and writes binary archives.

```powershell
Add-Type -Path (Join-Path (Split-Path $fixtureTools) 'FixtureArchive.dll')
$archive = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($native)
$rolling = $archive.RootElement.GetProperty('RollingDecisions').EnumerateArray()
[object[]]$steps = foreach ($step in $rolling) {
    $relentless = [MonsterTrain2Poju.Fixtures.FixtureValue]::new()
    [ordered]@{
        Action = $step.GetProperty('Action').ToObjectGraph()
        IsEndTurn = $step.GetProperty('IsEndTurn').GetBoolean()
        RelentlessDecisions = if ($step.TryGetProperty('RelentlessDecisions', [ref]$relentless)) {
            ,($relentless.ToObjectGraph())
        }
    }
}
ConvertTo-Json -InputObject $steps -Depth 50 -Compress |
    dotnet $fixtureTools freeze-actions $native - $frozen
if ($LASTEXITCODE -ne 0) { throw 'Could not freeze the recorded inputs.' }
dotnet $fixtureTools verify-offline-native $frozen $native
if ($LASTEXITCODE -ne 0) { throw 'The frozen route differs from the native archive.' }
dotnet $fixtureTools improve-suffix $native $frozen $candidate 5000 1 $decision $reason
if ($LASTEXITCODE -ne 0) { throw 'Could not investigate the rejected branch.' }
```

The request is written to `$candidate.unsupported.mt2f` when the search reaches
the requested rejection. For the 2.3.1 Ring 6 right Zone recording, the pre-fix
tool at `9d97cfa1` verified 54 recorded inputs and extracted the Eclipse request
after 24 inputs with reason `Room moon callback payload requires independent
native calibration.` Its prefix and boundary matched the request already
observed in the original game. The request retained the source archive hash and
game module ID.

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
