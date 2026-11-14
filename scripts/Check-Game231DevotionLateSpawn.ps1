#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/devotion-late-spawn'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'full-battle.mt2f') { throw 'Unexpected late-spawn manifest.' }
$path = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'Late-spawn capture hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --devotion-late-spawn-battle-only $path
if ($LASTEXITCODE) { throw 'Independent late-spawn validation failed.' }
Write-Output 'GAME-231-DEVOTION-LATE-SPAWN PASS: unchanged FriendlyUnitPunish and old Whelp owners, eight plays including a copied birth, zero/one/two levels, early level-up before late status, divisor-two application, 20 native divisor tests and 32 component branches, full callbacks/RNG and 16 complete battle chains. Broader graft/mirror/trigger combinations remain separate.'
