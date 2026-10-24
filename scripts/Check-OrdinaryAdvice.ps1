#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'FixturePaths.ps1')
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Ordinary advice checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t"
function Get-VerifiedPair([string] $Case) {
    foreach ($mode in @('search', 'replay')) {
        $name = "full-battle-player-$Case-$mode.mt2f"
        $path = Get-TestFixturePath "$name"
        $rows = @($manifest | Where-Object archive -CEQ $name)
        if ($rows.Count -ne 1 -or (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
            (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $rows[0].archive_sha256) {
            throw "Ordinary advice archive bytes/hash mismatch: $name"
        }
        $path
    }
}
$archives = @(Get-VerifiedPair 'ordinary-continuation')
dotnet $checker --ordinary-advice-only $archives[0]
if ($LASTEXITCODE -ne 0) { throw 'Ordinary advice concurrency/horizon/divergence checks failed.' }
dotnet $checker --player-ordinary-advice-pair-only @archives
if ($LASTEXITCODE -ne 0) { throw 'Independent ordinary advice pair checks failed.' }
dotnet $checker --player-floor-browse-pair-only @archives
if ($LASTEXITCODE -ne 0) { throw 'Floor browsing no longer retains native advice.' }
$crossTurn = @(Get-VerifiedPair 'cross-turn-scroll')
dotnet $checker --player-ordinary-advice-pair-only @crossTurn
if ($LASTEXITCODE -ne 0) { throw 'Cross-turn/native wheel continuation pair checks failed.' }
$liveName = 'player-live-conduit-floor-scroll.mt2f'
$livePath = Get-TestFixturePath "$liveName"
$liveRows = @($manifest | Where-Object archive -CEQ $liveName)
if ($liveRows.Count -ne 1 -or (Get-Item -LiteralPath $livePath).Length -ne [long]$liveRows[0].archive_bytes -or
    (Get-FileHash -LiteralPath $livePath -Algorithm SHA256).Hash -ine $liveRows[0].archive_sha256) {
    throw 'Live Conduit scrolling archive bytes/hash mismatch.'
}
dotnet $checker --live-floor-scroll-only $livePath
if ($LASTEXITCODE -ne 0) { throw 'Live Conduit selection callback regression failed.' }
$natural = @(Get-VerifiedPair 'natural-ring7-ordinary')
dotnet $checker --terminal-advice-only $natural[0]
if ($LASTEXITCODE -ne 0) { throw 'Terminal losing-forecast continuation checks failed.' }
dotnet $checker --player-natural-ring7-default-pair-only @natural
if ($LASTEXITCODE -ne 0) { throw 'Natural Ring7 default-budget native pair checks failed.' }
dotnet $checker --player-ordinary-advice-pair-only @natural
if ($LASTEXITCODE -ne 0) { throw 'Natural Ring7 same-turn continuation checks failed.' }
Write-Output 'ORDINARY-ADVICE-FOCUSED PASS: no full regression executed.'
