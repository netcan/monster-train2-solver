#requires -Version 7.4
param([switch]$SkipBuild, [switch]$WithBaselineReplay)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/room-health-query-batch'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or (($manifest.path | Sort-Object) -join ',') -cne 'batch.mt2f,battle.mt2f') { throw 'Unexpected room-health batch manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Room-health archive size/hash differs: $path" }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$checkArguments = @('--room-health-mechanism-batch-only', (Join-Path $directory 'batch.mt2f'))
if ($WithBaselineReplay) { $checkArguments += Join-Path $directory 'battle.mt2f' }
dotnet $checker @checkArguments
if ($LASTEXITCODE) { throw 'Independent generated room-health query comparison failed.' }
Write-Output 'GAME-231-ROOM-HEALTH-MECHANISM-BATCH PASS: generated native query matrix; battle and card-hover lifecycle acceptance is separate.'
