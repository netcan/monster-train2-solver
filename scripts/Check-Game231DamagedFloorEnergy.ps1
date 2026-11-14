#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/damaged-floor-energy'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'full-battle.mt2f') { throw 'Unexpected damaged-floor energy manifest.' }
$path = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'Damaged-floor energy capture hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --damaged-floor-energy-battle-only $path
if ($LASTEXITCODE) { throw 'Independent damaged-floor energy validation failed.' }
Write-Output 'GAME-231-DAMAGED-FLOOR-ENERGY PASS: unchanged original EmberForDamagedPerFloor, 20 native roster/limit successors and 32 immutable branches, eight pure queries, three notified callbacks, paid heal/reinjury and 4/3/4 turn income, complete battle/RNG and 16 full branches. Earlier before-draw health/room effects remain guarded pending ordered native calibration.'
