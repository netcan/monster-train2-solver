#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('Strategy', 'Keys', 'Native', 'FragileStrategy', 'FragileNative')][string[]] $Case = @('Strategy', 'Keys', 'Native', 'FragileStrategy', 'FragileNative'),
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Default advice checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = @(Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t")
function Get-Archive([string] $Name) {
    $path = Join-Path $workspace "tests/fixtures/$Name"
    $rows = @($manifest | Where-Object archive -CEQ $Name)
    if ($rows.Count -ne 1 -or -not (Test-Path -LiteralPath $path) -or
        (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $rows[0].archive_sha256) {
        throw "Default advice archive bytes/hash mismatch: $Name"
    }
    $path
}
function Invoke-Check([string] $Mode, [string[]] $Archives) {
    dotnet $checker $Mode @Archives
    if ($LASTEXITCODE -ne 0) { throw "Default advice check failed: $Mode" }
}
if ('Keys' -in $Case) {
    Invoke-Check '--state-key-only' @(Get-Archive 'full-battle-player-natural-ring7-ordinary-search.mt2f')
}
if ('Strategy' -in $Case) {
    Invoke-Check '--natural-ring7-default-planner-only' @(Get-Archive 'full-battle-player-natural-ring7-ordinary-search.mt2f')
    Invoke-Check '--shorter-advice-budget-only' @(Get-Archive 'player-fragile-budget-boundary.mt2f')
    Invoke-Check '--atomic-advice-budget-only' @(Get-Archive 'advisor-atomic-budget-failure.mt2f')
    Invoke-Check '--ordinary-advice-budget-only' @(Get-Archive 'advisor-ordinary-budget-failure.mt2f')
}
if ('FragileStrategy' -in $Case) {
    Invoke-Check '--fragile-default-planner-only' @(Get-Archive 'full-battle-player-boss-fragile-adaptive-budget-loss.mt2f')
}
if ('FragileNative' -in $Case) {
    Invoke-Check '--player-boss-fragile-default-pair-only' @(
        (Get-Archive 'full-battle-player-boss-fragile-default-search.mt2f'),
        (Get-Archive 'full-battle-player-boss-fragile-default-replay.mt2f'))
}
if ('Native' -in $Case) {
    Invoke-Check '--player-natural-ring7-default-win-pair-only' @(
        (Get-Archive 'full-battle-player-natural-ring7-short-budget-search.mt2f'),
        (Get-Archive 'full-battle-player-natural-ring7-short-budget-replay.mt2f'))
    Invoke-Check '--player-state-key-pair-only' @(
        (Get-Archive 'full-battle-player-state-key-search.mt2f'),
        (Get-Archive 'full-battle-player-state-key-replay.mt2f'))
}
Write-Output "DEFAULT-ADVICE-FOCUSED PASS cases=$($Case -join ','); no full regression executed."
