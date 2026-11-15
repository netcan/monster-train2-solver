#requires -Version 7.4
param([switch]$SkipBuild, [switch]$ComponentOnly)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/pyre-damage-taken'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or (($manifest.path | Sort-Object) -join ',') -cne 'batch.mt2f,battle.mt2f') { throw 'Unexpected Pyre damage manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Pyre damage archive size/hash differs: $path" }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --pyre-damage-taken-batch-only (Join-Path $directory 'batch.mt2f')
if ($LASTEXITCODE) { throw 'Independent Pyre damage matrix failed.' }
if (!$ComponentOnly) {
    dotnet $checker --pyre-damage-taken-battle-only (Join-Path $directory 'battle.mt2f')
    if ($LASTEXITCODE) { throw 'Independent Pyre damage battle failed.' }
}
Write-Output "GAME-231-PYRE-DAMAGE-TAKEN PASS: shared ordered post-defense modifier; battle included=$(!$ComponentOnly)."
