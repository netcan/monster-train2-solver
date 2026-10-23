#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Covenant checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$fixtures = Join-Path $workspace 'tests/fixtures'
$single = @('00', '01', '04', '08') | ForEach-Object { Join-Path $fixtures "true-final-boss-spawn-calibration-covenant$_.mt2f" }
$combinations = @(3, 5, 6, 7, 9, 10, 11, 12, 13, 14) | ForEach-Object { Join-Path $fixtures "true-final-boss-spawn-calibration-covenants-$_.mt2f" }
$paths = @($single) + @($combinations) + @(Join-Path $fixtures 'true-final-boss-spawn-calibration-covenants.mt2f')
dotnet $checker --true-final-boss-covenants-matrix-only @paths
if ($LASTEXITCODE -ne 0) { throw 'Original Covenant birth matrix failed.' }
dotnet $checker --true-final-boss-covenant-battle-only (Join-Path $fixtures 'true-final-boss-covenant-battle-combined.mt2f')
if ($LASTEXITCODE -ne 0) { throw 'Combined original Covenant full battle failed.' }
foreach ($name in @('covenant04', 'covenants')) {
    dotnet $checker --true-final-boss-relentless-only (Join-Path $fixtures "true-final-boss-relentless-calibration-$name.mt2f")
    if ($LASTEXITCODE -ne 0) { throw "Original Covenant relentless control failed: $name" }
}
foreach ($name in @('true-final-boss-lethal-calibration-covenant04.mt2f', 'true-final-boss-lethal-preview-calibration-covenants.mt2f')) {
    dotnet $checker --true-final-boss-lethal-only (Join-Path $fixtures $name)
    if ($LASTEXITCODE -ne 0) { throw "Original Covenant terminal control failed: $name" }
}
foreach ($target in @('dominion', 'savagery', 'entropybot', 'entropymid', 'entropytop')) {
    dotnet $checker --true-final-boss-paid-spell-only (Join-Path $fixtures "true-final-boss-paid-spell-covenants-$target.mt2f")
    if ($LASTEXITCODE -ne 0) { throw "Original Covenant paid-spell target failed: $target" }
}
Write-Output 'TRUE-FINAL-BOSS-COVENANTS-FOCUSED PASS; no full regression executed.'
