#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Ordinary advice checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t"
$archives = foreach ($mode in @('search', 'replay')) {
    $name = "full-battle-player-ordinary-continuation-$mode.mt2f"
    $path = Join-Path $workspace "tests/fixtures/$name"
    $rows = @($manifest | Where-Object archive -CEQ $name)
    if ($rows.Count -ne 1 -or (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $rows[0].archive_sha256) {
        throw "Ordinary advice archive bytes/hash mismatch: $name"
    }
    $path
}
dotnet $checker --ordinary-advice-only $archives[0]
if ($LASTEXITCODE -ne 0) { throw 'Ordinary advice concurrency/horizon/divergence checks failed.' }
dotnet $checker --player-ordinary-advice-pair-only @archives
if ($LASTEXITCODE -ne 0) { throw 'Independent ordinary advice pair checks failed.' }
dotnet $checker --player-floor-browse-pair-only @archives
if ($LASTEXITCODE -ne 0) { throw 'Floor browsing no longer retains native advice.' }
Write-Output 'ORDINARY-ADVICE-FOCUSED PASS: no full regression executed.'
