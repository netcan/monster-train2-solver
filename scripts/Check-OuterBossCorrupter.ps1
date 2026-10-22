#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'ModelChecks build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$fixtures = Join-Path $workspace 'tests/fixtures'
Push-Location $fixtures
try {
    dotnet $checker --outer-boss-corrupter-pure
    if ($LASTEXITCODE -ne 0) { throw 'Corrupter pure controls failed.' }
    dotnet $checker --outer-boss-corrupter-only full-battle-outer-boss-corrupter-casting.mt2f outer-boss-corrupter-birth-calibration.mt2f outer-boss-corrupter-phase-calibration.mt2f boss-action-corrupter-controller-calibration.mt2f
    if ($LASTEXITCODE -ne 0) { throw 'Original Corrupter native chain failed.' }
    dotnet $checker --outer-boss-corrupter-pair-only full-battle-outer-boss-corrupter-casting.mt2f full-battle-outer-boss-corrupter-casting-replay.mt2f
    if ($LASTEXITCODE -ne 0) { throw 'Independent Corrupter replay failed.' }
    dotnet $checker --outer-boss-corrupter-relentless-only outer-boss-corrupter-relentless-turn-calibration.mt2f outer-boss-corrupter-relentless-phase-calibration.mt2f
    if ($LASTEXITCODE -ne 0) { throw 'Controlled Corrupter Relentless chain failed.' }
} finally { Pop-Location }
