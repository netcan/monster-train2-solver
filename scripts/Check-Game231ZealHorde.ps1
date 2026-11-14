#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/zeal-horde'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or ($manifest.path -join ',') -cne 'lifecycle.mt2f,full-battle.mt2f') { throw 'Unexpected Zeal/Horde manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'Zeal/Horde capture hash/size differs.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --zeal-horde-only (Join-Path $directory 'lifecycle.mt2f')
if ($LASTEXITCODE) { throw 'Independent Zeal/Horde lifecycle validation failed.' }
dotnet $checker --zeal-horde-battle-only (Join-Path $directory 'full-battle.mt2f')
if ($LASTEXITCODE) { throw 'Independent Zeal/Horde battle validation failed.' }
Write-Output 'GAME-231-ZEAL-HORDE PASS: 13 native full API/queue/drained successors, 32 immutable component branches, actual five-play/seven-turn victory, three troop deaths/Harvest, full chain/RNG, 16 immutable battle branches and 128-state serial/parallel action search. Other Horde health/merge/death combinations remain separate.'
