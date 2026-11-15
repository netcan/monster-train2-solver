#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/end-battle-resources'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'calibration.mt2f') { throw 'Unexpected end-battle resource manifest.' }
$path = Join-Path $directory $manifest[0].path
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'End-battle resource archive size/hash differs.' }
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
foreach ($archive in @($path, (Join-Path $workspace 'tests/fixtures/campaign/progression/campaign-battle-resource-calibration.mt2f'))) {
    dotnet $checker --campaign-battle-resources-only $archive
    if ($LASTEXITCODE) { throw "Independent end-battle resource validation failed: $archive" }
}
Write-Output 'GAME-231-END-BATTLE-RESOURCES PASS: 864 current and 756 legacy native consumer cases; combat recognition preserves unknown companion guards. Ordinary natural battle integration is separate.'
