#requires -Version 7.4
[CmdletBinding()]
param(
    [ValidateSet('AllyBuffer', 'Fragile', 'Corrupter')][string[]] $Case = @('AllyBuffer', 'Fragile', 'Corrupter'),
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
if (-not $SkipBuild) {
    dotnet build (Join-Path $workspace 'src/ModelChecks/ModelChecks.csproj') -c Release --no-restore -p:WarningLevel=0 -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Trial checker build failed.' }
}
$checker = Join-Path $workspace 'src/ModelChecks/bin/Release/net8.0/ModelChecks.dll'
$manifest = Import-Csv -LiteralPath (Join-Path $workspace 'tests/fixtures/manifest.tsv') -Delimiter "`t"
$pyre = @{ AllyBuffer = 7105; Fragile = 7961; Corrupter = 8745 }
foreach ($variant in $Case) {
    $stem = $variant.ToLowerInvariant()
    $archives = @(
        "full-battle-outer-boss-trial-$stem-casting.mt2f",
        "outer-boss-trial-$stem-birth-calibration.mt2f",
        "outer-boss-trial-$stem-phase-calibration.mt2f",
        "boss-action-trial-$stem-controller-calibration.mt2f",
        "full-battle-outer-boss-trial-$stem-replay.mt2f",
        "outer-boss-trial-$stem-relentless-turn-calibration.mt2f",
        "outer-boss-trial-$stem-relentless-phase-calibration.mt2f"
    )
    $paths = @(foreach ($archive in $archives) {
        $path = Join-Path $workspace "tests/fixtures/$archive"
        $rows = @($manifest | Where-Object archive -CEQ $archive)
        if ($rows.Count -ne 1 -or (Get-Item -LiteralPath $path).Length -ne [long]$rows[0].archive_bytes -or
            (Get-FileHash -LiteralPath $path).Hash -ine $rows[0].archive_sha256) {
            throw "Trial archive metadata differs: $archive"
        }
        $path
    })
    dotnet $checker --outer-boss-trial-only $variant $paths[0] $paths[1] $paths[2] $paths[3]
    if ($LASTEXITCODE -ne 0) { throw "Trial $variant native calibration failed." }
    dotnet $checker --outer-boss-trial-pair-only $variant $paths[0] $paths[4]
    if ($LASTEXITCODE -ne 0) { throw "Trial $variant independent replay failed." }
    dotnet $checker --outer-boss-trial-relentless-only $variant $paths[5] $paths[6] $pyre[$variant]
    if ($LASTEXITCODE -ne 0) { throw "Trial $variant controlled Relentless calibration failed." }
}
Write-Output "OUTER-BOSS-TRIAL-FOCUSED PASS cases=$($Case -join ','); no full regression executed."
