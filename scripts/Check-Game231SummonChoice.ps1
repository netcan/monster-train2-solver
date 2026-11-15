#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/summon-choice-offers'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or ($manifest.path -join ',') -cne 'offers.mt2f,battle.mt2f') { throw 'Unexpected summon choice manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw "Summon choice archive hash/size differs: $($entry.path)" }
}
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --summon-choice-offers-only (Join-Path $directory 'offers.mt2f') (Join-Path $directory 'battle.mt2f')
if ($LASTEXITCODE) { throw 'Independent summon choice offer validation failed.' }
Write-Output 'GAME-231-SUMMON-CHOICE PASS: native-calibrated offer stage, explicit DLC profiles, immutable parallel branches and baseline search; original skill/action/UI/birth integration is not established by this offer-stage fixture.'
