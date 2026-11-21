#requires -Version 7.4
param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/thik-deck-change'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'tier3.mt2f') { throw 'Unexpected Thik deck-change manifest.' }
$archive = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $archive).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $archive).Hash -ine $manifest[0].sha256) { throw 'Thik deck-change archive differs.' }
& dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --thik-deck-change-only $archive
if ($LASTEXITCODE) { throw 'Thik deck-change replay failed.' }
