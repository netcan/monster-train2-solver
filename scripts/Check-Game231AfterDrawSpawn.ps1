#requires -Version 7.4
param(
    [switch]$SkipBuild,
    [ValidateSet('cult-leader.mt2f', 'cult-leader-lonely-rooms.mt2f', 'cult-leader-morsels.mt2f', 'morsels.mt2f')][string]$Fixture
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/after-draw-spawn'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ((($manifest.path | Sort-Object) -join ',') -cne 'cult-leader-lonely-rooms.mt2f,cult-leader-morsels.mt2f,cult-leader.mt2f,morsels.mt2f') {
    throw 'Unexpected after-draw spawn manifest.'
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
foreach ($entry in $manifest) {
    if ($Fixture -and $entry.path -cne $Fixture) { continue }
    $archive = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $archive).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $archive).Hash -ine $entry.sha256) { throw "After-draw spawn archive differs: $($entry.path)" }
    & dotnet $checker --after-draw-spawn-only $archive
    if ($LASTEXITCODE) { throw "After-draw spawn replay failed: $($entry.path)" }
}
