#requires -Version 7.4
param([switch]$SkipBuild, [switch]$ComponentOnly)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/late-attack'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 3 -or (($manifest.path | Sort-Object) -join ',') -cne 'batch.mt2f,battle.mt2f,percentage-batch.mt2f') { throw 'Unexpected late-attack manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Late attack archive size/hash differs: $path" }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --late-attack-batch-only (Join-Path $directory 'batch.mt2f')
if ($LASTEXITCODE) { throw 'Independent late-attack matrix failed.' }
dotnet $checker --percentage-stats-batch-only (Join-Path $directory 'percentage-batch.mt2f')
if ($LASTEXITCODE) { throw 'Independent percentage birth with nonneutral late multiplier failed.' }
if (!$ComponentOnly) {
    dotnet $checker --original-late-attack-only (Join-Path $directory 'battle.mt2f')
    if ($LASTEXITCODE) { throw 'Independent original late-attack battle failed.' }
}
Write-Output "GAME-231-LATE-ATTACK PASS: shared attack multiplier and percentage composition; battle included=$(!$ComponentOnly)."
