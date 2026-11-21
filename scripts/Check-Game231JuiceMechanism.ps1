#requires -Version 7.4
param([switch]$SkipBuild, [switch]$ComponentOnly)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/juice'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or (($manifest.path | Sort-Object) -join ',') -cne 'batch.mt2f,battle.mt2f') { throw 'Unexpected Juice manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Juice archive size/hash differs: $path" }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --juice-mechanism-batch-only (Join-Path $directory 'batch.mt2f')
if ($LASTEXITCODE) { throw 'Independent Juice callback matrix failed.' }
if (!$ComponentOnly) {
    dotnet $checker --original-juice-upgrades-only (Join-Path $directory 'battle.mt2f')
    if ($LASTEXITCODE) { throw 'Independent original Juice upgrade battle failed.' }
    $mollisDirectory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/mollis-cycle-of-life'
    $mollis = @(Import-Csv -LiteralPath (Join-Path $mollisDirectory 'manifest.tsv') -Delimiter "`t")
    $battle = Join-Path $mollisDirectory 'battle.mt2f'
    if ($mollis.Count -ne 1 -or $mollis[0].path -cne 'battle.mt2f' -or
        (Get-Item -LiteralPath $battle).Length -ne [long]$mollis[0].bytes -or
        (Get-FileHash -LiteralPath $battle -Algorithm SHA256).Hash -ine $mollis[0].sha256) {
        throw 'Original Mollis battle size or hash differs.'
    }
    dotnet $checker --original-mollis-cycle-only $battle
    if ($LASTEXITCODE) { throw 'Independent original Mollis Cycle of Life replay failed.' }
}
Write-Output "GAME-231-JUICE-MECHANISM PASS: five original upgrades, 180 native callback pairs; battle included=$(!$ComponentOnly)."
