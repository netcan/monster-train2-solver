#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    if (-not $SkipBuild) {
        dotnet build src/ModelChecks/ModelChecks.csproj -c Release --no-restore -p:WarningLevel=0 -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Advice check build failed.' }
    }
    dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --tentative-advice-only
    if ($LASTEXITCODE -ne 0) { throw 'Tentative advice input checks failed.' }
    dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --battle-advice-only tests/fixtures/planning/search/full-battle-planner-starter-search.mt2f
    if ($LASTEXITCODE -ne 0) { throw 'Advice session check failed.' }
    dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --floor-browse-advice-only tests/fixtures/advisor/presentation/full-battle-player-boss-search.mt2f
    if ($LASTEXITCODE -ne 0) { throw 'Floor-browsing advice reuse check failed.' }
    $cases = @(Get-ChildItem tests/fixtures -Recurse -File -Filter 'full-battle-advisor-*-search.mt2f')
    if ($cases.Count -ne 3) { throw 'Expected three curated natural advisor cases.' }
    foreach ($case in $cases) {
        $replay = Join-Path $case.DirectoryName ($case.Name -replace '-search\.mt2f$', '-replay.mt2f')
        dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --native-advice-only $case.FullName
        if ($LASTEXITCODE -ne 0) { throw "Live advisor evidence failed: $($case.Name)" }
        dotnet run --project src/ModelChecks/ModelChecks.csproj -c Release --no-build -- --planner-pair-only $case.FullName $replay
        if ($LASTEXITCODE -ne 0) { throw "Independent advisor replay failed: $($case.Name)" }
    }
} finally { Pop-Location }
