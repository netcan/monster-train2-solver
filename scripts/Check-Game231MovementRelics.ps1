#requires -Version 7.4
param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
$project = Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj'
if (!$SkipBuild) {
    dotnet build $project -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'ModelChecks build failed.' }
}

$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/movement-relics'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'movement-receipts.tsv') -Delimiter "`t")
$expected = 'floor-transition-damage.mt2f,pyregel-on-ascend.mt2f'
if ($manifest.Count -ne 2 -or (($manifest.path | Sort-Object) -join ',') -cne $expected) {
    throw 'Unexpected movement relic manifest.'
}

$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
foreach ($entry in $manifest) {
    $archive = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $archive).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ine $entry.sha256) {
        throw "Movement relic archive differs: $archive"
    }
    dotnet $checker --bump-only $archive
    if ($LASTEXITCODE -ne 0) { throw "Movement relic check failed: $($entry.path)" }
}
