#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/projection-audits/punished-champions'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or ($manifest.path -join ',') -cne 'catalog.mt2f,baseline.mt2f') { throw 'Unexpected champion definition manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Champion archive hash/size differs: $($entry.path)" }
}
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --punished-champion-definitions-only (Join-Path $directory 'catalog.mt2f') (Join-Path $directory 'baseline.mt2f')
if ($LASTEXITCODE) { throw 'Independent champion definition audit failed.' }
Write-Output 'GAME-231-PUNISHED-CHAMPION-DEFINITIONS PASS: definition audit and unchanged-state baseline; upgrade/summon-choice runtime acceptance remains separate.'
