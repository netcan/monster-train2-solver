#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild, [switch] $ConsumerOnly)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/end-battle-resources'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or (($manifest.path | Sort-Object) -join ',') -cne 'calibration.mt2f,natural-ring1.mt2f') { throw 'Unexpected end-battle resource manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'End-battle resource archive size/hash differs.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
foreach ($archive in @((Join-Path $directory 'calibration.mt2f'), (Join-Path $workspace 'tests/fixtures/campaign/progression/campaign-battle-resource-calibration.mt2f'))) {
    dotnet $checker --campaign-battle-resources-only $archive
    if ($LASTEXITCODE) { throw "Independent end-battle resource validation failed: $archive" }
}
if (-not $ConsumerOnly) {
    dotnet $checker --campaign-battle-resource-natural-only (Join-Path $directory 'natural-ring1.mt2f')
    if ($LASTEXITCODE) { throw 'Independent natural carried-owner battle validation failed.' }
}
Write-Output "GAME-231-END-BATTLE-RESOURCES PASS: 864 current and 756 legacy consumer cases; unknown companion guards; natural carried-owner battle included=$(-not $ConsumerOnly)."
