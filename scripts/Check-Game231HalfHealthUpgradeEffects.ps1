#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/half-health-deathwish/upgrade-effects'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'upgrade-effects.mt2f') { throw 'Unexpected half-health upgrade-effect fixture manifest.' }
$path = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'Half-health upgrade-effect capture hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --half-health-upgrade-effects-only $path
if ($LASTEXITCODE) { throw 'Independent half-health upgrade-effect validation failed.' }
Write-Output 'GAME-231-HALF-HEALTH-UPGRADE-EFFECTS PASS: 28 native effects, complete API/drained states and spawner writes, 32 immutable branches. Full cards, authored Bloodshed/Punish, death and shared worlds require separate acceptance.'
