#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/tenacious'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'full-battle.mt2f') { throw 'Unexpected Tenacious battle manifest.' }
$path = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'Tenacious capture hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --tenacious-battle-only $path
if ($LASTEXITCODE) { throw 'Independent Tenacious battle validation failed.' }
Write-Output 'GAME-231-TENACIOUS PASS: unchanged original SpellsNoKillFriendlies, 58 native damage/source boundaries, four fixed nonstackable admissions, friendly lethal/one-HP/heal/armor/self-damage, hostile spell/Spikes controls, complete battle/RNG and 16 immutable branches. Mixed status pools, subtype/boss filters and random nonstackable targeting remain guarded.'
