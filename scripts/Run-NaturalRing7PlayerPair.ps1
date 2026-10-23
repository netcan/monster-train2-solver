#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $InputProfile,
    [ValidateSet('Clean', 'Default')][string] $Preset = 'Clean',
    [string] $SourceProfile = '',
    [ValidateRange(1, 4)][int] $NativeWorker = 2,
    [ValidateRange(60, 3600)][int] $TimeoutSeconds = 1800,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$InputProfile = (Resolve-Path -LiteralPath $InputProfile).Path
$savePath = Join-Path $InputProfile 'saves/save-singlePlayer.json'
$inputHash = '812C73153880F32805F9E7A4AC77698023DB97CB7D4BF615350643799407A47C'
if ((Get-FileHash -LiteralPath $savePath -Algorithm SHA256).Hash -cne $inputHash) {
    throw 'Natural Ring7 pairs require the immutable seed424244 six-win checkpoint.'
}
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Ring7 player-pair checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$common = @{
    Policy = 'units-spells-and-junk'; InputProfile = $InputProfile; BattleDistance = 6
    PlayerAdvisor = $true; RollingAdvice = $true; PlannerTacticalSeed = $Preset -eq 'Clean'
    PlannerHorizon = $(if ($Preset -eq 'Clean') { 12 } else { 3 })
    PlannerTimeLimitMs = $(if ($Preset -eq 'Clean') { 30000 } else { 3000 })
    BranchCardMasks = $true; PhysicalSpawnPoints = $true
    SettleDeathDissolves = $true; SettleCardAnimations = $true
    NativeWorker = $NativeWorker; TimeoutSeconds = $TimeoutSeconds
}
$profiles = @{}
$buildNative = -not $SkipBuild
foreach ($mode in @('search', 'replay')) {
    if ($mode -eq 'search' -and $SourceProfile) {
        $profile = (Resolve-Path -LiteralPath $SourceProfile).Path
    } else {
        $step = @{ SkipBuild = -not $buildNative }
        if ($mode -eq 'replay') { $step.RollingInput = Join-Path $profiles.search 'full-battle.mt2f' }
        $native = $null
        & (Join-Path $PSScriptRoot 'Run-FullBattleProbe.ps1') @common @step | ForEach-Object {
            $line = $_.ToString()
            if ($line.StartsWith('{')) { $native = $line | ConvertFrom-Json } else { Write-Output $line }
        }
        $buildNative = $false
        if ($null -eq $native) { throw "Missing Ring7 native result: $mode" }
        $profile = $native.Profile
    }
    $accepted = Get-Content -LiteralPath (Join-Path $profile 'native-accepted-summary.json') -Raw | ConvertFrom-Json
    $trace = Join-Path $profile 'full-battle.mt2f'
    $loaded = Get-Content -LiteralPath (Join-Path $profile 'native-checkpoint-input.json') -Raw | ConvertFrom-Json
    if (-not $accepted.NativePassed -or -not $accepted.OriginalFilesUnchanged -or $accepted.ExitCode -ne 0 -or
        $accepted.CaptureFailures -ne 0 -or $accepted.Mismatches -ne 0 -or $accepted.Unsupported -ne 0 -or $accepted.Pending -ne 0 -or
        (Get-FileHash -LiteralPath $trace -Algorithm SHA256).Hash -ine $accepted.TraceSha256 -or $loaded.SHA256 -ine $inputHash) {
        throw "Ring7 native acceptance failed or the trace/input changed: $mode"
    }
    $profiles[$mode] = $profile
    [pscustomobject]@{ Preset = $Preset; Mode = $mode; Profile = $profile; NativePassed = $true
        NativeSeconds = $accepted.NativeSeconds } | ConvertTo-Json -Compress
}
$source = Join-Path $profiles.search 'full-battle.mt2f'
$replay = Join-Path $profiles.replay 'full-battle.mt2f'
$mode = "--player-natural-ring7-$($Preset.ToLowerInvariant())-pair-only"
dotnet $checker $mode $source $replay
if ($LASTEXITCODE -ne 0) { throw 'Independent Ring7 player-pair acceptance failed.' }
if ((Get-FileHash -LiteralPath $savePath -Algorithm SHA256).Hash -cne $inputHash) { throw 'Original Ring7 checkpoint changed.' }
$pair = [pscustomobject]@{
    Schema = 1; Preset = $Preset; InputProfile = $InputProfile; InputSaveSHA256 = $inputHash
    SourceProfile = $profiles.search; ReplayProfile = $profiles.replay
    SourceSha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
    ReplaySha256 = (Get-FileHash -LiteralPath $replay -Algorithm SHA256).Hash.ToLowerInvariant()
    IndependentFocusedCheck = $mode; Passed = $true
}
$pair | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $profiles.search 'player-pair-accepted.json') -Encoding utf8
$pair | ConvertTo-Json -Compress
