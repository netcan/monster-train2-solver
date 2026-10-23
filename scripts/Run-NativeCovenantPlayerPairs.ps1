#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateRange(1, 10)][int[]] $Rank,
    [ValidateSet('Startup', 'Outer', 'Final')][string] $Battle = 'Outer',
    [ValidateSet('AllyBuffer', 'Fragile', 'Corrupter')][string[]] $Variant = @('AllyBuffer'),
    [ValidateRange(1, 2)][int] $NativeWorker = 1,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if ($Battle -ne 'Outer' -and $PSBoundParameters.ContainsKey('Variant')) { throw '-Variant requires -Battle Outer.' }
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Native player-pair checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$buildNative = -not $SkipBuild
foreach ($level in $Rank) {
    $bosses = if ($Battle -eq 'Outer') { $Variant } else { @('') }
    foreach ($boss in $bosses) {
        $common = @{
            StartingCovenantLevel = $level; NativeWorker = $NativeWorker; TimeoutSeconds = 900
            PlayerAdvisor = $true; RollingAdvice = $true; PlannerTacticalSeed = $true
            PlannerHorizon = 3; PlannerTimeLimitMs = 3000; BranchCardMasks = $true
            PhysicalSpawnPoints = $true; SettleDeathDissolves = $true; SettleCardAnimations = $true
        }
        if ($Battle -eq 'Outer') {
            $common.BattleDistance = 7; $common.OriginalOuterBossVariant = $boss
            $common.OuterBossBirth = $true; $common.OuterBossTurns = $true
        } elseif ($Battle -eq 'Final') {
            $common.BattleDistance = 8; $common.OriginalTrueFinalBoss = $true; $common.OuterBossTurns = $true
        }
        $profiles = @{}
        foreach ($mode in @('search', 'replay')) {
            $step = @{ SkipBuild = -not $buildNative }
            if ($mode -eq 'replay') { $step.RollingInput = Join-Path $profiles.search 'full-battle.mt2f' }
            $nativeResult = $null
            & (Join-Path $PSScriptRoot 'Run-FullBattleProbe.ps1') @common @step | ForEach-Object {
                $line = $_.ToString()
                if ($line.StartsWith('{')) { $nativeResult = $line | ConvertFrom-Json } else { Write-Output $line }
            }
            $buildNative = $false
            if ($null -eq $nativeResult) { throw "Missing native result: $Battle/$level/$boss/$mode" }
            $summary = Join-Path $nativeResult.Profile 'native-accepted-summary.json'
            if (-not (Test-Path -LiteralPath $summary -PathType Leaf)) { throw "Missing completed native acceptance: $summary" }
            $accepted = Get-Content -LiteralPath $summary -Raw | ConvertFrom-Json
            $trace = Join-Path $nativeResult.Profile 'full-battle.mt2f'
            if (-not $accepted.NativePassed -or $accepted.ExitCode -ne 0 -or -not $accepted.OriginalFilesUnchanged -or
                $accepted.CaptureFailures -ne 0 -or $accepted.Mismatches -ne 0 -or $accepted.Unsupported -ne 0 -or
                $accepted.Pending -ne 0 -or (Get-FileHash -LiteralPath $trace -Algorithm SHA256).Hash -ine $accepted.TraceSha256) {
                throw "Native acceptance failed or trace changed: $Battle/$level/$boss/$mode"
            }
            $profiles[$mode] = $nativeResult.Profile
            [pscustomobject]@{ Rank = $level; Battle = $Battle; Variant = $boss; Mode = $mode
                Profile = $nativeResult.Profile; NativePassed = $true; NativeSeconds = $accepted.NativeSeconds } | ConvertTo-Json -Compress
        }
        $source = Join-Path $profiles.search 'full-battle.mt2f'
        $replay = Join-Path $profiles.replay 'full-battle.mt2f'
        $arguments = switch ($Battle) {
            'Outer' { @('--player-native-covenant-outer-boss-pair-only', $source, $replay, $boss, "$level") }
            'Final' { @('--player-native-covenant-final-boss-pair-only', $source, $replay, "$level") }
            'Startup' {
                $suffix = if ($level -eq 1) { '' } else { '-' + $level.ToString('D2') }
                @("--player-native-covenant-startup$suffix-pair-only", $source, $replay)
            }
        }
        dotnet $checker @arguments
        if ($LASTEXITCODE -ne 0) { throw "Independent player-pair acceptance failed: $Battle/$level/$boss" }
        $pair = [pscustomobject]@{
            Schema = 1; Battle = $Battle; Rank = $level; Variant = $boss
            SourceProfile = $profiles.search; ReplayProfile = $profiles.replay
            SourceSha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
            ReplaySha256 = (Get-FileHash -LiteralPath $replay -Algorithm SHA256).Hash.ToLowerInvariant()
            IndependentFocusedCheck = $arguments[0]; Passed = $true
        }
        $pair | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $profiles.search 'player-pair-accepted.json') -Encoding utf8
        $pair | ConvertTo-Json -Compress
    }
}
