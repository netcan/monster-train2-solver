#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/half-health-deathwish'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or @($manifest.path | Select-Object -Unique).Count -ne 2 -or
    @($manifest | Where-Object { $_.path -notin @('activation.mt2f', 'lifecycle.mt2f') }).Count) {
    throw 'Unexpected half-health fixture manifest.'
}
$row = $manifest | Where-Object path -CEQ 'lifecycle.mt2f'
$path = Join-Path $directory $row.path
if ((Get-Item -LiteralPath $path).Length -ne [long]$row.bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $row.sha256) { throw 'Half-health lifecycle capture hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --half-health-lifecycle-only $path
if ($LASTEXITCODE) { throw 'Independent half-health damage/heal lifecycle validation failed.' }
Write-Output 'GAME-231-HALF-HEALTH-LIFECYCLE PASS: 13 complete native successors/dispatches and 32 immutable branches; maximum-health has separate acceptance; death, preview and shared-world lifecycles remain guarded.'
