#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/devotion-damage'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'full-battle.mt2f') { throw 'Unexpected Devotion damage battle manifest.' }
$path = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'Devotion damage capture hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --devotion-damage-battle-only $path
if ($LASTEXITCODE) { throw 'Independent Devotion damage battle validation failed.' }
Write-Output 'GAME-231-DEVOTION-DAMAGE PASS: unchanged original DrawCardOnEarlyDiscard and Venerate, shared battle-damage thresholds, one award per large hit, cross-turn carry, cost/Consume, native level/preview controls and complete battle/RNG with 16 immutable branches.'
