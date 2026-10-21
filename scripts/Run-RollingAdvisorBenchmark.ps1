#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateRange(0, 20)] [int] $BattleDistance = 2,
    [ValidateRange(1, 2147483647)] [int] $Seed = 424242,
    [ValidateRange(1, 2)] [int] $NativeWorker = 1,
    [ValidateRange(2, 16)] [int] $PlannerHorizon = 3,
    [switch] $PlannerTacticalSeed,
    [ValidateRange(0, 2)] [int] $PlannerDeploymentRoom = 2,
    [ValidateRange(100, 30000)] [int] $PlannerTimeLimitMs = 3000,
    [ValidateRange(60, 3600)] [int] $TimeoutSeconds = 1200,
    [string] $SourceTrace = '',
    [string] $ReplayTrace = '',
    [string] $BaselineTrace = '',
    [string] $InputProfile = '',
    [switch] $RequireWin,
    [switch] $OuterBossTurns,
    [switch] $OuterBossBirth,
    [switch] $SettleDeathDissolves,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    if (-not $SkipBuild) {
        foreach ($project in @('Probe', 'ModelChecks')) {
            dotnet build "src/$project/$project.csproj" -c Release --no-restore -p:WarningLevel=0 -v:q
            if ($LASTEXITCODE -ne 0) { throw "Rolling advisor $project build failed." }
        }
    }
    $arguments = @{ Seed = $Seed; BattleDistance = $BattleDistance; NativeWorker = $NativeWorker
        SkipBuild = $true; PhysicalSpawnPoints = $true; BranchCardMasks = $true; Policy = 'units-spells-and-junk'
        TimeoutSeconds = $TimeoutSeconds; PlannerHorizon = $PlannerHorizon; PlannerDeploymentRoom = $PlannerDeploymentRoom
        PlannerTimeLimitMs = $PlannerTimeLimitMs }
    if ($InputProfile) { $arguments.InputProfile = $InputProfile }
    if ($PlannerTacticalSeed) { $arguments.PlannerTacticalSeed = $true }
    if ($SettleDeathDissolves) { $arguments.SettleDeathDissolves = $true }
    if ($OuterBossTurns) { $arguments.OuterBossTurns = $true }
    if ($OuterBossBirth) { $arguments.OuterBossBirth = $true }
    function Invoke-RollingNative([hashtable] $Parameters, [bool] $VictoryRequired = $false) {
        $output = @(& (Join-Path $PSScriptRoot 'Run-FullBattleProbe.ps1') @Parameters)
        $output | Where-Object { $_ -is [string] -and $_ -match '^FULL-BATTLE-(RUN|TIMING|INSPECTION)' } | Write-Host
        $summary = @($output | Where-Object { $_ -is [string] -and $_.TrimStart().StartsWith('{') })
        if ($summary.Count -ne 1) { throw 'Expected exactly one native rolling summary.' }
        $result = $summary[0] | ConvertFrom-Json
        if (-not $result.NativePassed -or -not $result.OriginalFilesUnchanged -or
            $result.CaptureFailures -ne 0 -or $result.Mismatches -ne 0 -or $result.Unsupported -ne 0 -or $result.Pending -ne 0) {
            throw 'Native rolling battle was not accepted.'
        }
        if ($VictoryRequired -and -not $result.NativeWon) {
            throw "Native battle lost; victory required. Trace retained: $($result.Trace)"
        }
        return $result.Trace
    }
    if (-not $SourceTrace) {
        $arguments.RollingAdvice = $true
        $SourceTrace = Invoke-RollingNative $arguments $RequireWin.IsPresent
    }
    $SourceTrace = (Resolve-Path -LiteralPath $SourceTrace).Path
    if ($RequireWin) {
        Add-Type -Path (Join-Path $workspace 'src/FixtureArchive/bin/Release/net8.0/FixtureArchive.dll')
        $document = [MonsterTrain2Poju.Fixtures.FixtureDocument]::Read($SourceTrace)
        try {
            if (-not $document.RootElement.GetProperty('NativeWon').GetBoolean()) {
                throw "Source battle lost; victory required. Trace retained: $SourceTrace"
            }
        } finally { $document.Dispose() }
    }
    if (-not $ReplayTrace) {
        $arguments.RollingAdvice = $true; $arguments.RollingInput = $SourceTrace
        $ReplayTrace = Invoke-RollingNative $arguments $RequireWin.IsPresent
    }
    $ReplayTrace = (Resolve-Path -LiteralPath $ReplayTrace).Path
    if (-not $BaselineTrace) {
        $arguments.Remove('RollingAdvice'); $arguments.Remove('RollingInput')
        $BaselineTrace = Invoke-RollingNative $arguments
    }
    $BaselineTrace = (Resolve-Path -LiteralPath $BaselineTrace).Path
    $check = if ($RequireWin) { '--rolling-advice-victory-pair-only' } else { '--rolling-advice-pair-only' }
    dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- $check $SourceTrace $ReplayTrace $BaselineTrace
    if ($LASTEXITCODE -ne 0) { throw 'Rolling native replay/policy comparison failed.' }
    [pscustomobject]@{ Seed = $Seed; BattleDistance = $BattleDistance; VictoryRequired = $RequireWin.IsPresent;
        Source = $SourceTrace; Replay = $ReplayTrace; Baseline = $BaselineTrace } |
        ConvertTo-Json -Compress
} finally { Pop-Location }
