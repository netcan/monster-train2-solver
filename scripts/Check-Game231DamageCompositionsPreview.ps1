#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/damage-compositions-preview'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 4 -or ($manifest.path -join ',') -cne 'player.mt2f,enemy.mt2f,player-boundaries.mt2f,enemy-boundaries.mt2f') { throw 'Unexpected damage-composition preview manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'Damage-composition archive hash/size differs.' }
    $mode = if ($entry.path -like '*-boundaries.mt2f') { '--damage-composition-boundaries-preview-only' } else { '--damage-composition-card-preview-only' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') $mode $path
    if ($LASTEXITCODE) { throw "Independent damage-composition validation failed: $($entry.path)" }
}
Write-Output 'GAME-231-DAMAGE-COMPOSITIONS-PREVIEW PASS: 1152 player/enemy mixed-target and multiple-main hovers, fixed/ranged damage, main-driven drop overrides, cancelled continuation with preserved OnCast, living room-wide drop candidates and continuous death queues; complete native actors, copy/effect/cancel states, exact RNG and 16 immutable branches each. Linked damage, multiple OnCast effects, traits and other callbacks remain separate.'
