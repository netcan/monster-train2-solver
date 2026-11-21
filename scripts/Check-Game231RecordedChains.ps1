#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild, [string[]] $Archive = @())

$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$runId = (Get-Date -Format yyyyMMdd-HHmmss) + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$report = Join-Path $workspace "results/recorded-chain-triage-$runId/report.tsv"
& dotnet $checker --game231-recorded-chain-triage $workspace $report @Archive
if ($LASTEXITCODE) { throw "Recorded chain triage failed. See $report" }
