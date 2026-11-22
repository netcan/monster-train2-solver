#requires -Version 7.4
param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    $project = Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj'
    dotnet build $project -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'ModelChecks build failed.' }
}

$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/configured-opening-relics'
$names = @(
    'boss-mute-chicot.mt2f',
    'egg-hatch-removal.mt2f',
    'hymnist-team-spawn-and-duplicates.mt2f',
    'pyre-and-first-unit-births.mt2f'
)
$entries = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t" |
    Where-Object { $_.path -cin $names } | Sort-Object path)
if (($entries.path -join ',') -cne ($names -join ',')) {
    throw 'Unexpected spawn status relic receipts.'
}

$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
foreach ($entry in $entries) {
    $archive = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $archive).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ine $entry.sha256) {
        throw "Spawn status relic archive differs: $archive"
    }
    dotnet $checker --relic-spawn-status-only $archive
    if ($LASTEXITCODE -ne 0) { throw "Spawn status relic check failed: $($entry.path)" }
}
Write-Output 'GAME-231-SPAWN-STATUS-RELICS PASS. Four archived native birth traces.'
