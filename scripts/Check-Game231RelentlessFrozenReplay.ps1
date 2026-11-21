#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$build = Join-Path $workspace 'results/relentless-frozen-replay-check'
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/relentless-frozen-replay'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if (($manifest.path -join ',') -cne 'abandon.mt2f,continue.mt2f') {
    throw 'Unexpected Relentless frozen replay manifest.'
}

if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --artifacts-path $build -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'Isolated ModelChecks build failed.' }
}
$bin = Join-Path $build 'bin/ModelChecks/release'
Add-Type -Path (Join-Path $bin 'FixtureArchive.dll')

function Get-RelentlessChoices($Row) {
    $choices = [Activator]::CreateInstance([MonsterTrain2Poju.Fixtures.FixtureValue])
    if ($Row.TryGetProperty('RelentlessDecisions', [ref] $choices) -and $choices.ValueKind -ne 'Null') {
        foreach ($choice in $choices.EnumerateArray()) {
            '{0}/{1}/{2}' -f $choice.GetProperty('RoomIndex').GetInt32(),
                $choice.GetProperty('CompletedRounds').GetInt32(), $choice.GetProperty('Continue').GetBoolean()
        }
    }
}

foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) {
        throw "Relentless frozen replay hash or size differs for $($entry.path)."
    }

    $capture = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($path)
    try {
        $root = $capture.RootElement
        $rolling = @($root.GetProperty('RollingDecisions').EnumerateArray())
        $actions = @($root.GetProperty('Actions').EnumerateArray())
        $turns = @($root.GetProperty('Turns').EnumerateArray())
        if ($rolling.Count -ne 8 -or $actions.Count -ne 1 -or $turns.Count -ne 7) {
            throw "Incomplete frozen battle in $($entry.path)."
        }
        $native = $actions + $turns
        $dialogs = $root.GetProperty('RelentlessBattle').GetProperty('Dialogs').GetArrayLength()
        $expectedDialogs = if ($entry.path -ceq 'abandon.mt2f') { 1 } else { 10 }
        if ($dialogs -ne $expectedDialogs) { throw "Unexpected native dialog count in $($entry.path)." }

        for ($index = 0; $index -lt $rolling.Count; $index++) {
            $row = $rolling[$index]
            $observed = $native[$index]
            $plan = $row.GetProperty('PlanSteps')
            $endTurn = $index -gt 0
            if ($row.GetProperty('Mode').GetString() -cne 'replay' -or $row.GetProperty('Nodes').GetInt32() -ne 0 -or
                $row.GetProperty('Difference').ValueKind -ne 'Null' -or $row.GetProperty('IsEndTurn').GetBoolean() -ne $endTurn -or
                $plan.GetArrayLength() -ne 1 -or $plan[0].GetProperty('IsEndTurn').GetBoolean() -ne $endTurn -or
                -not $plan[0].GetProperty('Action').ContentEquals($row.GetProperty('Action')) -or
                -not $row.GetProperty('Root').ContentEquals($observed.GetProperty('Before')) -or
                -not $row.GetProperty('Actual').ContentEquals($observed.GetProperty('Actual')) -or
                -not $row.GetProperty('Expected').ContentEquals($row.GetProperty('Planned')) -or
                -not $row.GetProperty('Outcome').ContentEquals($row.GetProperty('PlannedOutcome')) -or
                -not $row.GetProperty('Outcome').ContentEquals($observed.GetProperty('ActualOutcome'))) {
                throw "Frozen decision $index differs from the captured input, forecast or successor in $($entry.path)."
            }

            if (-not $endTurn -and -not $row.GetProperty('Action').ContentEquals($observed.GetProperty('Action'))) {
                throw "Frozen deployment differs from the native input in $($entry.path)."
            }
            $choices = (Get-RelentlessChoices $row) -join ','
            $planned = (Get-RelentlessChoices $plan[0]) -join ','
            $executed = (Get-RelentlessChoices $observed) -join ','
            if ($choices -cne $planned -or $choices -cne $executed) {
                throw "Frozen Relentless choices differ from native inputs at decision $index in $($entry.path)."
            }
            $expected = $row.GetProperty('Expected')
            $actual = $row.GetProperty('Actual')
            if (-not $expected.GetProperty('RngStreams').ContentEquals($actual.GetProperty('RngStreams')) -or
                -not $expected.GetProperty('Spawn').GetProperty('Rng').ContentEquals($actual.GetProperty('Spawn').GetProperty('Rng'))) {
                throw "Frozen RNG differs after decision $index in $($entry.path)."
            }
        }
    }
    finally { $capture.Dispose() }
    $calibration = 'relentless-battle-' + [IO.Path]::GetFileNameWithoutExtension($entry.path)
    dotnet (Join-Path $bin 'ModelChecks.dll') --rolling-advice-only $path $calibration
    if ($LASTEXITCODE) { throw "Rolling forecast replay failed for $($entry.path)." }
}

dotnet (Join-Path $bin 'ModelChecks.dll') --relentless-battle-pair-only (Join-Path $directory 'abandon.mt2f') (Join-Path $directory 'continue.mt2f')
if ($LASTEXITCODE) { throw 'Native Relentless frozen replay check failed.' }
Write-Output 'GAME-231-RELENTLESS-FROZEN-REPLAY PASS two eight-input replays, 1 Abandon and 10 Continue dialogs, complete native successors and RNG.'
