#requires -Version 7.4
param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/elle'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 3 -or (($manifest.path | Sort-Object) -join ',') -cne 'tier1.mt2f,tier2.mt2f,tier3.mt2f') {
    throw 'Unexpected Elle manifest.'
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
foreach ($entry in $manifest) {
    $battle = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $battle).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $battle).Hash -ine $entry.sha256) { throw "Elle archive differs: $($entry.path)" }
    dotnet $checker --elle-only $battle
    if ($LASTEXITCODE) { throw "Native Elle check failed: $($entry.path)" }
}
