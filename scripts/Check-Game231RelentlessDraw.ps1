#requires -Version 7.4
param([switch]$SkipBuild, [switch]$ComponentOnly)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/relentless-draw'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 3 -or (($manifest.path | Sort-Object) -join ',') -cne 'batch.mt2f,battle-t2.mt2f,battle-t3.mt2f') { throw 'Unexpected Relentless draw manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Relentless draw archive differs: $path" }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --relentless-draw-batch-only (Join-Path $directory 'batch.mt2f')
if ($LASTEXITCODE) { throw 'Independent native Relentless draw matrix failed.' }
if (!$ComponentOnly) {
    foreach ($battle in @('battle-t2.mt2f', 'battle-t3.mt2f')) {
        dotnet $checker --relentless-draw-battle-only (Join-Path $directory $battle)
        if ($LASTEXITCODE) { throw "Independent native Relentless draw battle failed: $battle" }
    }
}
Write-Output "GAME-231-RELENTLESS-DRAW PASS: cached Relentless draw and combined Warborne T2/T3 effects; battles included=$(!$ComponentOnly)."
