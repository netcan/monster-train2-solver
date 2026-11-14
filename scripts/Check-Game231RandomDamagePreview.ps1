#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/random-damage-preview'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or ($manifest.path -join ',') -cne 'player.mt2f,enemy.mt2f') { throw 'Unexpected random-damage preview manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'Random-damage preview archive hash/size differs.' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --random-damage-card-preview-only $path
    if ($LASTEXITCODE) { throw "Independent random-damage preview validation failed: $($entry.path)" }
}
Write-Output 'GAME-231-RANDOM-DAMAGE-PREVIEW PASS: 1024 player/enemy targeted/room/main/OnCast hovers, exact native BattleTest draws, full copied and removed actors, copy/effect/cancel states and 16 immutable branches per root; real Battle stream preserved. Random targets, linked damage, traits and other callbacks remain separate.'
