#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/horde-maximum'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or ($manifest.path -join ',') -cne 'lifecycle.mt2f,full-battle.mt2f') { throw 'Unexpected Horde maximum-health manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'Horde maximum-health capture hash/size differs.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --horde-maximum-only (Join-Path $directory 'lifecycle.mt2f')
if ($LASTEXITCODE) { throw 'Independent Horde maximum-health lifecycle validation failed.' }
dotnet $checker --horde-maximum-battle-only (Join-Path $directory 'full-battle.mt2f')
if ($LASTEXITCODE) { throw 'Independent Horde maximum-health battle validation failed.' }
Write-Output 'GAME-231-HORDE-MAXIMUM PASS: 22 native API/queue/drained successors, 32 immutable component branches, five-play/seven-turn native victory, three troop deaths/Harvest, capped healing/spawner offsets, full chain/RNG, 16 immutable battle branches and 128-state serial/parallel search. Lethal sacrifice/revival and other Horde combinations remain separate.'
