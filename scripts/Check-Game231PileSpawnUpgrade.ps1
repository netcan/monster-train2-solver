#requires -Version 7.4
param([switch]$SkipBuild, [switch]$ComponentOnly)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/pile-spawn-upgrade'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or (($manifest.path | Sort-Object) -join ',') -cne 'batch.mt2f,battle.mt2f') { throw 'Unexpected pile birth-upgrade manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Pile birth-upgrade archive differs: $path" }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --pile-spawn-upgrade-batch-only (Join-Path $directory 'batch.mt2f')
if ($LASTEXITCODE) { throw 'Independent native pile birth-upgrade matrix failed.' }
if (!$ComponentOnly) {
    dotnet $checker --pile-spawn-upgrade-battle-only (Join-Path $directory 'battle.mt2f')
    if ($LASTEXITCODE) { throw 'Independent native pile birth-upgrade battle failed.' }
}
Write-Output "GAME-231-PILE-SPAWN-UPGRADE PASS: shared repeated direct birth upgrades; battle included=$(!$ComponentOnly)."
