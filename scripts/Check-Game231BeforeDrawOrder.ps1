#requires -Version 7.4
param([switch]$SkipBuild, [switch]$ComponentOnly)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (!$SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/before-draw-order'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or (($manifest.path | Sort-Object) -join ',') -cne 'energy-first.mt2f,heal-first.mt2f') { throw 'Unexpected before-draw order manifest.' }
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Before-draw order archive differs: $path" }
    $mode = if ($ComponentOnly) { '--before-draw-pair-only' } else { '--before-draw-order-only' }
    dotnet $checker $mode $path
    if ($LASTEXITCODE) { throw "Native before-draw order check failed: $($entry.path)" }
}
Write-Output "GAME-231-BEFORE-DRAW-ORDER PASS: unchanged original owners in both orders, distinct native income and immutable dispatch; complete battles included=$(!$ComponentOnly)."
