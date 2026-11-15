#requires -Version 7.4
[CmdletBinding()]
param([switch] $SkipBuild, [switch] $WithBattleReplay)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/room-health-card-hover'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 1 -or $manifest[0].path -cne 'hover.mt2f') { throw 'Unexpected room-health hover manifest.' }
$path = Join-Path $directory 'hover.mt2f'
if ((Get-Item -LiteralPath $path).Length -ne [long]$manifest[0].bytes -or
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $manifest[0].sha256) { throw 'Room-health hover archive hash/size differs.' }
dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --room-health-card-hover-only $path
if ($LASTEXITCODE) { throw 'Independent room-health hover validation failed.' }
if ($WithBattleReplay) {
    & (Join-Path $PSScriptRoot 'Check-Game231RoomHealthAttack.ps1') -SkipBuild -LifecycleOnly
    if ($LASTEXITCODE) { throw 'Original aura battle replay failed.' }
}
Write-Output 'GAME-231-ROOM-HEALTH-CARD-HOVER PASS: 49 native previews, original carriers, exact dynamic attacks, full copied/restored state and immutable branches; ability inputs remain separate.'
