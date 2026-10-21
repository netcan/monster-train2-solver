#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('starter', 'relics', 'ability', 'moon', 'history')]
    [string] $Case = 'starter',
    [ValidateRange(1, 2)] [int] $NativeWorker = 1,
    [string] $SourceTrace = '',
    [ValidateRange(1, 2147483647)] [int] $Seed = 424242,
    [switch] $Advisor,
    [switch] $ShowWindow,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    if (-not $SkipBuild) {
        foreach ($project in @('Probe', 'ModelChecks')) {
            dotnet build "src/$project/$project.csproj" -c Release --no-restore -p:WarningLevel=0 -v:q
            if ($LASTEXITCODE -ne 0) { throw "Planner $project build failed." }
        }
    }
    $arguments = @{
        SkipBuild = $true; PhysicalSpawnPoints = $true; Planner = $true
        NativeWorker = $NativeWorker; PlannerTurn = 1; PlannerHorizon = 2
        Policy = 'units-spells-and-junk'
        Seed = $Seed; Advisor = [bool]$Advisor; ShowWindow = [bool]$ShowWindow
    }
    switch ($Case) {
        'starter' { $arguments.PlannerTurn = 2; $arguments.PlannerHorizon = 3 }
        'relics' { $arguments.OriginalRelicBatch = $true }
        'ability' { $arguments.AbilityActivation = $true; $arguments.Policy = 'units-and-junk' }
        'moon' { $arguments.OriginalMoonCardUpgrades = $true; $arguments.PlannerTurn = 3 }
        'history' {
            $arguments.OriginalIsolatedSpellIds = @('f1dbd7d5-74fc-4507-aba1-dc1a8ec9393b')
            $arguments.OriginalUnitAbilityId = 'f6fa19db-cad1-4104-91a7-48098daabe4f'
            $arguments.FailFastOnIsolatedUnitReject = $true
            $arguments.PlannerTurn = 3
        }
    }
    function Invoke-NativePlan([hashtable] $Parameters) {
        Write-Host "PLANNER-NATIVE case=$Case worker=$NativeWorker replay=$($Parameters.ContainsKey('PlannerInput'))"
        $output = @(& (Join-Path $PSScriptRoot 'Run-FullBattleProbe.ps1') @Parameters)
        $output | Where-Object { $_ -is [string] -and $_ -match '^FULL-BATTLE-(RUN|TIMING|INSPECTION)' } | Write-Host
        $summary = @($output | Where-Object { $_ -is [string] -and $_.TrimStart().StartsWith('{') })
        if ($summary.Count -ne 1) { throw 'Native planner run did not return exactly one summary.' }
        $result = $summary[0] | ConvertFrom-Json
        if (-not $result.NativePassed -or $result.CaptureFailures -ne 0 -or $result.Mismatches -ne 0 -or
            $result.Unsupported -ne 0 -or $result.Pending -ne 0) { throw 'Native planner run was not accepted.' }
        return $result.Trace
    }
    if (-not $SourceTrace) { $SourceTrace = Invoke-NativePlan $arguments }
    $SourceTrace = (Resolve-Path -LiteralPath $SourceTrace).Path
    $arguments.PlannerInput = $SourceTrace
    $arguments.Advisor = $false
    $arguments.ShowWindow = $false
    $replayed = Invoke-NativePlan $arguments
    dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --planner-pair-only $SourceTrace $replayed
    if ($LASTEXITCODE -ne 0) { throw "Native planner pair failed: $Case" }
    [pscustomobject]@{ Case = $Case; Search = $SourceTrace; Replay = $replayed } | ConvertTo-Json -Compress
} finally { Pop-Location }
