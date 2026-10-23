#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Room attachment birth checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t"
$checks = @(
    @('--room-attachment-birth-only', 'player-room-attachment-birth-observed.mt2f'),
    @('--observed-successors-only', 'player-room-attachment-birth-observed.mt2f'),
    @('--stewards-quarters-only', 'full-battle-stewards-quarters.mt2f'),
    @('--stewards-quarters-only', 'full-battle-stewards-quarters-birth-order.mt2f'),
    @('--original-start-room-attachment-only', 'full-battle-original-start-room-attachment-floor3.mt2f'),
    @('--room-moon-callback-only', 'room-moon-callback-calibration.mt2f'),
    @('--enchantment-summon-only', 'enchantment-summon-calibration.mt2f')
)
foreach ($check in $checks) {
    $path = Join-Path $workspace "tests/fixtures/$($check[1])"
    $rows = @($manifest | Where-Object archive -CEQ $check[1])
    if ($rows.Count -ne 1 -or (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $rows[0].archive_sha256) {
        throw "Room attachment birth fixture bytes/hash mismatch: $($check[1])"
    }
    dotnet $checker $check[0] $path
    if ($LASTEXITCODE -ne 0) { throw "Room attachment birth check failed: $($check[0]) $($check[1])" }
}
Write-Output 'ROOM-ATTACHMENT-BIRTH-FOCUSED PASS: failed historical forecasts remain unaccepted; no full regression executed.'
