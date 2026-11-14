#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/relentless-battle'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or ($manifest.path -join ',') -cne 'abandon.mt2f,continue.mt2f') {
    throw 'Unexpected Relentless battle manifest.'
}
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) {
        throw 'Relentless battle capture hash/size differs.'
    }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
dotnet $checker --relentless-battle-pair-only (Join-Path $directory 'abandon.mt2f') (Join-Path $directory 'continue.mt2f')
if ($LASTEXITCODE) { throw 'Independent Relentless battle validation failed.' }
dotnet $checker --relentless-search-only (Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/relentless-notification/notification.mt2f')
if ($LASTEXITCODE) { throw 'Bounded Relentless search validation failed.' }
Write-Output 'GAME-231-RELENTLESS-BATTLE PASS: paired native policies, reserved-arrival destruction, complete replay/RNG, executable search plans, serial/parallel/exact/planner checks and bounded infinite continuation. Live player dialog interaction remains unverified.'
