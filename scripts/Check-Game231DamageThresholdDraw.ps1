#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/damage-threshold-draw'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'full-battle.mt2f') { throw 'Unexpected damage threshold draw manifest.' }
$path = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) {
    throw 'Damage threshold draw capture hash/size differs.'
}
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --damage-threshold-draw-battle-only $path
if ($LASTEXITCODE) { throw 'Independent damage threshold draw full-battle validation failed.' }
Write-Output 'GAME-231-DAMAGE-THRESHOLD-DRAW PASS: original DrawCardOnTurnDamage and RevengeTriggerTwice, shared threshold, once-per-turn draw/reset, actual double-Hit, preview controls and complete battle/RNG with 16 immutable branches.'
