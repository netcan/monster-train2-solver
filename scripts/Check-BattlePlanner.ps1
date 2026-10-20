#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    if (-not $SkipBuild) {
        dotnet build src/ModelChecks/ModelChecks.csproj -c Release --no-restore -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Planner check build failed.' }
    }
    $cases = @(
        @{ Archive = 'full-battle-units-spells-and-junk.mt2f'; Turn = 2; Horizon = 3 },
        @{ Archive = 'full-battle-original-relic-batch.mt2f'; Turn = 1; Horizon = 2 },
        @{ Archive = 'full-battle-ability-activation.mt2f'; Turn = 1; Horizon = 2 },
        @{ Archive = 'full-battle-original-moon-card-upgrades.mt2f'; Turn = 1; Horizon = 2 },
        @{ Archive = 'full-battle-original-lunar-echo-ability.mt2f'; Turn = 1; Horizon = 2 }
    )
    foreach ($case in $cases) {
        Write-Host "PLANNER-CASE $($case.Archive)"
        dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --battle-planner-only "tests/fixtures/$($case.Archive)" $case.Turn $case.Horizon
        if ($LASTEXITCODE -ne 0) { throw "Planner case failed: $($case.Archive)" }
    }
} finally { Pop-Location }
