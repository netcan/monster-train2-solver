#requires -Version 7.4
param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    $project = Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj'
    dotnet build $project -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'ModelChecks build failed.' }
}

$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/controlled-original-resource-relics'
$entries = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t" |
    Where-Object { $_.path -ceq 'energy-draw-and-pyre.mt2f' })
if ($entries.Count -ne 1) { throw 'Expected one original resource relic receipt.' }

$entry = $entries[0]
$archive = Join-Path $directory $entry.path
if ((Get-Item -LiteralPath $archive).Length -ne [long]$entry.bytes -or
    (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ine $entry.sha256) {
    throw "Resource relic archive differs: $archive"
}

$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --original-resource-relics-only $archive
if ($LASTEXITCODE -ne 0) { throw 'Original resource relic check failed.' }

Write-Output 'GAME-231-RESOURCE-RELICS PASS. One archived battle with eight original relic owners.'
