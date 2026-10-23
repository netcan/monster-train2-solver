#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateRange(1, 10)][int[]] $Rank,
    [ValidateSet('Startup', 'Outer', 'Final', 'Campaign')][string] $Battle = 'Outer',
    [ValidateSet('AllyBuffer', 'Fragile', 'Corrupter')][string[]] $Variant = @('AllyBuffer'),
    [string] $InputProfile = '',
    [ValidateRange(1, 7)][int] $BattleDistance = 1,
    [switch] $AngelDuo,
    [ValidateSet('', 'LoopResolve', 'RevengeHarvest')][string] $AngelDuoVariant = '',
    [ValidateSet('', 'CaelAtkHP', 'CaelDamageShield', 'CaelLifesteal', 'ArkionPyre')][string] $EarlyOuterBossVariant = '',
    [ValidateRange(2, 16)][int] $PlannerHorizon = 3,
    [ValidateRange(100, 30000)][int] $PlannerTimeLimitMs = 3000,
    [ValidateRange(60, 3600)][int] $TimeoutSeconds = 900,
    [ValidateRange(1, 2)][int] $NativeWorker = 1,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if ($AngelDuo -and ($Battle -ne 'Campaign' -or $Rank.Count -ne 1 -or $Rank[0] -ne 2 -or $BattleDistance -ne 5)) {
    throw '-AngelDuo requires the original rank-two Ring6 Campaign input; its checker verifies the exact scenario and unmodified bosses.'
}
if ($AngelDuoVariant -and -not $AngelDuo) { throw '-AngelDuoVariant requires -AngelDuo and its explicit selected-variant checker.' }
if ($EarlyOuterBossVariant -and ($AngelDuo -or $Battle -ne 'Campaign' -or $Rank.Count -ne 1 -or
    $Rank[0] -ne 2 -or $BattleDistance -ne $(if ($EarlyOuterBossVariant.StartsWith('Cael')) { 5 } else { 2 }))) {
    throw 'Early outer-boss player pairs require their original rank-two Ring3/Ring6 checkpoint and dedicated checker.'
}
if ($Battle -ne 'Outer' -and $PSBoundParameters.ContainsKey('Variant')) { throw '-Variant requires -Battle Outer.' }
if ($Battle -eq 'Campaign') {
    if (-not $InputProfile -or $Rank.Count -ne 1) { throw 'Campaign pairs require one rank and an immutable native InputProfile.' }
    $InputProfile = (Resolve-Path -LiteralPath $InputProfile).Path
    $inputSave = Join-Path $InputProfile 'saves/save-singlePlayer.json'
    $save = Get-Content -LiteralPath $inputSave -Raw | ConvertFrom-Json
    if ($save.gameSequence -ne 6 -or $save.wasDevCheatUsed -or $save.startingConditions.ascensionLevel -ne $Rank[0] -or
        $save.startingConditions.seed -ne 424242 -or $save.runState.distance -ne $BattleDistance -or
        $save.completedBattles.Count -ne $BattleDistance -or @($save.completedBattles | Where-Object bossHPRemaining -GT 0).Count) {
        throw 'Campaign pairs require the declared original difficulty, seed and complete prior native victories at BattleIntro.'
    }
    $inputHash = (Get-FileHash -LiteralPath $inputSave -Algorithm SHA256).Hash
} elseif ($InputProfile -or $PSBoundParameters.ContainsKey('BattleDistance')) {
    throw '-InputProfile and -BattleDistance require -Battle Campaign.'
}
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
            StartingCovenantLevel = $level; NativeWorker = $NativeWorker; TimeoutSeconds = $TimeoutSeconds
            PlayerAdvisor = $true; RollingAdvice = $true; PlannerTacticalSeed = $true
            PlannerHorizon = $PlannerHorizon; PlannerTimeLimitMs = $PlannerTimeLimitMs; BranchCardMasks = $true
            PhysicalSpawnPoints = $true; SettleDeathDissolves = $true; SettleCardAnimations = $true
        }
        if ($Battle -eq 'Campaign') {
            $common.Remove('StartingCovenantLevel')
            $common.InputProfile = $InputProfile; $common.BattleDistance = $BattleDistance
            if ($AngelDuoVariant) {
                $common.AngelDuoVariant = $AngelDuoVariant
                $common.OuterBossBirth = $true; $common.OuterBossTurns = $true
            }
            if ($EarlyOuterBossVariant) {
                $common.OriginalEarlyOuterBossVariant = $EarlyOuterBossVariant
                $common.OuterBossBirth = $true; $common.OuterBossTurns = $true
            }
        } elseif ($Battle -eq 'Outer') {
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
            if ($Battle -eq 'Campaign') {
                $loaded = Get-Content -LiteralPath (Join-Path $nativeResult.Profile 'native-checkpoint-input.json') -Raw | ConvertFrom-Json
                if ($loaded.SHA256 -ine $inputHash) { throw 'A campaign process loaded a different native checkpoint.' }
            }
            $profiles[$mode] = $nativeResult.Profile
            [pscustomobject]@{ Rank = $level; Battle = $Battle; Variant = $boss; Mode = $mode
                Profile = $nativeResult.Profile; NativePassed = $true; NativeSeconds = $accepted.NativeSeconds } | ConvertTo-Json -Compress
        }
        $source = Join-Path $profiles.search 'full-battle.mt2f'
        $replay = Join-Path $profiles.replay 'full-battle.mt2f'
        $arguments = switch ($Battle) {
            'Campaign' {
                if ($AngelDuo) {
                    $check = switch ($AngelDuoVariant) {
                        'LoopResolve' { '--player-angel-duo-loop-resolve-pair-only' }
                        'RevengeHarvest' { '--player-angel-duo-revenge-harvest-pair-only' }
                        default { '--player-angel-duo-pair-only' }
                    }
                    @($check, $source, $replay)
                } elseif ($EarlyOuterBossVariant) {
                    if ($EarlyOuterBossVariant -eq 'ArkionPyre') { @('--player-arkion-pyre-pair-only', $source, $replay) }
                    else { @('--player-cael-pair-only', $source, $replay, $EarlyOuterBossVariant.Substring(4)) }
                } else {
                    @('--player-native-covenant-campaign-pair-only', $source, $replay, "$level", "$BattleDistance")
                }
            }
            'Outer' { @('--player-native-covenant-outer-boss-pair-only', $source, $replay, $boss, "$level") }
            'Final' { @('--player-native-covenant-final-boss-pair-only', $source, $replay, "$level") }
            'Startup' {
                $suffix = if ($level -eq 1) { '' } else { '-' + $level.ToString('D2') }
                @("--player-native-covenant-startup$suffix-pair-only", $source, $replay)
            }
        }
        dotnet $checker @arguments
        if ($LASTEXITCODE -ne 0) { throw "Independent player-pair acceptance failed: $Battle/$level/$boss" }
        if ($Battle -eq 'Campaign' -and (Get-FileHash -LiteralPath $inputSave -Algorithm SHA256).Hash -cne $inputHash) {
            throw 'The original native campaign checkpoint changed during player-pair acceptance.'
        }
        $pair = [pscustomobject]@{
            Schema = 1; Battle = $Battle; Rank = $level; Variant = $boss
            SourceProfile = $profiles.search; ReplayProfile = $profiles.replay
            SourceSha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
            ReplaySha256 = (Get-FileHash -LiteralPath $replay -Algorithm SHA256).Hash.ToLowerInvariant()
            IndependentFocusedCheck = $arguments[0]; Passed = $true
        }
        if ($Battle -eq 'Campaign') {
            $pair | Add-Member -NotePropertyName InputProfile -NotePropertyValue $InputProfile
            if ($AngelDuoVariant) { $pair | Add-Member -NotePropertyName AngelDuoVariant -NotePropertyValue $AngelDuoVariant }
            if ($EarlyOuterBossVariant) { $pair | Add-Member -NotePropertyName EarlyOuterBossVariant -NotePropertyValue $EarlyOuterBossVariant }
            $pair | Add-Member -NotePropertyName InputSaveSHA256 -NotePropertyValue $inputHash
            $pair | Add-Member -NotePropertyName BattleDistance -NotePropertyValue $BattleDistance
        }
        $pair | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $profiles.search 'player-pair-accepted.json') -Encoding utf8
        $pair | ConvertTo-Json -Compress
    }
}
