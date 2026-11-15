#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/drag-them-down'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'battle.mt2f') { throw 'Unexpected Drag Them Down manifest.' }
$entry = $manifest[0]; $path = Join-Path $directory $entry.path
if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'Drag Them Down archive hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --original-drag-them-down-only $path
if ($LASTEXITCODE) { throw 'Independent Drag Them Down validation failed.' }
Write-Output 'GAME-231-DRAG-THEM-DOWN PASS: original spell, status-before-damage, same-floor chained propagation, empty floor, lethal return and full winning successors/RNG.'
