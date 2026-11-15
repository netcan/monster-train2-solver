#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/missing-health-status'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'battle.mt2f') { throw 'Unexpected missing-health status manifest.' }
$path = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'Missing-health status archive hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --original-missing-health-status-only $path
if ($LASTEXITCODE) { throw 'Independent original missing-health status validation failed.' }
Write-Output 'GAME-231-MISSING-HEALTH-STATUS PASS: unchanged Collector/Oil, four original skills, room/tower/Pyre totals and per-target status attribution, full winning chain, independent branches and zero-unsupported serial/parallel search.'
