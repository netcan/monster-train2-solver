#requires -Version 7.4
[CmdletBinding()]
param(
    [switch] $SkipBuild,
    [ValidateSet('All', 'Hover', 'Battle')]
    [string] $Case = 'All',
    [ValidateSet('All', 'Player', 'Enemy')]
    [string] $Team = 'All'
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/oncast-groups'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 3 -or ($manifest.path -join ',') -cne 'player.mt2f,enemy.mt2f,battle.mt2f') { throw 'Unexpected OnCast groups manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'OnCast groups archive hash/size differs.' }
}
$selected = @($manifest | Where-Object {
    if ($_.path -eq 'battle.mt2f') { $Case -ne 'Hover' }
    else { $Case -ne 'Battle' -and ($Team -eq 'All' -or $_.path -ieq "$Team.mt2f") }
})
foreach ($entry in $selected) {
    $flag = if ($entry.path -eq 'battle.mt2f') { '--oncast-groups-battle-only' } else { '--oncast-groups-hover-only' }
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') $flag (Join-Path $directory $entry.path)
    if ($LASTEXITCODE) { throw "Independent OnCast groups validation failed: $($entry.path)" }
}
Write-Output "GAME-231-ONCAST-GROUPS PASS: $($selected.path -join ', '); native group cancellation, persistent target history and full successors/RNG."
