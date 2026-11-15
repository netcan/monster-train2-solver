#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/wicked-fervor'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'battle.mt2f') { throw 'Unexpected Wicked Fervor manifest.' }
$entry = $manifest[0]; $path = Join-Path $directory $entry.path
if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'Wicked Fervor archive hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --original-wicked-fervor-only $path
if ($LASTEXITCODE) { throw 'Independent Wicked Fervor validation failed.' }
Write-Output 'GAME-231-WICKED-FERVOR PASS: original spell, ordered discard list, protected cards, zero discard control and full winning successors/RNG.'
