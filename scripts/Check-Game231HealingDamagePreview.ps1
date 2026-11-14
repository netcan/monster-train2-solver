#requires -Version 7.4
[CmdletBinding()]
param(
    [switch] $SkipBuild,
    [ValidateSet('All', 'Player', 'Enemy')]
    [string] $Team = 'All'
)

$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE) { throw 'ModelChecks build failed.' }
}
$directory = Join-Path $workspace 'tests/compatibility/game-2.3.1/mechanics/healing-damage-preview'
$manifest = @(Import-Csv -LiteralPath (Join-Path $directory 'manifest.tsv') -Delimiter "`t")
if ($manifest.Count -ne 2 -or ($manifest.path -join ',') -cne 'player.mt2f,enemy.mt2f') { throw 'Unexpected healing/damage preview manifest.' }
foreach ($entry in $manifest) {
    $path = Join-Path $directory $entry.path
    if ((Get-Item -LiteralPath $path).Length -ne [long]$entry.bytes -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $entry.sha256) { throw 'Healing/damage archive hash/size differs.' }
}
$selected = @($manifest | Where-Object { $Team -eq 'All' -or $_.path -ieq "$Team.mt2f" })
foreach ($entry in $selected) {
    dotnet (Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll') --healing-damage-card-preview-only (Join-Path $directory $entry.path)
    if ($LASTEXITCODE) { throw "Independent healing/damage validation failed: $($entry.path)" }
}
Write-Output "GAME-231-HEALING-DAMAGE-PREVIEW PASS: $($selected.Count * 640) native hovers ($Team), pure healing, ordered damage/healing and main/OnCast compositions, zero/immunity callbacks, post-lethal healing, full temporary actors and copy/effect/cancel states, exact RNG and 16 immutable branches each. Linked healing, traits, upgrades and other callbacks remain separate."
