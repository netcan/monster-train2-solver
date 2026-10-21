#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateRange(0, 20)] [int] $BattleDistance = 2,
    [ValidateRange(1, 2147483647)] [int] $Seed = 424242,
    [ValidateRange(1, 2)] [int] $NativeWorker = 1,
    [string] $SourceTrace = '',
    [string] $ReplayTrace = '',
    [string] $BaselineTrace = '',
    [string] $InputProfile = '',
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
        TimeoutSeconds = 900 }
    if ($InputProfile) { $arguments.InputProfile = $InputProfile }
    function Invoke-RollingNative([hashtable] $Parameters) {
        $output = @(& (Join-Path $PSScriptRoot 'Run-FullBattleProbe.ps1') @Parameters)
        $output | Where-Object { $_ -is [string] -and $_ -match '^FULL-BATTLE-(RUN|TIMING|INSPECTION)' } | Write-Host
        $summary = @($output | Where-Object { $_ -is [string] -and $_.TrimStart().StartsWith('{') })
        if ($summary.Count -ne 1) { throw 'Expected exactly one native rolling summary.' }
        $result = $summary[0] | ConvertFrom-Json
        if (-not $result.NativePassed -or -not $result.OriginalFilesUnchanged -or
            $result.CaptureFailures -ne 0 -or $result.Mismatches -ne 0 -or $result.Unsupported -ne 0 -or $result.Pending -ne 0) {
            throw 'Native rolling battle was not accepted.'
        }
        return $result.Trace
    }
    if (-not $SourceTrace) {
        $arguments.RollingAdvice = $true
        $SourceTrace = Invoke-RollingNative $arguments
    }
    $SourceTrace = (Resolve-Path -LiteralPath $SourceTrace).Path
    if (-not $ReplayTrace) {
        $arguments.RollingAdvice = $true; $arguments.RollingInput = $SourceTrace
        $ReplayTrace = Invoke-RollingNative $arguments
    }
    $ReplayTrace = (Resolve-Path -LiteralPath $ReplayTrace).Path
    if (-not $BaselineTrace) {
        $arguments.Remove('RollingAdvice'); $arguments.Remove('RollingInput')
        $BaselineTrace = Invoke-RollingNative $arguments
    }
    $BaselineTrace = (Resolve-Path -LiteralPath $BaselineTrace).Path
    dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --rolling-advice-pair-only $SourceTrace $ReplayTrace $BaselineTrace
    if ($LASTEXITCODE -ne 0) { throw 'Rolling native replay/policy comparison failed.' }
    [pscustomobject]@{ Seed = $Seed; BattleDistance = $BattleDistance; Source = $SourceTrace; Replay = $ReplayTrace; Baseline = $BaselineTrace } |
        ConvertTo-Json -Compress
} finally { Pop-Location }
