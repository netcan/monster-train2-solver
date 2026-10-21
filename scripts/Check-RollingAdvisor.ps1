#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet(5, 7, 8)] [int[]] $ProgressedRing = @(),
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    if (-not $SkipBuild) {
        dotnet build src/ModelChecks/ModelChecks.csproj -c Release --no-restore -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Rolling advisor check build failed.' }
    }
    if ($ProgressedRing.Count -eq 0) {
        $archives = foreach ($ring in @(1, 2, 5, 7)) {
            foreach ($kind in @('search', 'replay', 'policy')) {
                "tests/fixtures/full-battle-rolling-advisor-ring$ring-$kind.mt2f"
            }
        }
        dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --rolling-advice-suite-only @archives
        if ($LASTEXITCODE -ne 0) { throw 'Full rolling battles, independent replays or completed policy comparison failed.' }
        $ProgressedRing = @(5, 7, 8)
    }
    foreach ($ring in $ProgressedRing | Select-Object -Unique) {
        $preparation = if ($ring -eq 8) { '-gold' } else { '' }
        $archives = foreach ($kind in @('search', 'replay', 'policy')) {
            "tests/fixtures/full-battle-progressed-ring$ring$preparation-$kind.mt2f"
        }
        dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --rolling-advice-victory-pair-only @archives
        if ($LASTEXITCODE -ne 0) { throw "Naturally progressed ring $ring victory/replay/policy acceptance failed." }
    }
} finally { Pop-Location }
