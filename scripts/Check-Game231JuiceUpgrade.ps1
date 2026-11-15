#requires -Version 7.4
param([switch]$SkipBuild, [switch]$ComponentOnly)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/juice-upgrade'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or (($manifest.path | Sort-Object) -join ',') -cne 'batch.mt2f,battle.mt2f') { throw 'Unexpected Juice upgrade manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Juice upgrade archive size/hash differs: $path" }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --juice-upgrade-batch-only (Join-Path $directory 'batch.mt2f')
if ($LASTEXITCODE) { throw 'Independent Juice source callback matrix failed.' }
if (!$ComponentOnly) {
    dotnet $checker --juice-upgrade-battle-only (Join-Path $directory 'battle.mt2f')
    if ($LASTEXITCODE) { throw 'Independent Juice draw-upgrade battle failed.' }
}
Write-Output "GAME-231-JUICE-UPGRADE PASS: shared source callback, queue mutation and clone/alias lifetimes; battle included=$(!$ComponentOnly)."
