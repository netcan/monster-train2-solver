#requires -Version 7.4
param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/opening-capacity'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'battle.mt2f') { throw 'Unexpected opening capacity manifest.' }
$battle = Join-Path $directory 'battle.mt2f'
if ((Get-Item -LiteralPath $battle).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $battle).Hash -ine $manifest[0].sha256) { throw 'Opening capacity archive differs.' }
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --opening-capacity-only $battle
if ($LASTEXITCODE) { throw 'Native opening capacity check failed.' }
