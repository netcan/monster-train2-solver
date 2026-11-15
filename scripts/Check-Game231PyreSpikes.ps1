#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/pyre-spikes-terminal'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
$names = @('sweep.mt2f', 'single.mt2f', 'sustain.mt2f')
if ($manifest.Count -ne 3 -or @($manifest.path | Sort-Object -Unique).Count -ne 3 -or
    @($manifest.path | Where-Object { $_ -cnotin $names }).Count) { throw 'Unexpected Pyre spikes manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Pyre spikes archive hash/size differs: $($entry.path)" }
}
$paths = @($names | ForEach-Object { Join-Path $directory $_ })
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --pyre-spikes-only @paths
if ($LASTEXITCODE) { throw 'Independent native Pyre spikes validation failed.' }
Write-Output 'GAME-231-PYRE-SPIKES PASS: three native terminal losses, live damage writer/queue-entry evidence, first-target sweep interruption, nonlethal and single-attack controls, complete room/train replay and independent immutable branches.'
