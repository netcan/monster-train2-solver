#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Cost', 'Attack', 'Healing', 'PhaseHeal', 'PhaseEnergy', 'HandPower', 'Status', 'Covenant03')]
    [string] $Relic,
    [string] $SourceProfile = '',
    [string] $ReplayProfile = '',
    [ValidateRange(1, 2)][int] $NativeWorker = 1,
    [ValidateRange(60, 3600)][int] $TimeoutSeconds = 1200,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$profilesRoot = [IO.Path]::GetFullPath((Join-Path $workspace '.probe-runs')).TrimEnd('\')
if ($ReplayProfile -and -not $SourceProfile) { throw 'ReplayProfile requires its original SourceProfile.' }
$reusedSource = [bool]$SourceProfile
$reusedReplay = [bool]$ReplayProfile
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Moon relic player checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
$flag = if ($Relic -eq 'Covenant03') { 'OriginalMoonCardUpgradesLunacy' } else { 'OriginalMoonCardUpgrades' + $Relic + 'Relic' }
$gate = switch ($Relic) {
    'Healing' { '--player-moon-room-healing-relic-pair-only' }
    'PhaseHeal' { '--player-moon-room-phase-heal-relic-pair-only' }
    'PhaseEnergy' { '--player-moon-room-phase-energy-relic-pair-only' }
    'HandPower' { '--player-moon-room-hand-power-relic-pair-only' }
    'Status' { '--player-moon-room-status-relic-pair-only' }
    'Covenant03' { '--player-moon-room-covenant03-pair-only' }
    default { '--player-moon-room-spells-pair-only' }
}
function Read-AcceptedProfile([string] $Profile, [bool] $Frozen) {
    $resolved = (Resolve-Path -LiteralPath $Profile).Path.TrimEnd('\')
    if ((Split-Path $resolved -Parent) -ine $profilesRoot) { throw 'Native profiles must be direct children of .probe-runs.' }
    $summary = Get-Content -LiteralPath (Join-Path $resolved 'native-accepted-summary.json') -Raw | ConvertFrom-Json
    $trace = Join-Path $resolved 'full-battle.mt2f'
    if (-not $summary.NativePassed -or -not $summary.NativeWon -or $summary.ExitCode -ne 0 -or
        -not $summary.OriginalFilesUnchanged -or $summary.CaptureFailures -ne 0 -or $summary.Mismatches -ne 0 -or
        $summary.Unsupported -ne 0 -or $summary.Pending -ne 0 -or
        (Get-FileHash -LiteralPath $trace -Algorithm SHA256).Hash -ine $summary.TraceSha256) {
        throw "Native profile failed acceptance or changed: $resolved"
    }
    $document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($trace)
    try {
        if (-not $document.RootElement.GetProperty($flag).GetBoolean()) {
            throw "Native profile does not contain the declared original relic: $flag"
        }
        if ($Relic -eq 'Covenant03' -and
            $document.RootElement.GetProperty('StartingCovenantControl').GetProperty('AscensionLevel').GetInt32() -ne 3) {
            throw 'Covenant03 requires original cumulative rank-three startup.'
        }
        $player = $document.RootElement.GetProperty('PlayerAdvisor')
        if ($player.ValueKind.ToString() -ne 'Object' -or
            $document.RootElement.GetProperty('RollingDecisions').GetArrayLength() -lt 2) {
            throw 'An actual rolling player capture is required; component and scripted battles cannot be reused.'
        }
        $observation = $player.GetProperty('FrozenObservation')
        if (($observation.ValueKind.ToString() -eq 'Object') -ne $Frozen) {
            throw 'Source/replay roles differ; a replay must retain its frozen-observation record.'
        }
        if ($Frozen -and (-not $observation.GetProperty('Paused').GetBoolean() -or
            $observation.GetProperty('SearchesAfterPause').GetInt32() -ne 0)) {
            throw 'Frozen replay did not pause background searches.'
        }
    } finally { $document.Dispose() }
    return [pscustomobject]@{ Profile = $resolved; Trace = $trace; Sha256 = $summary.TraceSha256 }
}
$buildNative = -not $SkipBuild
function Invoke-NativePairStep([string] $InputTrace) {
    $parameters = @{
        PlayerAdvisor = $true; RollingAdvice = $true; PlannerHorizon = 6; PlannerTimeLimitMs = 10000
        PlannerTacticalSeed = $true; PhysicalSpawnPoints = $true; SettleDeathDissolves = $true
        SettleCardAnimations = $true; BranchCardMasks = $true; NativeWorker = $NativeWorker
        TimeoutSeconds = $TimeoutSeconds; SkipBuild = -not $buildNative
    }
    $parameters[$flag] = $true
    if ($Relic -eq 'Covenant03') { $parameters.StartingCovenantLevel = 3 }
    if ($InputTrace) { $parameters.RollingInput = $InputTrace }
    $result = $null
    & (Join-Path $PSScriptRoot 'Run-FullBattleProbe.ps1') @parameters | ForEach-Object {
        $line = $_.ToString()
        if ($line.StartsWith('{')) { $result = $line | ConvertFrom-Json }
        else { Write-Host $line }
    }
    if ($null -eq $result -or -not $result.Profile) { throw 'Native runner did not return its completed profile.' }
    return Read-AcceptedProfile $result.Profile ([bool]$InputTrace)
}

$source = if ($SourceProfile) { Read-AcceptedProfile $SourceProfile $false } else { Invoke-NativePairStep '' }
if (-not $reusedSource) { $buildNative = $false }
Write-Output ('MOON-PAIR-SOURCE accepted=true reused=' + $reusedSource + ' profile=' + $source.Profile)
$callbackGate = switch ($Relic) {
    'Status' { '--moon-status-callbacks-only' }
    'HandPower' { '--moon-hand-power-callbacks-only' }
    default { '' }
}
if ($callbackGate) {
    dotnet $checker $callbackGate $source.Trace
    if ($LASTEXITCODE -ne 0) { throw 'Original source callbacks differ; frozen replay was not started.' }
}
if ($Relic -eq 'Covenant03') {
    dotnet $checker --moon-covenant-startup-only $source.Trace 3
    if ($LASTEXITCODE -ne 0) { throw 'Original Covenant startup differs; frozen replay was not started.' }
}
$replay = if ($ReplayProfile) { Read-AcceptedProfile $ReplayProfile $true } else { Invoke-NativePairStep $source.Trace }
if ($source.Profile -ieq $replay.Profile) { throw 'Source and frozen replay must come from independent native profiles.' }
Write-Output ('MOON-PAIR-REPLAY accepted=true reused=' + $reusedReplay + ' profile=' + $replay.Profile)

# Never publish a passing receipt until complete roots, forecasts, successors,
# RNG and both original callback sets pass the independent model/pair gate.
dotnet $checker $gate $source.Trace $replay.Trace
if ($LASTEXITCODE -ne 0) { throw 'Independent moon relic player-pair acceptance failed.' }
foreach ($accepted in @($source, $replay)) {
    if ((Get-FileHash -LiteralPath $accepted.Trace -Algorithm SHA256).Hash -ine $accepted.Sha256) {
        throw 'Native trace changed during independent acceptance.'
    }
}
$receiptPath = Join-Path $source.Profile 'player-pair-accepted.json'
$receipt = [ordered]@{
    Schema = 1; Composition = $(if ($Relic -eq 'Covenant03') { 'MoonRoomCovenant03' } else { 'MoonRoom' + $Relic + 'Relic' })
    SourceProfile = $source.Profile; ReplayProfile = $replay.Profile
    SourceSha256 = $source.Sha256; ReplaySha256 = $replay.Sha256
    IndependentFocusedCheck = $gate; ReusedSource = $reusedSource; ReusedReplay = $reusedReplay; Passed = $true
}
[IO.File]::WriteAllText($receiptPath, ($receipt | ConvertTo-Json) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
[pscustomobject]@{ Passed = $true; Composition = $receipt.Composition; Receipt = $receiptPath } | ConvertTo-Json -Compress
