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
if ($manifest.Count -ne 2 -or (($manifest.path | Sort-Object) -join ',') -cne 'tier3-battle.mt2f,tier3.mt2f') {
    throw 'Unexpected Thik deck-change manifest.'
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
foreach ($entry in $manifest) {
    $archive = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $archive).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $archive).Hash -ine $entry.sha256) { throw "Thik deck-change archive differs: $($entry.path)" }
    & dotnet $checker --thik-deck-change-only $archive
    if ($LASTEXITCODE) { throw "Thik deck-change replay failed: $($entry.path)" }
}
