#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    if (-not $SkipBuild) {
        dotnet build src/ModelChecks/ModelChecks.csproj -c Release --no-restore -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Rolling advisor check build failed.' }
    }
    $archives = foreach ($ring in @(1, 2, 5, 7)) {
        foreach ($kind in @('search', 'replay', 'policy')) {
            "tests/fixtures/full-battle-rolling-advisor-ring$ring-$kind.mt2f"
        }
    }
    dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --rolling-advice-suite-only @archives
    if ($LASTEXITCODE -ne 0) { throw 'Full rolling battles, independent replays or completed policy comparison failed.' }
} finally { Pop-Location }
